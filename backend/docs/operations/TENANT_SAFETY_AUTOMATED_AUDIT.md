# Tenant Safety — Automated Audit Script

**Date:** 2026-03-12  
**Purpose:** Document the repo tool that scans C# files for risky multi-tenant data-access patterns. The script is heuristic only; it helps surface review candidates and clearly unsafe usages. It does **not** replace code review or the tenant-safety guidelines.

---

## 1. Purpose of the script

The **tenant safety audit script** (`tools/tenant_safety_audit.ps1`) recursively scans `backend/src` (and optionally `backend/tests`) for:

1. **IgnoreQueryFilters usage** that may not be explicitly company-scoped.
2. **Navigation-property update patterns** that may be vulnerable to EF Core relationship fixup (wrong-tenant entity attached via fixup, then updated).

The goal is to give developers and CI a fast, deterministic check that flags likely issues without requiring a full Roslyn analyzer. Findings are labeled by severity so that only **clearly unsafe** cases cause a non-zero exit code.

---

## 2. What it checks

### CHECK A — IgnoreQueryFilters risk

- **Finds:** Every `.IgnoreQueryFilters()` call in the scanned C# files.
- **Classifies** using a short window of lines (same statement / next ~18 lines):
  - **SAFE_LIKELY (LOW):** An explicit company or tenant constraint appears nearby (e.g. `CompanyId`, `TenantId`, `CurrentTenantId`, `disposal.CompanyId`, `order.CompanyId` in the query chain or same block).
  - **NEEDS_REVIEW (MEDIUM):** `IgnoreQueryFilters` is present but no obvious company scoping in that window. May be intentional (e.g. Auth, platform jobs, retention, testing).
  - **FLAG_UNSAFE (HIGH):** `IgnoreQueryFilters` is used on a **tenant-scoped entity set** (e.g. Orders, Assets, BillingRatecards, ServiceInstallers, etc.) with no CompanyId/TenantId constraint in the local block. These are treated as clearly unsafe.

The script uses a built-in list of tenant-scoped set names to decide when to raise HIGH vs MEDIUM.

**CHECK A — QueryByIdOnly (FindAsync/Find/Single on tenant-scoped set)**  
The script also flags calls like `FindAsync(id)` or `Single(id)` on tenant-scoped sets when there is no CompanyId/TenantId in a nearby line window (MEDIUM). To reduce noise from known-safe patterns while keeping real risks visible:

- **Reference-data sets** (OrderCategories, Partners, Materials, MaterialCategories, BuildingTypes, ServiceInstallers): an **expanded window** (lines before + after) is used. If the window contains evidence of company-scoped context (e.g. `CompanyId`, `companyId`, `rate.`, `buildingId`), the finding is **not** raised. This avoids flagging post-create/post-update DTO enrichment where the parent entity is already company-scoped and global filters remain active.
- **Primary-entity sets** (Orders, Assets, AssetDisposals, BillingRatecards, Buildings, Invoices, Departments, etc.): only the **forward** window is used; no downgrade. Primary tenant data access by Id only remains MEDIUM so it stays visible for review or remediation.

**Reviewed-safe QueryByIdOnly patterns** (not raised when the expanded window shows context): reference-data lookups immediately after a create/update that has RequireCompanyId or a parent with CompanyId; DTO expansion for display (e.g. rate names); building-scoped material/order-type lookups. This does **not** apply to primary tenant entity access (e.g. loading an Asset or Order by Id for business logic), any path using IgnoreQueryFilters, or write/update paths using the looked-up entity—those remain in scope for review.

**Known remaining MEDIUM (intentional):** One QueryByIdOnly finding is expected and left visible: **AssetService.CreateDisposalAsync** — the fallback `FindAsync(dto.AssetId)` when `companyId` is null. It is a primary-entity lookup (Assets) and uses Id only in that fallback path; when `companyId` is provided, the method uses an explicit company-scoped predicate. The finding remains so that the Id-only fallback stays in review scope and future changes do not hide primary asset access.

### CHECK B — Relationship fixup risk

- **Finds:** Assignments to a property on a navigation (e.g. `disposal.Asset.Status = ...`, `entity.Nav.UpdatedAt = ...`).
- **Flags (MEDIUM):** When, in the same method, there is a guarded lookup (e.g. `FirstOrDefaultAsync` with `CompanyId` in the block) but **no obvious null-clearing** of that navigation (e.g. `disposal.Asset = null` in an `else` branch). Such methods are candidates for the fixup bug described in [EFCORE_RELATIONSHIP_FIXUP_RISK.md](../architecture/EFCORE_RELATIONSHIP_FIXUP_RISK.md).

This check is heuristic: it does not prove correctness, only surfaces likely review candidates.

---

## 3. Limitations

- **Heuristic only.** The script uses line-based and regex-based checks. It is not a compiler-grade analyzer. False positives and false negatives are possible.
- **No full AST.** Method boundaries and “same query” are approximated (e.g. by scanning nearby lines or a simple method-boundary guess). Scoping that spans many lines or is in a separate method may be missed or misclassified.
- **Intentional bypasses.** Auth, platform job processing, event retention, seeders, and test-only code often use `IgnoreQueryFilters` without CompanyId by design. These typically appear as MEDIUM (NEEDS_REVIEW), not HIGH, unless they touch a tenant-scoped entity set.
- **CHECK B** only considers a single method block and simple patterns (e.g. `= null`). More complex clearing logic may not be detected.

The script **supplements** manual review and the guidelines; it does not replace them.

---

## 4. How to run it

From the **repository root**:

```powershell
.\tools\tenant_safety_audit.ps1
```

- Scans **backend/src** only.  
- Excludes `bin/`, `obj/`, and generated files (e.g. `.Designer.cs`, `.g.cs`).

To include **backend/tests**:

```powershell
.\tools\tenant_safety_audit.ps1 -IncludeTests
```

Optional:

- **-Quiet** — Only print the summary and exit code; omit per-finding detail.

Example (summary only):

```powershell
.\tools\tenant_safety_audit.ps1 -Quiet
```

---

## 5. How to interpret findings

- **HIGH:** IgnoreQueryFilters on a tenant-scoped set with no CompanyId/TenantId constraint in the local block. Treat as **clearly unsafe**; fix or document and justify.
- **MEDIUM:** Either (A) IgnoreQueryFilters with no obvious scoping nearby but not on a known tenant-scoped set (often intentional: Auth, jobs, retention, tests), or (B) FindAsync/Find/Single on a tenant-scoped set with no CompanyId in the checked window (query by Id only; consider explicit company scope), or (C) navigation property updated with a guarded lookup but no obvious null-clearing (fixup-risk review candidate). Review and either add scoping/null-clearing or document why it is safe.
- **LOW:** IgnoreQueryFilters with explicit company/tenant scoping nearby (SAFE_LIKELY). Informational only.

**Exit code:**

- **0** — No HIGH findings. MEDIUM/LOW may still be present; review as needed.
- **1** — At least one HIGH finding. Remediation or documented justification required before considering the audit “clean” for tenant safety.
- **2** — Script error (e.g. paths not found).

---

## 6. Relation to other docs

- **[TENANT_QUERY_SAFETY_GUIDELINES.md](../architecture/TENANT_QUERY_SAFETY_GUIDELINES.md)** — When to use global filters vs explicit company-scoped queries; Pattern A vs Pattern B; rules for `IgnoreQueryFilters()`. The audit script checks for violations of these patterns.
- **[EFCORE_RELATIONSHIP_FIXUP_RISK.md](../architecture/EFCORE_RELATIONSHIP_FIXUP_RISK.md)** — How EF fixup can attach a wrong-tenant entity to a navigation property and how to defend (guarded load + clear navigation when null). CHECK B aims to find methods that might be missing the “clear when null” step.
- **[IGNORE_QUERY_FILTERS_AUDIT.md](IGNORE_QUERY_FILTERS_AUDIT.md)** — Manual audit of every `IgnoreQueryFilters()` use with classifications (SAFE_EXPLICIT_COMPANY, SAFE_PLATFORM, NEEDS_REVIEW, UNSAFE). The automated script is a lightweight, repeatable subset of that review and helps prevent regressions.

---

## 7. CI suitability

- **Yes.** The script is deterministic and fast (scans files under `backend/src` and optionally `backend/tests`). No database or runtime required.
- **Recommended:** Run from repo root, e.g. `.\tools\tenant_safety_audit.ps1` (src only). Use `-IncludeTests` if you want test code included. Exit code 1 on any HIGH finding so CI can fail the build when a clearly unsafe pattern is introduced.
- MEDIUM findings do **not** change the exit code; they are for human review. Teams can optionally track MEDIUM count or allow-list known intentional usages.
