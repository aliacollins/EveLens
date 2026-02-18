# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architectural Laws (NEVER VIOLATE)

These laws prevent regression to the old monolithic architecture. 821 tests enforce them.

1. **No Static State** — Never create static mutable state. Use AppServices + interfaces.
2. **No God Objects** — No class >500 lines or referenced by >30 files. Split early.
3. **Dependencies Flow Down** — `Core→Data→Serialization→Models→Infrastructure→Common→EVEMon`. Never reverse. `AssemblyBoundaryTests` enforces this.
4. **New Services Must Be Testable** — Constructor injection, interfaces, test doubles. No dependency on EveMonClient/Settings statics.
5. **Events Through EventAggregator Only** — Never add static events. Never `EveMonClient.X += handler`. Always `AppServices.EventAggregator?.Subscribe<T>()`.
6. **Lazy by Default** — Collections/expensive objects use `Lazy<T>`. Constructors must be fast.
7. **No Sync-Over-Async** — Never `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` except in Program.cs bootstrap.
8. **Right File, Right Assembly** — Interfaces→Core, Enums→Data, DTOs→Serialization, Services→Common/Services, UI→EVEMon project.
9. **EveMonClient Is Frozen** — Never add to it. New service→AppServices. New event→EventAggregator. New property→AppServices. Only Program.cs/AppServices/adapters may touch it.
10. **All Async Void Must Have Try/Catch** — WinForms requires async void for handlers. Wrap entire body in try/catch.
11. **Event Subscriptions Must Be Disposed** — Store `IDisposable` from Subscribe, dispose on form close/control dispose.
12. **Tests Prove Behavior** — No feature without a test. No bug fix without a regression test. Commit tests with code.
13. **Serialization DTOs Are the Data Contract** — Changes to SerializableSettings/JsonConfig/etc. require round-trip tests.
14. **No Direct EveMonClient Access from UI** — Use `AppServices.Characters` not `EveMonClient.Characters`. If AppServices lacks it, add a forwarding property.

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

**Version flow:** `5.2.0-alpha.1 → 5.2.0-alpha.2 → 5.2.0-beta.1 → 5.2.0 (stable)`

| Branch | Purpose | Push Method |
|--------|---------|-------------|
| `feature/*`, `experimental/*` | Development work | `git push` (direct OK) |
| `alpha` | Alpha testing | `promote.ps1 alpha` only |
| `beta` | Beta testing | `promote.ps1 beta` only |
| `main` | Stable releases | `promote.ps1 stable` only |

## Build & Test Commands

```bash
# Build (WSL — dotnet is on Windows side)
"/mnt/c/Program Files/dotnet/dotnet.exe" build EVEMon.sln -c Debug

# Run (must use Windows path for GUI)
"/mnt/c/Program Files/dotnet/dotnet.exe" run --project src/EVEMon/EVEMon.csproj

# Run all tests (821 tests, ~30s)
"/mnt/c/Program Files/dotnet/dotnet.exe" test tests/EVEMon.Tests/EVEMon.Tests.csproj

# Run a single test class
"/mnt/c/Program Files/dotnet/dotnet.exe" test tests/EVEMon.Tests/EVEMon.Tests.csproj --filter "FullyQualifiedName~ClassName"

# Run a single test method
"/mnt/c/Program Files/dotnet/dotnet.exe" test tests/EVEMon.Tests/EVEMon.Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName"

# Build release
"/mnt/c/Program Files/dotnet/dotnet.exe" publish src/EVEMon/EVEMon.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64
```

## Commit Guidelines

- **NEVER push to protected branches directly** — pre-push hook blocks it
- **NEVER commit without explicit user approval** — wait for confirmation
- **No Claude attribution** — do not add "Co-Authored-By: Claude" to commits
- **Batch related changes** — group into single meaningful commits

## Architecture (Post-Phoenix)

### Assembly Hierarchy (Acyclic DAG)

```
EVEMon (WinForms UI, entry point)
  └→ EVEMon.Common (services, settings, static facades)
       ├→ EVEMon.Core (interfaces only, zero dependencies — the leaf)
       ├→ EVEMon.Data (enums, constants, static game data)
       ├→ EVEMon.Serialization (ESI/Eve/Settings DTOs)
       ├→ EVEMon.Models (domain models)
       └→ EVEMon.Infrastructure (EventAggregator, services)
```

No circular dependencies. `EVEMon.Core` is the dependency-free interface layer. All assemblies have `InternalsVisibleTo("EVEMon.Tests")`.

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
EveMonClient.Characters  // → use AppServices.Characters
Settings.UI.MainWindow   // → still static, no replacement yet
```

### Key Components

**AppServices** (`src/EVEMon.Common/Services/AppServices.cs`) — Static DI facade. Lazy-initializes 20+ services. Overridable via `Set*()` methods for testing. `Reset()` for test isolation. `SyncToServiceLocator()` bridges to EVEMon.Core's `ServiceLocator`.

**EventAggregator** (`src/EVEMon.Infrastructure/Services/EventAggregator.cs`) — Sole event delivery mechanism. Supports strong and weak references. Thread-safe (ConcurrentDictionary + locks). All old static events on EveMonClient have been removed.

**Event types** live in two namespaces:
- `EVEMon.Core.Events` — Timer events (`SecondTickEvent`, `FiveSecondTickEvent`, `ThirtySecondTickEvent`)
- `EVEMon.Common.Events` — Domain events (`CharacterUpdatedEvent`, `SettingsChangedEvent`, etc.)

**Settings** — Static partial class split across `Settings.cs`, `SettingsLoader.cs`, `SettingsIO.cs`. JSON is source of truth (migrated from XML). `SmartSettingsManager` coalesces saves. `SettingsSaveSubscriber` wires EventAggregator events to `Settings.Save()`.

**Startup flow** (`src/EVEMon/Program.cs`):
```
EveMonClient.Initialize() → ServiceRegistration.Configure() →
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

### Interfaces (`src/EVEMon.Core/Interfaces/`)

19 interfaces including: `IEventAggregator`, `IDispatcher`, `ISettingsProvider`, `ICharacterRepository` (split into `ICharacterReader`/`ICharacterWriter`), `IStation`, `IApplicationPaths`, `ITraceService`, `IEsiClient`

### ViewModel Layer (`src/EVEMon.Common/ViewModels/`)

MVVM infrastructure using CommunityToolkit.Mvvm. ViewModels live in `EVEMon.Common` (no new assembly).

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
// Pattern 1: Isolated character (no EveMonClient needed)
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

- **122 `EveMonClient.` references** — mostly collection access and lifecycle in EVEMon.Common
- **637 `Settings.` references** — `Settings.UI.*` accounts for ~400 of these (UI layer)
- Forms use `AppServices.*` static access rather than constructor injection
- `AppServices` wraps static classes (Strangler Fig) — not yet true DI composition

New code should use interfaces and EventAggregator. The static access paths remain for backward compatibility during migration.

## How to Add a Feature (End-to-End)

Follow this sequence for any new feature. Skip steps that don't apply.

1. **Define the interface** in `src/EVEMon.Core/Interfaces/IMyService.cs` — XML docs with summary, remarks, production/testing info
2. **Define event types** in `src/EVEMon.Core/Events/` (if timer/no deps) or `src/EVEMon.Common/Events/CommonEvents.cs` (if carries model data). Use singleton `Instance` for parameterless events.
3. **Implement the service** in `src/EVEMon.Common/Services/MyServiceImpl.cs` — accept dependencies via constructor
4. **Register in AppServices.cs**:
   ```csharp
   private static Lazy<IMyService> s_myService = new(() => new MyServiceImpl());
   public static IMyService MyService => s_myService.Value;
   internal static void SetMyService(IMyService svc) => s_myService = new(() => svc);
   ```
5. **Sync to ServiceLocator** if Models/Infrastructure need access — add property to `ServiceLocator.cs`, add sync line in `AppServices.SyncToServiceLocator()`
6. **Subscribe in UI** via `SubscribeOnUI<T>()`, store `IDisposable`, dispose on close
7. **Write tests** — use `NullCharacterServices` for characters, `new EventAggregator()` for events, `Substitute.For<IMyService>()` for mocks
8. **Run full suite**: `dotnet test tests/EVEMon.Tests/EVEMon.Tests.csproj` — 821+ must pass
9. **Verify laws**: no `EveMonClient.` in UI, no static events, no async void without try/catch

## Avalonia Architectural Laws (NEVER VIOLATE)

These laws govern all code in `src/EVEMon.Avalonia/`. They extend (not replace) the main Architectural Laws above.

15. **Every View Must Have a ViewModel** — No view may bind directly to Character/CCPCharacter model collections. Create a ViewModel in `EVEMon.Common/ViewModels/` that transforms model data into bindable properties. Code-behind only wires the ViewModel, never builds data structures.
16. **ViewModels Live in EVEMon.Common** — ViewModels are shared infrastructure, not Avalonia-specific. They must be testable without Avalonia references. Display-only wrapper types (color, formatting) may live in `EVEMon.Avalonia/ViewModels/` but must contain zero business logic.
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
20. **DataGrid Requires .ToList()** — EVEMon's custom `ReadonlyCollection<T>` does not implement `IList`. Always call `.ToList()` before setting `ItemsSource`. Never bind DataGrid ItemsSource directly to model collection properties in XAML.
21. **DataGrid Theme Required** — `App.axaml` must include `<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"/>` before the EVEMon theme. Without this, DataGrid rows render with zero height.
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
- **Scrollbars**: 8px thin, gold thumb, no arrows (defined in EVEMonTheme.axaml)
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
EVEMon.Avalonia/
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
  Themes/                          — EVEMonTheme.axaml
```

### Pattern for New Avalonia List Views (End-to-End)

1. **Create ViewModel** in `EVEMon.Common/ViewModels/Lists/XxxViewModel.cs` — inherit `CharacterViewModelBase`, expose grouped data as `List<XxxGroupEntry>` via `IReadOnlyList`, include filter/grouping logic.
2. **Create display wrapper** (if needed) in `EVEMon.Avalonia/ViewModels/XxxDisplayEntry.cs` — adds IBrush properties for colors, formatted strings. No business logic.
3. **Create AXAML** in `EVEMon.Avalonia/Views/CharacterMonitor/CharacterXxxView.axaml` — use Expander+ItemsControl pattern for grouped views, DataGrid for flat tabular views. Full-width stretch. Pill search bar with clear button.
4. **Create code-behind** using standard wiring pattern (Law 17). Dispose ViewModel in `OnDetachedFromVisualTree`.
5. **Add to CharacterMonitorView.axaml** — new TabItem with compact header.
6. **Write tests** for the ViewModel in `tests/EVEMon.Tests/ViewModels/`.

## Project Context

- **Maintainer:** Alia Collins (EVE character)
- **GitHub:** https://github.com/aliacollins/evemon
- **.NET 8 Windows Forms + Avalonia** application (dual UI)
- **ESI API** for EVE Online data (OAuth2)
- Settings location: `%APPDATA%\EVEMon\`
