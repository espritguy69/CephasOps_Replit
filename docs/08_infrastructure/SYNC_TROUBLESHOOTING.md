# 🔧 PC Sync Troubleshooting Guide

> **Quick solutions to common sync problems**

---

## 🎯 Quick Diagnosis

**Identify your issue:**

| Symptom | Likely Cause | Jump to Section |
|---------|--------------|-----------------|
| Git pull fails | Uncommitted changes or conflicts | [Git Issues](#git-issues) |
| Can't connect to database | PostgreSQL not running or wrong credentials | [Database Issues](#database-issues) |
| Migration errors | Database version mismatch | [Migration Issues](#migration-issues) |
| Backend won't start | Port in use or missing config | [Backend Issues](#backend-issues) |
| Frontend won't start | Port in use or missing packages | [Frontend Issues](#frontend-issues) |
| Sync script stops mid-way | Various - check error message | [Script Issues](#script-issues) |

---

## 🔴 Git Issues

### Issue: "Cannot pull with uncommitted changes"

**Error:**
```
error: Your local changes to the following files would be overwritten by merge:
  backend/src/CephasOps.Application/Notifications/Services/NotificationService.cs
Please commit your changes or stash them before you merge.
```

**Solution 1 - Stash (Temporary Save):**
```powershell
# Save your changes
git stash

# Pull updates
git pull origin single_company

# Restore your changes
git stash pop
```

**Solution 2 - Commit:**
```powershell
# Commit your changes
git add .
git commit -m "WIP: work in progress before sync"

# Pull updates
git pull origin single_company
```

---

### Issue: "Merge conflict"

**Error:**
```
CONFLICT (content): Merge conflict in backend/src/...
Automatic merge failed; fix conflicts and then commit the result.
```

**Solution:**
```powershell
# Open the conflicted file in VS Code
code path/to/conflicted/file.cs

# Look for conflict markers:
# <<<<<<< HEAD
#   Your changes
# =======
#   Their changes
# >>>>>>> origin/single_company

# Choose which version to keep (or combine both)
# Remove the conflict markers

# After fixing all conflicts:
git add .
git commit -m "Resolved merge conflicts"
```

**Pro Tip:** Use VS Code's built-in merge conflict resolver (appears at top of file).

---

### Issue: "Diverged branches"

**Error:**
```
Your branch and 'origin/single_company' have diverged,
and have 3 and 5 different commits each, respectively.
```

**Solution:**
```powershell
# See what's different
git log --oneline --graph --all

# Pull with rebase (cleaner history)
git pull --rebase origin single_company

# Or pull with merge (safer)
git pull origin single_company

# If conflicts occur, follow merge conflict steps above
```

---

### Issue: "Permission denied (publickey)"

**Error:**
```
Permission denied (publickey).
fatal: Could not read from remote repository.
```

**Solution:**
```powershell
# Check your SSH keys
ssh-add -l

# If no keys, generate one
ssh-keygen -t ed25519 -C "your_email@example.com"

# Add to SSH agent
ssh-add ~/.ssh/id_ed25519

# Add public key to GitHub/GitLab/Azure DevOps
Get-Content ~/.ssh/id_ed25519.pub | clip
# Paste into your Git provider's SSH keys settings
```

---

## 🗄️ Database Issues

### Issue: "Connection refused" or "Connection timeout"

**Error:**
```
Npgsql.NpgsqlException: Connection refused
   at Npgsql.NpgsqlConnector.Connect()
```

**Solution 1 - Check if PostgreSQL is running:**
```powershell
# Check service status
Get-Service postgresql*

# If not running, start it
Start-Service postgresql-x64-14  # Adjust version number
```

**Solution 2 - Verify connection string:**
```powershell
# View your secrets
cd backend/src/CephasOps.Api
dotnet user-secrets list

# Should show:
# ConnectionStrings:DefaultConnection = Host=localhost;Database=cephasops;...

# If missing or wrong, set it:
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=cephasops;Username=postgres;Password=YOUR_PASSWORD"
```

**Solution 3 - Test connection manually:**
```powershell
# Try connecting with psql
psql -U postgres -d cephasops

# If it asks for password and connects, your config is wrong
# If it fails, PostgreSQL might not be accessible
```

---

### Issue: "Database does not exist"

**Error:**
```
3D000: database "cephasops" does not exist
```

**Solution:**
```powershell
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE cephasops;

# Exit
\q

# Run migrations
cd backend
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
```

---

### Issue: "Password authentication failed"

**Error:**
```
28P01: password authentication failed for user "postgres"
```

**Solution:**
```powershell
# Reset PostgreSQL password
psql -U postgres

# In psql:
ALTER USER postgres PASSWORD 'new_password';
\q

# Update your secrets
cd backend/src/CephasOps.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=cephasops;Username=postgres;Password=new_password"
```

---

## 🔄 Migration Issues

### Issue: "Pending migrations exist"

**Error:**
```
Relational database migration pending. Apply all pending migrations before attempting model changes.
```

**Solution:**
```powershell
# Apply pending migrations
cd backend
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api

# Or use the script
.\migrate.ps1
```

---

### Issue: "Migration already applied"

**Error:**
```
The migration '20250203_AddNotificationDepartmentId' has already been applied to the database.
```

**Solution:**
This is usually not an error - it means your database is already up to date.

If sync script fails because of this, it's a false positive. Your database is fine.

---

### Issue: "Column already exists"

**Error:**
```
42701: column "department_id" of relation "notifications" already exists
```

**Solution:**
```powershell
# This means your database is ahead of or out of sync with migrations

# Option 1: Reset to a known state (CAUTION: data loss)
cd backend
dotnet ef database drop --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api

# Option 2: Manually fix in database
psql -U postgres -d cephasops
# Run SQL to fix the issue
\q

# Option 3: Roll back problematic migration
dotnet ef database update PreviousMigrationName --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
```

---

## 🖥️ Backend Issues

### Issue: "Address already in use"

**Error:**
```
System.IO.IOException: Failed to bind to address http://localhost:5000: address already in use.
```

**Solution:**
```powershell
# Find what's using port 5000
netstat -ano | findstr :5000

# Output shows PID in last column:
# TCP    0.0.0.0:5000    0.0.0.0:0    LISTENING    12345

# Kill the process
taskkill /PID 12345 /F

# Or use a different port temporarily
# Edit backend/src/CephasOps.Api/Properties/launchSettings.json
# Change "http://localhost:5000" to "http://localhost:5001"
```

---

### Issue: "Type or namespace not found"

**Error:**
```
CS0246: The type or namespace name 'Something' could not be found
```

**Solution:**
```powershell
# Clean and restore
cd backend
dotnet clean
dotnet restore
dotnet build

# If still failing, check for missing NuGet packages
# Compare .csproj files with other PC
```

---

### Issue: "Missing appsettings or secrets"

**Error:**
```
System.InvalidOperationException: Unable to resolve service for type 'IConfiguration'
```

**Solution:**
```powershell
# Verify secrets exist
cd backend/src/CephasOps.Api
dotnet user-secrets list

# If empty, set required secrets:
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=cephasops;Username=postgres;Password=your_password"
dotnet user-secrets set "JwtSettings:SecretKey" "your-super-secret-key-at-least-32-characters-long"
dotnet user-secrets set "JwtSettings:Issuer" "CephasOps"
dotnet user-secrets set "JwtSettings:Audience" "CephasOpsUsers"
dotnet user-secrets set "JwtSettings:ExpirationMinutes" "60"
dotnet user-secrets set "SYNCFUSION_LICENSE_KEY" "your-syncfusion-license"
```

---

## 🎨 Frontend Issues

### Issue: "Port 5173 already in use"

**Error:**
```
Port 5173 is in use, trying another one...
```

**Solution:**
```powershell
# Find and kill process
Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue | ForEach-Object {
    Stop-Process -Id $_.OwningProcess -Force
}

# Or let Vite use another port (it does this automatically)
```

---

### Issue: "Module not found"

**Error:**
```
Error: Cannot find module '@/components/ui/button'
```

**Solution:**
```powershell
# Reinstall packages
cd frontend
Remove-Item -Recurse -Force node_modules
Remove-Item package-lock.json
npm install

# If still failing, check path aliases in vite.config.ts and tsconfig.json
```

---

### Issue: "Cannot connect to API"

**Error in browser console:**
```
Failed to fetch: ERR_CONNECTION_REFUSED
```

**Solution:**
```powershell
# Check if backend is running
# Open http://localhost:5000/health in browser

# If backend is not running, start it:
cd backend
dotnet watch run --project src/CephasOps.Api

# Check .env.local in frontend
cd frontend
Get-Content .env.local
# Should have: VITE_API_BASE_URL=http://localhost:5000/api

# If wrong or missing:
echo "VITE_API_BASE_URL=http://localhost:5000/api" > .env.local

# Restart frontend
npm run dev
```

---

## 🤖 Script Issues

### Issue: "sync-pc.ps1 cannot be loaded"

**Error:**
```
sync-pc.ps1 cannot be loaded because running scripts is disabled on this system.
```

**Solution:**
```powershell
# Check current execution policy
Get-ExecutionPolicy

# Set to allow local scripts (run as Administrator)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or bypass for single run
powershell -ExecutionPolicy Bypass -File .\sync-pc.ps1
```

---

### Issue: "Script stops at npm install"

**Symptom:** Script hangs at "Updating frontend dependencies..."

**Solution:**
```powershell
# Cancel script (Ctrl+C)

# Try manual install to see real error
cd frontend
npm install

# Common fixes:
# - Clear npm cache: npm cache clean --force
# - Delete node_modules and package-lock.json
# - Check internet connection
# - Try with --verbose flag: npm install --verbose
```

---

### Issue: "Git stash prompt blocks script"

**Symptom:** Script waits for user input

**Solution:**
```powershell
# Commit or stash before running script
git stash

# Or run script with pre-stashed changes
git stash && .\sync-pc.ps1 && git stash pop
```

---

## 🔍 Advanced Diagnostics

### Check All Services at Once

```powershell
# Backend
Test-NetConnection -ComputerName localhost -Port 5000

# Frontend
Test-NetConnection -ComputerName localhost -Port 5173

# Database
Test-NetConnection -ComputerName localhost -Port 5432

# All should show "TcpTestSucceeded: True" if running
```

---

### Check Package Versions Match

```powershell
# Backend - .NET SDK version
dotnet --version
# Should be 10.x.x

# Frontend - Node version
node --version
# Should be 18.x or higher

# Frontend - npm version
npm --version
# Should be 9.x or higher
```

---

### View Detailed Migration History

```powershell
cd backend

# List all migrations
dotnet ef migrations list --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api

# View SQL for a specific migration
dotnet ef migrations script --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api

# Check database version
psql -U postgres -d cephasops -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"
```

---

## 📊 Health Check Commands

Run these to verify everything is working:

```powershell
# 1. Git status
git status

# 2. Backend health
Invoke-WebRequest -Uri http://localhost:5000/health

# 3. Frontend
Invoke-WebRequest -Uri http://localhost:5173

# 4. Database
psql -U postgres -d cephasops -c "SELECT version();"

# 5. Services
Get-Service postgresql*
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 5000,5173 -ErrorAction SilentlyContinue
```

---

## 🆘 Nuclear Options (Last Resort)

### Option 1: Clean Slate (Code Only)

```powershell
# Delete all git changes
git reset --hard origin/single_company
git clean -fd

# Run full sync
.\sync-pc.ps1
```

**⚠️ WARNING**: This deletes all uncommitted changes!

---

### Option 2: Clean Slate (Database)

```powershell
# Drop and recreate database
psql -U postgres -c "DROP DATABASE IF EXISTS cephasops;"
psql -U postgres -c "CREATE DATABASE cephasops;"

# Apply all migrations
cd backend
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
```

**⚠️ WARNING**: This deletes all data!

---

### Option 3: Fresh Clone

```powershell
# Backup your current folder
Move-Item C:\Projects\CephasOps C:\Projects\CephasOps.backup

# Clone fresh
cd C:\Projects
git clone <repository-url> CephasOps

# Setup from scratch
cd CephasOps
.\sync-pc.ps1
```

**⚠️ WARNING**: Start from zero!

---

## 📞 Still Stuck?

If none of these solutions work:

1. **Document your error:**
   - Copy exact error message
   - Note what command you ran
   - Note what you've tried

2. **Check logs:**
   - Backend logs in terminal
   - Frontend console in browser (F12)
   - PostgreSQL logs: `C:\Program Files\PostgreSQL\14\data\log`

3. **Ask for help:**
   - Share error message
   - Share relevant logs
   - Explain what you were trying to do

4. **Update this guide:**
   - Once resolved, add your solution here
   - Help the next person!

---

## 💡 Prevention Tips

**Prevent sync issues by:**

- ✅ Syncing daily at start of work
- ✅ Committing frequently (small commits)
- ✅ Never committing secrets or credentials
- ✅ Testing locally before pushing
- ✅ Communicating major changes to team
- ✅ Keeping services running with watch mode
- ✅ Running health checks after sync

---

**Last Updated**: December 3, 2025  
**Version**: 1.0  
**Maintained By**: CephasOps DevOps Team

