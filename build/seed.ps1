#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Seed the database with test data
.DESCRIPTION
    Populates the database with sample data for development and testing
.PARAMETER Environment
    ASP.NET Core environment. Default: Development
.PARAMETER Clear
    Clear existing data before seeding
.EXAMPLE
    .\seed.ps1
    .\seed.ps1 -Clear
    .\seed.ps1 -Environment Staging
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Development', 'Staging', 'Production')]
    [string]$Environment = 'Development',
    
    [Parameter()]
    [switch]$Clear
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$rootDir = Split-Path $PSScriptRoot -Parent
$apiProject = Join-Path $rootDir "src\Api\Netemplate.Api.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Seeding Database" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
if ($Clear) {
    Write-Host "Mode: Clear and Seed" -ForegroundColor Yellow
}
Write-Host ""

# Set environment variable
$env:ASPNETCORE_ENVIRONMENT = $Environment

try {
    Push-Location $rootDir

    if ($Environment -eq 'Production') {
        Write-Host "WARNING: You are about to seed a PRODUCTION database!" -ForegroundColor Red
        $confirmation = Read-Host "Are you absolutely sure? Type 'SEED PRODUCTION' to confirm"
        if ($confirmation -ne 'SEED PRODUCTION') {
            Write-Host "Cancelled." -ForegroundColor Yellow
            exit 0
        }
    }

    if ($Clear) {
        Write-Host "Clearing existing data..." -ForegroundColor Yellow
        Write-Host "WARNING: This will delete existing data!" -ForegroundColor Red
        $confirmation = Read-Host "Continue? (y/N)"
        if ($confirmation -ne 'y') {
            Write-Host "Cancelled." -ForegroundColor Yellow
            exit 0
        }
        
        # Execute clear SQL script if exists
        $clearScript = Join-Path $PSScriptRoot "seed\clear-data.sql"
        if (Test-Path $clearScript) {
            Write-Host "Executing clear script..." -ForegroundColor Yellow
            # This would require additional tooling or a seed endpoint
            Write-Host "Clear script found at: $clearScript" -ForegroundColor Yellow
        }
    }

    Write-Host "Seeding database..." -ForegroundColor Yellow
    
    # Create sample seed data inline for demonstration
    $seedData = @{
        products = @(
            @{
                id = [guid]::NewGuid().ToString()
                name = "Sample Product 1"
                description = "This is a sample product for testing"
                price = @{
                    amount = 29.99
                    currency = "USD"
                }
            },
            @{
                id = [guid]::NewGuid().ToString()
                name = "Sample Product 2"
                description = "Another sample product"
                price = @{
                    amount = 49.99
                    currency = "USD"
                }
            },
            @{
                id = [guid]::NewGuid().ToString()
                name = "Sample Product 3"
                description = "Premium sample product"
                price = @{
                    amount = 99.99
                    currency = "USD"
                }
            }
        )
    }

    # Save seed data to temporary file
    $tempSeedFile = Join-Path $env:TEMP "netemplate-seed-$(Get-Date -Format 'yyyyMMddHHmmss').json"
    $seedData | ConvertTo-Json -Depth 10 | Set-Content $tempSeedFile

    Write-Host ""
    Write-Host "Seed data prepared:" -ForegroundColor Yellow
    Write-Host "- Products: $($seedData.products.Count)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Seed data file: $tempSeedFile" -ForegroundColor DarkGray
    Write-Host ""

    # In a real implementation, you would:
    # 1. Create a seeder endpoint or CLI tool in the API
    # 2. Call it here with the seed data
    # 3. Or use EF Core's HasData() in DbContext
    
    Write-Host "NOTE: To fully implement seeding, you need to:" -ForegroundColor Yellow
    Write-Host "  1. Add a seeder service in the API project" -ForegroundColor Yellow
    Write-Host "  2. Call it via a dedicated endpoint or CLI command" -ForegroundColor Yellow
    Write-Host "  3. Or configure seed data in ApplicationDbContext.OnModelCreating" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Sample seed data has been prepared in: $tempSeedFile" -ForegroundColor Yellow
    Write-Host ""

    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Seed preparation completed!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Seeding failed: $_" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
