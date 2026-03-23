# AWO Number Extraction

## Overview

AWO Number (Assurance Work Order Number) is a critical identifier for Assurance orders in CephasOps. This document explains how AWO Numbers are extracted, stored, and used throughout the parsing and order creation workflow.

---

## 1. What is AWO Number?

AWO Number is a unique identifier assigned to Assurance work orders. It is:
- **Required** for Assurance orders
- Used to track and reference assurance cases
- Typically provided by TIME in emails or PDF attachments
- Stored separately from Ticket ID (TTKT) for clarity

**Example AWO Numbers:**
- `437884`
- `123456`
- `987654`

---

## 2. Extraction Methods

The parser uses multiple strategies to extract AWO Number, depending on the source format:

### 2.1 From Filename (PDF Attachments)

**Pattern:** Filenames containing `AWO####` pattern

**Examples:**
- `APPMT...__AWO437884_.pdf`
- `AWO123456_20251118.pdf`
- `WorkOrder_AWO987654.pdf`

**Extraction Logic:**
```regex
AWO(\d+)
```

**Priority:** **Highest** - Filename extraction takes precedence over text extraction

### 2.2 From Email Body / PDF Text

**Patterns (in order of priority):**
1. `AWO[:\s<>]*(\d+)` - Matches "AWO: 437884", "AWO 437884", "AWO<437884>"
2. `AWO\s*Number[:\s]+(\d+)` - Matches "AWO Number: 437884", "AWO Number 437884"
3. `Work\s*Order\s*No[:\s]+(\d+)` - Matches "Work Order No: 437884"

**Examples in Text:**
```
AWO: 437884
AWO Number: 123456
AWO Number 987654
Work Order No: 437884
```

**Extraction Logic:**
- Case-insensitive matching
- Handles variations in spacing and punctuation
- Extracts numeric portion only (digits)

### 2.3 From Excel Files

**For Assurance Orders in Excel:**

The Excel parser looks for AWO Number in label-value pairs:

**Supported Labels (normalized matching):**
- `AWO NUMBER`
- `AWO NUMBER:`
- `AWO NO`
- `AWO NO.`
- `AWO`
- `AWO:`
- `AWO Number`
- `AWO Number:`

**Extraction Method:**
- Uses `ExtractRightSideValueByLabels` helper method
- Column-agnostic (works regardless of Excel column position)
- Case-insensitive label matching
- Returns numeric value from adjacent cell

---

## 3. Data Flow

### 3.1 Parser Pipeline

```
Source (Email/PDF/Excel)
  ↓
Extract AWO Number (multiple strategies)
  ↓
Store in ParsedOrderData.AwoNumber
  ↓
Map to ParsedOrderDraft.AwoNumber
  ↓
Store in Database (ParsedOrderDrafts.AwoNumber)
  ↓
Display in Parser Review UI
  ↓
Map to Order Form (CreateOrderPage.awoNumber)
  ↓
Create Order with AWO Number
```

### 3.2 ParsedOrderData → ParsedOrderDraft Mapping

**Backend Services:**
- `ParserService` - Maps `data.AwoNumber → draft.AwoNumber` for file uploads
- `EmailIngestionService` - Maps `data.AwoNumber → draft.AwoNumber` for:
  - Excel attachments
  - PDF attachments  
  - Email body parsing (for Assurance emails without attachments)

### 3.3 ParsedOrderDraft → Order Form Mapping

**Frontend (CreateOrderPage):**
```typescript
if (draft.awoNumber) {
  setValue('awoNumber', draft.awoNumber);
}
```

---

## 4. Database Schema

### 4.1 ParsedOrderDrafts Table

**Column:** `AwoNumber`

**Type:** `character varying(100)`

**Constraints:**
- **Nullable:** Yes (optional for non-Assurance orders)
- **Max Length:** 100 characters

**Migration:** `20251218055129_AddAwoNumberToParsedOrderDraft`

### 4.2 Entity Configuration

**EF Core Configuration:**
```csharp
builder.Property(pod => pod.AwoNumber)
    .HasMaxLength(100);
```

**Domain Entity:**
```csharp
/// <summary>
/// AWO Number - required for Assurance orders
/// </summary>
public string? AwoNumber { get; set; }
```

---

## 5. Order Type Specific Behavior

### 5.1 Assurance Orders

**Required:** Yes

**Validation:**
- Frontend validation requires AWO Number for Assurance order types
- Validated in `CreateOrderPage` form schema

**Usage:**
- Stored in Order entity when Assurance order is created
- Used for tracking and reference

### 5.2 Other Order Types

**Required:** No

**Behavior:**
- AWO Number field is optional/nullable
- Not extracted for non-Assurance orders
- Can be left empty

---

## 6. Implementation Details

### 6.1 Excel Parser (SyncfusionExcelParserService)

**Method:** `ParseFromDataTable` (ExcelDataReader path)
```csharp
data.AwoNumber = ExtractRightSideValueByLabels(dataTable, 
    "AWO NUMBER", "AWO NUMBER:", "AWO NO", "AWO NO.", 
    "AWO", "AWO:", "AWO Number", "AWO Number:");
```

**Method:** `ParseActivation` (Syncfusion path)
```csharp
data.AwoNumber = ExtractRightSideValueByLabels(worksheet, 
    "AWO NUMBER", "AWO NUMBER:", "AWO NO", "AWO NO.", 
    "AWO", "AWO:", "AWO Number", "AWO Number:")
    ?? ExtractByLabel(worksheet, "AwoNumber");
```

### 6.2 PDF Parser (PdfOrderParserService)

**Method:** `ExtractAwoNumber`
```csharp
// 1. Try filename first (e.g., APPMT...__AWO437884_.pdf)
var fileNameMatch = Regex.Match(fileName, @"AWO(\d+)", RegexOptions.IgnoreCase);

// 2. Try text patterns
var patterns = new[]
{
    @"AWO[:\s<>]*(\d+)",
    @"AWO\s*Number[:\s]+(\d+)",
    @"Work\s*Order\s*No[:\s]+(\d+)"
};
```

**Storage:**
```csharp
data.AwoNumber = awoNumber; // Store in property, not just remarks
```

### 6.3 Email Body Parsing (EmailIngestionService)

**For Assurance emails without attachments:**
- Uses `PdfOrderParserService.ParseFromText` to extract from email body
- Merges with PDF attachment data if both present:
  ```csharp
  if (string.IsNullOrEmpty(attachmentDraft.AwoNumber) && 
      !string.IsNullOrEmpty(bodyParsedData.AwoNumber))
  {
      attachmentDraft.AwoNumber = bodyParsedData.AwoNumber;
  }
  ```

---

## 7. Confidence Scoring

**Impact on Confidence:**
- AWO Number extraction does not directly affect confidence score
- Missing AWO Number for Assurance orders may trigger validation warnings
- Confidence is primarily based on core fields (Service ID, Customer Name, Address, Appointment Date)

---

## 8. Validation Rules

### 8.1 Frontend Validation

**CreateOrderPage Form Schema:**
```typescript
awoNumber: z.string().optional(), // Optional in base schema

// Custom validation for Assurance orders
if (isAssuranceOrder(data.orderType)) {
  if (!data.awoNumber?.trim()) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ['awoNumber'],
      message: 'AWO Number is required for Assurance orders',
    });
  }
}
```

### 8.2 Backend Validation

- AWO Number is stored as-is (no normalization)
- No specific backend validation rules (handled by frontend)
- Optional field in database schema

---

## 9. Testing & Verification

### 9.1 Test Cases

**Excel Files:**
- Test with AWO Number in various label formats
- Test with AWO Number in different columns
- Test without AWO Number (should be null)

**PDF Files:**
- Test with AWO Number in filename
- Test with AWO Number in PDF text
- Test without AWO Number

**Email Body:**
- Test with AWO Number in email subject
- Test with AWO Number in email body
- Test with AWO Number in both PDF and email body (merge logic)

### 9.2 Verification Steps

1. **Parse Assurance email/PDF/Excel**
2. **Check ParsedOrderDraft in database:**
   ```sql
   SELECT "AwoNumber", "OrderTypeCode" 
   FROM "ParsedOrderDrafts" 
   WHERE "OrderTypeCode" LIKE '%ASSURANCE%';
   ```
3. **Verify in Parser Review UI:**
   - AWO Number should be visible in draft details
4. **Verify in CreateOrderPage:**
   - AWO Number should populate in form when creating order from draft
   - Validation should require AWO Number for Assurance orders

---

## 10. Troubleshooting

### 10.1 AWO Number Not Extracted

**Possible Causes:**
- Filename doesn't match `AWO####` pattern
- Text format doesn't match extraction patterns
- Excel label doesn't match supported variations
- AWO Number in unexpected location

**Solutions:**
- Verify filename/text format matches extraction patterns
- Check parser logs for extraction attempts
- Manually review and update in Parser Review UI
- Consider adding additional extraction patterns if needed

### 10.2 AWO Number Extracted Incorrectly

**Possible Causes:**
- Pattern matching false positives
- Multiple AWO Numbers in source (first match wins)
- Incorrect text extraction from PDF

**Solutions:**
- Review source file/email for multiple AWO Number references
- Check PDF text extraction quality
- Manually correct in Parser Review UI before approval

---

## 11. Related Documentation

- [Parser Specification](./SPECIFICATION.md) - Overall parser behavior
- [Assurance Orders](./SPECIFICATION.md#35-time-assurance-ttkt) - Assurance order handling
- [Parser Entities](../../05_data_model/entities/parser_entities.md) - Database schema
- [CreateOrderPage](../../07_frontend/ORDER_CREATION_PAGE_FLOW.md) - Order creation workflow

---

## 12. Changelog

**2025-01-14: Initial Implementation**
- Added AWO Number extraction to Excel parser (both ExcelDataReader and Syncfusion paths)
- Added AWO Number extraction to PDF parser
- Added AWO Number property to ParsedOrderData, ParsedOrderDraft, and DTOs
- Added database migration for AwoNumber column
- Updated frontend interfaces and CreateOrderPage mapping
- Created comprehensive documentation

---

**Last Updated:** 2025-01-14  
**Maintained By:** Development Team

