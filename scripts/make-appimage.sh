#!/bin/bash
set -e

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

# Minimal PNG icon placeholder
printf '\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x01\x00\x00\x00\x01\x08\x02\x00\x00\x00\x90wS\xde\x00\x00\x00\x0cIDATx\x9cc\xf8\x0f\x00\x00\x01\x01\x00\x05\x18\xd8N\x00\x00\x00\x00IEND\xaeB\x60\x82' > "$WORK/AppDir/evelens.png"
cp "$WORK/AppDir/evelens.png" "$WORK/AppDir/usr/share/icons/hicolor/256x256/apps/"

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
