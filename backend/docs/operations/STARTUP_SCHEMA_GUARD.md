# Startup Schema Guard — Platform Safety

**Status:** Active in Development, Staging, and Production.

**Purpose:** Fail fast at application startup when critical schema objects are missing, instead of starting and later failing at runtime with "relation does not exist" or similar errors. Prevents the application from serving requests when critical schema drift exists, even if `__EFMigrationsHistory` appears current.  
**Scope:** Development, staging, and production. Skipped when `ASPNETCORE_ENVIRONMENT=Testing` (in-memory database).

**Behavior:** If all guarded tables exist, startup continues. If any guarded table is missing, startup fails fast with clear operator guidance (see §4).

---

## 1. What the guard checks

The guard runs **once at startup**, after the application is built and the database connection is available, and **before** the app is considered ready to serve requests.

It checks that the following **tables exist** in the **public** schema:

| Table | Reason |
|-------|--------|
| **ConnectorDefinitions** | Required by external integration bus; was missing after a past partial migration. |
| **ConnectorEndpoints** | Same. |
| **ExternalIdempotencyRecords** | Same. |
| **OutboundIntegrationAttempts** | Same. |

No other tables or columns are checked. This is an intentional, narrow list based on objects that have previously caused real runtime failures when missing despite migration history showing applied. **Full schema readiness is operator-owned:** apply the idempotent migration script (or bundle) and run `backend/scripts/check-migration-state.sql` to verify; the guard only verifies these four tables at startup.

---

## 2. Why it exists

We had an incident where:

- `__EFMigrationsHistory` showed migrations (including AddExternalIntegrationBus) as applied.
- The four integration-bus tables above did **not** exist in the database (due to a partial/repair script that recorded the migration without applying its full schema).
- The application started successfully but later failed at runtime with "relation does not exist" when code touched those tables.

The startup schema guard prevents that situation: if any of the guarded tables are missing, the application **does not start** and logs a clear, operator-friendly error.

---

## 3. Startup behavior

- **Success:** All four tables exist → guard logs a single informational message (`Startup schema guard: all 4 critical tables present.`) and startup continues.
- **Failure:** One or more guarded tables are missing → guard logs an error listing the missing table(s), then throws `InvalidOperationException`. The application process exits (or throws during startup). No requests are served.

The guard does **not**:

- Apply migrations.
- Write to the database.
- Depend on MigrationAudit.
- Check the rest of the schema (only the four tables above).

---

## 4. What operators should do when the guard fails

1. **Do not** try to "fix" by inserting or removing rows in `__EFMigrationsHistory` without applying the full migration.
2. **Run** `backend/scripts/check-migration-state.sql` (or equivalent) to confirm which objects are missing.
3. **Remediate** by applying the **full** migration (bundle or full idempotent script) so that the missing tables are created. If a documented remediation script exists for the missing objects (e.g. `apply-schema-drift-remediation.sql` for the four integration-bus tables), use it and then verify.
4. **Verify** again with `check-migration-state.sql` until all expected tables exist.
5. **Restart** the application; the guard should then pass.

See **EF_MIGRATION_SCHEMA_GUARD.md** (§7 “If drift is detected again”) for the full remediation workflow.

---

## 5. Relationship to other mechanisms

| Mechanism | Role |
|-----------|------|
| **EF migrations / __EFMigrationsHistory** | EF uses history to decide which migrations to apply. The guard does **not** replace or modify this; it only checks that a small set of critical tables exist after the fact. |
| **check-migration-state.sql** | Script for operators to run **after** applying migrations. It includes the same four tables (and more). The guard is an automatic startup check; the script is for manual/post-deploy verification. |
| **MigrationAudit** | Operational audit of deployment events (who, when, verification result). The guard does not read or write MigrationAudit. |
| **Database health check** | The `/health` endpoint’s database check only verifies connectivity. The startup schema guard verifies that specific tables exist; it runs once at startup and is not part of the health endpoint. |

---

## 6. Configuration and environment

- **No configuration flag** — The guard runs whenever the application starts and the environment is not `Testing`. There is no switch to disable it in Development, staging, or production.
- **Testing** — When `ASPNETCORE_ENVIRONMENT=Testing`, the guard is **skipped** (in-memory database is used and the guarded tables are not required in the same way).

---

## 7. Implementation location

- **Class:** `CephasOps.Api.Startup.StartupSchemaGuard`
- **Invocation:** In `Program.cs`, after `builder.Build()` and the first startup scope (BackgroundJobs log, PlatformGuardLogger), in a dedicated scope that resolves `ApplicationDbContext` and runs `EnsureCriticalTablesExistAsync`. If the method throws, startup fails before `app.Run()`.
