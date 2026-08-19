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

    # Fetch first to ensure remote refs are up to date
    git fetch origin "refs/heads/${Branch}:refs/remotes/origin/${Branch}" 2>$null

    # Try remote ref first (authoritative, avoids stale local refs and
    # ambiguous refname issues when tags share the branch name)
    foreach ($ref in @("refs/remotes/origin/${Branch}", "refs/heads/${Branch}")) {
        try {
            $content = git show "${ref}:SharedAssemblyInfo.cs" 2>$null
            # git show returns an array of lines in PowerShell — join before matching
            # so -match populates $matches capture groups instead of filtering the array
            $joined = ($content -join "`n")
            if ($joined -match 'AssemblyInformationalVersion\("([^"]+)"\)') {
                return $matches[1]
            }
        } catch { }
    }
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

    # A null read means the fetch or show failed (stale lock file, network, etc.).
    # NEVER proceed on a guess — a silently-assumed build counter stamps a version
    # that collides with an existing release (bit us 2026-06-03 and 2026-08-19).
    if (-not $targetVersion) {
        throw ("Could not read SharedAssemblyInfo.cs from '$targetBranch'. " +
            "Check 'git fetch' health (stale .git/refs/**/*.lock?) and retry.")
    }

    if ($targetVersion) {
        try {
            $tv = Parse-Version $targetVersion
            # Only inherit the target branch's build counter when it belongs to the
            # SAME version line. A new major/minor/patch starts its own count at 1 —
            # otherwise the first 1.5.0 alpha after 1.4.0-alpha.6 would be alpha.7.
            if ($tv.Channel -eq $TargetChannel -and
                $tv.Major -eq $v.Major -and
                $tv.Minor -eq $v.Minor -and
                $tv.Patch -eq $v.Patch) {
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
            # Alpha/beta: leave the changelog alone. The changelog is human-curated
            # (Keep a Changelog categories); the promote -Message belongs in the
            # commit only. Injecting it here put raw one-liners at the top of every
            # release's notes (shipped that way in 1.5.0-beta.1 -- never again).
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

function Invoke-PullRequestMerge {
    param(
        [string]$PromoteBranch,
        [string]$TargetBranch,
        [string]$Title,
        [string]$Body
    )

    if (-not $DryRun) {
        # Push the promote branch
        git push --no-verify origin "refs/heads/${PromoteBranch}:refs/heads/${PromoteBranch}"
        if ($LASTEXITCODE -ne 0) { throw "git push promote branch failed (exit code $LASTEXITCODE)" }
        Write-Success "Pushed promote branch: $PromoteBranch"

        # Create PR
        $prUrl = gh pr create --base $TargetBranch --head $PromoteBranch --title $Title --body $Body --repo aliacollins/EveLens 2>&1
        if ($LASTEXITCODE -ne 0) { throw "PR creation failed: $prUrl" }
        Write-Success "Created PR: $prUrl"

        # Merge PR (0 approvals required, merges immediately)
        gh pr merge $prUrl --merge --repo aliacollins/EveLens 2>&1
        if ($LASTEXITCODE -ne 0) { throw "PR merge failed for $prUrl" }
        Write-Success "Merged PR into $TargetBranch"

        # Clean up remote promote branch
        git push origin --delete "refs/heads/$PromoteBranch" 2>$null
        Write-Info "Deleted remote promote branch"
    } else {
        Write-Info "[DRY RUN] Would push $PromoteBranch, create PR to $TargetBranch, and merge"
    }
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

        # CRITICAL: merge by explicit refs/heads/ — a bare branch name is ambiguous when a
        # tag with the same name exists, and git resolves the TAG. This exact failure shipped
        # v1.4.0 built from the months-old 'beta' release TAG instead of the beta branch.
        $sourceRef = "refs/heads/$SourceBranch"
        git show-ref --verify --quiet $sourceRef
        if ($LASTEXITCODE -ne 0) { throw "Source branch '$SourceBranch' does not exist as a local branch ($sourceRef)" }
        $expectedTip = git rev-parse $sourceRef

        # First try a normal merge
        git merge $sourceRef --no-ff -m "Merge $SourceBranch into $TargetBranch"
        if ($LASTEXITCODE -ne 0) {
            # Merge conflicted — abort and retry with -X theirs strategy.
            # Cross-branch promotes (alpha→beta, beta→stable) always conflict on
            # version files (SharedAssemblyInfo, README badges, patch XMLs) because
            # both branches bump them independently. Using -X theirs takes the source
            # branch content, then Phase 3 applies the correct target version on top.
            git merge --abort 2>$null
            Write-Warning "Normal merge conflicted. Retrying with source-wins strategy..."
            git merge $sourceRef --no-ff -X theirs -m "Merge $SourceBranch into $TargetBranch"
            if ($LASTEXITCODE -ne 0) {
                git merge --abort 2>$null
                throw "Merge conflict: $SourceBranch into $TargetBranch failed even with -X theirs. Manual resolution required."
            }
        }

        # POST-MERGE CONTENT VERIFICATION: the source branch tip must now be an ancestor of
        # the target. If it isn't, the merge brought in something other than the branch we
        # asked for — hard stop before anything gets pushed or released.
        git merge-base --is-ancestor $expectedTip HEAD
        if ($LASTEXITCODE -ne 0) {
            throw "CONTENT VERIFICATION FAILED: $SourceBranch tip ($expectedTip) is not contained in the merge result. The merge picked up the wrong ref. Aborting."
        }
        Write-Success "Content verified: $SourceBranch tip $($expectedTip.Substring(0,9)) is in the merge"
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

    # All promotions use a temporary branch + PR to satisfy branch protection.
    # Protected branches (main, alpha, beta) require PRs — no direct push.
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $promoteBranch = "promote/$Channel-$timestamp"

    if ($isCrossBranch) {
        # ================================================================
        # CROSS-BRANCH PROMOTE (e.g., feature->alpha, alpha->beta)
        # Merge source into a promote branch based on target, update version
        # files, then PR into the protected target branch.
        # ================================================================

        Write-Step "Phase 2: Preparing promote branch from $targetBranch..."

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

            if (-not $DryRun) {
                # Create promote branch from target, merge source into it
                git fetch origin "refs/heads/${targetBranch}:refs/remotes/origin/${targetBranch}" 2>$null
                git checkout -B $promoteBranch "origin/$targetBranch"
                if ($LASTEXITCODE -ne 0) { throw "git checkout promote branch from $targetBranch failed" }

                # CRITICAL: merge by explicit refs/heads/ — a bare name resolves to a TAG when
                # one shares the name (this shipped v1.4.0 from the stale 'beta' release tag).
                $sourceRef = "refs/heads/$currentBranch"
                git show-ref --verify --quiet $sourceRef
                if ($LASTEXITCODE -ne 0) { throw "Source branch '$currentBranch' does not exist as a local branch ($sourceRef)" }
                $expectedTip = git rev-parse $sourceRef

                # Merge source branch into promote branch
                git merge $sourceRef --no-ff -m "Merge $currentBranch into $targetBranch"
                if ($LASTEXITCODE -ne 0) {
                    git merge --abort 2>$null
                    Write-Warning "Normal merge conflicted. Retrying with source-wins strategy..."
                    git merge $sourceRef --no-ff -X theirs -m "Merge $currentBranch into $targetBranch"
                    if ($LASTEXITCODE -ne 0) {
                        git merge --abort 2>$null
                        throw "Merge conflict: $currentBranch into promote branch failed even with -X theirs. Manual resolution required."
                    }
                }

                # POST-MERGE CONTENT VERIFICATION: source tip must be an ancestor of the result
                git merge-base --is-ancestor $expectedTip HEAD
                if ($LASTEXITCODE -ne 0) {
                    throw "CONTENT VERIFICATION FAILED: $currentBranch tip ($expectedTip) is not in the merge result. Wrong ref was merged. Aborting."
                }
                Write-Success "Content verified: $currentBranch tip $($expectedTip.Substring(0,9)) is in the merge"
                Write-Success "Merged $currentBranch into promote branch"
            }
        } catch {
            Write-Error "Merge failed: $_"
            if (-not $DryRun) {
                git checkout $currentBranch 2>$null
                git branch -D $promoteBranch 2>$null
            }
            exit 1
        }

        # Phase 3: Update version files on the promote branch and commit
        Write-Step "Phase 3: Updating version files on promote branch..."

        try {
            Update-SharedAssemblyInfo $nextVersion $Channel
            Update-Changelog $nextVersion $Message $Channel
            Update-PatchXml $nextVersion $Channel $Message
            Update-ReadmeVersion $nextVersion $Channel
        } catch {
            Write-Error "File update failed: $_"
            if (-not $DryRun) {
                git checkout $currentBranch 2>$null
                git branch -D $promoteBranch 2>$null
            }
            exit 1
        }

        try {
            Invoke-GitCommit $commitMsg
        } catch {
            Write-Error "Commit failed: $_"
            if (-not $DryRun) {
                git checkout $currentBranch 2>$null
                git branch -D $promoteBranch 2>$null
            }
            exit 1
        }

        # PR into target
        Write-Step "Phase 3b: Creating PR to $targetBranch..."
        try {
            Invoke-PullRequestMerge $promoteBranch $targetBranch $commitMsg "Automated promotion: $currentBranch -> $targetBranch ($nextVersion)`n`n$Message"
        } catch {
            Write-Error "PR merge failed: $_"
            Write-Warning "Promote branch '$promoteBranch' is still on remote. Create PR manually."
            if (-not $DryRun) { git checkout $currentBranch 2>$null }
            exit 1
        }

        # Return to source branch and sync local target
        if (-not $DryRun) {
            git checkout $currentBranch 2>$null
            git fetch origin "refs/heads/${targetBranch}:refs/heads/${targetBranch}" 2>$null
            git branch -D $promoteBranch 2>$null
        }

    } else {
        # ================================================================
        # SAME-BRANCH PROMOTE (e.g., on alpha, promoting to alpha)
        # Create a promote branch from current state, update version files,
        # then PR into the protected branch.
        # ================================================================

        Write-Step "Phase 2: Preparing promote branch..."

        if (-not $DryRun) {
            git checkout -b $promoteBranch
            if ($LASTEXITCODE -ne 0) { throw "Failed to create promote branch" }
        }

        try {
            Update-SharedAssemblyInfo $nextVersion $Channel
            Update-Changelog $nextVersion $Message $Channel
            Update-PatchXml $nextVersion $Channel $Message
            Update-ReadmeVersion $nextVersion $Channel
        } catch {
            Write-Error "File update failed: $_"
            if (-not $DryRun) {
                git checkout -- .
                git checkout $targetBranch 2>$null
                git branch -D $promoteBranch 2>$null
            }
            exit 1
        }

        # Phase 3: Commit and PR
        Write-Step "Phase 3: Committing and creating PR..."

        try {
            Invoke-GitCommit $commitMsg
        } catch {
            Write-Error "Commit failed: $_"
            if (-not $DryRun) {
                git checkout -- .
                git checkout $targetBranch 2>$null
                git branch -D $promoteBranch 2>$null
            }
            exit 1
        }

        try {
            Invoke-PullRequestMerge $promoteBranch $targetBranch $commitMsg "Automated promotion: $targetBranch ($nextVersion)`n`n$Message"
        } catch {
            Write-Error "PR merge failed: $_"
            Write-Warning "Promote branch '$promoteBranch' is still on remote. Create PR manually."
            if (-not $DryRun) { git checkout $targetBranch 2>$null }
            exit 1
        }

        # Return to target branch and sync
        if (-not $DryRun) {
            git checkout $targetBranch 2>$null
            git pull origin $targetBranch 2>$null
            git branch -D $promoteBranch 2>$null
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
