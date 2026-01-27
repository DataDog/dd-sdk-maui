# Technical Specification - Datadog SDK for .NET MAUI

## Overview

This document provides a comprehensive technical specification for the Datadog SDK .NET MAUI bindings. It describes the architecture, implementation details, build process, and design decisions.

**Version**: 1.0.0 (Phase 1 Complete)
**Target Frameworks**: net10.0-ios, net10.0-android
**Status**: Foundation complete, Logs module functional

## Project Goals

1. **Provide native Datadog SDK functionality to .NET MAUI applications**
   - Expose iOS (Swift) and Android (Kotlin) SDKs to C#
   - Support observability features: Logs, RUM, Traces, Crash Reporting

2. **Maintain platform parity**
   - Feature parity between iOS and Android implementations
   - Consistent API surface across platforms

3. **Enable cross-platform development**
   - Single NuGet package for unified consumption
   - Platform-specific initialization while sharing core API

4. **Follow native SDK architecture**
   - Minimize abstractions to stay close to upstream SDKs
   - Preserve native SDK patterns and terminology

## Architecture

### Three-Layer Architecture

```
┌─────────────────────────────────────────────────┐
│         Consumer Application (.NET MAUI)        │
│              example/example.csproj             │
└─────────────────────┬───────────────────────────┘
                      │ PackageReference
                      ↓
┌─────────────────────────────────────────────────┐
│          Meta-package (DatadogSdk.Maui)         │
│     bindings/DatadogSdk.Maui/...csproj          │
│                                                 │
│  - Aggregates platform-specific bindings        │
│  - Single entry point for consumers             │
└──────────────┬──────────────────┬───────────────┘
               │                  │
       iOS     │                  │     Android
               ↓                  ↓
┌──────────────────────┐  ┌──────────────────────┐
│   iOS C# Binding     │  │  Android C# Bindings │
│  (Xamarin.iOS)       │  │  (Android Bindings)  │
│                      │  │                      │
│  - ApiDefinition.cs  │  │  - 4 NuGet projects  │
│  - StructsAndEnums   │  │    (Internal, Core,  │
│                      │  │     Logs, Wrapper)   │
└──────────┬───────────┘  └──────────┬───────────┘
           │                         │
           │ References              │ References
           │ XCFramework             │ AAR
           ↓                         ↓
┌──────────────────────┐  ┌──────────────────────┐
│  iOS Native Wrapper  │  │ Android Native Wrap. │
│  (Swift + ObjC)      │  │  (Kotlin + JVM)      │
│                      │  │                      │
│  - DatadogWrapper    │  │  - DatadogWrapper    │
│  - LogsWrapper       │  │  - LogsWrapper       │
└──────────┬───────────┘  └──────────┬───────────┘
           │                         │
           │ Imports                 │ Imports
           ↓                         ↓
┌──────────────────────┐  ┌──────────────────────┐
│    dd-sdk-ios        │  │   dd-sdk-android     │
│  (Native Swift SDK)  │  │  (Native Kotlin SDK) │
└──────────────────────┘  └──────────────────────┘
```

### Why This Architecture?

**Challenge**: Datadog provides native SDKs, but .NET MAUI requires C# APIs.

**Solution**: Multi-layer approach with clear separation of concerns:

1. **Native Wrappers**: Thin compatibility layer
   - iOS: Swift code with `@objc` annotations (Objective-C bridge)
   - Android: Kotlin code with `@JvmStatic` annotations
   - Simplifies native APIs for easier C# binding

2. **C# Bindings**: Platform-specific interop
   - iOS: Xamarin.iOS binding (ApiDefinition.cs)
   - Android: Android Java Bindings (auto-generated from AAR)
   - Handles marshaling between .NET and native types

3. **Meta-package**: Platform unification
   - Single NuGet package references both platform bindings
   - Conditional compilation based on target framework
   - Consumer apps only need one package reference

## iOS Implementation

### Native Wrapper (Swift)

**Location**: `native-wrappers/ios/DatadogWrapper/`

**Technology Stack**:
- Swift 5.9+
- Swift Package Manager
- dd-sdk-ios 3.5.0+ (Swift Package dependency)

**Key Files**:
```
DatadogWrapper/
├── Package.swift                    # SPM manifest
└── Sources/DatadogWrapper/
    ├── DatadogWrapper.swift         # SDK initialization
    └── LogsWrapper.swift            # Logging API
```

**Package.swift**:
```swift
let package = Package(
    name: "DatadogWrapper",
    platforms: [.iOS(.v12)],
    products: [
        .library(name: "DatadogWrapper", type: .dynamic, targets: ["DatadogWrapper"])
    ],
    dependencies: [
        .package(url: "https://github.com/DataDog/dd-sdk-ios.git", from: "3.5.0")
    ],
    targets: [
        .target(
            name: "DatadogWrapper",
            dependencies: [
                .product(name: "DatadogCore", package: "dd-sdk-ios"),
                .product(name: "DatadogLogs", package: "dd-sdk-ios")
            ],
            path: "Sources/DatadogWrapper"
        )
    ]
)
```

**Objective-C Bridge Requirements**:

All Swift classes must:
1. Be marked with `@objc(ClassName)`
2. Inherit from `NSObject`
3. Mark methods as `@objc public static`

Example:
```swift
@objc(DatadogWrapper)
public class DatadogWrapper: NSObject {

    @objc public static func initialize(
        clientToken: String,
        environment: String,
        service: String,
        site: String
    ) -> Bool {
        let datadogSite: DatadogSite
        switch site.lowercased() {
        case "us1": datadogSite = .us1
        case "us3": datadogSite = .us3
        case "us5": datadogSite = .us5
        case "eu1": datadogSite = .eu1
        case "ap1": datadogSite = .ap1
        case "us1_fed": datadogSite = .us1_FED
        default: datadogSite = .us1
        }

        let configuration = Datadog.Configuration(
            clientToken: clientToken,
            env: environment,
            site: datadogSite
        )

        Datadog.initialize(
            with: configuration,
            trackingConsent: .granted
        )

        Logs.enable()

        return true
    }
}
```

### XCFramework Build Process

**Script**: `native-wrappers/ios/build.sh`

**Steps**:
1. Clean previous builds (`.build`, `.swiftpm`, `build/`)
2. Build for iOS device (arm64):
   ```bash
   swift build -c release \
     --triple arm64-apple-ios \
     --sdk $(xcrun --sdk iphoneos --show-sdk-path)
   ```
3. Build for iOS simulator (arm64 + x86_64):
   ```bash
   swift build -c release \
     --triple arm64-apple-ios-simulator \
     --sdk $(xcrun --sdk iphonesimulator --show-sdk-path)

   swift build -c release \
     --triple x86_64-apple-ios-simulator \
     --sdk $(xcrun --sdk iphonesimulator --show-sdk-path)
   ```
4. Create fat binary for simulator architectures:
   ```bash
   lipo -create \
     .build/arm64-apple-ios-simulator/release/libDatadogWrapper.dylib \
     .build/x86_64-apple-ios-simulator/release/libDatadogWrapper.dylib \
     -output simulator/libDatadogWrapper.dylib
   ```
5. Create framework structure for each architecture
6. Generate XCFramework:
   ```bash
   xcodebuild -create-xcframework \
     -framework device/DatadogWrapper.framework \
     -framework simulator/DatadogWrapper.framework \
     -output build/DatadogWrapper.xcframework
   ```
7. Copy XCFramework to bindings directory:
   ```bash
   cp -R build/DatadogWrapper.xcframework \
     ../../bindings/DatadogSdk.iOS.Binding/NativeReference/
   ```

**Output**: `DatadogWrapper.xcframework` with structure:
```
DatadogWrapper.xcframework/
├── Info.plist
├── ios-arm64/
│   └── DatadogWrapper.framework/
│       ├── DatadogWrapper (binary)
│       └── Info.plist
└── ios-arm64_x86_64-simulator/
    └── DatadogWrapper.framework/
        ├── DatadogWrapper (binary)
        ├── Info.plist
        └── _CodeSignature/CodeResources
```

### iOS C# Binding

**Location**: `bindings/DatadogSdk.iOS.Binding/`

**Technology**: Xamarin.iOS binding project

**Key Files**:
- `ApiDefinition.cs` - Interface definitions for native APIs
- `StructsAndEnums.cs` - Supporting types
- `DatadogSdk.iOS.Binding.csproj` - Project file with NativeReference

**ApiDefinition.cs Structure**:
```csharp
using System;
using Foundation;
using ObjCRuntime;

namespace DatadogSdk.iOS.Binding
{
    [BaseType(typeof(NSObject))]
    interface DatadogWrapper
    {
        [Static]
        [Export("initializeWithClientToken:environment:service:site:")]
        bool Initialize(string clientToken, string environment, string service, string site);

        [Static]
        [Export("initializeWithClientToken:environment:service:")]
        bool Initialize(string clientToken, string environment, string service);
    }

    [BaseType(typeof(NSObject))]
    interface LogsWrapper
    {
        [Static]
        [Export("enableLogs")]
        void EnableLogs();

        [Static]
        [Export("logDebug:")]
        void LogDebug(string message);

        [Static]
        [Export("logInfo:")]
        void LogInfo(string message);

        [Static]
        [Export("logWarn:")]
        void LogWarn(string message);

        [Static]
        [Export("logError:")]
        void LogError(string message);
    }
}
```

**Export Selector Mapping**:
- Swift: `initialize(clientToken:environment:service:site:)`
- Objective-C: `initializeWithClientToken:environment:service:site:`
- C#: `Initialize(string, string, string, string)`

Pattern: First parameter name becomes method name, subsequent become part of selector.

**Project Configuration**:
```xml
<ItemGroup>
  <NativeReference Include="NativeReference\DatadogWrapper.xcframework">
    <Kind>Framework</Kind>
    <ForceLoad>True</ForceLoad>
    <SmartLink>True</SmartLink>
  </NativeReference>
</ItemGroup>
```

## Android Implementation

### Native Wrapper (Kotlin)

**Location**: `native-wrappers/android/datadogwrapper/`

**Technology Stack**:
- Kotlin 1.9+
- Gradle 8.10+
- dd-sdk-android 3.5.0 (Maven dependencies)

**Key Files**:
```
datadogwrapper/
├── build.gradle.kts                 # Module configuration
└── src/main/kotlin/com/datadog/wrapper/
    ├── DatadogWrapper.kt            # SDK initialization
    └── LogsWrapper.kt               # Logging API
```

**build.gradle.kts**:
```kotlin
plugins {
    id("com.android.library")
    kotlin("android")
}

android {
    namespace = "com.datadog.wrapper"
    compileSdk = 36

    defaultConfig {
        minSdk = 23
        targetSdk = 36
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles("consumer-rules.pro")
        }
    }
}

dependencies {
    implementation("com.datadoghq:dd-sdk-android-core:3.5.0")
    implementation("com.datadoghq:dd-sdk-android-logs:3.5.0")
}
```

**JvmStatic Requirement**:

Kotlin companion object methods must use `@JvmStatic` to expose as static methods:

```kotlin
package com.datadog.wrapper

import android.content.Context
import com.datadog.android.Datadog
import com.datadog.android.DatadogSite
import com.datadog.android.core.configuration.Configuration
import com.datadog.android.privacy.TrackingConsent

class DatadogWrapper {
    companion object {
        @JvmStatic
        fun initialize(
            context: Context,
            clientToken: String,
            environment: String,
            service: String,
            site: String = "us1"
        ): Boolean {
            return try {
                val datadogSite = when (site.lowercase()) {
                    "us1" -> DatadogSite.US1
                    "us3" -> DatadogSite.US3
                    "us5" -> DatadogSite.US5
                    "eu1" -> DatadogSite.EU1
                    "ap1" -> DatadogSite.AP1
                    "us1_fed" -> DatadogSite.US1_FED
                    else -> DatadogSite.US1
                }

                val configuration = Configuration.Builder(
                    clientToken = clientToken,
                    env = environment,
                    service = service
                )
                    .useSite(datadogSite)
                    .build()

                Datadog.initialize(context, configuration, TrackingConsent.GRANTED)
                true
            } catch (e: Exception) {
                e.printStackTrace()
                false
            }
        }
    }
}
```

### AAR Build Process

**Command**: `./gradlew :datadogwrapper:assembleRelease`

**Output**: `datadogwrapper/build/outputs/aar/datadogwrapper-release.aar`

**AAR Structure**:
```
datadogwrapper-release.aar (ZIP archive)
├── AndroidManifest.xml
├── classes.jar                      # Compiled Kotlin code
├── R.txt                           # Resources
└── libs/                           # Transitive dependencies (embedded)
```

### Android C# Bindings

**Challenge**: dd-sdk-android has complex Maven dependency tree. Binding the wrapper AAR directly would also bind all transitive dependencies, causing duplicate type definitions.

**Solution**: Multi-project approach

**Projects** (in dependency order):
1. **DatadogSdk.Android.Internal** - Binds `dd-sdk-android-internal-3.5.0.aar`
2. **DatadogSdk.Android.Core** - Binds `dd-sdk-android-core-3.5.0.aar`
3. **DatadogSdk.Android.Logs** - Binds `dd-sdk-android-logs-3.5.0.aar`
4. **DatadogSdk.Android.Binding** - Binds `datadogwrapper-release.aar`

**Key Pattern**: Core SDK bindings are **PackageReferences**, not ProjectReferences.

**DatadogSdk.Android.Internal.csproj**:
```xml
<ItemGroup>
  <EmbeddedJar Include="Jars\dd-sdk-android-internal-3.5.0.aar" />
</ItemGroup>
```

**DatadogSdk.Android.Core.csproj**:
```xml
<ItemGroup>
  <PackageReference Include="DatadogSdk.Android.Internal" Version="1.0.0" />
  <EmbeddedJar Include="Jars\dd-sdk-android-core-3.5.0.aar" />
</ItemGroup>
```

**DatadogSdk.Android.Binding.csproj**:
```xml
<ItemGroup>
  <!-- Core SDK as NuGet packages -->
  <PackageReference Include="DatadogSdk.Android.Internal" Version="1.0.0" />
  <PackageReference Include="DatadogSdk.Android.Core" Version="1.0.0" />
  <PackageReference Include="DatadogSdk.Android.Logs" Version="1.0.0" />

  <!-- Wrapper AAR -->
  <EmbeddedJar Include="Jars\datadogwrapper-release.aar" />
</ItemGroup>
```

**Metadata.xml Transformations**:

Remove duplicate types from transitive dependencies:
```xml
<metadata>
  <!-- Rename package to .NET convention -->
  <attr path="/api/package[@name='com.datadog.wrapper']"
        name="managedName">DatadogSdk.Android.Binding</attr>

  <!-- Remove dd-sdk-android packages (already bound separately) -->
  <remove-node path="/api/package[starts-with(@name, 'com.datadog.android')]" />
  <remove-node path="/api/package[starts-with(@name, 'com.lyft.kronos')]" />
</metadata>
```

### Generated C# Code

Android bindings auto-generate C# from Java/Kotlin bytecode.

**Example Output** (`obj/Release/net10.0-android/generated/src/DatadogSdk.Android.Binding.DatadogWrapper.cs`):
```csharp
namespace DatadogSdk.Android.Binding {

    [Register("com/datadog/wrapper/DatadogWrapper", DoNotGenerateAcw=true)]
    public sealed partial class DatadogWrapper : Java.Lang.Object {

        [Register("initialize",
                  "(Landroid/content/Context;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;)Z",
                  "")]
        public static unsafe bool Initialize(
            Android.Content.Context context,
            string clientToken,
            string environment,
            string service,
            string site)
        {
            // JNI marshaling implementation
        }
    }
}
```

## Meta-package (DatadogSdk.Maui)

**Location**: `bindings/DatadogSdk.Maui/`

**Purpose**: Unified NuGet package for .NET MAUI applications

**Structure**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0-ios;net10.0-android</TargetFrameworks>
    <PackageId>DatadogSdk.Maui</PackageId>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <!-- iOS binding -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0-ios'">
    <PackageReference Include="DatadogSdk.iOS.Binding" Version="1.0.0" />
  </ItemGroup>

  <!-- Android bindings -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
    <PackageReference Include="DatadogSdk.Android.Internal" Version="1.0.0" />
    <PackageReference Include="DatadogSdk.Android.Core" Version="1.0.0" />
    <PackageReference Include="DatadogSdk.Android.Logs" Version="1.0.0" />
    <PackageReference Include="DatadogSdk.Android.Binding" Version="1.0.0" />
  </ItemGroup>
</Project>
```

**Consumer Usage**:
```xml
<!-- Consumer app only needs one package -->
<ItemGroup>
  <PackageReference Include="DatadogSdk.Maui" Version="1.0.0" />
</ItemGroup>
```

## Build System

### Root Build Script (`./build.sh`)

**Purpose**: Complete rebuild of all components in dependency order

**Execution Flow**:
```
1. iOS Native Wrapper
   └─> Swift Package Manager → XCFramework

2. Android Native Wrapper
   └─> Gradle → AAR

3. iOS C# Binding
   └─> dotnet build + pack → NuGet (.nupkg)

4. Android C# Bindings (sequential)
   ├─> DatadogSdk.Android.Internal → NuGet
   ├─> DatadogSdk.Android.Core → NuGet
   ├─> DatadogSdk.Android.Logs → NuGet
   └─> DatadogSdk.Android.Binding → NuGet

5. Meta-package
   └─> DatadogSdk.Maui → NuGet
```

**Output**: All NuGet packages in `./local-packages/`

### Example Build Script (`./example/build.sh`)

**Purpose**: Build and run example application

**Options**:
- `--ios` - Build for iOS (default)
- `--android` - Build for Android
- `--run` - Launch on simulator/emulator
- `--clean` - Clean and force NuGet restore

**Internal Steps**:
1. Clean (if `--clean` flag)
2. `dotnet restore --force --no-cache`
3. `dotnet build -f net10.0-{platform}`
4. If `--run`: `dotnet build -t:Run -f net10.0-{platform}`

## Local Development Workflow

### NuGet.Config

**Location**: Project root

**Purpose**: Configure local package source during development

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local-packages" value="./local-packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### Package Version Management

**Current Strategy**: All packages use version `1.0.0` during development

**Incrementing Versions**:
1. Update version in each binding `.csproj` file
2. Rebuild: `./build.sh`
3. Update meta-package dependencies to match
4. Example app will automatically pick up new version

**Future**: Version management will be automated in CI/CD pipeline

## API Surface (Current - Phase 1)

### Initialization

**iOS**:
```csharp
using DatadogSdk.iOS.Binding;

// In AppDelegate.cs
public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
{
    var initialized = DatadogWrapper.Initialize(
        clientToken: "pub...",
        environment: "prod",
        service: "my-app",
        site: "us1"
    );

    return base.FinishedLaunching(application, launchOptions);
}
```

**Android**:
```csharp
using DatadogSdk.Android.Binding;

// In MainActivity.cs
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    var initialized = DatadogWrapper.Initialize(
        context: this,
        clientToken: "pub...",
        environment: "prod",
        service: "my-app",
        site: "us1"
    );
}
```

### Logging

**iOS**:
```csharp
using DatadogSdk.iOS.Binding;

LogsWrapper.EnableLogs();
LogsWrapper.LogDebug("Debug message");
LogsWrapper.LogInfo("Info message");
LogsWrapper.LogWarn("Warning message");
LogsWrapper.LogError("Error message");
```

**Android**:
```csharp
using DatadogSdk.Android.Binding;

LogsWrapper.EnableLogs();
LogsWrapper.LogDebug("Debug message");
LogsWrapper.LogInfo("Info message");
LogsWrapper.LogWarn("Warning message");
LogsWrapper.LogError("Error message");
```

## Known Limitations

### Phase 1 Limitations

1. **Platform-specific initialization required**
   - iOS: Initialize in AppDelegate
   - Android: Initialize in MainActivity
   - No unified cross-platform API yet (coming in Phase 2)

2. **No configuration builder**
   - Must pass all parameters directly to Initialize
   - No fluent API for optional settings (Phase 2)

3. **Logs only**
   - RUM, Traces, Crash Reporting not yet implemented
   - Planned for Phases 3-6

4. **No session management**
   - Session IDs not unified across modules
   - Planned for Phase 2

5. **No privacy controls API**
   - TrackingConsent always set to GRANTED
   - User-controllable privacy planned for Phase 2

6. **iOS and Android only**
   - No macOS, Windows support (platform bindings don't exist)
   - Mac Catalyst possible future addition

7. **Fixed site configuration**
   - Site must be specified at initialization
   - No runtime site switching

### Technical Constraints

1. **XCFramework size**
   - iOS XCFramework ~11MB (includes dd-sdk-ios)
   - Includes both device + simulator architectures
   - App store submissions use device-only slice

2. **Android APK size**
   - dd-sdk-android dependencies add ~2-3MB
   - R8/ProGuard minification reduces impact

3. **Minimum OS versions**
   - iOS: 12.0+ (dd-sdk-ios requirement)
   - Android: API 23+ (dd-sdk-android requirement)

## Roadmap

### Phase 2: Core SDK & Documentation (Planned)
- Unified initialization API
- Configuration builder with fluent API
- Session management infrastructure
- Privacy controls (TrackingConsent)
- Comprehensive documentation

### Phase 3: RUM Foundation (Planned)
- RUM module initialization
- View tracking
- Action tracking
- Resource tracking

### Phase 4: Error & Crash Reporting (Planned)
- Error tracking
- Crash reporting integration
- Custom error reporting

### Phase 5: Tracing & APM (Planned)
- Distributed tracing
- Network request tracing
- Custom span creation

### Phase 6: Advanced Features (Planned)
- Global context attributes
- User identification
- Feature flags integration

### Phase 7: Testing & Polish (Planned)
- Integration tests
- Performance benchmarks
- Documentation finalization

## Testing Strategy

### Current Testing Approach

**Manual Validation**:
1. Build example app
2. Run on iOS simulator
3. Run on Android emulator
4. Verify logs appear in Datadog UI

**Validation Steps**:
```bash
# Build bindings
./build.sh

# Test iOS
cd example && ./build.sh --ios --run
# Check Datadog Logs Explorer: service:example env:dev

# Test Android
./build.sh --android --run
# Check Datadog Logs Explorer: service:example env:dev
```

### Future Testing (Phase 7)

- Unit tests for binding layer
- Integration tests with mock Datadog backend
- E2E tests on real devices
- Performance benchmarks
- Memory leak detection

## Contributing

See `CONTRIBUTING.md` for:
- Development environment setup
- Build instructions
- Testing procedures
- Pull request guidelines

## Versioning

**Current**: 1.0.0 (Phase 1 Complete)

**Strategy**: Semantic Versioning (SemVer)
- Major: Breaking API changes
- Minor: New features (backward compatible)
- Patch: Bug fixes

**Pre-release**: Alpha/Beta tags during development
- Example: `1.1.0-alpha.1`, `1.1.0-beta.2`

## License

[License information to be added]

## References

- [dd-sdk-ios](https://github.com/DataDog/dd-sdk-ios) - Native iOS SDK
- [dd-sdk-android](https://github.com/DataDog/dd-sdk-android) - Native Android SDK
- [Datadog Documentation](https://docs.datadoghq.com/mobile/)
- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [Xamarin.iOS Binding Documentation](https://learn.microsoft.com/en-us/xamarin/ios/platform/binding-objective-c/)
- [Android Java Binding Documentation](https://learn.microsoft.com/en-us/xamarin/android/platform/binding-java-library/)
