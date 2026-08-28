#!/bin/bash
# Builds the macOS .app bundle inside WSL (Unix permissions preserved).
# Usage: make-macapp.sh <channel> <version>
# Invoked by release.ps1 -- this file is source, NOT a generated artifact.
set -e

CHANNEL="${1:?usage: make-macapp.sh <channel> <version>}"
VERSION="${2:?usage: make-macapp.sh <channel> <version>}"

PUBLISH_DIR="/mnt/d/evemon-main/publish/osx-arm64"
RELEASES_DIR="/mnt/d/evemon-main/releases"
ICONS_DIR="/mnt/d/evemon-main/installer/icons"

APP_DIR="/tmp/EveLens.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# Apple-conformant layout: Contents/MacOS holds ONLY executable code (the
# single-file apphost + native dylibs) so the bundle can be code-signed and
# notarized -- non-Mach-O files inside MacOS/ fall under Apple's "nested code"
# resource rules and break the signature seal. Everything else (datafiles,
# changelog) lives in Contents/Resources, where EveLens's resource resolution
# (Datafile.InstallResourceDirectories) knows to look.
cp "$PUBLISH_DIR/EveLens" "$APP_DIR/Contents/MacOS/"
cp "$PUBLISH_DIR"/*.dylib "$APP_DIR/Contents/MacOS/"
cp -r "$PUBLISH_DIR/Resources/." "$APP_DIR/Contents/Resources/"
[ -f "/mnt/d/evemon-main/CHANGELOG.md" ] && cp "/mnt/d/evemon-main/CHANGELOG.md" "$APP_DIR/Contents/Resources/"

# Set executable permission on the main binary
chmod +x "$APP_DIR/Contents/MacOS/EveLens"

# Copy icon into Resources
cp "$ICONS_DIR/evelens.icns" "$APP_DIR/Contents/Resources/evelens.icns"

# Create Info.plist
cat > "$APP_DIR/Contents/Info.plist" << PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>
  <string>EveLens</string>
  <key>CFBundleDisplayName</key>
  <string>EveLens</string>
  <key>CFBundleIdentifier</key>
  <string>dev.evelens.app</string>
  <key>CFBundleVersion</key>
  <string>$VERSION</string>
  <key>CFBundleShortVersionString</key>
  <string>$VERSION</string>
  <key>CFBundleExecutable</key>
  <string>EveLens</string>
  <key>CFBundleIconFile</key>
  <string>evelens</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

# Hand the bundle to release.ps1 at a Windows-visible path: signing
# (rcodesign, Windows side) and zipping (zip-macapp.py assigns unix modes by
# rule) both happen there now. Zipping here would bake in a signature-less app.
OUT_DIR="/mnt/d/evemon-main/publish/macapp"
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"
cp -r "$APP_DIR" "$OUT_DIR/EveLens.app"
rm -rf "$APP_DIR"
echo "=== macOS .app bundle created at $OUT_DIR/EveLens.app ==="
