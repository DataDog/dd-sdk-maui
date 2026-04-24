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

log_result() {
    local name=$1
    local exit_code=$2
    if [ "$exit_code" -eq 0 ]; then
        echo -e "  ${GREEN}✓${NC} $name"
    else
        echo -e "  ${RED}✗${NC} $name"
    fi
}

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Parse command line arguments
RUN_MAUI=false
RUN_IOS=false
RUN_ANDROID=false
RUN_ALL=true
DOTNET_VERSION="10"

while [[ $# -gt 0 ]]; do
    case $1 in
        --maui)
            RUN_MAUI=true
            RUN_ALL=false
            shift
            ;;
        --ios)
            RUN_IOS=true
            RUN_ALL=false
            shift
            ;;
        --android)
            RUN_ANDROID=true
            RUN_ALL=false
            shift
            ;;
        --net9)
            DOTNET_VERSION="9"
            shift
            ;;
        -h|--help)
            echo "Usage: ./check.sh [OPTIONS]"
            echo ""
            echo "Runs test suites for the Datadog MAUI SDK."
            echo "With no options, all test suites are run."
            echo ""
            echo "Options:"
            echo "  --maui             Run C# unit tests only"
            echo "  --ios              Run iOS Swift tests only"
            echo "  --android          Run Android Kotlin tests only"
            echo "  --net9             Run C# tests against .NET 9 (default: .NET 10)"
            echo "  -h, --help         Show this help message"
            echo ""
            echo "Multiple flags can be combined:"
            echo "  ./check.sh --maui --android"
            echo "  ./check.sh --maui --net9"
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            echo "Run ./check.sh --help for usage"
            exit 1
            ;;
    esac
done

# If no specific flag was set, run everything
if [ "$RUN_ALL" = true ]; then
    RUN_MAUI=true
    RUN_IOS=true
    RUN_ANDROID=true
fi

# Track results
MAUI_RESULT=-1
IOS_RESULT=-1
ANDROID_RESULT=-1

# ============================================================================
# C# / MAUI Tests
# ============================================================================
if [ "$RUN_MAUI" = true ]; then
    log_section "C# Tests (xUnit)"

    log_info "Running dotnet test (net${DOTNET_VERSION}.0)..."
    if dotnet test "$SCRIPT_DIR/tests/DatadogSdk.Maui.Tests/" -f "net${DOTNET_VERSION}.0"; then
        MAUI_RESULT=0
        log_info "C# tests passed"
    else
        MAUI_RESULT=1
        log_error "C# tests failed"
    fi
fi

# ============================================================================
# iOS Swift Tests
# ============================================================================
if [ "$RUN_IOS" = true ]; then
    log_section "iOS Tests (XCTest)"

    log_info "Running xcodebuild test (iOS Simulator)..."
    # DatadogRUM requires UIKit, so swift test (macOS) won't work.
    # We must use xcodebuild with an iOS Simulator destination.
    # Pick the first available iPhone simulator by UDID.
    SIM_UDID=$(xcrun simctl list devices available -j | python3 -c "
import json, sys
data = json.load(sys.stdin)
# Prefer an already-booted iPhone, otherwise pick the newest available one
best = None
for runtime, devices in sorted(data['devices'].items(), reverse=True):
    if 'iOS' not in runtime:
        continue
    for d in devices:
        if 'iPhone' in d['name'] and d['isAvailable']:
            if d.get('state') == 'Booted':
                print(d['udid']); sys.exit(0)
            if best is None:
                best = d['udid']
if best:
    print(best)
" 2>/dev/null)

    if [ -z "$SIM_UDID" ]; then
        log_error "No available iPhone simulator found"
        IOS_RESULT=1
    elif (cd "$SCRIPT_DIR/native-wrappers/ios/DatadogWrapper" && xcodebuild test \
        -scheme DatadogWrapper \
        -destination "platform=iOS Simulator,id=$SIM_UDID" \
        -quiet); then
        IOS_RESULT=0
        log_info "iOS tests passed"
    else
        IOS_RESULT=1
        log_error "iOS tests failed"
    fi
fi

# ============================================================================
# Android Kotlin Tests
# ============================================================================
if [ "$RUN_ANDROID" = true ]; then
    log_section "Android Tests (JUnit + MockK)"

    log_info "Running gradlew test..."
    if (cd "$SCRIPT_DIR/native-wrappers/android" && ./gradlew :datadogwrapper:test); then
        ANDROID_RESULT=0
        log_info "Android tests passed"
    else
        ANDROID_RESULT=1
        log_error "Android tests failed"
    fi
fi

# ============================================================================
# Summary
# ============================================================================
log_section "Results"

FAILED=0

if [ "$RUN_MAUI" = true ]; then
    log_result "C# (xUnit)" $MAUI_RESULT
    [ "$MAUI_RESULT" -ne 0 ] && FAILED=1
fi

if [ "$RUN_IOS" = true ]; then
    log_result "iOS (XCTest)" $IOS_RESULT
    [ "$IOS_RESULT" -ne 0 ] && FAILED=1
fi

if [ "$RUN_ANDROID" = true ]; then
    log_result "Android (JUnit)" $ANDROID_RESULT
    [ "$ANDROID_RESULT" -ne 0 ] && FAILED=1
fi

echo ""

if [ "$FAILED" -ne 0 ]; then
    echo -e "${RED}Some test suites failed.${NC}"
    exit 1
else
    echo -e "${GREEN}All test suites passed.${NC}"
fi
