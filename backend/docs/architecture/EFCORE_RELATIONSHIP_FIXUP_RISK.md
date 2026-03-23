# EF Core Relationship Fixup — Tenant Safety Risk

**Date:** 2026-03-12  
**Purpose:** Document the risk that EF Core relationship fixup can attach tracked entities from another tenant to navigation properties, bypassing guarded queries. This document supplements the tenant query safety guidelines and the IgnoreQueryFilters audit.

---

## 1. What is EF Core relationship fixup?

In Entity Framework Core, **relationship fixup** is the mechanism that keeps navigation properties and foreign key values in sync when entities are tracked by the same `DbContext`.

- EF **automatically connects** navigation properties when related entities are **already tracked** in the `DbContext`.
- This happens **even if the entity was not loaded by the current query** — fixup is based on matching key values (FK/PK), not on how the entity entered the context.
- As a result, a navigation property can reference an entity that was:
  - Loaded by a previous query in the same request or test,
  - Attached or seeded earlier,
  - Or loaded without tenant-scoped filters (e.g. via `IgnoreQueryFilters()` in another path).

So: **you cannot assume a navigation property is “safe” just because your current code path did not load it.** If a related entity with the matching key is already tracked, EF may attach it to the navigation.

---

## 2. Tenant safety risk

This behavior can break tenant isolation when:

1. You load an entity that has a foreign key (e.g. `Disposal` with `AssetId`).
2. The same `DbContext` already has another entity tracked (e.g. an `Asset` from **another company**) that was loaded or seeded earlier.
3. EF relationship fixup sees the matching `AssetId` and attaches that tracked asset to `disposal.Asset`.
4. Your code then uses a **guarded query** (e.g. load asset only when `a.Id == disposal.AssetId && a.CompanyId == disposal.CompanyId`) and correctly gets **null** for a cross-company disposal.
5. You assign `disposal.Asset = asset` only when the guarded query returns a result — but you **do not clear** the navigation when it returns null.
6. The navigation property **still points to the wrong-company asset** due to fixup.
7. Later code (e.g. `disposal.Asset.Status = AssetStatus.Disposed`) updates that wrong-tenant entity, and `SaveChanges` persists the change.

So the **guarded query is correct**, but fixup can leave a **wrong-tenant entity** on the navigation, and any update to that navigation affects the wrong tenant.

### When this is more likely

- **IgnoreQueryFilters()** is used somewhere, so entities from other tenants can be loaded into the same context.
- Entities are **tracked from earlier operations** (e.g. seed data, previous queries in the same request or test).
- Navigation properties are **not explicitly validated or cleared** when the guarded lookup returns null.

---

## 3. Real incident: AssetService.ApproveDisposalAsync

During the IgnoreQueryFilters tenant-safety audit, this exact scenario occurred:

1. **Disposal** was loaded with an `AssetId` pointing to an asset belonging to **Company B**.
2. The disposal itself belonged to **Company A** (cross-company reference in data).
3. The **DbContext** already had that **Asset (Company B)** tracked (e.g. from test seed).
4. **EF relationship fixup** attached that tracked asset to `disposal.Asset` based on `disposal.AssetId`.
5. The **guarded query** (asset only if `a.CompanyId == disposal.CompanyId`) correctly returned **null**.
6. The code set `disposal.Asset = asset` only when the query returned a value; it did **not** set `disposal.Asset = null` when the query returned null.
7. So `disposal.Asset` still pointed to Company B’s asset, and the code set `disposal.Asset.Status = AssetStatus.Disposed`, updating the **wrong tenant’s** asset.

The fix was to **clear the navigation** when the guarded lookup does not return a valid entity (see defensive pattern below).

---

## 4. Defensive pattern

When loading a tenant-scoped related entity and using it for updates, use this pattern so fixup cannot leave an invalid entity attached.

### 4.1 Load with explicit company constraint

```csharp
var asset = await _context.Assets
    .IgnoreQueryFilters()
    .Where(a => a.Id == disposal.AssetId && a.CompanyId == disposal.CompanyId)
    .FirstOrDefaultAsync(cancellationToken);
```

### 4.2 Assign or clear the navigation

```csharp
if (asset != null && !asset.IsDeleted)
{
    disposal.Asset = asset;
}
else
{
    disposal.Asset = null;  // clear any fixup-attached entity
}
```

**Why clear when null?**  
Clearing the navigation when the guarded lookup returns null ensures that any entity EF attached via fixup (e.g. from another company) is no longer referenced. Subsequent code that checks `disposal.Asset != null` will then only see an asset that was loaded through the guarded path.

---

## 5. Guidelines for developers

- **When using `IgnoreQueryFilters()` with tenant-scoped entities,** always constrain by `CompanyId` (or equivalent tenant key) in the same query.
- **Never assume a navigation property is safe** if it was not explicitly loaded by your guarded query — it may have been set by fixup from a previously tracked entity.
- **Clear navigation properties** when the guarded lookup returns null (or no valid entity), so fixup cannot leave a wrong-tenant entity attached.
- **Be cautious in long-lived DbContexts or tests** where multiple entities (from different tenants or from seed) may already be tracked; fixup can connect them as soon as FK/PK values match.

---

## 6. Related documentation

This document **supplements** the following:

- **[TENANT_QUERY_SAFETY_GUIDELINES.md](TENANT_QUERY_SAFETY_GUIDELINES.md)** — When to use global filters vs explicit company-scoped queries; query patterns for workflow, profitability, and tests. The fixup risk applies when you bypass filters and must be mitigated with explicit scope and navigation clearing.
- **[IGNORE_QUERY_FILTERS_AUDIT.md](../operations/IGNORE_QUERY_FILTERS_AUDIT.md)** — Audit of `IgnoreQueryFilters()` usage and tenant-safety remediation. The fixup risk was discovered during that audit (e.g. in `AssetService.ApproveDisposalAsync`).

For the broader tenant-safety checklist and guard behavior, see **[PLATFORM_SAFETY_HARDENING_INDEX.md](../operations/PLATFORM_SAFETY_HARDENING_INDEX.md)** and **[TENANT_SAFETY_DEVELOPER_GUIDE.md](TENANT_SAFETY_DEVELOPER_GUIDE.md)**.
