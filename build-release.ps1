# =====================================================================
# LabScape - Production Build and Packaging Script
# =====================================================================
param (
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  LabScape - Production Build and Packaging Pipeline" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Run Automated Test Suite
if (-not $SkipTests) {
    Write-Host "`n[1/4] Running automated test suite..." -ForegroundColor Yellow
    dotnet test LabScape.slnx -c Release --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Unit tests failed! Aborting build."
        exit 1
    }
    Write-Host "  Passed: All unit tests succeeded." -ForegroundColor Green
} else {
    Write-Host "`n[1/4] Skipping tests as requested." -ForegroundColor DarkGray
}

# 2. Publish Application in Release Mode
Write-Host "`n[2/4] Publishing LabScape.App in Release mode..." -ForegroundColor Yellow
$PublishDir = Join-Path $ScriptDir "dist\publish"
if (Test-Path $PublishDir) {
    try {
        Get-ChildItem -Path $PublishDir -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
    } catch {
        # Continue if any locked file
    }
}

dotnet publish src/LabScape.App/LabScape.App.csproj -c Release -o $PublishDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publishing failed! Aborting."
    exit 1
}
Write-Host "  Published to: $PublishDir" -ForegroundColor Green

# 3. Ensure Web Viewer and Assets are Present in Publish Directory
Write-Host "`n[3/4] Verifying web-viewer and assets distribution..." -ForegroundColor Yellow
$WebViewerDest = Join-Path $PublishDir "web-viewer"
if (-not (Test-Path $WebViewerDest)) {
    New-Item -ItemType Directory -Path $WebViewerDest -Force | Out-Null
}
Copy-Item -Path (Join-Path $ScriptDir "web-viewer\*") -Destination $WebViewerDest -Recurse -Force
Write-Host "  Web viewer bundle integrated." -ForegroundColor Green

# 4. Generate Single-File Setup Executable / Inno Setup Compiler
Write-Host "`n[4/4] Building Single-File Setup Installer..." -ForegroundColor Yellow
$SetupDir = Join-Path $ScriptDir "dist\setup"
if (-not (Test-Path $SetupDir)) {
    New-Item -ItemType Directory -Path $SetupDir -Force | Out-Null
}

# Check for Inno Setup 6 ISCC compiler
$isccPath = $null
$foundCommand = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
if ($foundCommand) {
    $isccPath = $foundCommand.Source
} else {
    $candidates = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($cand in $candidates) {
        if (Test-Path $cand) {
            $isccPath = $cand
            break
        }
    }
}

if ($isccPath) {
    Write-Host "  Found Inno Setup Compiler: $isccPath" -ForegroundColor Cyan
    $issFile = Join-Path $ScriptDir "installer\LabScape.iss"
    Start-Process -FilePath $isccPath -ArgumentList "`"$issFile`"" -Wait -NoNewWindow
    Write-Host "  Inno Setup compiled to dist\setup\" -ForegroundColor Green
} else {
    Write-Host "  Inno Setup compiler not found in standard paths." -ForegroundColor Yellow
    Write-Host "  Building self-contained native Single-File Setup Installer..." -ForegroundColor Cyan

    # Compress payload for embedded self-extracting installer
    $payloadZip = Join-Path $ScriptDir "src\LabScape.Setup\Resources\payload.zip"
    $payloadDir = Split-Path -Parent $payloadZip
    if (-not (Test-Path $payloadDir)) {
        New-Item -ItemType Directory -Path $payloadDir -Force | Out-Null
    }
    if (Test-Path $payloadZip) {
        Remove-Item -Force $payloadZip
    }
    Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $payloadZip -Force

    # Compile LabScape.Setup into dist/setup/LabScape-Setup-v1.0.0.exe
    dotnet publish src/LabScape.Setup/LabScape.Setup.csproj -c Release -o $SetupDir
    $setupExe = Join-Path $SetupDir "LabScape.Setup.exe"
    $targetExe = Join-Path $SetupDir "LabScape-Setup-v1.0.0.exe"
    if (Test-Path $setupExe) {
        Copy-Item -Path $setupExe -Destination $targetExe -Force
    }
    Write-Host "  Native Single-File Administrator Setup created: $targetExe" -ForegroundColor Green
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  Build and Packaging Pipeline Complete!" -ForegroundColor Green
Write-Host "  Setup Directory: $SetupDir" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
