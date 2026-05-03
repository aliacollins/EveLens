#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

PUBLISH_DIR="/mnt/d/evemon-main/publish/linux-x64"
OUTPUT_DIR="/mnt/d/evemon-main/releases"
CHANNEL="${1:-stable}"

echo "=== Creating AppImage for $CHANNEL ==="

WORK="/tmp/evelens-appimage"
rm -rf "$WORK"
mkdir -p "$WORK/AppDir/usr/bin"
mkdir -p "$WORK/AppDir/usr/share/icons/hicolor/256x256/apps"

cp -r "$PUBLISH_DIR"/* "$WORK/AppDir/usr/bin/"
chmod +x "$WORK/AppDir/usr/bin/EveLens"

cat > "$WORK/AppDir/EveLens.desktop" << 'EOF'
[Desktop Entry]
Name=EveLens
Comment=Character Intelligence for EVE Online
Exec=EveLens
Icon=evelens
Type=Application
Categories=Game;Utility;
EOF

cat > "$WORK/AppDir/AppRun" << 'EOF'
#!/bin/bash
HERE="$(dirname "$(readlink -f "${0}")")"
exec "$HERE/usr/bin/EveLens" "$@"
EOF
chmod +x "$WORK/AppDir/AppRun"

# Copy the real 256x256 icon
cp "$PROJECT_ROOT/installer/icons/evelens-256.png" "$WORK/AppDir/evelens.png"
cp "$PROJECT_ROOT/installer/icons/evelens-256.png" "$WORK/AppDir/usr/share/icons/hicolor/256x256/apps/evelens.png"

# Download appimagetool if not cached
TOOL="$WORK/appimagetool"
if [ ! -f "$TOOL" ]; then
    echo "Downloading appimagetool..."
    wget -q "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage" -O "$TOOL"
    chmod +x "$TOOL"
fi

cd "$WORK"
ARCH=x86_64 "$TOOL" --no-appstream AppDir "EveLens-${CHANNEL}-linux-x86_64.AppImage"

cp "EveLens-${CHANNEL}-linux-x86_64.AppImage" "$OUTPUT_DIR/"
ls -lh "$OUTPUT_DIR/EveLens-${CHANNEL}-linux-x86_64.AppImage"
echo "=== AppImage created ==="
