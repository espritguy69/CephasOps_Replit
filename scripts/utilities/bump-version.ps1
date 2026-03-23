# CephasOps Version Bumping Script
# Usage: .\scripts\bump-version.ps1 -Type [major|minor|patch] -Message "Release message"

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("major", "minor", "patch")]
    [string]$Type,
    
    [Parameter(Mandatory=$false)]
    [string]$Message = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$CreateTag,
    
    [Parameter(Mandatory=$false)]
    [switch]$Push
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 CephasOps Version Bumper" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Check if GitVersion is installed
$gitVersionInstalled = Get-Command "dotnet-gitversion" -ErrorAction SilentlyContinue
if (-not $gitVersionInstalled) {
    Write-Host "⚠️  GitVersion not found. Installing..." -ForegroundColor Yellow
    dotnet tool install -g GitVersion.Tool
}

# Get current version
Write-Host "`n📊 Current Version Information:" -ForegroundColor Green
$currentVersion = dotnet-gitversion | ConvertFrom-Json
Write-Host "  Version: $($currentVersion.FullSemVer)" -ForegroundColor White
Write-Host "  NuGet: $($currentVersion.NuGetVersion)" -ForegroundColor White
Write-Host "  Assembly: $($currentVersion.AssemblySemVer)" -ForegroundColor White

# Determine new version based on type
$newVersion = switch ($Type) {
    "major" { 
        $parts = $currentVersion.MajorMinorPatch.Split('.')
        "$([int]$parts[0] + 1).0.0"
    }
    "minor" { 
        $parts = $currentVersion.MajorMinorPatch.Split('.')
        "$($parts[0]).$([int]$parts[1] + 1).0"
    }
    "patch" { 
        $parts = $currentVersion.MajorMinorPatch.Split('.')
        "$($parts[0]).$($parts[1]).$([int]$parts[2] + 1)"
    }
}

Write-Host "`n🎯 Bumping $Type version..." -ForegroundColor Yellow
Write-Host "  From: $($currentVersion.MajorMinorPatch)" -ForegroundColor Gray
Write-Host "  To:   $newVersion" -ForegroundColor Green

# Update Directory.Build.props
$buildPropsPath = "backend\Directory.Build.props"
if (Test-Path $buildPropsPath) {
    Write-Host "`n📝 Updating Directory.Build.props..." -ForegroundColor Yellow
    $content = Get-Content $buildPropsPath -Raw
    $content = $content -replace '(<VersionPrefix>)(.*?)(</VersionPrefix>)', "`$1$newVersion`$3"
    Set-Content -Path $buildPropsPath -Value $content -NoNewline
    Write-Host "  ✓ Updated VersionPrefix to $newVersion" -ForegroundColor Green
}

# Create commit with semantic version message
$commitMessage = if ($Message) {
    "+semver: $Type`n`n$Message"
} else {
    "+semver: $Type`n`nBump $Type version to $newVersion"
}

Write-Host "`n💾 Committing changes..." -ForegroundColor Yellow
git add $buildPropsPath
git commit -m $commitMessage
Write-Host "  ✓ Committed with message: $commitMessage" -ForegroundColor Green

# Create tag if requested
if ($CreateTag) {
    $tagName = "v$newVersion"
    Write-Host "`n🏷️  Creating tag: $tagName" -ForegroundColor Yellow
    git tag -a $tagName -m "Release $tagName`n`n$Message"
    Write-Host "  ✓ Tag created: $tagName" -ForegroundColor Green
}

# Push if requested
if ($Push) {
    Write-Host "`n📤 Pushing to remote..." -ForegroundColor Yellow
    git push origin HEAD
    if ($CreateTag) {
        git push origin $tagName
    }
    Write-Host "  ✓ Pushed to remote" -ForegroundColor Green
}

# Show new version
Write-Host "`n✨ Version bump complete!" -ForegroundColor Cyan
Write-Host "  New Version: $newVersion" -ForegroundColor Green
Write-Host "`n📋 Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review the changes" -ForegroundColor White
Write-Host "  2. Push to remote: git push origin HEAD" -ForegroundColor White
if (-not $CreateTag) {
    Write-Host "  3. Create release tag: git tag -a v$newVersion -m 'Release v$newVersion'" -ForegroundColor White
}

