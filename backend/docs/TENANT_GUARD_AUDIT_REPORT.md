# Tenant Guard Middleware Verification Audit Report

**Date:** 2026-03-12  
**Scope:** Tenant enforcement layer (TenantGuardMiddleware, AllowNoTenantAttribute, pipeline order, ITenantProvider integration, controller compatibility, bypass risks).

---

## Tenant resolution flow (request-time)

Effective company is resolved once per request via `ITenantProvider.GetEffectiveCompanyIdAsync()` (called by TenantGuardMiddleware). All consumers (guard, SubscriptionEnforcementMiddleware, TenantContextService, controllers) use the same cached result via `CurrentTenantId`.

**IMPORTANT:** All tenant-aware services MUST use `ITenantProvider` and MUST NOT read `CurrentUser.CompanyId` (or `ICurrentUserService.CompanyId`) directly.

**Precedence:**

1. **X-Company-Id** — If the user is SuperAdmin and the header is present and a valid non-empty GUID, that company is used.
2. **JWT company_id** — If `CurrentUser.CompanyId` from the JWT is present and non-empty, that company is used.
3. **Department → Company fallback** — Only when JWT company is missing: resolve company from the user’s department memberships. If the user has departments in **exactly one** company, that company is used; if **multiple distinct** companies, resolution is treated as ambiguous and left unresolved; if **none**, left unresolved.
4. **Unresolved** — No effective company; guard returns 403 for tenant-required endpoints.

**Intentional limitations:**

- Login-time resolution (AuthService.ResolveUserCompanyIdAsync) is unchanged and remains the source of JWT `company_id`; request-time fallback does not override it.
- Department fallback is **fallback-only**: it is used only when JWT company is null/empty; it never overrides JWT or X-Company-Id.
- DefaultCompanyId is **not** part of this canonical path; guard and subscription use the above precedence only. Legacy/bootstrap use of DefaultCompanyId elsewhere is unchanged.
- Ambiguous multi-company department membership is rejected (unresolved) and logged; no company is inferred.
- When department fallback is used, an informational log is written with UserId, CompanyId, Path, and resolution source `DepartmentFallback`.

---

## 1. Verdict

**PASS**

The implementation is correct and complete for the current codebase. The middleware fails closed when `ITenantProvider` is null (returns 403 and logs). Request-time department → company fallback is implemented in `ITenantProvider.GetEffectiveCompanyIdAsync` and used by the guard so that authenticated users with null JWT company but a single department company can pass the guard.

---

## 2. Confirmed Correct

- **Pipeline order (Program.cs 614–648):** `UseRouting()` runs before `TenantGuardMiddleware`; `UseAuthentication()` runs before the guard; `TenantGuardMiddleware` runs before `UseAuthorization()` and before `MapControllers()`. Protected controller code cannot run before tenant validation.

- **Middleware logic (TenantGuardMiddleware.cs):**
  - Reads `HttpContext.GetEndpoint()` and checks path before endpoint (path exclusions apply even when endpoint is null for health/swagger).
  - Checks `endpoint.Metadata.GetMetadata<AllowNoTenantAttribute>()` and `GetMetadata<IAllowAnonymous>()`.
  - Path-based exclusions: `/api/auth`, `/api/platform`, `/health`, `/swagger` via `PathString.StartsWithSegments(..., StringComparison.OrdinalIgnoreCase)`.
  - Uses `ITenantProvider.CurrentTenantId` only; no duplicate fallback chain in the middleware.
  - Treats both `null` and `Guid.Empty` as invalid (`tenantId.HasValue && tenantId.Value != Guid.Empty`).
  - Returns 403 with JSON `{ "error": "Company context is required for this operation." }` without throwing.
  - Does not call `_next(context)` when blocking; pipeline stops.
  - Logs blocked requests with path and user id (NameIdentifier / sub / "(anonymous)") via structured logging.

- **Edge cases:**
  - `endpoint == null` (unmatched routes): skip validation → request continues → 404 from routing; not converted to tenant 403.
  - Swagger: path `/swagger` and subpaths excluded.
  - Health: path `/health` excluded; `MapHealthChecks("/health")` matches.
  - Auth controller at `[Route("api/auth")]`; platform at `api/platform/tenants`, `api/platform/usage`; all excluded by path.

- **AllowNoTenantAttribute:** Simple attribute, `[AttributeUsage(Class | Method, Inherited = true)]`; discoverable via `endpoint.Metadata.GetMetadata<AllowNoTenantAttribute>()`; usable on controller or action; no conflict with `AllowAnonymous`.

- **ITenantProvider integration:** Middleware calls `await tenantProvider.GetEffectiveCompanyIdAsync(context.RequestAborted)` then reads `tenantProvider.CurrentTenantId`. `TenantProvider` (Api.Services) implements the canonical precedence: X-Company-Id (SuperAdmin only), JWT company, Department→Company fallback, then unresolved (no DefaultCompanyId in this path). Registered as Scoped in Program.cs; single resolution path for all consumers.

- **Controller compatibility:** Controllers using `RequireCompanyId(_tenantProvider)` receive the same `ITenantProvider`; middleware blocks missing tenant before the controller runs. `RequireCompanyId()` returns the same message and 403; behavior is consistent. No contradictory enforcement.

- **No minimal APIs:** Only `MapControllers()` and `MapHealthChecks("/health")`; no `MapGet`/`MapPost`/`MapGroup` that could bypass the guard.

- **RequestLogContextMiddleware:** Runs after the guard; only enriches Serilog context (CompanyId, UserId, etc.); no tenant-sensitive execution before the guard.

- **SubscriptionEnforcementMiddleware:** Runs after the guard; uses **ITenantProvider.CurrentTenantId** for subscription check (aligned with guard); ordering is correct (guard first).

---

## 3. Issues Found

None. The middleware already blocks with 403 when `ITenantProvider` is null (TenantGuardMiddleware.cs lines 41–50: LogError, set 403, write JSON, return without calling _next). No open defect.

---

## 4. Required Fixes

None. Current code is fail-closed when the tenant provider is missing.

---

## 5. Bypass / Risk Notes

- **Null ITenantProvider:** Already handled; middleware returns 403 and does not call `_next` when provider is null.
- **Canonical request-time chain:** At request time the chain is: **X-Company-Id (SuperAdmin only)** → **JWT company_id** (from `ICurrentUserService.CompanyId`) → **Department→Company fallback** (when JWT company missing; single company only) → **Unresolved**. Login-time resolution (AuthService.ResolveUserCompanyIdAsync) is unchanged; request-time department fallback applies only when JWT company is null/empty.
- **SubscriptionEnforcementMiddleware (post-completion):** Now uses **ITenantProvider.CurrentTenantId** for `GetAccessForCompanyAsync`; no mismatch with TenantGuard. When tenant is null or Guid.Empty, subscription check is skipped (allow).
- **WebhooksController ([AllowAnonymous], api/integration/webhooks):** Correctly bypassed via `IAllowAnonymous` metadata; no change needed.
- **ExcelToPdfController GetHealth ([AllowAnonymous]):** Correctly bypassed via metadata; no change needed.

---

## 6. Validation Matrix

| Case | Expected | Result |
|------|----------|--------|
| **A** Authenticated user with JWT company_id | Allowed | **Supported** |
| **B** Authenticated user with null User.CompanyId but department company available (single company) | Allowed | **Supported** (request-time department fallback in TenantProvider.GetEffectiveCompanyIdAsync) |
| **C** Authenticated user with no company, no department, no DefaultCompanyId | Blocked 403 | **Supported** |
| **D** SuperAdmin with X-Company-Id | Allowed (header company) | **Supported** |
| **E** /api/auth/login | Bypassed | **Supported** |
| **F** /api/platform/* | Bypassed | **Supported** |
| **G** /swagger and /health | Bypassed | **Supported** |
| **H** Endpoint marked [AllowNoTenant] | Bypassed | **Supported** |
| **I** Unmatched route (404) | Not converted to tenant 403 | **Supported** (endpoint null → skip) |

---

## 7. Final Go / No-Go

**Go for production.** The implementation is correct, complete, and safe for production. Tenant-context completion pass (2026-03-12) aligned SubscriptionEnforcementMiddleware and TenantContextService with ITenantProvider; see docs/operations/TENANT_CONTEXT_COMPLETION_REPORT.md.
