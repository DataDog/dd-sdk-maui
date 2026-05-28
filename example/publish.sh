#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 Datadog, Inc.
#
# publish.sh
#
# Publishes the example app for device distribution and optionally uploads
# debug symbols (dSYMs / ProGuard mappings) to Datadog.
#
# Prerequisites:
#   - Run ./build.sh from the repo root first to build native wrappers + NuGet packages
#   - Install datadog-ci: npm install -g @datadog/datadog-ci
#   - Export DATADOG_API_KEY in your shell
#
# Usage:
#   ./publish.sh --ios
#   ./publish.sh --android
#   ./publish.sh --ios --no-upload    (skip symbol upload)

set -e

# ── Colors ────────────────────────────────────────────────────────────────────

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_section() { echo ""; echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; echo -e "${BLUE}▶ $1${NC}"; echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; }
log_info()    { echo -e "${GREEN}✓${NC} $1"; }
log_error()   { echo -e "${RED}✗${NC} $1"; }

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# ── Usage ─────────────────────────────────────────────────────────────────────

usage() {
    echo "Usage: ./publish.sh --ios|--android [--no-upload]"
    echo ""
    echo "Publishes the example app and uploads debug symbols to Datadog."
    echo ""
    echo "Flags:"
    echo "  --ios          Publish for iOS (device build with dSYM)"
    echo "  --android      Publish for Android (with ProGuard mapping)"
    echo "  --no-upload    Skip symbol upload to Datadog"
    echo ""
    echo "Environment:"
    echo "  DATADOG_API_KEY    Required for symbol upload"
    echo "  DATADOG_SITE       Optional (defaults to datadoghq.com)"
}

# ── Argument parsing ──────────────────────────────────────────────────────────

TARGET=""
UPLOAD=true

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
        --no-upload)
            UPLOAD=false
            shift
            ;;
        *)
            log_error "Unknown option: $1"
            echo ""
            usage
            exit 1
            ;;
    esac
done

if [ -z "$TARGET" ]; then
    usage
    exit 1
fi

# ── Clean ─────────────────────────────────────────────────────────────────────

log_section "Cleaning previous build artifacts"
rm -rf bin/Release obj/Release
log_info "Cleaned bin/Release and obj/Release"

# ── Publish ───────────────────────────────────────────────────────────────────

UPLOAD_FLAG="false"
[ "$UPLOAD" = true ] && UPLOAD_FLAG="true"

if [ "$TARGET" = "ios" ]; then
    log_section "Publishing iOS Release (device)"

    dotnet publish -c Release -f net10.0-ios -r ios-arm64 \
        -p:DatadogUploadSymbols=$UPLOAD_FLAG \
        -v n -tl:off

elif [ "$TARGET" = "android" ]; then
    log_section "Publishing Android Release"

    dotnet publish -c Release -f net10.0-android \
        -p:DatadogUploadSymbols=$UPLOAD_FLAG \
        -v n -tl:off
fi

# ── Summary ───────────────────────────────────────────────────────────────────

log_section "Publish complete"

if [ "$TARGET" = "ios" ]; then
    echo ""
    log_info "IPA: bin/Release/net10.0-ios/ios-arm64/publish/example.ipa"
    log_info "App dSYM: bin/Release/net10.0-ios/ios-arm64/example.app.dSYM"
elif [ "$TARGET" = "android" ]; then
    echo ""
    log_info "APK: bin/Release/net10.0-android/publish/"
    if [ -f "bin/Release/net10.0-android/mapping.txt" ]; then
        log_info "Mapping: bin/Release/net10.0-android/mapping.txt"
    fi
fi

if [ "$UPLOAD" = true ]; then
    log_info "App symbols uploaded to Datadog (see build output above)"
else
    echo ""
    echo -e "${YELLOW}Symbol upload skipped (--no-upload).${NC}"
    echo "To upload manually, re-run without --no-upload."
fi
echo ""
