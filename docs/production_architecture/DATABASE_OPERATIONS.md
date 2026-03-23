# Database Operations, Backup, and Recovery

**Purpose:** Production database operations for PostgreSQL: migrations, backup, restore, retention, and health.

---

## 1. Migration application strategy

- **Apply migrations** as a separate step **before** deploying new application code that depends on the new schema. Run `dotnet ef database update` (or idempotent SQL script) from a migration runner or release pipeline.
- **Order:** (1) Backup (optional but recommended). (2) Apply migrations (single transaction where possible). (3) Deploy application. (4) Verify health.
- **Rollback:** Application code should remain compatible with the previous migration state if you need to roll back the app; avoid deploying app that requires a new column and then rolling back DB. Prefer backward-compatible migrations (add column nullable, then backfill, then make non-null in a later migration).
- **Design-time:** Use `IDesignTimeDbContextFactory` or equivalent; connection from config. Do not run migrations from the main API process at startup in production (run from pipeline or dedicated job).

---

## 2. Pre-deploy database checks

- **Connectivity:** Health check (e.g. /health/ready with DatabaseHealthCheck) verifies connection.
- **Pending migrations:** In CI/CD, run `dotnet ef migrations list` or script diff; fail the gate if there are unapplied migrations and the release includes migration.
- **Schema guard:** Use `backend/scripts/check-migration-state.sql` (or equivalent) to verify expected tables/columns after migration. See backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md if present.

---

## 3. Backup cadence

- **Full backup:** Daily at minimum; retain 7–30 days per policy.
- **Point-in-time recovery (PITR):** If using PostgreSQL WAL archiving or managed service (e.g. RDS, Azure PostgreSQL), enable PITR and retain WAL for 7–14 days so you can restore to a point in time.
- **Pre-migration backup:** Take a backup immediately before applying migrations in production.

---

## 4. Restore testing guidance

- **Periodically** restore a backup to a non-production environment and run smoke tests (app starts, key queries work, tenant scope intact).
- **Document** restore procedure: stop app, restore DB from backup (or PITR), run migrations if needed, start app, verify.

---

## 5. Point-in-time recovery

- Align with PostgreSQL WAL archiving or managed service PITR. Restore to a timestamp before the incident; re-apply any transactions after that point manually if necessary, or accept data loss for that window.
- **Tenant data:** Restore is whole-DB; all tenants restored together. No per-tenant restore in current design.

---

## 6. Index maintenance

- **Vacuum/Analyze:** Run VACUUM (and ANALYZE) periodically (e.g. weekly or via autovacuum). Monitor bloat and long-running queries.
- **Indexes:** Tenant query indexes (CompanyId, CompanyId+CreatedAt, etc.) are in place; monitor slow query log and add indexes if new patterns appear. Avoid redundant indexes.

---

## 7. Large-table growth monitoring

- **Tables to watch:** Orders, JobExecutions, EventStore, Notifications, Files (metadata), TenantMetricsDaily, TenantAnomalyEvents, StockLedgerEntry, OrderStatusLog.
- **Actions:** Partitioning (e.g. by date) for very large tables if growth is unbounded; retention jobs to archive or delete old data (see retention below).
- **File metadata:** Files table grows with uploads; storage lifecycle and retention policy should align with business rules.

---

## 8. Retention guidance

| Data | Retention (suggested) | Action |
|------|------------------------|--------|
| **TenantMetricsDaily** | 13+ months | Keep for billing/reporting; archive older if needed. |
| **TenantMetricsMonthly** | Indefinite (billing) | Keep. |
| **TenantAnomalyEvents** | 90 days | Delete or archive older; or configurable. |
| **JobExecutions** | 90 days for completed/failed | Purge or archive; keep Pending/Running. |
| **EventStore** | Per policy (e.g. 1–2 years) | Archive or delete old; ensure replay/rebuild still possible for required window. |
| **Notifications / dispatch** | 30–90 days | Retention job already exists; tune. |
| **InboundWebhookReceipt** | Per EventPlatformRetentionOptions | Platform retention service. |
| **File metadata** | Align with storage lifecycle and legal hold | Soft delete; hard delete after archive period. |
| **Audit / impersonation logs** | Per compliance | Retain per policy; do not shorten without review. |

---

## 9. Recovery checklist

1. **Incident:** Identify scope (full DB corruption, single table, accidental delete).
2. **Stop writes:** If necessary, stop application or put in read-only to prevent further damage.
3. **Backup:** Ensure latest backup is safe; use PITR if available.
4. **Restore:** Restore from backup or PITR to new instance or over existing (with caution).
5. **Verify:** Run schema check and smoke queries; verify tenant data isolation.
6. **Re-apply migrations:** If restore was to an older backup, apply migrations again.
7. **Resume app:** Start application; monitor errors and Guardian.
8. **Communicate:** Notify tenants if data loss or downtime occurred.

---

## 10. Database health indicators

- **Connection:** Health check succeeds; no connection pool exhaustion.
- **Replication lag:** If using read replicas, lag < threshold (e.g. 1 s).
- **Long-running queries:** Alert on queries > N seconds; identify and tune or kill.
- **Lock contention:** Monitor lock wait events; tune batch size and job concurrency if needed.
- **Disk:** Free space and growth rate; alert before full.

---

## 11. References

- [CI_CD_PIPELINE.md](CI_CD_PIPELINE.md) – Migration gate and deployment order.
- [backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md](https://github.com/CephasOps/CephasOps/blob/main/backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md) – Schema verification script.
