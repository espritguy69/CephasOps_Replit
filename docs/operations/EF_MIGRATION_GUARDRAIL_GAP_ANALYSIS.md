# EF Migration Guardrail — Gap Analysis

**Purpose:** Identify what is currently missing for migration protection so guardrails can be added without modifying historical migrations or the snapshot.

**Context:** 139 total migrations; 95 active (EF-discoverable); 44 script-only (no Designer). Snapshot drift deferred. No migrations deleted or archived.

---

## 1. Current state inspected

### 1.1 Scripts

| Script | Purpose | Gap |
|--------|---------|-----|
| `backend/scripts/audit-migration-designers.ps1` | Lists migrations missing Designer; reports counts. | Read-only. No exit code for CI; no "fail on new no-Designer" mode; no large-migration check. |
| `backend/scripts/create-migration.ps1` | Runs `dotnet ef migrations add` from Infrastructure with Api startup. | Does not verify Designer was created after add; no post-add hygiene check. |
| `backend/scripts/verify-migration.ps1` | (Exists; content not migration-hygiene focused in this audit.) | — |
| `backend/scripts/check-workflow-migrations.ps1` | DB check for WorkflowDefinitions table/history. | DB-dependent; not a file-level guardrail. |
| `backend/scripts/apply-migration.ps1`, `apply-migration-quick.ps1` | Apply migrations. | No pre-check for Designer presence. |

**Missing:** A single validation script that (a) fails or warns when any migration lacks a Designer, (b) can detect "new" no-Designer (e.g. vs baseline 44), (c) optionally flags suspiciously large migration files.

### 1.2 GitHub workflows

| Workflow | Relevance | Gap |
|----------|-----------|-----|
| `.github/workflows/e2e.yml` | Runs `dotnet ef database update` for E2E. | No step that checks for new migrations without Designer before/after. No guardrail on PR. |
| `.github/workflows/versioning.yml` | Versioning. | No migration checks. |
| `.github/workflows/parser-governance.yml`, `diagnostics-manual.yml` | Other. | No migration checks. |

**Missing:** No CI job that validates migration hygiene (Designer presence, or recommended checklist in docs). Adding a dedicated workflow or step would close the gap; if too invasive, document a recommended CI step in markdown.

### 1.3 Documentation

| Doc | Content | Gap |
|-----|---------|-----|
| `docs/MIGRATION_HYGIENE.md` | Inventory, no-Designer list, strategy. | No explicit "official safe workflow" or "run this before PR." No snapshot drift early-warning. |
| `docs/operations/EF_FUTURE_AUTHORING_RULES.md` | Rules for applying, adding, sync, re-baseline. | Mentions large migration as drift; could be stronger "stop and escalate" guidance. |
| `docs/operations/EF_MIGRATION_FINAL_CLEANUP_DECISION.md` | Final cleanup answers. | No guardrail or validation reference. |
| `backend/scripts/MIGRATION_RUNBOOK.md` | Operations, bundle, repair scripts. | Contains stale "24 legacy" (should be 44). No mandatory pre-migration checklist; no validation script reference. |

**Missing:** One short "official safe workflow" doc; snapshot drift early-warning in workflow + future rules + runbook; validation script documented; optional CI recommendation.

### 1.4 Developer onboarding / conventions

- No root-level or backend-level README that mandates "run validate-migration-hygiene before migration PR."
- No pre-commit hook or CONTRIBUTING note for migrations.
- `create-migration.ps1` does not run a post-add check.

**Missing:** Clear workflow doc + validation script + "run before PR" in docs; optional mention in CONTRIBUTING or AGENTS.md.

---

## 2. Protection gaps summary

| Gap | Severity | Mitigation |
|-----|----------|------------|
| No automatic check for missing Designer on new migrations | High | Add `validate-migration-hygiene.ps1`; document run before PR; optionally CI. |
| No warning for suspiciously large new migrations (snapshot drift) | Medium | Document in workflow + future rules: "if huge, stop; do not commit blindly; escalate." |
| No explicit pre-commit / CI guidance for migrations | Medium | Document in EF_SAFE_MIGRATION_WORKFLOW.md and optionally in a recommended CI step. |
| No single "official" command template + post-add verification | Medium | Safe workflow doc + create-migration.ps1 can recommend running validation after add. |
| No validation checklist for migration PR review | Medium | Add short checklist to guardrail decision or workflow doc. |
| Stale counts in MIGRATION_HYGIENE.md §7 and runbook "24" | Low | Fix to 139 / 95 / 44 and "44" where applicable. |

---

## 3. Out of scope (no action)

- Modifying existing migration logic or snapshot.
- Regenerating Designers for the 44.
- Deleting, archiving, renaming, or reordering migrations.
- Fixing snapshot drift in this pass.

---

## 4. Next steps (guardrail implementation)

1. Create **EF_SAFE_MIGRATION_WORKFLOW.md** — strict, short workflow.
2. Create **validate-migration-hygiene.ps1** — non-destructive checks; document how to run.
3. Add **snapshot drift early-warning** to workflow, future rules, and runbook.
4. Optionally add **CI step** or document recommended CI step.
5. Create **EF_MIGRATION_GUARDRAIL_DECISION.md** — what was added, what developers must do.
