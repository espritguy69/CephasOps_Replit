# InstallerType Update Error Analysis

**Date:** 2026-01-05  
**Error:** 400 - "JSON value could not be converted to InstallerType enum" and "dto field is required"

---

## Problem Identified

### Root Cause

**Issue 1: JSON Enum Serialization**
- Frontend sends: `"installerType": "InHouse"` or `"installerType": "Subcontractor"` (string)
- Backend expects: Enum value, but JSON serializer is not configured to accept strings
- **Program.cs** doesn't have `JsonStringEnumConverter` configured

**Issue 2: Missing InstallerType in handleToggleStatus**
- `handleToggleStatus` function doesn't include `installerType` in update payload
- This might cause validation errors if backend expects it

---

## Current Configuration

### Backend Enum Definition
```csharp
public enum InstallerType
{
    InHouse = 0,
    Subcontractor = 1
}
```

### Backend DTO
```csharp
public class UpdateServiceInstallerDto
{
    public InstallerType? InstallerType { get; set; } // Nullable
    // ... other fields
}
```

### Frontend Payload
```typescript
{
  installerType: 'InHouse' | 'Subcontractor', // String value
  // ... other fields
}
```

### JSON Serialization Config (Program.cs)
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new NullableGuidJsonConverter());
        // ❌ MISSING: JsonStringEnumConverter
    });
```

---

## Solution

### Fix 1: Add JsonStringEnumConverter

**File:** `backend/src/CephasOps.Api/Program.cs`

Add `JsonStringEnumConverter` to accept string enum values:

```csharp
using System.Text.Json.Serialization;

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new NullableGuidJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // ✅ ADD THIS
    });
```

### Fix 2: Update handleToggleStatus

**File:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`

Include `installerType` in the update payload:

```typescript
const handleToggleStatus = async (si: ServiceInstaller): Promise<void> => {
  try {
    const installerType = si.installerType || (si.isSubcontractor ? 'Subcontractor' : 'InHouse');
    const updatedData: UpdateServiceInstallerRequest = {
      name: si.name,
      email: si.email || undefined,
      phone: si.phone || undefined,
      isActive: !si.isActive,
      employeeId: si.employeeId || undefined,
      siLevel: si.siLevel,
      installerType: installerType, // ✅ ADD THIS
      isSubcontractor: installerType === 'Subcontractor', // Sync for backward compatibility
      departmentId: si.departmentId || undefined,
    };
    
    await updateMutation.mutateAsync({ id: si.id, data: updatedData });
  } catch (err) {
    // Error is handled by mutation hook
  }
};
```

---

## Verification

After fixes:
1. Frontend sends: `"installerType": "InHouse"` (string)
2. Backend receives: String value
3. JsonStringEnumConverter converts: `"InHouse"` → `InstallerType.InHouse`
4. Update succeeds ✅

---

## Files to Modify

1. **`backend/src/CephasOps.Api/Program.cs`**
   - Add `using System.Text.Json.Serialization;`
   - Add `JsonStringEnumConverter` to JSON options

2. **`frontend/src/pages/settings/ServiceInstallersPage.tsx`**
   - Update `handleToggleStatus` to include `installerType`

---

## Expected Behavior After Fix

- ✅ Frontend sends string enum values: `"InHouse"` or `"Subcontractor"`
- ✅ Backend accepts and converts string to enum
- ✅ Update operation succeeds
- ✅ No more 400 errors

