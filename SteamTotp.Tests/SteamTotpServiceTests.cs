using NSubstitute;

namespace SteamTotp.Tests;

public class SteamTotpServiceTests
{
    private readonly ITimeProvider _timeProvider;
    private readonly ISecretDecoder _secretDecoder;
    private readonly SteamTotpService _service;

    public SteamTotpServiceTests()
    {
        _timeProvider = Substitute.For<ITimeProvider>();
        _secretDecoder = Substitute.For<ISecretDecoder>();
        _service = new SteamTotpService(_timeProvider, _secretDecoder);
    }

    [Fact]
    public void Time_ReturnsTimeFromProvider()
    {
        _timeProvider.GetTime(0).Returns(1000);

        var result = _service.Time();

        Assert.Equal(1000, result);
    }

    [Fact]
    public void Time_WithOffset_PassesOffsetToProvider()
    {
        _timeProvider.GetTime(5).Returns(1005);

        var result = _service.Time(5);

        Assert.Equal(1005, result);
    }

    [Fact]
    public void GenerateAuthCode_EmptySecret_ThrowsArgumentException()
    {
        var secret = Array.Empty<byte>();

        Assert.Throws<ArgumentException>(() => _service.GenerateAuthCode(secret));
    }

    [Fact]
    public void GenerateAuthCode_CodeIsFiveCharacters()
    {
        var secret = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        _timeProvider.GetTime(0).Returns(1000L);
        _secretDecoder.DecodeHex("0102030405").Returns(secret);

        var result = _service.GenerateAuthCodeHex("0102030405");

        Assert.Equal(5, result.Length);
        Assert.All(result, c => Assert.True(char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void GenerateAuthCodeHex_CallsSecretDecoder()
    {
        var secret = new byte[] { 0x01 };
        _timeProvider.GetTime(0).Returns(0L);
        _secretDecoder.DecodeHex("01").Returns(secret);

        _service.GenerateAuthCodeHex("01");

        _secretDecoder.Received(1).DecodeHex("01");
    }

    [Fact]
    public void GenerateAuthCodeBase64_CallsSecretDecoder()
    {
        var secret = new byte[] { 0x01 };
        _timeProvider.GetTime(0).Returns(0L);
        _secretDecoder.DecodeBase64("AQ==").Returns(secret);

        _service.GenerateAuthCode("AQ==");

        _secretDecoder.Received(1).DecodeBase64("AQ==");
    }

    [Fact]
    public void GenerateConfirmationKey_EmptySecret_ThrowsArgumentException()
    {
        var secret = Array.Empty<byte>();

        Assert.Throws<ArgumentException>(() => _service.GenerateConfirmationKey(secret, 1000));
    }

    [Fact]
    public void GenerateConfirmationKey_ReturnsBase64String()
    {
        var secret = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        _timeProvider.GetTime(0).Returns(0L);

        var result = _service.GenerateConfirmationKey(secret, 1000);

        Assert.NotEmpty(result);
        var decoded = Convert.FromBase64String(result);
        Assert.Equal(20, decoded.Length);
    }

    [Fact]
    public void GenerateConfirmationKeyHex_CallsSecretDecoder()
    {
        var secret = new byte[] { 0x01 };
        _secretDecoder.DecodeHex("01").Returns(secret);

        _service.GenerateConfirmationKeyHex("01", 1000);

        _secretDecoder.Received(1).DecodeHex("01");
    }

    [Fact]
    public void GenerateConfirmationKey_WithTag_IncludesTagInHmac()
    {
        var secret = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

        var resultWithTag = _service.GenerateConfirmationKey(secret, 1000, "conf");
        var resultWithoutTag = _service.GenerateConfirmationKey(secret, 1000);

        Assert.NotEqual(resultWithTag, resultWithoutTag);
    }
}