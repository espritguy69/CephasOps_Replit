# SI Mobile – Gaps and Reference (frontend-si vs si-mobile)

Version: 1.0  
Status: Implementation Reference  

---

## 1. Purpose

This document records:

- **API contracts** used by si-mobile (aligned with backend and frontend-si).
- **Naming and workflow** alignment (backend status codes vs doc vs frontend-si).
- **Gaps** between the current web frontend-si and the new native si-mobile for this phase.

---

## 2. API Contracts (Reused by si-mobile)

### 2.1 Auth

| Endpoint | Method | Request | Response |
|----------|--------|---------|----------|
| `/api/auth/login` | POST | `{ "Email": string, "Password": string }` | `{ success, data: { accessToken, refreshToken?, user: { id, name, email, phone?, roles } } }` |
| `/api/auth/me` | GET | (Bearer token) | User object |
| Token storage | - | Store `accessToken` (and optionally `refreshToken`) in secure storage |

- Backend DTOs: `LoginRequestDto` (Email, Password), `LoginResponseDto` (AccessToken, RefreshToken, User).
- frontend-si sends PascalCase (`Email`, `Password`); backend may accept camelCase if configured. si-mobile will send same shape as frontend-si for compatibility.

### 2.2 Orders (Jobs)

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/orders` | GET | List orders; backend filters by current user (assigned SI). Params: `fromDate`, `toDate`, `status`, etc. |
| `/api/orders/:id` | GET | Order detail |

- Response: Order with `id`, `status`, `customerName`, `addressLine1`, `city`, `appointmentDate`, `orderType`, etc. (camelCase or PascalCase depending on backend serialization).

### 2.3 Workflow

| Endpoint | Method | Request | Purpose |
|----------|--------|---------|---------|
| `/api/workflow/execute` | POST | `{ entityType: "Order", entityId: string (Guid), targetStatus: string, payload?: object }` | Execute status transition |
| `/api/workflow/allowed-transitions` | GET | Query: `entityType=Order`, `entityId`, `currentStatus` | Get allowed next statuses |

- Backend: `ExecuteTransitionDto` – EntityId (Guid), EntityType, TargetStatus, Payload (optional). Payload can include `remarks`, `location` (lat/long).
- frontend-si uses: `executeTransition(orderId, toStatus, { remarks, location })`.

### 2.4 Earnings

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/payroll/earnings` | GET | Current user's earnings; params: `fromDate`, `toDate` |

- frontend-si derives “today / week / month” in client and calls with date range.

### 2.5 Service Installer Profile

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/service-installers` | GET | List (backend filters by current user/tenant); SI finds own by matching `userId` to current user |

- frontend-si: after login, fetches this list and finds SI where `userId` === current user `id` to set `serviceInstaller` and `siId`.

---

## 3. Workflow Status Alignment

### 3.1 Data model / doc (SI_APP_DATA_MODEL_MAPPING.md, ARCHITECTURE.md)

- Internal codes: `ASSIGNED`, `ON_THE_WAY`, `MET_CUSTOMER`, `START_WORK`, `COMPLETE`, `PROBLEM`, `RESCHEDULE`.
- UI labels: Assigned, On the Way, Met Customer, Start Work, Complete, Report Problem, Request Reschedule.

### 3.2 frontend-si usage

- Statuses seen in code: `Pending`, `Assigned`, `OnTheWay`, `MetCustomer`, `Installing`, `OrderCompleted`, `Completed`.
- Completion transition: `OrderCompleted` (and possibly `Completed` in backend).

### 3.3 Backend (WorkflowEngineService)

- Uses status strings from workflow definitions (company/partner/order-type specific). Examples referenced: `Assigned`, `OrderCompleted`, `Completed`.
- Allowed-transitions returned as `WorkflowTransitionDto` (e.g. FromStatus, ToStatus, Name).

### 3.4 si-mobile stance

- **Use backend as source of truth**: call `allowed-transitions` for each order and show only those transitions; display `order.status` as returned by API.
- Map common backend statuses to UI labels where needed (e.g. `OnTheWay` → “On the Way”, `OrderCompleted` → “Complete”). Do not invent new status codes; add mapping only for display.

---

## 4. Gaps: frontend-si vs si-mobile (This Phase)

### 4.1 Implemented in frontend-si, placeholder or later in si-mobile

| Area | frontend-si | si-mobile (phase 1) |
|------|-------------|----------------------|
| **Material scanning** | MaterialsScanPage, SerialScanner, recordMaterialUsage, getRequiredMaterials | Placeholder screen / module; API layer stubbed |
| **Assurance replacement** | ReplacementForm, recordMaterialReplacement, markDeviceAsFaulty | Placeholder; same API contracts documented for later |
| **Extra work** | Not fully in frontend-si | Placeholder; align with SI_APP_DATA_MODEL_MAPPING (ExtraWorkCatalog, OrderExtraWork, etc.) when backend ready |
| **Proof capture** | PhotoUpload, PhotoGallery, photos API | Placeholder; proof checklist and upload to be added |
| **Completion review** | JobDetailPage validates checklist + materials before OrderCompleted | Placeholder “Completion review” step; reuse same validation ideas when implementing |
| **Reschedule** | RescheduleRequestModal, requestReschedule, getScheduleSlotForOrder | Placeholder; API exists in frontend-si |
| **OTP / Password reset** | Not in LoginPage | Out of scope this phase |
| **Scan tab** | No dedicated “Scan” tab in current nav (materials under Materials) | Tab present per ARCHITECTURE; screen can be “Scan” entry point with placeholders |

### 4.2 Navigation

- **frontend-si**: Dashboard, Jobs, Orders (admin), Materials (scan/tracking/returns), Service Installers (admin), Earnings (subcon), Profile (button, no route).
- **si-mobile (ARCHITECTURE)**: Home, Jobs, Scan, Earnings, Profile. “Current Job” bar when a job is active.
- **Gap**: si-mobile uses 5-tab structure and Current Job bar; no admin/Orders/Materials management in this phase.

### 4.3 Dashboard / Home

- **frontend-si**: Dashboard shows orders/jobs KPIs, recent orders, low stock (admin); uses getAllOrders vs getAssignedJobs by role.
- **si-mobile**: Home shows installer-focused summary (today’s jobs, completed, remaining, problems), active job card, earnings summary, quick actions (Scan, My Inventory, Report Problem, Help).

### 4.4 Auth storage

- **frontend-si**: `localStorage` for authToken / refreshToken.
- **si-mobile**: Secure storage (e.g. expo-secure-store) for tokens; no localStorage.

### 4.5 API client

- **frontend-si**: Vite `import.meta.env.VITE_API_BASE_URL`, fetch with Bearer from context/localStorage, unwraps `ApiResponse<T>`.
- **si-mobile**: Env config (e.g. APP_API_URL); same unwrap logic; token from secure storage + auth context.

---

## 5. References

- Backend: `AuthController`, `WorkflowController`, `OrdersController` (and payroll/earnings, service-installers).
- frontend-si: `src/api/client.ts`, `workflow.ts`, `orders.ts`, `si-app.ts`, `earnings.ts`, `AuthContext.tsx`, `JobDetailPage`, `JobsListPage`, `DashboardPage`, `EarningsPage`.
- Docs: `frontend-si/ARCHITECTURE.md`, `frontend-si/SI_APP_DATA_MODEL_MAPPING.md`, `docs/07_frontend/si_app_architecture.md` (if present), `docs/business/si_app_journey.md`, `docs/architecture/data_model_overview.md`.

---

## 6. Summary

- si-mobile **reuses** the same auth, orders, workflow, earnings, and service-installer APIs as frontend-si.
- **Workflow status**: Use backend `allowed-transitions` and `order.status`; map to UI labels; no new status codes.
- **Gaps**: Material scanning, assurance replacement, extra work, proof capture, completion review, and reschedule are **placeholders or later** in si-mobile; navigation and Home are aligned with ARCHITECTURE; auth uses secure storage instead of localStorage.

---

## 7. SI Mobile deliverables (phase 1)

- **Gap list:** This document (§4).
- **Implementation notes:** `si-mobile/IMPLEMENTATION_NOTES.md` – scope, screens, API layer, navigation, how to run, next steps.
