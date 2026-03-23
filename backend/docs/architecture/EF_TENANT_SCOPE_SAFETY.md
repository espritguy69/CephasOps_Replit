# EF Core Tenant Scope and SaveChanges Safety

**Purpose:** Describes how tenant isolation is enforced at the persistence layer: TenantScope, global query filters, TenantSafetyGuard (SaveChanges validation and AssertTenantContext), and platform bypass rules.

**For day-to-day guidance (when to set scope, bypass rules, try/finally, PR checklist, test run), see [TENANT_SAFETY_DEVELOPER_GUIDE.md](TENANT_SAFETY_DEVELOPER_GUIDE.md).**

---

## Canonical tenant source (request-time)

Effective tenant is resolved once per request by **ITenantProvider.GetEffectiveCompanyIdAsync()** (called from TenantGuardMiddleware). Resolution precedence:

1. **X-Company-Id** — SuperAdmin override when header is present and valid GUID.
2. **JWT CompanyId** — From the authenticated user’s token.
3. **Department → Company fallback** — When JWT company is null/empty: resolve from the user’s department memberships. If exactly one company, use it; if multiple distinct companies, leave unresolved; if none, leave unresolved.
4. **Unresolved** — No effective company; guard returns 403 for tenant-required endpoints.

Request-time consumers must use **ITenantProvider.CurrentTenantId** (or the value set in TenantScope). Direct use of **CurrentUser.CompanyId** or **ICurrentUserService.CompanyId** for tenant logic is prohibited except inside TenantProvider (JWT step) and login-time auth (AuthService).

---

## TenantScope

- **Type:** `TenantScope` (static, AsyncLocal&lt;Guid?&gt;).
- **Role:** Holds the resolved tenant id for the current async context (request or background job). Used by EF global query filters and by TenantSafetyGuard.
- **Set by:** API middleware (from ITenantProvider after TenantGuardMiddleware) and by JobExecutionWorkerHostedService (from `job.CompanyId`) before executing each job.
- **Read by:** ApplicationDbContext (query filters), TenantSafetyGuard (SaveChanges validation and AssertTenantContext).

---

## Global query filters

- **CompanyScopedEntity** and all derived types: filter by `TenantScope.CurrentTenantId` (or allow when null for design-time/migrations).
- **User, BackgroundJob, JobExecution, OrderPayoutSnapshot, InboundWebhookReceipt:** Same pattern (tenant id null or equals entity CompanyId).

When TenantScope.CurrentTenantId is null, filters allow all rows (for design-time and platform bypass). When set, only rows for that tenant are returned.

---

## TenantSafetyGuard

### SaveChangesAsync validation

In **ApplicationDbContext.SaveChangesAsync** (before calling base.SaveChangesAsync):

- If **TenantSafetyGuard.IsPlatformBypassActive** is true → skip validation.
- Otherwise, read **TenantScope.CurrentTenantId**. If it is null or Guid.Empty:
  - For each **Added**, **Modified**, or **Deleted** entity where **TenantSafetyGuard.IsTenantScopedEntityType(entry.Entity.GetType())** is true, throw **InvalidOperationException**.

So: saving tenant-scoped entities without tenant context is blocked unless platform bypass is active.

### Tenant-scoped entity types (IsTenantScopedEntityType)

- **CompanyScopedEntity** (and all derived types).
- **User**.
- **BackgroundJob**.
- **JobExecution**.
- **OrderPayoutSnapshot**.
- **InboundWebhookReceipt**.

These match the entity types that have tenant query filters in ApplicationDbContext.

### AssertTenantContext()

- **Use:** Before high-risk paths such as **IgnoreQueryFilters()** on tenant-scoped data.
- **Behavior:** If platform bypass is active, no-op. Otherwise, if **TenantScope.CurrentTenantId** is null or Guid.Empty, throws **InvalidOperationException**.
- **Rule:** Any production code that uses IgnoreQueryFilters on tenant-relevant data must either call **TenantSafetyGuard.AssertTenantContext()** immediately before, or run inside a documented platform bypass.

---

## Platform bypass rules

Platform bypass disables SaveChanges tenant validation and allows AssertTenantContext to pass without tenant set. Use only for:

1. **EventPlatformRetentionService** — Retention cleanup across tenants; uses EnterPlatformBypass at start, ExitPlatformBypass in finally.
2. **DatabaseSeeder** — Seeding; uses EnterPlatformBypass before seeding, ExitPlatformBypass in finally.
3. **ApplicationDbContextFactory** — Design-time only; EnterPlatformBypass in CreateDbContext; no Exit (process exits after operation).

**Rule:** EnterPlatformBypass must be paired with ExitPlatformBypass in a finally block except in short-lived design-time factory. For the full list of allowed bypasses and the try/finally restoration pattern, see [TENANT_SAFETY_DEVELOPER_GUIDE.md](TENANT_SAFETY_DEVELOPER_GUIDE.md) Sections 2 and 3.

---

## Summary

| Component | Role |
|-----------|------|
| ITenantProvider | Canonical request-time tenant resolution (X-Company-Id, JWT, department fallback). |
| TenantScope | Holds resolved tenant for the current async context; used by EF filters and TenantSafetyGuard. |
| Global query filters | Restrict reads to TenantScope.CurrentTenantId (or allow all when null). |
| TenantSafetyGuard (SaveChanges) | Blocks saving tenant-scoped entities when tenant context is missing, unless platform bypass is active. |
| TenantSafetyGuard (AssertTenantContext) | Required before IgnoreQueryFilters on tenant data unless in platform bypass. |
| Platform bypass | EventPlatformRetentionService, DatabaseSeeder, ApplicationDbContextFactory (design-time) only. |

See also: **TENANT_SAFETY_DEVELOPER_GUIDE.md** (primary entry point for when/how and tests), **TENANT_GUARD_AUDIT_REPORT.md**, **TENANT_RESOLUTION_AUDIT_REPORT.md**, **operations/TENANT_SAFETY_FINAL_VERIFICATION.md**. For an index of all platform safeguards (tenant, financial, EventStore, observability, SI workflow), see **operations/PLATFORM_SAFETY_HARDENING_INDEX.md**.
