#!/bin/bash
# build-macapp.sh - Creates a macOS .app bundle for EveLens (ARM64)
# Usage: bash scripts/build-macapp.sh [version]
# Runs in WSL or native Linux/macOS. Produces a .app.zip ready for distribution.

set -euo pipefail

# Ensure cargo/rcodesign are on PATH (installed via rustup)
[ -f "$HOME/.cargo/env" ] && source "$HOME/.cargo/env"

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

# Step 4: Ad-hoc codesign to prevent "app is damaged" on macOS
# Without this, Gatekeeper quarantine blocks the app entirely on Sonoma+.
# Ad-hoc signing (no Apple Developer ID) allows "right-click → Open" to work
# and avoids the scary "damaged" error.
if command -v codesign &>/dev/null; then
    # Native macOS: use codesign
    echo "Ad-hoc signing with codesign..."
    codesign --force --deep --sign - "$APP_BUNDLE"
elif command -v rcodesign &>/dev/null; then
    # Linux/WSL: use rcodesign (cargo install apple-codesign)
    echo "Ad-hoc signing with rcodesign..."
    rcodesign sign "$APP_BUNDLE"
else
    echo "Warning: No codesign tool available. App may show 'damaged' on macOS." >&2
    echo "  Install rcodesign: cargo install apple-codesign" >&2
    echo "  Users will need to run: xattr -cr EveLens.app" >&2
fi

# Step 5: Create distributable zip
echo "Creating distributable zip..."
rm -f "$OUTPUT"
cd "$REPO_ROOT/publish"
if command -v zip &>/dev/null; then
    zip -ry "$(basename "$OUTPUT")" "EveLens.app/" >/dev/null
else
    # Fallback: use Python zipfile when zip is not installed (common in minimal WSL)
    # Must preserve Unix permissions (especially +x on main binary) or macOS
    # will show "app is damaged" when trying to launch.
    # Fallback: use Python zipfile when zip is not installed (common in minimal WSL)
    # Must preserve Unix permissions (especially +x on main binary) or macOS
    # will show "app is damaged" when trying to launch.
    ZIPNAME="$(basename "$OUTPUT")"
    python3 -c "
import zipfile, os, stat
with zipfile.ZipFile('$ZIPNAME', 'w', zipfile.ZIP_DEFLATED) as zf:
    for root, dirs, files in os.walk('EveLens.app'):
        for f in files:
            fp = os.path.join(root, f)
            info = zipfile.ZipInfo(fp)
            st = os.stat(fp)
            info.external_attr = (st.st_mode & 0xFFFF) << 16
            with open(fp, 'rb') as fh:
                zf.writestr(info, fh.read())
"
fi
cd "$REPO_ROOT"

# Cleanup
rm -rf "$APP_BUNDLE"
rm -rf "$REPO_ROOT/publish/osx-arm64-sc"

echo "=== macOS app bundle created: $OUTPUT ==="
echo "Size: $(du -h "$OUTPUT" | cut -f1)"
echo ""
echo "Note: This app is unsigned. Users must run: xattr -cr EveLens.app"
