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
    private static readonly TimeSpan PositionCompareInterval = TimeSpan.FromSeconds(15);

    private readonly TokenStore _tokenStore = new(TokenStore.DefaultPath);
    private DispatcherTimer? _serverTimer;
    private ServerReader? _serverReader;
    private Dictionary<int, ResourceLabel> _resourceLabels = [];
    private (int North, int East)? _lastHudPosition;
    private DateTime _lastPositionCompare = DateTime.MinValue;
    private bool _authBusy;

    private void InitServerTab()
    {
        ServerEmailBox.Text = _settings.ServerEmail;
        ServerRegionBox.Text = _settings.ServerRegion.ToString();
        _gameData.Loaded += store => Dispatcher.BeginInvoke(() =>
            _resourceLabels = store.Resources.GroupBy(r => r.Id)
                .ToDictionary(g => g.Key, g => new ResourceLabel(g.First().Name, g.First().Tier, g.First().Tag, g.First().MaxHealth)));
        InitResourceMap();
        _serverTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = ServerUiInterval };
        _serverTimer.Tick += (_, _) => OnServerTick();
        _serverTimer.Start();
        ShowTokenStatus();
    }

    // Appelé à chaque lecture du HUD : sert à comparer la position de l'écran avec celle du serveur.
    private void RememberHudPosition(HudState hud)
    {
        if (hud.Position is { } p)
        {
            _lastHudPosition = p;
        }
        _lastHudTarget = hud.Target;
        _lastHudTargetAt = DateTime.Now;
    }

    private void ShowTokenStatus()
    {
        var token = _tokenStore.Load();
        if (token is null)
        {
            TokenStatusText.Text = "Aucun jeton enregistré. Étapes : e-mail → « Recevoir le code » → code → « Valider ».";
            TokenStatusText.Foreground = System.Windows.Media.Brushes.Gold;
            ForgetTokenButton.IsEnabled = false;
            return;
        }
        var info = TokenInfo.TryRead(token);
        var expires = info?.ExpiresAt is { } exp ? $", valable jusqu'au {exp.ToLocalTime():dd/MM/yyyy HH:mm}" : ", sans date de fin";
        TokenStatusText.Text = $"Jeton enregistré (chiffré). Compte {TokenInfo.ShortIdentity(info?.HexIdentity)}{expires}.";
        TokenStatusText.Foreground = info?.ExpiresAt is { } e && e < DateTimeOffset.Now
            ? System.Windows.Media.Brushes.IndianRed
            : System.Windows.Media.Brushes.LightGreen;
        ForgetTokenButton.IsEnabled = true;
    }

    private async void RequestCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var email = ServerEmailBox.Text.Trim();
        if (!email.Contains('@') || _authBusy)
        {
            TokenStatusText.Text = "Entrez l'adresse e-mail liée à votre compte BitCraft.";
            return;
        }
        _settings.ServerEmail = email;
        await RunAuthAsync(async http =>
        {
            var result = await new AuthClient(http).RequestCodeAsync(email, CancellationToken.None);
            if (result.Success)
            {
                TokenStatusText.Text = "Code demandé. Regardez vos e-mails (et les indésirables), puis entrez le code.";
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
            TokenStatusText.Text = "Entrez l'adresse e-mail et le code reçu.";
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

    private async void ServerConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serverReader is { State: ReaderState.Connexion or ReaderState.Connecte })
        {
            return;
        }
        var token = _tokenStore.Load();
        if (token is null)
        {
            ServerStateText.Text = "Pas de jeton : faites d'abord l'étape 1.";
            return;
        }
        if (!int.TryParse(ServerRegionBox.Text, out var region) || region is < 1 or > 100)
        {
            ServerStateText.Text = "Numéro de région invalide (exemple : 18 pour R18).";
            return;
        }
        _settings.ServerRegion = region;
        // Vérifié le 2026-11-18 (essai v0.3.8) : se connecter avec le jeu ouvert éjecte le jeu
        // (« Logged in elsewhere »). Aucune dérogation possible.
        if (IsGameRunning())
        {
            _log.Alerte("Serveur", "Connexion refusée : le jeu est lancé (le jeu serait éjecté).");
            ServerStateText.Text = "Le jeu est lancé : connexion impossible. Quittez le jeu, connectez le bot, puis relancez le jeu.";
            ServerStateText.Foreground = System.Windows.Media.Brushes.Gold;
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
        var labels = _resourceLabels;
        _serverReader = new ServerReader(_log, id => labels.GetValueOrDefault(id));
        _serverReader.StateChanged += () => Dispatcher.BeginInvoke(ShowServerState);
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
            ReaderState.Connecte => System.Windows.Media.Brushes.LightGreen,
            ReaderState.Connexion => System.Windows.Media.Brushes.Gold,
            ReaderState.Perdu or ReaderState.Erreur => System.Windows.Media.Brushes.IndianRed,
            _ => System.Windows.Media.Brushes.White,
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

    private void OnServerTick()
    {
        UpdateOverlay();
        ServerGameRunningText.Text = IsGameRunning() ? "Jeu : lancé." : "Jeu : fermé.";
        SrvHudPosition.Text = _lastHudPosition is { } hp ? $"N {hp.North}, E {hp.East}" : "— (lancer « Démarrer » dans Supervision)";
        var reader = _serverReader;
        if (reader is null || reader.State == ReaderState.Deconnecte)
        {
            return;
        }
        ServerTrafficText.Text = $"Messages reçus : {reader.MessageCount} (dont {reader.TransactionCount} changements).";
        var s = reader.Snapshot();
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
        SrvSatiation.Text = s.Satiation is { } sa ? $"{sa:0.#}" : "—";
        SrvUpdated.Text = reader.LastUpdate is { } at ? $"{at:HH:mm:ss}" : "—";

        ShowResources(reader, s);
        ShowInventories(reader, s);

        // Pour trouver la conversion entre les coordonnées du serveur et celles de l'écran (H2).
        if (s.Position is { } sp && _lastHudPosition is { } hud && DateTime.Now - _lastPositionCompare >= PositionCompareInterval)
        {
            _lastPositionCompare = DateTime.Now;
            // Constat des essais du 2026-11-18 : E ≈ x ÷ 3 et N ≈ z ÷ 3, à 1 près.
            _log.Info("Serveur", $"Comparaison des positions : serveur {sp} (÷3 : E {sp.X / 3:0.0}, N {sp.Z / 3:0.0}) ; écran N {hud.North}, E {hud.East} ; écart E {hud.East - sp.X / 3:+0.0;-0.0;0}, N {hud.North - sp.Z / 3:+0.0;-0.0;0}.");
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
                PositionEcran = _lastHudPosition is { } hp ? $"N {hp.North}, E {hp.East}" : null,
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

    // À la fermeture du bot : fermeture propre de la liaison, sans bloquer l'interface.
    private void CloseServerReader()
    {
        _serverTimer?.Stop();
        _settings.ServerEmail = ServerEmailBox.Text.Trim();
        if (int.TryParse(ServerRegionBox.Text, out var region) && region is >= 1 and <= 100)
        {
            _settings.ServerRegion = region;
        }
        var reader = _serverReader;
        _serverReader = null;
        if (reader is not null)
        {
            Task.Run(() => reader.DisposeAsync().AsTask()).Wait(TimeSpan.FromSeconds(4));
        }
    }
}
