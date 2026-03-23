# EF Migrations Without Designer (Script-Only) Manifest

**Purpose:** List migrations that have a main `.cs` file but **no** `.Designer.cs`. These are not discovered by `dotnet ef migrations list` or `dotnet ef database update`. Apply them via idempotent SQL scripts when required.

**Runbook:** See [MIGRATION_RUNBOOK.md](../../scripts/MIGRATION_RUNBOOK.md). Do not delete or archive these migrations; apply via scripts and document here.

---

## How to Apply

1. Ensure the database is in a state where the migration’s dependencies already exist.
2. Run the idempotent script (e.g. `psql -f backend/scripts/apply-<Name>.sql`) or the path noted in the table.
3. If your process inserts into `__EFMigrationsHistory`, insert the migration id **only after** the script has been applied and verified.

---

## Manifest

| Migration ID | Name | Description | Idempotent Script |
|--------------|------|-------------|-------------------|
| 20260313140000 | AddEnterpriseSaaSColumnsAndTenantActivity | Adds HealthScore, HealthStatus, RateLimitExceededCount to TenantMetricsDaily; creates TenantActivityEvents table and index. | `backend/scripts/apply-AddEnterpriseSaaSColumnsAndTenantActivity.sql` |

---

## Counts (for validation)

- **Script-only migrations listed in this manifest:** 1 (as of 2026-03-13).  
- Update this count when adding or reclassifying migrations.  
- Other script-only migrations (e.g. legacy 47) may be documented in [EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md](EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md) or runbook; this manifest focuses on recent/enterprise script-only migrations.
