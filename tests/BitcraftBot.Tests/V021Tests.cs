using BitcraftBot.Core.Logging;
using BitcraftBot.Core.Monitoring;

namespace BitcraftBot.Tests;

public class ResourceGuardTests
{
    private static ResourceSample Sample(double botRam = 200, double botCpu = 2, double availMb = 16000, double totalMb = 32000) => new()
    {
        At = DateTime.Now,
        BotCpuPercent = botCpu,
        BotRamMb = botRam,
        BotManagedMb = 50,
        BotThreads = 20,
        BotHandles = 500,
        SystemCpuPercent = 10,
        SystemRamTotalMb = totalMb,
        SystemRamAvailableMb = availMb,
    };

    [Fact]
    public void NormalSample_NoEvent() =>
        Assert.Empty(new ResourceGuard(new ResourceLimits()).Evaluate(Sample()));

    [Fact]
    public void BotRam_Warning_Then_Critical_Then_Back()
    {
        var guard = new ResourceGuard(new ResourceLimits { BotRamWarningMb = 400, BotRamCriticalMb = 800, BotRamSustainedSamples = 1 });

        var warning = guard.Evaluate(Sample(botRam: 450));
        Assert.Equal(ResourceLevel.Alerte, Assert.Single(warning).Level);

        // Même état : pas de nouvel événement (le journal ne se remplit pas).
        Assert.Empty(guard.Evaluate(Sample(botRam: 460)));

        var critical = guard.Evaluate(Sample(botRam: 900));
        Assert.Equal(ResourceLevel.Critique, Assert.Single(critical).Level);

        var back = guard.Evaluate(Sample(botRam: 200));
        Assert.Equal(ResourceLevel.Normal, Assert.Single(back).Level);
    }

    [Fact]
    public void BotCpu_AlertOnlyWhenSustained()
    {
        var guard = new ResourceGuard(new ResourceLimits { BotCpuWarningPercent = 25, BotCpuSustainedSamples = 3 });
        Assert.Empty(guard.Evaluate(Sample(botCpu: 50)));
        Assert.Empty(guard.Evaluate(Sample(botCpu: 50)));
        Assert.Equal(ResourceLevel.Alerte, Assert.Single(guard.Evaluate(Sample(botCpu: 50))).Level);
        Assert.Equal(ResourceLevel.Normal, Assert.Single(guard.Evaluate(Sample(botCpu: 5))).Level);
    }

    [Fact]
    public void BotCpu_ShortSpike_NoAlert()
    {
        var guard = new ResourceGuard(new ResourceLimits { BotCpuWarningPercent = 25, BotCpuSustainedSamples = 3 });
        Assert.Empty(guard.Evaluate(Sample(botCpu: 80)));
        Assert.Empty(guard.Evaluate(Sample(botCpu: 5)));
        Assert.Empty(guard.Evaluate(Sample(botCpu: 80)));
    }

    [Fact]
    public void SystemRam_LowAvailable_Alert()
    {
        var guard = new ResourceGuard(new ResourceLimits { SystemRamAvailableWarningPercent = 10 });
        var events = guard.Evaluate(Sample(availMb: 2000, totalMb: 32000)); // 6 % libre
        Assert.Equal(ResourceLevel.Alerte, Assert.Single(events).Level);
    }

    [Fact]
    public void Monitor_RealSample_IsPlausible()
    {
        var monitor = new ResourceMonitor();
        Thread.Sleep(200);
        var s = monitor.Sample();
        Assert.InRange(s.BotRamMb, 1, 4000);
        Assert.InRange(s.BotCpuPercent, 0, 100);
        Assert.InRange(s.SystemCpuPercent, 0, 100);
        Assert.True(s.SystemRamTotalMb > 1000);
        Assert.InRange(s.SystemRamAvailableMb, 1, s.SystemRamTotalMb);
    }
}

public class LogLevelTests
{
    [Fact]
    public void DetailLevel_IsWrittenAsDETAIL() =>
        Assert.Equal("2026-11-16 14:30:05.000 [DÉTAIL] [Système] x",
            new LogEntry(new DateTime(2026, 11, 16, 14, 30, 5), LogLevel.Detail, "Système", "x").ToString());
}
