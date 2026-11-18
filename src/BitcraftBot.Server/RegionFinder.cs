using System.Globalization;
using BitcraftBot.Server.Protocol;

namespace BitcraftBot.Server;

// Découverte (avant la lecture de la région) : demande au module global si le compte existe et dans quelle région
// se trouve le personnage. Liaison courte, en lecture seule, ouverte seulement quand le jeu est fermé :
// fermer une liaison globale pendant que le jeu tourne pourrait déconnecter le personnage (constats H1).
public static class RegionFinder
{
    public const string GlobalModule = "bitcraft-live-global";

    public static IReadOnlyList<string> Queries(string hexIdentity) =>
    [
        $"SELECT * FROM user_state WHERE identity = 0x{hexIdentity}",
        $"SELECT * FROM user_region_state WHERE identity = 0x{hexIdentity}",
        "SELECT * FROM region_connection_info",
        "SELECT * FROM world_region_name_state",
    ];

    public static async Task<RegionDiscovery> FindAsync(string token, string? hexIdentity, TimeSpan timeout, CancellationToken cancel)
    {
        Schema schema;
        using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(30) })
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd(ServerEndpoints.UserAgent);
            schema = Schema.Parse(await http.GetStringAsync(ServerEndpoints.SchemaUri(ServerEndpoints.Host, GlobalModule), cancel));
        }

        var done = new TaskCompletionSource<RegionDiscovery>(TaskCreationOptions.RunContinuationsAsynchronously);
        var identity = Row.Normalize(hexIdentity);
        var messages = 0;
        await using var connection = new SpacetimeConnection();
        connection.MessageReceived += json =>
        {
            Interlocked.Increment(ref messages);
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
                        done.TrySetResult(Read(cache, identity!));
                        break;
                    case SubscriptionErrorEvent error:
                        done.TrySetResult(RegionDiscovery.Fail($"requête refusée : {error.Error}"));
                        break;
                }
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException)
            {
                done.TrySetResult(RegionDiscovery.Fail($"réponse illisible : {ex.Message}"));
            }
        };
        connection.Closed += reason => done.TrySetResult(RegionDiscovery.Fail(Volatile.Read(ref messages) == 0
            ? "le module global a refusé la liaison dès l'ouverture (authentification du compte trop ancienne ?)"
            : $"liaison terminée ({reason})"));

        await connection.ConnectAsync(ServerEndpoints.SubscribeUri(ServerEndpoints.Host, GlobalModule), token, cancel);
        var finished = await Task.WhenAny(done.Task, Task.Delay(timeout, cancel));
        return finished == done.Task ? done.Task.Result : RegionDiscovery.Fail("pas de réponse du module global à temps");
    }

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
}
