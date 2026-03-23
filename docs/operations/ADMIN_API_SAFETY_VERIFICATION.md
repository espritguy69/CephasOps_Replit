# Admin API Safety — Replay, Ledger, Timeline

**Purpose:** Verification and documentation of authorization, pagination, and query limits for replay, event store, ledger, and timeline admin APIs. Aligns with **docs/SYSTEM_HARDENING_AUDIT.md** §5.

---

## 1. Authorization

| API area | Policy | Permission | Company scope |
|----------|--------|------------|----------------|
| **Event store** (`/api/event-store/*`) | `Jobs` | `JobsView` (list/dashboard), `JobsAdmin` (retry/replay) | Non–super-admin: `ScopeCompanyId()`; requests for another company → 403. |
| **Operations overview** (`GET /api/admin/operations/overview`) | Roles | `SuperAdmin`, `Admin` + `JobsView` | Read-only; compact summary of job executions, event store (24h), payout health, system health. Internal operational visibility only (not customer analytics). |
| **Operational replay** (`/api/operational-replay/*`) | `Jobs` | `JobsAdmin` | Same; scope enforced so only own company can be used for execute/list. |
| **Event ledger** (`/api/event-ledger/*`) | `Jobs` | `JobsAdmin` | Same; list/detail scoped by company. |
| **Timeline** (workflow/order history) | `Jobs` or equivalent | Appropriate permission | Company-scoped where applicable. |

**Verification:** Controllers use `[Authorize(Policy = "Jobs")]` and `[RequirePermission(PermissionCatalog.JobsAdmin)]` (or `JobsView` for read-only). `ScopeCompanyId()` is used to restrict non–super-admins to their company.

---

## 2. Pagination and limits

| Endpoint / service | Parameter | Cap | Notes |
|--------------------|-----------|-----|--------|
| Event store list | `pageSize` | 100 (`EventStoreController.MaxPageSize`) | Clamped in controller. |
| Event store failed/dead-letter | `pageSize` | 100 | Same. |
| Event store processing log | `pageSize` | 100 | Same. |
| Ledger list entries | `pageSize` | 100 | Clamped (see EventLedgerController). |
| Timeline (workflow/order/unified) | `limit` | 500 | Clamped to max 500. |
| Replay operations list | `pageSize` | 100 | Clamped. |
| Replay preview / execution | `MaxEvents` | 10_000 per query | Bounded in `GetEventsForReplayAsync` (take = min(maxEvents ?? 5000, 10000)). |

**Verification:** No unbounded list endpoints in the reviewed admin paths; all use `Math.Clamp` or equivalent to cap page size or limit.

---

## 3. Query efficiency

- **Event store:** Queries filter by `CompanyId`, `Status`, `OccurredAtUtc`, etc.; indexes exist on these columns.
- **Ledger:** Indexes on `(CompanyId, LedgerFamily, OccurredAtUtc)`, `(EntityType, EntityId, LedgerFamily)`, and idempotency keys.
- **Replay operations:** Index on `(CompanyId, State, RequestedAtUtc)` for list/filter by state.

No full table scans without filters were identified in the reviewed admin code paths.

---

## 4. Remaining considerations

- **Large result sets:** Even with pagination, very large tables may make first page slow; consider date-range or status filters for operational use.
- **Replay execution:** Long-running replays hold the company lock and one background job worker slot; monitor for stuck jobs and use cancel when needed.

---

## 5. Summary

| Check | Status |
|-------|--------|
| Authorization (Jobs policy, JobsAdmin/JobsView) | Verified on replay, event store, ledger, timeline. |
| Company scope for non–super-admin | Enforced. |
| Pagination / page size caps | Enforced (e.g. 100 for lists, 500 for timeline limit). |
| Replay/replay-preview event cap | Bounded (e.g. 10_000 per query). |
| Query limits / indexes | Indexes and filters in place; no unbounded scans. |

**Last verified:** 2026-03-12. Update this doc when adding new admin endpoints or changing authorization/pagination. Operations overview: see [OPERATIONAL_OBSERVABILITY_REPORT.md](../../backend/docs/operations/OPERATIONAL_OBSERVABILITY_REPORT.md).
