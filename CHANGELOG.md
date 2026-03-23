# Changelog

All notable changes to EveLens will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Velopack auto-update system with delta updates across Windows, Linux, and macOS
- GitHub Actions CI/CD pipeline — automated builds, tests, and releases on push
- GitVersion for automatic semantic versioning from git branch topology
- Channel-based update intervals: alpha (1h), beta (3h), stable (6h)
- Release notes displayed in the update dialog (from GitHub Release body)
- Force "Check for Updates" in Help menu with download + restart flow
- **Windows code signing** with Certum Open Source Developer certificate — eliminates SmartScreen/Trojan false positives
- `sign-release.ps1` — local script to build, sign, and upload Windows release artifacts
- Assembly version stamping in CI for all platforms (SharedAssemblyInfo.cs updated before build)

### Changed

- Update system completely replaced — Velopack replaces custom AutoUpdateService, Inno Setup, AppImage scripts, and macOS bundle scripts
- CI/CD completely replaced — GitHub Actions replaces promote.ps1 and release-*.ps1 scripts
- Git security lock removed — GitHub branch protection rules enforce protected branches
- GitVersion config updated for v6.x — ContinuousDelivery mode with per-branch labels

### Removed

- `AutoUpdateService.cs` — replaced by Velopack
- `UpdateNotifyWindow` and `DataUpdateNotifyWindow` — replaced by inline update dialog
- `release-alpha.ps1`, `release-beta.ps1`, `release-stable.ps1` — replaced by GitHub Actions
- `build-installer.ps1`, `build-appimage.sh`, `build-macapp.sh` — replaced by Velopack packaging
- `git-lock.sh`, `git-unlock.sh` — replaced by GitHub branch protection
- `evelens-patch-*.xml` update feeds — replaced by GitHub Releases API

## [1.0.0-beta.2] - 2026-03-19

### Added

- **ESI Health State Machine** — per-(character, endpoint) state machine replaces event-based error notifications. States: Healthy, Degraded, Failing, Suspended. Fires only on transitions — no more error spam. (Issue #34)
- **EndpointHealthTracker** with dynamic rolling time window that self-tunes from ESI cache headers (fast endpoints = short window, slow endpoints = long window)
- **Hysteresis recovery** — 3 consecutive successes required to transition back to Healthy, preventing the error flapping that caused ~100 activity log entries
- **`ISchedulerStatus.GetNextFetchTime()`** — reads directly from the scheduler's priority queue, fixing the "19 hours until next refresh" stale display bug
- **HealthNotificationSubscriber** — bridges state transitions to activity log: Failing = one entry, Suspended = one entry, recovery = auto-clear, Degraded = silence
- **Traffic-light health dots** on character overview: green (healthy), yellow (degraded/fetching), red (failing/suspended)
- **42 new tests** for EndpointHealthTracker covering all transitions, hysteresis, dynamic windows, edge cases
- **In-app Diagnostic Stream viewer** — Debug menu opens a live log window with filters (All/ESI/Events/Warnings/Health/Scheduler), auto-scroll, 2000-line buffer
- **Debug build isolation** — debug builds use `%APPDATA%\EveLens Debug\` to prevent cross-contamination with production data
- **`update-sde.ps1`** — automated SDE update pipeline: download, extract, YamlToSqlite, XmlGenerator, diff report, version stamp
- **Branching policy** in CLAUDE.md — all work on feature/fix/experimental branches from alpha

### Changed

- **SDE updated to CCP build 3261822** (Catalyst expansion, March 18 2026):
  - 5 new skills: Capital Disintegrator Specialization, Amarr/Caldari/Gallente/Minmatar Fighter Specialization
  - 82 new types, 31 modified types, 127 typeDogma changes
  - All balance changes: carrier cargo bays, fighter stats (squadron 9→6, HP/DPS +50%), FAX cap booster bonuses, Black Ops tank nerfs, SOCT damage nerfs, recon targeting range -15%
- Status bar countdown reads from scheduler queue instead of stale QueryMonitor reconstruction
- CharacterOverviewView uses CharacterHealthSummary instead of QueryMonitors.HasErrors
- TcpJsonLoggerProvider refactored with Start/Stop control and OnLogLine callback for in-process subscribers
- Scheduler-driven ESI endpoints disconnected from ShouldNotifyError (8 non-scheduler callers kept for backward compat)

### Fixed

- ~100 ESI error entries accumulating in activity log during intermittent connectivity (Issue #34)
- "19 hours until next refresh" stale display when error cache expires before scheduler's next fetch
- Error notification flapping caused by ShouldNotifyError gate resetting on every success
- XmlGenerator NuGet version conflict (System.Configuration.ConfigurationManager 8.0.0 → 8.0.1)
- promote.ps1 version counter resetting on cross-channel promotions (array-join bug in PowerShell)
- promote.ps1 not fetching remote ref before reading target branch version

## [1.0.0-beta.1] - 2026-03-16

### Added

- Window position save/restore across restarts and multi-monitor setups

### Fixed

- promote.ps1 cross-branch merge strategy for alpha-to-beta promotions

## [1.0.0-alpha.44] - 2026-03-15

### Added

- Smarter ESI error notifications: 3-consecutive-failure suppression for transient errors
- Immediate notification for auth failures (401/403) and not-found (404)
- Error categorization labels: Token refresh, Connection error, Auth expired, Not found, Rate limited, ESI server error
- 29 new tests in ErrorNotificationTests.cs

## [1.0.0-alpha.1] - 2026-02-25

### Added

- Cross-platform support: Windows x64, Linux x64, macOS Apple Silicon
- Avalonia UI replacing WinForms — dark theme, modern controls
- ESI Scheduler with priority queue, per-character rate limiting, phased cold start
- Resilience pipeline: CircuitBreakerPolicy, CharacterAlivePolicy, RetryPolicy
- EventAggregator replacing all static events
- 19 character sub-tab views (Skills, Assets, Mail, Contracts, etc.)
- MVVM ViewModels for all list views with filter/sort/group pipeline
- Plan editor with skill browser and attribute optimizer
- Settings migration from EVEMon (`%APPDATA%\EVEMon` → `%APPDATA%\EveLens`)
- Auto-update checking via patch XML feeds
- TCP diagnostic stream on port 5555 for structured JSON-lines debugging
- 1741 tests covering architecture, models, services, serialization, integration

### Changed

- Complete architectural rewrite from monolithic EVEMon to modular EveLens
- Assembly hierarchy: Core → Data → Serialization → Models → Infrastructure → Common → EveLens
- Brand identity: EVEMon → EveLens (Character Intelligence for EVE Online)

[unreleased]: https://github.com/aliacollins/evelens/compare/v1.0.0-beta.2...HEAD
[1.0.0-beta.2]: https://github.com/aliacollins/evelens/compare/v1.0.0-beta.1...v1.0.0-beta.2
[1.0.0-beta.1]: https://github.com/aliacollins/evelens/compare/v1.0.0-alpha.44...v1.0.0-beta.1
[1.0.0-alpha.44]: https://github.com/aliacollins/evelens/compare/v1.0.0-alpha.1...v1.0.0-alpha.44
[1.0.0-alpha.1]: https://github.com/aliacollins/evelens/releases/tag/v1.0.0-alpha.1
