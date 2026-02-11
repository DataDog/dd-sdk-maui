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
CLEAN=false

show_help() {
    echo "Usage: ./build.sh [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --ios          Build for iOS (default if no target specified)"
    echo "  --android      Build for Android"
    echo "  --run          Run the app after building"
    echo "  --clean        Clean before building"
    echo "  -h, --help     Show this help message"
    echo ""
    echo "Examples:"
    echo "  ./build.sh --ios --run           # Build and run iOS app"
    echo "  ./build.sh --android --clean     # Clean and build Android app"
    echo "  ./build.sh --clean --ios --run   # Clean, build, and run iOS app"
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
        --run)
            RUN_APP=true
            shift
            ;;
        --clean)
            CLEAN=true
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
log_info "Removing bin directory..."
rm -rf bin
log_info "Clearing NuGet global cache for DatadogSdk packages..."
rm -rf ~/.nuget/packages/datadogsdk.*
log_info "Clean complete"

# ============================================================================
# Build and Run
# ============================================================================
if [ "$TARGET" = "ios" ]; then
    log_section "Building iOS App"

    log_info "Building iOS app..."
    dotnet build -f net10.0-ios

    if [ "$RUN_APP" = true ]; then
        log_info "Launching iOS app on simulator..."
        dotnet build -t:Run -f net10.0-ios
    fi

    log_info "iOS app built successfully"

elif [ "$TARGET" = "android" ]; then
    log_section "Building Android App"

    if [ "$RUN_APP" = true ]; then
        log_info "Building and running Android app on emulator..."
        dotnet build -t:Run -f net10.0-android -p:AndroidAttachDebugger=false
    else
        log_info "Building Android app..."
        dotnet build -f net10.0-android
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
