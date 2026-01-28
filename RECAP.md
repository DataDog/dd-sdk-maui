# Datadog MAUI SDK - Technical Recap

## 1. Feature Comparison: Kyle's Bindings vs Our Implementation

### Overview

| Aspect | Kyle's Bindings | Our Implementation |
|--------|-----------------|-------------------|
| **Repository** | `kyletaylored/datadog-dotnet-mobile-sdk-bindings` | `maui/` (this repo) |
| **Approach** | Direct 1:1 bindings to native SDKs | Unified cross-platform abstraction |
| **Architecture** | Platform-specific APIs exposed directly | Single API that works on both platforms |
| **Integration** | Manual initialization in platform code | `MauiAppBuilder.UseDatadog()` extension |
| **Status** | Community/unofficial | Official Datadog SDK |

---

### Feature Matrix

| Feature | Kyle's iOS | Kyle's Android | Our iOS | Our Android |
|---------|-----------|----------------|---------|-------------|
| **Core SDK** |
| SDK Initialization | ✅ | ✅ | ✅ | ✅ |
| Site Configuration | ✅ US1/US3/US5/EU1/AP1/US1_FED | ✅ Same | ✅ Same + Staging | ✅ Same |
| Tracking Consent | ✅ Granted/NotGranted/Pending | ✅ Same | ✅ Same | ✅ Same |
| Batch Size | ✅ Small/Medium/Large | ✅ Same | ✅ Same | ✅ Same |
| Upload Frequency | ✅ Frequent/Average/Rare | ✅ Same | ✅ Same | ✅ Same |
| User Info (setUser) | ✅ | ✅ | ❌ | ❌ |
| Global Attributes | ✅ | ✅ | ❌ | ❌ |
| **RUM** |
| Enable RUM | ✅ | ✅ | ✅ | ✅ |
| Session Sample Rate | ✅ | ✅ | ✅ | ✅ |
| Manual View Tracking | ✅ StartView/StopView | ✅ Same | ✅ Same | ✅ Same |
| Auto View Tracking | ✅ UIKitViewsPredicate | ✅ TrackUserInteractions | ❌ | ❌ |
| Manual Action Tracking | ✅ AddAction/StartAction/StopAction | ✅ Same | ✅ AddAction only | ✅ Same |
| Auto Action Tracking | ✅ UIKitActionsPredicate | ✅ TrackUserInteractions | ❌ | ❌ |
| Resource Tracking | ✅ Start/Stop/Add Resource | ✅ Same | ✅ Start/Stop | ✅ Same |
| Error Tracking | ✅ AddError | ✅ Same | ✅ Same | ✅ Same |
| Long Task Tracking | ✅ LongTaskThreshold | ✅ TrackLongTasks | ❌ | ❌ |
| Frustration Tracking | ✅ TrackFrustrations | ✅ Same | ❌ | ❌ |
| Background Events | ✅ TrackBackgroundEvents | ✅ Same | ❌ | ❌ |
| RUM Attributes | ✅ Add/Remove | ✅ Same | ✅ via params | ✅ Same |
| **Logs** |
| Enable Logs | ✅ | ✅ | ❌ | ❌ |
| Logger Creation | ✅ Logger.Create() | ✅ Logger.Builder | ❌ | ❌ |
| Log Levels | ✅ Debug/Info/Notice/Warn/Error/Critical | ✅ D/I/W/E/Wtf | ❌ | ❌ |
| Log Attributes | ✅ | ✅ | ❌ | ❌ |
| Log Tags | ✅ | ✅ | ❌ | ❌ |
| Bundle with RUM | ✅ | ✅ | ❌ | ❌ |
| Network Info in Logs | ✅ | ✅ | ❌ | ❌ |
| **Trace/APM** |
| Enable Trace | ✅ | ✅ | ❌ | ❌ |
| Manual Spans | ✅ StartSpan/SetTag/Finish | ✅ Same | ❌ | ❌ |
| Trace Context Injection | ✅ B3/W3C/Datadog/OTel formats | ✅ Same | ❌ | ❌ |
| First-Party Hosts | ✅ | ✅ | ❌ | ❌ |
| URL Session Instrumentation | ✅ | N/A | ❌ | N/A |
| **Crash Reporting** |
| Native Crash Reporting | ✅ DDCrashReporter.Enable() | ✅ NdkCrashReports.Enable() | ✅ CrashReporting.enable() | ✅ NdkCrashReports.enable() |
| Managed Exception Handling | ❌ | ❌ | ✅ AppDomain + TaskScheduler | ✅ Same |
| **Session Replay** |
| Enable Session Replay | ✅ | ✅ | ❌ | ❌ |
| Start/Stop Recording | ✅ | ✅ | ❌ | ❌ |
| Sample Rate | ✅ | ✅ | ❌ | ❌ |
| Text Privacy | ✅ MaskAll/MaskSensitive/Allow | ✅ Same | ❌ | ❌ |
| Image Privacy | ✅ MaskAll/MaskNonBundled/None | ✅ Same | ❌ | ❌ |
| Touch Privacy | ✅ Show/Hide | ✅ Same | ❌ | ❌ |
| **WebView Tracking** |
| Enable WebView | ✅ WKWebView | ✅ WebView | ❌ | ❌ |
| Allowed Hosts | ✅ | ✅ | ❌ | ❌ |
| **HTTP Tracking** |
| Auto HTTP Instrumentation | ✅ URLSessionInstrumentation | ❌ Manual | ✅ DatadogHttpMessageHandler | ✅ Same |
| IHttpClientFactory | ❌ | ❌ | ✅ AddDatadogTracking() | ✅ Same |
| Excluded Hosts | ❌ | ❌ | ✅ | ✅ |
| **OpenTelemetry** |
| OTel API | ✅ Bcr.Otel.Api.iOS | ✅ Trace.Otel | ❌ | ❌ |

---

### Key Differences

#### 1. **API Design Philosophy**

**Kyle's Approach - Direct Bindings:**
```csharp
// iOS - requires platform-specific code
#if IOS
var config = new DDConfiguration("clientToken", "env");
config.Site = DDSite.Us1;
DDDatadog.Initialize(config, DDTrackingConsent.Granted);

var rumConfig = new DDRUMConfiguration("appId");
DDRUM.Enable(rumConfig);
#endif

#if ANDROID
var config = new DDConfiguration.Builder("clientToken", "env", "", "service")
    .UseSite(DatadogSite.Us1)
    .Build();
Datadog.Initialize(context, config, TrackingConsent.Granted);

var rumConfig = new RumConfiguration.Builder("appId").Build();
Rum.Enable(rumConfig);
#endif
```

**Our Approach - Unified API:**
```csharp
// Works on both platforms - no conditional compilation needed
builder.UseDatadog(new DatadogConfiguration
{
    ClientToken = "clientToken",
    Env = "production",
    Site = DatadogSite.US1,
    RumApplicationId = "appId"
});
```

#### 2. **Initialization Timing**

**Kyle's:** Manual - must be called in AppDelegate (iOS) or MainActivity (Android)
**Ours:** Automatic - hooks into MAUI lifecycle events

#### 3. **Type Safety**

**Kyle's:** Exposes native types (NSObject, NSDictionary, Java.Lang.Object)
**Ours:** Uses standard .NET types (Dictionary<string, object>)

#### 4. **HTTP Tracking**

**Kyle's:** iOS only via URLSessionInstrumentation, no .NET HttpClient support
**Ours:** .NET HttpClient via DelegatingHandler, works with IHttpClientFactory

#### 5. **Managed Exception Handling**

**Kyle's:** Not implemented - only native crashes
**Ours:** Full .NET exception capture via AppDomain.UnhandledException and TaskScheduler.UnobservedTaskException

---

### Kyle's Packages (NuGet)

**iOS:**
- `Bcr.Datadog.iOS.ObjC` - Core + Logs + RUM + Trace
- `Bcr.Datadog.iOS.CrashReporting`
- `Bcr.Datadog.iOS.SessionReplay`
- `Bcr.Datadog.iOS.WebViewTracking`
- `Bcr.Otel.Api.iOS`

**Android:**
- `Bcr.Datadog.Android.Core`
- `Bcr.Datadog.Android.Logs`
- `Bcr.Datadog.Android.Rum`
- `Bcr.Datadog.Android.Trace`
- `Bcr.Datadog.Android.Trace.Otel`
- `Bcr.Datadog.Android.Ndk`
- `Bcr.Datadog.Android.SessionReplay`
- `Bcr.Datadog.Android.SessionReplay.Material`
- `Bcr.Datadog.Android.WebView`

---

## 2. Our Implementation Architecture

### Three-Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    MAUI Application                          │
│                   (Your App Code)                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                 Datadog.Maui (C# - Unified API)             │
│  ┌───────────────┐ ┌───────────────┐ ┌───────────────────┐  │
│  │  DatadogSdk   │ │      Rum      │ │  CrashReporting   │  │
│  │  (singleton)  │ │  (singleton)  │ │     (static)      │  │
│  └───────────────┘ └───────────────┘ └───────────────────┘  │
│  ┌─────────────────────────────────────────────────────────┐│
│  │           Http/DatadogHttpMessageHandler                ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
            ┌─────────────────┴─────────────────┐
            ▼                                   ▼
┌───────────────────────────┐       ┌───────────────────────────┐
│     Platforms/iOS         │       │     Platforms/Android     │
│  (ObjC Runtime P/Invoke)  │       │    (Java Bindings)        │
│                           │       │                           │
│  - DatadogSdkiOS.cs       │       │  - DatadogSdkAndroid.cs   │
│  - RumiOS.cs              │       │  - RumAndroid.cs          │
│  - CrashReportingiOS.cs   │       │  - CrashReportingAndroid  │
└───────────────────────────┘       └───────────────────────────┘
            │                                   │
            ▼                                   ▼
┌───────────────────────────┐       ┌───────────────────────────┐
│   DatadogMauiWrapper      │       │   DatadogMauiWrapper      │
│   (Swift Static Library)  │       │   (Kotlin AAR)            │
│                           │       │                           │
│   libDatadogWrapper.a     │       │   datadogwrapper.aar      │
└───────────────────────────┘       └───────────────────────────┘
            │                                   │
            ▼                                   ▼
┌───────────────────────────┐       ┌───────────────────────────┐
│     dd-sdk-ios 2.22.0     │       │   dd-sdk-android 2.22.0   │
│                           │       │                           │
│  - DatadogCore            │       │  - dd-sdk-android-rum     │
│  - DatadogRUM             │       │  - dd-sdk-android-logs    │
│  - DatadogCrashReporting  │       │  - dd-sdk-android-ndk     │
└───────────────────────────┘       └───────────────────────────┘
```

### Initialization Flow

```
1. MauiProgram.cs
   │
   └── builder.UseDatadog(config)
       │
       └── MauiAppBuilderExtensions.ConfigureLifecycleEvents()
           │
           ├── iOS: WillFinishLaunching event
           │   │
           │   ├── DatadogSdk.Initialize(config)
           │   │   └── DDMauiWrapper.initialize(clientToken, env, site, ...)
           │   │
           │   ├── Rum.Enable(applicationId)
           │   │   └── DDMauiWrapper.enableRum(appId, sampleRate)
           │   │
           │   └── CrashReporting
           │       ├── EnableManagedExceptionHandling()
           │       │   ├── AppDomain.UnhandledException += handler
           │       │   └── TaskScheduler.UnobservedTaskException += handler
           │       │
           │       └── EnableNative()
           │           └── DDMauiWrapper.enableCrashReporting()
           │               └── CrashReporting.enable()
           │
           └── Android: OnCreate event
               │
               └── (Same flow with Android native calls)
```

### HTTP Tracking Flow

```
HttpClient Request
       │
       ▼
DatadogHttpMessageHandler.SendAsync()
       │
       ├── Check if URL should be skipped (Datadog endpoints, excluded hosts)
       │
       ├── Generate unique resourceKey: "{Method}_{Guid}"
       │
       ├── Rum.Instance.StartResource(key, method, url, attributes)
       │   └── Native: DDMauiWrapper.startResource(...)
       │
       ├── Start Stopwatch
       │
       ├── base.SendAsync() ──────► Actual HTTP Request
       │
       └── On Response:
           │
           ├── Success:
           │   └── Rum.Instance.StopResource(key, statusCode, size, attributes)
           │       └── Native: DDMauiWrapper.stopResource(...)
           │
           └── Error:
               └── Rum.Instance.StopResourceWithError(key, message, attributes)
                   └── Native: DDMauiWrapper.stopResourceWithError(...)
```

### Crash Reporting Flow

**Managed Exceptions (.NET):**
```
Unhandled Exception Thrown
         │
         ▼
AppDomain.CurrentDomain.UnhandledException event
         │
         ▼
Our Handler (CrashReporting.cs)
         │
         ├── Extract: message, stackTrace, exception type
         │
         └── Rum.Instance.AddError(
                 message,
                 RumErrorSource.Source,
                 stackTrace,
                 attributes: {
                     "error.kind": "UnhandledException",
                     "error.type": "System.NullReferenceException",
                     "error.is_terminating": true/false
                 }
             )
             │
             └── Sent to Datadog RUM as Error event
```

**Native Crashes (iOS/Android):**
```
Native Crash (SIGSEGV, SIGABRT, etc.)
         │
         ▼
PLCrashReporter (iOS) / NDK Crash Reporter (Android)
         │
         ├── Capture crash context, stack frames, registers
         │
         └── Save crash report to disk

         ═══════════ App Terminates ═══════════

Next App Launch:
         │
         ▼
SDK Initialization
         │
         ├── Detect pending crash report
         │
         └── Upload to Datadog Error Tracking
             │
             └── Appears in Error Tracking dashboard
                 with full symbolicated stack trace
```

---

## 3. Currently Supported Features

### ✅ Fully Implemented

| Feature | iOS | Android | API |
|---------|-----|---------|-----|
| SDK Initialization | ✅ | ✅ | `UseDatadog(config)` |
| Multi-site Support | ✅ | ✅ | `DatadogSite.US1/EU1/...` |
| Tracking Consent | ✅ | ✅ | `TrackingConsent.Granted/Pending/NotGranted` |
| Batch Configuration | ✅ | ✅ | `BatchSize`, `UploadFrequency` |
| RUM Views | ✅ | ✅ | `Rum.Instance.StartView()`, `StopView()` |
| RUM Actions | ✅ | ✅ | `Rum.Instance.AddAction()` |
| RUM Errors | ✅ | ✅ | `Rum.Instance.AddError()` |
| RUM Resources | ✅ | ✅ | `Rum.Instance.StartResource()`, `StopResource()` |
| HTTP Tracking | ✅ | ✅ | `DatadogHttpMessageHandler` |
| IHttpClientFactory | ✅ | ✅ | `.AddDatadogTracking()` |
| Native Crash Reporting | ✅ | ✅ | `CrashReporting.EnableNative()` |
| Managed Exceptions | ✅ | ✅ | `CrashReporting.EnableManagedExceptionHandling()` |
| Custom Attributes | ✅ | ✅ | `Dictionary<string, object>` on all methods |

### Configuration Options

```csharp
new DatadogConfiguration
{
    // Required
    ClientToken = "pub...",                    // Datadog client token
    Env = "production",                        // Environment name

    // Optional - Defaults shown
    Site = DatadogSite.US1,                    // Datadog site
    Service = null,                            // Service name (default: bundle ID)
    RumApplicationId = null,                   // RUM app ID (enables RUM)
    TrackingConsent = TrackingConsent.Pending, // Initial consent
    BatchSize = BatchSize.Medium,              // Upload batch size
    UploadFrequency = UploadFrequency.Average, // Upload frequency

    // Crash Reporting - NEW
    EnableCrashReporting = true,               // Native crash reporting
    CatchUnhandledExceptions = true,           // .NET exception handling
    TrackAppHangs = true                       // App hang detection
}
```

### HTTP Tracking Options

```csharp
// Option 1: Direct HttpClient
var client = new HttpClient(new DatadogHttpMessageHandler());

// Option 2: With options
var client = new HttpClient(new DatadogHttpMessageHandler(new DatadogHttpTrackingOptions
{
    ExcludedHosts = new[] { "internal-api.example.com" },
    TrackRequestHeaders = false,
    TrackResponseHeaders = false
}));

// Option 3: IHttpClientFactory
services.AddHttpClient<IMyService, MyService>()
    .AddDatadogTracking();

// Option 4: IHttpClientFactory with options
services.AddHttpClient<IMyService, MyService>()
    .AddDatadogTracking(options =>
    {
        options.ExcludedHosts = new[] { "localhost" };
    });
```

### 🚧 Not Yet Implemented (Planned)

| Feature | Priority | Notes |
|---------|----------|-------|
| **Logs** | High | Logger API, log levels, attributes |
| **Traces/APM** | High | Spans, distributed tracing |
| **User Info** | Medium | `SetUserInfo(id, name, email)` |
| **Global Attributes** | Medium | Attributes on all events |
| **Session Replay** | Medium | Privacy controls, recording |
| **Auto View Tracking** | Medium | MAUI navigation integration |
| **Auto Action Tracking** | Medium | Gesture recognition |
| **WebView Tracking** | Low | Hybrid app support |
| **OpenTelemetry** | Low | OTel integration |

---

## 4. File Structure

```
maui/
├── src/Datadog.Maui/
│   ├── Datadog.Maui.csproj          # Multi-target: net10.0-ios, net10.0-android
│   │
│   ├── # Shared API (cross-platform)
│   ├── DatadogConfiguration.cs       # Configuration record
│   ├── DatadogSdk.cs                 # SDK singleton (partial)
│   ├── IDatadogSdk.cs                # SDK interface
│   ├── Rum.cs                        # RUM singleton (partial)
│   ├── IRum.cs                       # RUM interface
│   ├── CrashReporting.cs             # Crash reporting (partial)
│   ├── Enums.cs                      # RumActionType, RumErrorSource, etc.
│   ├── MauiAppBuilderExtensions.cs   # UseDatadog() extension
│   │
│   ├── Http/
│   │   ├── DatadogHttpMessageHandler.cs      # DelegatingHandler
│   │   ├── DatadogHttpTrackingOptions.cs     # Options class
│   │   └── HttpClientBuilderExtensions.cs    # AddDatadogTracking()
│   │
│   └── Platforms/
│       ├── iOS/
│       │   ├── DatadogSdkiOS.cs              # iOS SDK implementation
│       │   ├── RumiOS.cs                     # iOS RUM + ObjC interop
│       │   └── CrashReportingiOS.cs          # iOS crash reporting
│       └── Android/
│           ├── DatadogSdkAndroid.cs          # Android SDK implementation
│           ├── RumAndroid.cs                 # Android RUM
│           └── CrashReportingAndroid.cs      # Android crash reporting
│
├── native/
│   ├── ios/
│   │   ├── DatadogWrapper/
│   │   │   ├── Package.swift                 # Swift Package (dd-sdk-ios deps)
│   │   │   └── Sources/DatadogWrapper/
│   │   │       └── DatadogMauiWrapper.swift  # ObjC-compatible wrapper
│   │   └── lib/iphonesimulator/
│   │       └── libDatadogWrapper.a           # Compiled static library
│   │
│   └── android/
│       └── datadogwrapper/
│           ├── build.gradle.kts              # Gradle (dd-sdk-android deps)
│           └── src/main/kotlin/com/datadog/maui/
│               └── DatadogMauiWrapper.kt     # JVM-static wrapper
│
└── MauiApp1/                                 # Test application
    ├── MauiProgram.cs                        # SDK initialization
    ├── MainPage.xaml                         # UI with test buttons
    └── MainPage.xaml.cs                      # Test handlers
```

---

## 5. Build Commands

### iOS

```bash
# Build MAUI app for iOS Simulator
cd /Users/roman.gaignault/dev/maui
dotnet build MauiApp1/MauiApp1.csproj -f net10.0-ios -c Debug

# Run on iOS Simulator
dotnet build MauiApp1/MauiApp1.csproj -f net10.0-ios -c Debug -t:Run \
  -p:_DeviceName=:v2:udid=7971D7C0-5021-4D67-B889-1DB6BD5F33EA

# Rebuild native iOS library (if Swift code changed)
cd native/ios/DatadogWrapper
swift package resolve
xcodebuild -scheme DatadogWrapper \
  -destination 'generic/platform=iOS Simulator' \
  -configuration Release \
  BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
  clean build

# Create static library
libtool -static -o ../lib/iphonesimulator/libDatadogWrapper.a \
  ~/Library/Developer/Xcode/DerivedData/DatadogWrapper-*/Build/Products/Release-iphonesimulator/*.o
```

### Android

```bash
# Build MAUI app for Android
dotnet build MauiApp1/MauiApp1.csproj -f net10.0-android -c Debug

# Rebuild native Android library (if Kotlin code changed)
cd native/android
./gradlew :datadogwrapper:assembleRelease
```

---

*Last updated: January 23, 2026*
