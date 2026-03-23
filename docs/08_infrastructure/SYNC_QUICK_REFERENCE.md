# 🚀 PC Sync - Quick Reference Card

> **Print this page or keep it handy for daily use!**

---

## ⚡ Quick Commands

### Full Sync (Everything)
```powershell
.\sync-pc.ps1
```
**Use when**: Starting work on second PC, or after major changes

**Time**: ~5 minutes

---

### Code Only (No DB/Packages)
```powershell
.\quick-code-sync.ps1
```
**Use when**: Only source code changed

**Time**: ~30 seconds

---

### Database Only (No Code/Packages)
```powershell
.\quick-db-sync.ps1
```
**Use when**: Only migrations were added

**Time**: ~1 minute

---

## 🎯 Daily Workflow

### Morning Routine - Second PC

```powershell
# 1. Quick sync
.\quick-code-sync.ps1

# 2. Check for migrations
cd backend
dotnet ef migrations list

# 3. If migrations exist
.\quick-db-sync.ps1

# 4. Start services
dotnet watch run --project src/CephasOps.Api

# 5. In new terminal
cd frontend
npm run dev
```

**Total Time**: 2-3 minutes

---

## 🔥 Emergency Commands

### Git Conflicts?
```powershell
git stash          # Save your work
git pull           # Get updates
git stash pop      # Restore your work
# Resolve conflicts in VS Code
```

### Port Already Used?
```powershell
# Find and kill process
netstat -ano | findstr :5000
taskkill /PID XXXX /F
```

### Database Connection Failed?
```powershell
# Check PostgreSQL
Get-Service postgresql*

# Start if stopped
Start-Service postgresql-x64-14

# Verify connection
dotnet user-secrets list --project backend/src/CephasOps.Api
```

### Packages Broken?
```powershell
# Backend
cd backend
dotnet clean
dotnet restore

# Frontend
cd frontend
Remove-Item -Recurse -Force node_modules
npm install
```

---

## ✅ Pre-Sync Checklist

Before running sync on second PC:

- [ ] Committed/pushed changes from first PC
- [ ] Closed any running services on second PC
- [ ] Have 5-10 minutes available
- [ ] PostgreSQL is running

---

## 🚦 Status Checks

### Is Backend Running?
```powershell
# Check in browser
http://localhost:5000/health

# Or check process
Get-Process -Name "dotnet" | Where-Object {$_.Path -like "*CephasOps*"}
```

### Is Frontend Running?
```powershell
# Check in browser
http://localhost:5173

# Or check process
Get-NetTCPConnection -LocalPort 5173
```

### Are Migrations Up to Date?
```powershell
cd backend
dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api
```

---

## 📦 What Gets Synced?

| Component | Full Sync | Code Sync | DB Sync |
|-----------|-----------|-----------|---------|
| Git Pull | ✅ | ✅ | ❌ |
| .NET Packages | ✅ | ❌ | ❌ |
| npm Packages | ✅ | ❌ | ❌ |
| Migrations | ✅ | ❌ | ✅ |

---

## 🎨 Color-Coded Outputs

**Green ✅** = Success - Continue  
**Yellow ⚠️** = Warning - Review but OK  
**Red ❌** = Error - Must fix  
**Cyan ℹ️** = Info - FYI only

---

## 🔄 Sync Decision Tree

```
Do you have uncommitted changes?
├─ Yes → Stash or Commit first
└─ No → Continue

What changed on other PC?
├─ Code only → quick-code-sync.ps1
├─ Database only → quick-db-sync.ps1
├─ Both/Multiple → sync-pc.ps1
└─ Not sure → sync-pc.ps1 (safe default)

Did sync complete successfully?
├─ Yes → Start services and test
└─ No → Check error message
    ├─ Git conflict → Resolve manually
    ├─ DB error → Check PostgreSQL
    └─ Package error → Check internet
```

---

## ⏱️ Time Estimates

| Task | Time | Frequency |
|------|------|-----------|
| Full sync | 5-10 min | Once per day |
| Code sync | 30 sec | Multiple times |
| DB sync | 1-2 min | When migrations added |
| Start services | 30 sec | Once per session |

---

## 📍 Important File Locations

```
Project Root/
├─ sync-pc.ps1              ← Full sync
├─ quick-code-sync.ps1      ← Code only
├─ quick-db-sync.ps1        ← Database only
│
├─ backend/
│  ├─ start.ps1             ← Start backend
│  └─ migrate.ps1           ← Manual migration
│
├─ frontend/
│  └─ start.ps1             ← Start frontend
│
└─ docs/08_infrastructure/
   └─ PC_SYNC_GUIDE.md      ← Full documentation
```

---

## 🆘 Get Help

**Full Guide**: `docs/08_infrastructure/PC_SYNC_GUIDE.md`

**Common Issues**: See Section "Common Issues & Solutions" in guide

**Can't Find Answer**: Contact dev team

---

## 💡 Pro Tips

1. **Use `dotnet watch`** - Auto-reloads on code changes
2. **Sync in morning** - Before starting work on second PC
3. **Commit often** - Small commits = fewer conflicts
4. **Test after sync** - Quick smoke test prevents surprises
5. **Keep services running** - Less restart = more productivity

---

## 🎯 Most Common Command

```powershell
.\sync-pc.ps1
```

**When in doubt, run this.** It's safe and comprehensive.

---

**Print Date**: _________  
**Version**: 1.0  
**Last Updated**: December 3, 2025

