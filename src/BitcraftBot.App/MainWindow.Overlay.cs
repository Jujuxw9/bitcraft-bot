using System.Windows;
using System.Windows.Media;
using BitcraftBot.Core.Monitoring;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Server;

namespace BitcraftBot.App;

// Overlay sur l'écran du jeu (demande de Julien, 2026-11-19) : le bot est-il connecté ? lit-il l'écran ?
// et rappel des touches. Visible seulement quand le jeu est visible (pas par-dessus un autre programme).
public partial class MainWindow
{
    private static readonly Brush OnBrush = Frozen(129, 201, 149);
    private static readonly Brush WaitBrush = Frozen(253, 214, 99);
    private static readonly Brush OffBrush = Frozen(170, 170, 170);

    private OverlayWindow? _overlay;

    private void InitOverlay()
    {
        OverlayCheck.IsChecked = _settings.OverlayEnabled;
    }

    private void OverlayCheck_Click(object sender, RoutedEventArgs e)
    {
        _settings.OverlayEnabled = OverlayCheck.IsChecked == true;
        _log.Info("Config", $"Overlay sur l'écran du jeu : {(_settings.OverlayEnabled ? "affiché" : "masqué")}.");
        UpdateOverlay();
    }

    // Appelé toutes les 500 ms (minuterie de l'onglet Personnage).
    private void UpdateOverlay()
    {
        // Recherche de la fenêtre du jeu partagée avec le contrôleur d'actions (v0.3.19).
        var game = CurrentGame();

        var visible = _settings.OverlayEnabled && game is not null
                      && (_inputSender.IsForeground(game.Handle) || GameVisibility.IsUncovered(game));
        if (!visible)
        {
            _overlay?.Hide();
            return;
        }

        _overlay ??= new OverlayWindow();
        var state = _serverReader?.State ?? ReaderState.Deconnecte;
        var (server, serverColor) = state switch
        {
            ReaderState.Connecte => ("● Serveur", OnBrush),
            ReaderState.Connexion => ("◌ Serveur", WaitBrush),
            _ => ("○ Serveur", OffBrush),
        };
        var (screen, screenColor) = _safety.IsRunning
            ? (_gameVisible ? ("● Écran", OnBrush) : ("◌ Écran", WaitBrush))
            : ("○ Écran", OffBrush);
        _overlay.Show(server, serverColor, screen, screenColor);
        // Toujours affiché (demande de Julien, v0.3.16) : repas automatique activé ou non.
        _overlay.ShowMeal(_settings.AutoEatEnabled ? "● Repas" : "○ Repas", _settings.AutoEatEnabled ? _mealOverlayBrush : OffBrush);
        _overlay.ShowSystem(_lastResourceSample is { } sample ? SystemSummary.Overlay(sample) : null);
        if (!_overlay.IsVisible)
        {
            _overlay.Show();
        }
        _overlay.PlaceBottomCenter(game!.ClientArea.X, game.ClientArea.Y, game.ClientArea.Width, game.ClientArea.Height);
    }

    private void CloseOverlay()
    {
        _overlay?.Close();
        _overlay = null;
    }

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
