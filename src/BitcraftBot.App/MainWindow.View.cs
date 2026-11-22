using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Perception.Screen;

namespace BitcraftBot.App;

// v0.4.3 (R1 bis, D-052) : en jeu, le bot lit la direction de la caméra (cône de la mini-carte) et voit les fenêtres
// du jeu ouvertes (inventaire, chariot, carte…). AUCUNE action : il l'affiche et l'écrit dans le journal.
// Appelé par le fil « Ce que voit le bot » (une fois par seconde).
public partial class MainWindow
{
    // Ce que voit le bot en jeu à un instant donné.
    private sealed record GameView(DateTime At, CameraHeading? Heading, string HeadingWhy, IReadOnlyList<OpenWindow> Windows);

    private volatile GameView? _view;
    private double? _loggedHeading;
    private string? _headingCandidateWhy;
    private double? _headingCandidate;
    private string _windowsKey = "";
    private string _windowsCandidate = "";
    private string _windowsLogged = "";
    private IReadOnlyList<OpenWindow> _windowsWithTitles = [];

    // Direction lue il y a moins de 2 s (sert à noter la direction au moment d'un clic de Julien).
    private double? FreshHeading(DateTime at) =>
        _view is { Heading: { } h } v && at - v.At < TimeSpan.FromSeconds(2) ? h.Bearing : null;

    private void WatchView(DateTime now, Frame frame, OcrReader? ocr)
    {
        var heading = MinimapCone.Read(frame, out var why);
        var windows = GameWindows.Detect(frame);

        // Titres lus seulement quand les fenêtres changent (la lecture de texte coûte plus cher).
        var key = string.Join("|", windows.Select(w => $"{w.Kind}:{w.Area.X / 48}:{w.Area.Y / 48}:{w.Area.Width / 48}:{w.Area.Height / 48}"));
        if (key != _windowsKey)
        {
            _windowsKey = key;
            _windowsWithTitles = ocr is null
                ? windows
                : windows.Select(w => GameWindows.ReadTitleAsync(ocr, frame, w).GetAwaiter().GetResult()).ToList();
        }
        _view = new GameView(now, heading, why, _windowsWithTitles);
        LogHeading(heading, why);
        LogWindows(key);
        Dispatcher.BeginInvoke(ShowView);
    }

    private void ClearView()
    {
        if (_view is null)
        {
            return;
        }
        _view = null;
        _windowsKey = "";
        Dispatcher.BeginInvoke(ShowView);
    }

    // Journal : un changement de direction de plus de 20°, vu 2 fois de suite (à 10° près).
    private void LogHeading(CameraHeading? heading, string why)
    {
        if (heading is null)
        {
            if (_loggedHeading is not null && _headingCandidateWhy == why)
            {
                _log.Detail("Caméra", $"Direction de la caméra pas lue : {why}.");
                _loggedHeading = null;
            }
            _headingCandidateWhy = why;
            _headingCandidate = null;
            return;
        }
        _headingCandidateWhy = null;
        if (_headingCandidate is { } c && CameraHeading.Gap(c, heading.Bearing) <= 10
            && (_loggedHeading is not { } l || CameraHeading.Gap(l, heading.Bearing) > 20))
        {
            _log.Detail("Caméra", $"Caméra {heading.Toward} (cône de la mini-carte, {(heading.Confidence >= 0.99 ? "deux bords vus" : "un bord vu")}).");
            _loggedHeading = heading.Bearing;
        }
        _headingCandidate = heading.Bearing;
    }

    // Journal : fenêtres ouvertes ou fermées, vues 2 fois de suite.
    private void LogWindows(string key)
    {
        if (key != _windowsCandidate)
        {
            _windowsCandidate = key;
            return;
        }
        if (key == _windowsLogged)
        {
            return;
        }
        _windowsLogged = key;
        _log.Info("Écran", _windowsWithTitles.Count == 0
            ? "Plus aucune fenêtre du jeu ouverte."
            : $"Fenêtre(s) du jeu ouverte(s) : {string.Join(", ", _windowsWithTitles.Select(w => w.French))}.");
    }

    private void ShowView()
    {
        var v = _view;
        if (v is null)
        {
            HeadingText.Text = "—";
            HeadingText.Foreground = Theme("Muted");
            WindowsText.Text = "—";
            WindowsText.Foreground = Theme("Muted");
            return;
        }
        HeadingText.Text = v.Heading is { } h ? h.Toward : $"pas lue ({v.HeadingWhy})";
        HeadingText.Foreground = Theme(v.Heading is null ? "Warn" : "Good");
        WindowsText.Text = v.Windows.Count == 0 ? "aucune" : string.Join(", ", v.Windows.Select(w => w.French));
        WindowsText.Foreground = Theme(v.Windows.Count == 0 ? "Good" : "Warn");
    }
}
