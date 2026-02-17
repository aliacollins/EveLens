# Changelog

All notable changes to EVEMon will be documented in this file.

---

# EVEMon 5.2.0 — Project Phoenix

> A ground-up re-architecture of EVEMon's internals — same app you know, built to last another decade.

Project Phoenix disassembles the monolithic EVEMon.Common into six purpose-built assemblies,
replaces all static event wiring with a centralized EventAggregator, and introduces
dependency injection throughout. Your settings, plans, and characters carry forward unchanged.

Full details and downloads at **https://evemon.dev**

---

## What's New for Users

### Features
- **ESI Scope Selector** — choose exactly what data EVEMon can access. Three presets (Full Monitoring, Standard, Skill Planner Only) or build your own from 15 feature groups. Your choice is saved and respected on every login.
- **Bulk Re-authentication** — after restoring a settings backup, re-auth all your characters in one window instead of clicking through each one. Also available from File → Re-authenticate All Characters.
- **Settings Backup Improvements** — import and export in both JSON and XML formats. Fork migration detection warns you and safely clears stale tokens when restoring backups from other EVEMon forks.
- **ESI Key Status Indicators** — character list now shows connection state: No API Key, Connecting, Re-auth Required, or Error.
- **One-Click Crash Reporting** — redesigned crash dialog with a Submit Report button. Reports are PII-sanitized through an 8-phase pipeline before submission.
- **Simplified Blank Characters** — single-click creation instead of the old save-to-XML workflow.

### Privacy & Security
- ESI scope presets give you granular control over what CCP data EVEMon requests — you no longer have to authorize everything.
- ESI refresh tokens are automatically cleared when restoring settings from backup, preventing stale token reuse.
- Fork migration detection clears ESI keys from imported settings that belong to a different EVEMon fork.
- Crash reports strip file paths, character names, token fragments, and system identifiers before transmission.

### Performance & Stability
- **30+ character support** — SmartQueryScheduler staggers API calls with 4-tier prioritization, preventing the request avalanche that crashed previous versions.
- **60+ character tick cascade fixed** — 89% reduction in per-tick handler invocations with re-entrancy guards.
- **Settings save coalescing** — SmartSettingsManager batches rapid save requests into single writes, eliminating the file-lock contention that caused intermittent save failures.
- **Zero sync-over-async** — every `.Result`, `.Wait()`, and `.GetAwaiter().GetResult()` removed (except Program.cs bootstrap). No more UI thread deadlocks from OAuth callback collisions.
- **Async safety** — all 57 async void event handlers (WinForms requirement) wrapped in try/catch. Unhandled exceptions in background handlers no longer crash the app silently.
- **Font rendering** — ClearType for overview, Segoe UI for footer and assets (fix #15).
- **Asset list crash** — virtual-mode ListView no longer crashes with 500+ items (fix #14).

---

## What Changed Under the Hood

### Architecture

EVEMon.Common was a 200+ file monolith where everything depended on everything.
Project Phoenix splits it into six assemblies with enforced one-way dependencies:

```
EVEMon (WinForms UI)
  └→ EVEMon.Common (services, settings, static facades)
       ├→ EVEMon.Core (interfaces only — zero dependencies)
       ├→ EVEMon.Data (enums, constants, game data)
       ├→ EVEMon.Serialization (ESI / Eve / Settings DTOs)
       ├→ EVEMon.Models (domain models)
       └→ EVEMon.Infrastructure (EventAggregator, services)
```

**Key changes:**
- **EventAggregator replaces static events** — 67 static events on EveMonClient removed. All event delivery now goes through `AppServices.EventAggregator` with `SubscribeOnUI<T>()` for thread-safe UI updates. Subscriptions return `IDisposable` for deterministic cleanup.
- **AppServices facade** — static DI container that lazy-initializes 20+ services. Every service is overridable via `Set*()` methods for testing. `Reset()` provides test isolation.
- **Per-character event scoping** — `SubscribeOnUIForCharacter<T>()` filters events before UI marshaling, eliminating 43+ manual character-match checks scattered across forms.
- **ICharacterServices abstraction** — characters no longer reach into EveMonClient directly. Production code uses `EveMonClientCharacterServices`; tests use `NullCharacterServices`.
- **Settings split** — Settings.cs went from 1,457 lines to 332, with extraction into SettingsLoader, SettingsMigration, and SettingsIO partials. JSON is now the source of truth.
- **CharacterQueryOrchestrator** — replaces CharacterDataQuerying with 4-tier prioritized query scheduling and rate-limit awareness.

### By the Numbers

| Metric | Before (5.1.x) | After (5.2.0) | Change |
|--------|-----------------|---------------|--------|
| Tests | 305 | 970 | +665 (+218%) |
| Test files | ~15 | 51 | +36 |
| Assemblies | 2 | 7 | +5 |
| EveMonClient references | 414 | 65 | -349 (-84%) |
| EveMonClient.Events.cs | 1,639 lines | 108 lines | -93% |
| Static events | 74 | 6 | -92% |
| Service interfaces | ~5 | 22 | +17 |
| Async void try/catch | ~0% | 100% | Complete |
| UI static event subs | 190 | 0 | All via EventAggregator |
| Net code change | — | — | +35,651 / -11,716 lines across 879 files |

### Architectural Laws

These 14 rules are enforced by tests and prevent regression to the old monolith.
Every PR must satisfy all of them.

1. **No Static State** — use AppServices + interfaces, never static mutable fields.
2. **No God Objects** — no class over 500 lines or referenced by more than 30 files.
3. **Dependencies Flow Down** — Core→Data→Serialization→Models→Infrastructure→Common→EVEMon. Never reverse.
4. **New Services Must Be Testable** — constructor injection, interfaces, test doubles.
5. **Events Through EventAggregator Only** — no static events, no `EveMonClient.X += handler`.
6. **Lazy by Default** — collections and expensive objects use `Lazy<T>`, constructors must be fast.
7. **No Sync-Over-Async** — no `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` except bootstrap.
8. **Right File, Right Assembly** — interfaces in Core, enums in Data, DTOs in Serialization, services in Common.
9. **EveMonClient Is Frozen** — never add to it. New services go through AppServices.
10. **All Async Void Must Have Try/Catch** — WinForms event handlers must wrap entire body.
11. **Event Subscriptions Must Be Disposed** — store the `IDisposable`, dispose on form close.
12. **Tests Prove Behavior** — no feature without a test, no bug fix without a regression test.
13. **Serialization DTOs Are the Data Contract** — changes require round-trip tests.
14. **No Direct EveMonClient Access from UI** — use `AppServices.*` properties.

---

## CI/CD & Developer Experience
- **GitHub Actions pipeline** — automated build + full test suite on every push and PR to main/alpha/beta. Fails if test count drops below 900.
- **PR template** — every pull request includes an Architectural Laws checklist.
- **Promotion system** — `promote.ps1` handles version bumps, changelog updates, and branch merges for alpha/beta/stable. Transactional with rollback on failure.
- **CLAUDE.md** — comprehensive architectural guide with end-to-end feature addition walkthrough, replacing the outdated DEVELOPER.md.
- **Assembly READMEs** — each of the 6 assemblies has a README explaining purpose, dependencies, and key files.

---

## What Was Removed
- **67 static events** and **68 dead OnXxx() bridge methods** from EveMonClient.Events.cs (-1,531 lines)
- **CharacterDataQuerying / CorporationDataQuerying / CentralQueryScheduler** — replaced by CharacterQueryOrchestrator
- **FeatureFlags.cs** — all flags were permanently true, scaffolding removed
- **Certificate Browser tab** — hidden (CCP removed certificates from EVE Online)
- **Legacy test projects** — Tests.EVEMon.Common, Tests.EVEMon, Tests.Helpers replaced by unified EVEMon.Tests
- **Dead tooling** — MSBuildVersioning DLL, ReSharper code style files, Balsamiq design mockups
- **DEVELOPER.md** — superseded by CLAUDE.md
- **16 obsolete methods** across the codebase

---

## Bug Fixes (from 5.1.3-alpha)
- Fix #14: Virtual-mode ListView crash when opening assets with 500+ items
- Fix #15: Font rendering quality — ClearType for overview, Segoe UI for footer/assets
- Fix #17: 60+ character tick cascade — 89% handler reduction, re-entrancy guard
- Fix promote.ps1 detached HEAD bug during merge to alpha/beta
- Fix settings migration for imported/blank characters
- Guard UpdateManager against null TopicAddress/PatchAddress
- Fix promotion pipeline: transactional phases, CRLF-safe regexes, README validation

---

## What's Next
- Migrate remaining 65 `EveMonClient.*` references through AppServices
- Migrate ~637 `Settings.*` static references (mostly `Settings.UI.*` in the UI layer)
- Move forms from `AppServices.*` static access to constructor injection
- Complete Certificates and Masteries data generators in XmlGenerator
- Migrate XmlGenerator's `WebRequest.CreateHttp` to `HttpClient`
- SDE update automation script (download → extract → convert → generate in one step)

---

## Links

- **Website:** https://evemon.dev
- **GitHub:** https://github.com/aliacollins/evemon
- **Forums:** https://forums.eveonline.com/t/evemon-lives-new-maintainer-v5-1-0-released/504429

---

## Previous Releases

## [5.1.2] - 2026-02-02
- First stable release with .NET 8 migration, auto-update, performance improvements
