using System.Windows;
using BitcraftBot.Input.Actions;
using BitcraftBot.Input.Human;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Reprise automatique des récoltes (v0.4.24, demande de Julien) : voir HarvestResume. Ici, l'interrupteur, le journal
// et l'envoi des gestes par le contrôleur d'actions, avec toutes ses sécurités
// (jeu au premier plan, écran « en jeu », chat fermé, pause quand Julien tape, souris bougée = arrêt).
// v0.4.25 : plus de Tab. Le bot retient le dernier clic de Julien dans le jeu ; pour reprendre, il pose la souris sur ce
// point (sans cliquer), attend 150 ms, regarde si le contour vert du jeu apparaît (seulement noté dans le journal :
// à vérifier), puis appuie sur M. Le serveur confirme ensuite que la bonne ressource est frappée.
public partial class MainWindow
{
    private static readonly TimeSpan HoverWait = TimeSpan.FromMilliseconds(150);
    private const int OutlineBox = 24;

    private readonly HarvestResume _resume = new();
    private readonly HumanMouse _resumeMouse = new(new Random());
    private string _resumeState = "";
    private ulong? _loggedGameTarget;
    private ClickPoint? _lastGameClick;
    private int _cameraMoves;
    private bool _resumeBusy;

    private void InitResume()
    {
        ResumeCheck.IsChecked = _settings.AutoResumeEnabled;
        _log.Info("Reprise", _settings.AutoResumeEnabled
            ? "Reprise des récoltes activée : une récolte arrêtée faute d'endurance est relancée quand l'endurance revient (souris posée sur le point de votre clic, puis M, sans bouger)."
            : "Reprise des récoltes coupée (interrupteur dans OPTIONS).");
    }

    private void ResumeCheck_Click(object sender, RoutedEventArgs e)
    {
        _settings.AutoResumeEnabled = ResumeCheck.IsChecked == true;
        _settings.Save(LocalSettings.DefaultPath);
        _log.Info("Reprise", _settings.AutoResumeEnabled ? "Reprise des récoltes activée (case cochée)." : "Reprise des récoltes coupée (case décochée).");
        if (_settings.AutoResumeEnabled && _actions.IsHalted)
        {
            _actions.Resume("reprise des récoltes réactivée");
        }
    }

    // Clic de Julien dans la fenêtre du jeu (écoute en lecture seule) : position écran + position du personnage.
    private void RememberGameClick(DateTime at, int x, int y)
    {
        _lastGameClick = new ClickPoint(x, y, new DateTimeOffset(at.ToUniversalTime(), TimeSpan.Zero), _lastSnapshot?.Position, _cameraMoves);
    }

    // Appelé toutes les 500 ms avec la dernière lecture du serveur.
    private void RunResume(ServerReader reader, PlayerSnapshot s)
    {
        var connected = reader.State == ReaderState.Connecte && s.SignedIn == true;
        // Journal : cible sélectionnée dans le jeu.
        if (connected && s.Target != _loggedGameTarget)
        {
            _loggedGameTarget = s.Target;
            var name = s.Target is { } t ? s.Resources.FirstOrDefault(r => r.EntityId == t)?.Name ?? $"entité {t} (pas une ressource de la zone)" : "aucune";
            _log.Detail("Reprise", $"Cible sélectionnée dans le jeu : {name}.");
        }
        var hadPoint = _resume.Point;
        var decision = _resume.Update(new ResumeInput
        {
            Now = DateTimeOffset.UtcNow,
            Enabled = _settings.AutoResumeEnabled && connected,
            Action = connected ? s.Action : null,
            LastExtractTarget = s.LastExtract?.Target,
            Position = s.Position,
            Stamina = s.Stamina,
            MaxStamina = s.MaxStamina,
            Target = s.Target,
            Resources = s.Resources,
            // Sac plein : pas bloquant (le chariot peut prendre la suite) ; un M sans effet deux fois arrête la reprise.
            BagFull = false,
            LastClick = _lastGameClick,
            CameraMoves = _cameraMoves,
        });
        if (_resume.Point is { } learned && !ReferenceEquals(learned, hadPoint))
        {
            _log.Detail("Reprise", $"Point de la souris retenu pour la reprise : ({learned.X}, {learned.Y}).");
        }
        if (decision.Log is { } line)
        {
            if (decision.Alert) _log.Alerte("Reprise", line);
            else _log.Info("Reprise", line);
        }
        var state = decision.State;
        if (decision.Step == ResumeStep.Interact)
        {
            if (_resumeBusy || _actions.WhyNot() is { } wait)
            {
                // Le contrôleur refuse pour l'instant : on refera l'essai au prochain passage, sans compter d'échec.
                _resume.NotSent();
                state = $"{decision.State} attend : {(_resumeBusy ? "geste en cours" : _actions.WhyNot())}";
            }
            else
            {
                _ = HoverAndInteractAsync(decision.X, decision.Y);
            }
        }
        if (state != _resumeState)
        {
            _resumeState = state;
            ResumeStatusText.Text = _settings.AutoResumeEnabled ? state : "Coupé.";
            _log.Detail("Reprise", state);
        }
    }

    // Souris posée sur le point (sans clic), courte attente, contour vert regardé (journal seulement), puis M.
    private async Task HoverAndInteractAsync(int x, int y)
    {
        _resumeBusy = true;
        try
        {
            if (CurrentGame() is not { } game)
            {
                _resume.NotSent();
                return;
            }
            var c = GameWindowLocator.GetClientArea(game.Handle);
            var area = new SafeArea(c.X, c.Y, c.X + c.Width - 1, c.Y + c.Height - 1);
            var before = GreenAround(x, y);
            var moved = await _actions.MoveAsync("Reprise : souris sur la ressource", x, y, area, _resumeMouse);
            if (!moved.Done)
            {
                _log.Info("Reprise", $"Souris pas déplacée : {moved.Detail}.");
                _resume.NotSent();
                return;
            }
            await Task.Delay(HoverWait);
            var after = GreenAround(x, y);
            _log.Detail("Reprise", before is { } b && after is { } a
                ? $"Contour vert autour du point (à vérifier) : {b} → {a} pixels verts{(a >= b + 12 ? ", contour probablement vu" : ", pas de contour net")}."
                : "Contour vert : capture impossible.");
            var (cx, cy) = _inputSender.CursorPosition();
            if (Math.Abs(cx - x) > 4 || Math.Abs(cy - y) > 4)
            {
                _log.Info("Reprise", "Souris bougée avant M : touche pas envoyée.");
                _resume.NotSent();
                return;
            }
            var pressed = await _actions.PressAsync("Reprise : Interact (M)", GameKeys.Interact);
            if (!pressed.Done)
            {
                _log.Info("Reprise", $"Touche pas envoyée (Interact) : {pressed.Detail}.");
                _resume.NotSent();
            }
        }
        finally
        {
            _resumeBusy = false;
        }
    }

    // Nombre de pixels vert vif dans un petit carré autour du point (contour du jeu sur l'élément survolé).
    private int? GreenAround(int x, int y)
    {
        try
        {
            var frame = new ScreenCapturer().Capture(new ScreenRect(x - OutlineBox, y - OutlineBox, OutlineBox * 2, OutlineBox * 2));
            return VividGreenPixels(frame.Pixels);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    // Pixels BGRA : vert nettement au-dessus du rouge et du bleu.
    internal static int VividGreenPixels(byte[] bgra)
    {
        var count = 0;
        for (var i = 0; i + 3 < bgra.Length; i += 4)
        {
            int b = bgra[i], g = bgra[i + 1], r = bgra[i + 2];
            if (g >= 150 && g - r >= 60 && g - b >= 60)
            {
                count++;
            }
        }
        return count;
    }
}
