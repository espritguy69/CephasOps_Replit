# 🚀 Versioning Quick Start Guide

This guide helps you get started with CephasOps automatic versioning system.

---

## ✅ What's Been Set Up

1. **GitVersion Configuration** (`GitVersion.yml`)
   - Automatic semantic versioning based on Git history
   - Branch-based version tags (alpha, beta, etc.)

2. **.NET Project Versioning** (`backend/Directory.Build.props`)
   - Centralized version management for all projects
   - Automatic version injection from GitVersion

3. **CI/CD Versioning** (`.github/workflows/versioning.yml`)
   - Automatic version calculation on every build
   - Automatic release creation on main branch

4. **Version Scripts**
   - `scripts/bump-version.ps1` - Manual version bumping
   - `scripts/get-version.ps1` - Display current version

5. **Documentation** (`docs/08_infrastructure/VERSIONING_STRATEGY.md`)
   - Complete versioning strategy and best practices

---

## 🎯 Quick Commands

### Check Current Version
```powershell
.\scripts\get-version.ps1
```

### Bump Version Manually
```powershell
# Patch version (bug fixes)
.\scripts\bump-version.ps1 -Type patch -Message "Fix critical bug" -CreateTag -Push

# Minor version (new features)
.\scripts\bump-version.ps1 -Type minor -Message "Add new feature" -CreateTag -Push

# Major version (breaking changes)
.\scripts\bump-version.ps1 -Type major -Message "Breaking API changes" -CreateTag -Push
```

### Install GitVersion (if needed)
```powershell
dotnet tool install -g GitVersion.Tool
```

---

## 📋 How It Works

### Automatic Versioning
- **Development branch:** Versions like `1.1.0-alpha.5` (auto-increments)
- **Main branch:** Versions like `1.0.0` (from Git tags)
- **Feature branches:** Inherit from base branch

### Version Calculation
GitVersion automatically calculates versions based on:
- Git tags (e.g., `v1.0.0`)
- Branch names (development, main, feature/*, etc.)
- Commit messages with `+semver:` hints
- Merge history

### CI/CD Integration
- Every push to GitHub triggers version calculation
- Main branch pushes create automatic releases
- Version is embedded in all build artifacts

---

## 🔧 Setup Steps

1. **Install GitVersion** (one-time)
   ```powershell
   dotnet tool install -g GitVersion.Tool
   ```

2. **Verify Installation**
   ```powershell
   dotnet-gitversion
   ```

3. **Test Version Calculation**
   ```powershell
   .\scripts\get-version.ps1
   ```

4. **Build with Version**
   ```powershell
   cd backend
   dotnet build CephasOps.sln
   ```

---

## 📝 Commit Message Hints

You can control version increments with commit messages:

```
+semver: major    # Increment major version
+semver: minor    # Increment minor version  
+semver: patch    # Increment patch version
+semver: none     # No version change
```

Example:
```
git commit -m "Add new API endpoint +semver: minor"
```

---

## 🏷️ Creating Releases

### Automatic (via CI/CD)
- Push to `main` branch → Automatic release created
- Tag and GitHub release created automatically

### Manual
```powershell
# Create and push tag
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
```

---

## 📚 More Information

- **Full Documentation:** `docs/08_infrastructure/VERSIONING_STRATEGY.md`
- **GitVersion Docs:** https://gitversion.net/docs/
- **Semantic Versioning:** https://semver.org/

---

## 🆘 Troubleshooting

### Version not updating?
1. Check GitVersion is installed: `dotnet tool list -g`
2. Verify `GitVersion.yml` exists in root
3. Run `.\scripts\get-version.ps1` to see calculated version

### Wrong version calculated?
1. Check branch naming matches conventions
2. Review Git tags: `git tag -l`
3. Check GitVersion output: `dotnet-gitversion`

---

**Ready to go!** Your versioning system is now fully configured. 🎉

