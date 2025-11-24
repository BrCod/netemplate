#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run the Netemplate API
.DESCRIPTION
    Starts the API application with configurable environment and launch profile
.PARAMETER Environment
    ASP.NET Core environment (Development, Staging, Production). Default: Development
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Debug
.PARAMETER NoBuild
    Skip building before running
.PARAMETER Watch
    Run with hot reload (watch mode)
.PARAMETER LaunchProfile
    Launch profile to use (e.g., "http", "https")
.EXAMPLE
    .\run.ps1
    .\run.ps1 -Environment Production
    .\run.ps1 -Watch
    .\run.ps1 -LaunchProfile https
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Development', 'Staging', 'Production')]
    [string]$Environment = 'Development',
    
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    
    [Parameter()]
    [switch]$NoBuild,
    
    [Parameter()]
    [switch]$Watch,
    
    [Parameter()]
    [string]$LaunchProfile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$rootDir = Split-Path $PSScriptRoot -Parent
$apiProject = Join-Path $rootDir "src\Api\Netemplate.Api.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running Netemplate API" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Project: $apiProject" -ForegroundColor Yellow
if ($Watch) {
    Write-Host "Mode: Watch (Hot Reload)" -ForegroundColor Yellow
}
if ($LaunchProfile) {
    Write-Host "Launch Profile: $LaunchProfile" -ForegroundColor Yellow
}
Write-Host ""

try {
    Push-Location (Split-Path $apiProject -Parent)

    # Set environment variable
    $env:ASPNETCORE_ENVIRONMENT = $Environment

    $runArgs = @()
    
    if ($Watch) {
        $runArgs += 'watch'
        $runArgs += 'run'
    } else {
        $runArgs += 'run'
    }

    $runArgs += '--configuration', $Configuration

    if ($NoBuild) {
        $runArgs += '--no-build'
    }

    if ($LaunchProfile) {
        $runArgs += '--launch-profile', $LaunchProfile
    }

    Write-Host "Starting API..." -ForegroundColor Yellow
    Write-Host "Command: dotnet $($runArgs -join ' ')" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Cyan
    Write-Host ""

    & dotnet $runArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Application exited with code $LASTEXITCODE"
    }
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Application failed: $_" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
