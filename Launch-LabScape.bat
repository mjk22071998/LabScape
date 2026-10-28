@echo off
setlocal enabledelayedexpansion

:: ===================================================================
:: LabScape - Application Launcher
:: Automatically launches the latest compiled executable from this branch:
:: - dist\publish\LabScape.App.exe (Release Publish)
:: - src\LabScape.App\bin\Release\net10.0-windows\LabScape.App.exe
:: - src\LabScape.App\bin\Debug\net10.0-windows\LabScape.App.exe
:: ===================================================================

set "BASE_DIR=%~dp0"
set "TARGET_DIR="
set "TARGET_EXE="

:: 1. Initial check for available binaries
if exist "%BASE_DIR%dist\publish\LabScape.App.exe" (
    set "TARGET_DIR=%BASE_DIR%dist\publish"
    set "TARGET_EXE=%BASE_DIR%dist\publish\LabScape.App.exe"
)

if exist "%BASE_DIR%src\LabScape.App\bin\Release\net10.0-windows\LabScape.App.exe" (
    if not defined TARGET_EXE (
        set "TARGET_DIR=%BASE_DIR%src\LabScape.App\bin\Release\net10.0-windows"
        set "TARGET_EXE=%BASE_DIR%src\LabScape.App\bin\Release\net10.0-windows\LabScape.App.exe"
    )
)

if exist "%BASE_DIR%src\LabScape.App\bin\Debug\net10.0-windows\LabScape.App.exe" (
    if not defined TARGET_EXE (
        set "TARGET_DIR=%BASE_DIR%src\LabScape.App\bin\Debug\net10.0-windows"
        set "TARGET_EXE=%BASE_DIR%src\LabScape.App\bin\Debug\net10.0-windows\LabScape.App.exe"
    )
)

:: 2. Check if a newly compiled Debug or Release build is newer than publish
if exist "%BASE_DIR%src\LabScape.App\bin\Debug\net10.0-windows\LabScape.App.exe" if exist "%BASE_DIR%dist\publish\LabScape.App.exe" (
    for /f %%a in ('powershell -NoProfile -Command "if ((Get-Item '%BASE_DIR%src\LabScape.App\bin\Debug\net10.0-windows\LabScape.App.exe').LastWriteTime -gt (Get-Item '%BASE_DIR%dist\publish\LabScape.App.exe').LastWriteTime) { 'DEBUG' } else { 'PUBLISH' }"') do (
        if "%%a"=="DEBUG" (
            set "TARGET_DIR=%BASE_DIR%src\LabScape.App\bin\Debug\net10.0-windows"
            set "TARGET_EXE=%BASE_DIR%src\LabScape.App\bin\Debug\net10.0-windows\LabScape.App.exe"
        )
    )
)

if not defined TARGET_EXE (
    echo [ERROR] No compiled LabScape executable found!
    echo Please run 'dotnet build' to compile the application.
    pause
    exit /b 1
)

cd /d "%TARGET_DIR%"
start "" "%TARGET_EXE%" %*
