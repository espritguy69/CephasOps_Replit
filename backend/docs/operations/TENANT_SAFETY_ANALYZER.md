# Tenant Safety Roslyn Analyzer (CEPHAS001–CEPHAS004)

**Date:** 2026-03-12  
**Purpose:** Document the CephasOps Roslyn analyzer that reports diagnostics for multi-tenant safety patterns. It runs during build and in the IDE when the analyzer project is referenced.

**Analyzer project:** `analyzers/CephasOps.TenantSafetyAnalyzers/`  
**See also:** [analyzers/README.md](../../../analyzers/README.md) for setup and suppression.

Each rule’s **DiagnosticDescriptor** includes a short description and a **helpLinkUri** pointing to this document (with fragment `#cephas001`–`#cephas004`). Use the Error List or lightbulb to open the help link.

---

## 1. What the analyzer checks

| ID | Rule | Trigger | Action |
|----|------|---------|--------|
| **CEPHAS001** | Unsafe IgnoreQueryFilters on tenant-scoped entity | A LINQ chain calls `IgnoreQueryFilters()` on a known tenant-scoped set (e.g. Orders, Assets, BillingRatecards) and there is no explicit CompanyId/TenantId constraint in the same query chain or immediate predicate. | Add a `Where` (or equivalent) that constrains by company/tenant. |
| **CEPHAS002** | IgnoreQueryFilters with ambiguous tenant scoping | `IgnoreQueryFilters()` is used but company/tenant scoping is not obvious in the query chain (set may not be in the built-in tenant list). | Review; add explicit scoping or suppress with justification. |
| **CEPHAS003** | Possible EF Core relationship fixup risk | Code mutates a navigation property (e.g. `disposal.Asset.Status = ...`) and the method contains a guarded lookup (e.g. FirstOrDefaultAsync with CompanyId) but no obvious `disposal.Asset = null` (or similar) when the lookup returns null. | Clear the navigation when the guarded lookup returns null; see [EFCORE_RELATIONSHIP_FIXUP_RISK.md](../architecture/EFCORE_RELATIONSHIP_FIXUP_RISK.md). |
| **CEPHAS004** | Tenant-scoped entity queried by Id only | A query on a known tenant-scoped set filters by Id/key only with no CompanyId/TenantId constraint: predicate-based (e.g. `FirstOrDefaultAsync(o => o.Id == id)`) or key-based (e.g. `FindAsync(id)`, `Find(id)`). | Add an explicit company/tenant constraint in the predicate or in the same block (e.g. `o.Id == id && o.CompanyId == companyId`, or validate after `FindAsync(id)`). |

Rule summaries (for help links):

- [CEPHAS001](#cephas001): Unsafe IgnoreQueryFilters usage on tenant-scoped entity.
- [CEPHAS002](#cephas002): IgnoreQueryFilters usage with ambiguous or non-obvious tenant scoping.
- [CEPHAS003](#cephas003): Possible EF Core relationship fixup risk when mutating a navigation target without clearing invalid navigation state.
- [CEPHAS004](#cephas004): Tenant-scoped entity queried by Id only without explicit company scoping.

---

## 2. How to run it

- **IDE:** Reference the analyzer project from the backend (see [analyzers/README.md](../../../analyzers/README.md)); build and open C# files. Diagnostics appear in the Error List and as squiggles.
- **Build:** Same reference; `dotnet build` runs the analyzers. Warnings/errors appear in the build output according to severity.

---

## 2.5. CI (GitHub Actions)

A **tenant-safety workflow** (`.github/workflows/tenant-safety.yml`) runs on pull requests and pushes to `main`/`master` when paths under `backend/`, `analyzers/`, `tools/`, `.editorconfig`, or the workflow file change. It:

- Runs the analyzer test project (Release).
- Builds the API project with **CEPHAS001 and CEPHAS004 enforced as errors** (`WarningsAsErrors=CEPHAS001,CEPHAS004`); CEPHAS002 and CEPHAS003 remain warnings.
- Runs `tools/tenant_safety_audit.ps1` as a complementary heuristic check.

CI therefore fails on CEPHAS001 or CEPHAS004 (and on analyzer test failures or audit HIGH findings). CEPHAS002 and CEPHAS003 stay as warnings for review.

---

## 3. Configuring severity

Severities are configured in the **repository root `.editorconfig`** in the section “CephasOps Tenant Safety Analyzer Rules” under `[*.cs]`. That keeps IDE and build consistent and versioned. Current v1 policy: all four rules are `warning`. **CEPHAS001** is the most likely future candidate for promotion to `error` after false-positive review; **CEPHAS002** and **CEPHAS003** remain review-oriented warnings (more heuristic). To override locally (e.g. CEPHAS001 as `error`), add a local `.editorconfig` in your working tree; the nearest file wins. Use `none` to disable a rule, or `error` to fail the build.

---

## 4. Suppression

Use standard Roslyn suppression so intentional exceptions are explicit:

- **Pragma:** `#pragma warning disable CEPHAS001` … `#pragma warning restore CEPHAS001`
- **SuppressMessage:** `[SuppressMessage("CephasOps.TenantSafety", "CEPHAS001", Justification = "...")]`
- **GlobalSuppressions.cs:** For project-wide exceptions.

Do not suppress without a clear justification; document in code or in [IGNORE_QUERY_FILTERS_AUDIT.md](IGNORE_QUERY_FILTERS_AUDIT.md) where appropriate.

---

## 5. Rule anchors (for help links)

<a id="cephas001"></a>**CEPHAS001** — Unsafe IgnoreQueryFilters on tenant-scoped entity.  
<a id="cephas002"></a>**CEPHAS002** — IgnoreQueryFilters with ambiguous tenant scoping.  
<a id="cephas003"></a>**CEPHAS003** — Possible EF Core relationship fixup risk.  
<a id="cephas004"></a>**CEPHAS004** — Tenant-scoped entity queried by Id only without explicit company scoping.

---

## 6. Relation to other docs

- **[TENANT_QUERY_SAFETY_GUIDELINES.md](../architecture/TENANT_QUERY_SAFETY_GUIDELINES.md)** — Pattern A vs B; when to use IgnoreQueryFilters with explicit CompanyId.
- **[EFCORE_RELATIONSHIP_FIXUP_RISK.md](../architecture/EFCORE_RELATIONSHIP_FIXUP_RISK.md)** — Why CEPHAS003 exists and the defensive pattern.
- **[IGNORE_QUERY_FILTERS_AUDIT.md](IGNORE_QUERY_FILTERS_AUDIT.md)** — Manual audit of all IgnoreQueryFilters() usages.
- **[TENANT_SAFETY_AUTOMATED_AUDIT.md](TENANT_SAFETY_AUTOMATED_AUDIT.md)** — Repo script (`tools/tenant_safety_audit.ps1`); CHECK_A covers IgnoreQueryFilters and FindAsync/Find/Single on tenant-scoped sets; use alongside the analyzer for repo-wide scans.
