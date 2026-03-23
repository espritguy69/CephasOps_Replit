# Tenant Safety CI Enforcement (Phase 5)

This document describes how tenant-safety verification is enforced in CI so that future changes cannot silently regress tenant isolation.

## Where the checks live

- **Workflow:** `.github/workflows/tenant-safety.yml`
- **Trigger:** Pull requests and pushes to `main` / `master`, when paths under `backend/`, `analyzers/`, `tools/`, `.editorconfig`, or the workflow file itself change.
- **Runner:** `ubuntu-latest` with .NET 10.0.x.

The workflow runs a single job `tenant-safety`. Each step below must succeed; a failing step fails the job and the PR check.

## Mandatory steps (tenant-safety gate)

| Step name in CI | Purpose |
|-----------------|--------|
| **Build Application** | Ensures `CephasOps.Application` compiles. |
| **Tenant Safety Invariants** | Runs Phase 4 guard tests: tenant context assertions and SaveChanges tenant integrity. |
| **Build API with CEPHAS001 and CEPHAS004 enforced** | Builds `CephasOps.Api` and treats analyzer diagnostics CEPHAS001/CEPHAS004 as errors. |
| **Tenant Boundary Tests** | Runs API-level tenant-boundary tests (cross-tenant isolation at HTTP layer). |
| **Tenant Safety Gate** | Summary step; only runs if all previous steps passed. |

Other steps in the same workflow (analyzer tests, tenant safety audit script, tenant scope CI, architecture freeze, health dashboard, drift check, etc.) are also required to pass; this document focuses on the build and test steps that directly enforce tenant-safety verification.

## Exact commands

Commands are run from the **repository root**.

### Build Application

```bash
dotnet build backend/src/CephasOps.Application/CephasOps.Application.csproj --configuration Release --no-restore
```

(Restore is done earlier via `dotnet restore backend/tests/CephasOps.Application.Tests/...`.)

### Tenant Safety Invariants (Application.Tests)

```bash
dotnet test backend/tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj \
  --filter "FullyQualifiedName~TenantSafetyInvariantTests|FullyQualifiedName~SaveChangesTenantIntegrity" \
  --configuration Release \
  --no-restore \
  --logger "console;verbosity=normal"
```

Covers:

- `TenantSafetyInvariantTests`: AssertTenantContext, RequireCompany, RequireTenantOrBypass, RunWithTenantScopeAsync behaviour, SaveChanges with/without tenant context.
- `SaveChangesTenantIntegrityTests`: SaveChangesAsync validation for tenant-scoped entities.

### Build API (with analyzer enforcement)

```bash
dotnet build backend/src/CephasOps.Api/CephasOps.Api.csproj \
  --configuration Release \
  --no-restore \
  -p:WarningsAsErrors=CEPHAS001,CEPHAS004
```

### Tenant Boundary Tests (Api.Tests)

```bash
dotnet test backend/tests/CephasOps.Api.Tests/CephasOps.Api.Tests.csproj \
  --filter "FullyQualifiedName~TenantBoundaryTests" \
  --configuration Release \
  --no-restore \
  --logger "console;verbosity=normal"
```

Covers all tests in `TenantBoundaryTests` (list isolation, cross-tenant get/update/delete → 404/403, company context requirements).

## Why these checks are mandatory

- **Tenant isolation** is a hard requirement: one tenant must never see or modify another tenant’s data. Regressions would be security and compliance issues.
- **Phase 4 guards** (SaveChanges validation, TenantScopeGuard, executor usage) prevent accidental tenant-scope bypass in application code; the invariant tests lock that behaviour in.
- **Tenant Boundary Tests** lock in API-level isolation (controllers, auth, and services) so that cross-tenant requests return 403/404 and lists are scoped to the current tenant.

Making these steps mandatory in CI ensures that any PR that breaks tenant safety fails the pipeline and cannot merge until fixed.

## How PRs show tenant-safety status

- The workflow name is **Tenant Safety**. On a PR, the GitHub Actions check appears under that name.
- If any step fails (including **Tenant Safety Invariants** or **Tenant Boundary Tests**), the **Tenant Safety** check is red and the pipeline is failed.
- A separate job **Tenant Safety Report (PR)** runs after the main job and posts or updates a comment on the PR with a Tenant Safety Health summary (score, violations, executor adoption, etc.). That report is informational; the gate is the main **tenant-safety** job.

## Local verification

Before pushing, you can run the same test filters locally:

```bash
# From repo root

# Application: tenant safety invariants + SaveChanges integrity
dotnet test backend/tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj \
  --filter "FullyQualifiedName~TenantSafetyInvariantTests|FullyQualifiedName~SaveChangesTenantIntegrity" \
  -c Release

# Api: tenant boundary tests
dotnet test backend/tests/CephasOps.Api.Tests/CephasOps.Api.Tests.csproj \
  --filter "FullyQualifiedName~TenantBoundaryTests" \
  -c Release
```

## References

- **Workflow:** `.github/workflows/tenant-safety.yml`
- **Phase 4 guards:** `docs/tenant_boundary_tests/TENANT_SAFETY_GUARDS.md`
- **Boundary test summary:** `docs/tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md`
- **Backend CI operations:** `backend/docs/operations/TENANT_SAFETY_CI.md`
