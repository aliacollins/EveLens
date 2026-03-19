<#
.SYNOPSIS
    Interactive promotion dashboard with local HTTP server.

.DESCRIPTION
    Launches a local web server (localhost:5050) serving an interactive
    dashboard. The browser sends commands via fetch(), the server executes
    them (build, test, git, changelog), and returns results in real-time.

.PARAMETER Channel
    Target channel: alpha, beta, or stable.

.PARAMETER Port
    Local server port (default: 5050).

.EXAMPLE
    .\scripts\promote-dashboard.ps1 alpha
    .\scripts\promote-dashboard.ps1 beta -Port 8080
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet("alpha", "beta", "stable")]
    [string]$Channel = "alpha",

    [int]$Port = 5050
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$RepoUrl = "https://github.com/aliacollins/evelens"

Set-Location $RepoRoot

# ── Gather static info ──
$currentBranch = git branch --show-current
$lastCommit = git log --oneline -1
$versionLine = Select-String -Path "SharedAssemblyInfo.cs" -Pattern 'AssemblyInformationalVersion\("([^"]+)"\)'
$currentVersion = if ($versionLine.Matches.Groups.Count -gt 1) { $versionLine.Matches.Groups[1].Value } else { "unknown" }

$sourceBranch = switch ($Channel) { "alpha" { $currentBranch } "beta" { "alpha" } "stable" { "beta" } }
$targetBranch = switch ($Channel) { "alpha" { "alpha" } "beta" { "beta" } "stable" { "main" } }

# ── Read HTML template ──
$html = Get-Content "$RepoRoot/scripts/templates/promote-dashboard.html" -Raw

# Inject static config into HTML
$configJson = @{
    channel = $Channel
    sourceBranch = $sourceBranch
    targetBranch = $targetBranch
    currentVersion = $currentVersion
    currentBranch = $currentBranch
    lastCommit = $lastCommit
    repoUrl = $RepoUrl
    port = $Port
    prUrl = "$RepoUrl/compare/$targetBranch...$sourceBranch"
} | ConvertTo-Json -Compress

$html = $html -replace '__CONFIG__', $configJson

# ── API Handlers ──
function Handle-Api($path) {
    switch ($path) {
        "/api/status" {
            $uncommitted = git status --short
            $hasUncommitted = ($uncommitted | Where-Object { $_ -ne "" } | Measure-Object).Count -gt 0
            return @{ pass = (-not $hasUncommitted); detail = if ($hasUncommitted) { ($uncommitted | Select-Object -First 5) -join "; " } else { "Working tree clean" } } | ConvertTo-Json
        }
        "/api/build" {
            $output = & dotnet build EveLens.sln -c Debug --verbosity quiet 2>&1
            $errors = ($output | Select-String "error CS" | Measure-Object).Count
            return @{ pass = ($errors -eq 0); detail = if ($errors -eq 0) { "Build clean (0 errors)" } else { "$errors error(s) found" }; errors = $errors } | ConvertTo-Json
        }
        "/api/tests" {
            $output = & dotnet test tests/EveLens.Tests/EveLens.Tests.csproj --verbosity quiet 2>&1
            $passLine = $output | Select-String "Passed!"
            $pass = $passLine -ne $null
            $count = "?"
            if ("$passLine" -match "Passed:\s*(\d+)") { $count = $Matches[1] }
            return @{ pass = $pass; detail = if ($pass) { "$count tests passing" } else { "Tests failed" }; count = $count } | ConvertTo-Json
        }
        "/api/changelog" {
            $content = Get-Content "CHANGELOG.md" -Raw
            $m = [regex]::Match($content, '(?s)## \[Unreleased\]\s*\n(.*?)(?=\n## \[)')
            $unreleased = if ($m.Success) { $m.Groups[1].Value.Trim() } else { "" }
            return @{ content = $unreleased; hasEntries = ($unreleased.Length -gt 10) } | ConvertTo-Json -Depth 2
        }
        "/api/dangerous" {
            $files = @()
            try {
                $changed = git diff "$targetBranch...$sourceBranch" --name-only 2>$null
                foreach ($f in $changed) {
                    if ($f -match "ESIKey\.cs") { $files += @{ file = "ESIKey.cs"; level = "BLOCK"; reason = "Never modify deserialization" } }
                    if ($f -match "EveLensClient\.cs") { $files += @{ file = "EveLensClient.cs"; level = "WARN"; reason = "Frozen god object" } }
                }
            } catch { }
            return @{ pass = ($files.Count -eq 0); files = $files } | ConvertTo-Json -Depth 3
        }
        "/api/diff" {
            try {
                $stat = git diff "$targetBranch...$sourceBranch" --stat 2>$null
                $fileCount = [Math]::Max(0, ($stat | Measure-Object).Count - 1)
                $summary = if ($stat) { $stat[-1] } else { "No changes" }
                return @{ files = $fileCount; summary = "$summary" } | ConvertTo-Json
            } catch {
                return @{ files = 0; summary = "Could not compute diff" } | ConvertTo-Json
            }
        }
        "/api/push" {
            try {
                $output = git push origin $currentBranch 2>&1
                return @{ pass = $true; detail = "Pushed $currentBranch to origin" } | ConvertTo-Json
            } catch {
                return @{ pass = $false; detail = "$_" } | ConvertTo-Json
            }
        }
        default {
            return '{"error": "Unknown endpoint"}'
        }
    }
}

# ── HTTP Server ──
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:${Port}/")
$listener.Start()

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  EveLens Promotion Dashboard" -ForegroundColor Cyan
Write-Host "  http://localhost:${Port}" -ForegroundColor White
Write-Host "  Channel: $Channel ($sourceBranch -> $targetBranch)" -ForegroundColor Gray
Write-Host "  Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Start-Process "http://localhost:${Port}"

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $request = $context.Request
        $response = $context.Response

        $path = $request.Url.LocalPath

        if ($path -eq "/" -or $path -eq "/index.html") {
            # Serve dashboard HTML
            $buffer = [System.Text.Encoding]::UTF8.GetBytes($html)
            $response.ContentType = "text/html; charset=utf-8"
            $response.ContentLength64 = $buffer.Length
            $response.OutputStream.Write($buffer, 0, $buffer.Length)
        }
        elseif ($path.StartsWith("/api/")) {
            # API endpoint
            Write-Host "  API: $path" -ForegroundColor DarkGray
            $result = Handle-Api $path
            $buffer = [System.Text.Encoding]::UTF8.GetBytes($result)
            $response.ContentType = "application/json; charset=utf-8"
            $response.Headers.Add("Access-Control-Allow-Origin", "*")
            $response.ContentLength64 = $buffer.Length
            $response.OutputStream.Write($buffer, 0, $buffer.Length)
        }
        else {
            $response.StatusCode = 404
            $buffer = [System.Text.Encoding]::UTF8.GetBytes("Not found")
            $response.ContentLength64 = $buffer.Length
            $response.OutputStream.Write($buffer, 0, $buffer.Length)
        }

        $response.Close()
    }
}
finally {
    $listener.Stop()
    Write-Host "Server stopped." -ForegroundColor Yellow
}
