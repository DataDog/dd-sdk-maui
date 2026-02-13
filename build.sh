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

# Parse command line arguments
FORMAT_ONLY=false
CHECK_FORMAT=false
CLEAN=false

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
        --clean)
            CLEAN=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./build.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --format         Auto-format all C# code and exit"
            echo "  --check-format   Check C# formatting without modifying files"
            echo "  --clean          Clean all build artifacts before building"
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

# Ensure local-packages directory exists
mkdir -p local-packages

# ============================================================================
# iOS Native Wrapper
# ============================================================================
log_section "Building iOS Native Wrapper"

cd native-wrappers/ios/DatadogWrapper

# Clean previous build
log_info "Cleaning previous iOS build..."
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
# Android Bindings (in dependency order)
# ============================================================================
log_section "Building Android Bindings"

# Array of Android binding projects in dependency order
ANDROID_BINDINGS=(
    "DatadogSdk.Android.Internal"
    "DatadogSdk.Android.Core"
    "DatadogSdk.Android.Logs"
    "DatadogSdk.Android.Binding"
)

for BINDING in "${ANDROID_BINDINGS[@]}"; do
    log_info "Building $BINDING..."

    cd "$SCRIPT_DIR/bindings/$BINDING"

    # Clean
    rm -rf bin obj

    # Build
    dotnet build -c Release

    # Pack
    dotnet pack -c Release

    # Copy to local packages
    cp bin/Release/*.nupkg "$SCRIPT_DIR/local-packages/"

    log_info "$BINDING built successfully"
done

# ============================================================================
# Meta-package
# ============================================================================
log_section "Building Meta-package (DatadogSdk.Maui)"

cd "$SCRIPT_DIR/bindings/DatadogSdk.Maui"

# Clean
log_info "Cleaning meta-package..."
rm -rf bin obj
log_info "Clearing NuGet global cache for DatadogSdk packages..."
rm -rf ~/.nuget/packages/datadogsdk.*

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
