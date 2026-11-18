namespace BitcraftBot.Server.Protocol;

// Copie locale des lignes reçues, table par table. Le serveur envoie d'abord tout (abonnement),
// puis seulement les changements (suppressions puis ajouts ; une modification = les deux).
public sealed class TableCache
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Dictionary<string, Row>> _tables = new(StringComparer.Ordinal);

    public void Clear()
    {
        lock (_lock)
        {
            _tables.Clear();
        }
    }

    public void Apply(TableChange change)
    {
        lock (_lock)
        {
            if (!_tables.TryGetValue(change.Table, out var rows))
            {
                rows = new Dictionary<string, Row>(StringComparer.Ordinal);
                _tables[change.Table] = rows;
            }
            foreach (var row in change.Deletes)
            {
                rows.Remove(row.Key);
            }
            foreach (var row in change.Inserts)
            {
                rows[row.Key] = row;
            }
        }
    }

    // Remplace tout le contenu (réponse complète à un nouvel abonnement).
    public void Reset(IEnumerable<TableChange> changes)
    {
        lock (_lock)
        {
            _tables.Clear();
        }
        foreach (var change in changes)
        {
            Apply(change);
        }
    }

    public IReadOnlyList<Row> Rows(string table)
    {
        lock (_lock)
        {
            return _tables.TryGetValue(table, out var rows) ? rows.Values.ToList() : [];
        }
    }

    public IReadOnlyDictionary<string, int> Counts()
    {
        lock (_lock)
        {
            return _tables.ToDictionary(t => t.Key, t => t.Value.Count);
        }
    }
}

public sealed record TableChange(string Table, IReadOnlyList<Row> Inserts, IReadOnlyList<Row> Deletes);
