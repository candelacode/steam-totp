using System;
using System.Text.Json;
using SteamKit2;
using SteamKit2.Authentication;
using SteamTotp;

namespace SteamTotp.Console.SteamKit2;

public sealed class SteamConnection : IDisposable
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _sharedSecret;
    private readonly string _identitySecret;
    private readonly SteamSession? _existingSession;
    private string? _accessToken;

    private SteamClient? _steamClient;
    private CallbackManager? _manager;
    private SteamUser? _steamUser;
    private SteamFriends? _steamFriends;
    private bool _isRunning;

    public SteamID? SteamId { get; private set; }
    public event Action<string>? OnLoggedOn;
    public event Action<string>? OnLoggedOff;
    public event Action? OnDisconnected;
    public event Action<Exception>? OnError;

    public SteamConnection(string username, string password, string sharedSecret, string identitySecret, SteamSession? existingSession = null)
    {
        _username = username;
        _password = password;
        _sharedSecret = sharedSecret;
        _identitySecret = identitySecret;
        _existingSession = existingSession;
    }

    public void Connect()
    {
        _steamClient = new SteamClient();
        _manager = new CallbackManager(_steamClient);
        _steamUser = _steamClient.GetHandler<SteamUser>();
        _steamFriends = _steamClient.GetHandler<SteamFriends>();

        _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnectedCallback);
        _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOnCallback);
        _manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOffCallback);
        _manager.Subscribe<SteamFriends.PersonaStateCallback>(OnPersonaState);
        _manager.Subscribe<SteamFriends.FriendMsgCallback>(OnFriendMessage);

        _isRunning = true;
        System.Console.WriteLine("Connecting to Steam...");
        _steamClient.Connect();
    }

    public void KeepAlive()
    {
        _isRunning = true;
        while (_isRunning)
        {
            _manager?.RunWaitCallbacks(TimeSpan.FromSeconds(1));
        }
    }

    private async void OnConnected(SteamClient.ConnectedCallback callback)
    {
        System.Console.WriteLine("Connected to Steam! Logging in...");
        var shouldRememberPassword = true;

        try
        {
            var authSession = await _steamClient!.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
            {
                Username = _username,
                Password = _password,
                IsPersistentSession = shouldRememberPassword,
                GuardData = _existingSession?.GuardData,
                Authenticator = new SteamTotpAuthenticatorAdapter(_sharedSecret)
            });

            System.Console.WriteLine("Polling for authentication result...");
            var pollResponse = await authSession.PollingWaitForResultAsync();

            _accessToken = pollResponse.RefreshToken;

            if (pollResponse.NewGuardData != null && _existingSession != null)
            {
                _existingSession.GuardData = pollResponse.NewGuardData;
            }

            _steamUser!.LogOn(new SteamUser.LogOnDetails
            {
                Username = pollResponse.AccountName,
                AccessToken = _accessToken,
                ShouldRememberPassword = shouldRememberPassword
            });

            ParseJsonWebToken(pollResponse.AccessToken, nameof(pollResponse.AccessToken));
            ParseJsonWebToken(pollResponse.RefreshToken, nameof(pollResponse.RefreshToken));
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Authentication failed: {ex.Message}");
            OnError?.Invoke(ex);
            _isRunning = false;
        }
    }

    private void OnDisconnectedCallback(SteamClient.DisconnectedCallback callback)
    {
        System.Console.WriteLine("Disconnected from Steam");
        OnDisconnected?.Invoke();
        _isRunning = false;
    }

    private void OnLoggedOnCallback(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result != EResult.OK)
        {
            System.Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);
            _isRunning = false;
            OnLoggedOff?.Invoke($"Failed: {callback.Result}");
            return;
        }

        SteamId = callback.ClientSteamID;
        System.Console.WriteLine("Successfully logged on!");
        System.Console.WriteLine($"SteamID: {SteamId}");

        if (!string.IsNullOrEmpty(_identitySecret) && SteamId != null)
        {
            var time = new SteamTotpService().Time();
            var confirmationKey = GenerateConfirmationKey(_identitySecret, time, "conf");
            System.Console.WriteLine($"Confirmation Key: {confirmationKey}");

            var deviceId = GenerateDeviceId(SteamId);
            System.Console.WriteLine($"Device ID: {deviceId}");
        }

        OnLoggedOn?.Invoke($"Logged on as {SteamId}");

        _steamFriends?.SetPersonaName("candelacards");

        if (_existingSession != null)
        {
            if (!string.IsNullOrEmpty(_accessToken))
                _existingSession.RefreshToken = _accessToken;
            _existingSession.SteamId64 = SteamId!.ToString();
            _existingSession.Save();
            System.Console.WriteLine("Session saved to steam_session.json");
        }
    }

    private void OnLoggedOffCallback(SteamUser.LoggedOffCallback callback)
    {
        System.Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        OnLoggedOff?.Invoke(callback.Result.ToString());
    }

    private void OnPersonaState(SteamFriends.PersonaStateCallback callback)
    {
        System.Console.WriteLine($"Persona state update: {callback.Name}");
    }

    private void OnFriendMessage(SteamFriends.FriendMsgCallback callback)
    {
        var sender = callback.Sender;
        var message = callback.Message;
        System.Console.WriteLine($"[Message from {sender}]: {message}");
    }

    private string GenerateConfirmationKey(string identitySecretBase64, long time, string tag)
    {
        var service = new SteamTotpService();
        return service.GenerateConfirmationKey(identitySecretBase64, time, tag);
    }

    private string GenerateDeviceId(SteamID steamId)
    {
        var generator = new DeviceIdGenerator();
        return generator.GetDeviceId(steamId.ToString());
    }

    private void ParseJsonWebToken(string token, string name)
    {
        var tokenComponents = token.Split('.');
        var base64 = tokenComponents[1].Replace('-', '+').Replace('_', '/');
        if (base64.Length % 4 != 0)
            base64 += new string('=', 4 - base64.Length % 4);

        var payloadBytes = Convert.FromBase64String(base64);
        var payload = JsonDocument.Parse(payloadBytes);
        var formatted = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        System.Console.WriteLine($"{name}: {formatted}");
        System.Console.WriteLine();
    }

    public void Disconnect()
    {
        _isRunning = false;
        if (_steamUser != null)
        {
            System.Console.WriteLine("Logging off...");
            _steamUser.LogOff();
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}