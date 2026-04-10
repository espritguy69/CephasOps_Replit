# Phase 7 Lease / Attempt-History — Truth Source and Closure

**Date:** 2026-03-09

---

## Schema truth source

- **Authoritative migration:** `20260309065950_VerifyNoPending` adds Phase 7 EventStore columns (LastClaimedAtUtc, LastClaimedBy, LastErrorType, ProcessingLeaseExpiresAtUtc, ProcessingNodeId, ProcessingStartedAtUtc) and creates the `EventStoreAttemptHistory` table with indexes.
- **Duplicate migration:** `20260312100000_AddEventStorePhase7LeaseAndAttemptHistory` previously added the same schema. When applied after 20260309065950 it would fail (column/table already exists). It has been converted to a **no-op** (empty Up/Down) so that:
  - Existing databases that have both migrations in `__EFMigrationsHistory` remain valid.
  - New deployments that run migrations in order do not double-apply; 20260309065950 applies the schema once, 20260312100000 does nothing.

## Script-only path

- **Script:** `backend/scripts/apply-EventStorePhase7LeaseAndAttemptHistory.sql`
- **Use when:** Phase 7 is not in the EF migration chain (e.g. script-only or repair) or you need an idempotent apply without running EF migrations.
- **Behavior:** Adds EventStore columns and EventStoreAttemptHistory table only if they do not exist; inserts into `__EFMigrationsHistory` with MigrationId `20260312100000_AddEventStorePhase7LeaseAndAttemptHistory` so EF considers Phase 7 applied.
- **Safe:** Idempotent; safe to run multiple times.

## Code

- **EventStoreRepository:** Uses Phase 7 columns for claim (ProcessingNodeId, ProcessingLeaseExpiresAtUtc, LastClaimedAtUtc, LastClaimedBy), MarkProcessedAsync clearance, and ResetStuckProcessingAsync / recovery.
- **EventStoreAttemptHistoryStore:** Implements IEventStoreAttemptHistoryStore (Domain); persists EventStoreAttemptRecord to EventStoreAttemptHistory.

## Summary

| Item | Source |
|------|--------|
| Phase 7 schema | 20260309065950_VerifyNoPending (migration) or apply-EventStorePhase7LeaseAndAttemptHistory.sql (script) |
| 20260312100000 | No-op; do not add schema here |
