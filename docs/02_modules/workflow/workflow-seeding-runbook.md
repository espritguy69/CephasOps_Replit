# Workflow Seeding Runbook (Production)

**Purpose:** Ensure required workflow definitions and transitions exist in the production database so orders can move through statuses correctly. Evidence-first, idempotent, no destructive changes.

**Related:** [DB Workflow Baseline Spec](operations/db_workflow_baseline_spec.md) | [Order lifecycle and statuses](business/order_lifecycle_and_statuses.md)

---

## 1. When to run

- **Before go-live:** After all DB migrations have been applied and before first production traffic.
- **After migrations:** Whenever a new deployment includes workflow table changes (e.g. soft-delete columns) and you need to (re-)seed workflows.
- **One-off repair:** If production was previously seeded with only a minimal order workflow (e.g. only Pending → Assigned), run the seed scripts to add missing transitions.

---

## 2. Preconditions

- **Database:** PostgreSQL; connection string set via `ConnectionStrings__DefaultConnection` or `appsettings.Development.json` (see [PostgreSQL rules](.cursor/rules/postgress.mdc)). Production: use application user `cephasops_app`.
- **Migrations applied:** Tables `WorkflowDefinitions` and `WorkflowTransitions` must exist; soft-delete migration (e.g. `AddSoftDeleteToCompanyScopedEntities`) must have been run so `IsDeleted` exists on both tables.
- **Companies:** At least one row in `Companies` (seeds use the first company ID).
- **Backup:** Take a DB backup before running seed scripts in production (recommended).
- **Maintenance window:** Optional; scripts are insert-only and idempotent, but running during low traffic is safer.

---

## 3. Required workflows (evidence from code and docs)

Production currently requires **one** workflow:

| Workflow   | EntityType | Description                                      |
|-----------|------------|--------------------------------------------------|
| Order     | `Order`    | GPON order lifecycle: scheduling, blocker, docket, billing, invoice rejection loop |

There is **no separate** Invoice or Docket workflow; all order statuses (including Rejected/Reinvoice) belong to the Order workflow.

**Workflow resolution (scope):** Effective workflow is resolved in this order: **Partner** → **Department** → **Order Type** → **General**. Only one active workflow is allowed per scope (CompanyId + EntityType + PartnerId + DepartmentId + OrderTypeCode). The seeded Order workflow is **general** (no partner, department, or order type). To add department- or order-type-scoped workflows, create them via the Workflow Definitions UI or API; use parent order type codes (e.g. `MODIFICATION`, `ACTIVATION`) for order-type scope. See [WORKFLOW_RESOLUTION_RULES.md](WORKFLOW_RESOLUTION_RULES.md).

---

## 4. Required Order workflow: states and transitions matrix

States are implied by transition endpoints (no separate `WorkflowStates` table). The following transitions are the **minimum required** set (canonical flow + invoice rejection loop + docket rejection loop).

### 4.1 Main and scheduling

| FromStatus              | ToStatus                 | Purpose / note |
|-------------------------|--------------------------|----------------|
| Pending                 | Assigned                 | Ops assigns SI; side effect: checkMaterialCollection |
| Pending                 | Cancelled                | Cancel before assignment |
| Assigned                | OnTheWay                 | SI or Ops marks on the way |
| Assigned                | Blocker                  | Pre-customer blocker |
| Assigned                | ReschedulePendingApproval| Ops requests TIME approval |
| Assigned                | Cancelled                | Cancel after assignment |
| OnTheWay                | MetCustomer              | SI arrives at customer |
| OnTheWay                | Blocker                  | Blocker en route |
| MetCustomer             | OrderCompleted           | SI completes job |
| MetCustomer             | Blocker                  | Post-customer blocker |

### 4.2 Blocker exits (known historically missing: Blocker → Assigned)

| FromStatus | ToStatus                 | Purpose / note |
|------------|--------------------------|----------------|
| Blocker    | MetCustomer              | Ops resumes at customer (e.g. same-day fix) |
| **Blocker**| **Assigned**             | Ops re-assigns (reschedule/reassign); **ensure this exists** |
| Blocker    | ReschedulePendingApproval| Ops requests reschedule approval |
| Blocker    | Cancelled                | Cancel from blocker |

### 4.3 Reschedule

| FromStatus              | ToStatus   |
|-------------------------|------------|
| ReschedulePendingApproval | Assigned |
| ReschedulePendingApproval | Cancelled |

### 4.4 Docket path

| FromStatus       | ToStatus         |
|------------------|------------------|
| OrderCompleted   | DocketsReceived  |
| DocketsReceived  | DocketsVerified  |
| DocketsReceived  | DocketsRejected  |
| DocketsRejected  | DocketsReceived  |
| DocketsVerified  | DocketsUploaded  |
| DocketsUploaded  | ReadyForInvoice |

### 4.5 Billing and invoice rejection loop

| FromStatus         | ToStatus           |
|--------------------|--------------------|
| ReadyForInvoice    | Invoiced           |
| Invoiced           | SubmittedToPortal  |
| SubmittedToPortal  | Completed          |
| Invoiced           | Rejected           |
| SubmittedToPortal  | Rejected           |
| Rejected           | ReadyForInvoice    |
| Rejected           | Reinvoice          |
| Reinvoice          | Invoiced           |

**Total expected transitions:** 30.

---

## 5. Scripts and run order

All scripts live under **`scripts/deploy/workflow/`**:

| Script | Purpose |
|--------|--------|
| `00_check_workflows.sql` | Evidence: list workflows, missing transitions, PASS/FAIL summary. Run before and after seeding. |
| `10_seed_order_workflow_if_missing.sql` | Idempotent: create Order WorkflowDefinition if missing and seed all 30 transitions. |
| `20_seed_invoice_transitions.sql` | Idempotent: add only the 5 invoice rejection loop transitions (use if DB had minimal seed only). |
| `30_seed_blocker_assigned_transition.sql` | Idempotent: add Blocker → Assigned only (use if that transition was missing). |

**Recommended order:** Check → Seed → Re-check.

**Migrations:** Apply all EF migrations before seeding (e.g. `dotnet ef database update`). The migration `AddUniqueActiveScopeIndexWorkflowDefinitions` adds a unique partial index so only one active workflow per scope can exist; apply it before creating multiple scoped workflows.

1. Run **check** to see current state.
2. Run **seed**: for a full seed run `10`; if you already have the full order workflow and only need the invoice loop or Blocker→Assigned, run `20` and/or `30` as needed.
3. Run **check** again and confirm PASS.

---

## 6. Step-by-step commands (psql)

Replace `$CONNECTION_STRING` with your actual connection string (e.g. `Host=localhost;Port=5432;Database=cephasops;Username=cephasops_app;Password=...;SslMode=Disable`). For psql use URI form or individual options.

```bash
# From repo root
cd c:\Projects\CephasOps

# 1) Pre-seed check (evidence)
psql "$CONNECTION_STRING" -f scripts/deploy/workflow/00_check_workflows.sql

# 2) Seed (full order workflow; idempotent)
psql "$CONNECTION_STRING" -f scripts/deploy/workflow/10_seed_order_workflow_if_missing.sql

# Optional: if you only need invoice loop or Blocker→Assigned on an already-seeded DB:
# psql "$CONNECTION_STRING" -f scripts/deploy/workflow/20_seed_invoice_transitions.sql
# psql "$CONNECTION_STRING" -f scripts/deploy/workflow/30_seed_blocker_assigned_transition.sql

# 3) Post-seed check (verification)
psql "$CONNECTION_STRING" -f scripts/deploy/workflow/00_check_workflows.sql
```

Windows (PowerShell) example with env var:

```powershell
$env:PGPASSWORD = "your_password"
psql -h localhost -p 5432 -U cephasops_app -d cephasops -f scripts/deploy/workflow/00_check_workflows.sql
psql -h localhost -p 5432 -U cephasops_app -d cephasops -f scripts/deploy/workflow/10_seed_order_workflow_if_missing.sql
psql -h localhost -p 5432 -U cephasops_app -d cephasops -f scripts/deploy/workflow/00_check_workflows.sql
```

---

## 7. Verification queries and expected results

### 7.1 Run the check script

Output of `00_check_workflows.sql` includes:

- **Section 1:** Rows from `WorkflowDefinitions` (active, not deleted). Expect at least one row with `EntityType` = `Order`.
- **Section 2:** Missing transitions (expected but not in DB). Expect **no rows** after a successful full seed.
- **Section 3:** Extra transitions (in DB but not in canonical list). Informational only; extra rows are acceptable.
- **Section 4:** Summary table with columns `Workflow`, `WorkflowName`, `Result`, `MissingTransitions`, `ExpectedTransitionCount`. Expect **Result = PASS** and **MissingTransitions = 0** for Order.

### 7.2 Manual verification (optional)

```sql
-- Count active Order workflow transitions (expect >= 30)
SELECT COUNT(*) AS transition_count
FROM "WorkflowTransitions" t
JOIN "WorkflowDefinitions" w ON w."Id" = t."WorkflowDefinitionId"
WHERE w."EntityType" = 'Order'
  AND (t."IsDeleted" = false OR t."IsDeleted" IS NULL)
  AND (w."IsDeleted" = false OR w."IsDeleted" IS NULL);

-- Ensure Blocker -> Assigned exists
SELECT "Id", "FromStatus", "ToStatus"
FROM "WorkflowTransitions" t
JOIN "WorkflowDefinitions" w ON w."Id" = t."WorkflowDefinitionId"
WHERE w."EntityType" = 'Order'
  AND t."FromStatus" = 'Blocker' AND t."ToStatus" = 'Assigned'
  AND (t."IsDeleted" = false OR t."IsDeleted" IS NULL);
```

Expected: `transition_count` ≥ 30; one row for Blocker → Assigned.

---

## 8. Rollback strategy

- **Scripts are insert-only:** They do not delete or update existing workflow data. There is no scripted “undo” for workflow seeding.
- **Rollback:** If you must revert, restore the database from the backup taken before seeding. Document the backup location and restore procedure in your own runbook.
- **No destructive changes:** No DELETEs are performed by these scripts.

---

## 9. Checklist: required workflows and how to verify

| # | Item | How to verify |
|---|------|----------------|
| 1 | Order WorkflowDefinition exists | Section 1 of `00_check_workflows.sql`: at least one row with EntityType = Order. |
| 2 | All 30 required transitions exist | Section 2 of `00_check_workflows.sql`: no rows (no missing transitions). |
| 3 | Summary shows PASS for Order | Section 4 of `00_check_workflows.sql`: Result = PASS, MissingTransitions = 0. |
| 4 | Blocker → Assigned present | Section 2 empty for that pair; or manual query in §7.2 returns one row. |
| 5 | Invoice rejection loop present | Section 2 empty for Invoiced→Rejected, SubmittedToPortal→Rejected, Rejected→ReadyForInvoice, Rejected→Reinvoice, Reinvoice→Invoiced. |

---

## 10. References

- **Schema:** `WorkflowDefinitions`, `WorkflowTransitions` (see `AddPhase6WorkflowEntities.sql`, `AddSoftDeleteToCompanyScopedEntities` migration).
- **Domain:** `CephasOps.Domain.Orders.Enums.OrderStatus`, `CephasOps.Domain.Workflow.Entities.WorkflowDefinition` / `WorkflowTransition`.
- **Existing seeds (reference):** `backend/scripts/postgresql-seeds/07_gpon_order_workflow.sql`, `backend/scripts/add-invoice-rejection-loop-transitions.sql`, `backend/scripts/create-order-workflow-if-missing.sql`.
- **Canonical spec:** [db_workflow_baseline_spec.md](operations/db_workflow_baseline_spec.md).
