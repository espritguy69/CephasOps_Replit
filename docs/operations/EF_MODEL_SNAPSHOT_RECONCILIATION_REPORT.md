# EF Model Snapshot — Reconciliation Report

**Date:** Migration recovery pass (Phase 4).  
**Artifact:** `backend/src/CephasOps.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`

---

## 1. Snapshot health assessment

**Verdict: drifted but not corrupted.**

- The snapshot **compiles** and is **structurally valid** (single `BuildModel`, consistent with EF 10).
- It is **behind** the current domain model and behind many schema changes that exist in migrations (discovered and non-discoverable) and in the live database.
- It is **not** randomly corrupted; it reflects an older, consistent model state (roughly post–EventStore Phase 2, pre–Phase 7 and pre–many no-Designer migrations).

---

## 2. Snapshot vs latest discoverable migration

- **Last discoverable migration:** 20260309120000_AddJobRunEventId.
- The snapshot is **not** a faithful “state after 20260309120000”; it predates many of the changes that are in the migration chain (e.g. EventStore Phase 7, EventStoreAttemptHistory, WorkerInstances, ParsedMaterialAliases, and various columns/tables from no-Designer migrations).
- So the snapshot is **behind** the “intended” state after the last discoverable migration. It was likely last updated by an older `dotnet ef migrations add` and then many migrations were added by hand or from scripts without updating it.

---

## 3. Snapshot vs current domain model

- **DbContext** registers many entities that do **not** appear in the snapshot (or appear with fewer columns/tables). Examples:
  - **EventStore:** Missing Phase 7 and related columns (e.g. CausationId, PayloadVersion, NextRetryAtUtc, ProcessingStartedAtUtc, ProcessingNodeId, ProcessingLeaseExpiresAtUtc, LastClaimedAtUtc, LastClaimedBy, LastErrorType) and no **EventStoreAttemptHistory** table.
  - **ParsedMaterialAliases:** Not present in snapshot (table created in no-Designer migration 20260311120000_AddParsedMaterialAlias).
  - **WorkerInstances, RebuildOperations, ReplayOperations, EventProcessingLog, LedgerEntries, SlaRules, SlaBreaches, WorkflowTransitionHistory,** etc.: Either missing or not fully aligned with current model.
- Adding a new migration today (`dotnet ef migrations add`) would produce a **large, noisy diff** (many adds and changes) because EF compares the current model to this old snapshot.
- **PendingModelChangesWarning** is suppressed at design-time and runtime so that `dotnet ef database update` and tooling can run; see `EF_PENDING_MODEL_CHANGES_REPAIR.md`.

---

## 4. Missing designers and lineage

- **44 migrations** have no `.Designer.cs`. Their schema changes were never reflected in the snapshot. So “missing designers” have **poisoned** the lineage in the sense that the snapshot was never advanced for those changes.
- The discovered chain (95 migrations) is internally consistent for the migrations that have Designers; the snapshot, however, does not match the “end” of that chain.

---

## 5. Recommendation: sync migration vs re-baseline

| Option | Assessment |
|--------|------------|
| **In-place snapshot sync** | Theoretically possible by generating one “sync” migration that adds all missing tables/columns with idempotent SQL. That migration would be very large and risky to maintain; reviewing and testing it would be heavy. **Not recommended** as a quick fix. |
| **Re-baseline** | Create a new baseline (e.g. one “Initial” or “Baseline” migration from current model) for **new** environments, while keeping the existing migration chain and scripts for existing DBs. **Recommended** only when the team is ready to invest in a dedicated migration-stability sprint and can document the two-track strategy (legacy chain + baseline). |
| **Current approach (no snapshot change)** | Leave the snapshot as-is; rely on PendingModelChangesWarning suppression; apply schema via discovered migrations (with idempotent fixes) and script-only migrations per manifest. **Recommended** for now. |

**Conclusion:** Snapshot is **not safely recoverable** by a small, low-risk in-place fix. Either a **large one-time sync migration** (with idempotent Up) or a **re-baseline** is the way to get to a clean snapshot; both are deferred to a dedicated, documented pass. No snapshot file changes are made in this recovery pass.
