# Datadog SDK for .NET MAUI

> Datadog Real User Monitoring (RUM) enables you to visualize and analyze the real-time performance and user journeys of your application's individual users.

## Current Features

- **Core SDK**: Initialize Datadog with full configuration support, including runtime tracking consent updates, global attributes, user info, and account info.
- **Logs**: Send logs from your .NET MAUI application to Datadog with support for debug, info, warn, and error levels, plus custom attributes.
- **Traces**: Manual span tracking with support for nested parent-child relationships, custom context attributes, and configurable endpoints.
- **RUM (Real User Monitoring)**: Full RUM tracking API including views, actions, resources, operations, timings, and session management. Configure session sampling, vitals monitoring, native view/interaction tracking, crash reporting, and first-party hosts for distributed tracing.
- **Error Tracking**: Automatic C# error and crash tracking. When RUM is enabled, unhandled exceptions and unobserved task exceptions are automatically captured and reported to Datadog. You can also manually report errors using `DdRum.AddError()`.

## Requirements

- .NET 9.0 or .NET 10.0
- iOS 15.0+ / Android API 23+

## Setup

To integrate the Datadog SDK into your .NET MAUI application, see the setup instructions below.

### Installation

Add the NuGet package to your MAUI `.csproj`:

```xml
<PackageReference Include="DatadogSdk.Maui" Version="0.0.1" />
```

### Initialization

The SDK supports two initialization patterns. Pick whichever matches your app's hosting style — they are mutually exclusive but otherwise equivalent.

#### Pattern 1 — Builder extensions (recommended for `MauiProgram.CreateMauiApp`)

```csharp
using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;
using DatadogSdk.Maui.Hosting;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseDatadogSdk(new DdSdkConfiguration
        {
            ClientToken = "your-client-token",
            Environment = "prod",
            TrackingConsent = TrackingConsent.Granted,
            Service = "my-maui-app",
            Site = DatadogSite.Us1,
            NativeCrashReportEnabled = true,
        })
        .UseDatadogLogs()
        .UseDatadogTrace()
        .UseDatadogRum(new DdRumConfiguration
        {
            ApplicationId = "your-rum-application-id",
            SessionSampleRate = 100.0,
            FirstPartyHosts = new List<FirstPartyHost>
            {
                new() { Match = "api.example.com", HeaderTypes = new List<TracingHeaderType> { TracingHeaderType.Datadog, TracingHeaderType.TraceContext } }
            },
        })
        .UseDatadogSessionReplay(new SessionReplayConfiguration { ReplaySampleRate = 100.0 });

    return builder.Build();
}
```

The `UseDatadog*` extensions live in `DatadogSdk.Maui.Hosting`. Each feature's `Enable` runs synchronously on the chain; for RUM, the automatic page / action / resource trackers attach at the first post-launch lifecycle event (`FinishedLaunching` on iOS, `OnApplicationCreating` on Android) — by that point MAUI has resolved `IApplication` and the trackers can subscribe to the `Application` instance before the first page appears.

#### Pattern 2 — Standalone calls

For apps that already have a custom host pipeline, or that want to enable features lazily, call the static APIs directly:

```csharp
using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>();

    DdSdk.Initialize(new DdSdkConfiguration
    {
        ClientToken = "your-client-token",
        Environment = "prod",
        TrackingConsent = TrackingConsent.Granted,
        // ...
    });

    return builder.Build();
}

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        DdLogs.Enable();
        DdTrace.Enable();
        DdRum.Enable(new DdRumConfiguration { ApplicationId = "your-rum-application-id" });
        DdSessionReplay.Enable(new SessionReplayConfiguration { ReplaySampleRate = 100.0 });
    }
}
```

Call `DdRum.Enable` from a page constructor (or any time `Application.Current` is already set) so its automatic page/action trackers can subscribe to MAUI's `Application` events.

#### File-based Configuration

You can also initialize from a JSON configuration file:

```json
{
  "ClientToken": "your-client-token",
  "Environment": "prod",
  "Site": "Us1",
  "Service": "my-maui-app",
  "TrackingConsent": "Granted",
  "Verbosity": "DEBUG",
  "FirstPartyHosts": [
    { "Match": "api.example.com", "HeaderTypes": ["Datadog", "TraceContext"] }
  ]
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
- `FirstPartyHosts` - List of first-party hosts for distributed tracing
- `NativeCrashReportEnabled` - Enable native iOS/Android crash reporting (default: `false`)
- `AdditionalConfiguration` - Additional configuration dictionary
- `ProxyConfiguration` - Proxy configuration object (see Proxy Configuration section)

### Tracking Consent

You can update the tracking consent at any time after initialization:

```csharp
// Update tracking consent at runtime (e.g., after user accepts a consent dialog)
DdSdk.SetTrackingConsent(TrackingConsent.Granted);
```

### Proxy Configuration

Route all SDK traffic through a proxy:

```csharp
using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;

DdSdk.Initialize(new DdSdkConfiguration
{
    ClientToken = "your-client-token",
    Environment = "prod",
    TrackingConsent = TrackingConsent.Granted,
    ProxyConfiguration = new ProxyConfiguration
    {
        Type = ProxyType.Http,
        Address = "proxy.example.com",
        Port = 8080,
        Username = "user",       // optional
        Password = "password"    // optional
    }
});
```

Supported proxy types: `Http`, `Https`, `Socks`. Authentication (username/password) is supported for HTTP and HTTPS proxies only.

File-based configuration:

```json
{
  "ClientToken": "your-client-token",
  "Environment": "prod",
  "ProxyConfiguration": {
    "Type": "Http",
    "Address": "proxy.example.com",
    "Port": 8080,
    "Username": "user",
    "Password": "password"
  }
}
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

### User Info

Set user information that is attached to all Datadog events:

```csharp
// Set user info (id is required, other fields optional)
DdSdk.SetUserInfo("user-123", "Jane Doe", "jane@example.com",
    new Dictionary<string, object> { { "plan", "premium" } });

// Add extra info to the current user (merges with existing)
DdSdk.AddUserExtraInfo(new Dictionary<string, object> { { "subscription", "annual" } });

// Read current user info (returns null if not set)
var user = DdSdk.GetUserInfo();

// Clear user info
DdSdk.ClearUserInfo();
```

### Account Info

Set account information that is attached to all Datadog events:

```csharp
// Set account info (id is required, other fields optional)
DdSdk.SetAccountInfo("acct-456", "Acme Corp",
    new Dictionary<string, object> { { "tier", "enterprise" } });

// Add extra info to the current account (merges with existing)
DdSdk.AddAccountExtraInfo(new Dictionary<string, object> { { "region", "us-east" } });

// Read current account info (returns null if not set)
var account = DdSdk.GetAccountInfo();

// Clear account info
DdSdk.ClearAccountInfo();
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
    NativeViewTracking = true,
    NativeInteractionTracking = true,
    VitalsUpdateFrequency = VitalsUpdateFrequency.Average
});

// Track views
DdRum.StartView("home_screen", "Home");
DdRum.StopView("home_screen");

// Track actions
DdRum.AddAction(RumActionType.Tap, "Login Button");
// Or for long-running actions:
DdRum.StartAction(RumActionType.Scroll, "Feed Scroll");
DdRum.StopAction(RumActionType.Scroll, "Feed Scroll");

// Track resources
DdRum.StartResource("api-call-1", RumResourceMethod.Get, "https://api.example.com/users");
DdRum.StopResource("api-call-1", 200, RumResourceKind.Xhr, 2048);

// Add custom timing
DdRum.AddTiming("time_to_interactive");

// Add view loading time
DdRum.AddViewLoadingTime(overwrite: false);

// Manage view attributes
// Call AddViewAttribute *after* the view has been started by the SDK. With
// automatic view tracking enabled, override OnNavigatedTo on your page (not
// the constructor or OnAppearing) — by then the SDK has already called
// StartView for the destination, so the attribute lands on the right view.
//
// protected override void OnNavigatedTo(NavigatedToEventArgs args)
// {
//     base.OnNavigatedTo(args);
//     DdRum.AddViewAttribute("screen_variant", "A");
// }
DdRum.AddViewAttribute("screen_variant", "A");
DdRum.RemoveViewAttribute("screen_variant");

// Track operations (e.g., checkout flow, file upload)
DdRum.StartOperation("checkout", operationKey: "op-1",
    new Dictionary<string, object> { { "step", "payment" } });
// On success:
DdRum.SucceedOperation("checkout", operationKey: "op-1");
// On failure:
DdRum.FailOperation("checkout", OperationFailure.Error, operationKey: "op-1",
    new Dictionary<string, object> { { "error_code", 500 } });

// Report errors
DdRum.AddError("Something went wrong", RumErrorSource.Source, "stacktrace here");

// Stop session
DdRum.StopSession();
```

When RUM is enabled, C# error tracking is automatically started. Unhandled exceptions (`AppDomain.UnhandledException`) and unobserved task exceptions (`TaskScheduler.UnobservedTaskException`) are captured and reported as RUM errors.

#### Error Event Mapper

Use `ErrorEventMapper` to modify or drop error events before they are sent to Datadog:

```csharp
DdRum.Enable(new DdRumConfiguration
{
    ApplicationId = "your-rum-application-id",
    ErrorEventMapper = errorEvent =>
    {
        // Add custom context
        errorEvent.Context["team"] = "mobile";

        // Drop errors matching a pattern (return null to discard)
        if (errorEvent.Message.Contains("ignore-this"))
            return null;

        // Modify the message
        errorEvent.Message = "[MyApp] " + errorEvent.Message;

        return errorEvent;
    }
});
```

The mapper receives a `DdRumErrorEvent` with `Message`, `Source`, `Stacktrace`, `Context`, and `TimestampMs` properties. It applies to all C# errors — both automatic (crash tracking) and manual (`DdRum.AddError`).

### Automatic Tracking

By default, the SDK automatically tracks:
- **Views**: MAUI page navigations via `Application.PageAppearing` (one app-level event covering Shell route changes, `Navigation.PushAsync`, and modals). For Shell apps, the destination route is resolved at `Shell.Navigating` time and used as the view name (e.g. `MainPage/DetailPage`).
- **Actions**: User interactions with buttons, switches, checkboxes, pickers, and gesture recognizers. `Button` and `ImageButton` taps fire on `Pressed` (not `Clicked`) so the action is recorded against the source view before any navigation triggered by a `Clicked` handler can shift the active view. A consequence is that an abandoned press (finger dragged off the button before release) is recorded as a tap.
- **Resources**: HTTP requests via DiagnosticListener (all HttpClient requests, including third-party libraries)

Automatic tracking is enabled by default. To customize or disable:

```csharp
DdRum.Enable(new DdRumConfiguration
{
    ApplicationId = "your-rum-application-id",

    // Disable automatic tracking
    AutomaticViewTracking = false,
    AutomaticActionTracking = false,
    AutomaticResourceTracking = false,

    // Or customize view names
    ViewNamePredicate = (page) => page switch
    {
        MainPage => "Home",
        _ => null  // use default name
    },

    // Skip specific pages
    ViewTrackingPredicate = (page) => page is not SplashPage,

    // Filter/modify auto-tracked actions
    ActionEventMapper = (action) =>
    {
        // Drop actions on debug buttons
        if (action.Name.Contains("Debug")) return null;
        return action;
    },

    // Filter/modify auto-tracked resources
    ResourceEventMapper = (resource) =>
    {
        // Drop analytics requests
        if (resource.Url.Contains("analytics")) return null;
        return resource;
    }
});
```

**View naming priority:** Custom `ViewNamePredicate` → resolved Shell route (forward navs and back navs both produce absolute paths like `MainPage/DetailPage`) → Page class name. Pages pushed via `Navigation.PushAsync` (which Shell internally assigns synthetic `D_FAULT_…` routes) fall through to the page class name.

**Setting view attributes from page code:** call `DdRum.AddViewAttribute(...)` from `OnNavigatedTo` (not the constructor or `OnAppearing`). The SDK's `StartView` for the destination has already fired by the time `OnNavigatedTo` runs, so the attribute attaches to the new view rather than the previous one.

**Known limitation — gesture-driven navigation:** `TapGestureRecognizer.Tapped` and `SwipeGestureRecognizer.Swiped` only fire on completion. If a tap/swipe handler triggers a navigation, the resulting action is bucketed under the destination view rather than the source. This applies only to `View`s with explicit gesture recognizers; `Button`/`ImageButton` taps are unaffected.

**Action target naming priority:** `AutomationId` → `StyleId` (x:Name) → control type name. Use `ActionEventMapper` to customize names further.

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
./check.sh --maui              # C# unit tests (xUnit, .NET 10)
./check.sh --maui --net9       # C# unit tests against .NET 9
./check.sh --ios               # Swift tests (XCTest)
./check.sh --android           # Kotlin tests (JUnit + MockK)
```

## Example App

Build and run the example app (defaults to .NET 10):

```bash
cd example
./build.sh --ios --run          # iOS, .NET 10
./build.sh --android --run      # Android, .NET 10
./build.sh --ios --run --net9   # iOS, .NET 9
./build.sh --android --run --net9  # Android, .NET 9
```

## Contributing

Pull requests are welcome. First, open an issue to discuss what you would like to change.

See [CONTRIBUTING.md](CONTRIBUTING.md) for the development setup guide.

## License

[Apache License, v2.0](LICENSE)
