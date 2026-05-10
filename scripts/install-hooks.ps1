<#
.SYNOPSIS
    Installs git hooks for code graph auto-regeneration.

.DESCRIPTION
    Sets up a post-commit hook that regenerates .codegraph/ when .cs files change.
    The hook only runs the generator if structural files were modified (events, services, interfaces).

.EXAMPLE
    .\scripts\install-hooks.ps1
#>

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$HooksDir = Join-Path $RepoRoot ".git/hooks"

if (-not (Test-Path $HooksDir)) {
    Write-Host "ERROR: .git/hooks directory not found. Are you in a git repo?" -ForegroundColor Red
    exit 1
}

$hookContent = @'
#!/bin/bash
# Auto-regenerate code graph when structural files change
# Installed by scripts/install-hooks.ps1

CHANGED_FILES=$(git diff-tree --no-commit-id --name-only -r HEAD 2>/dev/null)

# Only regenerate if structural files changed
if echo "$CHANGED_FILES" | grep -qE "(AppServices|EventAggregator|Events/|Interfaces/|\.csproj)"; then
    echo "[codegraph] Structural change detected, regenerating graph..."
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "scripts/codegraph.ps1" -Quiet 2>/dev/null
    if [ $? -eq 0 ]; then
        echo "[codegraph] Graph updated."
    else
        echo "[codegraph] Warning: graph generation failed (non-blocking)."
    fi
fi
'@

$hookPath = Join-Path $HooksDir "post-commit"

# Check if hook already exists
if (Test-Path $hookPath) {
    $existing = Get-Content $hookPath -Raw
    if ($existing -match "codegraph") {
        Write-Host "Post-commit hook already has codegraph integration." -ForegroundColor Green
        exit 0
    }
    # Append to existing hook
    Add-Content -Path $hookPath -Value "`n$hookContent"
    Write-Host "Appended codegraph to existing post-commit hook." -ForegroundColor Green
} else {
    Set-Content -Path $hookPath -Value $hookContent -Encoding UTF8 -NoNewline
    Write-Host "Created post-commit hook with codegraph integration." -ForegroundColor Green
}

# Make executable (matters in WSL/Unix)
& git update-index --chmod=+x $hookPath 2>$null

Write-Host "Done! The code graph will auto-regenerate on commits that change structural files." -ForegroundColor Cyan
