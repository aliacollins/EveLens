#!/bin/bash
set -e

APP_DIR="/tmp/EveLens.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# Copy published files preserving structure
cp -r /mnt/d/evemon-main/publish/osx-arm64/* "$APP_DIR/Contents/MacOS/"

# Set executable permission on the main binary
chmod +x "$APP_DIR/Contents/MacOS/EveLens"

# Create Info.plist
cat > "$APP_DIR/Contents/Info.plist" << 'PLIST'
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
  <string>1.2.0-beta.1</string>
  <key>CFBundleShortVersionString</key>
  <string>1.2.0-beta.1</string>
  <key>CFBundleExecutable</key>
  <string>EveLens</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

# Zip with Unix permissions preserved (use cd to get clean paths)
cd /tmp
zip -r -y "/mnt/d/evemon-main/releases/EveLens-1.2.0-beta.1-osx-arm64.app.zip" EveLens.app
rm -rf "$APP_DIR"
echo "=== macOS .app bundle created ==="