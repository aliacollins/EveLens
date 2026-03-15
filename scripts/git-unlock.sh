#!/bin/bash
# git-unlock.sh — Removes the git lock if correct passphrase is provided
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOCKFILE="$REPO_ROOT/.gitlock"
PASSPHRASE="GITPUSHALLOW-HARIOM"

if [ -z "$1" ]; then
    echo "Usage: scripts/git-unlock.sh <passphrase>"
    echo "Git operations are LOCKED."
    exit 1
fi

if [ "$1" != "$PASSPHRASE" ]; then
    echo "WRONG PASSPHRASE. Git remains locked."
    exit 1
fi

rm -f "$LOCKFILE"
echo "Git operations UNLOCKED. You may now commit and push."
echo "Run: scripts/git-lock.sh to re-lock when done."
