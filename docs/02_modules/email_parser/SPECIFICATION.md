# EMAIL_PARSER.md — Full Production Specification

CephasOps Email Parser is the engine responsible for converting ANY incoming partner email—Excel, PDF, HTML, or human-written—into a valid, structured CephasOps Order.

This is the largest and most detailed document because the parser is the brain of CephasOps. This specification covers:

- TIME FTTH/FTTO parsing
- TIME–Digi HSBB parsing
- TIME–Celcom HSBB parsing
- Modification Indoor/Outdoor logic
- Assurance (TTKT/LOSi/LOBi) extraction (including AWO Number extraction - see [AWO_NUMBER_EXTRACTION.md](./AWO_NUMBER_EXTRACTION.md))
- Human email reschedule approvals
- Multi-partner templates
- Required fields mapping
- Data normalization rules
- Date/time NLP engine
- Contact auto-fixing
- Partner identification logic
- Multiple Excel formats handling
- Duplicate detection logic
- Parser → Order mapping flow

---

**Related Documentation:**

- [Email Pipeline Architecture](../../01_system/EMAIL_PIPELINE.md) - Overall pipeline design and flow
- [Workflow Engine](../../01_system/WORKFLOW_ENGINE.md) - How parsed events trigger workflows
- [OVERVIEW.md](./OVERVIEW.md) - Parser module overview
- [AWO_NUMBER_EXTRACTION.md](./AWO_NUMBER_EXTRACTION.md) - Detailed AWO Number extraction documentation

---

It supports TIME, Digi, Celcom, U-Mobile, FTTH, FTTO, FTTR, SDU, Assurance, Modifications (Indoor/Outdoor), and human reschedule approvals.

This document defines exactly how the parser must behave.

---

# 1. Email Sources (Supported Types)

The parser must support all of the following:

### 1.1 Structured Emails With Attachments  
- **TIME FTTH Activation**
- **TIME FTTO Activation**
- **TIME Modification (Indoor/Outdoor)**
- **TIME–Digi HSBB Activation**
- **TIME–Celcom HSBB Activation**

Attachments may be:
- `.xls`
- `.xlsx`
- `.pdf`
- `.htm` exported from Outlook

### 1.2 Semi-Structured Emails Without Attachments  
- **TIME Assurance** (TTKT)
- **TIME AWO**
- **FTTH/FTTO Reschedule Approvals**

### 1.3 Unstructured Human Replies  
Examples:
- “Approved, please proceed”
- “Ok noted, rescheduled to 25/11 at 10am”
- “Appointment changed, new slot is tomorrow 2pm”

These DO NOT have attachments but MUST update existing orders.

### 1.4. Department / Vertical Scope

The Email Parser itself is department-agnostic. It focuses on:

- Identifying the partner (TIME, Digi, Celcom, etc.)
- Applying the correct ParserTemplate
- Producing structured JSON for Order creation or update

Department (GPON, CWO, NWO, etc.) is resolved by:

- The Partner configuration (Partner → Department)
- Or explicit EmailRule routing when needed

In the current phase, all parsed emails are routed to the GPON department. Future departments (CWO, NWO) can reuse the same parser logic with different ParserTemplates and EmailRules.

---

# 2. PARTNER IDENTIFICATION LOGIC

Partner type is detected using **FROM**, **SUBJECT**, and **BODY** fields in that order.

---

## 2.1 TIME FTTH / FTTO
**FROM:**
- `noreply@time.com.my`
- `no-reply@time.com.my`
- `activation@time.com.my`

**SUBJECT contains:**
- “FTTH”
- “FTTO”
- “New Activation”
- “Installation”

**Attachment:**
- Excel with Time Standard Format

---

## 2.2 TIME–Digi HSBB
**FROM:**  
- `hsbb@digi.com.my`  
- `noreply@time.com.my` (forwarded)  

**SUBJECT contains:**  
- `TIME-Digi HSBB`  
- `DIGI00`  

**Attachment:** Excel in Digi layout.

---

## 2.3 TIME–Celcom HSBB
**FROM:**  
- `celcomhsbb@time.com.my`

**SUBJECT contains:**  
- `TIME-Celcom HSBB`  
- `CELCOM00`  

**Attachment:** Excel in Celcom layout.

---

## 2.4 TIME Modification – Indoor/Outdoor
**SUBJECT contains:**  
- `Modification`  
- `Indoor relocation`  
- `Outdoor relocation`  
- `Relocation FTTH`  

### Outdoor detection:
- Two addresses present
- Excel includes "Old Address" and "New Address"

### Indoor detection:
- One address but includes:
  - “Move within unit”
  - “Relocate inside”
  - “From room to room”
  - “Relocate to kitchen/hall/bedroom”

---

## 2.5 TIME Assurance (TTKT)
**BODY contains:**
- `TTKT`
- `Service ID`
- `Issue: LOSi/LOBi`
- `Work Order URL`
- `Appointment Date`
- `Appointment Time`

---

## 2.6 Human-Reply Reschedule Approvals
**FROM:**  
- Usually TIME email addresses **OR** building management forwarding TIME.

**SUBJECT:**  
Same as original thread  
OR  
Contains “Reschedule” or “Approved”.

**BODY signals include:**
- “Approved“
- “Ok proceed”
- “New appointment is”
- “Slot confirmed”
- “We have rescheduled to”
- “Time changed to”

These MUST activate the reschedule engine.

---

# 3. FIELD EXTRACTION RULES BY PARTNER

Each partner/emails type has specific column patterns.

---

# 3.1 TIME FTTH / FTTO Excel Field Map

| Excel Field | JSON Path |
|-------------|-----------|
| Customer Name | customer.name |
| Contact No. | customer.contactNo |
| Service ID | uniqueId (serviceId) |
| Full Address | address.fullAddress |
| Appointment Date | appointment.date |
| Appointment Time | appointment.time |
| Building Name | address.buildingName |
| Order Type | partnerOrderType |
| ONU Username | network.username |
| ONU Password | network.password |
| ONU Model | network.onuModel |
| PACKAGE / BANDWIDTH | networkInfo.package (split) + networkInfo.bandwidth (split) |
| LOGIN ID | networkInfo.loginId |
| PASSWORD | networkInfo.password |
| WAN IP | networkInfo.wanIp |
| LAN IP | networkInfo.lanIp |
| Gateway | networkInfo.gateway |
| Subnet Mask | networkInfo.subnetMask |
| SERVICE ID. / PASSWORD (VOIP) | voip.serviceId (split) + voip.password (split) |
| IP ADDRESS ONU (VOIP) | voip.ipAddressOnu |
| GATEWAY ONU (VOIP) | voip.gatewayOnu |
| SUBNET MASK ONU (VOIP) | voip.subnetMaskOnu |
| IP ADDRESS SRP (VOIP) | voip.ipAddressSrp |
| Remarks (VOIP) | voip.remarks |

---

# 3.2 TIME–Digi HSBB Excel Field Map

| Excel Field | JSON Path |
|-------------|-----------|
| Digi Order ID (DIGI00xxxx) | uniqueId |
| Customer Name | customer.name |
| Installation Address | address.fullAddress |
| Contact | customer.contactNo |
| Preferred Date | appointment.date |
| Preferred Slot | appointment.time |
| Bandwidth | broadband.bandwidth |
| ONU Username | network.username |
| Password | network.password |

---

# 3.3 TIME–Celcom HSBB Field Map

Same layout as Digi except:

**Partner Order ID:** `CELCOM00xxxxx`  
**Contact sometimes inside body, not Excel.**

---

# 3.4 TIME Modification – Outdoor Field Map

Outdoor has:
- Old Address
- New Address

Mapping:

```json
"relocation": {
  "type": "Outdoor",
  "oldAddress": { ... },
  "newAddress": { ... }
}
```

---

### 3.4.1 TIME Modification Outdoor – Database-First Strategy

**Strategy Overview:**
For Modification Outdoor orders, the parser uses a database-first approach to leverage existing order data and reduce parsing errors.

**Decision Rules (January 2025):**

1. **Service ID Matching:**
   - **Rule:** Exact match only (case-sensitive, no normalization)
   - **Rationale:** Standard across all systems, single source of truth
   - **Implementation:** Query database using exact Service ID (TBBN format) from Excel

2. **Multiple Orders with Same Service ID:**
   - **Rule:** Use the activation order details (first order created for that Service ID)
   - **Rationale:** Activation order contains the original customer setup information
   - **Implementation:** Query orders by Service ID, order by `CreatedAt ASC`, take first result

3. **Old Address Handling:**
   - **Rule:** Always pull Old Address from database first
   - **Override:** Allow Excel override if Excel Old Address differs from database
   - **Rationale:** Database is source of truth, but Excel may have corrections
   - **Implementation:**
     - If existing order found → use `existingOrder.ServiceAddress` as Old Address
     - Extract Old Address from Excel
     - If Excel Old Address exists and differs → use Excel value (log override)

4. **Data Source Priority:**
   - **Rule:** Excel is the main source for new/changed fields
   - **Rationale:** Excel contains the latest information from TIME
   - **Implementation:**
     - **From Database (if existing order found):**
       - Customer Name
       - Customer Phone
       - Customer Email
       - Old Address (with Excel override allowed)
       - Original ONU credentials (for reference)
     - **From Excel (always extracted):**
       - Service ID (TBBN) — used for lookup
       - New Address (Service Address) — where customer is moving to
       - New ONU Password — provided by TIME for new location
       - Remarks — special instructions from TIME
       - Appointment Date/Time — new appointment details

**Parsing Flow:**

```
Step 1: Extract Service ID from Excel (quick extraction)
   ↓
Step 2: Query database for existing order by Service ID (exact match)
   ↓
Step 3a: If existing order found:
   - Load customer info from database
   - Load old address from database
   - Extract NEW fields from Excel:
     * New Address (Service Address)
     * New ONU Password
     * Remarks
     * Appointment details
   - Materials: Usually empty (customer brings device)
   
Step 3b: If no existing order found:
   - Full Excel parsing (fallback to activation-style parsing)
   - Extract all fields from Excel
```

**Key Fields for Modification Outdoor:**

| Field | Source | Priority | Notes |
|-------|--------|----------|-------|
| Service ID (TBBN) | Excel | Required | Used for database lookup |
| Customer Name | Database → Excel | Database first | Excel only if no DB match |
| Customer Phone | Database → Excel | Database first | Excel only if no DB match |
| Old Address | Database → Excel | Database first | Excel can override if different |
| New Address | Excel | Required | Always from Excel (where they're moving to) |
| New ONU Password | Excel | Required | Provided by TIME for new location |
| Remarks | Excel | Optional | Special instructions from TIME |
| Materials | Excel | Usually empty | Customer brings device (IsRequired = false) |

**Materials Handling:**
- **Default Behavior:** Materials list is usually empty for modification outdoor
- **Reason:** Customer brings their own device to the new site
- **Exception:** Only mark materials as required if Excel explicitly shows YES (rare case where customer lost device)
- **Implementation:** Parse materials from Excel but default `IsRequired = false` unless Excel explicitly indicates YES

**Applicable Order Types:**
This database-first strategy applies to:
- ✅ **Modification Outdoor** — Primary use case
- ✅ **TIME Assurance** — Existing service, extract issue details
- ✅ **Value Added Service** — Existing customer, extract new service details
- ✅ **Repeating Orders** — Same customer, extract new order details

**Error Handling:**
- If Service ID extraction fails → Full Excel parsing (fallback)
- If database lookup fails → Full Excel parsing (fallback)
- If Excel New Address missing → Log warning, use database address if available
- If Excel New ONU Password missing → Log error, order may need manual review

---

### 3.4.2 Service ID Detection and Matching Rules

**Two-Phase Approach:**

The Service ID handling has two distinct phases with different matching rules:

#### **Phase 1: Service ID Header Detection (Label Finding)**

**Rule:** Normalized match (flexible)

**Normalization Steps:**
- Convert to lowercase for comparison
- Trim leading/trailing whitespace
- Collapse multiple spaces into single space
- Convert hyphens (`-`) and underscores (`_`) to spaces

**Purpose:** To reliably detect the "Service ID" label in inconsistent Excel templates where the label may appear as:
- "Service ID"
- "Service-ID"
- "SERVICE ID"
- "Service  ID" (multiple spaces)
- "Service_ID"
- "service id"

**Implementation:**
```csharp
// Example: Finding the label
var normalizedLabel = "Service ID".ToLowerInvariant()
    .Replace("-", " ")
    .Replace("_", " ")
    .Trim();
// Then compare with normalized cell values
```

#### **Phase 2: Service ID Value Matching (Database Lookup)**

**Rule:** Exact match only (strict, case-sensitive, no normalization)

**Requirements:**
- Use literal text exactly as found in Excel
- No case conversion
- No whitespace normalization
- No character replacement
- Query database with the unmodified string
- If not found → return error (do not proceed with partial data)

**Purpose:** Service ID (TBBN format, e.g., "TBBN620278G") acts as a primary key for database lookup. Accuracy must be 100% to ensure:
- Correct order identification
- Data integrity
- No false matches

**Implementation:**
```csharp
// Example: Using the extracted value
var serviceIdValue = extractedValue; // Use exactly as extracted, no normalization
var existingOrder = await _context.Orders
    .Where(o => o.ServiceId == serviceIdValue) // Exact match, case-sensitive
    .FirstOrDefaultAsync();
    
if (existingOrder == null)
{
    // Log error: Service ID not found in database
    // Option: Fall back to full Excel parsing OR return error
}
```

**Key Distinction:**

| Phase | Purpose | Matching Rule | Example |
|-------|---------|----------------|---------|
| **Header Detection** | Find the "Service ID" label in Excel | Normalized (flexible) | "Service ID", "SERVICE-ID", "Service  ID" → all match |
| **Value Matching** | Use TBBN value for database lookup | Exact (strict) | "TBBN620278G" must match exactly "TBBN620278G" |

**Error Handling:**

- **Header Detection Failure:**
  - If "Service ID" label not found → Log warning, attempt alternative labels ("ServiceID", "SERVICE ID", etc.)
  - If still not found → Fall back to full Excel parsing

- **Value Matching Failure:**
  - If Service ID extracted but not found in database → Log error
  - Options:
    1. Return error and require manual review
    2. Fall back to full Excel parsing (treat as new customer)
  - Decision: Based on business rules (new customer vs. data error)

**Examples:**

**Example 1: Flexible Header Detection**
```
Excel cell: "Service  ID" (with extra space)
Normalized: "service id"
Match: ✅ Found (normalized comparison)
Extracted value: "TBBN620278G"
```

**Example 2: Strict Value Matching**
```
Extracted value: "TBBN620278G"
Database query: WHERE ServiceId = 'TBBN620278G' (exact match)
Result: ✅ Found existing order
```

**Example 3: Case Sensitivity in Value**
```
Extracted value: "tbbn620278g" (lowercase)
Database query: WHERE ServiceId = 'tbbn620278g'
Database value: "TBBN620278G" (uppercase)
Result: ❌ Not found (case-sensitive match required)
```

**Note:** If Excel contains lowercase Service ID but database has uppercase, this is a data quality issue that should be logged and reviewed, not automatically normalized.

---

3.5 TIME Modification – Indoor Field Map

One address, but with notes:

"relocation": {
  "type": "Indoor",
  "oldLocationNote": "Bedroom 2",
  "newLocationNote": "Living Hall"
}


Parsed from:

“move to bedroom”

“relocate to hall”

“shift to kitchen”

3.6 TIME Assurance Email (Plain Text)

Parser extracts:

Example Block:

Customer Details
Customer Name: KUAN TE SIANG
Service ID: TBBN620278G
TTID: TTKT202511178606510
Contact No: 0166587158 (Peggy)
Address: Block B, Level 33A, Unit 20, UNITED POINT...

Issues: Link Down LOSi/LOBi
Appointment: 29 Nov 2025 11:00 AM
Work Order URL: https://iptv.time.com.my/...

Extraction logic:

Service ID: → uniqueId

TTID: → assurance.ticketId

AWO Number: → assurance.awoNumber (extracted from filename pattern `AWO####` or text patterns like "AWO: 437884", "AWO Number: 437884")

Contact No: → customer.contactNo

Issues: → assurance.issueCategory

Appointment: → appointment.date/time

URL: → workOrderUrl

**AWO Number Extraction:**
- Primary: Extracted from filename pattern `AWO####` (e.g., `APPMT...__AWO437884_.pdf`)
- Secondary: Extracted from email body/text using patterns:
  - `AWO[:\s<>]*(\d+)`
  - `AWO\s*Number[:\s]+(\d+)`
  - `Work\s*Order\s*No[:\s]+(\d+)`
- Stored in `ParsedOrderDraft.awoNumber` field
- Required field for Assurance orders when creating Order in CreateOrderPage

4. Contact Number Auto-Fix Engine
Rules:

Remove symbols: +, -, spaces

Convert +60XXXXXXXX → 0XXXXXXXX

If the number is 9 digits → prefix 0

If the number starts with 1 and has 8–10 digits → prefix 0

Examples:

Input	Output
+60126556688	0126556688
122164657	0122164657
016-663-9910	0166639910
5. Date & Time Parsing Engine (NLP)

Supports formats:

Date Formats:

29/11/2025

29-11-2025

29 Nov

Nov 29

Tomorrow

Next Monday

Time Formats:

11:00

11 AM

1100

11.00am

1–3pm slot

Combined:

“29 November 11am”

“Tomorrow morning”

“This Friday 2pm slot”

Parser uses:

Regex

NLP keywords (“slot”, “approved for”, “booked”)

Relative date resolver

6. Reschedule Approval Parsing

Reschedule can be triggered by:

6.1 Structured Approval

TIME sends a new Excel with updated date/time.

6.2 Semi-Structured Approval

Body contains updated appointment:

Example:

Approved. New appointment is 25/11 at 10am.


Parsed using:

Date regex

Time regex

Approval detection keywords

6.3 Unstructured Human Approval

Example emails:

“Okay can, please send installer at 2pm tomorrow”

“Book for 29/11 afternoon”

“Proceed with this Saturday 9am”

Parser extracts:

Date

Time

Confirms Service ID match

Updates order

7. Duplicate Order Handling

To prevent duplicates:

Parser checks:

PRIMARY KEY MATCH:

serviceId

partnerOrderId

TTKT + serviceId

SECONDARY MATCH:

Customer name

Address

Appointment

If match found → update existing order.

8. Parser → Order Mapping Flow
Email received
↓
Partner detected
↓
Order type detected
↓
Attachment or body parsed
↓
Fields normalized
↓
Duplicate check
↓
If new → create order
If existing → update order (reschedule or enhancement)
↓
Trigger status changes (e.g. ReschedulePendingApproval → Assigned)

9. Error Handling
If mandatory fields missing:

Parser logs error

Adds order to "Parser Review Queue"

---

## 10. Email Rules & VIP Email Handling

The parser supports configurable email rules and VIP email identification. These rules integrate with the Email Pipeline's classification stage to route, filter, and prioritize incoming emails.

For pipeline architecture context, see [Email Pipeline - Classification Stage](../../01_system/EMAIL_PIPELINE.md#4-classification-stage-partner--intent-detection).

### 10.1 Email Rules

Email rules allow filtering and routing of emails based on:
- FROM address patterns (wildcard supported: `*`, `?`)
- Domain patterns (e.g. `@time.com.my`)
- Subject content (case-insensitive partial match)

**Rule Actions:**
- `RouteToDepartment` - Route email to a specific department
- `RouteToUser` - Route email to a specific user
- `MarkVipOnly` - Mark as VIP only (no routing)
- `Ignore` - Skip processing this email
- `MarkVipAndRouteToDepartment` - Mark as VIP and route to department
- `MarkVipAndRouteToUser` - Mark as VIP and route to user

**Rule Evaluation:**
- Rules are evaluated in priority order (higher priority first)
- Rules can be scoped to a specific mailbox or apply to all mailboxes
- First matching rule is applied (except for `Ignore` which stops evaluation)

### 10.2 VIP Email Identification

VIP emails are identified through:
1. **VipEmail Entity** - Exact email address match (e.g. `ceo@company.com`)
2. **EmailRule** - Pattern-based matching with `IsVip = true` or `MarkVipOnly` action

### 10.3 VIP Email Handling

When a VIP email is detected:
- `EmailMessage.IsVip` flag is set to `true`
- `EmailMessage.MatchedRuleId` or `EmailMessage.MatchedVipEmailId` is recorded
- `ParseSession.IsVip` flag is set to `true`
- Notifications are triggered (if `EmailVipStrictMode = true` or rule requires it)

**Notification Behavior:**
- If `VipEmail.NotifyUserId` is set, notification is sent to that user
- If `VipEmail.NotifyRole` is set, notification is sent to all users with that role
- Notification channel is determined by `EmailDefaultNotificationChannel` setting
- In strict mode (`EmailVipStrictMode = true`), notifications are mandatory

### 10.4 Rule Priority & Evaluation Order

1. Check VIP email list first (exact match)
2. Evaluate rules in priority order (highest first)
3. Apply first matching rule
4. If rule is `Ignore`, stop processing immediately
5. Otherwise, continue with normal parsing flow

### 10.5 Configuration

Rules and VIP emails are configured through:
- **Global Settings** - System-wide defaults
- **Company Settings** - Company-specific overrides
- **EmailRule API** - `/api/companies/{companyId}/email-rules`
- **VipEmail API** - `/api/companies/{companyId}/vip-emails`

---

## 11. Email Sources Configuration

Email sources (mailboxes) and rules are configured through:

1. **EmailAccount** - Mailbox configuration (host, provider, credentials)
2. **EmailRule** - Filtering and routing rules
3. **VipEmail** - VIP email list
4. **Global Settings** - System-wide email behavior settings
5. **Company Settings** - Company-specific email settings

See [EMAIL_PARSER_SETUP.md](../02_modules/EMAIL_PARSER_SETUP.md) for setup instructions.

Does NOT create order unless minimum required fields exist

If contact number invalid:

Attempt to auto-fix

If still invalid → log warning

If date/time missing or unreadable:

Parser flags order as “Pending → Needs Appointment”

10. Example Outputs
10.1 TIME–Digi Parsed JSON
{
  "partnerGroup": "TIMEDIGI",
  "uniqueId": "DIGI0016775",
  "customer": { "name": "ADIB OMAR", "contactNo": "0178819201" },
  "address": "...",
  "appointment": { "date": "2025-11-26", "time": "10:00" },
  "status": "Pending"
}

10.2 Reschedule Approval Parsed JSON
{
  "rescheduleApproved": true,
  "approvedDateTime": "2025-11-25 10:00",
  "status": "Assigned"
}

---

## 11. Testing Tools and Utilities

### 11.1 Assurance File Parser Tool

The Assurance File Parser is a testing tool located in `backend/ParserPlayground/` that parses assurance files (.msg and .pdf) and extracts key information for testing and validation purposes.

**Updated from:** `backend/ParserPlayground/README_ASSURANCE_PARSER.md`

#### Features

The parser extracts:
- Appointment date and time
- Issue descriptions
- Remarks
- All URLs (including setappt URL)
- Service ID, Ticket ID, AWO Number
- Customer information
- Contact information (phone and email)
- Service address

#### Usage

**Option 1: Using PowerShell Script (Recommended)**

```powershell
cd backend\ParserPlayground
.\run-assurance-parser.ps1
```

This script will:
1. Back up the original `Program.cs`
2. Use `ParseAssuranceProgram.cs` as the entry point
3. Run the parser
4. Restore the original `Program.cs`

**Option 2: Manual Run**

1. Temporarily rename `Program.cs` to `Program.cs.backup`
2. Rename `ParseAssuranceProgram.cs` to `Program.cs`
3. Run: `dotnet run`
4. Restore original files

#### Output

The parser displays:
- **Basic Information**: Service ID, Ticket ID, AWO Number, Customer Name
- **Appointment Date & Time**: Extracted date and time window
- **Issue Description**: Extracted from remarks
- **URLs**: All URLs found, categorized by type (APPOINTMENT SETUP, ASSIGN SI, DOCKET, WORK ORDER)
- **Full Remarks**: Complete remarks with all extracted information
- **Contact Information**: Phone and email
- **Address**: Service address

#### Files Processed

Test files should be placed in `backend\test-data` directory:
- MSG files (e.g., `APPMT - CEPHAS TRADING  SERVICESTBBNA749296GWANG YAODONGTTKT202512178631842AWO444622.msg`)
- PDF files (e.g., `APPMT - _CEPHAS TRADING & SERVICES__TBBNA261593G__Chow Yu Yang__TTKT202511138603863__AWO437884_.pdf`)

#### Technical Details

- Uses `PdfTextExtractionService` for PDF text extraction
- Uses `MsgReader` library for .msg file parsing
- Uses `PdfOrderParserService` for text parsing and field extraction
- All extracted data is displayed in a formatted, readable output

---

## 11.2 Troubleshooting: Parser Upload Fix

**Updated from:** `backend/README_PARSER_UPLOAD_FIX.md`

### Issue: File Upload Failures

File uploads may fail with: "An error occurred while saving the entity changes"

### Root Cause

The database schema for `ParseSessions` table needs to be updated to:
1. Make `EmailMessageId` nullable (file uploads don't have email messages)
2. Ensure `RowVersion` column exists with proper default
3. Ensure `UpdatedAt` has a default value
4. Ensure `SourceType` and `SourceDescription` columns exist

### Solution

#### Step 1: Run the Database Migration Script

Execute the SQL script in your PostgreSQL database:

```bash
# Using psql
psql -h localhost -U postgres -d cephasops -f backend/fix-parser-upload-schema.sql
```

The script will:
- ✅ Make `EmailMessageId` nullable (if it's currently NOT NULL)
- ✅ Add default value to `UpdatedAt` column
- ✅ Ensure `RowVersion` column exists with `gen_random_bytes(8)` default
- ✅ Add `SourceType` and `SourceDescription` columns if missing

#### Step 2: Restart Backend

After running the SQL script, restart your backend:

```bash
cd backend/src/CephasOps.Api
dotnet watch run
```

#### Step 3: Test File Upload

1. Go to `http://localhost:5173/orders`
2. Click "Import Orders"
3. Upload an Excel file (e.g., `.xls` or `.xlsx`)
4. The upload should now succeed

### Verification

After running the script, verify the schema:

```sql
-- Check EmailMessageId is nullable
SELECT column_name, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'ParseSessions' 
AND column_name = 'EmailMessageId';
-- Should return: is_nullable = 'YES'

-- Check RowVersion exists
SELECT column_name, column_default 
FROM information_schema.columns 
WHERE table_name = 'ParseSessions' 
AND column_name = 'RowVersion';
-- Should return: column_default = 'gen_random_bytes(8)'::bytea
```

### Code Changes Already Applied

The following code changes have already been made:
- ✅ `ParserService.cs` - Sets `UpdatedAt` and `EmailMessageId = null` when creating ParseSession
- ✅ `ParseSessionConfiguration.cs` - Configures `EmailMessageId` as nullable and RowVersion properly
- ✅ `ParserController.cs` - Improved error messages

---

## 12. End of Email Parser Specification

This document defines ALL logic required to convert any incoming upstream communication from TIME or its partners into a structured CephasOps Order.

It MUST match implementation in:

- Email ingestion worker
- ParserService
- OrderService
- RescheduleEngine
- UI
- Logging

---

**Related Documentation:**

- [Email Pipeline Architecture](../../01_system/EMAIL_PIPELINE.md) - Pipeline flow and architecture
- [OVERVIEW.md](./OVERVIEW.md) - Parser module overview
- [WORKFLOW.md](./WORKFLOW.md) - Processing workflow
- [BUILDING_MATCHING.md](./BUILDING_MATCHING.md) - Building resolution
- [Workflow Engine](../../01_system/WORKFLOW_ENGINE.md) - Status transitions

---

**END OF EMAIL PARSER SPECIFICATION**
