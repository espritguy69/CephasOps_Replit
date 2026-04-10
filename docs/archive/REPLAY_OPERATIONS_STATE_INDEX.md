# ReplayOperations State Index

## Purpose

Improve query performance for replay-operation monitoring and admin workflows by adding index support for **State**-driven queries on the `ReplayOperations` table.

## What was added

- **Index:** `IX_ReplayOperations_CompanyId_State_RequestedAtUtc`  
  **Columns:** `(CompanyId, State, RequestedAtUtc)`  
  **Table:** `ReplayOperations`

One composite index only; no other schema or replay logic changes.

## Which queries it supports

- **List/history with state filter (company-scoped):**  
  `WHERE CompanyId = @p AND State = @state ORDER BY RequestedAtUtc DESC`  
  Used when the API or admin UI filters replay operations by state (e.g. active, failed, pending) and scope by company. The index supports both the filter and the sort.

- **List/history (company-scoped, no state filter):**  
  `WHERE CompanyId = @p ORDER BY RequestedAtUtc DESC`  
  The existing index `IX_ReplayOperations_CompanyId_RequestedAtUtc` remains the best fit; the new index can also be used by the planner (prefix match on `CompanyId` then scan by `RequestedAtUtc`).

- **Operational diagnostics and background workflows** that filter by `State` (e.g. “show failed replays”, “show active/running”) and optionally by company, with ordering by `RequestedAtUtc`.

## Why it was needed

- The list endpoint (`ReplayOperationQueryService.ListAsync`) is company-scoped and ordered by `RequestedAtUtc`. Adding optional **State** filtering (active/failed/pending) is a natural extension for replay history screens and support workflows.
- Without an index that includes `State`, state-filtered queries would require full table (or full company) scans and a separate sort. The composite index allows index seeks by company and state and returns rows already ordered by `RequestedAtUtc`.

## Migration

- **Migration:** `20260309200000_ReplayOperationsStateIndex`  
- **Up:** Creates `IX_ReplayOperations_CompanyId_State_RequestedAtUtc` on `ReplayOperations`.  
- **Down:** Drops the index.  
- **Behavior:** Performance-only; no replay semantics, API contracts, or state transitions are changed.

## Limitations

- **Global list by state only** (no company filter), e.g. `WHERE State = @state ORDER BY RequestedAtUtc DESC`, is not specifically targeted by this index. The existing `IX_ReplayOperations_RequestedAtUtc` index can still be used for unscoped list ordered by time; adding a separate `(State, RequestedAtUtc)` index was not in scope for this minimal change.
