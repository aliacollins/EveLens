# Contributing to EveLens

EveLens is an open source character monitoring and skill planning tool for EVE Online. It's built on .NET 8 with Avalonia UI, runs on Windows, macOS, and Linux, and is licensed under GPL v2.

This guide will help you get started if you want to contribute.

---

## The Most Valuable Contribution Right Now

Bug reports. Seriously.

EveLens is in beta and the single best thing you can do is use it and tell us what breaks. File an issue at [github.com/aliacollins/evelens/issues](https://github.com/aliacollins/evelens/issues) with:

- What happened
- What you expected
- Steps to reproduce (if you can)
- Screenshots or crash logs

If you're running 10+ characters, your bug reports are especially valuable -- that's where edge cases live.

---

## Before You Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A C# IDE -- [Visual Studio 2022](https://visualstudio.microsoft.com/), [JetBrains Rider](https://www.jetbrains.com/rider/), or [VS Code](https://code.visualstudio.com/) with the C# Dev Kit extension
- Git
- An EVE Online account with ESI access (for testing authenticated features)

### Clone and Build

```bash
git clone https://github.com/aliacollins/evelens.git
cd evelens
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

There are **1,700+ tests** with over **1,400 architecture enforcement tests** that validate assembly boundaries, dependency direction, naming conventions, and structural integrity. All tests must pass before submitting a PR. The architecture tests are not optional -- they are the guardrails that prevent regression to the monolithic patterns of the original codebase.

### Run the App

```bash
dotnet run --project src/EveLens.Avalonia/EveLens.Avalonia.csproj
```

---

## Understanding the Architecture

**Read this section before writing code.** The architecture is intentional and enforced. Code that violates these patterns will not pass tests.

EveLens was rewritten from a monolithic WinForms application (114,000 lines with static god objects, circular dependencies, and 74 static events) into a clean, layered architecture. The 14 Architectural Laws exist to prevent regression.

### Assembly Hierarchy

Dependencies flow **downward only**. This is enforced by `AssemblyBoundaryTests` using DFS cycle detection at build time.

```
EveLens.Avalonia (UI layer -- Avalonia views, platform adapters)
    |
EveLens.Common (services, settings, ViewModels, static facades)
    |
    +-- EveLens.Core (interfaces only, zero dependencies -- the leaf)
    +-- EveLens.Data (enums, constants, static game data)
    +-- EveLens.Serialization (ESI/EVE/Settings DTOs)
    +-- EveLens.Models (domain models -- Character, Skill, Plan)
    +-- EveLens.Infrastructure (EventAggregator, logging, services)
```

**No circular dependencies.** If assembly A depends on assembly B, assembly B must never depend on assembly A, directly or transitively. The tests will catch it.

### The 14 Architectural Laws

Every PR is checked against these. Break one and the test suite fails.

| # | Law | What It Means |
|---|-----|--------------|
| 1 | **No Static State** | Never create static mutable state. Use `AppServices` + interfaces. |
| 2 | **No God Objects** | No class >500 lines or referenced by >30 files. Split early. |
| 3 | **Dependencies Flow Down** | `Core -> Data -> Serialization -> Models -> Infrastructure -> Common -> EveLens`. Never reverse. |
| 4 | **New Services Must Be Testable** | Constructor injection, interfaces, test doubles. No dependency on `EveLensClient`/`Settings` statics. |
| 5 | **Events Through EventAggregator Only** | Never add static events. Always `AppServices.EventAggregator?.Subscribe<T>()`. |
| 6 | **Lazy by Default** | Collections and expensive objects use `Lazy<T>`. Constructors must be fast. |
| 7 | **No Sync-Over-Async** | Never `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` except in `Program.cs` bootstrap. |
| 8 | **Right File, Right Assembly** | Interfaces go in Core. Enums go in Data. DTOs go in Serialization. Services go in Common. UI goes in EveLens.Avalonia. |
| 9 | **EveLensClient Is Frozen** | Never add to it. New service -> `AppServices`. New event -> `EventAggregator`. New property -> `AppServices`. |
| 10 | **All Async Void Must Have Try/Catch** | Wrap the entire body in try/catch. Unhandled exceptions in async void crash the app. |
| 11 | **Event Subscriptions Must Be Disposed** | Store the `IDisposable` from `Subscribe`, dispose on form close or control detach. |
| 12 | **Tests Prove Behavior** | No feature without a test. No bug fix without a regression test. Commit tests with code. |
| 13 | **Serialization DTOs Are the Data Contract** | Changes to `SerializableSettings`, `JsonConfig`, etc. require round-trip tests. |
| 14 | **No Direct EveLensClient Access from UI** | Use `AppServices.Characters` not `EveLensClient.Characters`. |

### Key Components

**`AppServices`** (`src/EveLens.Common/Services/AppServices.cs`) -- Static DI facade. Lazy-initializes 20+ services. Overridable via `Set*()` methods for testing. `Reset()` for test isolation.

**`EventAggregator`** (`src/EveLens.Infrastructure/Services/EventAggregator.cs`) -- Sole event delivery mechanism. Supports strong and weak references. Thread-safe. All communication between components goes through this.

**`Settings`** -- Static partial class. JSON is source of truth. `SmartSettingsManager` coalesces saves to avoid disk thrashing.

### UI Architecture

The UI uses Avalonia with the MVVM pattern:

- **Views** live in `src/EveLens.Avalonia/Views/`
- **ViewModels** live in `src/EveLens.Common/ViewModels/` (shared, testable, no Avalonia references)
- **Display wrappers** (Avalonia-specific colors/brushes) may live in `src/EveLens.Avalonia/ViewModels/` but contain zero business logic
- Data binding connects Views to ViewModels -- minimal code-behind
- 6 dark mode theme palettes defined in `src/EveLens.Avalonia/Themes/`

---

## How to Contribute

### Picking Something to Work On

1. **Issues labeled `good first issue`** -- scoped, understood, and ready for someone new.
2. **Issues labeled `help wanted`** -- bigger items where contribution is welcome.
3. **Bug fixes** -- always welcome. If you found it and know how to fix it, go for it.

If you want to add a new feature or make a significant change, **open an issue first** to discuss the approach. This saves everyone time -- especially you.

### Branching and Workflow

All work happens on feature branches created from `alpha`. Never commit directly to `alpha`, `beta`, or `main`.

```bash
# Start new work
git checkout alpha && git pull
git checkout -b feature/my-feature    # for features
git checkout -b fix/issue-description # for bug fixes

# Do your work, commit, push
git push origin feature/my-feature

# Submit a pull request targeting alpha
```

**Branch naming:** Use `feature/`, `fix/`, or `experimental/` prefix.

**Protected branches:** `alpha`, `beta`, and `main` are protected. Only the maintainer promotes code between them using the promotion system.

### Adding a Feature (End-to-End)

Follow this sequence. Skip steps that don't apply.

1. **Define the interface** in `src/EveLens.Core/Interfaces/`
2. **Define event types** in `src/EveLens.Core/Events/` (timer events) or `src/EveLens.Common/Events/` (domain events)
3. **Implement the service** in `src/EveLens.Common/Services/`
4. **Register in `AppServices.cs`** with a `Lazy<T>` property and `Set*()` override for testing
5. **Subscribe in UI** via `SubscribeOnUI<T>()`, store the `IDisposable`, dispose on detach
6. **Write tests** using `NullCharacterServices` for characters, `new EventAggregator()` for events, `Substitute.For<T>()` for mocks
7. **Run the full test suite** -- all 1,700+ tests must pass
8. **Update CHANGELOG.md** -- add your change to the `[Unreleased]` section

### Fixing a Bug

1. **Write a failing test** that reproduces the bug
2. **Fix the code** so the test passes
3. **Run the full suite** to make sure nothing else broke
4. **Update CHANGELOG.md** under `### Fixed`

---

## Testing

### Test Organization

| Directory | Focus |
|-----------|-------|
| `Architecture/` | Assembly boundary validation, dependency direction, naming rules |
| `Integration/` | EventAggregator E2E, character scaling, settings persistence |
| `Models/` | Character state, ESI keys, plans, market orders, skills |
| `Services/` | AppServices routing, ServiceLocator sync, SmartSettingsManager |
| `Settings/` | Round-trip serialization, migration detection, loader paths |
| `Regression/` | Crash prevention, data integrity, GitHub issue reproductions |
| `Serialization/` | ESI DTOs, character DTOs, settings DTOs |
| `ViewModels/` | ViewModel behavior, filtering, grouping, sorting |

### Test Patterns

```csharp
// Isolated character (no EveLensClient needed)
var services = new NullCharacterServices();
var identity = new CharacterIdentity(1L, "Test Pilot");
var character = new CCPCharacter(identity, services);

// Service tests with mocks
var aggregator = new EventAggregator(); // real, not mocked
var dispatcher = Substitute.For<IDispatcher>();
dispatcher.When(d => d.Invoke(Arg.Any<Action>()))
    .Do(ci => ci.ArgAt<Action>(0).Invoke());

// Event verification
aggregator.Subscribe<CharacterUpdatedEvent>(e => received = true);
aggregator.Publish(new CharacterUpdatedEvent(character));
received.Should().BeTrue();
```

### Test Collections

Tests that mutate `AppServices` shared state use `[Collection("AppServices")]` to prevent parallel execution conflicts. Apply this attribute when your tests call `AppServices.Set*()`, `Reset()`, or `SyncToServiceLocator()`.

---

## Code Style and Conventions

- **Follow the existing code.** Consistency matters more than personal preference. Look at nearby files for conventions.
- **Use interfaces from `EveLens.Core`** for dependency injection. Don't create tight coupling.
- **Keep ViewModels testable.** No Avalonia references in `EveLens.Common/ViewModels/`.
- **Name things clearly.** If a class name needs a comment to explain what it does, rename it.
- **Don't over-engineer.** Three similar lines of code is better than a premature abstraction. Only add complexity when it's needed now, not when it might be needed later.
- **Events go through EventAggregator.** Never add static events or wire events directly between components.
- **Dispose subscriptions.** Every `Subscribe<T>()` call returns an `IDisposable`. Store it. Dispose it on close/detach. Leaking subscriptions causes crashes.

### Changelog

Every commit that changes user-visible behavior must update `CHANGELOG.md`. Write entries in the `[Unreleased]` section.

- Write for humans, not machines -- explain what changed and why
- Bold the feature name so readers can scan quickly
- One entry per user-visible change, not per file or commit
- No internal details (class names, file names, test counts)
- Categories: **Added**, **Changed**, **Fixed**, **Removed**, **Deprecated**, **Security**

---

## ESI and EVE Online Data

EveLens integrates with CCP's EVE Swagger Interface (ESI) for live character data. If you're working on ESI-related features:

- **Respect CCP's rate limits.** The scheduler (`SmartQueryScheduler`) handles this -- don't bypass it.
- **Never log or expose ESI tokens** or character data in debug output.
- **Test with your own ESI keys.** Don't hardcode credentials.
- **Scope control is mandatory.** If you're adding a new ESI endpoint, make sure it's behind scope control -- users choose what data EveLens can access.
- **Handle errors gracefully.** ESI endpoints go down. Use the health tracking system (`EndpointHealthTracker`) rather than spamming error notifications.

---

## Brand Identity

- The product is always **EveLens** (capital E, capital L, no spaces)
- The original project is **EVEMon** when referenced historically
- The executable is `EveLens.exe`
- Namespaces use `EveLens.*`

---

## PR Guidelines

### What Makes a Good PR

- **Small and focused.** One logical change per PR.
- **Well-tested.** New functionality has tests. Bug fixes have regression tests.
- **Clearly described.** What you changed, why, and how to test it.
- **Passes all tests.** Including architecture boundary tests.
- **Updates CHANGELOG.md** if the change is user-visible.

### What Not to Submit

- PRs without tests for new functionality
- PRs that break the assembly boundary rules
- Cosmetic-only refactors across many files (high noise, low signal)
- Features that weren't discussed in an issue first
- Changes to `EveLensClient` (it's frozen -- Law 9)
- Static events (Law 5)
- Sync-over-async patterns (Law 7)

### Review Process

Alia reviews all PRs. Be patient -- this is a community project, not a company with a review team. PRs that are small, focused, well-tested, and clearly described get reviewed fastest.

A PR that does one thing well beats a PR that does five things at once.

---

## Community

- Be constructive. Bug reports, feature suggestions, and code contributions are all welcome.
- Be respectful. We're all here because we care about EVE tools.
- If you have questions about the codebase or architecture, open a discussion or ask in an issue. There are no dumb questions about a codebase this size.

---

## License

By contributing to EveLens, you agree that your contributions will be licensed under GPL v2, consistent with the rest of the project.

---

Fly safe. o7
