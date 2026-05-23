namespace SteamTotp.Tests;

public class DeviceIdGeneratorTests
{
    private readonly DeviceIdGenerator _generator = new();

    [Fact]
    public void GetDeviceId_EmptySteamId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _generator.GetDeviceId(""));
    }

    [Fact]
    public void GetDeviceId_ValidSteamId_ReturnsAndroidFormattedId()
    {
        var result = _generator.GetDeviceId("STEAM_1:1:12345678");

        Assert.StartsWith("android:", result);
        var guid = result.Substring("android:".Length);
        Assert.Equal(5, guid.Split('-').Length);
    }

    [Fact]
    public void GetDeviceId_SameSteamId_ReturnsSameDeviceId()
    {
        var steamId = "STEAM_1:1:12345678";

        var result1 = _generator.GetDeviceId(steamId);
        var result2 = _generator.GetDeviceId(steamId);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetDeviceId_DifferentSalt_ReturnsDifferentDeviceId()
    {
        var steamId = "STEAM_1:1:12345678";

        var result1 = _generator.GetDeviceId(steamId, "salt1");
        var result2 = _generator.GetDeviceId(steamId, "salt2");

        Assert.NotEqual(result1, result2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetDeviceId_NullOrEmptySalt_UsesDefaultSalt(string? salt)
    {
        var steamId = "STEAM_1:1:12345678";

        var result1 = _generator.GetDeviceId(steamId);
        var result2 = _generator.GetDeviceId(steamId, salt);

        Assert.Equal(result1, result2);
    }
}