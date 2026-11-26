using System.Globalization;
using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;

namespace BitcraftBot.Server;

// Découverte (avant la lecture de la région) : demande au module global si le compte existe et dans quelle région
// se trouve le personnage. Liaison courte, en lecture seule, ouverte seulement quand le jeu est fermé :
// fermer une liaison globale pendant que le jeu tourne pourrait déconnecter le personnage (constats H1).
public static class RegionFinder
{
    public const string GlobalModule = "idc-global";

    public static IReadOnlyList<string> Queries(string hexIdentity) =>
    [
        $"SELECT * FROM user_state WHERE identity = 0x{hexIdentity}",
        $"SELECT * FROM user_region_state WHERE identity = 0x{hexIdentity}",
        "SELECT * FROM region_connection_info",
        "SELECT * FROM world_region_name_state",
    ];

    public static async Task<RegionDiscovery> FindAsync(string token, string? hexIdentity, TimeSpan timeout, Action<string>? diagnostic, CancellationToken cancel)
    {
        Schema schema;
        using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(30) })
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd(ServerEndpoints.UserAgent);
            schema = Schema.Parse(await http.GetStringAsync(ServerEndpoints.SchemaUri(ServerEndpoints.Host, GlobalModule), cancel));
        }

        var done = new TaskCompletionSource<RegionDiscovery>(TaskCreationOptions.RunContinuationsAsynchronously);
        // v0.4.16 (archive du chat) : une fois la région trouvée, même liaison, un 2e abonnement pour les messages privés.
        // Pas de liaison de plus : rien ne change pour le jeu. Refus ou silence = découverte gardée, sans messages privés.
        var withDms = new TaskCompletionSource<RegionDiscovery>(TaskCreationOptions.RunContinuationsAsynchronously);
        RegionDiscovery? found = null;
        var identity = Row.Normalize(hexIdentity);
        var messages = 0;
        await using var connection = new SpacetimeConnection();
        if (diagnostic is not null)
        {
            connection.Diagnostic += diagnostic;
        }
        connection.MessageReceived += json =>
        {
            var n = Interlocked.Increment(ref messages);
            diagnostic?.Invoke(ServerMessages.Summarize(n, json, withExcerpt: true));
            try
            {
                switch (ServerMessages.Parse(json, schema))
                {
                    case IdentityEvent id:
                        identity ??= Row.Normalize(id.HexIdentity);
                        if (identity is null)
                        {
                            done.TrySetResult(RegionDiscovery.Fail("identité du compte inconnue"));
                            return;
                        }
                        _ = connection.SubscribeAsync(Queries(identity), 1, CancellationToken.None);
                        break;
                    case SubscriptionEvent sub:
                        var cache = new TableCache();
                        cache.Reset(sub.Tables);
                        if (Volatile.Read(ref found) is { } first)
                        {
                            var dms = DirectMessages.Read(cache);
                            withDms.TrySetResult(first with { DirectMessages = dms, DirectMessagesNote = $"{dms.Count} message(s) privé(s) lu(s)" });
                            break;
                        }
                        var result = Read(cache, identity!);
                        if (result.Success)
                        {
                            Volatile.Write(ref found, result);
                            _ = connection.SubscribeAsync(Queries(identity!).Append(DirectMessages.Query), 2, CancellationToken.None);
                        }
                        done.TrySetResult(result);
                        break;
                    case SubscriptionErrorEvent error:
                        if (Volatile.Read(ref found) is { } kept)
                        {
                            withDms.TrySetResult(kept with { DirectMessagesNote = $"messages privés refusés par le serveur : {error.Error}" });
                            break;
                        }
                        done.TrySetResult(RegionDiscovery.Fail($"requête refusée : {error.Error}"));
                        break;
                }
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException)
            {
                if (Volatile.Read(ref found) is { } kept)
                {
                    withDms.TrySetResult(kept with { DirectMessagesNote = $"messages privés illisibles : {ex.Message}" });
                    return;
                }
                done.TrySetResult(RegionDiscovery.Fail($"réponse illisible : {ex.Message}"));
            }
        };
        connection.Closed += reason =>
        {
            if (Volatile.Read(ref found) is { } kept)
            {
                withDms.TrySetResult(kept with { DirectMessagesNote = $"liaison terminée avant les messages privés ({reason})" });
            }
            done.TrySetResult(RegionDiscovery.Fail(Volatile.Read(ref messages) == 0
                ? "le module global a refusé la liaison dès l'ouverture (normal si le jeu n'a pas été lancé depuis plus d'une heure : ce module exige une connexion au jeu de moins d'1 h)"
                : $"liaison terminée ({reason})"));
        };

        await connection.ConnectAsync(ServerEndpoints.SubscribeUri(ServerEndpoints.Host, GlobalModule), token, cancel);
        var finished = await Task.WhenAny(done.Task, Task.Delay(timeout, cancel));
        if (finished != done.Task)
        {
            return RegionDiscovery.Fail("pas de réponse du module global à temps");
        }
        var discovery = done.Task.Result;
        if (!discovery.Success)
        {
            return discovery;
        }
        // Messages privés : 5 s au plus de plus ; sinon, la découverte est gardée sans eux.
        var dmFinished = await Task.WhenAny(withDms.Task, Task.Delay(DirectMessagesWait, cancel));
        return dmFinished == withDms.Task ? withDms.Task.Result : discovery with { DirectMessagesNote = "pas de réponse à temps pour les messages privés" };
    }

    public static readonly TimeSpan DirectMessagesWait = TimeSpan.FromSeconds(5);

    // Lit le résultat : compte connu ? région du personnage ? adresse du module de cette région ?
    public static RegionDiscovery Read(TableCache cache, string hexIdentity)
    {
        var id = Row.Normalize(hexIdentity);
        var user = cache.Rows("user_state").FirstOrDefault(r => Row.Normalize(r.Identity("identity")) == id);
        if (user is null)
        {
            return RegionDiscovery.Fail("aucun personnage pour ce compte (l'e-mail est-il bien celui du compte du jeu ?)") with { AccountKnown = false };
        }
        var regionRow = cache.Rows("user_region_state").FirstOrDefault(r => Row.Normalize(r.Identity("identity")) == id);
        var regionId = regionRow?.I32("region_id");
        if (regionId is null)
        {
            return RegionDiscovery.Fail("compte connu, mais région du personnage inconnue") with { AccountKnown = true, EntityId = user.U64("entity_id") };
        }
        var connection = cache.Rows("region_connection_info").FirstOrDefault(r => r.I32("id") == regionId);
        var name = cache.Rows("world_region_name_state").FirstOrDefault(r => r.I32("id") == regionId)?.Get("player_facing_name")?.ToString();
        var host = connection?.Get("host")?.ToString();
        return new RegionDiscovery(true, null, true, user.U64("entity_id"), regionId, name,
            HostOnly(host) ?? ServerEndpoints.Host,
            connection?.Get("module")?.ToString() ?? ServerEndpoints.RegionModule(regionId.Value));
    }

    // L'hôte peut arriver avec « https:// » ou un port : on ne garde que le nom.
    public static string? HostOnly(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }
        return Uri.TryCreate(host.Contains("://", StringComparison.Ordinal) ? host : "https://" + host, UriKind.Absolute, out var uri)
            ? uri.Host
            : host;
    }

    public static string Describe(RegionDiscovery d) =>
        d.Success
            ? string.Create(CultureInfo.InvariantCulture, $"personnage (entité {d.EntityId}) dans la région {d.RegionId} « {d.RegionName ?? "?"} » → module {d.Module} sur {d.Host}")
            : $"découverte impossible : {d.Error}";
}

public sealed record RegionDiscovery(bool Success, string? Error, bool AccountKnown, ulong? EntityId, int? RegionId, string? RegionName, string? Host, string? Module)
{
    public static RegionDiscovery Fail(string error) => new(false, error, false, null, null, null, null, null);

    // v0.4.16 : messages privés de Julien (reçus et envoyés) lus pendant la découverte, et ce qui s'est passé.
    public IReadOnlyList<DirectMessage> DirectMessages { get; init; } = [];

    public string DirectMessagesNote { get; init; } = "messages privés pas demandés";
}
