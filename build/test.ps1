#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run tests for the Netemplate solution
.DESCRIPTION
    Executes unit and integration tests with optional coverage reporting
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Debug
.PARAMETER Filter
    Test filter expression (e.g., "FullyQualifiedName~ProductTests")
.PARAMETER Coverage
    Generate code coverage report
.PARAMETER NoBuild
    Skip building before running tests
.EXAMPLE
    .\test.ps1
    .\test.ps1 -Coverage
    .\test.ps1 -Filter "FullyQualifiedName~Product"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    
    [Parameter()]
    [string]$Filter,
    
    [Parameter()]
    [switch]$Coverage,
    
    [Parameter()]
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$rootDir = Split-Path $PSScriptRoot -Parent
$testsDir = Join-Path $rootDir "tests"
$coverageDir = Join-Path $rootDir "coverage"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running Netemplate Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
if ($Filter) {
    Write-Host "Filter: $Filter" -ForegroundColor Yellow
}
if ($Coverage) {
    Write-Host "Coverage: Enabled" -ForegroundColor Yellow
}
Write-Host ""

try {
    Push-Location $rootDir

    $testArgs = @(
        'test'
        '--configuration', $Configuration
        '--verbosity', 'normal'
    )

    if ($NoBuild) {
        $testArgs += '--no-build'
    }

    if ($Filter) {
        $testArgs += '--filter', $Filter
    }

    if ($Coverage) {
        Write-Host "Setting up code coverage..." -ForegroundColor Yellow
        
        if (Test-Path $coverageDir) {
            Remove-Item $coverageDir -Recurse -Force
        }
        New-Item -ItemType Directory -Path $coverageDir -Force | Out-Null

        $testArgs += '--collect:"XPlat Code Coverage"'
        $testArgs += '--results-directory', $coverageDir
        $testArgs += '--settings', (Join-Path $PSScriptRoot "coverlet.runsettings")
    }

    Write-Host "Executing tests..." -ForegroundColor Yellow
    Write-Host "Command: dotnet $($testArgs -join ' ')" -ForegroundColor DarkGray
    Write-Host ""

    & dotnet $testArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE"
    }

    if ($Coverage) {
        Write-Host ""
        Write-Host "Coverage report generated in: $coverageDir" -ForegroundColor Yellow
        
        # Find the coverage file
        $coverageFile = Get-ChildItem -Path $coverageDir -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1
        if ($coverageFile) {
            Write-Host "Coverage file: $($coverageFile.FullName)" -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "All tests passed!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Tests failed: $_" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
