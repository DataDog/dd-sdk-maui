#!/bin/bash
# update-native-sdk.sh
#
# Updates the version of the upstream Datadog native SDKs (iOS and/or Android)
# across all files that reference them, and writes the new version to
# versions.properties so other scripts pick it up automatically.
#
# Usage:
#   ./update-native-sdk.sh --ios <version>
#   ./update-native-sdk.sh --android <version>
#   ./update-native-sdk.sh --ios <version> --android <version>
#
# Files modified by --ios:
#   versions.properties
#   native-wrappers/ios/DatadogWrapper/Package.swift
#
# Files modified by --android:
#   versions.properties
#   native-wrappers/android/datadogwrapper/build.gradle.kts
#   bindings/DatadogSdk.Android.Internal/DatadogSdk.Android.Internal.csproj
#   bindings/DatadogSdk.Android.Core/DatadogSdk.Android.Core.csproj
#   bindings/DatadogSdk.Android.Logs/DatadogSdk.Android.Logs.csproj
#   bindings/DatadogSdk.Android.Trace/DatadogSdk.Android.Trace.csproj

set -e

# ── Colors ────────────────────────────────────────────────────────────────────

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_section() { echo ""; echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; echo -e "${BLUE}▶ $1${NC}"; echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; }
log_info()    { echo -e "${GREEN}✓${NC} $1"; }
log_warning() { echo -e "${YELLOW}⚠${NC} $1"; }
log_error()   { echo -e "${RED}✗${NC} $1"; }

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# ── Usage ─────────────────────────────────────────────────────────────────────

usage() {
    echo "Usage: ./update-native-sdk.sh --ios <version> --android <version>"
    echo ""
    echo "Updates the upstream Datadog native SDK versions across the repository."
    echo ""
    echo "Flags:"
    echo "  --ios <version>      Set dd-sdk-ios version (e.g., 3.6.0)"
    echo "  --android <version>  Set dd-sdk-android version (e.g., 3.6.0)"
    echo ""
    echo "At least one flag is required. Both can be used together."
    echo ""
    echo "Current versions (from versions.properties):"
    source "$SCRIPT_DIR/versions.properties"
    echo "  iOS:     $IOS_NATIVE_VERSION"
    echo "  Android: $ANDROID_NATIVE_VERSION"
}

# ── Argument parsing ──────────────────────────────────────────────────────────

NEW_IOS=""
NEW_ANDROID=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --ios)
            NEW_IOS="$2"
            shift 2
            ;;
        --android)
            NEW_ANDROID="$2"
            shift 2
            ;;
        *)
            log_error "Unknown option: $1"
            echo ""
            usage
            exit 1
            ;;
    esac
done

if [ -z "$NEW_IOS" ] && [ -z "$NEW_ANDROID" ]; then
    usage
    exit 1
fi

# ── Validate ──────────────────────────────────────────────────────────────────

validate_version() {
    local version="$1"
    local label="$2"
    if ! echo "$version" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9][A-Za-z0-9.\-]*)?$'; then
        log_error "Invalid $label version: $version"
        echo "Expected: MAJOR.MINOR.PATCH (e.g., 3.6.0)"
        exit 1
    fi
}

[ -n "$NEW_IOS" ]     && validate_version "$NEW_IOS" "iOS"
[ -n "$NEW_ANDROID" ] && validate_version "$NEW_ANDROID" "Android"

# ── Load current versions ────────────────────────────────────────────────────

VERSIONS_FILE="$SCRIPT_DIR/versions.properties"

if [ ! -f "$VERSIONS_FILE" ]; then
    log_error "versions.properties not found at: $VERSIONS_FILE"
    exit 1
fi

source "$VERSIONS_FILE"

echo ""
[ -n "$NEW_IOS" ]     && echo -e "iOS SDK:     ${YELLOW}$IOS_NATIVE_VERSION${NC} → ${GREEN}$NEW_IOS${NC}"
[ -n "$NEW_ANDROID" ] && echo -e "Android SDK: ${YELLOW}$ANDROID_NATIVE_VERSION${NC} → ${GREEN}$NEW_ANDROID${NC}"

# ── Helper: replace in file ───────────────────────────────────────────────────

replace_in_file() {
    local file="$1"
    local regex="$2"
    local label="${3:-$file}"

    if [ ! -f "$file" ]; then
        log_error "File not found: $file"
        return 1
    fi

    perl -pi -e "$regex" "$file"
    log_info "$label"
}

# ── iOS updates ───────────────────────────────────────────────────────────────

if [ -n "$NEW_IOS" ]; then
    if [ "$IOS_NATIVE_VERSION" = "$NEW_IOS" ]; then
        log_warning "iOS SDK already at $NEW_IOS — skipping"
    else
        log_section "iOS native SDK → $NEW_IOS"

        ESC_IOS=$(echo "$IOS_NATIVE_VERSION" | sed 's/\./\\./g')

        # versions.properties
        replace_in_file "$VERSIONS_FILE" \
            "s|^IOS_NATIVE_VERSION=${ESC_IOS}$|IOS_NATIVE_VERSION=${NEW_IOS}|" \
            "versions.properties (IOS_NATIVE_VERSION)"

        # Package.swift: .package(url: "...", from: "X.Y.Z")
        replace_in_file "$SCRIPT_DIR/native-wrappers/ios/DatadogWrapper/Package.swift" \
            "s|(from: \")${ESC_IOS}(\")|\${1}${NEW_IOS}\${2}|g" \
            "native-wrappers/ios/DatadogWrapper/Package.swift"
    fi
fi

# ── Android updates ───────────────────────────────────────────────────────────

if [ -n "$NEW_ANDROID" ]; then
    if [ "$ANDROID_NATIVE_VERSION" = "$NEW_ANDROID" ]; then
        log_warning "Android SDK already at $NEW_ANDROID — skipping"
    else
        log_section "Android native SDK → $NEW_ANDROID"

        ESC_ANDROID=$(echo "$ANDROID_NATIVE_VERSION" | sed 's/\./\\./g')

        # versions.properties
        replace_in_file "$VERSIONS_FILE" \
            "s|^ANDROID_NATIVE_VERSION=${ESC_ANDROID}$|ANDROID_NATIVE_VERSION=${NEW_ANDROID}|" \
            "versions.properties (ANDROID_NATIVE_VERSION)"

        # build.gradle.kts
        replace_in_file "$SCRIPT_DIR/native-wrappers/android/datadogwrapper/build.gradle.kts" \
            "s|(com\.datadoghq:dd-sdk-android-[a-z-]+:)${ESC_ANDROID}|\${1}${NEW_ANDROID}|g" \
            "native-wrappers/android/datadogwrapper/build.gradle.kts"

        # Android binding csproj files (AndroidMavenLibrary Version=)
        for rel_path in \
            "bindings/DatadogSdk.Android.Internal/DatadogSdk.Android.Internal.csproj" \
            "bindings/DatadogSdk.Android.Core/DatadogSdk.Android.Core.csproj" \
            "bindings/DatadogSdk.Android.Logs/DatadogSdk.Android.Logs.csproj" \
            "bindings/DatadogSdk.Android.Trace/DatadogSdk.Android.Trace.csproj"; do

            replace_in_file "$SCRIPT_DIR/$rel_path" \
                "s|(Include=\"com\.datadoghq:[^\"]+\"\s+Version=\")${ESC_ANDROID}(\")|\${1}${NEW_ANDROID}\${2}|g" \
                "$rel_path"
        done
    fi
fi

# ── Resolve transitive dependencies ───────────────────────────────────────────

if [ -n "$NEW_ANDROID" ] && [ "$ANDROID_NATIVE_VERSION" != "$NEW_ANDROID" ]; then
    log_section "Resolving Android transitive dependencies"
    log_info "Running resolve-android-deps.sh to detect transitive dependency changes..."
    echo ""

    RESOLVE_EXIT=0
    "$SCRIPT_DIR/resolve-android-deps.sh" || RESOLVE_EXIT=$?

    if [ "$RESOLVE_EXIT" -eq 2 ]; then
        log_warning "Some NuGet PackageReference versions may need manual review (see above)"
    elif [ "$RESOLVE_EXIT" -ne 0 ]; then
        log_error "Transitive dependency resolution encountered an error"
    fi
fi

# ── Done ──────────────────────────────────────────────────────────────────────

log_section "Done"

echo "Native SDK version(s) updated."
echo ""
echo "Next steps:"
echo "  1. Review any transitive dependency warnings above"
echo "  2. Run ./build.sh to rebuild the native wrappers and bindings"
echo "  3. Run ./verify-artifacts.sh to validate the build output"
echo ""
