# Email Setup and Parse Flow Diagram

**Date:** December 12, 2025  
**Purpose:** Visual representation of email account setup, email ingestion, parsing, and order creation

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    EMAIL SETUP & PARSE SYSTEM                            │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   EMAIL ACCOUNT SETUP   │      │   EMAIL INGESTION     │
        │  (Configuration)        │      │  (Background Worker)  │
        ├───────────────────────┤      ├───────────────────────┤
        │ • EmailAccount         │      │ • EmailIngestionService│
        │ • Provider (IMAP/POP3) │      │ • Polls every 60s      │
        │ • Credentials (secure) │      │ • Downloads emails     │
        │ • Department routing   │      │ • Stores EmailMessage  │
        │ • Parser template      │      │ • Marks for parsing    │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      EMAIL CLASSIFICATION      │
                    │  (Partner & Intent Detection)  │
                    └───────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   PARSER ENGINE        │      │   ATTACHMENT PROCESSOR │
        │  (Data Extraction)      │      │  (Excel/PDF/HTML)     │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      DATA NORMALIZATION        │
                    │  (Contact, Address, Date)      │
                    └───────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      ORDER RESOLVER            │
                    │  (New Order vs Update)          │
                    └───────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │   PARSED ORDER DRAFT          │
                    │  (Review Queue)               │
                    └───────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      ORDER CREATION            │
                    │  (Approved Draft → Order)     │
                    └───────────────────────────────┘
```

---

## Complete Flow: Email Setup to Order Creation

```
[STEP 1: EMAIL ACCOUNT SETUP]
         |
         v
┌────────────────────────────────────────┐
│ CREATE EMAIL ACCOUNT                    │
│ POST /api/companies/{id}/email-accounts │
└────────────────────────────────────────┘
         |
         v
EmailAccount {
  Name: "Cephas Orders Mailbox"
  Provider: "IMAP" (or POP3/O365)
  Host: "mail.cephas.com.my"
  Username: "admin@cephas.com.my"
  IsActive: true
  PollIntervalSec: 60
  DefaultDepartmentId: GPON
  DefaultParserTemplateId: TIME_FTTH
}
         |
         v
┌────────────────────────────────────────┐
│ STORE CREDENTIALS (SECURE)              │
│ POST /api/companies/{id}/settings       │
└────────────────────────────────────────┘
         |
         v
CompanySetting {
  Key: "email.account.{emailAccountId}.credentials"
  Value: {
    Password: "encrypted_password"
    Port: 993
    UseSsl: true
  }
  IsEncrypted: true
}
         |
         v
[STEP 2: EMAIL INGESTION (Background Worker)]
         |
         v
[Every 60 seconds (configurable)]
         |
         v
┌────────────────────────────────────────┐
│ POLL MAILBOX                            │
│ EmailIngestionService.PollMailbox()    │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[New Emails] [No New Emails]
   |              |
   |              v
   |         [Wait 60s, retry]
   |
   v
┌────────────────────────────────────────┐
│ DOWNLOAD EMAIL                          │
│ - Fetch metadata (FROM, SUBJECT, etc.)  │
│ - Download attachments                  │
│ - Convert .msg → .eml → HTML            │
└────────────────────────────────────────┘
         |
         v
┌────────────────────────────────────────┐
│ CREATE EmailMessage                     │
└────────────────────────────────────────┘
         |
         v
EmailMessage {
  EmailAccountId: [Account ID]
  From: "noreply@time.com.my"
  To: "admin@cephas.com.my"
  Subject: "FTTH Activation - TBBN1234567"
  BodyHtml: "<html>...</html>"
  Attachments: [
    { Name: "A1647145.xls", Size: 45678 }
  ]
  ReceivedAt: 2025-12-12 10:30:00
  Processed: false
  MessageId: "unique-message-id"
  ThreadId: "thread-id-if-reply"
}
         |
         v
[STEP 3: EMAIL CLASSIFICATION]
         |
         v
┌────────────────────────────────────────┐
│ CLASSIFY EMAIL                          │
│ EmailClassificationService             │
└────────────────────────────────────────┘
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[FROM]    [SUBJECT]      [BODY]
   |         |              |
   |         |              |
   └─────────┴──────────────┘
         |
         v
Classification Result {
  PartnerGroup: "TIME"
  Partner: "TIME FTTH"
  OrderType: "ACTIVATION"
  Intent: "New Order"
  Confidence: 0.98
  DepartmentId: GPON
}
         |
         v
[STEP 4: PARSER TEMPLATE MATCHING]
         |
         v
┌────────────────────────────────────────┐
│ MATCH PARSER TEMPLATE                   │
│ Priority:                               │
│ 1. Exact FROM match                     │
│ 2. SUBJECT keyword match                │
│ 3. Default template for EmailAccount    │
│ 4. Manual review if no match            │
└────────────────────────────────────────┘
         |
         v
ParserTemplate {
  PartnerId: TIME_FTTH
  DepartmentId: GPON
  OrderType: ACTIVATION
  DetectionRules: {
    FromPattern: "noreply@time.com.my"
    SubjectKeywords: ["FTTH", "Activation"]
  }
  FieldMappings: { ... }
}
         |
         v
[STEP 5: ATTACHMENT PROCESSING]
         |
    ┌────┴────┐
    |         |
    v         v
[Has Attachments] [No Attachments]
   |                  |
   |                  v
   |            [Body Processing]
   |            (TTKT, Reschedule)
   |
   v
┌────────────────────────────────────────┐
│ PROCESS ATTACHMENT                       │
│ - Download and store temporarily        │
│ - Detect format (.xls, .xlsx, .pdf)    │
│ - Convert Excel → PDF (snapshot)        │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[Excel File] [PDF/HTML]
   |              |
   |              v
   |         [PDF/HTML Parser]
   |         (Assurance, old formats)
   |
   v
[STEP 6: EXCEL PARSING]
         |
         v
┌────────────────────────────────────────┐
│ EXCEL PARSER ENGINE                     │
│ (See EXCEL_PARSE_FLOW.md)               │
└────────────────────────────────────────┘
         |
         v
[Extract Data]
  - Service ID (TBBN)
  - Customer Name, Phone, Email
  - Service Address
  - Appointment Date/Time
  - Package/Bandwidth
  - ONU Details
  - Technical Details
         |
         v
[STEP 7: DATA NORMALIZATION]
         |
         v
┌────────────────────────────────────────┐
│ NORMALIZE DATA                          │
└────────────────────────────────────────┘
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[Contact] [Address]    [Date/Time]
   |         |              |
   |         |              |
   └─────────┴──────────────┘
         |
         v
Normalized Data {
  CustomerPhone: "0122334455" (normalized)
  Address: {
    BuildingName: "ROYCE RESIDENCE"
    AddressLine1: "No 1, Jalan SS15/4"
    City: "Subang Jaya"
    Postcode: "47500"
    State: "Selangor"
  }
  AppointmentDate: "2025-12-15 10:00:00"
}
         |
         v
[STEP 8: BUILDING MATCHING]
         |
         v
┌────────────────────────────────────────┐
│ BUILDING MATCHING SERVICE               │
│ Priority:                               │
│ 1. Building code (exact)                │
│ 2. Name + Postcode (normalized)         │
│ 3. Name + City (normalized)             │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[MATCHED] [NOT MATCHED]
   |            |
   |            v
   |       BuildingStatus = "New"
   |
   v
BuildingStatus = "Existing"
BuildingId: [Matched Building ID]
         |
         v
[STEP 9: DUPLICATE DETECTION]
         |
         v
┌────────────────────────────────────────┐
│ CHECK FOR DUPLICATES                    │
│ - Service ID (TBBN)                     │
│ - Partner Order ID                       │
│ - Customer + Address (fuzzy match)      │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[DUPLICATE] [NEW ORDER]
   |            |
   |            v
   |       [Create Draft]
   |
   v
[Update Existing Order]
  OR
[Skip (if already processed)]
         |
         v
[STEP 10: CREATE PARSED ORDER DRAFT]
         |
         v
┌────────────────────────────────────────┐
│ CREATE ParsedOrderDraft                 │
└────────────────────────────────────────┘
         |
         v
ParsedOrderDraft {
  EmailMessageId: [Email ID]
  PartnerId: TIME_FTTH
  DepartmentId: GPON
  OrderTypeId: ACTIVATION
  ServiceId: "TBBN1234567"
  CustomerName: "John Doe"
  CustomerPhone: "0122334455"
  ServiceAddress: { ... }
  BuildingId: [Matched Building]
  BuildingStatus: "Existing"
  AppointmentDate: "2025-12-15 10:00:00"
  ParsedData: { ... } (JSON)
  ConfidenceScore: 0.95
  Status: "PendingReview"
  ParseErrors: []
  ParseWarnings: []
}
         |
         v
[STEP 11: REVIEW QUEUE]
         |
         v
┌────────────────────────────────────────┐
│ ADMIN REVIEW                            │
│ ParseSessionReviewPage                  │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[APPROVE] [REJECT/EDIT]
   |            |
   |            v
   |       [Edit Draft]
   |       [Re-parse]
   |
   v
[STEP 12: CREATE ORDER]
         |
         v
┌────────────────────────────────────────┐
│ CREATE ORDER FROM DRAFT                  │
│ OrderService.CreateFromParsedDraft()    │
└────────────────────────────────────────┘
         |
         v
Order {
  Status: "Pending"
  PartnerId: TIME_FTTH
  DepartmentId: GPON
  OrderTypeId: ACTIVATION
  ServiceId: "TBBN1234567"
  CustomerName: "John Doe"
  CustomerPhone: "0122334455"
  ServiceAddress: { ... }
  BuildingId: [Building ID]
  AppointmentDate: "2025-12-15 10:00:00"
  ParsedOrderDraftId: [Draft ID]
}
         |
         v
[Order Created Successfully]
         |
         v
[Order appears in Scheduler]
[Order appears in Orders List]
[Workflow Engine activates]
```

---

## Email Account Setup Flow

```
[Admin: Settings → Email → Mailboxes]
         |
         v
┌────────────────────────────────────────┐
│ CREATE EMAIL ACCOUNT                    │
└────────────────────────────────────────┘
         |
         v
Form Fields:
  - Name: "Cephas Orders Mailbox"
  - Provider: [IMAP | POP3 | Microsoft Graph]
  - Host: "mail.cephas.com.my"
  - Port: 993 (IMAP) or 110 (POP3)
  - Username: "admin@cephas.com.my"
  - Use SSL: true
  - Poll Interval: 60 seconds
  - Default Department: GPON
  - Default Parser Template: TIME_FTTH
         |
         v
┌────────────────────────────────────────┐
│ SAVE EMAIL ACCOUNT                      │
│ POST /api/companies/{id}/email-accounts│
└────────────────────────────────────────┘
         |
         v
EmailAccount Created {
  Id: "550e8400-e29b-41d4-a716-446655440000"
  CompanyId: [Company ID]
  Name: "Cephas Orders Mailbox"
  Provider: "IMAP"
  Host: "mail.cephas.com.my"
  Username: "admin@cephas.com.my"
  IsActive: true
  PollIntervalSec: 60
  DefaultDepartmentId: GPON
  DefaultParserTemplateId: TIME_FTTH
  LastPolledAt: null
}
         |
         v
┌────────────────────────────────────────┐
│ STORE PASSWORD (SECURE)                 │
│ POST /api/companies/{id}/settings       │
└────────────────────────────────────────┘
         |
         v
CompanySetting {
  Key: "email.account.550e8400-e29b-41d4-a716-446655440000.credentials"
  Value: {
    Password: "encrypted_password_here"
    Port: 993
    UseSsl: true
    AdditionalSettings: {}
  }
  IsEncrypted: true
  IsSensitive: true
}
         |
         v
[Email Account Ready]
         |
         v
[Background Worker starts polling]
```

---

## Email Ingestion Worker Flow

```
[Background Worker: EmailIngestionService]
         |
         v
[Every 60 seconds (or configured interval)]
         |
         v
┌────────────────────────────────────────┐
│ GET ACTIVE EMAIL ACCOUNTS               │
│ WHERE IsActive = true                   │
└────────────────────────────────────────┘
         |
         v
[For each EmailAccount]
         |
         v
┌────────────────────────────────────────┐
│ CONNECT TO MAILBOX                      │
│ - Load credentials from CompanySetting  │
│ - Connect via IMAP/POP3/O365            │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[SUCCESS] [FAILURE]
   |            |
   |            v
   |       [Log Error]
   |       [Retry later]
   |
   v
┌────────────────────────────────────────┐
│ FETCH NEW EMAILS                        │
│ - Query: UNSEEN emails since LastPolled│
│ - Limit: 100 per poll (configurable)    │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[New Emails] [No New Emails]
   |              |
   |              v
   |         [Update LastPolledAt]
   |         [Wait 60s, retry]
   |
   v
[For each Email]
         |
         v
┌────────────────────────────────────────┐
│ DOWNLOAD EMAIL                          │
│ - Fetch metadata (FROM, SUBJECT, etc.) │
│ - Download body (HTML/text)             │
│ - Download attachments                  │
│ - Convert .msg → .eml → HTML            │
└────────────────────────────────────────┘
         |
         v
┌────────────────────────────────────────┐
│ CREATE EmailMessage                     │
└────────────────────────────────────────┘
         |
         v
EmailMessage {
  EmailAccountId: [Account ID]
  From: "noreply@time.com.my"
  To: "admin@cephas.com.my"
  Cc: null
  Subject: "FTTH Activation - TBBN1234567"
  BodyText: "Plain text body..."
  BodyHtml: "<html>...</html>"
  Attachments: [
    {
      Name: "A1647145.xls"
      Size: 45678
      ContentType: "application/vnd.ms-excel"
      FileId: [File ID after upload]
    }
  ]
  ReceivedAt: 2025-12-12 10:30:00
  Processed: false
  MessageId: "unique-message-id"
  ThreadId: "thread-id-if-reply"
  InReplyTo: null
  References: []
}
         |
         v
┌────────────────────────────────────────┐
│ MARK FOR PARSING                        │
│ Processed = false                       │
└────────────────────────────────────────┘
         |
         v
[EmailMessage saved to database]
         |
         v
[Trigger Parser Service]
         |
         v
[Update LastPolledAt]
```

---

## Email Classification Flow

```
[EmailMessage: Processed = false]
         |
         v
┌────────────────────────────────────────┐
│ EMAIL CLASSIFICATION SERVICE            │
│ EmailClassificationService.Classify()   │
└────────────────────────────────────────┘
         |
         v
[Extract Features]
  - FROM address
  - SUBJECT line
  - BODY preview (first 500 chars)
  - Attachment types
         |
         v
┌────────────────────────────────────────┐
│ PARTNER GROUP DETECTION                 │
└────────────────────────────────────────┘
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[FROM]    [SUBJECT]      [BODY]
   |         |              |
   |         |              |
   └─────────┴──────────────┘
         |
         v
Partner Group Detection:
  - FROM contains "@time.com.my" → TIME
  - FROM contains "@digi.com.my" → TIMEDIGI
  - FROM contains "@celcom.com.my" → TIMECELCOM
  - SUBJECT contains "DIGI00" → TIMEDIGI
  - SUBJECT contains "CELCOM00" → TIMECELCOM
         |
         v
┌────────────────────────────────────────┐
│ ORDER TYPE (INTENT) DETECTION           │
└────────────────────────────────────────┘
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[ACTIVATION] [MODIFICATION] [ASSURANCE]
   |              |              |
   |              |              |
   v              v              v
[Keywords:    [Keywords:    [Keywords:
 "FTTH",      "Modification", "TTKT",
 "Activation", "Relocation",  "AWO",
 "New Order"] "Indoor/Outdoor"] "Assurance"]
         |
         v
┌────────────────────────────────────────┐
│ CONFIDENCE SCORING                      │
└────────────────────────────────────────┘
         |
         v
Confidence Calculation:
  - Exact FROM match: +0.4
  - SUBJECT keyword match: +0.3
  - BODY keyword match: +0.2
  - Attachment type match: +0.1
  - Total: 0.0 - 1.0
         |
         v
┌────────────────────────────────────────┐
│ CLASSIFICATION RESULT                   │
└────────────────────────────────────────┘
         |
         v
ClassificationResult {
  PartnerGroup: "TIME"
  Partner: "TIME FTTH"
  OrderType: "ACTIVATION"
  Intent: "New Order"
  Confidence: 0.98
  DepartmentId: GPON
  ParserTemplateId: TIME_FTTH_TEMPLATE
}
         |
    ┌────┴────┐
    |         |
    v         v
[Confidence >= 0.75] [Confidence < 0.75]
   |                        |
   |                        v
   |                  [Send to Review Queue]
   |                  [Manual Classification]
   |
   v
[Proceed to Parser]
```

---

## Parser Template Matching Flow

```
[Classification Result Available]
         |
         v
┌────────────────────────────────────────┐
│ MATCH PARSER TEMPLATE                   │
│ ParserTemplateService.FindTemplate()    │
└────────────────────────────────────────┘
         |
         v
[Priority 1: Exact FROM Match]
  ParserTemplate.find(
    PartnerId = TIME_FTTH
    DetectionRules.FromPattern = "noreply@time.com.my"
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND]  [NOT FOUND]
   |            |
   |            v
   |       [Priority 2: SUBJECT Keyword Match]
   |           ParserTemplate.find(
   |             PartnerId = TIME_FTTH
   |             DetectionRules.SubjectKeywords = ["FTTH"]
   |           )
   |            |
   |       ┌────┴────┐
   |       |         |
   |       v         v
   |   [FOUND]  [NOT FOUND]
   |       |            |
   |       |            v
   |       |       [Priority 3: Default Template]
   |       |           EmailAccount.DefaultParserTemplateId
   |       |            |
   |       |       ┌────┴────┐
   |       |       |         |
   |       |       v         v
   |       |   [FOUND]  [NO TEMPLATE]
   |       |       |            |
   |       |       |            v
   |       |       |       [Manual Review Required]
   |       |       |       [Mark for Admin]
   |       |       |            |
   |       └───────┴────────────┘
   |               |
   └───────────────┘
         |
         v
ParserTemplate {
  Id: "template-id"
  PartnerId: TIME_FTTH
  DepartmentId: GPON
  OrderType: ACTIVATION
  DetectionRules: {
    FromPattern: "noreply@time.com.my"
    SubjectKeywords: ["FTTH", "Activation"]
    BodyKeywords: []
  }
  FieldMappings: {
    "Service ID": "serviceId"
    "Customer Name": "customer.name"
    "Installation Address": "address.fullAddress"
    ...
  }
  ValidationRules: {
    RequiredFields: ["serviceId", "customer.name"]
    FieldFormats: { ... }
  }
  PostParseActions: {
    CreateDraft: true
    AutoApprove: false
    NotifyAdmin: true
  }
}
         |
         v
[Template Selected]
         |
         v
[Proceed to Attachment/Body Processing]
```

---

## Data Normalization Flow

```
[Raw Parsed Data from Parser]
         |
         v
┌────────────────────────────────────────┐
│ CONTACT NUMBER NORMALIZATION             │
└────────────────────────────────────────┘
         |
         v
Input: "+60122334455"
  OR: "122164657"
  OR: "016-663-9910"
         |
         v
Normalization Steps:
  1. Remove all non-digits
  2. Remove country code (+60)
  3. Ensure starts with "0"
  4. Format: "0XXXXXXXXX"
         |
         v
Output: "0122334455"
         |
         v
┌────────────────────────────────────────┐
│ ADDRESS STANDARDIZATION                 │
└────────────────────────────────────────┘
         |
         v
Input: "No 1, Jalan SS15/4, ROYCE RESIDENCE, 47500 Subang Jaya, Selangor"
         |
         v
Normalization Steps:
  1. Extract building name
  2. Extract unit/block/floor
  3. Extract street name
  4. Extract postcode
  5. Extract city
  6. Extract state
  7. Remove trailing commas
  8. Standardize abbreviations
         |
         v
Output: {
  BuildingName: "ROYCE RESIDENCE"
  UnitNo: null
  AddressLine1: "No 1, Jalan SS15/4"
  AddressLine2: null
  City: "Subang Jaya"
  Postcode: "47500"
  State: "Selangor"
}
         |
         v
┌────────────────────────────────────────┐
│ DATE/TIME STANDARDIZATION               │
└────────────────────────────────────────┘
         |
         v
Input: "29 Nov 11am"
  OR: "Tomorrow 2pm"
  OR: "25/11 – slot 10-12"
         |
         v
NLP Date/Time Parser:
  1. Detect relative dates ("tomorrow", "next week")
  2. Detect absolute dates ("29 Nov", "25/11")
  3. Detect time ("11am", "2pm", "10-12")
  4. Combine into DateTime
         |
         v
Output: "2025-12-15 10:00:00"
         |
         v
┌────────────────────────────────────────┐
│ PARTNER ID NORMALIZATION                │
└────────────────────────────────────────┘
         |
         v
Input: "DIGI0016775"
  OR: "CELCOM0016996"
  OR: "TBBN1234567"
         |
         v
Normalization:
  1. Detect format (TBBN, DIGI00, CELCOM00)
  2. Extract numeric part
  3. Validate format
  4. Map to Partner
         |
         v
Output: {
  ServiceId: "TBBN1234567"
  ServiceIdType: "TBBN"
  PartnerId: TIME_FTTH
}
         |
         v
[Normalized Data Ready]
         |
         v
[Proceed to Building Matching]
```

---

## Order Resolver Flow (New vs Update)

```
[Normalized Parsed Data]
         |
         v
┌────────────────────────────────────────┐
│ ORDER RESOLVER SERVICE                   │
│ OrderResolverService.Resolve()           │
└────────────────────────────────────────┘
         |
         v
[Step 1: Service ID Match]
  Order.find(ServiceId = "TBBN1234567")
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND]  [NOT FOUND]
   |            |
   |            v
   |       [Step 2: Partner Order ID Match]
   |           Order.find(PartnerOrderId = "DIGI0016775")
   |            |
   |       ┌────┴────┐
   |       |         |
   |       v         v
   |   [FOUND]  [NOT FOUND]
   |       |            |
   |       |            v
   |       |       [Step 3: TTKT Match (Assurance)]
   |       |           Order.find(TTKT = "TTKT12345")
   |       |            |
   |       |       ┌────┴────┐
   |       |       |         |
   |       |       v         v
   |       |   [FOUND]  [NOT FOUND]
   |       |       |            |
   |       |       |            v
   |       |       |       [Step 4: Thread ID Match (Reschedule)]
   |       |       |           EmailMessage.find(ThreadId = "thread-id")
   |       |       |           → Find related Order
   |       |       |            |
   |       |       |       ┌────┴────┐
   |       |       |       |         |
   |       |       |       v         v
   |       |       |   [FOUND]  [NOT FOUND]
   |       |       |       |            |
   |       |       |       |            v
   |       |       |       |       [Step 5: Fuzzy Match]
   |       |       |       |           Customer Name + Address
   |       |       |       |           (similarity > 0.85)
   |       |       |       |            |
   |       |       |       |       ┌────┴────┐
   |       |       |       |       |         |
   |       |       |       |       v         v
   |       |       |       |   [FOUND]  [NEW ORDER]
   |       |       |       |       |            |
   |       |       |       |       |            |
   |       └───────┴───────┴───────┴────────────┘
   |               |                    |
   |               |                    v
   |               |            [Create New Draft]
   |               |            [Intent: New Order]
   |               |
   |               v
   |       [Update Existing Order]
   |       [Intent: Update/Reschedule]
   |
   v
[Update Scenarios]
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[Reschedule] [Correction] [Duplicate]
   |              |              |
   |              |              |
   v              v              v
[Update      [Update        [Skip
 Appointment] Customer/     (already
             Address]       processed)]
```

---

## Key Components

### EmailAccount Entity
```
EmailAccount
├── Id (Guid)
├── CompanyId (Guid)
├── Name (string)
├── Provider (enum: IMAP, POP3, MicrosoftGraph)
├── Host (string)
├── Port (int)
├── Username (string)
├── IsActive (bool)
├── PollIntervalSec (int)
├── DefaultDepartmentId (Guid?)
├── DefaultParserTemplateId (Guid?)
├── LastPolledAt (DateTime?)
└── CreatedAt, UpdatedAt
```

### EmailMessage Entity
```
EmailMessage
├── Id (Guid)
├── EmailAccountId (Guid)
├── From (string)
├── To (string)
├── Cc (string?)
├── Subject (string)
├── BodyText (string?)
├── BodyHtml (string?)
├── Attachments (JSON)
├── ReceivedAt (DateTime)
├── Processed (bool)
├── MessageId (string)
├── ThreadId (string?)
└── InReplyTo (string?)
```

### ParsedOrderDraft Entity
```
ParsedOrderDraft
├── Id (Guid)
├── EmailMessageId (Guid)
├── PartnerId (Guid)
├── DepartmentId (Guid)
├── OrderTypeId (Guid)
├── ServiceId (string)
├── CustomerName (string)
├── CustomerPhone (string)
├── CustomerEmail (string?)
├── ServiceAddress (JSON)
├── BuildingId (Guid?)
├── BuildingStatus (string)
├── AppointmentDate (DateTime?)
├── ParsedData (JSON)
├── ConfidenceScore (decimal)
├── Status (enum: PendingReview, Approved, Rejected)
├── ParseErrors (JSON)
└── ParseWarnings (JSON)
```

---

## Integration Points

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    EMAIL PARSER INTEGRATION                              │
└─────────────────────────────────────────────────────────────────────────┘

1. SETTINGS MODULE
   ┌─────────────────────────────────────┐
   │ EmailAccount configuration           │
   │ ParserTemplate configuration         │
   │ CompanySetting (credentials)         │
   └─────────────────────────────────────┘

2. ORDERS MODULE
   ┌─────────────────────────────────────┐
   │ Order creation from ParsedOrderDraft │
   │ Order update from reschedule emails │
   └─────────────────────────────────────┘

3. BUILDINGS MODULE
   ┌─────────────────────────────────────┐
   │ Building matching during parsing     │
   │ Building creation for new buildings  │
   └─────────────────────────────────────┘

4. WORKFLOW ENGINE
   ┌─────────────────────────────────────┐
   │ Order status transitions             │
   │ Workflow activation on order create  │
   └─────────────────────────────────────┘

5. NOTIFICATIONS MODULE
   ┌─────────────────────────────────────┐
   │ Notify admin on parse errors         │
   │ Notify admin on review queue         │
   └─────────────────────────────────────┘
```

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/01_system/EMAIL_PIPELINE.md` - Email Pipeline Architecture
- `docs/02_modules/email_parser/EXCEL_PARSE_FLOW.md` - Excel Parsing Details
- `docs/02_modules/email_parser/OVERVIEW.md` - Email Parser Overview
- `docs/02_modules/email_parser/SPECIFICATION.md` - Full Parser Specification

