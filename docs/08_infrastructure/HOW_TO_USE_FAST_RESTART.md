# 🎯 How to Use Fast Restart - Visual Guide

## 🚀 **METHOD 1: PowerShell Scripts** (Easiest)

### **Start Everything:**
```powershell
.\restart-all.ps1
```

**What happens:**
1. Stops any running backend/frontend
2. Opens 2 PowerShell windows:
   - Window 1: Backend with hot reload 🔥
   - Window 2: Frontend with Vite HMR ⚡
3. Both services auto-reload on code changes!

**Time:** ~5-8 seconds

---

### **Start Backend Only:**
```powershell
.\restart-backend.ps1
```
**Time:** ~3-5 seconds

---

### **Start Frontend Only:**
```powershell
.\restart-frontend.ps1
```
**Time:** ~2-3 seconds

---

## 🎨 **METHOD 2: Cursor Tasks** (Best for Development)

### **Step-by-Step:**

1. **Open Command Palette**
   - Press `Ctrl+Shift+P` (Windows/Linux)
   - Or `Cmd+Shift+P` (Mac)

2. **Type:** `Tasks: Run Task`

3. **Select One:**
   ```
   🚀 Start All Services (Watch Mode)     ← Start everything
   Backend: Watch (Hot Reload)            ← Backend only
   Frontend: Dev (Vite)                   ← Frontend only
   Backend: Quick Restart                 ← Quick backend restart
   ```

4. **Services start in Cursor terminals!**

---

## 🔥 **WHAT IS HOT RELOAD?**

### **Example:**

```
1. Backend is running with dotnet watch
           ↓
2. You edit OrdersController.cs
           ↓
3. You save the file (Ctrl+S)
           ↓
4. Terminal shows: "🔥 Hot reload of changes succeeded"
           ↓
5. Changes are LIVE instantly (1-2 seconds)
           ↓
6. No need to restart! ✅
```

### **What You'll See:**

**In Backend Terminal:**
```
dotnet watch 🔥 Hot reload enabled
dotnet watch 💡 Press Ctrl+R to restart
dotnet watch ⌚ File changed: OrdersController.cs
dotnet watch 🔥 Hot reload of changes succeeded
```

**In Frontend Browser:**
- Component updates instantly
- State is preserved
- No page reload!

---

## ⚡ **QUICK COMPARISON:**

### **Old Way (Manual):**
```
1. Stop backend (Ctrl+C)
2. Wait 2-3 seconds
3. Run: dotnet run
4. Wait 10-15 seconds for startup
5. Backend ready

Total: 12-18 seconds
```

### **New Way (Hot Reload):**
```
1. Save file
2. Wait 1-2 seconds
3. Backend ready

Total: 1-2 seconds ✨
```

**90% FASTER!** 🚀

---

## 📋 **WHEN TO USE EACH METHOD:**

### **Use Scripts When:**
- ✅ Starting fresh (beginning of day)
- ✅ After migrations
- ✅ After package installations
- ✅ Backend crashed and needs restart

### **Use Cursor Tasks When:**
- ✅ You want integrated UI
- ✅ You want persistent terminals in Cursor
- ✅ You prefer keyboard shortcuts

### **Use Hot Reload When:**
- ✅ Making code changes (99% of the time!)
- ✅ Services are already running
- ✅ Just coding normally

---

## 🎓 **TYPICAL DAY:**

### **9:00 AM - Start Work**
```powershell
.\restart-all.ps1
```
Services start in 5-8 seconds ✅

---

### **9:05 AM - 5:00 PM - Coding**
- Edit code → Save
- Hot reload → 1-2 seconds ⚡
- Keep coding → Save
- Hot reload → 1-2 seconds ⚡
- **No manual restarts!** 🎉

---

### **12:00 PM - Add New Package**
```bash
dotnet add package SomePackage
```
Then restart:
```powershell
.\restart-backend.ps1
```
**Time: 3-5 seconds** ✅

---

### **3:00 PM - Run Migration**
```bash
dotnet ef database update
```
Then restart:
```powershell
.\restart-backend.ps1
```
**Time: 3-5 seconds** ✅

---

### **5:00 PM - End of Day**
- Just close terminals
- Or leave running for next day!

---

## 💡 **PRO TIPS:**

### **Tip 1: Keep Terminals Open**
- Don't close backend/frontend terminals
- They'll keep watching for changes
- Resume coding anytime - no startup delay!

### **Tip 2: Use Dual Monitors**
- Monitor 1: Cursor (code)
- Monitor 2: Browser (auto-updates) + Terminal windows

### **Tip 3: Press Ctrl+R**
When backend is running with `dotnet watch`, press `Ctrl+R` to force a full restart without stopping the process.

### **Tip 4: Check Hot Reload Status**
Look for this in backend terminal:
```
dotnet watch 🔥 Hot reload enabled
```
If you see this, you're good to go!

---

## 🎯 **WHAT TO EXPECT:**

### **After Saving a Code File:**

**Backend:**
```
dotnet watch ⌚ File changed: OrdersController.cs
dotnet watch 🔥 Hot reload of changes succeeded in 1.2s
```

**Frontend:**
```
[vite] hmr update /src/pages/OrdersListPage.tsx
[vite] page reload src/pages/OrdersListPage.tsx
```

**Browser:**
- Updates instantly (no manual refresh!)

---

## ✅ **CURRENT STATUS:**

| Service | Status | Mode | Reload Time |
|---------|--------|------|-------------|
| Backend | 🔥 **Running** | **dotnet watch** | **1-2 sec** |
| Frontend | 🟢 Running | Vite HMR | <1 sec |

**Backend Terminal Shows:**
```
dotnet watch 🔥 Hot reload enabled
Now listening on: http://localhost:5000
Application started
```

This means hot reload is **ACTIVE** and ready! ✅

---

## 🎉 **YOU'RE READY!**

**3 ways to restart:**
1. **Scripts**: `.\restart-all.ps1`
2. **Cursor Tasks**: `Ctrl+Shift+P` → Tasks
3. **Manual**: `dotnet watch run`

**From now on:**
- ⚡ 90% faster restarts
- 🔥 Most changes don't need restart
- 💻 Smoother workflow
- ⏰ 10+ minutes saved per day

**Happy fast coding!** 🚀

