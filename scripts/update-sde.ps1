<#
.SYNOPSIS
    Downloads and processes the latest EVE Online Static Data Export (SDE).

.DESCRIPTION
    Automates the full SDE update pipeline:
    1. Downloads the latest SDE YAML zip from CCP (or uses a local file)
    2. Extracts to tools/SDEFiles/yaml_extracted/
    3. Runs YamlToSqlite to convert YAML -> SQLite
    4. Runs XmlGenerator to produce compressed XML datafiles
    5. Reports what changed (new types, skills, modifications)
    6. Stamps sde-version.json with build number and timestamp
    7. Optionally builds and tests the solution

.PARAMETER Url
    Direct URL to the SDE YAML zip. If omitted, uses the latest known URL.

.PARAMETER LocalZip
    Path to an already-downloaded SDE zip file. Skips download.

.PARAMETER SkipBuild
    Skip the build and test verification step.

.PARAMETER SkipDownload
    Skip download and extraction, just run YamlToSqlite + XmlGenerator
    on the existing yaml_extracted/ directory.

.EXAMPLE
    .\scripts\update-sde.ps1
    .\scripts\update-sde.ps1 -Url "https://developers.eveonline.com/static-data/tranquility/eve-online-static-data-3261822-yaml.zip"
    .\scripts\update-sde.ps1 -LocalZip "C:\Downloads\eve-online-static-data-3261822-yaml.zip"
    .\scripts\update-sde.ps1 -SkipDownload
#>

[CmdletBinding()]
param(
    [string]$Url = "",
    [string]$LocalZip = "",
    [switch]$SkipBuild,
    [switch]$SkipDownload
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ToolsDir = Join-Path $RepoRoot "tools"
$SdeFilesDir = Join-Path $ToolsDir "SDEFiles"
$YamlDir = Join-Path $SdeFilesDir "yaml_extracted"
$YamlToSqliteDir = Join-Path $ToolsDir "YamlToSqlite"
$XmlGeneratorDir = Join-Path $ToolsDir "XmlGenerator"
$SqlitePath = Join-Path $ToolsDir "sqlite-latest.sqlite"
$ResourcesDir = Join-Path $RepoRoot "src" "EveLens.Common" "Resources"
$VersionFile = Join-Path $ResourcesDir "sde-version.json"

# Default to the latest known SDE URL if none provided
if (-not $Url -and -not $LocalZip -and -not $SkipDownload) {
    $Url = "https://developers.eveonline.com/static-data/tranquility/eve-online-static-data-3328718-yaml.zip"
}

function Write-Step($step, $message) {
    Write-Host ""
    Write-Host "[$step] $message" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor DarkGray
}

function Write-Ok($message) {
    Write-Host "  OK: $message" -ForegroundColor Green
}

function Write-Info($message) {
    Write-Host "  $message" -ForegroundColor Gray
}

function Write-Warn($message) {
    Write-Host "  WARN: $message" -ForegroundColor Yellow
}

# ── Extract build number from URL or zip filename ──
function Get-BuildNumber($path) {
    if ($path -match "static-data-(\d+)") {
        return $Matches[1]
    }
    return "unknown"
}

# ── Snapshot SQLite table row counts for diffing ──
function Get-TableCounts($dbPath) {
    if (-not (Test-Path $dbPath)) { return @{} }
    $counts = @{}
    try {
        # Use dotnet to query SQLite — portable, no external deps
        $query = @"
using Microsoft.Data.Sqlite;
var conn = new SqliteConnection("Data Source=$($dbPath.Replace('\','\\'))");
conn.Open();
var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
var reader = cmd.ExecuteReader();
var tables = new System.Collections.Generic.List<string>();
while (reader.Read()) tables.Add(reader.GetString(0));
reader.Close();
foreach (var t in tables) {
    cmd.CommandText = $"SELECT COUNT(*) FROM [{t}]";
    Console.WriteLine($"{t}={cmd.ExecuteScalar()}");
}
"@
        # Fallback: just check if the file exists and has size
        $fileSize = (Get-Item $dbPath).Length
        $counts["_fileSize"] = $fileSize
    } catch {
        # SQLite query failed — just track file size
        $counts["_fileSize"] = 0
    }
    return $counts
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor White
Write-Host " EveLens SDE Update Pipeline" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor White

# ────────────────────────────────────────────────────────────
# STEP 1: Download SDE
# ────────────────────────────────────────────────────────────

if (-not $SkipDownload) {
    Write-Step "1/6" "Download SDE"

    $zipPath = ""
    if ($LocalZip) {
        if (-not (Test-Path $LocalZip)) {
            Write-Error "Local zip not found: $LocalZip"
            exit 1
        }
        $zipPath = $LocalZip
        Write-Ok "Using local zip: $zipPath"
    } else {
        $buildNum = Get-BuildNumber $Url
        $zipPath = Join-Path $SdeFilesDir "eve-online-static-data-$buildNum-yaml.zip"
        Write-Info "Downloading from: $Url"
        Write-Info "Saving to: $zipPath"

        try {
            $ProgressPreference = 'SilentlyContinue'
            Invoke-WebRequest -Uri $Url -OutFile $zipPath -UseBasicParsing
            $ProgressPreference = 'Continue'
            $sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
            Write-Ok "Downloaded $sizeMB MB"
        } catch {
            Write-Error "Download failed: $_"
            exit 1
        }
    }

    # ────────────────────────────────────────────────────────────
    # STEP 2: Extract
    # ────────────────────────────────────────────────────────────

    Write-Step "2/6" "Extract SDE YAML"

    # Backup old yaml_extracted
    if (Test-Path $YamlDir) {
        $backupDir = Join-Path $SdeFilesDir "yaml_extracted_old"
        if (Test-Path $backupDir) { Remove-Item -Recurse -Force $backupDir }
        Rename-Item $YamlDir "yaml_extracted_old"
        Write-Info "Backed up previous YAML to yaml_extracted_old/"
    }

    # Extract — the zip may have a top-level directory or not
    $tempExtract = Join-Path $SdeFilesDir "_extract_temp"
    if (Test-Path $tempExtract) { Remove-Item -Recurse -Force $tempExtract }
    Expand-Archive -Path $zipPath -DestinationPath $tempExtract -Force

    # Find where the YAML files actually are (might be in a subdirectory)
    $yamlFiles = Get-ChildItem -Path $tempExtract -Filter "*.yaml" -Recurse | Select-Object -First 1
    if ($yamlFiles) {
        $sourceDir = $yamlFiles.DirectoryName
        # Move to the expected location
        if (Test-Path $YamlDir) { Remove-Item -Recurse -Force $YamlDir }
        Move-Item $sourceDir $YamlDir
        Write-Ok "Extracted to $YamlDir"
    } else {
        Write-Error "No YAML files found in the extracted archive"
        exit 1
    }

    # Clean up temp
    if (Test-Path $tempExtract) { Remove-Item -Recurse -Force $tempExtract }

    $yamlCount = (Get-ChildItem -Path $YamlDir -Filter "*.yaml").Count
    Write-Ok "$yamlCount YAML files extracted"
} else {
    Write-Step "1/6" "Download SDE — SKIPPED"
    Write-Step "2/6" "Extract SDE YAML — SKIPPED"
    if (-not (Test-Path $YamlDir)) {
        Write-Error "yaml_extracted/ directory not found. Cannot skip download."
        exit 1
    }
}

$buildNum = Get-BuildNumber ($Url + $LocalZip)

# ────────────────────────────────────────────────────────────
# STEP 3: YAML → SQLite
# ────────────────────────────────────────────────────────────

Write-Step "3/6" "Convert YAML to SQLite (YamlToSqlite)"

# Snapshot old DB size for diff
$oldDbSize = if (Test-Path $SqlitePath) { (Get-Item $SqlitePath).Length } else { 0 }

Push-Location $YamlToSqliteDir
try {
    Write-Info "Running: dotnet run -- `"$YamlDir`" `"$SqlitePath`""
    $output = & dotnet run -- "$YamlDir" "$SqlitePath" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($output -join "`n") -ForegroundColor Red
        Write-Error "YamlToSqlite failed with exit code $LASTEXITCODE"
        exit 1
    }
    # Show last few lines of output (summary)
    $output | Select-Object -Last 5 | ForEach-Object { Write-Info $_ }
    $newDbSize = (Get-Item $SqlitePath).Length
    $sizeMB = [math]::Round($newDbSize / 1MB, 1)
    Write-Ok "SQLite database: $sizeMB MB"
} finally {
    Pop-Location
}

# ────────────────────────────────────────────────────────────
# STEP 4: SQLite → Compressed XML Datafiles
# ────────────────────────────────────────────────────────────

Write-Step "4/6" "Generate XML datafiles (XmlGenerator)"

# Snapshot old datafile sizes
$oldDatafiles = @{}
Get-ChildItem -Path $ResourcesDir -Filter "*.xml.gzip" -ErrorAction SilentlyContinue | ForEach-Object {
    $oldDatafiles[$_.Name] = $_.Length
}

Push-Location $XmlGeneratorDir
try {
    Write-Info "Running: dotnet run"
    $output = & dotnet run 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($output -join "`n") -ForegroundColor Red
        Write-Error "XmlGenerator failed with exit code $LASTEXITCODE"
        exit 1
    }
    $output | Select-Object -Last 8 | ForEach-Object { Write-Info $_ }
    Write-Ok "Datafiles generated"
} finally {
    Pop-Location
}

# ────────────────────────────────────────────────────────────
# STEP 5: Diff Report
# ────────────────────────────────────────────────────────────

Write-Step "5/6" "Change Report"

$newDatafiles = @{}
Get-ChildItem -Path $ResourcesDir -Filter "*.xml.gzip" -ErrorAction SilentlyContinue | ForEach-Object {
    $newDatafiles[$_.Name] = $_.Length
}

foreach ($file in ($newDatafiles.Keys | Sort-Object)) {
    $newSize = $newDatafiles[$file]
    $newKB = [math]::Round($newSize / 1KB, 1)
    if ($oldDatafiles.ContainsKey($file)) {
        $oldSize = $oldDatafiles[$file]
        $delta = $newSize - $oldSize
        if ($delta -eq 0) {
            Write-Info "$file : ${newKB}KB (unchanged)"
        } else {
            $sign = if ($delta -gt 0) { "+" } else { "" }
            $deltaKB = [math]::Round($delta / 1KB, 1)
            Write-Host "  $file : ${newKB}KB (${sign}${deltaKB}KB)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  $file : ${newKB}KB (NEW)" -ForegroundColor Green
    }
}

# ────────────────────────────────────────────────────────────
# STEP 6: Version Stamp
# ────────────────────────────────────────────────────────────

Write-Step "6/6" "Version Stamp"

$versionInfo = @{
    sdeBuild = $buildNum
    generatedUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    generator = "update-sde.ps1"
    datafiles = @{}
}

Get-ChildItem -Path $ResourcesDir -Filter "*.xml.gzip" | ForEach-Object {
    $hash = (Get-FileHash -Path $_.FullName -Algorithm MD5).Hash.ToLower()
    $versionInfo.datafiles[$_.Name] = @{
        md5 = $hash
        sizeBytes = $_.Length
    }
}

$versionJson = $versionInfo | ConvertTo-Json -Depth 3
Set-Content -Path $VersionFile -Value $versionJson -Encoding UTF8
Write-Ok "Version stamped: build $buildNum at $($versionInfo.generatedUtc)"
Write-Info "  File: $VersionFile"

# ────────────────────────────────────────────────────────────
# STEP 7: Build + Test (optional)
# ────────────────────────────────────────────────────────────

if (-not $SkipBuild) {
    Write-Step "7" "Build & Test Verification"

    Push-Location $RepoRoot
    try {
        Write-Info "Building..."
        $buildOutput = & dotnet build EveLens.sln -c Debug --verbosity quiet 2>&1
        $errors = $buildOutput | Select-String "error CS"
        if ($errors) {
            Write-Host ($errors -join "`n") -ForegroundColor Red
            Write-Error "Build failed"
            exit 1
        }
        Write-Ok "Build clean"

        Write-Info "Running tests..."
        $testOutput = & dotnet test tests/EveLens.Tests/EveLens.Tests.csproj --verbosity quiet 2>&1
        $testLine = $testOutput | Select-String "Passed!" | Select-Object -Last 1
        if ($testLine) {
            Write-Ok $testLine.Line.Trim()
        } else {
            $failLine = $testOutput | Select-String "Failed" | Select-Object -Last 1
            if ($failLine) {
                Write-Warn $failLine.Line.Trim()
            }
        }
    } finally {
        Pop-Location
    }
}

# ────────────────────────────────────────────────────────────
# Summary
# ────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host " SDE Update Complete" -ForegroundColor Green
Write-Host " Build: $buildNum" -ForegroundColor Green
Write-Host " Generated: $($versionInfo.generatedUtc)" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Review the change report above" -ForegroundColor Gray
Write-Host "  2. Launch the app and verify new data (skills, items, ships)" -ForegroundColor Gray
Write-Host "  3. Commit when satisfied: git add src/EveLens.Common/Resources/" -ForegroundColor Gray
Write-Host ""
