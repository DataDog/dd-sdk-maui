#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 Datadog, Inc.
#
# resolve-android-deps.sh
#
# Resolves the Android native wrapper's transitive dependency tree via Gradle
# and synchronises android-transitive-deps.json + csproj files.
#
# What it does:
#   1. Runs Gradle dependency resolution on releaseRuntimeClasspath
#   2. Delegates parsing, comparison, and updates to resolve-android-deps.py
#      (extracts Maven versions, compares against android-transitive-deps.json,
#       auto-updates csproj files, and warns about NuGet drift)
#
# Usage:
#   ./resolve-android-deps.sh            # resolve + update
#   ./resolve-android-deps.sh --check    # resolve + report only (no file changes)

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
DEPS_JSON="$SCRIPT_DIR/android-transitive-deps.json"
GRADLE_DIR="$SCRIPT_DIR/native-wrappers/android"
BINDINGS_DIR="$SCRIPT_DIR/bindings"

# ── Arguments ─────────────────────────────────────────────────────────────────

CHECK_ONLY=false
if [ "${1:-}" = "--check" ]; then
    CHECK_ONLY=true
fi

# ── Prerequisite checks ──────────────────────────────────────────────────────

if [ ! -f "$DEPS_JSON" ]; then
    log_error "android-transitive-deps.json not found at: $DEPS_JSON"
    exit 1
fi

if ! command -v python3 &> /dev/null; then
    log_error "python3 is required but not found"
    exit 1
fi

# ── Step 1: Resolve Gradle dependency tree ────────────────────────────────────

log_section "Resolving Gradle dependency tree"

GRADLE_OUTPUT=$(cd "$GRADLE_DIR" && ./gradlew :datadogwrapper:dependencies --configuration releaseRuntimeClasspath 2>/dev/null)

if [ -z "$GRADLE_OUTPUT" ]; then
    log_error "Gradle dependency resolution returned no output"
    exit 1
fi

log_info "Gradle dependency tree resolved"

# ── Steps 2–7: Parse, compare, and update via Python ─────────────────────────

log_section "Analysing dependency changes"

export GRADLE_OUTPUT
PYTHON_EXIT=0
python3 "$SCRIPT_DIR/resolve-android-deps.py" "$DEPS_JSON" "$BINDINGS_DIR" "$CHECK_ONLY" || PYTHON_EXIT=$?

# ── Summary ───────────────────────────────────────────────────────────────────

if [ "$PYTHON_EXIT" -eq 2 ]; then
    log_section "Done (with warnings)"
    echo ""
    echo "AndroidMavenLibrary versions have been auto-updated."
    echo "Review the warnings above and update NuGet PackageReference versions manually."
elif [ "$PYTHON_EXIT" -ne 0 ]; then
    log_section "Done (drift detected)"
    exit 1
else
    log_section "Done"
fi
