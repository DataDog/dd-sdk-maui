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
TARGET=""
RUN_APP=false
CONFIG="Debug"

show_help() {
    echo "Usage: ./build.sh [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --ios          Build for iOS (default if no target specified)"
    echo "  --android      Build for Android"
    echo "  --release      Build in Release mode (enables AOT, trimming)"
    echo "  --run          Run the app after building"
    echo "  -h, --help     Show this help message"
    echo ""
    echo "Examples:"
    echo "  ./build.sh --ios --run              # Build and run iOS app (Debug)"
    echo "  ./build.sh --android --release      # Build Android app in Release mode"
    echo "  ./build.sh --ios --release --run    # Build and run iOS release app"
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --ios)
            TARGET="ios"
            shift
            ;;
        --android)
            TARGET="android"
            shift
            ;;
        --release)
            CONFIG="Release"
            shift
            ;;
        --run)
            RUN_APP=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Default to iOS if no target specified
if [ -z "$TARGET" ]; then
    TARGET="ios"
fi

# ============================================================================
# Clean
# ============================================================================
log_section "Cleaning Example App"
log_info "Removing bin and obj directories..."
rm -rf bin obj
log_info "Clearing NuGet global cache for DatadogSdk packages..."
rm -rf ~/.nuget/packages/datadogsdk.*
log_info "Clean complete"

# ============================================================================
# Restore
# ============================================================================
log_section "Restoring NuGet Packages"
log_info "Running dotnet restore..."
dotnet restore
log_info "Restore complete"

# ============================================================================
# Build and Run
# ============================================================================
if [ "$TARGET" = "ios" ]; then
    log_section "Building iOS App ($CONFIG)"

    # Auto-detect Xcode version to pick the matching iOS SDK pack
    XCODE_VERSION=$(xcodebuild -version 2>/dev/null | head -1 | sed 's/Xcode //')
    XCODE_MAJOR_MINOR=$(echo "$XCODE_VERSION" | cut -d. -f1,2)
    XCODE_ARG="-p:iOSTargetPlatformVersion=$XCODE_MAJOR_MINOR"
    log_info "Detected Xcode $XCODE_VERSION → targeting iOS platform version $XCODE_MAJOR_MINOR"

    # Auto-detect an available iOS simulator (prefer iPhone, latest runtime)
    SIMULATOR_UDID=$(xcrun simctl list devices available -j \
        | python3 -c "
import json, sys
data = json.load(sys.stdin)
candidates = []
for runtime, devices in data.get('devices', {}).items():
    if 'iOS' not in runtime:
        continue
    for d in devices:
        if d.get('isAvailable') and 'iPhone' in d.get('name', ''):
            candidates.append((runtime, d['udid'], d['name']))
candidates.sort(reverse=True)
if candidates:
    print(candidates[0][1])
" 2>/dev/null)

    if [ -n "$SIMULATOR_UDID" ]; then
        DEVICE_ARG="-p:_DeviceName=:v2:udid=$SIMULATOR_UDID"
        SIM_NAME=$(xcrun simctl list devices available | grep "$SIMULATOR_UDID" | sed 's/(.*//' | xargs)
        log_info "Using simulator: $SIM_NAME ($SIMULATOR_UDID)"
    else
        DEVICE_ARG=""
        log_warning "Could not auto-detect simulator, using default"
    fi

    # Ensure actool intermediate directory exists (workaround for .NET 10 preview ACTool bug)
    mkdir -p "obj/${CONFIG}/net10.0-ios/iossimulator-arm64/actool"

    log_info "Building iOS app..."
    dotnet build -c "$CONFIG" -f net10.0-ios --no-restore $DEVICE_ARG $XCODE_ARG

    if [ "$RUN_APP" = true ]; then
        log_info "Launching iOS app on simulator..."
        dotnet build -t:Run -c "$CONFIG" -f net10.0-ios --no-restore $DEVICE_ARG $XCODE_ARG
    fi

    log_info "iOS app built successfully"

elif [ "$TARGET" = "android" ]; then
    log_section "Building Android App ($CONFIG)"

    if [ "$RUN_APP" = true ]; then
        log_info "Building and running Android app on emulator..."
        dotnet build -t:Run -c "$CONFIG" -f net10.0-android -p:AndroidAttachDebugger=false --no-restore
    else
        log_info "Building Android app..."
        dotnet build -c "$CONFIG" -f net10.0-android --no-restore
    fi

    log_info "Android app built successfully"
fi

# ============================================================================
# Summary
# ============================================================================
log_section "Build Complete!"

echo ""
if [ "$RUN_APP" = true ]; then
    echo -e "${GREEN}Example app built and running on $TARGET!${NC}"
else
    echo -e "${GREEN}Example app built successfully for $TARGET!${NC}"
    echo ""
    echo "To run the app, use:"
    echo -e "${YELLOW}  ./build.sh --$TARGET --run${NC}"
fi
echo ""
