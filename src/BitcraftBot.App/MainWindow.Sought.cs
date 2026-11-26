using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// v0.4.23 (demande de Julien) : « ressource recherchée » à la place du « meilleur arbre ». Julien tape le nom d'une
// ressource, la choisit dans la liste (nom et tier), et le choix est gardé. La carte entoure d'or la plus proche de lui,
// parmi celles lues en direct et celles gardées en mémoire.
public partial class MainWindow
{
    private const int SoughtListMax = 40;

    // Nom exact de la ressource recherchée (données du jeu), null = aucune.
    private string? _sought;
    private bool _settingSoughtText;

    private void InitSought()
    {
        _sought = string.IsNullOrWhiteSpace(_settings.MapSoughtResource) ? null : _settings.MapSoughtResource;
        ShowSoughtText();
    }

    // Toutes les ressources du jeu, « nom · Tn », triées par nom (une seule ligne par nom).
    private IReadOnlyList<SoughtChoice> SoughtChoices() =>
        (_gameData.Current?.Resources ?? [])
            .Where(r => r.Name.Length > 0)
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SoughtChoice(g.First().Name, g.Min(r => r.Tier)))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private void ShowSoughtText()
    {
        _settingSoughtText = true;
        SoughtBox.Text = _sought is null ? "" : SoughtChoices().FirstOrDefault(c => c.Name.Equals(_sought, StringComparison.OrdinalIgnoreCase))?.ToString() ?? _sought;
        _settingSoughtText = false;
    }

    private void SoughtBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_settingSoughtText)
        {
            return;
        }
        FillSoughtList();
    }

    private void SoughtBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => SoughtBox.SelectAll();

    // Liste des ressources dont le nom contient le texte tapé (chaque mot tapé doit s'y trouver).
    private void FillSoughtList()
    {
        var words = SoughtBox.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w != "·" && !(w.Length >= 2 && w[0] is 'T' or 't' && w[1..].All(char.IsDigit)))
            .ToList();
        if (words.Count == 0)
        {
            SoughtPopup.IsOpen = false;
            return;
        }
        var matches = SoughtChoices()
            .Where(c => words.All(w => c.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .Take(SoughtListMax)
            .ToList();
        SoughtList.ItemsSource = matches;
        SoughtList.SelectedIndex = matches.Count > 0 ? 0 : -1;
        SoughtPopup.IsOpen = matches.Count > 0;
    }

    private void SoughtBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!SoughtPopup.IsOpen)
        {
            return;
        }
        switch (e.Key)
        {
            case Key.Down:
                SoughtList.SelectedIndex = Math.Min(SoughtList.SelectedIndex + 1, SoughtList.Items.Count - 1);
                SoughtList.ScrollIntoView(SoughtList.SelectedItem);
                e.Handled = true;
                break;
            case Key.Up:
                SoughtList.SelectedIndex = Math.Max(SoughtList.SelectedIndex - 1, 0);
                SoughtList.ScrollIntoView(SoughtList.SelectedItem);
                e.Handled = true;
                break;
            case Key.Enter:
                if (SoughtList.SelectedItem is SoughtChoice choice)
                {
                    LockSought(choice);
                }
                e.Handled = true;
                break;
            case Key.Escape:
                SoughtPopup.IsOpen = false;
                ShowSoughtText();
                e.Handled = true;
                break;
        }
    }

    private void SoughtList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is SoughtChoice choice)
        {
            LockSought(choice);
        }
    }

    private void SoughtClear_Click(object sender, RoutedEventArgs e) => SetSought(null);

    private void LockSought(SoughtChoice choice)
    {
        SoughtPopup.IsOpen = false;
        SetSought(choice.Name);
    }

    private void SetSought(string? name)
    {
        _sought = name;
        _settings.MapSoughtResource = name;
        _settings.Save(LocalSettings.DefaultPath);
        _loggedBest = null;
        ShowSoughtText();
        _log.Info("Carte", name is null ? "Ressource recherchée : aucune." : $"Ressource recherchée : {name} (la plus proche est entourée d'or sur la carte).");
    }

    // La ressource recherchée la plus proche du personnage (en direct ou gardée en mémoire), pas épuisée.
    private NearbyResource? NearestSought(PlayerSnapshot s, IReadOnlyList<NearbyResource> remembered)
    {
        if (_sought is not { } name || s.Position is not { } me)
        {
            return null;
        }
        return s.Resources.Concat(remembered)
            .Where(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && r.Health is not <= 0)
            .MinBy(r => r.Position.ScreenDistanceTo(me));
    }
}

// Une ressource du jeu dans la liste de recherche.
public sealed record SoughtChoice(string Name, int Tier)
{
    public override string ToString() => Tier > 0 ? $"{Name} · T{Tier}" : Name;
}
