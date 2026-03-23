# CephasOps Roslyn Analyzers

Roslyn analyzers that run during build and in the IDE to detect multi-tenant safety issues in CephasOps.

## Project

- **CephasOps.TenantSafetyAnalyzers** — Tenant-safety rules (IgnoreQueryFilters, EF fixup risk, query-by-Id-only).
- **CephasOps.TenantSafetyAnalyzers.Tests** — Unit tests for the analyzers.

## Diagnostic IDs

Each rule has a clear title, message, description, and **help link** in the analyzer metadata (pointing to `backend/docs/operations/TENANT_SAFETY_ANALYZER.md`). Use the Error List / lightbulb to open the help link.

| ID | Severity | Meaning |
|----|----------|--------|
| **CEPHAS001** | Warning | Unsafe `IgnoreQueryFilters()` on a tenant-scoped DbSet/entity with no explicit CompanyId/TenantId constraint in the query chain. |
| **CEPHAS002** | Warning | `IgnoreQueryFilters()` with ambiguous or non-obvious tenant scoping (review-oriented; may be intentional e.g. Auth, platform jobs). |
| **CEPHAS003** | Warning | Possible EF Core relationship fixup risk when mutating a navigation target without clearing invalid navigation state. |
| **CEPHAS004** | Warning | Tenant-scoped entity queried by Id (or key) only without explicit company scoping. |

## How to run

- **IDE:** Open the solution and build; diagnostics appear in the Error List and as squiggles.
- **Build:** Add a project reference from the projects you want to analyze (see below). Then `dotnet build` runs the analyzers.
- **Tests:** `cd analyzers/CephasOps.TenantSafetyAnalyzers.Tests && dotnet test`

## Enabling the analyzer in the backend

Add a reference to the analyzer project so it runs on `backend/src` code:

In **CephasOps.Api.csproj** (or a shared Directory.Build.props):

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\analyzers\CephasOps.TenantSafetyAnalyzers\CephasOps.TenantSafetyAnalyzers.csproj"
                    ReferenceOutputAssembly="false"
                    OutputItemType="Analyzer"
                    PrivateAssets="all" />
</ItemGroup>
```

Use a path relative to your project. After adding, rebuild; CEPHAS001/002/003 will appear in the IDE and in build output.

## Configuring severity

Severities are configured in the **repository root `.editorconfig`** (section “CephasOps Tenant Safety Analyzer Rules” under `[*.cs]`). That keeps IDE and build consistent and versioned. Current v1 policy: all four rules are `warning`. **CEPHAS001** is the most likely future candidate for promotion to `error` after false-positive review; **CEPHAS002** and **CEPHAS003** remain review-oriented warnings. To override locally (e.g. test CEPHAS001 as `error` without changing the committed file), add a local `.editorconfig` in your working directory or use a user-level editorconfig; the nearest file wins.

## Suppression

Use standard Roslyn suppression so intentional exceptions are explicit:

- **Pragma:** `#pragma warning disable CEPHAS001` … `#pragma warning restore CEPHAS001`
- **Attribute:** `[SuppressMessage("CephasOps.TenantSafety", "CEPHAS001", Justification = "Platform-wide query; see IGNORE_QUERY_FILTERS_AUDIT.")]`
- **GlobalSuppressions.cs:** Add a `SuppressMessage` with scope `module` or `type`/`member` for intentional exceptions.

Document intentional suppressions (e.g. in code review or in `backend/docs/operations/IGNORE_QUERY_FILTERS_AUDIT.md`).

## Related documentation

- **backend/docs/architecture/TENANT_QUERY_SAFETY_GUIDELINES.md** — When to use global filters vs explicit company-scoped queries.
- **backend/docs/architecture/EFCORE_RELATIONSHIP_FIXUP_RISK.md** — EF fixup and the defensive pattern (clear navigation when guarded lookup returns null).
- **backend/docs/operations/IGNORE_QUERY_FILTERS_AUDIT.md** — Manual audit of `IgnoreQueryFilters()` usage.
- **backend/docs/operations/TENANT_SAFETY_AUTOMATED_AUDIT.md** — Repo script (`tools/tenant_safety_audit.ps1`) that complements this analyzer.
