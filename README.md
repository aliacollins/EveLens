# EveLens

**Character Intelligence for EVE Online**

[![GPL licensed](https://img.shields.io/badge/license-GPL%20v2-blue.svg)]()
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)]()
[![Cross-Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-brightgreen.svg)]()
[![ALPHA](https://img.shields.io/badge/channel-ALPHA-red.svg)]()

---

## What Is EveLens?

EveLens is a character monitoring and skill planning tool for EVE Online. It runs on **Windows, Linux, and macOS** with a modern dark UI built on Avalonia.

**Current Version: 1.0.0-alpha.1**

---

## Downloads

| Platform | Download | Requirements |
|----------|----------|-------------|
| **Windows (Installer)** | [EveLens-install.exe](https://github.com/aliacollins/evelens/releases/tag/alpha) | Installs .NET 8 automatically |
| **Windows (Portable)** | [ZIP](https://github.com/aliacollins/evelens/releases/tag/alpha) | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Linux x64** | [ZIP](https://github.com/aliacollins/evelens/releases/tag/alpha) | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **macOS Apple Silicon** | [ZIP](https://github.com/aliacollins/evelens/releases/tag/alpha) | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |

**Linux/macOS:** Extract the ZIP, then run:
```bash
dotnet "EveLens.dll"
```

---

## Features

### Cross-Platform
EveLens runs natively on Windows, Linux, and macOS from a single codebase. System tray, notifications, clipboard, and dialogs all use native platform APIs. Same features, same UI, same updates on every platform.

### 6 Dark Themes
Six color palettes inspired by New Eden's empires. Every control respects the active theme.

- **Dark Space** (default) — navy + gold
- **Caldari Blue** — cool steel blues
- **Amarr Gold** — warm golds and ambers
- **Minmatar Rust** — earthy oranges
- **Gallente Green** — teals and greens
- **Midnight** — deep purples

### ESI Scope Control
You choose what data EveLens can access. Three presets (Full, Standard, Skill Planner Only) or custom selection from 16 feature categories. When you re-authenticate with fewer scopes, EveLens automatically detects revoked permissions, clears stale data, and deletes disk caches for those endpoints.

### Skill Constellation
GPU-accelerated interactive visualization of EVE's ~400-skill tree rendered as a star constellation. Skills as stars, prerequisites as connecting lines, skill groups as colored nebula clusters. Zoom, pan, search, click-to-inspect. Trained skills glow, training skills pulse.

### Character Monitoring
Full character monitoring across 20+ data tabs: Skills, Skill Queue, Assets, Market Orders, Contracts, Industry Jobs, Wallet Journal, Wallet Transactions, Mail, Notifications, Kill Log, Planetary Industry, Research Points, Standings, Contacts, Employment History, Medals, Loyalty Points, Factional Warfare.

### Skill Planning
Multi-tab plan editor with integrated Skills, Ships, Items, and Blueprint browsers. Entry detail panel with prerequisite tracking and completion checkmarks. Inline duplicate name prevention.

### More
- **Native OS notifications** — skill completions via system notification center
- **Activity center** — bell icon with 200-entry event log
- **Character grouping** — organize characters by purpose
- **Balance change indicator** — real-time ISK delta with directional arrows
- **Data cache** — near-instant tab loading, limited offline viewing
- **Per-character endpoint manager** — toggle individual ESI endpoints on/off
- **Auto-update** — background update checking with alpha/beta/stable channels

---

## Alpha Changelog (Cumulative)

- Cross-platform: Windows, Linux, macOS from single codebase
- 6 EVE-faction dark themes with runtime switching
- ESI scope selector with dynamic revocation handling
- Skill constellation GPU-accelerated visualization
- Native OS notifications (Windows/Linux/macOS)
- Activity/notification center with event history
- Character grouping and balance change tracking
- Character data cache with offline support
- Redesigned plan editor with multi-tab browsers
- Per-character endpoint manager
- Settings redesign: single scrollable page
- About window with 57+ contributor history
- WinForms removed — Avalonia-only, net8.0 target
- ImageService migrated from System.Drawing to SkiaSharp
- Outlook calendar integration removed (Google Calendar remains)
- 1,511 tests passing, 14 architectural laws enforced

---

## Features Being Tested

- Cross-platform stability on Linux and macOS
- Theme switching across all views
- ESI scope revocation and cache invalidation
- Skill constellation performance with large skill sets
- Native notifications on all three platforms

---

## Update Channels

| Channel | Use Case | Download |
|---------|----------|----------|
| Stable | Recommended for daily use | [Latest Release](https://github.com/aliacollins/evelens/releases/latest) |
| Beta | Pre-release testing | [Beta Release](https://github.com/aliacollins/evelens/releases/tag/beta) |
| **Alpha** (you are here) | Experimental features | [Alpha Release](https://github.com/aliacollins/evelens/releases/tag/alpha) |

---

## Report Issues

Found a bug? Please report it: [GitHub Issues](https://github.com/aliacollins/evelens/issues)

---

## Maintainer

**Alia Collins** (EVE Online) | [CapsuleerKit](https://www.capsuleerkit.com/)

---

## Credits

EVEMon was originally created by the **EVEMon Dev Team** (Jimi Charalampidis and 57+ contributors, 2006-2015) and maintained by **Peter Han** (2015-2021). Their work over 20 years built the foundation that EveLens stands on.

- [Original EVEMon Dev Team](https://github.com/evemondevteam/)
- [Peter Han's fork](https://github.com/peterhaneve/evemon)

I don't accept donations. If you want to support EveLens, please donate to Peter Han or the original EVEMon Dev Team.

---

## License

GPL v2 — See [LICENSE](src/EveLens.Common/Resources/License/gpl.txt)
