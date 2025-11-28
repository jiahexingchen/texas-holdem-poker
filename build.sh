#!/bin/bash

# Texas Hold'em Poker - Build Script
# Usage: ./build.sh [android|ios|windows|macos|server]

set -e

PROJECT_ROOT=$(cd "$(dirname "$0")" && pwd)
CLIENT_PATH="$PROJECT_ROOT/client"
SERVER_PATH="$PROJECT_ROOT/server"
BUILD_PATH="$PROJECT_ROOT/builds"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Create build directory
mkdir -p "$BUILD_PATH"

build_android() {
    log_info "Building Android APK..."
    
    # Check for Unity
    UNITY_PATH=""
    if [ -d "/Applications/Unity/Hub/Editor" ]; then
        UNITY_PATH=$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | head -1)
    elif [ -d "$HOME/Unity/Hub/Editor" ]; then
        UNITY_PATH=$(ls -d $HOME/Unity/Hub/Editor/*/Editor/Unity 2>/dev/null | head -1)
    elif [ -f "/usr/bin/unity" ]; then
        UNITY_PATH="/usr/bin/unity"
    fi
    
    if [ -z "$UNITY_PATH" ]; then
        log_error "Unity not found. Please install Unity Hub and Unity 6."
        log_info "Download from: https://unity.com/download"
        exit 1
    fi
    
    log_info "Using Unity: $UNITY_PATH"
    
    OUTPUT_PATH="$BUILD_PATH/TexasHoldem_$(date +%Y%m%d_%H%M%S).apk"
    
    "$UNITY_PATH" \
        -batchmode \
        -nographics \
        -projectPath "$CLIENT_PATH" \
        -executeMethod TexasHoldem.Editor.BuildScript.BuildAndroidCLI \
        -buildPath "$OUTPUT_PATH" \
        -logFile "$BUILD_PATH/build_android.log" \
        -quit
    
    if [ -f "$OUTPUT_PATH" ]; then
        log_info "Build successful: $OUTPUT_PATH"
        log_info "APK size: $(du -h "$OUTPUT_PATH" | cut -f1)"
    else
        log_error "Build failed. Check $BUILD_PATH/build_android.log"
        exit 1
    fi
}

build_ios() {
    log_info "Building iOS project..."
    
    # Similar to Android but for iOS
    UNITY_PATH=$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | head -1)
    
    if [ -z "$UNITY_PATH" ]; then
        log_error "Unity not found on macOS"
        exit 1
    fi
    
    OUTPUT_PATH="$BUILD_PATH/iOS_$(date +%Y%m%d_%H%M%S)"
    
    "$UNITY_PATH" \
        -batchmode \
        -nographics \
        -projectPath "$CLIENT_PATH" \
        -executeMethod TexasHoldem.Editor.BuildScript.BuildiOSCLI \
        -buildPath "$OUTPUT_PATH" \
        -logFile "$BUILD_PATH/build_ios.log" \
        -quit
    
    log_info "Xcode project created at: $OUTPUT_PATH"
    log_info "Open in Xcode to archive and export IPA"
}

build_server() {
    log_info "Building Go server..."
    
    cd "$SERVER_PATH"
    
    # Download dependencies
    go mod tidy
    
    # Build for current platform
    go build -o "$BUILD_PATH/server" ./cmd/server
    
    # Cross-compile for Linux (production)
    GOOS=linux GOARCH=amd64 go build -o "$BUILD_PATH/server_linux_amd64" ./cmd/server
    
    log_info "Server binaries built:"
    ls -la "$BUILD_PATH"/server*
}

run_server() {
    log_info "Starting server..."
    
    cd "$SERVER_PATH"
    go run ./cmd/server
}

show_help() {
    echo "Texas Hold'em Poker - Build Script"
    echo ""
    echo "Usage: ./build.sh [command]"
    echo ""
    echo "Commands:"
    echo "  android    Build Android APK"
    echo "  ios        Build iOS Xcode project"
    echo "  server     Build Go server binaries"
    echo "  run        Run server locally"
    echo "  help       Show this help"
    echo ""
    echo "Examples:"
    echo "  ./build.sh android   # Build APK"
    echo "  ./build.sh server    # Build server"
    echo "  ./build.sh run       # Run server for testing"
}

case "$1" in
    android)
        build_android
        ;;
    ios)
        build_ios
        ;;
    server)
        build_server
        ;;
    run)
        run_server
        ;;
    help|--help|-h|"")
        show_help
        ;;
    *)
        log_error "Unknown command: $1"
        show_help
        exit 1
        ;;
esac
