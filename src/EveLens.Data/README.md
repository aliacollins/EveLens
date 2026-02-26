# EveLens.Data

Enumerations, constants, attributes, extensions. Pure value types. No business logic, no service references.

## What goes here
- Enumerations: `EveAttribute`, `Race`, `Gender`, `BlueprintActivity`, `OrderState`, `SkillFilter`, etc.
- Constants: `EveConstants` (skill IDs, attribute caps), `DBConstants`, `CultureConstants`, `EveMonConstants`
- Custom attributes: `HeaderAttribute`, `UpdateAttribute`, `FullKeyAttribute`
- Extension methods: `StringExtensions`, `TimeExtensions`, `ObjectExtensions`, `HttpExtensions`
- Data interfaces: `ICharacterAttribute`, `IEveMessage`
- Settings DTOs in `SettingsObjects/`
- Datafile XSLT transforms in `Serialization/Datafiles/`

## What does NOT go here
- Service implementations or business logic
- Anything with network calls or I/O
- Types that depend on Models, Infrastructure, or Common

## Key folders
- `Enumerations/` -- 70+ enums covering all EVE game concepts
- `Constants/` -- `EveConstants`, `DBConstants`, `CultureConstants`, `NetworkConstants`
- `Extensions/` -- String, time, object, HTTP helper methods
- `Attributes/` -- Custom attributes for serialization and UI

## Dependencies
- EveLens.Core only
