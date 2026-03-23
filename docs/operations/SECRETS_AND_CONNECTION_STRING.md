# Secrets and Connection String

## Connection string

The application reads the PostgreSQL connection string from configuration key `DefaultConnection`. **Do not commit connection strings or passwords to the repository.**

### Setting the connection string

- **Environment variable (recommended):** Set `ConnectionStrings__DefaultConnection` to the full connection string.
  - Example (bash): `export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=cephasops;Username=cephasops_app;Password=YOUR_SECRET;SslMode=Disable;Include Error Detail=true"`
  - Example (PowerShell): `$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=cephasops;Username=cephasops_app;Password=YOUR_SECRET;SslMode=Disable;Include Error Detail=true"`
- **appsettings.json** has an empty placeholder; in production always use environment or a secret store (e.g. Azure Key Vault, AWS Secrets Manager).

### Application user

Production should use the dedicated database user `cephasops_app` (non-superuser). Create it once using:

- Script: `backend/scripts/postgresql-seeds/00_create_cephasops_app_user.sql`
- Replace `<app_password>` with a strong password and run as postgres (or equivalent). Store the password only in a vault or env.

### Scripts that need database access

Scripts under `backend/scripts/` and `backend/src/CephasOps.Infrastructure/Persistence/Migrations/` that connect to PostgreSQL must receive the connection string via:

- Parameter: `-ConnectionString "Host=...;Database=...;Username=...;Password=..."`
- Or environment: `$env:DefaultConnection` (PowerShell)

Do not hardcode connection strings or passwords in scripts. Document in each script: "Do not commit connection strings; pass via parameter or environment."

### Logging

- Verify no logger or middleware logs `IConfiguration` or the connection string.
- Runbook: "Verify no log sink (e.g. Seq, App Insights) receives ConnectionStrings or secrets."

### Checklist (ongoing)

- [ ] Production secrets stored in vault or managed secret store.
- [ ] No connection strings or passwords committed; appsettings and .cursor/rules use placeholders or env references only.
- [ ] Scripts accept connection string via parameter or `$env:DefaultConnection`.
- [ ] CI/CD does not log ConnectionStrings or *Password*.
- [ ] Rotate `cephasops_app` password on a schedule (e.g. quarterly) and update secret store only.
