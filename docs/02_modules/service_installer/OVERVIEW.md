
# SERVICE_INSTALLER_APP_MODULE.md
Full Architecture Documentation — CephasOps Mobile PWA for Service Installers (SI App)

---

## 1. Purpose

The SI App is a **mobile-first PWA** designed for:

- In‑house Service Installers (SI)
- Sub‑contractor SIs
- Future contractors across multiple companies

The app must work:

- **Online + Offline**
- **On mobile browser + installable PWA**
- With **camera access** (QR + serial scanning)
- With **GPS stamping** (location accuracy)
- With **photo evidence support** including timestamp & geo-tagging

---

## 2. Core Capabilities

The SI app provides:

### 2.1 Job List (Today, Upcoming, Completed)
Each job shows:
- Service ID (Unique ID)
- Customer name
- Building name
- Address (click → open Google Maps)
- Partner (TIME / Celcom / Digi / U‑Mobile)
- Appointment time
- Status badge:
  - Pending  
  - Assigned  
  - On The Way  
  - Met Customer  
  - Blocked (Customer / Building / Network)  
  - Order Completed  

### 2.2 Job Status Updates
SIs can update statuses:

1. **Assigned → On The Way**  
   - Capture time  
   - GPS location  

2. **On The Way → Met Customer**  
   - Capture time  
   - GPS location  

3. **Met Customer → Order Completed**  
   - Capture time  
   - GPS location  
   - Upload photo evidence  
   - Serial scanning  
   - Material usage  

4. **Blocker** (Customer / Building / Network)
   - Select blocker type  
   - Add remarks  
   - Upload photo  
   - App notifies admin  

All timestamps must sync with server for KPI.

---

## 3. Camera + Serial Scanning

### 3.1 Scan for:
- ONU device serials
- Router serials
- Splitter ports
- Material QR labels
- Cables (if labelled)
- RMA returning items

### 3.2 Camera Requirements
- Auto-focus  
- Low-light enhancement  
- Continuous scanning mode  
- Offline queue + sync when online  

### 3.3 Evidence Photos
Each photo must include:
- **Timestamp**
- **GPS location**
- **SI name**
- **Job ID**
- **Watermark overlay** (generated client-side)

---

## 4. Material Handling (Issued → Used → Returned)

### 4.1 Material In‑Hand Screen
Shows:
- Serial items (ONU, Router, Boosters)
- Non-serial materials  
- Balance stock
- Required materials vs actual usage for each job

### 4.2 Scan Material Usage
- Scan serial to confirm installation
- Auto-attach to order
- Deduct from SI’s stock
- Create RMA if defective

### 4.3 Material Return
For incomplete jobs:
- SI scans items and returns to warehouse
- Warehouse confirms return with scan

---

## 5. GPS & Time Logging

All status changes must capture:

```
{
  status: "OnTheWay",
  timestamp_device: "...",
  timestamp_server: "...",
  gps_lat: "...",
  gps_lng: "...",
  accuracy: "...",
}
```

KPI matrix uses server time.

---

## 6. Offline Mode Requirements

The PWA must:

- Cache job list offline  
- Allow taking photos offline  
- Allow scanning offline  
- Store serials offline  
- Queue all updates  
- Sync automatically when regained connection  

---

## 7. SI Performance Dashboard

SIs can see their own performance only:

### Monthly Stats:
- Total jobs completed
- KPI compliance (time-based)
- Average completion duration
- Blockers raised
- Material accuracy (correct/incorrect usage)
- Earnings (if subcontractor)

### With graphical charts.

---

## 8. Sub‑Contractor Differences

Subcon SIs:
- Cannot view company-level metrics
- Cannot view partner rates
- Cannot see internal comments
- Cannot modify materials except their own issued stock

---

## 9. Security

- JWT tokens for API
- Refresh tokens
- Device binding (optional)
- Permission: `si.*`
- All requests must verify SI’s assignment to the job

---

## 10. API Endpoints (Docs Only)

```
GET  /api/si/jobs/today
GET  /api/si/jobs/upcoming
POST /api/si/jobs/{jobId}/status
POST /api/si/jobs/{jobId}/photo
POST /api/si/materials/scan
POST /api/si/materials/return
GET  /api/si/performance
```

---

## 11. Integration With Other Modules

### Orders
- Status changes update Orders module

### Scheduler
- Schedules push to SI app automatically

### Inventory
- Material usage updates SI stock & warehouse

### RMA
- Faulty scans create RMA tasks

### Payroll
- SI earnings auto-generated based on:
  - Job type
  - KPI compliance
  - Subcon rate table

---

## 12. Summary

The SI App enables:
- Real-time job handling
- Secure scanning & logging
- GPS & timestamp accuracy for audit
- Smart material tracking
- PWA offline reliability
- Independent SI performance dashboards

All fully documented and ready for implementation.
