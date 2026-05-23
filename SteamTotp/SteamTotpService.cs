namespace SteamTotp;

using System.Buffers.Binary;

public sealed class SteamTotpService
{
    private static readonly char[] Chars = "23456789BCDFGHJKMNPQRTVWXY".ToCharArray();
    private const int CodeLength = 5;
    private const int TimeStepSeconds = 30;

    private readonly ITimeProvider _timeProvider;
    private readonly ISecretDecoder _secretDecoder;

    public SteamTotpService() : this(new TimeProvider(), new SecretDecoder())
    {
    }

    public SteamTotpService(ITimeProvider timeProvider, ISecretDecoder secretDecoder)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _secretDecoder = secretDecoder ?? throw new ArgumentNullException(nameof(secretDecoder));
    }

    public long Time(int offset = 0) => _timeProvider.GetTime(offset);

    public string GenerateAuthCode(ReadOnlySpan<byte> secret, int offset = 0)
    {
        if (secret.Length == 0)
            throw new ArgumentException("Secret cannot be empty", nameof(secret));

        var time = _timeProvider.GetTime(offset);
        var buffer = CreateTimeBuffer(time);
        var hmac = ComputeHmacSha1(secret, buffer);
        var code = ExtractCode(hmac);

        return code;
    }

    public string GenerateAuthCode(string base64Secret, int offset = 0)
    {
        var secret = _secretDecoder.DecodeBase64(base64Secret);
        return GenerateAuthCode(secret, offset);
    }

    public string GenerateAuthCodeHex(string hexSecret, int offset = 0)
    {
        var secret = _secretDecoder.DecodeHex(hexSecret);
        return GenerateAuthCode(secret, offset);
    }

    public string GenerateConfirmationKey(ReadOnlySpan<byte> identitySecret, long time, string? tag = null)
    {
        if (identitySecret.Length == 0)
            throw new ArgumentException("Identity secret cannot be empty", nameof(identitySecret));

        var dataLen = 8 + (tag?.Length ?? 0);
        var buffer = new byte[dataLen];

        WriteBigEndian64(time, buffer);
        if (tag != null)
        {
            var tagBytes = System.Text.Encoding.ASCII.GetBytes(tag);
            Buffer.BlockCopy(tagBytes, 0, buffer, 8, tagBytes.Length);
        }

        var hmac = ComputeHmacSha1(identitySecret, buffer);
        return Convert.ToBase64String(hmac);
    }

    public string GenerateConfirmationKey(string identitySecretBase64, long time, string? tag = null)
    {
        var secret = _secretDecoder.DecodeBase64(identitySecretBase64);
        return GenerateConfirmationKey(secret, time, tag);
    }

    public string GenerateConfirmationKeyHex(string identitySecretHex, long time, string? tag = null)
    {
        var secret = _secretDecoder.DecodeHex(identitySecretHex);
        return GenerateConfirmationKey(secret, time, tag);
    }

    private byte[] CreateTimeBuffer(long time)
    {
        var buffer = new byte[8];
        var steps = time / TimeStepSeconds;
        WriteBigEndian64(steps, buffer);
        return buffer;
    }

    private static void WriteBigEndian64(long value, byte[] buffer)
    {
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
    }

    private byte[] ComputeHmacSha1(ReadOnlySpan<byte> key, byte[] data)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA1(key.ToArray());
        return hmac.ComputeHash(data);
    }

    private string ExtractCode(byte[] hmac)
    {
        var start = hmac[19] & 0x0F;
        var fullcode = BinaryPrimitives.ReadUInt32BigEndian(hmac.AsSpan(start, 4)) & 0x7FFFFFFF;

        var code = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            code[i] = Chars[fullcode % (uint)Chars.Length];
            fullcode /= (uint)Chars.Length;
        }

        return new string(code);
    }
}