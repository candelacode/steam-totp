namespace SteamTotp.Tests;

public class TimeProviderTests
{
    [Fact]
    public void Time_ReturnsUnixTimestamp()
    {
        var provider = new TestTimeProvider();
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var time = provider.GetTime();
        var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Assert.InRange(time, before, after);
    }

    [Fact]
    public void Time_WithOffset_AddsOffset()
    {
        var provider = new TestTimeProvider();
        var offset = 100;
        var baseTime = provider.GetTime();
        var timeWithOffset = provider.GetTime(offset);

        Assert.Equal(baseTime + offset, timeWithOffset);
    }
}

internal class TestTimeProvider : ITimeProvider
{
    public long GetTime(int offset = 0) =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() + offset;
}