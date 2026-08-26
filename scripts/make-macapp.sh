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

# Copy published files preserving structure
cp -r "$PUBLISH_DIR"/* "$APP_DIR/Contents/MacOS/"

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

# Zip with Unix permissions preserved (use cd to get clean paths)
cd /tmp
zip -r -y "$RELEASES_DIR/EveLens-${CHANNEL}-osx-arm64.app.zip" EveLens.app
rm -rf "$APP_DIR"
echo "=== macOS .app bundle created ==="
