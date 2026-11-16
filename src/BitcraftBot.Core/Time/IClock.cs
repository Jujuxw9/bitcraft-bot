namespace BitcraftBot.Core.Time;

// Horloge abstraite : permet de tester les durées sans attendre réellement.
public interface IClock
{
    DateTime Now { get; }
}

// Horloge réelle du PC.
public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    public DateTime Now => DateTime.Now;
}
