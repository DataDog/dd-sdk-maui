#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
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
#   - (Optional) export DATADOG_SITE in your shell to use a site other than the default one
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
SITE="${DATADOG_SITE:-}"   # reporting only — datadog-ci reads the env var itself

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

DD_ARGS=(-p:DatadogUploadSymbols=$UPLOAD_FLAG)

if [ "$UPLOAD" = true ]; then
    # The site is not passed through: DATADOG_SITE is inherited by datadog-ci, which
    # resolves it itself. Nothing here needs to know about it.
    #
    # This script exists to validate the symbol pipeline, so a failed upload must fail
    # the run rather than being downgraded to a warning the way an app build wants.
    DD_ARGS+=(-p:DatadogFailOnSymbolUploadError=true)

    log_info "Uploading symbols to site: ${SITE:-datadoghq.com (default)}"
fi

# The Datadog manifest targets log at Importance="low", which the console hides at
# -v n. Rather than raising the console to -v d (which buries the build in MSBuild
# internals), attach a file logger at detailed verbosity and pull the Datadog lines
# out of it for the summary. obj/ is gitignored.
mkdir -p obj
DETAILED_LOG="obj/publish-detailed.log"
LOG_ARGS=(/fl "/flp:verbosity=detailed;logfile=$DETAILED_LOG")

# Don't let `set -e` abort before the summary: the artifact paths below are the most
# useful output when an upload fails, so capture the exit code and report at the end.
set +e
if [ "$TARGET" = "ios" ]; then
    log_section "Publishing iOS Release (device)"

    dotnet publish -c Release -f net10.0-ios -r ios-arm64 \
        "${DD_ARGS[@]}" "${LOG_ARGS[@]}" \
        -v n -tl:off

elif [ "$TARGET" = "android" ]; then
    log_section "Publishing Android Release"

    dotnet publish -c Release -f net10.0-android \
        "${DD_ARGS[@]}" "${LOG_ARGS[@]}" \
        -v n -tl:off
fi
PUBLISH_EXIT=$?
set -e

# ── Summary ───────────────────────────────────────────────────────────────────

log_section "Publish complete"

# Reports the managed symbolication inputs: the portable PDBs that
# `datadog-ci ppdb-symbols upload` sends, and the debug-id manifest that maps
# each assembly simple name to the debug id of the assembly that actually
# shipped (post-trim/strip, so it differs from the PDB's own intrinsic id).
report_ppdb() {
    local out_dir="$1" obj_dir="$2" bundle_manifest="$3"
    local manifest="$obj_dir/dd_debug_ids.json"

    local pdbs
    pdbs=$(find "$out_dir" -maxdepth 1 -name '*.pdb' 2>/dev/null | sort)
    if [ -n "$pdbs" ]; then
        log_info "Portable PDBs in $out_dir:"
        while IFS= read -r pdb; do
            echo "    $(basename "$pdb") ($(wc -c < "$pdb" | tr -d ' ') bytes)"
        done <<< "$pdbs"
    else
        log_error "No portable PDBs found in $out_dir"
    fi

    if [ -f "$manifest" ]; then
        local entries
        entries=$(python3 -c 'import json,sys; print(len(json.load(open(sys.argv[1]))))' "$manifest" 2>/dev/null || echo "?")
        log_info "Debug-id manifest: $manifest ($entries assemblies)"
        if [ -n "$bundle_manifest" ] && [ -f "$bundle_manifest" ]; then
            log_info "Debug-id manifest (in app bundle): $bundle_manifest"
        fi
    else
        log_error "Debug-id manifest missing: $manifest"
    fi
}

# Reports which Datadog targets actually executed, plus their messages — including the
# Importance="low" lines the console suppresses at -v n. Reads the detailed log, where
# target entries appear as `Target "_DdFoo" in project ...`.
report_phases() {
    [ -f "$DETAILED_LOG" ] || return 0

    local targets messages
    targets=$(grep -oE 'Target "(_Dd|Datadog)[A-Za-z_]*"' "$DETAILED_LOG" \
              | sed 's/^Target "//; s/"$//' | sort -u)
    messages=$(grep -oE 'Datadog: .*' "$DETAILED_LOG" | sort -u)

    echo ""
    if [ -n "$targets" ]; then
        log_info "Datadog targets that ran:"
        while IFS= read -r t; do echo "    $t"; done <<< "$targets"
    else
        log_error "No Datadog targets ran — check that the Datadog.Maui package is referenced."
    fi

    if [ -n "$messages" ]; then
        log_info "Datadog target output:"
        while IFS= read -r m; do echo "    $m"; done <<< "$messages"
    fi

    log_info "Full detailed log: $DETAILED_LOG"
}

if [ "$TARGET" = "ios" ]; then
    echo ""
    log_info "IPA: bin/Release/net10.0-ios/ios-arm64/publish/example.ipa"
    log_info "App dSYM: bin/Release/net10.0-ios/ios-arm64/example.app.dSYM"
    report_ppdb "bin/Release/net10.0-ios/ios-arm64" \
                "obj/Release/net10.0-ios/ios-arm64" \
                "bin/Release/net10.0-ios/ios-arm64/example.app/dd_debug_ids.json"
elif [ "$TARGET" = "android" ]; then
    echo ""
    log_info "APK: bin/Release/net10.0-android/publish/"
    if [ -f "bin/Release/net10.0-android/mapping.txt" ]; then
        log_info "Mapping: bin/Release/net10.0-android/mapping.txt"
    fi
    report_ppdb "bin/Release/net10.0-android" \
                "obj/Release/net10.0-android" \
                ""
fi

report_phases

if [ "$UPLOAD" = true ]; then
    if [ $PUBLISH_EXIT -eq 0 ]; then
        log_info "App symbols uploaded to ${SITE:-datadoghq.com (default)} (see build output above)"
    else
        echo ""
        log_error "Symbol upload FAILED (exit $PUBLISH_EXIT) — see the Datadog Symbol Upload Summary above."
        echo "Crashes from this build will not symbolicate."
        echo "Check that DATADOG_API_KEY is valid for site ${SITE:-datadoghq.com (default)}."
        echo ""
        exit $PUBLISH_EXIT
    fi
else
    echo ""
    echo -e "${YELLOW}Symbol upload skipped (--no-upload).${NC}"
    echo "To upload manually, re-run without --no-upload."
fi
echo ""

# A publish failure unrelated to symbol upload (e.g. compile error) must not exit 0.
exit $PUBLISH_EXIT
