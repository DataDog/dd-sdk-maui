#!/bin/bash
set -e

echo "🔨 Building DatadogWrapper XCFramework..."

# Navigate to the package directory
cd "$(dirname "$0")/DatadogWrapper"

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

  # Clean up intermediate archives (optional, but saves space)
  echo ""
  echo "🧹 Cleaning up intermediate archives..."
  rm -rf ./build/ios-arm64.xcarchive
  rm -rf ./build/ios-simulator.xcarchive

  echo "✨ Build complete!"
else
  echo "❌ XCFramework creation failed!"
  exit 1
fi
