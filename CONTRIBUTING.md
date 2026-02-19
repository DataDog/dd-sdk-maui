# Contributing to Datadog SDK for .NET MAUI

Thank you for your interest in contributing to the Datadog SDK for .NET MAUI! This guide will help you set up your development environment, understand the build process, and make your first contribution.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Build Scripts Explained](#build-scripts-explained)
- [Development Workflow](#development-workflow)
- [Testing Your Changes](#testing-your-changes)
- [Common Tasks](#common-tasks)
- [Troubleshooting](#troubleshooting)
- [Pull Request Guidelines](#pull-request-guidelines)

## Prerequisites

### Required Software

**macOS Development Machine** (required for iOS development):
- macOS 13.0+ (Ventura or later)
- Xcode 15.0+ with Command Line Tools
- .NET 10 SDK
- Android SDK (for Android development)

**Tools and SDKs**:

1. **.NET 10 SDK**
   ```bash
   # Download from https://dotnet.microsoft.com/download
   # Verify installation:
   dotnet --version  # Should show 10.0.x
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

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/DataDog/dd-sdk-maui.git
cd dd-sdk-maui
```

### 2. Verify Environment

```bash
# Check .NET
dotnet --version

# Check Xcode
xcodebuild -version

# Check Android SDK
which adb  # Should return path to adb

# Check Java
java -version
```

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
│   └── android/
│       ├── gradlew            # Gradle wrapper
│       └── datadogwrapper/    # Android library module
│
├── bindings/                   # C# binding projects
│   ├── DatadogSdk.iOS.Binding/
│   ├── DatadogSdk.Android.*/  # 4 Android binding projects
│   └── DatadogSdk.Maui/       # C# intermediary layer + meta-package
│       ├── DdSdkConfiguration.cs  # Configuration object
│       ├── DdSdk.cs               # SDK init (unified API)
│       └── DdLogs.cs              # Logging (unified API)
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

**Output**: `DatadogSdk.iOS.Binding.1.0.0.nupkg`

#### Step 4: Android C# Bindings (4 projects)

Builds in dependency order:
1. **DatadogSdk.Android.Internal** - Internal APIs binding
2. **DatadogSdk.Android.Core** - Core SDK binding (depends on Internal)
3. **DatadogSdk.Android.Logs** - Logs module binding (depends on Core)
4. **DatadogSdk.Android.Binding** - Wrapper binding (depends on all above)

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

**Output**: `DatadogSdk.Maui.1.0.0.nupkg` (aggregates iOS + Android bindings)

### Example Build Script (`./example/build.sh`)

**Purpose**: Build and run the example MAUI application.

**Usage**:
```bash
./build.sh [OPTIONS]

Options:
  --ios          Build for iOS (default)
  --android      Build for Android
  --run          Run the app after building
  --clean        Clean before building (forces NuGet refresh)
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

# Clean build (useful after updating bindings)
./build.sh --clean --ios --run
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
   # iOS
   dotnet build -f net10.0-ios --no-restore

   # Android
   dotnet build -f net10.0-android --no-restore
   ```

4. **Run** (if `--run` flag provided):
   ```bash
   # iOS
   dotnet build -t:Run -f net10.0-ios --no-restore

   # Android
   dotnet build -t:Run -f net10.0-android -p:AndroidAttachDebugger=false --no-restore
   ```

The build script always cleans `obj/` and the NuGet cache to prevent stale restore data from causing build failures after SDK rebuilds.

## Development Workflow

### Typical Development Cycle

1. **Make changes to native code** (Swift or Kotlin)
2. **Rebuild native wrapper** (produces XCFramework or AAR)
3. **Rebuild C# bindings** (produces NuGet packages)
4. **Test in example app** with `--clean` flag

### Full Rebuild Flow

```bash
# From project root
./build.sh

# Test changes
cd example
./build.sh --clean --ios --run
./build.sh --clean --android --run
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
./build.sh --clean --ios --run
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
./build.sh --clean --android --run
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
   ./build.sh --clean --ios --run    # iOS
   ./build.sh --clean --android --run # Android
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
- `DdSdkConfigurationTests` - Configuration object defaults and validation
- `DdSdkConversionTests` - Enum-to-string conversions
- `InternalLogTests` - SDK logging with verbosity filtering

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

#### 4. Rebuild and Test
```bash
./build.sh
cd example
./build.sh --ios --run
./build.sh --android --run
```

### Updating Native SDK Versions

#### iOS (dd-sdk-ios)

Edit `native-wrappers/ios/DatadogWrapper/Package.swift`:
```swift
dependencies: [
    .package(url: "https://github.com/DataDog/dd-sdk-ios.git", from: "3.6.0")
]
```

Rebuild:
```bash
cd native-wrappers/ios
./build.sh
```

#### Android (dd-sdk-android)

Edit `native-wrappers/android/datadogwrapper/build.gradle.kts`:
```kotlin
dependencies {
    implementation("com.datadoghq:dd-sdk-android-core:3.6.0")
    implementation("com.datadoghq:dd-sdk-android-logs:3.6.0")
}
```

Rebuild:
```bash
cd native-wrappers/android
./gradlew :datadogwrapper:assembleRelease
```

## Troubleshooting

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
<PackageReference Include="DatadogSdk.Android.Core" Version="1.0.0" />
```

### Example App Doesn't Reflect Changes

**Cause**: NuGet package cache holding old version.

**Solution**: Use `--clean` flag:
```bash
cd example
./build.sh --clean --ios --run
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

### Before Submitting

1. **Build and test**:
   ```bash
   ./build.sh
   cd example
   ./build.sh --clean --ios --run
   ./build.sh --clean --android --run
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

[License information to be added]

---

**Happy Contributing!** 🎉

If you have questions or need help, don't hesitate to open an issue or discussion on GitHub.
