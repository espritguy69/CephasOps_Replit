# Duplicate Detection Improvements

**Date:** 2025-12-12  
**Status:** ✅ Implemented

---

## Overview

This document describes the improvements made to prevent duplicate order creation when processing emails and Excel attachments.

---

## Problems Addressed

### 1. **File-Level Duplicates**
**Issue:** The same Excel file could be processed multiple times if it arrived in different emails (different MessageIds), leading to duplicate orders.

**Solution:** Added file content hashing (SHA256) to detect duplicate files across different emails.

### 2. **ServiceId Case/Whitespace Variations**
**Issue:** Orders with the same ServiceId but different case or whitespace (e.g., "TBBN123" vs "tbbn123" vs " TBBN123 ") would create duplicates.

**Solution:** Normalize ServiceId (trim + uppercase) before duplicate checks.

### 3. **Insufficient Logging**
**Issue:** Difficult to track when and why duplicates were detected.

**Solution:** Added comprehensive logging for all duplicate detection scenarios.

### 4. **Parser Playground Cache Issues**
**Issue:** Build cache prevented testing different files.

**Solution:** Improved file path resolution with multiple fallback paths.

---

## Implementation Details

### 1. File Hash Storage

**Entity Change:** Added `FileHash` property to `ParsedOrderDraft`

```csharp
/// <summary>
/// SHA256 hash of the source file content (for duplicate detection)
/// </summary>
public string? FileHash { get; set; }
```

**Database Migration Required:**
```sql
ALTER TABLE parsed_order_drafts 
ADD COLUMN file_hash VARCHAR(64) NULL;

CREATE INDEX idx_parsed_order_drafts_file_hash 
ON parsed_order_drafts(company_id, file_hash) 
WHERE file_hash IS NOT NULL;
```

### 2. File Hash Computation

**Location:** `EmailIngestionService.ComputeFileHashAsync()`

- Computes SHA256 hash of file content
- Returns lowercase hex string (64 characters)
- Used before processing any Excel attachment

### 3. Duplicate Detection Logic

**Location:** `EmailIngestionService.ProcessEmailAsync()` - Attachment processing loop

**Process:**
1. Compute file hash for each attachment
2. Check if draft with same hash exists (last 30 days)
3. If duplicate found:
   - Log warning with details
   - Create placeholder draft with "Rejected" status
   - Skip processing to prevent duplicate order
4. If not duplicate:
   - Process normally and store hash

**Example Log:**
```
⚠️ DUPLICATE FILE DETECTED: File M1805811.xls (Hash: abc123...) was already processed in draft {DraftId} on 2025-12-10 14:30:00. 
Skipping to prevent duplicate order creation. Original draft ServiceId: TBBN620278G
```

### 4. ServiceId Normalization

**Location:** `ParserService.ApproveParsedOrderAsync()`

**Process:**
1. Normalize incoming ServiceId: `Trim().ToUpperInvariant()`
2. Fetch candidate orders from database
3. Normalize database ServiceIds in memory
4. Match using normalized values
5. Fallback to exact match if normalization finds nothing

**Benefits:**
- Prevents duplicates from case differences
- Prevents duplicates from whitespace differences
- Backward compatible (exact match fallback)

**Example:**
- Input: `" TBBN123 "` → Normalized: `"TBBN123"`
- Database: `"tbbn123"` → Normalized: `"TBBN123"`
- Match: ✅ Found

### 5. Enhanced Logging

**Duplicate Detection Logs:**
- File hash computation
- Duplicate file detection (with original draft details)
- ServiceId normalization (original vs normalized)
- Existing order found (with order details)
- No existing order found

**Log Levels:**
- `Information`: Normal operations, duplicate found
- `Warning`: Duplicate file detected, skipped
- `Debug`: Detailed matching process

### 6. UX duplicate warning (Feb 2026)

**Purpose:** Warn the user *before* approving or reviewing a draft when its Service ID already exists as an order, so they can choose to continue (and update that order) or cancel.

**Backend:**
- **Endpoint:** `GET /api/parser/drafts/check-duplicate?serviceId=XXX`
- **Returns:** `OrderExistsByServiceIdDto`: `{ exists, orderId?, serviceId?, ticketId? }`
- **Logic:** Same normalization as `ApproveParsedOrderAsync` (trim + uppercase); query Orders by company and normalized ServiceId; return first match if any.

**Frontend (Parser Listing):**
- **Review & create order:** When the user clicks "Review & create order" for a draft that has a Service ID, the app calls the check endpoint. If an order exists, a modal is shown: "An order with Service ID X already exists (Ticket: Y). Proceeding may update that order. Continue?" with optional "View existing order" link. User can Cancel or Continue to review.
- **Bulk approve:** Before calling bulk approve, the app checks each selected draft that has a Service ID. If any have an existing order, a confirm dialog is shown: "X of the selected draft(s) have Service IDs that already exist as orders. Proceeding may update those orders. Continue?" User can cancel or proceed.

---

## Database Migration

### Step 1: Add FileHash Column

```sql
-- Add file_hash column to parsed_order_drafts table
ALTER TABLE parsed_order_drafts 
ADD COLUMN file_hash VARCHAR(64) NULL;

-- Add index for efficient duplicate lookups
CREATE INDEX idx_parsed_order_drafts_file_hash 
ON parsed_order_drafts(company_id, file_hash) 
WHERE file_hash IS NOT NULL;

-- Add comment
COMMENT ON COLUMN parsed_order_drafts.file_hash IS 
'SHA256 hash of source file content for duplicate detection';
```

### Step 2: Create Migration (EF Core)

```powershell
cd backend/src/CephasOps.Infrastructure
dotnet ef migrations add AddFileHashToParsedOrderDraft --startup-project ../CephasOps.Api --context ApplicationDbContext
```

### Step 3: Apply Migration

```powershell
dotnet ef database update --startup-project ../CephasOps.Api --context ApplicationDbContext
```

---

## Testing

### Test Case 1: Duplicate File Detection

**Scenario:**
1. Email 1 arrives with `M1805811.xls` → Processed, draft created
2. Email 2 arrives with same `M1805811.xls` → Should be detected as duplicate

**Expected:**
- First email: Draft created with `FileHash` set
- Second email: Placeholder draft created with `ValidationStatus = "Rejected"`
- Log shows duplicate detection warning
- No duplicate order created

### Test Case 2: ServiceId Normalization

**Scenario:**
1. Order exists with ServiceId: `"TBBN123"`
2. New draft arrives with ServiceId: `" tbbn123 "`

**Expected:**
- Normalization: `"TBBN123"` (both)
- Match found: Existing order updated
- Log shows normalization details

### Test Case 3: Case Variations

**Scenario:**
1. Order exists with ServiceId: `"TBBN123"`
2. New draft arrives with ServiceId: `"tbbn123"`

**Expected:**
- Normalization: `"TBBN123"` (both)
- Match found: Existing order updated

---

## Configuration

### Duplicate Detection Window

**Current:** 30 days  
**Location:** `EmailIngestionService.ProcessEmailAsync()`

```csharp
.Where(d => d.CreatedAt >= DateTime.UtcNow.AddDays(-30))
```

**To Change:** Modify the `AddDays(-30)` value.

**Recommendation:** 
- 30 days for active processing
- 90 days for compliance/audit
- Consider making this configurable per company

---

## Performance Considerations

### File Hash Computation

- **Cost:** Minimal (SHA256 is fast)
- **Impact:** ~1-5ms per file
- **Benefit:** Prevents expensive duplicate order processing

### ServiceId Normalization Query

- **Current:** Fetches all orders with ServiceId, normalizes in memory
- **Optimization:** Consider adding computed column or index on normalized ServiceId
- **Impact:** Acceptable for current scale (< 1000 orders per company)

**Future Optimization:**
```sql
-- Add computed column for normalized ServiceId
ALTER TABLE orders 
ADD COLUMN service_id_normalized VARCHAR(50) 
GENERATED ALWAYS AS (UPPER(TRIM(service_id))) STORED;

CREATE INDEX idx_orders_service_id_normalized 
ON orders(company_id, service_id_normalized);
```

---

## Monitoring

### Key Metrics to Track

1. **Duplicate File Detection Rate**
   - Count of duplicate files detected per day
   - Most common duplicate files
   - Source email patterns

2. **ServiceId Normalization Matches**
   - Count of matches found via normalization vs exact match
   - Case/whitespace variation patterns

3. **False Positives**
   - Duplicate detections that were actually different files
   - ServiceId matches that were incorrect

### Log Queries

```sql
-- Count duplicate files detected today
SELECT COUNT(*) 
FROM parsed_order_drafts 
WHERE validation_status = 'Rejected' 
  AND validation_notes LIKE 'Duplicate file detected%'
  AND created_at >= CURRENT_DATE;

-- Find most common duplicate files
SELECT file_hash, source_file_name, COUNT(*) as duplicate_count
FROM parsed_order_drafts 
WHERE file_hash IS NOT NULL
GROUP BY file_hash, source_file_name
HAVING COUNT(*) > 1
ORDER BY duplicate_count DESC;
```

---

## Rollback Plan

If issues arise, rollback steps:

1. **Remove FileHash Column:**
```sql
DROP INDEX IF EXISTS idx_parsed_order_drafts_file_hash;
ALTER TABLE parsed_order_drafts DROP COLUMN IF EXISTS file_hash;
```

2. **Revert Code Changes:**
- Remove `FileHash` property from entity
- Remove hash computation logic
- Remove duplicate detection checks
- Revert ServiceId normalization (keep exact match only)

3. **Data Impact:**
- No data loss (FileHash is nullable)
- Existing drafts unaffected
- Orders already created remain valid

---

## Future Enhancements

### 1. Configurable Detection Window
- Make duplicate detection window configurable per company
- Allow different windows for different file types

### 2. Fuzzy Matching
- Detect similar files (not just exact duplicates)
- Use similarity threshold (e.g., 95% match)
- Flag for manual review

### 3. Duplicate Dashboard
- UI to view detected duplicates
- Manual merge/approve options
- Statistics and trends

### 4. ServiceId Index Optimization
- Add computed column for normalized ServiceId
- Improve query performance for large datasets

---

## Related Documentation

- [Email Parser Workflow](./WORKFLOW.md)
- [Email Parser Specification](./SPECIFICATION.md)
- [Order Creation Testing](../../06_ai/EMAIL_PARSER_ORDER_CREATION_TESTING.md)

---

## Changelog

**2025-12-12:**
- ✅ Added FileHash property to ParsedOrderDraft entity
- ✅ Implemented file hash computation (SHA256)
- ✅ Added duplicate file detection logic
- ✅ Improved ServiceId normalization
- ✅ Enhanced duplicate detection logging
- ✅ Fixed parser playground cache issues

