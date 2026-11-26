using System.Globalization;
using System.Text.Json;
using BitcraftBot.Server.Protocol;
using Microsoft.Data.Sqlite;

namespace BitcraftBot.Server.World;

// Archive du chat en local (v0.4.16, D-068) : tout le chat vu par le bot (Local, Région, LFG, Trade)
// et les messages privés de Julien (reçus et envoyés) quand le module global les donne.
// Petite base SQLite dans le dossier « archive-chat » à côté du .exe (ignoré par Git).
// Le serveur ne garde que les messages récents : on archive ce qui passe pendant que le bot est connecté.
// Un message n'est jamais enregistré deux fois (identifiant du serveur + sorte de message).

// Un message archivé. Channel : code du canal du serveur, ou ChatArchive.PrivateChannel pour un message privé.
public sealed record ArchivedChatMessage(
    string Kind,
    ulong ServerId,
    DateTimeOffset SentAt,
    int Channel,
    string PlayerName,
    ulong? PlayerEntityId,
    string Language,
    string Text,
    string? Region,
    bool ByBot,
    ulong? ReceiverEntityId = null)
{
    public string ChannelName => ChatArchive.ChannelName(Channel);
}

public sealed class ChatArchive : IDisposable
{
    public const string FolderName = "archive-chat";
    public const string FileName = "chat.db";

    // Sortes de messages : chat public (chat_message_state) ou message privé (direct_message_state).
    public const string KindChat = "chat";
    public const string KindPrivate = "prive";

    // Canal fictif des messages privés (le serveur n'en a pas pour eux dans la table des messages privés).
    public const int PrivateChannel = 100;

    private readonly SqliteConnection _db;
    private readonly object _lock = new();

    public string Location { get; }

    private ChatArchive(SqliteConnection db, string location)
    {
        _db = db;
        Location = location;
        Execute("""
            CREATE TABLE IF NOT EXISTS messages (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                kind TEXT NOT NULL,
                server_id TEXT NOT NULL,
                sent_at INTEGER NOT NULL,
                channel INTEGER NOT NULL,
                player_name TEXT NOT NULL,
                player_entity_id TEXT,
                language TEXT NOT NULL,
                text TEXT NOT NULL,
                region TEXT,
                by_bot INTEGER NOT NULL,
                receiver_entity_id TEXT,
                archived_at INTEGER NOT NULL,
                UNIQUE (kind, server_id)
            );
            CREATE INDEX IF NOT EXISTS messages_sent_at ON messages (sent_at);
            CREATE INDEX IF NOT EXISTS messages_player ON messages (player_name COLLATE NOCASE);
            """);
    }

    // Base dans le dossier donné (créé au besoin).
    public static ChatArchive OpenFolder(string folder)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, FileName);
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        db.Open();
        return new ChatArchive(db, path);
    }

    // Base en mémoire (tests).
    public static ChatArchive InMemory()
    {
        var db = new SqliteConnection("Data Source=:memory:");
        db.Open();
        return new ChatArchive(db, ":memory:");
    }

    // Enregistre les messages ; ceux déjà présents sont ignorés. Renvoie le nombre de messages nouveaux.
    public int Add(IEnumerable<ArchivedChatMessage> messages)
    {
        lock (_lock)
        {
            using var transaction = _db.BeginTransaction();
            using var cmd = _db.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT OR IGNORE INTO messages
                    (kind, server_id, sent_at, channel, player_name, player_entity_id, language, text, region, by_bot, receiver_entity_id, archived_at)
                VALUES ($kind, $id, $at, $channel, $name, $player, $lang, $text, $region, $bot, $receiver, $archived)
                """;
            var p = new Dictionary<string, SqliteParameter>();
            foreach (var name in new[] { "$kind", "$id", "$at", "$channel", "$name", "$player", "$lang", "$text", "$region", "$bot", "$receiver", "$archived" })
            {
                var param = cmd.CreateParameter();
                param.ParameterName = name;
                cmd.Parameters.Add(param);
                p[name] = param;
            }
            var added = 0;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var m in messages)
            {
                p["$kind"].Value = m.Kind;
                p["$id"].Value = Id(m.ServerId);
                p["$at"].Value = m.SentAt.ToUnixTimeSeconds();
                p["$channel"].Value = m.Channel;
                p["$name"].Value = m.PlayerName;
                p["$player"].Value = m.PlayerEntityId is { } e ? Id(e) : DBNull.Value;
                p["$lang"].Value = m.Language;
                p["$text"].Value = m.Text;
                p["$region"].Value = (object?)m.Region ?? DBNull.Value;
                p["$bot"].Value = m.ByBot ? 1 : 0;
                p["$receiver"].Value = m.ReceiverEntityId is { } r ? Id(r) : DBNull.Value;
                p["$archived"].Value = now;
                added += cmd.ExecuteNonQuery();
            }
            transaction.Commit();
            return added;
        }
    }

    public long Count()
    {
        lock (_lock)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM messages";
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
    }

    // Recherche : joueur (morceau du nom, sans tenir compte des majuscules), canal (null = tous), le plus récent en premier.
    public IReadOnlyList<ArchivedChatMessage> Search(string? player = null, int? channel = null, int limit = 500)
    {
        lock (_lock)
        {
            using var cmd = _db.CreateCommand();
            var where = new List<string>();
            if (!string.IsNullOrWhiteSpace(player))
            {
                where.Add("instr(lower(player_name), lower($player)) > 0");
                cmd.Parameters.AddWithValue("$player", player.Trim());
            }
            if (channel is { } c)
            {
                where.Add("channel = $channel");
                cmd.Parameters.AddWithValue("$channel", c);
            }
            cmd.CommandText = "SELECT kind, server_id, sent_at, channel, player_name, player_entity_id, language, text, region, by_bot, receiver_entity_id FROM messages"
                + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
                + " ORDER BY sent_at DESC, id DESC LIMIT $limit";
            cmd.Parameters.AddWithValue("$limit", Math.Max(1, limit));
            var list = new List<ArchivedChatMessage>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ArchivedChatMessage(
                    reader.GetString(0),
                    ulong.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                    DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2)),
                    reader.GetInt32(3),
                    reader.GetString(4),
                    reader.IsDBNull(5) ? null : ulong.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                    reader.GetString(6),
                    reader.GetString(7),
                    reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.GetInt64(9) != 0,
                    reader.IsDBNull(10) ? null : ulong.Parse(reader.GetString(10), CultureInfo.InvariantCulture)));
            }
            return list;
        }
    }

    public static string ChannelName(int channel) => channel == PrivateChannel ? "Privé" : ChatChannels.Name(channel);

    // Message du chat public → message archivé.
    public static ArchivedChatMessage From(ChatMessage m, string? region, bool byBot) =>
        new(KindChat, m.EntityId, m.SentAt, m.Channel, m.Username, m.OwnerEntityId, m.Language, m.Text, region, byBot);

    // Message privé → message archivé (le joueur est l'expéditeur).
    public static ArchivedChatMessage From(DirectMessage m) =>
        new(KindPrivate, m.EntityId, m.SentAt, PrivateChannel, m.Username, m.SenderEntityId, m.Language, m.Text, null, false, m.ReceiverEntityId);

    // Les identifiants du serveur sont des entiers positifs sur 64 bits : rangés en texte (SQLite n'a que des entiers signés).
    private static string Id(ulong id) => id.ToString(CultureInfo.InvariantCulture);

    private void Execute(string sql)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _db.Dispose();
        }
    }
}

// Message privé (table direct_message_state du module global ; chaque joueur ne voit que ses messages envoyés ou reçus).
// Format du code serveur : entity_id, username (expéditeur), title_id, sender_entity_id, receiver_entity_id,
// text, timestamp (secondes), language_code (facultatif).
public sealed record DirectMessage(ulong EntityId, ulong SenderEntityId, ulong ReceiverEntityId, string Username, string Language, string Text,
    DateTimeOffset SentAt);

public static class DirectMessages
{
    public const string Table = "direct_message_state";
    public const string Query = "SELECT * FROM direct_message_state";

    public static IReadOnlyList<DirectMessage> Read(TableCache cache)
    {
        var list = new List<DirectMessage>();
        foreach (var row in cache.Rows(Table))
        {
            if (Parse(row) is { } m)
            {
                list.Add(m);
            }
        }
        return list.OrderBy(m => m.SentAt).ThenBy(m => m.EntityId).ToList();
    }

    public static DirectMessage? Parse(Row row)
    {
        if (row.U64("entity_id") is not { } id || row.U64("sender_entity_id") is not { } sender
            || row.U64("receiver_entity_id") is not { } receiver || row.I64("timestamp") is not { } ts)
        {
            return null;
        }
        var text = row.Get("text") is { ValueKind: JsonValueKind.String } t ? t.GetString() ?? "" : "";
        var name = row.Get("username") is { ValueKind: JsonValueKind.String } u ? u.GetString() ?? "" : "";
        return new DirectMessage(id, sender, receiver, name, OptionalText(row.Get("language_code")) ?? "", text, DateTimeOffset.FromUnixTimeSeconds(ts));
    }

    // Texte facultatif du serveur : « "fr" », « {"some":"fr"} », « [0,"fr"] » ou rien (formats possibles, à vérifier).
    public static string? OptionalText(JsonElement? value)
    {
        if (value is not { } v)
        {
            return null;
        }
        switch (v.ValueKind)
        {
            case JsonValueKind.String:
                return v.GetString();
            case JsonValueKind.Object:
                foreach (var p in v.EnumerateObject())
                {
                    if (p.Name is "some" or "Some" or "0")
                    {
                        return OptionalText(p.Value);
                    }
                }
                return null;
            case JsonValueKind.Array when v.GetArrayLength() == 2 && v[0].ValueKind == JsonValueKind.Number && v[0].GetInt32() == 0:
                return OptionalText(v[1]);
            default:
                return null;
        }
    }
}
