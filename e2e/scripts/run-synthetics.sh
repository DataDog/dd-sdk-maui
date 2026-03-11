#!/bin/bash
set -eo pipefail

# Run Synthetics tests for a given platform.
#
# Usage: ./run-synthetics.sh <PLATFORM>
#   PLATFORM: android

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
E2E_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

source "$SCRIPT_DIR/secrets/get-secret.sh"
source "$SCRIPT_DIR/install-datadog-ci.sh"

PLATFORM=$1

if [ -z "$PLATFORM" ]; then
    echo "Usage: ./run-synthetics.sh <android>"
    exit 1
fi

API_KEY="$(get_secret "$DD_MAUI_E2E_API_KEY")"
APP_KEY="$(get_secret "$DD_MAUI_E2E_APP_KEY")"

echo "Running Synthetics tests for $PLATFORM..."

"$E2E_DIR/datadog-ci" synthetics run-tests \
    --apiKey "$API_KEY" \
    --appKey "$APP_KEY" \
    --config "$E2E_DIR/synthetics/${PLATFORM}.config.json"

echo "Synthetics tests complete."
