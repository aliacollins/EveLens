# EveLens.Common

Legacy core assembly. Characters, services, controls, settings, EveLensClient. The facade layer that wires everything together.

**This assembly is shrinking over time** as code moves to Models, Infrastructure, and Data.

## What goes here (for now)
- `AppServices` -- DI facade; registers all adapter services, syncs to ServiceLocator
- `EveLensClient` -- frozen legacy hub (74 static events, global collections, tiered timers)
- `Settings`, `SettingsLoader`, `SettingsMigration`, `SettingsIO` -- settings management
- Adapter services in `Services/` that wrap EveLensClient statics behind Core interfaces
- Character models: `Character`, `CCPCharacter`, `UriCharacter`
- ESI query monitors, collections, factories
- Static data loaders (`StaticSkills`, `StaticItems`, etc.)

## Key files
- `Services/AppServices.cs` -- Central DI facade, creates all service adapters
- `Services/UIEventSubscriber.cs` -- Bridges EventAggregator events to UI thread
- `Services/SettingsSaveSubscriber.cs` -- Auto-saves settings on SettingsChanged events
- `Services/SmartSettingsManager.cs` -- Feature-flagged settings provider
- `EveLensClient.cs` + `EveLensClient.Events.cs` -- Legacy static hub (do not extend)

## Key folders
- `Services/` -- 22 adapter services (Strangler Fig pattern around statics)
- `Models/` -- Character, Plan, ESIKey, Skill, and all sub-models
- `Data/` -- Static data classes (items, skills, blueprints, geography)
- `Helpers/` -- Utility classes (PlanIOHelper, TimeCheck, etc.)

## Dependencies
- All lower assemblies: Core, Data, Serialization, Models, Infrastructure
- Google.Apis (Calendar, Drive), MailKit, NetOfficeFw.Outlook, SharpZipLib, YamlDotNet
