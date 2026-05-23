using SteamTotp;

var service = new SteamTotpService();
var deviceIdGenerator = new DeviceIdGenerator();

Console.WriteLine("SteamTotp .NET Library Demo");
Console.WriteLine("============================\n");

var secretHex = "0123456789ABCDEF0123456789ABCDEF01234567";
Console.WriteLine($"Secret (hex): {secretHex}\n");

var authCode = service.GenerateAuthCodeHex(secretHex);
Console.WriteLine($"Auth Code: {authCode}");

var authCodeWithOffset = service.GenerateAuthCodeHex(secretHex, 10);
Console.WriteLine($"Auth Code (offset +10): {authCodeWithOffset}\n");

var steamId = "STEAM_1:1:12345678";
var deviceId = deviceIdGenerator.GetDeviceId(steamId);
Console.WriteLine($"SteamID: {steamId}");
Console.WriteLine($"Device ID: {deviceId}\n");

var identitySecretHex = "FEDCBA9876543210FEDCBA9876543210FEDCBA98";
var confirmationKey = service.GenerateConfirmationKeyHex(identitySecretHex, service.Time(), "conf");
Console.WriteLine($"Confirmation Key: {confirmationKey}\n");

Console.WriteLine("Done!");
