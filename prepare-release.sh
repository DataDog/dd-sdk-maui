#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 Datadog, Inc.
#
# prepare-release.sh
#
# Orchestrates a full SDK release: runs tests, builds all NuGet packages,
# and verifies the resulting artifacts.
#
# Usage:
#   ./prepare-release.sh [OPTIONS]
#
# Options:
#   --skip-tests     Skip running check.sh before building
#   --dry-run        Print what would be done without executing build steps
#   -h, --help       Show this help message

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

SKIP_TESTS=false
DRY_RUN=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./prepare-release.sh [OPTIONS]"
            echo ""
            echo "Orchestrates a full SDK release:"
            echo "  1. Runs all test suites (unless --skip-tests)"
            echo "  2. Builds all NuGet packages via build.sh"
            echo "  3. Verifies artifacts via verify-artifacts.sh"
            echo ""
            echo "Options:"
            echo "  --skip-tests     Skip running check.sh before building"
            echo "  --dry-run        Print steps without executing them"
            echo "  -h, --help       Show this help message"
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            echo "Run ./release.sh --help for usage"
            exit 1
            ;;
    esac
done

# ── Read current version ──────────────────────────────────────────────────────

source "$SCRIPT_DIR/versions.properties"

# ── Banner ────────────────────────────────────────────────────────────────────

echo ""
echo -e "${BLUE}┌─────────────────────────────────────────────────────┐${NC}"
echo -e "${BLUE}│  Datadog MAUI SDK — Release ${SDK_VERSION}${NC}"
echo -e "${BLUE}└─────────────────────────────────────────────────────┘${NC}"
[ "$SKIP_TESTS" = true ] && log_warning "Tests will be skipped (--skip-tests)"
[ "$DRY_RUN" = true ]    && echo -e "${YELLOW}(dry-run mode: build steps will not execute)${NC}"

# ── Step helpers ──────────────────────────────────────────────────────────────

run_step() {
    local label="$1"
    shift
    log_section "$label"
    if [ "$DRY_RUN" = true ]; then
        echo -e "${YELLOW}[dry-run]${NC} Would run: $*"
    else
        "$@"
    fi
}

# ── 1. Build ──────────────────────────────────────────────────────────────────

run_step "Building all packages" "$SCRIPT_DIR/build.sh"

# ── 2. Verify artifacts ───────────────────────────────────────────────────────

run_step "Verifying artifacts" "$SCRIPT_DIR/verify-artifacts.sh"

# ── 3. Tests ──────────────────────────────────────────────────────────────────

if [ "$SKIP_TESTS" = false ]; then
    run_step "Running test suites" "$SCRIPT_DIR/check.sh"
else
    log_section "Tests"
    log_warning "Skipping tests (--skip-tests)"
fi

# ── 4. Summary ────────────────────────────────────────────────────────────────

log_section "Release summary — DatadogSdk.Maui $SDK_VERSION"

if [ "$DRY_RUN" = false ]; then
    echo ""
    echo "Packages produced in ./local-packages/:"
    echo ""

    TOTAL_SIZE=0

    for pkg in "$SCRIPT_DIR/local-packages/"DatadogSdk.*.nupkg; do
        if [ -f "$pkg" ]; then
            size=$(du -sh "$pkg" 2>/dev/null | cut -f1)
            printf "  ${GREEN}✓${NC}  %-55s %s\n" "$(basename "$pkg")" "$size"
        fi
    done

    echo ""
    log_info "Release build complete for version $SDK_VERSION"
    echo ""
    echo "To publish to NuGet.org:"
    echo -e "  ${YELLOW}./publish.sh --api-key <YOUR_API_KEY>${NC}"
else
    echo -e "${YELLOW}Dry-run complete — no packages were built.${NC}"
fi
echo ""
