#!/bin/bash
# git-lock.sh — Re-enables the git lock (blocks commits and pushes)
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOCKFILE="$REPO_ROOT/.gitlock"

echo "LOCKED" > "$LOCKFILE"
echo "Git operations LOCKED. Commits and pushes are blocked."
echo "Run: scripts/git-unlock.sh <passphrase> to unlock."
