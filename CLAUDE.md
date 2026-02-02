# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Promotion System (CRITICAL)

**NEVER push directly to main, alpha, or beta branches.** Use the promotion system:

```powershell
# Promote to alpha (from any branch)
.\scripts\promote.ps1 alpha -Message "Added feature X"

# Promote to beta (usually from alpha)
.\scripts\promote.ps1 beta -Message "Ready for beta testing"

# Promote to stable/main (from alpha or beta)
.\scripts\promote.ps1 stable -Message "Production release"
```

The promote script automatically:
- Increments version in `SharedAssemblyInfo.cs`
- Updates `CHANGELOG.md`
- Updates `updates/patch-*.xml`
- Creates standardized commit message
- Merges and pushes to target branch

**Version flow:**
```
5.2.0-alpha.1 → 5.2.0-alpha.2 → 5.2.0-beta.1 → 5.2.0 (stable)
```

## Branch Workflow

| Branch | Purpose | Push Method |
|--------|---------|-------------|
| `feature/*` | Development work | `git push` (direct OK) |
| `alpha` | Alpha testing | `promote.ps1 alpha` only |
| `beta` | Beta testing | `promote.ps1 beta` only |
| `main` | Stable releases | `promote.ps1 stable` only |

**Typical workflow:**
1. Create feature branch: `git checkout -b feature/my-feature`
2. Make changes, commit locally
3. When ready for alpha: `.\scripts\promote.ps1 alpha -Message "description"`
4. Test in alpha, fix issues, promote again
5. When stable: `.\scripts\promote.ps1 stable -Message "description"`

## Build Commands

```bash
# Build
dotnet build EVEMon.sln -c Debug

# Run
dotnet run --project src/EVEMon/EVEMon.csproj

# Build release
dotnet publish src/EVEMon/EVEMon.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64

# Build installer (after promote to create release)
.\scripts\release-stable.ps1 5.2.0   # For stable
.\scripts\release-beta.ps1           # For beta
```

## Commit Guidelines

- **NEVER push to protected branches directly** - A pre-push hook will block you
- **NEVER commit without explicit user approval** - Wait for confirmation
- **No Claude attribution** - Do not add "Co-Authored-By: Claude" to commits
- **Batch related changes** - Group into single meaningful commits

## Architecture

### EveMonClient - Central Controller
`EveMonClient.cs` is a static "god object" - the application's hub:
- **74 static events** for UI updates (`EveMonClient.Events.cs`)
- **Global collections**: `Characters`, `ESIKeys`, `MonitoredCharacters`
- **Tiered timers**: `SecondTick` (1s), `FiveSecondTick` (5s), `ThirtySecondTick` (30s)

### Data Flow
```
ESI API → QueryMonitor → Model (CCPCharacter) → EveMonClient.OnXxx() → UI event subscription
```

### Core Models
- `Character.cs` - Base character with skills, attributes, plans
- `CCPCharacter.cs` - ESI-connected character (extends Character)
- `ESIKey.cs` - OAuth tokens and character identity
- `BasePlan.cs` / `Plan.cs` - Skill training plans

### Settings
- `Settings.cs` - Main manager, handles XML→JSON migration
- `SettingsFileManager.cs` - JSON file I/O
- Location: `%APPDATA%\EVEMon\`

## Versioning

| Type | AssemblyVersion | Display |
|------|-----------------|---------|
| Stable | `5.2.0.0` | `5.2.0` |
| Alpha | `5.2.0.1` | `5.2.0-alpha.1` |
| Beta | `5.2.0.2` | `5.2.0-beta.2` |

Edit `SharedAssemblyInfo.cs` directly only for manual overrides. Normally use `promote.ps1`.

## Update Channels

| Channel | File | Auto-updated by |
|---------|------|-----------------|
| Stable | `updates/patch.xml` | `promote.ps1 stable` |
| Beta | `updates/patch-beta.xml` | `promote.ps1 beta` |
| Alpha | `updates/patch-alpha.xml` | `promote.ps1 alpha` |

## Bug Fix Documentation

Document every bug fix with root cause, fix, and files changed.

### Issue #4: Settings Not Saving
**Root Cause:** `GetRevisionNumber()` returned 0 for both `revision="0"` and missing attribute.
**Fix:** Return -1 for missing, change checks to `< 0`.
**Files:** `Util.cs`, `Settings.cs`, `PlanIOHelper.cs`

### Issue #5: Certificates Not Accurate
**Root Cause:** CCP removed certificates from EVE.
**Fix:** Hide Certificate Browser tab in `PlanWindow.cs`.

### Fork Migration (v5.1.2)
**Detection:** `forkId` attribute; if missing, `revision > 1000` = peterhaneve user.
**Files:** `Settings.cs`, `SerializableSettings.cs`

### 30+ Characters Crash (v5.1.2)
**Root Cause:** Dead Hammertime API + async fire-and-forget.
**Fix:** New `StructureLookupService` with deduplication.
**Files:** `EveIDToStation.cs`, `StructureLookupService.cs`

## Project Context

- **Maintainer:** Alia Collins (EVE character)
- **GitHub:** https://github.com/aliacollins/evemon
- **.NET 8 Windows Forms** application
- **ESI API** for EVE Online data (OAuth2)
