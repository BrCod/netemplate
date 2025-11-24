# Build Scripts

This directory contains PowerShell scripts for building, testing, running, and managing the Netemplate application.

## Prerequisites

- .NET 8 SDK
- PowerShell 5.1 or PowerShell Core 7+
- EF Core CLI tools (installed automatically by scripts)

## Scripts

### build.ps1

Builds the entire solution.

```powershell
# Build with Release configuration (default)
.\build.ps1

# Build with Debug configuration
.\build.ps1 -Configuration Debug

# Clean and build
.\build.ps1 -Clean
```

**Parameters:**
- `-Configuration`: Build configuration (`Debug` or `Release`). Default: `Release`
- `-Clean`: Clean before building

### test.ps1

Runs all tests in the solution.

```powershell
# Run all tests
.\test.ps1

# Run tests with code coverage
.\test.ps1 -Coverage

# Run specific tests using filter
.\test.ps1 -Filter "FullyQualifiedName~Product"

# Run tests without building
.\test.ps1 -NoBuild
```

**Parameters:**
- `-Configuration`: Build configuration (`Debug` or `Release`). Default: `Debug`
- `-Filter`: Test filter expression
- `-Coverage`: Generate code coverage report
- `-NoBuild`: Skip building before running tests

### run.ps1

Runs the API application.

```powershell
# Run with Development environment (default)
.\run.ps1

# Run with Production environment
.\run.ps1 -Environment Production

# Run with hot reload (watch mode)
.\run.ps1 -Watch

# Run with specific launch profile
.\run.ps1 -LaunchProfile https

# Run without building
.\run.ps1 -NoBuild
```

**Parameters:**
- `-Environment`: ASP.NET Core environment (`Development`, `Staging`, `Production`). Default: `Development`
- `-Configuration`: Build configuration (`Debug` or `Release`). Default: `Debug`
- `-NoBuild`: Skip building before running
- `-Watch`: Run with hot reload
- `-LaunchProfile`: Launch profile to use

### migrate.ps1

Manages Entity Framework Core database migrations.

```powershell
# Add a new migration
.\migrate.ps1 -Action Add -Name InitialCreate

# Update database to latest migration
.\migrate.ps1 -Action Update

# Update database to specific migration
.\migrate.ps1 -Action Update -Target PreviousMigration

# Rollback all migrations
.\migrate.ps1 -Action Update -Target 0

# List all migrations
.\migrate.ps1 -Action List

# Generate SQL script
.\migrate.ps1 -Action Script -Output migration.sql

# Remove last migration
.\migrate.ps1 -Action Remove

# Drop database
.\migrate.ps1 -Action Drop
```

**Parameters:**
- `-Action`: Migration action (`Add`, `Remove`, `Update`, `List`, `Script`, `Drop`)
- `-Name`: Migration name (required for `Add` action)
- `-Target`: Target migration for `Update` or `Script` actions
- `-Environment`: ASP.NET Core environment. Default: `Development`
- `-Output`: Output file path for `Script` action

### seed.ps1

Seeds the database with test data.

```powershell
# Seed database
.\seed.ps1

# Clear existing data and seed
.\seed.ps1 -Clear

# Seed staging environment
.\seed.ps1 -Environment Staging
```

**Parameters:**
- `-Environment`: ASP.NET Core environment. Default: `Development`
- `-Clear`: Clear existing data before seeding

## Common Workflows

### Initial Setup

```powershell
# 1. Build the solution
.\build.ps1 -Clean

# 2. Create initial migration
.\migrate.ps1 -Action Add -Name InitialCreate

# 3. Update database
.\migrate.ps1 -Action Update

# 4. Seed test data
.\seed.ps1

# 5. Run the application
.\run.ps1
```

### Development Workflow

```powershell
# Run with hot reload
.\run.ps1 -Watch

# In another terminal, run tests on changes
.\test.ps1 -Watch
```

### Testing Workflow

```powershell
# Run all tests
.\test.ps1

# Run specific test category
.\test.ps1 -Filter "Category=Integration"

# Run with coverage
.\test.ps1 -Coverage
```

### Database Management

```powershell
# Add migration
.\migrate.ps1 -Action Add -Name AddProductTable

# Preview migration as SQL
.\migrate.ps1 -Action Script -Output preview.sql

# Apply migration
.\migrate.ps1 -Action Update

# Rollback one migration
.\migrate.ps1 -Action Update -Target PreviousMigrationName

# Rollback all migrations
.\migrate.ps1 -Action Update -Target 0
```

## CI/CD Integration

These scripts are designed to be used in CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Build
  run: ./build/build.ps1 -Configuration Release

- name: Test
  run: ./build/test.ps1 -Coverage

- name: Apply Migrations
  run: ./build/migrate.ps1 -Action Update -Environment Staging
```

## Troubleshooting

### EF Core Tools Not Found

The `migrate.ps1` script will automatically install EF Core CLI tools if not found. To manually install:

```powershell
dotnet tool install --global dotnet-ef
```

### Connection String Issues

Ensure your `appsettings.json` or environment variables contain valid connection strings for your target environment.

### Permission Errors

Run PowerShell as Administrator if you encounter permission errors, or adjust your execution policy:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```
