namespace SteamTotp;

public interface ISecretDecoder
{
    byte[] DecodeBase64(string base64);
    byte[] DecodeHex(string hex);
}

public sealed class SecretDecoder : ISecretDecoder
{
    public byte[] DecodeBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            throw new ArgumentException("Base64 string cannot be empty", nameof(base64));

        return Convert.FromBase64String(base64);
    }

    public byte[] DecodeHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            throw new ArgumentException("Hex string cannot be empty", nameof(hex));

        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have even length", nameof(hex));

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }
}