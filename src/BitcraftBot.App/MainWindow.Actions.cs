using BitcraftBot.Core.Logging;
using BitcraftBot.Input.Actions;
using BitcraftBot.Perception.Capture;

namespace BitcraftBot.App;

// Contrôleur d'actions commun (v0.3.18) : toutes les touches et tous les clics du bot passent par lui,
// avec les mêmes sécurités (jeu actif et visible, Julien ne tape pas, une action à la fois, limite par minute,
// souris bougée, touches toujours relâchées). Aujourd'hui, seule action : Maj + E (repas automatique).
public partial class MainWindow
{
    private readonly ActionController _actions;
    private GameWindow? _actionGame;
    private DateTime _actionGameAt = DateTime.MinValue;
    private bool _closeReady;
    private bool _closeStarted;

    private ActionController CreateActionController()
    {
        var context = new ActionContext
        {
            GameWindow = () => CurrentGame()?.Handle,
            GameVisible = () => CurrentGame() is { } g && GameVisibility.IsUncovered(g),
            LastPhysicalKey = () => _lastPhysicalKey,
        };
        var controller = new ActionController(_inputSender, context, new ActionOptions { StopOnMouseMove = _settings.StopOnMouseMove });
        controller.Log += (level, message) =>
        {
            switch (level)
            {
                case ActionLogLevel.Erreur: _log.Erreur("Actions", message); break;
                case ActionLogLevel.Alerte: _log.Alerte("Actions", message); break;
                case ActionLogLevel.Info: _log.Info("Actions", message); break;
                default: _log.Detail("Actions", message); break;
            }
        };
        return controller;
    }

    // Fenêtre du jeu : celle de la lecture de l'écran si elle tourne, sinon cherchée (au plus toutes les 2 s).
    // Seule recherche partagée par l'overlay, le contrôleur d'actions et la touche Num 0 (v0.3.19).
    private GameWindow? CurrentGame()
    {
        if (_gameWindow is { } g)
        {
            return g;
        }
        if (DateTime.Now - _actionGameAt >= TimeSpan.FromSeconds(2))
        {
            _actionGameAt = DateTime.Now;
            _actionGame = _locator.Find();
        }
        return _actionGame;
    }

    // Couleur du thème BitCraft (Themes/BitCraft.xaml).
    private System.Windows.Media.Brush Theme(string key) => (System.Windows.Media.Brush)FindResource(key);
}
