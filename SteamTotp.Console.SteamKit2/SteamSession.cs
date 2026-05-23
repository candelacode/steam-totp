using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SteamTotp.Console.SteamKit2;

public sealed class SteamSession
{
    private static readonly string SessionFileName = "steam_session.json";

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("steamId64")]
    public string SteamId64 { get; set; } = string.Empty;

    [JsonPropertyName("guardData")]
    public string? GuardData { get; set; }

    public static string GetSessionFilePath() => Path.Combine(AppContext.BaseDirectory, SessionFileName);

    public static SteamSession? Load()
    {
        var path = GetSessionFilePath();
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SteamSession>(json);
    }

    public void Save()
    {
        UpdatedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(GetSessionFilePath(), json);
    }

    public static void Delete()
    {
        var path = GetSessionFilePath();
        if (File.Exists(path))
            File.Delete(path);
    }
}