namespace SteamTotp;

public interface ITimeProvider
{
    long GetTime(int offset = 0);
}

public sealed class TimeProvider : ITimeProvider
{
    public long GetTime(int offset = 0) => 
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() + offset;
}