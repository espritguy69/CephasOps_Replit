# quick-code-sync.ps1 - Quick code-only sync
# Use this when only source code changed (no database, no new packages)

Write-Host "📥 Quick Code Sync" -ForegroundColor Cyan
Write-Host "==================`n" -ForegroundColor Cyan

# Check current branch
$currentBranch = git branch --show-current
Write-Host "Current branch: $currentBranch`n" -ForegroundColor Yellow

# Check for uncommitted changes
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Host "⚠️  You have uncommitted changes:" -ForegroundColor Yellow
    git status --short
    Write-Host "`nStash, commit, or discard them before pulling.`n" -ForegroundColor Yellow
    exit 1
}

# Pull latest code
Write-Host "Pulling latest code..." -ForegroundColor Yellow
git pull origin $currentBranch

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Code synced successfully!" -ForegroundColor Green
    Write-Host "`nℹ️  Note: If using 'dotnet watch' or 'npm run dev', changes will auto-reload." -ForegroundColor Cyan
    Write-Host "ℹ️  If you see new migrations or package changes, run .\sync-pc.ps1 instead.`n" -ForegroundColor Cyan
} else {
    Write-Host "`n❌ Git pull failed!" -ForegroundColor Red
    Write-Host "Resolve any conflicts manually.`n" -ForegroundColor Yellow
    exit 1
}

