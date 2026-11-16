using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BitcraftBot.Core.Monitoring;

// Une mesure des ressources : le bot, le jeu et le PC.
public sealed record ResourceSample
{
    public required DateTime At { get; init; }

    // Bot
    public required double BotCpuPercent { get; init; }
    public required double BotRamMb { get; init; }
    public required double BotManagedMb { get; init; }
    public required int BotThreads { get; init; }
    public required int BotHandles { get; init; }

    // Jeu (null si le jeu n'est pas lancé)
    public double? GameCpuPercent { get; init; }
    public double? GameRamMb { get; init; }

    // PC
    public required double SystemCpuPercent { get; init; }
    public required double SystemRamTotalMb { get; init; }
    public required double SystemRamAvailableMb { get; init; }

    public double SystemRamAvailablePercent => SystemRamTotalMb > 0 ? 100 * SystemRamAvailableMb / SystemRamTotalMb : 100;
}

// Mesure la consommation du processeur (CPU) et de la mémoire (RAM).
// Lecture seule : ne modifie rien, ni dans le bot, ni dans le jeu, ni dans Windows.
public sealed class ResourceMonitor
{
    public const string GameProcessName = "BitCraft";

    private readonly Process _self = Process.GetCurrentProcess();
    private TimeSpan _lastBotCpu;
    private DateTime _lastAt;
    private TimeSpan? _lastGameCpu;
    private int? _lastGameId;
    private ulong _lastIdle, _lastKernel, _lastUser;

    public ResourceMonitor()
    {
        _self.Refresh();
        _lastBotCpu = _self.TotalProcessorTime;
        _lastAt = DateTime.Now;
        ReadSystemTimes(out _lastIdle, out _lastKernel, out _lastUser);
    }

    public ResourceSample Sample()
    {
        var now = DateTime.Now;
        var elapsed = Math.Max(0.001, (now - _lastAt).TotalSeconds);
        var cores = Environment.ProcessorCount;

        _self.Refresh();
        var botCpu = _self.TotalProcessorTime;
        var botCpuPercent = Percent((botCpu - _lastBotCpu).TotalSeconds, elapsed * cores);
        _lastBotCpu = botCpu;

        double? gameCpu = null;
        double? gameRam = null;
        var game = FindGame();
        if (game is not null)
        {
            using (game)
            {
                try
                {
                    var total = game.TotalProcessorTime;
                    if (_lastGameCpu is { } previous && _lastGameId == game.Id)
                    {
                        gameCpu = Percent((total - previous).TotalSeconds, elapsed * cores);
                    }
                    _lastGameCpu = total;
                    _lastGameId = game.Id;
                    gameRam = game.WorkingSet64 / 1048576.0;
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    // Accès refusé ou jeu fermé entre-temps : on ignore pour cette mesure.
                }
            }
        }
        else
        {
            _lastGameCpu = null;
            _lastGameId = null;
        }

        var systemCpu = 0.0;
        if (ReadSystemTimes(out var idle, out var kernel, out var user))
        {
            var busy = (kernel - _lastKernel) + (user - _lastUser) - (idle - _lastIdle);
            var all = (kernel - _lastKernel) + (user - _lastUser);
            systemCpu = all > 0 ? Math.Clamp(100.0 * busy / all, 0, 100) : 0;
            (_lastIdle, _lastKernel, _lastUser) = (idle, kernel, user);
        }

        var memory = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        GlobalMemoryStatusEx(ref memory);

        _lastAt = now;
        return new ResourceSample
        {
            At = now,
            BotCpuPercent = botCpuPercent,
            BotRamMb = _self.WorkingSet64 / 1048576.0,
            BotManagedMb = GC.GetTotalMemory(false) / 1048576.0,
            BotThreads = _self.Threads.Count,
            BotHandles = _self.HandleCount,
            GameCpuPercent = gameCpu,
            GameRamMb = gameRam,
            SystemCpuPercent = systemCpu,
            SystemRamTotalMb = memory.TotalPhys / 1048576.0,
            SystemRamAvailableMb = memory.AvailPhys / 1048576.0,
        };
    }

    private static double Percent(double cpuSeconds, double availableSeconds) =>
        Math.Clamp(100 * cpuSeconds / availableSeconds, 0, 100);

    private static Process? FindGame()
    {
        var processes = Process.GetProcessesByName(GameProcessName);
        // On garde le premier et on libère les autres.
        for (var i = 1; i < processes.Length; i++)
        {
            processes[i].Dispose();
        }
        return processes.Length > 0 ? processes[0] : null;
    }

    private static bool ReadSystemTimes(out ulong idle, out ulong kernel, out ulong user)
    {
        var ok = GetSystemTimes(out var i, out var k, out var u);
        idle = i;
        kernel = k;
        user = u;
        return ok;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);
}
