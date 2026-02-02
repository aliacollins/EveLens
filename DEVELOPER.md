# Contributing to EVEMon

Thank you for your interest in contributing to EVEMon!

## Development Setup

### Prerequisites
- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code with C# extension

### Building from Source

```bash
# Clone the repository
git clone https://github.com/aliacollins/evemon.git
cd evemon

# Build the solution
dotnet build EVEMon.sln -c Debug

# Run
dotnet run --project src/EVEMon/EVEMon.csproj
```

### ESI Credentials for Development

EVEMon requires ESI API credentials. For development:

1. Go to [EVE Developers](https://developers.eveonline.com/) and log in
2. Create a new application:
   - **Name:** EVEMon-Dev (or any name)
   - **Callback URL:** `http://localhost:4916/callback/`
   - **Scopes:** Select all scopes needed for testing
3. Create `esi-credentials.json` in the repo root (it's gitignored):
```json
{
  "ClientID": "your-client-id",
  "ClientSecret": "your-client-secret"
}
```

Alternatively, enter credentials in EVEMon via **Tools → Options → Network → ESI Settings**.

---

## Project Structure

```
evemon/
├── src/
│   ├── EVEMon/                    # Main WinForms application
│   ├── EVEMon.Common/             # Core library (models, services, API)
│   ├── EVEMon.LogitechG15/        # Logitech keyboard support
│   ├── EVEMon.PieChart/           # Chart controls
│   ├── EVEMon.Sales/              # Market/sales functionality
│   ├── EVEMon.Watchdog/           # Process watchdog
│   └── EVEMon.WindowsApi/         # Windows API interop
├── tools/
│   ├── XmlGenerator/              # Generates EVEMon data files from SDE
│   └── YamlToSqlite/              # Converts EVE SDE YAML to SQLite
└── SharedAssemblyInfo.cs          # Version info for all projects
```

---

## SDE Data Tools

EVEMon uses static data from CCP's SDE (Static Data Export). To update:

### 1. Download SDE
The SDE is published by CCP. [Fuzzwork](https://www.fuzzwork.co.uk/dump/) provides a convenient SQLite conversion:
```bash
curl -O https://www.fuzzwork.co.uk/dump/sqlite-latest.sqlite.bz2
bunzip2 sqlite-latest.sqlite.bz2
```
Note: The data comes from CCP's official SDE - Fuzzwork just converts it to SQLite format for easier use.

### 2. Run XmlGenerator
```bash
dotnet run --project tools/XmlGenerator/XmlGenerator.csproj
```

This generates the XML/gzip files in `src/EVEMon.Common/Resources/`.

---

## Creating a Release

### Version Format

| Type | Version Format | Display | Example |
|------|---------------|---------|---------|
| Stable | `x.y.z.0` | `x.y.z` | `5.2.0.0` → `5.2.0` |
| Alpha | `x.y.z.N` | `x.y.z-alpha.N` | `5.2.0.1` → `5.2.0-alpha.1` |
| Beta | `x.y.z.N` | `x.y.z-beta.N` | `5.2.0.1` → `5.2.0-beta.1` |

### 1. Update Version
Edit `SharedAssemblyInfo.cs`:
```csharp
// For stable release
[assembly: AssemblyVersion("5.2.0.0")]
[assembly: AssemblyFileVersion("5.2.0.0")]
[assembly: AssemblyInformationalVersion("5.2.0")]

// For alpha/beta release
[assembly: AssemblyVersion("5.2.0.1")]
[assembly: AssemblyFileVersion("5.2.0.1")]
[assembly: AssemblyInformationalVersion("5.2.0-alpha.1")]
```

### 2. Using Release Scripts (Recommended)

**For Beta/Alpha releases:**
```powershell
.\scripts\release-beta.ps1
```
This builds EVEMon, creates ZIP and installer, and uploads to rolling `beta` release on GitHub.

**For Stable releases:**
```powershell
.\scripts\release-stable.ps1 5.2.0
```
This creates a versioned release (v5.2.0) with ZIP and installer.

### 3. Manual Release Process

**Build Release:**
```bash
dotnet publish src/EVEMon/EVEMon.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64
```

**Create Portable Zip:**
```powershell
Compress-Archive -Path publish/win-x64/* -DestinationPath EVEMon-5.2.0-win-x64.zip
```

**Create Installer:**
```powershell
.\scripts\build-installer.ps1
```
Or manually with Inno Setup:
```powershell
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" /DMyAppVersion=5.2.0 installer\EVEMon.iss
```
This creates `publish/EVEMon-install-5.2.0.exe`.

**Create GitHub Release:**
```bash
gh release create v5.2.0 EVEMon-5.2.0-win-x64.zip EVEMon-install-5.2.0.exe --title "EVEMon v5.2.0"
```

---

## Installer

The EVEMon installer is built with [Inno Setup 6](https://jrsoftware.org/isdl.php).

### Installer Features
- Install location selection (Program Files or custom)
- Desktop and Start Menu shortcuts
- Automatic .NET 8 Desktop Runtime download/install
- Uninstaller registered in Windows Settings
- Non-admin installation option (to user's AppData)
- Silent install support (`/SILENT` or `/VERYSILENT`)

### Installer Files
| File | Purpose |
|------|---------|
| `installer/EVEMon.iss` | Inno Setup script |
| `scripts/build-installer.ps1` | Build script |
| `publish/EVEMon-install-{version}.exe` | Output installer |

### Building the Installer

**Prerequisites:**
- [Inno Setup 6](https://jrsoftware.org/isdl.php) installed

**Build:**
```powershell
.\scripts\build-installer.ps1
```

Options:
- `-Version 5.2.0` - Override version number
- `-SkipBuild` - Use existing `publish/win-x64` build

---

## Update Channels

EVEMon has three update channels:

| Channel | Patch File | Who Gets Updates |
|---------|------------|------------------|
| Stable | `updates/patch.xml` | Normal users |
| Beta | `updates/patch-beta.xml` | Beta builds |
| Alpha | `updates/patch-alpha.xml` | Alpha builds |

The app automatically checks the correct channel based on its version string.

### After a Stable Release
Update `updates/patch.xml` to notify users:
```xml
<release>
  <date>2026-01-11</date>
  <version>5.2.0.0</version>
  <url>https://github.com/aliacollins/evemon/releases/tag/v5.2.0</url>
  <autopatchurl>https://github.com/.../EVEMon-install-5.2.0.exe</autopatchurl>
  <message>Release notes here</message>
</release>
```

---

## Debug vs Release Builds

- **Debug builds** show the Test menu (for exception testing, notifications, etc.)
- **Release builds** hide the Test menu automatically via `[Conditional("DEBUG")]`

---

## Code Style

- Use C# 12 features where appropriate
- Follow existing code patterns
- Add XML documentation for public APIs
- Use async/await for all I/O operations

---

## Deprecated Features

These features were removed due to API changes:
- **OneDrive** - Requires Microsoft Graph API rewrite
- **Dropbox** - Dropbox API v7 breaking changes
- **IGB Service** - EVE removed In-Game Browser

---

## Questions?

Open an issue or check the [wiki](https://github.com/aliacollins/evemon/wiki).
