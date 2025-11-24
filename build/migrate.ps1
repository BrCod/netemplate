#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Manage database migrations
.DESCRIPTION
    Add, remove, apply, or rollback Entity Framework Core migrations
.PARAMETER Action
    Migration action: Add, Remove, Update, List, Script, Drop
.PARAMETER Name
    Migration name (required for Add action)
.PARAMETER Target
    Target migration for Update or Script actions (use "0" for rollback to initial state)
.PARAMETER Environment
    ASP.NET Core environment. Default: Development
.PARAMETER Output
    Output file path for Script action
.EXAMPLE
    .\migrate.ps1 -Action Add -Name InitialCreate
    .\migrate.ps1 -Action Update
    .\migrate.ps1 -Action Update -Target PreviousMigration
    .\migrate.ps1 -Action Script -Output migration.sql
    .\migrate.ps1 -Action List
    .\migrate.ps1 -Action Remove
    .\migrate.ps1 -Action Drop
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Add', 'Remove', 'Update', 'List', 'Script', 'Drop')]
    [string]$Action,
    
    [Parameter()]
    [string]$Name,
    
    [Parameter()]
    [string]$Target,
    
    [Parameter()]
    [ValidateSet('Development', 'Staging', 'Production')]
    [string]$Environment = 'Development',
    
    [Parameter()]
    [string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$rootDir = Split-Path $PSScriptRoot -Parent
$persistenceProject = Join-Path $rootDir "src\Infrastructure.Persistence.Postgres\Netemplate.Infrastructure.Persistence.Postgres.csproj"
$apiProject = Join-Path $rootDir "src\Api\Netemplate.Api.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Migration: $Action" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Persistence Project: $persistenceProject" -ForegroundColor Yellow
Write-Host ""

# Set environment variable
$env:ASPNETCORE_ENVIRONMENT = $Environment

try {
    Push-Location $rootDir

    # Ensure EF Core tools are installed
    Write-Host "Checking EF Core tools..." -ForegroundColor Yellow
    $efVersion = dotnet tool list --global | Select-String "dotnet-ef"
    if (-not $efVersion) {
        Write-Host "Installing EF Core tools..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-ef
    } else {
        Write-Host "EF Core tools already installed: $efVersion" -ForegroundColor Green
    }
    Write-Host ""

    switch ($Action) {
        'Add' {
            if (-not $Name) {
                throw "Migration name is required for Add action. Use -Name parameter."
            }
            Write-Host "Adding migration: $Name" -ForegroundColor Yellow
            dotnet ef migrations add $Name --project $persistenceProject --startup-project $apiProject
        }
        'Remove' {
            Write-Host "Removing last migration..." -ForegroundColor Yellow
            Write-Host "WARNING: This will remove the last migration file." -ForegroundColor Red
            $confirmation = Read-Host "Continue? (y/N)"
            if ($confirmation -eq 'y') {
                dotnet ef migrations remove --project $persistenceProject --startup-project $apiProject
            } else {
                Write-Host "Cancelled." -ForegroundColor Yellow
                exit 0
            }
        }
        'Update' {
            if ($Target) {
                Write-Host "Updating database to migration: $Target" -ForegroundColor Yellow
                if ($Target -eq '0') {
                    Write-Host "WARNING: This will rollback ALL migrations!" -ForegroundColor Red
                    $confirmation = Read-Host "Continue? (y/N)"
                    if ($confirmation -ne 'y') {
                        Write-Host "Cancelled." -ForegroundColor Yellow
                        exit 0
                    }
                }
                dotnet ef database update $Target --project $persistenceProject --startup-project $apiProject
            } else {
                Write-Host "Updating database to latest migration..." -ForegroundColor Yellow
                dotnet ef database update --project $persistenceProject --startup-project $apiProject
            }
        }
        'List' {
            Write-Host "Listing migrations..." -ForegroundColor Yellow
            dotnet ef migrations list --project $persistenceProject --startup-project $apiProject
        }
        'Script' {
            $scriptArgs = @(
                'migrations', 'script'
                '--project', $persistenceProject
                '--startup-project', $apiProject
                '--idempotent'
            )
            
            if ($Target) {
                $scriptArgs += '--migration', $Target
            }
            
            if ($Output) {
                Write-Host "Generating migration script to: $Output" -ForegroundColor Yellow
                $scriptArgs += '--output', $Output
            } else {
                Write-Host "Generating migration script..." -ForegroundColor Yellow
            }
            
            dotnet ef $scriptArgs
        }
        'Drop' {
            Write-Host "WARNING: This will DROP the entire database!" -ForegroundColor Red
            $confirmation = Read-Host "Are you sure? Type 'DROP' to confirm"
            if ($confirmation -eq 'DROP') {
                dotnet ef database drop --project $persistenceProject --startup-project $apiProject --force
            } else {
                Write-Host "Cancelled." -ForegroundColor Yellow
                exit 0
            }
        }
    }

    if ($LASTEXITCODE -ne 0) {
        throw "Migration command failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Migration $Action completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Migration failed: $_" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
