#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
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
DOTNET_VERSION="10"

show_help() {
    echo "Usage: ./build.sh [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --ios          Build for iOS (default if no target specified)"
    echo "  --android      Build for Android"
    echo "  --release      Build in Release mode (enables AOT, trimming)"
    echo "  --run          Run the app after building"
    echo "  --net9         Build against .NET 9 instead of .NET 10 (default)"
    echo "  -h, --help     Show this help message"
    echo ""
    echo "Examples:"
    echo "  ./build.sh --ios --run              # Build and run iOS app (Debug, .NET 10)"
    echo "  ./build.sh --android --release      # Build Android app in Release mode"
    echo "  ./build.sh --ios --net9 --run       # Build and run iOS app against .NET 9"
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
        --net9)
            DOTNET_VERSION="9"
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
log_info "Clearing NuGet global cache for Datadog packages..."
rm -rf ~/.nuget/packages/datadog.*
log_info "Clean complete"

# ============================================================================
# Restore
# ============================================================================
log_section "Restoring NuGet Packages"
log_info "Running dotnet restore..."
RESTORE_TFM="net${DOTNET_VERSION}.0-${TARGET}"
dotnet restore -p:TargetFramework="$RESTORE_TFM"
log_info "Restore complete"

# ============================================================================
# Build and Run
# ============================================================================
if [ "$TARGET" = "ios" ]; then
    log_section "Building iOS App ($CONFIG)"

    # Auto-detect the latest available iPhone simulator (prefer booted, then latest runtime)
    SIMULATOR_INFO=$(python3 -c "
import json, subprocess
out = subprocess.check_output(['xcrun', 'simctl', 'list', 'devices', '--json'], text=True)
data = json.loads(out)
booted, available = [], []
for runtime, devices in data.get('devices', {}).items():
    if 'iOS' not in runtime:
        continue
    for d in devices:
        if not d.get('isAvailable') or 'iPhone' not in d.get('name', ''):
            continue
        entry = (runtime, d['udid'], d['name'])
        if d.get('state') == 'Booted':
            booted.append(entry)
        else:
            available.append(entry)
booted.sort(reverse=True)
available.sort(reverse=True)
pick = (booted or available or [None])[0]
if pick:
    print(f'{pick[1]}|{pick[2]}')
" 2>/dev/null)

    if [ -n "$SIMULATOR_INFO" ]; then
        SIMULATOR_UDID="${SIMULATOR_INFO%%|*}"
        SIM_NAME="${SIMULATOR_INFO##*|}"
        DEVICE_ARG=("-p:_DeviceName=:v2:udid=$SIMULATOR_UDID")
        log_info "Using simulator: $SIM_NAME ($SIMULATOR_UDID)"
    else
        DEVICE_ARG=()
        log_warning "Could not auto-detect simulator, using default"
    fi

    IOS_TFM="net${DOTNET_VERSION}.0-ios"


    # Ensure actool intermediate directory exists (workaround for .NET 10 preview ACTool bug)
    mkdir -p "obj/${CONFIG}/${IOS_TFM}/iossimulator-arm64/actool"

    log_info "Building iOS app (${IOS_TFM})..."
    dotnet build -c "$CONFIG" -f "$IOS_TFM" --no-restore "${DEVICE_ARG[@]}"
    if [ "$RUN_APP" = true ]; then
        log_info "Launching iOS app on simulator..."
        dotnet build -t:Run -c "$CONFIG" -f "$IOS_TFM" --no-restore "${DEVICE_ARG[@]}"
    fi

    log_info "iOS app built successfully"

elif [ "$TARGET" = "android" ]; then
    log_section "Building Android App ($CONFIG)"

    ANDROID_TFM="net${DOTNET_VERSION}.0-android"

    if [ "$RUN_APP" = true ]; then
        log_info "Building and running Android app on emulator (${ANDROID_TFM})..."
        dotnet build -t:Run -c "$CONFIG" -f "$ANDROID_TFM" -p:AndroidAttachDebugger=false --no-restore
    else
        log_info "Building Android app (${ANDROID_TFM})..."
        dotnet build -c "$CONFIG" -f "$ANDROID_TFM" --no-restore
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
