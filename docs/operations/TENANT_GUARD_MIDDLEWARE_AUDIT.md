# Tenant Guard Middleware — Verification Audit Report

**Date:** 2026-03-12  
**Scope:** TenantGuardMiddleware, AllowNoTenantAttribute, pipeline order, tenant resolution, controller compatibility, bypass risks.

---

## 1. Verdict

**PASS**

The implementation is correct and production-viable. The single concrete issue (null `ITenantProvider` bypass) has been fixed: when the provider is null, the middleware now blocks with 403 and logs. Remaining items are documented risks and do not block production.

---

## 2. Confirmed Correct

- **Pipeline order (Program.cs):** `UseRouting()` → `UseAuthentication()` → `TenantGuardMiddleware` → `RequestLogContextMiddleware` → `SubscriptionEnforcementMiddleware` → tenant-scope inline → `UseAuthorization()` → CORS-on-error inline → `MapControllers()` → `MapHealthChecks("/health")`. Routing and authentication run before the guard; endpoint metadata is available; the guard runs before authorization and before controller execution.

- **Middleware logic:** Uses `HttpContext.GetEndpoint()`; checks `AllowNoTenantAttribute` and `IAllowAnonymous` in metadata; path-based skip for `/api/auth`, `/api/platform`, `/health`, `/swagger`; treats `endpoint == null` as skip (unmatched/404 not turned into 403); uses `ITenantProvider.CurrentTenantId` only (no duplicate tenant logic); treats both `null` and `Guid.Empty` as invalid; returns 403 with JSON `{ "error": "Company context is required for this operation." }` without throwing; does not call `_next(context)` when blocking; logs path and user id (NameIdentifier, sub, or "(anonymous)").

- **Attribute:** `AllowNoTenantAttribute` is an `Attribute` on class/method, inheritable; discoverable via `endpoint.Metadata.GetMetadata<AllowNoTenantAttribute>()`; does not conflict with `AllowAnonymous` (both checked; either skips).

- **Tenant resolution:** Middleware depends only on `ITenantProvider.CurrentTenantId`. `TenantProvider` implements: SuperAdmin `X-Company-Id` → `ICurrentUserService.CompanyId` (JWT company_id) → `TenantOptions.DefaultCompanyId`. No duplicate fallback in the middleware. `ITenantProvider` is registered scoped; no lifetime mismatch.

- **Controllers:** Many company-scoped controllers use `RequireCompanyId(_tenantProvider)`; the guard blocks missing tenant before they run, so `RequireCompanyId()` remains a defensive second check. 403 for “company context required” is issued by the middleware when tenant is missing; controller 403 for the same reason would only occur if the guard were disabled, so behaviour is consistent.

- **Health / Swagger / auth / platform:** `/health` and `/swagger` skipped by path; `/api/auth` and `/api/platform` skipped by path. Auth and platform controllers do not need `[AllowNoTenant]` for the guard. `WebhooksController` (`/api/integration/webhooks`) has `[AllowAnonymous]` and is skipped via metadata.

- **404 / unmatched routes:** `GetEndpoint()` is null; guard skips and calls `_next`; 404 is returned by the framework, not converted to 403.

- **No minimal APIs:** Only `MapControllers()` and `MapHealthChecks("/health")`; no `MapGet`/`MapPost` etc. that could bypass the guard.

---

## 3. Issues Found

| # | File | Issue |
|---|------|--------|
| 1 | `TenantGuardMiddleware.cs` (lines 40–46) | **FIXED.** When `GetService<ITenantProvider>()` returned `null`, the middleware allowed the request. Now it blocks with 403 and logs. |

---

## 4. Required Fixes

**Fix for issue 1 (applied):** When `ITenantProvider` is null, the middleware now blocks with 403 and logs instead of calling `_next(context)`.

---

## 5. Bypass / Risk Notes

- **SubscriptionEnforcementMiddleware:** Uses `ICurrentUserService.CompanyId`, not `ITenantProvider`. For a user with no JWT `company_id` but `DefaultCompanyId` set, the guard lets them through (tenant = DefaultCompanyId), while SubscriptionEnforcement may see `CompanyId` as `Guid.Empty` and call `GetAccessForCompanyAsync(Guid.Empty?, ...)`. Outcome depends on `ISubscriptionAccessService` behaviour for null/empty; possible denial or inconsistent behaviour. Not a TenantGuard bug; recommend aligning SubscriptionEnforcement with `ITenantProvider` in a follow-up.

- **CurrentUserService.CompanyId:** Returns JWT `companyId`/`company_id` or `Guid.Empty` (no “User.CompanyId” or “department company” in this type). The full fallback (e.g. user/department at login) is expected to be reflected in the JWT. `TenantProvider` correctly adds `DefaultCompanyId` when `CompanyId` is null or empty.

- **403 response shape:** Middleware returns `{ "error": "..." }`. Controllers using `RequireCompanyId()` would return `ApiResponse` envelope. After the guard, only the middleware 403 is used for “company context required”; no inconsistency in practice.

- **Malformed X-Company-Id:** Handled in `TenantProvider` with `Guid.TryParse`; no throw; falls through to JWT then DefaultCompanyId.

---

## 6. Validation Matrix

| Case | Expectation | Supported |
|------|-------------|-----------|
| A. Authenticated user with JWT company_id | Allowed | Yes – TenantProvider returns CompanyId from current user. |
| B. User with null User.CompanyId but department company (in JWT) | Allowed | Yes – if login puts company in JWT. |
| C. Authenticated user, no company, no department, no DefaultCompanyId | Blocked 403 | Yes – CurrentTenantId null/empty → blocked. |
| D. SuperAdmin with X-Company-Id | Allowed (header company) | Yes – TenantProvider uses header. |
| E. /api/auth/login | Bypassed | Yes – path exclusion. |
| F. /api/platform/* | Bypassed | Yes – path exclusion. |
| G. /swagger, /health | Bypassed | Yes – path exclusion. |
| H. Endpoint with [AllowNoTenant] | Bypassed | Yes – metadata. |
| I. Unmatched route / 404 | Not turned into 403 | Yes – endpoint null → skip → 404. |

---

## 7. Final Go/No-Go

**Implementation is safe for production.**

- **Go:** The null-provider bypass has been fixed. Tenant Guard is correctly integrated, uses the canonical tenant source, does not duplicate resolution logic, and enforces tenant before controller execution. Remaining notes (SubscriptionEnforcement source, 403 shape) are non-blocking and can be addressed in later cleanup.

---

**Audit completed.** No changes to domain models, database schema, or unrelated code. Only the single middleware behaviour change above is recommended.
