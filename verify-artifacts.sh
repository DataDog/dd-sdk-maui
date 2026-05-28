#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 Datadog, Inc.
#
# verify-artifacts.sh
#
# Validates build artifacts produced by build.sh:
#   - NuGet packages exist in local-packages/
#   - ProGuard rules contain the required keep directives
#   - NuGet package structure includes the .targets + merged rules for propagation
#
# Run after build.sh. Does NOT run unit tests (use check.sh for that).
#
# Usage:
#   ./verify-artifacts.sh              # Run all checks
#   ./verify-artifacts.sh --nuget      # NuGet existence + structure only
#   ./verify-artifacts.sh --proguard   # ProGuard rules content only
#   ./verify-artifacts.sh -h           # Help

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_section() {
    echo ""
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}▶ $1${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

log_pass() { echo -e "  ${GREEN}✓${NC} $1"; }
log_fail() { echo -e "  ${RED}✗${NC} $1"; }
log_warn() { echo -e "  ${YELLOW}⚠${NC} $1"; }

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Load version references
source "$SCRIPT_DIR/versions.properties"

# ── Argument parsing ─────────────────────────────────────────────────────────

RUN_NUGET=false
RUN_PROGUARD=false
RUN_DEPS=false
RUN_ALL=true

while [[ $# -gt 0 ]]; do
    case $1 in
        --nuget)
            RUN_NUGET=true
            RUN_ALL=false
            shift
            ;;
        --proguard)
            RUN_PROGUARD=true
            RUN_ALL=false
            shift
            ;;
        --deps)
            RUN_DEPS=true
            RUN_ALL=false
            shift
            ;;
        -h|--help)
            echo "Usage: ./verify-artifacts.sh [OPTIONS]"
            echo ""
            echo "Validates build artifacts produced by build.sh."
            echo "With no options, all checks are run."
            echo ""
            echo "Options:"
            echo "  --nuget      Check NuGet package existence and structure"
            echo "  --proguard   Check ProGuard rules content and completeness"
            echo "  --deps       Check Android transitive dependency version alignment"
            echo "  -h, --help   Show this help message"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Run ./verify-artifacts.sh --help for usage"
            exit 1
            ;;
    esac
done

if [ "$RUN_ALL" = true ]; then
    RUN_NUGET=true
    RUN_PROGUARD=true
    RUN_DEPS=true
fi

FAILED=0

# ── Helper: assert a file exists ─────────────────────────────────────────────

assert_file() {
    local label="$1"
    local path="$2"
    if [ -f "$path" ]; then
        log_pass "$label"
        return 0
    else
        log_fail "$label  →  not found: $path"
        FAILED=1
        return 1
    fi
}

# ── Helper: assert a pattern exists in a file ────────────────────────────────

assert_pattern() {
    local label="$1"
    local file="$2"
    local pattern="$3"
    if grep -q "$pattern" "$file" 2>/dev/null; then
        log_pass "$label"
        return 0
    else
        log_fail "$label  →  pattern not found: $pattern"
        FAILED=1
        return 1
    fi
}

# ── Helper: assert a path exists inside a .nupkg ─────────────────────────────

assert_nupkg_contains() {
    local label="$1"
    local nupkg="$2"
    local inner_path="$3"
    if unzip -l "$nupkg" 2>/dev/null | grep -q "$inner_path"; then
        log_pass "$label"
        return 0
    else
        log_fail "$label  →  missing from $(basename "$nupkg"): $inner_path"
        FAILED=1
        return 1
    fi
}

# ════════════════════════════════════════════════════════════════════════════
# NUGET CHECKS
# ════════════════════════════════════════════════════════════════════════════

NUGET_RESULT=0

if [ "$RUN_NUGET" = true ]; then

    # ── 1. All packages must be present ──────────────────────────────────────
    log_section "NuGet packages exist"

    PACKAGES=(
        "DatadogSdk.iOS.Binding.${SDK_VERSION}.nupkg"
        "DatadogSdk.Android.Internal.${SDK_VERSION}.nupkg"
        "DatadogSdk.Android.Core.${SDK_VERSION}.nupkg"
        "DatadogSdk.Android.Logs.${SDK_VERSION}.nupkg"
        "DatadogSdk.Android.Trace.${SDK_VERSION}.nupkg"
        "DatadogSdk.Android.Rum.${SDK_VERSION}.nupkg"
        "DatadogSdk.Android.SessionReplay.${SDK_VERSION}.nupkg"
        "DatadogSdk.Android.Binding.${SDK_VERSION}.nupkg"
        "DatadogSdk.Maui.${SDK_VERSION}.nupkg"
    )

    for pkg in "${PACKAGES[@]}"; do
        assert_file "$pkg" "local-packages/$pkg" || NUGET_RESULT=1
    done

    # ── 2. Android Binding NuGet structure ───────────────────────────────────
    # These entries will be absent until the ProGuard extraction step is
    # implemented in build.sh and the .csproj is updated to pack them.
    log_section "DatadogSdk.Android.Binding NuGet structure"

    BINDING_PKG="local-packages/DatadogSdk.Android.Binding.${SDK_VERSION}.nupkg"

    if [ -f "$BINDING_PKG" ]; then
        # Assembly must always be present
        assert_nupkg_contains \
            "lib/.../DatadogSdk.Android.Binding.dll" \
            "$BINDING_PKG" \
            "lib/.*DatadogSdk.Android.Binding.dll" || NUGET_RESULT=1

        # .targets file — injects ProGuard rules into consuming app builds
        assert_nupkg_contains \
            "buildTransitive/DatadogSdk.Android.Binding.targets" \
            "$BINDING_PKG" \
            "buildTransitive/.*DatadogSdk.Android.Binding.targets" || NUGET_RESULT=1

        # Merged ProGuard rules file — must be under buildTransitive alongside .targets
        assert_nupkg_contains \
            "buildTransitive/.../datadog-merged.pro" \
            "$BINDING_PKG" \
            "buildTransitive/.*datadog-merged.pro" || NUGET_RESULT=1
    else
        log_warn "Skipping structure checks — DatadogSdk.Android.Binding.${SDK_VERSION}.nupkg not found"
        NUGET_RESULT=1
    fi

fi

# ════════════════════════════════════════════════════════════════════════════
# PROGUARD CHECKS
# ════════════════════════════════════════════════════════════════════════════

PROGUARD_RESULT=0

if [ "$RUN_PROGUARD" = true ]; then

    # ── 3. Current proguard.txt baseline ─────────────────────────────────────
    # These rules are already committed. They must never regress.
    log_section "ProGuard baseline rules (Transforms/proguard.txt)"

    CURRENT_RULES="bindings/DatadogSdk.Android.Binding/Transforms/proguard.txt"

    if [ -f "$CURRENT_RULES" ]; then
        assert_pattern "keep com.datadog.** classes"      "$CURRENT_RULES" "keep class com.datadog\.\*\*"       || PROGUARD_RESULT=1
        assert_pattern "keep com.datadog.** interfaces"   "$CURRENT_RULES" "keep interface com.datadog\.\*\*"   || PROGUARD_RESULT=1
        # Kotlin metadata — required for Kotlin stdlib reflection
        assert_pattern "keep kotlin.Metadata"             "$CURRENT_RULES" "kotlin.Metadata"                    || PROGUARD_RESULT=1
        # JvmStatic annotation — ensures @JvmStatic methods are retained
        assert_pattern "keepclassmembers @JvmStatic"      "$CURRENT_RULES" "@kotlin.jvm.JvmStatic"              || PROGUARD_RESULT=1
    else
        log_fail "Transforms/proguard.txt not found — has it been deleted?"
        PROGUARD_RESULT=1
    fi

    # ── 4. Merged ProGuard rules file ─────────────────────────────────────────
    # This file is generated by the extraction step in build.sh (not yet
    # implemented). These checks will fail until that step exists.
    log_section "Merged ProGuard rules (proguard/datadog-merged.pro)"

    MERGED_RULES="bindings/DatadogSdk.Android.Binding/proguard/datadog-merged.pro"

    if [ -f "$MERGED_RULES" ]; then
        assert_pattern "keep com.datadog.** classes"      "$MERGED_RULES" "keep class com.datadog\.\*\*"       || PROGUARD_RESULT=1
        assert_pattern "keep com.datadog.** interfaces"   "$MERGED_RULES" "keep interface com.datadog\.\*\*"   || PROGUARD_RESULT=1
        assert_pattern "keep kotlin.Metadata"             "$MERGED_RULES" "kotlin.Metadata"                    || PROGUARD_RESULT=1
        # At least the Datadog namespace must be protected
        assert_pattern "upstream Datadog namespace kept"  "$MERGED_RULES" "com.datadog."                        || PROGUARD_RESULT=1
    else
        log_fail "proguard/datadog-merged.pro not found"
        log_warn "  This file is generated by the ProGuard extraction step in build.sh."
        log_warn "  Implement that step and re-run build.sh to generate it."
        PROGUARD_RESULT=1
    fi

fi

# ════════════════════════════════════════════════════════════════════════════
# TRANSITIVE DEPENDENCY CHECKS
# ════════════════════════════════════════════════════════════════════════════

DEPS_RESULT=0

if [ "$RUN_DEPS" = true ]; then

    log_section "Android transitive dependency alignment"

    if [ -f "$SCRIPT_DIR/resolve-android-deps.sh" ] && [ -f "$SCRIPT_DIR/android-transitive-deps.json" ]; then
        if "$SCRIPT_DIR/resolve-android-deps.sh" --check 2>&1 | tail -n +7; then
            log_pass "Transitive dependency versions match the mapping"
        else
            log_fail "Transitive dependency drift detected"
            log_warn "  Run ./resolve-android-deps.sh to update the mapping and csproj files"
            DEPS_RESULT=1
        fi
    else
        log_warn "resolve-android-deps.sh or android-transitive-deps.json not found — skipping"
    fi

fi

# ════════════════════════════════════════════════════════════════════════════
# Summary
# ════════════════════════════════════════════════════════════════════════════

log_section "Results"

if [ "$RUN_NUGET" = true ]; then
    if [ "$NUGET_RESULT" -eq 0 ]; then
        echo -e "  ${GREEN}✓${NC} NuGet checks"
    else
        echo -e "  ${RED}✗${NC} NuGet checks"
    fi
fi

if [ "$RUN_PROGUARD" = true ]; then
    if [ "$PROGUARD_RESULT" -eq 0 ]; then
        echo -e "  ${GREEN}✓${NC} ProGuard checks"
    else
        echo -e "  ${RED}✗${NC} ProGuard checks"
    fi
fi

if [ "$RUN_DEPS" = true ]; then
    if [ "$DEPS_RESULT" -eq 0 ]; then
        echo -e "  ${GREEN}✓${NC} Dependency alignment checks"
    else
        echo -e "  ${RED}✗${NC} Dependency alignment checks"
    fi
fi

echo ""

[ "$NUGET_RESULT" -ne 0 ] && FAILED=1
[ "$PROGUARD_RESULT" -ne 0 ] && FAILED=1
[ "$DEPS_RESULT" -ne 0 ] && FAILED=1

if [ "$FAILED" -ne 0 ]; then
    echo -e "${RED}Some checks failed.${NC}"
    exit 1
else
    echo -e "${GREEN}All checks passed.${NC}"
fi
