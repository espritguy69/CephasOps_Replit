# EF Post-Baseline Drift Audit

**Date:** Post-baseline drift classification pass.  
**Context:** After baseline mismatch resolution, the authoritative baseline was 139 total main migrations, 94 with Designer, 45 without Designer. The validator observed 141 total, 94 with Designer, 47 without Designer — i.e. **two extra** no-Designer migrations.

---

## 1. Baseline (authoritative after mismatch resolution)

| Metric | Value |
|--------|--------|
| Total main migrations | 139 |
| With Designer (EF-discoverable) | 94 |
| Without Designer (script-only) | 45 |

**Source:** `docs/operations/EF_MIGRATION_BASELINE_MISMATCH_DECISION.md`, `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` §2.2, `backend/scripts/validate-migration-hygiene.ps1`.

The full list of the 45 no-Designer migrations is in `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` §2.2 (table rows 1–45).

---

## 2. Observed workspace state

| Metric | Value |
|--------|--------|
| Total main migrations | 141 |
| With Designer | 94 |
| Without Designer | 47 |

**Source:** `backend/scripts/audit-migration-designers.ps1` (run from `backend/` or repo root).

---

## 3. The two extra no-Designer migrations

By comparing the current audit output (47 no-Designer) to the documented list of 45 in §2.2 of the full audit, the **two extra** migrations are:

| # | Migration ID | Timestamp | Has .Designer.cs? | In manifest/docs? | Discoverable by EF? |
|---|----------------------------|------------|-------------------|-------------------|---------------------|
| 1 | **20260310100000_AddCommandProcessingLogs** | 2026-03-10 10:00:00 | No | No | No |
| 2 | **20260310110000_AddWorkflowInstancesAndSteps** | 2026-03-10 11:00:00 | No | No | No |

---

## 4. Evidence per migration

### 4.1 20260310100000_AddCommandProcessingLogs

- **File:** `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260310100000_AddCommandProcessingLogs.cs` (single file; no `.Designer.cs`).
- **Content:** Creates table `CommandProcessingLogs` with columns Id, IdempotencyKey, CommandType, CorrelationId, WorkflowInstanceId, Status, ResultJson, ErrorMessage, CompletedAtUtc, CreatedAtUtc; unique index on IdempotencyKey; index on (Status, CreatedAtUtc). Full `Up`/`Down` implementation.
- **Domain/Infrastructure:** Domain entity `CephasOps.Domain.Commands.CommandProcessingLog` exists. Infrastructure has `CommandProcessingLogConfiguration` (table name "CommandProcessingLogs") and `ApplicationDbContext.CommandProcessingLogs` DbSet. Migration schema matches the entity/configuration.
- **Referenced in docs/scripts:** Not mentioned in `docs/` or `backend/scripts/` (no script reference).
- **Intent:** Migration matches live model; created as script-only (no Designer). **Conclusion:** Intentional new script-only migration for the command-processing feature.

### 4.2 20260310110000_AddWorkflowInstancesAndSteps

- **File:** `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260310110000_AddWorkflowInstancesAndSteps.cs` (single file; no `.Designer.cs`).
- **Content:** Creates table `WorkflowInstances` (Id, WorkflowDefinitionId, WorkflowType, EntityType, EntityId, CurrentStep, Status, CorrelationId, PayloadJson, CompanyId, CreatedAt, UpdatedAt, CompletedAt) and table `WorkflowSteps` (Id, WorkflowInstanceId, StepName, Status, StartedAt, CompletedAt, PayloadJson, CompensationDataJson) with FK to WorkflowInstances. Full `Up`/`Down` implementation.
- **Domain/Infrastructure:** Domain entities `WorkflowInstance` and `WorkflowStepRecord` exist. Infrastructure has `WorkflowInstanceConfiguration`, `WorkflowStepRecordConfiguration`, and `ApplicationDbContext.WorkflowInstances` / `WorkflowStepRecords`. Migration schema aligns with the workflow engine model.
- **Referenced in docs/scripts:** Not mentioned in `docs/` or `backend/scripts/`.
- **Intent:** Migration matches live model; created as script-only. **Conclusion:** Intentional new script-only migration for the workflow engine (Phase 6).

---

## 5. Summary

- Both extra migrations are **real schema migrations** for entities that exist in Domain and are registered in ApplicationDbContext.
- Neither has a Designer; neither appears in the no-Designer manifest or full audit table (they were added after the 45 baseline was documented).
- They are **not** in `ApplicationDbContextModelSnapshot.cs` (consistent with other script-only migrations that are not reflected in the snapshot).
- No evidence of being temporary, accidental, or local-only; they belong in the repo as **intentional script-only migrations**.

**Recommendation:** Classify both as **B. Intentional new script-only migration**. Add them to the no-Designer manifest and full audit; set authoritative counts to 141 total, 94 with Designer, 47 without; update the validator and related docs accordingly.
