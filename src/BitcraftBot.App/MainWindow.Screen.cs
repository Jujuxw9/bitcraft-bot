using System.Diagnostics;
using System.IO;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Perception.Screen;

namespace BitcraftBot.App;

// Ce que voit le bot (v0.4.1, demande de Julien) : bureau de Windows, autre programme, jeu réduit, ou le jeu,
// et dans le jeu : accueil, connexion, chargement, en jeu. Tourne tout le temps (même sans « Démarrer »),
// une fois par seconde, sur son propre fil. L'image n'est regardée que si le jeu est visible
// (jamais une autre fenêtre de Julien).
public partial class MainWindow
{
    private static readonly TimeSpan ScreenInterval = TimeSpan.FromSeconds(1);

    // Une capture d'un écran inconnu au plus par minute, pour apprendre ses mots (images/analyse).
    private static readonly TimeSpan UnknownCaptureInterval = TimeSpan.FromMinutes(1);

    private readonly OcrReader? _ocr;
    private volatile ScreenSituation? _situation;
    private ScreenSituation? _candidate;
    private ScreenReading? _lastReading;
    private CancellationTokenSource? _screenCts;
    private readonly HashSet<GameScreen> _screensCaptured = [];
    private DateTime _lastUnknownCapture = DateTime.MinValue;
    private string? _lastUnknownText;

    // Réglages du jeu lus au démarrage (registre, lecture seule, liste blanche : jamais le jeton ni l'e-mail).
    private void CheckGameSettings()
    {
        try
        {
            var checks = GameSettings.Read();
            if (checks.Count == 0)
            {
                _log.Info("Réglages du jeu", "Réglages de BitCraft introuvables (jeu jamais lancé sur ce PC ?).");
                return;
            }
            foreach (var c in checks.Where(c => !c.Ok))
            {
                var message = $"« {c.French} » vaut {c.Value ?? "?"} (conseillé : {c.Expected}) : {c.Why}.";
                if (c.Required)
                {
                    _log.Alerte("Réglages du jeu", message);
                }
                else
                {
                    _log.Info("Réglages du jeu", message);
                }
            }
            var ok = checks.Count(c => c.Ok);
            _log.Info("Réglages du jeu", $"{ok} réglage(s) sur {checks.Count} comme conseillé ({string.Join(", ", checks.Where(c => c.Ok).Select(c => c.French))}).");
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            _log.Detail("Réglages du jeu", $"Lecture impossible : {ex.Message}");
        }
    }

    private void InitScreenWatch(OcrReader? ocr)
    {
        CheckGameSettings();
        _screenCts = new CancellationTokenSource();
        var token = _screenCts.Token;
        var classifier = ocr is null ? null : new GameScreenClassifier(ocr);
        new Thread(() => ScreenLoop(classifier, token)) { IsBackground = true, Name = "Ce que voit le bot", Priority = ThreadPriority.BelowNormal }.Start();
    }

    private void CloseScreenWatch() => _screenCts?.Cancel();

    private void ScreenLoop(GameScreenClassifier? classifier, CancellationToken token)
    {
        var locator = new GameWindowLocator();
        var capturer = new ScreenCapturer();
        var own = Process.GetCurrentProcess().ProcessName;
        GameWindow? game = null;
        var lastLocate = DateTime.MinValue;
        while (!token.WaitHandle.WaitOne(ScreenInterval))
        {
            try
            {
                var now = DateTime.Now;
                if (game is null || now - lastLocate >= TimeSpan.FromSeconds(3))
                {
                    // La recherche ignore les fenêtres réduites : on garde la dernière fenêtre connue pour le dire.
                    game = locator.Find() ?? game;
                    lastLocate = now;
                }
                if (game is not null && !ScreenSituations.Exists(game))
                {
                    game = null;
                }
                var running = game is not null;
                var minimized = running && ScreenSituations.IsMinimized(game!);
                var visible = running && !minimized && GameVisibility.IsUncovered(game!);
                var situation = ScreenSituations.Decide(running, minimized, visible, ScreenSituations.Foreground(), own);
                if (situation.Place == Place.Jeu && classifier is not null)
                {
                    var area = GameWindowLocator.GetClientArea(game!.Handle);
                    var frame = _latestFrame is { } f && now - f.CapturedAt < TimeSpan.FromSeconds(1.5) && f.Source == area ? f : capturer.Capture(area);
                    var reading = classifier.ClassifyAsync(frame).GetAwaiter().GetResult();
                    situation = situation with { Game = reading.Screen, Evidence = reading.Evidence };
                    _lastReading = reading;
                    KeepScreenCapture(frame, reading);
                }
                OnSituation(situation);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
            {
                _log.Detail("Écran", $"Lecture de ce que voit le bot impossible : {ex.Message}");
            }
        }
    }

    // Une situation n'est retenue que si elle est vue 2 fois de suite (évite les clignotements, ex. fondu du chargement).
    private void OnSituation(ScreenSituation s)
    {
        if (!s.SameAs(_candidate))
        {
            _candidate = s;
            return;
        }
        if (s.SameAs(_situation))
        {
            _situation = s;
            return;
        }
        _situation = s;
        var text = s.Place == Place.Jeu && s.Game == GameScreen.Inconnu && _lastReading?.Text is { Length: > 0 } t
            ? $"{s.Describe()} ({s.Evidence}). Texte lu : « {t} »"
            : $"{s.Describe()} ({s.Evidence}).";
        _log.Info("Écran", text);
        Dispatcher.BeginInvoke(ShowSituation);
    }

    private void ShowSituation()
    {
        var s = _situation;
        SeenText.Text = s?.Describe() ?? "—";
        SeenText.Foreground = Theme(s is null ? "Muted" : s.InGame ? "Good" : s.Place == Place.Jeu ? "Warn" : "Muted");
    }

    // Garde une capture de chaque écran du jeu reconnu (une fois par lancement du bot) et des écrans inconnus
    // (au plus une par minute, texte différent) : elles servent à vérifier et corriger la reconnaissance.
    private void KeepScreenCapture(Frame frame, ScreenReading reading)
    {
        string? name = null;
        if (reading.Screen is GameScreen.Accueil or GameScreen.Connexion or GameScreen.Chargement && _screensCaptured.Add(reading.Screen))
        {
            name = reading.Screen switch
            {
                GameScreen.Accueil => "ecran-accueil",
                GameScreen.Connexion => "ecran-connexion",
                _ => "ecran-chargement",
            };
        }
        else if (reading.Screen == GameScreen.Inconnu && DateTime.Now - _lastUnknownCapture >= UnknownCaptureInterval && reading.Text != _lastUnknownText)
        {
            _lastUnknownCapture = DateTime.Now;
            _lastUnknownText = reading.Text;
            name = "ecran-inconnu";
        }
        if (name is null)
        {
            return;
        }
        var folder = FindAnalysisFolder() ?? Path.Combine(AppContext.BaseDirectory, "captures");
        var path = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd}_analyse_{name}_{DateTime.Now:HHmmss}.png");
        FrameSaver.SavePngAsync(frame, path).GetAwaiter().GetResult();
        _log.Detail("Écran", $"Capture gardée pour vérifier la reconnaissance : {path}");
    }
}
