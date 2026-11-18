using System.Text.Json;

namespace BitcraftBot.Server.Protocol;

// Messages reçus du serveur (protocole SpacetimeDB « v1.json »), traduits en événements simples.
public abstract record ServerEvent(string Kind);

// Le serveur confirme l'identité du compte. Le jeton renvoyé dans ce message est volontairement ignoré.
public sealed record IdentityEvent(string? HexIdentity) : ServerEvent("IdentityToken");

// Réponse complète à un abonnement : remplace tout le contenu local.
public sealed record SubscriptionEvent(IReadOnlyList<TableChange> Tables) : ServerEvent("InitialSubscription");

// Changements dans les tables suivies (action d'un joueur, du serveur…).
public sealed record TransactionEvent(IReadOnlyList<TableChange> Tables, string? Reducer, string? Failure) : ServerEvent("TransactionUpdate");

// Abonnement refusé (requête invalide, table inconnue…).
public sealed record SubscriptionErrorEvent(string Error) : ServerEvent("SubscriptionError");

public sealed record UnknownEvent(string Type) : ServerEvent(Type);

public static class ServerMessages
{
    public static ServerEvent Parse(string json, Schema? schema)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return new UnknownEvent("non-objet");
        }
        var property = root.EnumerateObject().FirstOrDefault();
        var body = property.Value;
        switch (property.Name)
        {
            case "IdentityToken":
                return new IdentityEvent(body.TryGetProperty("identity", out var id) ? Row.ReadIdentity(id) : null);

            case "InitialSubscription":
                return new SubscriptionEvent(ReadTables(body.GetProperty("database_update"), schema));

            case "SubscribeMultiApplied":
                return new SubscriptionEvent(ReadTables(body.GetProperty("update"), schema));

            case "TransactionUpdate":
            {
                string? reducer = body.TryGetProperty("reducer_call", out var call) && call.TryGetProperty("reducer_name", out var rn)
                    ? rn.GetString()
                    : null;
                if (body.TryGetProperty("status", out var status))
                {
                    if (status.TryGetProperty("Committed", out var committed))
                    {
                        return new TransactionEvent(ReadTables(committed, schema), reducer, null);
                    }
                    if (status.TryGetProperty("Failed", out var failed))
                    {
                        return new TransactionEvent([], reducer, failed.ValueKind == JsonValueKind.String ? failed.GetString() : failed.GetRawText());
                    }
                }
                return new TransactionEvent([], reducer, null);
            }

            case "TransactionUpdateLight":
                return new TransactionEvent(ReadTables(body.GetProperty("update"), schema), null, null);

            case "SubscriptionError":
                return new SubscriptionErrorEvent(body.TryGetProperty("error", out var err) ? err.GetString() ?? "?" : body.GetRawText());

            default:
                return new UnknownEvent(property.Name ?? "?");
        }
    }

    private static List<TableChange> ReadTables(JsonElement databaseUpdate, Schema? schema)
    {
        var result = new List<TableChange>();
        if (!databaseUpdate.TryGetProperty("tables", out var tables))
        {
            return result;
        }
        foreach (var table in tables.EnumerateArray())
        {
            var name = table.GetProperty("table_name").GetString() ?? "?";
            var tableSchema = schema?.Find(name);
            var inserts = new List<Row>();
            var deletes = new List<Row>();
            if (table.TryGetProperty("updates", out var updates))
            {
                foreach (var update in updates.EnumerateArray())
                {
                    ReadRows(update, "deletes", tableSchema, deletes);
                    ReadRows(update, "inserts", tableSchema, inserts);
                }
            }
            result.Add(new TableChange(name, inserts, deletes));
        }
        return result;
    }

    private static void ReadRows(JsonElement update, string name, TableSchema? schema, List<Row> into)
    {
        if (!update.TryGetProperty(name, out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            return;
        }
        foreach (var row in rows.EnumerateArray())
        {
            // Chaque ligne est normalement une chaîne qui contient du JSON ; on accepte aussi du JSON direct.
            var text = row.ValueKind == JsonValueKind.String ? row.GetString()! : row.GetRawText();
            into.Add(Row.Parse(text, schema));
        }
    }

    // Résumé d'un message pour le journal : type, taille, et pour les premiers messages un extrait.
    // Tout ce qui ressemble à un jeton (JWT) est masqué.
    public static string Summarize(long index, string json, bool withExcerpt)
    {
        var type = "?";
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                type = doc.RootElement.EnumerateObject().FirstOrDefault().Name ?? "?";
            }
        }
        catch (JsonException)
        {
            type = "JSON illisible";
        }
        var text = $"Message n° {index} : {type}, {json.Length} caractères";
        if (!withExcerpt)
        {
            return text + ".";
        }
        var excerpt = Redact(json);
        if (excerpt.Length > 700)
        {
            excerpt = excerpt[..700] + "…";
        }
        return $"{text}. Extrait : {excerpt}";
    }

    public static string Redact(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+", "[jeton masqué]");

    // Message d'abonnement (ancien format « Subscribe » : chaque envoi remplace les abonnements précédents).
    public static string Subscribe(IEnumerable<string> queries, uint requestId) =>
        JsonSerializer.Serialize(new { Subscribe = new { query_strings = queries.ToArray(), request_id = requestId } });
}
