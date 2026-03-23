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

$Platforms = @(
    @{ Rid = "win-x64";    Exe = "EveLens.exe"; Dir = "publish/win-x64";    Out = "releases/win";   Sign = $true  }
    @{ Rid = "linux-x64";  Exe = "EveLens";     Dir = "publish/linux-x64";  Out = "releases/linux"; Sign = $false }
    @{ Rid = "osx-arm64";  Exe = "EveLens";     Dir = "publish/osx-arm64";  Out = "releases/osx";   Sign = $false }
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
    dotnet test tests/EveLens.Tests/EveLens.Tests.csproj -c Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed."
    }
    Write-Host "  Tests passed." -ForegroundColor Green

    # ── Build all platforms ──
    foreach ($plat in $Platforms) {
        Write-Host "`n=== Building $($plat.Rid) ===" -ForegroundColor Cyan
        if (Test-Path $plat.Dir) { Remove-Item $plat.Dir -Recurse -Force }

        dotnet publish $MainProject `
            -c Release -r $plat.Rid --self-contained `
            -o $plat.Dir -p:Version=$Version

        if ($LASTEXITCODE -ne 0) { throw "Build failed for $($plat.Rid)." }
        Write-Host "  Built $($plat.Rid)." -ForegroundColor Green
    }

    # ── Pack all platforms ──
    foreach ($plat in $Platforms) {
        Write-Host "`n=== Packing $($plat.Rid) ===" -ForegroundColor Cyan
        if (Test-Path $plat.Out) { Remove-Item $plat.Out -Recurse -Force }

        $packArgs = @(
            "pack"
            "-u", $AppName
            "-v", $Version
            "-p", $plat.Dir
            "-e", $plat.Exe
            "--channel", $Channel
            "-o", $plat.Out
        )

        if ($plat.Sign) {
            $packArgs += "--signParams"
            $packArgs += "/sha1 $CertThumbprint /fd SHA256 /tr $TimestampUrl /td SHA256"
        }

        & $vpkPath @packArgs
        if ($LASTEXITCODE -ne 0) { throw "Pack failed for $($plat.Rid)." }
        Write-Host "  Packed $($plat.Rid)." -ForegroundColor Green
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
foreach ($plat in $Platforms) {
    Get-ChildItem $plat.Out | ForEach-Object {
        $sizeMB = [math]::Round($_.Length / 1MB, 1)
        $label = if ($plat.Sign) { "(signed)" } else { "" }
        Write-Host "  $($_.Name) ($sizeMB MB) $label" -ForegroundColor Green
        $allFiles += $_.FullName
    }
}

# ── Upload ──
if ($DryRun) {
    Write-Host "`n=== DRY RUN — skipping upload ===" -ForegroundColor Yellow
    Write-Host "  Would create release: $releaseTag with $($allFiles.Count) files"
}
else {
    Write-Host "`n=== Uploading to GitHub Release ===" -ForegroundColor Cyan

    $prerelease = if ($Channel -eq 'stable') { @() } else { @("--prerelease") }

    # Delete existing release if present
    gh release view $releaseTag --repo $Repo --json tagName 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Replacing existing release: $releaseTag" -ForegroundColor Yellow
        gh release delete $releaseTag --repo $Repo --yes --cleanup-tag 2>$null
    }

    # Create release
    gh release create $releaseTag `
        --repo $Repo `
        --title "$AppName $Version" `
        @prerelease `
        --notes "See CHANGELOG.md for details." `
        @($allFiles)

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Release creation failed."
        exit 1
    }

    Write-Host "`n  Released: https://github.com/$Repo/releases/tag/$releaseTag" -ForegroundColor Green
}

Pop-Location
