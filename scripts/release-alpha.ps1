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

# Build all platforms
Push-Location $RepoRoot

$AvaloniaProject = "src\EVEMon.Avalonia\EVEMon.Avalonia.csproj"

Write-Host "Building Windows x64..." -ForegroundColor Yellow
dotnet publish $AvaloniaProject -c Release -r win-x64 --self-contained false -o "publish\win-x64"
if ($LASTEXITCODE -ne 0) { Write-Host "Windows build failed!" -ForegroundColor Red; Pop-Location; exit 1 }

Write-Host "Building Linux x64..." -ForegroundColor Yellow
dotnet publish $AvaloniaProject -c Release -r linux-x64 --self-contained false -o "publish\linux-x64"
if ($LASTEXITCODE -ne 0) { Write-Host "Linux build failed!" -ForegroundColor Red; Pop-Location; exit 1 }

Write-Host "Building macOS ARM64..." -ForegroundColor Yellow
dotnet publish $AvaloniaProject -c Release -r osx-arm64 --self-contained false -o "publish\osx-arm64"
if ($LASTEXITCODE -ne 0) { Write-Host "macOS build failed!" -ForegroundColor Red; Pop-Location; exit 1 }

Write-Host "All platforms built successfully." -ForegroundColor Green

# Create zips for each platform
$winZip = "publish\EVEMon-$Version-win-x64.zip"
$linuxZip = "publish\EVEMon-$Version-linux-x64.zip"
$macZip = "publish\EVEMon-$Version-osx-arm64.zip"

foreach ($zip in @($winZip, $linuxZip, $macZip)) {
    if (Test-Path $zip) { Remove-Item $zip }
}

Compress-Archive -Path "publish\win-x64\*" -DestinationPath $winZip
Compress-Archive -Path "publish\linux-x64\*" -DestinationPath $linuxZip
Compress-Archive -Path "publish\osx-arm64\*" -DestinationPath $macZip

# Build Windows installer
Write-Host "Building installer..." -ForegroundColor Cyan
& "$ScriptDir\build-installer.ps1" -Version $InstallerVersion -SkipBuild

$installerPath = "publish\EVEMon-install-$InstallerVersion.exe"
$hasInstaller = Test-Path $installerPath

if (-not $hasInstaller) {
    Write-Host "Warning: Installer build failed or Inno Setup not installed. Continuing with ZIPs only." -ForegroundColor Yellow
}

Write-Host "Uploading to alpha release..." -ForegroundColor Cyan

# Delete existing alpha release (ignore error if doesn't exist)
$ErrorActionPreference = "SilentlyContinue"
gh release delete alpha --yes --repo aliacollins/evemon 2>&1 | Out-Null

# Move the alpha tag to current HEAD
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
if ($readmeContent -match "## Alpha Changelog \(Cumulative\)([\s\S]*?)(?=\r?\n---\r?\n\r?\n## )") {
    $changelogSection = $Matches[1].Trim()
}

# Fallback: Read from CHANGELOG.md if README section is empty
if (-not $changelogSection) {
    Write-Host "README 'Alpha Changelog' section not found, falling back to CHANGELOG.md" -ForegroundColor Yellow
    $changelog = Get-Content "$RepoRoot\CHANGELOG.md" -Raw
    if ($changelog -match "## \[$([regex]::Escape($Version))\][^\n]*\n([\s\S]*?)(?=\n## \[)") {
        $changelogSection = $Matches[1].Trim()
    }
    if (-not $changelogSection) {
        $changelogSection = "See CHANGELOG.md for details."
    }
}

# Extract "Features Being Tested" section
$featuresSection = ""
if ($readmeContent -match "## Features Being Tested([\s\S]*?)(?=\r?\n---\r?\n)") {
    $featuresSection = $Matches[1].Trim()
}
if (-not $featuresSection) {
    $featuresSection = "See README.md for details."
}

# Generate release notes file
$releaseNotesPath = "$RepoRoot\publish\release-notes-alpha.md"
$releaseNotes = @"
## EVEMon Alpha Build - $Version

> **WARNING:** This is an **ALPHA** build. Expect bugs, crashes, and breaking changes.
>
> **Backup your settings before using**

---

### Downloads

| Platform | File | Requirements |
|----------|------|-------------|
| **Windows (Installer)** | ``EVEMon-install-$InstallerVersion.exe`` | Installs .NET 8 automatically |
| **Windows (Portable)** | ``EVEMon-$Version-win-x64.zip`` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Linux x64** | ``EVEMon-$Version-linux-x64.zip`` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **macOS Apple Silicon** | ``EVEMon-$Version-osx-arm64.zip`` | [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |

**Linux/macOS:** Extract and run ``dotnet "EVEMon NexT.dll"``

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

# Upload all files
$uploadFiles = @($winZip, $linuxZip, $macZip)
if ($hasInstaller) { $uploadFiles += $installerPath }

gh release create alpha @uploadFiles --prerelease --title "EVEMon Alpha ($Version)" --notes-file $releaseNotesPath --repo aliacollins/evemon

Pop-Location

Write-Host "Alpha release created!" -ForegroundColor Green
Write-Host "URL: https://github.com/aliacollins/evemon/releases/tag/alpha" -ForegroundColor Yellow
