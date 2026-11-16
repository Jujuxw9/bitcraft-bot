using BitcraftBot.Input.Human;

namespace BitcraftBot.Tests;

public class HumanMouseTests
{
    private static readonly SafeArea Area = new(350, 150, 1570, 800);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(42)]
    public void Path_EndsExactlyOnTarget_AndStaysInArea(int seed)
    {
        var mouse = new HumanMouse(new Random(seed));
        var path = mouse.Path(400, 200, 1500, 750, Area);
        Assert.Equal((1500, 750), (path[^1].X, path[^1].Y));
        Assert.All(path, p => Assert.True(Area.Contains(p.X, p.Y), $"({p.X}, {p.Y}) hors zone"));
    }

    [Fact]
    public void Path_TimeAlwaysIncreases()
    {
        var path = new HumanMouse(new Random(7)).Path(400, 200, 1200, 700, Area);
        for (var i = 1; i < path.Count; i++)
        {
            Assert.True(path[i].AtMs >= path[i - 1].AtMs);
        }
    }

    [Fact]
    public void Path_DurationIsHumanLike()
    {
        // Un geste de ~900 px dure entre 0,3 et 1,5 s pour un humain.
        for (var seed = 0; seed < 50; seed++)
        {
            var path = new HumanMouse(new Random(seed)).Path(400, 300, 1300, 300, Area);
            Assert.InRange(path[^1].AtMs, 300, 1500);
        }
    }

    [Fact]
    public void Path_IsCurved_NotAStraightLine()
    {
        // Sur un trajet horizontal, au moins un trajet sur 20 s'écarte nettement de la ligne droite.
        var maxDeviation = 0;
        for (var seed = 0; seed < 20; seed++)
        {
            var path = new HumanMouse(new Random(seed)).Path(400, 400, 1400, 400, Area);
            maxDeviation = Math.Max(maxDeviation, path.Max(p => Math.Abs(p.Y - 400)));
        }
        Assert.True(maxDeviation > 30, $"Écart maximal {maxDeviation} px : trajets trop droits.");
    }

    [Fact]
    public void Path_TargetOutsideArea_IsClamped()
    {
        var path = new HumanMouse(new Random(1)).Path(500, 500, 3000, -200, Area);
        Assert.Equal((Area.Right, Area.Top), (path[^1].X, path[^1].Y));
    }

    [Fact]
    public void Pause_IsBetween250And1750Ms()
    {
        var mouse = new HumanMouse(new Random(3));
        for (var i = 0; i < 200; i++)
        {
            Assert.InRange(mouse.PauseMs(), 250, 1750);
        }
    }
}

public class HumanCameraTests
{
    [Fact]
    public void Actions_UseOnlyCameraKeys_WithHumanDurations()
    {
        var camera = new HumanCamera(new Random(5));
        var seen = new HashSet<CameraKey>();
        for (var i = 0; i < 500; i++)
        {
            var action = camera.Next(i);
            seen.Add(action.Key);
            Assert.InRange(action.HoldMs, 60, 900);
            Assert.InRange(action.PauseMs, 350, 2200);
        }
        // Toutes les touches de caméra finissent par être utilisées, et rien d'autre n'existe.
        Assert.Equal(5, seen.Count);
    }

    [Theory]
    [InlineData(CameraKey.Haut, 0x11)]      // Z sur AZERTY
    [InlineData(CameraKey.Gauche, 0x1E)]    // Q sur AZERTY
    [InlineData(CameraKey.Bas, 0x1F)]       // S
    [InlineData(CameraKey.Droite, 0x20)]    // D
    [InlineData(CameraKey.Recentrer, 0x2C)] // W sur AZERTY
    public void ScanCodes_MatchAzertyPhysicalKeys(CameraKey key, int scanCode) =>
        Assert.Equal(scanCode, CameraKeys.ScanCode(key));

    [Fact]
    public void NoScanCode_IsTheForbiddenXKey()
    {
        // X (cache les arbres, 0x2D) ne doit jamais être envoyé (D-024).
        Assert.DoesNotContain(Enum.GetValues<CameraKey>(), k => CameraKeys.ScanCode(k) == 0x2D);
    }
}
