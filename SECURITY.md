# Security

## Reporting a Problem

Please report security problems privately. Do not open a public issue or discussion.

Use GitHub's private reporting: go to the [Security tab](https://github.com/aliacollins/EveLens/security) of the repository and choose "Report a vulnerability".

Include what you found, how to reproduce it, and which version of EveLens you used. You will get a reply, and you are credited in the release notes when the fix ships unless you ask not to be.

## What to Report

- Anything that exposes ESI tokens, character data or settings to someone else.
- Anything that lets a file, a link or data from outside make EveLens run code or write outside its own folders.
- Problems with the installer, the updater or a published release file.

## Releases

Official releases are published only at [github.com/aliacollins/EveLens/releases](https://github.com/aliacollins/EveLens/releases). Windows builds are code signed and macOS builds are notarized. Each release includes a `SHA256SUMS.txt` file with the SHA-256 checksum of every download.

## Supported Versions

Security fixes are made for the latest stable release.
