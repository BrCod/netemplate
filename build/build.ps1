#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build the Netemplate solution
.DESCRIPTION
    Builds the entire solution with configurable build configuration
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release
.PARAMETER Clean
    Clean before building
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Configuration Debug
    .\build.ps1 -Clean
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter()]
    [switch]$Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$rootDir = Split-Path $PSScriptRoot -Parent
$solutionFile = Join-Path $rootDir "Netemplate.sln"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Netemplate Solution" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Solution: $solutionFile" -ForegroundColor Yellow
Write-Host ""

try {
    Push-Location $rootDir

    if ($Clean) {
        Write-Host "Cleaning solution..." -ForegroundColor Yellow
        dotnet clean $solutionFile --configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed with exit code $LASTEXITCODE"
        }
        Write-Host "Clean completed successfully!" -ForegroundColor Green
        Write-Host ""
    }

    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore $solutionFile
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed with exit code $LASTEXITCODE"
    }
    Write-Host "Restore completed successfully!" -ForegroundColor Green
    Write-Host ""

    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build $solutionFile --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Build failed: $_" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
