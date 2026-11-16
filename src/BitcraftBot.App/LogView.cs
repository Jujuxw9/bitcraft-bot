using System.Collections.ObjectModel;
using System.Windows.Media;
using BitcraftBot.Core.Logging;

namespace BitcraftBot.App;

// Une ligne affichée dans l'onglet Journal.
public sealed record LogLine(LogEntry Entry, string Text, Brush Color);

// Garde les dernières lignes du journal et n'affiche que les niveaux cochés.
public sealed class LogView
{
    private const int MaxLines = 3000;
    private static readonly Brush ErreurBrush = Frozen(Color.FromRgb(242, 139, 130));
    private static readonly Brush AlerteBrush = Frozen(Color.FromRgb(253, 214, 99));
    private static readonly Brush InfoBrush = Frozen(Color.FromRgb(200, 200, 200));
    private static readonly Brush DetailBrush = Frozen(Color.FromRgb(140, 145, 150));

    private readonly List<LogLine> _all = [];
    private readonly HashSet<LogLevel> _visible = [LogLevel.Erreur, LogLevel.Alerte, LogLevel.Info];

    public ObservableCollection<LogLine> Visible { get; } = [];

    public int AlertCount { get; private set; }
    public int ErrorCount { get; private set; }

    public LogLine Add(LogEntry entry)
    {
        var line = new LogLine(entry, $"{entry.Time:HH:mm:ss} {LogEntry.LevelText(entry.Level),-6} [{entry.Category}] {entry.Message}",
            entry.Level switch
            {
                LogLevel.Erreur => ErreurBrush,
                LogLevel.Alerte => AlerteBrush,
                LogLevel.Detail => DetailBrush,
                _ => InfoBrush,
            });
        if (entry.Level == LogLevel.Alerte) AlertCount++;
        if (entry.Level == LogLevel.Erreur) ErrorCount++;

        _all.Add(line);
        if (_all.Count > MaxLines)
        {
            var removed = _all[0];
            _all.RemoveAt(0);
            Visible.Remove(removed);
        }
        if (_visible.Contains(entry.Level))
        {
            Visible.Add(line);
        }
        return line;
    }

    public void SetVisible(LogLevel level, bool visible)
    {
        if (visible) _visible.Add(level);
        else _visible.Remove(level);
        Visible.Clear();
        foreach (var line in _all.Where(l => _visible.Contains(l.Entry.Level)))
        {
            Visible.Add(line);
        }
    }

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
