<#
.SYNOPSIS
    Validates the code graph against the actual codebase to detect drift.

.DESCRIPTION
    Cross-checks .codegraph/ files against live source code. Reports:
    - Events in code but missing from graph
    - Events in graph but removed from code
    - Subscriber/publisher count mismatches
    - Service consumers that moved or vanished
    Returns exit code 1 if any drift detected (suitable for CI).

.PARAMETER GraphDir
    Path to the .codegraph directory (default: .codegraph)

.PARAMETER SrcDir
    Path to source directory (default: src)

.PARAMETER Fix
    If set, regenerates the graph instead of just reporting drift

.EXAMPLE
    .\scripts\validate-codegraph.ps1
    .\scripts\validate-codegraph.ps1 -Fix
#>

param(
    [string]$GraphDir = "",
    [string]$SrcDir = "",
    [switch]$Fix
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

if (-not $GraphDir) { $GraphDir = Join-Path $RepoRoot ".codegraph" }
if (-not $SrcDir) { $SrcDir = Join-Path $RepoRoot "src" }

$driftFound = $false
$issues = @()

function Add-Issue($severity, $category, $message) {
    $script:issues += [PSCustomObject]@{
        Severity = $severity
        Category = $category
        Message = $message
    }
    if ($severity -eq "ERROR") { $script:driftFound = $true }
}

# --- Check graph exists ---
if (-not (Test-Path $GraphDir)) {
    Write-Host "ERROR: No code graph found at $GraphDir. Run scripts/codegraph.ps1 first." -ForegroundColor Red
    exit 1
}

$eventsFile = Join-Path $GraphDir "events.md"
$servicesFile = Join-Path $GraphDir "services.md"
$indexFile = Join-Path $GraphDir "index.md"

if (-not (Test-Path $eventsFile) -or -not (Test-Path $servicesFile)) {
    Write-Host "ERROR: Graph files incomplete. Run scripts/codegraph.ps1 to regenerate." -ForegroundColor Red
    exit 1
}

# --- Extract graph timestamp ---
$indexContent = Get-Content $indexFile -ErrorAction SilentlyContinue
$graphCommit = ""
foreach ($line in $indexContent) {
    if ($line -match 'Commit:\s*(\w+)') {
        $graphCommit = $matches[1]
        break
    }
}

$prevErrorPref = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
$currentCommit = (git -C $RepoRoot rev-parse --short HEAD 2>$null)
$changedFiles = @(git -C $RepoRoot diff --name-only HEAD 2>$null)
$ErrorActionPreference = $prevErrorPref

if ($graphCommit -and $currentCommit -and $graphCommit -ne $currentCommit) {
    Add-Issue "WARN" "Staleness" "Graph was generated at commit $graphCommit, current is $currentCommit"
}

# --- Check for changed files since graph generation ---
$csChanged = $changedFiles | Where-Object { $_ -match '\.cs$' -and $_ -match '^src/' }
if ($csChanged -and $csChanged.Count -gt 0) {
    Add-Issue "WARN" "Staleness" "$($csChanged.Count) .cs files changed since last commit (graph may be stale)"
}

# --- Validate Events: check current code against graph ---
Write-Host "  [validate] Checking event flow..." -ForegroundColor DarkCyan

$csFiles = Get-ChildItem -Path $SrcDir -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" }

$livePublishers = @{}
$liveSubscribers = @{}

foreach ($file in $csFiles) {
    $lines = Get-Content $file.FullName

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Publish
        if ($line -match 'Publish\(\s*new\s+(?:CommonEvents\.)?(\w+Event\w*)\s*[\(\{]') {
            $evt = $matches[1]
            if (-not $livePublishers.ContainsKey($evt)) { $livePublishers[$evt] = 0 }
            $livePublishers[$evt]++
        }
        elseif ($line -match 'Publish\(\s*(?:Common\.Events\.)?(?:CommonEvents\.)?(\w+Event\w*)\.Instance') {
            $evt = $matches[1]
            if (-not $livePublishers.ContainsKey($evt)) { $livePublishers[$evt] = 0 }
            $livePublishers[$evt]++
        }

        # Subscribe
        if ($line -match 'Subscribe(?:OnUI)?<\s*(?:Common\.Events\.)?(?:CommonEvents\.)?(?:Core\.Events\.)?(\w+Event\w*)\s*>') {
            $evt = $matches[1]
            if ($evt -match '^T[A-Z]' -or $evt -eq "TEvent") { continue }
            if (-not $liveSubscribers.ContainsKey($evt)) { $liveSubscribers[$evt] = 0 }
            $liveSubscribers[$evt]++
        }
    }
}

# Parse graph events
$graphEvents = @{}
$eventsContent = Get-Content $eventsFile
$currentEvent = ""
foreach ($line in $eventsContent) {
    if ($line -match '^## (\w+Event\w*)') {
        $currentEvent = $matches[1]
        $graphEvents[$currentEvent] = @{ pubs = 0; subs = 0 }
    }
    elseif ($currentEvent -and $line -match '^\s+- src/') {
        # Count entries under published/subscribed sections
        # We track these via section context
    }
}

# Parse quick-ref for counts (more reliable)
$quickRefFile = Join-Path $GraphDir "quick-ref.md"
$quickRefContent = Get-Content $quickRefFile
$graphEventCounts = @{}
foreach ($line in $quickRefContent) {
    if ($line -match '^(\w+Event\w*)\s*\|\s*pub:(\d+)\s+sub:(\d+)') {
        $graphEventCounts[$matches[1]] = @{ pub = [int]$matches[2]; sub = [int]$matches[3] }
    }
}

# Compare live vs graph
$allLiveEvents = @($livePublishers.Keys) + @($liveSubscribers.Keys) | Sort-Object -Unique

foreach ($evt in $allLiveEvents) {
    if (-not $graphEventCounts.ContainsKey($evt)) {
        Add-Issue "ERROR" "Events" "Event '$evt' exists in code but MISSING from graph"
    } else {
        $graphPub = $graphEventCounts[$evt].pub
        $graphSub = $graphEventCounts[$evt].sub
        $livePub = if ($livePublishers.ContainsKey($evt)) { $livePublishers[$evt] } else { 0 }
        $liveSub = if ($liveSubscribers.ContainsKey($evt)) { $liveSubscribers[$evt] } else { 0 }

        if ($graphPub -ne $livePub) {
            Add-Issue "ERROR" "Events" "Event '$evt' publisher count: graph=$graphPub, actual=$livePub"
        }
        if ($graphSub -ne $liveSub) {
            Add-Issue "ERROR" "Events" "Event '$evt' subscriber count: graph=$graphSub, actual=$liveSub"
        }
    }
}

# Check for events in graph but gone from code
foreach ($evt in $graphEventCounts.Keys) {
    $livePub = if ($livePublishers.ContainsKey($evt)) { $livePublishers[$evt] } else { 0 }
    $liveSub = if ($liveSubscribers.ContainsKey($evt)) { $liveSubscribers[$evt] } else { 0 }
    if ($livePub -eq 0 -and $liveSub -eq 0 -and -not $allLiveEvents.Contains($evt)) {
        Add-Issue "WARN" "Events" "Event '$evt' in graph but no longer in code (dead entry)"
    }
}

# --- Validate Services: check consumer counts ---
Write-Host "  [validate] Checking service registry..." -ForegroundColor DarkCyan

$servicesContent = Get-Content $servicesFile
$graphServiceCounts = @{}
foreach ($line in $servicesContent) {
    if ($line -match '^## (\w+)\s*\([^)]+\)\s*\[(\d+) consumers\]') {
        $graphServiceCounts[$matches[1]] = [int]$matches[2]
    }
}

# Spot check top services (full recount is expensive, check top 5)
$topServices = $graphServiceCounts.GetEnumerator() | Sort-Object -Property Value -Descending | Select-Object -First 5
foreach ($svc in $topServices) {
    $pattern = "AppServices\.$($svc.Key)\b"
    $liveCount = 0
    foreach ($file in $csFiles) {
        $relPath = $file.FullName.Replace($RepoRoot, "").Replace("\", "/").TrimStart("/")
        if ($relPath -match "AppServices\.cs$") { continue }
        $content = Get-Content $file.FullName -Raw
        if ($content -match $pattern) { $liveCount++ }
    }
    if ($liveCount -ne $svc.Value) {
        Add-Issue "ERROR" "Services" "Service '$($svc.Key)' consumer count: graph=$($svc.Value), actual=$liveCount"
    }
}

# --- Validate Interfaces: check implementor counts ---
Write-Host "  [validate] Checking interface implementations..." -ForegroundColor DarkCyan

$interfacesFile = Join-Path $GraphDir "interfaces.md"
$interfacesContent = Get-Content $interfacesFile
$graphInterfaceCounts = @{}
foreach ($line in $interfacesContent) {
    if ($line -match '^## (I\w+)\s*\[(\d+) impl\]') {
        $graphInterfaceCounts[$matches[1]] = [int]$matches[2]
    }
}

# Spot check interfaces with implementors
$ifacesWithImpls = $graphInterfaceCounts.GetEnumerator() | Where-Object { $_.Value -gt 0 } | Select-Object -First 5
foreach ($iface in $ifacesWithImpls) {
    $liveCount = 0
    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName -Raw
        if ($content -match "class\s+\w+\s*(?:<[^>]*>)?\s*:\s*[^{]*\b$($iface.Key)\b") {
            $liveCount++
        }
    }
    if ($liveCount -ne $iface.Value) {
        Add-Issue "ERROR" "Interfaces" "Interface '$($iface.Key)' implementor count: graph=$($iface.Value), actual=$liveCount"
    }
}

# --- Report ---
Write-Host ""
if ($issues.Count -eq 0) {
    Write-Host "  [validate] PASS - Code graph is in sync with codebase" -ForegroundColor Green
} else {
    $errors = ($issues | Where-Object { $_.Severity -eq "ERROR" }).Count
    $warnings = ($issues | Where-Object { $_.Severity -eq "WARN" }).Count

    if ($errors -gt 0) {
        Write-Host "  [validate] DRIFT DETECTED - $errors errors, $warnings warnings" -ForegroundColor Red
    } else {
        Write-Host "  [validate] STALE - $warnings warnings (no structural drift)" -ForegroundColor Yellow
    }

    Write-Host ""
    foreach ($issue in $issues) {
        $color = if ($issue.Severity -eq "ERROR") { "Red" } elseif ($issue.Severity -eq "WARN") { "Yellow" } else { "White" }
        Write-Host "  [$($issue.Severity)] [$($issue.Category)] $($issue.Message)" -ForegroundColor $color
    }
}

# --- Auto-fix if requested ---
if ($Fix -and $driftFound) {
    Write-Host ""
    Write-Host "  [validate] Regenerating graph to fix drift..." -ForegroundColor Cyan
    $genScript = Join-Path $RepoRoot "scripts/codegraph.ps1"
    & $genScript -Quiet
    Write-Host "  [validate] Graph regenerated. Re-validating..." -ForegroundColor Cyan
    & $MyInvocation.MyCommand.Path -GraphDir $GraphDir -SrcDir $SrcDir
    exit $LASTEXITCODE
}

if ($driftFound) { exit 1 } else { exit 0 }
