# SaaS Operations Hardening Report

**Date:** 2026-03-13

This report documents the SaaS operations hardening phase completed after the initial scaling phase. It covers default trial plan assurance, subscription admin APIs, storage usage tracking and enforcement, and related operational updates.

---

## 1. Summary

| Area | Change |
|------|--------|
| **Trial plan** | Default billing plan with slug `trial` is ensured at seed; provisioning without explicit PlanSlug always resolves. |
| **Subscription admin** | GET and PATCH endpoints for tenant subscription (plan, status, trial end, limits). SuperAdmin only. |
| **Storage tracking** | File uploads and deletes (FilesController / FileService) update tenant StorageBytes usage. |
| **Storage enforcement** | Uploads are blocked when tenant would exceed storage quota; API returns 403 with clear message. |
| **Docs** | This report plus updates to SAAS_SCALING_ARCHITECTURE.md and SAAS_OPERATIONS_GUIDE.md. |

---

## 2. Default trial billing plan

### 2.1 Behaviour

- **Where:** `DatabaseSeeder.EnsureDefaultTrialBillingPlanAsync()` (Infrastructure).
- **When:** Runs during normal database seed, immediately after migrations (before company/roles).
- **What:** If no `BillingPlan` with `Slug == "trial"` exists, inserts one with:
  - Name: `Trial`
  - Slug: `trial`
  - BillingCycle: Monthly
  - Price: 0, Currency: MYR
  - IsActive: true
- **Idempotent:** Only inserts when the row is missing; safe to run on every seed.
- **Provisioning:** `CompanyProvisioningService` uses `PlanSlug ?? "trial"` and falls back to first active plan if `trial` is missing; with this seed, `trial` always exists when seed has run.

### 2.2 Operational notes

- New environments: run the standard seed (e.g. app startup with seed enabled) so the trial plan exists.
- To add more plans (e.g. Starter, Pro), add them via migration or seed; do not remove the `trial` slug for evaluation tenants.

---

## 3. Subscription admin APIs

### 3.1 Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/platform/tenants/{tenantId}/subscription` | Get current subscription (active/trialing preferred, else latest). Returns 404 if none. |
| PATCH | `/api/platform/tenants/{tenantId}/subscription` | Update subscription; only provided fields are applied. |

### 3.2 PATCH body (all optional)

- **PlanSlug** – Billing plan slug (must exist and be active).
- **Status** – One of: Active, Trialing, Cancelled, PastDue.
- **TrialEndsAtUtc** – When trial ends (UTC).
- **NextBillingDateUtc** – Next billing date (UTC).
- **SeatLimit** – Max users; non‑negative or omit to keep current.
- **StorageLimitBytes** – Max storage in bytes; non‑negative or omit to keep current.

Validation: invalid plan slug or status returns 400 with a clear message. Only SuperAdmin (and required permission) can call these endpoints.

### 3.3 Implementation

- **Service:** `IPlatformAdminService.GetTenantSubscriptionAsync` and `UpdateTenantSubscriptionAsync`.
- **DTO:** `PlatformTenantSubscriptionUpdateRequest`; response uses existing `TenantSubscriptionDto` (audit‑friendly, includes plan slug and timestamps).

---

## 4. Storage usage and enforcement

### 4.1 Storage tracking

- **Metric:** `TenantUsageService.MetricKeys.StorageBytes` in **TenantUsageRecord** (current month bucket).
- **Increment:** After a successful file upload, `FileService` calls `ITenantUsageService.RecordStorageDeltaAsync(companyId, fileSizeBytes)`.
- **Decrement:** After a successful file delete, `FileService` calls `RecordStorageDeltaAsync(companyId, -fileSizeBytes)`. Total is clamped so it does not go below zero.
- **Resolve tenant:** Both `RecordStorageDeltaAsync` and `GetCurrentStorageBytesAsync` resolve `TenantId` from `CompanyId`; accounting is tenant‑scoped and does not double‑count across tenants.

### 4.2 Where it applies

- **FilesController** → **FileService.UploadFileAsync** / **DeleteFileAsync**: storage is updated and enforced here.
- **Email ingestion** and other flows that persist files via **IFileService.UploadFileAsync** are covered by the same logic.
- Parser uploads that do not go through FileService are not yet metered; add similar tracking if they persist long‑term storage.

### 4.3 Storage limit enforcement

- **Before upload:** `FileService.UploadFileAsync` calls `ISubscriptionEnforcementService.IsWithinStorageLimitAsync(tenantId, currentStorage + fileSize)`. If the tenant has a storage limit and would exceed it, the service throws `InvalidOperationException("Storage quota exceeded. Cannot upload file.")`.
- **API response:** FilesController catches that message and returns **403 Forbidden** with the same message (no 500).
- **When limits apply:** Only when the tenant has `StorageLimitBytes` set on their subscription; otherwise uploads are allowed. Enforcement and usage services are optional (null‑injected); if not registered, uploads still succeed and storage is not metered.

---

## 5. Operational caveats and support

- **Default trial plan:** Ensure seed has run at least once so the trial plan exists; otherwise provisioning without PlanSlug may fall back to “first active plan” or create no subscription.
- **Storage totals:** Stored per tenant per month; aggregation jobs (TenantMetricsDaily / TenantMetricsMonthly) use the same metric. For support, use platform usage APIs or query TenantUsageRecord / TenantMetricsDaily for StorageBytes.
- **Quota exceeded:** Tenant sees 403 with “Storage quota exceeded” from file upload; advise upgrading plan or increasing StorageLimitBytes via PATCH subscription.
- **Cross‑tenant:** Storage and enforcement are keyed by tenant; no cross‑tenant leakage.

---

## 6. Tests

- **Provisioning:** Existing `CompanyProvisioningServiceTests` cover provisioning with trial plan; seed in tests provides a plan with slug `trial`.
- **Subscription admin:** Platform admin subscription GET/PATCH can be tested via integration tests or by calling the endpoints with a SuperAdmin token.
- **Storage:** FileService behaviour (enforcement + recording) can be unit‑tested with mocked ITenantUsageService and ISubscriptionEnforcementService; within‑quota upload and over‑quota rejection should be covered.

---

## 7. References

- [SAAS_SCALING_ARCHITECTURE.md](SAAS_SCALING_ARCHITECTURE.md) – Overall scaling architecture.
- [SAAS_OPERATIONS_GUIDE.md](SAAS_OPERATIONS_GUIDE.md) – Day‑to‑day operations, endpoints, and support.
- [TENANT_PROVISIONING_FLOW.md](TENANT_PROVISIONING_FLOW.md) – Provisioning flow and trial behaviour.
