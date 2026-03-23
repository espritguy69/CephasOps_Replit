# Rollback and Database Restore

This document clarifies what the repository provides for rollback and what **operators must own** for database restore. No application code performs DB restore; it is an operational runbook requirement.

---

## What the repository provides

- **infra/scripts/rollback.ps1** — Reverts **deployment** only:
  - **Docker:** `docker compose down` (stops stack; to restore a previous image, re-run deploy with the desired tag).
  - **Kubernetes:** `kubectl rollout undo deployment/cephasops-api` (reverts to previous revision).

The script does **not**:
- Restore the database from backup
- Run point-in-time recovery
- Modify `__EFMigrationsHistory` or schema

---

## Operator-owned: database restore

Database backup and restore are **not** implemented in this repository. They are an **operator responsibility**.

### Pre-launch action items (required before first tenant)

1. **Document** your database backup procedure (e.g. PostgreSQL `pg_dump`, managed backup, or cloud backup).
2. **Document** your restore procedure (restore from backup, verify schema with `backend/scripts/check-migration-state.sql`).
3. **Document** RPO (recovery point objective) and RTO (recovery time objective).
4. **Test** a full restore in a non-production environment and confirm the application can connect and run after restore.
5. **Decide** under what incident conditions you will perform a DB restore (e.g. data corruption, failed migration, rollback requiring prior DB state) and who authorizes it.

### When rollback requires DB restore

- If you revert application deployment to a **previous version** that expects an **older schema**, you may need to restore the database to a backup from that era, or run a down-migration path if you have one. Document this decision and procedure.
- If a bad migration or script was applied, fix forward (preferred) or restore from backup and re-apply correct migrations; document the chosen approach.

### See also

- [GO_LIVE_CHECKLIST.md](GO_LIVE_CHECKLIST.md) — Includes checkbox for "Database backups verified (restore test, RPO/RTO documented)" and rollback/restore sign-off.
- [INCIDENT_RESPONSE.md](INCIDENT_RESPONSE.md) — For tenant data and database incident response.
- [backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md](../../backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md) — Migration and schema verification workflow.
