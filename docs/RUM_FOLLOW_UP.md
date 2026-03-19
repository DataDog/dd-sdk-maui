# DdRum Module - Follow-Up Items

Items that are intentionally deferred from the initial DdRum configuration PR and need to be wired up in subsequent work.

## RUM-15107: Error Tracking / Crash Reporting

### What's already wired up
- **iOS**: `CrashReporting.enable()` from `DatadogCrashReporting` is called when `nativeCrashReportEnabled = true`. This correctly enables native crash reporting on iOS.
- **Android NDK**: `NdkCrashReports.enable()` from `dd-sdk-android-ndk` is called when `nativeCrashReportEnabled = true`. This enables C++/NDK crash tracking.
- **Android Java**: Java crash reporting is **always on** because the Android SDK defaults `setCrashReportsEnabled(true)` on the core `Configuration.Builder`. Setting `nativeCrashReportEnabled = false` does NOT disable Java crash reporting.

### Known limitation: Android Java crash reporting cannot be disabled post-init
- `setCrashReportsEnabled()` is only available on `Configuration.Builder` at core init time. Since MAUI's model calls `DdSdk.Initialize()` first and `DdRum.Enable()` later, we cannot retroactively disable it.
- The RN SDK doesn't have this problem because it processes core + RUM config in a single `initialize()` call, passing `nativeCrashReportEnabled` to `setCrashReportsEnabled()` before `Datadog.initialize()`.
- **Action needed in RUM-15107**: Either accept this behavior (document that Android Java crash reporting follows the SDK default), or add `NativeCrashReportEnabled` to `DdSdkConfiguration` so it can be set at core init time.

### Still needed
- C#-level error/crash handler for MAUI-specific exceptions (the main scope of RUM-15107).

## RUM-15184: Action, View, Resource Tracking API

- The `DdRum` class currently only has `Enable()`. The tracking API methods (startView, stopView, startAction, stopAction, addAction, startResource, stopResource, addError, addTiming, etc.) will be added in this ticket.

## Event Mappers (Stubs)

- `ErrorEventMapper`, `ResourceEventMapper`, and `ActionEventMapper` are accepted as `Func<object, object?>` on `DdRumConfiguration` but are **not wired to native**.
- Setting any of them logs a warning at `Enable()` time: "not yet supported and will be ignored."
- Implementing these requires a callback bridge from C# to native (non-trivial). The delegate types will also need to change from `Func<object, object?>` to proper RUM event types once they're defined.
- **Future ticket needed** to implement the full callback bridge.

## First-Party Hosts on Android

- On **iOS**, `firstPartyHosts` + `resourceTraceSampleRate` are fully wired to `urlSessionTracking` via `.traceWithHeaders(hostsWithHeaders:sampleRate:)`.
- On **Android**, `firstPartyHosts` configuration belongs at the **core `Configuration.Builder` level**, not on `RumConfiguration.Builder`. The `addFirstPartyHostsWithHeaderType` method does not exist on `RumConfiguration.Builder`.
- **Action needed**: When implementing resource tracking (RUM-15184), wire `firstPartyHosts` through the core `DatadogWrapper.initialize()` call on Android, or find the correct RUM-level API in the bumped SDK version.

## Initial Resource Threshold

- `initialResourceThreshold` is accepted in config and **stored as a static property** on both platforms (`DdRum.initialResourceThreshold`).
- On iOS, the RN SDK uses this with a `TimeBasedTNSResourcePredicate` resource event mapper. This is RN-specific logic that needs a MAUI equivalent.
- **Action needed**: Wire this into actual resource filtering when resource tracking is implemented (RUM-15184).

## Native SDK Version Bump (PR #12)

- Native SDK versions are defined in `versions.properties` and updated via `update-native-sdk.sh`.
- After a version bump, the RUM module may need API adjustments if any of these changed between versions:
  - `RumConfiguration.Builder` method signatures (Android)
  - `RUM.Configuration` property names/types (iOS)
  - `VitalsUpdateFrequency` package location (Android - currently in `com.datadog.android.rum.configuration`)
  - `TracingHeaderType` availability and enum values
- Any breaking changes will surface as **compile errors**, making them easy to catch and fix.
