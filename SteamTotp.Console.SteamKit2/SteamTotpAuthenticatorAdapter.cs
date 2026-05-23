using System;
using SteamKit2;
using SteamKit2.Authentication;
using SteamTotp;

namespace SteamTotp.Console.SteamKit2;

public sealed class SteamTotpAuthenticatorAdapter : IAuthenticator
{
    private readonly string _sharedSecret;

    public SteamTotpAuthenticatorAdapter(string sharedSecret)
    {
        _sharedSecret = sharedSecret;
    }

    public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
    {
        var service = new SteamTotpService();
        var code = service.GenerateAuthCode(_sharedSecret);
        System.Console.WriteLine($"Generated 2FA code: {code}");
        return Task.FromResult(code);
    }

    public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
    {
        return Task.FromResult(string.Empty);
    }

    public Task<bool> AcceptDeviceConfirmationAsync()
    {
        return Task.FromResult(false);
    }
}