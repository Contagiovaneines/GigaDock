using DockWindows.App.Common;

namespace DockWindows.Tests;

public sealed class PokemonEvolutionClockTests
{
    [Fact]
    public void EvolutionStartsAtSelectionRegardlessOfSystemUptime()
    {
        var time = new TestTime { Timestamp = TimeSpan.FromDays(5).Ticks };
        var clock = new PokemonEvolutionClock(time);
        Assert.Equal(0, clock.Stage);
        Assert.True(clock.Select(25));
        Assert.Equal(0, clock.Stage);
        time.Timestamp += TimeSpan.FromMinutes(59).Ticks;
        Assert.Equal(0, clock.Stage);
        time.Timestamp += TimeSpan.FromMinutes(1).Ticks;
        Assert.Equal(1, clock.Stage);
        Assert.False(clock.Select(25));
        Assert.Equal(1, clock.Stage);
        time.Timestamp += TimeSpan.FromHours(2).Ticks;
        Assert.Equal(2, clock.Stage);
        Assert.True(clock.Select(133));
        Assert.Equal(0, clock.Stage);
    }

    private sealed class TestTime : TimeProvider
    {
        public long Timestamp { get; set; }
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Timestamp;
    }
}
