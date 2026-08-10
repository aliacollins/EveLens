<#
.SYNOPSIS
    Downloads EVE SDE and generates a localized translation datafile for EveLens.

.DESCRIPTION
    Downloads the EVE Online Static Data Export (SDE), extracts types.yaml
    and groups.yaml, parses the requested language's name translations, and
    generates eve-translations-{OutputCode}.xml.gzip for use by StaticTranslations.

.PARAMETER Language
    SDE language key to extract (e.g. "zh", "ko", "ja", "ru", "fr", "de", "es").

.PARAMETER OutputCode
    Output file/locale code (e.g. "zh-CN", "ko"). Defaults to the SDE Language
    code, except "zh" maps to "zh-CN" to match the EveLens locale convention.

.PARAMETER SdeUrl
    URL to the SDE zip. Defaults to latest Tranquility export.

.EXAMPLE
    .\tools\generate-translations.ps1 -Language ko
    .\tools\generate-translations.ps1 -Language zh -OutputCode zh-CN
#>

param(
    [string]$Language = "zh",
    [string]$OutputCode = "",
    [string]$SdeUrl = "",
    [string]$LocalZip = ""
)

if ([string]::IsNullOrEmpty($OutputCode)) {
    $OutputCode = if ($Language -eq "zh") { "zh-CN" } else { $Language }
}

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$OutputDir = Join-Path $ProjectRoot "src/EveLens.Common/Resources"
$TempDir = Join-Path $env:TEMP "evelens-sde"

Write-Host "`n=== EveLens SDE Translation Generator ===" -ForegroundColor Cyan

# Create temp directory
if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
New-Item -ItemType Directory -Path $TempDir | Out-Null

# Resolve the SDE zip: use -LocalZip if given, else download from CCP's official
# distribution (Law 15: developers.eveonline.com/static-data, never Fuzzwork/S3 mirrors).
if (-not [string]::IsNullOrEmpty($LocalZip)) {
    $sdeZip = $LocalZip
    Write-Host "  Using local SDE zip: $sdeZip" -ForegroundColor Green
}
else {
    if ([string]::IsNullOrEmpty($SdeUrl)) {
        Write-Host "  Resolving latest SDE build..." -ForegroundColor Yellow
        $latest = Invoke-RestMethod -Uri "https://developers.eveonline.com/static-data/tranquility/latest.jsonl" -UseBasicParsing
        $build = $latest.buildNumber
        $SdeUrl = "https://developers.eveonline.com/static-data/tranquility/eve-online-static-data-$build-yaml.zip"
        Write-Host "  Latest build: $build" -ForegroundColor Green
    }
    $sdeZip = Join-Path $TempDir "sde.zip"
    Write-Host "  Downloading SDE..." -ForegroundColor Yellow
    Write-Host "  URL: $SdeUrl" -ForegroundColor Gray
    try {
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $SdeUrl -OutFile $sdeZip -UseBasicParsing
        $ProgressPreference = 'Continue'
        $sizeMB = [math]::Round((Get-Item $sdeZip).Length / 1MB, 1)
        Write-Host "  Downloaded: $sizeMB MB" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to download SDE: $_"
        exit 1
    }
}

# Extract only the files we need
Write-Host "  Extracting typeIDs.yaml and groupIDs.yaml..." -ForegroundColor Yellow

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($sdeZip)

# August 2026+ SDE zips use flat paths; older ones prefixed with fsd/
$targetFiles = @(
    "fsd/types.yaml", "types.yaml",
    "fsd/groups.yaml", "groups.yaml",
    "fsd/marketGroups.yaml", "marketGroups.yaml"
)

foreach ($entry in $zip.Entries) {
    foreach ($target in $targetFiles) {
        if ($entry.FullName -eq $target) {
            $outPath = Join-Path $TempDir $entry.Name
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $outPath, $true)
            Write-Host "  Extracted: $($entry.Name) ($([math]::Round($entry.Length / 1MB, 1)) MB)" -ForegroundColor Green
        }
    }
}
$zip.Dispose()

# Parse YAML files for Chinese translations
# The SDE YAML format for typeIDs is:
#   12345:
#     name:
#       en: "English Name"
#       zh: "Chinese Name"
#
# We use a simple line-based parser since we only need name.zh fields

Write-Host "`n  Parsing translations..." -ForegroundColor Yellow

function Parse-SdeYaml {
    param([string]$FilePath, [string]$NameField = "name")

    $translations = @{}
    $currentId = $null
    $inName = $false
    $lineNum = 0

    foreach ($line in [System.IO.File]::ReadLines($FilePath)) {
        $lineNum++

        # Top-level ID (no indentation)
        if ($line -match '^(\d+):') {
            $currentId = [int]$Matches[1]
            $inName = $false
            continue
        }

        # Name section start (exactly 2-space indent under the top-level ID)
        if ($currentId -and $line -match "^\s{2}${NameField}:\s*$") {
            $inName = $true
            continue
        }

        # Target-language translation within name section (language keys are 4-space indented).
        # Note: language lines (de/en/.../ko/.../zh) are children of name:, so we must NOT treat
        # them as "exit" fields — only a 2-space sibling field or a new top-level ID ends the section.
        if ($inName -and $line -match "^\s{4}${Language}:\s*(.+)") {
            $locName = $Matches[1].Trim().Trim('"').Trim("'")
            if ($locName -and $locName.Length -gt 0) {
                $translations[$currentId] = $locName
            }
            $inName = $false
            continue
        }

        # A sibling field of name: (exactly 2-space indent) exits the name section
        if ($inName -and $line -match '^\s{2}\w+:') {
            $inName = $false
        }

        # Another top-level section exits name too
        if ($inName -and $line -match '^\s{0,1}\w') {
            $inName = $false
        }
    }

    return $translations
}

$typeIdsPath = Join-Path $TempDir "types.yaml"
$groupIdsPath = Join-Path $TempDir "groups.yaml"
$marketGroupsPath = Join-Path $TempDir "marketGroups.yaml"

$typeTranslations = @{}
$groupTranslations = @{}

if (Test-Path $typeIdsPath) {
    Write-Host "  Parsing types.yaml..." -ForegroundColor Gray
    $typeTranslations = Parse-SdeYaml -FilePath $typeIdsPath
    Write-Host "  Found $($typeTranslations.Count) type translations" -ForegroundColor Green
}

if (Test-Path $groupIdsPath) {
    Write-Host "  Parsing groups.yaml (inventory groups)..." -ForegroundColor Gray
    $groupTranslations = Parse-SdeYaml -FilePath $groupIdsPath
    Write-Host "  Found $($groupTranslations.Count) inventory-group translations" -ForegroundColor Green
}

# Market groups drive the ship/item/blueprint browser TREES (MarketGroup.LocalizedName ->
# StaticTranslations.GetGroupName). They use a separate ID space from inventory groups, so we
# merge both into the same <groups> dictionary. Without this the browser tree headers
# (Battleships, Cruisers, ...) stay English even when the language is set.
if (Test-Path $marketGroupsPath) {
    Write-Host "  Parsing marketGroups.yaml..." -ForegroundColor Gray
    $marketGroupTranslations = Parse-SdeYaml -FilePath $marketGroupsPath -NameField "nameID"
    # marketGroups use 'nameID:' (with a 'name:' sub-key) in newer SDE; try 'name' too if empty.
    if ($marketGroupTranslations.Count -eq 0) {
        $marketGroupTranslations = Parse-SdeYaml -FilePath $marketGroupsPath -NameField "name"
    }
    Write-Host "  Found $($marketGroupTranslations.Count) market-group translations" -ForegroundColor Green
    foreach ($kvp in $marketGroupTranslations.GetEnumerator()) {
        $groupTranslations[$kvp.Key] = $kvp.Value
    }
}

Write-Host "  Total: $($typeTranslations.Count) types, $($groupTranslations.Count) groups (inventory + market)" -ForegroundColor Green

# Generate XML
Write-Host "`n  Generating eve-translations-$OutputCode.xml.gzip..." -ForegroundColor Yellow

$xml = [System.Text.StringBuilder]::new()
[void]$xml.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$xml.AppendLine("<translations language=`"$OutputCode`">")
[void]$xml.AppendLine('  <skills>')

# Skills are types in category 16, but we include all types for ships/items too
foreach ($kvp in $typeTranslations.GetEnumerator() | Sort-Object Key) {
    $escapedName = [System.Security.SecurityElement]::Escape($kvp.Value)
    [void]$xml.AppendLine("    <skill id=`"$($kvp.Key)`" name=`"$escapedName`" />")
}
[void]$xml.AppendLine('  </skills>')
[void]$xml.AppendLine('  <groups>')
foreach ($kvp in $groupTranslations.GetEnumerator() | Sort-Object Key) {
    $escapedName = [System.Security.SecurityElement]::Escape($kvp.Value)
    [void]$xml.AppendLine("    <group id=`"$($kvp.Key)`" name=`"$escapedName`" />")
}
[void]$xml.AppendLine('  </groups>')
[void]$xml.AppendLine('</translations>')

# Write gzipped
$outputPath = Join-Path $OutputDir "eve-translations-$OutputCode.xml.gzip"
$xmlBytes = [System.Text.Encoding]::UTF8.GetBytes($xml.ToString())

$fileStream = [System.IO.File]::Create($outputPath)
$gzipStream = [System.IO.Compression.GZipStream]::new($fileStream, [System.IO.Compression.CompressionLevel]::Optimal)
$gzipStream.Write($xmlBytes, 0, $xmlBytes.Length)
$gzipStream.Close()
$fileStream.Close()

$sizeMB = [math]::Round((Get-Item $outputPath).Length / 1MB, 2)
Write-Host "  Written: $outputPath ($sizeMB MB)" -ForegroundColor Green
Write-Host "  Types: $($typeTranslations.Count), Groups: $($groupTranslations.Count)" -ForegroundColor Green

# Cleanup
Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "`n=== Done ===" -ForegroundColor Cyan
Write-Host "  Translation file ready ($OutputCode). Restart EveLens to see localized names." -ForegroundColor Green
