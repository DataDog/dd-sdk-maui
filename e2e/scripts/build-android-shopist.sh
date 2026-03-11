#!/bin/bash
set -eo pipefail

# Build the Shopist MAUI app for Android with the locally-built SDK.
#
# Prerequisites:
#   - SDK NuGet packages already built in ./local-packages/
#   - .NET MAUI workload installed
#   - Android SDK installed
#
# Output: e2e/output/com.datadog.shopist.maui-Signed.apk

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
E2E_DIR="$ROOT_DIR/e2e"
SHOPIST_DIR="$E2E_DIR/shopist-maui"
OUTPUT_DIR="$E2E_DIR/output"

source "$SCRIPT_DIR/secrets/get-secret.sh"

echo "=== Building Shopist MAUI Android APK ==="

# Fetch client token from Vault
DD_CLIENT_TOKEN="$(get_secret "$DD_MAUI_E2E_CLIENT_TOKEN")"

# Clean previous build
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"
mkdir -p "$E2E_DIR/logs"

# Setup SSH for GitHub access (fetch deploy key from Vault)
SSH_KEY_PATH="$HOME/.ssh/id_ed25519"
if [ "$CI" = "true" ] && [ ! -f "$SSH_KEY_PATH" ]; then
    echo "Configuring SSH for GitHub access..."
    mkdir -p ~/.ssh
    get_secret "$DD_MAUI_E2E_SSH_KEY" > "$SSH_KEY_PATH"
    chmod 600 "$SSH_KEY_PATH"
    ssh-keyscan -t ed25519,rsa github.com >> ~/.ssh/known_hosts 2>/dev/null
    cat <<SSHEOF > ~/.ssh/config
Host github.com
    HostName github.com
    User git
    IdentityFile $SSH_KEY_PATH
    StrictHostKeyChecking yes
SSHEOF
fi

# Clone or update shopist-maui
if [ -d "$SHOPIST_DIR" ]; then
    echo "Updating shopist-maui..."
    cd "$SHOPIST_DIR"
    git fetch origin
    git reset --hard origin/main
else
    echo "Cloning shopist-maui..."
    git clone git@github.com:DataDog/shopist-maui.git "$SHOPIST_DIR"
    cd "$SHOPIST_DIR"
fi

# Add NuGet.Config pointing to the SDK's local packages
cat > "$SHOPIST_DIR/NuGet.Config" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local-packages" value="../../local-packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

# Add DatadogSdk.Maui package reference to Shopist.csproj if not already present
if ! grep -q "DatadogSdk.Maui" "$SHOPIST_DIR/Shopist.csproj"; then
    echo "Adding DatadogSdk.Maui package reference..."
    sed -i '' '/<\/Project>/i\
  <!-- Datadog SDK for .NET MAUI -->\
  <ItemGroup>\
    <PackageReference Include="DatadogSdk.Maui" Version="*" />\
  </ItemGroup>\
' "$SHOPIST_DIR/Shopist.csproj"
fi

# Bump Android minSdkVersion to 23 (required by Datadog SDK)
sed -i '' "s/SupportedOSPlatformVersion.*android.*21.0/SupportedOSPlatformVersion Condition=\"\$([MSBuild]::GetTargetPlatformIdentifier('\$(TargetFramework)')) == 'android'\">23.0/g" "$SHOPIST_DIR/Shopist.csproj"

# Suppress Java dependency warnings (same as example app)
if ! grep -q "XA4241" "$SHOPIST_DIR/Shopist.csproj"; then
    echo "Adding Android warning suppression..."
    sed -i '' '/<\/Project>/i\
  <PropertyGroup Condition="$(TargetFramework.Contains('"'"'-android'"'"'))">\
    <NoWarn>$(NoWarn);XA4241;XA4242</NoWarn>\
  </PropertyGroup>\
' "$SHOPIST_DIR/Shopist.csproj"
fi

# Initialize Datadog SDK in MauiProgram.cs if not already done
if ! grep -q "DdSdk" "$SHOPIST_DIR/MauiProgram.cs"; then
    echo "Adding Datadog SDK initialization to MauiProgram.cs..."
    # Add using statement
    sed -i '' '1i\
using DatadogSdk.Maui;\
' "$SHOPIST_DIR/MauiProgram.cs"

    # Add SDK initialization before builder.Build()
    sed -i '' '/return builder\.Build/i\
\        // Initialize Datadog SDK\
\        DdSdk.Initialize(new DdSdkConfiguration\
\        {\
\            ClientToken = "'"$DD_CLIENT_TOKEN"'",\
\            Environment = "e2e",\
\            Service = "shopist-maui"\
\        });\
\        DdLogs.Enable();\
\        DdLogs.Info("shopist_maui_app_launched");\
' "$SHOPIST_DIR/MauiProgram.cs"
fi

echo "Building Android APK (Release)..."
cd "$SHOPIST_DIR"

# Clear NuGet cache for DatadogSdk packages to ensure local versions are used
rm -rf ~/.nuget/packages/datadogsdk.*

dotnet publish Shopist.csproj \
    -f net10.0-android \
    -c Release \
    -o "$OUTPUT_DIR" \
    2>&1 | tee "$E2E_DIR/logs/build_android.log"

echo "=== Android APK build complete ==="
echo "Output: $OUTPUT_DIR/"
ls -la "$OUTPUT_DIR/"*.apk 2>/dev/null || echo "WARNING: No APK found in output directory"
