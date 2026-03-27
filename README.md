# EveLens

**Character Intelligence for EVE Online**

[![GitHub Release](https://img.shields.io/github/v/release/aliacollins/evelens?label=latest)](https://github.com/aliacollins/evelens/releases)
[![GPL licensed](https://img.shields.io/badge/license-GPL%20v2-blue.svg)]()
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)]()
[![Cross-Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-brightgreen.svg)]()

---

## EveLens 1.0.0 Is Here

EveLens is a complete, ground-up rewrite of EVEMon -- the character planner EVE pilots relied on for nearly 20 years. What was once a Windows-only desktop app locked to legacy frameworks is now a modern, cross-platform tool built on **.NET 8** and **Avalonia UI**, running natively on **Windows, Linux, and macOS**.

This isn't a patch or a fork update. It's 114,000 lines of WinForms reduced to 34,000 lines of modern code, with 1,783 tests, 14 architectural laws, and support for **100+ characters** out of the box.

**Website:** [evelens.dev](https://evelens.dev)

---

## Download

**[Download EveLens 1.0.0](https://github.com/aliacollins/evelens/releases/latest)**

| Platform | Format | Requirements |
|----------|--------|-------------|
| **Windows (Installer)** | `EveLens-stable-Setup.exe` | None -- installs .NET 8 automatically |
| **Windows (Portable)** | `EveLens-stable-Portable.zip` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Linux (AppImage)** | `EveLens-*-linux-x86_64.AppImage` | None -- single file, just run |
| **Linux (Portable)** | `EveLens-*-linux-x64.zip` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **macOS (App)** | `EveLens-*-osx-arm64.app.zip` | None -- extract and run |
| **macOS (Portable)** | `EveLens-*-osx-arm64.zip` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |

The Windows installer is **code-signed** by a verified Certum certificate.

---

## Install Guide

### Windows

Download and run `EveLens-stable-Setup.exe`. The installer handles everything -- .NET runtime, shortcuts, auto-updates. The binary is code-signed, so Windows SmartScreen won't block it.

### Linux (AppImage -- Recommended)

```bash
# Download the AppImage from the release page, then:
chmod +x EveLens-*-linux-x86_64.AppImage
./EveLens-*-linux-x86_64.AppImage
```

That's it. Single file, no install, no dependencies. Works on Ubuntu, Fedora, Arch, and most distros.

### Linux (Portable)

```bash
unzip EveLens-*-linux-x64.zip -d evelens
chmod +x evelens/EveLens
./evelens/EveLens
```

Requires [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

### macOS (Apple Silicon)

1. Download `EveLens-*-osx-arm64.app.zip` from the release page
2. Extract the zip -- you'll get `EveLens.app`
3. Drag `EveLens.app` to your Applications folder
4. **First launch:** Right-click the app and select "Open" (required for unsigned apps)
5. After the first launch, it opens normally from Launchpad

### macOS (Portable)

```bash
unzip EveLens-*-osx-arm64.zip -d evelens
chmod +x evelens/EveLens
./evelens/EveLens
```

> **Coming from EVEMon?** We recommend a fresh start. EveLens is a complete rewrite -- your EVE characters are tied to your ESI tokens, not your old settings. Add your characters through the ESI login and you're good to go.

---

## What You Get

### Cross-Platform -- Finally
EveLens runs natively on Windows, Linux, and macOS from a single codebase. System tray, notifications, clipboard, and dialogs all use native platform APIs. Same features, same UI, same updates on every platform. No Wine, no workarounds.

### 100+ Character Support
Built from the ground up for scale. Smart batch scheduling, per-character circuit breakers, and staggered ESI polling mean 60, 80, or 100+ characters load fast and stay responsive. The old EVEMon collapsed at ~50 characters -- EveLens doesn't.

### 6 Dark Themes
Six color palettes inspired by New Eden's empires. Every control respects the active theme with runtime switching.

- **Dark Space** (default) -- navy + gold
- **Caldari Blue** -- cool steel blues
- **Amarr Gold** -- warm golds and ambers
- **Minmatar Rust** -- earthy oranges
- **Gallente Green** -- teals and greens
- **Midnight** -- deep purples

### ESI Scope Control
You choose what data EveLens can access. Three presets (Full, Standard, Skill Planner Only) or custom selection from 16 feature categories. When you re-authenticate with fewer scopes, EveLens automatically detects revoked permissions, clears stale data, and deletes disk caches for those endpoints. Your data, your rules.

### Skill Constellation
GPU-accelerated interactive visualization of EVE's ~400-skill tree rendered as a star constellation. Skills as stars, prerequisites as connecting lines, skill groups as colored nebula clusters. Zoom, pan, search, click-to-inspect. Trained skills glow, training skills pulse.

### Full Character Monitoring
20+ data tabs per character: Skills, Skill Queue, Clones, Assets, Market Orders, Contracts, Industry Jobs, Wallet Journal, Wallet Transactions, Mail, Notifications, Kill Log, Planetary Industry, Research Points, Standings, Contacts, Employment History, Medals, Loyalty Points, Factional Warfare.

### Skill Planning
Multi-tab plan editor with integrated Skills, Ships, Items, and Blueprint browsers. Entry detail panel with prerequisite tracking and completion checkmarks. Attribute optimization and injector cost estimates.

### Smart ESI Error Handling
Per-endpoint health state machine that tracks ESI reliability per character. Transient errors are silently retried. Persistent failures get one activity log entry, not a hundred. Recovery is automatic. No more error spam filling your notification bell.

### SDE Updated for Catalyst Expansion (March 19, 2026)
- 5 new skills: Capital Disintegrator Specialization, Amarr/Caldari/Gallente/Minmatar Fighter Specialization
- 82 new types, 31 modified types, 127 typeDogma changes
- All balance changes: carrier cargo bays, fighter stats, FAX cap booster bonuses, Black Ops tank nerfs

### Everything Else
- **Clone tracking** -- jump clone locations, implants, and jump cooldown timer (respects Infomorph Synchronizing skill)
- **Native OS notifications** -- skill completions via your system's notification center
- **Activity center** -- bell icon with 200-entry event log, smart noise filtering
- **Character groups** -- organize characters by purpose
- **Balance change indicator** -- real-time ISK delta with directional arrows
- **Auto-update** -- Velopack-powered updates with delta downloads, checks in the background
- **Data cache** -- near-instant tab loading, limited offline viewing
- **Per-character endpoint manager** -- toggle individual ESI endpoints on/off
- **Privacy mode** -- for streamers and screenshots
- **Collapse persistence** -- your expand/collapse state remembered across sessions
- **Window position memory** -- saves and restores across restarts and multi-monitor setups
- **Code-signed Windows installer** -- verified Certum certificate, no SmartScreen warnings

---

## Update Channels

| Channel | Use Case | Download |
|---------|----------|----------|
| Stable | Recommended for daily use | [Latest Release](https://github.com/aliacollins/evelens/releases/latest) |
| Beta | Pre-release testing | [Beta Releases](https://github.com/aliacollins/evelens/releases) |
| **Alpha** | Bleeding edge, experimental features(you are here) | [Alpha Releases](https://github.com/aliacollins/evelens/releases) |

---

## Report Issues

Found a bug? Please report it: **[GitHub Issues](https://github.com/aliacollins/evelens/issues)**

---

## Maintainer

**Alia Collins** (EVE Online) | [evelens.dev](https://evelens.dev) | [CapsuleerKit](https://www.capsuleerkit.com/)

---

## Heritage

EVEMon was created by **Jimi Charalampidis** and **57+ contributors** (2006-2015), then maintained by **Peter Han** (2015-2021). Their 20 years of work built the foundation that EveLens stands on. This project is a direct continuation of that lineage -- same GPL v2 license, same commitment to the EVE community.

- [Original EVEMon Dev Team](https://github.com/evemondevteam/)
- [Peter Han's fork](https://github.com/peterhaneve/evemon)

I'm not accepting donations -- I just want to know if EveLens makes your EVE life a little easier. If it does, that's enough for me. Building something people genuinely find useful and actually use is what drives me. Share it with your corp, mention it in a fleet chat, or just drop me a message and tell me what you think. If it works for you, that would mean the world -- not ISK. I take donations in kind words. o7

---

## Alpha Changelog (Cumulative)

See [CHANGELOG.md](CHANGELOG.md) for the full changelog. The alpha channel receives all in-progress changes.

## Features Being Tested

Features currently in alpha/beta testing before stable release.

## License

GPL v2 -- See [LICENSE](src/EveLens.Common/Resources/License/gpl.txt)
