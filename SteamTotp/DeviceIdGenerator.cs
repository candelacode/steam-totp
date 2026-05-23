using System.Security.Cryptography;
using System.Text;

namespace SteamTotp;

public interface IDeviceIdGenerator
{
    string GetDeviceId(string steamId, string? salt = null);
}

public sealed class DeviceIdGenerator : IDeviceIdGenerator
{
    private const string AndroidPrefix = "android:";
    private const string DefaultSalt = "";

    public string GetDeviceId(string steamId, string? salt = null)
    {
        if (string.IsNullOrEmpty(steamId))
            throw new ArgumentException("SteamID cannot be empty", nameof(steamId));

        salt ??= DefaultSalt;
        var input = steamId + salt;
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        var hex = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return FormatDeviceId(hex);
    }

    private static string FormatDeviceId(string hex)
    {
        return $"{AndroidPrefix}{hex.Substring(0, 8)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}-{hex.Substring(16, 4)}-{hex.Substring(20)}";
    }
}