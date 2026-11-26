using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using BitcraftBot.Server;
using BitcraftBot.Server.Auth;
using BitcraftBot.Server.World;
using HudState = BitcraftBot.Perception.Hud.HudState;

namespace BitcraftBot.App;

// Connexion au compte par code e-mail (onglet Paramètres), lecture en direct du serveur (onglet Personnage) (D-039).
public partial class MainWindow
{
    private static readonly TimeSpan ServerUiInterval = TimeSpan.FromMilliseconds(500);

    private readonly TokenStore _tokenStore = new(TokenStore.DefaultPath);
    private DispatcherTimer? _serverTimer;
    private ServerReader? _serverReader;

    // Dernière lecture du serveur (toutes les 500 ms), pour les autres onglets.
    private PlayerSnapshot? _lastSnapshot;
    private Dictionary<int, ResourceLabel> _resourceLabels = [];
    private bool _authBusy;

    private void InitServerTab()
    {
        ServerEmailBox.Text = _settings.ServerEmail;
        ServerRegionBox.Text = "18"; // v0.4.13 : toujours la région R18 (Keldan), réglage retiré de l'écran
        _gameData.Loaded += store => Dispatcher.BeginInvoke(() =>
            _resourceLabels = store.Resources.GroupBy(r => r.Id)
                .ToDictionary(g => g.Key, g => new ResourceLabel(g.First().Name, g.First().Tier, g.First().Tag, g.First().MaxHealth)));
        InitResourceMap();
        _serverTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = ServerUiInterval };
        _serverTimer.Tick += (_, _) => OnServerTick();
        _serverTimer.Start();
        ShowTokenStatus();
    }

    // Appelé à chaque lecture de l'écran : la cible sélectionnée, rapprochée des ressources du serveur (onglet Carte).
    private void RememberHudTarget(HudState hud)
    {
        _lastHudTarget = hud.Target;
        _lastHudTargetAt = DateTime.Now;
    }

    private void ShowTokenStatus()
    {
        var token = _tokenStore.Load();
        if (token is null)
        {
            TokenStatusText.Text = "Pas de jeton : e-mail → code → Valider.";
            TokenStatusText.Foreground = Theme("Warn");
            AccountPanel.Visibility = Visibility.Visible;
            AccountToggleButton.Content = "Masquer";
            ForgetTokenButton.IsEnabled = false;
            return;
        }
        var info = TokenInfo.TryRead(token);
        var expires = info?.ExpiresAt is { } exp ? $", valable jusqu'au {exp.ToLocalTime():dd/MM/yyyy HH:mm}" : ", sans date de fin";
        TokenStatusText.Text = $"Jeton enregistré ({TokenInfo.ShortIdentity(info?.HexIdentity)}).";
        TokenStatusText.Foreground = info?.ExpiresAt is { } e && e < DateTimeOffset.Now
            ? Theme("Bad")
            : Theme("Good");
        ForgetTokenButton.IsEnabled = true;
    }

    // v0.4.14 : la partie « Compte » est repliée (elle ne change plus) ; ce bouton l'ouvre ou la referme.
    private void AccountToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var open = AccountPanel.Visibility != Visibility.Visible;
        AccountPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        AccountToggleButton.Content = open ? "Masquer" : "Afficher";
    }

    private async void RequestCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var email = ServerEmailBox.Text.Trim();
        if (!email.Contains('@') || _authBusy)
        {
            TokenStatusText.Text = "E-mail manquant.";
            return;
        }
        _settings.ServerEmail = email;
        await RunAuthAsync(async http =>
        {
            var result = await new AuthClient(http).RequestCodeAsync(email, CancellationToken.None);
            if (result.Success)
            {
                TokenStatusText.Text = "Code envoyé : regardez vos e-mails.";
                _log.Info("Serveur", "Code de connexion demandé au service de BitCraft.");
            }
            else
            {
                TokenStatusText.Text = $"Échec de la demande de code : {result.Error}.";
                _log.Erreur("Serveur", $"Demande de code refusée : {result.Error}.");
            }
        });
    }

    private async void ValidateCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var email = ServerEmailBox.Text.Trim();
        var code = ServerCodeBox.Text.Trim();
        if (code.Length == 0 || !email.Contains('@') || _authBusy)
        {
            TokenStatusText.Text = "E-mail ou code manquant.";
            return;
        }
        await RunAuthAsync(async http =>
        {
            var result = await new AuthClient(http).AuthenticateAsync(email, code, CancellationToken.None);
            if (result is { Success: true, Token: { } token })
            {
                _tokenStore.Save(token);
                ServerCodeBox.Text = string.Empty;
                var info = TokenInfo.TryRead(token);
                _log.Info("Serveur", $"Jeton reçu et enregistré, chiffré (compte {TokenInfo.ShortIdentity(info?.HexIdentity)}).");
                ShowTokenStatus();
            }
            else
            {
                TokenStatusText.Text = $"Code refusé : {result.Error}.";
                _log.Erreur("Serveur", $"Échange du code refusé : {result.Error}.");
            }
        });
    }

    private async Task RunAuthAsync(Func<HttpClient, Task> action)
    {
        _authBusy = true;
        RequestCodeButton.IsEnabled = false;
        ValidateCodeButton.IsEnabled = false;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd(ServerEndpoints.UserAgent);
            await action(http);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TokenStatusText.Text = $"Service de connexion injoignable : {ex.Message}";
            _log.Erreur("Serveur", $"Service de connexion injoignable : {ex.Message}");
        }
        finally
        {
            _authBusy = false;
            RequestCodeButton.IsEnabled = true;
            ValidateCodeButton.IsEnabled = true;
        }
    }

    private void ForgetTokenButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Supprimer le jeton enregistré sur ce PC ? Il faudra redemander un code pour se reconnecter.",
                "Oublier le jeton", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }
        _tokenStore.Delete();
        _log.Info("Serveur", "Jeton supprimé de ce PC.");
        ShowTokenStatus();
    }

    private static bool IsGameRunning() => Process.GetProcessesByName("BitCraft").Length > 0;

    private async void ServerConnectButton_Click(object sender, RoutedEventArgs e) => await ConnectServerAsync();

    // Connexion en lecture seule (bouton, ou démarrage automatique v0.4.15).
    private async Task ConnectServerAsync()
    {
        if (_serverReader is { State: ReaderState.Connexion or ReaderState.Connecte })
        {
            return;
        }
        var token = _tokenStore.Load();
        if (token is null)
        {
            ServerStateText.Text = "Pas de jeton (carte Compte).";
            return;
        }
        if (!int.TryParse(ServerRegionBox.Text, out var region) || region is < 1 or > 100)
        {
            ServerStateText.Text = "Région invalide (ex. 18).";
            return;
        }
        _settings.ServerRegion = region;
        // Vérifié le 2026-11-18 (essai v0.3.8) : se connecter avec le jeu ouvert éjecte le jeu
        // (« Logged in elsewhere »). Aucune dérogation possible.
        if (IsGameRunning())
        {
            _log.Alerte("Serveur", "Connexion refusée : le jeu est lancé (le jeu serait éjecté).");
            ServerStateText.Text = "Jeu lancé : quittez-le avant de connecter.";
            ServerStateText.Foreground = Theme("Warn");
            MessageBox.Show(this,
                "Le jeu est lancé.\n\nSi le bot se connecte maintenant, le jeu affiche « Logged in elsewhere » et vous éjecte.\n\n" +
                "Ordre à suivre : quittez le jeu, cliquez « Se connecter » ici, puis relancez le jeu.",
                "Jeu lancé", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _log.Info("Serveur", $"Connexion en lecture seule à la région R{region} (jeu fermé : ordre respecté).");

        if (_serverReader is not null)
        {
            await _serverReader.DisposeAsync();
        }
        // v0.4.15 : noms des ressources lus au moment voulu (les données du jeu peuvent arriver après la connexion automatique).
        _serverReader = new ServerReader(_log, id => _resourceLabels.GetValueOrDefault(id));
        _serverReader.StateChanged += () => Dispatcher.BeginInvoke(ShowServerState);
        // v0.4.23 : le lecteur range lui-même dans la mémoire de la carte les morceaux reçus (dont les zones découvertes).
        _serverReader.TerrainMemory = _terrainStore;
        ServerConnectButton.IsEnabled = false;
        ServerDisconnectButton.IsEnabled = true;
        // Le jeu est fermé ici : la découverte par le module global est sans risque.
        await _serverReader.ConnectAsync(token, region, discover: true, CancellationToken.None);
        ShowServerState();
    }

    private async void ServerDisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serverReader is null)
        {
            return;
        }
        // Le bot se connecte toujours avant le jeu : sa déconnexion ne touche pas le jeu (vérifié le 2026-11-18).
        await _serverReader.DisconnectAsync("demandé par Julien");
        ShowServerState();
    }

    private void ShowServerState()
    {
        var reader = _serverReader;
        var state = reader?.State ?? ReaderState.Deconnecte;
        ServerStateText.Text = reader?.StateDetail ?? "Pas connecté.";
        ServerStateText.Foreground = state switch
        {
            ReaderState.Connecte => Theme("Good"),
            ReaderState.Connexion => Theme("Warn"),
            ReaderState.Perdu or ReaderState.Erreur => Theme("Bad"),
            _ => Theme("Text"),
        };
        var active = state is ReaderState.Connexion or ReaderState.Connecte;
        ServerConnectButton.IsEnabled = !active;
        ServerDisconnectButton.IsEnabled = active;
        ServerSnapshotButton.IsEnabled = reader is not null && state != ReaderState.Deconnecte;
        if (state == ReaderState.Perdu)
        {
            ServerStateText.Text += " Pour se reconnecter sans risque : quitter le jeu, se reconnecter ici, puis relancer le jeu.";
        }
    }

    // Onglets : seul l'onglet affiché est redessiné (v0.3.19, moins de CPU).
    // v0.4.11 : numéro retrouvé d'après l'onglet lui-même (l'onglet CHAT, ajouté en v0.4.10, avait décalé les numéros fixes).
    private int BotTab => Tabs.Items.IndexOf(BotTabItem);
    private int CharacterTab => Tabs.Items.IndexOf(CharacterTabItem);
    private int MapTab => Tabs.Items.IndexOf(MapTabItem);
    private int SettingsTab => Tabs.Items.IndexOf(SettingsTabItem);
    private int TravelerTab => Tabs.Items.IndexOf(TravelerTabItem);
    private static readonly TimeSpan GameRunningInterval = TimeSpan.FromSeconds(3);

    private ServerReader? _snapshotReader;
    private long _snapshotVersion = -1;
    private IReadOnlyList<InventoryInfo> _lastInventories = [];
    private DateTime _gameRunningCheckedAt = DateTime.MinValue;

    private void OnServerTick()
    {
        UpdateOverlay();
        var tab = Tabs.SelectedIndex;
        if (tab == SettingsTab && DateTime.Now - _gameRunningCheckedAt >= GameRunningInterval)
        {
            _gameRunningCheckedAt = DateTime.Now;
            ServerGameRunningText.Text = IsGameRunning() ? "Jeu : lancé." : "Jeu : fermé.";
        }
        var reader = _serverReader;
        if (reader is null || reader.State == ReaderState.Deconnecte)
        {
            FlushTasks();
            if (tab == BotTab) UpdateDashboard(null, null, []);
            if (tab == TravelerTab) ShowTravelersOffline();
            return;
        }
        // Instantané et inventaires recalculés seulement si le serveur a envoyé quelque chose depuis le dernier passage.
        var version = reader.MessageCount;
        if (!ReferenceEquals(reader, _snapshotReader) || version != _snapshotVersion || _lastSnapshot is null)
        {
            _snapshotReader = reader;
            _snapshotVersion = version;
            _lastSnapshot = reader.Snapshot();
            _lastInventories = reader.InventoryRefused ? [] : reader.Inventories();
        }
        var s = _lastSnapshot;
        var inventories = _lastInventories;

        // Toujours, même onglet caché : journal des tâches, repas automatique, métier en cours, cible et meilleur arbre.
        _skillTracker.Update(s.Skills, DateTime.Now);
        if (reader.State == ReaderState.Connecte)
        {
            TrackTasks(s, inventories, !reader.InventoryRefused);
            TrackCamera(s);
            TrackAround(s);
        }
        else
        {
            FlushTasks();
        }
        RunMeals(reader, s, inventories);
        RunAbilities(reader, s);
        RunResume(reader, s);
        RunGpt(reader, s);
        ArchiveChat(reader, s);
        ShowResources(reader, s, tab == MapTab);

        if (tab == CharacterTab)
        {
            SrvCharacter.Text = s.EntityId is { } id
                ? $"entité {id}, {(s.SignedIn == true ? "connecté dans le jeu" : "pas connecté dans le jeu")}"
                : "—";
            SrvRegion.Text = s.RegionIndex is { } ri ? $"{s.RegionName ?? "?"} (index {ri})"
                : reader.Discovery is { Success: true } dr ? $"{dr.RegionName ?? "?"} (région {dr.RegionId}, {dr.Module})"
                : reader.Discovery is { Error: { } de } ? $"inconnue ({de})" : "—";
            SrvPosition.Text = s.Position is { } p ? $"{p.ScreenText}  (serveur : {p})" : "—";
            SrvDestination.Text = s.Destination is { } d ? $"{d.ScreenText}{(s.Walking == true ? " — en marche" : "")}" : "—";
            SrvHealth.Text = s.Health is { } h ? $"{h:0.#}" : "—";
            SrvStamina.Text = s.Stamina is { } st ? $"{st:0.#}" : "—";
            SrvUpdated.Text = reader.LastUpdate is { } at ? $"{at:HH:mm:ss}" : "—";
            ShowInventories(reader, s, inventories);
        }
        if (tab == TravelerTab)
        {
            ShowTravelers(reader, s, inventories);
        }
        if (tab == BotTab)
        {
            ShowBuffs(s);
            UpdateDashboard(reader, s, inventories);
        }
    }

    private async void ServerSnapshotButton_Click(object sender, RoutedEventArgs e)
    {
        var reader = _serverReader;
        if (reader is null)
        {
            return;
        }
        try
        {
            var snapshot = reader.Snapshot();
            var tables = reader.DumpTables().ToDictionary(t => t.Key, t => t.Value.Take(200).Select(r => r.ToString()).ToList());
            var content = new
            {
                Heure = DateTime.Now,
                Etat = reader.StateDetail,
                Messages = reader.MessageCount,
                Instantane = snapshot with { Resources = snapshot.Resources.Take(50).ToList() },
                Tables = tables,
            };
            var path = Path.Combine(Path.GetDirectoryName(_log.FilePath)!, $"instantane-serveur_{DateTime.Now:HH-mm-ss}.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true }));
            _log.Info("Serveur", $"Instantané enregistré : {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _log.Erreur("Serveur", $"Instantané impossible : {ex.Message}");
        }
    }

    private static readonly TimeSpan CloseDisconnectTimeout = TimeSpan.FromSeconds(8);

    // À la fermeture du bot : déconnexion propre du serveur, en attendant qu'elle soit finie (v0.3.18).
    private async Task CloseServerReaderAsync()
    {
        var reader = TakeReaderForClose();
        if (reader is null)
        {
            return;
        }
        _log.Info("Serveur", "Fermeture du bot : déconnexion du serveur d'abord.");
        var disconnect = reader.DisconnectAsync("fermeture du bot");
        if (await Task.WhenAny(disconnect, Task.Delay(CloseDisconnectTimeout)) != disconnect)
        {
            _log.Alerte("Serveur", $"Déconnexion pas confirmée après {CloseDisconnectTimeout.TotalSeconds:0} s : la liaison est abandonnée (le serveur la coupe de lui-même).");
        }
    }

    // Windows s'arrête ou ferme la session : pas le temps d'attendre l'interface, déconnexion directe.
    private void CloseServerReaderNow()
    {
        var reader = TakeReaderForClose();
        if (reader is not null)
        {
            Task.Run(() => reader.DisconnectAsync("arrêt de Windows")).Wait(TimeSpan.FromSeconds(4));
        }
    }

    private ServerReader? TakeReaderForClose()
    {
        _serverTimer?.Stop();
        _settings.ServerEmail = ServerEmailBox.Text.Trim();
        if (int.TryParse(ServerRegionBox.Text, out var region) && region is >= 1 and <= 100)
        {
            _settings.ServerRegion = region;
        }
        var reader = _serverReader;
        _serverReader = null;
        return reader;
    }
}
