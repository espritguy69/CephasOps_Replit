# Security Surface Audit

**Purpose:** Platform security audit focused on multi-tenant SaaS exposure: anonymous and platform-admin endpoints, impersonation, support tooling, retry/replay, file access, and rate limiting.

---

## 1. [AllowAnonymous] endpoints

| Endpoint / area | Auth boundary | Tenant sensitivity | Current protection | Follow-up |
|-----------------|---------------|--------------------|--------------------|-----------|
| **POST /api/auth/login** | Anonymous (then JWT) | High (tenant established after login) | Login required for all tenant data; subscription/trial enforced after auth | Ensure login is rate-limited (e.g. lockout); consider CAPTCHA for public signup |
| **POST /api/platform/signup** (PlatformSignupController) | AllowAnonymous | High (creates tenant/company) | Self-service signup; validate email/code; provisioning creates tenant | Rate limit signup by IP; optional allowlist |
| **WebhooksController** (e.g. inbound webhooks) | AllowAnonymous | High (payload may target tenant) | Validate signature/secret per webhook; set tenant from payload | Ensure no tenant override from body without verification |
| **ExcelToPdfController** (e.g. one action) | AllowAnonymous on one action | Medium | Confirm use case (e.g. internal only or signed URL) | Restrict to internal or signed URLs if public |

---

## 2. SuperAdmin-only endpoints

| Area | Route | Protection | Notes |
|------|--------|------------|--------|
| Platform analytics | api/platform/analytics/* | [Authorize(Roles = "SuperAdmin")], RequirePermission(AdminTenantsView) | Dashboard, tenant-health, anomalies, drift, performance-health |
| Platform support | api/platform/support/* | [Authorize(Roles = "SuperAdmin")] | Diagnostics, impersonation, retry jobs, audit |
| Tenant provisioning | api/platform/tenants/* | [Authorize(Roles = "SuperAdmin")], RequirePermission(AdminTenantsEdit) | Provision, list, diagnostics, suspend/resume, subscription |

---

## 3. Impersonation

| Endpoint | Auth | Tenant sensitivity | Protection | Follow-up |
|----------|------|--------------------|------------|-----------|
| **POST /api/platform/support/impersonate** | SuperAdmin only | Critical (issues JWT as tenant admin) | Audit-logged; returns token for first Admin of tenant | Limit to support workflow; optional time-limited token |

---

## 4. Support tooling

| Endpoint / area | Auth | Tenant sensitivity | Protection |
|-----------------|------|--------------------|------------|
| Platform support (diagnostics, retry jobs) | SuperAdmin | High | All actions behind SuperAdmin; audit recommended |
| Admin roles / users (AdminUsersController, AdminRolesController) | SuperAdmin, Admin | High | Role-based; tenant context from current user |

---

## 5. Retry / replay endpoints

| Endpoint | Auth | Tenant sensitivity | Protection |
|----------|------|--------------------|------------|
| OperationalReplayController | Likely Admin/SuperAdmin | High (replay affects tenant events) | Ensure replay runs under explicit tenant scope |
| OperationalRebuildController | Same | High | Same |
| BackgroundJobsController / JobOrchestrationController (retry) | Admin/SuperAdmin | High | Retry only within tenant or by platform admin with audit |

---

## 6. Upload / download and file access

| Area | Auth | Tenant sensitivity | Protection |
|------|------|--------------------|------------|
| FilesController (upload/download) | Authenticated | High (files are tenant-scoped) | TenantGuardMiddleware + ITenantProvider; storage path by CompanyId |
| File/document access | Same | Same | Enforce tenant scope on read/write; no companyId override from client |

---

## 7. Public signup

| Endpoint | Auth | Tenant sensitivity | Protection |
|----------|------|--------------------|------------|
| POST /api/platform/signup (self-service) | AllowAnonymous | High | Validates code/email; creates tenant and admin; rate limit recommended |

---

## 8. Endpoints accepting companyId or tenantId

| Pattern | Risk | Mitigation |
|---------|------|------------|
| Platform routes (e.g. GET /api/platform/tenants/{tenantId}/diagnostics) | Client supplies tenantId | SuperAdmin only; tenantId is resource key, not override of current user |
| Tenant provisioning (body includes company/tenant data) | Creation only | No override of existing tenant; validation and uniqueness checks |
| Other controllers | Most use ITenantProvider | No client-supplied companyId/tenantId for scope; any body DTO with tenant/company is for creation or admin targeting |

---

## 9. Rate-limited vs non–rate-limited public endpoints

| Endpoint type | Rate limited | Notes |
|---------------|--------------|--------|
| Tenant-scoped API (after auth) | Yes (TenantRateLimitMiddleware, per tenant) | 100/min, 1000/hour default; plan overrides |
| Login | Per-account lockout (Auth) | No global rate limit on login endpoint; consider IP-based limit |
| Signup | Not per-tenant (no tenant yet) | Recommend IP or global signup rate limit |
| AllowAnonymous webhooks | No tenant rate limit | Webhook-specific secret/signature; optional IP limit |

---

## 10. Summary

- **Critical:** Impersonation is SuperAdmin-only and audit-logged; ensure audit is retained.
- **High:** AllowAnonymous surfaces (login, signup, webhooks, one ExcelToPdf action) are the main public attack surface; protect with rate limiting and validation.
- **Current protection:** TenantGuardMiddleware, TenantScopeExecutor, subscription enforcement, and SuperAdmin/Admin role checks are in place. No major redesign recommended; document and optionally harden rate limits on login/signup and review ExcelToPdf exposure.
