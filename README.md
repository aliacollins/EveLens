# EVEMon

[![GPL licensed](https://img.shields.io/badge/license-GPL%20v2-blue.svg)]()
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)]()
[![ALPHA](https://img.shields.io/badge/branch-ALPHA-red.svg)]()

## Project Status / Fork Note

This repository is **Alia Collins' independent fork** of EVEMon. My goal is to build features that matter and ship them fast - including **DarkMon** (dark mode for EVEMon).

There is also an **established community-maintained fork** here:
https://github.com/mgoeppner/evemon

- If you want the long-running community fork: **use mgoeppner/evemon**
- If you want my fork (building what matters, shipping fast): **use this repo**

**Lineage / credit:** EVEMon is originally by the EVEMonDevTeam and Peter Han, and many community contributors. Full history and attribution are preserved in this repository.

---

## Current Version: 5.1.3-alpha.5

---

## Installation

**Recommended:** Download the installer which automatically installs .NET 8 if needed:
- [EVEMon Installer](https://github.com/aliacollins/evemon/releases/tag/alpha)

**Manual:** Download the portable ZIP and ensure you have:
- Windows 10/11
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## What's New in 5.1.3

### Bug Fixes
- **#14 Virtual-mode ListView crash** — Fixed `InvalidOperationException` when opening assets with 500+ items
- **#15 Font rendering quality** — ClearType rendering for overview panel, Segoe UI for footer/assets
- **#17 60+ character tick cascade** — Reduced event handlers from ~2,760 to ~300 (89% reduction), re-entrancy guard prevents crash

### New Features
- **One-click crash reporting** — Secure diagnostic report pipeline with 8-phase PII sanitizer
- **Redesigned crash dialog** — Prominent Submit Report button, Copy Details, Data Directory access
- **ESI key status indicators** — Color-coded connection status on character overview
- **Simplified blank character creation** — Single-click instead of save-to-XML two-step

### Infrastructure
- Settings migration hardening for imported/blank characters
- UpdateManager null guard for malformed update XML
- Promote script fix for detached HEAD during merge

### Previous (5.1.2) Features

**Modern Framework (.NET 8 Migration)**
- Migrated from .NET Framework 4.8 to .NET 8
- Improved performance, security, and future compatibility
- SDK-style project format for easier maintenance
- Full 64-bit native support

**New Installer**
- One-click installer with automatic .NET 8 runtime download
- Settings backup before upgrade (automatic)
- Fork notice for users migrating from older versions
- Silent install support for auto-updates

**Auto-Update System**
- Seamless background updates
- Automatic app restart after update completes
- Separate alpha/beta/stable update channels
- No manual downloads required

**Performance Improvements**
- Splash screen with loading progress indicators
- Tiered update timers (1s/5s/30s) reduce CPU usage by 60%+
- Event batching reduces UI thrashing during bulk updates
- Virtual ListView handles 5000+ assets smoothly
- Optimized for 100+ character accounts

**User Experience Enhancements**
- Loading indicators during API fetch operations
- Toast notifications for connection status changes
- ESI key warning indicators (expired/invalid tokens)
- Modern About dialog with dark/light theme toggle

**Skill Planning**
- Booster injection simulation (cerebral accelerators)
- Accurate training time with accelerator bonuses

### Bug Fixes

- **Settings not saving** - Fixed revision number detection that caused settings to reset
- **Fork migration** - Seamless migration from peterhaneve and mgoeppner versions
- **Certificate Browser** - Tab hidden (CCP removed certificates from EVE)
- **30+ character crash** - Fixed Hammertime API removal causing crashes
- **Structure lookups** - Proper request deduplication prevents API spam

### Technical Changes

- **JSON settings format** - Auto-migrates from legacy XML, atomic writes prevent corruption
- **Per-character settings files** - Better organization, reduced file size
- **Window title shows version** - Easy identification of which version you're running

---

## Alpha Changelog (Cumulative)

- **Fix #14:** Virtual-mode ListView crash when opening assets with 500+ items
- **Fix #15:** Font rendering quality — ClearType for overview, Segoe UI for footer/assets
- **Fix #17:** 60+ character tick cascade — 89% handler reduction, re-entrancy guard
- **One-click crash reporting** with 8-phase PII sanitizer and webhook pipeline
- **Redesigned crash dialog** with prominent Submit Report button
- **ESI key status indicators** — No API Key, Connecting, Re-auth Required, Error
- **Simplified blank character creation** — single-click instead of save-to-XML
- Hardened settings migration for imported/blank characters
- Guard UpdateManager against null TopicAddress/PatchAddress
- Fix promote.ps1 detached HEAD bug during merge to alpha/beta

---

## Features Being Tested

- **One-click crash reporting** — Submit diagnostic reports directly from the crash dialog
- **ESI key status indicators** — Visual connection status on character overview
- **Performance with 60+ characters** — Reduced event handler overhead
- **Font rendering improvements** — ClearType and modern font stack

---

## Update Channels

| Channel | Use Case | Download |
|---------|----------|----------|
| Stable | Recommended for daily use | [Latest Release](https://github.com/aliacollins/evemon/releases/latest) |
| Beta | Pre-release testing | [Beta Release](https://github.com/aliacollins/evemon/releases/tag/beta) |
| **Alpha** | Experimental features(you are here) | [Alpha Release](https://github.com/aliacollins/evemon/releases/tag/alpha) |

---

## Report Issues

Found a bug? Please report it!

- [GitHub Issues](https://github.com/aliacollins/evemon/issues)

---

## Maintainer

**Alia Collins** (EVE Online) | [CapsuleerKit](https://www.capsuleerkit.com/)

---

## License

GPL v2 - See [LICENSE](src/EVEMon.Common/Resources/License/gpl.txt) for details.

---

## Credits

### Previous Maintainer
**Peter Han** (EVE Online)
- [GitHub (upstream fork)](https://github.com/peterhaneve/evemon)

### Original Creator
**EVEMonDevTeam**
- [GitHub](https://github.com/evemondevteam/)
- [Bitbucket](https://bitbucket.org/EVEMonDevTeam)
- [Website](https://evemondevteam.github.io/evemon/)
- [Documentation](https://evemon.readthedocs.org/)

### Support the Project
I don't accept donations. If you want to support EVEMon, please donate to Peter Han or the original EVEMonDevTeam who built this tool over many years.
