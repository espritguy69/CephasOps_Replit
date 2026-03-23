# EF Tenant-Scope Safety Layer — Audit & Implementation Report

**Date:** 2026-03-12  
**Scope:** Audit of data-access patterns, choice of safety model, and implementation of EF-level tenant protection.

---

## 1. Verdict

**SAFE TO IMPLEMENT AND IMPLEMENTED**

Global query filters were already in place for all `CompanyScopedEntity` types, `User`, and `BackgroundJob`. Three additional entity types with `CompanyId` that did not inherit `CompanyScopedEntity` (JobExecution, OrderPayoutSnapshot, InboundWebhookReceipt) were given the same tenant filter. One explicit bypass was added for platform-wide retention (InboundWebhookReceipts). No schema changes, no second tenant-resolution path, no weakening of fail-closed behavior.

---

## 2. Audit Findings

### 2.1 Current tenant-scoped entity patterns

- **CompanyScopedEntity:** Abstract base in Domain with `CompanyId`, `IsDeleted`, timestamps, `RowVersion`. All entities inheriting from it receive a global query filter in `ApplicationDbContext` (soft delete + tenant). Over 90 entity types inherit it (Order, Invoice, Material, Department, etc.).
- **User and BackgroundJob:** Have `CompanyId` but do not inherit CompanyScopedEntity; each has an explicit tenant filter in DbContext (same rule: `TenantScope.CurrentTenantId == null || CompanyId == TenantScope.CurrentTenantId`).
- **TenantScope:** Static `AsyncLocal<Guid?> CurrentTenantId` in Infrastructure; set by API middleware from `ITenantProvider.CurrentTenantId` and by job worker from `job.CompanyId`. Not set during migrations or design-time.

### 2.2 Entities with CompanyId but no filter (pre-implementation)

- **JobExecution:** Queried via EF in JobExecutionQueryService, AdminService, and in MarkSucceeded/MarkFailed (after worker sets TenantScope). Claiming uses raw SQL (no filter). Without a filter, API/dashboard could see other tenants’ job executions.
- **OrderPayoutSnapshot:** Queried by OrderPayoutSnapshotService and PayoutAnomalyService. Without a filter, missing CompanyId in a query could expose other tenants’ snapshots.
- **InboundWebhookReceipt:** Queried by InboundWebhookReceiptStore and EventPlatformRetentionService. Without a filter, listing or lookup could cross tenants. Retention deletes old receipts platform-wide and must bypass the filter.

### 2.3 Current risks

- Reliance on service/controller discipline for the three types above; a forgotten `CompanyId` filter could leak data.
- EventStoreEntry has CompanyId but was not given a global filter (replay/rebuild semantics left to application layer).
- Some use of `IgnoreQueryFilters()` in production (OrderService for soft-deleted order, AssetService, EventPlatformRetentionService); all are documented or now documented.

### 2.4 Architectural constraints

- SuperAdmin X-Company-Id must remain correct (TenantScope set from ITenantProvider).
- Background worker claims jobs via raw SQL; execution runs with TenantScope set per job.
- Migrations and design-time must not apply tenant filter (TenantScope null).
- No domain model changes (no new interface required; filters applied in Infrastructure only).

---

## 3. Chosen Approach

**Global query filters only** for entities that have a direct `CompanyId` and are tenant-scoped by design:

- **Already present:** All CompanyScopedEntity types, User, BackgroundJob.
- **Added:** JobExecution, OrderPayoutSnapshot, InboundWebhookReceipt with the same rule: `TenantScope.CurrentTenantId == null || CompanyId == TenantScope.CurrentTenantId`.

**Why this fits CephasOps:**

- Single tenant source (TenantScope set from ITenantProvider or job.CompanyId); no duplicate resolution.
- Null TenantScope keeps migrations, design-time, and platform-wide jobs safe.
- SuperAdmin X-Company-Id is unchanged (middleware sets TenantScope from header).
- No SaveChanges validation or command interception; minimal surface area.
- EventStoreEntry left unfiltered to avoid impacting replay/rebuild without further design.

**Explicit bypass:** EventPlatformRetentionService uses `IgnoreQueryFilters()` on InboundWebhookReceipts for the retention delete so platform-wide cleanup still works.

---

## 4. Files Changed

| File | Change |
|------|--------|
| **backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs** | Added global query filter for JobExecution, OrderPayoutSnapshot, InboundWebhookReceipt (same tenant rule as User/BackgroundJob). |
| **backend/src/CephasOps.Application/Integration/EventPlatformRetentionService.cs** | Added `.IgnoreQueryFilters()` to InboundWebhookReceipts queries used for retention delete (platform-wide). |
| **docs/architecture/EF_TENANT_SCOPE_SAFETY.md** | New: documents which entities are filtered, which are not, tenant source, SuperAdmin behavior, missing-tenant behavior, bypasses, limitations. |
| **docs/architecture/EF_TENANT_SCOPE_SAFETY_REPORT.md** | New: this report. |

---

## 5. Safety Guarantees Added

- **JobExecution:** EF queries (e.g. AdminService dashboard, MarkSucceeded/MarkFailed) are tenant-scoped when TenantScope is set. Worker claiming remains raw SQL; execution runs with TenantScope set.
- **OrderPayoutSnapshot:** All EF reads are tenant-scoped when TenantScope is set; no accidental cross-tenant snapshot exposure.
- **InboundWebhookReceipt:** All EF reads (store lookups, listing) are tenant-scoped when TenantScope is set. Retention delete explicitly bypasses filter and is documented.

---

## 6. Limitations / Explicit Exclusions

- **EventStoreEntry** is not globally filtered; replay/rebuild and event-sourcing behavior are left to application logic. Optional future addition.
- **SaveChanges** does not validate or set CompanyId; existing service-level behavior is unchanged.
- **Admin job listing:** Dashboard/GetDiagnostics that query JobExecutions now see only the current tenant (or X-Company-Id company). If a platform-wide “all jobs” view is required, that path must use `IgnoreQueryFilters()` and be documented and access-controlled.
- **Defense-in-depth:** This layer does not replace RequireCompanyId() or service-level scoping; it reduces impact of missing filters only.

---

## 7. Final Recommendation

**Production-ready defense-in-depth.** The implementation is minimal, uses the existing tenant source, and does not break SuperAdmin, background jobs, or migrations. Documented in EF_TENANT_SCOPE_SAFETY.md. Recommend keeping EventStoreEntry and any future “tenant-optional” entities out of the global filter until their semantics are explicitly defined.
