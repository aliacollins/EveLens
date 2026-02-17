# ViewModel Layer: MVVM Extraction of Filter/Sort/Group Pipeline

## Where We Started

EVEMon's 11 character monitoring list controls each contained their own filter/sort/group pipeline. Every control — assets, market orders, contracts, industry jobs, wallet journal, wallet transactions, mail messages, notifications, kill log, planetary, research points — implemented the same three-phase data transformation:

1. **Filter** items by text search across multiple fields
2. **Sort** by a user-selected column with ascending/descending toggle
3. **Group** by a user-selected grouping mode into named sections

The implementations varied between 400 and 1,500 lines per control. None were tested. Each was a procedural tangle of `UpdateContent()`, `UpdateContentByGroup<TKey>()`, `IsTextMatching()`, and `UpdateSort()` methods that mixed data logic with WinForms rendering code.

### The Duplication

Every control contained a variant of this pattern:

```csharp
// CharacterMarketOrdersList.cs (before) — ~800 lines of filter/sort/group
private void UpdateContent()
{
    var orders = Character.MarketOrders.Where(x => x.Item != null && x.Station != null);

    if (m_hideInactive)
        orders = orders.Where(x => x.IsAvailable);

    if (m_showIssuedFor != IssuedFor.All)
        orders = orders.Where(x => x.IssuedFor == m_showIssuedFor);

    if (!string.IsNullOrEmpty(m_textFilter))
        orders = orders.Where(x => IsTextMatching(x));

    // ... 200 lines of sorting switch statements ...
    // ... 300 lines of grouping logic ...
    // ... mixed with ListView population, column formatting, tooltip creation ...
}

private bool IsTextMatching(MarketOrder x)
{
    return x.Item.Name.Contains(m_textFilter, ...) ||
           x.Station.Name.Contains(m_textFilter, ...) ||
           // ... 10 more fields
}

private void UpdateContentByGroup<TKey>(IEnumerable<MarketOrder> orders, ...)
{
    // Another 150 lines grouping orders into ListView groups
}

private void UpdateSort()
{
    // 50 lines of sort logic + visual arrow update
}
```

Multiply this by 11 controls. Each had slightly different field names, slightly different sort comparisons, slightly different grouping strategies — but the same structural pattern.

### Problems

1. **Zero test coverage** — None of the filter/sort/group logic was testable. It was embedded inside WinForms controls that required a running message loop.

2. **Copy-paste drift** — Each control independently evolved its implementation. Bug fixes applied to one were not applied to others.

3. **UI thread coupling** — All data transformation ran on the UI thread, interleaved with rendering. No separation between "what to show" and "how to show it."

4. **God object violations** — Several controls exceeded 1,000 lines (Law #2), with filter/sort/group logic accounting for 60-70% of the code.

## What Changed

### Architecture: ListViewModel Pipeline

A generic `ListViewModel<TItem, TColumn, TGrouping>` base class consolidates the pipeline. Concrete subclasses implement four domain-specific methods (~80-116 lines each). The base class handles the rest (278 lines, shared across all 11).

```
                                ListViewModel<TItem, TColumn, TGrouping>
                                ┌─────────────────────────────────────────┐
 User sets TextFilter,          │  GetSourceItems()        ← subclass    │
 SortColumn, Grouping,    ───►  │    ↓                                   │
 or Character changes           │  MatchesFilter()         ← subclass    │
                                │    ↓                                   │
                                │  CompareItems()          ← subclass    │
                                │    ↓                                   │
                                │  GetGroupKey()           ← subclass    │
                                │    ↓                                   │
                                │  GroupedItems  ──────────────────────► UI reads here
                                │  TotalItemCount                        │
                                └─────────────────────────────────────────┘
```

Each concrete VM subscribes to domain events (`MarketOrdersUpdatedEvent`, `SettingsChangedEvent`, etc.) and calls `Refresh()` when data changes. The control reads `GroupedItems` and populates its `ListView`.

### Before/After: MarketOrders

**Before** (CharacterMarketOrdersList.cs, ~800 lines filter/sort/group):
```csharp
private void UpdateContent()             // 200+ lines: filter, sort, group, render
private void UpdateContentByGroup<TKey>() // 150 lines: group and render
private bool IsTextMatching()             // 30 lines: text search
private void UpdateSort()                 // 50 lines: sort + visual arrow
```

**After** (MarketOrdersListViewModel.cs, 116 lines total):
```csharp
public sealed class MarketOrdersListViewModel
    : ListViewModel<MarketOrder, MarketOrderColumn, MarketOrderGrouping>
{
    private bool _hideInactive;
    private IssuedFor _showIssuedFor = IssuedFor.All;

    public bool HideInactive
    {
        get => _hideInactive;
        set { if (SetProperty(ref _hideInactive, value)) Refresh(); }
    }

    public IssuedFor ShowIssuedFor
    {
        get => _showIssuedFor;
        set { if (SetProperty(ref _showIssuedFor, value)) Refresh(); }
    }

    protected override IEnumerable<MarketOrder> GetSourceItems()
    {
        if (Character is not CCPCharacter ccp)
            return Array.Empty<MarketOrder>();

        IEnumerable<MarketOrder> orders = ccp.MarketOrders
            .Where(x => x.Item != null && x.Station != null);

        if (_hideInactive)
            orders = orders.Where(x => x.IsAvailable);
        if (_showIssuedFor != IssuedFor.All)
            orders = orders.Where(x => x.IssuedFor == _showIssuedFor);

        return orders;
    }

    protected override bool MatchesFilter(MarketOrder x, string filter) =>
        x.Item.Name.Contains(filter, ignoreCase: true) || /* ... */;

    protected override int CompareItems(MarketOrder x, MarketOrder y, MarketOrderColumn column) =>
        column switch
        {
            MarketOrderColumn.Item => string.Compare(x.Item.Name, y.Item.Name, ...),
            MarketOrderColumn.UnitaryPrice => x.UnitaryPrice.CompareTo(y.UnitaryPrice),
            /* ... */
        };

    protected override string GetGroupKey(MarketOrder item, MarketOrderGrouping grouping) =>
        grouping switch
        {
            MarketOrderGrouping.State => item.State.ToString(),
            MarketOrderGrouping.OrderType => item is BuyOrder ? "Buying Orders" : "Selling Orders",
            /* ... */
        };
}
```

**After** (CharacterMarketOrdersList.cs control, relevant section):
```csharp
// Sync UI state to VM
_viewModel.Character = Character;
_viewModel.SortColumn = m_sortCriteria;
_viewModel.SortAscending = m_sortAscending;
_viewModel.Grouping = m_grouping;
_viewModel.TextFilter = m_textFilter;
_viewModel.HideInactive = Settings.UI.MainWindow.MarketOrders.HideInactiveOrders;
_viewModel.ShowIssuedFor = m_showIssuedFor;

// Read filtered/sorted/grouped data from VM — no filter/sort/group code here
var groupedItems = _viewModel.GroupedItems;

// Populate ListView from groupedItems (rendering only)
foreach (var group in groupedItems)
{
    var lvGroup = new ListViewGroup(group.Key);
    foreach (var item in group.Items)
    {
        var lvItem = CreateListViewItem(item);
        lvItem.Group = lvGroup;
        lvOrders.Items.Add(lvItem);
    }
}
```

The control retains rendering logic (creating `ListViewItem` instances, column formatting, tooltips, context menus). It no longer contains any filter/sort/group code. The old methods (`IsTextMatching`, `UpdateSort`, `UpdateContentByGroup`) were deleted.

## Class Hierarchy

```
ObservableObject (CommunityToolkit.Mvvm)
  └─ ViewModelBase (IDisposable, Subscribe<T>, Track, SetPropertyOnUI)
       ├─ CharacterViewModelBase (Character, SubscribeForCharacter<T>)
       │    ├─ ListViewModel<TItem, TColumn, TGrouping> (filter/sort/group pipeline)
       │    │    ├─ AssetsListViewModel
       │    │    ├─ MarketOrdersListViewModel (+ HideInactive, ShowIssuedFor)
       │    │    ├─ ContractsListViewModel (+ HideInactive, ShowIssuedFor)
       │    │    ├─ IndustryJobsListViewModel (+ HideInactive, ShowIssuedFor)
       │    │    ├─ WalletJournalListViewModel
       │    │    ├─ WalletTransactionsListViewModel
       │    │    ├─ MailMessagesListViewModel
       │    │    ├─ NotificationsListViewModel
       │    │    ├─ KillLogListViewModel
       │    │    ├─ PlanetaryListViewModel (+ ShowEcuOnly)
       │    │    └─ ResearchPointsListViewModel
       │    ├─ CharacterMonitorBodyViewModel (page routing)
       │    └─ PlanEditorViewModel (plan + sort state)
       ├─ FormViewModel (IsDirty, Apply, Cancel)
       │    └─ SettingsFormViewModel (SelectedCategory)
       └─ MainWindowViewModel (Characters collection, SelectedCharacter)
```

## Base Classes

### ViewModelBase (145 lines)

Foundation for all ViewModels. Inherits `ObservableObject` from CommunityToolkit.Mvvm for `INotifyPropertyChanged` support. Implements `IDisposable` with automatic subscription tracking.

```csharp
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

    // Test constructor — inject dependencies explicitly
    protected ViewModelBase(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)

    // Production constructor — pulls from AppServices
    protected ViewModelBase()
        : this(AppServices.EventAggregator, AppServices.Dispatcher)
```

Key methods:

| Method | Purpose |
|--------|---------|
| `Subscribe<T>(handler)` | Subscribe to an event via EventAggregator. Auto-tracked for disposal. |
| `Track(disposable)` | Track an external IDisposable for automatic cleanup. |
| `SetPropertyOnUI<T>(ref field, value)` | Set property and raise PropertyChanged on UI thread via Dispatcher. |
| `Dispose()` | Dispose all tracked subscriptions and external disposables. |

The two-constructor pattern is critical for testability. Tests inject a real `EventAggregator` (not mocked — it's simple enough) and a mock `IDispatcher`. Production code uses the parameterless constructor which pulls from `AppServices`.

### CharacterViewModelBase (75 lines)

Adds character-scoping. When `Character` changes, `OnCharacterChanged()` fires for subclass override (ListViewModel uses this to call `Refresh()`).

The key method is `SubscribeForCharacter<T>()`:

```csharp
protected void SubscribeForCharacter<TEvent>(Action<TEvent> handler)
    where TEvent : CharacterEventBase
{
    Subscribe<TEvent>(e =>
    {
        if (_character != null && e.Character == _character)
            handler(e);
    });
}
```

This eliminates the `if (e.Character == _character)` guard that was copy-pasted into every event handler in every control. The VM subscribes once; the base class handles filtering.

### ListViewModel (278 lines)

The pipeline itself. Three type parameters give compile-time safety:

- `TItem` — the domain model (e.g., `MarketOrder`, `Asset`)
- `TColumn` — the sort column enum (e.g., `MarketOrderColumn`, `AssetColumn`)
- `TGrouping` — the grouping mode enum (e.g., `MarketOrderGrouping`, `AssetGrouping`)

Observable properties trigger `Refresh()` on change:

```csharp
public string TextFilter { get; set; }         // "" default
public TGrouping Grouping { get; set; }        // default(TGrouping)
public TColumn SortColumn { get; set; }        // default(TColumn)
public bool SortAscending { get; set; }        // true default
```

Output properties:

```csharp
public IReadOnlyList<ListGrouping<TItem>> GroupedItems { get; }  // filtered + sorted + grouped
public int TotalItemCount { get; }                                // count after filter, before group
```

`Refresh()` runs the full pipeline:

```
GetSourceItems() → filter by MatchesFilter() → sort by CompareItems() → group by GetGroupKey()
```

When no grouping is active (default enum value), all items go into a single group with an empty key. This guarantees `GroupedItems` is never null and always has at least one entry, simplifying control rendering.

`ToggleSort(column)` implements the standard UX: same column reverses direction, different column resets to ascending.

### FormViewModel (74 lines)

Base for dialog forms. `IsDirty` tracking with `MarkDirty()`, and `Apply()`/`Cancel()` methods that call abstract `OnApply()`/`OnCancel()` then reset the dirty flag.

## Binding Helpers

The binding layer lives in `ViewModels/Binding/` and bridges VMs to WinForms controls. All bindings are one-way (VM→Control), auto-marshal to the UI thread, and return `IDisposable` for cleanup.

### PropertyBinding (144 lines)

Extension methods for property-level binding:

```csharp
// Bind VM.Status to label.Text
_vm.BindText(statusLabel, nameof(vm.Status), vm => vm.Status);

// Bind VM.IsReady to button.Visible
_vm.BindVisible(saveButton, nameof(vm.IsReady), vm => vm.IsReady);

// Generic: bind any VM property to any control property
_vm.Bind(progressBar, nameof(vm.Progress), vm => vm.Progress,
    (bar, value) => bar.Value = value);
```

Each binding applies the current value immediately, then subscribes to `PropertyChanged`. The subscription returns an `ActionDisposable` that unsubscribes when disposed. Thread marshaling uses `Control.BeginInvoke()` with guards for disposed controls.

### ListViewBindingHelper (130 lines)

Binds a `ListViewModel<,,>` to a WinForms `ListView`:

```csharp
ListViewBindingHelper.Bind(
    _viewModel,
    lvOrders,
    item => CreateListViewItem(item),     // rendering function
    column => (MarketOrderColumn)column.Tag  // column mapping
);
```

Wires:
- `ListView.ColumnClick` → `ViewModel.ToggleSort(column)`
- `ViewModel.PropertyChanged["GroupedItems"]` → repopulate ListView with groups and items

The repopulation creates `ListViewGroup` instances from `ListGrouping<T>.Key` and `ListViewItem` instances from the rendering function. Thread marshaling via `ListView.BeginInvoke()`.

### CompositeDisposable (96 lines)

Thread-safe `IDisposable` collection. Used by `ViewModelBase` to track all event subscriptions (Law #11 enforcement). Disposes in LIFO order. Safe to call `Dispose()` multiple times. If `Add()` is called after disposal, the added disposable is immediately disposed.

### ActionDisposable (24 lines)

Wraps an `Action` as `IDisposable`. Thread-safe via `Interlocked.Exchange` — the action fires exactly once, no matter how many times `Dispose()` is called.

## UI Integration Pattern

Every control follows the same wiring lifecycle:

```
OnLoad           → Create VM (parameterless constructor)
                 → Set VM.Character
OnVisibleChanged → Sync VM.Character when tab becomes visible
UpdateContent    → Sync UI state → VM properties (TextFilter, SortColumn, etc.)
                 → Read VM.GroupedItems → populate ListView
OnDisposed       → VM.Dispose()
```

### Control Wiring Map

| Control | ViewModel | Extra Properties |
|---------|-----------|-----------------|
| `CharacterAssetsList` | `AssetsListViewModel` | — |
| `CharacterMarketOrdersList` | `MarketOrdersListViewModel` | `HideInactive`, `ShowIssuedFor` |
| `CharacterContractsList` | `ContractsListViewModel` | `HideInactive`, `ShowIssuedFor` |
| `CharacterIndustryJobsList` | `IndustryJobsListViewModel` | `HideInactive`, `ShowIssuedFor` |
| `CharacterWalletJournalList` | `WalletJournalListViewModel` | — |
| `CharacterWalletTransactionsList` | `WalletTransactionsListViewModel` | — |
| `CharacterEveMailMessagesList` | `MailMessagesListViewModel` | — |
| `CharacterEveNotificationsList` | `NotificationsListViewModel` | — |
| `CharacterKillLogList` | `KillLogListViewModel` | — |
| `CharacterPlanetaryList` | `PlanetaryListViewModel` | `ShowEcuOnly` |
| `CharacterResearchPointsList` | `ResearchPointsListViewModel` | — |
| `CharacterMonitorBody` | `CharacterMonitorBodyViewModel` | — |
| `MainWindow` | `MainWindowViewModel` | — |
| `PlanWindow` | `PlanEditorViewModel` | — |
| `SettingsForm` | `SettingsFormViewModel` | — |

### Event Subscriptions by VM

Each list VM subscribes to the events that should trigger a data refresh:

| ViewModel | Character-Scoped Events | Global Events |
|-----------|------------------------|---------------|
| Assets | `CharacterAssetsUpdatedEvent`, `CharacterInfoUpdatedEvent` | `ConquerableStationListUpdatedEvent`, `EveFlagsUpdatedEvent`, `SettingsChangedEvent`, `ItemPricesUpdatedEvent` |
| MarketOrders | `MarketOrdersUpdatedEvent`, `CharacterMarketOrdersUpdatedEvent` | `SettingsChangedEvent`, `ConquerableStationListUpdatedEvent` |
| Contracts | `ContractsUpdatedEvent` | `SettingsChangedEvent`, `ConquerableStationListUpdatedEvent` |
| IndustryJobs | `IndustryJobsUpdatedEvent` | `SettingsChangedEvent`, `ConquerableStationListUpdatedEvent` |
| WalletJournal | `CharacterWalletJournalUpdatedEvent` | `SettingsChangedEvent`, `EveIDToNameUpdatedEvent` |
| WalletTransactions | `CharacterWalletTransactionsUpdatedEvent` | `SettingsChangedEvent`, `ConquerableStationListUpdatedEvent` |
| MailMessages | `CharacterEVEMailMessagesUpdatedEvent`, `CharacterEVEMailBodyDownloadedEvent` | `SettingsChangedEvent`, `EveIDToNameUpdatedEvent` |
| Notifications | `CharacterEVENotificationsUpdatedEvent` | `SettingsChangedEvent` |
| KillLog | `CharacterKillLogUpdatedEvent` | `SettingsChangedEvent` |
| Planetary | `CharacterPlanetaryColoniesUpdatedEvent`, `CharacterPlanetaryLayoutUpdatedEvent` | `SettingsChangedEvent` |
| ResearchPoints | `CharacterResearchPointsUpdatedEvent` | `SettingsChangedEvent`, `ConquerableStationListUpdatedEvent` |

Character-scoped events use `SubscribeForCharacter<T>()` (fires only when `event.Character == vm.Character`). Global events use `Subscribe<T>()` (fires for all characters).

## Test Coverage

### Test Strategy

VMs are testable because they accept `IEventAggregator` via constructor injection. Tests create a real `EventAggregator` (no mocking needed — it's a simple pub/sub), pass it to the VM, and verify behavior by:

1. Setting properties and checking `PropertyChanged` events
2. Calling `Refresh()` and inspecting `GroupedItems`
3. Publishing events and verifying the VM responds
4. Disposing and verifying no more events are received

```csharp
// Test pattern: verify business filter triggers refresh
[Fact]
public void HideInactive_SetTrue_TriggersRefresh()
{
    var vm = new MarketOrdersListViewModel(new EventAggregator());
    bool groupedItemsChanged = false;
    ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(vm.GroupedItems))
            groupedItemsChanged = true;
    };

    vm.HideInactive = true;

    groupedItemsChanged.Should().BeTrue();
    vm.GroupedItems.Should().NotBeNull();
    vm.Dispose();
}
```

### Test Inventory (166 methods across 15 files)

**Base class tests (51 methods):**

| File | Tests | Coverage |
|------|-------|----------|
| `ViewModelBaseTests` | 14 | Constructor null guards, `Subscribe<T>()` auto-tracking, `Track()`, `SetPropertyOnUI<T>()` with/without dispatcher, `Dispose()` unsubscription, double-dispose safety |
| `ListViewModelTests` | 18 | Full pipeline with `TestListViewModel`: filter by text, sort by column, group by category, `ToggleSort()` same/different column, combined operations, empty source, null source |
| `FormViewModelTests` | 8 | `IsDirty` initial/after change, `MarkDirty()`, `Apply()` calls `OnApply()` and resets dirty, `Cancel()` calls `OnCancel()` and resets dirty |
| `CompositeDisposableTests` | 8 | Add/Dispose, LIFO ordering, thread safety, add-after-dispose immediate disposal, `Count`, `IsDisposed` |
| `CharacterMonitorBodyViewModelTests` | 3 | Character binding, page routing, disposal |

**Concrete VM tests (101 methods):**

| File | Tests | Coverage |
|------|-------|----------|
| `ListViewModelInstantiationTests` | 32 | All 11 VMs: instantiation, dispose, refresh-with-no-character, defaults. Cross-cutting: `GroupedItems` never null after refresh, always ≥1 group, safe double-dispose, `PropertyChanged` for TextFilter/SortColumn/SortAscending/Grouping, `ToggleSort` same column reverses/different column resets |
| `AssetsListViewModelTests` | 15 | Deep tests: defaults (TextFilter, SortAscending, Grouping), `PropertyChanged`, `ToggleSort`, event subscription safety (publish with no character, publish after dispose), `GroupedItems` guarantee |
| `MarketOrdersListViewModelTests` | 6 | `HideInactive` default/PropertyChanged/refresh trigger, `ShowIssuedFor` default/PropertyChanged/refresh trigger |
| `ContractsListViewModelTests` | 6 | Same pattern as MarketOrders |
| `IndustryJobsListViewModelTests` | 6 | Same pattern as MarketOrders |
| `PlanetaryListViewModelTests` | 3 | `ShowEcuOnly` default/PropertyChanged/refresh trigger |
| `MainWindowViewModelTests` | 10 | Character collection, SelectedCharacter, refresh on `MonitoredCharacterCollectionChangedEvent` |
| `CharacterMonitorBodyViewModelTests` | 7 | Character binding, page selection, visibility |
| `PlanEditorViewModelTests` | 8 | Plan property, sort column/ascending, `ToggleSort`, `PlanChangedEvent` subscription |
| `SettingsFormViewModelTests` | 8 | Dirty tracking, apply/cancel invocation, `SelectedCategory` |

**Architecture enforcement tests (14 methods):**

| Test | Enforcement |
|------|-------------|
| `AllViewModels_InheritFromViewModelBase` | Type hierarchy correctness |
| `NoViewModel_ReferencesWindowsForms` | Assembly boundary (Law #8) — VMs must not depend on WinForms |
| `AllViewModels_ImplementIDisposable` | Disposal contract (Law #11) |
| `NoViewModel_ExceedsGodObjectLimit` | <100 members per VM (Law #2) |
| `ViewModels_LiveInCorrectNamespace` | `EVEMon.Common.ViewModels.*` only |
| `ViewModelBase_HasSubscribeMethod` | Core infrastructure existence |
| `ViewModelBase_HasTrackMethod` | Core infrastructure existence |
| `ConcreteViewModels_CountIsExpected` | ≥15 VMs exist (catches accidental deletion) |
| `AllListViewModels_HaveMatchingUIControl` | Every list VM maps to a control by name |
| `AllDelegatedControls_RemovedOldFilterMethods` | No `IsTextMatching`, `UpdateSort`, `UpdateContentByGroup` in controls |
| `AllListViewModels_GroupedItems_NeverNull_WhenNoCharacter` | GroupedItems contract (never null, ≥1 group) |
| `ListViewModel_Pipeline_FilterSortGroupContract` | TextFilter/SortColumn/SortAscending/Grouping all trigger Refresh |
| `AllViewModels_WiredInUI_ExceptOrphans` | Every VM has a `_viewModel` field in a UI class |
| `BindingHelpers_DoNotDependOnConcreteViewModels` | Binding layer references only generic `ListViewModel<,,>`, not concrete VMs |

## File Inventory

### Source Files (24 files, 2,332 lines)

```
src/EVEMon.Common/ViewModels/
├── ViewModelBase.cs                             145 lines
├── CharacterViewModelBase.cs                     75 lines
├── ListViewModel.cs                             278 lines
├── FormViewModel.cs                              74 lines
├── MainWindowViewModel.cs                        81 lines
├── CharacterMonitorBodyViewModel.cs              50 lines
├── PlanEditorViewModel.cs                        80 lines
├── SettingsFormViewModel.cs                      53 lines
├── Binding/
│   ├── PropertyBinding.cs                       144 lines
│   ├── ListViewBindingHelper.cs                 130 lines
│   ├── CompositeDisposable.cs                    96 lines
│   ├── ActionDisposable.cs                       24 lines
│   └── WinFormsBindingExtensions.cs              77 lines
└── Lists/
    ├── AssetsListViewModel.cs                   103 lines
    ├── MarketOrdersListViewModel.cs             116 lines
    ├── ContractsListViewModel.cs                107 lines
    ├── IndustryJobsListViewModel.cs             106 lines
    ├── WalletJournalListViewModel.cs             79 lines
    ├── WalletTransactionsListViewModel.cs        79 lines
    ├── MailMessagesListViewModel.cs              78 lines
    ├── NotificationsListViewModel.cs             67 lines
    ├── KillLogListViewModel.cs                   97 lines
    ├── PlanetaryListViewModel.cs                102 lines
    └── ResearchPointsListViewModel.cs            91 lines
```

### Test Files (15 files, 2,706 lines)

```
tests/EVEMon.Tests/
├── Architecture/
│   └── ViewModelArchitectureTests.cs            407 lines  (14 tests)
└── ViewModels/
    ├── ViewModelBaseTests.cs                    224 lines  (14 tests)
    ├── ListViewModelTests.cs                    335 lines  (18 tests)
    ├── FormViewModelTests.cs                    128 lines   (8 tests)
    ├── CompositeDisposableTests.cs              142 lines   (8 tests)
    ├── MainWindowViewModelTests.cs              132 lines  (10 tests)
    ├── CharacterMonitorBodyViewModelTests.cs    142 lines  (10 tests)
    ├── PlanEditorViewModelTests.cs              110 lines   (8 tests)
    ├── SettingsFormViewModelTests.cs             98 lines   (8 tests)
    └── Lists/
        ├── ListViewModelInstantiationTests.cs   434 lines  (32 tests)
        ├── AssetsListViewModelTests.cs          194 lines  (15 tests)
        ├── MarketOrdersListViewModelTests.cs    101 lines   (6 tests)
        ├── ContractsListViewModelTests.cs       101 lines   (6 tests)
        ├── IndustryJobsListViewModelTests.cs    101 lines   (6 tests)
        └── PlanetaryListViewModelTests.cs        57 lines   (3 tests)
```

### Modified Existing Files (18 files, net -775 lines)

```
 CLAUDE.md                                          |  36 +-
 src/EVEMon.Common/EVEMon.Common.csproj             |   1 +    (CommunityToolkit.Mvvm)
 src/EVEMon/CharacterMonitoring/CharacterAssetsList.cs     | -183 lines
 src/EVEMon/CharacterMonitoring/CharacterContractsList.cs  | -120 lines
 src/EVEMon/CharacterMonitoring/CharacterEveMailMessagesList.cs | -130 lines
 src/EVEMon/CharacterMonitoring/CharacterEveNotificationsList.cs | -95 lines
 src/EVEMon/CharacterMonitoring/CharacterIndustryJobsList.cs | -130 lines
 src/EVEMon/CharacterMonitoring/CharacterKillLogList.cs    | rewritten
 src/EVEMon/CharacterMonitoring/CharacterMarketOrdersList.cs | -110 lines
 src/EVEMon/CharacterMonitoring/CharacterMonitorBody.cs    |  +12 lines
 src/EVEMon/CharacterMonitoring/CharacterPlanetaryList.cs  | -160 lines
 src/EVEMon/CharacterMonitoring/CharacterResearchPointsList.cs | rewritten
 src/EVEMon/CharacterMonitoring/CharacterWalletJournalList.cs | -120 lines
 src/EVEMon/CharacterMonitoring/CharacterWalletTransactionsList.cs | -130 lines
 src/EVEMon/MainWindow.cs                           |  +11 lines
 src/EVEMon/SettingsUI/SettingsForm.cs              |   +9 lines
 src/EVEMon/SkillPlanner/PlanWindow.cs              |  +20 lines
 tests/EVEMon.Tests/EVEMon.Tests.csproj             |   +1 line  (EVEMon project reference)
```

## Totals

| | Files | Lines |
|---|---|---|
| VM source code | 24 | 2,332 |
| VM test code | 15 | 2,706 |
| **Total new code** | **39** | **5,038** |
| Existing UI code changed | 18 | net -775 |
| Test methods | | 166 |
| Total tests passing | | 1,201 |

## Design Decisions

### Why Extract Only the Pipeline

All 11 controls were migrated in a single pass — VMs created, old methods deleted, controls rewired, tests written. The scope was deliberately limited to the filter/sort/group pipeline. Rendering logic (creating `ListViewItem` instances, column formatting, tooltips, context menus, virtual mode) stays in the controls where it belongs. The VMs own data transformation; the controls own presentation. This clean split means the VMs are fully testable without a WinForms message loop, while the controls remain responsible for the things that are inherently visual.

### Why CommunityToolkit.Mvvm

Provides `ObservableObject` with `SetProperty()` and `INotifyPropertyChanged` — the same foundation used by WPF and MAUI MVVM. If EVEMon migrates off WinForms, the VMs and binding infrastructure work as-is. The toolkit is a single NuGet with no transitive dependencies.

### Why Three Type Parameters on ListViewModel

`ListViewModel<TItem, TColumn, TGrouping>` uses generic enums for columns and grouping. This gives compile-time type safety:

```csharp
// Compile error: can't compare Assets by MarketOrderColumn
vm.CompareItems(asset1, asset2, MarketOrderColumn.Item);  // ← type error
```

Without generics, you'd need runtime casts and lose the compiler's help.

### Why Test Constructors Instead of AppServices Mocking

The two-constructor pattern avoids `[Collection("AppServices")]` serialization requirements. Tests create independent `EventAggregator` instances — each test is fully isolated, no shared state, no ordering dependencies, no `AppServices.Reset()` teardown. This is why the VM tests run in ~1 second while service tests using `AppServices` take longer.

### Why Architecture Tests via Reflection

Source-level validation (grepping `.cs` files) is fragile — it breaks on comments, string literals, and reformatting. Reflection checks compiled types:

- A method is gone when `GetMethods()` doesn't return it (comments don't count)
- A field exists when `GetFields()` returns it (not when a variable appears in source)
- A type inherits from a base when `IsAssignableFrom()` returns true

The test project references the UI assembly (`EVEMon.csproj`) specifically to enable these checks. This is safe because test→UI is a one-way dependency (the UI never references tests).
