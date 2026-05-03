$ErrorActionPreference = 'Stop'
$TempDir = "C:\Users\arpan\AppData\Local\Temp\evelens-sde-may"
$OutputDir = "D:\evemon-main\src\EveLens.Common\Resources"

Write-Host "Parsing types.yaml for zh translations..."
$typeTranslations = @{}
$currentId = $null
$inName = $false

foreach ($line in [System.IO.File]::ReadLines((Join-Path $TempDir "types.yaml"), [System.Text.Encoding]::UTF8)) {
    if ($line -match '^(\d+):$') {
        $currentId = [int]$Matches[1]
        $inName = $false
        continue
    }
    if ($null -ne $currentId -and $line -match '^\s{2}name:$') {
        $inName = $true
        continue
    }
    if ($inName -and $line -match '^\s{4}zh:\s*(.+)') {
        $zhName = $Matches[1].Trim().Trim('"').Trim("'")
        if ($zhName -and $zhName.Length -gt 0) {
            $typeTranslations[$currentId] = $zhName
        }
        $inName = $false
        continue
    }
    if ($inName -and $line -match '^\s{2}\w') {
        $inName = $false
    }
}
Write-Host "  Types with zh: $($typeTranslations.Count)"

Write-Host "Parsing groups.yaml..."
$groupTranslations = @{}
$currentId = $null
$inName = $false

foreach ($line in [System.IO.File]::ReadLines((Join-Path $TempDir "groups.yaml"), [System.Text.Encoding]::UTF8)) {
    if ($line -match '^(\d+):$') {
        $currentId = [int]$Matches[1]
        $inName = $false
        continue
    }
    if ($null -ne $currentId -and $line -match '^\s{2}name:$') {
        $inName = $true
        continue
    }
    if ($inName -and $line -match '^\s{4}zh:\s*(.+)') {
        $zhName = $Matches[1].Trim().Trim('"').Trim("'")
        if ($zhName -and $zhName.Length -gt 0) {
            $groupTranslations[$currentId] = $zhName
        }
        $inName = $false
        continue
    }
    if ($inName -and $line -match '^\s{2}\w') {
        $inName = $false
    }
}
Write-Host "  Groups with zh: $($groupTranslations.Count)"

Write-Host "Parsing marketGroups.yaml..."
$marketGroupTranslations = @{}
$currentId = $null
$inName = $false

$mgPath = Join-Path $TempDir "marketGroups.yaml"
if (Test-Path $mgPath) {
    foreach ($line in [System.IO.File]::ReadLines($mgPath, [System.Text.Encoding]::UTF8)) {
        if ($line -match '^(\d+):$') {
            $currentId = [int]$Matches[1]
            $inName = $false
            continue
        }
        if ($null -ne $currentId -and $line -match '^\s{2}nameID:$') {
            $inName = $true
            continue
        }
        if ($inName -and $line -match '^\s{4}zh:\s*(.+)') {
            $zhName = $Matches[1].Trim().Trim('"').Trim("'")
            if ($zhName -and $zhName.Length -gt 0) {
                $marketGroupTranslations[$currentId] = $zhName
            }
            $inName = $false
            continue
        }
        if ($inName -and $line -match '^\s{2}\w') { $inName = $false }
    }
}
Write-Host "  Market groups with zh: $($marketGroupTranslations.Count)"

# Merge market group translations into group translations
foreach ($kvp in $marketGroupTranslations.GetEnumerator()) {
    $groupTranslations[$kvp.Key] = $kvp.Value
}
Write-Host "  Total groups (merged): $($groupTranslations.Count)"

Write-Host "Generating XML..."
$xml = [System.Text.StringBuilder]::new()
[void]$xml.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$xml.AppendLine('<translations language="zh-CN">')
[void]$xml.AppendLine('  <skills>')
foreach ($kvp in $typeTranslations.GetEnumerator() | Sort-Object Key) {
    $escaped = [System.Security.SecurityElement]::Escape($kvp.Value)
    [void]$xml.AppendLine("    <skill id=`"$($kvp.Key)`" name=`"$escaped`" />")
}
[void]$xml.AppendLine('  </skills>')
[void]$xml.AppendLine('  <groups>')
foreach ($kvp in $groupTranslations.GetEnumerator() | Sort-Object Key) {
    $escaped = [System.Security.SecurityElement]::Escape($kvp.Value)
    [void]$xml.AppendLine("    <group id=`"$($kvp.Key)`" name=`"$escaped`" />")
}
[void]$xml.AppendLine('  </groups>')
[void]$xml.AppendLine('</translations>')

$outputPath = Join-Path $OutputDir "eve-translations-zh-CN.xml.gzip"
$xmlBytes = [System.Text.Encoding]::UTF8.GetBytes($xml.ToString())
$fs = [System.IO.File]::Create($outputPath)
$gz = [System.IO.Compression.GZipStream]::new($fs, [System.IO.Compression.CompressionLevel]::Optimal)
$gz.Write($xmlBytes, 0, $xmlBytes.Length)
$gz.Close()
$fs.Close()

$sizeMB = [math]::Round((Get-Item $outputPath).Length / 1MB, 2)
Write-Host "Written: $outputPath ($sizeMB MB)"
Write-Host "Types: $($typeTranslations.Count), Groups: $($groupTranslations.Count)"
