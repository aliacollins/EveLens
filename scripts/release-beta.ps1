# release-beta.ps1 - Build and push to rolling beta release
# Usage: .\scripts\release-beta.ps1

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

Write-Host "Building EVEMon Beta Release..." -ForegroundColor Cyan

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

Write-Host "Uploading to beta release..." -ForegroundColor Cyan

# Delete existing beta release (ignore error if doesn't exist)
$ErrorActionPreference = "SilentlyContinue"
gh release delete beta --yes --repo aliacollins/evemon 2>&1 | Out-Null

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
## EVEMon Beta Build - $Version

> **BETA:** This is a pre-release build for testing before stable release.
>
> Please report any issues you find!

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

### What's New in This Beta
$recentChanges

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

gh release create beta @uploadFiles --prerelease --title "EVEMon Beta ($Version)" --notes-file $releaseNotesPath --repo aliacollins/evemon

Pop-Location

Write-Host "Beta release updated!" -ForegroundColor Green
Write-Host "URL: https://github.com/aliacollins/evemon/releases/tag/beta" -ForegroundColor Yellow
