# EveLens

**Character Intelligence for EVE Online**

[![GPL licensed](https://img.shields.io/badge/license-GPL%20v2-blue.svg)]()
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)]()
[![Cross-Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-brightgreen.svg)]()
[![ALPHA](https://img.shields.io/badge/branch-ALPHA-red.svg)]()

---

## EveLens Is Now in Beta

**Starting March 16, 2026** — EveLens enters beta testing ahead of its **stable release on March 20th**.

EveLens is a complete, ground-up rewrite of EVEMon — the character planner EVE pilots relied on for nearly 20 years. What was once a Windows-only desktop app locked to legacy frameworks is now a modern, cross-platform tool built on .NET 8 and Avalonia UI, running natively on **Windows, Linux, and macOS**.

This isn't a patch or a fork update. It's 114,000 lines of WinForms reduced to 34,000 lines of modern code, with 1,741 tests, 14 architectural laws, and support for **100+ characters** out of the box.

**Current Version: 1.0.0-alpha.42**

**Website:** [evelens.dev](https://evelens.dev)

---

## Download

[EveLens Installer](https://github.com/aliacollins/evelens/releases/tag/alpha)

| Platform | Format | Requirements |
|----------|--------|-------------|
| **Windows (Installer)** | `.exe` installer | Installs .NET 8 automatically |
| **Windows (Portable)** | `.zip` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Linux (AppImage)** | `.AppImage` | None — just download and run |
| **Linux (Portable)** | `.zip` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **macOS (App)** | `.app.zip` | None — extract, drag to Applications |
| **macOS (Portable)** | `.zip` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |

### Quick Start

**Windows:** Download and run the installer — it handles everything.

**Linux:**
```bash
chmod +x EveLens-1.0.0-alpha.42-linux-x64.AppImage
./EveLens-1.0.0-alpha.42-linux-x64.AppImage
```

**macOS:** Extract the zip, drag EveLens to Applications, right-click → Open on first launch (unsigned app).

> **Coming from EVEMon?** We recommend a fresh install. EveLens is a complete rewrite and a clean start gives the smoothest experience. Your EVE characters are tied to your ESI tokens, not your old settings.

---

## What You Get

### Cross-Platform — Finally
EveLens runs natively on Windows, Linux, and macOS from a single codebase. System tray, notifications, clipboard, and dialogs all use native platform APIs. Same features, same UI, same updates on every platform. No Wine, no workarounds.

### 100+ Character Support
Built from the ground up for scale. Smart batch scheduling, per-character circuit breakers, and staggered ESI polling mean 60, 80, or 100+ characters load fast and stay responsive. The old EVEMon collapsed at ~50 characters — EveLens doesn't.

### 6 Dark Themes
Six color palettes inspired by New Eden's empires. Every control respects the active theme with runtime switching.

- **Dark Space** (default) — navy + gold
- **Caldari Blue** — cool steel blues
- **Amarr Gold** — warm golds and ambers
- **Minmatar Rust** — earthy oranges
- **Gallente Green** — teals and greens
- **Midnight** — deep purples

### ESI Scope Control
You choose what data EveLens can access. Three presets (Full, Standard, Skill Planner Only) or custom selection from 16 feature categories. When you re-authenticate with fewer scopes, EveLens automatically detects revoked permissions, clears stale data, and deletes disk caches for those endpoints. Your data, your rules.

### Skill Constellation
GPU-accelerated interactive visualization of EVE's ~400-skill tree rendered as a star constellation. Skills as stars, prerequisites as connecting lines, skill groups as colored nebula clusters. Zoom, pan, search, click-to-inspect. Trained skills glow, training skills pulse.

### Full Character Monitoring
20+ data tabs per character: Skills, Skill Queue, Clones, Assets, Market Orders, Contracts, Industry Jobs, Wallet Journal, Wallet Transactions, Mail, Notifications, Kill Log, Planetary Industry, Research Points, Standings, Contacts, Employment History, Medals, Loyalty Points, Factional Warfare.

### Skill Planning
Multi-tab plan editor with integrated Skills, Ships, Items, and Blueprint browsers. Entry detail panel with prerequisite tracking and completion checkmarks. Attribute optimization and injector cost estimates.

### Everything Else
- **Clone tracking** — jump clone locations, implants, and jump cooldown timer (respects Infomorph Synchronizing skill)
- **Native OS notifications** — skill completions via your system's notification center
- **Activity center** — bell icon with 200-entry event log, smart noise filtering
- **Character groups** — organize characters by purpose with drag reorder
- **Balance change indicator** — real-time ISK delta with directional arrows
- **Auto-update** — checks for updates in the background, one-click install
- **Data cache** — near-instant tab loading, limited offline viewing
- **Per-character endpoint manager** — toggle individual ESI endpoints on/off
- **Privacy mode** — for streamers and screenshots
- **Collapse persistence** — your expand/collapse state remembered across sessions

---

## What's New in 1.0.0

EveLens 1.0.0 is a complete rewrite. Here's what changed from the original EVEMon:

- **Cross-platform** — Windows, Linux, and macOS from a single codebase (Avalonia UI)
- **Native installers** — Linux AppImage and macOS .app bundle, no .NET install required
- **100+ character support** — smart ESI scheduling with per-character resilience
- **6 faction dark themes** with runtime switching
- **ESI scope control** — choose exactly what data to share, with dynamic revocation
- **Skill constellation** — GPU-accelerated interactive skill tree visualization
- **Clone tracking** — jump clone locations, implants, cooldown timer
- **Smarter error handling** — transient ESI failures suppressed, persistent errors categorized
- **Collapse persistence** — expand/collapse state saved across all views and restarts
- **Activity center** — notification bell with event history and smart noise filtering
- **Character groups** — organize and reorder characters
- **Auto-update** — background checking with one-click install on all platforms
- **Redesigned settings** — single scrollable page with all options
- **1,741 tests** with 14 architectural laws enforced automatically
- **WinForms removed** — pure Avalonia, .NET 8 target

---

## Alpha Changelog (Cumulative)

See "What's New in 1.0.0" above for the full feature list.

---

## Features Being Tested

- Cross-platform stability on Linux and macOS
- 100+ character ESI scheduling and resilience
- Auto-update on all platforms
- Group collapse/expand on overview
- Window position/size persistence

---

## Update Channels

| Channel | Use Case | Download |
|---------|----------|----------|
| Stable | Recommended for daily use (March 20th) | [Latest Release](https://github.com/aliacollins/evelens/releases/latest) |
| Beta | Pre-release testing | [Beta Release](https://github.com/aliacollins/evelens/releases/tag/beta) |
| **Alpha** | Bleeding edge, experimental features(you are here) | [Alpha Release](https://github.com/aliacollins/evelens/releases/tag/alpha) |

---

## Report Issues

Found a bug? Please report it: [GitHub Issues](https://github.com/aliacollins/evelens/issues)

---

## Maintainer

**Alia Collins** (EVE Online) | [evelens.dev](https://evelens.dev) | [CapsuleerKit](https://www.capsuleerkit.com/)

---

## Heritage

EVEMon was created by **Jimi Charalampidis** and **57+ contributors** (2006-2015), then maintained by **Peter Han** (2015-2021). Their 20 years of work built the foundation that EveLens stands on. This project is a direct continuation of that lineage — same GPL v2 license, same commitment to the EVE community.

- [Original EVEMon Dev Team](https://github.com/evemondevteam/)
- [Peter Han's fork](https://github.com/peterhaneve/evemon)

I'm not accepting donations — I just want to know if EveLens makes your EVE life a little easier. If it does, that's enough for me. Building something people genuinely find useful and actually use is what drives me. Share it with your corp, mention it in a fleet chat, or just drop me a message and tell me what you think. If it works for you, that would mean the world — not ISK. I take donations in kind words. o7

---

## License

GPL v2 — See [LICENSE](src/EveLens.Common/Resources/License/gpl.txt)
