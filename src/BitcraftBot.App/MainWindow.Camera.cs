using System.IO;
using System.Text.Json;
using System.Windows;
using BitcraftBot.Input.Actions;
using BitcraftBot.Input.Mouse;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Server.Aim;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// R1 (v0.4.0) : le bot apprend la caméra en regardant Julien jouer. AUCUNE action envoyée au jeu.
// Écoute de la souris (lecture seule, D-049) : clics droits de Julien dans le jeu, molette, bouton du milieu.
// Écoute du clavier (déjà là) : W = caméra recentrée ; Q / D = caméra tournée.
// Les clics appris sont gardés dans camera-clics.json, à côté du .exe : l'apprentissage continue d'une session à l'autre.
public partial class MainWindow
{
    public const string CameraFileName = "camera-clics.json";

    private readonly CameraLearner _camera = new();
    private GlobalMouseHook? _mouseHook;

    private static string CameraFilePath => Path.Combine(AppContext.BaseDirectory, CameraFileName);

    private void InitCamera()
    {
        try
        {
            if (File.Exists(CameraFilePath))
            {
                _camera.Load(JsonSerializer.Deserialize<List<CameraPair>>(File.ReadAllText(CameraFilePath)) ?? []);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _log.Alerte("Caméra", $"Clics appris illisibles ({ex.Message}) : on repart de zéro.");
        }
        _log.Info("Caméra", _camera.Pairs.Count > 0
            ? $"{_camera.Pairs.Count} clic(s) déjà appris. {_camera.Summary()}"
            : "Apprentissage de la caméra : couper des arbres à la main (clic droit), caméra recentrée avec W.");
        try
        {
            _mouseHook = new GlobalMouseHook();
            _mouseHook.ButtonDown += (button, x, y, injected) =>
            {
                var at = DateTime.Now;
                if (!injected)
                {
                    Dispatcher.BeginInvoke(() => OnMouseButton(at, button, x, y));
                }
            };
            _mouseHook.Wheel += injected =>
            {
                var at = DateTime.Now;
                if (!injected)
                {
                    Dispatcher.BeginInvoke(() => OnCameraMoved(at, "molette"));
                }
            };
            _log.Info("Caméra", "Écoute de la souris active (lecture seule) : clics droits dans le jeu, molette.");
        }
        catch (InvalidOperationException ex)
        {
            _log.Erreur("Caméra", $"Écoute de la souris impossible : {ex.Message}. La caméra ne sera pas apprise.");
        }
        ShowCamera();
    }

    private void CloseCamera()
    {
        _mouseHook?.Dispose();
        _mouseHook = null;
    }

    // Touches de caméra, tapées par Julien ou par le bot (plus tard, R3) : seulement si le jeu est la fenêtre active.
    private bool _chatKeyLogged;

    private void OnCameraKey(int scanCode)
    {
        if (scanCode != GameKeys.W && scanCode != GameKeys.Q && scanCode != GameKeys.D)
        {
            return;
        }
        if (!GameIsForeground())
        {
            return;
        }
        // v0.4.5 : chat ouvert = la lettre est tapée dans le chat, la caméra ne bouge pas (W du 23/11 à 22:32).
        if (ChatLikelyOpen(DateTime.Now))
        {
            if (!_chatKeyLogged)
            {
                _chatKeyLogged = true;
                _log.Detail("Caméra", "W, Q ou D ignorée : le chat du jeu est ouvert (lettre tapée dans le chat).");
            }
            return;
        }
        _chatKeyLogged = false;
        if (scanCode == GameKeys.W)
        {
            var was = _camera.State;
            _camera.OnReset(DateTime.Now);
            CheckAfterW();
            if (was != CameraState.Recentree)
            {
                _log.Detail("Caméra", "W : caméra recentrée, clics droits écoutés.");
            }
        }
        else
        {
            OnCameraMoved(DateTime.Now, scanCode == GameKeys.Q ? "Q" : "D");
            return;
        }
        ShowCamera();
    }

    private void OnCameraMoved(DateTime at, string by)
    {
        if (!GameIsForeground())
        {
            return;
        }
        if (_camera.OnMoved(at, by) is { } e)
        {
            WriteCameraEvent(e);
        }
        ShowCamera();
    }

    private void OnMouseButton(DateTime at, MouseButton button, int x, int y)
    {
        if (button == MouseButton.Gauche || !GameIsForeground() || CurrentGame() is not { } game)
        {
            return;
        }
        if (button == MouseButton.Milieu)
        {
            OnCameraMoved(at, "bouton du milieu");
            return;
        }
        var area = GameWindowLocator.GetClientArea(game.Handle);
        if (area.IsEmpty || x < area.X || y < area.Y || x >= area.X + area.Width || y >= area.Y + area.Height)
        {
            return;
        }
        if (_camera.OnRightClick(at, x - area.X, y - area.Y, area.Width, area.Height, _lastSnapshot, FreshHeading(at)) is { } e)
        {
            WriteCameraEvent(e);
        }
        ShowCamera();
    }

    // Appelé à chaque lecture du serveur (500 ms), liaison active.
    private void TrackCamera(PlayerSnapshot s)
    {
        var last = _camera.LastPair;
        var waiting = _camera.WaitingConfirmation;
        foreach (var e in _camera.Update(DateTime.Now, s))
        {
            WriteCameraEvent(e);
        }
        var learned = !ReferenceEquals(last, _camera.LastPair);
        if (learned)
        {
            SaveCamera();
        }
        if (learned || waiting != _camera.WaitingConfirmation)
        {
            ShowCamera();
        }
    }

    private void SaveCamera()
    {
        try
        {
            File.WriteAllText(CameraFilePath, JsonSerializer.Serialize(_camera.Pairs, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.Alerte("Caméra", $"Clics appris pas enregistrés : {ex.Message}");
        }
    }

    private void WriteCameraEvent(CameraEvent e)
    {
        if (e.Important)
        {
            _log.Info("Caméra", e.Message);
        }
        else
        {
            _log.Detail("Caméra", e.Message);
        }
    }

    private bool GameIsForeground() => CurrentGame() is { } g && _inputSender.IsForeground(g.Handle);

    // Carte « Caméra » de l'onglet Paramètres.
    private void ShowCamera()
    {
        CameraStateText.Text = _camera.State switch
        {
            CameraState.Recentree => "Caméra recentrée (W)",
            CameraState.Tournee => $"Caméra tournée ({_camera.MovedBy}) : appuyer sur W",
            _ => "Caméra : W pas encore vu",
        };
        CameraStateText.Foreground = Theme(_camera.State == CameraState.Tournee ? "Warn" : _camera.State == CameraState.Recentree ? "Good" : "Muted");
        CameraCountText.Text = $"Clics appris : {_camera.Pairs.Count}{(_camera.WaitingConfirmation ? " (un clic attend la récolte)" : "")}";
        if (_camera.Fit is { MedianErrorPx: { } m } f)
        {
            CameraPrecisionText.Text = f.Reliable
                ? $"Précision : {m:0} px (but : moins de 25 px)"
                : $"Pas fiable : {f.Used} clics gardés, {f.Rejected} écartés";
            CameraPrecisionText.Foreground = Theme(f.Reliable && m <= 25 ? "Good" : "Warn");
            CameraFormulaText.Text = $"Formule {(f.Formula == CameraFormula.Plate ? "plate" : "perspective")}, {f.Used} clic(s) gardé(s)"
                                     + (f.Rejected > 0 ? $", {f.Rejected} écarté(s)" : "");
        }
        else
        {
            CameraPrecisionText.Text = "Précision : — (5 clics minimum)";
            CameraPrecisionText.Foreground = Theme("Muted");
            CameraFormulaText.Text = _camera.Fit is null ? "" : $"Formule : {_camera.Fit.Used} clic(s)";
        }
        CameraLastText.Text = _camera.LastPair is { } p
            ? $"Dernier : {p.Resource}{(_camera.LastPredictionErrorPx is { } e ? $", écart {e:0} px" : "")}"
            : "";
    }

    private void CameraForgetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_camera.Pairs.Count == 0)
        {
            return;
        }
        var answer = MessageBox.Show(this, $"Oublier les {_camera.Pairs.Count} clics appris ?", "Caméra", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }
        _camera.Forget();
        SaveCamera();
        _log.Info("Caméra", "Clics appris oubliés (bouton).");
        ShowCamera();
    }
}
