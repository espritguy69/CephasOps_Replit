# Architecture Known Gaps

**Date:** April 2026  
**Status:** Active — these are unresolved architecture risks

---

## AG-1: Missing Solution File (.sln)

**Severity:** High  
**Location:** `backend/` has no `.sln` file  
**Impact:** Standard .NET tooling (Visual Studio, `dotnet build` at solution level, `dotnet test --all`) won't work. Developers must build individual projects.  
**Fix:** Generate `CephasOps.sln` linking Api, Application, Domain, Infrastructure, Tests, and Analyzers projects.

---

## AG-2: Application → Infrastructure Dependency Violation

**Severity:** Medium  
**Location:** `CephasOps.Application.csproj` references `CephasOps.Infrastructure`  
**Impact:** Breaks Clean Architecture's dependency rule. Application layer should depend only on Domain, with Infrastructure implementing Application interfaces.  
**Current Justification:** Comment in .csproj says "pragmatic choice" — Application needs `ApplicationDbContext` directly for complex queries.  
**Fix:** Extract repository interfaces into Application layer, implement in Infrastructure. Significant refactor.

---

## AG-3: Broken Tenant Safety Analyzer Reference

**Severity:** High  
**Location:** `CephasOps.Api.csproj` references `../../../analyzers/CephasOps.TenantSafetyAnalyzers.csproj`  
**Impact:** If analyzer project is not found at that relative path, tenant safety rules (`CEPHAS001`–`CEPHAS004`) are silently disabled at build time. No build error is produced — the protection simply doesn't run.  
**Fix:** Verify analyzer project location matches the reference path. Add a CI step that asserts the analyzer produced at least one diagnostic.

---

## AG-4: Syncfusion Version Mismatch Across Projects

**Severity:** Low  
**Location:**
- `CephasOps.Api`: `Syncfusion.Licensing` **31.2.15**
- `CephasOps.Application`: Syncfusion packages **31.1.17**
- `CephasOps.Infrastructure`: Syncfusion packages **31.1.17**

**Impact:** Potential assembly loading conflicts or licensing issues at runtime.  
**Fix:** Align all Syncfusion packages to a single version.

---

## AG-5: ExcelDataReader Version Mismatch

**Severity:** Low  
**Location:**
- `CephasOps.Api` + `CephasOps.Application`: version **3.6.0**
- `ParserPlayground`: version **3.7.0**

**Impact:** Different parsing behavior between dev testing and production.  
**Fix:** Align versions.

---

## AG-6: No Shared UI Library Between Frontends

**Severity:** Medium  
**Location:** `frontend/src/components/ui/` and `frontend-si/src/components/ui/`  
**Impact:** Button, Card, Badge, and other base components are duplicated. UI inconsistencies and double maintenance burden.  
**Fix:** Extract shared components into a workspace package (pnpm workspaces or similar).

---

## AG-7: Orphaned Root-Level Files

**Severity:** Low  
**Location:** Project root contains 18 files that don't belong:
- `main.py`, `pyproject.toml` — Python artifacts, not part of .NET/React stack
- `supabase_backup_20251210_230102.sql` — 1.1MB database dump
- 10+ PowerShell scripts (`start-all.ps1`, `sync-pc.ps1`, etc.) — local dev utilities
- SQL fragments (`phase11_only.sql`, `check_migration.sql`, etc.)

**Impact:** Cluttered project root, repo size inflation.  
**Fix:** Move scripts to `scripts/`, SQL to `backend/scripts/historical-fixes/`, delete `main.py`/`pyproject.toml`.

---

## AG-8: 13 Orphaned SQL Fix Scripts in `backend/` Root

**Severity:** Low  
**Location:** `backend/*.sql` (e.g., `fix-all-rowversion.sql`, `fix-soft-delete-columns.sql`)  
**Impact:** Unclear if already applied. Clutters backend directory.  
**Fix:** Move to `backend/scripts/historical-fixes/` or delete if confirmed applied.

---

## AG-9: Hardcoded Credentials in PowerShell Scripts

**Severity:** Critical (Security)  
**Location:** Multiple `.ps1` files contain hardcoded password `J@saw007` and Supabase connection details  
**Impact:** Credential exposure in version control  
**Fix:** Replace with environment variable references. Rotate the exposed password.
