# Distributed Cache and Coordination Strategy

**Purpose:** Define where CephasOps benefits from distributed cache/coordination; keep tenant truth in PostgreSQL; avoid cross-tenant cache pollution.

---

## 1. Current state

- **In-memory:** `IMemoryCache` (AddMemoryCache) for settings and general use; per-process.
- **Rate limiting:** `TenantRateLimitMiddleware` uses **IRateLimitStore**. When **ConnectionStrings:Redis** is set, **RedisRateLimitStore** (StackExchange.Redis) provides shared rate limit state across API replicas; otherwise **InMemoryRateLimitStore** is used (per-node). Keys: `ratelimit:tenant:{tenantId}:m:{minuteSlot}` and `:h:{hourSlot}` (fixed window).
- **LedgerBalanceCache:** Domain entity (table); not application cache.
- **Redis:** Optional; used only for rate limit when configured. No IDistributedCache in use for other caches yet.

---

## 2. Where distributed cache/coordination helps

| Use case | Benefit | Tenant-safe? |
|----------|---------|--------------|
| **Rate limit state** | Shared buckets across API replicas; consistent 429 across nodes | Yes: key by CompanyId/Guid. |
| **Platform health / dashboard** | Short TTL cache (e.g. 1–5 min) to avoid repeated heavy aggregations | Yes: platform-level; no tenant key. |
| **Analytics (tenant-health, anomalies)** | Short TTL cache for GET /api/platform/analytics/* responses | Yes: SuperAdmin only; cache key can include "platform" or route. |
| **Worker coordination / leader election** | Single active scheduler or Guardian when multiple replicas run | N/A (coordination only). |
| **Distributed locks** | Only if truly needed (e.g. one instance of a repair job per tenant) | Must scope lock by tenant if used. |

---

## 3. What must never be cached without tenant-aware keys

- Any cache that stores **tenant-scoped data** (e.g. company settings, user list, order list) **must** use a key that includes **TenantId or CompanyId**. Never use a global key for tenant data.
- **Invalidation:** When tenant data changes, invalidate or version the cache key for that tenant only. Prefer short TTL for tenant-scoped cache to limit leakage risk.
- **Cross-tenant pollution:** If a bug causes cache key to omit tenant id, one tenant could see another’s data. Mitigation: always use key pattern like `tenant:{tenantId}:...` or `company:{companyId}:...` for tenant-scoped entries.

---

## 4. Cache key naming guidance

- **Rate limit:** `ratelimit:tenant:{companyId}` or `ratelimit:{companyId}`. Value: bucket state (counts, window).
- **Platform health (no tenant in key):** `platform:health:v1` or `platform:analytics:platform-health`. TTL 1–5 min.
- **Dashboard (platform):** `platform:analytics:dashboard:v1`. TTL 5–15 min.
- **Tenant-scoped (if added):** `tenant:{tenantId}:settings:...`, `company:{companyId}:...`. TTL short (e.g. 1–5 min).

---

## 5. TTL guidance

| Cache | Suggested TTL | Invalidation |
|-------|----------------|--------------|
| Rate limit bucket | Sliding window (e.g. 1 min, 1 hour); not TTL but window reset | N/A (time-based). |
| Platform health | 1–5 min | Time-based; optional manual flush. |
| Dashboard | 5–15 min | Time-based. |
| Tenant-scoped (if any) | 1–5 min | On write to that tenant; or accept stale until TTL. |

---

## 6. Invalidation

- **Platform caches:** TTL-only is acceptable; optional admin “flush cache” (existing FlushCache) can clear in-memory; if Redis added, flush by key pattern.
- **Tenant-scoped:** Invalidate on update (e.g. company settings changed) or use short TTL.

---

## 7. Redis: recommended or optional

- **Recommended for production** when:
  - Multiple API replicas and you want **consistent per-tenant rate limiting** across nodes.
  - You want **short-lived platform dashboard/health cache** to reduce DB load.
- **Optional:** Single-replica or low traffic can stay with in-memory rate limit and no distributed cache.
- **Do not** move tenant truth (authoritative data) to Redis; PostgreSQL remains source of truth. Redis is for ephemeral state and cache only.

---

## 8. Minimal abstraction (optional)

If Redis is introduced later, use **IDistributedCache** (or a thin wrapper) so that:
- Rate limit middleware can optionally use IDistributedCache for bucket state when configured.
- Platform analytics can optionally cache GET responses with a tenant-safe key strategy (platform keys only or tenant-prefixed).

A minimal hook: **IRateLimitStore** or **IDistributedCache** binding. When not configured, rate limit continues to use in-memory dictionary. No full implementation required in this phase; document the extension point in this file and in ENVIRONMENT_CONFIGURATION.md.

---

## 9. Risks of cross-tenant cache pollution

- **Risk:** Bug or misconfiguration stores tenant A’s data under a key that tenant B can read (e.g. missing tenant id in key).
- **Mitigation:** (1) Always include tenant/company id in key for any tenant-scoped cache. (2) Prefer short TTL. (3) Code review and tests for cache key construction. (4) Do not cache raw tenant payloads in a shared key.

---

## 10. Summary

- **Cache:** Rate limit state and optional platform/dashboard cache are the main candidates for Redis. Tenant data must use tenant-scoped keys; never cache tenant truth outside PostgreSQL.
- **Coordination:** Leader election for schedulers/Guardian can use DB lock or Redis if multiple replicas run worker code.
- **Implementation:** Current code uses in-memory rate limit; add IDistributedCache/Redis only when multi-replica rate limit or dashboard cache is needed. Document the extension point; no forced full implementation in this phase.
