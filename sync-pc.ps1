# sync-pc.ps1 - Complete PC sync automation for CephasOps
# Version: 1.0
# Purpose: Sync code, dependencies, and database between multiple development PCs

param(
    [switch]$SkipFrontendSI,
    [switch]$SkipDatabaseMigration,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "`n$Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Yellow
}

# ASCII Art Header
Write-Host @"

 ██████╗███████╗██████╗ ██╗  ██╗ █████╗ ███████╗ ██████╗ ██████╗ ███████╗
██╔════╝██╔════╝██╔══██╗██║  ██║██╔══██╗██╔════╝██╔═══██╗██╔══██╗██╔════╝
██║     █████╗  ██████╔╝███████║███████║███████╗██║   ██║██████╔╝███████╗
██║     ██╔══╝  ██╔═══╝ ██╔══██║██╔══██║╚════██║██║   ██║██╔═══╝ ╚════██║
╚██████╗███████╗██║     ██║  ██║██║  ██║███████║╚██████╔╝██║     ███████║
 ╚═════╝╚══════╝╚═╝     ╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝ ╚═════╝ ╚═╝     ╚══════╝
                                                                          
                    🔄 Multi-PC Sync Script
"@ -ForegroundColor Cyan

Write-Host "`nStarting synchronization process...`n" -ForegroundColor White

# Store original location
$originalLocation = Get-Location

try {
    # ============================================================================
    # STEP 1: GIT PULL
    # ============================================================================
    Write-Step "📥 STEP 1: Pulling latest code from Git..."
    
    # Check current branch
    $currentBranch = git branch --show-current
    Write-Info "Current branch: $currentBranch"
    
    # Check for uncommitted changes
    $gitStatus = git status --porcelain
    if ($gitStatus) {
        Write-Info "You have uncommitted changes:"
        git status --short
        Write-Host "`nOptions:" -ForegroundColor Yellow
        Write-Host "  1. Stash changes (git stash)" -ForegroundColor White
        Write-Host "  2. Commit changes (git add . && git commit)" -ForegroundColor White
        Write-Host "  3. Abort sync" -ForegroundColor White
        
        $choice = Read-Host "`nEnter choice (1-3)"
        switch ($choice) {
            "1" {
                Write-Info "Stashing changes..."
                git stash
                $stashed = $true
            }
            "2" {
                git add .
                $message = Read-Host "Enter commit message"
                git commit -m $message
            }
            "3" {
                Write-Error-Custom "Sync aborted by user"
                exit 0
            }
            default {
                Write-Error-Custom "Invalid choice. Aborting."
                exit 1
            }
        }
    }
    
    # Pull from remote
    git pull origin $currentBranch
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Git pull failed! Please resolve conflicts manually."
        exit 1
    }
    
    # Restore stashed changes if we stashed
    if ($stashed) {
        Write-Info "Restoring stashed changes..."
        git stash pop
    }
    
    Write-Success "Code synchronized"

    # ============================================================================
    # STEP 2: BACKEND DEPENDENCIES
    # ============================================================================
    Write-Step "📦 STEP 2: Updating backend dependencies..."
    
    Set-Location backend
    
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Backend restore failed!"
        exit 1
    }
    
    Write-Success "Backend packages updated"

    # ============================================================================
    # STEP 3: FRONTEND DEPENDENCIES
    # ============================================================================
    Write-Step "📦 STEP 3: Updating frontend dependencies..."
    
    Set-Location ../frontend
    
    if ($Verbose) {
        npm install
    } else {
        npm install --silent
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Frontend npm install failed!"
        exit 1
    }
    
    Write-Success "Frontend packages updated"

    # ============================================================================
    # STEP 4: FRONTEND-SI DEPENDENCIES (Optional)
    # ============================================================================
    if (-not $SkipFrontendSI) {
        Write-Step "📦 STEP 4: Updating frontend-SI dependencies..."
        
        Set-Location ../frontend-si
        
        if ($Verbose) {
            npm install
        } else {
            npm install --silent
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-Info "Frontend-SI npm install failed (this is optional, continuing...)"
        } else {
            Write-Success "Frontend-SI packages updated"
        }
    } else {
        Write-Info "Skipping frontend-SI (--SkipFrontendSI flag set)"
    }

    # ============================================================================
    # STEP 5: DATABASE MIGRATIONS
    # ============================================================================
    if (-not $SkipDatabaseMigration) {
        Write-Step "🗄️  STEP 5: Applying database migrations..."
        
        Set-Location $originalLocation
        Set-Location backend
        
        # Check if there are pending migrations
        Write-Info "Checking for pending migrations..."
        
        dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
        if ($LASTEXITCODE -ne 0) {
            Write-Error-Custom "Database migration failed!"
            Write-Info "Common fixes:"
            Write-Host "  - Verify PostgreSQL is running: Get-Service postgresql*" -ForegroundColor White
            Write-Host "  - Check connection string: dotnet user-secrets list --project src/CephasOps.Api" -ForegroundColor White
            Write-Host "  - Verify database exists and is accessible" -ForegroundColor White
            exit 1
        }
        
        Write-Success "Database migrations applied"
    } else {
        Write-Info "Skipping database migration (--SkipDatabaseMigration flag set)"
    }

    # ============================================================================
    # STEP 6: VERIFICATION
    # ============================================================================
    Write-Step "✨ STEP 6: Verification..."
    
    Set-Location $originalLocation
    
    Write-Host "`n" -NoNewline
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                                                           ║" -ForegroundColor Green
    Write-Host "║          ✅ SYNC COMPLETED SUCCESSFULLY! ✅                ║" -ForegroundColor Green
    Write-Host "║                                                           ║" -ForegroundColor Green
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
    
    Write-Host "`n📋 Summary:" -ForegroundColor Cyan
    Write-Host "  ✓ Code synchronized from Git ($currentBranch)" -ForegroundColor White
    Write-Host "  ✓ Backend dependencies restored" -ForegroundColor White
    Write-Host "  ✓ Frontend dependencies installed" -ForegroundColor White
    if (-not $SkipFrontendSI) {
        Write-Host "  ✓ Frontend-SI dependencies installed" -ForegroundColor White
    }
    if (-not $SkipDatabaseMigration) {
        Write-Host "  ✓ Database migrations applied" -ForegroundColor White
    }
    
    Write-Host "`n🚀 Next Steps:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Start Backend (with hot reload):" -ForegroundColor Yellow
    Write-Host "    cd backend" -ForegroundColor White
    Write-Host "    dotnet watch run --project src/CephasOps.Api" -ForegroundColor White
    Write-Host ""
    Write-Host "  Start Frontend (in a new terminal):" -ForegroundColor Yellow
    Write-Host "    cd frontend" -ForegroundColor White
    Write-Host "    npm run dev" -ForegroundColor White
    Write-Host ""
    Write-Host "  Or use the automated startup script:" -ForegroundColor Yellow
    Write-Host "    .\backend\start.ps1" -ForegroundColor White
    Write-Host ""
    
    Write-Host "  Open in browser: http://localhost:5173" -ForegroundColor Cyan
    Write-Host ""

} catch {
    Write-Host "`n" -NoNewline
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║                                                           ║" -ForegroundColor Red
    Write-Host "║                   ❌ SYNC FAILED ❌                        ║" -ForegroundColor Red
    Write-Host "║                                                           ║" -ForegroundColor Red
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Red
    Write-Host ""
    Write-Error-Custom "An error occurred: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "📚 For help, see: docs/08_infrastructure/PC_SYNC_GUIDE.md" -ForegroundColor Yellow
    Write-Host ""
    Set-Location $originalLocation
    exit 1
} finally {
    # Always return to original location
    Set-Location $originalLocation
}

