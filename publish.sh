#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
#
# publish.sh
#
# Publishes all Datadog MAUI SDK NuGet packages to the NuGet registry in the
# correct dependency order.
#
# Usage:
#   ./publish.sh [OPTIONS]
#
# Options:
#   --api-key <key>    NuGet API key (or set NUGET_API_KEY env var)
#   --source <url>     NuGet source URL (default: https://api.nuget.org/v3/index.json)
#   --packages-dir <path>  Directory containing .nupkg files (default: ./local-packages)
#   --dry-run          Show what would be pushed without pushing
#   -h, --help         Show this help message
#
# Publish order (dependency order — each package must exist before its dependents):
#   1. DatadogSdk.Android.Internal
#   2. DatadogSdk.iOS.Binding
#   3. DatadogSdk.Android.Core
#   4. DatadogSdk.Android.Logs
#   5. DatadogSdk.Android.Binding
#   6. DatadogSdk.Maui

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
cd "$SCRIPT_DIR"

# ── Argument parsing ──────────────────────────────────────────────────────────

API_KEY="${NUGET_API_KEY:-}"
SOURCE="https://api.nuget.org/v3/index.json"
PACKAGES_DIR="$SCRIPT_DIR/local-packages"
DRY_RUN=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --api-key)
            API_KEY="$2"
            shift 2
            ;;
        --source)
            SOURCE="$2"
            shift 2
            ;;
        --packages-dir)
            PACKAGES_DIR="$2"
            shift 2
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./publish.sh [OPTIONS]"
            echo ""
            echo "Publishes all SDK NuGet packages to the registry in dependency order."
            echo ""
            echo "Options:"
            echo "  --api-key <key>       NuGet API key (or set NUGET_API_KEY env var)"
            echo "  --source <url>        NuGet source URL"
            echo "                        (default: https://api.nuget.org/v3/index.json)"
            echo "  --packages-dir <path> Directory containing .nupkg files"
            echo "                        (default: ./local-packages)"
            echo "  --dry-run             Show what would be pushed without pushing"
            echo "  -h, --help            Show this help message"
            echo ""
            echo "The NUGET_API_KEY environment variable is used if --api-key is not provided."
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            echo "Run ./publish.sh --help for usage"
            exit 1
            ;;
    esac
done

# ── Validate ──────────────────────────────────────────────────────────────────

if [ "$DRY_RUN" = false ] && [ -z "$API_KEY" ]; then
    log_error "NuGet API key is required. Provide it via --api-key or NUGET_API_KEY env var."
    exit 1
fi

if [ ! -d "$PACKAGES_DIR" ]; then
    log_error "Packages directory not found: $PACKAGES_DIR"
    echo "Run ./prepare-release.sh first to build the packages."
    exit 1
fi

# ── Read current version ──────────────────────────────────────────────────────

source "$SCRIPT_DIR/versions.properties"

# ── Package list (dependency order) ──────────────────────────────────────────
#
# Each package must be published before any package that depends on it.
# NuGet resolves dependencies at restore time, so dependents must find their
# dependencies already in the registry.

PACKAGES=(
    "DatadogSdk.Android.Internal.${SDK_VERSION}.nupkg"
    "DatadogSdk.iOS.Binding.${SDK_VERSION}.nupkg"
    "DatadogSdk.Android.Core.${SDK_VERSION}.nupkg"
    "DatadogSdk.Android.Logs.${SDK_VERSION}.nupkg"
    "DatadogSdk.Android.Trace.${SDK_VERSION}.nupkg"
    "DatadogSdk.Android.Rum.${SDK_VERSION}.nupkg"
    "DatadogSdk.Android.SessionReplay.${SDK_VERSION}.nupkg"
    "DatadogSdk.Android.Binding.${SDK_VERSION}.nupkg"
    "DatadogSdk.Maui.${SDK_VERSION}.nupkg"
)

# ── Banner ────────────────────────────────────────────────────────────────────

echo ""
echo -e "${BLUE}┌─────────────────────────────────────────────────────┐${NC}"
echo -e "${BLUE}│  Publishing DatadogSdk.Maui ${SDK_VERSION} to NuGet${NC}"
echo -e "${BLUE}└─────────────────────────────────────────────────────┘${NC}"
echo ""
echo -e "  Source: ${YELLOW}$SOURCE${NC}"
echo -e "  From:   ${YELLOW}$PACKAGES_DIR${NC}"
[ "$DRY_RUN" = true ] && echo -e "  ${YELLOW}(dry-run: no packages will be pushed)${NC}"

# ── Pre-flight: verify all packages exist ─────────────────────────────────────

log_section "Pre-flight: verifying packages exist"

MISSING=0
for pkg in "${PACKAGES[@]}"; do
    path="$PACKAGES_DIR/$pkg"
    if [ -f "$path" ]; then
        size=$(du -sh "$path" 2>/dev/null | cut -f1)
        printf "  ${GREEN}✓${NC}  %-55s %s\n" "$pkg" "$size"
    else
        log_error "Missing: $pkg"
        MISSING=1
    fi
done

if [ "$MISSING" -ne 0 ]; then
    echo ""
    log_error "One or more packages are missing. Run ./prepare-release.sh first."
    exit 1
fi

# ── Publish ───────────────────────────────────────────────────────────────────

log_section "Publishing packages"

FAILED=0

for pkg in "${PACKAGES[@]}"; do
    path="$PACKAGES_DIR/$pkg"

    if [ "$DRY_RUN" = true ]; then
        echo -e "  ${YELLOW}[dry-run]${NC} Would push: $pkg"
    else
        echo -e "  Pushing ${YELLOW}$pkg${NC}..."
        if dotnet nuget push "$path" \
            --source "$SOURCE" \
            --api-key "$API_KEY" \
            --skip-duplicate; then
            log_info "$pkg"
        else
            log_error "$pkg — push failed"
            FAILED=1
        fi
    fi
done

# ── Summary ───────────────────────────────────────────────────────────────────

log_section "Summary"

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}Dry-run complete — no packages were pushed.${NC}"
    echo "Remove --dry-run to publish."
elif [ "$FAILED" -ne 0 ]; then
    echo -e "${RED}Some packages failed to publish. Check the output above.${NC}"
    exit 1
else
    log_info "All ${#PACKAGES[@]} packages published successfully at version $SDK_VERSION"
    echo ""
    echo -e "Users can now add the SDK with:"
    echo -e "  ${YELLOW}dotnet add package DatadogSdk.Maui${NC}"
fi

# ── Upload SDK framework symbols ─────────────────────────────────────────────

if [ -x "$SCRIPT_DIR/upload-sdk-symbols.sh" ]; then
    if [ "$DRY_RUN" = true ]; then
        "$SCRIPT_DIR/upload-sdk-symbols.sh" --dry-run
    else
        "$SCRIPT_DIR/upload-sdk-symbols.sh"
    fi
fi
echo ""
