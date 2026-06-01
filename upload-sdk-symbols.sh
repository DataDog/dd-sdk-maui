#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
#
# upload-sdk-symbols.sh
#
# Uploads DatadogWrapper debug symbols (dSYMs) to Datadog.
# Run this when releasing a new version of the SDK — the symbols are shared
# across all consumer apps using the same NuGet package version.
#
# Prerequisites:
#   - Run ./build.sh first to generate the dSYMs
#   - Install datadog-ci: npm install -g @datadog/datadog-ci
#   - Export DATADOG_API_KEY in your shell
#
# Usage:
#   ./upload-sdk-symbols.sh
#   ./upload-sdk-symbols.sh --dry-run

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
log_warn()    { echo -e "${YELLOW}!${NC} $1"; }

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# ── Arguments ─────────────────────────────────────────────────────────────────

DRY_RUN=""
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run) DRY_RUN="--dry-run"; shift ;;
        *) echo "Usage: ./upload-sdk-symbols.sh [--dry-run]"; exit 1 ;;
    esac
done

# ── Prerequisites ─────────────────────────────────────────────────────────────

log_section "Checking prerequisites"

if ! command -v datadog-ci &>/dev/null; then
    log_error "datadog-ci is not installed. Run: npm install -g @datadog/datadog-ci"
    exit 1
fi
log_info "datadog-ci found"

if [ -z "$DATADOG_API_KEY" ]; then
    log_error "DATADOG_API_KEY is not set. Export it in your shell."
    exit 1
fi
log_info "DATADOG_API_KEY is set"

DSYM_DIR="bindings/Datadog.iOS.Binding/NativeReference/dSYMs"

if [ ! -d "$DSYM_DIR" ]; then
    log_error "dSYMs not found at $DSYM_DIR. Run ./build.sh first."
    exit 1
fi

# ── Collect symbols ──────────────────────────────────────────────────────────

log_section "SDK Debug Symbols"

DSYM_COUNT=$(find "$DSYM_DIR" -maxdepth 1 -name "*.dSYM" -type d 2>/dev/null | wc -l | tr -d ' ')

if [ "$DSYM_COUNT" -eq 0 ]; then
    log_error "No dSYM bundles found in $DSYM_DIR"
    exit 1
fi

log_info "Found $DSYM_COUNT dSYM(s):"
find "$DSYM_DIR" -maxdepth 1 -name "*.dSYM" -type d | while read -r dsym; do
    UUID=$(dwarfdump --uuid "$dsym" 2>/dev/null | awk '{print $2}' | head -1)
    echo "         $(basename "$dsym") (UUID: ${UUID:-unknown})"
done

# ── Upload ───────────────────────────────────────────────────────────────────

log_section "Uploading to Datadog"

SITE="${DATADOG_SITE:-datadoghq.com}"

if [ -n "$DRY_RUN" ]; then
    log_warn "Dry run — no files will be uploaded"
fi

datadog-ci dsyms upload "$DSYM_DIR" $DRY_RUN

# ── Summary ──────────────────────────────────────────────────────────────────

log_section "Upload complete"
log_info "Site: $SITE"
log_info "dSYMs uploaded: $DSYM_COUNT"
if [ -n "$DRY_RUN" ]; then
    log_warn "This was a dry run. Re-run without --dry-run to upload."
fi
echo ""
