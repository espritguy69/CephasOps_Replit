# CephasOps Version Display Script
# Usage: .\scripts\get-version.ps1

$ErrorActionPreference = "Stop"

Write-Host "📊 CephasOps Version Information" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Check if GitVersion is installed
$gitVersionInstalled = Get-Command "dotnet-gitversion" -ErrorAction SilentlyContinue
if (-not $gitVersionInstalled) {
    Write-Host "⚠️  GitVersion not found. Installing..." -ForegroundColor Yellow
    dotnet tool install -g GitVersion.Tool
    Write-Host "✓ GitVersion installed" -ForegroundColor Green
}

# Get version information
Write-Host "`n🔍 Calculating version from Git history..." -ForegroundColor Yellow
$versionInfo = dotnet-gitversion | ConvertFrom-Json

Write-Host "`n📦 Version Information:" -ForegroundColor Green
Write-Host "  Full SemVer:     $($versionInfo.FullSemVer)" -ForegroundColor White
Write-Host "  NuGet Version:   $($versionInfo.NuGetVersion)" -ForegroundColor White
Write-Host "  Assembly SemVer: $($versionInfo.AssemblySemVer)" -ForegroundColor White
Write-Host "  Major.Minor.Patch: $($versionInfo.MajorMinorPatch)" -ForegroundColor White

Write-Host "`n🏷️  Version Details:" -ForegroundColor Green
Write-Host "  Major:           $($versionInfo.Major)" -ForegroundColor White
Write-Host "  Minor:           $($versionInfo.Minor)" -ForegroundColor White
Write-Host "  Patch:           $($versionInfo.Patch)" -ForegroundColor White
Write-Host "  PreReleaseTag:   $($versionInfo.PreReleaseTag)" -ForegroundColor White
Write-Host "  BuildMetadata:   $($versionInfo.BuildMetadata)" -ForegroundColor White

Write-Host "`n🌿 Git Information:" -ForegroundColor Green
Write-Host "  Branch:          $($versionInfo.BranchName)" -ForegroundColor White
Write-Host "  Sha:             $($versionInfo.Sha.Substring(0, 8))" -ForegroundColor White
Write-Host "  Commits Since Version Source: $($versionInfo.CommitsSinceVersionSource)" -ForegroundColor White

Write-Host "`n📅 Version Source:" -ForegroundColor Green
Write-Host "  Version Source Sha: $($versionInfo.VersionSourceSha.Substring(0, 8))" -ForegroundColor White

# Check for .NET project version
$buildPropsPath = "backend\Directory.Build.props"
if (Test-Path $buildPropsPath) {
    Write-Host "`n⚙️  .NET Project Configuration:" -ForegroundColor Green
    $buildProps = [xml](Get-Content $buildPropsPath)
    $versionPrefix = $buildProps.Project.PropertyGroup.VersionPrefix
    Write-Host "  VersionPrefix:   $versionPrefix" -ForegroundColor White
}

Write-Host "`n✨ Done!" -ForegroundColor Cyan

