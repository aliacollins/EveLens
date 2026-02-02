# build-installer.ps1 - Build EVEMon and create installer
# Usage: .\scripts\build-installer.ps1
# Or with explicit version: .\scripts\build-installer.ps1 -Version 5.2.0

param(
    [Parameter(Mandatory=$false)]
    [string]$Version,
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Get script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

Push-Location $ProjectRoot
try {
    # If version not specified, read from SharedAssemblyInfo.cs
    if (-not $Version) {
        $SharedAssemblyInfo = Get-Content "SharedAssemblyInfo.cs" -Raw
        if ($SharedAssemblyInfo -match 'AssemblyInformationalVersion\("([^"]+)"\)') {
            $Version = $Matches[1]
            # Remove any pre-release suffix for installer filename (e.g., "5.2.0-alpha.1" -> "5.2.0")
            # But keep it for display purposes
            $InstallerVersion = $Version -replace '-.*$', ''
        } else {
            Write-Host "Could not read version from SharedAssemblyInfo.cs" -ForegroundColor Red
            exit 1
        }
    } else {
        $InstallerVersion = $Version
    }

    Write-Host "Building EVEMon Installer v$Version" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan

    # Step 1: Build EVEMon
    if (-not $SkipBuild) {
        Write-Host ""
        Write-Host "Step 1: Building EVEMon..." -ForegroundColor Yellow

        dotnet publish "src\EVEMon\EVEMon.csproj" -c Release -r win-x64 --self-contained false -o "publish\win-x64"

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "Build completed successfully." -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "Step 1: Skipping build (using existing publish\win-x64)" -ForegroundColor Yellow
    }

    # Step 2: Check for Inno Setup
    Write-Host ""
    Write-Host "Step 2: Locating Inno Setup Compiler..." -ForegroundColor Yellow

    $IsccPaths = @(
        "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )

    $IsccPath = $null
    foreach ($path in $IsccPaths) {
        if (Test-Path $path) {
            $IsccPath = $path
            break
        }
    }

    if (-not $IsccPath) {
        Write-Host ""
        Write-Host "Inno Setup 6 not found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please install Inno Setup 6 from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "After installation, run this script again." -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Found: $IsccPath" -ForegroundColor Green

    # Step 3: Compile installer
    Write-Host ""
    Write-Host "Step 3: Compiling installer..." -ForegroundColor Yellow

    $IssFile = Join-Path $ProjectRoot "installer\EVEMon.iss"

    & $IsccPath "/DMyAppVersion=$InstallerVersion" $IssFile

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Installer compilation failed!" -ForegroundColor Red
        exit 1
    }

    # Step 4: Verify output
    Write-Host ""
    Write-Host "Step 4: Verifying output..." -ForegroundColor Yellow

    $InstallerFile = Join-Path $ProjectRoot "publish\EVEMon-install-$InstallerVersion.exe"

    if (Test-Path $InstallerFile) {
        $FileInfo = Get-Item $InstallerFile
        $SizeMB = [math]::Round($FileInfo.Length / 1MB, 2)

        Write-Host ""
        Write-Host "======================================" -ForegroundColor Cyan
        Write-Host "Installer created successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "  File: $InstallerFile" -ForegroundColor White
        Write-Host "  Size: $SizeMB MB" -ForegroundColor White
        Write-Host ""
        Write-Host "To test: Start-Process `"$InstallerFile`"" -ForegroundColor Gray
    } else {
        Write-Host "Installer file not found at expected location: $InstallerFile" -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
}
