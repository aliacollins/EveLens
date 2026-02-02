# release-alpha.ps1 - Build and push to rolling alpha release
# Usage: .\scripts\release-alpha.ps1

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

Write-Host "Building EVEMon Alpha Release..." -ForegroundColor Cyan

# Read version from SharedAssemblyInfo.cs
$SharedAssemblyInfo = Get-Content "$RepoRoot\SharedAssemblyInfo.cs" -Raw
if ($SharedAssemblyInfo -match 'AssemblyInformationalVersion\("([^"]+)"\)') {
    $Version = $Matches[1]
    # Extract base version for installer (e.g., "5.2.0-alpha.1" -> "5.2.0")
    $InstallerVersion = $Version -replace '-.*$', ''
    Write-Host "Version: $Version (Installer: $InstallerVersion)" -ForegroundColor Gray
} else {
    Write-Host "Could not read version from SharedAssemblyInfo.cs" -ForegroundColor Red
    exit 1
}

# Build
Push-Location $RepoRoot
dotnet publish "src\EVEMon\EVEMon.csproj" -c Release -r win-x64 --self-contained false -o "publish\win-x64"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

# Create zip
$zipPath = "publish\EVEMon-$Version-win-x64.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath }
Compress-Archive -Path "publish\win-x64\*" -DestinationPath $zipPath

# Build installer
Write-Host "Building installer..." -ForegroundColor Cyan
& "$ScriptDir\build-installer.ps1" -Version $InstallerVersion -SkipBuild

$installerPath = "publish\EVEMon-install-$InstallerVersion.exe"
$hasInstaller = Test-Path $installerPath

if (-not $hasInstaller) {
    Write-Host "Warning: Installer build failed or Inno Setup not installed. Continuing with ZIP only." -ForegroundColor Yellow
}

Write-Host "Uploading to alpha release..." -ForegroundColor Cyan

# Delete existing alpha release (ignore error if doesn't exist)
$ErrorActionPreference = "SilentlyContinue"
gh release delete alpha --yes --repo aliacollins/evemon 2>&1 | Out-Null

# Move the alpha tag to current HEAD
# First delete the old tag (remote and local), then create new one
Write-Host "Updating alpha tag to current commit..." -ForegroundColor Gray
git push origin --delete refs/tags/alpha 2>&1 | Out-Null
git tag -d alpha 2>&1 | Out-Null
$ErrorActionPreference = "Stop"

# Create new alpha tag at current HEAD and push it explicitly as a tag
git tag alpha
git push origin refs/tags/alpha

# Extract "Alpha Changelog (Cumulative)" section from README for release notes
$readmeContent = Get-Content "$RepoRoot\README.md" -Raw
$changelogSection = ""
if ($readmeContent -match "## Alpha Changelog \(Cumulative\)([\s\S]*?)(?=\n---\n\n## )") {
    $changelogSection = $Matches[1].Trim()
}

# Extract "Features Being Tested" section
$featuresSection = ""
if ($readmeContent -match "## Features Being Tested([\s\S]*?)(?=\n---\n)") {
    $featuresSection = $Matches[1].Trim()
}

# Generate release notes file (avoids PowerShell parsing issues with markdown)
$releaseNotesPath = "$RepoRoot\publish\release-notes-alpha.md"
$releaseNotes = @"
## EVEMon Alpha Build - $Version

> **WARNING:** This is an **ALPHA** build. Expect bugs, crashes, and breaking changes.
>
> **Backup your settings before using:** ``%APPDATA%\EVEMon\``

---

### Installation

**Recommended: Installer**
- Download ``EVEMon-install-$InstallerVersion.exe``
- Automatically installs .NET 8 Desktop Runtime if needed

**Alternative: Portable ZIP**
- Download ``EVEMon-$Version-win-x64.zip``
- Requires [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Cumulative Changelog
$changelogSection

---

## Features Being Tested
$featuresSection

---

### Want Stable Instead?

Download stable releases from: [GitHub Releases](https://github.com/aliacollins/evemon/releases)

---

**Report Issues:** https://github.com/aliacollins/evemon/issues

**Maintainer:** Alia Collins (EVE Online) | [CapsuleerKit](https://www.capsuleerkit.com/)
"@

Set-Content -Path $releaseNotesPath -Value $releaseNotes

# Upload files based on what's available
if ($hasInstaller) {
    gh release create alpha $zipPath $installerPath --prerelease --title "EVEMon Alpha ($Version)" --notes-file $releaseNotesPath --repo aliacollins/evemon
} else {
    gh release create alpha $zipPath --prerelease --title "EVEMon Alpha ($Version)" --notes-file $releaseNotesPath --repo aliacollins/evemon
}

Pop-Location

Write-Host "Alpha release created!" -ForegroundColor Green
Write-Host "URL: https://github.com/aliacollins/evemon/releases/tag/alpha" -ForegroundColor Yellow
