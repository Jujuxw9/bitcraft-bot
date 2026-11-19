using System.Windows;
using System.Windows.Media;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Server;

namespace BitcraftBot.App;

// Overlay sur l'écran du jeu (demande de Julien, 2026-11-19) : le bot est-il connecté ? lit-il l'écran ?
// et rappel des touches. Visible seulement quand le jeu est visible (pas par-dessus un autre programme).
public partial class MainWindow
{
    private static readonly TimeSpan OverlayLocateInterval = TimeSpan.FromSeconds(2);
    private static readonly Brush OnBrush = Frozen(129, 201, 149);
    private static readonly Brush WaitBrush = Frozen(253, 214, 99);
    private static readonly Brush OffBrush = Frozen(170, 170, 170);

    private OverlayWindow? _overlay;
    private GameWindow? _overlayGame;
    private DateTime _overlayLocatedAt = DateTime.MinValue;

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
        var game = _gameWindow;
        if (game is null && DateTime.Now - _overlayLocatedAt >= OverlayLocateInterval)
        {
            _overlayLocatedAt = DateTime.Now;
            _overlayGame = new GameWindowLocator().Find();
        }
        game ??= _overlayGame;

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
            ReaderState.Connecte => ("● Bot connecté au serveur (lecture)", OnBrush),
            ReaderState.Connexion => ("◌ Bot : connexion au serveur…", WaitBrush),
            _ => ("○ Bot pas connecté au serveur", OffBrush),
        };
        var (screen, screenColor) = _safety.IsRunning
            ? (_gameVisible ? ("● Lecture de l'écran en cours", OnBrush) : ("◌ Lecture de l'écran en pause (jeu caché)", WaitBrush))
            : ("○ Lecture de l'écran arrêtée", OffBrush);
        _overlay.Show(server, serverColor, screen, screenColor);
        _overlay.ShowMeal(_settings.AutoEatEnabled ? MealOverlayText : null, _mealOverlayBrush);
        if (!_overlay.IsVisible)
        {
            _overlay.Show();
        }
        _overlay.PlaceAt(game!.ClientArea.X, game.ClientArea.Y);
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
