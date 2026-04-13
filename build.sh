#!/bin/bash
set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_section() {
    echo ""
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}▶ $1${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

log_info() {
    echo -e "${GREEN}✓${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

log_error() {
    echo -e "${RED}✗${NC} $1"
}

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Load version references
source "$SCRIPT_DIR/versions.properties"

# Parse command line arguments
FORMAT_ONLY=false
CHECK_FORMAT=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --format)
            FORMAT_ONLY=true
            shift
            ;;
        --check-format)
            CHECK_FORMAT=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./build.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --format         Auto-format all C# code and exit"
            echo "  --check-format   Check C# formatting without modifying files"
            echo "  -h, --help       Show this help message"
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Handle format commands
if [ "$FORMAT_ONLY" = true ]; then
    log_section "Formatting C# Code"
    log_info "Running dotnet format..."
    dotnet format "$SCRIPT_DIR/example.slnx"
    log_info "Formatting complete"
    exit 0
fi

if [ "$CHECK_FORMAT" = true ]; then
    log_section "Checking C# Code Formatting"
    log_info "Running dotnet format --verify-no-changes..."
    if dotnet format "$SCRIPT_DIR/example.slnx" --verify-no-changes; then
        log_info "All files are formatted correctly"
    else
        log_error "Some files need formatting. Run ./build.sh --format to fix."
        exit 1
    fi
    exit 0
fi

# Clean stale local packages and NuGet cache so multi-target rebuilds are picked up
rm -rf local-packages
mkdir -p local-packages
rm -rf ~/.nuget/packages/datadogsdk.*

# ============================================================================
# iOS Native Wrapper
# ============================================================================
log_section "Building iOS Native Wrapper"

cd native-wrappers/ios/DatadogWrapper

# Clean previous build
log_info "Cleaning previous iOS build..."
swift package clean 2>/dev/null || true
rm -rf .build .swiftpm

# Build XCFramework using existing script
log_info "Building XCFramework..."
cd ..
chmod +x build.sh
./build.sh

log_info "iOS native wrapper built successfully"

# ============================================================================
# Android Native Wrapper
# ============================================================================
log_section "Building Android Native Wrapper"

cd "$SCRIPT_DIR/native-wrappers/android"

# Clean previous build
log_info "Cleaning previous Android build..."
./gradlew clean

# Build AAR
log_info "Building Android AAR..."
./gradlew :datadogwrapper:assembleRelease

# Copy AAR to bindings
log_info "Copying AAR to bindings..."
cp datadogwrapper/build/outputs/aar/datadogwrapper-release.aar \
   "$SCRIPT_DIR/bindings/DatadogSdk.Android.Binding/Jars/"

log_info "Android native wrapper built successfully"

# ============================================================================
# iOS Binding
# ============================================================================
log_section "Building iOS Binding"

cd "$SCRIPT_DIR/bindings/DatadogSdk.iOS.Binding"

# Clean and build
log_info "Cleaning iOS binding..."
rm -rf bin obj

log_info "Building iOS binding..."
dotnet build -c Release

log_info "Packing iOS binding..."
dotnet pack -c Release

log_info "Copying iOS binding to local packages..."
cp bin/Release/*.nupkg "$SCRIPT_DIR/local-packages/"

log_info "iOS binding built successfully"

# ============================================================================
# Android Bindings — dependency chain (Internal → Core → Logs)
# DatadogSdk.Android.Binding is built separately after ProGuard extraction
# ============================================================================
log_section "Building Android Bindings"

ANDROID_DEPS=(
    "DatadogSdk.Android.Internal"
    "DatadogSdk.Android.Core"
    "DatadogSdk.Android.Logs"
    "DatadogSdk.Android.Trace"
    "DatadogSdk.Android.Rum"
)

for BINDING in "${ANDROID_DEPS[@]}"; do
    log_info "Building $BINDING..."

    cd "$SCRIPT_DIR/bindings/$BINDING"

    rm -rf bin obj
    # Build each TFM sequentially to avoid Maven cache file-lock contention
    # when both net9.0-android and net10.0-android download the same AAR.
    for TFM in net9.0-android net10.0-android; do
        dotnet build -c Release -f "$TFM"
    done
    dotnet pack -c Release
    cp bin/Release/*.nupkg "$SCRIPT_DIR/local-packages/"

    log_info "$BINDING built successfully"
done

# ============================================================================
# ProGuard rules extraction
# Merge rules from the Datadog AARs (now available in bin/) with the wrapper's
# own consumer-rules.pro into a single file that travels in the NuGet package.
# ============================================================================
log_section "Extracting and merging ProGuard rules"

PROGUARD_OUT="$SCRIPT_DIR/bindings/DatadogSdk.Android.Binding/proguard/datadog-merged.pro"
INTERNAL_BIN="$SCRIPT_DIR/bindings/DatadogSdk.Android.Internal/bin/Release/net10.0-android"
CORE_BIN="$SCRIPT_DIR/bindings/DatadogSdk.Android.Core/bin/Release/net10.0-android"
LOGS_BIN="$SCRIPT_DIR/bindings/DatadogSdk.Android.Logs/bin/Release/net10.0-android"
TRACE_BIN="$SCRIPT_DIR/bindings/DatadogSdk.Android.Trace/bin/Release/net10.0-android"
RUM_BIN="$SCRIPT_DIR/bindings/DatadogSdk.Android.Rum/bin/Release/net10.0-android"

log_info "Starting from wrapper consumer-rules.pro..."
echo "# Auto-generated file - Do not edit" > "$PROGUARD_OUT"
echo "" >> "$PROGUARD_OUT"
cat "$SCRIPT_DIR/native-wrappers/android/datadogwrapper/consumer-rules.pro" >> "$PROGUARD_OUT"

# Append rules extracted from each Datadog AAR
# proguard.txt is the standard consumer-rules location inside an AAR
for aar in \
    "$CORE_BIN/dd-sdk-android-core-${ANDROID_NATIVE_VERSION}.aar" \
    "$INTERNAL_BIN/dd-sdk-android-internal-${ANDROID_NATIVE_VERSION}.aar" \
    "$LOGS_BIN/dd-sdk-android-logs-${ANDROID_NATIVE_VERSION}.aar" \
    "$TRACE_BIN/dd-sdk-android-trace-${ANDROID_NATIVE_VERSION}.aar" \
    "$RUM_BIN/dd-sdk-android-rum-${ANDROID_NATIVE_VERSION}.aar"; do

    if [ -f "$aar" ]; then
        aar_name=$(basename "$aar")
        extracted=$(unzip -p "$aar" proguard.txt 2>/dev/null || true)
        if [ -n "$extracted" ]; then
            { echo ""; echo "# --- Rules from $aar_name ---"; echo "$extracted"; } >> "$PROGUARD_OUT"
            log_info "Appended rules from $aar_name"
        else
            log_warning "No proguard.txt in $aar_name (skipping)"
        fi
    else
        log_warning "AAR not found, skipping: $(basename "$aar")"
    fi
done

log_info "ProGuard rules merged → bindings/DatadogSdk.Android.Binding/proguard/datadog-merged.pro"

# ============================================================================
# Android Binding — wrapper (built last so it packs the merged ProGuard rules)
# ============================================================================
log_info "Building DatadogSdk.Android.Binding..."

cd "$SCRIPT_DIR/bindings/DatadogSdk.Android.Binding"

rm -rf bin obj
for TFM in net9.0-android net10.0-android; do
    dotnet build -c Release -f "$TFM"
done
dotnet pack -c Release
cp bin/Release/*.nupkg "$SCRIPT_DIR/local-packages/"

log_info "DatadogSdk.Android.Binding built successfully"

# ============================================================================
# Meta-package
# ============================================================================
log_section "Building Meta-package (DatadogSdk.Maui)"

cd "$SCRIPT_DIR/bindings/DatadogSdk.Maui"

# Clean
log_info "Cleaning meta-package..."
rm -rf bin obj

# Build
log_info "Building meta-package..."
dotnet build -c Release

# Pack
log_info "Packing meta-package..."
dotnet pack -c Release

# Copy to local packages
log_info "Copying meta-package to local packages..."
cp bin/Release/*.nupkg "$SCRIPT_DIR/local-packages/"

log_info "Meta-package built successfully"

# ============================================================================
# Summary
# ============================================================================
log_section "Build Complete!"

echo ""
echo -e "${GREEN}All bindings and native wrappers have been rebuilt successfully!${NC}"
echo ""
echo "Local NuGet packages updated in: ./local-packages/"
echo ""
echo "You can now build the example app by running:"
echo -e "${YELLOW}  cd example && ./build.sh${NC}"
echo ""
