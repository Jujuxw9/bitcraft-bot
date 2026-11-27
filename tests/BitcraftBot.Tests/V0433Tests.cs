using BitcraftBot.Input.Actions;
using BitcraftBot.Server;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.33 (T-15, contrôle du dépôt) : quelques cas limites des fonctions de la nuit du 27/11 qui n'avaient pas de test.
// Aucun changement de comportement : ces tests fixent ce que fait le code aujourd'hui.
public class V0433Tests
{
    private static NearbyResource Res(ulong id, double x, double z) =>
        new(id, 7, "Pin mûr", 2, "Tree", new ServerPosition(x, z), 0);

    [Fact]
    public void MemoryKeepsAResourceSeenExactly72HoursAgo()
    {
        var now = new DateTime(2026, 11, 27, 11, 0, 0, DateTimeKind.Utc);
        var memory = new ResourceMemory(() => now);
        memory.Update([Res(1, 100, 100)], null);
        now = now.Add(ResourceMemory.KeepFor);
        Assert.Equal((0, 1), memory.Cleanup());
        now = now.AddSeconds(1);
        Assert.Equal((1, 0), memory.Cleanup());
    }

    [Fact]
    public void SeenTextUsesMinutesUnderOneHour()
    {
        var now = new DateTime(2026, 11, 27, 11, 0, 0, DateTimeKind.Utc);
        var memory = new ResourceMemory(() => now);
        memory.Update([Res(1, 100, 100)], null);
        Assert.Null(memory.SeenText(2));
        now = now.AddMinutes(59);
        Assert.Equal("vue il y a 59 min", memory.SeenText(1));
        now = now.AddMinutes(61);
        Assert.Equal("vue il y a 2 h", memory.SeenText(1));
    }

    [Theory]
    [InlineData(double.NaN, 100)]
    [InlineData(100, double.PositiveInfinity)]
    [InlineData(-1, 100)]
    [InlineData(100, ResourceMemory.WorldSmallCells + 1)]
    public void InvalidPositionsAreRejected(double x, double z)
    {
        var memory = new ResourceMemory();
        memory.Update([Res(1, x, z)], null);
        Assert.Equal(0, memory.Count);
        Assert.Equal(1, memory.RejectedPosition);
    }

    [Fact]
    public void ZeroIdIsRejected() => Assert.False(ResourceMemory.Valid(Res(0, 100, 100)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Keldan (R18)")]
    [InlineData("(0, 6700)")]
    [InlineData("(8918, 30001)")]
    public void MapBarRefusesUnreadableOrOutOfBoundsText(string? text) => Assert.Null(MapScale.ParseBar(text));

    [Fact]
    public void MapBarKeepsTheLastPairAndAcceptsOtherSeparators()
    {
        Assert.Equal((8918, 6700), MapScale.ParseBar("Keldan (R18) | (8918, 6700)"));
        Assert.Equal((9041, 6808), MapScale.ParseBar("(1, 2) puis ( 9041 ; 6808 )"));
        Assert.Equal((9041, 6808), MapScale.ParseBar("(9041.6808)"));
    }

    [Fact]
    public void CartAndBoatKeysAreNeverAllowed()
    {
        foreach (var key in GameKeys.Forbidden)
        {
            Assert.False(GameKeys.Allowed.ContainsKey(key));
        }
        Assert.True(GameKeys.IsForbidden([GameKeys.Shift, GameKeys.V]));
        Assert.True(GameKeys.IsForbidden([GameKeys.Shift, GameKeys.B]));
        Assert.False(GameKeys.IsForbidden([GameKeys.Sprint, GameKeys.Interact]));
    }
}
