namespace BitcraftBot.Perception.Capture;

// Rectangle à l'écran, en pixels physiques.
public readonly record struct ScreenRect(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public override string ToString() => $"{Width}×{Height} en ({X}, {Y})";
}

// Une image capturée : pixels au format BGRA 32 bits, ligne du haut en premier.
public sealed class Frame
{
    public required byte[] Pixels { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required DateTime CapturedAt { get; init; }

    // Zone de l'écran d'où vient l'image (sert à convertir une position image -> écran).
    public required ScreenRect Source { get; init; }

    public int Stride => Width * 4;
}
