#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
#
# bump-version.sh
#
# Bumps the MAUI SDK version across versions.properties, all binding .csproj
# files, internal cross-references, and the example app.
#
# Usage:
#   ./bump-version.sh <new-version>
#
# Arguments:
#   new-version    Semver version (e.g., 1.2.0 or 1.2.0-beta.1)
#
# Files modified:
#   versions.properties
#   bindings/Datadog.*/Datadog.*.csproj   — <Version> tag
#   bindings/Datadog.Android.Binding/*.csproj — PackageReference to Datadog.Android.Logs
#   bindings/Datadog.Maui/*.csproj            — PackageReferences to iOS/Android bindings
#   example/example.csproj                       — PackageReference to Datadog.Maui

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
    echo "Usage: ./bump-version.sh <new-version>"
    echo ""
    echo "Bumps the MAUI SDK version across all binding .csproj files,"
    echo "cross-references, and the example app."
    echo ""
    echo "Arguments:"
    echo "  new-version    Semver version (e.g., 1.2.0 or 1.2.0-beta.1)"
    echo ""
    echo "Current version (from versions.properties):"
    source "$SCRIPT_DIR/versions.properties"
    echo "  SDK: $SDK_VERSION"
}

# ── Argument parsing ──────────────────────────────────────────────────────────

NEW_VERSION=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -*)
            log_error "Unknown option: $1"
            echo ""
            usage
            exit 1
            ;;
        *)
            if [ -z "$NEW_VERSION" ]; then
                NEW_VERSION="$1"
            else
                log_error "Unexpected argument: $1"
                exit 1
            fi
            shift
            ;;
    esac
done

if [ -z "$NEW_VERSION" ]; then
    usage
    exit 1
fi

# ── Validate ──────────────────────────────────────────────────────────────────

if ! echo "$NEW_VERSION" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9][A-Za-z0-9.\-]*)?$'; then
    log_error "Invalid version format: $NEW_VERSION"
    echo "Expected: MAJOR.MINOR.PATCH (e.g., 1.2.0) or MAJOR.MINOR.PATCH-suffix (e.g., 1.2.0-beta.1)"
    exit 1
fi

# ── Load current version ─────────────────────────────────────────────────────

VERSIONS_FILE="$SCRIPT_DIR/versions.properties"

if [ ! -f "$VERSIONS_FILE" ]; then
    log_error "versions.properties not found at: $VERSIONS_FILE"
    exit 1
fi

source "$VERSIONS_FILE"
CURRENT_VERSION="$SDK_VERSION"

if [ "$CURRENT_VERSION" = "$NEW_VERSION" ]; then
    log_warning "Already at version $NEW_VERSION — nothing to do"
    exit 0
fi

echo ""
echo -e "Bumping MAUI SDK version: ${YELLOW}$CURRENT_VERSION${NC} → ${GREEN}$NEW_VERSION${NC}"

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

# ── Escape version for use in perl regex ─────────────────────────────────────

ESC_CURRENT=$(echo "$CURRENT_VERSION" | sed 's/\./\\./g')

# ── 1. versions.properties ───────────────────────────────────────────────────

log_section "versions.properties"

replace_in_file "$VERSIONS_FILE" \
    "s|^SDK_VERSION=${ESC_CURRENT}$|SDK_VERSION=${NEW_VERSION}|" \
    "versions.properties (SDK_VERSION)"

# ── 2. <Version> tag in all binding .csproj files ────────────────────────────

log_section "Binding .csproj <Version> tags"

BINDING_CSPROJ_FILES=(
    "bindings/Datadog.iOS.Binding/Datadog.iOS.Binding.csproj"
    "bindings/Datadog.Android.Internal/Datadog.Android.Internal.csproj"
    "bindings/Datadog.Android.Core/Datadog.Android.Core.csproj"
    "bindings/Datadog.Android.Logs/Datadog.Android.Logs.csproj"
    "bindings/Datadog.Android.Trace/Datadog.Android.Trace.csproj"
    "bindings/Datadog.Android.Rum/Datadog.Android.Rum.csproj"
    "bindings/Datadog.Android.SessionReplay/Datadog.Android.SessionReplay.csproj"
    "bindings/Datadog.Android.Binding/Datadog.Android.Binding.csproj"
    "bindings/Datadog.Maui/Datadog.Maui.csproj"
)

for rel_path in "${BINDING_CSPROJ_FILES[@]}"; do
    replace_in_file "$SCRIPT_DIR/$rel_path" \
        "s|<Version>${ESC_CURRENT}</Version>|<Version>${NEW_VERSION}</Version>|g" \
        "$rel_path"
done

# ── 3. Datadog.* PackageReference versions ────────────────────────────────

log_section "Internal PackageReference versions"

CROSS_REF_FILES=(
    "bindings/Datadog.Android.Binding/Datadog.Android.Binding.csproj"
    "bindings/Datadog.Maui/Datadog.Maui.csproj"
)

for rel_path in "${CROSS_REF_FILES[@]}"; do
    replace_in_file "$SCRIPT_DIR/$rel_path" \
        "s|(Include=\"Datadog\.[^\"]*\"\s+Version=\")${ESC_CURRENT}(\")|\${1}${NEW_VERSION}\${2}|g" \
        "$rel_path (cross-references)"
done

# ── 4. Example app Datadog.Maui PackageReference ─────────────────────────

log_section "Example app PackageReference"

replace_in_file "$SCRIPT_DIR/example/example.csproj" \
    "s|(Include=\"Datadog\.Maui\"\s+Version=\")${ESC_CURRENT}(\")|\${1}${NEW_VERSION}\${2}|g" \
    "example/example.csproj"

# ── 5. Version tracking table ────────────────────────────────────────────────

log_section "Version tracking"

"$SCRIPT_DIR/update-version-tracking.sh"

# ── Done ──────────────────────────────────────────────────────────────────────

log_section "Done"

log_info "Version bumped from $CURRENT_VERSION to $NEW_VERSION"
echo ""
echo "Next steps:"
echo "  1. Run ./build.sh to rebuild all packages at the new version"
echo "  2. Run ./verify-artifacts.sh to validate the new packages"
echo ""
