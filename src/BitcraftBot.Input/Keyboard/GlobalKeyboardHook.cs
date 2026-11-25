using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BitcraftBot.Input.Keyboard;

// Écoute le clavier de tout Windows, même quand le jeu a le focus.
// Lecture seule : les touches continuent d'arriver normalement au jeu.
// Doit être créé sur un fil qui traite les messages Windows (le fil de l'interface).
public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;
    private const uint LLKHF_EXTENDED = 0x01;
    private const uint LLKHF_INJECTED = 0x10;

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    // Gardé en champ pour que le ramasse-miettes ne le détruise pas.
    private readonly LowLevelKeyboardProc _callback;
    private IntPtr _hook;

    // v0.4.14 : seul le premier appui d'une touche tenue est signalé (voir KeyRepeatFilter).
    private readonly KeyRepeatFilter _repeat = new();

    // Touche enfoncée : scancode, touche étendue, touche simulée par un programme.
    public event Action<int, bool, bool>? KeyDown;

    public GlobalKeyboardHook()
    {
        _callback = OnHook;
        using var module = Process.GetCurrentProcess().MainModule;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _callback, GetModuleHandle(module?.ModuleName), 0);
        if (_hook == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Impossible d'écouter le clavier (code {Marshal.GetLastWin32Error()}).");
        }
    }

    private IntPtr OnHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
        {
            var up = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            _repeat.Release(up.scanCode, (up.flags & LLKHF_EXTENDED) != 0);
        }
        else if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var extended = (data.flags & LLKHF_EXTENDED) != 0;
            if (!_repeat.IsFirstPress(data.scanCode, extended, Environment.TickCount64))
            {
                return CallNextHookEx(_hook, nCode, wParam, lParam);
            }
            var injected = (data.flags & LLKHF_INJECTED) != 0;
            try
            {
                KeyDown?.Invoke((int)data.scanCode, extended, injected);
            }
            catch
            {
                // Une erreur ici ne doit jamais bloquer le clavier de Windows.
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
