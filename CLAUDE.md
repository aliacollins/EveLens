# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architectural Laws (NEVER VIOLATE)

These laws prevent regression to the old monolithic architecture. 1857 tests enforce them.

1. **No Static State** — Never create static mutable state. Use AppServices + interfaces.
2. **No God Objects** — No class >500 lines or referenced by >30 files. Split early.
3. **Dependencies Flow Down** — `Core→Data→Serialization→Models→Infrastructure→Common→EveLens`. Never reverse. `AssemblyBoundaryTests` enforces this.
4. **New Services Must Be Testable** — Constructor injection, interfaces, test doubles. No dependency on EveLensClient/Settings statics.
5. **Events Through EventAggregator Only** — Never add static events. Never `EveLensClient.X += handler`. Always `AppServices.EventAggregator?.Subscribe<T>()`.
6. **Lazy by Default** — Collections/expensive objects use `Lazy<T>`. Constructors must be fast.
7. **No Sync-Over-Async** — Never `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` except in Program.cs bootstrap.
8. **Right File, Right Assembly** — Interfaces→Core, Enums→Data, DTOs→Serialization, Services→Common/Services, UI→EveLens project.
9. **EveLensClient Is Frozen** — Never add to it. New service→AppServices. New event→EventAggregator. New property→AppServices. Only Program.cs/AppServices/adapters may touch it.
10. **All Async Void Must Have Try/Catch** — WinForms requires async void for handlers. Wrap entire body in try/catch.
11. **Event Subscriptions Must Be Disposed** — Store `IDisposable` from Subscribe, dispose on form close/control dispose.
12. **Tests Prove Behavior** — No feature without a test. No bug fix without a regression test. Commit tests with code.
13. **Serialization DTOs Are the Data Contract** — Changes to SerializableSettings/JsonConfig/etc. require round-trip tests.
14. **No Direct EveLensClient Access from UI** — Use `AppServices.Characters` not `EveLensClient.Characters`. If AppServices lacks it, add a forwarding property.
15. **No Fuzzwork** — Never use Fuzzwork (fuzzwork.co.uk) for SDE data, market data, or any EVE data. Always use CCP's official SDE from `https://developers.eveonline.com/static-data` and ESI API directly.

## Promotion System (CRITICAL)

**NEVER push directly to main, alpha, or beta branches.** Use the promotion system:

```powershell
# Promote to alpha (from any branch)
.\scripts\promote.ps1 alpha -Message "Added feature X"

# Promote to beta (usually from alpha)
.\scripts\promote.ps1 beta -Message "Ready for beta testing"

# Promote to stable/main (from alpha or beta)
.\scripts\promote.ps1 stable -Message "Production release"
```

The promote script automatically increments version in `SharedAssemblyInfo.cs`, updates `CHANGELOG.md` and `updates/patch-*.xml`, creates standardized commit message, and merges/pushes.

**Version flow:** `1.0.0-alpha.1 → 1.0.0-alpha.2 → 1.0.0-beta.1 → 1.0.0 (stable)`

| Branch | Purpose | Push Method |
|--------|---------|-------------|
| `feature/*`, `fix/*`, `experimental/*` | Development work | `git push` (direct OK) |
| `alpha` | Alpha testing | `promote.ps1 alpha` only |
| `beta` | Beta testing | `promote.ps1 beta` only |
| `main` | Stable releases | `promote.ps1 stable` only |

### Branching Policy (MANDATORY)

**All work MUST happen on feature branches created from `alpha`.** Never work directly on `beta` or `main`.

```bash
# Starting new work — ALWAYS create a branch from alpha
git checkout alpha && git pull
git checkout -b feature/my-feature    # for new features
git checkout -b fix/issue-description # for bug fixes
git checkout -b experimental/idea     # for experiments

# Do your work, commit, push the branch
git push origin feature/my-feature

# When ready, promote to alpha (from the feature branch)
.\scripts\promote.ps1 alpha -Message "description"
```

**Rules:**
- **NEVER commit directly to `alpha`, `beta`, or `main`** — GitHub branch protection enforces this
- **NEVER checkout `beta` or `main` for development** — only README updates are acceptable
- **Branch naming:** `feature/`, `fix/`, or `experimental/` prefix required
- **Branch from `alpha`** — alpha is always the development baseline
- **Delete branches after merge** — keep the branch list clean
- **Pushing to alpha/beta/main triggers GitHub Actions** — builds, tests, and releases automatically

## Build & Test Commands

```bash
# Build (WSL — dotnet is on Windows side)
"/mnt/c/Program Files/dotnet/dotnet.exe" build EveLens.sln -c Debug

# Run (must use Windows path for GUI)
"/mnt/c/Program Files/dotnet/dotnet.exe" run --project src/EveLens.Avalonia/EveLens.Avalonia.csproj

# Run all tests (1857 tests, ~90s)
"/mnt/c/Program Files/dotnet/dotnet.exe" test tests/EveLens.Tests/EveLens.Tests.csproj

# Run a single test class
"/mnt/c/Program Files/dotnet/dotnet.exe" test tests/EveLens.Tests/EveLens.Tests.csproj --filter "FullyQualifiedName~ClassName"

# Run a single test method
"/mnt/c/Program Files/dotnet/dotnet.exe" test tests/EveLens.Tests/EveLens.Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName"

# Build release
"/mnt/c/Program Files/dotnet/dotnet.exe" publish src/EveLens.Avalonia/EveLens.Avalonia.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64
```

## TCP Diagnostic Stream (Port 5555)

The app streams structured JSON-lines over TCP for real-time debugging. `TcpJsonLoggerProvider` (registered in `AppServices.cs`) listens on port 5555 (or `EVELENS_DIAG_PORT` env var).

```bash
# Connect from WSL (app runs on Windows side, use gateway IP — NOT localhost)
WINHOST=$(ip route show default | awk '{print $3}' | head -1)
nc $WINHOST 5555

# Helper script with built-in filters
./scripts/diag-stream.sh              # all events
./scripts/diag-stream.sh esi          # ESI HTTP requests only
./scripts/diag-stream.sh fetch        # scheduler fetches only
./scripts/diag-stream.sh warn         # warnings/errors only
./scripts/diag-stream.sh evt          # EventAggregator events

# Manual jq filters
nc $WINHOST 5555 | jq -r 'select(.tag == "ESI") | .msg'        # ESI requests only
nc $WINHOST 5555 | jq -r 'select(.tag == "FETCH") | .msg'      # Scheduler fetches
nc $WINHOST 5555 | jq -r 'select(.tag == "EVT") | .msg'        # EventAggregator events
nc $WINHOST 5555 | jq -r 'select(.lvl == "WRN") | .msg'        # Warnings only
```

**JSON format:**
```json
{"ts":"2026-02-18T06:55:50.210Z","lvl":"INF","tag":"ESI","cat":"HttpClientServiceRequest","msg":"GET /characters/12345/skills → 304 Not Modified (89ms)"}
{"ts":"...","lvl":"INF","tag":"FETCH","cat":"EsiScheduler","msg":"char47/Skills → 200 OK (180ms), tokens=142/150"}
{"ts":"...","lvl":"WRN","tag":"WARN","cat":"SmartQueryScheduler","msg":"ESI rate limited — active=18/20"}
```

**Key files:**
- Provider: `src/EveLens.Infrastructure/Logging/TcpJsonLoggerProvider.cs`
- Registration: `src/EveLens.Common/Services/AppServices.cs:453`
- Listens on `IPAddress.Any`, port 5555 by default

## Changelog (MANDATORY)

**Every commit that changes behavior MUST update `CHANGELOG.md`.**

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Categories:

- **Added** — new features
- **Changed** — changes in existing functionality
- **Deprecated** — soon-to-be removed features
- **Removed** — now removed features
- **Fixed** — bug fixes
- **Security** — vulnerability fixes

**Rules:**
- Write entries in the `[Unreleased]` section as you work
- On release, the CI pipeline moves `[Unreleased]` entries into the new version section
- Write for humans, not machines — explain *what changed and why*, not *which files*
- One entry per user-visible change, not per commit
- GitHub Actions uses the changelog section as release notes in the update dialog

**Example:**
```markdown
## [Unreleased]

### Fixed

- ESI errors no longer spam the activity log during intermittent connectivity (Issue #34)
```

## Commit Guidelines

- **NEVER push to protected branches directly** — GitHub branch protection enforces this
- **NEVER commit without explicit user approval** — wait for confirmation
- **No Claude attribution** — do not add "Co-Authored-By: Claude" to commits
- **Batch related changes** — group into single meaningful commits

## Architecture (Post-Phoenix)

### Assembly Hierarchy (Acyclic DAG)

```
EveLens (WinForms UI, entry point)
  └→ EveLens.Common (services, settings, static facades)
       ├→ EveLens.Core (interfaces only, zero dependencies — the leaf)
       ├→ EveLens.Data (enums, constants, static game data)
       ├→ EveLens.Serialization (ESI/Eve/Settings DTOs)
       ├→ EveLens.Models (domain models)
       └→ EveLens.Infrastructure (EventAggregator, services)
```

No circular dependencies. `EveLens.Core` is the dependency-free interface layer. All assemblies have `InternalsVisibleTo("EveLens.Tests")`.

### Service Architecture (Strangler Fig Pattern)

The codebase is mid-migration from static god objects to DI. Two coexisting access patterns:

**New pattern (use this for new code):**
```csharp
// Subscribe to events via EventAggregator
_sub = AppServices.EventAggregator?.SubscribeOnUI<FiveSecondTickEvent>(this, OnTick);
// Dispose in OnFormClosing/Dispose
_sub?.Dispose();

// Access services via AppServices facade
AppServices.TraceService?.Trace("message");
AppServices.CharacterRepository.Characters;
```

**Old pattern (still exists in ~120 places, being phased out):**
```csharp
// DO NOT add new code using these patterns
EveLensClient.Characters  // → use AppServices.Characters
Settings.UI.MainWindow   // → still static, no replacement yet
```

### Key Components

**AppServices** (`src/EveLens.Common/Services/AppServices.cs`) — Static DI facade. Lazy-initializes 20+ services. Overridable via `Set*()` methods for testing. `Reset()` for test isolation. `SyncToServiceLocator()` bridges to EveLens.Core's `ServiceLocator`.

**EventAggregator** (`src/EveLens.Infrastructure/Services/EventAggregator.cs`) — Sole event delivery mechanism. Supports strong and weak references. Thread-safe (ConcurrentDictionary + locks). All old static events on EveLensClient have been removed.

**Event types** live in two namespaces:
- `EveLens.Core.Events` — Timer events (`SecondTickEvent`, `FiveSecondTickEvent`, `ThirtySecondTickEvent`)
- `EveLens.Common.Events` — Domain events (`CharacterUpdatedEvent`, `SettingsChangedEvent`, etc.)

**Settings** — Static partial class split across `Settings.cs`, `SettingsLoader.cs`, `SettingsIO.cs`. JSON is source of truth (migrated from XML). `SmartSettingsManager` coalesces saves. `SettingsSaveSubscriber` wires EventAggregator events to `Settings.Save()`.

**Startup flow** (`src/EveLens/Program.cs`):
```
EveLensClient.Initialize() → ServiceRegistration.Configure() →
AppServices.SyncToServiceLocator() → Settings.Initialize() →
GlobalDatafileCollection.LoadAsync() → Settings.ImportDataAsync() →
MainWindow
```

### Data Flow (Current)

```
ESI API → SmartQueryScheduler → QueryMonitor → CCPCharacter →
  EventAggregator.Publish(CharacterUpdatedEvent) →
  UI subscribes via SubscribeOnUI<T>()
```

### Core Models

- `Character.cs` / `CCPCharacter.cs` — Character with skills, attributes, plans. Constructed with `ICharacterServices` (use `NullCharacterServices` in tests)
- `ESIKey.cs` — OAuth tokens, constructed from `SerializableESIKey`
- `Plan.cs` — Skill training plans (requires loaded game data for prerequisite resolution)

### Interfaces (`src/EveLens.Core/Interfaces/`)

19 interfaces including: `IEventAggregator`, `IDispatcher`, `ISettingsProvider`, `ICharacterRepository` (split into `ICharacterReader`/`ICharacterWriter`), `IStation`, `IApplicationPaths`, `ITraceService`, `IEsiClient`

### ViewModel Layer (`src/EveLens.Common/ViewModels/`)

MVVM infrastructure using CommunityToolkit.Mvvm. ViewModels live in `EveLens.Common` (no new assembly).

**Base classes:**
- `ViewModelBase` — ObservableObject + IDisposable, auto-tracked subscriptions via CompositeDisposable, `Subscribe<T>()`, `SetPropertyOnUI<T>()`
- `CharacterViewModelBase` — Character property, `SubscribeForCharacter<T>()` for character-filtered events
- `ListViewModel<TItem,TColumn,TGrouping>` — Generic filter/sort/group pipeline. TextFilter, Grouping, SortColumn → `Refresh()` → GroupedItems
- `FormViewModel` — IsDirty tracking, Apply/Cancel for dialogs

**Concrete ViewModels (11 list VMs + 4 app VMs):**
- `Lists/AssetsListViewModel`, `MarketOrdersListViewModel`, `ContractsListViewModel`, `IndustryJobsListViewModel`, `WalletJournalListViewModel`, `WalletTransactionsListViewModel`, `MailMessagesListViewModel`, `NotificationsListViewModel`, `KillLogListViewModel`, `PlanetaryListViewModel`, `ResearchPointsListViewModel`
- `MainWindowViewModel`, `CharacterMonitorBodyViewModel`, `PlanEditorViewModel`, `SettingsFormViewModel`

**Binding helpers** (`ViewModels/Binding/`): `PropertyBinding`, `ListViewBindingHelper`, `CompositeDisposable`, `ActionDisposable`

**Migration status: COMPLETE.** All 11 list controls fully delegate filter/sort/group to their VMs. Old pipeline methods (`IsTextMatching`, `UpdateSort`, `UpdateContentByGroup`) deleted from all controls. `PlanEditorViewModel` wired in `PlanWindow`, `SettingsFormViewModel` wired in `SettingsForm`. Architecture tests enforce VM-to-control pairing, old method removal, and GroupedItems contract guarantees.

**Pattern for new list views:**
```csharp
// 1. Create VM subclass
public sealed class MyListViewModel : ListViewModel<MyItem, MyColumn, MyGrouping>
{
    protected override IEnumerable<MyItem> GetSourceItems() => ...;
    protected override bool MatchesFilter(MyItem item, string filter) => ...;
    protected override int CompareItems(MyItem x, MyItem y, MyColumn column) => ...;
    protected override string GetGroupKey(MyItem item, MyGrouping grouping) => ...;
}

// 2. In the control, read from VM:
_viewModel.Refresh();
var groups = _viewModel.GroupedItems; // filtered, sorted, grouped
```

## Testing

**1201 tests** across 66 files. xUnit 2.9 + FluentAssertions 6.12 + NSubstitute 5.1.

### Test Patterns

```csharp
// Pattern 1: Isolated character (no EveLensClient needed)
var services = new NullCharacterServices();
var identity = new CharacterIdentity(1L, "Test Pilot");
var character = new CCPCharacter(identity, services);

// Pattern 2: Service tests with mocks
var aggregator = new EventAggregator(); // real, not mocked
var dispatcher = Substitute.For<IDispatcher>();
dispatcher.When(d => d.Invoke(Arg.Any<Action>())).Do(ci => ci.ArgAt<Action>(0).Invoke());

// Pattern 3: Settings round-trip
var settings = new SerializableSettings { Revision = 5 };
var result = XmlRoundTrip(settings); // or JSON round-trip

// Pattern 4: Event verification
aggregator.Subscribe<CharacterUpdatedEvent>(e => received = true);
aggregator.Publish(new CharacterUpdatedEvent(character));
received.Should().BeTrue();
```

### Test Collections

Tests that mutate `AppServices` shared state use `[Collection("AppServices")]` to prevent parallel execution conflicts. Apply this attribute when writing tests that call `AppServices.Set*()`, `Reset()`, or `SyncToServiceLocator()`.

### Test Organization

| Directory | Focus |
|-----------|-------|
| `Integration/` | EventAggregator E2E, character scaling, settings persistence |
| `Models/` | Character state, ESI keys, plans, market orders, contracts, skills |
| `Services/` | AppServices routing, ServiceLocator sync, SmartSettingsManager, subscribers |
| `Settings/` | Round-trip, migration detection, loader paths |
| `Regression/` | Crash prevention, data integrity, GitHub issue reproductions |
| `Serialization/` | ESI DTOs, character DTOs, settings DTOs |
| `Architecture/` | Assembly boundary validation (no circular deps) |

## Versioning

| Type | AssemblyVersion | Display |
|------|-----------------|---------|
| Stable | `5.2.0.0` | `5.2.0` |
| Alpha | `5.2.0.1` | `5.2.0-alpha.1` |
| Beta | `5.2.0.2` | `5.2.0-beta.2` |

Edit `SharedAssemblyInfo.cs` directly only for manual overrides. Normally use `promote.ps1`.

## Bug Fix Documentation

Document every bug fix with root cause, fix, and files changed.

### Issue #4: Settings Not Saving
**Root Cause:** `GetRevisionNumber()` returned 0 for both `revision="0"` and missing attribute.
**Fix:** Return -1 for missing, change checks to `< 0`.

### Issue #5: Certificates Not Accurate
**Root Cause:** CCP removed certificates from EVE.
**Fix:** Hide Certificate Browser tab in `PlanWindow.cs`.

### Fork Migration (v5.1.2)
**Detection:** `forkId` attribute; if missing, `revision > 1000` = peterhaneve user.
**Tested in:** `SettingsMigrationTests.cs`, `GitHubIssueTests.cs`

### 30+ Characters Crash (v5.1.2)
**Root Cause:** Dead Hammertime API + async fire-and-forget.
**Fix:** `StructureLookupService` with deduplication + `SmartQueryScheduler` staggered startup.

## Remaining Static Coupling (Migration Targets)

- **122 `EveLensClient.` references** — mostly collection access and lifecycle in EveLens.Common
- **637 `Settings.` references** — `Settings.UI.*` accounts for ~400 of these (UI layer)
- Forms use `AppServices.*` static access rather than constructor injection
- `AppServices` wraps static classes (Strangler Fig) — not yet true DI composition

New code should use interfaces and EventAggregator. The static access paths remain for backward compatibility during migration.

## Localization (i18n)

**Architecture:** Dictionary-based `Loc` static class in `src/EveLens.Common/Services/LocalizationService.cs`. No .resx files — translations are inline dictionaries for simplicity with 2 languages.

**Usage:** `Loc.Get("key")` returns the translated string. Falls back to English if key missing in current language.

**Adding a new string:**
1. Add key to `RegisterEnglish()` dictionary
2. Add same key to `RegisterChineseSimplified()` dictionary
3. Use `Loc.Get("key")` in code-behind (not AXAML — set via code-behind after InitializeComponent)

**Languages:** `en` (English), `zh-CN` (Simplified Chinese). Language stored in `Settings.UI.Language`, applied on startup in `App.axaml.cs`. Changing language triggers auto-restart via `AppServices.ApplicationLifecycle.Restart()`.

**SDE Translations:** `StaticTranslations` class loads 50K+ Chinese type/group names from `eve-translations-zh-CN.xml.gzip`. Generated by `tools/parse-sde.ps1` from CCP's Static Data Export. `StaticSkill.LocalizedName`, `StaticSkillGroup.LocalizedName`, `Item.LocalizedName`, `MarketGroup.LocalizedName` all check translations first, fall back to English `Name`.

**Key files:**
- `src/EveLens.Common/Services/LocalizationService.cs` — All UI string translations
- `src/EveLens.Common/Data/StaticTranslations.cs` — SDE game data translations
- `src/EveLens.Common/Resources/eve-translations-zh-CN.xml.gzip` — 50K type + 2.6K group translations
- `tools/generate-translations.ps1` — Downloads SDE and generates translation file
- `tools/parse-sde.ps1` — Parses SDE YAML for Chinese names

## Doctrine Designer (Global Plan Templates)

Shared skill templates that can be applied across multiple characters. Persisted in `Settings.GlobalPlanTemplates`.

**Key files:**
- `src/EveLens.Common/SettingsObjects/GlobalPlanTemplateSettings.cs` — Data model
- `src/EveLens.Common/ViewModels/GlobalPlanDashboardViewModel.cs` — ViewModel with CRUD, comparison, plan generation
- `src/EveLens.Avalonia/Views/Dialogs/GlobalPlanDashboardWindow.axaml(.cs)` — UI
- `tests/EveLens.Tests/ViewModels/GlobalPlanDashboardViewModelTests.cs` — 17 tests

**Persistence:** `GlobalPlanTemplate` stored in `SerializableSettings.GlobalPlanTemplates`, serialized to `config.json` via `JsonGlobalPlanTemplate` DTO. Round-trip through Decomposer/Loader.

## Release Script Encoding

The `release.ps1` script extracts changelog entries for GitHub release notes. Use ASCII-safe characters (`--` not `—`, `>` not `→`) in CHANGELOG.md to avoid encoding issues with `gh release create`. The script writes `release-notes.md` with UTF-8 BOM which `gh` may misinterpret.

## How to Add a Feature (End-to-End)

Follow this sequence for any new feature. Skip steps that don't apply.

1. **Define the interface** in `src/EveLens.Core/Interfaces/IMyService.cs` — XML docs with summary, remarks, production/testing info
2. **Define event types** in `src/EveLens.Core/Events/` (if timer/no deps) or `src/EveLens.Common/Events/CommonEvents.cs` (if carries model data). Use singleton `Instance` for parameterless events.
3. **Implement the service** in `src/EveLens.Common/Services/MyServiceImpl.cs` — accept dependencies via constructor
4. **Register in AppServices.cs**:
   ```csharp
   private static Lazy<IMyService> s_myService = new(() => new MyServiceImpl());
   public static IMyService MyService => s_myService.Value;
   internal static void SetMyService(IMyService svc) => s_myService = new(() => svc);
   ```
5. **Sync to ServiceLocator** if Models/Infrastructure need access — add property to `ServiceLocator.cs`, add sync line in `AppServices.SyncToServiceLocator()`
6. **Subscribe in UI** via `SubscribeOnUI<T>()`, store `IDisposable`, dispose on close
7. **Write tests** — use `NullCharacterServices` for characters, `new EventAggregator()` for events, `Substitute.For<IMyService>()` for mocks
8. **Run full suite**: `dotnet test tests/EveLens.Tests/EveLens.Tests.csproj` — 1857+ must pass
9. **Verify laws**: no `EveLensClient.` in UI, no static events, no async void without try/catch

## Avalonia Architectural Laws (NEVER VIOLATE)

These laws govern all code in `src/EveLens.Avalonia/`. They extend (not replace) the main Architectural Laws above.

15. **Every View Must Have a ViewModel** — No view may bind directly to Character/CCPCharacter model collections. Create a ViewModel in `EveLens.Common/ViewModels/` that transforms model data into bindable properties. Code-behind only wires the ViewModel, never builds data structures.
16. **ViewModels Live in EveLens.Common** — ViewModels are shared infrastructure, not Avalonia-specific. They must be testable without Avalonia references. Display-only wrapper types (color, formatting) may live in `EveLens.Avalonia/ViewModels/` but must contain zero business logic.
17. **Standard View Wiring Pattern** — Every view code-behind follows this exact pattern:
    ```csharp
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LoadData();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        LoadData();
    }
    private void LoadData()
    {
        Character? character = DataContext as Character;
        if (character == null)
        {
            var parent = this.FindAncestorOfType<CharacterMonitorView>();
            character = parent?.DataContext as Character;
        }
        if (character == null) return;
        _viewModel ??= new XxxViewModel();
        _viewModel.Character = character;
        // Populate controls from ViewModel
    }
    ```
18. **No Business Logic in Views** — Views handle: control wiring, event handler delegation to ViewModel, visual tree navigation. Views never: filter data, sort data, compute aggregates, build display models, or format strings from model properties.
19. **Grouped Views Use Expander Pattern** — All grouped list views (Skills, Assets, etc.) use the same Expander-based layout with full-width stretch styles. Group headers show: name, count, summary value. Items stretch to fill width. Collapse/Expand All buttons required.
20. **DataGrid Requires .ToList()** — EveLens's custom `ReadonlyCollection<T>` does not implement `IList`. Always call `.ToList()` before setting `ItemsSource`. Never bind DataGrid ItemsSource directly to model collection properties in XAML.
21. **DataGrid Theme Required** — `App.axaml` must include `<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"/>` before the EveLens theme. Without this, DataGrid rows render with zero height.
22. **Portrait/Image Loading Is Async Code-Behind** — Character portraits and corporation logos load via `ImageService.GetCharacterImageAsync()` / `CorporationImage` in code-behind after visual tree attachment. Use `DrawingImageToAvaloniaConverter.Instance` to convert `System.Drawing.Image` to `Avalonia.Media.Imaging.Bitmap`. Never block the UI thread.
23. **Consistent Visual Language** — All views use: 11pt body text, 12pt headers, `EveAccentPrimaryBrush` for names/titles, `EveTextSecondaryBrush` for metadata, `EveTextDisabledBrush` for tertiary info, `EveSuccessGreenBrush` for ISK values, `EveBackgroundMediumBrush` for elevated surfaces. Pill-shaped buttons (CornerRadius=12). Thin modern scrollbars. Full-width expander headers.
24. **Dispose on Detach** — ViewModels with EventAggregator subscriptions must be disposed in `OnDetachedFromVisualTree`, not `OnUnloaded` (which fires on tab switches and causes crashes).
25. **ObservableCharacter Is Thin** — `ObservableCharacter` wraps display properties only (≤30). No collections, no methods, no business logic. If it exceeds 30 properties, split or delegate to a sub-VM. Architecture test enforces this cap. Collections belong in dedicated ListViewModels.

### Avalonia UI Design System (ENFORCED)

These are non-negotiable visual standards. Every pixel must be intentional.

**Typography Scale:**
| Usage | Size | Weight | Brush |
|-------|------|--------|-------|
| Window title | 15pt | Bold | EveAccentPrimaryBrush |
| Section header (group name) | 12pt | SemiBold | EveAccentPrimaryBrush |
| Body text (data values) | 11pt | Regular | EveTextPrimaryBrush |
| Secondary info (metadata) | 11pt | Regular | EveTextSecondaryBrush |
| Tertiary info (rank, volume) | 10pt | Regular | EveTextDisabledBrush |
| Status bar | 11pt | Regular | EveTextSecondaryBrush |

**Color Semantics (never deviate):**
- **Gold** (`EveAccentPrimaryBrush`) — Names, titles, active selection, group headers
- **Green** (`EveSuccessGreenBrush`) — ISK values, positive status (Online, Omega)
- **Yellow** (`EveWarningYellowBrush`) — Training in progress, warnings
- **Red** (`EveErrorRedBrush`) — Errors, negative values, expired
- **Gray light** (`EveTextSecondaryBrush`) — Counts, dates, secondary labels
- **Gray dark** (`EveTextDisabledBrush`) — Ranks, volumes, tertiary data
- **Primary text** (`EveTextPrimaryBrush`) — Item names, skill names, data values

**Spacing & Layout:**
- Outer padding: 10px horizontal, 7px vertical on toolbars/headers
- Inner padding: 8-12px on cards and group headers
- Item row padding: 20px left indent (under group), 4px vertical
- Group margin: 1px bottom between groups
- Card margin: 4-6px between cards
- Button padding: 10px horizontal, 4px vertical

**Component Standards:**
- **Search bar**: Pill shape (CornerRadius=14), dark background, transparent TextBox inside, clear button (✕) appears on input
- **Action buttons**: Pill shape (CornerRadius=12), text-only, grouped in StackPanel with Spacing=6
- **Toggle buttons**: Pill shape, highlight on checked state
- **Expander groups**: Full-width stretch (Laws in theme), Medium background header, Dark background items
- **Level indicators**: 5 blocks, 10×12px, CornerRadius=2, Gold=trained, Green=training, Dark=untrained
- **Status bar**: Bottom-docked, 1px top border, same medium background as toolbar
- **Scrollbars**: 8px thin, gold thumb, no arrows (defined in EveLensTheme.axaml)
- **Cards**: CornerRadius=6, 1px border, medium background, min-height for consistency

**Layout Rules:**
- All list views use full-width stretch — no ragged widths
- Grids use `ColumnDefinitions="*,Auto,Auto,..."` — first column stretches, rest auto-size
- Long text uses `TextTrimming="CharacterEllipsis"` with `ToolTip.Tip` for full text
- Items within groups are sorted alphabetically by default
- Groups sorted alphabetically by default
- Consistent status bar format: `"Label: {count} items | Secondary: {value}"`

**Interaction Patterns:**
- **Filter**: Immediate (on text change), clear button appears when non-empty
- **Grouping**: ComboBox dropdown, re-renders on change
- **Collapse/Expand**: Pair of buttons, always visible in toolbar
- **Navigation**: Click on overview card → navigate to character tab
- **Images**: Load async after visual tree attachment, show placeholder border while loading

### Avalonia File Organization

```
EveLens.Avalonia/
  Views/
    MainWindow.axaml(.cs)          — Menu, status bar, tab container
    CharacterMonitor/
      CharacterMonitorView.axaml   — Header + sub-tab container
      CharacterMonitorHeader.axaml — Portrait + character info
      CharacterOverviewView.axaml  — All-characters card grid
      Character*View.axaml(.cs)    — One per sub-tab (19 total)
    Dialogs/                       — Modal windows
    Shared/                        — Reusable controls (future)
  ViewModels/                      — Avalonia-specific display wrappers ONLY
  Converters/                      — IValueConverter implementations
  Services/                        — Platform adapters (Dispatcher, Dialog, etc.)
  Themes/                          — EveLensTheme.axaml
```

### Pattern for New Avalonia List Views (End-to-End)

1. **Create ViewModel** in `EveLens.Common/ViewModels/Lists/XxxViewModel.cs` — inherit `CharacterViewModelBase`, expose grouped data as `List<XxxGroupEntry>` via `IReadOnlyList`, include filter/grouping logic.
2. **Create display wrapper** (if needed) in `EveLens.Avalonia/ViewModels/XxxDisplayEntry.cs` — adds IBrush properties for colors, formatted strings. No business logic.
3. **Create AXAML** in `EveLens.Avalonia/Views/CharacterMonitor/CharacterXxxView.axaml` — use Expander+ItemsControl pattern for grouped views, DataGrid for flat tabular views. Full-width stretch. Pill search bar with clear button.
4. **Create code-behind** using standard wiring pattern (Law 17). Dispose ViewModel in `OnDetachedFromVisualTree`.
5. **Add to CharacterMonitorView.axaml** — new TabItem with compact header.
6. **Write tests** for the ViewModel in `tests/EveLens.Tests/ViewModels/`.

## Brand Identity

**Product name:** EveLens
**Tagline:** Character Intelligence for EVE Online
**Domain:** evelens.dev
**Executable:** EveLens.exe

This project was formerly known as EVEMon. It was rebranded to EveLens in February 2026. The codebase is forked exclusively from peterhaneve/evemon (v4.0.20). No code from any other fork was ever used.

**Naming rules:**
- The product is always "EveLens" (capital E, capital L, no spaces)
- The original project is always "EVEMon" when referenced historically
- The original team is always "EVEMon Development Team" in copyright headers
- Copyright line: `Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins`
- Never use "EVEMon NexT" — that name is retired
- Settings migrate from `%APPDATA%\EVEMon` to `%APPDATA%\EveLens` automatically on first launch

**Assembly names:** `EveLens.*` (EveLens.Common, EveLens.Core, EveLens.Data, etc.)
**Namespaces:** `EveLens.*`
**Main class:** `EveLensClient` (frozen — see Law 9)
**Constants:** `EveLensConstants`
**Named mutex:** `EveLensInstanceSignal`
**Env vars:** `EVELENS_DIAG_PORT`
**Icon:** `src/EveLens.Avalonia/Properties/EveLens.ico` (used on all windows, tray, About page)

## Project Context

- **Maintainer:** Alia Collins (EVE character)
- **GitHub:** https://github.com/aliacollins/evelens
- **.NET 8 Avalonia** cross-platform application (Windows, Linux, macOS)
- **ESI API** for EVE Online data (OAuth2)
- Settings location: `%APPDATA%\EveLens\`
- **Version:** 1.3.0-beta.1
- **Tests:** 1857 passing
