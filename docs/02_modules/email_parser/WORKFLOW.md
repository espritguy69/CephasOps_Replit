# Email Parser Workflow

## End-to-End Email Processing Flow

This document describes the complete workflow from email arrival to order creation.

### Department Scope

The Email Parser operates **across all departments** but routes emails to department-specific workflows:

- **GPON Department**: Currently active with full email automation (TIME, Digi, Celcom partners)
- **CWO Department**: Future-ready - will use same parser engine with CWO-specific templates
- **NWO Department**: Future-ready - will use same parser engine with NWO-specific templates

Parser Templates (configured in **Settings → Email → Parser Templates**) define how incoming email content is transformed into structured JSON. Each template is associated with a specific Partner and Department, determining which workflow lifecycle is applied.

For detailed pipeline architecture, see [EMAIL_PIPELINE.md](../../01_system/EMAIL_PIPELINE.md).

---

## 1. Email Ingestion (Background Worker)

### Process:
```
Every N seconds (configurable per EmailAccount):
  ↓
Background Worker polls active mailboxes
  ↓
New emails detected → Downloaded
  ↓
EmailMessage record created in database
  ↓
Marked for parsing
```

### Components:
- **EmailIngestionService** - Background polling service
- **EmailAccount** - Mailbox configuration
- **EmailMessage** - Stored email record

### Configuration:
- Poll interval: `pollIntervalSec` (default: 60 seconds)
- Active status: `isActive` must be true
- Credentials: Stored securely in `CompanySetting`

---

## 2. Parser Template Matching

### Process:
```
Email received
  ↓
Extract: FROM address, SUBJECT, BODY preview
  ↓
Match against ParserTemplate rules:
  - FROM pattern match
  - SUBJECT pattern match
  - Partner identification
  ↓
Template matched → Use parsing rules
  ↓
No match → Use default template or manual review
```

### Matching Priority:
1. **Exact FROM match** (e.g., `noreply@time.com.my`)
2. **SUBJECT keyword match** (e.g., "FTTH", "Modification")
3. **Default template** for EmailAccount
4. **Manual review** if no template

---

## 3. Attachment Processing

### Supported Formats:
- **Excel** (.xls, .xlsx) - Primary format for activations/modifications
- **PDF** - Assurance orders, some partner formats
- **HTML/HTM** - Outlook exports

### Process:
```
Attachments detected
  ↓
For each attachment:
  - Download and store temporarily
  - Convert Excel → PDF (for snapshot)
  - Extract data using template rules
  ↓
Create ParsedOrderDraft for each order found
```

---

## 4. Data Extraction & Parsing

### Excel Parsing:
```
Excel file opened
  ↓
Apply ParserTemplate field mappings
  ↓
Extract:
  - Service ID
  - Customer name, phone, email
  - Service address
  - Appointment date/time
  - Package/bandwidth
  - ONU details
  - Technical details
  ↓
Validate required fields
  ↓
Calculate confidence score
```

### Confidence Scoring:
- **90-100%**: All required fields present
- **70-89%**: Most fields present, minor issues
- **50-69%**: Some fields missing
- **<50%**: Major parsing issues, needs review

---

## 5. Building Matching (Auto-Resolution)

Building matching is **department-agnostic** - the same matching logic is shared across all departments (GPON, CWO, NWO). The parser provides address text, and the matching service resolves it against the global buildings database.

### Process:
```
Address extracted from parsed data
  ↓
Extract building name, city, postcode
  ↓
BuildingMatchingService searches database:
  Priority 1: Building code (exact)
  Priority 2: Name + Postcode (normalized)
  Priority 3: Name + City (normalized)
  ↓
Match found?
  ├─ YES → Set BuildingId, BuildingStatus = "Existing"
  └─ NO → BuildingStatus = "New"
```

### Benefits:
- ✅ 80-90% of buildings auto-matched
- ✅ Reduces manual work
- ✅ Prevents duplicate buildings
- ✅ Faster order approval
- ✅ Shared across all departments

See: [BUILDING_MATCHING.md](./BUILDING_MATCHING.md) for detailed matching algorithm.

---

## 6. ParsedOrderDraft Creation

### Draft Record Created:
```sql
ParsedOrderDraft {
  Id: new GUID
  ParseSessionId: session ID
  CompanyId: from email account
  
  # Extracted Data
  ServiceId: from Excel
  CustomerName: from Excel
  CustomerPhone: from Excel (auto-fixed)
  AddressText: full address
  BuildingId: from matching (if found)
  BuildingName: extracted
  BuildingStatus: "Existing" or "New"
  AppointmentDate: parsed date
  
  # Metadata
  ConfidenceScore: 0.0 - 1.0
  ValidationStatus: "Pending" | "NeedsReview"
  ValidationNotes: parsing results
  SourceFileName: original file name
}
```

---

## 7. Manual Review Queue

### UI Workflow:
```
User navigates to: /orders/parser
  ↓
ParseSession list shown:
  - Completed (green)
  - Processing (blue)
  - Failed (red)
  ↓
User selects session → ParsedOrderDrafts shown
  ↓
For each draft:
  - Review parsed data
  - Check confidence score
  - Verify building match status
  ↓
User clicks "View" → Modal shows full details
  ↓
User decides:
  ├─ "Approve" → Create order
  └─ "Reject" → Mark as rejected
```

### Building Resolution:
```
If BuildingStatus = "Existing":
  → Building already matched, no modal shown
  → Order approval happens instantly
  
If BuildingStatus = "New":
  → Quick Add Building modal appears
  → User can:
    a) Select from similar buildings (if found)
    b) Create new building with pre-filled data
  → After building creation, order approval continues
```

---

## 8. Order Creation (Approval)

### Process:
```
User clicks "Approve" on ParsedOrderDraft
  ↓
Check: Building exists?
  ├─ YES → Continue
  └─ NO → Show Quick Add Building modal
  ↓
Check: Duplicate order by ServiceId?
  ├─ YES → Update existing order
  └─ NO → Create new order
  ↓
Order created with:
  - Data from ParsedOrderDraft
  - BuildingId (matched or newly created)
  - SourceEmailId (audit trail)
  - Status: "Pending" (default)
  ↓
ParsedOrderDraft updated:
  - CreatedOrderId: set
  - ValidationStatus: "Valid"
  ↓
Success! Order ready for scheduling
```

---

## 9. Error Handling

### Common Scenarios:

**Parsing Failures:**
- Missing required fields → ValidationStatus = "NeedsReview"
- Low confidence (<50%) → Manual review required
- Unsupported format → Error logged, manual entry needed

**Building Issues:**
- Building not found → Quick Add Building modal
- Duplicate building attempted → Error, select existing

**Order Creation Issues:**
- Missing OrderType → Cannot create, needs manual fix
- Invalid data → Validation error, user must correct

---

## 10. Background Job Processing

### Schedule:
- **Email polling**: Every 60 seconds (configurable)
- **Parse sessions**: Immediate after email ingestion
- **Cleanup**: Daily (remove old parse sessions/drafts)

### Monitoring:
- View background jobs: `/api/background-jobs/health`
- Check parse sessions: `/api/parser/sessions`
- Review drafts: `/api/parser/sessions/{id}/drafts`

---

## Complete Workflow Diagram

```
┌─────────────────┐
│ Email Arrives   │
└────────┬────────┘
         │
         ↓
┌─────────────────────────┐
│ Background Worker Polls │
│ (Every 60 sec)          │
└────────┬────────────────┘
         │
         ↓
┌──────────────────────┐
│ Email Downloaded     │
│ EmailMessage Created │
└────────┬─────────────┘
         │
         ↓
┌──────────────────────────┐
│ Template Matching        │
│ (FROM, SUBJECT patterns) │
└────────┬─────────────────┘
         │
         ↓
┌───────────────────────────┐
│ Attachment Parsing        │
│ (Excel → Data extraction) │
└────────┬──────────────────┘
         │
         ↓
┌──────────────────────────┐
│ Building Auto-Matching   │
│ (80-90% success rate)    │
└────────┬─────────────────┘
         │
         ↓
┌──────────────────────────┐
│ ParsedOrderDraft Created │
│ Status: Pending/Review   │
└────────┬─────────────────┘
         │
         ↓
┌──────────────────┐
│ Manual Review UI │
│ /orders/parser   │
└────────┬─────────┘
         │
         ↓
┌─────────────────┐
│ User: "Approve" │
└────────┬────────┘
         │
         ├─ Building Exists? ─→ YES ──┐
         │                             │
         └─ NO ───→ Quick Add Building │
                                       │
                                       ↓
                              ┌──────────────┐
                              │ Order Created│
                              │ Status: Pending│
                              └──────────────┘
```

---

## Related Documentation

- [OVERVIEW.md](./OVERVIEW.md) - Email Parser overview
- [SETUP.md](./SETUP.md) - Configuration and setup guide
- [SPECIFICATION.md](./SPECIFICATION.md) - Detailed parsing rules
- [BUILDING_MATCHING.md](./BUILDING_MATCHING.md) - Building resolution logic

---

## Status

✅ **Production Implementation**
- Email ingestion: Active
- Excel parsing: Working
- Building matching: Active (90% auto-match rate)
- Manual review: Functional
- Order creation: Working
- Deduplication: Enabled

