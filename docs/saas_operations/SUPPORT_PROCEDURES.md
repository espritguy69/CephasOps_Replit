# Support Procedures

**Date:** 2026-03-13

Platform support tooling for SuperAdmin: tenant diagnostics, logs, impersonation, and job retry. All actions require SuperAdmin and appropriate permissions; sensitive actions are audit-logged.

---

## 1. Tenant diagnostics

- **Endpoint:** `GET /api/platform/support/tenants/{tenantId}/diagnostics`  
  (Alternatively use `GET /api/platform/tenants/{tenantId}/diagnostics`.)

Returns:
- Tenant and company identifiers, status.
- User count, order count.
- Subscription status, trial end, next billing date.

**Use:** Triage support tickets, verify tenant state before making changes.

---

## 2. Viewing tenant logs

- **Endpoint:** `GET /api/platform/support/tenants/{tenantId}/logs`

Returns a hint object:
- `tenantId`
- `message`: instructs to query your log aggregation (e.g. Seq, Application Insights) filtered by TenantId.
- `filterExample`: example filter string (e.g. `TenantId = "<guid>"`).

Structured logs (Serilog) include **TenantId** and **CompanyId**; use these fields in your log sink to filter by tenant.

---

## 3. Impersonate tenant admin

- **Endpoint:** `POST /api/platform/support/impersonate`  
- **Body:** `{ "tenantId": "<guid>" }`

**Behaviour:**
- Resolves the tenant’s company and finds the first active user with role Admin or SuperAdmin.
- Calls `IAuthService.CreateTokenForImpersonationAsync(targetUserId, requestedByUserId)`.
- Returns a **short-lived JWT** (e.g. 60 minutes) in the same shape as login: `AccessToken`, `ExpiresAt`, `User`. No refresh token.
- **Audit:** An auth audit event `Impersonation` is logged with the target user id.

**Use:** Reproduce issues in the tenant context. Use the returned `AccessToken` as Bearer for subsequent API calls.

**Security:** SuperAdmin only; audit log records who impersonated whom.

---

## 4. Retry tenant jobs

- **Endpoint:** `POST /api/platform/support/tenants/{tenantId}/jobs/{jobExecutionId}/retry`

**Behaviour:**
- Verifies the job belongs to the tenant (via `CompanyId` and company’s `TenantId`).
- Sets job `Status` to `Pending`, clears processing lease fields, sets `NextRunAtUtc` to now.
- Worker will pick it up on the next poll.

**Use:** Retry failed or dead-letter jobs for a specific tenant after fixing the cause.

---

## 5. Permissions and audit

- All support endpoints require **SuperAdmin**.
- Diagnostics and logs hint: `AdminTenantsView`.
- Impersonate and job retry: `AdminTenantsEdit`.
- Impersonation is logged as auth event type `Impersonation`; ensure audit log retention and access control for compliance.

---

## 6. References

- [SAAS_OPERATIONS_GUIDE.md](../saas_scaling/SAAS_OPERATIONS_GUIDE.md) – Admin operations overview.
- [SAAS_SCALING_ARCHITECTURE.md](../saas_scaling/SAAS_SCALING_ARCHITECTURE.md) – Platform admin and observability.
