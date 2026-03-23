# CephasOps Scripts

Collection of utility scripts for database management, seeding, and maintenance.

---

## Seed Data Migration Scripts

### Complete Migration
```powershell
# Run complete migration (backup, remove code, import seeds, verify)
.\migrate-seed-data.ps1

# Dry run (preview changes)
.\migrate-seed-data.ps1 -DryRun

# Skip backup
.\migrate-seed-data.ps1 -SkipBackup

# Skip code removal (only import seeds)
.\migrate-seed-data.ps1 -SkipCodeRemoval
```

### Individual Steps

**1. Remove C# Seeding Code:**
```powershell
.\remove-seeding-code.ps1
.\remove-seeding-code.ps1 -DryRun  # Preview only
```

**2. Run PostgreSQL Seed Scripts:**
```powershell
cd postgresql-seeds
.\run-all-seeds.ps1

# Or with custom connection
.\run-all-seeds.ps1 -Host localhost -Port 5432 -Database cephasops -Username postgres
```

**3. Verify Seed Data:**
```powershell
.\verify-seed-data.ps1
```

---

## Password Hash Calculator

```powershell
.\calculate-password-hash.ps1
```

Calculates password hashes using the same algorithm as DatabaseSeeder (SHA256 + salt).

---

## Tenant Safety Audit (repo root)

From the **repository root**, run the tenant-safety audit script to scan C# code for risky multi-tenant patterns (IgnoreQueryFilters without company scoping, navigation fixup risk):

```powershell
.\tools\tenant_safety_audit.ps1
.\tools\tenant_safety_audit.ps1 -IncludeTests   # include backend/tests
```

See **docs/operations/TENANT_SAFETY_AUTOMATED_AUDIT.md** for what it checks, severities, and CI usage.

---

## Other Scripts

See individual script files for usage instructions.

---

**Last Updated:** 2025-01-05
