namespace SteamTotp;

public sealed class TimeOffsetResult
{
    public TimeOffsetResult(long offset, long latencyMs)
    {
        Offset = offset;
        LatencyMs = latencyMs;
    }

    public long Offset { get; }
    public long LatencyMs { get; }
}

public interface ITimeOffsetService
{
    Task<TimeOffsetResult> GetTimeOffsetAsync(CancellationToken cancellationToken = default);
}

public sealed class TimeOffsetService : ITimeOffsetService
{
    private const string Host = "api.steampowered.com";
    private const string Path = "/ITwoFactorService/QueryTime/v1/";

    private readonly ITimeProvider _timeProvider;
    private readonly HttpClient _httpClient;

    public TimeOffsetService() : this(new TimeProvider(), new HttpClient())
    {
    }

    public TimeOffsetService(ITimeProvider timeProvider, HttpClient httpClient)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<TimeOffsetResult> GetTimeOffsetAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://{Host}{Path}");
        request.Content = new ByteArrayContent(Array.Empty<byte>());
        request.Content.Headers.ContentLength = 0;

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP error {(int)response.StatusCode}");

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = System.Text.Json.JsonDocument.Parse(stream);
        
        var root = document.RootElement;
        var responseObj = root.GetProperty("response");
        var serverTime = responseObj.GetProperty("server_time").GetInt64();

        var endTime = DateTimeOffset.UtcNow;
        var localTime = _timeProvider.GetTime();
        var offset = serverTime - localTime;
        var latency = (endTime - startTime).Milliseconds;

        return new TimeOffsetResult(offset, latency);
    }
}