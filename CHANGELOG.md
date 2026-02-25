# Changelog — EVEMon NexT

---

## IMPORTANT: Legacy EVEMon Is Retired

**EVEMon NexT v1.0.0 replaces all previous EVEMon versions (5.x and earlier).**

The legacy WinForms application has been fully retired. This repository now contains only EVEMon NexT — a cross-platform Avalonia application targeting .NET 8 on **Windows, Linux, and macOS**.

If you are running EVEMon 5.1.x or earlier:
- Your settings and skill plans carry forward automatically
- ESI tokens will need re-authentication (OAuth tokens are not transferable)
- The WinForms UI is gone — EVEMon NexT uses Avalonia

**There will be no further updates to the legacy 5.x line.**

---

## [1.0.0-alpha.1] - 2026-02-25

### Cross-Platform — Windows, Linux, macOS

EVEMon is no longer Windows-only. NexT runs natively on all three desktop platforms from a single codebase, powered by Avalonia UI and SkiaSharp rendering.

| Platform | Download |
|----------|----------|
| **Windows x64** | Installer (.exe) or portable ZIP |
| **Linux x64** | Portable ZIP — run with `dotnet "EVEMon NexT.dll"` |
| **macOS Apple Silicon** | Portable ZIP — run with `dotnet "EVEMon NexT.dll"` |

All platforms get the same features, the same UI, and the same updates. System tray, notifications, clipboard, and dialogs all use native platform APIs.

---

### 6 EVE-Faction Dark Themes

EVEMon NexT ships with six color palettes inspired by New Eden's empires. Every pixel — buttons, tabs, scrollbars, data grids, cards, expanders — respects the active theme.

- **Dark Space** (default) — EVE's iconic navy + gold
- **Caldari Blue** — Cool steel blues
- **Amarr Gold** — Warm golds and ambers
- **Minmatar Rust** — Earthy oranges and rust
- **Gallente Green** — Teals and greens
- **Midnight** — Deep purples, near-black

Settings -> Appearance -> Theme. Takes effect on restart.

---

### ESI Scope Selector — You Control Your Data

The legacy EVEMon requested every possible API permission with no way to limit it. NexT gives you granular control over what CCP data EVEMon can access.

**Three presets:**
- **Full Monitoring** — all features enabled
- **Standard Monitoring** — skills, wallet, assets, market, industry, contracts (excludes mail, notifications, planetary, kills, corp data)
- **Skill Planner Only** — minimal: skills, clones, implants

**Custom mode:** Open the Scope Editor to toggle 16 individual feature categories. Skills & Training Queue is mandatory; everything else is optional.

**Dynamic scope management:** When you re-authenticate with fewer scopes than before, NexT automatically:
- Detects which scopes were revoked
- Clears in-memory data for those endpoints
- Deletes disk cache files for revoked data
- Shows "ESI scope not authorized" in affected tabs instead of stale data

---

### Skill Constellation — Interactive Skill Graph

A GPU-accelerated visualization that renders EVE's entire ~400-skill tree as a star constellation.

- **Skills as stars** — positioned by skill group, connected by prerequisite lines
- **Nebula clusters** — color-coded skill groups form visual regions
- **Zoom and pan** — mouse wheel to zoom, click-drag to navigate
- **Search** — type to highlight matching skills with dropdown
- **Click to inspect** — detail panel shows level pips, training status, prerequisites with completion checkmarks, rank, group
- **Group chips** — colored chips at bottom show group statistics, click to highlight entire group
- **Training animations** — trained skills glow, actively training skills pulse
- **Toggle labels** — show/hide skill names to reduce clutter

This turns skill planning from reading spreadsheets into exploring a map.

---

### More New Features

- **Native OS Notifications** — toast notifications through the system notification center (Windows WinRT, Linux libnotify, macOS osascript). Get skill completion alerts even when EVEMon is minimized.
- **Activity/Notification Center** — bell icon in status bar with scrollable event log. ESI fetches, skill completions, errors — 200-entry history with unread badge.
- **Character Grouping** — named groups ("PVP Alts", "Industry Toons") to organize the Overview card grid.
- **Balance Change Indicator** — real-time ISK delta with green/red directional arrows, compact T/B/M/K notation, 15-second auto-clear.
- **Character Data Cache** — every ESI response cached to local JSON. Near-instant tab loading, limited offline viewing, atomic writes, scope-aware invalidation.
- **Plan Editor** — multi-tab interface (Plan + Skills + Ships + Items + Blueprints browser), entry detail panel with prerequisites, inline duplicate name prevention.
- **Per-Character Endpoint Manager** — gear button per character tab to toggle individual ESI endpoints on/off. Disable what you don't need.
- **Settings Redesign** — single scrollable page with sidebar navigation replacing scattered dialogs.
- **About Window** — 3-column layout with full 57+ contributor history from EVEMon's 20-year life.
- **Diagnostic Stream** — TCP JSON-lines on port 5555 for real-time developer monitoring.

---

### Architecture — Project Phoenix

Project Phoenix (February 13-22, 2026) rebuilt EVEMon's internals in 10 days:

- **6-assembly split** — monolithic EVEMon.Common (875 files) decomposed into Core, Data, Serialization, Models, Infrastructure, Common with enforced dependency boundaries
- **EventAggregator** — replaced 74 static events with typed pub/sub. No more memory leaks from unsubscribed handlers.
- **59 ViewModels** — built from zero. Business logic in shared ViewModels, not UI controls.
- **ESI Scheduler rewrite** — priority-queue background scheduler. Per-character rate limiting, phased cold start. Character #100 no longer waits 500 seconds.
- **1,511 tests** — up from 23 in the original. 14 architectural laws enforced by automated tests.
- **SkiaSharp imaging** — all image loading cross-platform via SkiaSharp, replacing deprecated System.Drawing.
- **File-based instance lock** — cross-platform single-instance detection replacing Windows-only named Semaphore.

---

### What Was Removed

- **WinForms UI** — 6 projects, ~136,000 lines deleted (EVEMon, PieChart, Watchdog, LogitechG15, WindowsApi, Sales)
- **Outlook Calendar** — Windows-only COM Interop removed. Google Calendar remains.
- **Certificate Browser** — CCP removed certificates from EVE Online
- **67 static events** and 68 dead bridge methods from EveMonClient

---

### Links

- **GitHub:** https://github.com/aliacollins/evemon
- **Issues:** https://github.com/aliacollins/evemon/issues

---

## Previous Releases (Legacy — Retired)

### [5.1.2] - 2026-02-02
- First stable release with .NET 8 migration, auto-update, performance improvements

### [5.1.0] - 2026-01-15
- New maintainer release

### [5.0.1] - 2026-01-04
- Resurrection from 4-year dormancy, .NET 8 migration
