# Migration Governance

**Date:** March 2026  
**Purpose:** Rules for EF Core migrations so the chain stays append-only, the snapshot matches the migration chain, and schema changes are safe and repeatable. Enforcement is via existing migration hygiene CI and validators; this document is the single reference for migration governance.

**Related:** [migration_integrity_watch.md](migration_integrity_watch.md) | [EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md](EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md) | [architecture_guardrails.md](../architecture/architecture_guardrails.md)

---

## 1. Rules

### 1.1 Migrations must be append-only

- **Do not** modify or delete existing migration files. Any schema or model change must be a **new** migration.
- **Do not** rename migrations or change their timestamp/order in the chain.
- **Rationale:** The chain is applied in order; changing history breaks reproducibility and can break environments that already applied the changed migration.

### 1.2 Snapshot must match migration chain

- **ApplicationDbContextModelSnapshot.cs** is the EF model at the latest migration. It must stay in sync with the migration chain.
- **Do not** edit the snapshot unless the task is **explicit snapshot reconciliation** (e.g. a one-time sync migration approved and documented).
- **Rationale:** Editing the snapshot casually causes the next generated migration to be a large “sync” migration that recreates or alters many objects; this is a governance event and must be deliberate.

### 1.3 No manual schema edits in tracked environments

- Schema changes in development, staging, or production must go through **EF migrations** (or documented **script-only** migrations listed in the no-Designer manifest).
- **Do not** apply ad-hoc SQL (e.g. CREATE TABLE, ALTER TABLE) to tracked databases without a corresponding migration or documented script in the manifest.
- **Rationale:** Manual edits drift from the model and snapshot; future migrations can fail or overwrite changes.

### 1.4 Migration scripts must pass integrity checks

- Before merging any change that adds or modifies migrations:
  1. Run **backend/scripts/validate-migration-hygiene.ps1**. It must pass.
  2. Create migrations using **backend/scripts/create-migration.ps1 -MigrationName "DescriptiveName"** so that both main `.cs` and `.Designer.cs` are produced (unless the migration is intentionally script-only and documented in the manifest).
- **Rationale:** The validator enforces that new migrations have a Designer and that counts match the documented baseline; the create script ensures a consistent, named migration.

---

## 2. Integrity checks (what they enforce)

| Check | Script / process | Enforces |
|-------|-------------------|----------|
| Migration hygiene | `backend/scripts/validate-migration-hygiene.ps1` | New migrations have `.Designer.cs`; migration counts align with baseline; no accidental script-only. |
| Create path | `backend/scripts/create-migration.ps1 -MigrationName "Name"` | Consistent naming and structure for new migrations. |
| CI | `.github/workflows/migration-hygiene.yml` | On PR/push that touch Migrations, validator runs; failure blocks merge. |

See [migration_integrity_watch.md](migration_integrity_watch.md) for chain safety observations, schema drift risk, and operational cautions.

---

## 3. Script-only migrations

- Some migrations are **intentionally** script-only (main `.cs` only, no `.Designer.cs`). They are listed in **docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md** and reflected in the validator baseline.
- **Do not** add a script-only migration without: (1) classifying it as intentional, (2) adding it to the manifest, (3) updating the validator expected count and any docs that state migration counts.
- Apply script-only migrations via idempotent SQL or runbooks; do not rely on `dotnet ef database update` to create those objects.

---

## 4. References

- [migration_integrity_watch.md](migration_integrity_watch.md) – Watchdog for chain safety and drift.
- [EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md](EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md) – Usability and conditions.
- [EF_FUTURE_AUTHORING_RULES.md](EF_FUTURE_AUTHORING_RULES.md) – How to add and apply migrations.
- [backend/scripts/MIGRATION_RUNBOOK.md](../../backend/scripts/MIGRATION_RUNBOOK.md) – Operational runbook.
- [.cursor/rules/ef-migration-governance.mdc](../../.cursor/rules/ef-migration-governance.mdc) – Cursor migration governance rule.
