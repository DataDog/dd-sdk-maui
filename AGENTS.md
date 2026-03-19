# AI Agent Navigation Guide - dd-sdk-maui

This document provides context and instructions for AI agents working on the Datadog SDK for .NET MAUI bindings. It explains the project architecture, common workflows, key patterns, and critical implementation details.

## Project Overview

**Purpose**: Provide .NET MAUI bindings for Datadog's native iOS and Android SDKs, enabling observability features (logs, RUM, traces) in cross-platform mobile applications.

**Current Status**: Phase 3 In Progress (Core SDK + Logs + RUM Configuration & Enablement)
- ✅ iOS native wrapper with XCFramework bindings
- ✅ Android native wrapper with multi-project NuGet bindings
- ✅ Unified meta-package (DatadogSdk.Maui)
- ✅ Full `DdSdkConfiguration` object (TrackingConsent, BatchSize, BatchProcessingLevel, UploadFrequency, Site, Service, Version/VersionSuffix, Verbosity, AdditionalConfiguration)
- ✅ Runtime `SetTrackingConsent` API
- ✅ `DdLogsConfiguration` with `CustomEndpoint` support
- ✅ `DdRumConfiguration` with full RUM parameter set (sampling, tracking, vitals, crash reporting, first-party hosts)
- ✅ `DdRum.Enable()` wired to native iOS (DatadogRUM + DatadogCrashReporting) and Android (dd-sdk-android-rum + dd-sdk-android-ndk)
- ✅ `_dd.needsClearTextHttp` internal key support via `AdditionalConfiguration`
- ✅ Unit tests at all three layers (`check.sh`)
- ✅ Example app validated on both platforms with mock local server
- ✅ Logs successfully reaching Datadog backend

**Architecture**: Multi-layer approach
```
Native SDK (dd-sdk-ios / dd-sdk-android)
    ↓
Native Wrapper (Swift / Kotlin)
    ↓
C# Bindings (Xamarin.iOS / Android bindings)
    ↓
Meta-package (DatadogSdk.Maui) - platform unification
    ↓
Consumer App (.NET MAUI)
```

## Critical Context for AI Agents

### Why This Architecture Exists

**Problem**: Datadog provides native SDKs (Swift for iOS, Kotlin for Android) but .NET MAUI needs C# bindings.

**Solution**: Three-layer approach
1. **Native Wrapper**: Thin Objective-C compatible layer (iOS) / JVM compatible layer (Android)
2. **C# Bindings**: Platform-specific bindings using Xamarin tooling
3. **Meta-package**: Single NuGet package for cross-platform consumption

**Why not bind directly?**
- dd-sdk-ios uses Swift, which isn't directly bindable to C# (needs Objective-C bridge)
- dd-sdk-android has complex Maven dependencies requiring separate binding projects
- Native wrappers provide stable, simplified APIs insulated from upstream changes

### Key Architectural Decisions

1. **iOS uses XCFramework approach**
   - Swift Package Manager builds native wrapper as dynamic library
   - XCFramework bundles device + simulator architectures
   - C# binding references XCFramework via `<NativeReference>`

2. **Android uses multi-project NuGet approach**
   - Core SDK split into 3 binding projects (Internal, Core, Logs)
   - Wrapper binding project consumes these as NuGet PackageReferences
   - This avoids duplicate type definitions from transitive AAR dependencies

3. **Local NuGet packages during development**
   - Bindings published to `./local-packages/` directory
   - NuGet.Config at root configures local package source
   - Enables rapid iteration without publishing to remote feed

## Directory Structure

```
dd-sdk-maui/
├── build.sh                       # Root build script (rebuilds everything)
├── check.sh                       # Runs all unit tests (iOS/Android/C#)
├── update-native-sdk.sh           # Bump native SDK versions across all files
├── resolve-android-deps.sh        # Resolve Android transitive deps via Gradle
├── verify-artifacts.sh            # Validate build artifacts + dependency alignment
├── bump-version.sh                # Bump MAUI SDK version across all files
├── versions.properties            # Single source of truth for all versions
├── android-transitive-deps.json   # Maven→NuGet dependency mapping for Android
├── NuGet.Config                   # Local package source configuration
│
├── native-wrappers/               # Platform-native code
│   ├── ios/
│   │   ├── build.sh              # Builds XCFramework
│   │   └── DatadogWrapper/       # Swift Package Manager project
│   │       ├── Package.swift     # SPM manifest (dd-sdk-ios dependency)
│   │       └── Sources/DatadogWrapper/
│   │           ├── DatadogWrapper.swift   # SDK initialization (DdSdkNativeWrapper)
│   │           ├── DdLogs.swift           # Logging API
│   │           ├── DdRum.swift            # RUM API
│   │           └── Protocols/             # Dependency injection protocols
│   │
│   └── android/
│       ├── build.gradle.kts      # Root Gradle project
│       ├── gradlew               # Gradle wrapper
│       └── datadogwrapper/       # Android library module
│           ├── build.gradle.kts  # Module config (dd-sdk-android deps)
│           └── src/
│               ├── main/kotlin/com/datadog/wrapper/
│               │   ├── DatadogWrapper.kt   # SDK initialization
│               │   ├── DdLogs.kt           # Logging API
│               │   └── DdRum.kt            # RUM API
│               └── test/kotlin/com/datadog/wrapper/
│                   ├── DatadogWrapperTest.kt
│                   ├── DdLogsTest.kt
│                   └── DdRumTest.kt
│
├── bindings/                      # C# binding projects
│   ├── DatadogSdk.iOS.Binding/
│   │   ├── ApiDefinition.Core.cs  # iOS core binding interface
│   │   ├── ApiDefinition.Logs.cs  # iOS logs binding interface
│   │   └── NativeReference/
│   │       └── DatadogWrapper.xcframework/   # XCFramework binary
│   │
│   ├── DatadogSdk.Android.Internal/    # dd-sdk-android-internal bindings
│   ├── DatadogSdk.Android.Core/        # dd-sdk-android-core bindings
│   ├── DatadogSdk.Android.Logs/        # dd-sdk-android-logs bindings
│   ├── DatadogSdk.Android.Rum/         # dd-sdk-android-rum AAR binding
│   ├── DatadogSdk.Android.Binding/     # Kotlin wrapper bindings
│   │   ├── Jars/
│   │   │   └── datadogwrapper-release.aar
│   │   └── Transforms/
│   │       ├── Metadata.xml      # Binding fixups
│   │       └── proguard.txt      # R8/ProGuard rules
│   │
│   └── DatadogSdk.Maui/          # Meta-package (unified)
│       ├── Configuration/         # Configuration namespace
│       │   ├── DdSdkConfiguration.cs
│       │   ├── FileBasedConfiguration.cs  # JSON config parser
│       │   ├── DdLogsConfiguration.cs  # Logs module configuration (CustomEndpoint)
│       │   ├── DdRumConfiguration.cs   # RUM module configuration
│       │   ├── VitalsUpdateFrequency.cs
│       │   ├── TracingHeaderType.cs
│       │   ├── DdFirstPartyHost.cs
│       │   ├── TrackingConsent.cs
│       │   ├── BatchSize.cs
│       │   ├── BatchProcessingLevel.cs
│       │   ├── UploadFrequency.cs
│       │   ├── DatadogSite.cs
│       │   └── SdkVerbosity.cs
│       ├── DdSdk.cs               # SDK initialization + SetTrackingConsent + BuildAdditionalConfiguration
│       ├── DdLogs.cs              # Logging API
│       ├── DdRum.cs               # RUM API
│       └── InternalLog.cs         # SDK-internal console logging
│
├── tests/                         # C# unit tests
│   └── DatadogSdk.Maui.Tests/
│       ├── DdSdkConfigurationTests.cs    # SDK init via test bridge (INativeBridge)
│       ├── DdLogsConfigurationTests.cs
│       ├── DdRumConfigurationTests.cs
│       ├── DdSdkConversionTests.cs       # ConvertSite, ConvertTrackingConsent,
│       │                                 # BuildAdditionalConfiguration
│       ├── FileBasedConfigurationTests.cs # JSON config parsing + validation
│       ├── MockNativeSdkBridge.cs        # Shared test double for DdSdk.INativeBridge
│       ├── InternalLogTests.cs
│       └── Fixtures/                     # JSON test fixtures
│           ├── full_config.json
│           ├── minimal_config.json
│           └── malformed_config.json
│
├── example/                       # Test/demo MAUI application
│   ├── build.sh                  # Example app build script
│   ├── MauiProgram.cs            # SDK initialization
│   ├── MainPage.xaml.cs          # Log sending UI
│   ├── Resources/Raw/appsettings.json  # ClientToken, Environment
│   └── Platforms/
│       └── Android/
│           ├── AndroidManifest.xml           # networkSecurityConfig reference
│           └── Resources/xml/
│               └── network_security_config.xml  # Allows cleartext HTTP
│
└── local-packages/                # Local NuGet package output
    ├── DatadogSdk.iOS.Binding.0.0.1.nupkg
    ├── DatadogSdk.Android.*.nupkg
    └── DatadogSdk.Maui.0.0.1.nupkg
```

## Common Workflows

### 1. Making Changes to Native Wrappers

**iOS:**
```bash
cd native-wrappers/ios/DatadogWrapper

# Edit Swift files
vim Sources/DatadogWrapper/DatadogWrapper.swift

# Build XCFramework
cd .. && ./build.sh

# XCFramework copied to bindings/DatadogSdk.iOS.Binding/NativeReference/
```

**Android:**
```bash
cd native-wrappers/android

# Edit Kotlin files
vim datadogwrapper/src/main/kotlin/com/datadog/wrapper/DatadogWrapper.kt

# Build AAR
./gradlew :datadogwrapper:assembleRelease

# Copy to bindings manually or use root build.sh
cp datadogwrapper/build/outputs/aar/datadogwrapper-release.aar \
   ../../bindings/DatadogSdk.Android.Binding/Jars/
```

### 2. Updating C# Bindings

**iOS binding changes:**
```bash
cd bindings/DatadogSdk.iOS.Binding

# Edit ApiDefinition.cs to match new native APIs
vim ApiDefinition.cs

# Rebuild binding
dotnet build -c Release
dotnet pack -c Release
cp bin/Release/*.nupkg ../../local-packages/
```

**Android binding changes:**
```bash
cd bindings/DatadogSdk.Android.Binding

# If needed, update Metadata.xml to fix binding issues
vim Transforms/Metadata.xml

# Rebuild
dotnet build -c Release
dotnet pack -c Release
cp bin/Release/*.nupkg ../../local-packages/
```

### 3. Full Rebuild (Most Common)

```bash
# From project root
./build.sh

# This rebuilds everything in order:
# 1. iOS native wrapper → XCFramework
# 2. Android native wrapper → AAR
# 3. iOS binding → NuGet
# 4. Android bindings (4 projects) → NuGet
# 5. Meta-package → NuGet
```

### 4. Testing Changes

```bash
# After rebuilding bindings
cd example

# Clean to force NuGet refresh
./build.sh --clean --ios --run

# Or for Android
./build.sh --clean --android --run
```

## Critical Implementation Details

### iOS Binding Specifics

**Objective-C Export Requirements:**
```swift
// Swift classes MUST be @objc and inherit from NSObject
@objc(DatadogWrapper)         // ObjC name stays DatadogWrapper for binding compatibility
public class DdSdkNativeWrapper: NSObject {

    // Mapping helpers (testable independently)
    static func mapSite(_ site: String) -> DatadogSite { ... }
    static func mapTrackingConsent(_ consent: String) -> TrackingConsent { ... }
    static func mapVerbosity(_ verbosity: String) -> CoreLoggerLevel { ... }
    static func mapBatchSize(_ batchSize: String) -> Datadog.Configuration.BatchSize { ... }
    static func mapUploadFrequency(_ freq: String) -> Datadog.Configuration.UploadFrequency { ... }
    static func mapBatchProcessingLevel(_ level: String) -> Datadog.Configuration.BatchProcessingLevel { ... }

    @objc public static func initialize(
        clientToken: String,
        environment: String,
        service: String?,     // nullable
        site: String,
        verbosity: String,
        trackingConsent: String,
        batchSize: String?,
        uploadFrequency: String?,
        batchProcessingLevel: String?,
        additionalConfiguration: NSDictionary?
    ) -> Bool { ... }
}
```

**C# Binding Syntax** (`ApiDefinition.Core.cs`):
```csharp
[BaseType(typeof(NSObject))]
interface DatadogWrapper
{
    [Static]
    [Export("initializeWithClientToken:environment:service:site:verbosity:trackingConsent:batchSize:uploadFrequency:batchProcessingLevel:additionalConfiguration:")]
    bool Initialize(string clientToken, string environment,
        [NullAllowed] string service, string site, string verbosity,
        string trackingConsent, [NullAllowed] string batchSize,
        [NullAllowed] string uploadFrequency, [NullAllowed] string batchProcessingLevel,
        [NullAllowed] NSDictionary additionalConfiguration);
}
```

**Export Selector Pattern:**
- Swift method: `initialize(clientToken:environment:service:site:verbosity:...)`
- Objective-C selector: `initializeWithClientToken:environment:service:site:verbosity:...`
- Each parameter label becomes part of the selector after the first

### Android Binding Specifics

**JvmStatic Requirement:**
```kotlin
class DatadogWrapper {
    companion object {
        // Mapping helpers (testable independently)
        @JvmStatic fun mapSite(site: String): DatadogSite = ...
        @JvmStatic fun mapTrackingConsent(consent: String): TrackingConsent = ...
        @JvmStatic fun mapVerbosity(verbosity: String): Int = ...  // returns Log.* constant
        @JvmStatic fun mapBatchSize(batchSize: String): BatchSize = ...
        @JvmStatic fun mapUploadFrequency(uploadFrequency: String): UploadFrequency = ...
        @JvmStatic fun mapBatchProcessingLevel(level: String): BatchProcessingLevel = ...

        @JvmStatic  // Critical: exposes as static method to C#
        fun initialize(
            context: Context,
            clientToken: String,
            environment: String,
            service: String?,           // nullable
            site: String = "us1",
            verbosity: String = "error",
            trackingConsent: String = "pending",
            batchSize: String? = null,
            uploadFrequency: String? = null,
            batchProcessingLevel: String? = null,
            additionalConfiguration: Map<String, Any>? = null
        ): Boolean { ... }
    }
}
```

**`_dd.needsClearTextHttp` support**: if `additionalConfiguration["_dd.needsClearTextHttp"] == true`, the wrapper calls `_InternalProxy.allowClearTextHttp(builder)` before building the configuration. Pair with a custom endpoint and `network_security_config.xml` allowing cleartext for local testing.

**Metadata.xml Transformations:**
```xml
<!-- Rename package to .NET convention -->
<attr path="/api/package[@name='com.datadog.wrapper']"
      name="managedName">DatadogSdk.Android.Binding</attr>

<!-- Remove duplicate types from transitive dependencies -->
<remove-node path="/api/package[starts-with(@name, 'com.datadog.android')]" />
```

**Dependency Management:**
- Core SDK split into separate binding projects
- Wrapper binding uses PackageReference (not ProjectReference)
- This prevents duplicate type definitions

**Android Transitive Dependencies:**

Android runtime dependencies (OkHttp, Gson, Kotlin, AndroidX) are declared in two places:
1. `AndroidMavenLibrary` entries in `DatadogSdk.Android.Core.csproj` — use Maven versions directly
2. `PackageReference` entries in multiple `.csproj` files — use NuGet versions (which may differ from Maven versions)

The mapping between Maven artifacts and NuGet packages is tracked in `android-transitive-deps.json`. Key fields:
- `maven_version`: what Gradle resolves for this artifact
- `nuget_version`: the NuGet package version in `.csproj` files
- `nuget_covers_maven`: max Maven version the NuGet package is known to satisfy

When bumping the native Android SDK, `resolve-android-deps.sh` automatically:
- Resolves the Gradle dependency tree
- Auto-updates `AndroidMavenLibrary` versions in csproj files
- Warns when a NuGet `PackageReference` needs manual review (Maven version exceeds `nuget_covers_maven`)

**iOS has no transitive dependency problem** — SPM statically links everything into the XCFramework.

### Parameter Naming Convention

**Current standard:** Use `service` (not `serviceName`)

```csharp
// iOS
DatadogWrapper.Initialize(
    clientToken: "token",
    environment: "prod",
    service: "my-app"
);

// Android
DatadogWrapper.Initialize(
    context: this,
    clientToken: "token",
    environment: "prod",
    service: "my-app",
    site: "us1"
);
```

## Build Scripts

### Root build.sh

**Purpose**: Rebuild all native wrappers and bindings in correct dependency order.

**Steps:**
1. iOS native wrapper (Swift) → XCFramework
2. Android native wrapper (Kotlin) → AAR
3. iOS C# binding → NuGet package
4. Android C# bindings (4 projects in order) → NuGet packages
5. Meta-package → NuGet package

**Output**: All NuGet packages in `./local-packages/`

### update-native-sdk.sh

**Purpose**: Bump native SDK versions across all files that reference them.

**Usage:**
```bash
./update-native-sdk.sh --ios 3.8.0 --android 3.8.0
```

**Files modified**: `versions.properties`, `Package.swift`, `build.gradle.kts`, Android binding `.csproj` files. For Android bumps, also runs `resolve-android-deps.sh` to handle transitive dependencies.

### resolve-android-deps.sh

**Purpose**: Resolve Android Gradle dependency tree and synchronize `android-transitive-deps.json` + `.csproj` files.

**Usage:**
```bash
./resolve-android-deps.sh           # Resolve + update files
./resolve-android-deps.sh --check   # Resolve + report only (no changes)
```

### verify-artifacts.sh

**Purpose**: Validate build artifacts — NuGet packages, ProGuard rules, and Android transitive dependency alignment.

**Usage:**
```bash
./verify-artifacts.sh           # Run all checks
./verify-artifacts.sh --deps    # Dependency alignment only
./verify-artifacts.sh --nuget   # NuGet packages only
```

### example/build.sh

**Purpose**: Build and run example app after bindings are ready.

**Options:**
- `--ios` / `--android`: Target platform
- `--run`: Launch on simulator/emulator after building
- `--clean`: Clean before building (forces NuGet refresh)

## Common Issues and Solutions

### Issue: Android build fails with "duplicate type" errors

**Cause**: Binding project references another binding as ProjectReference, causing transitive AAR dependencies to be bound twice.

**Solution**: Use PackageReference instead of ProjectReference in Android binding projects.

### Issue: iOS binding shows "selector not found" at runtime

**Cause**: Swift method not properly exported to Objective-C, or selector name mismatch.

**Solution**:
1. Verify `@objc` annotation on Swift class and methods
2. Check selector format in `[Export("...")]` matches Swift signature
3. Use `nm` to inspect XCFramework symbols: `nm -gU DatadogWrapper.framework/DatadogWrapper`

### Issue: Changes to bindings not reflected in example app

**Cause**: NuGet package cache holding old version.

**Solution**:
```bash
cd example
./build.sh --clean --ios
# Clean forces dotnet restore --force --no-cache
```

### Issue: Gradle build fails with "SDK location not found"

**Cause**: Missing `local.properties` or `ANDROID_HOME` environment variable.

**Solution**: Ensure Android SDK path is configured:
```bash
export ANDROID_HOME=$HOME/Library/Android/sdk
# Or create native-wrappers/android/local.properties:
# sdk.dir=/Users/username/Library/Android/sdk
```

## Testing Checklist

Before committing changes:

1. **Run unit tests**
   ```bash
   ./check.sh               # All suites
   ./check.sh --maui        # C# xUnit only
   ./check.sh --android     # Kotlin JUnit + MockK only
   ./check.sh --ios         # Swift XCTest only
   ```

2. **Build native wrappers**
   ```bash
   cd native-wrappers/ios && ./build.sh
   cd ../android && ./gradlew :datadogwrapper:assembleRelease
   ```

3. **Rebuild all bindings**
   ```bash
   cd ../.. && ./build.sh
   ```

4. **Test iOS**
   ```bash
   cd example && ./build.sh --clean --ios --run
   # Verify logs in Datadog UI
   ```

5. **Test Android**
   ```bash
   ./build.sh --clean --android --run
   # Verify logs in Datadog UI
   ```

6. **Check NuGet packages**
   ```bash
   ls -lh ../local-packages/
   # Verify timestamps are recent
   ```

## Useful Commands Reference

```bash
# Full rebuild
./build.sh

# Run all unit tests
./check.sh

# iOS build and run
cd example && ./build.sh --ios --run

# Android build and run
cd example && ./build.sh --android --run

# Check XCFramework symbols
nm -gU bindings/DatadogSdk.iOS.Binding/NativeReference/DatadogWrapper.xcframework/ios-arm64/DatadogWrapper.framework/DatadogWrapper

# Inspect Android AAR contents
unzip -l bindings/DatadogSdk.Android.Binding/Jars/datadogwrapper-release.aar

# View generated Android binding code
ls bindings/DatadogSdk.Android.Binding/obj/Release/net10.0-android/generated/src/

# Run C# unit tests directly
dotnet test tests/DatadogSdk.Maui.Tests/

# Run Android unit tests directly
cd native-wrappers/android && ./gradlew :datadogwrapper:test

# Run iOS unit tests directly
cd native-wrappers/ios/DatadogWrapper && swift test

# Clean all build artifacts
git clean -fdx -e local-packages -e .planning
```

## For New Contributors

1. Read `CONTRIBUTING.md` for setup instructions
2. Read `SPEC.md` for architecture deep-dive
3. Run `./build.sh` to verify everything builds
4. Run example app on both platforms
5. Make small changes and test iteratively

## Getting Help

- Look at git history for examples: `git log --oneline`
- Review test logs in example app platform initialization code
