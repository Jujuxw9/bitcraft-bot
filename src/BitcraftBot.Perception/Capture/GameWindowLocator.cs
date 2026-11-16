using System.Diagnostics;
using System.Text;
using BitcraftBot.Perception.Native;

namespace BitcraftBot.Perception.Capture;

// Fenêtre de jeu trouvée.
public sealed record GameWindow(IntPtr Handle, string Title, string ProcessName, ScreenRect ClientArea);

// Cherche la fenêtre du jeu parmi les fenêtres ouvertes.
public sealed class GameWindowLocator
{
    // Mot recherché dans le titre de la fenêtre (À vérifier : titre exact de la fenêtre du jeu).
    public string TitleKeyword { get; set; } = "BitCraft";

    public GameWindow? Find()
    {
        var ownProcessId = (uint)Environment.ProcessId;
        GameWindow? best = null;

        Win32.EnumWindows((handle, _) =>
        {
            if (!Win32.IsWindowVisible(handle) || Win32.IsIconic(handle))
            {
                return true;
            }
            Win32.GetWindowThreadProcessId(handle, out var processId);
            if (processId == ownProcessId)
            {
                // On ignore la fenêtre du bot, dont le titre contient aussi « BitCraft ».
                return true;
            }
            var title = GetTitle(handle);
            if (!IsCandidateTitle(title, TitleKeyword))
            {
                return true;
            }
            var area = GetClientArea(handle);
            if (area.IsEmpty)
            {
                return true;
            }
            var window = new GameWindow(handle, title, GetProcessName(processId), area);
            // S'il y a plusieurs candidates, on garde la plus grande.
            if (best is null || area.Width * area.Height > best.ClientArea.Width * best.ClientArea.Height)
            {
                best = window;
            }
            return true;
        }, IntPtr.Zero);

        return best;
    }

    // Règle de reconnaissance du titre, séparée pour être testée.
    // Le titre doit commencer par le mot-clé. On écarte la fenêtre du bot, les navigateurs
    // et les éditeurs, dont le titre a la forme « page - programme » (ex. « BitCraft Wiki - Google Chrome »).
    public static bool IsCandidateTitle(string title, string keyword) =>
        !string.IsNullOrWhiteSpace(title)
        && title.TrimStart().StartsWith(keyword, StringComparison.OrdinalIgnoreCase)
        && !title.Contains("bot", StringComparison.OrdinalIgnoreCase)
        && !title.Contains(" - ", StringComparison.Ordinal)
        && !title.Contains(" — ", StringComparison.Ordinal);

    // Zone utile de la fenêtre (sans bordure ni barre de titre), en coordonnées écran.
    public static ScreenRect GetClientArea(IntPtr handle)
    {
        if (!Win32.IsWindow(handle) || !Win32.GetClientRect(handle, out var rect))
        {
            return default;
        }
        var origin = new Win32.POINT { X = 0, Y = 0 };
        Win32.ClientToScreen(handle, ref origin);
        return new ScreenRect(origin.X, origin.Y, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public static ScreenRect PrimaryScreen() =>
        new(0, 0, Win32.GetSystemMetrics(Win32.SM_CXSCREEN), Win32.GetSystemMetrics(Win32.SM_CYSCREEN));

    private static string GetTitle(IntPtr handle)
    {
        var length = Win32.GetWindowTextLength(handle);
        if (length <= 0)
        {
            return string.Empty;
        }
        var builder = new StringBuilder(length + 1);
        Win32.GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetProcessName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return "?";
        }
        catch (InvalidOperationException)
        {
            return "?";
        }
    }
}
