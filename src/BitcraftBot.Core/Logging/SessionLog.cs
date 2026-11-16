using System.Text;
using BitcraftBot.Core.Time;

namespace BitcraftBot.Core.Logging;

public enum LogLevel
{
    Info,
    Alerte,
    Erreur,
}

// Une ligne du journal.
public sealed record LogEntry(DateTime Time, LogLevel Level, string Category, string Message)
{
    public override string ToString() =>
        $"{Time:yyyy-MM-dd HH:mm:ss.fff} [{LevelText(Level)}] [{Category}] {Message}";

    private static string LevelText(LogLevel level) => level switch
    {
        LogLevel.Info => "INFO",
        LogLevel.Alerte => "ALERTE",
        LogLevel.Erreur => "ERREUR",
        _ => level.ToString().ToUpperInvariant(),
    };
}

// Journal d'une session (D-031) : un fichier par session,
// rangé dans journaux/v{version}/{jour}/session_{jour}_{heure}.log.
public sealed class SessionLog : IDisposable
{
    private readonly object _lock = new();
    private readonly IClock _clock;
    private readonly StreamWriter _writer;

    public string FilePath { get; }

    // Prévient l'interface à chaque nouvelle ligne.
    public event Action<LogEntry>? EntryWritten;

    public SessionLog(string rootDirectory, string version, IClock? clock = null)
    {
        _clock = clock ?? SystemClock.Instance;
        FilePath = BuildFilePath(rootDirectory, version, _clock.Now);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        _writer = new StreamWriter(FilePath, append: true, new UTF8Encoding(false)) { AutoFlush = true };
    }

    // Calcule le chemin du fichier : un dossier par version, puis par jour.
    public static string BuildFilePath(string rootDirectory, string version, DateTime start)
    {
        var day = start.ToString("yyyy-MM-dd");
        var fileName = $"session_{start:yyyy-MM-dd_HH-mm-ss}.log";
        return Path.Combine(rootDirectory, "journaux", $"v{ShortVersion(version)}", day, fileName);
    }

    // "0.1.0+abc123" -> "0.1.0" : le dossier ne garde que le numéro de version.
    public static string ShortVersion(string version)
    {
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }

    public void Info(string category, string message) => Write(LogLevel.Info, category, message);

    public void Alerte(string category, string message) => Write(LogLevel.Alerte, category, message);

    public void Erreur(string category, string message) => Write(LogLevel.Erreur, category, message);

    public void Write(LogLevel level, string category, string message)
    {
        var entry = new LogEntry(_clock.Now, level, category, message);
        lock (_lock)
        {
            _writer.WriteLine(entry.ToString());
        }
        EntryWritten?.Invoke(entry);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
        }
    }
}
