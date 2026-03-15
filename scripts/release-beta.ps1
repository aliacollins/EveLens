# release-beta.ps1 - Build and push to rolling beta release
# Usage: .\scripts\release-beta.ps1

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

Write-Host "Building EveLens Beta Release..." -ForegroundColor Cyan

# Read version from SharedAssemblyInfo.cs
$SharedAssemblyInfo = Get-Content "$RepoRoot\SharedAssemblyInfo.cs" -Raw
if ($SharedAssemblyInfo -match 'AssemblyInformationalVersion\("([^"]+)"\)') {
    $Version = $Matches[1]
    $InstallerVersion = $Version -replace '-.*$', ''
    Write-Host "Version: $Version (Installer: $InstallerVersion)" -ForegroundColor Gray
} else {
    Write-Host "Could not read version from SharedAssemblyInfo.cs" -ForegroundColor Red
    exit 1
}

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

# Build Windows installer
Write-Host "Building installer..." -ForegroundColor Cyan
& "$ScriptDir\build-installer.ps1" -Version $InstallerVersion -SkipBuild

$installerPath = "publish\EveLens-install-$InstallerVersion.exe"
$hasInstaller = Test-Path $installerPath

if (-not $hasInstaller) {
    Write-Host "Warning: Installer build failed or Inno Setup not installed. Continuing with ZIPs only." -ForegroundColor Yellow
}

Write-Host "Uploading to beta release..." -ForegroundColor Cyan

# Delete existing beta release (ignore error if doesn't exist)
$ErrorActionPreference = "SilentlyContinue"
gh release delete beta --yes --repo aliacollins/evelens 2>&1 | Out-Null

Write-Host "Updating beta tag to current commit..." -ForegroundColor Gray
git push origin --delete refs/tags/beta 2>&1 | Out-Null
git tag -d beta 2>&1 | Out-Null
$ErrorActionPreference = "Stop"

git tag beta
git push origin refs/tags/beta

# Read README for "What's New" section
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
$releaseNotesPath = "$RepoRoot\publish\release-notes-beta.md"
$releaseNotes = @"
## EveLens Beta Build - $Version

> **BETA:** This is a pre-release build for testing before stable release.
>
> Please report any issues you find!

> **Coming from EVEMon or an older fork?** We recommend a fresh install rather than importing old settings. EveLens is a complete rewrite and a clean start will give you the smoothest experience.

---

### Downloads

| Platform | File | Requirements |
|----------|------|-------------|
| **Windows (Installer)** | ``EveLens-install-$InstallerVersion.exe`` | Installs .NET 8 automatically |
| **Windows (Portable)** | ``EveLens-$Version-win-x64.zip`` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Linux (AppImage)** | ``EveLens-$Version-linux-x64.AppImage`` | Just download and run |
| **Linux (Portable)** | ``EveLens-$Version-linux-x64.zip`` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **macOS (App)** | ``EveLens-$Version-osx-arm64.app.zip`` | Extract, drag to Applications |
| **macOS (Portable)** | ``EveLens-$Version-osx-arm64.zip`` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |

**Linux AppImage:** ``chmod +x EveLens-*.AppImage && ./EveLens-*.AppImage``
**macOS App:** Extract zip, drag to Applications, right-click → Open on first launch. If you see "app is damaged," run ``xattr -cr EveLens.app`` in Terminal first
**Portable builds:** Require [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0), run ``dotnet "EveLens.dll"``

---

### What's New in This Beta
$recentChanges

---

### Want Stable Instead?

Download stable releases from: [GitHub Releases](https://github.com/aliacollins/evelens/releases)

---

**Report Issues:** https://github.com/aliacollins/evelens/issues

**Maintainer:** Alia Collins (EVE Online) | [CapsuleerKit](https://www.capsuleerkit.com/)
"@

Set-Content -Path $releaseNotesPath -Value $releaseNotes

# Upload all files
$uploadFiles = @($winZip, $linuxZip, $macZip)
if ($hasInstaller) { $uploadFiles += $installerPath }
$appImagePath = "publish\EveLens-$Version-linux-x64.AppImage"
if (Test-Path $appImagePath) { $uploadFiles += $appImagePath }
$macAppPath = "publish\EveLens-$Version-osx-arm64.app.zip"
if (Test-Path $macAppPath) { $uploadFiles += $macAppPath }

gh release create beta @uploadFiles --prerelease --title "EveLens Beta ($Version)" --notes-file $releaseNotesPath --repo aliacollins/evelens

Pop-Location

Write-Host "Beta release updated!" -ForegroundColor Green
Write-Host "URL: https://github.com/aliacollins/evelens/releases/tag/beta" -ForegroundColor Yellow
