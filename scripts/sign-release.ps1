<#
.SYNOPSIS
    Builds, signs, and uploads Windows release artifacts for EveLens.

.DESCRIPTION
    This script replaces (or creates) signed Windows artifacts in a GitHub Release.
    CI handles Linux/macOS builds. This script handles Windows signing locally
    because the Certum SimplySign certificate lives on this machine.

.PARAMETER Version
    SemVer version string (e.g., "1.0.0-alpha.43"). Auto-detected from GitVersion if omitted.

.PARAMETER Channel
    Release channel: alpha, beta, or stable. Auto-detected from branch if omitted.

.PARAMETER SkipBuild
    Skip the dotnet publish step (use existing publish output).

.PARAMETER DryRun
    Build and sign but don't upload to GitHub.

.EXAMPLE
    .\scripts\sign-release.ps1
    .\scripts\sign-release.ps1 -Version "1.0.0-beta.2" -Channel beta
    .\scripts\sign-release.ps1 -DryRun
#>

param(
    [string]$Version,
    [string]$Channel,
    [switch]$SkipBuild,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $ProjectRoot

# ── Configuration ──
$SignToolPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
$CertThumbprint = "790D3430F3154FC6ACE68E0F5701D165BFFD3BC9"
$TimestampUrl = "http://time.certum.pl"
$MainProject = "src/EveLens.Avalonia/EveLens.Avalonia.csproj"
$AppName = "EveLens"
$PublishDir = "publish/win-x64"
$ReleaseDir = "releases/win"

# ── Preflight checks ──
Write-Host "`n=== Preflight Checks ===" -ForegroundColor Cyan

# Check SimplySign cert is available
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $CertThumbprint }
if (-not $cert) {
    Write-Error "Certificate not found in store. Is SimplySign Desktop running and logged in?"
    exit 1
}
Write-Host "  [OK] Certificate: $($cert.Subject)" -ForegroundColor Green

# Check signtool
if (-not (Test-Path $SignToolPath)) {
    Write-Error "signtool.exe not found at: $SignToolPath"
    exit 1
}
Write-Host "  [OK] signtool.exe found" -ForegroundColor Green

# Check vpk
$vpkPath = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
if (-not (Test-Path $vpkPath)) {
    $vpkPath = (Get-Command vpk -ErrorAction SilentlyContinue).Source
    if (-not $vpkPath) {
        Write-Host "  [..] Installing vpk..." -ForegroundColor Yellow
        dotnet tool install -g vpk
        $vpkPath = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
    }
}
Write-Host "  [OK] vpk found" -ForegroundColor Green

# Check gh
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) not found. Install from https://cli.github.com"
    exit 1
}
Write-Host "  [OK] gh CLI found" -ForegroundColor Green

# ── Detect channel from branch ──
if (-not $Channel) {
    $branch = git rev-parse --abbrev-ref HEAD
    switch ($branch) {
        'main'  { $Channel = 'stable' }
        'beta'  { $Channel = 'beta' }
        default { $Channel = 'alpha' }
    }
}
Write-Host "  [OK] Channel: $Channel" -ForegroundColor Green

# ── Detect version from GitVersion ──
if (-not $Version) {
    Write-Host "  [..] Running GitVersion..." -ForegroundColor Yellow
    try {
        $gvOutput = dotnet-gitversion 2>$null | ConvertFrom-Json
        $Version = $gvOutput.FullSemVer
    }
    catch {
        # Fallback: try gitversion as dotnet tool
        try {
            $gvOutput = dotnet gitversion 2>$null | ConvertFrom-Json
            $Version = $gvOutput.FullSemVer
        }
        catch {
            Write-Error "Could not detect version. Install GitVersion or pass -Version explicitly."
            exit 1
        }
    }
}
Write-Host "  [OK] Version: $Version" -ForegroundColor Green

$releaseTag = "v$Version"
Write-Host "`n  Signing: $AppName $Version ($Channel)" -ForegroundColor Magenta

# ── Step 0: Update SharedAssemblyInfo.cs ──
$SharedAssemblyInfoPath = Join-Path $ProjectRoot "SharedAssemblyInfo.cs"
$originalContent = Get-Content $SharedAssemblyInfoPath -Raw

# Parse semver: "1.0.0-alpha.1" → Major=1, Minor=0, Patch=0, PreLabel=alpha, PreNum=1
if ($Version -match '^(\d+)\.(\d+)\.(\d+)(?:-([a-z]+)\.?(\d+))?$') {
    $major = $Matches[1]
    $minor = $Matches[2]
    $patch = $Matches[3]
    $preLabel = $Matches[4]
    $preNum = if ($Matches[5]) { $Matches[5] } else { "0" }

    # 4th component: 0 for stable, N for prerelease
    $revision = if ($preLabel) { $preNum } else { "0" }
    $assemblyVer = "$major.$minor.$patch.$revision"
    $infoVer = $Version
}
else {
    Write-Error "Could not parse version: $Version"
    exit 1
}

Write-Host "`n=== Updating SharedAssemblyInfo.cs ===" -ForegroundColor Cyan
Write-Host "  AssemblyVersion:              $assemblyVer" -ForegroundColor Green
Write-Host "  AssemblyFileVersion:          $assemblyVer" -ForegroundColor Green
Write-Host "  AssemblyInformationalVersion: $infoVer" -ForegroundColor Green

$updatedContent = $originalContent `
    -replace 'AssemblyVersion\("[^"]*"\)', "AssemblyVersion(`"$assemblyVer`")" `
    -replace 'AssemblyFileVersion\("[^"]*"\)', "AssemblyFileVersion(`"$assemblyVer`")" `
    -replace 'AssemblyInformationalVersion\("[^"]*"\)', "AssemblyInformationalVersion(`"$infoVer`")"

Set-Content $SharedAssemblyInfoPath $updatedContent -NoNewline

# ── Step 1: Build ──
if (-not $SkipBuild) {
    Write-Host "`n=== Building Windows Release ===" -ForegroundColor Cyan

    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }

    try {
        dotnet publish $MainProject `
            -c Release -r win-x64 --self-contained `
            -o $PublishDir `
            -p:Version=$Version

        if ($LASTEXITCODE -ne 0) {
            throw "Build failed."
        }
        Write-Host "  Build complete." -ForegroundColor Green
    }
    finally {
        # Restore SharedAssemblyInfo.cs so we don't leave dirty working tree
        Set-Content $SharedAssemblyInfoPath $originalContent -NoNewline
        Write-Host "  SharedAssemblyInfo.cs restored." -ForegroundColor Gray
    }
}
else {
    # Restore immediately — no build needed
    Set-Content $SharedAssemblyInfoPath $originalContent -NoNewline
    if (-not (Test-Path "$PublishDir/EveLens.exe")) {
        Write-Error "No publish output found at $PublishDir. Run without -SkipBuild."
        exit 1
    }
    Write-Host "`n=== Skipping build (using existing output) ===" -ForegroundColor Yellow
}

# ── Step 2: Pack with signing ──
Write-Host "`n=== Packing with Code Signing ===" -ForegroundColor Cyan

if (Test-Path $ReleaseDir) {
    Remove-Item $ReleaseDir -Recurse -Force
}

$signParams = "/sha1 $CertThumbprint /fd SHA256 /tr $TimestampUrl /td SHA256"

& $vpkPath pack `
    -u $AppName `
    -v $Version `
    -p $PublishDir `
    -e EveLens.exe `
    --channel $Channel `
    --signParams $signParams `
    -o $ReleaseDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "vpk pack failed."
    exit 1
}
Write-Host "  Pack complete." -ForegroundColor Green

# ── Step 3: Verify signature ──
Write-Host "`n=== Verifying Signature ===" -ForegroundColor Cyan

$setupExe = Get-ChildItem "$ReleaseDir/*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($setupExe) {
    & $SignToolPath verify /pa $setupExe.FullName
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Signature valid: $($setupExe.Name)" -ForegroundColor Green
    }
    else {
        Write-Warning "Signature verification failed for $($setupExe.Name)"
    }
}

# ── Step 4: Upload to GitHub Release ──
if ($DryRun) {
    Write-Host "`n=== DRY RUN - Skipping upload ===" -ForegroundColor Yellow
    Write-Host "  Would upload to release: $releaseTag"
    Get-ChildItem $ReleaseDir | ForEach-Object { Write-Host "    $($_.Name) ($([math]::Round($_.Length / 1MB, 1)) MB)" }
}
else {
    Write-Host "`n=== Uploading to GitHub Release ===" -ForegroundColor Cyan

    # Check if release exists
    $releaseExists = gh release view $releaseTag --json tagName 2>$null
    if (-not $releaseExists) {
        Write-Host "  Creating release: $releaseTag" -ForegroundColor Yellow
        $prerelease = if ($Channel -eq 'stable') { "" } else { "--prerelease" }
        $title = "$AppName $Version"

        gh release create $releaseTag `
            --title $title `
            $prerelease `
            --notes "Signed Windows release for $Version"
    }

    # Upload artifacts (--clobber replaces existing files with same name)
    $artifacts = Get-ChildItem $ReleaseDir
    foreach ($artifact in $artifacts) {
        $sizeMB = [math]::Round($artifact.Length / 1MB, 1)
        Write-Host "  Uploading: $($artifact.Name) ($sizeMB MB)" -ForegroundColor Yellow
        gh release upload $releaseTag $artifact.FullName --clobber
    }

    Write-Host "`n  Done! Signed release uploaded to: $releaseTag" -ForegroundColor Green
    Write-Host "  https://github.com/aliacollins/EveLens/releases/tag/$releaseTag" -ForegroundColor Gray
}

Pop-Location
