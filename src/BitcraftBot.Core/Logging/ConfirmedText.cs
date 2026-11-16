namespace BitcraftBot.Core.Logging;

// Texte (zone, région) qui ne change qu'après deux lectures identiques de suite.
public sealed class ConfirmedText
{
    private string? _confirmed;
    private string? _candidate;

    public void Reset()
    {
        _confirmed = null;
        _candidate = null;
    }

    // Renvoie le changement confirmé (avant, après), sinon null.
    public (string Before, string After)? Update(string? read)
    {
        if (read is null)
        {
            return null;
        }
        if (_confirmed is null)
        {
            _confirmed = read;
            return null;
        }
        if (read == _confirmed)
        {
            _candidate = null;
            return null;
        }
        if (_candidate != read)
        {
            _candidate = read;
            return null;
        }
        var change = (_confirmed, read);
        _confirmed = read;
        _candidate = null;
        return change;
    }
}
