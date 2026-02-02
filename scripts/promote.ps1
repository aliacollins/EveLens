<#
.SYNOPSIS
    EVEMon Promotion System - Standardized workflow for pushing code through branches.

.DESCRIPTION
    This script handles all branch promotions with automatic versioning,
    README updates, changelog management, and release creation.

.PARAMETER Channel
    Target channel: alpha, beta, or stable

.PARAMETER Message
    Summary of changes (required for alpha/beta, optional for stable)

.PARAMETER SkipBuild
    Skip build verification

.PARAMETER DryRun
    Show what would happen without making changes

.EXAMPLE
    .\promote.ps1 alpha -Message "Added installer support"
    .\promote.ps1 beta -Message "Ready for beta testing"
    .\promote.ps1 stable -Message "Production release"
#>

param(
    [Parameter(Mandatory=$true, Position=0)]
    [ValidateSet("alpha", "beta", "stable")]
    [string]$Channel,

    [Parameter(Mandatory=$false)]
    [string]$Message,

    [switch]$SkipBuild,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

# Colors for output
function Write-Step { param($msg) Write-Host "`n>> $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "   OK: $msg" -ForegroundColor Green }
function Write-Warning { param($msg) Write-Host "   WARN: $msg" -ForegroundColor Yellow }
function Write-Error { param($msg) Write-Host "   ERROR: $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "   $msg" -ForegroundColor Gray }

# ============================================================================
# VERSION MANAGEMENT
# ============================================================================

function Get-CurrentVersion {
    $sharedAssemblyInfo = Join-Path $RepoRoot "SharedAssemblyInfo.cs"
    $content = Get-Content $sharedAssemblyInfo -Raw

    if ($content -match 'AssemblyInformationalVersion\("([^"]+)"\)') {
        return $matches[1]
    }
    throw "Could not find AssemblyInformationalVersion in SharedAssemblyInfo.cs"
}

function Parse-Version {
    param([string]$Version)

    # Parse versions like "5.2.0", "5.2.0-alpha.1", "5.2.0-beta.2"
    if ($Version -match '^(\d+)\.(\d+)\.(\d+)(?:-(alpha|beta)\.(\d+))?$') {
        return @{
            Major = [int]$matches[1]
            Minor = [int]$matches[2]
            Patch = [int]$matches[3]
            Channel = if ($matches[4]) { $matches[4] } else { "stable" }
            Build = if ($matches[5]) { [int]$matches[5] } else { 0 }
        }
    }
    throw "Invalid version format: $Version"
}

function Get-BranchVersion {
    param([string]$Branch)

    try {
        $content = git show "refs/heads/${Branch}:SharedAssemblyInfo.cs" 2>$null
        if ($content -match 'AssemblyInformationalVersion\("([^"]+)"\)') {
            return $matches[1]
        }
    } catch { }
    return $null
}

function Get-NextVersion {
    param(
        [string]$CurrentVersion,
        [string]$TargetChannel
    )

    $v = Parse-Version $CurrentVersion

    # Check target branch's current version to handle re-promotions correctly
    $targetBranch = if ($TargetChannel -eq "stable") { "main" } else { $TargetChannel }
    $targetVersion = Get-BranchVersion $targetBranch
    $targetBuild = 0

    if ($targetVersion) {
        try {
            $tv = Parse-Version $targetVersion
            if ($tv.Channel -eq $TargetChannel) {
                $targetBuild = $tv.Build
            }
        } catch { }
    }

    switch ($TargetChannel) {
        "alpha" {
            if ($v.Channel -eq "alpha") {
                # Increment alpha build: alpha.1 -> alpha.2
                # Use max of current+1 or target+1 to handle re-promotions
                $nextBuild = [Math]::Max($v.Build + 1, $targetBuild + 1)
                return "$($v.Major).$($v.Minor).$($v.Patch)-alpha.$nextBuild"
            } else {
                # Start new alpha: 5.2.0 -> 5.2.1-alpha.1 or 5.2.0-beta.1 -> 5.2.0-alpha.1
                $nextBuild = [Math]::Max(1, $targetBuild + 1)
                if ($v.Channel -eq "stable") {
                    return "$($v.Major).$($v.Minor).$($v.Patch + 1)-alpha.$nextBuild"
                }
                return "$($v.Major).$($v.Minor).$($v.Patch)-alpha.$nextBuild"
            }
        }
        "beta" {
            # Always use target branch build + 1, or 1 if no beta exists
            $nextBuild = [Math]::Max(1, $targetBuild + 1)
            return "$($v.Major).$($v.Minor).$($v.Patch)-beta.$nextBuild"
        }
        "stable" {
            # Drop pre-release tag: 5.2.0-alpha.N or 5.2.0-beta.N -> 5.2.0
            return "$($v.Major).$($v.Minor).$($v.Patch)"
        }
    }
}

function Get-AssemblyVersion {
    param([string]$Version, [string]$Channel)

    $v = Parse-Version $Version

    # Stable uses revision 0, pre-release uses build number
    $revision = if ($Channel -eq "stable") { 0 } else { $v.Build }

    return "$($v.Major).$($v.Minor).$($v.Patch).$revision"
}

function Update-SharedAssemblyInfo {
    param([string]$Version, [string]$Channel)

    $file = Join-Path $RepoRoot "SharedAssemblyInfo.cs"
    $assemblyVersion = Get-AssemblyVersion $Version $Channel

    $content = Get-Content $file -Raw
    $content = $content -replace 'AssemblyVersion\("[^"]+"\)', "AssemblyVersion(`"$assemblyVersion`")"
    $content = $content -replace 'AssemblyFileVersion\("[^"]+"\)', "AssemblyFileVersion(`"$assemblyVersion`")"
    $content = $content -replace 'AssemblyInformationalVersion\("[^"]+"\)', "AssemblyInformationalVersion(`"$Version`")"

    if (-not $DryRun) {
        Set-Content $file $content -NoNewline
    }
    Write-Success "SharedAssemblyInfo.cs -> $Version ($assemblyVersion)"
}

# ============================================================================
# CHANGELOG MANAGEMENT
# ============================================================================

function Update-Changelog {
    param(
        [string]$Version,
        [string]$Message,
        [string]$Channel
    )

    $file = Join-Path $RepoRoot "CHANGELOG.md"
    $date = Get-Date -Format "yyyy-MM-dd"

    if (-not (Test-Path $file)) {
        # Create new changelog
        $content = @"
# Changelog

All notable changes to EVEMon will be documented in this file.

## [Unreleased]

## [$Version] - $date
- $Message

"@
    } else {
        $content = Get-Content $file -Raw

        if ($Channel -eq "stable") {
            # Move Unreleased to versioned section
            $unreleasedMatch = [regex]::Match($content, '## \[Unreleased\]\r?\n([\s\S]*?)(?=\r?\n## \[|$)')
            $unreleasedContent = if ($unreleasedMatch.Success) { $unreleasedMatch.Groups[1].Value.Trim() } else { "- $Message" }

            if ([string]::IsNullOrWhiteSpace($unreleasedContent)) {
                $unreleasedContent = "- $Message"
            }

            $newSection = "## [Unreleased]`n`n## [$Version] - $date`n$unreleasedContent"
            $content = $content -replace '## \[Unreleased\][\s\S]*?(?=\r?\n## \[|$)', $newSection
        } else {
            # Add entry to Unreleased
            $entry = "- $Message"
            $content = $content -replace '(## \[Unreleased\]\r?\n)', "`$1$entry`n"
        }
    }

    if (-not $DryRun) {
        Set-Content $file $content -NoNewline
    }
    Write-Success "CHANGELOG.md updated"
}

# ============================================================================
# PATCH XML MANAGEMENT
# ============================================================================

function Update-PatchXml {
    param(
        [string]$Version,
        [string]$Channel,
        [string]$Message
    )

    $fileName = switch ($Channel) {
        "stable" { "patch.xml" }
        "beta" { "patch-beta.xml" }
        "alpha" { "patch-alpha.xml" }
    }

    $file = Join-Path (Join-Path $RepoRoot "updates") $fileName
    $date = Get-Date -Format "yyyy-MM-dd"
    $assemblyVersion = Get-AssemblyVersion $Version $Channel

    $tagName = if ($Channel -eq "stable") { "v$Version" } else { $Channel }
    $installerName = "EVEMon-install-$($Version -replace '-.*','').exe"

    $content = @"
<?xml version="1.0" encoding="utf-8"?>
<!--
  $($Channel.ToUpper()) Update Channel
  This file is checked by EVEMon $($Channel.ToUpper()) builds for updates.
-->
<evemon>
  <releases>
    <release>
      <date>$date</date>
      <version>$assemblyVersion</version>
      <url>https://github.com/aliacollins/evemon/releases/tag/$tagName</url>
      <autopatchurl>https://github.com/aliacollins/evemon/releases/download/$tagName/$installerName</autopatchurl>
      <autopatchargs>/SILENT</autopatchargs>
      <message><![CDATA[EVEMon $Version

$Message]]></message>
    </release>
  </releases>
  <datafiles>
  </datafiles>
</evemon>
"@

    if (-not $DryRun) {
        Set-Content $file $content
    }
    Write-Success "$fileName updated"
}

# ============================================================================
# README MANAGEMENT
# ============================================================================

function Update-ReadmeVersion {
    param(
        [string]$Version,
        [string]$Channel
    )

    $file = Join-Path $RepoRoot "README.md"
    $content = Get-Content $file -Raw

    # Update version badge
    $badgeColor = switch ($Channel) {
        "stable" { "green" }
        "beta" { "yellow" }
        "alpha" { "red" }
    }
    $badgeText = $Channel.ToUpper()

    # Update the alpha/beta/STABLE badge if present
    $content = $content -replace '\[!\[(ALPHA|BETA|STABLE)\]\([^\)]+\)\]\(\)', "[![$badgeText](https://img.shields.io/badge/branch-$badgeText-$badgeColor.svg)]()"

    # Update "Current Version:" line (e.g., "## Current Version: 5.1.2-beta.1")
    $content = $content -replace '(## Current Version:) [^\r\n]+', "`$1 $Version"

    # Update version in "Current experimental features" section
    $content = $content -replace 'experimental features \(v[^\)]+\)', "experimental features (v$Version)"

    if (-not $DryRun) {
        Set-Content $file $content -NoNewline
    }
    Write-Success "README.md version updated"
}

# ============================================================================
# BUILD VERIFICATION
# ============================================================================

function Test-Build {
    Write-Step "Verifying build..."

    $result = & dotnet build (Join-Path $RepoRoot "EVEMon.sln") -c Debug --nologo -v q 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
        Write-Host $result
        return $false
    }
    Write-Success "Build succeeded"
    return $true
}

# ============================================================================
# GIT OPERATIONS
# ============================================================================

function Get-CurrentBranch {
    return (git rev-parse --abbrev-ref HEAD).Trim()
}

function Test-CleanWorkingTree {
    $status = git status --porcelain
    return [string]::IsNullOrWhiteSpace($status)
}

function Get-UncommittedChanges {
    return git status --porcelain
}

function Invoke-GitCommit {
    param([string]$Message)

    if (-not $DryRun) {
        git add -A
        git commit -m $Message
    }
    Write-Success "Committed: $Message"
}

function Invoke-GitPush {
    param([string]$Branch)

    if (-not $DryRun) {
        # Use --no-verify to bypass our own pre-push hook (we're the official promote script)
        # Use explicit refs/heads/ to avoid ambiguity with tags of the same name (alpha, beta)
        git push --no-verify origin "refs/heads/${Branch}:refs/heads/${Branch}"
    }
    Write-Success "Pushed to origin/$Branch"
}

function Invoke-GitMerge {
    param(
        [string]$SourceBranch,
        [string]$TargetBranch
    )

    if (-not $DryRun) {
        # Use explicit refs to avoid ambiguity with tags of the same name
        git checkout "refs/heads/$TargetBranch" --
        git merge $SourceBranch --no-ff -m "Merge $SourceBranch into $TargetBranch"
    }
    Write-Success "Merged $SourceBranch -> $TargetBranch"
}

# ============================================================================
# MAIN PROMOTION LOGIC
# ============================================================================

function Invoke-Promote {
    $currentBranch = Get-CurrentBranch
    $currentVersion = Get-CurrentVersion
    $nextVersion = Get-NextVersion $currentVersion $Channel

    Write-Host ""
    Write-Host "============================================" -ForegroundColor White
    Write-Host "  EVEMon Promotion System" -ForegroundColor White
    Write-Host "============================================" -ForegroundColor White
    Write-Host ""
    Write-Host "  Current Branch:  $currentBranch" -ForegroundColor Gray
    Write-Host "  Current Version: $currentVersion" -ForegroundColor Gray
    Write-Host "  Target Channel:  $Channel" -ForegroundColor Cyan
    Write-Host "  Next Version:    $nextVersion" -ForegroundColor Green
    Write-Host ""

    if ($DryRun) {
        Write-Host "  [DRY RUN - No changes will be made]" -ForegroundColor Yellow
        Write-Host ""
    }

    # Validation
    Write-Step "Validating..."

    # Check for message
    if (-not $Message) {
        if ($Channel -eq "stable") {
            $Message = "Production release"
        } else {
            Write-Error "Message is required for $Channel promotions. Use -Message `"description`""
            exit 1
        }
    }
    Write-Success "Message: $Message"

    # Check working tree
    $uncommitted = Get-UncommittedChanges
    if ($uncommitted) {
        Write-Warning "Uncommitted changes detected - they will be included in this promotion"
        Write-Info $uncommitted
    }

    # Verify build
    if (-not $SkipBuild) {
        if (-not (Test-Build)) {
            Write-Error "Fix build errors before promoting"
            exit 1
        }
    }

    # Branch logic
    Write-Step "Preparing branches..."

    $targetBranch = $Channel
    if ($Channel -eq "stable") { $targetBranch = "main" }

    # For alpha: can promote from any branch
    # For beta: should come from alpha
    # For stable: should come from beta or alpha

    if ($Channel -eq "beta" -and $currentBranch -ne "alpha") {
        Write-Warning "Promoting to beta from '$currentBranch' instead of 'alpha'"
    }

    if ($Channel -eq "stable" -and $currentBranch -notin @("alpha", "beta")) {
        Write-Warning "Promoting to stable from '$currentBranch' instead of 'alpha' or 'beta'"
    }

    # Update files
    Write-Step "Updating version files..."
    Update-SharedAssemblyInfo $nextVersion $Channel
    Update-Changelog $nextVersion $Message $Channel
    Update-PatchXml $nextVersion $Channel $Message
    Update-ReadmeVersion $nextVersion $Channel

    # Commit changes
    Write-Step "Committing changes..."
    $commitMsg = switch ($Channel) {
        "alpha" { "Alpha $nextVersion`: $Message" }
        "beta" { "Beta $nextVersion`: $Message" }
        "stable" { "Release v$nextVersion" }
    }
    Invoke-GitCommit $commitMsg

    # Handle branch operations
    Write-Step "Pushing changes..."

    if ($currentBranch -ne $targetBranch) {
        # Push source branch first (with the version update commit)
        Write-Info "Pushing $currentBranch..."
        Invoke-GitPush $currentBranch

        # Then merge to target branch
        Write-Info "Merging $currentBranch -> $targetBranch..."
        Invoke-GitMerge $currentBranch $targetBranch
    }

    # Push target branch
    Write-Info "Pushing $targetBranch..."
    Invoke-GitPush $targetBranch

    # Summary
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  PROMOTION COMPLETE" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Version: $nextVersion" -ForegroundColor White
    Write-Host "  Branch:  $targetBranch" -ForegroundColor White
    Write-Host "  Message: $Message" -ForegroundColor White
    Write-Host ""

    Write-Host "  Next steps:" -ForegroundColor Yellow
    switch ($Channel) {
        "alpha" {
            Write-Host "    1. Run: .\scripts\release-alpha.ps1" -ForegroundColor Gray
            Write-Host "    2. This will build and upload to rolling 'alpha' release" -ForegroundColor Gray
        }
        "beta" {
            Write-Host "    1. Run: .\scripts\release-beta.ps1" -ForegroundColor Gray
            Write-Host "    2. This will build and upload to rolling 'beta' release" -ForegroundColor Gray
        }
        "stable" {
            Write-Host "    1. Run: .\scripts\release-stable.ps1 $($nextVersion)" -ForegroundColor Gray
            Write-Host "    2. This will create GitHub release v$nextVersion with installer" -ForegroundColor Gray
        }
    }

    Write-Host ""
}

# Run
Invoke-Promote
