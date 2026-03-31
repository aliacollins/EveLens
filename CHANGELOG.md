# Changelog

All notable changes to EveLens will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Drag-to-reorder in Plan Editor** -- grab the grip handle to reorder skills in your training plan. Multi-select with shift/ctrl+click, drag as a group. Prerequisite constraints enforced in real-time (blue indicator = valid, red = blocked). Toast notifications on success/failure. Ghost placeholder shows original position during drag. Press animation with scale transition for tactile feedback
- **Chain ribbons** -- colored left-edge strips visually group related skills by training goal. Colors are stable (deterministic from goal skill ID). Chain position drives ribbon corner radius (first/mid/last/solo)
- **Timeline minimap** -- proportional colored bar at the top of the plan showing time distribution by chain. Legend chips show chain names
- **Goal inference engine** -- automatically detects training goals (leaf skills with no in-plan dependents) and assigns prerequisite chains. Shared prerequisites resolved by first-claimer rule
- **Skill Farm Dashboard** -- full economics dashboard for skill extraction characters. ESI Jita pricing for PLEX and extractors, per-character tax from Accounting skill, extraction readiness tracking, monthly profit projections, and Omega sustainability analysis. Privacy mode support hides character names for streaming/screenshots
- **Recent Plans menu** -- the Plans menu now shows the 5 most recently opened plans per character with training time, for quick access without going through Manage Plans
- **Skill detail sidebar** -- double-click any skill in the Plan Editor to see description, unlocked skills, enabled items (with icons), and plan-to actions in the right panel. Click unlocked skills to drill into the prerequisite tree
- **Skill browser filters** -- four filter modes in the Plan Editor's Skills tab: All Skills, Trained, Have Prerequisites, and Untrained
- **Keyboard shortcuts** -- Ctrl+Q (quit), Ctrl+W (close plan window), Ctrl+Shift+W (close all child windows), Ctrl+N (new plan), Ctrl+M (manage plans), Ctrl+, (settings). All shown in menus and Help > Keyboard Shortcuts dialog with OS-specific labels
- **Reverse skill/item lookups** -- new StaticSkills.GetDependentSkills() and GetItemsRequiringSkill() for browsing what a skill unlocks
- **Plan activity tracking** -- plans now track when they were last opened via LastActivity timestamp, persisted across sessions
- **Queue health on overview cards** -- theme-aware card tints across all 6 palettes show queue status at a glance. Status dots with labels: green (>5 days), yellow (<5 days), red (<24 hours), dark red (empty), gray (paused). Click a status dot to navigate to that character

### Changed

- **macOS install instructions** -- simplified to xattr-only method since right-click Open and Privacy Settings don't work with unsigned apps (Gatekeeper reports "broken" not "unsigned")
- **Skill browser attribute filter** defaults to "All Attributes" instead of auto-selecting the detected remap
- **Plan Editor sidebar** widened to 320px for better content layout
- **Gmail-style mail view** -- split view with mail list on left, reading pane on right. Click a mail to read inline instead of opening a separate window. Body auto-loads when ESI finishes downloading. Flat list sorted newest first
- **Employment history list view** -- toggle between horizontal Timeline and vertical List view. Card-row design with corp logos, date range, duration badge, and "Current" indicator. View preference saved across sessions
- **Skill level breakdown tooltip** -- hover the stats line in the character header to see skill counts at each level (V:3 IV:5 III:8 etc.)

### Fixed

- **Market transaction item names** -- item names were blank because the ESI→model layer never resolved TypeID to TypeName (Phoenix refactoring regression). Now falls back to StaticItems lookup
- **Wallet journal "Undefined"** -- new ESI ref types not in the 2018 RefTypes.xml mapping showed as "Undefined". Now preserves the raw ESI string and humanizes it (e.g. "player_trading" → "Player Trading")
- **Unicode ship names** -- ship names with non-ASCII characters (e.g. ♪ ♥ ♪) were displayed as literal \uNNNN escape sequences instead of rendered glyphs. All JSON serialization paths now preserve unicode as-is
- **App hangs on quit with child windows open** -- closing the app while a Plan Editor or other child window was open caused the process to hang and become a zombie (macOS). Child windows are now tracked and closed before shutdown
- **Plan window blocks main window** -- child windows no longer force themselves above the main window on macOS. All windows are independent and freely switchable via Alt+Tab / Cmd+`
- **New Plan dialog keyboard focus ([#50])** -- TextBox now receives focus immediately on open. Typing replaces the default "Plan N" name without needing to click first

### Removed

- **Queue health flyout** -- the clock icon and flyout in the status bar have been replaced by the overview card tints and status dots, which are more scalable and visible

## [1.1.0] - 2026-03-29

### Added

- **Character Skill Comparison** -- compare up to 10 characters side-by-side with theme-aware level blocks, differences-only toggle, and auto-sizing columns ([#45])
- **Variable font scaling** -- a Font Size slider in Settings > Appearance scales all text from 80% to 150%. Every font in the app (895 values across 71 files) now uses a 7-tier type scale derived from a single base size. Changes apply live as you drag the slider and persist across sessions. Architecture tests prevent hardcoded font sizes from creeping back in
- **Untrained filter** -- new filter button in the Skills tab shows skills not yet injected ([#33])
- **Queue health monitor** -- a clock icon in the status bar shows how many character queues need attention. Click it to see all characters sorted by urgency with countdown timers and end dates. Click any character to jump straight to their Queue tab ([#43])
- **Queue end date** in the Queue tab -- the status bar now shows when the queue finishes and a countdown timer so you know exactly when to refresh your training plan
- **Add Character card** -- a ghost card in the character overview lets you add new characters without navigating menus. The portrait strip also has a `+` button for quick access ([#41])
- **Add Another flow** -- after adding a character via SSO, you can immediately add another without reopening the dialog. Characters are auto-imported on successful login, no extra confirmation step needed
- **Group and character reorder** -- click a group chip to expand a member reorder panel with ▲ ▼ buttons. ◀ ▶ moves groups left/right to change their display order in the Overview and portrait strip ([#42])
- **Group dividers in portrait strip** -- visible separator lines between groups for clearer visual separation ([#42])
- **Help text in Manage Groups** -- guidance text explains how to assign characters, reorder members, and manage groups ([#42])

### Changed

- **Manage Character Groups** completely redesigned -- tag-based UI shows each character with colored group tags. Click `+ Assign` to pick a group from a radio-button flyout. Groups are managed inline with rename and delete icons ([#42])
- **Group colors in portrait strip** -- characters are ordered by group with colored accent bars under their portraits ([#42])
- **Blueprint browser** uses the same hierarchical tree as Ships and Items -- no more duplicate "Amarr" entries. The full market group path is preserved with a "Can Build Only" filter ([#39])
- **Consistent skill counts** -- unpublished skills are now filtered uniformly across the Skills tab, Plan Editor, and Character Comparison ([#37], [#33])

### Fixed

- **Queue Health now shows all characters** -- previously only "monitored" characters (a legacy EVEMon concept with no UI toggle) appeared in the Queue Health flyout and badge. Characters migrated from old EVEMon settings could become invisible ghosts. All characters are now guaranteed to be monitored on import ([#47])
- **Queue Health flyout scrolls** -- added ScrollViewer with max height to prevent the flyout from overflowing off-screen with many characters
- **Full character names in Comparison** -- column headers and portraits now show full names instead of first name only ([#45])
- **Live font scaling** -- code-behind windows (Manage Groups, Comparison, Skills, Overview, dialogs) now rebuild on font scale change instead of showing stale sizes ([#42])
- **ESI token race condition** -- requests no longer fire with expired tokens. Tokens refresh proactively 100 seconds before expiry, and a pre-flight check blocks any request when the token is expired or refreshing. This prevents the error budget depletion that caused the scheduler to back off for 20+ hours with 30+ characters ([#34])
- **401 vs 403 distinction** -- expired tokens (401) are now treated as transient and don't trigger "re-authentication required" notifications. Only permanent auth failures like revoked scopes (403) trigger that message. The scheduler re-enqueues 401s after 15 seconds instead of suspending all jobs ([#34])
- **Startup token refresh** -- all ESI tokens are refreshed during the splash screen before the scheduler starts dispatching, preventing the burst of 401s that occurred on app launch ([#34])
- **TextBox auto-focus** -- dialog text inputs now auto-focus on open across all dialogs: Create Blank Character, Manage Groups, Manage Plans, Implant Sets, and Skill Constellation search ([#42])
- **macOS .app bundle** -- the app was not recognised by macOS because executable permissions were lost during packaging. Now built via WSL with proper Unix permissions

### Removed

- **Google Analytics tracker** -- removed dead code that hashed MAC addresses for fingerprinting. Never had callers, never had consent ([#40])
- **In-game browser server** -- removed legacy IGB HTTP server (5 files) that could bind port 80. CCP retired the IGB years ago ([#40])

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
[#40]: https://github.com/aliacollins/EveLens/issues/40
[#41]: https://github.com/aliacollins/EveLens/issues/41
[#42]: https://github.com/aliacollins/EveLens/issues/42
[#43]: https://github.com/aliacollins/EveLens/issues/43
[#45]: https://github.com/aliacollins/EveLens/issues/45
[#47]: https://github.com/aliacollins/EveLens/issues/47
[#50]: https://github.com/aliacollins/EveLens/issues/50
[unreleased]: https://github.com/aliacollins/evelens/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/aliacollins/evelens/compare/v1.1.0-beta.1...v1.1.0
[1.1.0-beta.1]: https://github.com/aliacollins/evelens/compare/v1.0.0...v1.1.0-beta.1
[1.0.0]: https://github.com/aliacollins/evelens/compare/v1.0.0-beta.2...v1.0.0
[1.0.0-beta.2]: https://github.com/aliacollins/evelens/compare/v1.0.0-beta.1...v1.0.0-beta.2
[1.0.0-beta.1]: https://github.com/aliacollins/evelens/compare/v1.0.0-alpha.1...v1.0.0-beta.1
[1.0.0-alpha.1]: https://github.com/aliacollins/evelens/releases/tag/v1.0.0-alpha.1
