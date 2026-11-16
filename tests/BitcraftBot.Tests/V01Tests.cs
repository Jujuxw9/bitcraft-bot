using BitcraftBot.Core.Logging;
using BitcraftBot.Core.Safety;
using BitcraftBot.Core.Time;
using BitcraftBot.Input.Keyboard;
using BitcraftBot.Perception.Capture;

namespace BitcraftBot.Tests;

// Horloge réglable à la main pour les tests.
internal sealed class FakeClock(DateTime start) : IClock
{
    public DateTime Now { get; set; } = start;
}

public class SafetyMonitorTests
{
    private static readonly DateTime T0 = new(2026, 11, 16, 14, 0, 0);

    [Fact]
    public void MaxDuration_StopsSession_WhenReached()
    {
        var clock = new FakeClock(T0);
        var monitor = new SafetyMonitor(new SafetyOptions { MaxDuration = TimeSpan.FromMinutes(15) }, clock);
        StopReason? reason = null;
        monitor.StopRequested += r => reason = r;

        monitor.Start();
        clock.Now = T0.AddMinutes(14).AddSeconds(59);
        monitor.Tick();
        Assert.True(monitor.IsRunning);

        clock.Now = T0.AddMinutes(15);
        monitor.Tick();
        Assert.False(monitor.IsRunning);
        Assert.Equal(StopReason.DureeMaximale, reason);
        Assert.Equal(TimeSpan.FromMinutes(15), monitor.LastSessionDuration);
    }

    [Fact]
    public void MaxDuration_Disabled_NeverStops()
    {
        var clock = new FakeClock(T0);
        var monitor = new SafetyMonitor(new SafetyOptions { MaxDurationEnabled = false }, clock);
        monitor.Start();
        clock.Now = T0.AddHours(5);
        monitor.Tick();
        Assert.True(monitor.IsRunning);
        Assert.Null(monitor.Remaining);
    }

    [Fact]
    public void EmergencyKey_StopsSession_WhenEnabled()
    {
        var monitor = new SafetyMonitor(new SafetyOptions(), new FakeClock(T0));
        StopReason? reason = null;
        monitor.StopRequested += r => reason = r;
        monitor.Start();

        monitor.OnEmergencyKey();

        Assert.False(monitor.IsRunning);
        Assert.Equal(StopReason.ToucheUrgence, reason);
    }

    [Fact]
    public void EmergencyKey_Ignored_WhenDisabled()
    {
        var monitor = new SafetyMonitor(new SafetyOptions { EmergencyKeyEnabled = false }, new FakeClock(T0));
        monitor.Start();
        monitor.OnEmergencyKey();
        Assert.True(monitor.IsRunning);
    }

    [Fact]
    public void StopRequested_FiresOnlyOnce()
    {
        var monitor = new SafetyMonitor(new SafetyOptions(), new FakeClock(T0));
        var count = 0;
        monitor.StopRequested += _ => count++;
        monitor.Start();
        monitor.OnEmergencyKey();
        monitor.RequestStop(StopReason.Manuel);
        monitor.Tick();
        Assert.Equal(1, count);
    }
}

public class EmergencyKeyTests
{
    [Fact]
    public void Numpad5_IsEmergencyStop() => Assert.True(EmergencyKey.IsEmergencyStop(0x4C, isExtended: false));

    [Theory]
    [InlineData(0x06, false)] // touche 5 de la rangée du haut
    [InlineData(0x4C, true)]  // même scancode mais touche étendue
    [InlineData(0x4B, false)] // Num 4
    [InlineData(0x2D, false)] // touche X (cache les arbres)
    public void OtherKeys_AreNotEmergencyStop(int scanCode, bool extended) =>
        Assert.False(EmergencyKey.IsEmergencyStop(scanCode, extended));
}

public class SessionLogTests
{
    [Fact]
    public void FilePath_IsSortedByVersionThenDay()
    {
        var path = SessionLog.BuildFilePath(@"C:\bot", "0.1.0+abc123", new DateTime(2026, 11, 16, 14, 30, 5));
        Assert.Equal(@"C:\bot\journaux\v0.1.0\2026-11-16\session_2026-11-16_14-30-05.log", path);
    }

    [Fact]
    public void Write_AppendsFormattedLine()
    {
        var root = Path.Combine(Path.GetTempPath(), "bitcraftbot-tests", Guid.NewGuid().ToString("N"));
        var clock = new FakeClock(new DateTime(2026, 11, 16, 14, 30, 5, 123));
        try
        {
            string file;
            using (var log = new SessionLog(root, "0.1.0", clock))
            {
                log.Alerte("Sécurité", "test d'arrêt");
                file = log.FilePath;
            }
            var lines = File.ReadAllLines(file);
            Assert.Single(lines);
            Assert.Equal("2026-11-16 14:30:05.123 [ALERTE] [Sécurité] test d'arrêt", lines[0]);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

public class GameWindowLocatorTests
{
    [Theory]
    [InlineData("BitCraft", true)]
    [InlineData("BitCraft Online", true)]
    [InlineData("bitcraft", true)]
    [InlineData("BitCraft Bot — v0.1.0", false)] // la fenêtre du bot elle-même
    [InlineData("bitcraft-bot - Visual Studio Code", false)]
    [InlineData("BitCraft Wiki - Google Chrome", false)]
    [InlineData("Le wiki BitCraft", false)]
    [InlineData("", false)]
    [InlineData("Discord", false)]
    public void IsCandidateTitle(string title, bool expected) =>
        Assert.Equal(expected, GameWindowLocator.IsCandidateTitle(title, "BitCraft"));
}
