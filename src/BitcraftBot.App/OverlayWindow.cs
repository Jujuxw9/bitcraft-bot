using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace BitcraftBot.App;

// Petite pastille posée sur l'écran du jeu, en bas au centre, juste au-dessus de la barre d'action (v0.4.13, demande de Julien) :
// trois voyants (serveur, écran, repas auto), sans texte long. Plus haut elle cachait des informations du jeu.
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

    private readonly TextBlock _server = Line(13, FontWeights.SemiBold);
    private readonly TextBlock _screen = Line(13, FontWeights.SemiBold);
    private readonly TextBlock _meal = Line(13, FontWeights.SemiBold);

    // Point d'accroche (pixels écran) : milieu du bord bas de la pastille.
    private (int X, int Y)? _anchor;

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
        _meal.Margin = new Thickness(14, 0, 0, 0);
        _screen.Margin = new Thickness(14, 0, 0, 0);
        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(190, 11, 24, 40)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(200, 210, 182, 93)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(13),
            Padding = new Thickness(14, 4, 14, 5),
            Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { _server, _screen, _meal } },
        };
        SizeChanged += (_, _) => Replace();
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

    // Ligne du repas automatique (cachée s'il est désactivé).
    public void ShowMeal(string? text, Brush color)
    {
        _meal.Visibility = text is null ? Visibility.Collapsed : Visibility.Visible;
        _meal.Text = text ?? "";
        _meal.Foreground = color;
    }

    // Place la pastille en bas au centre de la zone du jeu, juste au-dessus de la barre d'action et du panneau de cible
    // (environ 76 % de la hauteur), pour ne cacher ni la vie, ni l'endurance, ni les icônes de la barre (zone en pixels écran).
    public void PlaceBottomCenter(int areaX, int areaY, int areaWidth, int areaHeight)
    {
        _anchor = (areaX + areaWidth / 2, areaY + (int)(areaHeight * 0.76));
        Replace();
    }

    private void Replace()
    {
        if (_anchor is not { } a)
        {
            return;
        }
        var source = PresentationSource.FromVisual(this);
        var toDip = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var p = toDip.Transform(new Point(a.X, a.Y));
        var left = p.X - ActualWidth / 2;
        var top = p.Y - ActualHeight;
        if (Math.Abs(Left - left) > 0.5) Left = left;
        if (Math.Abs(Top - top) > 0.5) Top = top;
    }

    private static TextBlock Line(double size, FontWeight weight) =>
        new() { FontSize = size, FontWeight = weight, FontFamily = new FontFamily("Segoe UI"), Foreground = new SolidColorBrush(Color.FromRgb(233, 223, 196)) };

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int index, int value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint affinity);
}
