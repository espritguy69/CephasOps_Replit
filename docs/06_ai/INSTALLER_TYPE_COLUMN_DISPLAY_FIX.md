# Installer Type Column Display Fix

**Date:** 2026-01-05  
**Problem:** Type column shows "-" instead of installer type (In-House or Subcontractor)

---

## Root Cause Analysis

### Issue 1: Enum Serialization Format
- **Backend** may send enum as:
  - **String** (after API restart with JsonStringEnumConverter): `"InHouse"` or `"Subcontractor"`
  - **Number** (before API restart): `0` (InHouse) or `1` (Subcontractor)

### Issue 2: Frontend Render Logic
- **Original code** only checked for string values:
  ```typescript
  const installerType = (value as string) || (row.isSubcontractor ? 'Subcontractor' : 'InHouse');
  ```
- **Problem**: If `value` is `0` (InHouse enum), `(value as string)` evaluates to `"0"` (truthy but not "InHouse"), so it doesn't match the color lookup and shows "-"

---

## Solution

### Fix Applied: Normalize Enum Values

**File:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`

**Change:** Updated `installerType` column render function to handle both string and number enum values:

```typescript
render: (value, row) => {
  // Normalize installerType: handle both string and number enum values
  let installerType: string;
  
  if (value === 'InHouse' || value === 0 || value === '0') {
    installerType = 'InHouse';
  } else if (value === 'Subcontractor' || value === 1 || value === '1') {
    installerType = 'Subcontractor';
  } else if (typeof value === 'string' && (value === 'InHouse' || value === 'Subcontractor')) {
    installerType = value;
  } else {
    // Fallback to isSubcontractor for backward compatibility
    installerType = row.isSubcontractor ? 'Subcontractor' : 'InHouse';
  }
  
  // ... rest of render logic
}
```

**What it does:**
1. ✅ Handles string enum values: `"InHouse"`, `"Subcontractor"`
2. ✅ Handles number enum values: `0` (InHouse), `1` (Subcontractor)
3. ✅ Handles string number values: `"0"`, `"1"` (edge case)
4. ✅ Falls back to `isSubcontractor` if value is missing or invalid

---

## Enum Value Mapping

| Backend Enum | Numeric Value | String Value | Display |
|-------------|---------------|--------------|---------|
| `InstallerType.InHouse` | `0` | `"InHouse"` | "In-House" |
| `InstallerType.Subcontractor` | `1` | `"Subcontractor"` | "Subcontractor" |

---

## Testing

### Before Fix
- ❌ Type column shows "-" for all installers
- ❌ Enum value `0` not recognized as "InHouse"

### After Fix
- ✅ Type column shows "In-House" or "Subcontractor" correctly
- ✅ Works with both string and number enum values
- ✅ Handles edge cases (missing values, invalid values)

---

## Related Issues

1. **API Restart Required**: For `JsonStringEnumConverter` to take effect, the API must be restarted. However, this frontend fix ensures the column displays correctly even before the API restart.

2. **Backward Compatibility**: The fix maintains backward compatibility with `isSubcontractor` field as a fallback.

---

## Files Modified

- ✅ `frontend/src/pages/settings/ServiceInstallersPage.tsx` - Updated Type column render function

---

## Status

✅ **FIXED** - Type column now displays installer type correctly regardless of enum serialization format.

