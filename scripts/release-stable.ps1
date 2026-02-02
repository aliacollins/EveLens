# release-stable.ps1 - Promote current build to stable release
# Usage: .\scripts\release-stable.ps1 5.0.3

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

# Validate version format
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "Invalid version format. Use: X.Y.Z (e.g., 5.0.3)" -ForegroundColor Red
    exit 1
}

Write-Host "Creating stable release v$Version..." -ForegroundColor Cyan

# Build fresh
Write-Host "Building EVEMon Release..." -ForegroundColor Cyan
dotnet publish "src\EVEMon\EVEMon.csproj" -c Release -r win-x64 --self-contained false -o "publish\win-x64"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Create zip with version in name
$zipPath = "publish\EVEMon-$Version-win-x64.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath }
Compress-Archive -Path "publish\win-x64\*" -DestinationPath $zipPath

# Build installer
Write-Host "Building installer..." -ForegroundColor Cyan
& "$PSScriptRoot\build-installer.ps1" -Version $Version -SkipBuild

if ($LASTEXITCODE -ne 0) {
    Write-Host "Installer build failed!" -ForegroundColor Red
    exit 1
}

$installerPath = "publish\EVEMon-install-$Version.exe"
if (-not (Test-Path $installerPath)) {
    Write-Host "Installer not found at: $installerPath" -ForegroundColor Red
    exit 1
}

# Create git tag
git tag -a "v$Version" -m "Release v$Version"
git push origin "v$Version"

Write-Host "Creating GitHub release..." -ForegroundColor Cyan

# Upload both zip and installer
gh release create "v$Version" $zipPath $installerPath --title "EVEMon v$Version" --notes @"
## EVEMon v$Version

### Installation Options

**Option 1: Installer (Recommended)**
1. Download ``EVEMon-install-$Version.exe``
2. Run the installer
3. Follow the setup wizard
4. The installer will download .NET 8 Desktop Runtime if needed

**Option 2: Portable ZIP**
1. Download ``EVEMon-$Version-win-x64.zip``
2. Extract to a folder
3. Run ``EVEMon.exe``
4. Requires [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Requirements
- Windows 10/11 (x64)
- .NET 8.0 Desktop Runtime (installer downloads automatically)

### First Time Setup
1. Run EVEMon
2. Add your character via **File -> Add Character**
3. Authorize with EVE Online SSO

### Changelog
$(git log --oneline $(git describe --tags --abbrev=0 HEAD^ 2>$null)..HEAD 2>$null || echo "Initial release")

---
See [README](https://github.com/aliacollins/evemon#readme) for full documentation.
"@

Write-Host ""
Write-Host "Stable release v$Version created!" -ForegroundColor Green
Write-Host "URL: https://github.com/aliacollins/evemon/releases/tag/v$Version" -ForegroundColor Yellow

# Reminder about patch.xml
Write-Host ""
Write-Host "IMPORTANT: Don't forget to update updates/patch.xml for auto-update notifications!" -ForegroundColor Yellow
