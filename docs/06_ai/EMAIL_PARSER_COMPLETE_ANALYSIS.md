# Email Parser - Complete Architecture and Functionality Analysis

**Date:** 2025-01-20  
**Status:** Comprehensive Analysis Complete

---

## Executive Summary

The CephasOps Email Parser is a fully automated system that polls email mailboxes, extracts order data from emails and attachments, and converts them into structured CephasOps Orders. The system operates via background services, uses parser templates for configuration, and provides both automated and manual review workflows.

**Current Status:**
- ✅ Email polling system: **ACTIVE** (Background services running)
- ✅ Landing page: **EXISTS** (`/orders/parser` - ParserListingPage)
- ✅ CRUD operations: **PARTIAL** (Read/Approve/Reject exist, Create/Update/Delete limited)
- ✅ Parser templates: **FULLY CONFIGURABLE** (Settings → Email → Parser Templates)
- ⚠️ Monitoring dashboard: **BASIC** (needs enhancement)
- ⚠️ Manual trigger: **AVAILABLE** (via API, not prominently exposed in UI)

---

## 1. Email Polling System

### 1.1 Architecture

**Two-Tier Background Service Architecture:**

```
┌─────────────────────────────────────────────────────────┐
│ EmailIngestionSchedulerService (Background Service)     │
│ - Runs every 30 seconds                                  │
│ - Checks EmailAccounts for polling schedule              │
│ - Creates BackgroundJob entries for accounts due polling │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ BackgroundJobProcessorService (Background Service)      │
│ - Runs every 30 seconds                                  │
│ - Processes queued BackgroundJobs                         │
│ - Executes EmailIngest jobs                              │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ EmailIngestionService                                    │
│ - Connects to IMAP/POP3 mailbox                          │
│ - Downloads new emails                                   │
│ - Creates EmailMessage records                           │
│ - Triggers parsing workflow                              │
└─────────────────────────────────────────────────────────┘
```

### 1.2 Components

**Backend Services:**
- **`EmailIngestionSchedulerService`** (`backend/src/CephasOps.Application/Workflow/Services/EmailIngestionSchedulerService.cs`)
  - Background service registered in `Program.cs`
  - Checks every 30 seconds for accounts due polling
  - Creates `BackgroundJob` entries with type `"EmailIngest"`
  - Calculates next poll time: `LastPolledAt + PollIntervalSec`

- **`BackgroundJobProcessorService`** (`backend/src/CephasOps.Application/Workflow/Services/BackgroundJobProcessorService.cs`)
  - Background service registered in `Program.cs`
  - Processes queued `BackgroundJob` entries
  - Handles job types: `EmailIngest`, `PnlRebuild`, `NotificationSend`, etc.
  - Executes `EmailIngestionService.IngestEmailsAsync()` for `EmailIngest` jobs

- **`EmailIngestionService`** (`backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs`)
  - Core email processing service
  - Supports IMAP and POP3 protocols
  - Downloads emails using MailKit library
  - Creates `EmailMessage` records in database
  - Triggers parsing for each email

### 1.3 Configuration

**EmailAccount Entity:**
- `IsActive`: Boolean flag to enable/disable polling
- `PollIntervalSec`: Polling interval in seconds (default: 60)
- `LastPolledAt`: Timestamp of last successful poll
- `Provider`: `"IMAP"` or `"POP3"`
- `Host`, `Port`, `Username`: Connection details
- Credentials stored securely in `CompanySetting` (key: `email.account.{id}.credentials`)

**Registration in Program.cs:**
```csharp
builder.Services.AddHostedService<BackgroundJobProcessorService>();
builder.Services.AddHostedService<EmailIngestionSchedulerService>();
```

### 1.4 Polling Flow

```
1. EmailIngestionSchedulerService runs (every 30s)
   ↓
2. Query active EmailAccounts where IsActive = true AND PollIntervalSec > 0
   ↓
3. For each account:
   - Calculate: nextPollTime = LastPolledAt + PollIntervalSec
   - If now >= nextPollTime - 5s (tolerance):
     - Check if BackgroundJob already exists for this account
     - If not, create BackgroundJob with:
       - JobType: "EmailIngest"
       - Payload: { "emailAccountId": "..." }
       - State: Queued
   ↓
4. BackgroundJobProcessorService runs (every 30s)
   ↓
5. Query queued BackgroundJobs (State = Queued)
   ↓
6. For each EmailIngest job:
   - Set State = Running
   - Call EmailIngestionService.IngestEmailsAsync()
   - Update account.LastPolledAt = DateTime.UtcNow
   - Set State = Succeeded or Failed
```

### 1.5 Email Fetching

**IMAP Support:**
- Uses `MailKit.Net.Imap.ImapClient`
- Connects to IMAP server
- Searches for unread messages since last poll
- Downloads message body and attachments

**POP3 Support:**
- Uses `MailKit.Net.Pop3.Pop3Client`
- Connects to POP3 server
- Downloads messages (POP3 doesn't support search, so downloads all)
- Marks messages as read (if server supports)

### 1.6 Status

**✅ WORKING** - Background services are registered and running. Polling occurs automatically based on `PollIntervalSec` configuration per email account.

---

## 2. Email Parser Landing Page

### 2.1 Current State

**Location:** `frontend/src/pages/parser/ParserListingPage.tsx`  
**Route:** `/orders/parser` and `/orders/parser/list`

**What's Displayed:**
- ✅ List of parsed order drafts (`ParsedOrderDraft` entities)
- ✅ Advanced filtering (status, source type, date range, search)
- ✅ Pagination (default: 50 items per page)
- ✅ Sorting (by createdAt, serviceId, customerName, etc.)
- ✅ Status badges (Pending, Valid, NeedsReview, Rejected)
- ✅ Building status indicators (Existing, New, Not Matched)
- ✅ Confidence score display
- ✅ Review action button (navigates to Create Order page)

**Columns Displayed:**
1. Service ID
2. Customer Name
3. Address
4. Building Status
5. Validation Status
6. Confidence Score
7. Source File
8. Created At
9. Actions (Review button)

### 2.2 Features

**Filtering:**
- Status filter: Pending, Valid, NeedsReview, Rejected
- Source type filter: Email, Manual Upload
- Date range filter
- Search (by service ID, customer name, address)

**Actions:**
- **Review**: Opens Create Order page with draft data pre-filled
- **View Details**: (Not currently implemented - would show full draft JSON)

### 2.3 What's Missing

**Missing Features:**
- ❌ View raw email content
- ❌ View parse session details
- ❌ View parsing errors/logs
- ❌ Manual parse trigger button (prominent)
- ❌ Parse history timeline
- ❌ Email source link (link to EmailManagementPage)
- ❌ Bulk actions (approve/reject multiple)
- ❌ Export parsed data

### 2.4 Recommendations

**Should Display:**
1. **Parse Session Summary Card:**
   - Total sessions today
   - Successful parses
   - Failed parses
   - Pending review count

2. **Recent Activity Feed:**
   - Latest parsed emails
   - Parse errors
   - Auto-approved orders

3. **Quick Actions:**
   - "Parse Email Now" button (manual trigger)
   - "View All Emails" link
   - "Parser Templates" link

---

## 3. CRUD Operations for Email Parser

### 3.1 Current CRUD Status

#### **Parser Templates** (Settings → Email → Parser Templates)

**✅ CREATE:** Available via `POST /api/parser-templates`  
**✅ READ:** Available via `GET /api/parser-templates`  
**✅ UPDATE:** Available via `PUT /api/parser-templates/{id}`  
**✅ DELETE:** Available via `DELETE /api/parser-templates/{id}`  

**Frontend:** `frontend/src/features/email/ParserTemplatesPage.tsx`

#### **Email Accounts** (Settings → Email → Email Accounts)

**✅ CREATE:** Available via `POST /api/email-accounts`  
**✅ READ:** Available via `GET /api/email-accounts`  
**✅ UPDATE:** Available via `PUT /api/email-accounts/{id}`  
**✅ DELETE:** Available via `DELETE /api/email-accounts/{id}`  

**Frontend:** `frontend/src/features/email/EmailMailboxesPage.tsx`

#### **Parsed Order Drafts** (Parser Listing Page)

**✅ READ:** Available via `GET /api/parser/drafts` (with filters)  
**✅ UPDATE:** Available via `PUT /api/parser/drafts/{id}` (edit draft fields)  
**✅ APPROVE:** Available via `POST /api/parser/drafts/{id}/approve` (creates Order)  
**✅ REJECT:** Available via `POST /api/parser/drafts/{id}/reject`  
**❌ DELETE:** Not available (soft-delete via rejection)

**Frontend:** `frontend/src/pages/parser/ParserListingPage.tsx`

#### **Parse Sessions**

**✅ READ:** Available via `GET /api/parser/sessions`  
**❌ CREATE:** Not directly available (created automatically by parser)  
**❌ UPDATE:** Not available  
**❌ DELETE:** Not available  

**Frontend:** Limited - only accessible via API, no dedicated UI page

#### **Email Messages**

**✅ READ:** Available via `GET /api/emails` (in EmailManagementPage)  
**✅ MANUAL PARSE:** Available via `POST /api/email-accounts/{id}/poll`  
**❌ UPDATE:** Not available (read-only)  
**❌ DELETE:** Not available (read-only)

**Frontend:** `frontend/src/pages/email/EmailManagementPage.tsx`

### 3.2 Missing CRUD Operations

**Parse Sessions:**
- ❌ View parse session details page
- ❌ View parse session errors
- ❌ Retry failed parse session
- ❌ Delete parse session

**Email Messages:**
- ❌ Edit email metadata
- ❌ Delete email message
- ❌ Reparse email manually (prominent button)
- ❌ View email parsing history

**Parsed Order Drafts:**
- ❌ Bulk approve/reject
- ❌ Delete draft (hard delete)
- ❌ Duplicate draft
- ❌ Export drafts to CSV

**Parser Templates:**
- ✅ Full CRUD available (no gaps)

**Email Accounts:**
- ✅ Full CRUD available (no gaps)

### 3.3 Recommendations

**Priority 1 (High):**
1. **Manual Parse Trigger UI:**
   - Add prominent "Parse Now" button in EmailManagementPage
   - Add "Reparse Email" action in email detail view
   - Add "Retry Parse Session" in parse session view

2. **Parse Session Details Page:**
   - Create `/orders/parser/sessions/{id}` route
   - Display session metadata, parsed drafts, errors
   - Show attachment previews

3. **Bulk Actions:**
   - Add checkbox selection in ParserListingPage
   - Implement bulk approve/reject
   - Add bulk export

**Priority 2 (Medium):**
4. **Email Message Management:**
   - Add "Reparse" button in EmailManagementPage
   - Add "View Parse Results" link
   - Add email deletion (with confirmation)

5. **Parse History:**
   - Add timeline view showing parse → draft → order flow
   - Add error log viewer

---

## 4. Complete Architecture Flow

### 4.1 End-to-End Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. EMAIL ARRIVES IN MAILBOX                                   │
│    - Partner sends email with order attachment               │
│    - Email stored in IMAP/POP3 mailbox                       │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. BACKGROUND POLLING DETECTS EMAIL                         │
│    EmailIngestionSchedulerService (every 30s)               │
│    - Checks if account.LastPolledAt + PollIntervalSec       │
│      has elapsed                                             │
│    - Creates BackgroundJob (EmailIngest)                     │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. BACKGROUND JOB PROCESSOR EXECUTES                        │
│    BackgroundJobProcessorService (every 30s)                │
│    - Picks up queued EmailIngest job                         │
│    - Calls EmailIngestionService.IngestEmailsAsync()        │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. EMAIL DOWNLOADED AND STORED                               │
│    EmailIngestionService                                     │
│    - Connects to mailbox (IMAP/POP3)                        │
│    - Downloads new emails                                    │
│    - Creates EmailMessage record in database                 │
│    - Downloads attachments → EmailAttachment records        │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. PARSER TEMPLATE MATCHING                                  │
│    ParserTemplateService                                     │
│    - Extracts FROM address, SUBJECT, BODY preview            │
│    - Matches against ParserTemplate rules:                  │
│      * FROM pattern match (e.g., "*@time.com.my")           │
│      * SUBJECT pattern match (e.g., "FTTH", "Activation")   │
│      * EmailAccountId match (if template restricted)         │
│    - Selects best matching template                         │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 6. ATTACHMENT PROCESSING                                     │
│    EmailIngestionService.ProcessEmailAsync()                  │
│    - Detects attachments (Excel, PDF, MSG)                   │
│    - Downloads and stores temporarily                        │
│    - Creates ParseSession record                             │
│    - Routes to appropriate parser:                           │
│      * Excel → TimeExcelParserService                        │
│      * PDF → PdfOrderParserService                           │
│      * MSG → Extract attachments, then parse               │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 7. DATA EXTRACTION                                           │
│    Parser Services (TimeExcelParserService, etc.)            │
│    - Opens file (Excel/PDF)                                  │
│    - Applies ParserTemplate field mappings                   │
│    - Extracts: Service ID, Customer, Address,                │
│      Appointment, Package, ONU details, etc.                │
│    - Normalizes data (phone, address, dates)                 │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 8. BUILDING MATCHING                                         │
│    BuildingService                                           │
│    - Extracts address from parsed data                       │
│    - Searches Buildings database:                            │
│      Priority 1: Building code (exact)                      │
│      Priority 2: Name + Postcode (normalized)                │
│      Priority 3: Name + City (normalized)                   │
│    - Sets BuildingId if match found                         │
│    - Sets BuildingStatus: "Existing" or "New"                │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 9. VALIDATION & CONFIDENCE SCORING                           │
│    ParserService                                             │
│    - Validates required fields (per template)                │
│    - Calculates confidence score:                            │
│      * 90-100%: All required fields present                │
│      * 70-89%: Most fields present, minor issues            │
│      * 50-69%: Some fields missing                           │
│      * <50%: Major parsing issues                            │
│    - Sets ValidationStatus:                                 │
│      * "Valid" (if confidence >= 90% and all required OK)    │
│      * "NeedsReview" (if confidence < 90% or missing fields) │
│      * "Pending" (if validation not complete)               │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 10. PARSED ORDER DRAFT CREATION                              │
│     ParserService                                            │
│     - Creates ParsedOrderDraft record                       │
│     - Stores extracted data as JSON                         │
│     - Links to ParseSession                                  │
│     - Links to EmailMessage (if from email)                 │
│     - Links to Building (if matched)                        │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 11. AUTO-APPROVAL OR REVIEW QUEUE                            │
│     ParserTemplate.AutoApprove flag                          │
│     - If AutoApprove = true AND ValidationStatus = "Valid": │
│       → Automatically create Order                           │
│     - If AutoApprove = false OR ValidationStatus != "Valid": │
│       → Send to Review Queue (ParserListingPage)            │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 12. ORDER CREATION (Manual or Auto)                          │
│     OrderService                                             │
│     - User reviews draft in CreateOrderPage                 │
│     - User edits/corrects data if needed                     │
│     - User clicks "Approve" → Creates Order                │
│     - Order enters workflow lifecycle                       │
│     - ParsedOrderDraft.CreatedOrderId is set                │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 13. NOTIFICATIONS                                            │
│     NotificationService                                       │
│     - Sends notification to assigned user/department          │
│     - Email notification (if configured)                     │
│     - In-app notification                                    │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 Component Map

**Backend Services:**
- `EmailIngestionSchedulerService` - Polling scheduler
- `BackgroundJobProcessorService` - Job executor
- `EmailIngestionService` - Email downloader
- `ParserTemplateService` - Template matcher
- `TimeExcelParserService` - Excel parser
- `PdfOrderParserService` - PDF parser
- `ParserService` - Orchestrator
- `BuildingService` - Building matcher
- `OrderService` - Order creator
- `NotificationService` - Notifier

**Backend Controllers:**
- `EmailAccountsController` - Email account CRUD
- `ParserController` - Parse sessions, drafts, manual upload
- `EmailsController` - Email messages (read-only)

**Frontend Pages:**
- `ParserListingPage` - Draft review queue
- `EmailManagementPage` - Email viewer
- `ParserTemplatesPage` - Template management
- `EmailMailboxesPage` - Account management
- `CreateOrderPage` - Draft review/edit/approve

**Database Entities:**
- `EmailAccount` - Mailbox configuration
- `EmailMessage` - Email metadata
- `EmailAttachment` - Attachment metadata
- `ParseSession` - Parse job session
- `ParsedOrderDraft` - Extracted order data
- `ParserTemplate` - Parsing rules
- `BackgroundJob` - Job queue

---

## 5. Parser Templates System

### 5.1 How Templates Work

**Parser Templates** are configuration records that define:
- Which emails to match (FROM pattern, SUBJECT pattern)
- How to extract data (field mappings, regex rules)
- What order type to create
- Whether to auto-approve or require review

**Location:** Settings → Email → Parser Templates  
**Entity:** `ParserTemplate` (`backend/src/CephasOps.Domain/Parser/Entities/ParserTemplate.cs`)

### 5.2 Template Matching Logic

**Priority Order:**
1. **Exact FROM match** (e.g., `noreply@time.com.my`)
2. **FROM pattern match** (e.g., `*@time.com.my`, `noreply@*`)
3. **SUBJECT keyword match** (e.g., contains "FTTH", "Activation")
4. **EmailAccountId match** (if template restricted to specific mailbox)
5. **Default template** (if EmailAccount has DefaultParserTemplateId)
6. **Manual review** (if no template matches)

**Matching Service:** `ParserTemplateService.FindMatchingTemplateAsync()`

### 5.3 Template Structure

**Key Fields:**
- `PartnerPattern`: FROM address pattern (supports wildcards: `*`, `?`)
- `SubjectPattern`: SUBJECT keyword pattern
- `EmailAccountId`: Optional restriction to specific mailbox
- `OrderTypeId`: Order type to create
- `OrderTypeCode`: Fallback order type code
- `AutoApprove`: Auto-create orders if validation passes
- `Priority`: Evaluation order (higher = evaluated first)
- `FieldMappings`: JSON structure defining how to extract fields

### 5.4 Template Management

**CRUD Operations:**
- ✅ **Create:** `POST /api/parser-templates`
- ✅ **Read:** `GET /api/parser-templates`
- ✅ **Update:** `PUT /api/parser-templates/{id}`
- ✅ **Delete:** `DELETE /api/parser-templates/{id}`

**Frontend:** `frontend/src/features/email/ParserTemplatesPage.tsx`

### 5.5 Field Mappings

Templates define field mappings in JSON format:
```json
{
  "serviceId": {
    "source": "excel",
    "column": "SERVICE ID",
    "required": true
  },
  "customerName": {
    "source": "excel",
    "column": "CUSTOMER NAME",
    "required": true
  },
  "addressText": {
    "source": "excel",
    "column": "SERVICE ADDRESS",
    "required": true
  }
}
```

---

## 6. Integration Points

### 6.1 Service Orders

**Integration:**
- Parsed drafts → Orders via `POST /api/parser/drafts/{id}/approve`
- Order creation triggers workflow lifecycle
- Order links back to `ParsedOrderDraft` via `CreatedOrderId`

### 6.2 Buildings

**Integration:**
- Building matching during parsing
- Auto-links `ParsedOrderDraft.BuildingId` if match found
- Sets `BuildingStatus` to "Existing" or "New"

### 6.3 Customers

**Integration:**
- Customer data extracted from parsed emails
- Customer created automatically when order is created (if not exists)
- Customer matching by phone/email (future enhancement)

### 6.4 Installers

**Integration:**
- Installer assignment happens after order creation (workflow step)
- Not directly integrated with parser

### 6.5 Notifications

**Integration:**
- Notifications sent when:
  - Parse session completes
  - Draft requires review
  - Order created from draft
- NotificationService handles delivery

### 6.6 Confidence Scoring

**Integration:**
- Confidence score calculated during parsing
- Used to determine `ValidationStatus`
- Affects auto-approval decision
- Displayed in ParserListingPage

---

## 7. Configuration and Settings

### 7.1 Email Accounts

**Configurable:**
- ✅ Mailbox name
- ✅ Provider (IMAP/POP3)
- ✅ Host, Port
- ✅ Username
- ✅ Password (stored in CompanySetting)
- ✅ Poll interval (seconds)
- ✅ Active status
- ✅ Default department
- ✅ Default parser template

**Location:** Settings → Email → Email Accounts  
**API:** `/api/email-accounts`

### 7.2 Parser Templates

**Configurable:**
- ✅ Template name and code
- ✅ Partner pattern (FROM address)
- ✅ Subject pattern
- ✅ Order type mapping
- ✅ Auto-approve flag
- ✅ Field mappings (JSON)
- ✅ Priority

**Location:** Settings → Email → Parser Templates  
**API:** `/api/parser-templates`

### 7.3 Polling Configuration

**Per EmailAccount:**
- `PollIntervalSec`: How often to poll (default: 60 seconds)
- `IsActive`: Enable/disable polling
- `LastPolledAt`: Read-only timestamp

**Global:**
- Scheduler interval: 30 seconds (hardcoded in `EmailIngestionSchedulerService`)
- Job processor interval: 30 seconds (hardcoded in `BackgroundJobProcessorService`)

### 7.4 Auto-Processing vs Manual Review

**Auto-Processing:**
- Controlled by `ParserTemplate.AutoApprove` flag
- If `true` AND `ValidationStatus = "Valid"` → Auto-create order
- If `false` OR `ValidationStatus != "Valid"` → Send to review queue

**Manual Review:**
- User reviews drafts in ParserListingPage
- User can edit draft data before approval
- User clicks "Approve" to create order

### 7.5 Notification Settings

**Configurable:**
- Email notifications (via EmailTemplate system)
- In-app notifications (via NotificationService)
- Notification recipients (department, user, role-based)

---

## 8. Error Handling and Logs

### 8.1 Error Handling

**Failed Parsing:**
- `EmailMessage.ParserStatus` set to `"Error"`
- `EmailMessage.ParserError` stores error message
- `ParseSession.ErrorMessage` stores session-level errors
- `ParsedOrderDraft.ValidationStatus` set to `"Rejected"` if critical errors

**Failed Email Download:**
- BackgroundJob.State set to `"Failed"`
- BackgroundJob.LastError stores error message
- Retry logic: Up to `MaxRetries` (default: 3) with exponential backoff

**Invalid Email Format:**
- Parser attempts to extract data
- Missing fields reduce confidence score
- Draft sent to review queue if confidence < 90%

### 8.2 Logging

**Log Locations:**
- Application logs (Serilog) - backend console/file
- Database: `EmailMessage.ParserError`
- Database: `ParseSession.ErrorMessage`
- Database: `BackgroundJob.LastError`

**Log Levels:**
- `Information`: Normal operations (email fetched, parsed, order created)
- `Warning`: Non-critical issues (template not matched, low confidence)
- `Error`: Critical failures (connection error, parsing exception)

### 8.3 Error Notifications

**Notifications Sent:**
- Parse session failure → Assigned user/department
- Email download failure → System admin (if configured)
- Critical parsing errors → Review queue notification

---

## 9. Frontend Components

### 9.1 Existing Components

**Pages:**
1. **ParserListingPage** (`frontend/src/pages/parser/ParserListingPage.tsx`)
   - Main landing page for parsed drafts
   - Filtering, sorting, pagination
   - Review action

2. **EmailManagementPage** (`frontend/src/pages/email/EmailManagementPage.tsx`)
   - Email viewer
   - Email list with filters
   - Compose/reply functionality
   - Manual poll trigger (via API)

3. **ParserTemplatesPage** (`frontend/src/features/email/ParserTemplatesPage.tsx`)
   - Template CRUD
   - Template configuration form

4. **EmailMailboxesPage** (`frontend/src/features/email/EmailMailboxesPage.tsx`)
   - Email account CRUD
   - Account configuration form

5. **ParserSnapshotViewerPage** (`frontend/src/pages/parser/ParserSnapshotViewerPage.tsx`)
   - View parse session snapshots (PDF preview)

**Components:**
- DataTable (for listing)
- Modal (for forms)
- StatusBadge (for status display)
- LoadingSpinner (for loading states)

### 9.2 Missing Components

**Missing Pages:**
- ❌ Parse Session Details Page
- ❌ Email Parse History Page
- ❌ Parser Dashboard (summary statistics)
- ❌ Parse Error Log Viewer

**Missing Components:**
- ❌ Manual Parse Trigger Button (prominent)
- ❌ Bulk Action Toolbar
- ❌ Parse Timeline View
- ❌ Email → Draft → Order Link Visualization

### 9.3 Recommendations

**Priority 1:**
1. **Parser Dashboard:**
   - Create `/orders/parser/dashboard` route
   - Display statistics (today's parses, success rate, pending count)
   - Recent activity feed
   - Quick actions (Parse Now, View Templates)

2. **Parse Session Details:**
   - Create `/orders/parser/sessions/{id}` route
   - Display session metadata, drafts, errors
   - Show attachment previews
   - Retry button

**Priority 2:**
3. **Bulk Actions:**
   - Add checkbox selection to ParserListingPage
   - Implement bulk approve/reject
   - Add bulk export

4. **Enhanced Email Management:**
   - Add "Reparse" button in email detail view
   - Add "View Parse Results" link
   - Add parse status indicator

---

## 10. Compare with Excel Parser

### 10.1 Architecture Differences

**Email Parser:**
- ✅ Automated background polling
- ✅ Email-based workflow
- ✅ Template matching by FROM/SUBJECT
- ✅ Multi-email processing
- ✅ EmailMessage entity for tracking

**Excel Parser:**
- ✅ Manual file upload
- ✅ Immediate processing
- ✅ No template matching (user selects template)
- ✅ Single file processing
- ✅ No email tracking

### 10.2 Similarities

**Shared Components:**
- ✅ Same parser services (TimeExcelParserService, PdfOrderParserService)
- ✅ Same ParsedOrderDraft entity
- ✅ Same ParseSession entity
- ✅ Same validation logic
- ✅ Same confidence scoring
- ✅ Same building matching

### 10.3 Workflow Differences

**Email Parser Workflow:**
```
Email → EmailMessage → ParseSession → ParsedOrderDraft → Order
```

**Excel Parser Workflow:**
```
File Upload → ParseSession → ParsedOrderDraft → Order
```

### 10.4 Confidence Scoring

**Both Use Same Algorithm:**
- Required fields present: +10% per field
- Optional fields present: +5% per field
- Building matched: +10%
- Data validation passed: +20%
- Total: 0-100%

**Difference:**
- Email parser may have lower confidence if email format is unexpected
- Excel parser has higher confidence if file format matches template exactly

---

## 11. Current State Summary

### 11.1 What's Working

✅ **Email Polling:**
- Background services running
- Automatic polling based on PollIntervalSec
- IMAP and POP3 support
- Error handling and retries

✅ **Email Parsing:**
- Template matching
- Excel parsing (TIME format)
- PDF parsing
- Building matching
- Confidence scoring

✅ **Draft Management:**
- Draft listing with filters
- Draft review and approval
- Order creation from drafts

✅ **Configuration:**
- Email account CRUD
- Parser template CRUD
- Polling configuration

### 11.2 What's Missing

❌ **UI Enhancements:**
- Parser dashboard (statistics)
- Parse session details page
- Bulk actions
- Manual parse trigger (prominent)

❌ **Monitoring:**
- Parse success rate dashboard
- Error log viewer
- Parse history timeline
- Email → Draft → Order visualization

❌ **Advanced Features:**
- Draft editing (limited)
- Draft duplication
- Draft export
- Parse retry from UI

### 11.3 Implementation Gaps

**High Priority:**
1. Parser dashboard page
2. Parse session details page
3. Manual parse trigger button
4. Bulk approve/reject

**Medium Priority:**
5. Parse error log viewer
6. Draft editing enhancement
7. Parse history timeline
8. Email → Draft link in UI

**Low Priority:**
9. Draft export
10. Draft duplication
11. Parse retry from UI
12. Advanced filtering options

---

## 12. Configuration Guide

### 12.1 Setting Up Email Parser

**Step 1: Create Email Account**
1. Navigate to Settings → Email → Email Accounts
2. Click "Add Email Account"
3. Fill in:
   - Name: "TIME Orders Mailbox"
   - Provider: IMAP or POP3
   - Host: mail server hostname
   - Port: 993 (IMAP) or 995 (POP3)
   - Username: email address
   - Poll Interval: 60 seconds
   - Is Active: true
4. Save credentials in CompanySetting (key: `email.account.{id}.credentials`)

**Step 2: Create Parser Template**
1. Navigate to Settings → Email → Parser Templates
2. Click "Add Template"
3. Fill in:
   - Name: "TIME FTTH Activation"
   - Code: "TIME_FTTH"
   - Partner Pattern: "*@time.com.my"
   - Subject Pattern: "FTTH"
   - Order Type: Select from dropdown
   - Auto Approve: false (for review)
4. Configure field mappings (JSON)

**Step 3: Test Polling**
1. Use API: `POST /api/email-accounts/{id}/poll`
2. Check EmailManagementPage for new emails
3. Check ParserListingPage for new drafts

### 12.2 Troubleshooting

**Polling Not Working:**
- Check `EmailAccount.IsActive = true`
- Check `PollIntervalSec > 0`
- Check credentials in CompanySetting
- Check application logs for connection errors
- Verify IMAP/POP3 server accessibility

**Parsing Failures:**
- Check `EmailMessage.ParserError` field
- Check `ParseSession.ErrorMessage` field
- Verify ParserTemplate matches email
- Check application logs for parsing exceptions

**Drafts Not Appearing:**
- Check `ParsedOrderDraft.ValidationStatus`
- Check filters in ParserListingPage
- Verify ParseSession completed successfully
- Check for soft-deleted records

---

## 13. Files Reference

### 13.1 Backend Files

**Services:**
- `backend/src/CephasOps.Application/Workflow/Services/EmailIngestionSchedulerService.cs`
- `backend/src/CephasOps.Application/Workflow/Services/BackgroundJobProcessorService.cs`
- `backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs`
- `backend/src/CephasOps.Application/Parser/Services/ParserTemplateService.cs`
- `backend/src/CephasOps.Application/Parser/Services/ParserService.cs`

**Controllers:**
- `backend/src/CephasOps.Api/Controllers/EmailAccountsController.cs`
- `backend/src/CephasOps.Api/Controllers/ParserController.cs`
- `backend/src/CephasOps.Api/Controllers/EmailsController.cs`

**Entities:**
- `backend/src/CephasOps.Domain/Parser/Entities/EmailAccount.cs`
- `backend/src/CephasOps.Domain/Parser/Entities/EmailMessage.cs`
- `backend/src/CephasOps.Domain/Parser/Entities/ParseSession.cs`
- `backend/src/CephasOps.Domain/Parser/Entities/ParsedOrderDraft.cs`
- `backend/src/CephasOps.Domain/Parser/Entities/ParserTemplate.cs`

### 13.2 Frontend Files

**Pages:**
- `frontend/src/pages/parser/ParserListingPage.tsx`
- `frontend/src/pages/email/EmailManagementPage.tsx`
- `frontend/src/features/email/ParserTemplatesPage.tsx`
- `frontend/src/features/email/EmailMailboxesPage.tsx`

**API:**
- `frontend/src/api/parser.ts`
- `frontend/src/api/email.ts`

### 13.3 Documentation Files

- `docs/02_modules/email_parser/OVERVIEW.md`
- `docs/02_modules/email_parser/WORKFLOW.md`
- `docs/02_modules/email_parser/SPECIFICATION.md`
- `docs/02_modules/email_parser/SETUP.md`
- `docs/01_system/EMAIL_PIPELINE.md`

---

## 14. Answers to Specific Questions

### Q1: Is email polling currently active and working?
**A:** ✅ **YES** - Background services are registered and running. Polling occurs automatically based on `PollIntervalSec` per email account.

### Q2: Where is the email parser landing page?
**A:** `/orders/parser` - `ParserListingPage.tsx` displays parsed order drafts with filtering and review actions.

### Q3: What CRUD operations are available vs missing?
**A:** 
- ✅ Parser Templates: Full CRUD
- ✅ Email Accounts: Full CRUD
- ✅ Parsed Drafts: Read, Update, Approve, Reject (Delete missing)
- ⚠️ Parse Sessions: Read only (no UI page)
- ⚠️ Email Messages: Read only (no Update/Delete)

### Q4: How are parser templates managed?
**A:** Via Settings → Email → Parser Templates page. Full CRUD available. Templates define FROM/SUBJECT patterns, field mappings, and order type.

### Q5: How does confidence scoring work in email parser?
**A:** Same as Excel parser. Calculated based on required/optional fields present, building match, and validation. Score 0-100% determines ValidationStatus.

### Q6: What's the complete flow from email to order?
**A:** See Section 4.1 for detailed flow diagram. Summary: Email → Poll → Download → Parse → Draft → Review → Order.

### Q7: What monitoring/debugging tools exist?
**A:** 
- Application logs (Serilog)
- Database error fields (ParserError, ErrorMessage)
- BackgroundJob status tracking
- ⚠️ No dedicated UI dashboard (missing)

### Q8: How to manually trigger parsing?
**A:** 
- API: `POST /api/email-accounts/{id}/poll`
- ⚠️ Not prominently exposed in UI (missing button)

### Q9: Where are parsing logs stored?
**A:** 
- Application logs (Serilog)
- `EmailMessage.ParserError`
- `ParseSession.ErrorMessage`
- `BackgroundJob.LastError`

### Q10: What's different from Excel parser architecture?
**A:** 
- Email parser: Automated polling, email-based, template matching
- Excel parser: Manual upload, immediate processing, user-selected template
- Shared: Same parser services, same draft entity, same validation

---

## 15. Recommendations

### 15.1 Immediate Actions

1. **Add Parser Dashboard:**
   - Create `/orders/parser/dashboard` route
   - Display statistics and recent activity
   - Add "Parse Now" button

2. **Enhance ParserListingPage:**
   - Add bulk actions (approve/reject multiple)
   - Add "View Parse Session" link
   - Add "View Email" link

3. **Add Parse Session Details Page:**
   - Create `/orders/parser/sessions/{id}` route
   - Display session metadata, drafts, errors
   - Add "Retry Parse" button

### 15.2 Future Enhancements

1. **Advanced Monitoring:**
   - Parse success rate dashboard
   - Error log viewer with filtering
   - Parse history timeline

2. **Enhanced Draft Management:**
   - Draft editing UI (currently limited)
   - Draft duplication
   - Draft export to CSV

3. **Better Integration:**
   - Email → Draft → Order visualization
   - Parse status indicators in email list
   - Quick actions from email detail view

---

**End of Analysis**

