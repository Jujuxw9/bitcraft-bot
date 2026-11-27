using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Bonus actifs, onglet « Bot » (v0.3.16, refait en v0.4.13 sur demande de Julien) : une petite liste de pastilles, sans tableau :
// un logo, le nom (« Level 2 Food Regen »), le temps restant et les effets. Lus sur le serveur (active_buff_state)
// et décrits par les données du jeu (buff_desc). Les vraies images des bonus sont des signes d'une police du jeu :
// on dessine un logo par famille de bonus (nourriture, vitesse, charge, fabrication…).
public partial class MainWindow
{
    private static readonly TimeSpan BuffInterval = TimeSpan.FromSeconds(1);

    private DateTime _buffsShownAt = DateTime.MinValue;
    private string _buffSignature = "";

    // Logo (emoji) et couleur d'après le nom du bonus.
    private static (string Logo, string Color) BuffLogo(string name, bool beneficial)
    {
        var n = name.ToLowerInvariant();
        return n switch
        {
            _ when n.Contains("food regen") => ("🍖", "#2E7D4F"),
            _ when n.Contains("carry") || n.Contains("cargo") => ("📦", "#8A5A2B"),
            _ when n.Contains("action speed") || n.Contains("speed") => ("⚡", "#B8861B"),
            _ when n.Contains("crafting") => ("🔨", "#2F6FA3"),
            _ when n.Contains("construction") || n.Contains("building") => ("🏗", "#2F6FA3"),
            _ when n.Contains("freez") || n.Contains("cold") => ("❄", "#3C8FB5"),
            _ when n.Contains("teleport") => ("🌀", "#7B4BA8"),
            _ when n.Contains("rested") => ("💤", "#4B5FA8"),
            _ when n.Contains("regen") || n.Contains("health") => ("❤", "#A83C48"),
            _ when n.Contains("stamina") => ("🔋", "#B8861B"),
            _ => (beneficial ? "✨" : "☠", beneficial ? "#2E7D4F" : "#9E3B34"),
        };
    }

    private void ShowBuffs(PlayerSnapshot s)
    {
        if (DateTime.Now - _buffsShownAt < BuffInterval)
        {
            return;
        }
        _buffsShownAt = DateTime.Now;
        if (s.Buffs is null)
        {
            BuffPanel.Children.Clear();
            _buffSignature = "";
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
                var effects = desc is null || desc.Stats.Count == 0 ? "" : string.Join(", ", desc.Stats.Select(st => st.Text));
                var left = x.Left < 0 ? "permanent" : FormatDuration(x.Left) + (x.Buff.IsPaused ? " (pause)" : "");
                return (Name: desc?.Name ?? $"bonus n° {x.Buff.BuffId}", Left: left, Effects: effects, Good: desc?.Beneficial ?? true);
            })
            .ToList();
        // Signature sans les secondes : la liste n'est refaite que si un bonus apparaît, disparaît ou change de minute.
        var signature = string.Join("|", rows.Select(r => r.Name + r.Left));
        if (signature == _buffSignature)
        {
            return;
        }
        _buffSignature = signature;
        BuffPanel.Children.Clear();
        if (rows.Count == 0)
        {
            BuffPanel.Children.Add(new TextBlock { Text = "Aucun bonus actif.", Style = (Style)FindResource("MutedText") });
            return;
        }
        foreach (var r in rows)
        {
            BuffPanel.Children.Add(BuffChip(r.Name, r.Left, r.Effects, r.Good));
        }
    }

    // Une pastille : rond coloré avec le logo, puis nom, temps restant et effets.
    private Border BuffChip(string name, string left, string effects, bool good)
    {
        var (logo, color) = BuffLogo(name, good);
        var badge = new Border
        {
            Width = 34,
            Height = 34,
            CornerRadius = new CornerRadius(17),
            VerticalAlignment = VerticalAlignment.Center,
            Background = (Brush)new BrushConverter().ConvertFromString(color)!,
            Child = new TextBlock { Text = logo, FontSize = 17, FontFamily = new FontFamily("Segoe UI Emoji"), Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        };
        var text = new StackPanel { Margin = new Thickness(8, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.SemiBold });
        var detail = new TextBlock { FontSize = 12 };
        detail.Inlines.Add(new System.Windows.Documents.Run(left) { Foreground = (Brush)FindResource("Gold") });
        if (effects.Length > 0)
        {
            detail.Inlines.Add(new System.Windows.Documents.Run("  " + effects) { Foreground = (Brush)FindResource("Muted") });
        }
        text.Children.Add(detail);
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(badge);
        row.Children.Add(text);
        return new Border
        {
            Background = (Brush)FindResource("Track"),
            BorderBrush = (Brush)FindResource("CardBorder"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 4, 8, 4),
            Margin = new Thickness(0, 0, 8, 6),
            Child = row,
        };
    }

    private static string FormatDuration(int seconds) => seconds switch
    {
        < 90 => $"{seconds} s",
        < 5400 => $"{seconds / 60} min",
        _ => $"{seconds / 3600} h {seconds % 3600 / 60:00}",
    };
}
