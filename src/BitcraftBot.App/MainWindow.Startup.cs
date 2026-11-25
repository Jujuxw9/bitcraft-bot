using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BitcraftBot.Server;

namespace BitcraftBot.App;

// Démarrage automatique en séquence (v0.4.15, demande de Julien, D-066) : au lancement du bot, sans bouton,
//   1. connexion au serveur (lecture seule) ;
//   2. quand le serveur a tout envoyé (personnage, inventaires), lecture de l'écran ;
//   3. quand la lecture de l'écran tourne, le bot lance le jeu par Steam (le jeu n'est pas modifié : c'est le
//      lanceur normal de Steam, comme un double-clic sur le jeu).
// Le jeu ne doit pas être lancé avant le bot (sinon la connexion l'éjecterait) : dans ce cas rien ne démarre et un
// message le dit. Pas de nouvel essai automatique (D-039). Les boutons « Se connecter » et « Démarrer » ne s'affichent
// que si la connexion ou la lecture est arrêtée.
public partial class MainWindow
{
    private const string SteamGameId = "3454650"; // BitCraft Online sur Steam (lu dans la bibliothèque Steam du PC)
    private static readonly TimeSpan ServerReadyTimeout = TimeSpan.FromSeconds(90);

    // Pour les essais de Claude : « --sans-demarrage-auto » laisse le bot à l'arrêt (aucune connexion, jeu pas lancé).
    private static bool AutoStartDisabled =>
        Environment.GetCommandLineArgs().Any(a => a.Equals("--sans-demarrage-auto", StringComparison.OrdinalIgnoreCase));

    private void InitStartupButtons()
    {
        // Un bouton désactivé (action impossible pour l'instant) est caché : seuls « Se déconnecter » et « Arrêter »
        // restent quand tout tourne.
        foreach (var button in new[] { ServerConnectButton, ServerDisconnectButton, StartButton, StopButton })
        {
            button.IsEnabledChanged += (_, _) => button.Visibility = button.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
            button.Visibility = button.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private async Task RunStartupAsync()
    {
        if (AutoStartDisabled)
        {
            _log.Info("Démarrage", "Démarrage automatique désactivé (option « --sans-demarrage-auto »).");
            return;
        }
        // Étape 0 : le jeu ne doit pas tourner.
        if (IsGameRunning())
        {
            _log.Alerte("Démarrage", "Le jeu est déjà lancé : pas de connexion automatique (le jeu serait éjecté). Fermer le jeu, puis relancer le bot.");
            SetStatus("JEU DÉJÀ LANCÉ", "Ferme le jeu, puis relance le bot : il se connecte et lance le jeu lui-même.", "#5A4A1A");
            ServerStateText.Text = "Jeu déjà lancé : ferme-le, puis relance le bot.";
            ServerStateText.Foreground = Theme("Warn");
            return;
        }

        // Les noms des ressources viennent des données du jeu : on les attend un peu (20 s au plus).
        for (var i = 0; i < 40 && _gameData.Current is null; i++)
        {
            await Task.Delay(500);
        }

        // Étape 1 : connexion au serveur.
        _log.Info("Démarrage", "Étape 1/3 : connexion au serveur.");
        SetStatus("DÉMARRAGE", "1/3 · Connexion au serveur…", "#1F2F45");
        await ConnectServerAsync();
        if (!await WaitServerReadyAsync())
        {
            SetStatus("ARRÊTÉ", "Connexion au serveur impossible (voir le journal).", "#5A2A2A");
            return;
        }

        // Étape 2 : lecture de l'écran.
        _log.Info("Démarrage", "Étape 2/3 : serveur prêt (personnage et inventaires reçus), lecture de l'écran.");
        if (!_safety.IsRunning)
        {
            StartSession();
        }
        await Task.Delay(1000);
        if (!_safety.IsRunning)
        {
            _log.Alerte("Démarrage", "La lecture de l'écran ne s'est pas lancée : jeu pas lancé.");
            return;
        }

        // Étape 3 : lancement du jeu par Steam (sauf essai de Claude : « --sans-lancer-jeu »).
        if (Environment.GetCommandLineArgs().Any(a => a.Equals("--sans-lancer-jeu", StringComparison.OrdinalIgnoreCase)))
        {
            _log.Info("Démarrage", "Étape 3/3 : jeu pas lancé (option « --sans-lancer-jeu »).");
            return;
        }
        if (IsGameRunning())
        {
            _log.Info("Démarrage", "Étape 3/3 : le jeu tourne déjà (lancé entre-temps).");
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo($"steam://rungameid/{SteamGameId}") { UseShellExecute = true });
            _log.Info("Démarrage", "Étape 3/3 : lecture de l'écran confirmée, jeu lancé par Steam.");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            _log.Alerte("Démarrage", $"Lancement du jeu par Steam impossible : {ex.Message}. Lance le jeu toi-même.");
        }
    }

    // Serveur prêt : connecté, personnage trouvé, inventaires reçus (ou refusés : rien à attendre de plus).
    private async Task<bool> WaitServerReadyAsync()
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < ServerReadyTimeout)
        {
            var reader = _serverReader;
            if (reader is null || reader.State is ReaderState.Deconnecte or ReaderState.Perdu or ReaderState.Erreur)
            {
                _log.Alerte("Démarrage", $"Connexion au serveur échouée : {reader?.StateDetail ?? "pas de lecteur"}. Pas de nouvel essai automatique.");
                return false;
            }
            if (reader.State == ReaderState.Connecte)
            {
                var snapshot = reader.Snapshot();
                if (snapshot.EntityId is not null && (reader.InventoryRefused || reader.Inventories().Count > 0))
                {
                    return true;
                }
            }
            SetStatus("DÉMARRAGE", $"1/3 · Connexion au serveur… {watch.Elapsed.TotalSeconds:0} s", "#1F2F45");
            await Task.Delay(500);
        }
        _log.Alerte("Démarrage", $"Serveur pas prêt après {ServerReadyTimeout.TotalSeconds:0} s : lecture de l'écran et jeu pas lancés.");
        return false;
    }
}
