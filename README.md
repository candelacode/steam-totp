# SteamTotp

[![NuGet](https://img.shields.io/nuget/v/SteamTotp)](https://www.nuget.org/packages/SteamTotp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SteamTotp)](https://www.nuget.org/packages/SteamTotp)
[![License](https://img.shields.io/github/license/candelacode/steam-totp)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![CI](https://github.com/candelacode/steam-totp/actions/workflows/ci.yml/badge.svg)](https://github.com/candelacode/steam-totp/actions/workflows/ci.yml)

A .NET library for generating Steam-style TOTP authentication codes. Supports Steam Guard codes, mobile confirmation keys, device ID generation, and server time synchronization.

## Installation

```bash
dotnet add package SteamTotp
```

## Quick Start

```csharp
using SteamTotp;

var service = new SteamTotpService();

// Generate a Steam Guard code from a hex secret
var code = service.GenerateAuthCodeHex("0123456789ABCDEF0123456789ABCDEF01234567");
Console.WriteLine(code); // e.g., "2W3R4"
```

## API Reference

### SteamTotpService

Main service for TOTP operations.

```csharp
// Create with default dependencies
var service = new SteamTotpService();

// Create with custom dependencies (for testing)
var service = new SteamTotpService(customTimeProvider, customSecretDecoder);
```

#### Generate Authentication Code

```csharp
// From hex secret
string code = service.GenerateAuthCodeHex("0123456789ABCDEF0123456789ABCDEF01234567");

// From base64 secret
string code = service.GenerateAuthCode("SGVsbG8gV29ybGQ=");

// From raw bytes
string code = service.GenerateAuthCode(secretBytes);

// With time offset (seconds ahead/behind Steam server)
string code = service.GenerateAuthCodeHex("0123456789ABCDEF...", offset: 10);
```

The offset parameter adjusts for clock drift between your server and Steam's servers. Use `TimeOffsetService` to calculate the correct offset.

#### Generate Confirmation Key

Generates keys needed for mobile trade confirmations.

```csharp
// With tag: "conf" (confirm), "allow" (approve), "cancel" (reject), "details" (details)
string key = service.GenerateConfirmationKeyHex("FEDCBA9876543210...", timestamp, "conf");

// From base64 identity secret
string key = service.GenerateConfirmationKey(base64IdentitySecret, timestamp, "conf");
```

#### Get Current Time

```csharp
long time = service.Time();
long timeWithOffset = service.Time(offset: 10);
```

### TimeOffsetService

Synchronize with Steam's server time to ensure generated codes are valid.

```csharp
var offsetService = new TimeOffsetService();
var result = await offsetService.GetTimeOffsetAsync();

Console.WriteLine($"Offset: {result.Offset}s");
Console.WriteLine($"Latency: {result.LatencyMs}ms");
```

Use the offset when generating codes:

```csharp
var offsetService = new TimeOffsetService();
var result = await offsetService.GetTimeOffsetAsync();

var code = service.GenerateAuthCodeHex(secret, offset: (int)result.Offset);
```

### DeviceIdGenerator

Generate standardized Android device IDs from SteamIDs.

```csharp
var generator = new DeviceIdGenerator();

string deviceId = generator.GetDeviceId("STEAM_1:1:12345678");
// android:f1776fe6-a7ae-c9c3-e8f8-036c7edc1d31

// With custom salt
string deviceId = generator.GetDeviceId("STEAM_1:1:12345678", "my-salt");
```

### SecretDecoder

Decode hex and base64 encoded secrets.

```csharp
var decoder = new SecretDecoder();

byte[] fromHex = decoder.DecodeHex("0123456789ABCDEF");
byte[] fromBase64 = decoder.DecodeBase64("SGVsbG8gV29ybGQ=");
```

## Architecture

The library follows SOLID principles with dependency injection support:

| Interface | Default Implementation | Purpose |
|-----------|----------------------|---------|
| `ITimeProvider` | `TimeProvider` | Time abstraction (`DateTimeOffset.UtcNow`) |
| `ISecretDecoder` | `SecretDecoder` | Hex/Base64 decoding |
| `IDeviceIdGenerator` | `DeviceIdGenerator` | Device ID generation |
| `ITimeOffsetService` | `TimeOffsetService` | Server time sync |

## Testing

```bash
dotnet test
```

## Requirements

- .NET Standard 2.0 compatible runtime

## License

MIT
