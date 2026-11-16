using System.Runtime.InteropServices;

namespace BitcraftBot.Input.Native;

// Envoie de vrais mouvements de souris et appuis de touches à Windows (SendInput).
// Les touches sont envoyées par position physique (scancode) : indépendant de AZERTY / QWERTY.
public sealed class InputSender
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public (int X, int Y) CursorPosition()
    {
        GetCursorPos(out var p);
        return (p.X, p.Y);
    }

    public bool IsForeground(IntPtr window) => window != IntPtr.Zero && GetForegroundWindow() == window;

    public bool BringToFront(IntPtr window) => SetForegroundWindow(window);

    // Déplace le curseur à une position écran (coordonnées absolues sur l'ensemble des écrans).
    public void MoveTo(int x, int y)
    {
        var left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        var width = Math.Max(2, GetSystemMetrics(SM_CXVIRTUALSCREEN));
        var height = Math.Max(2, GetSystemMetrics(SM_CYVIRTUALSCREEN));
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = (int)Math.Round((x - left) * 65535.0 / (width - 1)),
                    dy = (int)Math.Round((y - top) * 65535.0 / (height - 1)),
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                },
            },
        };
        Send(input);
    }

    public void LeftDown() => Send(MouseButton(MOUSEEVENTF_LEFTDOWN));

    public void LeftUp() => Send(MouseButton(MOUSEEVENTF_LEFTUP));

    public void KeyDown(ushort scanCode) => Send(Key(scanCode, KEYEVENTF_SCANCODE));

    public void KeyUp(ushort scanCode) => Send(Key(scanCode, KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP));

    private static INPUT MouseButton(uint flag) =>
        new() { type = INPUT_MOUSE, u = new InputUnion { mi = new MOUSEINPUT { dwFlags = flag } } };

    private static INPUT Key(ushort scanCode, uint flags) =>
        new() { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wScan = scanCode, dwFlags = flags } } };

    private static void Send(INPUT input)
    {
        if (SendInput(1, [input], Marshal.SizeOf<INPUT>()) != 1)
        {
            throw new InvalidOperationException($"Envoi refusé par Windows (code {Marshal.GetLastWin32Error()}).");
        }
    }
}
