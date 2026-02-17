# Avalonia Migration Assessment

## The Honest Numbers

```
Total WinForms surface area:                    114,613 lines
  ├─ src/EVEMon/ hand-written code:              65,621
  ├─ src/EVEMon/ Designer.cs (auto-generated):   35,191
  ├─ src/EVEMon.Common/Controls/ code:            9,918
  └─ src/EVEMon.Common/Controls/ Designer.cs:     3,883
```

### What Each Category Becomes

**Designer.cs (39,074 lines) → ~6,000 lines of AXAML**

Auto-generated. Every `InitializeComponent()` with manual `this.button1.Location = new Point(12, 43)` positioning becomes declarative AXAML with `Grid`, `StackPanel`, `DockPanel`. A 500-line Designer.cs typically compresses to 30-80 lines of AXAML. The ratio is roughly 6:1.

**Filter/sort/group logic (~10,000 lines removed from controls) → 0 lines**

Already extracted to the VM layer (2,332 lines). The old `IsTextMatching()`, `UpdateSort()`, `UpdateContentByGroup()` methods were deleted from all 11 controls. This work is done. Carries over unchanged.

**Event wiring and lifecycle (~6,000 lines) → ~1,000 lines**

Manual `PropertyChanged += handler`, `Disposed += OnDisposed`, `SubscribeOnUI<T>()`, and `Control.DataBindings.Add()` calls. Avalonia replaces most of this with `{Binding PropertyName}` in AXAML and `[ObservableProperty]` attributes. The 11 list controls that currently have 15-20 lines each of subscription wiring become zero — Avalonia's binding engine handles it declaratively.

**Custom controls (9,918 lines) → ~4,000 lines**

57 controls in EVEMon.Common/Controls/. 3 are unused (delete). ~20 have direct Avalonia equivalents or are trivially replaced (NoFlickerPanel, BorderPanel, SplitContainerMinFixed). ~25 need simple Avalonia rebuilds. 6 need significant work: SkillQueueControl (custom painting), TrayIcon (platform interop), TreeView (1,780-line multi-select), CharacterPortrait (image caching), EveImage (overlay composition), DraggableListView (drag-drop reorder).

**Rendering and presentation (~40,000 lines) → ~20,000 lines of AXAML + code-behind**

This is the real work. `ListViewItem` creation, column formatting, tooltip building, context menus, OwnerDraw handlers, virtual mode. Each control's rendering section becomes an Avalonia `DataTemplate` + `ItemTemplate` + `Styles`. More concise than WinForms, but still manual.

### Realistic New Code

```
39K Designer.cs        → ~6K AXAML layouts
10K filter/sort/group  → 0 (VMs carry over)
 6K event wiring       → ~1K (binding declarations)
10K custom controls    → ~4K Avalonia controls
40K rendering          → ~20K AXAML views + code-behind
 9K Common decoupling  → ~3K interfaces + adapters
─────────────────────────────────────────────────
114K WinForms          → ~34K Avalonia
```

The 114K → 34K ratio is real. Avalonia is dramatically more concise than WinForms for layout, binding, and styling. The compression comes from:
- AXAML replaces manual pixel positioning (6:1 compression)
- Data binding replaces event wiring (6:1 compression)
- Styles/themes replace OwnerDraw handlers (3:1 compression)
- Data templates replace ListViewItem creation (2:1 compression)

---

## Root Cause Analysis: Why WinForms Is Entangled

69 files in EVEMon.Common import `System.Windows.Forms`. The root causes are five specific design decisions, not diffuse coupling:

### Root Cause 1: UI Thread Marshaling via Control.Invoke (5 files)

`Dispatcher.cs` captures `WindowsFormsSynchronizationContext` and uses `Control.Invoke`/`Control.BeginInvoke` for cross-thread marshaling. Every background event that touches UI flows through this.

**Files:** `Dispatcher.cs`, `UIEventSubscriber.cs`, `PropertyBinding.cs`, `WinFormsBindingExtensions.cs`, `ListViewBindingHelper.cs`

**Fix:** Replace `Dispatcher.cs` implementation with Avalonia's `Dispatcher.UIThread`. The `IDispatcher` interface in EVEMon.Core already abstracts this — swap the implementation.

**Depth:** SHALLOW. The interface boundary exists. One file changes.

### Root Cause 2: Dialog Services Embedded in Business Logic (8 files)

Services call `MessageBox.Show()`, `SaveFileDialog`, `OpenFileDialog` directly when they need user confirmation or file paths.

**Files:** `UIHelper.cs`, `FileHelper.cs`, `ListViewExporter.cs`, `KillLogExporter.cs`, `CloudStorageServiceProvider.cs`, `SettingsLoader.cs`, `SettingsMigration.cs`, `Emailer.cs`

**Fix:** Extract `IDialogService` interface with `ShowMessage()`, `ShowSaveDialog()`, `ShowOpenDialog()`. Register WinForms implementation now, Avalonia implementation later. Each file changes ~3-5 lines.

**Depth:** SHALLOW. Pattern is always the same: replace `MessageBox.Show(text, title)` with `_dialogService.ShowMessage(text, title)`.

### Root Cause 3: 29 Visual Components Live in Common (29 files)

Custom controls (`DraggableListView`, `SkillQueueControl`, `TrayIcon`, `CheckedComboBox`, `MultiPanel`, etc.) are in `EVEMon.Common/Controls/` instead of the UI project. This is a historical decision — Common was the only shared assembly.

**Fix:** These stay as WinForms controls during migration. The Avalonia project builds its own controls. Eventually, the WinForms controls become dead code and are deleted. No extraction needed — they're replaced, not migrated.

**Depth:** NONE. Don't touch them. Build Avalonia equivalents from scratch.

### Root Cause 4: System.Drawing.Image in Model Properties (5 files)

`Standing.cs`, `Contact.cs`, `KillLogItem.cs`, `Loyalty.cs`, `EmploymentRecord.cs` have `public Image EntityImage` properties. These models fetch images via `ServiceLocator.ImageService` and cache `System.Drawing.Bitmap` instances.

**Fix:** Change properties to `object?` (matching the `IImageService` interface pattern already established in EVEMon.Core). The Avalonia UI casts to its image type; the WinForms UI casts to `System.Drawing.Image`. Both work through the same `object?` contract.

**Depth:** MEDIUM. 5 files change. Public API changes, so callers update too — but callers are all in the UI layer which is being rewritten anyway.

### Root Cause 5: Window Management via Static Factories (3 files)

`WindowsFactory.cs` uses `Application.OpenForms` and `Form.Show()` to manage window lifecycle. `EVEMonForm.cs` uses `Screen.AllScreens` for position persistence. `UpdateManager.cs` calls `Application.Exit()`.

**Fix:** Extract `IWindowManager`, `IScreenInfo`, `IApplicationLifecycle` interfaces. These have a natural home in EVEMon.Core alongside the existing 19 interfaces.

**Depth:** SHALLOW. 3 interfaces, 3 implementations.

### False Positives (5 files)

5 files import `System.Windows.Forms` but only use `System.Drawing`. Remove the import. Zero code changes.

### Summary

| Root Cause | Files | Depth | Fix |
|---|---|---|---|
| UI thread marshaling | 5 | Shallow | Swap `IDispatcher` implementation |
| Dialog services | 8 | Shallow | Extract `IDialogService` |
| Controls in Common | 29 | None | Replace, don't migrate |
| Image in models | 5 | Medium | Change to `object?` |
| Window management | 3 | Shallow | Extract 3 interfaces |
| False positives | 5 | None | Remove unused import |
| **Total** | **55** | | **14 files actually need code changes** |

The other 14 files (of 69) are extension methods, comparers, settings objects, and infrastructure that need minor adjustments. The coupling is not as deep as the file count suggests.

---

## System.Drawing Contamination

System.Drawing is Windows-only. Every usage must be addressed.

### Clean Layers (zero contamination)

- **EVEMon.Core** — one `IImageService` returning `object?` (intentionally abstract)
- **EVEMon.Models** — zero System.Drawing
- **EVEMon.Data** — zero System.Drawing
- **EVEMon.Infrastructure** — zero System.Drawing
- **EVEMon.Serialization** — zero System.Drawing

### Contaminated Layer: EVEMon.Common

**Image pipeline (3 files, the real problem):**
- `ImageService.cs` — static service, returns `System.Drawing.Image`
- `HttpWebClientService.ImageDownload.cs` — `Image.FromStream()`
- `ImageHelper.cs` — URL generation only, returns `Uri` (actually clean)

**Model image properties (5 files):**
- `Standing.cs`, `Contact.cs`, `KillLogItem.cs`, `Loyalty.cs`, `EmploymentRecord.cs`
- Each has `public Image EntityImage` property
- Loads via `ServiceLocator.ImageService.GetImageAsync()`

**Settings serialization (clean):**
- `SerializableColor.cs` — stores `byte A, R, G, B`, conversion operators at boundary only
- `SerializableRectangle.cs` — stores `int Left, Top, Width, Height`, same pattern
- These are POCOs with edge conversion. No internal Drawing dependency.

**Font/printing (isolated, low priority):**
- `FontFactory.cs` — 6 overloads returning `System.Drawing.Font`
- `PlanPrinter.cs` — `PrintDocument` subclass with GDI+ rendering
- Only consumed by printing feature. Can stay as-is or move to UI project.

### Migration Path for Images

The image pipeline is the deepest System.Drawing dependency. Here's the RCA-level fix:

```
Current:
  ESI API → HttpWebClientService → Image.FromStream() → System.Drawing.Image
    → ImageService cache (PNG files on disk)
    → Model properties (public Image EntityImage)
    → UI controls (PictureBox.Image = model.EntityImage)

Target:
  ESI API → HttpWebClientService → byte[] (raw PNG/JPG bytes)
    → ImageCacheService (same PNG files on disk, but returns byte[])
    → Model properties (public object? EntityImage — or byte[])
    → Avalonia: converts byte[] to Avalonia.Media.Imaging.Bitmap
    → WinForms: converts byte[] to System.Drawing.Image (adapter)
```

The key insight: images are already cached as PNG files on disk. The `Image.FromStream()` call is the only place where `System.Drawing` enters. Replace it with returning raw bytes, and the entire pipeline becomes framework-agnostic.

---

## Bootstrap and Threading

### Current Startup Sequence

```
[STAThread] Main()
  ├─ Exception handlers (4 registered)
  ├─ SplashScreen.Show()
  ├─ Application.DoEvents()           ← forces splash to paint before message loop
  ├─ AppServices.Bootstrap()
  │   ├─ EveMonClient.Initialize()    ← creates global collections
  │   ├─ ServiceRegistration.Configure()
  │   └─ AppServices.SyncToServiceLocator()
  ├─ Settings.Initialize()            ← BLOCKS UI thread (file + potentially network I/O)
  ├─ GlobalDatafileCollection.Load()  ← BLOCKS UI thread (.Wait() on Task.Run)
  ├─ EveIDToName/Station caches       ← BLOCKS UI thread
  ├─ Settings.ImportDataAsync()       ← BLOCKS UI thread
  ├─ MainWindow()
  ├─ SplashScreen.Close()
  └─ Application.Run(mainWindow)      ← message loop starts
```

Three blocking `.Wait()` calls on the UI thread during splash screen. This is safe in WinForms because the message loop hasn't started, but it's an anti-pattern.

### Avalonia Startup

Avalonia uses a different model:

```
Main()
  ├─ BuildAvaloniaApp()
  │   ├─ UsePlatformDetect()
  │   ├─ WithInterFont()
  │   └─ UseFluentTheme(ThemeVariant.Dark)    ← dark mode, one line
  ├─ StartWithClassicDesktopLifetime()
  │   └─ Creates MainWindow
  │       └─ OnOpened() event
  │           ├─ Show loading overlay (not a separate form)
  │           ├─ await AppServices.BootstrapAsync()    ← async, non-blocking
  │           ├─ await Settings.InitializeAsync()
  │           ├─ await GlobalDatafileCollection.LoadAsync()
  │           ├─ Hide loading overlay
  │           └─ Show character tabs
```

The sync-over-async hacks disappear. Everything awaits properly. The loading state is an overlay in the main window, not a separate splash form.

### Threading Model Changes

| Concern | WinForms | Avalonia |
|---|---|---|
| UI thread dispatch | `WindowsFormsSynchronizationContext` | `Dispatcher.UIThread` |
| Marshal to UI | `Control.BeginInvoke()` | `Dispatcher.UIThread.Post()` |
| Timer on UI thread | `System.Windows.Forms.Timer` | `DispatcherTimer` |
| Cross-thread check | `control.InvokeRequired` | `Dispatcher.UIThread.CheckAccess()` |
| Sync context capture | `Dispatcher.Run(Thread.CurrentThread)` | Automatic in Avalonia |

The `IDispatcher` interface in EVEMon.Core already abstracts this. Swap implementation, not architecture.

---

## The Phases

Calibrated to Project Phoenix pace. In the last 3-4 days, Phoenix delivered: 24 source files (2,332 lines), 15 test files (2,706 lines), 18 modified files, 166 test methods, architecture enforcement — roughly 5K lines of quality code with tests per session.

### Phase 0: Common Decoupling (prerequisite, ~3K lines changed)

**What:** Extract 8 interfaces from EVEMon.Core, implement WinForms adapters in EVEMon.Common.

```
New interfaces in EVEMon.Core/Interfaces/:
  IDialogService         — ShowMessage, ShowSaveDialog, ShowOpenDialog, ShowFolderBrowser
  IClipboardService      — SetText, GetText
  IWindowManager         — ShowWindow<T>, GetOpenWindow<T>, CloseAll
  IScreenInfo            — PrimaryScreenBounds, AllScreenBounds
  IApplicationLifecycle  — Exit, Restart
```

Plus update `IDispatcher` to cover all marshaling cases, and change 5 model files to use `object?` for image properties.

**Result:** EVEMon.Common's non-control code no longer imports `System.Windows.Forms` in service/helper files. The 29 control files stay as-is (they're WinForms controls — that's their job).

**Tests:** Interface contract tests, adapter round-trip tests. ~30 tests.

**Size:** Similar to building the VM layer's base classes. One Phoenix session.

**Risk:** LOW. No behavior changes. Just moving the WinForms dependency behind interfaces that already conceptually exist.

### Phase 1: Avalonia Shell + Character Monitoring (the payoff, ~8K lines new)

**What:** New `EVEMon.Avalonia` project. MainWindow with character tabs. All 11 list views binding to existing VMs.

```
EVEMon.Avalonia/
├── App.axaml                    — Application with FluentTheme(Dark)
├── App.axaml.cs                 — Bootstrap sequence (async)
├── MainWindow.axaml             — Tab strip + content area
├── MainWindow.axaml.cs          — Binds to MainWindowViewModel
├── Views/
│   ├── CharacterMonitor.axaml   — Header + body + footer
│   ├── AssetsList.axaml         — DataGrid binding to AssetsListViewModel.GroupedItems
│   ├── MarketOrdersList.axaml   — DataGrid binding to MarketOrdersListViewModel
│   ├── ContractsList.axaml      — ...
│   ├── IndustryJobsList.axaml
│   ├── WalletJournalList.axaml
│   ├── WalletTransactionsList.axaml
│   ├── MailMessagesList.axaml
│   ├── NotificationsList.axaml
│   ├── KillLogList.axaml
│   ├── PlanetaryList.axaml
│   └── ResearchPointsList.axaml
├── Converters/
│   ├── IssuedForConverter.cs
│   ├── ISKFormatConverter.cs
│   └── TimeSpanFormatConverter.cs
├── Controls/
│   ├── SkillQueueBar.axaml      — Canvas-based skill queue visualization
│   └── CharacterPortrait.axaml  — Async image loading control
└── Platform/
    ├── AvaloniaDispatcher.cs    — IDispatcher implementation
    ├── AvaloniaDialogService.cs — IDialogService implementation
    └── TrayIconService.cs       — Platform-specific tray (Windows: NotifyIcon interop)
```

Each list view is ~50-80 lines of AXAML. The VM already provides `GroupedItems`, `TextFilter`, `SortColumn`, `SortAscending`, `TotalItemCount`. The AXAML binds directly:

```xml
<DataGrid ItemsSource="{Binding GroupedItems}"
          SortingColumn="{Binding SortColumn}"
          SortDirection="{Binding SortAscending}">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Item" Binding="{Binding Item.Name}" />
    <DataGridTextColumn Header="Price" Binding="{Binding UnitaryPrice, StringFormat='{}{0:N2} ISK'}" />
  </DataGrid.Columns>
</DataGrid>
```

**Dark mode:** `RequestedThemeVariant="Dark"` in App.axaml. Done.

**Result:** Working Avalonia app with character monitoring. All 11 list views functional with filter/sort/group. Dark mode. The WinForms app continues to work unchanged.

**Tests:** VM tests already pass (166 methods). Add Avalonia-specific integration tests for view rendering. ~20 tests.

**Size:** 2-3 Phoenix sessions. The VM layer eliminates the data logic work — it's pure view construction.

**Risk:** MEDIUM. First Avalonia code. Learning curve for AXAML patterns, Avalonia DataGrid quirks, platform-specific tray icon. But data logic is proven (VMs are tested).

### Phase 2: Skill Planner VM Extraction (~5K lines new)

**What:** Apply the same pattern we used for list views to the skill planner. Extract business logic from `PlanEditorControl.cs` (2,534 lines) and related controls into testable VMs.

The skill planner has these tangled concerns:
- **Plan entry management** — add/remove/reorder skills
- **Prerequisite resolution** — which skills are needed, in what order
- **Training time calculation** — time per skill, total time, remapping impact
- **Attribute optimization** — optimal attribute distribution
- **Drag-drop reordering** — visual reorder → model update
- **Column management** — show/hide/reorder columns
- **Plan import/export** — clipboard, file, EFT format

Each becomes a VM or service:

```
PlanEditorViewModel (extends ListViewModel<PlanEntry, PlanColumn, PlanGrouping>)
  ├─ Plan property (from existing PlanEditorViewModel)
  ├─ AddSkill(StaticSkill, level) → prerequisite resolution
  ├─ RemoveEntries(IEnumerable<PlanEntry>)
  ├─ MoveEntries(indices, targetIndex)
  ├─ TrainingTimeTotal, TrainingTimeSelected
  └─ AttributeOptimization integration

AttributeOptimizerViewModel
  ├─ CurrentAttributes, OptimalAttributes
  ├─ RemappingPoints
  └─ Optimize() → recalculates
```

**Result:** Skill planner logic is testable. PlanEditorControl reduces from 2,534 lines to ~800 (rendering only). Avalonia skill planner view binds to the same VMs.

**Tests:** ~40-60 tests for plan entry management, prerequisite resolution, training time calculation.

**Size:** 2-3 Phoenix sessions. This is the hardest extraction because drag-drop and prerequisite resolution are deeply tangled with the ListView.

**Risk:** HIGH. The plan editor is the most complex control in the app. Prerequisite resolution has subtle ordering constraints. Attribute optimization has mathematical correctness requirements. Thorough testing is critical.

### Phase 3: Settings, Dialogs, Remaining Windows (~6K lines new)

**What:** Port SettingsForm (4,875 lines), notification windows, detail windows, export dialogs.

SettingsForm becomes an Avalonia window with tree navigation (already modeled by `SettingsFormViewModel.SelectedCategory`). Each settings page becomes an AXAML `UserControl` with bindings.

Notification windows (contracts, industry jobs, market orders, planetary pins) are small — 200-400 lines each, mostly data display.

**Result:** Feature parity for settings and auxiliary windows.

**Size:** 1-2 Phoenix sessions.

**Risk:** LOW. Settings pages are mostly checkboxes, dropdowns, text fields — straightforward Avalonia forms.

### Phase 4: Polish, Platform Integration, Cutover (~4K lines new)

**What:** System tray, auto-update, printing (if kept), keyboard shortcuts, accessibility, final testing.

System tray in Avalonia requires platform-specific code. On Windows, wrap `NotifyIcon` via P/Invoke. On Linux/macOS, use `libappindicator` or menu bar integration.

Auto-update needs to work without WinForms dialogs — use Avalonia's async dialog model.

**Result:** Full feature parity. WinForms project can be removed.

**Size:** 1-2 Phoenix sessions.

**Risk:** MEDIUM. Platform-specific tray icon is the main unknown. Printing is niche (can defer).

---

## Risks

### Risk 1: Avalonia DataGrid Maturity

Avalonia's `DataGrid` is functional but less mature than WinForms' `ListView`. Grouping support requires `CollectionViewSource` or manual implementation. Virtual mode for large datasets (assets can be 10,000+ items) needs the `TreeDataGrid` community package.

**Mitigation:** The VM layer's `GroupedItems` output is a flat list of groups — it can feed any data control. If `DataGrid` is insufficient, switch to `TreeDataGrid` or `ItemsRepeater` without changing the VM.

### Risk 2: Skill Planner Extraction Complexity

PlanEditorControl.cs (2,534 lines) mixes drag-drop, prerequisite resolution, column management, and rendering. Extracting the logic without breaking the WinForms version is delicate.

**Mitigation:** Same strangler fig approach: build the VM alongside the existing control, wire it in, verify tests pass, then delete old code. The VM layer for list views proves this works.

### Risk 3: Image Pipeline

5 model classes expose `System.Drawing.Image` as public properties. Changing them to `object?` or `byte[]` ripples through all UI code that reads these properties.

**Mitigation:** The Avalonia UI is new code — it handles whatever type the models expose. The WinForms UI is being replaced. The ripple only matters during the transition period when both UIs coexist.

### Risk 4: Two UIs During Transition

During Phases 1-3, both WinForms and Avalonia projects exist. Changes to VMs or services must work for both. This doubles the testing surface.

**Mitigation:** VMs are framework-agnostic by design. A change to `MarketOrdersListViewModel` works identically in both UIs. The only code that differs is the view layer, and Avalonia views are new (not modified WinForms views).

### Risk 5: EVE Online Is Windows-Only

EVE Online only runs on Windows (or Linux via Proton). Cross-platform EVEMon has limited value if the game itself isn't cross-platform.

**Counterpoint:** (1) Dark mode alone justifies the migration. (2) EVEMon is a monitoring/planning tool — you don't need EVE running to use it. (3) Linux EVE players exist via Proton and want native tools. (4) WinForms is in maintenance mode; Avalonia is actively developed.

---

## Is It Worth It

### What You Get

- **Dark mode** — native, one line of configuration
- **Modern look** — Fluent theme, smooth animations, proper DPI scaling
- **Cross-platform** — Linux, macOS (secondary benefit)
- **3x less UI code** — 34K replacing 114K
- **Testable views** — Avalonia's headless testing mode (no message loop needed)
- **Future-proof** — WinForms is frozen; Avalonia is .NET's path for desktop UI
- **Design system** — consistent spacing, typography, colors via theme (no manual pixel positioning)

### What It Costs

Using Project Phoenix pace as the unit of measure:

| Phase | Phoenix Sessions | What's Delivered |
|---|---|---|
| Phase 0: Common Decoupling | 1 | Services decoupled from WinForms |
| Phase 1: Shell + Character Monitoring | 2-3 | Working app with 11 list views + dark mode |
| Phase 2: Skill Planner | 2-3 | Plan editor fully functional |
| Phase 3: Settings + Dialogs | 1-2 | Feature parity for settings/aux windows |
| Phase 4: Polish + Platform | 1-2 | Tray, updates, cutover |
| **Total** | **7-11 sessions** | **Full Avalonia app, WinForms deleted** |

### The Verdict

The VM layer was the hardest part of the migration, and it's done. It proved the pattern, built the test infrastructure, and eliminated the data logic from the UI. What remains is view construction — repetitive, well-understood, and dramatically compressed by AXAML's declarative model.

Phase 0 + Phase 1 together give you a working dark-mode Avalonia app with the most-used feature (character monitoring) in 3-4 sessions. That's a usable product. Phases 2-4 are incremental feature parity.

The alternative — adding dark mode to WinForms — requires fighting `OwnerDraw` on every control, custom-painting scrollbars, hacking the Win32 non-client area, and maintaining parallel light/dark code paths. And you still have a Windows-only, 2006-era application at the end.

Build the future. Don't maintain the past.
