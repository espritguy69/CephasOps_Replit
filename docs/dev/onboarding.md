# Developer Onboarding

**Related:** [Product overview](../overview/product_overview.md) | [Architecture / API surface](../architecture/api_surface_summary.md) | [Data model overview](../architecture/data_model_overview.md) | [06_ai/DEVELOPER_GUIDE](../06_ai/DEVELOPER_GUIDE.md) | [06_ai/QUICK_START](../06_ai/QUICK_START.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. Local setup

- **Backend:** .NET 10, EF Core 10, PostgreSQL.  
- **Admin frontend:** Node.js, React 18, Vite, TypeScript, Tailwind, Syncfusion.  
- **SI app:** Same stack (React/Vite/TS/Tailwind), separate app under `frontend-si/`.  
- **Database:** PostgreSQL (see `.cursor/rules/postgress.mdc` or env for connection). Default DB name: `cephasops`.

---

## 2. Run scripts (from repo root)

```powershell
# Backend
cd backend/src/CephasOps.Api
dotnet watch run

# Admin frontend
cd frontend
npm install
npm run dev

# SI app (optional)
cd frontend-si
npm install
npm run dev
```

- **Access:** Admin typically `http://localhost:5173`; API typically `http://localhost:5000`.  
- **E2E:** `.\run-e2e.ps1` (optionally `-SkipBackend` if API already running). Requires `frontend/.env` with `E2E_TEST_USER_EMAIL` and `E2E_TEST_USER_PASSWORD`.

---

## 3. Environment variables (key)

- **Backend:** `ConnectionStrings:DefaultConnection` (PostgreSQL); `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`; `Cors:AllowedOrigins`; optional `SYNCFUSION_LICENSE_KEY` (or from env).  
- **Frontend:** API base URL (e.g. Vite env); E2E test user for Playwright.  
- **Parser:** Email account settings (POP3/IMAP) configured via app/settings.

---

## 4. Architecture map (short)

- **API** → **Application** (services, DTOs) → **Domain** (entities, interfaces) → **Infrastructure** (EF Core, external services).  
- **Controllers** grouped by module: Orders, Billing, Scheduler, Parser, Inventory, Buildings, Departments, ServiceInstallers, Payroll, Pnl, Reports, Workflow, Settings, etc.  
- **Department-scoped** endpoints require valid department context (X-Department-Id or departmentId); 403 when not allowed.  
- **Background jobs:** In-process; table + BackgroundJobProcessorService; see [Background jobs](../operations/background_jobs.md).  
- **Admin user management:** SuperAdmin/Admin only; `api/admin/users` (list, create, update, activate/deactivate, roles, reset password, department memberships); sensitive actions audited; frontend at `/admin/users`. v1.2: last login, must-change-password, forced-change flow. v1.3 Phase B: password hashing via IPasswordHasher (legacy hashes still verify; new passwords stored with BCrypt; rehash on login). v1.3 Phase C: account lockout (repeated failed logins temporarily lock the account; config `Auth:Lockout`). v1.3 Phase D: email-based password reset (forgot-password and reset-with-token; config `Auth:PasswordReset`: optional `EmailAccountId`, `TokenExpiryMinutes`, `FrontendResetUrlBase` for reset link). Seed still uses legacy format so seeded admin can log in. **v1.4 Phase 1 – Authentication Security Monitoring:** Auth events (login success/failure, lockout, password change, reset requested/completed, admin reset, token refresh) are written to the existing audit log (`EntityType = "Auth"`). Admins can view a **User Security Activity** log at `/admin/security/activity` (filters: user, event type, date range). User detail modal shows last 10 security events. **v1.4 Phase 2 – Suspicious Activity Detection:** Anomaly detection over Auth events (excessive login failures, password reset abuse, multiple IP login); alerts on Security Activity page and in user detail. **v1.4 Phase 3 – Session Management:** Active sessions = refresh tokens; admins can list and revoke sessions from Security Activity page and user detail (revoke one or revoke all); confirm when revoking own session. See [Department & RBAC](../business/department_rbac.md) and [Admin User Management Audit and Plan](../ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md) (v1.4: § Authentication Security Monitoring, § Suspicious Activity Detection, § Session Management).

---

## 5. Where to read next

- **Business flows:** [Process flows](../business/process_flows.md), [Order lifecycle](../business/order_lifecycle_summary.md).  
- **Data model:** [05_data_model/DATA_MODEL_INDEX](../05_data_model/DATA_MODEL_INDEX.md), [REFERENCE_TYPES_AND_RELATIONSHIPS](../05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md).  
- **API:** [04_api/API_OVERVIEW](../04_api/API_OVERVIEW.md), [API surface summary](../architecture/api_surface_summary.md).  
- **RBAC:** [Department & RBAC](../business/department_rbac.md), [RBAC_MATRIX_REPORT](../RBAC_MATRIX_REPORT.md). **RBAC v2:** Permission catalog (module.action), role–permission matrix at `/admin/security/roles`, [RBAC_V2_PERMISSION_MATRIX_DELIVERABLE](../RBAC_V2_PERMISSION_MATRIX_DELIVERABLE.md). Phase 2: admin users, security, roles, payout, rates, payroll are permission-enforced. Phase 3: department scope—permissions = what, department memberships = where; department-scoped endpoints use ResolveDepartmentScopeOrFailAsync; see [RBAC_V2_ROLLOUT_PHASE3_DEPARTMENT_SCOPE](../RBAC_V2_ROLLOUT_PHASE3_DEPARTMENT_SCOPE.md). **Phase 4:** Full module coverage—Orders (orders.view, orders.edit), Reports (reports.view, reports.export), Inventory (inventory.view, inventory.edit), Background Jobs (jobs.view), Settings (settings.view, settings.edit); see [RBAC_V2_ROLLOUT_PHASE4_FULL_COVERAGE](../RBAC_V2_ROLLOUT_PHASE4_FULL_COVERAGE.md).
