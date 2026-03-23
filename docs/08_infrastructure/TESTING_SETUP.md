# 🧪 Testing Setup Guide

## Quick Start - Test Frontend Features

### Prerequisites
- ✅ Backend builds successfully (0 errors)
- ✅ PostgreSQL database running (optional for full testing)
- ✅ Node.js and npm installed

---

## Step 1: Start Backend API

```powershell
cd backend\src\CephasOps.Api
dotnet run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
```

**Note:** If database is not configured, some endpoints will fail, but the API will still start.

---

## Step 2: Start Frontend Dev Server

Open a **new terminal**:

```powershell
cd frontend
npm install  # Only needed first time
npm run dev
```

**Expected Output:**
```
  VITE v5.x.x  ready in xxx ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
```

---

## Step 3: Open Browser

Navigate to: **http://localhost:5173**

---

## 🎯 Features Ready to Test

### ✅ Authentication
- **Login Page** (`/login`)
  - Email/password form
  - Error handling
  - Redirects to dashboard on success

### ✅ Dashboard
- **Dashboard** (`/dashboard`)
  - My Tasks widget
  - Company name display
  - Placeholder widgets

### ✅ Orders Module
- **Orders List** (`/orders`)
  - List view with filters
  - Order cards
  - Status badges
- **Order Detail** (`/orders/:orderId`)
  - Full order information
  - Status transitions
  - History timeline

### ✅ Tasks Module
- **My Tasks** (`/tasks/my`)
  - Personal task list
  - Create task modal
  - Task status management
- **Department Tasks** (`/tasks/department/:departmentId`)
  - Department-wide view
  - Filtering and sorting

### ✅ Scheduler
- **Calendar View** (`/scheduler`)
  - Calendar interface
  - Slot management
- **SI Availability** (`/scheduler/availability`)
  - Availability management

### ✅ Settings Pages
- **Company Profile** (`/settings/company`)
- **Global Settings** (`/settings/global`)
- **Material Templates** (`/settings/material-templates`)
- **Document Templates** (`/settings/document-templates`)
- **KPI Profiles** (`/settings/kpi-profiles`)

### ✅ Other Modules
- **Inventory** (`/inventory`)
- **RMA** (`/rma`)
- **Billing - Invoices** (`/billing/invoices`)
- **Notifications** (`/notifications`)
- **Workflow Definitions** (`/workflow/definitions`)
- **Email Settings** (`/settings/email/*`)

---

## 🔍 What to Test

### 1. Navigation
- [ ] Sidebar navigation works
- [ ] All menu items clickable
- [ ] Routes load correctly
- [ ] Breadcrumbs appear on detail pages

### 2. UI Components
- [ ] Buttons are clickable
- [ ] Forms are interactive
- [ ] Modals open/close
- [ ] Toast notifications appear
- [ ] Loading spinners show
- [ ] Empty states display

### 3. Authentication Flow
- [ ] Login page loads
- [ ] Can enter credentials
- [ ] Error messages show (if backend not ready)
- [ ] Redirects work

### 4. API Integration
- [ ] API calls are made (check Network tab)
- [ ] Error handling works (404, 401, etc.)
- [ ] Loading states appear
- [ ] Data displays (if backend returns data)

---

## 🐛 Troubleshooting

### Backend Won't Start

**Error: Database connection failed**
- Check PostgreSQL is running
- Verify connection string in `appsettings.json` or environment variables
- Database will be created on first migration

**Error: Port already in use**
- Change port in `launchSettings.json`
- Or kill process using port 5000

### Frontend Won't Start

**Error: Cannot find module**
```powershell
cd frontend
rm -rf node_modules
npm install
```

**Error: Port 5173 in use**
- Vite will automatically use next available port
- Check console for actual port number

### API Calls Failing

**404 Errors:**
- Normal if backend endpoints not implemented yet
- Frontend handles gracefully with error messages

**CORS Errors:**
- Check backend CORS configuration
- Verify `VITE_API_BASE_URL` in frontend `.env`
- Backend should allow `http://localhost:5173`

**401 Unauthorized:**
- Login required
- Token may have expired
- Check browser console for auth errors

---

## 📊 Testing Checklist

### Core Functionality
- [ ] App loads without errors
- [ ] Login page accessible
- [ ] Navigation works
- [ ] All pages load (even if empty)
- [ ] No console errors

### UI/UX
- [ ] Responsive design works
- [ ] Sidebar toggle works
- [ ] Forms are usable
- [ ] Buttons have hover states
- [ ] Loading states appear
- [ ] Error messages are clear

### API Integration
- [ ] API calls are made correctly
- [ ] Headers include auth token
- [ ] Error handling works
- [ ] Loading states show during requests

---

## 🚀 Next Steps After Testing

1. **If backend endpoints are ready:**
   - Test full CRUD operations
   - Test authentication flow
   - Test data persistence

2. **If backend endpoints are pending:**
   - UI is ready and functional
   - Can continue frontend development
   - Backend integration will work when endpoints are ready

3. **Report Issues:**
   - Note any UI bugs
   - Note any missing features
   - Note any API integration issues

---

## 📝 Notes

- Frontend gracefully handles missing backend endpoints
- All UI components are functional
- Navigation and routing work independently of backend
- API integration is ready - just needs backend endpoints

**Status:** ✅ Frontend is **ready for testing**!

