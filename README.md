# EVEMon

[![GPL licensed](https://img.shields.io/badge/license-GPL%20v2-blue.svg)]()
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)]()
[![STABLE](https://img.shields.io/badge/branch-STABLE-green.svg)]()

## Project Status / Fork Note

This repository is **Alia Collins' independent fork** of EVEMon. My goal is to build features that matter and ship them fast - including **DarkMon** (dark mode for EVEMon).

There is also an **established community-maintained fork** here:
https://github.com/mgoeppner/evemon

- If you want the long-running community fork: **use mgoeppner/evemon**
- If you want my fork (building what matters, shipping fast): **use this repo**

**Lineage / credit:** EVEMon is originally by the EVEMonDevTeam and Peter Han, and many community contributors. Full history and attribution are preserved in this repository.

---

## Current Version: 5.1.2-alpha.11

---

## Installation

**Recommended:** Download the installer which automatically installs .NET 8 if needed:
- [EVEMon Installer](https://github.com/aliacollins/evemon/releases/tag/v5.1.2)

**Manual:** Download the portable ZIP and ensure you have:
- Windows 10/11
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## What's New in 5.1.2

### alpha.11 - Credential Encryption
- **Security**: ESI refresh tokens now encrypted with Windows DPAPI
- Tokens locked to your Windows user account — can't be stolen
- Backup files contain encrypted tokens (useless on other machines)
- Automatic migration from plaintext tokens on first run
- Re-authentication required after restoring backup on new computer

### alpha.10 - Version Correction
- Fixed version numbering from 5.2.0 to 5.1.2
- Cumulative changelog in README and release notes

### Major Features

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

## Update Channels

| Channel | Use Case | Download |
|---------|----------|----------|
| **Stable** | Recommended for daily use (you are here) | [Latest Release](https://github.com/aliacollins/evemon/releases/latest) |
| Beta | Pre-release testing | [Beta Release](https://github.com/aliacollins/evemon/releases/tag/beta) |
| Alpha | Experimental features | [Alpha Release](https://github.com/aliacollins/evemon/releases/tag/alpha) |

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
