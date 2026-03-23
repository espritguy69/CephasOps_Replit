# SaaS Remediation Changelog

**Date:** 2026-03-13  
**Purpose:** Record exact file-level changes from the SaaS migration remediation pass.

---

## Files changed

### 1. `backend/src/CephasOps.Application/Files/Services/IFileService.cs`

- **Problem:** `GetFileContentAsync(Guid fileId)` had no company parameter; when called outside a request (e.g. background job with no tenant scope), the global filter could allow any file (CurrentTenantId null → filter passes for all rows), risking cross-tenant file content read.
- **Fix:** Added optional parameter `Guid? companyId = null`. Signature is now `GetFileContentAsync(Guid fileId, Guid? companyId = null, CancellationToken cancellationToken = default)`. Documentation updated to state that when companyId is provided or when in tenant scope, only that company’s file is returned.

---

### 2. `backend/src/CephasOps.Application/Files/Services/FileService.cs`

- **Problem:** Same as above; implementation did not scope the file lookup by company when tenant scope was absent.
- **Fix:** Resolve effective company as `companyId ?? TenantScope.CurrentTenantId`. If effective company is null or Guid.Empty, log warning and return null. Otherwise query with `f.Id == fileId && f.CompanyId == effectiveCompanyId.Value`.

---

### 3. `backend/src/CephasOps.Api/HostedServices/TenantMetricsAggregationHostedService.cs`

- **Problem:** TenantMetricsAggregationJob ran without TenantScopeExecutor. Metrics entities are not in TenantSafetyGuard’s tenant-scoped list, but platform-wide aggregation should be explicit and future-proof.
- **Fix:** Wrapped the entire loop body (daily and monthly aggregation) in `TenantScopeExecutor.RunWithPlatformBypassAsync`. Added `using CephasOps.Infrastructure.Persistence` for TenantScopeExecutor. Summary comment updated to state that the job is platform-wide.

---

### 4. `backend/src/CephasOps.Application/Parser/Services/ParserReplayService.cs`

- **Problem:** Called `GetFileContentAsync(attachment.FileId.Value)` without company, which could be unsafe if run without tenant scope.
- **Fix:** Pass `attachment.CompanyId` as the second argument to `GetFileContentAsync`.

---

### 5. `backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs`

- **Problem:** Called `GetFileContentAsync(storedFile.Id)` without company.
- **Fix:** Pass `storedFile.CompanyId` as the second argument to `GetFileContentAsync`.

---

### 6. `backend/src/CephasOps.Api/Controllers/BillingController.cs`

- **Problem:** Called `GetFileContentAsync(generated.FileId)` without company when generating invoice PDF.
- **Fix:** Pass `companyId` (already in scope) as the second argument to `GetFileContentAsync`.

---

### 7. `backend/src/CephasOps.Application/Orders/Services/OrderService.cs`

- **Problem:** Comment for GetOrderByIdAsync said “SuperAdmin can access all companies (companyId is null)” but when companyId is null the global query filter still applies (current tenant from middleware).
- **Fix:** Comment updated to: “When companyId is provided, filter explicitly. When null (e.g. SuperAdmin), global query filter still applies (TenantScope.CurrentTenantId from middleware/X-Company-Id or JWT).”

---

### 8. `docs/remediation/SAAS_REMEDIATION_AUDIT.md` (new)

- **Purpose:** Audit of tenant resolution, EF/IgnoreQueryFilters, writes, file lookup, background jobs, dashboard, cache, raw SQL, uniqueness, and frontend. Severity and remediation per area; explicit list of platform bypasses and tenant-corrected areas.

---

### 9. `docs/remediation/SAAS_REMEDIATION_VERIFICATION.md` (new)

- **Purpose:** What was checked, what was fixed, what remains uncertain, manual QA checklist, and explicit lists of platform-wide bypasses retained and tenant-owned areas corrected.

---

### 10. `docs/remediation/SAAS_REMEDIATION_CHANGELOG.md` (this file)

- **Purpose:** File-level changelog for the remediation pass.

---

## Second-pass remediation (2026-03-13)

### Files changed in second pass

- **IFileService.cs:** GetFileInfoAsync had no company parameter; could return file metadata for any tenant when TenantScope was null. Added optional `Guid? companyId = null`; doc updated.
- **FileService.cs:** GetFileInfoAsync queried by fileId only. Resolve effective company from `companyId ?? TenantScope.CurrentTenantId`; return null when empty; query with `f.Id == fileId && f.CompanyId == effectiveCompanyId.Value`.
- **CarboneRenderer.cs:** Call updated to `GetFileInfoAsync(templateFileId, null, cancellationToken)` so the new optional companyId (null = use TenantScope) is passed correctly.
- **SAAS_REMEDIATION_VERIFICATION.md:** Added "Second-pass remediation findings": fixed now (GetFileInfoAsync), safe to defer (DiagnosticsController, RatesController CEPHAS004, idempotency), manual QA required. Updated file-access and tenant-corrected sections.
- **SAAS_REMEDIATION_CHANGELOG.md:** Added this second-pass section; updated follow-ups (GetFileInfoAsync done).

### Second-pass: fixed now

- **GetFileInfoAsync:** Tenant-safe file metadata; no metadata returned without effective company (parameter or TenantScope).

### Second-pass: safe to defer

- **DiagnosticsController check-seeding:** Platform diagnostic; queries run with or without tenant scope by design.
- **RatesController CEPHAS004:** FindAsync is subject to global query filter; explicit CompanyId in every lookup deferred as defense-in-depth.
- **Idempotency key:** Addressed in "Idempotency tenant-safety remediation" (below).

### Second-pass: manual QA required

- Order detail and list (tenant-scoped load); Rates list/detail; File list and download; Settings pages (companyId fallback); Reports and exports.

---

## Follow-up recommendations

1. **Idempotency:** Consider including tenant (or company) in idempotency key or store when in tenant context to avoid cross-tenant result reuse.  
2. **RatesController (optional):** Add explicit CompanyId to FindAsync lookups for defense-in-depth and to clear CEPHAS004. (GetFileInfoAsync was done in second pass.)  
3. **Frontend:** Manual QA on high-value tenant pages (orders, dashboards, reports, file lists) to confirm no blank or wrong-totals after SaaS migration.  
4. **Tests:** Add or extend tests for GetFileContentAsync/GetFileInfoAsync with null company and without tenant scope (expect null); and for TenantMetricsAggregationHostedService running under platform bypass (no guard throw).

---

## Manual QA–driven fixes (2026-03-13)

**Issue reproduced:** Orders list and paged orders list were not explicitly scoped by current tenant in the controller; they relied only on the EF global query filter (and for paged, companyId was set only when profitability/alert flags were true).

**Root cause:** Backend. `OrdersController.GetOrders` used `companyId = (Guid?)null` (legacy "Company feature removed" comment). `OrdersController.GetOrdersPaged` used `companyId = (includeProfitability || ...) ? _tenantProvider.CurrentTenantId : null`, so without those flags the query had no explicit company.

**Files changed:**

- **OrdersController.cs**
  - GetOrders: replaced `var companyId = (Guid?)null` with `var companyId = _tenantProvider.CurrentTenantId` and updated comment to "SaaS: scope orders list by current tenant (JWT or X-Company-Id for SuperAdmin)".
  - GetOrdersPaged: replaced conditional companyId with `var companyId = _tenantProvider.CurrentTenantId` always, with comment "SaaS: always scope paged list by current tenant; profitability/alert flags only control enrichment".

**Fix applied:** Both endpoints now pass current tenant to the order service so the list is explicitly tenant-scoped in the application layer; SuperAdmin company switch (X-Company-Id) correctly restricts orders to the selected company.

---

## Manual QA–driven fixes: rates detail, settings/SuperAdmin (2026-03-13)

**1. Rates detail explicit tenant scoping**

- **Symptom:** GET /api/rates/ratecards/{id} was queried by Id only (no explicit company filter).
- **Root cause:** Backend. GetRateCard did not use RequireCompanyId or filter by CompanyId.
- **Files changed:** `RatesController.cs` — added RequireCompanyId; query changed to `.Where(rc => rc.Id == id && rc.CompanyId == companyId).FirstOrDefaultAsync(...)`.
- **Fix applied:** Rate card detail is explicitly tenant-scoped; 404 when id is for another tenant.

**2. SuperAdmin company switch consistency (X-Company-Id)**

- **Symptom:** SuperAdmin company switch (e.g. by department) did not affect settings, rates, files, reports because the frontend never sent X-Company-Id.
- **Root cause:** Frontend. API client had no way to send X-Company-Id; backend already supports it for SuperAdmin.
- **Files changed:**
  - `frontend/src/api/client.ts`: Added `getCompanyIdFn` and `setCompanyIdGetter(fn)`; `buildHeaders` now adds `X-Company-Id` when the getter returns a value.
  - `frontend/src/contexts/DepartmentContext.tsx`: Import `setCompanyIdGetter`; added `companyIdRef` updated from `activeDepartment?.companyId ?? departments[0]?.companyId`; in useEffect set getter to return `companyIdRef.current` and clear on unmount.
- **Fix applied:** When the user has an active department (or at least one department), the effective company ID is sent as X-Company-Id on every request. Backend TenantProvider uses it for SuperAdmin, so settings, rates, files, and reports now respond to the same effective company (e.g. department selection).

---

## Idempotency tenant-safety remediation (2026-03-13)

**Goal:** Ensure idempotency keys are tenant-safe so the same logical key in different tenants does not reuse the same result or claim.

### Audit summary

| Area | Operation | Tenant/Platform | Change |
|------|-----------|----------------|--------|
| Command pipeline | CommandProcessingLog (IdempotencyBehavior) | Tenant when TenantScope set; platform when null | Key prefixed with `{CompanyId:N}:` when in tenant scope |
| Inbound webhooks | ExternalIdempotencyRecord (IExternalIdempotencyStore) | Tenant-owned (request.CompanyId) or platform (null) | Lookup/claim/completion scoped by (ConnectorKey, CompanyId, IdempotencyKey); unique index updated |
| Outbound delivery | OutboundIntegrationDelivery idempotency key | Per-event (eventId globally unique) | No change; key already unique per event |
| Notification dispatch | NotificationDispatch IdempotencyKey | Per-notification / per-event (GUIDs) | No change; keys use globally unique IDs |
| Event processing log | EventProcessingLog (EventId, HandlerName) | Per-event (EventId globally unique) | No change |

### 1. Command idempotency (CommandProcessingLog)

- **Problem:** Idempotency key was stored as provided; the same key from two tenants could claim or reuse the same row, allowing cross-tenant result reuse.
- **Fix:** In `IdempotencyBehavior` (Application layer), when `TenantScope.CurrentTenantId` has a value, the key passed to the store is prefixed: `{CompanyId:N}:{rawKey}`. Platform-wide operations (no tenant scope) use the raw key unchanged.
- **Backward compatibility:** Existing rows in CommandProcessingLogs have raw keys (no prefix). After deploy, tenant requests use prefixed keys (new rows). Platform requests continue to use raw keys and still match existing platform rows. No schema change; key format only.
- **Files changed:**
  - `backend/src/CephasOps.Application/Commands/Pipeline/IdempotencyBehavior.cs`: Injected `TenantScope` (Infrastructure); build `idempotencyKey` as prefixed when `TenantScope.CurrentTenantId` is set, else use `rawKey`; pass to store unchanged (store interface unchanged).

### 2. External idempotency (inbound webhooks)

- **Problem:** ExternalIdempotencyRecords had a unique index on `IdempotencyKey` only; same external key from two tenants (or same connector, different company) could collide.
- **Fix:** Idempotency is now scoped by (ConnectorKey, CompanyId, IdempotencyKey). TryClaimAsync, MarkCompletedAsync, and IsCompletedAsync all filter by these three; unique index changed from `IdempotencyKey` to (ConnectorKey, CompanyId, IdempotencyKey).
- **Backward compatibility:** Existing rows retain ConnectorKey and CompanyId; the new unique index allows one row per (ConnectorKey, CompanyId, IdempotencyKey). Platform webhooks (CompanyId null) remain one row per (ConnectorKey, IdempotencyKey). Migration drops `IX_ExternalIdempotencyRecords_IdempotencyKey` and creates composite unique index.
- **Files changed:**
  - `backend/src/CephasOps.Application/Integration/IExternalIdempotencyStore.cs`: MarkCompletedAsync and IsCompletedAsync now take `connectorKey` and `companyId`.
  - `backend/src/CephasOps.Application/Integration/ExternalIdempotencyStore.cs`: TryClaimAsync exists check and insert use (IdempotencyKey, ConnectorKey, CompanyId); MarkCompletedAsync and IsCompletedAsync look up by (IdempotencyKey, ConnectorKey, CompanyId).
  - `backend/src/CephasOps.Application/Integration/InboundWebhookRuntime.cs`: Pass `request.ConnectorKey` and `request.CompanyId` to IsCompletedAsync and MarkCompletedAsync.
  - `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Integration/ExternalIdempotencyRecordConfiguration.cs`: Replaced unique index on IdempotencyKey with unique index on (ConnectorKey, CompanyId, IdempotencyKey).
  - `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260313115405_ExternalIdempotencyTenantScopeUniqueIndex.cs`: Migration drops old index, creates new composite unique index (note: this migration may include other pending model changes; the ExternalIdempotencyRecords index change is the one required for this remediation).
  - `backend/tests/CephasOps.Application.Tests/Integration/InboundWebhookRuntimeTenantScopeTests.cs`: Mock setups updated for new IExternalIdempotencyStore signatures (IsCompletedAsync, MarkCompletedAsync with connectorKey and companyId).

### 3. Unchanged (no action)

- **OutboundIntegrationBus:** Idempotency key is `out-{eventId:N}-{endpoint.Id:N}`; EventId is globally unique, so no tenant collision.
- **NotificationDispatch:** Keys use notification Id or sourceEventId (GUIDs); globally unique.
- **EventProcessingLog:** Key is (EventId, HandlerName); EventId globally unique.

### Follow-up removed

- Idempotency follow-up from second-pass "safe to defer" is addressed by this remediation.

---

## Frontend tenant-boundary audit (2026-03-13)

**Purpose:** Full frontend audit for multi-tenant SaaS; minimal remediation only for confirmed tenant-boundary risks.

**Doc:** `docs/remediation/FRONTEND_TENANT_BOUNDARY_AUDIT_2026-03-13.md`

### Files changed

- **frontend/src/contexts/DepartmentContext.tsx**
  - On department switch (`activeDepartmentId` change), call `queryClient.invalidateQueries()` so cached data from the previous tenant is not shown. Uses `useQueryClient` and a ref to avoid invalidating on initial load.
- **frontend/src/api/parser.ts**
  - Parser upload and parser logs export now read department from `localStorage.getItem('cephasops.activeDepartmentId')` instead of `'activeDepartmentId'`, matching DepartmentContext so the correct tenant context is sent.

### Summary

- **Finding 1 (high):** React Query cache was not invalidated on department/company switch; list/detail data for buildings, assets, warehouses, etc. could show previous tenant data. Fixed by invalidating all queries when `activeDepartmentId` changes.
- **Finding 2 (medium):** Parser used wrong localStorage key for department ID; upload and export could use wrong or stale tenant. Fixed by using `cephasops.activeDepartmentId`.
- No backend API changes. Verdict: frontend tenant boundaries verified across reviewed surfaces; no critical/high leak left unfixed.
