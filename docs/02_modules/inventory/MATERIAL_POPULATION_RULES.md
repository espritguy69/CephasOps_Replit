# Material Population Rules

**Last Updated:** 2025-01-10  
**Status:** ✅ Implemented

## Overview

This document describes the rules and logic for automatically populating materials when orders are created in CephasOps. The system supports two mechanisms for default material assignment:

1. **Building Default Materials** - Per-building, per-order-type defaults
2. **Material Templates** - Company-wide templates by order type, building type, and partner

---

## 1. Building Default Materials

### 1.1 Purpose

Building Default Materials allow administrators to configure specific materials that should be automatically added to orders for a particular building and order type combination.

**Use Case:** A specific building (e.g., "Tower A, Block B") always requires certain materials for Activation orders (e.g., "Fiber Cable 50m", "ONU Device", "Wall Mount Bracket").

### 1.2 Data Model

**Entity:** `BuildingDefaultMaterial`

**Key Fields:**
- `BuildingId` - FK to Building
- `OrderTypeId` - FK to OrderType (e.g., Activation, Modification Outdoor)
- `MaterialId` - FK to Material (must be non-serialized)
- `DefaultQuantity` - Default quantity to apply
- `IsActive` - Whether this default is currently active
- `Notes` - Optional notes

**Constraints:**
- Unique constraint: `(BuildingId, OrderTypeId, MaterialId)` - prevents duplicates
- Only non-serialized materials can be set as defaults
- Materials must exist and be active

### 1.3 Resolution Logic

When an order is created or a building is assigned:

1. **Query Building Default Materials:**
   ```sql
   SELECT * FROM BuildingDefaultMaterials
   WHERE BuildingId = @buildingId
     AND OrderTypeId = @orderTypeId
     AND IsActive = true
   ```

2. **Create OrderMaterial records:**
   - For each active default material, create an `OrderMaterial` record
   - Set `PlannedQuantity = DefaultQuantity`
   - Set `IsPlanned = true` (not yet consumed)

3. **Validation:**
   - Material must exist and be active
   - Material must be non-serialized
   - Building must exist

### 1.4 API Endpoints

**Service:** `IBuildingDefaultMaterialService`

- `GET /api/buildings/{buildingId}/default-materials?orderTypeId={orderTypeId}`
- `POST /api/buildings/{buildingId}/default-materials`
- `PUT /api/buildings/{buildingId}/default-materials/{id}`
- `DELETE /api/buildings/{buildingId}/default-materials/{id}`
- `GET /api/buildings/{buildingId}/default-materials/for-order?orderTypeId={orderTypeId}`

### 1.5 Frontend Integration

**Location:** `frontend/src/pages/buildings/BuildingDetailPage.tsx`

- Tab: "Default Materials"
- Allows adding/editing/deleting default materials per order type
- Shows materials grouped by order type
- Validates that materials are non-serialized

---

## 2. Material Templates

### 2.1 Purpose

Material Templates provide company-wide default material "kits" that can be applied based on:
- Order Type (e.g., Activation, Modification)
- Building Type (e.g., High-Rise, Terrace, Commercial)
- Partner (optional, for partner-specific kits)

**Use Case:** All TIME Prelaid Activation orders for High-Rise buildings should include a standard kit: "Fiber Cable 100m", "ONU Device", "Splitter 1:8", etc.

### 2.2 Data Model

**Entity:** `MaterialTemplate`

**Key Fields:**
- `CompanyId` - FK to Company
- `Name` - Template name (e.g., "TIME Prelaid High-Rise Kit")
- `OrderType` - Order type code (e.g., "ACTIVATION", "MODIFICATION_OUTDOOR")
- `BuildingTypeId` - FK to BuildingType (nullable)
- `PartnerId` - FK to Partner (nullable, for partner-specific templates)
- `IsDefault` - Whether this is the default template for the combination
- `IsActive` - Whether template is active

**Entity:** `MaterialTemplateItem`

**Key Fields:**
- `MaterialTemplateId` - FK to MaterialTemplate
- `MaterialId` - FK to Material
- `Quantity` - Default quantity
- `UnitOfMeasure` - Unit of measure
- `IsSerialised` - Whether material is serialized (mirrors Material)
- `Notes` - Optional notes

### 2.3 Resolution Logic

When an order is created:

1. **Resolve Template Priority:**
   ```
   Priority 1: Partner-specific template
     WHERE CompanyId = @companyId
       AND PartnerId = @partnerId
       AND OrderType = @orderType
       AND BuildingTypeId = @buildingTypeId
       AND IsActive = true
   
   Priority 2: Default template (no partner)
     WHERE CompanyId = @companyId
       AND PartnerId IS NULL
       AND OrderType = @orderType
       AND BuildingTypeId = @buildingTypeId
       AND IsDefault = true
       AND IsActive = true
   
   Priority 3: Template without building type
     WHERE CompanyId = @companyId
       AND PartnerId = @partnerId (or NULL)
       AND OrderType = @orderType
       AND BuildingTypeId IS NULL
       AND IsActive = true
   ```

2. **Apply Template Items:**
   - For each `MaterialTemplateItem` in the resolved template:
     - Create `OrderMaterial` record
     - Set `PlannedQuantity = Item.Quantity`
     - Set `IsPlanned = true`

3. **Merge with Building Defaults:**
   - Building defaults take precedence over template items
   - If same material exists in both, use building default quantity

### 2.4 API Endpoints

**Service:** `IMaterialTemplateService`

- `GET /api/material-templates?orderType={orderType}&buildingTypeId={buildingTypeId}&partnerId={partnerId}`
- `GET /api/material-templates/{id}`
- `POST /api/material-templates`
- `PUT /api/material-templates/{id}`
- `POST /api/material-templates/{id}/set-default`

### 2.5 Status

**Current Status:** ⏳ **Partially Implemented**

- Backend entities and services exist
- Frontend UI pending
- Integration with order creation pending

---

## 3. Order Creation Integration

### 3.1 When Materials Are Populated

Materials are automatically populated when:

1. **Order is created** - If building and order type are provided
2. **Building is assigned** - If order already exists but building is assigned later
3. **Order type is changed** - If order type changes, materials are re-evaluated

### 3.2 Application Order

1. **First:** Apply Material Template (if available)
2. **Then:** Apply Building Default Materials (overrides template items)
3. **Finally:** User can manually add/remove materials

### 3.3 Code Location

**Service:** `OrderService.CreateOrderAsync()`

**Method:** `ApplyDefaultMaterialsToOrderAsync()` (to be implemented)

**Logic:**
```csharp
// 1. Resolve material template
var template = await _materialTemplateService.GetEffectiveTemplateAsync(
    companyId, partnerId, orderType, buildingTypeId);

// 2. Apply template items
if (template != null)
{
    foreach (var item in template.Items)
    {
        // Create OrderMaterial
    }
}

// 3. Apply building defaults (overrides template)
var buildingDefaults = await _buildingDefaultMaterialService
    .GetMaterialsForOrderAsync(buildingId, orderTypeId);

foreach (var default in buildingDefaults)
{
    // Create or update OrderMaterial
}
```

---

## 4. Best Practices

### 4.1 When to Use Building Defaults

- **Use for:** Building-specific requirements
- **Examples:**
  - Specific cable lengths for a building
  - Building-specific equipment (e.g., "Tower A requires special brackets")
  - Historical patterns (this building always needs X)

### 4.2 When to Use Material Templates

- **Use for:** Standard kits per order type/building type
- **Examples:**
  - Standard activation kit for high-rise buildings
  - Partner-specific kits (TIME vs other partners)
  - Regional variations (different kits for different states)

### 4.3 Maintenance

- **Review quarterly:** Check if defaults/templates are still accurate
- **Deactivate, don't delete:** Set `IsActive = false` instead of deleting
- **Document changes:** Use `Notes` field to document why materials were added/removed
- **Test before activating:** Create test orders to verify material population

---

## 5. Troubleshooting

### 5.1 Materials Not Populating

**Check:**
1. Is `IsActive = true` for defaults/templates?
2. Does building have defaults for this order type?
3. Does template exist for this order type + building type + partner?
4. Are materials non-serialized (for building defaults)?
5. Are materials active in inventory?

### 5.2 Wrong Materials Populating

**Check:**
1. Is there a more specific template (partner-specific vs default)?
2. Are building defaults overriding template incorrectly?
3. Is `IsDefault` flag set correctly on templates?
4. Are there multiple active templates for the same combination?

### 5.3 Performance Issues

**Optimization:**
- Indexes on `(BuildingId, OrderTypeId, IsActive)` for BuildingDefaultMaterials
- Indexes on `(CompanyId, OrderType, BuildingTypeId, PartnerId, IsActive)` for MaterialTemplates
- Cache templates in memory (refresh on template changes)

---

## 6. Future Enhancements

### 6.1 Planned Features

- [ ] Material template UI (admin page)
- [ ] Bulk import/export of templates
- [ ] Template versioning (effective dates)
- [ ] Material suggestions based on historical usage
- [ ] Integration with inventory availability (suggest alternatives if out of stock)

### 6.2 Considerations

- **Serialized Materials:** Currently only non-serialized materials can be defaults. Serialized materials require manual selection.
- **Quantity Variations:** Consider supporting quantity ranges or formulas (e.g., "1 per floor")
- **Conditional Logic:** Support for conditional materials (e.g., "If FTTR, add X; if FTTC, add Y")

---

## 7. Related Documentation

- `docs/02_modules/inventory/OVERVIEW.md` - Inventory module overview
- `docs/02_modules/orders/OVERVIEW.md` - Orders module overview
- `docs/02_modules/materials/MATERIAL_TEMPLATES_MODULE.md` - Material templates specification
- `docs/05_data_model/entities/buildings_entities.md` - Building entities documentation

---

## 8. API Examples

### 8.1 Get Building Default Materials

```http
GET /api/buildings/{buildingId}/default-materials?orderTypeId={orderTypeId}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "buildingId": "guid",
      "orderTypeId": "guid",
      "orderTypeName": "Activation",
      "materialId": "guid",
      "materialCode": "FIBER-50M",
      "materialDescription": "Fiber Cable 50m",
      "defaultQuantity": 2,
      "notes": "Standard for this building",
      "isActive": true
    }
  ]
}
```

### 8.2 Create Building Default Material

```http
POST /api/buildings/{buildingId}/default-materials
Content-Type: application/json

{
  "orderTypeId": "guid",
  "materialId": "guid",
  "defaultQuantity": 2,
  "notes": "Required for all activation orders"
}
```

---

**End of Document**

