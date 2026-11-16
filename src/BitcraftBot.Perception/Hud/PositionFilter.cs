namespace BitcraftBot.Perception.Hud;

// Garde-fou sur la position (Q-016, méthode D) : le personnage se déplace lentement.
// Une position qui « saute » loin de la précédente est mise en attente : elle n'est acceptée
// que si la lecture suivante la confirme (cas d'une téléportation réelle).
public sealed class PositionFilter
{
    // Vitesse maximale admise, en unités de carte par seconde, plus une marge fixe.
    // À vérifier en jeu : à pied, le personnage parcourt quelques unités par seconde ; une monture ou un chariot va plus vite.
    public const double MaxSpeedPerSecond = 40;
    public const double Margin = 15;

    private (int North, int East)? _accepted;
    private DateTime _acceptedAt;
    private (int North, int East)? _pending;

    public (int North, int East)? Last => _accepted;

    // Renvoie la position acceptée, ou null si la lecture est rejetée (saut à confirmer).
    public (int North, int East)? Accept((int North, int East)? read, DateTime at, out bool rejected)
    {
        rejected = false;
        if (read is null)
        {
            return null;
        }
        if (_accepted is null)
        {
            return Keep(read.Value, at);
        }
        var seconds = Math.Max((at - _acceptedAt).TotalSeconds, 0);
        var dn = read.Value.North - _accepted.Value.North;
        var de = read.Value.East - _accepted.Value.East;
        var distance = Math.Sqrt(dn * dn + de * de);
        if (distance <= MaxSpeedPerSecond * seconds + Margin || read == _pending)
        {
            return Keep(read.Value, at);
        }
        _pending = read;
        rejected = true;
        return null;
    }

    public void Reset()
    {
        _accepted = null;
        _pending = null;
    }

    private (int, int) Keep((int North, int East) position, DateTime at)
    {
        _accepted = position;
        _acceptedAt = at;
        _pending = null;
        return position;
    }
}
