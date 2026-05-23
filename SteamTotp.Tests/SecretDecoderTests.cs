namespace SteamTotp.Tests;

public class SecretDecoderTests
{
    private readonly SecretDecoder _decoder = new();

    [Fact]
    public void DecodeHex_ValidHex_ReturnsCorrectBytes()
    {
        var result = _decoder.DecodeHex("0123456789ABCDEF");
        
        Assert.Equal(8, result.Length);
        Assert.Equal(0x01, result[0]);
        Assert.Equal(0x23, result[1]);
        Assert.Equal(0x45, result[2]);
        Assert.Equal(0x67, result[3]);
        Assert.Equal(0x89, result[4]);
        Assert.Equal(0xAB, result[5]);
        Assert.Equal(0xCD, result[6]);
        Assert.Equal(0xEF, result[7]);
    }

    [Fact]
    public void DecodeHex_Lowercase_ReturnsCorrectBytes()
    {
        var result = _decoder.DecodeHex("0123456789abcdef");
        
        Assert.Equal(8, result.Length);
        Assert.Equal(0x01, result[0]);
        Assert.Equal(0xEF, result[7]);
    }

    [Fact]
    public void DecodeHex_Empty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _decoder.DecodeHex(""));
    }

    [Fact]
    public void DecodeHex_OddLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _decoder.DecodeHex("012"));
    }

    [Fact]
    public void DecodeBase64_ValidBase64_ReturnsCorrectBytes()
    {
        var result = _decoder.DecodeBase64("SGVsbG8gV29ybGQ=");
        
        Assert.Equal("Hello World", System.Text.Encoding.UTF8.GetString(result));
    }

    [Fact]
    public void DecodeBase64_Empty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _decoder.DecodeBase64(""));
    }
}