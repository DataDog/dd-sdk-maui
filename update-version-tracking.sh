#!/bin/bash
# update-version-tracking.sh
#
# Reads versions.properties and ensures NATIVE_SDK_VERSIONS.md has an entry
# for the current SDK version with its corresponding native SDK versions.
#
# Called automatically by bump-version.sh, but can also be run standalone
# to fix a missing or incorrect entry.
#
# Usage:
#   ./update-version-tracking.sh

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
VERSIONS_FILE="$SCRIPT_DIR/versions.properties"
TRACKING_FILE="$SCRIPT_DIR/NATIVE_SDK_VERSIONS.md"

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

if [ ! -f "$VERSIONS_FILE" ]; then
    echo -e "${RED}✗${NC} versions.properties not found"
    exit 1
fi

source "$VERSIONS_FILE"

NEW_ROW="| ${SDK_VERSION} | ${IOS_NATIVE_VERSION} | ${ANDROID_NATIVE_VERSION} |"

# Create the file with header if it doesn't exist
if [ ! -f "$TRACKING_FILE" ]; then
    cat > "$TRACKING_FILE" << 'EOF'
| MAUI SDK | iOS SDK | Android SDK |
|----------|---------|-------------|
EOF
fi

# Check if this SDK version already has an entry
if grep -q "^| ${SDK_VERSION} " "$TRACKING_FILE"; then
    # Replace existing entry (in case native versions changed)
    perl -pi -e "s/^\| \Q${SDK_VERSION}\E .*/$(echo "$NEW_ROW" | sed 's/[\/&]/\\&/g')/" "$TRACKING_FILE"
    echo -e "${GREEN}✓${NC} NATIVE_SDK_VERSIONS.md updated (replaced ${SDK_VERSION})"
else
    # Insert new row after the header separator line (line 2)
    sed -i '' "2 a\\
${NEW_ROW}
" "$TRACKING_FILE"
    echo -e "${GREEN}✓${NC} NATIVE_SDK_VERSIONS.md updated (added ${SDK_VERSION})"
fi
