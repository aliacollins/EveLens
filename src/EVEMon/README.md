# EVEMon

WinForms UI application. Forms, controls, dialogs. Entry point is `Program.cs`.

## Startup sequence
1. `Program.cs` -- Application entry point, calls `ServiceRegistration.Configure()`
2. `ServiceRegistration.cs` -- DI composition root, builds `IServiceProvider` from `AppServices`
3. `MainWindow.cs` -- Primary form, hosts character monitors and tray icon

## Key pattern: EventAggregator subscriptions
All forms and controls subscribe to typed events via `IEventAggregator.SubscribeOnUI<T>()`.
Store the returned `IDisposable` and dispose it when the form closes:
```csharp
_subscriptions.Add(eventAggregator.SubscribeOnUI<SecondTickEvent>(_ => UpdateClock()));
// In Dispose or FormClosing:
foreach (var sub in _subscriptions) sub.Dispose();
```

## Key folders
- `CharacterMonitoring/` -- Per-character tabs (skills, assets, orders, contracts, etc.)
- `SkillPlanner/` -- Plan editor, attribute optimizer, implant calculator
- `SettingsUI/` -- Settings dialog, tray popup, tray tooltip
- `ApiCredentialsManagement/` -- ESI OAuth login flow
- `Controls/` -- Reusable controls (OverviewItem, ReadingPane, etc.)
- `CharactersComparison/` -- Side-by-side character comparison window

## Dependencies
- EVEMon.Common (and transitively Core, Data, Serialization, Models, Infrastructure)
- EVEMon.PieChart, EVEMon.Watchdog, EVEMon.LogitechG15, EVEMon.WindowsApi
- Microsoft.Extensions.DependencyInjection
