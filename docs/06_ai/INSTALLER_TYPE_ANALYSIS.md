# Installer Type Field Analysis & Implementation Plan

**Date:** 2026-01-05  
**Objective:** Add "Installer Type" field with "In-House" and "Subcontractor" options to Service Installers

---

## 1. CURRENT STATE ANALYSIS

### 1.1 Does InstallerType Already Exist?

**Answer: ❌ NO**

**Evidence:**
- No matches found for "InstallerType", "installerType", or "installer_type" in backend or frontend
- No enum or lookup table exists for installer types

### 1.2 Current Implementation

**Existing Field: `IsSubcontractor` (boolean)**
- **Location:** `ServiceInstaller` entity
- **Type:** `bool`
- **Current Usage:**
  - `true` = Subcontractor
  - `false` = In-House Employee (implicit)

**Files Using IsSubcontractor:**
- Backend:
  - `ServiceInstaller.cs` - Entity property
  - `ServiceInstallerDto.cs` - DTOs (Create, Update, List)
  - `ServiceInstallerService.cs` - Service layer mapping
  - `ServiceInstallerConfiguration.cs` - EF Core configuration
  - `SchedulerService.cs` - Used in scheduler DTOs
  - Multiple migrations (field exists since initial creation)

- Frontend:
  - `serviceInstallers.ts` - TypeScript interfaces
  - `ServiceInstallersPage.tsx` - Form checkbox, table column, filter
  - `ServiceInstallersPageEnhanced.tsx` - Grid column
  - `scheduler.ts` - Scheduler types
  - `excelExport.ts` - Excel export utility

**Current Display:**
- Table shows: "Subcontractor" or "Employee" based on boolean
- Form uses: Checkbox labeled "Subcontractor"
- Filter uses: "Employee" vs "Subcontractor" options

---

## 2. RECOMMENDED APPROACH

### 2.1 Option Comparison

#### **Option A: Enum (RECOMMENDED) ✅**

**Pros:**
- Simple and explicit (In-House, Subcontractor)
- Type-safe in C# and TypeScript
- Easy to extend in future (e.g., "Freelancer", "Partner")
- No additional database table needed
- Better than boolean for clarity
- Can map existing boolean data easily

**Cons:**
- Requires code changes to add new types
- Less flexible than lookup table

**Implementation:**
- C# enum: `InstallerType { InHouse, Subcontractor }`
- Stored as string in database: "InHouse", "Subcontractor"
- Default: "InHouse"

#### **Option B: Lookup Table**

**Pros:**
- Very flexible - can add types via UI
- No code changes needed for new types

**Cons:**
- Overkill for just 2 values
- Additional table and joins
- More complex queries
- Not needed for current requirements

### 2.2 Decision: **Option A - Enum**

**Reasoning:**
1. Only 2 values needed (In-House, Subcontractor)
2. Unlikely to change frequently
3. Simpler implementation
4. Better performance (no joins)
5. Type-safe
6. Can migrate existing `IsSubcontractor` boolean easily

---

## 3. IMPLEMENTATION PLAN

### 3.1 Backend Changes

#### **Step 1: Create Enum**

**File:** `backend/src/CephasOps.Domain/ServiceInstallers/Enums/InstallerType.cs` (NEW)

```csharp
namespace CephasOps.Domain.ServiceInstallers.Enums;

public enum InstallerType
{
    InHouse = 0,
    Subcontractor = 1
}
```

#### **Step 2: Update Entity**

**File:** `backend/src/CephasOps.Domain/ServiceInstallers/Entities/ServiceInstaller.cs`

**Changes:**
- Add: `public InstallerType InstallerType { get; set; } = InstallerType.InHouse;`
- **Option:** Keep `IsSubcontractor` for backward compatibility (mark as obsolete) OR remove it
- **Recommendation:** Keep both temporarily, mark `IsSubcontractor` as `[Obsolete]`, migrate data, then remove in future

#### **Step 3: Update EF Core Configuration**

**File:** `backend/src/CephasOps.Infrastructure/Persistence/Configurations/ServiceInstallers/ServiceInstallerConfiguration.cs`

**Changes:**
- Add: `builder.Property(si => si.InstallerType).HasConversion<string>().HasMaxLength(50);`
- Keep `IsSubcontractor` configuration for now

#### **Step 4: Update DTOs**

**File:** `backend/src/CephasOps.Application/ServiceInstallers/DTOs/ServiceInstallerDto.cs`

**Changes:**
- Add `InstallerType` to `ServiceInstallerDto`
- Add `InstallerType` to `CreateServiceInstallerDto` (default: `InHouse`)
- Add `InstallerType?` to `UpdateServiceInstallerDto` (nullable)
- Keep `IsSubcontractor` for backward compatibility

#### **Step 5: Update Service Layer**

**File:** `backend/src/CephasOps.Application/ServiceInstallers/Services/ServiceInstallerService.cs`

**Changes:**
- Update all DTO mappings to include `InstallerType`
- Add logic to sync `InstallerType` with `IsSubcontractor` during migration period
- Update create/update methods

#### **Step 6: Create Migration**

**Migration Name:** `AddInstallerTypeToServiceInstallers`

**SQL Changes:**
1. Add `InstallerType` column (string, nullable initially)
2. Migrate existing data: `UPDATE "ServiceInstallers" SET "InstallerType" = CASE WHEN "IsSubcontractor" = true THEN 'Subcontractor' ELSE 'InHouse' END;`
3. Make `InstallerType` NOT NULL
4. Set default value to 'InHouse'

#### **Step 7: Update Controller (if needed)**

**File:** `backend/src/CephasOps.Api/Controllers/ServiceInstallersController.cs`

**Changes:**
- No validation changes needed (enum handles it)
- CSV export can use `InstallerType` instead of deriving from `IsSubcontractor`

---

### 3.2 Frontend Changes

#### **Step 1: Update TypeScript Types**

**File:** `frontend/src/types/serviceInstallers.ts`

**Changes:**
- Add enum: `export type InstallerType = 'InHouse' | 'Subcontractor';`
- Add `installerType: InstallerType;` to `ServiceInstaller` interface
- Add `installerType?: InstallerType;` to `CreateServiceInstallerRequest` (default: 'InHouse')
- Add `installerType?: InstallerType;` to `UpdateServiceInstallerRequest`
- Keep `isSubcontractor` for backward compatibility during migration

#### **Step 2: Update Form**

**File:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`

**Changes:**
- Replace checkbox with dropdown/select:
  ```tsx
  <select
    value={formData.installerType}
    onChange={(e) => setFormData({ ...formData, installerType: e.target.value as InstallerType })}
  >
    <option value="InHouse">In-House</option>
    <option value="Subcontractor">Subcontractor</option>
  </select>
  ```
- Update form data interface
- Update form initialization (default: 'InHouse')
- Update create/update handlers

#### **Step 3: Update Table Column**

**File:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`

**Changes:**
- Update "Type" column to use `installerType` instead of `isSubcontractor`
- Update render function to show "In-House" or "Subcontractor"
- Update filter to use `installerType`

#### **Step 4: Update Enhanced Page**

**File:** `frontend/src/pages/serviceInstallers/ServiceInstallersPageEnhanced.tsx`

**Changes:**
- Update grid column to use `installerType`
- Update dropdown data source for editing

#### **Step 5: Update Other References**

**Files:**
- `frontend/src/types/scheduler.ts` - Update if needed
- `frontend/src/utils/excelExport.ts` - Update export logic

---

## 4. MIGRATION STRATEGY

### 4.1 Data Migration Approach

**Phase 1: Add New Field (Non-Breaking)**
1. Add `InstallerType` column as nullable
2. Migrate existing data from `IsSubcontractor`
3. Make column NOT NULL with default

**Phase 2: Update Code (Backward Compatible)**
1. Add `InstallerType` to all DTOs and mappings
2. Keep `IsSubcontractor` for backward compatibility
3. Sync both fields during create/update

**Phase 3: Update Frontend**
1. Add `InstallerType` to forms and tables
2. Keep `IsSubcontractor` display for existing data
3. New records use `InstallerType`

**Phase 4: Cleanup (Future)**
1. Remove `IsSubcontractor` from entity
2. Remove from DTOs
3. Remove from frontend
4. Create migration to drop column

---

## 5. STEP-BY-STEP CHECKLIST

### Backend Implementation

- [ ] **1. Create Enum**
  - [ ] Create `InstallerType.cs` in `Domain/ServiceInstallers/Enums/`
  - [ ] Define: `InHouse = 0`, `Subcontractor = 1`

- [ ] **2. Update Entity**
  - [ ] Add `InstallerType` property to `ServiceInstaller.cs`
  - [ ] Set default: `InstallerType.InHouse`
  - [ ] Mark `IsSubcontractor` as `[Obsolete]` (optional, for migration period)

- [ ] **3. Update EF Core Configuration**
  - [ ] Add `InstallerType` property configuration
  - [ ] Configure as string conversion with max length 50

- [ ] **4. Update DTOs**
  - [ ] Add `InstallerType` to `ServiceInstallerDto`
  - [ ] Add `InstallerType` to `CreateServiceInstallerDto` (default: `InHouse`)
  - [ ] Add `InstallerType?` to `UpdateServiceInstallerDto`

- [ ] **5. Update Service Layer**
  - [ ] Update `GetServiceInstallersAsync` - map `InstallerType`
  - [ ] Update `GetServiceInstallerByIdAsync` - map `InstallerType`
  - [ ] Update `CreateServiceInstallerAsync` - set `InstallerType` from DTO
  - [ ] Update `UpdateServiceInstallerAsync` - update `InstallerType`
  - [ ] Add sync logic: `InstallerType = dto.IsSubcontractor ? InstallerType.Subcontractor : InstallerType.InHouse` (during migration)

- [ ] **6. Create Migration**
  - [ ] Run: `dotnet ef migrations add AddInstallerTypeToServiceInstallers`
  - [ ] Edit migration to:
    - Add column as nullable
    - Migrate data: `UPDATE "ServiceInstallers" SET "InstallerType" = CASE WHEN "IsSubcontractor" = true THEN 'Subcontractor' ELSE 'InHouse' END;`
    - Make NOT NULL
    - Set default value

- [ ] **7. Update Controller**
  - [ ] Update CSV export to use `InstallerType`

### Frontend Implementation

- [ ] **1. Update TypeScript Types**
  - [ ] Add `InstallerType` type: `'InHouse' | 'Subcontractor'`
  - [ ] Add `installerType` to `ServiceInstaller` interface
  - [ ] Add `installerType` to `CreateServiceInstallerRequest`
  - [ ] Add `installerType` to `UpdateServiceInstallerRequest`

- [ ] **2. Update Form (ServiceInstallersPage.tsx)**
  - [ ] Replace `isSubcontractor` checkbox with `installerType` dropdown
  - [ ] Update form data interface
  - [ ] Update form initialization (default: 'InHouse')
  - [ ] Update `handleCreate` to send `installerType`
  - [ ] Update `handleUpdate` to send `installerType`
  - [ ] Update `openEditModal` to set `installerType` from data

- [ ] **3. Update Table Column**
  - [ ] Update "Type" column to use `installerType`
  - [ ] Update render function
  - [ ] Update filter to use `installerType`

- [ ] **4. Update Enhanced Page**
  - [ ] Update grid column for `installerType`
  - [ ] Update dropdown data source

- [ ] **5. Update Other Files**
  - [ ] Update `scheduler.ts` if needed
  - [ ] Update `excelExport.ts` if needed

---

## 6. FORM FIELD SPECIFICATION

### Dropdown Component

**Location:** `ServiceInstallersPage.tsx` - Details tab

**Implementation:**
```tsx
<div className="space-y-1">
  <label className="text-sm font-medium">Installer Type *</label>
  <select
    name="installerType"
    value={formData.installerType}
    onChange={(e) => setFormData({ ...formData, installerType: e.target.value as InstallerType })}
    className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
    required
  >
    <option value="InHouse">In-House</option>
    <option value="Subcontractor">Subcontractor</option>
  </select>
</div>
```

**Position:** After "SI Level" field, before "Phone/Email" grid

**Validation:**
- Required field
- Must be one of: "InHouse", "Subcontractor"
- Default: "InHouse"

---

## 7. TABLE COLUMN SPECIFICATION

### Column Definition

**Location:** `ServiceInstallersPage.tsx` - columns array

**Current Column:**
```tsx
{ 
  key: 'isSubcontractor', 
  label: 'Type', 
  render: (value) => (
    <span className={`px-2 py-1 rounded text-xs font-medium border ${
      value 
        ? 'bg-amber-100 text-amber-800 border-amber-300'
        : 'bg-teal-100 text-teal-800 border-teal-300'
    }`}>
      {value ? 'Subcontractor' : 'Employee'}
    </span>
  ),
  sortable: true,
  sortValue: (row) => row.isSubcontractor ? 'Subcontractor' : 'Employee'
}
```

**Updated Column:**
```tsx
{ 
  key: 'installerType', 
  label: 'Type', 
  render: (value) => {
    const typeColors: Record<string, string> = {
      'InHouse': 'bg-teal-100 text-teal-800 border-teal-300',
      'Subcontractor': 'bg-amber-100 text-amber-800 border-amber-300',
    };
    const color = typeColors[value as string] || 'bg-gray-100 text-gray-800 border-gray-300';
    return (
      <span className={`px-2 py-1 rounded text-xs font-medium border ${color}`}>
        {value === 'InHouse' ? 'In-House' : value === 'Subcontractor' ? 'Subcontractor' : '-'}
      </span>
    );
  },
  sortable: true,
  sortValue: (row) => row.installerType === 'InHouse' ? 'In-House' : 'Subcontractor'
}
```

**Filter Update:**
- Replace "Employee" with "In-House"
- Keep "Subcontractor" option
- Update filter logic to use `installerType`

---

## 8. VALIDATION RULES

### Backend Validation

**Location:** Service layer (`ServiceInstallerService.cs`)

**Rules:**
- `InstallerType` must be valid enum value
- Default: `InstallerType.InHouse` if not provided
- No additional validation needed (enum handles it)

### Frontend Validation

**Location:** Form component

**Rules:**
- Required field
- Must be "InHouse" or "Subcontractor"
- Default: "InHouse"

---

## 9. DEFAULT VALUE RECOMMENDATION

**Default:** `InHouse` (In-House)

**Reasoning:**
1. Most installers are typically in-house employees
2. Matches current behavior (IsSubcontractor defaults to `false`)
3. More common use case

**Implementation:**
- Backend: `InstallerType.InHouse` (enum default)
- Frontend: `'InHouse'` in form initialization
- Database: Default constraint: `'InHouse'`

---

## 10. BACKWARD COMPATIBILITY STRATEGY

### Migration Period (Recommended: 1-2 months)

**Keep Both Fields:**
- `IsSubcontractor` (boolean) - marked as `[Obsolete]`
- `InstallerType` (enum) - new field

**Sync Logic:**
```csharp
// During create/update, sync both fields
if (dto.InstallerType.HasValue)
{
    serviceInstaller.InstallerType = dto.InstallerType.Value;
    serviceInstaller.IsSubcontractor = dto.InstallerType.Value == InstallerType.Subcontractor;
}
else if (dto.IsSubcontractor.HasValue)
{
    // Backward compatibility
    serviceInstaller.IsSubcontractor = dto.IsSubcontractor.Value;
    serviceInstaller.InstallerType = dto.IsSubcontractor.Value ? InstallerType.Subcontractor : InstallerType.InHouse;
}
```

### After Migration Period

1. Remove `IsSubcontractor` from entity
2. Remove from DTOs
3. Remove from frontend
4. Create migration to drop column
5. Update all references

---

## 11. FILES TO MODIFY

### Backend Files

1. **NEW:** `backend/src/CephasOps.Domain/ServiceInstallers/Enums/InstallerType.cs`
2. **MODIFY:** `backend/src/CephasOps.Domain/ServiceInstallers/Entities/ServiceInstaller.cs`
3. **MODIFY:** `backend/src/CephasOps.Infrastructure/Persistence/Configurations/ServiceInstallers/ServiceInstallerConfiguration.cs`
4. **MODIFY:** `backend/src/CephasOps.Application/ServiceInstallers/DTOs/ServiceInstallerDto.cs`
5. **MODIFY:** `backend/src/CephasOps.Application/ServiceInstallers/Services/ServiceInstallerService.cs`
6. **MODIFY:** `backend/src/CephasOps.Api/Controllers/ServiceInstallersController.cs`
7. **NEW:** Migration file (auto-generated)

### Frontend Files

1. **MODIFY:** `frontend/src/types/serviceInstallers.ts`
2. **MODIFY:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`
3. **MODIFY:** `frontend/src/pages/serviceInstallers/ServiceInstallersPageEnhanced.tsx`
4. **MODIFY:** `frontend/src/types/scheduler.ts` (if needed)
5. **MODIFY:** `frontend/src/utils/excelExport.ts` (if needed)

---

## 12. SUMMARY

### Current State
- ❌ `InstallerType` does NOT exist
- ✅ `IsSubcontractor` (boolean) exists and serves this purpose
- Current: `true` = Subcontractor, `false` = In-House (implicit)

### Recommended Approach
- ✅ **Enum** (Option A) - Simple, type-safe, easy to extend
- ❌ Lookup Table (Option B) - Overkill for 2 values

### Implementation Strategy
1. Add `InstallerType` enum field
2. Keep `IsSubcontractor` for backward compatibility
3. Migrate existing data
4. Update frontend to use `InstallerType`
5. Remove `IsSubcontractor` in future cleanup

### Key Decisions
- **Default Value:** `InHouse`
- **Storage:** String in database ("InHouse", "Subcontractor")
- **Migration:** Keep both fields during transition period
- **Form Control:** Dropdown/Select (not checkbox)
- **Table Display:** Badge with color coding

---

**Status:** Ready for implementation  
**Estimated Effort:** Medium (4-6 hours)  
**Risk Level:** Low (backward compatible approach)

