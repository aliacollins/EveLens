#!/bin/bash
# Builds the macOS .dmg installer inside WSL -- no Mac, no root, no kernel mounts.
# Usage: make-dmg.sh <channel> <version>
# Invoked by release.ps1 AFTER sign-macos.ps1, so the DMG carries the signed,
# notarized, stapled .app. This file is source, NOT a generated artifact.
#
# Why a DMG at all: dragging the app out of a DMG into Applications is a
# Finder-performed install, which is the signal macOS uses to stop Gatekeeper
# App Translocation. Apps installed from our .zip by Terminal mv or unzip-in-
# place keep their quarantine flag and run from a read-only translocated mount
# where the in-place updater can never apply (os error 30 -- the 1.5.0 macOS
# "version never changes" incident). The DMG makes the safe path the easy path.
#
# Toolchain (one-time setup, see below): mkfs.hfsplus (hfsprogs), the userspace
# `hfsplus` image manipulator (planetbeing/libdmg-hfsplus -- populates the image
# WITHOUT mounting; the WSL2 kernel has no hfsplus module), and `dmg`
# (fanquake/libdmg-hfsplus -- wraps the image as a compressed UDZO .dmg).
#   apt-get install hfsprogs cmake zlib1g-dev
#   planetbeing/libdmg-hfsplus  -> cmake --build --target hfsplus -> /usr/local/bin/hfsplus
#   fanquake/libdmg-hfsplus     -> cmake --build                  -> /usr/local/bin/dmg
set -e

CHANNEL="${1:?usage: make-dmg.sh <channel> <version>}"
VERSION="${2:?usage: make-dmg.sh <channel> <version>}"

APP_SRC="/mnt/d/evemon-main/publish/macapp/EveLens.app"
OUT="/mnt/d/evemon-main/releases/EveLens-${CHANNEL}-osx-arm64.dmg"

for tool in mkfs.hfsplus hfsplus dmg; do
    command -v "$tool" > /dev/null || {
        echo "make-dmg: '$tool' not found -- see toolchain comment in this script" >&2
        exit 1
    }
done
[ -d "$APP_SRC" ] || { echo "make-dmg: signed app not found at $APP_SRC" >&2; exit 1; }

STAGE="/tmp/evelens-dmg-root"
IMG="/tmp/evelens-dmg.hfs"
rm -rf "$STAGE" "$IMG"
mkdir -p "$STAGE"

# Stage on the Linux filesystem so permission bits are real -- drvfs (/mnt/d)
# fakes them, and the HFS+ image records whatever the staged files carry.
cp -R "$APP_SRC" "$STAGE/EveLens.app"
find "$STAGE" -type d -exec chmod 755 {} +
find "$STAGE" -type f -exec chmod 644 {} +
chmod 755 "$STAGE/EveLens.app/Contents/MacOS/"*

# Size: app + 10% slack + headroom for HFS+ metadata.
APP_MB=$(du -sm "$STAGE" | cut -f1)
IMG_MB=$(( APP_MB + APP_MB / 10 + 16 ))

dd if=/dev/zero of="$IMG" bs=1M count="$IMG_MB" status=none
mkfs.hfsplus -v "EveLens" "$IMG" > /dev/null

# Populate without mounting: the userspace tool writes the catalog directly.
hfsplus "$IMG" addall "$STAGE" > /dev/null
# The drag-and-drop target -- this symlink IS the installer UI.
hfsplus "$IMG" symlink "Applications" /Applications

# addall stores every file as 644 regardless of the staged mode -- an .app with
# a non-executable main binary mounts fine and then greets the user with
# "EveLens cannot be opened" (found the hard way on the first real-Mac test).
# Restore the executable bit through the tool's own chmod.
for f in "$STAGE/EveLens.app/Contents/MacOS/"*; do
    hfsplus "$IMG" chmod 755 "/EveLens.app/Contents/MacOS/$(basename "$f")" > /dev/null
done

# Verify before wrapping: the binary must exist AND be stored executable.
hfsplus "$IMG" ls /EveLens.app/Contents/MacOS | grep -q '^100755 .* EveLens$' || {
    echo "make-dmg: app binary missing or not executable inside the image" >&2
    hfsplus "$IMG" ls /EveLens.app/Contents/MacOS >&2
    exit 1
}

mkdir -p "$(dirname "$OUT")"
rm -f "$OUT"
dmg "$IMG" "$OUT" > /dev/null

rm -rf "$STAGE" "$IMG"
echo "make-dmg: wrote $OUT ($(du -m "$OUT" | cut -f1) MB, volume 'EveLens', app $VERSION)"
