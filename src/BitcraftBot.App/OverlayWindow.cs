using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace BitcraftBot.App;

// Petite pastille posée sur l'écran du jeu, en bas, centrée entre les icônes de gauche (chat, amis…) et la barre d'action
// (v0.4.15, demande de Julien avec capture) : trois voyants (serveur, écran, repas auto).
// Plus de touche pour la masquer : seulement l'interrupteur « Overlay sur le jeu » (Paramètres, D-064).
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
    // v0.4.23 (Julien) : deuxième ligne, le système (CPU et RAM du bot, du jeu).
    private readonly TextBlock _system = Line(11.5, FontWeights.Normal);

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
            CornerRadius = new CornerRadius(13),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 4, 14, 5),
            Child = new StackPanel
            {
                Children =
                {
                    new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Children = { _server, _screen, _meal } },
                    _system,
                },
            },
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

    // Ligne du système (cachée si aucune mesure).
    public void ShowSystem(string? text)
    {
        _system.Visibility = text is null ? Visibility.Collapsed : Visibility.Visible;
        _system.Text = text ?? "";
    }

    // Place la pastille dans l'espace libre entre les icônes de gauche et la barre d'action (zone en pixels écran).
    // Relevé sur une capture 1920 × 1080 : icônes de gauche jusqu'à x ≈ 265, barre d'action à partir de x ≈ 612,
    // icônes centrées vers y ≈ 1045. Le bas de la pastille est donc à 98,5 % de la hauteur, son milieu à 23 % de la largeur.
    public void PlaceBottomCenter(int areaX, int areaY, int areaWidth, int areaHeight)
    {
        _anchor = (areaX + (int)(areaWidth * 0.23), areaY + (int)(areaHeight * 0.985));
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
        new() { FontSize = size, FontWeight = weight, FontFamily = new FontFamily("Segoe UI"), Foreground = new SolidColorBrush(Color.FromRgb(233, 223, 196)), HorizontalAlignment = HorizontalAlignment.Center };

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int index, int value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint affinity);
}
