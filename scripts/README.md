# EveLens Release Scripts

## Workflow

```
[feature/fix branch] --> [promote.ps1 alpha] --> [promote.ps1 beta] --> [promote.ps1 stable]
        |                        |                       |                      |
        v                        v                       v                      v
   git commit            version stamped +        beta testers get       release.ps1 builds,
                         merged + pushed          the update             signs & publishes
```

## Commands

### Promote a branch (version stamp + merge + push)
```powershell
.\scripts\promote.ps1 alpha -Message "Added feature X"
.\scripts\promote.ps1 beta -Message "Ready for beta testing"
.\scripts\promote.ps1 stable -Message "Production release"
```
- Increments the version in `SharedAssemblyInfo.cs`
- Updates `CHANGELOG.md` and `updates/patch-*.xml`
- Merges and pushes via pull request (branch protection applies)

### Build, sign, and publish a release
```powershell
.\scripts\release.ps1 -Version 1.5.0 -Channel stable
```
- Publishes self-contained builds for win-x64, linux-x64, osx-arm64
- Packs win-x64 with Velopack (`vpk`) -- this produces the signed
  `EveLens-{channel}-Setup.exe` and update packages (delta + full)
- Windows signing requires SimplySign Desktop running
- macOS `.app` and Linux AppImage come from `make-macapp.sh` / `make-appimage.sh`
- Uploads artifacts to the GitHub release

**Must be run after promoting** -- pushing code alone does not create a release.

### Other scripts
| Script | Purpose |
|--------|---------|
| `promote-dashboard.ps1` | Overview of branch/version state |
| `update-sde.ps1` | Regenerate game data from CCP's Static Data Export |
| `make-macapp.sh` | Build macOS `.app` bundle (run via WSL for Unix permissions) |
| `make-appimage.sh` | Build Linux AppImage |
| `codegraph.ps1` / `validate-codegraph.ps1` | Dependency graph generation/validation |
| `diag-stream.sh` | Tail the TCP diagnostic stream (port 5555) |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) - for building
- [GitHub CLI](https://cli.github.com/) - for uploading releases
- SimplySign Desktop - for Windows code signing
- `vpk` (installed automatically by release.ps1) - Velopack packer

> **History:** the Inno Setup installer (`installer/EveLens.iss`) was retired in the
> 1.5.0 cycle. Velopack builds the Setup.exe, and releases are self-contained, so the
> installer's runtime-bootstrap job no longer exists. The `installer/` folder keeps
> only the icons and platform packaging assets.
