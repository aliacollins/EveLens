#!/bin/bash
# build-macapp.sh - Creates a macOS .app bundle for EveLens (ARM64)
# Usage: bash scripts/build-macapp.sh [version]
# Runs in WSL or native Linux/macOS. Produces a .app.zip ready for distribution.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
VERSION="${1:-1.0.0-alpha.1}"
APP_BUNDLE="$REPO_ROOT/publish/EveLens.app"
OUTPUT="$REPO_ROOT/publish/EveLens-${VERSION}-osx-arm64.app.zip"

echo "=== Building EveLens macOS App Bundle v${VERSION} ==="

# Find dotnet: prefer native, fall back to Windows-side
USE_WIN_DOTNET=false
if command -v dotnet &>/dev/null; then
    DOTNET="dotnet"
elif [ -x "/mnt/c/Program Files/dotnet/dotnet.exe" ]; then
    DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"
    USE_WIN_DOTNET=true
else
    echo "Error: dotnet not found. Install .NET 8 SDK or ensure Windows dotnet is accessible." >&2
    exit 1
fi

# Convert WSL path to Windows path when using Windows dotnet
winpath() {
    if [ "$USE_WIN_DOTNET" = true ]; then
        wslpath -w "$1"
    else
        echo "$1"
    fi
}

# Step 1: Publish self-contained for osx-arm64
echo "Publishing self-contained osx-arm64 build..."
"$DOTNET" publish "$(winpath "$REPO_ROOT/src/EveLens.Avalonia/EveLens.Avalonia.csproj")" \
    -c Release -r osx-arm64 --self-contained true \
    -o "$(winpath "$REPO_ROOT/publish/osx-arm64-sc")"

# Step 2: Create .app bundle structure
echo "Creating .app bundle..."
rm -rf "$APP_BUNDLE"
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy published files to MacOS directory
cp -r "$REPO_ROOT/publish/osx-arm64-sc/"* "$APP_BUNDLE/Contents/MacOS/"

# Copy and patch Info.plist
sed "s/VERSION_PLACEHOLDER/${VERSION}/g" \
    "$REPO_ROOT/installer/macos/Info.plist" > "$APP_BUNDLE/Contents/Info.plist"

# Step 3: Generate .icns icon
ICON_SRC="$REPO_ROOT/installer/icons/evelens-256.png"
ICNS_DST="$APP_BUNDLE/Contents/Resources/evelens.icns"

if command -v iconutil &>/dev/null && command -v sips &>/dev/null; then
    # macOS: use native tools
    echo "Generating .icns with iconutil..."
    ICONSET_DIR=$(mktemp -d)/evelens.iconset
    mkdir -p "$ICONSET_DIR"
    for SIZE in 16 32 64 128 256; do
        sips -z $SIZE $SIZE "$ICON_SRC" --out "$ICONSET_DIR/icon_${SIZE}x${SIZE}.png" >/dev/null 2>&1
    done
    for SIZE in 16 32 64 128; do
        DOUBLE=$((SIZE * 2))
        sips -z $DOUBLE $DOUBLE "$ICON_SRC" --out "$ICONSET_DIR/icon_${SIZE}x${SIZE}@2x.png" >/dev/null 2>&1
    done
    iconutil -c icns "$ICONSET_DIR" -o "$ICNS_DST"
    rm -rf "$(dirname "$ICONSET_DIR")"
elif command -v png2icns &>/dev/null; then
    # Linux with png2icns (icnsutils package)
    echo "Generating .icns with png2icns..."
    png2icns "$ICNS_DST" "$ICON_SRC"
elif [ -f "$REPO_ROOT/installer/icons/evelens.icns" ]; then
    # Pre-built .icns exists in repo
    echo "Using pre-built .icns..."
    cp "$REPO_ROOT/installer/icons/evelens.icns" "$ICNS_DST"
else
    echo "Warning: No .icns generation tool available. App will use default icon." >&2
    echo "Install icnsutils (apt install icnsutils) or run on macOS for proper icon." >&2
fi

# Ensure main binary is executable
chmod +x "$APP_BUNDLE/Contents/MacOS/EveLens"

# Step 4: Create distributable zip
echo "Creating distributable zip..."
rm -f "$OUTPUT"
cd "$REPO_ROOT/publish"
zip -ry "$(basename "$OUTPUT")" "EveLens.app/" >/dev/null
cd "$REPO_ROOT"

# Cleanup
rm -rf "$APP_BUNDLE"
rm -rf "$REPO_ROOT/publish/osx-arm64-sc"

echo "=== macOS app bundle created: $OUTPUT ==="
echo "Size: $(du -h "$OUTPUT" | cut -f1)"
echo ""
echo "Note: This app is unsigned. Users must right-click → Open on first launch."
