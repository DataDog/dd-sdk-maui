# Datadog SDK for .NET MAUI

> Datadog Real User Monitoring (RUM) enables you to visualize and analyze the real-time performance and user journeys of your application's individual users.

## Current Features

- **Core SDK**: Initialize Datadog with full configuration support, including runtime tracking consent updates and global attributes.
- **Logs**: Send logs from your .NET MAUI application to Datadog with support for debug, info, warn, and error levels, plus custom attributes.
- **Traces**: Manual span tracking with support for nested parent-child relationships, custom context attributes, and configurable endpoints.
- **RUM (Real User Monitoring)**: Enable RUM to track user sessions, views, actions, and crashes. Configure session sampling, vitals monitoring, native view/interaction tracking, crash reporting, and first-party hosts for distributed tracing.

## Setup

To integrate the Datadog SDK into your .NET MAUI application, see the setup instructions below.

### Installation

Add the NuGet package to your MAUI `.csproj`:

```xml
<PackageReference Include="DatadogSdk.Maui" Version="0.0.1" />
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

### Tracking Consent

You can update the tracking consent at any time after initialization:

```csharp
// Update tracking consent at runtime (e.g., after user accepts a consent dialog)
DdSdk.SetTrackingConsent(TrackingConsent.Granted);
```

### Attributes

Set global attributes that are attached to all future events (RUM, Logs, Traces):

```csharp
// Add a global attribute
DdSdk.AddAttribute("plan", "premium");

// Add multiple attributes at once
DdSdk.AddAttributes(new Dictionary<string, object>
{
    { "plan", "premium" },
    { "experiment", "new-checkout-flow" }
});

// Remove a global attribute
DdSdk.RemoveAttribute("experiment");

// Remove multiple attributes at once
DdSdk.RemoveAttributes(new List<string> { "plan", "experiment" });

// Read current attributes (returns a snapshot copy)
var attributes = DdSdk.GetAttributes();
```

### Logs

```csharp
using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

// Enable with default Datadog endpoint
DdLogs.Enable();

// Or with a custom endpoint (proxy, on-premises, or local mock server)
DdLogs.Enable(new DdLogsConfiguration
{
    CustomEndpoint = "https://logs-proxy.example.com/v1/input"
});

DdLogs.Debug("Debug message");
DdLogs.Info("Info message");
DdLogs.Warn("Warning message");
DdLogs.Error("Error message");
```

### Traces

```csharp
using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

// Enable with default Datadog endpoint
DdTrace.Enable();

// Or with a custom endpoint
DdTrace.Enable(new DdTraceConfiguration
{
    CustomEndpoint = "https://traces-proxy.example.com/v1/input"
});

// Start a span (returns a span ID for later use)
var spanId = DdTrace.StartSpan(
    "network.request",
    new Dictionary<string, string> { { "url", "/api/data" } },
    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
);

// Nested spans are automatically linked as parent-child
var childSpanId = DdTrace.StartSpan(
    "json.parse",
    new Dictionary<string, string>(),
    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
);

// Finish spans (LIFO order for proper nesting)
DdTrace.FinishSpan(childSpanId, new Dictionary<string, string>(),
    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
DdTrace.FinishSpan(spanId, new Dictionary<string, string> { { "status", "200" } },
    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
```

### RUM

```csharp
// Enable RUM with configuration
DdRum.Enable(new DdRumConfiguration
{
    ApplicationId = "your-rum-application-id",
    SessionSampleRate = 100.0,
    TrackFrustrations = true,
    TrackBackgroundEvents = true,
    NativeCrashReportEnabled = true,
    NativeViewTracking = true,
    NativeInteractionTracking = true,
    VitalsUpdateFrequency = VitalsUpdateFrequency.Average,
    FirstPartyHosts = new List<DdFirstPartyHost>
    {
        new() { Match = "api.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog, TracingHeaderType.TraceContext } }
    }
});
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
