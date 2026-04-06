#!/bin/bash
set -e

# Color codes for terminal output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Prevent running as root/sudo - many tools (Homebrew, sdkmanager) refuse to run as root
if [ "$EUID" -eq 0 ]; then
    echo ""
    echo -e "${RED}✗ Error: Do not run this script with sudo${NC}"
    echo ""
    echo "This script should be run as your normal user:"
    echo "  ./setup.sh"
    echo ""
    echo "The script will ask for your password (via sudo) only when"
    echo "specific steps require elevated privileges."
    echo ""
    exit 2
fi

# State variables (will be set by detection functions)
DOTNET_INSTALLED=false
MAUI_INSTALLED=false
XCODE_INSTALLED=false
CLI_TOOLS_INSTALLED=false
IOS_SIMULATOR_AVAILABLE=false
JAVA_INSTALLED=false
ANDROID_SDK_INSTALLED=false
ANDROID_AVD_AVAILABLE=false
PYTHON_INSTALLED=false

# State variables for verification and flags
VERIFICATION_PASSED=""  # "" = not run, true = passed, false = failed
IOS_BUILD_PASSED=false
ANDROID_BUILD_PASSED=false
SHOW_HELP=false
VERIFY_ONLY=false
RUN_VERIFICATION=false

# Utility functions
print_header() {
    local text="$1"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo " $text"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
}

print_status() {
    local label="$1"
    local value="$2"
    local color="$3"

    local symbol
    local color_code

    case "$color" in
        "green")
            symbol="✓"
            color_code="$GREEN"
            ;;
        "red")
            symbol="✗"
            color_code="$RED"
            ;;
        "yellow")
            symbol="⚠"
            color_code="$YELLOW"
            ;;
    esac

    # Format: symbol label (padded to 30 chars) value
    printf "${color_code}%-2s %-28s %s${NC}\n" "$symbol" "$label" "$value"
}

print_summary() {
    echo ""
    print_header "Summary"

    # Count missing required dependencies
    local missing=()

    [ "$DOTNET_INSTALLED" = false ] && missing+=("• .NET SDK 10")
    [ "$MAUI_INSTALLED" = false ] && missing+=("• MAUI workload")
    [ "$XCODE_INSTALLED" = false ] && missing+=("• Xcode 15.0+")
    [ "$CLI_TOOLS_INSTALLED" = false ] && missing+=("• Xcode Command Line Tools")
    [ "$IOS_SIMULATOR_AVAILABLE" = false ] && missing+=("• iOS Simulator")
    [ "$JAVA_INSTALLED" = false ] && missing+=("• Java JDK 17+")
    [ "$ANDROID_SDK_INSTALLED" = false ] && missing+=("• Android SDK")
    [ "$ANDROID_AVD_AVAILABLE" = false ] && missing+=("• Android Virtual Device (AVD)")
    [ "$PYTHON_INSTALLED" = false ] && missing+=("• Python 3")

    if [ ${#missing[@]} -eq 0 ]; then
        echo "Status: ✓ All required dependencies installed"
        echo -e "\n${BLUE}See CONTRIBUTING.md for next steps.${NC}\n"
        exit 0
    else
        echo "Status: ⚠ Missing required dependencies"
        echo ""
        echo "Missing:"
        for item in "${missing[@]}"; do
            echo "  $item"
        done
        echo ""
        echo "Run this script again to check progress, or see"
        echo "CONTRIBUTING.md for manual setup instructions."
        echo ""
        exit 1
    fi
}

prompt_user() {
    local message="$1"
    while true; do
        read -p "$(echo -e "${BLUE}${message} [y/n]:${NC} ")" yn
        case $yn in
            [Yy]* ) return 0;;
            [Nn]* ) return 1;;
            * ) echo "Please answer yes (y) or no (n).";;
        esac
    done
}

check_admin_privileges() {
    echo ""
    echo "This script requires administrator (sudo) privileges to install"
    echo "some components (.NET SDK, MAUI workload, Xcode license, etc.)."
    echo ""
    echo "If you are on a machine with restricted access, make sure you have"
    echo "requested admin privileges before"
    echo "continuing."
    echo ""

    if ! prompt_user "Do you have administrator (sudo) privileges?"; then
        echo ""
        echo "Please obtain admin privileges and re-run this script."
        exit 1
    fi
}

# Detection functions
check_macos() {
    local os_name=$(uname -s)

    if [ "$os_name" != "Darwin" ]; then
        echo ""
        echo "${RED}✗ Error: This script requires macOS${NC}"
        echo ""
        echo "The Datadog MAUI SDK requires macOS for iOS development."
        echo "You are currently running: $os_name"
        echo ""
        exit 2
    fi

    local macos_version=$(sw_vers -productVersion)
    print_status "macOS" "$macos_version" "green"
}

check_dotnet() {
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version 2>/dev/null)
        if [[ "${DOTNET_VERSION}" == 10.* ]]; then
            print_status ".NET SDK 10" "$DOTNET_VERSION" "green"
            DOTNET_INSTALLED=true
        else
            print_status ".NET SDK 10" "Found $DOTNET_VERSION, need 10.x" "red"
            DOTNET_INSTALLED=false
        fi
    else
        print_status ".NET SDK 10" "Not found" "red"
        DOTNET_INSTALLED=false
    fi
}

check_maui_workload() {
    if [ "$DOTNET_INSTALLED" = false ]; then
        print_status "MAUI workload" ".NET not available" "red"
        MAUI_INSTALLED=false
        return
    fi

    if dotnet workload list 2>/dev/null | grep -q "maui"; then
        print_status "MAUI workload" "Installed" "green"
        MAUI_INSTALLED=true
    else
        print_status "MAUI workload" "Not installed" "red"
        MAUI_INSTALLED=false
    fi
}

check_xcode() {
    if [ -d "/Applications/Xcode.app" ]; then
        XCODE_VERSION=$(xcodebuild -version 2>/dev/null | head -n1 | awk '{print $2}')
        XCODE_MAJOR=$(echo "$XCODE_VERSION" | cut -d. -f1)

        if [ "$XCODE_MAJOR" -ge 15 ]; then
            print_status "Xcode" "$XCODE_VERSION" "green"
            XCODE_INSTALLED=true
        else
            print_status "Xcode" "$XCODE_VERSION (need 15.0+)" "yellow"
            XCODE_INSTALLED=false
        fi

        # Check Command Line Tools
        if xcode-select -p &> /dev/null; then
            CLI_TOOLS_PATH=$(xcode-select -p)
            print_status "Command Line Tools" "Installed" "green"
            CLI_TOOLS_INSTALLED=true
        else
            print_status "Command Line Tools" "Not installed" "red"
            CLI_TOOLS_INSTALLED=false
        fi
    else
        print_status "Xcode" "Not installed" "red"
        print_status "Command Line Tools" "Not available" "red"
        XCODE_INSTALLED=false
        CLI_TOOLS_INSTALLED=false
    fi
}

check_ios_simulator() {
    if command -v xcrun &> /dev/null; then
        SIMULATOR_COUNT=$(xcrun simctl list devices available 2>/dev/null | grep -c "iPhone" || echo "0")
        if [ "$SIMULATOR_COUNT" -gt 0 ]; then
            print_status "iOS Simulator" "$SIMULATOR_COUNT available" "green"
            IOS_SIMULATOR_AVAILABLE=true
        else
            print_status "iOS Simulator" "No simulators configured" "red"
            IOS_SIMULATOR_AVAILABLE=false
        fi
    else
        print_status "iOS Simulator" "xcrun not available" "red"
        IOS_SIMULATOR_AVAILABLE=false
    fi
}

check_java() {
    if command -v java &> /dev/null; then
        JAVA_VERSION=$(java -version 2>&1 | head -n1 | awk -F '"' '{print $2}')
        JAVA_MAJOR=$(echo "$JAVA_VERSION" | cut -d. -f1)

        if [ "$JAVA_MAJOR" -ge 17 ]; then
            print_status "Java JDK" "$JAVA_VERSION" "green"
            JAVA_INSTALLED=true
        else
            print_status "Java JDK" "$JAVA_VERSION (need 17+)" "yellow"
            JAVA_INSTALLED=false
        fi
    else
        print_status "Java JDK 17+" "Not found" "red"
        JAVA_INSTALLED=false
    fi
}

check_android_sdk() {
    if [ -n "$ANDROID_HOME" ] && [ -d "$ANDROID_HOME" ]; then
        if [ -f "$ANDROID_HOME/platform-tools/adb" ]; then
            print_status "Android SDK" "$ANDROID_HOME" "green"
            ANDROID_SDK_INSTALLED=true
        else
            print_status "Android SDK" "Incomplete (missing tools)" "yellow"
            ANDROID_SDK_INSTALLED=false
        fi
    else
        print_status "Android SDK" "ANDROID_HOME not set" "red"
        ANDROID_SDK_INSTALLED=false
    fi
}

check_android_emulator() {
    if [ "$ANDROID_SDK_INSTALLED" = true ]; then
        AVD_COUNT=$("$ANDROID_HOME/emulator/emulator" -list-avds 2>/dev/null | wc -l | tr -d ' ')
        if [ "$AVD_COUNT" -gt 0 ]; then
            print_status "Android AVD" "$AVD_COUNT configured" "green"
            ANDROID_AVD_AVAILABLE=true
        else
            print_status "Android AVD" "No emulators configured" "red"
            ANDROID_AVD_AVAILABLE=false
        fi
    else
        print_status "Android AVD" "SDK not available" "red"
        ANDROID_AVD_AVAILABLE=false
    fi
}

check_python() {
    if command -v python3 &> /dev/null; then
        PYTHON_VERSION=$(python3 --version 2>/dev/null | awk '{print $2}')
        print_status "Python 3" "$PYTHON_VERSION" "green"
        PYTHON_INSTALLED=true
    else
        print_status "Python 3" "Not found" "red"
        PYTHON_INSTALLED=false
    fi
}

# Installation functions
install_dotnet_sdk() {
    # Idempotency: check if already installed
    if [ "$DOTNET_INSTALLED" = true ]; then
        echo ".NET SDK 10 is already installed, skipping."
        return 0
    fi

    echo ""
    echo "Installing .NET 10 SDK..."

    # Use the official dotnet-install script which resolves the correct download URL automatically
    local install_script="/tmp/dotnet-install.sh"

    echo "Downloading .NET install script..."
    if ! curl -fSL --retry 3 "https://dot.net/v1/dotnet-install.sh" -o "$install_script"; then
        echo "✗ Failed to download install script"
        echo "Please download manually from: https://dotnet.microsoft.com/download/dotnet/10.0"
        return 1
    fi
    chmod +x "$install_script"

    echo "Installing .NET SDK 10 (this may take a few minutes)..."
    if sudo "$install_script" --channel 10.0 --install-dir /usr/local/share/dotnet; then
        rm -f "$install_script"
        echo "✓ .NET SDK 10 installed successfully"

        # Update PATH for current session
        export PATH="/usr/local/share/dotnet:$PATH"

        return 0
    else
        rm -f "$install_script"
        echo "✗ Installation failed"
        echo "Please download manually from: https://dotnet.microsoft.com/download/dotnet/10.0"
        return 1
    fi
}

install_maui_workload() {
    # Idempotency: check if already installed
    if [ "$MAUI_INSTALLED" = true ]; then
        echo "MAUI workload is already installed, skipping."
        return 0
    fi

    # Verify .NET SDK is available
    if ! command -v dotnet &> /dev/null; then
        echo "✗ .NET SDK not found. Install .NET SDK first."
        return 1
    fi

    echo ""
    echo "Installing MAUI workload..."
    echo "(This may take several minutes to download and install)"

    # MAUI workload install 
    if sudo dotnet workload install maui; then
        echo "✓ MAUI workload installed successfully"
        return 0
    else
        echo "✗ Failed to install MAUI workload"
        echo "Try running manually: sudo dotnet workload install maui"
        return 1
    fi
}

install_dotnet_components() {
    local success=true

    # Install .NET SDK if needed
    if [ "$DOTNET_INSTALLED" = false ]; then
        set +e  # Disable exit on error for installation
        install_dotnet_sdk
        if [ $? -ne 0 ]; then
            success=false
        else
            # Re-detect after installation
            check_dotnet
        fi
        set -e  # Re-enable exit on error
    fi

    # Install MAUI workload if needed (and .NET is now available)
    if [ "$DOTNET_INSTALLED" = true ] && [ "$MAUI_INSTALLED" = false ]; then
        set +e  # Disable exit on error for installation
        install_maui_workload
        if [ $? -ne 0 ]; then
            success=false
        else
            # Re-detect after installation
            check_maui_workload
        fi
        set -e  # Re-enable exit on error
    fi

    # Print final status
    echo ""
    print_header "Installation Complete"

    if [ "$success" = true ] && [ "$DOTNET_INSTALLED" = true ] && [ "$MAUI_INSTALLED" = true ]; then
        echo "✓ .NET environment configured successfully"
        return 0
    else
        echo "⚠ Some installations did not complete successfully."
        echo "Run ./setup.sh again to check status or install manually."
        return 1
    fi
}

# iOS installation functions
guide_xcode_installation() {
    echo ""
    print_header "Xcode Installation Required"

    echo "Xcode is not installed or is outdated (need 15.0+)"
    echo ""
    echo "Please install Xcode from one of these sources:"
    echo "  1. Mac App Store: https://apps.apple.com/app/xcode/id497799835"
    echo "  2. Apple Developer Downloads: https://developer.apple.com/download/"
    echo ""
    echo "After installing Xcode, run this script again."
    echo ""
}

install_xcode_cli_tools() {
    # Idempotency: check if already installed
    if [ "$CLI_TOOLS_INSTALLED" = true ]; then
        echo "Command Line Tools are already installed, skipping."
        return 0
    fi

    echo ""
    echo "Installing Xcode Command Line Tools..."
    echo "A system dialog will appear - click Install and follow the prompts."
    echo ""

    # Trigger installation dialog
    xcode-select --install 2>/dev/null

    echo "Waiting for Command Line Tools installation to complete..."
    echo "(This may take several minutes)"

    # Wait for installation (poll every 5 seconds)
    local max_wait=600  # 10 minutes max
    local elapsed=0
    while ! xcode-select -p &> /dev/null; do
        if [ $elapsed -ge $max_wait ]; then
            echo "✗ Installation timeout after 10 minutes"
            echo "Please complete installation manually and run setup.sh again"
            return 1
        fi
        sleep 5
        elapsed=$((elapsed + 5))
    done

    echo "✓ Command Line Tools installed successfully"
    return 0
}

accept_xcode_license() {
    # Check if license already accepted
    if sudo xcodebuild -license check 2>/dev/null; then
        echo "Xcode license is already accepted, skipping."
        return 0
    fi

    if prompt_user "Xcode license needs to be accepted. Accept now?"; then
        echo "Accepting Xcode license (requires sudo)..."

        if sudo xcodebuild -license accept; then
            echo "✓ Xcode license accepted"
            return 0
        else
            echo "✗ Failed to accept Xcode license"
            echo "Try manually: sudo xcodebuild -license accept"
            return 1
        fi
    else
        echo ""
        echo "Skipping license acceptance."
        echo "You can accept later by running: sudo xcodebuild -license accept"
        return 1
    fi
}

list_ios_simulators() {
    echo ""
    echo "Available iOS Simulators:"
    xcrun simctl list devices available 2>/dev/null | grep "iPhone" | sed 's/^/  • /' | head -5
    local total=$(xcrun simctl list devices available 2>/dev/null | grep -c "iPhone")
    if [ "$total" -gt 5 ]; then
        echo "  ... and $((total - 5)) more"
    fi
    echo ""
}

setup_ios_simulator() {
    # Idempotency: check if simulators already exist
    local simulator_count=$(xcrun simctl list devices available 2>/dev/null | grep -c "iPhone" || echo "0")
    if [ "$simulator_count" -gt 0 ]; then
        echo "iOS Simulators already exist ($simulator_count found), skipping."
        return 0
    fi

    echo ""
    echo "Setting up iOS Simulator..."

    # Check for available iOS runtimes
    local runtime=$(xcrun simctl list runtimes 2>/dev/null | grep "iOS" | grep -v "unavailable" | head -n1 | awk '{print $NF}')

    if [ -z "$runtime" ]; then
        echo "✗ No iOS runtimes available"
        echo "Please open Xcode and download an iOS simulator runtime:"
        echo "  Xcode → Settings → Platforms → iOS"
        return 1
    fi

    # Extract runtime identifier
    local runtime_id=$(echo "$runtime" | tr -d '()')

    # Get iPhone device type (try iPhone 15 first, fallback to any iPhone)
    local device_type=$(xcrun simctl list devicetypes 2>/dev/null | grep "iPhone-15" | head -n1 | awk '{print $NF}' | tr -d '()')
    if [ -z "$device_type" ]; then
        device_type=$(xcrun simctl list devicetypes 2>/dev/null | grep "iPhone" | head -n1 | awk '{print $NF}' | tr -d '()')
    fi

    if [ -z "$device_type" ]; then
        echo "✗ No iPhone device types available"
        return 1
    fi

    # Create simulator
    echo "Creating iPhone simulator with runtime: $runtime_id"
    if xcrun simctl create "iPhone" "$device_type" "$runtime_id" &> /dev/null; then
        echo "✓ iOS Simulator created successfully"
        list_ios_simulators
        return 0
    else
        echo "✗ Failed to create iOS Simulator"
        echo "Try manually: xcrun simctl create \"iPhone\" \"$device_type\" \"$runtime_id\""
        return 1
    fi
}

install_ios_components() {
    local success=true

    # Check Xcode first (cannot auto-install)
    if [ "$XCODE_INSTALLED" = false ]; then
        guide_xcode_installation
        exit 1
    fi

    # Install Command Line Tools if needed
    if [ "$CLI_TOOLS_INSTALLED" = false ]; then
        set +e
        install_xcode_cli_tools
        if [ $? -ne 0 ]; then
            success=false
        else
            check_xcode  # Re-detect after installation
        fi
        set -e
    fi

    # Accept Xcode license if needed
    if sudo xcodebuild -license check 2>/dev/null; then
        echo "Xcode license already accepted, continuing..."
    else
        set +e
        accept_xcode_license
        if [ $? -ne 0 ]; then
            success=false
        fi
        set -e
    fi

    # Setup iOS Simulator if needed
    if [ "$IOS_SIMULATOR_AVAILABLE" = false ]; then
        set +e
        setup_ios_simulator
        if [ $? -ne 0 ]; then
            success=false
        else
            check_ios_simulator  # Re-detect after setup
        fi
        set -e
    fi

    # Print final status
    echo ""
    print_header "iOS Setup Complete"

    if [ "$success" = true ] && [ "$CLI_TOOLS_INSTALLED" = true ] && [ "$IOS_SIMULATOR_AVAILABLE" = true ]; then
        echo "✓ iOS development environment configured successfully"
        return 0
    else
        echo "⚠ Some iOS components did not configure successfully."
        echo "Run ./setup.sh again to check status or configure manually."
        return 1
    fi
}

# Android installation functions
install_java_jdk() {
    # Idempotency: check if already installed
    if [ "$JAVA_INSTALLED" = true ]; then
        echo "Java JDK 17+ is already installed, skipping."
        return 0
    fi

    echo ""
    echo "Installing Java JDK 17..."

    # Check if Homebrew is installed
    if ! command -v brew &> /dev/null; then
        echo "Homebrew not found. Installing Homebrew first..."
        echo "(This will require your password and may take several minutes)"

        if /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"; then
            echo "✓ Homebrew installed successfully"

            # Add Homebrew to PATH for current session
            if [ -f "/opt/homebrew/bin/brew" ]; then
                eval "$(/opt/homebrew/bin/brew shellenv)"
            elif [ -f "/usr/local/bin/brew" ]; then
                eval "$(/usr/local/bin/brew shellenv)"
            fi
        else
            echo "✗ Failed to install Homebrew"
            echo "Please install manually from: https://brew.sh"
            return 1
        fi
    fi

    # Install Java JDK 17
    echo "Installing Java JDK 17 via Homebrew..."
    if brew install openjdk@17; then
        # Create symlink for system Java wrappers
        echo "Creating system Java symlink..."
        sudo ln -sfn "$(brew --prefix)/opt/openjdk@17/libexec/openjdk.jdk" /Library/Java/JavaVirtualMachines/openjdk-17.jdk

        echo "✓ Java JDK 17 installed successfully"
        return 0
    else
        echo "✗ Failed to install Java JDK 17"
        echo "Try manually: brew install openjdk@17"
        return 1
    fi
}

configure_android_env() {
    local android_home="$1"

    # Detect shell profile based on user's login shell (not the script interpreter)
    local shell_rc
    if [[ "$SHELL" == */zsh ]]; then
        shell_rc="$HOME/.zshrc"
    elif [[ "$SHELL" == */bash ]]; then
        shell_rc="$HOME/.bash_profile"
    else
        shell_rc="$HOME/.profile"
    fi

    # Check if already configured (idempotency)
    if grep -q "ANDROID_HOME" "$shell_rc" 2>/dev/null; then
        echo "Android environment variables already configured in $shell_rc"
        return 0
    fi

    echo "Configuring Android environment variables in $shell_rc"

    # Add to shell profile
    cat >> "$shell_rc" << EOF

# Android SDK (added by setup.sh on $(date))
export ANDROID_HOME="$android_home"
export PATH="\$PATH:\$ANDROID_HOME/platform-tools"
export PATH="\$PATH:\$ANDROID_HOME/emulator"
export PATH="\$PATH:\$ANDROID_HOME/cmdline-tools/latest/bin"
EOF

    echo "✓ Environment variables added to $shell_rc"
    echo "Note: Restart your terminal or run: source $shell_rc"

    # Set for current session
    export ANDROID_HOME="$android_home"
    export PATH="$PATH:$ANDROID_HOME/platform-tools"
    export PATH="$PATH:$ANDROID_HOME/emulator"
    export PATH="$PATH:$ANDROID_HOME/cmdline-tools/latest/bin"

    return 0
}

install_android_sdk() {
    # Idempotency: check if already installed
    if [ "$ANDROID_SDK_INSTALLED" = true ]; then
        echo "Android SDK is already installed, skipping."
        return 0
    fi

    echo ""
    echo "Installing Android SDK..."
    echo "(This will download ~1-2 GB of data and may take 10-15 minutes)"

    # Set installation directory
    local android_home="$HOME/Library/Android/sdk"
    mkdir -p "$android_home"

    # Download command-line tools
    echo "Downloading Android command-line tools..."
    local sdk_tools_url="https://dl.google.com/android/repository/commandlinetools-mac-11076708_latest.zip"
    local temp_zip="/tmp/android-cmdline-tools.zip"

    if ! curl -fSL "$sdk_tools_url" -o "$temp_zip"; then
        echo "✗ Download failed"
        echo "Please download manually from: https://developer.android.com/studio#command-tools"
        return 1
    fi

    # Extract to correct location
    echo "Extracting command-line tools..."
    mkdir -p "$android_home/cmdline-tools"
    # Remove existing latest dir to avoid mv conflicts, then extract with overwrite
    rm -rf "$android_home/cmdline-tools/latest"
    unzip -qo "$temp_zip" -d "$android_home/cmdline-tools"
    mv "$android_home/cmdline-tools/cmdline-tools" "$android_home/cmdline-tools/latest"
    rm -f "$temp_zip"

    # Configure environment for installation
    export ANDROID_HOME="$android_home"
    export PATH="$PATH:$ANDROID_HOME/cmdline-tools/latest/bin"

    # Install essential SDK packages
    echo "Installing Android SDK packages..."
    echo "(This may take several minutes)"

    local sdkmanager="$android_home/cmdline-tools/latest/bin/sdkmanager"

    # Install packages without prompting
    yes | "$sdkmanager" --licenses > /dev/null 2>&1 || true

    if "$sdkmanager" --install \
        "platform-tools" \
        "platforms;android-34" \
        "build-tools;34.0.0" \
        "emulator" \
        "system-images;android-34;google_apis;arm64-v8a"; then

        echo "✓ Android SDK installed at $android_home"

        # Configure environment variables
        configure_android_env "$android_home"

        return 0
    else
        echo "✗ Failed to install SDK packages"
        return 1
    fi
}

accept_android_licenses() {
    # Check if SDK is installed
    if [ "$ANDROID_SDK_INSTALLED" = false ]; then
        echo "Android SDK not installed, skipping license acceptance."
        return 1
    fi

    # Check if licenses already accepted (idempotency)
    local sdkmanager="$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager"
    if "$sdkmanager" --licenses 2>&1 | grep -q "All SDK package licenses accepted"; then
        echo "Android SDK licenses already accepted, skipping."
        return 0
    fi

    echo ""
    echo "Accepting Android SDK licenses..."

    if yes | "$sdkmanager" --licenses > /dev/null 2>&1; then
        echo "✓ Android SDK licenses accepted"
        return 0
    else
        echo "✗ Failed to accept licenses"
        echo "Try manually: \$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --licenses"
        return 1
    fi
}

list_android_avds() {
    echo ""
    echo "Available Android AVDs:"
    "$ANDROID_HOME/emulator/emulator" -list-avds 2>/dev/null | sed 's/^/  • /'
    echo ""
}

create_android_avd() {
    # Idempotency: check if AVDs already exist
    local avd_count=$("$ANDROID_HOME/emulator/emulator" -list-avds 2>/dev/null | wc -l | tr -d ' ')
    if [ "$avd_count" -gt 0 ]; then
        echo "Android AVDs already exist ($avd_count found), skipping."
        return 0
    fi

    echo ""
    echo "Creating Android Virtual Device..."

    local avd_name="Pixel_7_API_34"
    local system_image="system-images;android-34;google_apis;arm64-v8a"
    local sdkmanager="$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager"
    local avdmanager="$ANDROID_HOME/cmdline-tools/latest/bin/avdmanager"

    # Ensure system image is installed
    echo "Ensuring system image is installed..."
    if ! "$sdkmanager" "$system_image" 2>&1; then
        echo "✗ Failed to install system image: $system_image"
        echo "Try manually: $sdkmanager \"$system_image\""
        return 1
    fi

    # Create AVD
    echo "Creating AVD: $avd_name"
    if echo "no" | "$avdmanager" create avd \
        --name "$avd_name" \
        --package "$system_image" \
        --device "pixel_7" 2>&1; then

        echo "✓ Android AVD created: $avd_name"
        list_android_avds
        return 0
    else
        echo "✗ Failed to create AVD: $avd_name"
        echo ""
        echo "You can create one manually:"
        echo "  1. Via Android Studio: Tools → Device Manager → Create Device"
        echo "  2. Via command line:"
        echo "     $avdmanager create avd --name \"$avd_name\" --package \"$system_image\" --device \"pixel_7\""
        return 1
    fi
}

install_android_components() {
    local success=true

    # Install Java JDK if needed
    if [ "$JAVA_INSTALLED" = false ]; then
        set +e
        install_java_jdk
        if [ $? -ne 0 ]; then
            success=false
        else
            check_java  # Re-detect
        fi
        set -e
    fi

    # Install Android SDK if needed
    if [ "$ANDROID_SDK_INSTALLED" = false ]; then
        set +e
        install_android_sdk
        if [ $? -ne 0 ]; then
            success=false
        else
            check_android_sdk  # Re-detect
        fi
        set -e
    fi

    # Accept licenses if SDK is now installed
    if [ "$ANDROID_SDK_INSTALLED" = true ]; then
        set +e
        accept_android_licenses
        set -e
    fi

    # Create AVD if needed
    if [ "$ANDROID_SDK_INSTALLED" = true ] && [ "$ANDROID_AVD_AVAILABLE" = false ]; then
        set +e
        create_android_avd
        if [ $? -ne 0 ]; then
            success=false
        else
            check_android_emulator  # Re-detect
        fi
        set -e
    fi

    # Print final status
    echo ""
    print_header "Android Setup Complete"

    if [ "$success" = true ] && [ "$JAVA_INSTALLED" = true ] && [ "$ANDROID_SDK_INSTALLED" = true ] && [ "$ANDROID_AVD_AVAILABLE" = true ]; then
        echo "✓ Android development environment configured successfully"
        echo ""
        echo "⚠ Important: Restart your terminal for environment variables to take effect"
        return 0
    else
        echo "⚠ Some Android components did not configure successfully."
        echo "Run ./setup.sh again to check status or configure manually."
        return 1
    fi
}

# Verification functions
verify_build_scripts() {
    echo ""
    echo "Checking build scripts..."

    # Test root build script exists
    if [ -x "./build.sh" ]; then
        echo "✓ Root build script found"
    else
        echo "⚠ Root build.sh not found or not executable"
    fi

    # Test example build script exists
    if [ -x "./example/build.sh" ]; then
        echo "✓ Example build script found"
    else
        echo "⚠ Example build.sh not found or not executable"
    fi
}

run_verification_test() {
    echo ""
    print_header "Running Verification Tests"

    echo "This will create a temporary MAUI project and test build capabilities."
    echo "(This may take 2-3 minutes and download NuGet packages)"
    echo ""

    # Create temporary test project
    local temp_dir=$(mktemp -d)
    local original_dir=$(pwd)

    echo "Creating test MAUI project in $temp_dir..."

    cd "$temp_dir"

    # Create test project
    if ! dotnet new maui -n VerificationTest &> /dev/null; then
        echo "✗ Failed to create MAUI project"
        echo "  This indicates an issue with .NET or MAUI workload installation"
        cd "$original_dir"
        rm -rf "$temp_dir"
        VERIFICATION_PASSED=false
        return 1
    fi

    cd VerificationTest

    # Test iOS build
    echo "Testing iOS build..."
    if dotnet build -f net10.0-ios &> /dev/null; then
        echo "✓ iOS build successful"
        IOS_BUILD_PASSED=true
    else
        echo "✗ iOS build failed"
        echo "  Check Xcode and iOS SDK installation"
        IOS_BUILD_PASSED=false
    fi

    # Test Android build
    echo "Testing Android build..."
    if dotnet build -f net10.0-android &> /dev/null; then
        echo "✓ Android build successful"
        ANDROID_BUILD_PASSED=true
    else
        echo "✗ Android build failed"
        echo "  Check Android SDK and environment variables"
        ANDROID_BUILD_PASSED=false
    fi

    # Cleanup
    cd "$original_dir"
    rm -rf "$temp_dir"

    # Overall result
    echo ""
    if [ "$IOS_BUILD_PASSED" = true ] && [ "$ANDROID_BUILD_PASSED" = true ]; then
        echo "✓ All verification tests passed"
        VERIFICATION_PASSED=true
        return 0
    else
        echo "⚠ Some verification tests failed"
        echo "  Review the errors above and re-run ./setup.sh"
        VERIFICATION_PASSED=false
        return 1
    fi
}

# Final reporting functions
generate_setup_report() {
    echo ""
    print_header "Setup Complete - Summary Report"

    # Required dependencies
    echo "Required Dependencies:"
    [ "$DOTNET_INSTALLED" = true ] && echo "  ✓ .NET SDK 10" || echo "  ✗ .NET SDK 10"
    [ "$MAUI_INSTALLED" = true ] && echo "  ✓ MAUI workload" || echo "  ✗ MAUI workload"
    [ "$XCODE_INSTALLED" = true ] && echo "  ✓ Xcode" || echo "  ✗ Xcode"
    [ "$CLI_TOOLS_INSTALLED" = true ] && echo "  ✓ Command Line Tools" || echo "  ✗ Command Line Tools"
    [ "$IOS_SIMULATOR_AVAILABLE" = true ] && echo "  ✓ iOS Simulator" || echo "  ✗ iOS Simulator"
    [ "$JAVA_INSTALLED" = true ] && echo "  ✓ Java JDK 17+" || echo "  ✗ Java JDK 17+"
    [ "$ANDROID_SDK_INSTALLED" = true ] && echo "  ✓ Android SDK" || echo "  ✗ Android SDK"
    [ "$ANDROID_AVD_AVAILABLE" = true ] && echo "  ✓ Android AVD" || echo "  ✗ Android AVD"
    [ "$PYTHON_INSTALLED" = true ] && echo "  ✓ Python 3" || echo "  ✗ Python 3"

    echo ""
    if [ "$VERIFICATION_PASSED" = true ]; then
        echo "Verification: ✓ Passed"
    elif [ "$VERIFICATION_PASSED" = false ]; then
        echo "Verification: ✗ Failed"
    else
        echo "Verification: - Not run (use --verify to run tests)"
    fi

    echo ""
}

print_next_steps() {
    # Determine overall status
    local required_deps_met=true
    [ "$DOTNET_INSTALLED" = false ] && required_deps_met=false
    [ "$MAUI_INSTALLED" = false ] && required_deps_met=false
    [ "$XCODE_INSTALLED" = false ] && required_deps_met=false
    [ "$CLI_TOOLS_INSTALLED" = false ] && required_deps_met=false
    [ "$IOS_SIMULATOR_AVAILABLE" = false ] && required_deps_met=false
    [ "$JAVA_INSTALLED" = false ] && required_deps_met=false
    [ "$ANDROID_SDK_INSTALLED" = false ] && required_deps_met=false
    [ "$ANDROID_AVD_AVAILABLE" = false ] && required_deps_met=false
    [ "$PYTHON_INSTALLED" = false ] && required_deps_met=false

    if [ "$required_deps_met" = true ]; then
        cat << 'EOF'
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 ✅ Environment Ready
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Your development environment is fully configured.

Next Steps:
  1. Build the SDK:
     ./build.sh

  2. Run the example app:
     cd example
     ./build.sh --ios --run
     ./build.sh --android --run

  3. See CONTRIBUTING.md for development workflows

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
EOF
        exit 0
    else
        cat << 'EOF'
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 ⚠ Setup Incomplete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Some required dependencies are still missing.

Next Steps:
  1. Review the summary above
  2. Run this script again to retry: ./setup.sh
  3. See CONTRIBUTING.md for manual setup instructions
  4. If issues persist, check the troubleshooting section

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
EOF
        exit 1
    fi
}

# Argument parsing and help
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                SHOW_HELP=true
                shift
                ;;
            --verify-only)
                VERIFY_ONLY=true
                shift
                ;;
            --verify)
                RUN_VERIFICATION=true
                shift
                ;;
            *)
                echo "Unknown option: $1"
                echo "Run ./setup.sh --help for usage information"
                exit 2
                ;;
        esac
    done
}

show_help() {
    cat << 'EOF'
Datadog MAUI SDK - Environment Setup Script

Usage: ./setup.sh [OPTIONS]

This script checks for required development dependencies and
optionally installs missing components to prepare your environment
for contributing to the Datadog MAUI SDK.

Options:
  -h, --help          Show this help message
  --verify-only       Only run verification tests, skip detection/installation
  --verify            Run verification tests after setup

Examples:
  ./setup.sh                    # Full setup with prompts
  ./setup.sh --verify          # Setup + verification
  ./setup.sh --verify-only      # Only run verification

Required Dependencies:
  • .NET 10 SDK
  • MAUI workload
  • Xcode 15.0+ with Command Line Tools
  • iOS Simulator
  • Java JDK 17+
  • Android SDK
  • Android Virtual Device (AVD)
  • Python 3 (for Android dependency resolution scripts)

For more information, see CONTRIBUTING.md
EOF
}

# Main execution
main() {
    parse_arguments "$@"

    if [ "$SHOW_HELP" = true ]; then
        show_help
        exit 0
    fi

    # If verify-only mode, skip detection and installation
    if [ "$VERIFY_ONLY" = true ]; then
        print_header "Verification Mode"
        echo "Running verification tests only (no detection or installation)"
        echo ""
        run_verification_test
        generate_setup_report
        if [ "$VERIFICATION_PASSED" = true ]; then
            exit 0
        else
            exit 1
        fi
    fi

    print_header "Datadog MAUI SDK - Environment Setup Check"

    # Platform check (exits if not macOS)
    check_macos

    # Check admin privileges early (before any installations)
    check_admin_privileges

    echo "Checking required dependencies..."
    echo ""

    # .NET and MAUI
    check_dotnet
    check_maui_workload

    # iOS tools
    check_xcode
    check_ios_simulator

    # Android tools
    check_java
    check_android_sdk
    check_android_emulator

    # Build tools
    check_python

    # Offer to install missing .NET components
    if [ "$DOTNET_INSTALLED" = false ] || [ "$MAUI_INSTALLED" = false ]; then
        echo ""
        print_header ".NET Components Missing"

        if prompt_user "Would you like to install missing .NET components now?"; then
            install_dotnet_components
        else
            echo ""
            echo "Skipping installation."
            echo -e "\n${BLUE}See CONTRIBUTING.md for manual installation instructions.${NC}"
        fi
    fi

    # Offer to install missing iOS components
    if [ "$XCODE_INSTALLED" = false ] || [ "$CLI_TOOLS_INSTALLED" = false ] || [ "$IOS_SIMULATOR_AVAILABLE" = false ]; then
        echo ""
        print_header "iOS Components Missing"

        if prompt_user "Would you like to set up missing iOS components now?"; then
            install_ios_components
        else
            echo ""
            echo "Skipping iOS setup."
            echo -e "\n${BLUE}See CONTRIBUTING.md for manual installation instructions.${NC}"
        fi
    fi

    # Offer to install missing Android components
    if [ "$JAVA_INSTALLED" = false ] || [ "$ANDROID_SDK_INSTALLED" = false ] || [ "$ANDROID_AVD_AVAILABLE" = false ]; then
        echo ""
        print_header "Android Components Missing"

        if prompt_user "Would you like to set up missing Android components now?"; then
            install_android_components
        else
            echo ""
            echo "Skipping Android setup."
            echo -e "\n${BLUE}See CONTRIBUTING.md for manual installation instructions.${NC}"
        fi
    fi

    # Run verification tests if requested
    if [ "$RUN_VERIFICATION" = true ]; then
        run_verification_test
    fi

    # Generate final report and next steps
    generate_setup_report
    print_next_steps
}

main "$@"
