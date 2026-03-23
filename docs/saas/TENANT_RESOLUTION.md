# Tenant Resolution

**Date:** 2026-03-13  
**Purpose:** Describe how the effective tenant (company) context is determined for API requests, background jobs, events, and webhooks. Reflects ITenantProvider and TenantGuardMiddleware behaviour.

---

## 1. Request-Time Resolution (API)

Effective tenant is resolved **once per request** by **ITenantProvider.GetEffectiveCompanyIdAsync()**, invoked from **TenantGuardMiddleware**.

### Resolution order

1. **X-Company-Id header** — If present and valid GUID, and the user is authorised (e.g. SuperAdmin), this value is used as the effective company. Used for platform admins to act in a specific tenant context.
2. **JWT CompanyId** — From the authenticated user's token (user's company).
3. **Department → company fallback** — If JWT company is null/empty: resolve from the user's department memberships. If exactly one company, use it; if multiple distinct companies, leave unresolved; if none, leave unresolved.
4. **Unresolved** — No effective company; TenantGuardMiddleware returns 403 for routes that require tenant context.

Request-time consumers must use **ITenantProvider.CurrentTenantId** (or the value set in TenantScope by Program.cs after the guard). Controllers must **not** use a different source for tenant (e.g. body parameter companyId for scoping) unless the API is explicitly designed to accept a company and the caller is authorised (e.g. platform admin with X-Company-Id).

---

## 2. Where Scope Is Set (API Pipeline)

After routing, authentication, and TenantGuardMiddleware:

- **Program.cs** sets `TenantScope.CurrentTenantId = tenantProvider.CurrentTenantId` for the request.
- Scope is cleared in a **finally** block so it does not leak to the next request.

Controllers do not set TenantScope; middleware and pipeline do. Controllers that need the current tenant use **ITenantProvider** or **RequireCompanyId** (which uses the provider and returns 403 if missing).

---

## 3. Background Jobs

- Each **BackgroundJob** (and **JobExecution**) carries **CompanyId** (nullable for platform-wide jobs).
- **BackgroundJobProcessorService** (and job worker) runs each job under **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(job.CompanyId, ...)**.
- So the effective tenant for the job is **job.CompanyId**; if null/empty, the run is under platform bypass (intended only for platform-owned jobs).

---

## 4. Event Dispatch and Replay

- **EventStoreDispatcherHostedService** and **EventReplayService** use **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)** per event/replay entry.
- Effective tenant = **entry.CompanyId**; if null/empty, platform bypass for that entry.

---

## 5. Webhooks

- **InboundWebhookRuntime** uses **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(request.CompanyId, ...)**.
- Effective tenant = **request.CompanyId** (from webhook payload or routing); if null/empty, platform bypass for that request.

---

## 6. Auth Flows (Login, Refresh, Forgot Password)

- For paths that load or update **tenant-scoped** data (e.g. user record, refresh token): tenant scope is set from the resolved user's **CompanyId**, or platform bypass is used only where the operation is intentionally platform-wide (e.g. lookup by email across tenants for login).
- Restoration (e.g. try/finally) is required so scope does not leak.

---

## 7. No DefaultCompanyId for Resolution

**TenantOptions.DefaultCompanyId** may exist in configuration for legacy reasons; it is **not** used in **TenantProvider.GetEffectiveCompanyIdAsync()** for request-time resolution. Tenant is always resolved from X-Company-Id, JWT, or department membership.

---

*See: [TENANCY_MODEL.md](TENANCY_MODEL.md), [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md), backend [EF_TENANT_SCOPE_SAFETY.md](../../backend/docs/architecture/EF_TENANT_SCOPE_SAFETY.md).*
