# ARCHITECTURAL LAWS (NEVER VIOLATE)

These rules exist because EVEMon spent 15 years as an untestable monolith.
Project Phoenix fixed it. These laws prevent regression.

821 tests enforce these laws. Break a law, break the tests.

---

## Law 1: No Static State

Never create static mutable state. No static collections, no static service
references, no static event handlers outside of AppServices and ServiceLocator
(which are the designated transition points).

```csharp
// FORBIDDEN
public static class MyNewService
{
    private static List<Character> _characters = new();
    public static void Add(Character c) => _characters.Add(c);
}

// CORRECT
public class MyNewService
{
    private readonly ICharacterRepository _characters;
    public MyNewService(ICharacterRepository characters)
    {
        _characters = characters;
    }
}
```

If you need global access, add it to AppServices with an interface.

## Law 2: No God Objects

No class should be referenced by more than 30 files. If a new class is growing
beyond 500 lines or accumulating responsibilities, split it before merging.

Warning signs of a god object forming:
- Class has more than 5 public methods unrelated to each other
- Class owns collections AND dispatches events AND does I/O
- Multiple unrelated features depend on it
- You need to modify it for every new feature

## Law 3: Dependencies Flow Down

```
EVEMon.Core → EVEMon.Data → EVEMon.Serialization → EVEMon.Models → EVEMon.Infrastructure → EVEMon.Common → EVEMon
```

Never reference an assembly to the right from one on the left. If EVEMon.Data
needs something from EVEMon.Models, extract an interface into EVEMon.Core.

If the build breaks with a circular reference, you put a file in the wrong
assembly. Move it or extract an interface. Never add a reverse project reference.

`AssemblyBoundaryTests.cs` enforces this at test time with reflection.

## Law 4: New Services Must Be Testable

Every new service must:
1. Accept dependencies through constructor parameters (interfaces, not concrete types)
2. Have at least one unit test
3. Work with test doubles (no requirement on EveMonClient, Settings, or static state)

```csharp
// CORRECT — testable
public class SkillFarmCalculator
{
    private readonly ICharacterRepository _characters;

    public SkillFarmCalculator(ICharacterRepository characters)
    {
        _characters = characters;
    }

    public decimal CalculateMonthlyYield(long characterId) { ... }
}

// TEST
[Fact]
public void CalculateMonthlyYield_WithMaxSkills_ReturnsExpected()
{
    var repo = Substitute.For<ICharacterRepository>();
    var calc = new SkillFarmCalculator(repo);
    // ...
}
```

## Law 5: Events Through EventAggregator Only

Never add static events. Never subscribe to EveMonClient events in new code.
All new event communication goes through EventAggregator with typed events.

```csharp
// FORBIDDEN
public static event EventHandler MyNewEvent;

// FORBIDDEN
EveMonClient.SomeEvent += handler;

// CORRECT — define event in EVEMon.Common/Events/CommonEvents.cs
public sealed class FleetCompositionChangedEvent
{
    public Fleet Fleet { get; }
    public FleetCompositionChangedEvent(Fleet fleet) => Fleet = fleet;
}

// CORRECT — publish
AppServices.EventAggregator?.Publish(new FleetCompositionChangedEvent(fleet));

// CORRECT — subscribe in UI (stores IDisposable, disposes on close)
_sub = AppServices.EventAggregator?.SubscribeOnUI<FleetCompositionChangedEvent>(
    this, e => UpdateFleetView(e.Fleet));
```

## Law 6: Lazy by Default

Collections and expensive objects in constructors must use Lazy<T> or be
created on first access. Constructors should be fast and side-effect free.

```csharp
// FORBIDDEN — 21 collections per character * 100 characters = instant lag
public MyCharacterView(Character character)
{
    _walletHistory = new WalletHistoryCollection(character);
    _assets = new AssetCollection(character);
}

// CORRECT
public MyCharacterView(Character character)
{
    _walletHistory = new Lazy<WalletHistoryCollection>(
        () => new WalletHistoryCollection(character));
}
```

## Law 7: No Sync-Over-Async

Never call .Result, .Wait(), or .GetAwaiter().GetResult() on tasks except
in Program.cs startup where there is no async context.

```csharp
// FORBIDDEN
var data = GetDataAsync().Result;
GetDataAsync().Wait();

// CORRECT
var data = await GetDataAsync().ConfigureAwait(false);
```

## Law 8: Right File, Right Assembly

Before creating a new file, check which assembly it belongs in:

| File type | Assembly | Rule |
|-----------|----------|------|
| Interface (no EVEMon deps) | EVEMon.Core | Always |
| Interface (with domain types) | EVEMon.Common/Interfaces | When it references models |
| Enum/constant | EVEMon.Data | Always |
| API/Settings DTO | EVEMon.Serialization | Always |
| Domain model | EVEMon.Models or EVEMon.Common/Models | Common if it references services |
| Service implementation | EVEMon.Common/Services | Register in AppServices |
| Event type (timer) | EVEMon.Core/Events | If no domain dependencies |
| Event type (domain) | EVEMon.Common/Events | If carries Character/model data |
| UI form/control | EVEMon (UI project) | Never in Common |
| Test | tests/EVEMon.Tests/ | Mirror the source directory |

If you're unsure, put it in EVEMon.Common. Moving files down the dependency
chain is easy. Moving them up (toward Core) requires extracting interfaces.

## Law 9: EveMonClient Is Frozen

Do not add methods, properties, events, or state to EveMonClient. It is a
legacy startup shell being phased out. If you need new functionality:

- New service → AppServices + interface in Core
- New event → CommonEvents.cs + EventAggregator
- New collection → own class registered in AppServices
- New setting → SettingsObjects/ folder
- New static property → AppServices (e.g., AppServices.IsDebugBuild)

The only code allowed to touch EveMonClient directly:
- `Program.cs` (bootstrap lifecycle)
- `AppServices.cs` (the facade wrapping it)
- Adapter classes (TraceServiceAdapter, ApplicationPathsAdapter)
- `EveMonClient.cs` / `EveMonClient.Events.cs` themselves

## Law 10: All Async Void Must Have Try/Catch

WinForms requires `async void` for event handlers. Every `async void` method
must wrap its entire body in try/catch. Unhandled exceptions in async void
crash the application with no stack trace.

```csharp
private async void button_Click(object? sender, EventArgs e)
{
    try
    {
        await DoWorkAsync();
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Async error in button_Click: {ex}");
    }
}
```

## Law 11: Event Subscriptions Must Be Disposed

Every `EventAggregator.Subscribe*()` call must store the returned `IDisposable`
and dispose it when the subscriber's lifetime ends.

```csharp
private IDisposable? _tickSub;

// Subscribe
_tickSub = AppServices.EventAggregator?.SubscribeOnUI<FiveSecondTickEvent>(
    this, e => OnTick());

// Dispose (in Dispose/OnFormClosing)
_tickSub?.Dispose();
_tickSub = null;
```

Failure to dispose causes memory leaks and ghost handlers on disposed controls.

## Law 12: Tests Prove Behavior

When adding a feature or fixing a bug:
1. Write a test that demonstrates the expected behavior
2. If fixing a bug, write a test that reproduces it first
3. Verify the test fails before the fix and passes after
4. Commit the test with the code

No feature is complete without a test. No bug fix is complete without a
regression test. The test suite is the specification.

Tests that mutate `AppServices` must use `[Collection("AppServices")]`.

## Law 13: Serialization DTOs Are the Data Contract

`SerializableCCPCharacter`, `SerializableESIKey`, `SerializableSettings`,
and the JSON DTOs (`JsonConfig`, `JsonCredentials`, `JsonCharacterData`)
define the contract with the file system. Changes to these types must
include round-trip tests (XML and/or JSON) to prove no data loss.

## Law 14: No Direct EveMonClient Access from UI

UI code (forms, controls in the `EVEMon` project) must access everything
through `AppServices`:

```csharp
// FORBIDDEN in UI code
EveMonClient.Characters
EveMonClient.IsDebugBuild
EveMonClient.FileVersionInfo

// CORRECT
AppServices.Characters
AppServices.IsDebugBuild
AppServices.FileVersionInfo
```

If `AppServices` doesn't expose what you need, add a forwarding property —
do not bypass it.
