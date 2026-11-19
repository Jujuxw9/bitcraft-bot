using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace BitcraftBot.App;

// Petit cadre posé sur l'écran du jeu (en haut à gauche) : état du bot et rappel des touches.
// Ce n'est pas une modification du jeu : c'est une fenêtre à part, au-dessus, qui :
// - laisse passer les clics et ne prend jamais le clavier (le jeu reste la fenêtre active) ;
// - est exclue des captures d'écran (le bot ne la lit pas, elle n'apparaît pas dans les images).
public sealed class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

    private readonly TextBlock _server = Line(14, FontWeights.SemiBold);
    private readonly TextBlock _screen = Line(13, FontWeights.Normal);
    private readonly TextBlock _keys = Line(12, FontWeights.Normal);

    public OverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        IsHitTestVisible = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        Title = "BitCraft Bot — overlay";
        _keys.Text = "Num 0 : capture   ·   Num 5 : arrêt (jeu actif)";
        _keys.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(170, 20, 21, 23)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(200, 60, 63, 68)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 5, 8, 5),
            Child = new StackPanel { Children = { _server, _screen, _keys } },
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        SetWindowLong(handle, GWL_EXSTYLE, GetWindowLong(handle, GWL_EXSTYLE) | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        // Exclue des captures (Windows 10 2004 et plus récent). Sans effet ailleurs : sans gravité.
        SetWindowDisplayAffinity(handle, WDA_EXCLUDEFROMCAPTURE);
    }

    // Texte et couleur de chaque ligne.
    public void Show(string server, Brush serverColor, string screen, Brush screenColor)
    {
        _server.Text = server;
        _server.Foreground = serverColor;
        _screen.Text = screen;
        _screen.Foreground = screenColor;
    }

    // Place le cadre en haut à gauche de la zone du jeu (coordonnées écran en pixels).
    public void PlaceAt(int screenX, int screenY)
    {
        var source = PresentationSource.FromVisual(this);
        var toDip = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var p = toDip.Transform(new Point(screenX + 14, screenY + 14));
        if (Math.Abs(Left - p.X) > 0.5) Left = p.X;
        if (Math.Abs(Top - p.Y) > 0.5) Top = p.Y;
    }

    private static TextBlock Line(double size, FontWeight weight) =>
        new() { FontSize = size, FontWeight = weight, FontFamily = new FontFamily("Segoe UI"), Foreground = Brushes.White };

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int index, int value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint affinity);
}
