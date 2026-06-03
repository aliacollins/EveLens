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
    [string]$SdeUrl = "https://eve-static-data-export.s3-eu-west-1.amazonaws.com/tranquility/sde.zip"
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
