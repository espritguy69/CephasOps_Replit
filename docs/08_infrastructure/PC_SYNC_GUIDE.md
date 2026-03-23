# 🔄 CephasOps Multi-PC Sync Guide

> **Purpose**: Step-by-step guide to sync your development work between multiple computers

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Quick Sync Checklist](#quick-sync-checklist)
4. [Detailed Step-by-Step Instructions](#detailed-step-by-step-instructions)
5. [Common Issues & Solutions](#common-issues--solutions)
6. [Automated Sync Scripts](#automated-sync-scripts)
7. [Safety Tips](#safety-tips)

---

## Overview

When working on CephasOps from multiple computers, you need to keep three main things in sync:

| Component | What It Is | Why It Matters |
|-----------|-----------|----------------|
| **Code** | Your source files (.cs, .tsx, .ts, etc.) | Contains all your logic and features |
| **Database Schema** | The structure of your PostgreSQL database | If out of sync, your app will crash |
| **Dependencies** | NuGet packages (.NET) and npm packages (React) | Missing packages = errors |
| **Configuration** | Environment variables, secrets, settings | Wrong config = connection failures |

---

## Prerequisites

### What You Need

- ✅ Access to both PCs (physically or via Remote Desktop)
- ✅ Git installed on both machines
- ✅ PostgreSQL installed on both machines
- ✅ .NET 10 SDK installed
- ✅ Node.js 18+ installed
- ✅ Visual Studio Code or Cursor installed
- ✅ Network connectivity (to pull from Git repository)

### Access Information You Should Know

- 🔐 Git repository URL (e.g., GitHub, GitLab, Azure DevOps)
- 🔐 Database connection string for the second PC
- 🔐 Any API keys or secrets needed

---

## Quick Sync Checklist

Use this quick checklist when syncing to your second PC:

```
□ Step 1: Pull latest code from Git
□ Step 2: Install/update backend dependencies (.NET packages)
□ Step 3: Install/update frontend dependencies (npm packages)
□ Step 4: Apply database migrations
□ Step 5: Verify environment variables
□ Step 6: Test startup (backend + frontend)
```

**Estimated Time**: 5-10 minutes for a routine sync

---

## Detailed Step-by-Step Instructions

### 🔸 Step 1: Access Your Second PC

#### Option A: Physical Access
Simply sit at the computer and open your terminal/PowerShell.

#### Option B: Remote Desktop (Windows)
1. Press `Win + R`
2. Type: `mstsc`
3. Enter the computer name or IP address
4. Enter your credentials
5. Once connected, open PowerShell or Windows Terminal

---

### 🔸 Step 2: Navigate to Your Project

Open PowerShell and navigate to your CephasOps folder:

```powershell
cd C:\Projects\CephasOps
```

**Verify you're in the right place:**
```powershell
# Should show folders like: backend, frontend, frontend-si, docs
Get-ChildItem
```

---

### 🔸 Step 3: Check Current Git Status

Before pulling changes, see what state you're in:

```powershell
git status
```

**What to look for:**
- ✅ `On branch single_company` (or your working branch)
- ✅ `Your branch is behind 'origin/single_company' by X commits` (means updates are available)
- ⚠️ `Changes not staged for commit` (you have uncommitted changes)

**If you have uncommitted changes:**

```powershell
# Option 1: Save your changes temporarily
git stash

# Option 2: Commit them first
git add .
git commit -m "WIP: saving work before sync"
```

---

### 🔸 Step 4: Pull Latest Code from Git

Download all the latest changes:

```powershell
git pull origin single_company
```

**Expected output:**
```
Updating abc1234..def5678
Fast-forward
 backend/src/CephasOps.Application/Notifications/DTOs/NotificationDto.cs | 10 +++++-----
 docs/02_modules/notifications/OVERVIEW.md                                | 25 +++++++++++++++++++++++++
 2 files changed, 30 insertions(+), 5 deletions(-)
```

**If you see conflicts:**
```powershell
# Git will tell you which files have conflicts
# Open those files and resolve the conflicts manually
# Look for markers like: <<<<<<< HEAD, =======, >>>>>>> 

# After resolving:
git add .
git commit -m "Resolved merge conflicts"
```

---

### 🔸 Step 5: Update Backend Dependencies

Navigate to the backend folder:

```powershell
cd backend
```

**Restore .NET packages:**
```powershell
dotnet restore
```

**Expected output:**
```
Determining projects to restore...
Restored C:\Projects\CephasOps\backend\src\CephasOps.Domain\CephasOps.Domain.csproj
Restored C:\Projects\CephasOps\backend\src\CephasOps.Application\CephasOps.Application.csproj
...
```

**If you see errors**, check that:
- You have .NET 10 SDK installed: `dotnet --version`
- Your internet connection is working

---

### 🔸 Step 6: Update Frontend Dependencies

Navigate back to root, then to frontend:

```powershell
cd ..
cd frontend
```

**Install/update npm packages:**
```powershell
npm install
```

**Expected output:**
```
added 245 packages, changed 12 packages in 45s
```

**Repeat for the Service Installer app:**
```powershell
cd ../frontend-si
npm install
```

---

### 🔸 Step 7: Apply Database Migrations (CRITICAL!)

This is the most important step! If you skip this, your app will crash.

Navigate to backend:
```powershell
cd ../backend
```

**Check if there are pending migrations:**
```powershell
# View all migrations
dotnet ef migrations list --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
```

**Apply migrations to your database:**
```powershell
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
```

**Expected output:**
```
Build succeeded.
Applying migration '20250203_AddNotificationDepartmentId'.
Applying migration '20250203_UpdateNotificationSchema'.
Done.
```

**Alternative: Use the migration script**
```powershell
.\migrate.ps1
```

---

### 🔸 Step 8: Verify Environment Variables

Check your environment configuration:

**Backend:**
```powershell
cd backend/src/CephasOps.Api

# Check if user secrets are configured
dotnet user-secrets list
```

**Required secrets:**
- `ConnectionStrings:DefaultConnection` - PostgreSQL connection string
- `JwtSettings:SecretKey` - JWT authentication key
- `SYNCFUSION_LICENSE_KEY` - Syncfusion license

**If missing, set them:**
```powershell
# Database connection
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=cephasops;Username=postgres;Password=YOUR_PASSWORD"

# JWT key
dotnet user-secrets set "JwtSettings:SecretKey" "your-super-secret-key-at-least-32-characters-long"

# Syncfusion license
dotnet user-secrets set "SYNCFUSION_LICENSE_KEY" "YOUR_LICENSE_KEY"
```

**Frontend:**
```powershell
cd ../../../frontend

# Check if .env.local exists
Get-Content .env.local
```

**Should contain:**
```
VITE_API_BASE_URL=http://localhost:5000/api
```

If the file doesn't exist, create it:
```powershell
echo "VITE_API_BASE_URL=http://localhost:5000/api" > .env.local
```

---

### 🔸 Step 9: Test Backend Startup

Navigate to backend root:
```powershell
cd ../../backend
```

**Start the backend in watch mode:**
```powershell
dotnet watch run --project src/CephasOps.Api
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Verify it's working:**
Open a browser and go to: `http://localhost:5000/health`

You should see:
```json
{
  "status": "Healthy"
}
```

**If you see errors**, check:
- Database connection string is correct
- PostgreSQL service is running: `Get-Service postgresql*`
- Port 5000 is not already in use

---

### 🔸 Step 10: Test Frontend Startup

Open a **new PowerShell window** (keep backend running):

```powershell
cd C:\Projects\CephasOps\frontend
npm run dev
```

**Expected output:**
```
VITE v5.x.x  ready in 2000 ms

➜  Local:   http://localhost:5173/
➜  Network: use --host to expose
```

Open browser to: `http://localhost:5173`

You should see the CephasOps login page.

---

### 🔸 Step 11: Verify the Sync

**Backend verification:**
- ✅ API starts without errors
- ✅ Database migrations applied successfully
- ✅ Health endpoint returns "Healthy"

**Frontend verification:**
- ✅ Dev server starts without errors
- ✅ Login page loads
- ✅ Can connect to backend API

**Database verification:**
```powershell
# Connect to PostgreSQL
psql -U postgres -d cephasops

# Check latest migrations
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;

# Exit
\q
```

---

## Common Issues & Solutions

### 🔴 Issue 1: Git Pull Fails with "Cannot pull with uncommitted changes"

**Solution:**
```powershell
# Save your changes temporarily
git stash

# Pull the changes
git pull origin single_company

# Restore your changes
git stash pop
```

---

### 🔴 Issue 2: Database Migration Fails

**Error message:**
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server.
```

**Solution:**
```powershell
# Check if PostgreSQL is running
Get-Service postgresql*

# If not running, start it
Start-Service postgresql-x64-14  # Adjust version as needed

# Verify connection string
dotnet user-secrets list --project src/CephasOps.Api
```

---

### 🔴 Issue 3: Backend Fails to Start - "Address already in use"

**Error message:**
```
Failed to bind to address http://localhost:5000: address already in use.
```

**Solution:**
```powershell
# Find what's using port 5000
netstat -ano | findstr :5000

# Kill the process (replace XXXX with PID from above)
taskkill /PID XXXX /F

# Or change the port in launchSettings.json
```

---

### 🔴 Issue 4: Frontend Can't Connect to Backend

**Error in browser console:**
```
Failed to fetch: ERR_CONNECTION_REFUSED
```

**Solution:**
1. Verify backend is running on port 5000
2. Check `.env.local` has correct API URL
3. Restart frontend after changing `.env.local`

---

### 🔴 Issue 5: Missing NuGet or npm Packages

**Error:**
```
The type or namespace 'Something' could not be found
```

**Solution:**
```powershell
# For backend
cd backend
dotnet restore
dotnet clean
dotnet build

# For frontend
cd frontend
rm -r node_modules
npm install
```

---

## Automated Sync Scripts

The CephasOps project includes helper scripts to automate syncing:

### 🔹 Full Sync Script (Recommended)

Create this script: `sync-pc.ps1` in project root:

```powershell
# sync-pc.ps1 - Complete PC sync automation

Write-Host "🔄 Starting CephasOps PC Sync..." -ForegroundColor Cyan

# Step 1: Git Pull
Write-Host "`n📥 Pulling latest code..." -ForegroundColor Yellow
git pull origin single_company
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Git pull failed! Resolve conflicts and try again." -ForegroundColor Red
    exit 1
}

# Step 2: Backend Dependencies
Write-Host "`n📦 Updating backend dependencies..." -ForegroundColor Yellow
cd backend
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Backend restore failed!" -ForegroundColor Red
    exit 1
}

# Step 3: Frontend Dependencies
Write-Host "`n📦 Updating frontend dependencies..." -ForegroundColor Yellow
cd ../frontend
npm install --silent
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Frontend npm install failed!" -ForegroundColor Red
    exit 1
}

# Step 4: Frontend-SI Dependencies
Write-Host "`n📦 Updating frontend-SI dependencies..." -ForegroundColor Yellow
cd ../frontend-si
npm install --silent
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Frontend-SI npm install failed!" -ForegroundColor Red
    exit 1
}

# Step 5: Database Migrations
Write-Host "`n🗄️  Applying database migrations..." -ForegroundColor Yellow
cd ../backend
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Database migration failed!" -ForegroundColor Red
    exit 1
}

# Success
Write-Host "`n✅ Sync complete! Your PC is now up to date." -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "  1. Run: cd backend && dotnet watch run --project src/CephasOps.Api" -ForegroundColor White
Write-Host "  2. In a new terminal: cd frontend && npm run dev" -ForegroundColor White

cd ..
```

**Usage:**
```powershell
.\sync-pc.ps1
```

---

### 🔹 Quick Database-Only Sync

If only database changed:

```powershell
# quick-db-sync.ps1
cd backend
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
```

---

### 🔹 Quick Code-Only Sync

If only code changed (no database, no new packages):

```powershell
# quick-code-sync.ps1
git pull origin single_company
Write-Host "✅ Code synced! If using 'dotnet watch', changes will auto-reload." -ForegroundColor Green
```

---

## Safety Tips

### ⚠️ Before Making Changes on Second PC

1. **Always pull first** - Don't start coding until you've synced
2. **Check the branch** - Make sure you're on the right branch
3. **Verify migrations** - Ensure database is up to date

### ⚠️ When Working Across Multiple PCs

1. **Commit frequently** - Small, frequent commits reduce conflicts
2. **Pull before you push** - Always sync before pushing your changes
3. **Use descriptive commit messages** - Helps track what changed
4. **Don't commit secrets** - Use user-secrets and .env files (git-ignored)

### ⚠️ Database Safety

1. **Backup before major migrations** - Especially in production
2. **Test migrations locally first** - Don't apply directly to production
3. **Never delete migration files** - Can break database history

```powershell
# Backup database before major changes
pg_dump -U postgres cephasops > backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql
```

---

## Sync Workflow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                         PC #1 (Current)                      │
│  ┌────────────┐  ┌────────────┐  ┌──────────────────┐       │
│  │  Make      │→ │   Commit   │→ │   Push to Git    │       │
│  │  Changes   │  │            │  │                  │       │
│  └────────────┘  └────────────┘  └──────────────────┘       │
└─────────────────────────────────┬───────────────────────────┘
                                  │
                                  ↓ (Git Repository)
┌─────────────────────────────────┴───────────────────────────┐
│                         PC #2 (Second)                       │
│  ┌────────────┐  ┌────────────┐  ┌──────────────────┐       │
│  │ Pull from  │→ │   Restore  │→ │ Apply Migrations │       │
│  │    Git     │  │  Packages  │  │                  │       │
│  └────────────┘  └────────────┘  └──────────────────┘       │
│                                           ↓                  │
│                                  ┌──────────────────┐        │
│                                  │   Test & Verify  │        │
│                                  └──────────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

---

## Summary Checklist for Daily Sync

**At the start of your day on the second PC:**

```
□ Open PowerShell in project root
□ git pull origin single_company
□ cd backend && dotnet restore
□ cd ../frontend && npm install
□ cd ../backend && dotnet ef database update [options]
□ Verify environment secrets are configured
□ Start backend: dotnet watch run
□ Start frontend: npm run dev
□ Test: Open http://localhost:5173 and login
```

**Time required**: ~5 minutes for routine sync

---

## Need Help?

**If you get stuck:**

1. Check the error message carefully
2. Refer to the "Common Issues" section above
3. Check the CephasOps documentation in `/docs`
4. Contact the team lead or senior developer

**Useful commands for troubleshooting:**

```powershell
# Check Git status
git status

# Check what changed
git log --oneline -10

# Check .NET version
dotnet --version

# Check Node version
node --version

# Check PostgreSQL service
Get-Service postgresql*

# Check running processes on ports
netstat -ano | findstr :5000
netstat -ano | findstr :5173
```

---

**Document Version**: 1.0  
**Last Updated**: December 3, 2025  
**Maintained By**: CephasOps Development Team

