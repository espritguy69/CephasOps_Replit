# Partner Revenue Rate Form with Checkboxes - Analysis

**Date:** 2026-01-05  
**Objective:** Add checkboxes for multi-select Order Categories and Installation Methods

---

## Current State Analysis

### ✅ 1. Backend Entity Structure

**File:** `backend/src/CephasOps.Domain/Rates/Entities/GponPartnerJobRate.cs`

**Current Structure:**
```csharp
public class GponPartnerJobRate : CompanyScopedEntity
{
    public Guid PartnerGroupId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid OrderTypeId { get; set; }
    public Guid OrderCategoryId { get; set; }        // ❌ Single value (not array)
    public Guid? InstallationMethodId { get; set; } // ❌ Single value (not array)
    public decimal RevenueAmount { get; set; }
    // ... other fields
}
```

**Database Table:** `GponPartnerJobRates`
- Single foreign key: `OrderCategoryId` (Guid, NOT NULL)
- Single foreign key: `InstallationMethodId` (Guid, nullable)
- **No junction tables exist**
- **No many-to-many relationships**

**Index Structure:**
```csharp
builder.HasIndex(r => new { r.PartnerGroupId, r.OrderTypeId, r.OrderCategoryId, r.InstallationMethodId });
```

This index suggests **one rate per unique combination** of these fields.

---

### ✅ 2. Current DTO Structure

**File:** `backend/src/CephasOps.Api/Controllers/RatesController.cs`

**CreateGponPartnerJobRateDto:**
```csharp
public class CreateGponPartnerJobRateDto
{
    public Guid PartnerGroupId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid OrderTypeId { get; set; }
    public Guid OrderCategoryId { get; set; }        // ❌ Single value
    public Guid? InstallationMethodId { get; set; } // ❌ Single value
    public decimal RevenueAmount { get; set; }
    // ... other fields
}
```

**Current API accepts:** Single values only, not arrays.

---

### ✅ 3. Current Frontend Form

**File:** `frontend/src/pages/settings/RateEngineManagementPage.tsx`

**Form State:**
```typescript
const [partnerRateForm, setPartnerRateForm] = useState({
  partnerGroupId: '',
  partnerId: '',
  orderTypeId: '',
  orderCategoryId: '',        // ❌ Single string
  installationMethodId: '',   // ❌ Single string
  revenueAmount: '',
  // ... other fields
});
```

**Current Form Fields:**
- Partner Group: Single Select dropdown
- Partner: Single Select dropdown
- Order Type: Single Select dropdown
- **Order Category: Single Select dropdown** ❌
- **Installation Method: Single Select dropdown** ❌
- Revenue Amount: Number input
- Valid From/To: Date inputs
- Notes: Text input
- Active: Checkbox

---

### ✅ 4. Business Logic Analysis

**Rate Resolution Logic:**
- Rate is keyed by: `PartnerGroupId + OrderTypeId + OrderCategoryId + InstallationMethodId`
- **One rate record = One specific combination**
- If user wants rate for multiple Order Categories, they need **multiple rate records**

**Current Design Pattern:**
- Each rate record represents **one specific combination**
- To cover multiple categories/methods, create **multiple rate records**

---

## Implementation Options

### Option A: Create Multiple Records (Recommended)

**Approach:** When user selects multiple Order Categories and Installation Methods, create one rate record per combination.

**Example:**
- User selects: Order Categories [FTTH, FTTO], Installation Methods [Prelaid, Non-Prelaid]
- System creates 4 records:
  1. FTTH + Prelaid
  2. FTTH + Non-Prelaid
  3. FTTO + Prelaid
  4. FTTO + Non-Prelaid

**Pros:**
- ✅ No database schema changes
- ✅ No entity changes
- ✅ No migration needed
- ✅ Maintains current rate resolution logic
- ✅ Simple to implement

**Cons:**
- ⚠️ Creates multiple records (but this is expected for rate combinations)

**Implementation:**
- Frontend: Checkboxes for multi-select
- On submit: Generate all combinations
- Backend: Accept single DTO, create multiple records (or frontend sends array of DTOs)

---

### Option B: Many-to-Many Relationships (Not Recommended)

**Approach:** Create junction tables for many-to-many relationships.

**Required Changes:**
1. Create `GponPartnerJobRateOrderCategories` junction table
2. Create `GponPartnerJobRateInstallationMethods` junction table
3. Update entity with navigation properties
4. Update DTOs to accept arrays
5. Update rate resolution logic
6. Create migration
7. Update all existing queries

**Pros:**
- ✅ Normalized database design
- ✅ One rate record with multiple relationships

**Cons:**
- ❌ Major schema change
- ❌ Requires migration
- ❌ Breaks existing rate resolution logic
- ❌ Complex implementation
- ❌ Risk of breaking existing functionality

**Not Recommended** - Too much risk and complexity for this use case.

---

### Option C: JSON Array Storage (Not Recommended)

**Approach:** Store arrays as JSON in single columns.

**Pros:**
- ✅ No junction tables

**Cons:**
- ❌ Not normalized
- ❌ Hard to query/index
- ❌ Breaks current rate resolution logic
- ❌ Complex queries

**Not Recommended** - Poor database design.

---

## Recommended Solution: Option A (Multiple Records)

### Implementation Plan

#### 1. Frontend Changes

**Update Form State:**
```typescript
const [partnerRateForm, setPartnerRateForm] = useState({
  partnerGroupId: '',
  partnerId: '',
  orderTypeId: '',
  orderCategoryIds: [] as string[],        // ✅ Array
  installationMethodIds: [] as string[],   // ✅ Array
  revenueAmount: '',
  validFrom: '',
  validTo: '',
  notes: '',
  isActive: true
});
```

**Add Checkbox Groups:**
- Order Categories: Checkbox group (multi-select)
- Installation Methods: Checkbox group (multi-select)

**Submit Handler:**
- Generate all combinations of selected Order Categories × Installation Methods
- Create one rate record per combination
- Call API multiple times OR create bulk endpoint

#### 2. Backend Changes (Optional)

**Option 2A: Frontend Creates Multiple Records**
- Frontend generates combinations
- Frontend calls existing `POST /api/rates/gpon/partner-rates` multiple times
- No backend changes needed

**Option 2B: Backend Bulk Create Endpoint**
- Create new endpoint: `POST /api/rates/gpon/partner-rates/bulk`
- Accept array of DTOs
- Create multiple records in transaction
- More efficient (single transaction)

---

## Detailed Implementation Plan

### Phase 1: Frontend Form with Checkboxes

#### Step 1: Update Form State

**File:** `frontend/src/pages/settings/RateEngineManagementPage.tsx`

```typescript
const [partnerRateForm, setPartnerRateForm] = useState({
  partnerGroupId: '',
  partnerId: '',
  orderTypeId: '',
  orderCategoryIds: [] as string[],        // Changed from orderCategoryId
  installationMethodIds: [] as string[],   // Changed from installationMethodId
  revenueAmount: '',
  validFrom: '',
  validTo: '',
  notes: '',
  isActive: true
});
```

#### Step 2: Create Checkbox Group Component

**Option A: Use Native Checkboxes**
```tsx
<div className="space-y-2">
  <label className="text-sm font-medium">Order Categories *</label>
  <div className="space-y-2 max-h-48 overflow-y-auto border rounded p-2">
    {orderCategories.map(cat => (
      <label key={cat.id} className="flex items-center gap-2 cursor-pointer">
        <input
          type="checkbox"
          checked={partnerRateForm.orderCategoryIds.includes(cat.id)}
          onChange={(e) => {
            if (e.target.checked) {
              setPartnerRateForm({
                ...partnerRateForm,
                orderCategoryIds: [...partnerRateForm.orderCategoryIds, cat.id]
              });
            } else {
              setPartnerRateForm({
                ...partnerRateForm,
                orderCategoryIds: partnerRateForm.orderCategoryIds.filter(id => id !== cat.id)
              });
            }
          }}
          className="h-4 w-4 rounded border-input"
        />
        <span className="text-sm">{cat.name}</span>
      </label>
    ))}
  </div>
  {partnerRateForm.orderCategoryIds.length === 0 && (
    <p className="text-xs text-destructive">At least one category must be selected</p>
  )}
</div>
```

**Option B: Use shadcn/ui Checkbox Component** (if available)

#### Step 3: Update Submit Handler

**Generate Combinations:**
```typescript
const handleCreatePartnerRate = async () => {
  // Validation
  if (!partnerRateForm.partnerGroupId || !partnerRateForm.orderTypeId) {
    showError('Partner Group and Order Type are required');
    return;
  }
  
  if (partnerRateForm.orderCategoryIds.length === 0) {
    showError('At least one Order Category must be selected');
    return;
  }
  
  if (partnerRateForm.installationMethodIds.length === 0) {
    showError('At least one Installation Method must be selected');
    return;
  }
  
  // Generate all combinations
  const combinations: Array<{
    orderCategoryId: string;
    installationMethodId: string | undefined;
  }> = [];
  
  for (const orderCategoryId of partnerRateForm.orderCategoryIds) {
    if (partnerRateForm.installationMethodIds.length === 0) {
      // If no methods selected, create one record with null method
      combinations.push({ orderCategoryId, installationMethodId: undefined });
    } else {
      for (const installationMethodId of partnerRateForm.installationMethodIds) {
        combinations.push({ orderCategoryId, installationMethodId });
      }
    }
  }
  
  // Create all rate records
  try {
    const promises = combinations.map(combo =>
      createGponPartnerJobRate({
        partnerGroupId: partnerRateForm.partnerGroupId,
        partnerId: partnerRateForm.partnerId || undefined,
        orderTypeId: partnerRateForm.orderTypeId,
        orderCategoryId: combo.orderCategoryId,
        installationMethodId: combo.installationMethodId,
        revenueAmount: parseFloat(partnerRateForm.revenueAmount) || 0,
        validFrom: partnerRateForm.validFrom || undefined,
        validTo: partnerRateForm.validTo || undefined,
        notes: partnerRateForm.notes || undefined,
        isActive: partnerRateForm.isActive
      })
    );
    
    await Promise.all(promises);
    showSuccess(`Created ${combinations.length} partner revenue rate(s) successfully`);
    setShowPartnerRateModal(false);
    resetPartnerRateForm();
    loadAllData();
  } catch (err: unknown) {
    const error = err as Error;
    showError(error.message || 'Failed to create partner rates');
  }
};
```

---

### Phase 2: Backend Bulk Create (Optional Enhancement)

#### Step 1: Create Bulk DTO

```csharp
public class BulkCreateGponPartnerJobRateDto
{
    public Guid PartnerGroupId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid OrderTypeId { get; set; }
    public List<Guid> OrderCategoryIds { get; set; } = new();
    public List<Guid> InstallationMethodIds { get; set; } = new();
    public decimal RevenueAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}
```

#### Step 2: Create Bulk Endpoint

```csharp
[HttpPost("gpon/partner-rates/bulk")]
public async Task<ActionResult<ApiResponse<List<GponPartnerJobRateDto>>>> BulkCreateGponPartnerJobRates(
    [FromBody] BulkCreateGponPartnerJobRateDto dto,
    CancellationToken cancellationToken = default)
{
    // Generate combinations
    var combinations = new List<(Guid OrderCategoryId, Guid? InstallationMethodId)>();
    
    foreach (var orderCategoryId in dto.OrderCategoryIds)
    {
        if (dto.InstallationMethodIds.Count == 0)
        {
            combinations.Add((orderCategoryId, null));
        }
        else
        {
            foreach (var installationMethodId in dto.InstallationMethodIds)
            {
                combinations.Add((orderCategoryId, installationMethodId));
            }
        }
    }
    
    // Create all records in transaction
    var rates = new List<GponPartnerJobRate>();
    foreach (var (orderCategoryId, installationMethodId) in combinations)
    {
        var rate = new GponPartnerJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            PartnerGroupId = dto.PartnerGroupId,
            PartnerId = dto.PartnerId,
            OrderTypeId = dto.OrderTypeId,
            OrderCategoryId = orderCategoryId,
            InstallationMethodId = installationMethodId,
            RevenueAmount = dto.RevenueAmount,
            Currency = dto.Currency ?? "MYR",
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsActive = dto.IsActive ?? true,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
        rates.Add(rate);
    }
    
    _context.GponPartnerJobRates.AddRange(rates);
    await _context.SaveChangesAsync(cancellationToken);
    
    // Return with names populated
    // ... (batch lookup and mapping)
}
```

---

## Checkbox Component Reference

### Native HTML Checkboxes

**Pattern from existing code:**
```tsx
// From MaterialsPage.tsx (line 484)
<input
  type="checkbox"
  checked={filters.isActive === true}
  onChange={(e) => setFilters({ ...filters, isActive: e.target.checked ? true : undefined })}
  className="h-3 w-3 rounded border-gray-300"
/>
```

### Checkbox Group Pattern

```tsx
<div className="space-y-2">
  <label className="text-sm font-medium">Order Categories *</label>
  <div className="space-y-2 max-h-48 overflow-y-auto border rounded p-2">
    {orderCategories.map(cat => (
      <label key={cat.id} className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-1 rounded">
        <input
          type="checkbox"
          checked={partnerRateForm.orderCategoryIds.includes(cat.id)}
          onChange={(e) => {
            // Handle toggle
          }}
          className="h-4 w-4 rounded border-input text-primary focus:ring-primary"
        />
        <span className="text-sm">{cat.name}</span>
      </label>
    ))}
  </div>
</div>
```

---

## Form Field Structure

### Complete Form Fields

1. **Partner Group** (dropdown) - Required
2. **Partner** (dropdown) - Optional
3. **Order Type** (dropdown) - Required
4. **Order Categories** (checkboxes) - Required, multi-select
5. **Installation Methods** (checkboxes) - Required, multi-select
6. **Revenue Amount** (number input) - Required
7. **Valid From** (date input) - Optional
8. **Valid To** (date input) - Optional
9. **Notes** (text input) - Optional
10. **Active** (checkbox) - Default: true

---

## Validation Rules

1. **Partner Group**: Required
2. **Order Type**: Required
3. **Order Categories**: At least one must be selected
4. **Installation Methods**: At least one must be selected (or allow "All Methods" = null)
5. **Revenue Amount**: Required, must be > 0

---

## State Management

### Form State Structure

```typescript
interface PartnerRateFormData {
  partnerGroupId: string;
  partnerId: string;
  orderTypeId: string;
  orderCategoryIds: string[];        // Array of selected IDs
  installationMethodIds: string[];   // Array of selected IDs
  revenueAmount: string;
  validFrom: string;
  validTo: string;
  notes: string;
  isActive: boolean;
}
```

### Toggle Handler

```typescript
const handleOrderCategoryToggle = (categoryId: string, checked: boolean) => {
  if (checked) {
    setPartnerRateForm({
      ...partnerRateForm,
      orderCategoryIds: [...partnerRateForm.orderCategoryIds, categoryId]
    });
  } else {
    setPartnerRateForm({
      ...partnerRateForm,
      orderCategoryIds: partnerRateForm.orderCategoryIds.filter(id => id !== categoryId)
    });
  }
};
```

---

## Submit Payload Format

### Option A: Multiple API Calls (Frontend)

```typescript
// Frontend generates combinations and calls API multiple times
for (const combo of combinations) {
  await createGponPartnerJobRate({
    partnerGroupId: '...',
    orderTypeId: '...',
    orderCategoryId: combo.orderCategoryId,
    installationMethodId: combo.installationMethodId,
    revenueAmount: 150.00,
    // ... other fields
  });
}
```

### Option B: Bulk API Call (Backend)

```typescript
// Frontend calls bulk endpoint once
await bulkCreateGponPartnerJobRates({
  partnerGroupId: '...',
  orderTypeId: '...',
  orderCategoryIds: ['id1', 'id2', 'id3'],
  installationMethodIds: ['id1', 'id2'],
  revenueAmount: 150.00,
  // ... other fields
});
```

---

## Reference Data Loading

**Endpoints Available:**
- ✅ `GET /api/order-categories` - Get all order categories
- ✅ `GET /api/installation-methods` - Get all installation methods
- ✅ `GET /api/partner-groups` - Get all partner groups
- ✅ `GET /api/order-types` - Get all order types

**Current Implementation:**
```typescript
// Already loaded in RateEngineManagementPage
const [orderCategories, setOrderCategories] = useState<OrderCategory[]>([]);
const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
```

---

## Files to Modify

### Frontend
1. ✅ `frontend/src/pages/settings/RateEngineManagementPage.tsx`
   - Update form state (orderCategoryIds, installationMethodIds arrays)
   - Add checkbox groups for Order Categories
   - Add checkbox groups for Installation Methods
   - Update submit handler to generate combinations
   - Update reset form function
   - Update edit handler (if editing, show selected checkboxes)

### Backend (Optional)
2. ⚠️ `backend/src/CephasOps.Api/Controllers/RatesController.cs`
   - Add bulk create endpoint (optional enhancement)
   - Add BulkCreateGponPartnerJobRateDto (optional)

---

## Summary

**Current State:**
- ❌ Entity supports single OrderCategoryId and InstallationMethodId
- ❌ Form uses single Select dropdowns
- ❌ No many-to-many relationships

**Recommended Solution:**
- ✅ **Option A: Create Multiple Records**
- ✅ Frontend: Add checkbox groups for multi-select
- ✅ Frontend: Generate all combinations on submit
- ✅ Frontend: Create multiple rate records (one per combination)
- ✅ Backend: No changes needed (or add bulk endpoint for efficiency)

**Implementation Complexity:** Medium
- Frontend: Checkbox groups + combination logic
- Backend: Optional bulk endpoint

**Risk Level:** Low
- No database schema changes
- No entity changes
- Maintains existing rate resolution logic

---

## Next Steps

1. **Implement checkbox groups in frontend form**
2. **Update form state to use arrays**
3. **Add combination generation logic**
4. **Update submit handler**
5. **Test with multiple selections**
6. **Optional: Add backend bulk endpoint for efficiency**

