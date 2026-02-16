# EVEMon.Serialization

All DTOs for ESI API responses, settings XML/JSON, and market data. No logic beyond data shape.

## Serialization formats
- **ESI API**: `DataContractJsonSerializer` for CCP's ESI endpoints
- **Settings (legacy)**: `XmlSerializer` for pre-5.x XML settings files
- **Settings (current)**: `System.Text.Json` for the new JSON settings format
- **Market data**: DTOs for EveMarketer, Fuzzworks, and EveMarketData

## What goes here
- Serializable DTO classes (`SerializableXxx`) that map to API or file formats
- No constructors with logic, no service references, no business rules

## What does NOT go here
- Deserialization logic or file I/O (that lives in Common or Infrastructure)
- Domain model classes (those are in Models)

## Key folders
- `Serialization/Esi/` -- ESI endpoint response DTOs (characters, assets, skills, market, etc.)
- `Serialization/Eve/` -- Static data file DTOs (items, skills, blueprints, certificates)
- `Serialization/Settings/` -- `SerializableSettings`, `SerializableSettingsCharacter`, etc.
- `Serialization/Exportation/` -- Plan export formats
- `Serialization/EveMarketer/`, `Fuzzworks/`, `Osmium/` -- Third-party market DTOs

## Dependencies
- EVEMon.Core, EVEMon.Data
