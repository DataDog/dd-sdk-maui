# AI Agent Navigation Guide - dd-sdk-maui

This document provides context and instructions for AI agents working on the Datadog SDK for .NET MAUI bindings. It explains the project architecture, common workflows, key patterns, and critical implementation details.

## Project Overview

**Purpose**: Provide .NET MAUI bindings for Datadog's native iOS and Android SDKs, enabling observability features (logs, RUM, traces) in cross-platform mobile applications.

**Current Status**: Phase 1 Complete (Foundation & Bindings + Logs)
- ✅ iOS native wrapper with XCFramework bindings
- ✅ Android native wrapper with multi-project NuGet bindings
- ✅ Unified meta-package (DatadogSdk.Maui)
- ✅ Example app validated on both platforms
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
├── NuGet.Config                   # Local package source configuration
│
├── native-wrappers/               # Platform-native code
│   ├── ios/
│   │   ├── build.sh              # Builds XCFramework
│   │   └── DatadogWrapper/       # Swift Package Manager project
│   │       ├── Package.swift     # SPM manifest (dd-sdk-ios dependency)
│   │       └── Sources/DatadogWrapper/
│   │           ├── DatadogWrapper.swift   # SDK initialization
│   │           └── DdLogs.swift      # Logging API
│   │
│   └── android/
│       ├── build.gradle.kts      # Root Gradle project
│       ├── gradlew               # Gradle wrapper
│       └── datadogwrapper/       # Android library module
│           ├── build.gradle.kts  # Module config (dd-sdk-android deps)
│           └── src/main/kotlin/com/datadog/wrapper/
│               ├── DatadogWrapper.kt   # SDK initialization
│               └── DdLogs.kt      # Logging API
│
├── bindings/                      # C# binding projects
│   ├── DatadogSdk.iOS.Binding/
│   │   ├── ApiDefinition.cs      # iOS binding interface definitions
│   │   ├── StructsAndEnums.cs    # Supporting types
│   │   └── NativeReference/
│   │       └── DatadogWrapper.xcframework/   # XCFramework binary
│   │
│   ├── DatadogSdk.Android.Internal/    # dd-sdk-android-internal bindings
│   ├── DatadogSdk.Android.Core/        # dd-sdk-android-core bindings
│   ├── DatadogSdk.Android.Logs/        # dd-sdk-android-logs bindings
│   ├── DatadogSdk.Android.Binding/     # Kotlin wrapper bindings
│   │   ├── Jars/
│   │   │   └── datadogwrapper-release.aar
│   │   └── Transforms/
│   │       ├── Metadata.xml      # Binding fixups
│   │       └── proguard.txt      # R8/ProGuard rules
│   │
│   └── DatadogSdk.Maui/          # Meta-package (unified)
│       └── DatadogSdk.Maui.csproj   # References iOS + Android bindings
│
├── example/                       # Test/demo MAUI application
│   ├── build.sh                  # Example app build script
│   ├── example.csproj            # References DatadogSdk.Maui
│   └── Platforms/
│       ├── iOS/AppDelegate.cs    # iOS initialization
│       └── Android/MainActivity.cs   # Android initialization
│
└── local-packages/                # Local NuGet package output
    ├── DatadogSdk.iOS.Binding.1.0.0.nupkg
    ├── DatadogSdk.Android.*.nupkg
    └── DatadogSdk.Maui.1.0.0.nupkg
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
@objc(DatadogWrapper)
public class DatadogWrapper: NSObject {

    // Static methods need @objc annotation
    @objc public static func initialize(
        clientToken: String,
        environment: String,
        service: String
    ) -> Bool {
        // Implementation
    }
}
```

**C# Binding Syntax:**
```csharp
[BaseType(typeof(NSObject))]
interface DatadogWrapper
{
    [Static]
    [Export("initializeWithClientToken:environment:service:")]
    bool Initialize(string clientToken, string environment, string service);
}
```

**Export Selector Pattern:**
- Swift method: `initialize(clientToken:environment:service:)`
- Objective-C selector: `initializeWithClientToken:environment:service:`
- Parameter names become part of selector

### Android Binding Specifics

**JvmStatic Requirement:**
```kotlin
class DatadogWrapper {
    companion object {
        @JvmStatic  // Critical: exposes as static method to C#
        fun initialize(
            context: Context,
            clientToken: String,
            environment: String,
            service: String,
            site: String = "us1"
        ): Boolean {
            // Implementation
        }
    }
}
```

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

1. **Build native wrappers**
   ```bash
   cd native-wrappers/ios && ./build.sh
   cd ../android && ./gradlew :datadogwrapper:assembleRelease
   ```

2. **Rebuild all bindings**
   ```bash
   cd ../.. && ./build.sh
   ```

3. **Test iOS**
   ```bash
   cd example && ./build.sh --clean --ios --run
   # Verify logs in Datadog UI
   ```

4. **Test Android**
   ```bash
   ./build.sh --clean --android --run
   # Verify logs in Datadog UI
   ```

5. **Check NuGet packages**
   ```bash
   ls -lh ../local-packages/
   # Verify timestamps are recent
   ```

## Useful Commands Reference

```bash
# Full rebuild
./build.sh

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
