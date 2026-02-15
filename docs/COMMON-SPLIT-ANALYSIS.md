# EVEMon.Common Split Analysis

**Generated:** 2026-02-15 by 6 parallel analysis agents
**Scope:** 906 files in EVEMon.Common → proposed 6-assembly split

---

## Proposed Assembly Structure

```
EVEMon.Core (12 files — already exists)
    ↑
EVEMon.Data (198 files — enums, static game data, datafile DTOs)
    ↑
EVEMon.Serialization (115 files — API/settings DTOs)
    ↑
EVEMon.Models (107 files — domain models, collections, helpers)
    ↑
EVEMon.Infrastructure (73 files — services, networking, query monitors)
    ↑
EVEMon.Common (152 files — EveMonClient, Settings, Controls, glue)
```

## File Distribution

| Assembly | Files | Key Directories |
|----------|-------|-----------------|
| EVEMon.Core | 12 | Interfaces/, Events/ |
| EVEMon.Data | 198 | Data/ (59), Serialization/Datafiles/ (42), Enumerations/ (97) |
| EVEMon.Serialization | 115 | Serialization/Esi/ (87), Eve/ (43), Settings/ (28), others |
| EVEMon.Models | 107 | Models/ (61+25+17+3), Collections/ (16) |
| EVEMon.Infrastructure | 73 | Services/ (15), Service/ (18), Net/ (16), QueryMonitor/ (9), Helpers/ (31), others |
| EVEMon.Common | 152 | Root (8), Controls/ (56), SettingsObjects/ (76), others |

## Circular Dependencies to Break

### Critical Cycles (must resolve before split)

| Cycle | A→B | B→A | Resolution |
|-------|-----|-----|------------|
| **Data ↔ Models** | 15 files | 29 files | Extract `ICharacterSkillProvider` to Core |
| **Models ↔ Service** | 30 files | 9 files | Extract `IESITokenProvider` to Core |
| **QueryMonitor ↔ Models** | 6 files | 3 files | Extract `IMonitoredCharacter` to Core |
| **Data ↔ Serialization** | 43 files | 15 files | Make DTOs dumb — remove static lookups from property getters |

### Acceptable Cycles (keep in same assembly)

| Cycle | Resolution |
|-------|------------|
| Models ↔ Helpers | Keep together — Helpers are de facto Model utilities |
| Models ↔ Collections | Keep together — Collections contain Model instances |

### Not Actually Cycles

| Pair | Direction | Notes |
|------|-----------|-------|
| Models → Serialization | One-way (41 files) | Serialization has 0 files importing Models |

## Interfaces Needed (minimum viable)

1. **`ICharacterSkillProvider`** — `GetSkillLevel(int skillID)`, `GetSkillPoints(int skillID)` — breaks Data → Models
2. **`IESITokenProvider`** — character ID + access token for API calls — breaks Service → Models
3. **`IMonitoredCharacter`** — character identity + monitoring flag — breaks QueryMonitor → Models
4. **`IApplicationPaths`** — `EVEMonDataDir`, `SettingsFilePath`, etc. — reduces EveMonClient coupling
5. **`ITraceService`** — `Trace(string message)` — reduces EveMonClient coupling (87+ files use Trace)

## Implementation Order (phased)

### Phase A: Extract Leaf Types (LOW RISK)
- Move Enumerations/, Constants/, Attributes/, Exceptions/ to EVEMon.Data (or new EVEMon.Shared)
- These have zero reverse dependencies — pure value types
- ~110 files, zero interface extraction needed

### Phase B: Make Serialization DTOs Pure (MEDIUM RISK)
- Remove StaticGeography/StaticItems calls from 15 Serialization DTO property getters
- Move enrichment logic to Model Import() methods
- Eliminates Serialization → Data dependency
- ~15 files changed

### Phase C: Extract Interfaces to Break Cycles (MEDIUM RISK)
- Create ICharacterSkillProvider, IESITokenProvider, IMonitoredCharacter in EVEMon.Core
- Update Data/ classes (Certificate, Mastery, etc.) to use ICharacterSkillProvider
- Update Service/ classes to use IESITokenProvider
- ~25 files changed

### Phase D: Create New Project Files (LOW RISK)
- Create EVEMon.Data.csproj, EVEMon.Serialization.csproj, EVEMon.Models.csproj, EVEMon.Infrastructure.csproj
- Set up project references matching the dependency graph
- Move files to their target assemblies
- Fix namespace using statements

### Phase E: EveMonClient Reduction (HIGH RISK)
- Extract IApplicationPaths, ITraceService
- Reduce EveMonClient from 91-file dependency hub
- This is the hardest phase — deferred until D is validated

## Key Findings

1. **Serialization → Models is NOT a cycle** — clean one-way dependency. Serialization can be extracted first.
2. **Enumerations are universal** — 151 files import them. Must be in the lowest-level assembly.
3. **EveMonClient is referenced by 144 files** (91 Common + 53 UI). It cannot be moved out of Common.
4. **Controls/ has 5 blocking issues** for extraction: EVEMonForm used by satellite projects, UIHelper/PlanPrinter instantiate dialogs, etc.
5. **Models ↔ Helpers should stay together** — CharacterScratchpad extends BaseCharacter, PlanScratchpad extends BasePlan.
6. **15 Serialization DTOs have static data lookups** in property getters — this is the key refactoring needed.

## EveMonClient Hub Analysis

- Referenced by 91 files in EVEMon.Common + 53 in EVEMon UI
- 1,061 total references
- Must stay in EVEMon.Common
- Inseparable types: Global Collections (7), Core Models (5), Settings, Threading, Services
- Separable with interfaces: Serialization, Data, Enumerations, SettingsObjects, Net HTTP
