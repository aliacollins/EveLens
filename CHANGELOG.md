# Changelog

All notable changes to EveLens will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0-beta.1] - 2026-03-24

### Added

- **Skill level breakdown** in the Skills tab — filter buttons let you instantly see how many skills you have at each level (V, IV, III, II, I) and switch between All Skills, All Trained, or Injected ([#33])
- **Attribute filter** in the Plan Editor — filter the skill list by primary/secondary attribute combo (e.g. Intelligence/Memory) to plan around your current remap. Your active remap is auto-detected and marked with a ★ ([#38])
- **Total SP** (trained + unallocated) now displayed in the character header stats line

### Fixed

- Plan Editor no longer shows unpublished skills like CFO Training or Chief Science Officer ([#37])
- Scrolling now works correctly in the Plan Editor's Skills and Blueprints tabs ([#39])

## [1.0.0] - 2026-03-23

### Added

- **Auto-updates** via Velopack with delta downloads across Windows, Linux, and macOS
- **Windows code signing** — eliminates SmartScreen warnings and false-positive antivirus detections
- "Check for Updates" in the Help menu with release notes in the update dialog

### Changed

- Update system completely replaced — Velopack handles all packaging and delivery
- Build and release pipeline moved to GitHub Actions

## [1.0.0-beta.2] - 2026-03-19

### Added

- **ESI health tracking** — smart per-endpoint health states replace noisy error notifications. You'll see one clear message when something breaks, and a recovery message when it's fixed — no more walls of error spam ([#34])
- **Health indicators** on the character overview — green (healthy), yellow (degraded), red (failing)
- **Live diagnostic viewer** in the Debug menu — real-time log with filters for ESI, events, warnings, and scheduler activity
- **SDE update to Catalyst expansion** (March 18, 2026) — 5 new skills, 82 new item types, carrier/fighter/FAX/Black Ops balance changes

### Fixed

- ~100 ESI error entries flooding the activity log during brief connectivity issues ([#34])
- "19 hours until next refresh" showing stale times when error cache expired
- Debug builds now use a separate data folder to avoid contaminating production settings

## [1.0.0-beta.1] - 2026-03-16

### Added

- Window position and size remembered across restarts, including multi-monitor setups

## [1.0.0-alpha.1] - 2026-02-25

### Added

- **Cross-platform support** — Windows x64, Linux x64, macOS Apple Silicon
- **Modern dark UI** built on Avalonia, replacing the legacy WinForms interface
- **Smart ESI scheduler** with priority queue, per-character rate limiting, and phased cold start
- **19 character tabs** — Skills, Assets, Market Orders, Contracts, Mail, Industry Jobs, Wallet, Notifications, Kill Log, Planetary, and more
- **Plan Editor** with skill browser, training time calculator, and attribute optimizer
- **Settings migration** — existing EVEMon settings imported automatically on first launch
- **TCP diagnostic stream** on port 5555 for real-time structured debugging

### Changed

- Complete rewrite from monolithic EVEMon to modular EveLens architecture
- Rebranded from EVEMon to EveLens — Character Intelligence for EVE Online

[#33]: https://github.com/aliacollins/EveLens/discussions/33
[#34]: https://github.com/aliacollins/EveLens/issues/34
[#37]: https://github.com/aliacollins/EveLens/issues/37
[#38]: https://github.com/aliacollins/EveLens/issues/38
[#39]: https://github.com/aliacollins/EveLens/issues/39
[unreleased]: https://github.com/aliacollins/evelens/compare/v1.1.0-beta.1...HEAD
[1.1.0-beta.1]: https://github.com/aliacollins/evelens/compare/v1.0.0...v1.1.0-beta.1
[1.0.0]: https://github.com/aliacollins/evelens/compare/v1.0.0-beta.2...v1.0.0
[1.0.0-beta.2]: https://github.com/aliacollins/evelens/compare/v1.0.0-beta.1...v1.0.0-beta.2
[1.0.0-beta.1]: https://github.com/aliacollins/evelens/compare/v1.0.0-alpha.1...v1.0.0-beta.1
[1.0.0-alpha.1]: https://github.com/aliacollins/evelens/releases/tag/v1.0.0-alpha.1
