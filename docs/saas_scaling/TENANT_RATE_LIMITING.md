# Tenant Rate Limiting

**Purpose:** Prevent a single tenant from overloading the API. Limits are applied per tenant (CompanyId).

---

## Configuration: SaaS:TenantRateLimit

| Option | Default | Description |
|--------|---------|-------------|
| **Enabled** | true | Turn per-tenant rate limiting on or off. |
| **RequestsPerMinute** | 100 | Default limit per tenant per rolling minute. |
| **RequestsPerHour** | 1000 | Default limit per tenant per rolling hour. |
| **Plans** | Trial: 50, Standard: 100, Enterprise: 500 | Per-plan overrides (requests per minute). Used when **ITenantRateLimitResolver** is registered and returns a plan-based limit. |

Example `appsettings.json`:

```json
{
  "SaaS": {
    "TenantRateLimit": {
      "Enabled": true,
      "RequestsPerMinute": 100,
      "RequestsPerHour": 1000,
      "Plans": {
        "Trial": 50,
        "Standard": 100,
        "Enterprise": 500
      }
    }
  }
}
```

---

## Behavior

- **Scope:** Requests that have a tenant context (CompanyId from **ITenantProvider**) are counted. Requests without a tenant (e.g. login, platform admin) are not rate limited.
- **Windows:** Both a rolling 1-minute and a rolling 1-hour window are enforced. Exceeding either returns **HTTP 429 Too Many Requests**.
- **Storage:** In-memory per-tenant buckets. For multi-instance production, consider a shared store (e.g. Redis) and a custom implementation.

---

## Plan-based overrides

Register an **ITenantRateLimitResolver** to return different limits per tenant (e.g. from subscription plan):

- Implement **GetLimitsAsync(companyId)** to return (RequestsPerMinute, RequestsPerHour), e.g. by resolving Company → Tenant → TenantSubscription → BillingPlan.Slug and using **TenantRateLimitOptions.Plans**.
- When not registered, the default **RequestsPerMinute** and **RequestsPerHour** from options are used for all tenants.

---

## Metrics

When a request is rejected:

- **HTTP 429** is returned with body `{"error":"Rate limit exceeded. Try again later."}`.
- A log entry is written with **TenantRateLimitExceeded**, **CompanyId**, and **LimitType** (Minute or Hour). Monitoring can count these for the **TenantRateLimitExceeded** metric.

---

## Safety

- Middleware runs after tenant context is established (e.g. after **TenantGuardMiddleware** / **TenantUsageRecordingMiddleware** as configured).
- No tenant scope or platform bypass is used; limits are applied per CompanyId only.
