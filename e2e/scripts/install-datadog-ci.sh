#!/bin/bash
set -e -o pipefail

# Download the datadog-ci standalone binary to $E2E_DIR/datadog-ci.
# Skips download if the binary already exists.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
E2E_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

DATADOG_CI_VERSION="5.9.0"
DATADOG_CI_PATH="$E2E_DIR/datadog-ci"

if [ -x "$DATADOG_CI_PATH" ]; then
    return 0 2>/dev/null || exit 0
fi

echo "Installing datadog-ci v${DATADOG_CI_VERSION}..."
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
    BINARY="datadog-ci_darwin-arm64"
else
    BINARY="datadog-ci_darwin-x64"
fi

curl -L --fail "https://github.com/DataDog/datadog-ci/releases/download/v${DATADOG_CI_VERSION}/${BINARY}" --output "$DATADOG_CI_PATH"
chmod +x "$DATADOG_CI_PATH"
