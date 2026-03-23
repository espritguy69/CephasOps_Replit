# InstallerType Update Error - Resolution Status

**Date:** 2026-01-05  
**Error:** 400 - "JSON value could not be converted to InstallerType enum" and "dto field is required"

---

## Status: ⚠️ **NOT YET RESOLVED** - API Restart Required

The fixes have been applied to the code, but **the API must be restarted** for the changes to take effect.

---

## Fixes Applied

### ✅ Fix 1: Added JsonStringEnumConverter

**File:** `backend/src/CephasOps.Api/Program.cs`

**Change:**
```csharp
options.JsonSerializerOptions.Converters.Add(
    new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)
);
```

**What it does:**
- Accepts string enum values: `"InHouse"` and `"Subcontractor"`
- Uses exact enum names (case-sensitive, no camelCase conversion)
- Converts strings to enum values during JSON deserialization

### ✅ Fix 2: Added InstallerType Update Logic

**File:** `backend/src/CephasOps.Application/ServiceInstallers/Services/ServiceInstallerService.cs`

**Change:**
Added InstallerType update logic in `UpdateServiceInstallerAsync` method:
```csharp
// Update InstallerType (prioritize InstallerType, fallback to IsSubcontractor for backward compatibility)
if (dto.InstallerType.HasValue)
{
    serviceInstaller.InstallerType = dto.InstallerType.Value;
    serviceInstaller.IsSubcontractor = dto.InstallerType.Value == InstallerType.Subcontractor; // Sync
}
else if (dto.IsSubcontractor.HasValue)
{
    // Backward compatibility: derive InstallerType from IsSubcontractor
    serviceInstaller.InstallerType = dto.IsSubcontractor.Value ? InstallerType.Subcontractor : InstallerType.InHouse;
    serviceInstaller.IsSubcontractor = dto.IsSubcontractor.Value;
}
```

**What it does:**
- Updates `InstallerType` field when provided in DTO
- Syncs `IsSubcontractor` for backward compatibility
- Falls back to `IsSubcontractor` if `InstallerType` not provided

### ✅ Fix 3: Updated handleToggleStatus

**File:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`

**Change:**
Added `installerType` to toggle status update payload.

---

## Why Error Still Occurs

**The error persists because:**
1. ❌ **API not restarted** - `Program.cs` changes require restart
2. ❌ **JSON converter not loaded** - Configuration is loaded at startup
3. ❌ **Service layer logic not active** - Code changes not in running process

---

## Resolution Steps

### Step 1: Stop the API

**Current Process:** Process ID 13652 (or similar)

**Stop it:**
- Close the terminal running `dotnet run` or `dotnet watch`
- Or kill process: `taskkill /F /PID 13652`

### Step 2: Restart the API

```bash
cd C:\Projects\CephasOps\backend
dotnet watch run --project src/CephasOps.Api/CephasOps.Api.csproj
```

### Step 3: Test the Update

1. Open Service Installers page
2. Click Edit on any installer
3. Change Installer Type (In-House ↔ Subcontractor)
4. Click Update
5. ✅ Should succeed without 400 error

---

## Expected Behavior After Restart

**Before Restart:**
```
Frontend → "installerType": "InHouse"
         ↓
Backend → ❌ Cannot convert string to enum → 400 Error
```

**After Restart:**
```
Frontend → "installerType": "InHouse"
         ↓
JsonStringEnumConverter → Converts "InHouse" to InstallerType.InHouse
         ↓
Service Layer → Updates InstallerType field
         ↓
Backend → ✅ Update succeeds
```

---

## Verification

After restarting, the error should be resolved. If it still occurs:

1. **Check API logs** for startup errors
2. **Verify JsonStringEnumConverter is loaded** (check startup logs)
3. **Test with a simple update** (change only InstallerType)
4. **Check browser Network tab** to see actual payload being sent

---

## Summary

✅ **Code fixes applied**  
❌ **API restart required**  
⏳ **Error will be resolved after restart**

**Action Required:** Stop and restart the API to apply the JSON converter configuration.

