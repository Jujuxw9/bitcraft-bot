using BitcraftBot.Input.Keyboard;

namespace BitcraftBot.Tests;

// v0.4.14 : touche tenue (W) signalée une seule fois.
public class V0414Tests
{
    private const uint W = 0x11;

    [Fact]
    public void HeldKeyIsReportedOnce()
    {
        var filter = new KeyRepeatFilter();
        Assert.True(filter.IsFirstPress(W, false, 0));
        // Répétition de Windows : après 500 ms, puis toutes les 33 ms pendant 9 s.
        for (long t = 500; t < 9000; t += 33)
        {
            Assert.False(filter.IsFirstPress(W, false, t));
        }
    }

    [Fact]
    public void ReleasedKeyCountsAgain()
    {
        var filter = new KeyRepeatFilter();
        Assert.True(filter.IsFirstPress(W, false, 0));
        filter.Release(W, false);
        Assert.True(filter.IsFirstPress(W, false, 100));
    }

    [Fact]
    public void LostReleaseIsForgottenAfterDelay()
    {
        var filter = new KeyRepeatFilter();
        Assert.True(filter.IsFirstPress(W, false, 0));
        Assert.True(filter.IsFirstPress(W, false, KeyRepeatFilter.ForgetAfterMs + 1));
    }

    [Fact]
    public void OtherKeysAreIndependent()
    {
        var filter = new KeyRepeatFilter();
        Assert.True(filter.IsFirstPress(W, false, 0));
        Assert.True(filter.IsFirstPress(0x10, false, 10));
        Assert.True(filter.IsFirstPress(W, true, 20));
    }
}
