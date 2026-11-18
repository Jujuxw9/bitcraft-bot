using System.ComponentModel;
using System.Runtime.CompilerServices;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Une ligne du tableau des ressources (onglet Serveur). Mise à jour sur place pour garder
// la sélection et le défilement pendant que la santé ou la distance changent.
public sealed class ResourceRow : INotifyPropertyChanged
{
    private string _health = "";
    private string _distance = "";
    private string _direction = "";
    private string _mark = "";

    public ResourceRow(NearbyResource r)
    {
        EntityId = r.EntityId;
        Name = r.Name;
        Age = r.Age;
        Tier = r.Tier?.ToString() ?? "?";
        Kind = r.Family;
        Position = r.Position.ScreenText;
        Refresh(r, "");
    }

    public ulong EntityId { get; }
    public string Name { get; }
    public string Age { get; }
    public string Tier { get; }
    public string Kind { get; }
    public string Position { get; }

    public string Health { get => _health; private set => Set(ref _health, value); }
    public string Distance { get => _distance; private set => Set(ref _distance, value); }
    public string Direction { get => _direction; private set => Set(ref _direction, value); }

    // Repère : « ★ meilleur », « ◎ cible écran ».
    public string Mark { get => _mark; private set => Set(ref _mark, value); }

    public void Refresh(NearbyResource r, string mark)
    {
        Health = FormatHealth(r.Health, r.MaxHealth);
        Distance = r.ScreenDistance.ToString("0.0");
        Direction = r.Direction;
        Mark = mark;
    }

    public static string FormatHealth(int? health, int? max) => (health, max) switch
    {
        ({ } h, { } m) when m > 0 => $"{h} / {m} ({100.0 * h / m:0} %)",
        ({ } h, _) => $"{h}",
        _ => "?",
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set(ref string field, string value, [CallerMemberName] string? name = null)
    {
        if (field == value)
        {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
