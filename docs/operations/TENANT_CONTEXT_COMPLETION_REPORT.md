# Tenant Context Completion Pass — Final Report

**Date:** 2026-03-12  
**Scope:** Final hardening of tenant-context consistency (TenantProvider, SubscriptionEnforcementMiddleware, TenantContextService, documentation).

---

## 1. Final Verdict

**COMPLETE**

All identified tenant-context gaps have been closed. One canonical request-time resolution path (ITenantProvider) is used by the guard, subscription enforcement, and tenant context; documentation matches implementation.

---

## 2. What Was Missing

1. **SubscriptionEnforcementMiddleware company source**  
   Subscription checks used `ICurrentUserService.CompanyId` (JWT company) instead of the effective request tenant. For SuperAdmin with `X-Company-Id`, the guard allowed the request using the header company, but subscription enforcement evaluated the JWT company, causing inconsistent allow/deny.

2. **TenantContextService company source**  
   `TenantContextService` (ITenantContext) resolved TenantId/Slug from `ICurrentUserService.CompanyId`. For SuperAdmin with `X-Company-Id`, subscription/usage UI could show the wrong tenant.

3. **Documentation vs implementation**  
   - The audit and CONTEXT described a chain that listed “User.CompanyId” and “department company” as request-time steps. In reality, both are resolved **at login** (AuthService.ResolveUserCompanyIdAsync) and baked into JWT; TenantProvider does not do request-time department resolution.
   - Subscription enforcement and tenant-context company source were not documented as aligned with ITenantProvider.

4. **TenantGuardMiddleware**  
   Pre-existing duplicate local variable `body` in the same method caused a compile error; fixed by renaming the second to `responseBody`.

---

## 3. What Was Fixed

| File | Change |
|------|--------|
| **backend/src/CephasOps.Api/Middleware/SubscriptionEnforcementMiddleware.cs** | Resolve **ITenantProvider** instead of ICurrentUserService. Use **tenantProvider.CurrentTenantId** for GetAccessForCompanyAsync. When CurrentTenantId is null or Guid.Empty, skip subscription check (allow). Removes mismatch with TenantGuard for SuperAdmin X-Company-Id. |
| **backend/src/CephasOps.Api/Services/TenantContextService.cs** | Inject **ITenantProvider** instead of ICurrentUserService. Use **tenantProvider.CurrentTenantId** to resolve company → TenantId/Slug. Ensures ITenantContext reflects the same effective tenant as guard and subscription. |
| **backend/src/CephasOps.Api/Middleware/TenantGuardMiddleware.cs** | Renamed second local variable `body` to `responseBody` to fix CS0136 (duplicate local in same scope). |
| **backend/docs/TENANT_GUARD_AUDIT_REPORT.md** | Updated: SubscriptionEnforcementMiddleware now uses ITenantProvider; canonical request-time chain clarified (User.CompanyId and department company are login-time, not request-time); Case B and Final Go/No-Go updated; reference to this completion report added. |
| **docs/architecture/SAAS_MULTI_TENANT_IMPLEMENTATION_SUMMARY.md** | §5 Tenant Isolation Enforcement: Documented canonical request-time resolution (X-Company-Id → JWT company_id → DefaultCompanyId; JWT set at login from User.CompanyId or first department). Documented that SubscriptionEnforcementMiddleware and TenantContextService use ITenantProvider.CurrentTenantId. |

---

## 4. What Was Verified

- **Single canonical path:** TenantGuardMiddleware, SubscriptionEnforcementMiddleware, TenantContextService, RequireCompanyId(), and the inline tenant-scope middleware in Program.cs all use **ITenantProvider.CurrentTenantId** (or depend on it) for effective request company. No second tenant-resolution chain in the platform layer.
- **TenantProvider request-time chain:** (1) X-Company-Id (SuperAdmin only), (2) JWT company_id (CurrentUserService), (3) TenantOptions.DefaultCompanyId. No request-time department lookup; department company is used only at login when building the JWT (AuthService.ResolveUserCompanyIdAsync).
- **Subscription enforcement:** Uses ITenantProvider; when tenant is null or Guid.Empty, subscription check is skipped (allow). Auth paths still skipped by IsAuthPathThatSkipsEnforcement; no double-check on bypassed routes.
- **Fail-closed:** TenantGuard still blocks when ITenantProvider is null or when CurrentTenantId is null/empty (unless path or metadata is bypassed). No weakening of fail-closed behavior.
- **Drift search:** Application-layer uses of `CompanyId ?? Guid.Empty` and currentUser.CompanyId are on entities, jobs, or domain events (e.g. order.CompanyId, job.CompanyId), not request tenant resolution; left as-is. Observability/event controllers that use currentUser.CompanyId for list scope were not changed (optional future alignment with ITenantProvider for SuperAdmin company switch).

---

## 5. Remaining Limitations

- **Case B (department company at request time):** Not supported. If a user has no company_id in the JWT (e.g. token issued before department membership) and no DefaultCompanyId, they are blocked with 403. Department company is only used at **login** to set JWT company_id; there is no request-time department fallback in TenantProvider. This is intentional and documented.
- **Observability/event list scope:** Some controllers (e.g. ObservabilityController, EventStoreController, OperationalReplayController) use `_currentUser.CompanyId` for ScopeCompanyId() or list filtering. For SuperAdmin with X-Company-Id, those lists still filter by JWT company unless the controller explicitly uses ITenantProvider. This is a known, optional alignment for a future pass; it does not affect guard, subscription, or tenant-context consistency.

---

## 6. Final Go / No-Go

**Go.** Tenant-context hardening is complete for production. One canonical tenant resolution (ITenantProvider) is used by the guard, subscription enforcement, and tenant context; documentation matches code; no remaining platform-layer drift for tenant resolution.

---

## Validation Matrix (Post-Completion)

| Case | Expected | Status |
|------|----------|--------|
| A – Authenticated user with JWT company_id | Allowed, effective company = JWT company | Supported |
| B – User with null User.CompanyId but department company at login | Allowed (JWT set from department at login) | Supported at login |
| C – No company, no department, no DefaultCompanyId | Blocked 403 | Supported |
| D – SuperAdmin with X-Company-Id | Guard and SubscriptionEnforcement use same company | Supported |
| E – /api/auth/login | Bypassed | Supported |
| F – /api/platform/* | Bypassed | Supported |
| G – /swagger and /health | Bypassed | Supported |
| H – [AllowNoTenant] | Bypassed | Supported |
| I – Controller RequireCompanyId() | Consistent with guard | Supported |
| J – ITenantProvider unavailable | Fail-closed, no downstream execution | Supported |
