using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BitcraftBot.Input.Mouse;

public enum MouseButton
{
    Gauche,
    Droit,
    Milieu,
}

// Écoute la souris de tout Windows (R1, v0.4.0, accord de Julien : D-049). Lecture seule : les clics arrivent
// normalement au jeu, rien n'est bloqué ni modifié.
// Tourne sur son propre fil, avec sa propre boucle de messages : la souris de Windows ne dépend jamais
// de l'interface du bot (si l'interface est occupée, la souris de Julien ne ralentit pas).
// Les déplacements ne sont pas transmis (trop nombreux) : seulement les boutons enfoncés et la molette.
public sealed class GlobalMouseHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_QUIT = 0x0012;
    private const uint LLMHF_INJECTED = 0x01;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    // Gardé en champ pour que le ramasse-miettes ne le détruise pas.
    private readonly LowLevelMouseProc _callback;
    private readonly Thread _thread;
    private IntPtr _hook;
    private uint _threadId;

    // Bouton enfoncé : bouton, x, y (pixels physiques de l'écran), clic simulé par un programme (le bot lui-même).
    // Appelé sur le fil de la souris : le destinataire doit repasser sur son propre fil, sans travail long ici.
    public event Action<MouseButton, int, int, bool>? ButtonDown;

    // Molette tournée (zoom de la caméra dans le jeu) : clic simulé ou non.
    public event Action<bool>? Wheel;

    public GlobalMouseHook()
    {
        _callback = OnHook;
        string? error = null;
        using var ready = new ManualResetEventSlim();
        _thread = new Thread(() =>
        {
            _threadId = GetCurrentThreadId();
            using (var module = Process.GetCurrentProcess().MainModule)
            {
                _hook = SetWindowsHookEx(WH_MOUSE_LL, _callback, GetModuleHandle(module?.ModuleName), 0);
            }
            if (_hook == IntPtr.Zero)
            {
                error = $"Impossible d'écouter la souris (code {Marshal.GetLastWin32Error()}).";
                ready.Set();
                return;
            }
            ready.Set();
            // Boucle de messages : indispensable pour que Windows appelle l'écoute.
            while (GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
            {
            }
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        })
        {
            IsBackground = true,
            Name = "Écoute de la souris",
            Priority = ThreadPriority.AboveNormal,
        };
        _thread.Start();
        ready.Wait(TimeSpan.FromSeconds(5));
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }
    }

    private IntPtr OnHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var message = (int)wParam;
            if (message is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_MOUSEWHEEL)
            {
                try
                {
                    var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    var injected = (data.flags & LLMHF_INJECTED) != 0;
                    switch (message)
                    {
                        case WM_LBUTTONDOWN: ButtonDown?.Invoke(MouseButton.Gauche, data.pt.X, data.pt.Y, injected); break;
                        case WM_RBUTTONDOWN: ButtonDown?.Invoke(MouseButton.Droit, data.pt.X, data.pt.Y, injected); break;
                        case WM_MBUTTONDOWN: ButtonDown?.Invoke(MouseButton.Milieu, data.pt.X, data.pt.Y, injected); break;
                        default: Wheel?.Invoke(injected); break;
                    }
                }
                catch
                {
                    // Une erreur ici ne doit jamais bloquer la souris de Windows.
                }
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_threadId != 0)
        {
            PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(TimeSpan.FromSeconds(2));
            _threadId = 0;
        }
    }
}
