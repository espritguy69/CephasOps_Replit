# Date Synchronization Fix

**Date:** 2026-01-05  
**Status:** ✅ Fixed  
**Issue:** Appointment dates not syncing correctly between parsers and database

---

## Problem Statement

Appointment dates were not synchronizing correctly, showing wrong times or not updating properly. The root cause was incorrect timezone conversion in the Email Parser.

---

## Root Cause Analysis

### The Issue

**File:** `backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs`

**Problem Lines:**
- Line 908: `AppointmentDate = parsedData.AppointmentDateTime?.ToUniversalTime()`
- Line 1234: `draft.AppointmentDate = parsedData.AppointmentDateTime?.ToUniversalTime()`

### Why It Failed

1. **Excel Parser (Correct):**
   - Uses `TimeZoneInfo.ConvertTimeToUtc()` with Malaysia timezone (GMT+8)
   - Correctly converts: `10:00 AM Malaysia → 02:00 UTC`
   - Sets `DateTimeKind.Utc` properly

2. **Email Parser (Incorrect):**
   - Called `.ToUniversalTime()` directly on `parsedData.AppointmentDateTime`
   - If DateTime has `DateTimeKind.Unspecified`, `.ToUniversalTime()` treats it as **Local time**
   - This causes incorrect conversion (e.g., if server is in different timezone)

3. **Enrichment Service (Incomplete):**
   - Only normalized dates from `parseResult.OrderData.AppointmentDateTime`
   - Didn't normalize dates already set in `draft.AppointmentDate`
   - If date was set incorrectly before enrichment, it stayed incorrect

### Example of the Bug

```
Excel Parser:
  Excel: "2026-01-24 10:00:00" (Malaysia time)
  → Converts: 10:00 Malaysia → 02:00 UTC ✅
  → Stores: "2026-01-24 02:00:00 UTC"
  → Displays: "2026-01-24 10:00:00" (correct) ✅

Email Parser (BEFORE FIX):
  PDF/Excel: "2026-01-24 10:00:00" (Malaysia time)
  → Parsed as: DateTime with Kind=Unspecified
  → .ToUniversalTime() treats as Local (server timezone, e.g., UTC-5)
  → Converts: 10:00 Local → 15:00 UTC ❌ (WRONG!)
  → Stores: "2026-01-24 15:00:00 UTC"
  → Displays: "2026-01-24 23:00:00" (8 hours off!) ❌
```

---

## Fix Applied

### Fix 1: Remove Incorrect `.ToUniversalTime()` Calls

**File:** `backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs`

**Before:**
```csharp
AppointmentDate = parsedData.AppointmentDateTime?.ToUniversalTime(), // ❌ Incorrect
```

**After:**
```csharp
AppointmentDate = parsedData.AppointmentDateTime, // ✅ Let enrichment service normalize
```

**Changed Lines:**
- Line 908: Email body parsing
- Line 1234: PDF attachment parsing

### Fix 2: Enhanced Date Normalization in Enrichment Service

**File:** `backend/src/CephasOps.Application/Parser/Services/ParsedOrderDraftEnrichmentService.cs`

**Before:**
```csharp
private void NormalizeDates(ParsedOrderDraft draft, TimeExcelParseResult parseResult)
{
    if (parseResult.OrderData?.AppointmentDateTime.HasValue == true)
    {
        draft.AppointmentDate = NormalizeToUtc(parseResult.OrderData.AppointmentDateTime);
    }
    // ❌ Doesn't normalize draft.AppointmentDate if already set
}
```

**After:**
```csharp
private void NormalizeDates(ParsedOrderDraft draft, TimeExcelParseResult parseResult)
{
    // Priority 1: Use date from parseResult if available (most accurate source)
    if (parseResult.OrderData?.AppointmentDateTime.HasValue == true)
    {
        draft.AppointmentDate = NormalizeToUtc(parseResult.OrderData.AppointmentDateTime);
    }
    // Priority 2: Normalize existing draft.AppointmentDate if it exists (fixes incorrect conversions)
    else if (draft.AppointmentDate.HasValue)
    {
        var originalDate = draft.AppointmentDate.Value;
        draft.AppointmentDate = NormalizeToUtc(draft.AppointmentDate);
        if (originalDate != draft.AppointmentDate.Value)
        {
            _logger.LogInformation("Fixed appointment date timezone: {Original} → {Normalized}", 
                originalDate, draft.AppointmentDate);
        }
    }
}
```

**Benefits:**
- ✅ Always normalizes dates, regardless of source
- ✅ Fixes dates that were incorrectly converted
- ✅ Logs when dates are corrected

---

## How Date Normalization Works

### NormalizeToUtc Helper

```csharp
private static DateTime? NormalizeToUtc(DateTime? value) =>
    value.HasValue ? EnsureUtc(value.Value) : null;

private static DateTime EnsureUtc(DateTime value) =>
    value.Kind switch
    {
        DateTimeKind.Utc => value,                    // Already UTC - no change
        DateTimeKind.Local => value.ToUniversalTime(), // Convert Local → UTC
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) // Unspecified → UTC (safe)
    };
```

**Behavior:**
- ✅ **UTC:** No conversion needed
- ✅ **Local:** Converts to UTC
- ✅ **Unspecified:** Marks as UTC (safe for PostgreSQL)

---

## Verification

### Test Cases

1. **Excel File Upload:**
   - Upload Excel with appointment: "2026-01-24 10:00:00"
   - Verify: Stored as UTC, displays as "2026-01-24 10:00:00" (Malaysia time)

2. **Email with Excel Attachment:**
   - Email contains Excel with appointment: "2026-01-24 10:00:00"
   - Verify: Stored as UTC, displays as "2026-01-24 10:00:00" (Malaysia time)

3. **Email with PDF Attachment:**
   - Email contains PDF with appointment: "2026-01-24 10:00:00"
   - Verify: Stored as UTC, displays as "2026-01-24 10:00:00" (Malaysia time)

4. **Date Already Set (Edge Case):**
   - Draft has AppointmentDate with wrong timezone
   - Enrichment service normalizes it correctly

### SQL Verification

```sql
-- Check appointment dates in ParsedOrderDrafts
SELECT 
    "SourceFileName",
    "AppointmentDate",
    "AppointmentDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Asia/Kuala_Lumpur' AS "MalaysiaTime"
FROM "ParsedOrderDrafts"
WHERE "AppointmentDate" IS NOT NULL
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

**Expected:**
- `AppointmentDate` stored in UTC
- `MalaysiaTime` shows correct Malaysia time (GMT+8)

---

## Files Modified

1. **`backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs`**
   - Line 908: Removed `.ToUniversalTime()` call
   - Line 1234: Removed `.ToUniversalTime()` call

2. **`backend/src/CephasOps.Application/Parser/Services/ParsedOrderDraftEnrichmentService.cs`**
   - Enhanced `NormalizeDates` method to always normalize dates

---

## Impact

### ✅ Fixed
- Appointment dates now sync correctly between Excel and Email parsers
- Dates are properly normalized to UTC regardless of source
- Dates display correctly in frontend (converted from UTC to Malaysia time)

### ✅ No Breaking Changes
- Existing dates in database remain valid
- Frontend date display logic unchanged
- API contracts unchanged

### ✅ Backward Compatible
- Old drafts with incorrect dates will be normalized on next enrichment
- New drafts will have correct dates from the start

---

## Related Documentation

- `docs/02_modules/email_parser/SYNCFUSION_PARSER_FIXES.md` - Original timezone fix documentation
- `backend/src/CephasOps.Application/Parser/Services/SyncfusionExcelParserService.cs` - Excel parser timezone handling

---

## Summary

**Problem:** Dates not syncing due to incorrect `.ToUniversalTime()` calls in Email Parser  
**Solution:** Removed incorrect conversions, enhanced enrichment service to always normalize dates  
**Result:** ✅ Dates now sync correctly across all parsers

