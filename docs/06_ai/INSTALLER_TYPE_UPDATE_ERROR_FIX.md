# InstallerType Update Error - Fix Applied

**Date:** 2026-01-05  
**Status:** ✅ Fixed

---

## Problem Summary

**Error:** 400 Bad Request
- "JSON value could not be converted to InstallerType enum"
- "dto field is required"

**Root Cause:**
1. Frontend sends enum as string: `"installerType": "InHouse"` or `"installerType": "Subcontractor"`
2. Backend JSON serializer not configured to accept string enum values
3. `handleToggleStatus` missing `installerType` in update payload

---

## Fixes Applied

### ✅ Fix 1: Added JsonStringEnumConverter

**File:** `backend/src/CephasOps.Api/Program.cs`

**Changes:**
```csharp
// Added import
using System.Text.Json.Serialization;

// Updated JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new CephasOps.Api.Converters.NullableGuidJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // ✅ NEW
    });
```

**What it does:**
- Allows ASP.NET Core to accept string enum values in JSON
- Converts `"InHouse"` → `InstallerType.InHouse`
- Converts `"Subcontractor"` → `InstallerType.Subcontractor`

### ✅ Fix 2: Updated handleToggleStatus

**File:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`

**Changes:**
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
      installerType: installerType, // ✅ ADDED
      isSubcontractor: installerType === 'Subcontractor', // Sync for backward compatibility
      departmentId: si.departmentId || undefined,
    };
    
    await updateMutation.mutateAsync({ id: si.id, data: updatedData });
  } catch (err) {
    // Error is handled by mutation hook
  }
};
```

**What it does:**
- Includes `installerType` in update payload
- Ensures both `installerType` and `isSubcontractor` are synced

---

## How It Works Now

### Before Fix ❌
```
Frontend → "installerType": "InHouse" (string)
         ↓
Backend → Cannot convert string to enum → 400 Error
```

### After Fix ✅
```
Frontend → "installerType": "InHouse" (string)
         ↓
JsonStringEnumConverter → Converts "InHouse" to InstallerType.InHouse
         ↓
Backend → Accepts enum value → Update succeeds
```

---

## Testing

**To verify the fix:**

1. **Restart the API** (required for Program.cs changes)
   ```bash
   # Stop current API process
   # Then restart:
   dotnet watch run --project src/CephasOps.Api/CephasOps.Api.csproj
   ```

2. **Test Update Operation:**
   - Open Service Installers page
   - Click Edit on any installer
   - Change Installer Type (In-House ↔ Subcontractor)
   - Click Update
   - ✅ Should succeed without 400 error

3. **Test Toggle Status:**
   - Click status toggle (if still visible)
   - ✅ Should include installerType in payload

---

## Files Modified

1. ✅ `backend/src/CephasOps.Api/Program.cs`
   - Added `using System.Text.Json.Serialization;`
   - Added `JsonStringEnumConverter` to JSON options

2. ✅ `frontend/src/pages/settings/ServiceInstallersPage.tsx`
   - Updated `handleToggleStatus` to include `installerType`

---

## Expected Behavior

- ✅ Frontend sends: `"installerType": "InHouse"` or `"installerType": "Subcontractor"`
- ✅ Backend receives and converts string to enum
- ✅ Update operation succeeds
- ✅ No more 400 errors
- ✅ Enum values properly serialized/deserialized

---

## Important Note

**⚠️ API Restart Required:**
The `Program.cs` changes require an API restart to take effect. The JSON serializer configuration is loaded at startup.

---

## Summary

**Root Cause:** Missing `JsonStringEnumConverter` in JSON serialization options  
**Solution:** Added converter to accept string enum values  
**Additional Fix:** Included `installerType` in `handleToggleStatus` payload  

The error should be resolved after restarting the API.

