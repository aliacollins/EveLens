# EVEMon.Tests

774 tests across 51 files. xUnit 2.9 + FluentAssertions 6.12 + NSubstitute 5.1.

## Running tests
```bash
dotnet test tests/EVEMon.Tests/EVEMon.Tests.csproj
```

## Sequential execution
All tests run sequentially (`parallelizeTestCollections: false` in `xunit.runner.json`).
EVEMon has shared static state (`EveMonClient`, `AppServices`, `ServiceLocator`) that
is not safe for parallel execution.

## Key patterns

**AppServices reset**: Most service tests call `AppServices.Reset()` in their constructor
to get a clean slate. Tests that mutate AppServices use `[Collection("AppServices")]`
to prevent interleaving.

**NullCharacterServices**: `TestDoubles/NullCharacterServices.cs` provides null-object
implementations of `ICharacterReader`, `ICharacterWriter`, etc. for tests that need
a character but don't care about its data.

**Collection attribute**: `[Collection("AppServices")]` groups tests that share the
static `AppServices` state. Defined in `AppServicesTestCollection.cs`.

## Test organization
- `Services/` -- AppServices, EventAggregator, adapters, SmartSettingsManager, schedulers
- `Models/` -- Character attributes, plans, skill queues, contracts, ESI keys
- `Settings/` -- Serialization, migration, round-trip, loader tests
- `Serialization/` -- ESI and character DTO serialization
- `Integration/` -- End-to-end EventAggregator, rate limiting, settings persistence
- `Regression/` -- GitHub issue reproductions, crash prevention, data integrity
- `QueryMonitor/` -- Query monitor behavior and scheduling
- `Helpers/` -- SettingsFileManager, UpdateBatcher
- `Net/` -- HttpWebClientService tests
- `Architecture/` -- Assembly boundary enforcement tests
