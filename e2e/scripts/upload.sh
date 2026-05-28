#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
set -e -o pipefail

# Upload app binary to Datadog Synthetics.
#
# Usage: ./upload.sh <FILE_PATH> <PLATFORM>
#   FILE_PATH: Path to app binary (.apk or .ipa)
#   PLATFORM:  ANDROID or IOS

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
E2E_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

source "$SCRIPT_DIR/secrets/get-secret.sh"
source "$SCRIPT_DIR/install-datadog-ci.sh"

FILE_PATH=$1
PLATFORM=$2

if [ -z "$FILE_PATH" ] || [ -z "$PLATFORM" ]; then
    echo "Usage: ./upload.sh <FILE_PATH> <PLATFORM>"
    echo "  PLATFORM: ANDROID or IOS"
    exit 1
fi

if [ ! -f "$FILE_PATH" ]; then
    echo "Error: File not found: $FILE_PATH"
    exit 1
fi

API_KEY="$(get_secret "$DD_MAUI_E2E_API_KEY")"
APP_KEY="$(get_secret "$DD_MAUI_E2E_APP_KEY")"

if [ "$PLATFORM" = "ANDROID" ]; then
    SYNTHETICS_APP_ID="$(get_secret "$DD_MAUI_E2E_SYNTHETICS_ANDROID_APP_ID")"
else
    echo "Error: Unknown platform '$PLATFORM'. Use ANDROID."
    exit 1
fi

TIMESTAMP=$(date +"%Y/%m/%d-%H:%M:%S")

echo "Uploading $PLATFORM binary to Synthetics..."
echo "  File: $FILE_PATH"
echo "  App ID: $SYNTHETICS_APP_ID"

"$E2E_DIR/datadog-ci" synthetics upload-application \
    --apiKey "$API_KEY" \
    --appKey "$APP_KEY" \
    --mobileApplicationId "$SYNTHETICS_APP_ID" \
    --mobileApplicationVersionFilePath "$FILE_PATH" \
    --versionName "shopist-maui_${PLATFORM}_${TIMESTAMP}" \
    --latest

echo "Upload complete."
