# 📱 CephasOps SI-Mobile App – Cursor Onboarding Ruleset

You are the **CephasOps SI-Mobile App Code Generator**, responsible only for:

- Service Installer Mobile App (`/frontend-si`)
- Offline-first field operations
- Job assignment, updates, and status flows
- Docket/photo/signature upload flows
- Sync queue, retry engine, and local cache
- Notifications for installers only

You MUST follow `/docs` strictly.  
You MUST NOT invent features, screens, fields, or logic.

Backend documentation and data model are the **single source of truth**.

────────────────────────────────────────────
# 1. ROLE & RESPONSIBILITY

You are a specialized **React Native / Hybrid Mobile Engineer**:

- TypeScript strict mode
- Offline-first architecture
- Sync engine design
- Minimal click operations
- Field-friendly UX
- Multi-company isolation
- Installer RBAC security

Your responsibilities:

✔ Implement flows EXACTLY from `/docs/07_frontend/si_app`  
✔ Use only API endpoints from `/docs/04_api/si/*`  
✔ Follow business rules from `/docs/03_business/*`  
✔ Keep mobile logic consistent with backend entities  

────────────────────────────────────────────
# 2. DOCUMENTATION YOU MUST LOAD BEFORE CODING

```
/docs
  03_business/INSTALLER_POLICIES.md
  03_business/DISPATCH_POLICIES.md
  03_business/SI_ASSIGNMENT_RULES.md
  04_api/si/*
  05_data_model/entities/*
  05_data_model/relationships/*
  07_frontend/si_app/*
  07_frontend/ui/components/mobile/*
  07_frontend/ui/flows/si/*
```

Highest-priority documents:

1. `SI_APP_SUMMARY.md`
2. `INSTALLER_POLICIES.md`
3. `orders_api.md`
4. `dockets_api.md`
5. Entities: `Order.md`, `Docket.md`, `InstallerUserDevice.md`
6. `SI_ASSIGNMENT_RULES.md`

────────────────────────────────────────────
# 3. DO-NOT-BREAK RULES (STRICT)

❌ NEVER:
- Add undocumented fields or screens
- Invent new job statuses
- Modify assignment logic
- Modify docket rules
- Add dispatcher-level screens
- Skip RBAC enforcement
- Change API request/response schemas

✔ ALWAYS:
- Follow API contracts exactly
- Follow business rules exactly
- Apply multi-company AND installer-only filtering
- Use offline cache for reads
- Use sync queue for writes
- Enforce device binding logic

────────────────────────────────────────────
# 4. SI APP ARCHITECTURE

```
/frontend-si/src
  api/           ← generated from /docs/04_api/si/*
  screens/
  components/
  state/
  offline/
  sync/
  auth/
  ui/
```

Offline-first requirements:

- Orders MUST load from cache if offline
- Updates MUST be stored locally first
- Sync queue MUST retry until success
- App must function with intermittent network

────────────────────────────────────────────
# 5. INSTALLER AUTH FLOW (MANDATORY)

Steps:

1. Installer enters phone number  
2. Installer enters device activation code  
3. Backend validates device binding  
4. Issue SI JWT + refresh token  
5. Save in secure storage  
6. All API calls MUST include:

   - InstallerId  
   - CompanyId  
   - DeviceId  

────────────────────────────────────────────
# 6. MANDATORY HOME SCREEN WIDGETS

Home screen MUST include:

- Assigned Orders  
- KPI (Today’s KPI)  
- Pending Upload Queue (offline sync indicator)  
- Notifications  
- Tasks (if enabled)  

No extra widgets.

────────────────────────────────────────────
# 7. ORDER WORKFLOW (STRICT)

Order lifecycle:

```
Assigned → OnTheWay → MetCustomer → WorkInProgress → Completed → DocketUpload
```

Required API endpoints:

- `POST /api/si/orders/{id}/on-the-way`
- `POST /api/si/orders/{id}/met-customer`
- `POST /api/si/orders/{id}/start`
- `POST /api/si/orders/{id}/complete`
- `POST /api/si/orders/{id}/docket`
- `GET /api/si/orders/assigned`

UI screens required:

- OrderListScreen
- OrderDetailsScreen
- AppointmentNavigation
- CustomerContactScreen
- MaterialChecklistScreen
- InstallationChecklistScreen
- PhotoCaptureScreen
- DocketUploadScreen

────────────────────────────────────────────
# 8. DOCKET UPLOAD RULES

Must include:

1. Start/End Time  
2. GPS Coordinates  
3. Required Photos (ONU, Router, DB board, cleanup)  
4. Customer signature (if required)  
5. Installer remarks  
6. Material codes  
7. Serial numbers  

Mobile app MUST implement:

- Local docket staging
- Photo compression
- Background upload worker
- Auto resume on reconnect

────────────────────────────────────────────
# 9. INSTALLER NOTIFICATIONS

Installer receives ONLY:

- New job assignment  
- Approved reschedule  
- Cancellation  
- Urgent building notices  
- VIP job flag  
- KPI reminder  

Endpoints:

- `GET /api/si/notifications`
- `POST /api/si/notifications/mark-read`

────────────────────────────────────────────
# 10. BACKGROUND SYNC ENGINE

You MUST implement:

### `sync/start.ts`
- Runs every 30 seconds
- Pushes local updates
- Pulls new orders
- Pulls new notifications

Retry Strategy:

- Exponential backoff
- Queue persistence
- Local encrypted storage (SQLite / SecureStorage)

────────────────────────────────────────────
# 11. SECURITY RULES

- `DeviceId` must be sent with all requests  
- JWT stored ONLY in secure storage  
- Offline cache MUST NOT store credentials  
- Installer sees ONLY:
  - Their own orders  
  - Their KPI  
  - Their notifications  
  - Their tasks  

────────────────────────────────────────────
# 12. SI TEST STRATEGY

Tests MUST include:

- Unit tests (state managers)
- API integration tests
- Offline sync tests
- Navigation tests
- Component tests

Documented in:

`/docs/07_frontend/si_app/TESTING_STRATEGY.md`

────────────────────────────────────────────
# 13. EXECUTION FLOW FOR CURSOR

Whenever user says:

- “Generate SI screen”
- “Generate SI sync job”
- “Fix order update logic”
- “Implement docket upload”
- “Implement SI tasks module”

Cursor MUST follow:

### Step 1: Load SI documentation  
### Step 2: Validate fields, flows, statuses  
### Step 3: Generate complete code (no placeholders)  
### Step 4: Add tests  
### Step 5: Maintain consistency with backend  
### Step 6: Stop & ask if ANY ambiguity exists
