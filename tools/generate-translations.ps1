<#
.SYNOPSIS
    Downloads EVE SDE and generates Chinese translation datafiles for EveLens.

.DESCRIPTION
    Downloads the EVE Online Static Data Export (SDE), extracts typeIDs.yaml
    and groupIDs.yaml, parses Chinese (zh) translations, and generates
    eve-translations-zh-CN.xml.gzip for use by StaticTranslations.

.PARAMETER SdeUrl
    URL to the SDE zip. Defaults to latest Tranquility export.

.EXAMPLE
    .\tools\generate-translations.ps1
#>

param(
    [string]$SdeUrl = "https://eve-static-data-export.s3-eu-west-1.amazonaws.com/tranquility/sde.zip"
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$OutputDir = Join-Path $ProjectRoot "src/EveLens.Common/Resources"
$TempDir = Join-Path $env:TEMP "evelens-sde"

Write-Host "`n=== EveLens SDE Translation Generator ===" -ForegroundColor Cyan

# Create temp directory
if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
New-Item -ItemType Directory -Path $TempDir | Out-Null

# Download SDE
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

# Extract only the files we need
Write-Host "  Extracting typeIDs.yaml and groupIDs.yaml..." -ForegroundColor Yellow

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($sdeZip)

$targetFiles = @(
    "fsd/types.yaml",
    "fsd/groups.yaml",
    "fsd/marketGroups.yaml"
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

        # Name section start
        if ($currentId -and $line -match "^\s{2,4}${NameField}:") {
            $inName = $true
            continue
        }

        # Chinese translation within name section
        if ($inName -and $line -match '^\s{4,8}zh:\s*(.+)') {
            $zhName = $Matches[1].Trim().Trim('"').Trim("'")
            if ($zhName -and $zhName.Length -gt 0) {
                $translations[$currentId] = $zhName
            }
            $inName = $false
            continue
        }

        # Any other field at the same or higher level exits the name section
        if ($inName -and $line -match '^\s{2,4}\w+:') {
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

$typeTranslations = @{}
$groupTranslations = @{}

if (Test-Path $typeIdsPath) {
    Write-Host "  Parsing typeIDs.yaml..." -ForegroundColor Gray
    $typeTranslations = Parse-SdeYaml -FilePath $typeIdsPath
    Write-Host "  Found $($typeTranslations.Count) type translations" -ForegroundColor Green
}

if (Test-Path $groupIdsPath) {
    Write-Host "  Parsing groupIDs.yaml..." -ForegroundColor Gray
    $groupTranslations = Parse-SdeYaml -FilePath $groupIdsPath
    Write-Host "  Found $($groupTranslations.Count) group translations" -ForegroundColor Green
}

# Filter to only skills and skill groups (category 16 = Skills)
# We include ALL types since items, ships, blueprints also need translation
Write-Host "  Total: $($typeTranslations.Count) types, $($groupTranslations.Count) groups" -ForegroundColor Green

# Generate XML
Write-Host "`n  Generating eve-translations-zh-CN.xml.gzip..." -ForegroundColor Yellow

$xml = [System.Text.StringBuilder]::new()
[void]$xml.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$xml.AppendLine('<translations language="zh-CN">')
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
$outputPath = Join-Path $OutputDir "eve-translations-zh-CN.xml.gzip"
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
Write-Host "  Translation file ready. Restart EveLens to see Chinese names." -ForegroundColor Green
