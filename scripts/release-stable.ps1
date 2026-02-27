# release-stable.ps1 - Promote current build to stable release
# Usage: .\scripts\release-stable.ps1 5.0.3

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

# Validate version format
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "Invalid version format. Use: X.Y.Z (e.g., 5.0.3)" -ForegroundColor Red
    exit 1
}

Write-Host "Creating stable release v$Version..." -ForegroundColor Cyan

# Build all platforms
Push-Location $RepoRoot

$AvaloniaProject = "src\EveLens.Avalonia\EveLens.Avalonia.csproj"

Write-Host "Building Windows x64..." -ForegroundColor Yellow
dotnet publish $AvaloniaProject -c Release -r win-x64 --self-contained false -o "publish\win-x64"
if ($LASTEXITCODE -ne 0) { Write-Host "Windows build failed!" -ForegroundColor Red; Pop-Location; exit 1 }

Write-Host "Building Linux x64..." -ForegroundColor Yellow
dotnet publish $AvaloniaProject -c Release -r linux-x64 --self-contained false -o "publish\linux-x64"
if ($LASTEXITCODE -ne 0) { Write-Host "Linux build failed!" -ForegroundColor Red; Pop-Location; exit 1 }

Write-Host "Building macOS ARM64..." -ForegroundColor Yellow
dotnet publish $AvaloniaProject -c Release -r osx-arm64 --self-contained false -o "publish\osx-arm64"
if ($LASTEXITCODE -ne 0) { Write-Host "macOS build failed!" -ForegroundColor Red; Pop-Location; exit 1 }

Write-Host "All portable builds completed." -ForegroundColor Green

# Build native installers (self-contained)
# Note: WSL commands emit to stderr (tool banners, warnings) which PowerShell
# treats as errors when ErrorActionPreference=Stop. Temporarily relax this.
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = "Continue"

Write-Host "Building Linux AppImage (self-contained)..." -ForegroundColor Yellow
$appImageResult = wsl bash ./scripts/build-appimage.sh $Version 2>&1
$hasAppImage = Test-Path "publish\EveLens-$Version-linux-x64.AppImage"
if (-not $hasAppImage) {
    Write-Host "Warning: AppImage build failed. Continuing without it." -ForegroundColor Yellow
    Write-Host $appImageResult -ForegroundColor Gray
}

Write-Host "Building macOS App Bundle (self-contained)..." -ForegroundColor Yellow
$macAppResult = wsl bash ./scripts/build-macapp.sh $Version 2>&1
$hasMacApp = Test-Path "publish\EveLens-$Version-osx-arm64.app.zip"
if (-not $hasMacApp) {
    Write-Host "Warning: macOS app bundle build failed. Continuing without it." -ForegroundColor Yellow
    Write-Host $macAppResult -ForegroundColor Gray
}

$ErrorActionPreference = $savedEAP

Write-Host "All platforms built successfully." -ForegroundColor Green

# Create zips for each platform
$winZip = "publish\EveLens-$Version-win-x64.zip"
$linuxZip = "publish\EveLens-$Version-linux-x64.zip"
$macZip = "publish\EveLens-$Version-osx-arm64.zip"

foreach ($zip in @($winZip, $linuxZip, $macZip)) {
    if (Test-Path $zip) { Remove-Item $zip }
}

Compress-Archive -Path "publish\win-x64\*" -DestinationPath $winZip
Compress-Archive -Path "publish\linux-x64\*" -DestinationPath $linuxZip
Compress-Archive -Path "publish\osx-arm64\*" -DestinationPath $macZip

# Build Windows installer (graceful fallback if Inno Setup not available)
Write-Host "Building installer..." -ForegroundColor Cyan
& "$ScriptDir\build-installer.ps1" -Version $Version -SkipBuild

$installerPath = "publish\EveLens-install-$Version.exe"
$hasInstaller = Test-Path $installerPath

if (-not $hasInstaller) {
    Write-Host "Warning: Installer build failed or Inno Setup not installed. Continuing with ZIPs only." -ForegroundColor Yellow
}

# Create git tag (handle existing tag from prior attempt)
$ErrorActionPreference = "SilentlyContinue"
git tag -d "v$Version" 2>&1 | Out-Null
git push origin --delete "refs/tags/v$Version" 2>&1 | Out-Null
$ErrorActionPreference = "Stop"

git tag -a "v$Version" -m "Release v$Version"
git push origin "refs/tags/v$Version"

Write-Host "Creating GitHub release..." -ForegroundColor Cyan

# Read README for "What's New" section for release notes
$readmeContent = Get-Content "$RepoRoot\README.md" -Raw
$recentChanges = ""
if ($readmeContent -match "## What's New in [0-9.]+\s*([\s\S]*?)(?=\r?\n---\r?\n)") {
    $recentChanges = $Matches[1].Trim()
}

if (-not $recentChanges) {
    Write-Host "README 'What's New' section not found, falling back to CHANGELOG.md" -ForegroundColor Yellow
    $changelog = Get-Content "$RepoRoot\CHANGELOG.md" -Raw
    if ($changelog -match "## \[$([regex]::Escape($Version))\][^\n]*\n([\s\S]*?)(?=\n## \[)") {
        $recentChanges = $Matches[1].Trim()
    }
    if (-not $recentChanges) {
        $recentChanges = "See CHANGELOG.md for details."
    }
}

# Generate release notes file
$releaseNotesPath = "$RepoRoot\publish\release-notes-stable.md"
$releaseNotes = @"
## EveLens v$Version

### Downloads

| Platform | File | Requirements |
|----------|------|-------------|
| **Windows (Installer)** | ``EveLens-install-$Version.exe`` | Installs .NET 8 automatically |
| **Windows (Portable)** | ``EveLens-$Version-win-x64.zip`` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Linux (AppImage)** | ``EveLens-$Version-linux-x64.AppImage`` | Just download and run |
| **Linux (Portable)** | ``EveLens-$Version-linux-x64.zip`` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **macOS (App)** | ``EveLens-$Version-osx-arm64.app.zip`` | Extract, drag to Applications |
| **macOS (Portable)** | ``EveLens-$Version-osx-arm64.zip`` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |

### Installation

**Windows:** Run the installer or extract the portable ZIP.

**Linux (AppImage):** ``chmod +x EveLens-*.AppImage && ./EveLens-*.AppImage``

**macOS (App):** Extract the zip, drag EveLens to Applications, right-click → Open on first launch. If you see "app is damaged," run ``xattr -cr EveLens.app`` in Terminal first.

**Portable builds (Linux/macOS):**
1. Install [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Extract the ZIP
3. Run: ``dotnet "EveLens.dll"``

### First Time Setup
1. Run EveLens
2. Add your character via **File -> Add Character**
3. Authorize with EVE Online SSO

---

### What's New
$recentChanges

---
See [README](https://github.com/aliacollins/evelens#readme) for full documentation.

**Report Issues:** https://github.com/aliacollins/evelens/issues

**Maintainer:** Alia Collins (EVE Online) | [CapsuleerKit](https://www.capsuleerkit.com/)
"@

Set-Content -Path $releaseNotesPath -Value $releaseNotes

# Delete existing release if re-running (ignore errors)
$ErrorActionPreference = "SilentlyContinue"
gh release delete "v$Version" --yes --repo aliacollins/evelens 2>&1 | Out-Null
$ErrorActionPreference = "Stop"

# Upload all files
$uploadFiles = @($winZip, $linuxZip, $macZip)
if ($hasInstaller) { $uploadFiles += $installerPath }
$appImagePath = "publish\EveLens-$Version-linux-x64.AppImage"
if (Test-Path $appImagePath) { $uploadFiles += $appImagePath }
$macAppPath = "publish\EveLens-$Version-osx-arm64.app.zip"
if (Test-Path $macAppPath) { $uploadFiles += $macAppPath }

gh release create "v$Version" @uploadFiles --title "EveLens v$Version" --notes-file $releaseNotesPath --repo aliacollins/evelens

Pop-Location

Write-Host ""
Write-Host "Stable release v$Version created!" -ForegroundColor Green
Write-Host "URL: https://github.com/aliacollins/evelens/releases/tag/v$Version" -ForegroundColor Yellow
