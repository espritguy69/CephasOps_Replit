# Admin / Operational API Safety

This document describes the authorization model, pagination and limits, company scoping, and failure semantics for admin and operational APIs (replay, event store observability, event ledger). It aligns with the implementation after the Admin API Safety Verification hardening pass.

## Authorization model

- **Policy:** All admin/ops endpoints use the **Jobs** authorization policy.
- **Permissions:**
  - **JobsView** – read-only: list events, event detail, dashboard, processing log, replay policy, timeline/trace.
  - **JobsAdmin** – full: execute replay, cancel, retry, rerun-failed, resume, ledger entries, timeline from ledger, etc.
- **SuperAdmin:** Bypasses permission checks and has no company scope (can access all companies).
- **Company-scoped admins:** Must have the appropriate permission (e.g. JobsAdmin) and are restricted to their `CompanyId` for data access.

## Company scoping

- **Scope:** Controllers derive a scope company via `ScopeCompanyId()`: `null` for SuperAdmin, otherwise the current user’s `CompanyId`.
- **List/query endpoints:** When `scopeCompanyId` is set, queries filter by that company. SuperAdmin can pass an optional `companyId` to narrow results.
- **Out-of-scope request:** If a non–SuperAdmin user sends a different `companyId` (e.g. in query) than their scope, the API returns **403 Forbidden** with a message such as "Company scope not allowed."
- **Detail/cancel by id:** Detail and cancel endpoints resolve the resource by id **and** scope. If the resource does not exist or belongs to another company, the API returns **404 Not Found** (no information leak).

## Pagination and limits

- **Replay operations list:** `page` (default 1), `pageSize` (default 20). Page is normalized to ≥ 1; `pageSize` is clamped to a maximum of **100**.
- **Event store lists (events, failed, dead-letter, processing log):** Same idea: `page` ≥ 1, `pageSize` clamped to **100**.
- **Ledger entries list:** `page` ≥ 1, `pageSize` clamped to **100**.
- **Timeline/limit params:** Ledger workflow-transition and order timelines enforce a maximum **limit** (e.g. 500). Larger values are clamped.

Services that perform `Skip` use a safe page (e.g. `Math.Max(1, page)`) so that `page` ≤ 0 never produces a negative skip.

## Input validation

- **Replay policy:** `eventType` (route segment) must not be null or whitespace; otherwise **400 Bad Request** with "eventType is required."
- **Page/size:** `page` and `pageSize` are normalized/clamped as above; invalid or oversized values do not cause unbounded queries.
- **GUIDs:** Invalid or missing GUIDs for id/entity parameters are handled by model binding or validation and result in **400** or **404** as appropriate.

## Failure semantics

- **401 Unauthorized:** Not authenticated.
- **403 Forbidden:** Authenticated but lacking permission, or company-scoped user requesting another company (e.g. `companyId` ≠ scope).
- **404 Not Found:** Resource not found or out of scope (e.g. replay operation, ledger entry, event) so that clients cannot distinguish “does not exist” from “not in your scope.”
- **400 Bad Request:** Invalid input (e.g. missing/whitespace `eventType`, invalid filter combination).
- **409 Conflict / 400:** Operation not allowed in current state (e.g. cancel/replay conflict); specific messages per endpoint.

Consistent use of 403 for company-scope violations and 404 for not-found/out-of-scope keeps behavior predictable across replay, event store, and ledger APIs.
