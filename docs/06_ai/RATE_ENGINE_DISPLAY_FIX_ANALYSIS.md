# Rate Engine Display Fix - Analysis

**Date:** 2026-01-05  
**Problem:** Rate Engine list shows "-" for Partner Group, Order Type, and Order Category even though values are selected/stored

---

## Root Cause

### Issue Identified

**Backend DTOs** only return **IDs**, not **names**:
- `GponPartnerJobRateDto` has `PartnerGroupId`, `OrderTypeId`, `OrderCategoryId` (IDs only)
- `GponSiJobRateDto` has `OrderTypeId`, `OrderCategoryId` (IDs only)
- `GponSiCustomRateDto` has `ServiceInstallerId`, `OrderTypeId`, `OrderCategoryId` (IDs only)

**Frontend expects** **name fields**:
- `partnerGroupName`, `partnerName`
- `orderTypeName`
- `orderCategoryName`
- `installationMethodName`
- `serviceInstallerName`

**Mapping methods** (`MapToGponPartnerJobRateDto`, etc.) don't populate name fields.

---

## Current State

### Backend DTOs (Lines 1064-1167)

```csharp
public class GponPartnerJobRateDto
{
    public Guid Id { get; set; }
    public Guid PartnerGroupId { get; set; }  // ❌ Only ID
    public Guid? PartnerId { get; set; }       // ❌ Only ID
    public Guid OrderTypeId { get; set; }      // ❌ Only ID
    public Guid OrderCategoryId { get; set; }  // ❌ Only ID
    public Guid? InstallationMethodId { get; set; } // ❌ Only ID
    // ... no name fields
}
```

### Frontend Types (frontend/src/types/rates.ts)

```typescript
export interface GponPartnerJobRate {
  partnerGroupId: string;
  partnerGroupName?: string;  // ✅ Expected but not returned
  partnerId?: string;
  partnerName?: string;        // ✅ Expected but not returned
  orderTypeId: string;
  orderTypeName?: string;      // ✅ Expected but not returned
  orderCategoryId: string;
  orderCategoryName?: string;  // ✅ Expected but not returned
  installationMethodId?: string;
  installationMethodName?: string; // ✅ Expected but not returned
}
```

### Mapping Methods (Lines 915-969)

```csharp
private static GponPartnerJobRateDto MapToGponPartnerJobRateDto(GponPartnerJobRate r) => new()
{
    Id = r.Id,
    PartnerGroupId = r.PartnerGroupId,  // ❌ Only ID, no name lookup
    OrderTypeId = r.OrderTypeId,        // ❌ Only ID, no name lookup
    OrderCategoryId = r.OrderCategoryId, // ❌ Only ID, no name lookup
    // ... no name fields populated
};
```

---

## Solution Required

### Step 1: Add Name Fields to DTOs

Add optional name fields to all three DTOs:
- `PartnerGroupName`
- `PartnerName`
- `OrderTypeName`
- `OrderCategoryName`
- `InstallationMethodName`
- `ServiceInstallerName` (for custom rates)

### Step 2: Update Mapping Methods

Change mapping methods to:
1. Accept `ApplicationDbContext` or lookup dictionaries
2. Look up related entities by ID
3. Populate name fields

### Step 3: Update GET Endpoints

Modify GET endpoints to:
1. Load related entities efficiently (batch lookup or Include)
2. Pass lookup data to mapping methods
3. Return DTOs with populated name fields

---

## Implementation Approach

### Option A: Batch Lookup (Recommended)

1. Collect all IDs from rates
2. Query related entities in batches
3. Create lookup dictionaries
4. Map rates with names from dictionaries

**Pros:**
- Efficient (fewer database queries)
- Works with existing static mapping methods

**Cons:**
- Requires refactoring mapping methods to accept dictionaries

### Option B: Include Navigation Properties

1. Add navigation properties to entities
2. Use `.Include()` in queries
3. Access names via navigation properties

**Pros:**
- Cleaner entity model
- EF Core handles relationships

**Cons:**
- Requires entity changes
- May need configuration updates

### Option C: Inline Lookups (Not Recommended)

1. Look up each entity individually in mapping method
2. N+1 query problem
3. Poor performance

---

## Recommended Solution: Batch Lookup

### Modified GET Endpoint Example

```csharp
[HttpGet("gpon/partner-rates")]
public async Task<ActionResult<ApiResponse<List<GponPartnerJobRateDto>>>> GetGponPartnerJobRates(...)
{
    // ... existing query logic ...
    var rates = await query.OrderBy(r => r.PartnerGroupId).ToListAsync(cancellationToken);

    // Batch lookup related entities
    var partnerGroupIds = rates.Where(r => r.PartnerGroupId != Guid.Empty)
        .Select(r => r.PartnerGroupId).Distinct().ToList();
    var partnerIds = rates.Where(r => r.PartnerId.HasValue)
        .Select(r => r.PartnerId!.Value).Distinct().ToList();
    var orderTypeIds = rates.Select(r => r.OrderTypeId).Distinct().ToList();
    var orderCategoryIds = rates.Select(r => r.OrderCategoryId).Distinct().ToList();
    var installationMethodIds = rates.Where(r => r.InstallationMethodId.HasValue)
        .Select(r => r.InstallationMethodId!.Value).Distinct().ToList();

    var partnerGroups = await _context.PartnerGroups
        .Where(pg => partnerGroupIds.Contains(pg.Id))
        .ToDictionaryAsync(pg => pg.Id, pg => pg.Name, cancellationToken);
    var partners = await _context.Partners
        .Where(p => partnerIds.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
    var orderTypes = await _context.OrderTypes
        .Where(ot => orderTypeIds.Contains(ot.Id))
        .ToDictionaryAsync(ot => ot.Id, ot => ot.Name, cancellationToken);
    var orderCategories = await _context.OrderCategories
        .Where(oc => orderCategoryIds.Contains(oc.Id))
        .ToDictionaryAsync(oc => oc.Id, oc => oc.Name, cancellationToken);
    var installationMethods = await _context.InstallationMethods
        .Where(im => installationMethodIds.Contains(im.Id))
        .ToDictionaryAsync(im => im.Id, im => im.Name, cancellationToken);

    // Map with names
    return this.Success(rates.Select(r => MapToGponPartnerJobRateDto(
        r, partnerGroups, partners, orderTypes, orderCategories, installationMethods)).ToList());
}
```

### Updated Mapping Method

```csharp
private static GponPartnerJobRateDto MapToGponPartnerJobRateDto(
    GponPartnerJobRate r,
    Dictionary<Guid, string> partnerGroups,
    Dictionary<Guid, string> partners,
    Dictionary<Guid, string> orderTypes,
    Dictionary<Guid, string> orderCategories,
    Dictionary<Guid, string> installationMethods) => new()
{
    Id = r.Id,
    PartnerGroupId = r.PartnerGroupId,
    PartnerGroupName = partnerGroups.GetValueOrDefault(r.PartnerGroupId),
    PartnerId = r.PartnerId,
    PartnerName = r.PartnerId.HasValue ? partners.GetValueOrDefault(r.PartnerId.Value) : null,
    OrderTypeId = r.OrderTypeId,
    OrderTypeName = orderTypes.GetValueOrDefault(r.OrderTypeId),
    OrderCategoryId = r.OrderCategoryId,
    OrderCategoryName = orderCategories.GetValueOrDefault(r.OrderCategoryId),
    InstallationMethodId = r.InstallationMethodId,
    InstallationMethodName = r.InstallationMethodId.HasValue 
        ? installationMethods.GetValueOrDefault(r.InstallationMethodId.Value) 
        : null,
    RevenueAmount = r.RevenueAmount,
    Currency = r.Currency,
    ValidFrom = r.ValidFrom,
    ValidTo = r.ValidTo,
    IsActive = r.IsActive,
    Notes = r.Notes,
    CreatedAt = r.CreatedAt,
    UpdatedAt = r.UpdatedAt
};
```

---

## Files to Modify

1. **`backend/src/CephasOps.Api/Controllers/RatesController.cs`**
   - Add name fields to DTOs (GponPartnerJobRateDto, GponSiJobRateDto, GponSiCustomRateDto)
   - Update GET endpoints to batch lookup related entities
   - Update mapping methods to accept and use lookup dictionaries

---

## Summary

**Problem:** Backend returns only IDs, frontend expects names  
**Solution:** Add name fields to DTOs, batch lookup related entities, populate names in mapping  
**Impact:** 3 GET endpoints, 3 DTOs, 3 mapping methods need updates

