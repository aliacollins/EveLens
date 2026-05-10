<#
.SYNOPSIS
    Build, sign, and release EveLens for all platforms.

.DESCRIPTION
    Builds Windows/Linux/macOS, signs Windows with Certum cert,
    packs with Velopack, uploads to GitHub Releases.

.PARAMETER Version
    SemVer version (e.g., "1.0.0-alpha.3"). Auto-detected from GitVersion if omitted.

.PARAMETER Channel
    Release channel: alpha, beta, or stable. Auto-detected from branch if omitted.

.PARAMETER DryRun
    Build and sign but don't upload.

.EXAMPLE
    .\scripts\release.ps1
    .\scripts\release.ps1 -Version "1.0.0-alpha.3"
    .\scripts\release.ps1 -DryRun
#>

param(
    [string]$Version,
    [string]$Channel,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $ProjectRoot

# ── Configuration ──
$CertThumbprint = "790D3430F3154FC6ACE68E0F5701D165BFFD3BC9"
$TimestampUrl = "http://time.certum.pl"
$MainProject = "src/EveLens.Avalonia/EveLens.Avalonia.csproj"
$AppName = "EveLens"
$Repo = "aliacollins/EveLens"

$WinPlatform = @{ Rid = "win-x64"; Exe = "EveLens.exe"; Dir = "publish/win-x64"; Out = "releases/win" }
$OtherPlatforms = @(
    @{ Rid = "linux-x64";  Dir = "publish/linux-x64";  Zip = "releases/EveLens-$Channel-linux-x64.zip" }
    @{ Rid = "osx-arm64";  Dir = "publish/osx-arm64";  Zip = "releases/EveLens-$Channel-osx-arm64.zip" }
)

# ── Preflight ──
Write-Host "`n=== Preflight ===" -ForegroundColor Cyan

$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $CertThumbprint }
if (-not $cert) {
    Write-Error "Certificate not found. Is SimplySign Desktop running?"
    exit 1
}
Write-Host "  [OK] Certificate: $($cert.Subject)" -ForegroundColor Green

$vpkPath = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
if (-not (Test-Path $vpkPath)) {
    Write-Host "  [..] Installing vpk..." -ForegroundColor Yellow
    dotnet tool install -g vpk
    $vpkPath = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
}
Write-Host "  [OK] vpk" -ForegroundColor Green

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) not found."
    exit 1
}
Write-Host "  [OK] gh CLI" -ForegroundColor Green

# ── Channel ──
if (-not $Channel) {
    $branch = git rev-parse --abbrev-ref HEAD
    switch ($branch) {
        'main'  { $Channel = 'stable' }
        'beta'  { $Channel = 'beta' }
        default { $Channel = 'alpha' }
    }
}
Write-Host "  [OK] Channel: $Channel" -ForegroundColor Green

# ── Version ──
if (-not $Version) {
    $gvPath = "$env:USERPROFILE\.dotnet\tools\dotnet-gitversion.exe"
    if ($gvPath -and (Test-Path $gvPath)) {
        $gvOutput = & $gvPath 2>$null | ConvertFrom-Json
        $Version = $gvOutput.FullSemVer
    }
    else {
        Write-Error "Pass -Version or install GitVersion ('dotnet tool install -g GitVersion.Tool')."
        exit 1
    }
}
Write-Host "  [OK] Version: $Version" -ForegroundColor Green

$releaseTag = "v$Version"
Write-Host "`n  Releasing: $AppName $Version ($Channel)" -ForegroundColor Magenta

# ── Parse version for assembly stamping ──
if ($Version -match '^(\d+)\.(\d+)\.(\d+)(?:-([a-z]+)\.?(\d+))?$') {
    $rev = if ($Matches[4]) { $Matches[5] } else { "0" }
    $asmVer = "$($Matches[1]).$($Matches[2]).$($Matches[3]).$rev"
} else {
    Write-Error "Could not parse version: $Version"
    exit 1
}

# ── Stamp SharedAssemblyInfo.cs ──
$SharedAsmPath = Join-Path $ProjectRoot "SharedAssemblyInfo.cs"
$originalAsm = Get-Content $SharedAsmPath -Raw

Write-Host "`n=== Version Stamp ===" -ForegroundColor Cyan
Write-Host "  Assembly: $asmVer | Info: $Version" -ForegroundColor Green

$stampedAsm = $originalAsm `
    -replace 'AssemblyVersion\("[^"]*"\)', "AssemblyVersion(`"$asmVer`")" `
    -replace 'AssemblyFileVersion\("[^"]*"\)', "AssemblyFileVersion(`"$asmVer`")" `
    -replace 'AssemblyInformationalVersion\("[^"]*"\)', "AssemblyInformationalVersion(`"$Version`")"
Set-Content $SharedAsmPath $stampedAsm -NoNewline

try {
    # ── Run tests ──
    Write-Host "`n=== Running Tests ===" -ForegroundColor Cyan
    $ErrorActionPreference = 'Continue'
    dotnet test tests/EveLens.Tests/EveLens.Tests.csproj -c Release --verbosity minimal 2>&1 | ForEach-Object { $_ }
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed."
    }
    Write-Host "  Tests passed." -ForegroundColor Green

    # ── Build all platforms ──
    $allPlatforms = @($WinPlatform) + $OtherPlatforms
    foreach ($plat in $allPlatforms) {
        Write-Host "`n=== Building $($plat.Rid) ===" -ForegroundColor Cyan
        if (Test-Path $plat.Dir) { Remove-Item $plat.Dir -Recurse -Force }

        $ErrorActionPreference = 'Continue'
        dotnet publish $MainProject `
            -c Release -r $plat.Rid --self-contained `
            -o $plat.Dir -p:Version=$Version 2>&1 | ForEach-Object { $_ }
        $ErrorActionPreference = 'Stop'

        if ($LASTEXITCODE -ne 0) { throw "Build failed for $($plat.Rid)." }
        Write-Host "  Built $($plat.Rid)." -ForegroundColor Green
    }

    # ── Pack Windows with Velopack + signing ──
    Write-Host "`n=== Packing win-x64 (signed) ===" -ForegroundColor Cyan
    if (Test-Path $WinPlatform.Out) { Remove-Item $WinPlatform.Out -Recurse -Force }

    $ErrorActionPreference = 'Continue'
    & $vpkPath pack `
        -u $AppName -v $Version `
        -p $WinPlatform.Dir -e $WinPlatform.Exe `
        --channel $Channel `
        --signParams "/sha1 $CertThumbprint /fd SHA256 /tr $TimestampUrl /td SHA256" `
        -o $WinPlatform.Out 2>&1 | ForEach-Object { $_ }
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -ne 0) { throw "Pack failed for win-x64." }
    Write-Host "  Packed win-x64 (signed)." -ForegroundColor Green

    if (-not (Test-Path "releases")) { New-Item -ItemType Directory -Path "releases" | Out-Null }

    # ── Linux: AppImage via WSL + raw zip ──
    Write-Host "`n=== Creating Linux AppImage (WSL) ===" -ForegroundColor Cyan
    $ErrorActionPreference = 'Continue'
    $linuxZip = $OtherPlatforms[0].Zip
    if (Test-Path $linuxZip) { Remove-Item $linuxZip -Force }
    Compress-Archive -Path "$($OtherPlatforms[0].Dir)/*" -DestinationPath $linuxZip
    Write-Host "  Zipped linux-x64." -ForegroundColor Green

    $appImagePath = "releases/EveLens-${Channel}-linux-x86_64.AppImage"
    if (Test-Path $appImagePath) { Remove-Item $appImagePath -Force }
    $wslScript = "/mnt/d/evemon-main/scripts/make-appimage.sh"
    wsl bash $wslScript $Channel 2>&1 | ForEach-Object { $_ }
    $ErrorActionPreference = 'Stop'
    if (Test-Path $appImagePath) {
        Write-Host "  AppImage created." -ForegroundColor Green
    } else {
        Write-Warning "AppImage creation failed -- zip will be uploaded instead."
    }

    # ── macOS: .app bundle + raw zip ──
    Write-Host "`n=== Creating macOS .app bundle ===" -ForegroundColor Cyan
    $macZip = $OtherPlatforms[1].Zip
    if (Test-Path $macZip) { Remove-Item $macZip -Force }
    Compress-Archive -Path "$($OtherPlatforms[1].Dir)/*" -DestinationPath $macZip
    Write-Host "  Zipped osx-arm64." -ForegroundColor Green

    # Build .app bundle via WSL to preserve Unix permissions and executable bit
    $appBundleZip = "releases/EveLens-${Channel}-osx-arm64.app.zip"
    if (Test-Path $appBundleZip) { Remove-Item $appBundleZip -Force }

    $wslPublishDir = "/mnt/d/evemon-main/publish/osx-arm64"
    $wslReleasesDir = "/mnt/d/evemon-main/releases"
    $wslIconsDir = "/mnt/d/evemon-main/installer/icons"
    $wslScript = @"
#!/bin/bash
set -e

APP_DIR="/tmp/EveLens.app"
rm -rf "`$APP_DIR"
mkdir -p "`$APP_DIR/Contents/MacOS"
mkdir -p "`$APP_DIR/Contents/Resources"

# Copy published files preserving structure
cp -r $wslPublishDir/* "`$APP_DIR/Contents/MacOS/"

# Set executable permission on the main binary
chmod +x "`$APP_DIR/Contents/MacOS/EveLens"

# Copy icon into Resources
cp "$wslIconsDir/evelens.icns" "`$APP_DIR/Contents/Resources/evelens.icns"

# Create Info.plist
cat > "`$APP_DIR/Contents/Info.plist" << 'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>
  <string>EveLens</string>
  <key>CFBundleDisplayName</key>
  <string>EveLens</string>
  <key>CFBundleIdentifier</key>
  <string>dev.evelens.app</string>
  <key>CFBundleVersion</key>
  <string>$Version</string>
  <key>CFBundleShortVersionString</key>
  <string>$Version</string>
  <key>CFBundleExecutable</key>
  <string>EveLens</string>
  <key>CFBundleIconFile</key>
  <string>evelens</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

# Zip with Unix permissions preserved (use cd to get clean paths)
cd /tmp
zip -r -y "$wslReleasesDir/EveLens-${Channel}-osx-arm64.app.zip" EveLens.app
rm -rf "`$APP_DIR"
echo "=== macOS .app bundle created ==="
"@

    # Write with Unix line endings and no BOM for WSL/bash compatibility
    $wslScript = $wslScript -replace "`r`n", "`n"
    [System.IO.File]::WriteAllText("$ProjectRoot/scripts/make-macapp.sh", $wslScript, (New-Object System.Text.UTF8Encoding $false))
    $ErrorActionPreference = 'Continue'
    wsl bash /mnt/d/evemon-main/scripts/make-macapp.sh 2>&1 | ForEach-Object { $_ }
    $ErrorActionPreference = 'Stop'

    if (Test-Path $appBundleZip) {
        Write-Host "  macOS .app bundle created." -ForegroundColor Green
    } else {
        Write-Warning "macOS .app bundle creation failed -- raw zip will be uploaded instead."
    }
}
finally {
    # Restore SharedAssemblyInfo.cs
    Set-Content $SharedAsmPath $originalAsm -NoNewline
    Write-Host "`n  SharedAssemblyInfo.cs restored." -ForegroundColor Gray
}

# ── Collect all artifacts ──
Write-Host "`n=== Artifacts ===" -ForegroundColor Cyan
$allFiles = @()

# Windows (Velopack, signed)
Get-ChildItem $WinPlatform.Out | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 1)
    Write-Host "  $($_.Name) ($sizeMB MB) (signed)" -ForegroundColor Green
    $allFiles += $_.FullName
}

# Linux zip
$file = Get-Item $OtherPlatforms[0].Zip
$sizeMB = [math]::Round($file.Length / 1MB, 1)
Write-Host "  $($file.Name) ($sizeMB MB)" -ForegroundColor Green
$allFiles += $file.FullName

# Linux AppImage (if created)
$appImageFile = "releases/EveLens-${Channel}-linux-x86_64.AppImage"
if (Test-Path $appImageFile) {
    $file = Get-Item $appImageFile
    $sizeMB = [math]::Round($file.Length / 1MB, 1)
    Write-Host "  $($file.Name) ($sizeMB MB)" -ForegroundColor Green
    $allFiles += $file.FullName
}

# macOS zip
$file = Get-Item $OtherPlatforms[1].Zip
$sizeMB = [math]::Round($file.Length / 1MB, 1)
Write-Host "  $($file.Name) ($sizeMB MB)" -ForegroundColor Green
$allFiles += $file.FullName

# macOS .app bundle
$appBundleZip = "releases/EveLens-${Channel}-osx-arm64.app.zip"
if (Test-Path $appBundleZip) {
    $file = Get-Item $appBundleZip
    $sizeMB = [math]::Round($file.Length / 1MB, 1)
    Write-Host "  $($file.Name) ($sizeMB MB)" -ForegroundColor Green
    $allFiles += $file.FullName
}

# ── Upload ──
if ($DryRun) {
    Write-Host "`n=== DRY RUN -- skipping upload ===" -ForegroundColor Yellow
    Write-Host "  Would create release: $releaseTag with $($allFiles.Count) files"
}
else {
    # Extract release notes from CHANGELOG.md
    # Looks for the section matching the version being released, falls back to [Unreleased]
    Write-Host "  Building release notes from CHANGELOG.md..." -ForegroundColor Yellow
    $changelogPath = Join-Path $ProjectRoot "CHANGELOG.md"
    $notes = ""
    if (Test-Path $changelogPath) {
        $inSection = $false
        $lines = @()
        foreach ($line in Get-Content $changelogPath) {
            # Match the exact version section first, then fall back to [Unreleased]
            if ($line -match "^\#\# \[$Version\]") { $inSection = $true; continue }
            if ($inSection -and $line -match '^\#\# \[') { break }
            if ($inSection) { $lines += $line }
        }
        $notes = ($lines -join "`n").Trim()

        # Fall back to [Unreleased] if no version-specific section found
        if (-not $notes) {
            $inSection = $false
            $lines = @()
            foreach ($line in Get-Content $changelogPath) {
                if ($line -match '^\#\# \[Unreleased\]') { $inSection = $true; continue }
                if ($inSection -and $line -match '^\#\# \[') { break }
                if ($inSection) { $lines += $line }
            }
            $notes = ($lines -join "`n").Trim()
        }
    }
    if (-not $notes) { $notes = "See CHANGELOG.md for details." }
    $notesFile = Join-Path $ProjectRoot "release-notes.md"
    Set-Content $notesFile $notes -Encoding UTF8

    Write-Host "`n=== Uploading to GitHub Release ===" -ForegroundColor Cyan

    $ErrorActionPreference = 'Continue'

    $prerelease = if ($Channel -eq 'stable') { "" } else { "--prerelease" }

    # Delete existing release if present
    $existCheck = gh release view $releaseTag --repo $Repo --json tagName 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Replacing existing release: $releaseTag" -ForegroundColor Yellow
        gh release delete $releaseTag --repo $Repo --yes --cleanup-tag 2>&1 | Out-Null
    }

    # Build the gh release create command
    $ghArgs = @("release", "create", $releaseTag, "--repo", $Repo, "--title", "$AppName $Version", "--notes-file", $notesFile)
    if ($prerelease) { $ghArgs += $prerelease }
    $ghArgs += $allFiles

    gh @ghArgs 2>&1 | ForEach-Object { Write-Host "  $_" }

    $ErrorActionPreference = 'Stop'

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Release creation failed."
        exit 1
    }

    # Cleanup
    Remove-Item $notesFile -ErrorAction SilentlyContinue

    Write-Host "`n  Released: https://github.com/$Repo/releases/tag/$releaseTag" -ForegroundColor Green
}

Pop-Location
