# GitHub CI/CD Pipeline Documentation

This document describes the automated CI/CD pipelines for the ChatApp project.

## Overview

The project uses GitHub Actions for continuous integration and deployment:

- **CI Pipeline** (`ci.yml`) - Runs on every push and pull request
- **Publish Pipeline** (`publish.yml`) - Runs on main branch pushes and version tags

## CI Pipeline (`ci.yml`)

### Triggers
- Push to `main` or `develop` branches
- Pull requests targeting `main` or `develop` branches

### Jobs

#### 1. **Build Job** (Multi-Platform)
Builds and tests the solution on three platforms:
- **Ubuntu** (Linux)
- **Windows** (for WPF client support)
- **macOS**

**Steps:**
1. Checkout code
2. Setup .NET 10 SDK
3. Restore NuGet dependencies
4. Build solution in Release configuration
5. Run unit tests

**Benefits:**
- Ensures code builds on different platforms
- WPF client builds only on Windows
- Early detection of platform-specific issues

#### 2. **Code Quality Job** (Ubuntu)
Performs static analysis and code quality checks.

**Steps:**
1. Checkout code
2. Setup .NET 10 SDK
3. Restore dependencies
4. Build with warnings-as-errors enabled (`/p:TreatWarningsAsErrors=true`)

**Note:** This job continues on error to allow builds to complete while flagging code quality issues.

#### 3. **Security Scan Job** (Ubuntu)
Scans for vulnerabilities in dependencies and configuration.

**Steps:**
1. Checkout code
2. Run Trivy vulnerability scanner
3. Upload results to GitHub Security tab

**Features:**
- Detects known CVEs in dependencies
- Filesystem scanning
- SARIF format for GitHub integration

## Publish Pipeline (`publish.yml`)

### Triggers
- Push to `main` branch
- Version tags matching `v*` (e.g., `v1.0.0`)
- Manual trigger via workflow_dispatch

### Jobs

#### 1. **Publish Server Job** (Ubuntu)
Builds and publishes the ASP.NET Core server.

**Output:**
- Self-contained server build
- Published to artifact `server-build`
- 30-day retention

**Command:**
```bash
dotnet publish src/ChatApp.Server/ChatApp.Server.csproj -c Release -o ./publish/server
```

#### 2. **Publish Client Job** (Windows)
Builds and publishes the WPF desktop client.

**Output:**
- Windows x64 self-contained executable
- Published to artifact `client-build-windows`
- 30-day retention

**Command:**
```bash
dotnet publish src/ChatApp.Client/ChatApp.Client.csproj -c Release -o ./publish/client-win-x64 \
  -f net10.0-windows --self-contained false --runtime win-x64
```

**Note:** WPF client requires Windows runner and can only build on Windows.

#### 3. **Create Release Job** (Ubuntu)
Creates a GitHub Release when version tags are pushed.

**Triggers when:**
- Git tag matching `v*` is pushed (e.g., `git tag v1.0.0 && git push --tags`)

**Output:**
- GitHub Release with both server and client builds attached
- Draft: No (automatically published)
- Accessible in Releases section

**Dependencies:**
- Requires successful completion of both publish jobs

## Usage Guide

### Running Workflows Locally

To test workflows locally before pushing:

```bash
# Install act (https://github.com/nektos/act)
brew install act

# Run CI workflow
act push -W .github/workflows/ci.yml

# Run publish workflow (requires secrets setup)
act workflow_dispatch -W .github/workflows/publish.yml
```

### Triggering Publish Workflow

**Option 1: Automatic on main branch push**
```bash
git checkout main
git push origin main
# Workflow runs automatically
```

**Option 2: Manual trigger via GitHub UI**
1. Go to Actions tab
2. Select "Publish" workflow
3. Click "Run workflow"
4. Select branch and run

**Option 3: Create release via git tag**
```bash
git tag v1.0.0
git push origin v1.0.0
# Workflow runs and creates GitHub Release with artifacts
```

### Viewing Results

1. **Go to Actions tab** on GitHub repository
2. Click on the workflow run
3. View job logs and artifacts
4. Download artifacts if needed

### Accessing Build Artifacts

After a successful publish workflow:

1. Go to **Actions** → **Publish** workflow run
2. Scroll to "Artifacts" section
3. Download desired build:
   - `server-build` - ASP.NET Core server
   - `client-build-windows` - WPF client executable

For version releases:
1. Go to **Releases** section
2. Download assets attached to the release

## Environment Requirements

The CI pipeline requires:
- **.NET 10 SDK** - Automatically installed by `setup-dotnet` action
- **Git** - For source control
- **NuGet** - Automatically used by dotnet CLI

**No special GitHub secrets needed** for the current pipelines.

## Customization

### Adding Database Tests

If you need to run database integration tests:

```yaml
- name: Start MySQL Service
  run: |
    sudo systemctl start mysql
    sudo mysql -e "CREATE DATABASE chatapp; CREATE USER 'chatapp'@'localhost' IDENTIFIED BY 'chatapp_dev'; GRANT ALL PRIVILEGES ON chatapp.* TO 'chatapp'@'localhost';"
```

### Adding Code Coverage

```yaml
- name: Install Coverage Tools
  run: dotnet tool install --global coverlet.console

- name: Generate Coverage
  run: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Adding Deploy Step

Add deployment after successful build:

```yaml
- name: Deploy to Azure
  run: |
    # Your deployment commands here
```

## Best Practices

1. **Keep workflows DRY** - Use reusable workflows for common patterns
2. **Cache dependencies** - Add caching to speed up builds
3. **Fail fast** - Stop on first error rather than continuing
4. **Document changes** - Update this file when modifying workflows
5. **Test locally** - Use `act` to test workflows before pushing
6. **Secure secrets** - Store credentials in GitHub Secrets, never in code

## Troubleshooting

### Build fails on Linux/macOS but works on Windows
- **Cause:** WPF client only builds on Windows
- **Solution:** Linux/macOS builds skip client, focus on server

### Tests timeout
- **Cause:** Database operations slow or no database available
- **Solution:** Implement database mocking or container setup

### Large artifact storage
- **Cause:** 30-day retention of large builds
- **Solution:** Adjust retention days or implement cleanup workflows

### GitHub Releases not created
- **Cause:** Missing version tag or publish job failure
- **Solution:** Check publish job logs and ensure tag format is `v*`

## Future Enhancements

Consider implementing:
- [ ] Code coverage reports
- [ ] SonarQube integration for code quality
- [ ] Docker image building for server
- [ ] Automated deployment to staging environment
- [ ] Performance benchmarking
- [ ] E2E testing with Playwright
- [ ] SBOM (Software Bill of Materials) generation

