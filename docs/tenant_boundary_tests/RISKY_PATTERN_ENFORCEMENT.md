# Risky Query Pattern Enforcement

This document describes how CephasOps reduces the risk of tenant-boundary regressions from known dangerous patterns, and what remains review-based.

## Dangerous patterns

1. **FindAsync / FirstOrDefaultAsync** on tenant-scoped entities without tenant criteria (or without TenantScope set).
2. **IgnoreQueryFilters()** on entities that have a tenant query filter.
3. **FromSqlRaw / ExecuteSqlRaw** without tenant filter in the SQL or without running inside tenant scope.
4. **Controllers** accepting `companyId` or `tenantId` on tenant business endpoints (allowing override by caller).
5. **Enrichment lookups** using unscoped `FindAsync(id)` for tenant-owned reference data (e.g. Partner, OrderType by id without company filter).

## What is enforced automatically

- **Tenant boundary tests**: The automatic tenant boundary test suite (Users, Warehouses, Departments, PaymentTerms, TimeSlots, Reports, companyId override) catches **behavioral** regressions: e.g. list returning other-tenant data, or by-id returning 200 for another tenant. Any change that removes or weakens tenant checks can cause these tests to fail.
- **Existing analyzers**: If the repo uses **CephasOps.TenantSafetyAnalyzers** (or similar), they may flag `IgnoreQueryFilters` or unscoped `FindAsync` in tenant-scoped code; see analyzer docs and `.editorconfig` for rules.
- **Code review**: PR reviews should check for the patterns above when touching persistence or tenant-scoped APIs.

## What is not enforced automatically (review-based)

- **Grep/scan**: No automated repo scan is run in CI today that fails the build on presence of `IgnoreQueryFilters()` or `FromSqlRaw` in tenant paths. Adding a lightweight script or analyzer that fails on allowlist violations is possible but must be low-noise (e.g. allowlisted files for legitimate platform-only use).
- **Allowlist**: A machine-readable allowlist of permitted uses (e.g. “IgnoreQueryFilters only in files X, Y for platform cleanup”) is not in place; could be added if the team wants strict enforcement.

## Recommendations

1. **Run boundary tests in CI**: Ensure the tenant boundary test suite runs on every PR and that failures are release-blocking for tenant-sensitive changes.
2. **Document patterns**: Keep this doc and the tenant safety architecture docs as the single source of truth for “what not to do.”
3. **Optional analyzer**: If the TenantSafetyAnalyzers project is enabled, configure rules for tenant-scoped code (e.g. warn on `IgnoreQueryFilters` in Application/Infrastructure unless in an allowlisted type).
4. **Optional scan**: A pre-commit or CI step that greps for `IgnoreQueryFilters`, `FromSqlRaw`, `ExecuteSqlRaw` in tenant-scoped namespaces and fails (or warns) with a link to this doc can be added; allowlist any legitimate platform-only usage.

## Summary

- **Automatic**: Behavioral protection via the tenant boundary test suite; optional analyzer if configured.
- **Review-based**: Risky patterns (unscoped FindAsync, IgnoreQueryFilters, raw SQL, companyId override) are documented and should be checked in code review; no noisy auto-fail on pattern presence unless an allowlist is maintained.
