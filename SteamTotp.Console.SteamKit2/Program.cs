using SteamTotp.Console.SteamKit2;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string? GetConfigValue(string key) =>
    configuration[key] ?? Environment.GetEnvironmentVariable(key);

string? username = GetConfigValue("STEAM_USER");
string? password = GetConfigValue("STEAM_PASSWORD");
string? sharedSecret = GetConfigValue("STEAM_SHARED_SECRET");
string? identitySecret = GetConfigValue("STEAM_IDENTITY_SECRET");

if (args.Length >= 4)
{
    username = args[0];
    password = args[1];
    sharedSecret = args[2];
    identitySecret = args[3];
}

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
    string.IsNullOrEmpty(sharedSecret) || string.IsNullOrEmpty(identitySecret))
{
    Console.WriteLine("Usage: SteamLogin [username] [password] [shared_secret] [identity_secret]");
    Console.WriteLine("Or set UserSecrets/environment variables: STEAM_USER, STEAM_PASSWORD, STEAM_SHARED_SECRET, STEAM_IDENTITY_SECRET");
    return;
}

Console.WriteLine($"Logging in as: {username}");

var existingSession = SteamSession.Load();
if (existingSession != null)
{
    Console.WriteLine($"Loaded existing session for SteamID: {existingSession.SteamId64}");
    Console.WriteLine($"Session updated at: {existingSession.UpdatedAt}");
}

using var connection = new SteamConnection(username, password, sharedSecret, identitySecret, existingSession);

connection.OnLoggedOn += message => Console.WriteLine($"Logged on event: {message}");
connection.OnLoggedOff += message => Console.WriteLine($"Logged off event: {message}");
connection.OnDisconnected += () => Console.WriteLine("Disconnected event fired");
connection.OnError += ex => Console.WriteLine($"Error: {ex.Message}");

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nDisconnecting...");
    connection.Disconnect();
};

Console.WriteLine("Press Ctrl+C to disconnect and exit.\n");
connection.Connect();
connection.KeepAlive();
Console.WriteLine("Disconnected. Goodbye!");