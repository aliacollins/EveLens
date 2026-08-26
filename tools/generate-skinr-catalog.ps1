<#
.SYNOPSIS
    Generates the SKINR catalog datafile from CCP's Static Data Export.

.DESCRIPTION
    ESI's SKINR routes return IDs only - names are localized, so CCP puts them in the
    SDE instead. This script joins every table needed to turn a bare ESI recipe into
    something renderable and displayable, and emits one compressed catalog consumed by
    SkinrCatalog / SkinrRecipeResolver.

    The resolution chain, all of it ground truth from the SDE - no inference:

      recipe.ship_type_id
        -> types.yaml            groupID, graphicID, factionID, shipTreeGroupID, name
        -> graphics.yaml         sofHullName / sofFactionName / sofRaceName
                                 (the three tokens a SOF DNA string is built from)
        -> skinrSlotsToMaterials keyed by the hull's factionID: which DNA material
                                 position each of SKINR slots 1-4 actually paints
        -> skinrSlotConfigurations  which of slots 1-8 that hull even has
        -> skinrTierThresholds   keyed by shipTreeGroupID: design points per tier

      recipe.layout.slots[].configuration.nanocoating.id (or .pattern.id)
        -> skinrComponents       resourceFile -> the DNA token, plus rarity/finish/
                                 projection/icon/localized name

    Two remaps are easy to conflate and must not be:

      skinrSlotsToMaterials[factionID]  ESI slot 1-4  ->  DNA material position m1-m4.
        Always a bijection. Sparse: only the 16 factions that ship their own hulls have
        an entry, the other 11 are identity and CCP omits them. A Rifter (Minmatar
        Republic, 500002) is fully reversed - slot 1 paints DNA position 4.

      EveSOFDataFaction.materialUsageMtl1..4  DNA position -> shader material index.
        Lives in the engine, NOT here, and is NOT a bijection - repeats are normal
        because two shader material indices legitimately share one colour.

    Getting either wrong is not a crash. It is a ship wearing the right four colours in
    the wrong four places, which is worse, because it looks like a working feature.

.PARAMETER SdeDir
    Directory of extracted SDE YAML. Defaults to tools/SDEFiles/yaml_extracted.

.PARAMETER SdeBuild
    SDE build number recorded in the catalog. Auto-detected from the newest
    eve-online-static-data-<build>-yaml.zip in tools/SDEFiles when omitted.

.PARAMETER OutputDir
    Where to write skinr-catalog.json.gz. Defaults to src/EveLens.Common/Resources.

.PARAMETER Uncompressed
    Also write skinr-catalog.json next to the gzip, for eyeballing and for diffing
    catalogs across SDE builds. Not shipped - do not commit it.

.EXAMPLE
    .\tools\generate-skinr-catalog.ps1
    .\tools\generate-skinr-catalog.ps1 -Uncompressed
#>

param(
    [string]$SdeDir = "",
    [string]$SdeBuild = "",
    [string]$OutputDir = "",
    [switch]$Uncompressed
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrEmpty($SdeDir)) {
    $SdeDir = Join-Path $ProjectRoot "tools/SDEFiles/yaml_extracted"
}
if ([string]::IsNullOrEmpty($OutputDir)) {
    $OutputDir = Join-Path $ProjectRoot "src/EveLens.Common/Resources"
}

# EveLens locale codes, not SDE codes. The SDE says "zh"; the app says "zh-CN".
# SKINR component and slot names exist nowhere except these files, so unlike type and
# group names - which StaticTranslations loads from eve-translations-<code>.xml.gzip -
# the catalog is their only source and has to carry every language the app supports.
$Locales = [ordered]@{ "en" = "en"; "zh-CN" = "zh"; "ko" = "ko" }

Write-Host "`n=== EveLens SKINR Catalog Generator ===" -ForegroundColor Cyan

if (-not (Test-Path $SdeDir)) {
    throw "SDE directory not found: $SdeDir. Extract an SDE zip there, or pass -SdeDir."
}

if ([string]::IsNullOrEmpty($SdeBuild)) {
    $zips = Get-ChildItem (Join-Path $ProjectRoot "tools/SDEFiles") -Filter "eve-online-static-data-*-yaml.zip" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending
    if ($zips -and $zips[0].Name -match 'static-data-(\d+)-yaml') {
        $SdeBuild = $Matches[1]
    } else {
        $SdeBuild = "unknown"
    }
}
Write-Host "  SDE dir:   $SdeDir" -ForegroundColor Gray
Write-Host "  SDE build: $SdeBuild" -ForegroundColor Gray

function Get-SdePath {
    param([string]$Name, [switch]$Optional)
    $p = Join-Path $SdeDir $Name
    if (-not (Test-Path $p)) {
        if ($Optional) { return $null }
        throw "Required SDE file missing: $Name"
    }
    return $p
}

#region YAML readers
# The SDE's YAML is machine-generated and rigidly regular: top-level integer keys at
# column 0, fields at two spaces, nested maps at four, list items as "  - ". That makes
# line-oriented state machines both correct and fast - types.yaml is 156 MB and a real
# YAML parser would neither fit in memory nor finish. Same approach as parse-sde.ps1.

# id -> scalar value of one 2-space field.
function Read-SdeScalarMap {
    param([string]$Path, [string]$Field)

    $map = @{}
    $id = $null
    $pattern = "^  $([regex]::Escape($Field)):\s*(.+)$"
    foreach ($line in [System.IO.File]::ReadLines($Path)) {
        if ($line -match '^(\d+):\s*$') { $id = [int]$Matches[1]; continue }
        if ($null -eq $id) { continue }
        if ($line -match $pattern) { $map[$id] = $Matches[1].Trim().Trim('"').Trim("'") }
    }
    return $map
}

# id -> ordered hashtable of EveLens locale -> name, read from a nested localized block.
# Only the requested languages are kept; a missing language simply has no key and the
# consumer falls back to English, exactly like Loc.Get does.
function Read-SdeLocalizedMap {
    param([string]$Path, [string]$Field = "name")

    $map = @{}
    $id = $null
    $inBlock = $false
    $blockStart = "^  $([regex]::Escape($Field)):\s*$"
    foreach ($line in [System.IO.File]::ReadLines($Path)) {
        if ($line -match '^(\d+):\s*$') { $id = [int]$Matches[1]; $inBlock = $false; continue }
        if ($null -eq $id) { continue }
        if ($line -match $blockStart) { $inBlock = $true; continue }
        if (-not $inBlock) { continue }
        # Any 2-space sibling field ends the block. Language keys sit at four spaces, so
        # this cannot fire early - but description: blocks also contain an "en:", which
        # is precisely why the block has to be delimited rather than grepped for.
        if ($line -match '^  \S') { $inBlock = $false; continue }
        if ($line -match '^    ([a-z]{2}):\s*(.+)$') {
            $sde = $Matches[1]
            $val = $Matches[2].Trim().Trim('"').Trim("'")
            foreach ($loc in $Locales.Keys) {
                if ($Locales[$loc] -eq $sde -and $val.Length -gt 0) {
                    if (-not $map.ContainsKey($id)) { $map[$id] = [ordered]@{} }
                    $map[$id][$loc] = $val
                }
            }
        }
    }
    return $map
}

# id -> { innerIntKey -> intValue }. Two users, both two-axis lookup tables:
#
#   skinrComponentPointValues[componentCategory][rarity] -> design points
#     Three outer keys matching componentCategories exactly (Material/Pattern/Metallic)
#     and six inner matching the rarities (Standard..Empyrean). Metallic is dearest:
#     a Standard Material is 25 points, a Standard Metallic 100.
#
#   skinrTierThresholds[shipTreeGroupID][tier] -> points required
#     Keyed by ship-tree hull line, NOT inventory groupID. The three lines absent from
#     it - Strategic Cruiser, Tactical Destroyer, Capsule - are exactly the hulls listed
#     under the "No custom skins allowed" slot configuration, which is a nice
#     cross-check that the keyspace is the one we think it is.
#
# Together they give the tier economy:
#   points = sum over filled slots of pointValues[category][rarity]
#   tier   = highest t where points >= tierThresholds[shipTreeGroupID][t]
# ESI hands us tier.level outright, so this is for "points to next tier" analytics
# rather than for anything on the render path.
function Read-SdeIntMatrix {
    param([string]$Path)

    $map = [ordered]@{}
    $id = $null
    foreach ($line in [System.IO.File]::ReadLines($Path)) {
        if ($line -match '^(\d+):\s*$') { $id = $Matches[1]; $map[$id] = [ordered]@{}; continue }
        if ($null -eq $id) { continue }
        if ($line -match '^  (\d+):\s*(-?\d+)\s*$') { $map[$id][$Matches[1]] = [int]$Matches[2] }
    }
    return $map
}
#endregion

#region skinr tables
Write-Host "`n  Parsing SKINR tables..." -ForegroundColor Yellow

# Slot identity. skinrSlotNames is the stable engineering key for an ESI slot id:
#   1 primary_nanocoating   2 secondary_nanocoating   3 tertiary_nanocoating
#   4 tech_area             5 pattern                 6 pattern_material
#   7 secondary_pattern     8 secondary_pattern_material
# Slots 1-4 are material slots and are the only ones skinrSlotsToMaterials remaps.
$slotNames      = Read-SdeScalarMap    -Path (Get-SdePath "skinrSlotNames.yaml")      -Field "name"
$slotCategories = Read-SdeScalarMap    -Path (Get-SdePath "skinrSlotCategories.yaml") -Field "name"
$slotLocalized  = Read-SdeLocalizedMap -Path (Get-SdePath "skinrSlots.yaml")
Write-Host "    slots: $($slotNames.Count) names, $($slotCategories.Count) categories" -ForegroundColor Gray

# skinrSlots.yaml: category plus which component categories the slot accepts.
$slots = [ordered]@{}
$sid = $null; $inAllowed = $false
foreach ($line in [System.IO.File]::ReadLines((Get-SdePath "skinrSlots.yaml"))) {
    if ($line -match '^(\d+):\s*$') {
        $sid = $Matches[1]; $inAllowed = $false
        $slots[$sid] = [ordered]@{
            name              = $slotNames[[int]$sid]
            category          = $null
            allowedComponents = @()
            displayName       = $slotLocalized[[int]$sid]
        }
        continue
    }
    if ($null -eq $sid) { continue }
    if ($line -match '^  allowedDesignComponentCategories:\s*$') { $inAllowed = $true; continue }
    if ($inAllowed -and $line -match '^  - (\d+)\s*$') { $slots[$sid].allowedComponents += [int]$Matches[1]; continue }
    if ($line -match '^  category:\s*(\d+)') { $slots[$sid].category = [int]$Matches[1]; $inAllowed = $false; continue }
    if ($line -match '^  \S') { $inAllowed = $false }
}

# skinrSlotConfigurations.yaml: which slots a given hull actually has. Resolution is by
# descending priority over the configurations whose `ships` list contains the hull, with
# the allowAllShips default (priority 3) as the floor. Configuration 6 has no `config`
# key at all - that is "no custom skins allowed", an empty slot set, not a parse failure.
$slotConfigs = [ordered]@{}
$cid = $null; $section = ""
foreach ($line in [System.IO.File]::ReadLines((Get-SdePath "skinrSlotConfigurations.yaml"))) {
    if ($line -match '^(\d+):\s*$') {
        $cid = $Matches[1]; $section = ""
        $slotConfigs[$cid] = [ordered]@{
            name = $null; priority = $null; allowAllShips = $false; slots = @(); ships = @()
        }
        continue
    }
    if ($null -eq $cid) { continue }
    if ($line -match '^  config:\s*$')        { $section = "slots"; continue }
    if ($line -match '^  ships:\s*$')         { $section = "ships"; continue }
    if ($line -match '^  name:\s*(.+)$')      { $slotConfigs[$cid].name = $Matches[1].Trim(); $section = ""; continue }
    if ($line -match '^  priority:\s*(\d+)')  { $slotConfigs[$cid].priority = [int]$Matches[1]; $section = ""; continue }
    if ($line -match '^  allowAllShips:\s*true') { $slotConfigs[$cid].allowAllShips = $true; $section = ""; continue }
    if ($line -match '^  - (\d+)\s*$') {
        if ($section -eq "slots") { $slotConfigs[$cid].slots += [int]$Matches[1] }
        elseif ($section -eq "ships") { $slotConfigs[$cid].ships += [int]$Matches[1] }
        continue
    }
    if ($line -match '^  \S') { $section = "" }
}
foreach ($k in $slotConfigs.Keys) {
    $c = $slotConfigs[$k]
    Write-Host ("    config {0,-3} {1,-26} prio={2} slots={3} ships={4}" -f `
        $k, $c.name, $c.priority, $c.slots.Count, $c.ships.Count) -ForegroundColor Gray
}

# skinrSlotsToMaterials.yaml: factionID -> list of { slotID, materialID }. Flattened to
# slotID -> materialID. Sparse by design; absent factions are identity.
$slotsToMaterials = [ordered]@{}
$fid = $null; $pendingSlot = $null; $pendingMaterial = $null
function Flush-SlotMaterial {
    if ($null -ne $script:fid -and $null -ne $script:pendingSlot -and $null -ne $script:pendingMaterial) {
        $slotsToMaterials[$script:fid][[string]$script:pendingSlot] = $script:pendingMaterial
    }
    $script:pendingSlot = $null; $script:pendingMaterial = $null
}
foreach ($line in [System.IO.File]::ReadLines((Get-SdePath "skinrSlotsToMaterials.yaml"))) {
    if ($line -match '^(\d+):\s*$') {
        Flush-SlotMaterial
        $fid = $Matches[1]; $slotsToMaterials[$fid] = [ordered]@{}
        continue
    }
    if ($null -eq $fid) { continue }
    # Indentation here differs from every other skinr file: the list IS the record's
    # value, so items sit at column 0 ("- materialID: 3") with continuation at two
    # spaces - not nested under a named field the way skinrComponents.associatedTypeIds
    # is. Assuming the nested shape parses to silently empty maps, which is why the
    # non-identity count below is asserted rather than merely printed.
    if ($line -match '^- (materialID|slotID):\s*(\d+)') {
        Flush-SlotMaterial
        if ($Matches[1] -eq 'slotID') { $pendingSlot = [int]$Matches[2] } else { $pendingMaterial = [int]$Matches[2] }
        continue
    }
    if ($line -match '^  (materialID|slotID):\s*(\d+)') {
        if ($Matches[1] -eq 'slotID') { $pendingSlot = [int]$Matches[2] } else { $pendingMaterial = [int]$Matches[2] }
        continue
    }
}
Flush-SlotMaterial
# Assert the shape rather than trust it. Every entry must be a bijection of the four
# material slots onto the four DNA positions; an empty or partial map would otherwise
# sail through and land every nanocoating on DNA position 1.
$nonIdentity = 0
foreach ($k in $slotsToMaterials.Keys) {
    $m = $slotsToMaterials[$k]
    $slotKeys = @($m.Keys | Sort-Object)
    $values   = @($m.Keys | ForEach-Object { $m[$_] } | Sort-Object)
    if ($slotKeys.Count -ne 4) {
        throw "skinrSlotsToMaterials[$k] has $($slotKeys.Count) slots, expected 4 - the parse is wrong."
    }
    if (($values -join ',') -ne '1,2,3,4') {
        throw "skinrSlotsToMaterials[$k] maps to [$($values -join ',')], which is not a permutation of 1-4."
    }
    $isIdentity = $true
    foreach ($s in $m.Keys) { if ($m[$s] -ne [int]$s) { $isIdentity = $false } }
    if (-not $isIdentity) { $nonIdentity++ }
}
if ($nonIdentity -eq 0) {
    throw "Every faction slot map came out as identity. CCP ships 16 permutations; this means the parse silently matched nothing."
}
Write-Host "    slotsToMaterials: $($slotsToMaterials.Count) factions ($nonIdentity non-identity)" -ForegroundColor Gray

# Component taxonomy and the tier economy.
$componentCategories = Read-SdeScalarMap  -Path (Get-SdePath "skinrComponentCategories.yaml") -Field "name"
$rarityNames         = Read-SdeLocalizedMap -Path (Get-SdePath "skinrComponentRarities.yaml")
$rarityRanks         = Read-SdeScalarMap  -Path (Get-SdePath "skinrComponentRarities.yaml") -Field "rank"
$pointValues         = Read-SdeIntMatrix  -Path (Get-SdePath "skinrComponentPointValues.yaml")
$tierThresholds      = Read-SdeIntMatrix  -Path (Get-SdePath "skinrTierThresholds.yaml")
Write-Host "    componentCategories: $($componentCategories.Count), rarities: $($rarityRanks.Count)" -ForegroundColor Gray
Write-Host "    pointValues: $($pointValues.Count) rarities, tierThresholds: $($tierThresholds.Count) ship-tree groups" -ForegroundColor Gray

$rarities = [ordered]@{}
foreach ($k in ($rarityRanks.Keys | Sort-Object)) {
    $rarities[[string]$k] = [ordered]@{ rank = [int]$rarityRanks[$k]; name = $rarityNames[$k] }
}
#endregion

#region components
Write-Host "`n  Parsing skinrComponents.yaml..." -ForegroundColor Yellow

$componentLocalized = Read-SdeLocalizedMap -Path (Get-SdePath "skinrComponents.yaml")

$components = [ordered]@{}
$compId = $null; $inAssoc = $false; $inBinder = $false; $inName = $false
$assocPendingType = $null; $assocPendingUses = $null

function Flush-Assoc {
    if ($null -ne $script:compId -and $null -ne $script:assocPendingType) {
        $components[$script:compId].associatedTypes += ,([ordered]@{
            typeID            = $script:assocPendingType
            licenseUsesGranted = $script:assocPendingUses
        })
    }
    $script:assocPendingType = $null; $script:assocPendingUses = $null
}

foreach ($line in [System.IO.File]::ReadLines((Get-SdePath "skinrComponents.yaml"))) {
    if ($line -match '^(\d+):\s*$') {
        Flush-Assoc
        $compId = $Matches[1]; $inAssoc = $false; $inBinder = $false; $inName = $false
        $components[$compId] = [ordered]@{
            name             = $componentLocalized[[int]$compId]
            category         = $null
            rarity           = $null
            finish           = $null
            resourceFile     = $null
            # The DNA token: resourceFile's basename with .red stripped. Precomputed here
            # so the resolver never does path surgery at render time, and so a change in
            # CCP's resource layout shows up as a catalog diff instead of a runtime bug.
            dnaToken         = $null
            iconFile         = $null
            projectionTypeU  = $null
            projectionTypeV  = $null
            published        = $false
            sequenceBinder   = $null
            associatedTypes  = @()
        }
        continue
    }
    if ($null -eq $compId) { continue }

    if ($line -match '^  associatedTypeIds:\s*$') { Flush-Assoc; $inAssoc = $true; $inBinder = $false; $inName = $false; continue }
    if ($line -match '^  sequenceBinder:\s*$')    { Flush-Assoc; $inAssoc = $false; $inBinder = $true; $inName = $false
                                                    $components[$compId].sequenceBinder = [ordered]@{ count = $null; itemTypeID = $null }; continue }
    if ($line -match '^  name:\s*$')              { Flush-Assoc; $inAssoc = $false; $inBinder = $false; $inName = $true; continue }

    if ($inAssoc) {
        if ($line -match '^  - (typeID|licenseUsesGranted):\s*(-?\d+)') {
            Flush-Assoc
            if ($Matches[1] -eq 'typeID') { $assocPendingType = [int]$Matches[2] } else { $assocPendingUses = [int]$Matches[2] }
            continue
        }
        if ($line -match '^    (typeID|licenseUsesGranted):\s*(-?\d+)') {
            if ($Matches[1] -eq 'typeID') { $assocPendingType = [int]$Matches[2] } else { $assocPendingUses = [int]$Matches[2] }
            continue
        }
    }
    if ($inBinder -and $line -match '^    (count|itemTypeID):\s*(\d+)') {
        $components[$compId].sequenceBinder[$Matches[1]] = [int]$Matches[2]
        continue
    }

    if ($line -match '^  \S') { Flush-Assoc; $inAssoc = $false; $inBinder = $false; $inName = $false }

    if ($line -match '^  category:\s*(\d+)')            { $components[$compId].category = [int]$Matches[1]; continue }
    if ($line -match '^  rarity:\s*(\d+)')              { $components[$compId].rarity = [int]$Matches[1]; continue }
    if ($line -match '^  finish:\s*(.+)$')              { $components[$compId].finish = $Matches[1].Trim(); continue }
    if ($line -match '^  projectionTypeU:\s*(.+)$')     { $components[$compId].projectionTypeU = $Matches[1].Trim(); continue }
    if ($line -match '^  projectionTypeV:\s*(.+)$')     { $components[$compId].projectionTypeV = $Matches[1].Trim(); continue }
    if ($line -match '^  iconFile:\s*(.+)$')            { $components[$compId].iconFile = $Matches[1].Trim(); continue }
    if ($line -match '^  published:\s*(true|false)')    { $components[$compId].published = ($Matches[1] -eq 'true'); continue }
    if ($line -match '^  resourceFile:\s*(.+)$') {
        $rf = $Matches[1].Trim()
        $components[$compId].resourceFile = $rf
        $leaf = $rf -replace '^.*/', ''
        $components[$compId].dnaToken = ($leaf -replace '\.red$', '')
        continue
    }
}
Flush-Assoc

$missingToken = @($components.Keys | Where-Object { -not $components[$_].dnaToken }).Count
Write-Host "    components: $($components.Count) ($missingToken without a DNA token)" -ForegroundColor Gray
if ($missingToken -gt 0) {
    throw "$missingToken components have no resolvable DNA token - the resourceFile parse is wrong."
}
$byCat = @{}
foreach ($k in $components.Keys) {
    $c = $components[$k].category
    if (-not $byCat.ContainsKey($c)) { $byCat[$c] = 0 }
    $byCat[$c]++
}
foreach ($c in ($byCat.Keys | Sort-Object)) {
    Write-Host ("    category {0} {1,-10} {2}" -f $c, $componentCategories[[int]$c], $byCat[$c]) -ForegroundColor Gray
}
#endregion

#region hulls
Write-Host "`n  Parsing graphics.yaml..." -ForegroundColor Yellow
$graphics = [ordered]@{}
$gid = $null
foreach ($line in [System.IO.File]::ReadLines((Get-SdePath "graphics.yaml"))) {
    if ($line -match '^(\d+):\s*$') { $gid = $Matches[1]; continue }
    if ($null -eq $gid) { continue }
    if ($line -match '^  sof(HullName|FactionName|RaceName|MaterialSetID):\s*(.+)$') {
        if (-not $graphics.Contains($gid)) { $graphics[$gid] = [ordered]@{} }
        $graphics[$gid][$Matches[1]] = $Matches[2].Trim()
    }
}
$withHull = @($graphics.Keys | Where-Object { $graphics[$_].HullName }).Count
Write-Host "    graphics with a SOF hull: $withHull of $($graphics.Count)" -ForegroundColor Gray

Write-Host "`n  Parsing groups.yaml (for the Ship category filter)..." -ForegroundColor Yellow
$groupCategory = Read-SdeScalarMap -Path (Get-SdePath "groups.yaml") -Field "categoryID"
$groupNames    = Read-SdeLocalizedMap -Path (Get-SdePath "groups.yaml")
$shipGroups = @{}
foreach ($k in $groupCategory.Keys) { if ([int]$groupCategory[$k] -eq 6) { $shipGroups[$k] = $true } }
Write-Host "    groups: $($groupCategory.Count), of category 6 (Ship): $($shipGroups.Count)" -ForegroundColor Gray

# types.yaml is ~156 MB. Single streaming pass, and the record is only kept if its group
# is in the Ship category and its graphic resolves to a SOF hull - anything else can
# never legitimately arrive as a recipe's ship_type_id.
Write-Host "`n  Streaming types.yaml (this is the slow part)..." -ForegroundColor Yellow
$sw = [System.Diagnostics.Stopwatch]::StartNew()

$hulls = [ordered]@{}
$typeId = $null; $rec = $null; $inTypeName = $false; $seen = 0
$FIELDS = 'groupID|graphicID|factionID|shipTreeGroupID|raceID|metaGroupID|marketGroupID|radius'

function Commit-Type {
    if ($null -eq $script:typeId -or $null -eq $script:rec) { return }
    $r = $script:rec
    if (-not $r.groupID) { return }
    if (-not $shipGroups.ContainsKey($r.groupID)) { return }
    if (-not $r.graphicID) { return }
    $g = $graphics[[string]$r.graphicID]
    if ($null -eq $g -or -not $g.HullName) { return }
    $hulls[[string]$script:typeId] = [ordered]@{
        name              = $r.name
        groupID           = $r.groupID
        groupName         = $groupNames[$r.groupID]
        graphicID         = $r.graphicID
        factionID         = $r.factionID
        shipTreeGroupID   = $r.shipTreeGroupID
        raceID            = $r.raceID
        metaGroupID       = $r.metaGroupID
        marketGroupID     = $r.marketGroupID
        radius            = $r.radius
        published         = $r.published
        sofHullName       = $g.HullName
        sofFactionName    = $g.FactionName
        sofRaceName       = $g.RaceName
        sofMaterialSetID  = if ($g.MaterialSetID) { [int]$g.MaterialSetID } else { $null }
    }
}

# Regex is the expensive part at this scale - ~4.6 million lines, and the overwhelming
# majority are localized description text we do not want. So every line is triaged by
# character inspection first and only the survivors are matched. This is the difference
# between a generator the maintainer runs happily on every SDE build and one they avoid.
$sdeLangOf = @{}
foreach ($loc in $Locales.Keys) { $sdeLangOf[$Locales[$loc]] = $loc }

foreach ($line in [System.IO.File]::ReadLines((Get-SdePath "types.yaml"))) {
    $len = $line.Length
    if ($len -eq 0) { continue }
    $c0 = $line[0]

    # Column 0 digit: a new top-level type record.
    if ($c0 -ge '0' -and $c0 -le '9') {
        Commit-Type
        if ($line -match '^(\d+):\s*$') {
            $typeId = [int]$Matches[1]
            $rec = [ordered]@{ name = $null; published = $false; radius = $null }
            $inTypeName = $false
            $seen++
            if (($seen % 10000) -eq 0) { Write-Host "      $seen types..." -ForegroundColor DarkGray }
        } else {
            $typeId = $null; $rec = $null
        }
        continue
    }
    if ($null -eq $typeId -or $c0 -ne ' ') { continue }

    # Depth 4+ only matters while inside a name: block. Description blocks are the bulk
    # of the file and this discards them without touching the regex engine.
    if ($len -gt 2 -and $line[2] -eq ' ') {
        if (-not $inTypeName) { continue }
        if ($len -le 4 -or $line[4] -eq ' ') { continue }   # wrapped continuation line
        if ($line -match '^    ([a-z]{2}):\s*(.+)$') {
            $loc = $sdeLangOf[$Matches[1]]
            if ($loc) {
                $val = $Matches[2].Trim().Trim('"').Trim("'")
                if ($val.Length -gt 0) {
                    if ($null -eq $rec.name) { $rec.name = [ordered]@{} }
                    $rec.name[$loc] = $val
                }
            }
        }
        continue
    }

    # Exactly two spaces of indent: a field of the current type. Any such line ends a
    # name: block, including name: itself starting a new one.
    $inTypeName = $false
    switch -Regex ($line) {
        '^  name:\s*$'                  { $inTypeName = $true; break }
        "^  ($FIELDS):\s*([\d.]+)\s*$"  {
            if ($Matches[1] -eq 'radius') { $rec['radius'] = [double]$Matches[2] }
            else { $rec[$Matches[1]] = [int]$Matches[2] }
            break
        }
        '^  published:\s*(true|false)'  { $rec.published = ($Matches[1] -eq 'true'); break }
    }
}
Commit-Type
$sw.Stop()
Write-Host "    scanned $seen types in $([math]::Round($sw.Elapsed.TotalSeconds,1))s" -ForegroundColor Gray
Write-Host "    SKINR-addressable hulls: $($hulls.Count)" -ForegroundColor Green

# Sanity gates. These are cheap and they catch an SDE layout change immediately, rather
# than three layers up as a hull that renders in someone else's paint.
# A hull with no factionID falls back to the identity slot mapping. For the unpublished
# NPC-only hulls (Concord Police Frigate and friends) that is correct and expected - ESI
# will never hand us one as a recipe's ship_type_id. A *published* hull missing its
# factionID would mean the join has drifted, so only that is worth shouting about.
$noFaction = @($hulls.Keys | Where-Object { -not $hulls[$_].factionID })
$noFactionPublished = @($noFaction | Where-Object { $hulls[$_].published })
Write-Host "    hulls without factionID: $($noFaction.Count) ($($noFactionPublished.Count) of them published)" -ForegroundColor Gray
if ($noFactionPublished.Count -gt 0) {
    Write-Host "    WARNING: published hulls lacking factionID will use identity slot mapping: $($noFactionPublished -join ', ')" -ForegroundColor Yellow
}
$rifter = $hulls["587"]
if ($null -eq $rifter) { throw "Sanity check failed: the Rifter (587) is not in the hull table." }
if ($rifter.sofHullName -ne "mf4_t1" -or $rifter.factionID -ne 500002) {
    throw "Sanity check failed: Rifter resolved to $($rifter.sofHullName)/$($rifter.factionID), expected mf4_t1/500002."
}
Write-Host "    sanity: Rifter 587 -> $($rifter.sofHullName) / $($rifter.sofFactionName) / $($rifter.sofRaceName) / faction $($rifter.factionID)" -ForegroundColor Gray
#endregion

#region emit
Write-Host "`n  Building catalog..." -ForegroundColor Yellow

# The join maps above are keyed by integer because that is what they join on. JSON object
# keys are strings by definition and ConvertTo-Json refuses a non-string-keyed dictionary
# outright, so re-key at the boundary - sorted numerically, which keeps the emitted
# catalog byte-stable across runs and makes SDE-build diffs readable.
function ConvertTo-StringKeyed {
    param($Map)
    $out = [ordered]@{}
    foreach ($k in ($Map.Keys | Sort-Object { [int]$_ })) { $out[[string]$k] = $Map[$k] }
    return $out
}

$catalog = [ordered]@{
    schemaVersion       = 1
    sdeBuild            = $SdeBuild
    generatedUtc        = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    locales             = @($Locales.Keys)
    slotNames           = ConvertTo-StringKeyed $slotNames
    slotCategories      = ConvertTo-StringKeyed $slotCategories
    slots               = $slots
    slotConfigurations  = $slotConfigs
    slotsToMaterials    = $slotsToMaterials
    componentCategories = ConvertTo-StringKeyed $componentCategories
    componentRarities   = $rarities
    componentPointValues = $pointValues
    tierThresholds      = $tierThresholds
    components          = $components
    hulls               = $hulls
}

$json = $catalog | ConvertTo-Json -Depth 12 -Compress
$bytes = [System.Text.Encoding]::UTF8.GetBytes($json)

if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }
$outPath = Join-Path $OutputDir "skinr-catalog.json.gz"

$fs = [System.IO.File]::Create($outPath)
$gz = [System.IO.Compression.GZipStream]::new($fs, [System.IO.Compression.CompressionLevel]::Optimal)
$gz.Write($bytes, 0, $bytes.Length)
$gz.Close()
$fs.Close()

$rawKB = [math]::Round($bytes.Length / 1KB, 1)
$gzKB  = [math]::Round((Get-Item $outPath).Length / 1KB, 1)
Write-Host "  Written: $outPath" -ForegroundColor Green
Write-Host "    raw $rawKB KB -> gzip $gzKB KB" -ForegroundColor Gray

if ($Uncompressed) {
    $plain = Join-Path $OutputDir "skinr-catalog.json"
    [System.IO.File]::WriteAllBytes($plain, [System.Text.Encoding]::UTF8.GetBytes(($catalog | ConvertTo-Json -Depth 12)))
    Write-Host "    also wrote $plain (do not commit)" -ForegroundColor DarkYellow
}
#endregion

Write-Host "`n=== Done ===" -ForegroundColor Cyan
Write-Host ("  {0} hulls, {1} components, {2} faction slot maps, {3} tier tables" -f `
    $hulls.Count, $components.Count, $slotsToMaterials.Count, $tierThresholds.Count) -ForegroundColor Green
