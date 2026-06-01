#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
set -e

echo "🔨 Building DatadogWrapper XCFramework..."

# Resolve script directory as absolute path
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Navigate to the package directory
cd "$SCRIPT_DIR/DatadogWrapper"

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf ./build

# Create build directory
mkdir -p ./build

# Build for iOS device (arm64)
echo "📱 Building for iOS device (arm64)..."
xcodebuild archive \
  -scheme DatadogWrapper \
  -destination "generic/platform=iOS" \
  -archivePath "./build/ios-arm64.xcarchive" \
  SKIP_INSTALL=NO \
  BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
  -quiet

# Build for iOS Simulator (arm64 + x86_64)
echo "🖥️  Building for iOS Simulator (arm64 + x86_64)..."
xcodebuild archive \
  -scheme DatadogWrapper \
  -destination "generic/platform=iOS Simulator" \
  -archivePath "./build/ios-simulator.xcarchive" \
  SKIP_INSTALL=NO \
  BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
  -quiet

# Create XCFramework
echo "📦 Creating XCFramework..."
xcodebuild -create-xcframework \
  -framework ./build/ios-arm64.xcarchive/Products/usr/local/lib/DatadogWrapper.framework \
  -framework ./build/ios-simulator.xcarchive/Products/usr/local/lib/DatadogWrapper.framework \
  -output ./build/DatadogWrapper.xcframework

# Verify the XCFramework was created
if [ -f "./build/DatadogWrapper.xcframework/Info.plist" ]; then
  echo "✅ XCFramework created successfully!"
  echo "📄 Location: $(pwd)/build/DatadogWrapper.xcframework"

  # List the architecture slices
  echo ""
  echo "📊 Architecture slices:"
  ls -1 ./build/DatadogWrapper.xcframework/ | grep -v Info.plist

  # Preserve dSYMs for symbol upload, then clean intermediate archives
  echo ""
  echo "📦 Preserving dSYMs..."
  if [ -d "./build/ios-arm64.xcarchive/dSYMs" ]; then
    cp -R ./build/ios-arm64.xcarchive/dSYMs ./build/dSYMs
    echo "📋 dSYMs saved to $(pwd)/build/dSYMs"
  fi

  echo "🧹 Cleaning up intermediate archives..."
  rm -rf ./build/ios-arm64.xcarchive
  rm -rf ./build/ios-simulator.xcarchive

  # Copy XCFramework to bindings directory
  echo ""
  echo "📋 Copying XCFramework to bindings..."
  BINDINGS_DIR="$SCRIPT_DIR/../../bindings/Datadog.iOS.Binding/NativeReference"
  rm -rf "$BINDINGS_DIR/DatadogWrapper.xcframework"
  cp -R ./build/DatadogWrapper.xcframework "$BINDINGS_DIR/"
  echo "📋 Copied to $BINDINGS_DIR/DatadogWrapper.xcframework"

  # Copy dSYMs to bindings so they can be included in symbol uploads
  if [ -d "./build/dSYMs" ]; then
    rm -rf "$BINDINGS_DIR/dSYMs"
    cp -R ./build/dSYMs "$BINDINGS_DIR/"
    echo "📋 dSYMs copied to $BINDINGS_DIR/dSYMs"
  fi

  echo "✨ Build complete!"
else
  echo "❌ XCFramework creation failed!"
  exit 1
fi
