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

# Build fresh
Write-Host "Building EVEMon Release..." -ForegroundColor Cyan
Push-Location $RepoRoot
dotnet publish "src\EVEMon\EVEMon.csproj" -c Release -r win-x64 --self-contained false -o "publish\win-x64"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

# Create zip with version in name
$zipPath = "publish\EVEMon-$Version-win-x64.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath }
Compress-Archive -Path "publish\win-x64\*" -DestinationPath $zipPath

# Build installer (graceful fallback if Inno Setup not available)
Write-Host "Building installer..." -ForegroundColor Cyan
& "$ScriptDir\build-installer.ps1" -Version $Version -SkipBuild

$installerPath = "publish\EVEMon-install-$Version.exe"
$hasInstaller = Test-Path $installerPath

if (-not $hasInstaller) {
    Write-Host "Warning: Installer build failed or Inno Setup not installed. Continuing with ZIP only." -ForegroundColor Yellow
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

# Fallback: Read from CHANGELOG.md if README section is empty
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

# Generate release notes file (avoids PowerShell parsing issues with markdown)
$releaseNotesPath = "$RepoRoot\publish\release-notes-stable.md"
$releaseNotes = @"
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

---

### What's New
$recentChanges

---
See [README](https://github.com/aliacollins/evemon#readme) for full documentation.

**Report Issues:** https://github.com/aliacollins/evemon/issues

**Maintainer:** Alia Collins (EVE Online) | [CapsuleerKit](https://www.capsuleerkit.com/)
"@

Set-Content -Path $releaseNotesPath -Value $releaseNotes

# Delete existing release if re-running (ignore errors)
$ErrorActionPreference = "SilentlyContinue"
gh release delete "v$Version" --yes --repo aliacollins/evemon 2>&1 | Out-Null
$ErrorActionPreference = "Stop"

# Upload files based on what's available
if ($hasInstaller) {
    gh release create "v$Version" $zipPath $installerPath --title "EVEMon v$Version" --notes-file $releaseNotesPath --repo aliacollins/evemon
} else {
    gh release create "v$Version" $zipPath --title "EVEMon v$Version" --notes-file $releaseNotesPath --repo aliacollins/evemon
}

Pop-Location

Write-Host ""
Write-Host "Stable release v$Version created!" -ForegroundColor Green
Write-Host "URL: https://github.com/aliacollins/evemon/releases/tag/v$Version" -ForegroundColor Yellow
