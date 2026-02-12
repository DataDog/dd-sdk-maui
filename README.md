# Datadog SDK for .NET MAUI

Datadog Real User Monitoring (RUM) enables you to visualize and analyze the real-time performance and user journeys of your application's individual users.

## Installation

Add the NuGet package to your MAUI project:

```xml
<PackageReference Include="DatadogSdk.Maui" Version="1.0.0" />
```

## Usage

### Initialize the SDK

In your `MauiProgram.cs`:

```csharp
using DatadogSdk.Maui;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    // ...

    DdSdk.Initialize(new DdSdkConfiguration
    {
        ClientToken = "your-client-token",
        Environment = "prod",
        Service = "my-maui-app",
        Verbosity = SdkVerbosity.DEBUG  // optional: DEBUG enables console + native SDK verbose logging
    });

    return builder.Build();
}
```

### Logging

```csharp
using DatadogSdk.Maui;

// Enable the logs module
DdLogs.Enable();

// Send logs at different levels
DdLogs.Debug("Debug message");
DdLogs.Info("Info message");
DdLogs.Warn("Warning message");
DdLogs.Error("Error message");

// Send logs with custom attributes
DdLogs.LogWithAttributes("info", "Order placed", new Dictionary<string, string>
{
    { "order_id", "12345" }
});
```

## Architecture

The SDK uses a four-layer architecture:

1. **Native Wrappers** (Swift/Kotlin) - Thin bridge to the Datadog native SDKs
2. **C# Bindings** - Platform-specific interop (Xamarin.iOS / Android Java Bindings)
3. **C# Intermediary Layer** (`DatadogSdk.Maui`) - Unified cross-platform API
4. **Consumer App** - Your .NET MAUI application

See [SPEC.md](SPEC.md) for full technical details.

## Development

```bash
# Build all native wrappers + bindings
./build.sh

# Run the example app
cd example
./build.sh --ios --run       # iOS simulator
./build.sh --android --run   # Android emulator
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for the full development guide.
