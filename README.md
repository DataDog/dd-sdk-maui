# Datadog SDK for .NET MAUI

> Datadog Real User Monitoring (RUM) enables you to visualize and analyze the real-time performance and user journeys of your application's individual users.

## Current Features

- **Logs**: Send logs from your .NET MAUI application to Datadog with support for debug, info, warn, and error levels, plus custom attributes.

## Setup

To integrate the Datadog SDK into your .NET MAUI application, see the setup instructions below.

### Installation

Add the NuGet package to your MAUI `.csproj`:

```xml
<PackageReference Include="DatadogSdk.Maui" Version="1.0.0" />
```

### Initialization

Initialize the SDK in your `MauiProgram.cs`:

```csharp
using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

DdSdk.Initialize(new DdSdkConfiguration
{
    // Required
    ClientToken = "your-client-token",
    Environment = "prod",
    TrackingConsent = TrackingConsent.Granted,

    // Optional
    Service = "my-maui-app",
    Site = DatadogSite.Us1,
    BatchSize = BatchSize.Medium,
    UploadFrequency = UploadFrequency.Average
});
```

#### File-based Configuration

You can also initialize from a JSON configuration file:

```json
{
  "ClientToken": "your-client-token",
  "Environment": "prod",
  "Site": "Us1",
  "Service": "my-maui-app",
  "TrackingConsent": "Granted",
  "Verbosity": "DEBUG"
}
```

```csharp
string json = File.ReadAllText("appsettings.json");
var config = FileBasedConfiguration.ParseJsonConfig(json);
DdSdk.Initialize(config);
```

**Required fields:**
- `ClientToken` - Your Datadog client token
- `Environment` - Environment name (e.g., "prod", "staging")
- `TrackingConsent` - User tracking consent (`Granted`, `NotGranted`, or `Pending`)

**Optional fields:**
- `Service` - Service name
- `Site` - Datadog site (`Us1`, `Us3`, `Us5`, `Eu1`, `Ap1`, `Ap2`, `Us1Fed`)
- `BatchSize` - Batch size for uploads (`Small`, `Medium`, `Large`)
- `BatchProcessingLevel` - Processing level (`Low`, `Medium`, `High`)
- `UploadFrequency` - Upload frequency (`Frequent`, `Average`, `Rare`)
- `Version` - Application version
- `VersionSuffix` - Version suffix
- `Verbosity` - SDK logging level
- `AdditionalConfiguration` - Additional configuration dictionary

### Logs

```csharp
using DatadogSdk.Maui;

DdLogs.Enable();

DdLogs.Debug("Debug message");
DdLogs.Info("Info message");
DdLogs.Warn("Warning message");
DdLogs.Error("Error message");
```

## Troubleshooting

If you encounter issues while using the SDK, check the existing [GitHub Issues](https://github.com/DataDog/dd-sdk-maui/issues) for known problems and solutions.

You can also enable verbose SDK logging to help diagnose issues:

```csharp
using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

DdSdk.Initialize(new DdSdkConfiguration
{
    ClientToken = "your-client-token",
    Environment = "prod",
    TrackingConsent = TrackingConsent.Granted,
    Verbosity = SdkVerbosity.DEBUG
});
```

## Testing

Run all test suites (iOS, Android, C#):

```bash
./check.sh
```

Individual suites:

```bash
./check.sh --maui      # C# unit tests (xUnit)
./check.sh --ios       # Swift tests (XCTest)
./check.sh --android   # Kotlin tests (JUnit + MockK)
```

## Contributing

Pull requests are welcome. First, open an issue to discuss what you would like to change.

See [CONTRIBUTING.md](CONTRIBUTING.md) for the development setup guide.

## License

[Apache License, v2.0](LICENSE)
