# EveLens.Core

Interfaces, events, and ServiceLocator. **Zero dependencies** on other EveLens assemblies. Every other assembly depends on this.

## What goes here
- Interfaces for services (`IEventAggregator`, `IDispatcher`, `ICharacterRepository`, etc.)
- Typed event classes (`SecondTickEvent`, `FiveSecondTickEvent`, `CharacterUpdatedEvent`)
- `ServiceLocator` -- static bridge for assemblies that can't reference EveLens.Common
- `ICharacterIdentity`, `IStation` -- minimal model interfaces

## What does NOT go here
- Implementations (those go in Infrastructure or Common/Services)
- Anything that references EveLens.Data, Models, or Common types
- Business logic of any kind

## Key files
- `Interfaces/` -- 22 service interfaces, all with XML docs
- `Events/TimerEvents.cs` -- SecondTick, FiveSecondTick, ThirtySecondTick (singleton instances)
- `Events/CoreEvents.cs` -- SettingsChanged, CharacterUpdated, ServerStatusUpdated, etc.
- `Events/CharacterLifecycleEvents.cs` -- CharacterCreated, CharacterDisposed
- `ServiceLocator.cs` -- Synced by AppServices.SyncToServiceLocator() at startup

## Dependencies
None. This is the dependency root.
