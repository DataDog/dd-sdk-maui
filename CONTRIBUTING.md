# Contributing to Datadog SDK for .NET MAUI

The code is licensed under the Apache License 2.0 (see [LICENSE](LICENSE) for details).

First of all, thanks for contributing! This document provides guidelines for contributing to this repository. To propose improvements, feel free to submit a PR.

## Submitting Issues

- If you think you've found an issue, search the issue list to see if there's an existing issue.
- Then, if you find nothing, open a GitHub issue.

## General Guidelines

This guide will help you set up your development environment, understand the build process, and make your first contribution.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Setup (Recommended)](#quick-setup-recommended)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Build Scripts Explained](#build-scripts-explained)
- [Development Workflow](#development-workflow)
- [Testing Your Changes](#testing-your-changes)
- [Common Tasks](#common-tasks)
- [Troubleshooting](#troubleshooting)
- [Pull Request Guidelines](#pull-request-guidelines)

## Prerequisites

> **Note**: The automated setup script (`./setup.sh`) can install most of these for you. This section is provided for reference and manual setup.

### Required Software

**macOS Development Machine** (required for iOS development):
- macOS 13.0+ (Ventura or later)
- Xcode 15.0+ with Command Line Tools
- .NET 9 SDK or .NET 10 SDK
- Android SDK (for Android development)

**Tools and SDKs**:

1. **.NET 9 or .NET 10 SDK**
   ```bash
   # Download from https://dotnet.microsoft.com/download
   # Verify installation:
   dotnet --version  # Should show 9.0.x or 10.0.x
   ```

2. **Xcode** (for iOS)
   ```bash
   # Install from Mac App Store
   # Install Command Line Tools:
   xcode-select --install
   ```

3. **Android SDK**
   ```bash
   # Install via Android Studio or Visual Studio
   # Set ANDROID_HOME environment variable:
   export ANDROID_HOME=$HOME/Library/Android/sdk
   export PATH=$PATH:$ANDROID_HOME/emulator
   export PATH=$PATH:$ANDROID_HOME/platform-tools
   ```

4. **Java JDK 17+** (for Android)
   ```bash
   # Install via Homebrew:
   brew install openjdk@17
   ```

### Optional Tools

- **Visual Studio for Mac** or **Visual Studio Code** with C# extension
- **Android Studio** (for Android emulator management)
- **Xcode Simulator** (comes with Xcode)

## Quick Setup (Recommended)

Use the automated setup script to configure your development environment:

```bash
./setup.sh
```

The script will:
- Check for required dependencies
- Offer to install missing components
- Configure environment variables
- Optionally verify the setup is functional

### Script Options

- `./setup.sh` - Full interactive setup with prompts
- `./setup.sh --help` - Show usage information
- `./setup.sh --verify` - Run verification tests after setup
- `./setup.sh --verify-only` - Only run verification tests

### Verification

To verify your environment is working after setup:

```bash
./setup.sh --verify
```

This creates a temporary MAUI project and tests both iOS and Android builds.

For manual setup or troubleshooting, continue with the sections below.

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/DataDog/dd-sdk-maui.git
cd dd-sdk-maui
```

### 2. Set Up Your Environment

Run the automated setup script:

```bash
./setup.sh
```

The script will check all required dependencies and offer to install missing components. Follow the prompts to complete your environment setup.

**Tip**: If you prefer manual setup, see the [Prerequisites](#prerequisites) section above for detailed instructions.

### 3. Initial Build

Run the root build script to compile all components:

```bash
./build.sh
```

This will:
1. Build iOS native wrapper (Swift → XCFramework)
2. Build Android native wrapper (Kotlin → AAR)
3. Build all C# bindings (→ NuGet packages)
4. Output everything to `./local-packages/`


### 4. Run Example App

Test that everything works:

```bash
cd example

# iOS
./build.sh --ios --run

# Android (ensure emulator is running)
./build.sh --android --run
```

You should see the example app launch and logs appearing in your Datadog account.

## Project Structure

```
dd-sdk-maui/
│
├── build.sh                    # Root build script (rebuilds everything)
├── NuGet.Config                # Local package source configuration
│
├── native-wrappers/            # Platform-native code
│   ├── ios/
│   │   ├── build.sh           # iOS-specific build script
│   │   └── DatadogWrapper/    # Swift Package Manager project
│   │   └── Sources/DatadogWrapper/
│   │       ├── DatadogWrapper.swift  # SDK initialization
│   │       ├── DdLogs.swift          # Logging API
│   │       └── DdRum.swift           # RUM API
│   └── android/
│       ├── gradlew            # Gradle wrapper
│       └── datadogwrapper/    # Android library module
│           └── src/main/kotlin/com/datadog/wrapper/
│               ├── DatadogWrapper.kt  # SDK initialization
│               ├── DdLogs.kt          # Logging API
│               └── DdRum.kt           # RUM API
│
├── bindings/                   # C# binding projects
│   ├── DatadogSdk.iOS.Binding/
│   ├── DatadogSdk.Android.*/  # 4+ Android binding projects
│   │   └── DatadogSdk.Android.Logs/  # Android Logs AAR binding
│   │   └── DatadogSdk.Android.Trace/  # Android Trace AAR binding
│   │   └── DatadogSdk.Android.Rum/  # Android RUM AAR binding
│   └── DatadogSdk.Maui/       # C# intermediary layer + meta-package
│       ├── DdSdkConfiguration.cs  # Configuration object
│       ├── DdSdk.cs               # SDK init (unified API)
│       ├── DdLogs.cs              # Logging (unified API)
│       └── DdTrace.cs             # Tracing (unified API)
│       └── DdRum.cs               # RUM (unified API)
│
├── example/                    # Test/demo application
│   ├── build.sh               # Example app build script
│   └── example.csproj         # MAUI app project
│
└── local-packages/             # NuGet package output (gitignored)
```

## Build Scripts Explained

### Root Build Script (`./build.sh`)

**Purpose**: Complete rebuild of all native wrappers and C# bindings in correct dependency order.

**What it does step-by-step**:

#### Step 1: iOS Native Wrapper
```bash
cd native-wrappers/ios/DatadogWrapper
rm -rf .build .swiftpm  # Clean previous build
cd .. && ./build.sh      # Run iOS build script
```

The iOS build script (`native-wrappers/ios/build.sh`):
1. Builds Swift code for iOS device (arm64)
2. Builds Swift code for iOS simulator (arm64 + x86_64)
3. Creates fat binary for simulator
4. Packages as XCFramework
5. Copies to `bindings/DatadogSdk.iOS.Binding/NativeReference/`

**Output**: `DatadogWrapper.xcframework` (~11MB)

#### Step 2: Android Native Wrapper
```bash
cd native-wrappers/android
./gradlew clean                              # Clean previous build
./gradlew :datadogwrapper:assembleRelease   # Build AAR
cp datadogwrapper/build/outputs/aar/datadogwrapper-release.aar \
   ../../bindings/DatadogSdk.Android.Binding/Jars/
```

**Output**: `datadogwrapper-release.aar` (~8KB)

#### Step 3: iOS C# Binding
```bash
cd bindings/DatadogSdk.iOS.Binding
rm -rf bin obj                  # Clean
dotnet build -c Release         # Build binding
dotnet pack -c Release          # Create NuGet package
cp bin/Release/*.nupkg ../../local-packages/
```

**Output**: `DatadogSdk.iOS.Binding.0.0.1.nupkg`

#### Step 4: Android C# Bindings (4 projects)

Builds in dependency order:
1. **DatadogSdk.Android.Internal** - Internal APIs binding
2. **DatadogSdk.Android.Core** - Core SDK binding (depends on Internal)
3. **DatadogSdk.Android.Logs** - Logs module binding (depends on Core)
4. **DatadogSdk.Android.Trace** - Trace module binding (depends on Core)
5. **DatadogSdk.Android.Binding** - Wrapper binding (depends on all above)

Each project:
```bash
cd bindings/DatadogSdk.Android.{Project}
rm -rf bin obj
dotnet build -c Release
dotnet pack -c Release
cp bin/Release/*.nupkg ../../local-packages/
```

**Why this order matters**: Later projects reference earlier ones as NuGet PackageReferences. Building out of order will fail.

#### Step 5: Meta-package
```bash
cd bindings/DatadogSdk.Maui
rm -rf bin obj
dotnet build -c Release
dotnet pack -c Release
cp bin/Release/*.nupkg ../../local-packages/
```

**Output**: `DatadogSdk.Maui.0.0.1.nupkg` (aggregates iOS + Android bindings)

### Example Build Script (`./example/build.sh`)

**Purpose**: Build and run the example MAUI application.

**Usage**:
```bash
./build.sh [OPTIONS]

Options:
  --ios          Build for iOS (default)
  --android      Build for Android
  --run          Run the app after building
  -h, --help     Show help message
```

**Examples**:
```bash
# Build for iOS (default)
./build.sh

# Build and run on iOS simulator
./build.sh --ios --run

# Build and run on Android emulator
./build.sh --android --run
```

**What it does**:

1. **Clean** (always):
   ```bash
   rm -rf bin obj                          # Remove build artifacts + stale NuGet restore data
   rm -rf ~/.nuget/packages/datadogsdk.*   # Clear NuGet global cache for our packages
   ```

2. **Restore packages**:
   ```bash
   dotnet restore
   ```

3. **Build** (with `--no-restore` to avoid redundant restores):
   ```bash
   # iOS (.NET 10)
   dotnet build -f net10.0-ios --no-restore

   # Android (.NET 10)
   dotnet build -f net10.0-android --no-restore

   # iOS (.NET 9)
   dotnet build -f net9.0-ios --no-restore

   # Android (.NET 9)
   dotnet build -f net9.0-android --no-restore
   ```

4. **Run** (if `--run` flag provided):
   ```bash
   # iOS
   dotnet build -t:Run -f net10.0-ios --no-restore

   # Android
   dotnet build -t:Run -f net10.0-android -p:AndroidAttachDebugger=false --no-restore
   ```

   Or use the `build.sh` script which also supports `--net9`:
   ```bash
   ./build.sh --ios --run           # .NET 10 (default)
   ./build.sh --ios --run --net9    # .NET 9
   ./build.sh --android --run --net9
   ```

The build script always cleans `obj/` and the NuGet cache to prevent stale restore data from causing build failures after SDK rebuilds.

## Development Workflow

### Typical Development Cycle

1. **Make changes to native code** (Swift or Kotlin)
2. **Rebuild native wrapper** (produces XCFramework or AAR)
3. **Rebuild C# bindings** (produces NuGet packages)
4. **Test in example app**

### Full Rebuild Flow

```bash
# From project root
./build.sh

# Test changes
cd example
./build.sh --ios --run
./build.sh --android --run
```

### Quick Rebuild Flows

**iOS only**:
```bash
cd native-wrappers/ios && ./build.sh
cd ../../bindings/DatadogSdk.iOS.Binding
dotnet pack -c Release
cp bin/Release/*.nupkg ../../local-packages/
cd ../../bindings/DatadogSdk.Maui
dotnet pack -c Release
cp bin/Release/*.nupkg ../../local-packages/
cd ../../example
./build.sh --ios --run
```

**Android only**:
```bash
cd native-wrappers/android
./gradlew :datadogwrapper:assembleRelease
cp datadogwrapper/build/outputs/aar/datadogwrapper-release.aar \
   ../../bindings/DatadogSdk.Android.Binding/Jars/
cd ../../bindings/DatadogSdk.Android.Binding
dotnet pack -c Release
cp bin/Release/*.nupkg ../../local-packages/
cd ../DatadogSdk.Maui
dotnet pack -c Release
cp bin/Release/*.nupkg ../../local-packages/
cd ../../example
./build.sh --android --run
```

**Tip**: Use `./build.sh` from root for simplicity. It's fast (~2-3 minutes).

## Testing Your Changes

### Manual Testing

1. **Build bindings**:
   ```bash
   ./build.sh
   ```

2. **Run example app**:
   ```bash
   cd example
   ./build.sh --ios --run    # iOS
   ./build.sh --android --run # Android
   ```

3. **Verify in Datadog UI**:
   - Open [Datadog Logs Explorer](https://app.datadoghq.com/logs)
   - Filter: `service:sergio-maui-test env:dev`
   - Look for test log messages

### Expected Test Logs

You should see logs like:
```
[INFO] iOS binding validation - LogInfo works!
[DEBUG] iOS binding validation - LogDebug works!
[WARN] iOS binding validation - LogWarn works!
[ERROR] iOS binding validation - LogError works!
```

### Unit Tests

Run the C# unit tests to verify SDK behavior:

```bash
# Run all tests
dotnet test tests/DatadogSdk.Maui.Tests

# Run with verbose output
dotnet test tests/DatadogSdk.Maui.Tests -v detailed

# Run specific test class
dotnet test tests/DatadogSdk.Maui.Tests --filter "FullyQualifiedName~DdSdkConfigurationTests"
```

**Test coverage:**
- `DdSdkConfigurationTests` - SDK initialization with all config options via test bridge
- `DdSdkConversionTests` - Enum-to-string conversions (site, consent, additional config)
- `FileBasedConfigurationTests` - JSON config parsing, validation, and error handling
- `InternalLogTests` - SDK logging with verbosity filtering
- `DdLogsConfigurationTests` - Logs module configuration
- `DdTraceConfigurationTests` - Trace module configuration

### Native Wrapper Tests

**iOS tests** (XCTest):
```bash
cd native-wrappers/ios/DatadogWrapper
swift test
```

**Android tests** (JUnit + MockK):
```bash
cd native-wrappers/android
./gradlew test

# View test report
open datadogwrapper/build/reports/tests/testDebugUnitTest/index.html
```

### Debugging Build Issues

**Enable verbose logging**:
```bash
# iOS build
cd native-wrappers/ios
./build.sh 2>&1 | tee build.log

# Android build
cd native-wrappers/android
./gradlew :datadogwrapper:assembleRelease --info

# .NET build
dotnet build -v detailed
```

**Check XCFramework symbols** (iOS):
```bash
nm -gU bindings/DatadogSdk.iOS.Binding/NativeReference/DatadogWrapper.xcframework/ios-arm64/DatadogWrapper.framework/DatadogWrapper
```

**Inspect AAR contents** (Android):
```bash
unzip -l bindings/DatadogSdk.Android.Binding/Jars/datadogwrapper-release.aar
```

## Common Tasks

### Adding a New API Method

#### 1. Add to Native Wrappers

**iOS** (`native-wrappers/ios/DatadogWrapper/Sources/DatadogWrapper/DdLogs.swift`):
```swift
@objc public static func logCritical(_ message: String) {
    logger?.critical(message)
}
```

**Android** (`native-wrappers/android/datadogwrapper/src/main/kotlin/com/datadog/wrapper/DdLogs.kt`):
```kotlin
@JvmStatic
fun logCritical(message: String) {
    logger?.critical(message)
}
```

#### 2. Update iOS C# Binding

**iOS** (`bindings/DatadogSdk.iOS.Binding/ApiDefinition.cs`):
```csharp
[Static]
[Export("logCritical:")]
void LogCritical(string message);
```

**Android**: Auto-generated from AAR (no manual changes needed)

#### 3. Add to C# Intermediary Layer

**`bindings/DatadogSdk.Maui/DdLogs.cs`**:
```csharp
public static void Critical(string message)
{
    DdSdk.LogDebug($"DdLogs.Critical called: {message}");

    NativeDdLogs.LogCritical(message);
}
```

This is the method consumers will call. The `#if ANDROID / #elif IOS` directives are only needed when the native APIs differ between platforms (e.g. different parameter types).

> **RUM module pattern**: The RUM module (`DdRum.cs`) uses a **dictionary bridge pattern** rather than individual parameters. Instead of passing each configuration field as a separate argument to the native layer, the entire `DdRumConfiguration` object is serialized into a `Dictionary<string, object>` (or equivalent native map) and passed as a single argument. This keeps the native bridge stable as new configuration fields are added. See `DdRum.cs` for a worked example of this pattern.

#### 4. Rebuild and Test
```bash
./build.sh
cd example
./build.sh --ios --run
./build.sh --android --run
```

### Updating Native SDK Versions

Use the `update-native-sdk.sh` script to bump native SDK versions across all files:

```bash
# Update both platforms
./update-native-sdk.sh --ios 3.8.0 --android 3.8.0

# Update one platform
./update-native-sdk.sh --android 3.8.0
```

This script updates `versions.properties`, `Package.swift`, `build.gradle.kts`, and all Android binding `.csproj` files in one pass.

**Android transitive dependencies**: When bumping the Android SDK, the script automatically runs `resolve-android-deps.sh` which:
1. Resolves the full Gradle dependency tree
2. Auto-updates `AndroidMavenLibrary` versions in `DatadogSdk.Android.Core.csproj`
3. Warns if any NuGet `PackageReference` versions need manual updating (e.g., `Xamarin.Kotlin.StdLib`, `GoogleGson`)

If you see NuGet warnings, check [nuget.org](https://www.nuget.org) for a compatible release and update the version in the relevant `.csproj` files and `android-transitive-deps.json`.

**iOS transitive dependencies**: Not a concern — SPM statically links all dependencies into the XCFramework at build time. No runtime dependency declarations are needed.

After bumping:
```bash
./build.sh                  # Full rebuild
./verify-artifacts.sh       # Validate artifacts + dependency alignment
```

## Troubleshooting

### Setup Script Issues

**"Permission denied" when running setup.sh:**
```bash
chmod +x setup.sh
./setup.sh
```

**Environment variables not persisting after setup:**
- Restart your terminal after running setup.sh
- Or manually source your shell profile: `source ~/.zshrc` (or `~/.bash_profile`)

**Verification tests fail but dependencies show as installed:**
- Try building the SDK directly: `./build.sh`
- Check MAUI workload: `dotnet workload list | grep maui`
- Verify simulator/emulator availability:
  - iOS: `xcrun simctl list devices | grep iPhone`
  - Android: `$ANDROID_HOME/emulator/emulator -list-avds`

**Android SDK licenses not accepted:**
```bash
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --licenses
```

**Xcode Command Line Tools installation hangs:**
- Cancel and install manually: `xcode-select --install`
- Or download from Apple Developer: https://developer.apple.com/download/

### iOS Build Fails with "selector not found"

**Cause**: Swift method not properly exposed to Objective-C.

**Solution**:
1. Verify `@objc(ClassName)` on class
2. Verify `@objc public static` on methods
3. Check selector in `[Export("...")]` matches Swift signature

**Debug**:
```bash
nm -gU bindings/DatadogSdk.iOS.Binding/NativeReference/DatadogWrapper.xcframework/ios-arm64/DatadogWrapper.framework/DatadogWrapper | grep initialize
```

### Android Build Fails with "duplicate type"

**Cause**: Binding project uses ProjectReference instead of PackageReference for Android bindings.

**Solution**: Use PackageReference in `.csproj`:
```xml
<PackageReference Include="DatadogSdk.Android.Core" Version="0.0.1" />
```

### Example App Doesn't Reflect Changes

**Cause**: NuGet package cache holding old version.

**Solution**: Run the example build script — it always cleans and refreshes the NuGet cache:
```bash
cd example
./build.sh --ios --run
```

### Gradle Fails with "SDK location not found"

**Cause**: Android SDK path not configured.

**Solution**: Set `ANDROID_HOME` or create `local.properties`:
```bash
echo "sdk.dir=$HOME/Library/Android/sdk" > native-wrappers/android/local.properties
```

### iOS Simulator Not Found

**Cause**: Simulator not running or not selected.

**Solution**:
```bash
# List available simulators
xcrun simctl list devices

# Boot a simulator
xcrun simctl boot "iPhone 15"

# Or use Xcode UI to launch simulator
```

### Android Emulator Not Found

**Cause**: No emulator running.

**Solution**:
```bash
# List available emulators
emulator -list-avds

# Start an emulator
emulator -avd Pixel_7_API_36 &

# Or use Android Studio AVD Manager
```

## Pull Request Guidelines

### Keep it small, focused

Avoid changing too many things at once. For instance if you're fixing a bug and at the same time adding some code refactor, it makes reviewing harder and the time-to-release longer.

### Commit messages

Please don't be this person: `git commit -m "Fixed stuff"`. Take a moment to write meaningful commit messages.

The commit message should describe the reason for the change and give extra details that will allow someone later on to understand in 5 seconds the thing you've been working on for a day.

### Squash your commits

Rebase your changes on `develop` and squash your commits whenever possible. This keeps history cleaner and easier to revert things. It also makes developers happier!

### Before Submitting

1. **Build and test**:
   ```bash
   ./build.sh
   cd example
   ./build.sh --ios --run
   ./build.sh --android --run
   ```

2. **Verify logs in Datadog**: Ensure your changes work end-to-end

3. **Update documentation**: If adding features, update AGENTS.md and SPEC.md

4. **Follow commit conventions**:
   ```
   feat(ios): add critical log level
   fix(android): correct site parameter mapping
   docs: update build instructions
   ```

### Pull Request Process

1. **Fork the repository**

2. **Create a feature branch**:
   ```bash
   git checkout -b feature/add-critical-logging
   ```

3. **Make your changes and commit**:
   ```bash
   git add .
   git commit -m "feat: add critical log level to iOS and Android"
   ```

4. **Push to your fork**:
   ```bash
   git push origin feature/add-critical-logging
   ```

5. **Open Pull Request** on GitHub

6. **Address review feedback**

### PR Checklist

- [ ] Code builds without errors on both iOS and Android
- [ ] Example app runs successfully on both platforms
- [ ] Logs appear in Datadog UI
- [ ] Documentation updated (if applicable)
- [ ] No unnecessary files committed (check .gitignore)
- [ ] Commit messages follow convention
- [ ] PR description explains what and why

## Development Environment Tips

### Visual Studio Code Setup

Recommended extensions:
- C# Dev Kit
- .NET MAUI (Preview)
- Swift (for native wrapper editing)
- Kotlin (for native wrapper editing)

### Xcode Setup

For iOS native wrapper development:
1. Open `native-wrappers/ios/DatadogWrapper` in Xcode
2. Select iOS Simulator target
3. Build and debug Swift code directly

### Android Studio Setup

For Android native wrapper development:
1. Open `native-wrappers/android` in Android Studio
2. Sync Gradle
3. Build `datadogwrapper` module

## Getting Help

- **Issues**: Open an issue on GitHub with:
  - Description of problem
  - Steps to reproduce
  - Build output / error messages
  - Platform (iOS/Android) and OS version

- **Questions**: Start a GitHub Discussion

- **Documentation**:
  - See `SPEC.md` for technical details
  - See `AGENTS.md` for AI agent context

## Code of Conduct

Be respectful, inclusive, and constructive in all interactions.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
