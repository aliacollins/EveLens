# EVEMon Release Scripts

## Workflow

```
[Make changes] --> [Push beta] --> [Test] --> [Push stable when ready]
     |                  |              |               |
     v                  v              v               v
  git commit    release-beta.ps1   Testers try   release-stable.ps1
                                   beta release
```

## Commands

### Build Installer Only
```powershell
.\scripts\build-installer.ps1
```
- Builds EVEMon and creates installer
- Reads version from SharedAssemblyInfo.cs
- Requires [Inno Setup 6](https://jrsoftware.org/isdl.php) installed
- Output: `publish\EVEMon-install-{version}.exe`

Options:
- `-Version 5.2.0` - Override version
- `-SkipBuild` - Use existing `publish\win-x64` build

### Push to Beta (do this often)
```powershell
.\scripts\release-beta.ps1
```
- Builds and uploads to rolling "beta" release
- Creates both ZIP and installer (if Inno Setup installed)
- Testers always get latest from: https://github.com/aliacollins/evemon/releases/tag/beta
- Overwrites previous beta each time

### Create Stable Release (when ready)
```powershell
.\scripts\release-stable.ps1 5.0.3
```
- Creates versioned release (v5.0.3)
- Creates both ZIP and installer
- Creates git tag
- Users download specific stable version
- **Remember to update `updates/patch.xml` after stable release!**

## Installer Features

The installer (`EVEMon-install-{version}.exe`) includes:
- Install location selection
- Desktop and Start Menu shortcuts
- Automatic .NET 8 Desktop Runtime download/install
- Uninstaller (via Windows Settings)
- Non-admin installation option

## Quick Flow Example

```powershell
# Made some fixes, push to beta for testing
git add .
git commit -m "Fix clone location display"
git push
.\scripts\release-beta.ps1

# After testing confirms it works, release stable
.\scripts\release-stable.ps1 5.0.3

# Update auto-update notification
# Edit updates/patch.xml with new version
```

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) - for building
- [Inno Setup 6](https://jrsoftware.org/isdl.php) - for creating installers
- [GitHub CLI](https://cli.github.com/) - for uploading releases
