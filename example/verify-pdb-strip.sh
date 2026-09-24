#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
#
# verify-pdb-strip.sh
#
# Regression guard for DatadogStripPdbFromBundle / DatadogKeepPortablePdbs
# (Datadog.Maui.targets). Publishes the example iOS app twice:
#
#   1. Debug config (_BundlerDebug defaults to true whenever Configuration
#      == 'Debug', same effect on PDB bundling as an explicit soft-debug
#      build) with DatadogUploadSymbols=true and DatadogKeepPortablePdbs
#      left at its default (false) -> asserts NO .pdb ends up inside
#      example.app. This is the case that actually exercises the strip
#      logic (the <ResolvedFileToPublish Remove=.../> in
#      DatadogStripPdbFromBundle).
#   2. Same, but with DatadogKeepPortablePdbs=true -> asserts a .pdb DOES
#      end up inside example.app. This only proves the target's Condition
#      correctly disables the Remove when opted out; the PDB itself is
#      added by the standard iOS SDK targets regardless of Datadog's
#      target, not by anything of ours.
#
# Publishing an iOS app always requires a device RID (the simulator RID is
# rejected by Xamarin.Shared.Sdk.Publish.targets), which in turn requires
# code signing. This uses ad-hoc signing (CodesignKey=-, no provisioning
# profile) so it needs no real certificate/identity, matching how this
# script is meant to run unattended in CI.
#
# Does not upload any symbols (no datadog-ci / API key required in CI);
# DatadogCheckCli just skips the upload steps and logs why.
#
# Usage:
#   ./verify-pdb-strip.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

log_section() { echo ""; echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; echo -e "${BLUE}▶ $1${NC}"; echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; }
log_pass() { echo -e "  ${GREEN}✓${NC} $1"; }
log_fail() { echo -e "  ${RED}✗${NC} $1"; exit 1; }

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

IOS_TFM="net10.0-ios"
IOS_RID="ios-arm64"
APP_BUNDLE="bin/Debug/${IOS_TFM}/${IOS_RID}/example.app"

log_section "Restoring (${IOS_TFM}/${IOS_RID})"
dotnet restore -p:TargetFramework="$IOS_TFM" -r "$IOS_RID"

publish() {
    local keep_flag="$1"

    rm -rf "bin/Debug/${IOS_TFM}"

    dotnet publish -c Debug -f "$IOS_TFM" -r "$IOS_RID" --no-restore \
        -p:DatadogUploadSymbols=true \
        -p:DatadogKeepPortablePdbs="$keep_flag" \
        -p:CodesignKey=- \
        -p:CodesignProvision= \
        -p:BuildIpa=false \
        -v n -tl:off

    if [ ! -d "$APP_BUNDLE" ]; then
        log_fail "Expected app bundle not found at $APP_BUNDLE"
    fi
}

count_pdbs_in_bundle() {
    find "$APP_BUNDLE" -name "*.pdb" | wc -l | tr -d ' '
}

log_section "Publishing with DatadogKeepPortablePdbs=false (default)"
publish "false"
PDB_COUNT_STRIPPED=$(count_pdbs_in_bundle)
if [ "$PDB_COUNT_STRIPPED" = "0" ]; then
    log_pass "No .pdb found in $APP_BUNDLE"
else
    log_fail "Expected 0 .pdb files in bundle, found $PDB_COUNT_STRIPPED"
fi

log_section "Publishing with DatadogKeepPortablePdbs=true"
publish "true"
PDB_COUNT_KEPT=$(count_pdbs_in_bundle)
if [ "$PDB_COUNT_KEPT" -gt "0" ]; then
    log_pass "Found $PDB_COUNT_KEPT .pdb file(s) in $APP_BUNDLE, as expected with opt-out set"
else
    log_fail "Expected at least one .pdb file in bundle with DatadogKeepPortablePdbs=true, found none"
fi

log_section "PDB strip verification passed"
