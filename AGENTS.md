# AGENTS.md

## Project overview

CephasOps is a multi-company ISP operations management platform. **.NET standard:** CephasOps is strictly standardized on .NET 10.0.x SDK and net10.0; all application and test projects target net10.0, and CI workflows use `dotnet-version: '10.0.x'`. for managing work orders, service installers, inventory, billing, payroll, and P&L analytics for ISP subcontracting in Malaysia.

- **Backend** (`backend/`): ASP.NET Core 10, Clean Architecture (Domain/Application/Infrastructure/Api), EF Core + PostgreSQL, JWT auth, Syncfusion, Serilog.
- **Frontend** (`frontend/`): React 18 + TypeScript + Vite, Syncfusion UI components, TanStack Query, Tailwind CSS v4, React Hook Form + Zod.
- **Frontend-SI** (`frontend-si/`): Separate service installer mobile-first app.

## Cursor Cloud specific instructions

### Prerequisites (installed by update script)

- .NET 10 SDK (`$HOME/.dotnet`)
- PostgreSQL 16 (service must be started: `sudo pg_ctlcluster 16 main start`)
- Node.js 22+ (pre-installed)

### Database setup (one-time)

The database backup at `supabase_backup_20251210_230102.sql` seeds the `cephasops` database. After restoring, run the idempotent EF Core migration script to apply newer migrations:

```
dotnet ef migrations script --idempotent --output /tmp/migrations.sql --project backend/src/CephasOps.Api
PGPASSWORD='J@saw007' psql -h localhost -p 5432 -U postgres -d cephasops -f /tmp/migrations.sql
```

The `dotnet ef database update` command fails with `PendingModelChangesWarning` because the design-time factory does not suppress it (the runtime `Program.cs` does). Use the idempotent SQL script approach instead.

If the generated migration script does not yet include **OperationalInsights**, **BillingPlanFeatures**, or **TenantFeatureFlags**, run `backend/scripts/apply-OperationalInsights-And-FeatureFlags.sql` (idempotent) after the main migration script. After any migration deployment, run `backend/scripts/check-migration-state.sql` to verify schema (see `backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md`).

### Running services

| Service | Command | Port | Notes |
|---------|---------|------|-------|
| Backend API | `cd backend/src/CephasOps.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://0.0.0.0:5000` | 5000 | Swagger at `/swagger` |
| Frontend | `cd frontend && npm run dev` | 5173 | Proxies `/api` to backend at :5000 |

### Credentials

- **Postgres**: `Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=J@saw007` (in `appsettings.Development.json`)
- **Admin login**: `simon@cephas.com.my` / `J@saw007`
- **Syncfusion license key**: in `frontend/.env.example` — copy to `frontend/.env`; backend has a fallback in `Program.cs`
- Never commit credentials to version control.

### Testing

- **Backend tests**: `cd backend/tests/CephasOps.Application.Tests && dotnet test` (Application); `cd backend/tests/CephasOps.Api.Tests && dotnet test` (Api)
- **Frontend tests**: `cd frontend && npx vitest run` (some workflow tests may fail due to pre-existing mock issues)
- **Frontend build**: `cd frontend && npm run build`
- **E2E (Playwright)**: `cd frontend && npx playwright test` (requires running backend + frontend). See `frontend/e2e/README.md` for env vars, projects, and CI.

**SaaS regression:** For PRs that touch tenant-scoped services, background jobs, EventStore, financial paths, or SI-app, run the relevant SaaS isolation tests (see `docs/remediation/SAAS_TEST_COVERAGE_INDEX.md`). Application tests include TenantIsolation, Events (EventStore/replay), FinancialIsolationGuard, and Integration tenant/API tests; ensure these pass before merge.

### Gotchas

- PowerShell scripts (`*.ps1`) in the repo are Windows-focused with hardcoded `C:\` paths. On Linux, run `dotnet run` / `npm run dev` directly.
- The Supabase backup has harmless errors about missing roles (`anon`, `authenticated`, `supabase_admin`) — these are Supabase platform roles not needed locally.
- Backend uses `dotnet watch` for hot reload in dev, but `dotnet run` is more reliable for initial setup.
- The `.cursorrules` file has comprehensive code generation rules — read it before making changes.
- `frontend/vite.config.ts` proxies `/api` requests to `http://localhost:5000`, so the frontend dev server handles CORS automatically.
