using System.Net.Http;
using BitcraftBot.Core.Logging;
using BitcraftBot.Server.Auth;
using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;

namespace BitcraftBot.Server;

// Lecture du serveur en direct (livrable H1, D-039).
// Étapes : schéma public → liaison chiffrée avec la région → identité → personnage → suivi du personnage et des ressources.
// Aucune reconnexion automatique : si la liaison tombe pendant que le jeu tourne, se reconnecter ferait du bot
// la « connexion la plus récente » et sa fermeture pourrait déconnecter le personnage (constats H1).
public sealed class ServerReader(SessionLog log, Func<int, ResourceLabel?> describe) : IAsyncDisposable
{
    private const string Category = "Serveur";

    // Rayon de lecture des ressources, en petites cases du serveur.
    public const int ResourceRadius = 40;

    // On relance l'abonnement aux ressources quand le personnage s'est éloigné de plus de ce nombre de cases.
    private const double ResubscribeDistance = ResourceRadius / 2.0;
    private static readonly TimeSpan MinResubscribeInterval = TimeSpan.FromSeconds(5);

    private readonly TableCache _cache = new();
    private SpacetimeConnection? _connection;
    private Schema? _schema;
    private string? _identity;
    private ulong? _entityId;
    private Area? _area;
    private string? _targetModule;
    private bool _resourcesRefused;
    private bool _healthRefused;
    private bool _inventoryRefused;
    private bool _inventoryLogged;
    private uint _requestId;
    private DateTime _lastSubscribe = DateTime.MinValue;
    private long _messages;
    private long _transactions;

    public ReaderState State { get; private set; } = ReaderState.Deconnecte;

    public string StateDetail { get; private set; } = "Pas connecté.";

    public DateTime? LastUpdate { get; private set; }

    public long MessageCount => Interlocked.Read(ref _messages);

    public long TransactionCount => Interlocked.Read(ref _transactions);

    public string? Identity => _identity;

    public event Action? StateChanged;

    // Zone des ressources lues (centre = position du personnage au moment de la demande) et heure de la demande.
    public Area? ResourceArea => _area;

    public DateTime? ResourceAreaUpdated { get; private set; }

    public bool HealthRefused => _healthRefused;

    public bool InventoryRefused => _inventoryRefused;

    // Sac, ceinture et coffres de banque du personnage.
    public IReadOnlyList<InventoryInfo> Inventories() => _entityId is { } e ? World.Inventories.Of(_cache, e) : [];

    public IReadOnlyDictionary<ulong, ServerPosition> BankPositions() => Banks.Positions(_cache);

    // Résultat de la dernière découverte par le module global (null si elle n'a pas été faite).
    public RegionDiscovery? Discovery { get; private set; }

    // discover = true seulement si le jeu est fermé (voir RegionFinder).
    public async Task ConnectAsync(string token, int region, bool discover, CancellationToken cancel)
    {
        if (_connection is not null)
        {
            return;
        }
        _cache.Clear();
        _entityId = null;
        _area = null;
        _resourcesRefused = false;
        _healthRefused = false;
        _inventoryRefused = false;
        _inventoryLogged = false;
        ResourceAreaUpdated = null;
        Interlocked.Exchange(ref _messages, 0);
        Interlocked.Exchange(ref _transactions, 0);
        var tokenInfo = TokenInfo.TryRead(token);
        _identity = Row.Normalize(tokenInfo?.HexIdentity);
        log.Info(Category, DescribeToken(tokenInfo));
        var host = ServerEndpoints.Host;
        var module = ServerEndpoints.RegionModule(region);
        try
        {
            Discovery = null;
            if (discover)
            {
                SetState(ReaderState.Connexion, "Recherche du personnage (module global)…");
                Discovery = await RegionFinder.FindAsync(token, _identity, TimeSpan.FromSeconds(15), m => log.Info(Category, $"[liaison {RegionFinder.GlobalModule}] {m}"), cancel);
                if (Discovery.Success)
                {
                    log.Info(Category, $"Découverte : {RegionFinder.Describe(Discovery)}.");
                    host = Discovery.Host!;
                    module = Discovery.Module!;
                    if (Discovery.RegionId != region)
                    {
                        log.Alerte(Category, $"La région du personnage ({Discovery.RegionId}) n'est pas celle demandée (R{region}) : lecture de la région du personnage.");
                    }
                }
                else
                {
                    log.Info(Category, $"Découverte : {Discovery.Error}. Pas grave : lecture directe de la région R{region}.");
                }
            }
            _targetModule = module;

            SetState(ReaderState.Connexion, $"Lecture du schéma de {module}…");
            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd(ServerEndpoints.UserAgent);
                var json = await http.GetStringAsync(ServerEndpoints.SchemaUri(host, module), cancel);
                _schema = Schema.Parse(json);
            }
            log.Info(Category, $"Schéma de {module} lu : {_schema.TableCount} tables.");

            SetState(ReaderState.Connexion, $"Liaison avec {module}…");
            var connection = new SpacetimeConnection();
            connection.MessageReceived += OnMessage;
            connection.Closed += OnClosed;
            connection.Diagnostic += m => log.Info(Category, $"[liaison {module}] {m}");
            _connection = connection;
            await connection.ConnectAsync(ServerEndpoints.SubscribeUri(host, module), token, cancel);
            log.Info(Category, $"Liaison ouverte avec {module} sur {host} (lecture seule). Identité du compte : {TokenInfo.ShortIdentity(_identity)}.");
            SetState(ReaderState.Connexion, "Liaison ouverte, attente de l'identité…");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.Erreur(Category, $"Connexion impossible : {ex.Message}");
            await DisconnectAsync("échec de connexion");
            SetState(ReaderState.Erreur, $"Connexion impossible : {ex.Message}");
        }
    }

    public async Task DisconnectAsync(string reason = "demandé")
    {
        var connection = _connection;
        _connection = null;
        if (connection is null)
        {
            return;
        }
        connection.Closed -= OnClosed;
        connection.MessageReceived -= OnMessage;
        await connection.DisposeAsync();
        log.Info(Category, $"Liaison fermée ({reason}).");
        SetState(ReaderState.Deconnecte, $"Déconnecté ({reason}).");
    }

    public PlayerSnapshot Snapshot() => SnapshotBuilder.Build(_cache, _identity, describe);

    // Copie lisible de toutes les lignes reçues, pour le diagnostic (le jeton n'y est jamais).
    public IReadOnlyDictionary<string, IReadOnlyList<Row>> DumpTables() =>
        _cache.Counts().Keys.ToDictionary(t => t, t => _cache.Rows(t));

    private void OnMessage(string json)
    {
        var count = Interlocked.Increment(ref _messages);
        LogRawMessage(count, json);
        ServerEvent message;
        try
        {
            message = ServerMessages.Parse(json, _schema);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException)
        {
            log.Erreur(Category, $"Message illisible ({json.Length} caractères) : {ex.Message}");
            return;
        }

        switch (message)
        {
            case IdentityEvent identity:
                _identity ??= Row.Normalize(identity.HexIdentity);
                if (identity.HexIdentity is not null && Row.Normalize(identity.HexIdentity) != _identity)
                {
                    log.Alerte(Category, "L'identité donnée par le serveur ne correspond pas à celle du jeton.");
                    _identity = Row.Normalize(identity.HexIdentity);
                }
                log.Info(Category, $"Identité confirmée par le serveur : {TokenInfo.ShortIdentity(_identity)}.");
                Subscribe(Queries.FindPlayer(_identity!), "recherche du personnage");
                break;

            case SubscriptionEvent subscription:
                _cache.Reset(subscription.Tables);
                LastUpdate = DateTime.Now;
                log.Info(Category, "Abonnement accepté : " + DescribeCounts());
                LogFirstInventory();
                OnSubscriptionApplied();
                break;

            case TransactionEvent transaction:
                Interlocked.Increment(ref _transactions);
                if (transaction.Failure is null)
                {
                    foreach (var change in transaction.Tables)
                    {
                        _cache.Apply(change);
                    }
                    LastUpdate = DateTime.Now;
                    MaybeFollowPlayer();
                }
                break;

            case SubscriptionErrorEvent error:
                log.Erreur(Category, $"Abonnement refusé par le serveur : {error.Error}");
                if (_entityId is { } ie && !_inventoryRefused)
                {
                    // D'abord sans le sac et les banques (requêtes ajoutées en v0.3.14).
                    _inventoryRefused = true;
                    log.Alerte(Category, "Nouvel essai sans le sac ni les banques.");
                    Subscribe(Queries.FollowPlayer(_identity!, ie, _area, withHealth: !_healthRefused, withInventory: false), "suivi sans le sac ni les banques");
                }
                else if (_area is { } refusedArea && !_healthRefused && _entityId is { } he)
                {
                    // D'abord sans la santé des ressources (requête ajoutée en v0.3.11), en gardant les ressources.
                    _healthRefused = true;
                    log.Alerte(Category, "Nouvel essai sans la santé des ressources.");
                    Subscribe(Queries.FollowPlayer(_identity!, he, refusedArea, withHealth: false, withInventory: !_inventoryRefused), "ressources sans la santé");
                }
                else if (_area is not null && !_resourcesRefused)
                {
                    // Les requêtes des ressources (avec jointure) sont les plus fragiles : on continue sans elles.
                    _resourcesRefused = true;
                    log.Alerte(Category, "Nouvel essai sans la liste des ressources proches.");
                    _area = null;
                    if (_entityId is { } e)
                    {
                        Subscribe(Queries.FollowPlayer(_identity!, e, null, withInventory: !_inventoryRefused), "suivi sans ressources");
                    }
                }
                else
                {
                    SetState(ReaderState.Erreur, $"Abonnement refusé : {error.Error}");
                }
                break;

            case UnknownEvent unknown:
                log.Detail(Category, $"Message « {unknown.Type} » ignoré.");
                break;
        }
    }

    // Une fois par connexion : un extrait brut d'un inventaire, pour vérifier le format réel des poches (v0.3.14).
    private void LogFirstInventory()
    {
        if (_inventoryLogged || _entityId is not { } e)
        {
            return;
        }
        var rows = _cache.Rows("inventory_state").ToList();
        if (rows.Count == 0)
        {
            return;
        }
        _inventoryLogged = true;
        var inventories = World.Inventories.Of(_cache, e);
        var raw = rows[0].Get("pockets")?.GetRawText() ?? "?";
        log.Info(Category, $"Sac et banques : {inventories.Count} inventaires, {inventories.Sum(i => i.Stacks.Count)} piles d'objets. Exemple brut de poches : {(raw.Length > 400 ? raw[..400] + "…" : raw)}");
    }

    private void OnSubscriptionApplied()
    {
        var snapshot = Snapshot();
        if (snapshot.EntityId is null)
        {
            log.Alerte(Category, "Aucun personnage de ce compte dans cette région. Vérifiez le numéro de région (R…).");
            SetState(ReaderState.Erreur, "Personnage introuvable dans cette région.");
            return;
        }
        if (_entityId is null)
        {
            _entityId = snapshot.EntityId;
            log.Info(Category, $"Personnage trouvé (entité {_entityId}).");
            Subscribe(Queries.FollowPlayer(_identity!, _entityId.Value, null, withInventory: !_inventoryRefused), "suivi du personnage");
            return;
        }
        SetState(ReaderState.Connecte, "Lecture en direct.");
        MaybeFollowPlayer();
    }

    // Abonne (ou réabonne) les ressources autour du personnage quand il s'est assez déplacé.
    private void MaybeFollowPlayer()
    {
        if (_entityId is not { } entity || _resourcesRefused || _identity is null)
        {
            return;
        }
        var position = Snapshot().Position;
        if (position is not { } p)
        {
            return;
        }
        var moved = _area is not { } a || new ServerPosition(a.CenterX, a.CenterZ).DistanceTo(p) > ResubscribeDistance;
        if (!moved || DateTime.Now - _lastSubscribe < MinResubscribeInterval)
        {
            return;
        }
        var previous = _area;
        _area = Area.Around(p.X, p.Z, ResourceRadius);
        ResourceAreaUpdated = DateTime.Now;
        var why = previous is { } old
            ? $"ressources autour de {p.ScreenText} : le personnage s'est éloigné de {new ServerPosition(old.CenterX, old.CenterZ).ScreenDistanceTo(p):0.0} cases écran du centre de l'ancienne zone"
            : $"ressources autour de {p.ScreenText}";
        Subscribe(Queries.FollowPlayer(_identity, entity, _area, withHealth: !_healthRefused, withInventory: !_inventoryRefused), why);
    }

    private void Subscribe(IReadOnlyList<string> queries, string why)
    {
        var connection = _connection;
        if (connection is null)
        {
            return;
        }
        _lastSubscribe = DateTime.Now;
        var id = ++_requestId;
        log.Info(Category, $"Abonnement n° {id} ({why}) : {string.Join(" ; ", queries)}");
        _ = Task.Run(async () =>
        {
            try
            {
                await connection.SubscribeAsync(queries, id, CancellationToken.None);
            }
            catch (Exception ex) when (ex is System.Net.WebSockets.WebSocketException or InvalidOperationException or ObjectDisposedException)
            {
                log.Erreur(Category, $"Envoi de l'abonnement impossible : {ex.Message}");
            }
        });
    }

    private void OnClosed(string reason)
    {
        _connection = null;
        if (MessageCount == 0)
        {
            // Fermée avant le moindre message : le serveur a refusé le compte à l'ouverture (code serveur :
            // personnage absent de cette région, ou compte pas authentifié depuis moins de 24 h).
            var hint = Discovery is { Success: false, AccountKnown: false } d && d.Error is not null
                ? $" Découverte : {d.Error}."
                : "";
            var detail = $"La région {_targetModule} a refusé la liaison dès l'ouverture. Causes possibles : personnage pas dans cette région, " +
                         $"ou connexion du compte trop ancienne (redemander un code juste avant).{hint}";
            log.Erreur(Category, detail + $" (détail technique : {reason})");
            SetState(ReaderState.Erreur, detail);
            return;
        }
        log.Alerte(Category, $"Liaison terminée : {reason}. Pas de reconnexion automatique (voir l'onglet Paramètres).");
        SetState(ReaderState.Perdu, $"Liaison terminée : {reason}.");
    }

    // Les 5 premiers messages d'une liaison sont notés avec un extrait (jeton masqué) ; ensuite, type et taille en « Détails ».
    private void LogRawMessage(long index, string json)
    {
        if (index <= 5)
        {
            log.Info(Category, ServerMessages.Summarize(index, json, withExcerpt: true));
        }
        else
        {
            log.Detail(Category, ServerMessages.Summarize(index, json, withExcerpt: false));
        }
    }

    // Ce que dit le jeton (sans le secret) : compte, date d'émission, âge, fin de validité.
    private static string DescribeToken(TokenInfo? info)
    {
        if (info is null)
        {
            return "Jeton illisible (format inattendu) : l'identité du compte sera demandée au serveur.";
        }
        var issued = info.IssuedAt is { } i ? $"émis le {i.ToLocalTime():dd/MM HH:mm:ss} (il y a {(DateTimeOffset.Now - i).TotalMinutes:0} min)" : "date d'émission inconnue";
        var expires = info.ExpiresAt is { } e ? $"valable jusqu'au {e.ToLocalTime():dd/MM/yyyy HH:mm}" : "sans date de fin";
        return $"Jeton : compte {TokenInfo.ShortIdentity(info.HexIdentity)}, {issued}, {expires}.";
    }

    private string DescribeCounts() =>
        string.Join(", ", _cache.Counts().OrderBy(c => c.Key).Select(c => $"{c.Key} {c.Value}"));

    private void SetState(ReaderState state, string detail)
    {
        State = state;
        StateDetail = detail;
        StateChanged?.Invoke();
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync("fermeture du bot");
}

public enum ReaderState
{
    Deconnecte,
    Connexion,
    Connecte,
    Perdu,
    Erreur,
}
