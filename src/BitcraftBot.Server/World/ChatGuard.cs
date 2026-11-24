using System.Globalization;
using System.Text;

namespace BitcraftBot.Server.World;

// v0.4.10 (demandes de Julien) : plafonds de réponses, amis et liste noire, ton selon le canal et l'heure, historique et statistiques.
// Rien ici n'envoie quoi que ce soit : ces classes décident seulement si le bot répond, et comment.

// Plafonds : par heure, par jour (24 h glissantes) et par joueur et par heure. Au plafond, le bot se tait.
public sealed class ChatQuota
{
    public const int PerHour = 20;
    public const int PerDay = 100;
    public const int PerPlayerPerHour = 3;

    private readonly List<(DateTimeOffset At, string User)> _sent = [];

    // null = on peut répondre ; sinon la raison, pour le journal.
    public string? Why(string user, DateTimeOffset now)
    {
        _sent.RemoveAll(s => now - s.At > TimeSpan.FromHours(24));
        var hour = _sent.Where(s => now - s.At <= TimeSpan.FromHours(1)).ToList();
        if (hour.Count >= PerHour)
        {
            return $"plafond de {PerHour} réponses par heure atteint";
        }
        if (_sent.Count >= PerDay)
        {
            return $"plafond de {PerDay} réponses par jour atteint";
        }
        var key = ChatLists.Normalize(user);
        return hour.Count(s => s.User == key) >= PerPlayerPerHour ? $"déjà {PerPlayerPerHour} réponses à {user} dans l'heure" : null;
    }

    public void Record(string user, DateTimeOffset now) => _sent.Add((now, ChatLists.Normalize(user)));

    public int LastHour(DateTimeOffset now) => _sent.Count(s => now - s.At <= TimeSpan.FromHours(1));

    public int LastDay(DateTimeOffset now) => _sent.Count(s => now - s.At <= TimeSpan.FromHours(24));
}

// Amis (ton plus familier) et liste noire (jamais de réponse) : fichiers texte « amis.txt » et « liste-noire.txt »
// dans le dossier « phrases-chat » à côté du .exe. Une ligne par pseudo ; « # » commence un commentaire.
// Pseudos comparés sans majuscules ni espaces ni signes.
public sealed class ChatLists
{
    public const string FriendsFile = "amis.txt";
    public const string BlockedFile = "liste-noire.txt";

    // Ami d'office, demandé par Julien le 2026-11-24 (écrit avec un K).
    public static readonly string[] DefaultFriends = ["The Krazy Turkey"];

    private readonly HashSet<string> _friends = [];
    private readonly HashSet<string> _blocked = [];

    public IReadOnlyCollection<string> Friends => _friends;
    public IReadOnlyCollection<string> Blocked => _blocked;

    public static string Normalize(string name) => new(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    public bool IsFriend(string name) => _friends.Contains(Normalize(name));
    public bool IsBlocked(string name) => _blocked.Contains(Normalize(name));

    public static ChatLists Parse(string friends, string blocked)
    {
        var lists = new ChatLists();
        Fill(lists._friends, friends);
        Fill(lists._blocked, blocked);
        lists._friends.ExceptWith(lists._blocked); // liste noire plus forte
        return lists;
    }

    private static void Fill(HashSet<string> set, string content)
    {
        foreach (var raw in content.Split('\n'))
        {
            var hash = raw.IndexOf('#');
            var key = Normalize(hash >= 0 ? raw[..hash] : raw);
            if (key.Length > 0)
            {
                set.Add(key);
            }
        }
    }

    // Lit les deux fichiers du dossier ; crée ceux qui manquent (amis.txt avec les amis d'office).
    public static ChatLists Load(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            var friends = Path.Combine(folder, FriendsFile);
            if (!File.Exists(friends))
            {
                File.WriteAllText(friends, "# Amis : un pseudo par ligne. Le bot leur répond sur un ton plus familier.\n"
                    + string.Join('\n', DefaultFriends) + "\n", new UTF8Encoding(false));
            }
            var blocked = Path.Combine(folder, BlockedFile);
            if (!File.Exists(blocked))
            {
                File.WriteAllText(blocked, "# Liste noire : un pseudo par ligne. Le bot ne répond jamais à ces joueurs.\n", new UTF8Encoding(false));
            }
            return Parse(File.ReadAllText(friends, Encoding.UTF8), File.ReadAllText(blocked, Encoding.UTF8));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Parse(string.Join('\n', DefaultFriends), "");
        }
    }
}

// Ton selon le canal (Trade sérieux, Local détendu) et l'ami (très familier) ; mots selon l'heure.
public static class ChatTone
{
    public readonly record struct Tone(double Smiley, double Casual);

    public static Tone For(int channel, bool friend)
    {
        if (friend)
        {
            return new Tone(0.5, 0.7);
        }
        return (ChatChannel)channel switch
        {
            ChatChannel.Commerce => new Tone(0.1, 0.05),
            ChatChannel.Local => new Tone(0.4, 0.35),
            ChatChannel.RechercheGroupe => new Tone(0.3, 0.25),
            _ => new Tone(CannedReplies.SmileyChance, ChatStyle.CasualChance),
        };
    }

    // Le matin « Bonjour », le soir « Bonsoir » (et « Good morning / afternoon / evening »). hour < 0 : rien.
    public static string AdaptToTime(string text, string language, int hour)
    {
        if (hour < 0)
        {
            return text;
        }
        var evening = hour >= 18 || hour < 5;
        if (language == "fr")
        {
            return evening
                ? text.Replace("Bonjour", "Bonsoir", StringComparison.Ordinal).Replace("bonne journée", "bonne soirée", StringComparison.Ordinal)
                    .Replace("Bonne journée", "Bonne soirée", StringComparison.Ordinal)
                : text.Replace("Bonsoir", "Bonjour", StringComparison.Ordinal).Replace("bonne soirée", "bonne journée", StringComparison.Ordinal)
                    .Replace("Bonne soirée", "Bonne journée", StringComparison.Ordinal);
        }
        var word = hour is >= 5 and < 12 ? "morning" : hour is >= 12 and < 18 ? "afternoon" : "evening";
        foreach (var w in new[] { "morning", "afternoon", "evening" })
        {
            text = text.Replace("Good " + w, "Good " + word, StringComparison.Ordinal).Replace("good " + w, "good " + word, StringComparison.Ordinal);
        }
        return text;
    }
}

// Une ligne de l'historique du chat (onglet CHAT).
public sealed record ChatHistoryEntry(DateTimeOffset At, string Channel, string User, string Message, string Theme, string Reply, string Status);

// Historique (200 dernières lignes) et statistiques : thèmes demandés, messages tombés dans [autre].
// Les comptes sont gardés dans « stats.txt » du dossier, et les messages sans thème dans « messages-sans-theme.txt »
// (pour savoir quels thèmes ajouter).
public sealed class ChatStats
{
    public const string StatsFile = "stats.txt";
    public const string UnmatchedFile = "messages-sans-theme.txt";
    private const int MaxHistory = 200;

    private readonly List<ChatHistoryEntry> _history = [];
    private readonly Dictionary<string, int> _themes = new(StringComparer.Ordinal);
    private readonly string? _folder;

    public ChatStats(string? folder = null)
    {
        _folder = folder;
        if (folder is null || Path.Combine(folder, StatsFile) is not { } path || !File.Exists(path))
        {
            return;
        }
        try
        {
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                var tab = line.IndexOf('\t');
                if (tab > 0 && int.TryParse(line[(tab + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    _themes[line[..tab]] = n;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Statistiques illisibles : on repart de zéro.
        }
    }

    public IReadOnlyList<ChatHistoryEntry> History => _history;
    public IReadOnlyDictionary<string, int> Themes => _themes;
    public int Total => _themes.Values.Sum();
    public int Unmatched => _themes.GetValueOrDefault(ChatPhraseBook.DefaultTheme);

    // Ajoute une ligne ; countTheme : le message compte dans les statistiques (pas pour un test).
    public void Add(ChatHistoryEntry entry)
    {
        _history.Add(entry);
        if (_history.Count > MaxHistory)
        {
            _history.RemoveAt(0);
        }
    }

    // Compte un message « @gpt » reçu dans les statistiques (pas pour un test).
    public void Count(ChatHistoryEntry entry)
    {
        _themes[entry.Theme] = _themes.GetValueOrDefault(entry.Theme) + 1;
        Save(entry);
    }

    private void Save(ChatHistoryEntry entry)
    {
        if (_folder is null)
        {
            return;
        }
        try
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllLines(Path.Combine(_folder, StatsFile),
                _themes.OrderByDescending(t => t.Value).Select(t => $"{t.Key}\t{t.Value}"), new UTF8Encoding(false));
            if (entry.Theme == ChatPhraseBook.DefaultTheme)
            {
                File.AppendAllText(Path.Combine(_folder, UnmatchedFile),
                    $"{entry.At.LocalDateTime:yyyy-MM-dd HH:mm}\t{entry.Message.ReplaceLineEndings(" ")}\n", new UTF8Encoding(false));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Pas grave : l'historique à l'écran reste.
        }
    }

    // Résumé court pour l'onglet.
    public string Summary()
    {
        if (Total == 0)
        {
            return "Aucun message « @gpt » compté pour l'instant.";
        }
        var top = string.Join(", ", _themes.Where(t => t.Key != ChatPhraseBook.DefaultTheme).OrderByDescending(t => t.Value).Take(8).Select(t => $"{t.Key} {t.Value}"));
        return $"{Total} message(s) comptés. Thèmes les plus demandés : {(top.Length > 0 ? top : "aucun")}. Sans thème (autre) : {Unmatched}.";
    }
}
