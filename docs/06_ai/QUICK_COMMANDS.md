# ⚡ Quick Commands Reference Card

## 🚀 **START/RESTART SERVICES**

| Command | Description | Time |
|---------|-------------|------|
| `.\restart-all.ps1` | **Restart both services (recommended)** | 5-8 sec |
| `.\restart-backend.ps1` | Restart backend only | 3-5 sec |
| `.\restart-frontend.ps1` | Restart frontend only | 2-3 sec |

---

## 🎨 **CURSOR TASKS (Ctrl+Shift+P)**

Type: **"Tasks: Run Task"** → Select:

- **🚀 Start All Services (Watch Mode)** ← Best for daily use
- Backend: Watch (Hot Reload)
- Frontend: Dev (Vite)
- Backend: Quick Restart
- Backend: Build Only

---

## 🔥 **HOT RELOAD (No Restart Needed)**

### **Start Once:**
```bash
# Terminal 1:
cd backend/src/CephasOps.Api
dotnet watch run

# Terminal 2:
cd frontend
npm run dev
```

### **Then Just Code!**
- Save files → Auto-reload happens
- Backend: 1-2 sec reload
- Frontend: <1 sec reload

---

## 🛠️ **WHEN TO RESTART:**

### **✅ Auto-Reload Works (No Restart):**
- Controller method changes
- Service logic updates
- React component changes
- CSS/Tailwind changes
- Type changes
- Most code changes!

### **❌ Restart Required:**
- Database migrations
- Package installations (`dotnet add`, `npm install`)
- Config file changes (`appsettings.json`, `vite.config.ts`)
- Middleware additions
- DI registration changes (`Program.cs`)

---

## 📍 **SERVICE URLs:**

- **Backend API**: http://localhost:5000
- **Frontend**: http://localhost:5173
- **Swagger**: http://localhost:5000/swagger (if enabled)

---

## 🔍 **CHECK IF RUNNING:**

```powershell
# Check backend
Invoke-WebRequest -Uri "http://localhost:5000/health" -Method GET

# Check frontend
Invoke-WebRequest -Uri "http://localhost:5173" -Method GET
```

---

## 🛑 **STOP SERVICES:**

```powershell
# Stop backend
Get-NetTCPConnection -LocalPort 5000 | Select-Object -ExpandProperty OwningProcess | ForEach-Object { Stop-Process -Id $_ -Force }

# Stop frontend
Get-NetTCPConnection -LocalPort 5173 | Select-Object -ExpandProperty OwningProcess | ForEach-Object { Stop-Process -Id $_ -Force }
```

---

## 💾 **DATABASE OPERATIONS:**

### **Apply Migration:**
```bash
cd backend/src/CephasOps.Api
dotnet ef database update
```

### **Create Migration:**
```bash
dotnet ef migrations add MigrationName --project ../CephasOps.Infrastructure
```

### **Apply SQL Script to Database:**
```powershell
# IMPORTANT: Use environment variables, never hardcode credentials
# For VPS: source /opt/cephasops/.env first
psql "host=localhost port=5432 dbname=cephasops user=cephasops_app sslmode=prefer" -f "script.sql"
```
> **Note:** The project has migrated from Supabase to self-hosted PostgreSQL 16. Do not use old Supabase connection strings.

---

## 📊 **TIME SAVINGS:**

| Action | Old Method | New Method | Saved |
|--------|-----------|------------|-------|
| Start services | 15 sec | 5 sec | **67% faster** |
| Code change | 15 sec | 1 sec | **93% faster** |
| Daily restarts | 10 min | 2 min | **8 min/day saved** |

---

## 🎓 **RECOMMENDED DAILY WORKFLOW:**

### **Morning:**
```powershell
# Start everything once
.\restart-all.ps1
```

### **During Day:**
- Just code and save files
- Hot reload handles everything
- Keep terminals open

### **Evening:**
- Close terminals (or leave running)
- Git commit/push

---

## 📌 **PINNED FOR QUICK ACCESS**

**Save this file to your bookmarks/desktop for instant reference!**

Quick copy-paste:
```powershell
# One-line restart everything:
.\restart-all.ps1

# One-line restart backend:
.\restart-backend.ps1
```

---

## ✅ **SETUP COMPLETE!**

All fast restart strategies are now configured and ready to use! 🚀

