using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Onglet « Personnage », carte « Bonus actifs » (v0.3.16, demande de Julien) : nom, type, temps restant et effets
// de chaque bonus du personnage, lus sur le serveur (active_buff_state) et décrits par les données du jeu (buff_desc).
public partial class MainWindow
{
    private static readonly TimeSpan BuffInterval = TimeSpan.FromSeconds(1);

    private DateTime _buffsShownAt = DateTime.MinValue;
    private string _buffSignature = "";

    private sealed record BuffRow(string Name, string Type, string Remaining, string Effects);

    private void ShowBuffs(PlayerSnapshot s)
    {
        if (DateTime.Now - _buffsShownAt < BuffInterval)
        {
            return;
        }
        _buffsShownAt = DateTime.Now;
        if (s.Buffs is null)
        {
            BuffSummary.Text = "Pas encore lus.";
            return;
        }
        var now = DateTimeOffset.UtcNow;
        var book = _foods;
        var rows = s.Buffs
            .Select(b => (Buff: b, Left: b.RemainingSeconds(now)))
            .Where(x => x.Left != 0)
            .OrderBy(x => x.Left < 0 ? 0 : 1)
            .ThenByDescending(x => x.Left)
            .Select(x =>
            {
                var desc = book?.Buffs.GetValueOrDefault(x.Buff.BuffId);
                var effects = desc is null || desc.Stats.Count == 0 ? "—" : string.Join(", ", desc.Stats.Select(st => st.Text));
                var left = x.Left < 0 ? "permanent" : FormatDuration(x.Left) + (x.Buff.IsPaused ? " (en pause)" : "");
                return new BuffRow(desc?.Name ?? $"bonus n° {x.Buff.BuffId}", desc?.Type ?? "?", left, effects);
            })
            .ToList();
        // Signature sans les secondes : la liste n'est refaite que si un bonus apparaît, disparaît ou change de minute.
        var signature = string.Join("|", rows.Select(r => r.Name + r.Remaining));
        if (signature == _buffSignature)
        {
            return;
        }
        _buffSignature = signature;
        BuffList.ItemsSource = rows;
        BuffSummary.Text = rows.Count == 0
            ? "Aucun bonus actif."
            : $"{rows.Count} bonus actif(s). Les bonus « en pause » ne s'écoulent que quand vous êtes en jeu.";
    }

    private static string FormatDuration(int seconds) => seconds switch
    {
        < 90 => $"{seconds} s",
        < 5400 => $"{seconds / 60} min",
        _ => $"{seconds / 3600} h {seconds % 3600 / 60:00}",
    };
}
