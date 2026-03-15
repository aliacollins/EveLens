#!/bin/bash
# build-appimage.sh - Creates an AppImage for EveLens (Linux x64)
# Usage: bash scripts/build-appimage.sh [version]
# Runs in WSL or native Linux. Downloads appimagetool automatically.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
VERSION="${1:-1.0.0-alpha.1}"
TOOLS_DIR="$SCRIPT_DIR/.tools"
APPDIR="$REPO_ROOT/publish/EveLens.AppDir"
OUTPUT="$REPO_ROOT/publish/EveLens-${VERSION}-linux-x64.AppImage"

echo "=== Building EveLens AppImage v${VERSION} ==="

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

# Step 1: Publish self-contained for linux-x64
echo "Publishing self-contained linux-x64 build..."
"$DOTNET" publish "$(winpath "$REPO_ROOT/src/EveLens.Avalonia/EveLens.Avalonia.csproj")" \
    -c Release -r linux-x64 --self-contained true \
    -o "$(winpath "$REPO_ROOT/publish/linux-x64-sc")"

# Step 2: Create AppDir structure
echo "Creating AppDir..."
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"

# Copy published files
cp -r "$REPO_ROOT/publish/linux-x64-sc/"* "$APPDIR/usr/bin/"

# Copy AppRun and make executable
cp "$REPO_ROOT/installer/linux/AppRun" "$APPDIR/AppRun"
chmod +x "$APPDIR/AppRun"

# Copy desktop file
cp "$REPO_ROOT/installer/linux/evelens.desktop" "$APPDIR/evelens.desktop"

# Copy icons — multiple sizes for proper desktop integration
cp "$REPO_ROOT/installer/icons/evelens-256.png" "$APPDIR/evelens.png"
cp "$REPO_ROOT/installer/icons/evelens-256.png" "$APPDIR/.DirIcon"
for SIZE in 16 32 48 64 128 256; do
    ICON_FILE="$REPO_ROOT/installer/icons/evelens-${SIZE}.png"
    if [ -f "$ICON_FILE" ]; then
        mkdir -p "$APPDIR/usr/share/icons/hicolor/${SIZE}x${SIZE}/apps"
        cp "$ICON_FILE" "$APPDIR/usr/share/icons/hicolor/${SIZE}x${SIZE}/apps/evelens.png"
    fi
done

# Ensure main binary is executable
chmod +x "$APPDIR/usr/bin/EveLens"

# Step 3: Download appimagetool if not cached
mkdir -p "$TOOLS_DIR"
APPIMAGETOOL="$TOOLS_DIR/appimagetool"

if [ ! -x "$APPIMAGETOOL" ]; then
    echo "Downloading appimagetool..."
    APPIMAGETOOL_URL="https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
    curl -fSL "$APPIMAGETOOL_URL" -o "$TOOLS_DIR/appimagetool.AppImage"
    chmod +x "$TOOLS_DIR/appimagetool.AppImage"

    # Extract for FUSE-less environments (WSL2, Docker, CI)
    echo "Extracting appimagetool (FUSE workaround)..."
    cd "$TOOLS_DIR"
    ./appimagetool.AppImage --appimage-extract >/dev/null 2>&1 || true
    if [ -x "$TOOLS_DIR/squashfs-root/AppRun" ]; then
        ln -sf "$TOOLS_DIR/squashfs-root/AppRun" "$APPIMAGETOOL"
    else
        # Direct execution works (native Linux with FUSE)
        ln -sf "$TOOLS_DIR/appimagetool.AppImage" "$APPIMAGETOOL"
    fi
    cd "$REPO_ROOT"
fi

# Step 4: Build AppImage
echo "Building AppImage..."
rm -f "$OUTPUT"
ARCH=x86_64 "$APPIMAGETOOL" "$APPDIR" "$OUTPUT"

# Cleanup
rm -rf "$APPDIR"
rm -rf "$REPO_ROOT/publish/linux-x64-sc"

echo "=== AppImage created: $OUTPUT ==="
echo "Size: $(du -h "$OUTPUT" | cut -f1)"
