<#
.SYNOPSIS
    EveLens Code Graph Generator — builds a living dependency/event/service graph.

.DESCRIPTION
    Analyzes the codebase using pattern matching to produce structured graph files
    in .codegraph/. Designed for AI agent consumption: grep-friendly, hierarchical,
    concise. Covers events (pub/sub), services, interfaces, assembly deps, settings.

.PARAMETER OutputDir
    Directory for graph output (default: .codegraph)

.PARAMETER Validate
    Run validation after generation (exit 1 if drift detected)

.PARAMETER Quiet
    Suppress progress output

.EXAMPLE
    .\scripts\codegraph.ps1
    .\scripts\codegraph.ps1 -Validate
    .\scripts\codegraph.ps1 -OutputDir .codegraph -Quiet
#>

param(
    [string]$OutputDir = ".codegraph",
    [switch]$Validate,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$SrcDir = Join-Path $RepoRoot "src"
$OutputPath = Join-Path $RepoRoot $OutputDir

function Write-Progress-Msg($msg) {
    if (-not $Quiet) { Write-Host "  [codegraph] $msg" -ForegroundColor DarkCyan }
}

function Get-RelativePath($fullPath) {
    $fullPath.Replace($RepoRoot, "").Replace("\", "/").TrimStart("/")
}

# --- Setup output directory ---
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$gitHash = (& git -C $RepoRoot rev-parse --short HEAD 2>&1 | Where-Object { $_ -is [string] }) | Select-Object -First 1
if (-not $gitHash) { $gitHash = "unknown" }

# ============================================================================
# SECTION 1: Assembly Dependencies
# ============================================================================
Write-Progress-Msg "Analyzing assembly dependencies..."

$projects = Get-ChildItem -Path $SrcDir -Filter "*.csproj" -Recurse
$assemblyGraph = @{}

foreach ($proj in $projects) {
    $projName = [System.IO.Path]::GetFileNameWithoutExtension($proj.Name)
    $content = Get-Content $proj.FullName -Raw
    $refs = [regex]::Matches($content, '<ProjectReference\s+Include="[^"]*\\([^"\\]+)\.csproj"')
    $deps = @()
    foreach ($ref in $refs) {
        $deps += $ref.Groups[1].Value
    }
    $assemblyGraph[$projName] = $deps
}

$assemblyOutput = @"
# Assembly Dependencies
# Generated: $timestamp | Commit: $gitHash
# Format: Assembly -> [dependency, dependency, ...]
# Read as: "Assembly depends on these"

"@

$layerOrder = @(
    "EveLens.Core",
    "EveLens.Data",
    "EveLens.Serialization",
    "EveLens.Models",
    "EveLens.Infrastructure",
    "EveLens.Common",
    "EveLens.Avalonia"
)

foreach ($layer in $layerOrder) {
    if ($assemblyGraph.ContainsKey($layer)) {
        $deps = $assemblyGraph[$layer]
        if ($deps.Count -eq 0) {
            $assemblyOutput += "$layer -> [] (leaf)`n"
        } else {
            $assemblyOutput += "$layer -> [$($deps -join ', ')]`n"
        }
    }
}

$assemblyOutput += "`n## Dependency Flow (top to bottom = more dependencies)`n"
$assemblyOutput += "EveLens.Core (leaf, zero deps)`n"
$assemblyOutput += "  <- EveLens.Data`n"
$assemblyOutput += "    <- EveLens.Serialization`n"
$assemblyOutput += "      <- EveLens.Models`n"
$assemblyOutput += "        <- EveLens.Infrastructure`n"
$assemblyOutput += "          <- EveLens.Common`n"
$assemblyOutput += "            <- EveLens.Avalonia (UI entry point)`n"

Set-Content -Path (Join-Path $OutputPath "assemblies.md") -Value $assemblyOutput -Encoding UTF8

# ============================================================================
# SECTION 2: Event Flow (Publish / Subscribe)
# ============================================================================
Write-Progress-Msg "Analyzing event publish/subscribe flow..."

$csFiles = Get-ChildItem -Path $SrcDir -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" }

$publishers = @{}  # eventType -> @(file:line)
$subscribers = @{} # eventType -> @(file:line)

foreach ($file in $csFiles) {
    $lines = Get-Content $file.FullName
    $relPath = Get-RelativePath $file.FullName

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNum = $i + 1

        # Publish patterns
        # Pattern 1: Publish(new EventType(...))
        if ($line -match 'Publish\(\s*new\s+(?:CommonEvents\.)?(\w+Event\w*)\s*[\(\{]') {
            $evt = $matches[1]
            if (-not $publishers.ContainsKey($evt)) { $publishers[$evt] = @() }
            $publishers[$evt] += "${relPath}:${lineNum}"
        }
        # Pattern 2: Publish(EventType.Instance)
        elseif ($line -match 'Publish\(\s*(?:Common\.Events\.)?(?:CommonEvents\.)?(\w+Event\w*)\.Instance') {
            $evt = $matches[1]
            if (-not $publishers.ContainsKey($evt)) { $publishers[$evt] = @() }
            $publishers[$evt] += "${relPath}:${lineNum}"
        }

        # Subscribe patterns
        # Pattern: Subscribe<EventType>( or SubscribeOnUI<EventType>(
        if ($line -match 'Subscribe(?:OnUI)?<\s*(?:Common\.Events\.)?(?:CommonEvents\.)?(?:Core\.Events\.)?(\w+Event\w*)\s*>') {
            $evt = $matches[1]
            # Skip generic type parameters (TEvent is not a real event type)
            if ($evt -match '^T[A-Z]' -or $evt -eq "TEvent") { continue }
            if (-not $subscribers.ContainsKey($evt)) { $subscribers[$evt] = @() }
            $subscribers[$evt] += "${relPath}:${lineNum}"
        }
    }
}

# Combine all known events
$allEvents = @($publishers.Keys) + @($subscribers.Keys) | Sort-Object -Unique

$eventsOutput = @"
# Event Flow Graph
# Generated: $timestamp | Commit: $gitHash
# Format: EventType
#   published-by: file:line, ...
#   subscribed-by: file:line, ...
# Events with no subscribers = potential dead code
# Events with no publishers = potential missing trigger

"@

foreach ($evt in $allEvents) {
    $eventsOutput += "## $evt`n"

    if ($publishers.ContainsKey($evt) -and $publishers[$evt].Count -gt 0) {
        $eventsOutput += "  published-by:`n"
        foreach ($loc in ($publishers[$evt] | Sort-Object)) {
            $eventsOutput += "    - $loc`n"
        }
    } else {
        $eventsOutput += "  published-by: NONE (orphan subscriber?)`n"
    }

    if ($subscribers.ContainsKey($evt) -and $subscribers[$evt].Count -gt 0) {
        $eventsOutput += "  subscribed-by:`n"
        foreach ($loc in ($subscribers[$evt] | Sort-Object)) {
            $eventsOutput += "    - $loc`n"
        }
    } else {
        $eventsOutput += "  subscribed-by: NONE (fire-and-forget or dead?)`n"
    }

    $eventsOutput += "`n"
}

# Summary stats
$orphanPubs = ($allEvents | Where-Object { -not $subscribers.ContainsKey($_) -or $subscribers[$_].Count -eq 0 }).Count
$orphanSubs = ($allEvents | Where-Object { -not $publishers.ContainsKey($_) -or $publishers[$_].Count -eq 0 }).Count
$eventsOutput += "## Summary`n"
$eventsOutput += "Total events: $($allEvents.Count)`n"
$eventsOutput += "Events with publishers only (no subscribers): $orphanPubs`n"
$eventsOutput += "Events with subscribers only (no publishers): $orphanSubs`n"

Set-Content -Path (Join-Path $OutputPath "events.md") -Value $eventsOutput -Encoding UTF8

# ============================================================================
# SECTION 3: Service Registry (AppServices)
# ============================================================================
Write-Progress-Msg "Analyzing service registry..."

$appServicesFile = Join-Path $SrcDir "EveLens.Common/Services/AppServices.cs"
$appServicesContent = Get-Content $appServicesFile

$services = @{} # serviceName -> @{ type=; impl=; consumers=@() }

# Extract service definitions (only interface/class-typed services, not primitives)
$excludedServiceTypes = @("bool", "string", "int", "long", "double", "float", "decimal", "object", "System.Diagnostics.FileVersionInfo")
$excludedServiceNames = @("Instance", "Value", "Closed", "IsAlphaVersion", "IsBetaVersion", "IsPreReleaseVersion", "IsDebugBuild", "IsDataLoaded", "EveAppDataFoldersExistInDefaultLocation", "ProductNameWithVersion", "VersionString", "SettingsFileName", "PrivacyMask", "PrivacyModeEnabled")

foreach ($line in $appServicesContent) {
    # Match: public static IType Name => ...
    if ($line -match 'public\s+static\s+(\S+)\s+(\w+)\s*=>') {
        $type = $matches[1]
        $name = $matches[2]
        if ($name -notin $excludedServiceNames -and $type -notin $excludedServiceTypes) {
            $services[$name] = @{ type = $type; consumers = @() }
        }
    }
}

# Find consumers of each service across the codebase
$serviceNames = $services.Keys | Sort-Object
foreach ($file in $csFiles) {
    $relPath = Get-RelativePath $file.FullName
    # Skip AppServices.cs itself
    if ($relPath -match "AppServices\.cs$") { continue }

    $content = Get-Content $file.FullName -Raw
    foreach ($svcName in $serviceNames) {
        if ($content -match "AppServices\.$svcName\b") {
            $services[$svcName].consumers += $relPath
        }
    }
}

$servicesOutput = @"
# Service Registry (AppServices)
# Generated: $timestamp | Commit: $gitHash
# Format: ServiceName (Type) -> [consumer files]
# High consumer count = high blast radius for interface changes

"@

foreach ($svcName in ($serviceNames | Sort-Object)) {
    $svc = $services[$svcName]
    $consumerCount = $svc.consumers.Count
    $servicesOutput += "## $svcName ($($svc.type)) [$consumerCount consumers]`n"
    if ($consumerCount -gt 0) {
        foreach ($consumer in ($svc.consumers | Sort-Object)) {
            $servicesOutput += "    - $consumer`n"
        }
    } else {
        $servicesOutput += "    (no external consumers)`n"
    }
    $servicesOutput += "`n"
}

Set-Content -Path (Join-Path $OutputPath "services.md") -Value $servicesOutput -Encoding UTF8

# ============================================================================
# SECTION 4: Interface Implementations
# ============================================================================
Write-Progress-Msg "Analyzing interface implementations..."

$interfaceFiles = Get-ChildItem -Path (Join-Path $SrcDir "EveLens.Core/Interfaces") -Filter "*.cs" -ErrorAction SilentlyContinue
$interfaces = @{} # interfaceName -> @{ file=; implementors=@() }

foreach ($iFile in $interfaceFiles) {
    $content = Get-Content $iFile.FullName -Raw
    $ifMatches = [regex]::Matches($content, 'interface\s+(I[A-Z]\w+)')
    foreach ($m in $ifMatches) {
        $ifName = $m.Groups[1].Value
        $interfaces[$ifName] = @{
            file = Get-RelativePath $iFile.FullName
            implementors = @()
        }
    }
}

# Find implementors
foreach ($file in $csFiles) {
    $relPath = Get-RelativePath $file.FullName
    $content = Get-Content $file.FullName -Raw

    foreach ($ifName in $interfaces.Keys) {
        # Match class declarations that implement this interface
        if ($content -match "class\s+(\w+)\s*(?:<[^>]*>)?\s*:\s*[^{]*\b$ifName\b") {
            $className = $matches[1]
            $interfaces[$ifName].implementors += @{ class = $className; file = $relPath }
        }
    }
}

$interfacesOutput = @"
# Interface Implementation Map
# Generated: $timestamp | Commit: $gitHash
# Format: IInterface (defined in file)
#   implemented-by: ClassName in file
# Multiple implementors = polymorphism/testing doubles

"@

foreach ($ifName in ($interfaces.Keys | Sort-Object)) {
    $iface = $interfaces[$ifName]
    $implCount = $iface.implementors.Count
    $interfacesOutput += "## $ifName [$implCount impl]`n"
    $interfacesOutput += "  defined: $($iface.file)`n"

    if ($implCount -gt 0) {
        $interfacesOutput += "  implemented-by:`n"
        foreach ($impl in $iface.implementors) {
            $interfacesOutput += "    - $($impl.class) in $($impl.file)`n"
        }
    } else {
        $interfacesOutput += "  implemented-by: NONE (interface-only contract?)`n"
    }
    $interfacesOutput += "`n"
}

Set-Content -Path (Join-Path $OutputPath "interfaces.md") -Value $interfacesOutput -Encoding UTF8

# ============================================================================
# SECTION 5: Settings Access Map
# ============================================================================
Write-Progress-Msg "Analyzing settings access patterns..."

$settingsAccess = @{} # category.property -> @(file:line)

foreach ($file in $csFiles) {
    $lines = Get-Content $file.FullName
    $relPath = Get-RelativePath $file.FullName

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNum = $i + 1

        # Match Settings.Category.Property (2+ levels deep)
        $settingsMatches = [regex]::Matches($line, '(?:Common\.)?Settings\.(\w+\.\w+)')
        foreach ($m in $settingsMatches) {
            $key = $m.Groups[1].Value
            # Skip method calls and internal patterns
            if ($key -match "^(Initialize|Import|Save|Load|Reset|Is\w+|Default)\.") { continue }
            if (-not $settingsAccess.ContainsKey($key)) { $settingsAccess[$key] = @() }
            $settingsAccess[$key] += "${relPath}:${lineNum}"
        }
    }
}

# Group by category
$categories = @{}
foreach ($key in $settingsAccess.Keys) {
    $parts = $key -split '\.'
    $cat = $parts[0]
    if (-not $categories.ContainsKey($cat)) { $categories[$cat] = @{} }
    $categories[$cat][$key] = $settingsAccess[$key]
}

$settingsOutput = @"
# Settings Access Map
# Generated: $timestamp | Commit: $gitHash
# Format: Settings.Category.Property -> [accessor files]
# High access count = high blast radius for settings changes

"@

foreach ($cat in ($categories.Keys | Sort-Object)) {
    $props = $categories[$cat]
    $totalRefs = ($props.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum
    $settingsOutput += "## Settings.$cat [$totalRefs total references]`n"

    foreach ($key in ($props.Keys | Sort-Object)) {
        $refs = $props[$key]
        $settingsOutput += "  $key [$($refs.Count) refs]`n"
        foreach ($ref in ($refs | Sort-Object | Select-Object -First 10)) {
            $settingsOutput += "    - $ref`n"
        }
        if ($refs.Count -gt 10) {
            $settingsOutput += "    ... and $($refs.Count - 10) more`n"
        }
    }
    $settingsOutput += "`n"
}

Set-Content -Path (Join-Path $OutputPath "settings.md") -Value $settingsOutput -Encoding UTF8

# ============================================================================
# SECTION 6: Impact Index (cross-cutting summary)
# ============================================================================
Write-Progress-Msg "Building impact index..."

# Build a file-to-connections map for quick lookups
$fileConnections = @{} # file -> @{ publishes=@(); subscribes=@(); services=@(); settings=@() }

foreach ($evt in $allEvents) {
    if ($publishers.ContainsKey($evt)) {
        foreach ($loc in $publishers[$evt]) {
            $file = ($loc -split ':')[0]
            if (-not $fileConnections.ContainsKey($file)) {
                $fileConnections[$file] = @{ publishes=@(); subscribes=@(); services=@(); settings=@() }
            }
            $fileConnections[$file].publishes += $evt
        }
    }
    if ($subscribers.ContainsKey($evt)) {
        foreach ($loc in $subscribers[$evt]) {
            $file = ($loc -split ':')[0]
            if (-not $fileConnections.ContainsKey($file)) {
                $fileConnections[$file] = @{ publishes=@(); subscribes=@(); services=@(); settings=@() }
            }
            $fileConnections[$file].subscribes += $evt
        }
    }
}

foreach ($svcName in $serviceNames) {
    foreach ($consumer in $services[$svcName].consumers) {
        if (-not $fileConnections.ContainsKey($consumer)) {
            $fileConnections[$consumer] = @{ publishes=@(); subscribes=@(); services=@(); settings=@() }
        }
        $fileConnections[$consumer].services += $svcName
    }
}

# Top connected files (highest blast radius)
$rankedFiles = $fileConnections.GetEnumerator() | ForEach-Object {
    $total = $_.Value.publishes.Count + $_.Value.subscribes.Count + $_.Value.services.Count
    [PSCustomObject]@{
        File = $_.Key
        Total = $total
        Publishes = $_.Value.publishes.Count
        Subscribes = $_.Value.subscribes.Count
        Services = $_.Value.services.Count
    }
} | Sort-Object -Property Total -Descending

$indexOutput = @"
# Code Graph Impact Index
# Generated: $timestamp | Commit: $gitHash
# This file helps answer: "What's the blast radius of touching file X?"

## Stats
- Total events tracked: $($allEvents.Count)
- Total services tracked: $($serviceNames.Count)
- Total files with connections: $($fileConnections.Count)
- Total interface contracts: $($interfaces.Count)

## Highest Blast Radius Files (top 30)
# Connections = events published + events subscribed + services consumed
# Touching these files has the widest ripple effect

"@

$top30 = $rankedFiles | Select-Object -First 30
foreach ($entry in $top30) {
    $indexOutput += "$($entry.File) [total=$($entry.Total) | pub=$($entry.Publishes) sub=$($entry.Subscribes) svc=$($entry.Services)]`n"
}

$indexOutput += "`n## Orphan Events (published but never subscribed)`n"
foreach ($evt in $allEvents) {
    if ((-not $subscribers.ContainsKey($evt)) -or $subscribers[$evt].Count -eq 0) {
        if ($publishers.ContainsKey($evt) -and $publishers[$evt].Count -gt 0) {
            $indexOutput += "- $evt (published in: $($publishers[$evt] -join ', '))`n"
        }
    }
}

$indexOutput += "`n## Ghost Subscriptions (subscribed but never published)`n"
foreach ($evt in $allEvents) {
    if ((-not $publishers.ContainsKey($evt)) -or $publishers[$evt].Count -eq 0) {
        if ($subscribers.ContainsKey($evt) -and $subscribers[$evt].Count -gt 0) {
            $indexOutput += "- $evt (subscribed in: $($subscribers[$evt] -join ', '))`n"
        }
    }
}

Set-Content -Path (Join-Path $OutputPath "index.md") -Value $indexOutput -Encoding UTF8

# ============================================================================
# SECTION 7: Quick-Lookup Format (single-file summary for fast loading)
# ============================================================================
Write-Progress-Msg "Building quick-lookup summary..."

$quickOutput = @"
# Code Graph Quick Reference
# Generated: $timestamp | Commit: $gitHash
# Load THIS file for fast impact analysis. Drill into specific files for details.

## Event Pub/Sub Count

"@

foreach ($evt in ($allEvents | Sort-Object)) {
    $pubCount = if ($publishers.ContainsKey($evt)) { $publishers[$evt].Count } else { 0 }
    $subCount = if ($subscribers.ContainsKey($evt)) { $subscribers[$evt].Count } else { 0 }
    $quickOutput += "$evt | pub:$pubCount sub:$subCount`n"
}

$quickOutput += "`n## Service Consumer Count`n"
foreach ($svcName in ($serviceNames | Sort-Object)) {
    $count = $services[$svcName].consumers.Count
    $quickOutput += "$svcName ($($services[$svcName].type)) | consumers:$count`n"
}

$quickOutput += "`n## Interface Implementor Count`n"
foreach ($ifName in ($interfaces.Keys | Sort-Object)) {
    $count = $interfaces[$ifName].implementors.Count
    $quickOutput += "$ifName | impls:$count`n"
}

Set-Content -Path (Join-Path $OutputPath "quick-ref.md") -Value $quickOutput -Encoding UTF8

# ============================================================================
# SECTION 8: .gitignore entry
# ============================================================================
$gitignorePath = Join-Path $RepoRoot ".gitignore"
$gitignoreContent = Get-Content $gitignorePath -Raw -ErrorAction SilentlyContinue
if ($gitignoreContent -and $gitignoreContent -notmatch '\.codegraph/') {
    Add-Content -Path $gitignorePath -Value "`n# Generated code graph (regenerate with scripts/codegraph.ps1)`n.codegraph/"
    Write-Progress-Msg "Added .codegraph/ to .gitignore"
}

# ============================================================================
# DONE
# ============================================================================
$totalFiles = (Get-ChildItem -Path $OutputPath -Filter "*.md").Count
Write-Progress-Msg "Done! Generated $totalFiles graph files in $OutputDir/"
Write-Progress-Msg "Files: assemblies.md, events.md, services.md, interfaces.md, settings.md, index.md, quick-ref.md"

if ($Validate) {
    Write-Progress-Msg "Running validation..."
    $validateScript = Join-Path $RepoRoot "scripts/validate-codegraph.ps1"
    if (Test-Path $validateScript) {
        & $validateScript -GraphDir $OutputPath -SrcDir $SrcDir
    } else {
        Write-Warning "Validation script not found at $validateScript"
        exit 1
    }
}
