<#
.SYNOPSIS
    EveLens Promotion System - Standardized workflow for pushing code through branches.

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

All notable changes to EveLens will be documented in this file.

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
        "stable" { "evelens-patch.xml" }
        "beta" { "evelens-patch-beta.xml" }
        "alpha" { "evelens-patch-alpha.xml" }
    }

    $file = Join-Path (Join-Path $RepoRoot "updates") $fileName
    $date = Get-Date -Format "yyyy-MM-dd"
    $assemblyVersion = Get-AssemblyVersion $Version $Channel

    $tagName = if ($Channel -eq "stable") { "v$Version" } else { $Channel }
    $installerName = "EveLens-install-$($Version -replace '-.*','').exe"

    $maxReleases = 10

    if (Test-Path $file) {
        # Load existing XML and prepend the new release, keeping up to $maxReleases
        [xml]$xml = Get-Content $file -Raw
        $releases = $xml.SelectSingleNode("//releases")

        $newRelease = $xml.CreateElement("release")

        $dateEl = $xml.CreateElement("date"); $dateEl.InnerText = $date
        $versionEl = $xml.CreateElement("version"); $versionEl.InnerText = $assemblyVersion
        $urlEl = $xml.CreateElement("url"); $urlEl.InnerText = "https://github.com/aliacollins/evelens/releases/tag/$tagName"
        $patchUrlEl = $xml.CreateElement("autopatchurl"); $patchUrlEl.InnerText = "https://github.com/aliacollins/evelens/releases/download/$tagName/$installerName"
        $patchArgsEl = $xml.CreateElement("autopatchargs"); $patchArgsEl.InnerText = "/SILENT"
        $messageEl = $xml.CreateElement("message")
        $cdata = $xml.CreateCDataSection("EveLens $Version`n`n$Message")
        $messageEl.AppendChild($cdata) | Out-Null

        $newRelease.AppendChild($dateEl) | Out-Null
        $newRelease.AppendChild($versionEl) | Out-Null
        $newRelease.AppendChild($urlEl) | Out-Null
        $newRelease.AppendChild($patchUrlEl) | Out-Null
        $newRelease.AppendChild($patchArgsEl) | Out-Null
        $newRelease.AppendChild($messageEl) | Out-Null

        # Prepend new release as first child
        if ($releases.HasChildNodes) {
            $releases.InsertBefore($newRelease, $releases.FirstChild) | Out-Null
        } else {
            $releases.AppendChild($newRelease) | Out-Null
        }

        # Trim to keep at most $maxReleases
        $releaseNodes = $releases.SelectNodes("release")
        while ($releaseNodes.Count -gt $maxReleases) {
            $releases.RemoveChild($releaseNodes[$releaseNodes.Count - 1]) | Out-Null
            $releaseNodes = $releases.SelectNodes("release")
        }

        if (-not $DryRun) {
            $xml.Save($file)
        }
    } else {
        # No existing file — create from scratch
        $content = @"
<?xml version="1.0" encoding="utf-8"?>
<!--
  $($Channel.ToUpper()) Update Channel
  This file is checked by EveLens $($Channel.ToUpper()) builds for updates.
-->
<evelens>
  <releases>
    <release>
      <date>$date</date>
      <version>$assemblyVersion</version>
      <url>https://github.com/aliacollins/evelens/releases/tag/$tagName</url>
      <autopatchurl>https://github.com/aliacollins/evelens/releases/download/$tagName/$installerName</autopatchurl>
      <autopatchargs>/SILENT</autopatchargs>
      <message><![CDATA[EveLens $Version

$Message]]></message>
    </release>
  </releases>
  <datafiles>
  </datafiles>
</evelens>
"@

        if (-not $DryRun) {
            Set-Content $file $content
        }
    }
    Write-Success "$fileName updated"
}

# ============================================================================
# README MANAGEMENT
# ============================================================================

function Update-ReadmeVersion {
    param(
        [string]$Version,
        [string]$Channel,
        [string]$TestCount = ""
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

    # Update "Current Version:" line (bold text format: **Current Version: X.Y.Z**)
    $content = $content -replace '\*\*Current Version: [^\*]+\*\*', "**Current Version: $Version**"

    # Update Quick Start AppImage filename version
    $content = $content -replace 'EveLens-[0-9]+\.[0-9]+\.[0-9]+(-[a-z]+\.[0-9]+)?-linux-x64\.AppImage', "EveLens-$Version-linux-x64.AppImage"

    # Update test count if provided (matches "N,NNN tests passing" or "N tests passing")
    if ($TestCount) {
        $content = $content -replace '[0-9,]+ tests passing', "$TestCount tests passing"
    }

    # Update version in "Current experimental features" section
    $content = $content -replace 'experimental features \(v[^\)]+\)', "experimental features (v$Version)"

    # Update installer download link based on channel
    $installerUrl = switch ($Channel) {
        "alpha"  { "https://github.com/aliacollins/evelens/releases/tag/alpha" }
        "beta"   { "https://github.com/aliacollins/evelens/releases/tag/beta" }
        "stable" { "https://github.com/aliacollins/evelens/releases/latest" }
    }
    $content = $content -replace '\[EveLens Installer\]\(https://github\.com/aliacollins/evelens/releases/[^)]+\)', "[EveLens Installer]($installerUrl)"

    # Update "you are here" marker in Update Channels table
    # First, remove existing marker and bold from all channel names
    $content = $content -replace '\s*\(you are here\)', ''
    $content = $content -replace '\| \*\*(Stable)\*\* \|', '| Stable |'
    $content = $content -replace '\| \*\*(Beta)\*\* \|', '| Beta |'
    $content = $content -replace '\| \*\*(Alpha)\*\* \|', '| Alpha |'

    # Bold the active channel and add "you are here" marker
    $activeChannel = switch ($Channel) {
        "alpha"  { "Alpha" }
        "beta"   { "Beta" }
        "stable" { "Stable" }
    }
    $replacement = "| **$activeChannel** | `$1(you are here) |"
    $content = $content -replace "\| $activeChannel \| ([^|]+?)\s*\|", $replacement

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

    $result = & dotnet build (Join-Path $RepoRoot "EveLens.sln") -c Debug --nologo -v q 2>&1
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
    # Use branch --show-current to avoid ambiguity when branch and tag share a name
    # (git rev-parse --abbrev-ref returns "heads/alpha" when both branch and tag "alpha" exist)
    return (git branch --show-current).Trim()
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
        if ($LASTEXITCODE -ne 0) { throw "git add failed (exit code $LASTEXITCODE)" }
        git commit -m $Message
        if ($LASTEXITCODE -ne 0) { throw "git commit failed (exit code $LASTEXITCODE)" }
    }
    Write-Success "Committed: $Message"
}

function Invoke-GitPush {
    param([string]$Branch)

    if (-not $DryRun) {
        # Use --no-verify to bypass our own pre-push hook (we're the official promote script)
        # Use explicit refs/heads/ to avoid ambiguity with tags of the same name (alpha, beta)
        git push --no-verify origin "refs/heads/${Branch}:refs/heads/${Branch}"
        if ($LASTEXITCODE -ne 0) { throw "git push to $Branch failed (exit code $LASTEXITCODE)" }
    }
    Write-Success "Pushed to origin/$Branch"
}

function Invoke-GitMerge {
    param(
        [string]$SourceBranch,
        [string]$TargetBranch
    )

    if (-not $DryRun) {
        # Use -B to checkout as a branch (not detached HEAD) while using
        # explicit refs/heads/ to avoid ambiguity with tags of the same name
        git checkout -B $TargetBranch "refs/heads/$TargetBranch"
        if ($LASTEXITCODE -ne 0) { throw "git checkout $TargetBranch failed (exit code $LASTEXITCODE)" }

        # First try a normal merge
        git merge $SourceBranch --no-ff -m "Merge $SourceBranch into $TargetBranch"
        if ($LASTEXITCODE -ne 0) {
            # Merge conflicted — abort and retry with -X theirs strategy.
            # Cross-branch promotes (alpha→beta, beta→stable) always conflict on
            # version files (SharedAssemblyInfo, README badges, patch XMLs) because
            # both branches bump them independently. Using -X theirs takes the source
            # branch content, then Phase 3 applies the correct target version on top.
            git merge --abort 2>$null
            Write-Warning "Normal merge conflicted. Retrying with source-wins strategy..."
            git merge $SourceBranch --no-ff -X theirs -m "Merge $SourceBranch into $TargetBranch"
            if ($LASTEXITCODE -ne 0) {
                git merge --abort 2>$null
                throw "Merge conflict: $SourceBranch into $TargetBranch failed even with -X theirs. Manual resolution required."
            }
        }
    }
    Write-Success "Merged $SourceBranch -> $TargetBranch"
}

# ============================================================================
# VALIDATION
# ============================================================================

function Test-ReadmeStructure {
    param([string]$Channel)

    $readme = Get-Content (Join-Path $RepoRoot "README.md") -Raw
    $issues = @()

    if ($Channel -eq "alpha") {
        if ($readme -notmatch "## Alpha Changelog \(Cumulative\)") {
            $issues += "README missing '## Alpha Changelog (Cumulative)' section (required by release-alpha.ps1)"
        }
        if ($readme -notmatch "## Features Being Tested") {
            $issues += "README missing '## Features Being Tested' section (required by release-alpha.ps1)"
        }
    }
    if ($Channel -in @("beta", "stable")) {
        if ($readme -notmatch "## What's New in \d+\.\d+\.\d+") {
            $issues += "README missing '## What's New in X.Y.Z' section (required by release-beta.ps1)"
        }
    }

    return $issues
}

function Test-GitHubRelease {
    param(
        [string]$Tag,
        [string]$ExpectedVersion
    )

    try {
        $releaseJson = gh release view $Tag --json name,tagName --repo aliacollins/evelens 2>$null
        if (-not $releaseJson) {
            Write-Warning "GitHub release '$Tag' not found"
            return $false
        }
        $release = $releaseJson | ConvertFrom-Json
        if ($release.name -notmatch [regex]::Escape($ExpectedVersion)) {
            Write-Warning "GitHub release title '$($release.name)' doesn't contain '$ExpectedVersion'"
            return $false
        }
        Write-Success "GitHub release verified: $($release.name)"
        return $true
    } catch {
        Write-Warning "Could not verify GitHub release: $_"
        return $false
    }
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
    Write-Host "  EveLens Promotion System" -ForegroundColor White
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

    # ================================================================
    # PHASE 1: VALIDATE - No changes made. Safe to fail at any point.
    # ================================================================
    Write-Step "Phase 1: Pre-flight validation..."

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

    # Validate README structure for release scripts
    $readmeIssues = Test-ReadmeStructure $Channel
    if ($readmeIssues.Count -gt 0) {
        foreach ($issue in $readmeIssues) {
            Write-Error $issue
        }
        Write-Error "Fix README structure before promoting. Release scripts depend on these sections."
        exit 1
    }
    Write-Success "README structure valid for $Channel release"

    # Branch validation
    $targetBranch = $Channel
    if ($Channel -eq "stable") { $targetBranch = "main" }

    if ($Channel -eq "beta" -and $currentBranch -ne "alpha") {
        Write-Warning "Promoting to beta from '$currentBranch' instead of 'alpha'"
    }
    if ($Channel -eq "stable" -and $currentBranch -notin @("alpha", "beta")) {
        Write-Warning "Promoting to stable from '$currentBranch' instead of 'alpha' or 'beta'"
    }

    Write-Success "All pre-flight checks passed"

    $commitMsg = switch ($Channel) {
        "alpha" { "Alpha $nextVersion`: $Message" }
        "beta" { "Beta $nextVersion`: $Message" }
        "stable" { "Release v$nextVersion" }
    }

    $isCrossBranch = ($currentBranch -ne $targetBranch)

    if ($isCrossBranch) {
        # ================================================================
        # CROSS-BRANCH PROMOTE (e.g., feature->alpha, alpha->beta)
        # Merge first, then update version files on the TARGET branch.
        # This prevents polluting the source branch with target-specific
        # version, badge, installer link, and "you are here" changes.
        # ================================================================

        # Phase 2: Push source and merge to target
        Write-Step "Phase 2: Merging $currentBranch -> $targetBranch..."

        $pushed = $false
        try {
            # Commit any uncommitted changes on source branch first
            if (-not $DryRun) {
                $uncommitted = Get-UncommittedChanges
                if ($uncommitted) {
                    git add -A
                    if ($LASTEXITCODE -ne 0) { throw "git add failed" }
                    git commit -m "Pre-promote: include uncommitted changes on $currentBranch"
                    if ($LASTEXITCODE -ne 0) { throw "git commit failed" }
                    Write-Success "Committed uncommitted changes on $currentBranch"
                }
            }

            Write-Info "Pushing $currentBranch..."
            Invoke-GitPush $currentBranch
            $pushed = $true

            Write-Info "Merging $currentBranch -> $targetBranch..."
            Invoke-GitMerge $currentBranch $targetBranch
        } catch {
            Write-Error "Merge failed: $_"
            if (-not $pushed -and -not $DryRun) {
                Write-Warning "Nothing was pushed. Rolling back..."
                git reset HEAD~1 2>$null
                git checkout -- .
                Write-Warning "Rollback complete."
            } elseif (-not $DryRun) {
                Write-Warning "Source branch was pushed but merge failed."
                Write-Warning "Resolve manually: git checkout $targetBranch && git merge $currentBranch"
            }
            exit 1
        }

        # Phase 3: Update version files on the TARGET branch and commit
        Write-Step "Phase 3: Updating version files on $targetBranch..."

        try {
            Update-SharedAssemblyInfo $nextVersion $Channel
            Update-Changelog $nextVersion $Message $Channel
            Update-PatchXml $nextVersion $Channel $Message
            Update-ReadmeVersion $nextVersion $Channel
        } catch {
            Write-Error "File update failed on $targetBranch`: $_"
            if (-not $DryRun) {
                Write-Warning "Rolling back file changes on $targetBranch..."
                git checkout -- .
                Write-Warning "Rollback complete. Merge is intact but version was not bumped."
            }
            exit 1
        }

        try {
            Invoke-GitCommit $commitMsg
        } catch {
            Write-Error "Commit failed on $targetBranch`: $_"
            if (-not $DryRun) {
                Write-Warning "Rolling back file changes..."
                git checkout -- .
            }
            exit 1
        }

        # Push target
        Write-Step "Pushing $targetBranch..."
        try {
            Invoke-GitPush $targetBranch
        } catch {
            Write-Error "Push to $targetBranch failed: $_"
            Write-Warning "Commit is local. Retry with: git push origin $targetBranch"
            exit 1
        }

    } else {
        # ================================================================
        # SAME-BRANCH PROMOTE (e.g., alpha->alpha when already on alpha)
        # Update files, commit, push - all on the current branch.
        # ================================================================

        # Phase 2: Update version files
        Write-Step "Phase 2: Updating version files..."

        try {
            Update-SharedAssemblyInfo $nextVersion $Channel
            Update-Changelog $nextVersion $Message $Channel
            Update-PatchXml $nextVersion $Channel $Message
            Update-ReadmeVersion $nextVersion $Channel
        } catch {
            Write-Error "File update failed: $_"
            if (-not $DryRun) {
                Write-Warning "Rolling back all file changes..."
                git checkout -- .
                Write-Warning "Rollback complete. No files were changed."
            }
            exit 1
        }

        # Phase 3: Commit and push
        Write-Step "Phase 3: Committing and pushing..."

        try {
            Invoke-GitCommit $commitMsg
        } catch {
            Write-Error "Commit failed: $_"
            if (-not $DryRun) {
                Write-Warning "Rolling back file changes..."
                git checkout -- .
                Write-Warning "Rollback complete. No commit was created."
            }
            exit 1
        }

        try {
            Write-Info "Pushing $targetBranch..."
            Invoke-GitPush $targetBranch
        } catch {
            Write-Error "Push failed: $_"
            if (-not $DryRun) {
                Write-Warning "Rolling back local commit..."
                git reset HEAD~1
                git checkout -- .
                Write-Warning "Rollback complete. No changes were pushed."
            }
            exit 1
        }
    }

    # ================================================================
    # PHASE 4: RELEASE - Best-effort. Push already succeeded.
    # ================================================================
    Write-Step "Phase 4: Building and creating GitHub release..."
    $releaseScript = switch ($Channel) {
        "alpha"  { "release-alpha.ps1" }
        "beta"   { "release-beta.ps1" }
        "stable" { "release-stable.ps1" }
    }
    $releaseScriptPath = Join-Path $ScriptDir $releaseScript

    if ($DryRun) {
        Write-Info "[DRY RUN] Would run: $releaseScript"
    } else {
        try {
            if ($Channel -eq "stable") {
                $stableVersion = $nextVersion -replace '-.*$', ''
                & $releaseScriptPath $stableVersion
            } else {
                & $releaseScriptPath
            }
            Write-Success "Release script completed"
        } catch {
            Write-Warning "Release script failed: $_"
            Write-Warning "Run manually: .\scripts\$releaseScript"
        }
    }

    # Post-flight: Verify GitHub release exists with correct version
    Write-Step "Verifying GitHub release..."
    $releaseTag = switch ($Channel) {
        "alpha"  { "alpha" }
        "beta"   { "beta" }
        "stable" { "v$nextVersion" }
    }

    if ($DryRun) {
        Write-Info "[DRY RUN] Would verify GitHub release tag: $releaseTag"
    } else {
        Test-GitHubRelease $releaseTag $nextVersion | Out-Null
    }

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
}

# Run
Invoke-Promote
