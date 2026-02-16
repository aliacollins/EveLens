# EVEMon.Models

Domain model base classes and collection infrastructure. The "nouns" of the application.

## What goes here
- Model base classes extracted from Common: `AccountStatus`, `BoosterInfo`, `WalletJournal`, etc.
- Typed collection classes: `ReadonlyCollection<T>`, `ReadonlyKeyedCollection<T>`, `FastList<T>`
- Collection extensions and comparers
- Helper classes: `CharacterAttributeScratchpad`, `FormatHelper`, `TaskHelper`
- Enum extension methods

## What does NOT go here
- Service implementations (those go in Infrastructure or Common/Services)
- Full character models (`Character`, `CCPCharacter`) -- those are still in Common
- Anything that references EVEMon.Common or EVEMon.Infrastructure

## Key folders
- `Models/` -- Domain model classes (API method descriptors, character sub-models)
- `Models/Extended/` -- Extended model classes
- `Collections/` -- Generic typed collections used throughout the app
- `Helpers/` -- `CharacterAttributeScratchpad`, `FormatHelper`, `TaskHelper`
- `Extensions/` -- `EnumExtensions`

## Dependencies
- EVEMon.Core, EVEMon.Data, EVEMon.Serialization
- YamlDotNet (for YAML-based data loading)
- `InternalsVisibleTo`: EVEMon.Common, EVEMon, EVEMon.Tests
