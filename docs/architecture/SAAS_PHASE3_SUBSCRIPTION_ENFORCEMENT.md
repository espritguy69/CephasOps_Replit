# SaaS Phase 3 — Subscription Enforcement

## Overview

Phase 3 ensures **tenant access and capability follow subscription status and tenant state**. Authenticated requests and background jobs are blocked or limited when the company is suspended/disabled, the subscription is cancelled/past-due/expired, or the tenant is in a non-operational state.

## State Model

### Company status (primary gate)

- **Active**, **Trial** → full access.
- **Suspended** → access denied (`tenant_suspended`).
- **Disabled** → access denied (`tenant_disabled`).
- **PendingProvisioning** → access denied (`tenant_pending_provisioning`).
- **Archived** → access denied (`tenant_archived`).

### Tenant subscription (when company has TenantId)

- **Active**, **Trialing** (and period not ended) → full access.
- **Cancelled** → access denied (`subscription_cancelled`).
- **PastDue** → read-only access (`subscription_past_due`); writes denied, reads allowed.
- Period end in the past → access denied (`subscription_expired`).

Legacy companies (no `TenantId`) or missing subscription records are treated as allowed to preserve existing behaviour.

## Enforcement Points

| Point | Behaviour |
|-------|-----------|
| **Login** | After resolving company, `GetAccessForCompanyAsync`; if not allowed, throw `TenantAccessDeniedException` → API returns **403** with `denialReason`. |
| **Refresh token** | Same check before issuing new tokens → **403** with `denialReason`. |
| **Request middleware** | After `UseAuthentication()` and `RequestLogContextMiddleware`: for authenticated requests (except auth paths), resolve company from user claims, call `GetAccessForCompanyAsync`; if not allowed, short-circuit with **403** and body `{ denialReason, readOnlyMode }`. |
| **Background jobs** | In `JobExecutionWorkerHostedService`, before executing a job with a valid `CompanyId`, call `GetAccessForCompanyAsync`; if not allowed, mark job failed (non-retryable) and skip execution. |

Auth paths that skip middleware enforcement: `/api/auth/login`, `/api/auth/refresh`, `/api/auth/forgot-password`, `/api/auth/change-password-required`, `/api/auth/reset-password-with-token`.

## Denial Reason Codes

- `tenant_suspended`, `tenant_disabled`, `tenant_pending_provisioning`, `tenant_archived`
- `subscription_cancelled`, `subscription_past_due`, `subscription_expired`
- `read_only_mode` (when in read-only due to past-due)
- Future: `feature_not_enabled`, `seat_limit_exceeded` (module entitlements / seat limits).

## Services and Types

- **SubscriptionAccessResult**: `Allowed`, `DenialReason`, `ReadOnlyMode`; static helpers `Allow()`, `Deny(reason)`, `ReadOnly(reason)`.
- **ISubscriptionAccessService** / **SubscriptionAccessService**: `GetAccessForCompanyAsync(companyId)`, `GetAccessForTenantAsync(tenantId)`, `CanPerformWritesAsync(companyId)`.
- **TenantAccessDeniedException**: thrown from auth when access is denied; carries `DenialReason`.
- **SubscriptionEnforcementMiddleware**: runs after authentication; returns 403 with JSON body when access is denied.

## Configuration

- `ISubscriptionAccessService` and `SubscriptionAccessService` are registered as scoped in `Program.cs`. Without registration, login/refresh and middleware treat subscription checks as no-ops (optional dependency).
- Middleware is inserted after `UseAuthentication()` and `RequestLogContextMiddleware()` so user and company context are available.

## Tests

- **SubscriptionAccessServiceTests** (`backend/tests/CephasOps.Application.Tests/Subscription/SubscriptionAccessServiceTests.cs`): unit tests for `GetAccessForCompanyAsync` (null/empty/unknown company, Active/Trial/Suspended/Disabled/PendingProvisioning/Archived, subscription Cancelled/PastDue/Expired) and `CanPerformWritesAsync`.
- **AuthServiceTests** (`backend/tests/CephasOps.Application.Tests/Auth/AuthServiceTests.cs`): `LoginAsync_WhenSubscriptionAccessDenied_ThrowsTenantAccessDeniedWithReason` and `RefreshTokenAsync_WhenSubscriptionAccessDenied_ThrowsTenantAccessDeniedWithReason` assert that when `ISubscriptionAccessService` returns Deny, auth throws `TenantAccessDeniedException` with the correct `DenialReason` (API then returns 403 with `denialReason`).

## Future Work

- **Module entitlements**: allow/deny by plan or feature flags (e.g. reports, workflows).
- **Seat limits**: enforce max users per tenant from subscription/plan.
- **Stricter read-only**: optionally deny all access (including reads) when subscription is past-due; currently reads are allowed.
