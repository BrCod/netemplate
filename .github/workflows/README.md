# GitHub Actions Workflows

This directory contains CI/CD workflows for the Netemplate project.

## Workflows

### ci.yml - Continuous Integration

**Triggers:**
- Push to `main`, `develop`, or `feat-*` branches
- Pull requests to `main` or `develop`
- Manual workflow dispatch

**Jobs:**

1. **Build**
   - Restores dependencies
   - Builds the solution in Release configuration
   - Uploads build artifacts

2. **Test**
   - Runs unit tests (excluding integration tests)
   - Runs integration tests against Postgres, Redis, and RabbitMQ services
   - Uploads test results

3. **Code Coverage**
   - Runs tests with coverage collection
   - Generates HTML coverage reports
   - Comments coverage percentage on PRs
   - Uploads coverage artifacts

4. **Lint**
   - Checks code formatting with `dotnet format`
   - Runs code analyzers with warnings treated as errors

5. **Security Scan**
   - Checks for vulnerable NuGet packages
   - Checks for deprecated packages
   - Uploads scan results

6. **Publish**
   - Publishes API artifacts (only on `main` or `develop`)
   - Creates version file with build metadata
   - Uploads publishable artifacts for 90 days

7. **Notify**
   - Aggregates status of all jobs
   - Reports overall pipeline success/failure

### pr.yml - Pull Request Validation

**Triggers:**
- Pull request opened, synchronized, reopened, or marked ready for review

**Jobs:**

1. **Validate**
   - Validates PR title follows Conventional Commits format
   - Ensures PR has a description
   - Checks for linked issues

2. **Build and Test**
   - Builds the solution
   - Runs all tests against service dependencies
   - Uploads test results

3. **Code Quality**
   - Checks code formatting
   - Runs static code analyzers

4. **Changes Check**
   - Detects changed files
   - Warns if source code changed without test changes

5. **Approve Ready**
   - Adds success comment when all checks pass

### release.yml - Release Workflow

**Triggers:**
- Push of version tags (e.g., `v1.0.0`)
- Manual workflow dispatch with version input

**Jobs:**

1. **Build and Publish**
   - Builds with version information
   - Runs tests
   - Publishes release artifacts
   - Creates release archive (tar.gz)
   - Creates GitHub Release with changelog

2. **Docker Build and Push**
   - Builds Docker image
   - Pushes to GitHub Container Registry
   - Tags with semantic versions
   - Generates Software Bill of Materials (SBOM)

### dependabot-auto-merge.yml - Dependabot Automation

**Triggers:**
- Pull requests from Dependabot

**Jobs:**

1. **Dependabot Auto-Merge**
   - Auto-approves patch and minor updates
   - Enables auto-merge for patch updates
   - Comments on major updates requiring manual review

## Usage

### Running Workflows Locally

You can test the build and test steps locally using the PowerShell scripts:

```powershell
# Build
.\build\build.ps1 -Configuration Release

# Test
.\build\test.ps1 -Coverage

# Lint
dotnet format --verify-no-changes
```

### Creating a Release

To create a new release:

```bash
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0

# Or use the GitHub UI to create a release
```

The release workflow will automatically:
- Build and test the code
- Publish artifacts
- Create a GitHub Release
- Build and push Docker image

### Manual Workflow Dispatch

All workflows support manual triggering via GitHub UI:

1. Go to **Actions** tab
2. Select the workflow
3. Click **Run workflow**
4. Select branch (and provide inputs if required)

## Environment Variables

The workflows use the following environment variables:

- `DOTNET_VERSION`: .NET SDK version (currently 8.0.x)
- `REGISTRY`: Container registry (ghcr.io)
- `ARTIFACT_NAME`: Name of published artifacts

## Secrets Required

The workflows require the following secrets:

- `GITHUB_TOKEN`: Automatically provided by GitHub Actions
- No additional secrets required for basic functionality

For production deployments, you may need:
- `DOCKER_USERNAME` / `DOCKER_PASSWORD`: Docker Hub credentials
- `AZURE_CREDENTIALS`: Azure deployment credentials
- `NUGET_API_KEY`: NuGet.org publishing key

## Service Dependencies

Integration tests require:
- **PostgreSQL 16** (port 5432)
- **Redis 7** (port 6379)
- **RabbitMQ 3** (ports 5672, 15672)

These are automatically provisioned as service containers in the workflows.

## Artifacts

The workflows produce the following artifacts:

1. **build-output** (1 day retention)
   - Compiled binaries from Release build

2. **test-results** (30 days retention)
   - Test result files (TRX format)

3. **coverage-report** (30 days retention)
   - HTML coverage reports
   - Cobertura XML for tool integration

4. **security-scan-results** (30 days retention)
   - Vulnerable package scan results
   - Deprecated package scan results

5. **netemplate-api-{run_number}** (90 days retention)
   - Published API ready for deployment

6. **sbom** (90 days retention)
   - Software Bill of Materials for Docker image

## Status Badges

Add these badges to your README.md:

```markdown
[![CI](https://github.com/BrCod/netemplate/actions/workflows/ci.yml/badge.svg)](https://github.com/BrCod/netemplate/actions/workflows/ci.yml)
[![Release](https://github.com/BrCod/netemplate/actions/workflows/release.yml/badge.svg)](https://github.com/BrCod/netemplate/actions/workflows/release.yml)
```

## Troubleshooting

### Tests Failing in CI but Passing Locally

1. Check service health status in workflow logs
2. Verify connection strings match service configurations
3. Ensure tests don't depend on local environment state

### Code Formatting Failures

Run locally to fix:
```powershell
dotnet format Netemplate.sln
```

### Security Vulnerabilities Detected

Review the security scan artifact and update affected packages:
```powershell
dotnet list package --vulnerable
dotnet add package <PackageName> --version <SafeVersion>
```

### Docker Build Failures

1. Ensure Dockerfile exists at `build/Dockerfile`
2. Check that all project references are valid
3. Verify base image is accessible

## Best Practices

1. **Always run tests locally** before pushing
2. **Keep PRs focused** - one feature/fix per PR
3. **Follow Conventional Commits** for PR titles
4. **Link issues** in PR descriptions
5. **Wait for CI** to pass before requesting review
6. **Review Dependabot PRs** before auto-merge
7. **Tag releases** using semantic versioning

## Maintenance

### Updating Workflow Dependencies

The workflows use pinned versions for actions. Update them via Dependabot or manually:

```yaml
- uses: actions/checkout@v4  # Check for v5, v6, etc.
```

### Modifying Service Versions

To update service container versions:

```yaml
services:
  postgres:
    image: postgres:17-alpine  # Update version here
```

## Performance Optimization

- **Caching**: Dependencies are cached by `setup-dotnet` action
- **Parallel Jobs**: Independent jobs run in parallel
- **Build Artifacts**: Shared between jobs to avoid rebuilding
- **Service Health Checks**: Ensure services are ready before tests
