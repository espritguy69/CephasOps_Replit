# Excel Parse Flow Diagram

**Date:** December 12, 2025  
**Purpose:** Visual representation of Excel file parsing process, field extraction, and data mapping

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    EXCEL PARSING SYSTEM                                  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   EXCEL FILE INPUT     │      │   PARSER TEMPLATE     │
        │  (.xls, .xlsx)         │      │  (Field Mappings)     │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Attachment from     │      │ • Partner-specific     │
        │   email                │      │ • Column mappings      │
        │ • Manual upload        │      │ • Validation rules    │
        │ • Multiple formats     │      │ • Extraction rules    │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      EXCEL PARSER ENGINE       │
                    │  (Syncfusion Excel Library)    │
                    └───────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   FIELD EXTRACTION     │      │   DATA VALIDATION      │
        │  (Column Mapping)      │      │  (Required Fields)     │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      STRUCTURED DATA           │
                    │  (JSON Output)                 │
                    └───────────────────────────────┘
```

---

## Complete Flow: Excel File to Parsed Data

```
[Excel File Received]
  Source: Email attachment OR Manual upload
  Format: .xls OR .xlsx
  Size: Variable
         |
         v
┌────────────────────────────────────────┐
│ STEP 1: FILE VALIDATION                 │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[Valid File] [Invalid File]
   |              |
   |              v
   |         [Reject]
   |         [Error: Invalid format]
   |
   v
Checks:
  ✓ File extension (.xls, .xlsx)
  ✓ File size < 10MB
  ✓ File is not corrupted
  ✓ File can be opened
         |
         v
┌────────────────────────────────────────┐
│ STEP 2: IDENTIFY PARSER TEMPLATE        │
└────────────────────────────────────────┘
         |
         v
[Context Available]
  - PartnerId: TIME_FTTH
  - DepartmentId: GPON
  - OrderType: ACTIVATION
         |
         v
┌────────────────────────────────────────┐
│ LOAD PARSER TEMPLATE                    │
│ ParserTemplate.find(                   │
│   PartnerId = TIME_FTTH                │
│   DepartmentId = GPON                  │
│   OrderType = ACTIVATION                │
│ )                                       │
└────────────────────────────────────────┘
         |
         v
ParserTemplate {
  FieldMappings: {
    "Service ID": "serviceId",
    "Customer Name": "customer.name",
    "Installation Address": "address.fullAddress",
    "Contact": "customer.contactNo",
    "Appointment Date": "appointment.date",
    "Appointment Time": "appointment.time",
    "ONU Username": "network.username",
    "ONU Password": "network.password",
    ...
  }
  ValidationRules: {
    RequiredFields: ["serviceId", "customer.name", "address.fullAddress"]
    FieldFormats: {
      "serviceId": "TBBN[A-Z]?\\d+[A-Z]?"
      "customer.contactNo": "\\d{10,11}"
    }
  }
}
         |
         v
┌────────────────────────────────────────┐
│ STEP 3: OPEN EXCEL FILE                 │
│ Syncfusion Excel Library                │
└────────────────────────────────────────┘
         |
         v
[Load Workbook]
  IWorkbook workbook = ExcelEngine.Excel.Workbooks.Open(fileStream)
         |
         v
[Get First Worksheet]
  IWorksheet worksheet = workbook.Worksheets[0]
         |
         v
┌────────────────────────────────────────┐
│ STEP 4: DETECT HEADER ROW               │
└────────────────────────────────────────┘
         |
         v
[Search for Header Row]
  Strategy:
    1. Check row 1 (most common)
    2. Check row 2 (if row 1 is empty)
    3. Search for keywords: "Service ID", "Customer Name", etc.
    4. Use first row with 3+ matching column names
         |
         v
Header Row Found: Row 1
  Columns:
    A: "Service ID"
    B: "Customer Name"
    C: "Installation Address"
    D: "Contact"
    E: "Appointment Date"
    F: "Appointment Time"
    ...
         |
         v
┌────────────────────────────────────────┐
│ STEP 5: MAP COLUMNS TO FIELDS            │
└────────────────────────────────────────┘
         |
         v
[For each FieldMapping in Template]
         |
         v
Column Mapping:
  "Service ID" (Excel) → "serviceId" (JSON)
  "Customer Name" (Excel) → "customer.name" (JSON)
  "Installation Address" (Excel) → "address.fullAddress" (JSON)
  "Contact" (Excel) → "customer.contactNo" (JSON)
  ...
         |
         v
[Handle Column Variations]
  - Flexible matching (case-insensitive)
  - Handle merged cells
  - Handle shifted columns
  - Handle header variations:
    "Service ID" matches "ServiceID", "SERVICE ID", "Service Id"
         |
         v
┌────────────────────────────────────────┐
│ STEP 6: EXTRACT DATA ROWS                │
└────────────────────────────────────────┘
         |
         v
[Iterate through data rows]
  Start from: HeaderRow + 1
  End at: Last non-empty row
         |
         v
[For each Row]
         |
         v
┌────────────────────────────────────────┐
│ STEP 7: EXTRACT FIELD VALUES             │
└────────────────────────────────────────┘
         |
         v
[For each Mapped Column]
         |
         v
Extract Value:
  Cell = worksheet[row, column]
  Value = Cell.Value (or Cell.DisplayText)
         |
         v
[Handle Cell Types]
  - Text: Direct value
  - Number: Convert to string if needed
  - Date: Parse Excel date format
  - Formula: Get calculated value
  - Empty: null or empty string
         |
         v
[Store in Temporary Structure]
  RowData {
    serviceId: "TBBN1234567"
    customerName: "John Doe"
    address: "No 1, Jalan SS15/4..."
    contactNo: "0122334455"
    appointmentDate: "2025-12-15"
    appointmentTime: "10:00"
    ...
  }
         |
         v
┌────────────────────────────────────────┐
│ STEP 8: VALIDATE REQUIRED FIELDS         │
└────────────────────────────────────────┘
         |
         v
[Check Required Fields]
  Required: ["serviceId", "customer.name", "address.fullAddress"]
         |
         v
Validation Result:
  ✓ serviceId: Present ("TBBN1234567")
  ✓ customer.name: Present ("John Doe")
  ✓ address.fullAddress: Present ("No 1...")
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [MISSING FIELDS]
   |            |
   |            v
   |       [Mark as Warning]
   |       [Continue with available data]
   |
   v
┌────────────────────────────────────────┐
│ STEP 9: VALIDATE FIELD FORMATS          │
└────────────────────────────────────────┘
         |
         v
[Format Validation]
  serviceId: Pattern "TBBN[A-Z]?\\d+[A-Z]?"
    ✓ "TBBN1234567" matches
  contactNo: Pattern "\\d{10,11}"
    ✓ "0122334455" matches (11 digits)
  appointmentDate: Date format
    ✓ "2025-12-15" valid
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Mark as Error]
   |    [Log validation error]
   |
   v
┌────────────────────────────────────────┐
│ STEP 10: TRANSFORM TO JSON STRUCTURE     │
└────────────────────────────────────────┘
         |
         v
[Build Nested JSON]
  Flat Excel → Nested JSON structure
         |
         v
Transformation:
  Excel: "Customer Name" → JSON: customer.name
  Excel: "Contact" → JSON: customer.contactNo
  Excel: "Installation Address" → JSON: address.fullAddress
  Excel: "ONU Username" → JSON: network.username
  Excel: "ONU Password" → JSON: network.password
         |
         v
┌────────────────────────────────────────┐
│ STEP 11: HANDLE SPECIAL FIELDS            │
└────────────────────────────────────────┘
         |
         v
[Special Field Processing]
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[Address] [Date/Time] [Materials]
   |            |              |
   |            |              |
   v            v              v
[Parse      [Combine      [Parse
 Address]   Date+Time]    Material
 Components]              List]
         |
         v
Address Parsing:
  Input: "No 1, Jalan SS15/4, ROYCE RESIDENCE, 47500 Subang Jaya"
  Output: {
    addressLine1: "No 1, Jalan SS15/4"
    buildingName: "ROYCE RESIDENCE"
    postcode: "47500"
    city: "Subang Jaya"
    state: "Selangor"
  }
         |
         v
Date/Time Combination:
  Date: "2025-12-15"
  Time: "10:00"
  Output: "2025-12-15 10:00:00"
         |
         v
Material Parsing:
  Input: "ONU x1, Patchcord 6m x2"
  Output: [
    { materialCode: "ONU", quantity: 1 },
    { materialCode: "PATCHCORD_6M", quantity: 2 }
  ]
         |
         v
┌────────────────────────────────────────┐
│ STEP 12: CALCULATE CONFIDENCE SCORE      │
└────────────────────────────────────────┘
         |
         v
Confidence Calculation:
  Base: 1.0
  - Missing required field: -0.2 per field
  - Invalid format: -0.1 per field
  - Missing optional field: -0.05 per field
  - Parsing warnings: -0.05 per warning
         |
         v
Example:
  All required fields: ✓
  All formats valid: ✓
  Some optional fields missing: -0.1
  Confidence: 0.90 (90%)
         |
         v
┌────────────────────────────────────────┐
│ STEP 13: CREATE PARSED DATA OBJECT       │
└────────────────────────────────────────┘
         |
         v
ParsedData {
  serviceId: "TBBN1234567"
  customer: {
    name: "John Doe"
    contactNo: "0122334455"
    email: "john@example.com"
  }
  address: {
    fullAddress: "No 1, Jalan SS15/4, ROYCE RESIDENCE, 47500 Subang Jaya"
    addressLine1: "No 1, Jalan SS15/4"
    buildingName: "ROYCE RESIDENCE"
    city: "Subang Jaya"
    postcode: "47500"
    state: "Selangor"
  }
  appointment: {
    date: "2025-12-15"
    time: "10:00"
    datetime: "2025-12-15 10:00:00"
  }
  network: {
    username: "user123"
    password: "pass456"
    onuModel: "HG8240H"
  }
  materials: [
    { code: "ONU", quantity: 1, isRequired: true },
    { code: "PATCHCORD_6M", quantity: 2, isRequired: true }
  ]
  confidenceScore: 0.90
  parseErrors: []
  parseWarnings: [
    "Optional field 'Customer Email' not found"
  ]
}
         |
         v
[Parsed Data Ready]
         |
         v
[Proceed to Normalization & Building Matching]
```

---

## Excel File Format Variations

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    TIME FTTH ACTIVATION FORMAT                          │
└─────────────────────────────────────────────────────────────────────────┘

Column Layout (Standard):
  A: Service ID (TBBN1234567)
  B: Customer Name
  C: Installation Address
  D: Contact
  E: Appointment Date
  F: Appointment Time
  G: Package/Bandwidth
  H: ONU Username
  I: ONU Password
  J: ONU Model
  K: Remarks

Header Row: Row 1
Data Rows: Row 2 onwards

┌─────────────────────────────────────────────────────────────────────────┐
│                    TIME-DIGI HSBB FORMAT                                │
└─────────────────────────────────────────────────────────────────────────┘

Column Layout (Digi Specific):
  A: Digi Order ID (DIGI0016775)
  B: Customer Name
  C: Installation Address
  D: Contact
  E: Preferred Date
  F: Preferred Slot
  G: Bandwidth
  H: ONU Username
  I: Password

Header Row: Row 1 (sometimes Row 2 if merged header)
Data Rows: Row 2 or Row 3 onwards

Special Handling:
  - Header may be shifted down (merged cells)
  - Column names may vary slightly
  - Date format may differ

┌─────────────────────────────────────────────────────────────────────────┐
│                    TIME-CELCOM HSBB FORMAT                              │
└─────────────────────────────────────────────────────────────────────────┘

Column Layout (Celcom Specific):
  A: Celcom Order ID (CELCOM0016996)
  B: Customer Name
  C: Installation Address
  D: Contact (sometimes in body, not Excel)
  E: Preferred Date
  F: Preferred Slot
  G: Bandwidth
  H: ONU Username
  I: Password

Similar to Digi but:
  - Order ID format: CELCOM00xxxxx
  - Contact may be in email body instead of Excel
```

---

## Column Mapping Examples

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    TIME FTTH FIELD MAPPINGS                              │
└─────────────────────────────────────────────────────────────────────────┘

Excel Column          →  JSON Path              →  Order Field
─────────────────────────────────────────────────────────────────────────
Service ID            →  serviceId              →  Order.ServiceId
Customer Name         →  customer.name          →  Order.CustomerName
Installation Address  →  address.fullAddress     →  Order.ServiceAddress
Contact               →  customer.contactNo     →  Order.CustomerPhone
Email                 →  customer.email          →  Order.CustomerEmail
Appointment Date      →  appointment.date        →  Order.AppointmentDate
Appointment Time      →  appointment.time       →  Order.AppointmentWindowFrom
Package               →  networkInfo.package     →  Order.Package
Bandwidth             →  networkInfo.bandwidth   →  Order.Bandwidth
ONU Username          →  network.username        →  Order.OnuUsername
ONU Password          →  network.password        →  Order.OnuPasswordEncrypted
ONU Model             →  network.onuModel        →  Order.OnuModel
Remarks               →  remarks                 →  Order.Remarks

┌─────────────────────────────────────────────────────────────────────────┐
│                    MATERIAL EXTRACTION MAPPINGS                          │
└─────────────────────────────────────────────────────────────────────────┘

Excel Column          →  JSON Path              →  Material List
─────────────────────────────────────────────────────────────────────────
Materials             →  materials[]            →  Order.Materials
  Format: "ONU x1, Patchcord 6m x2, Router x1"
  
Parsing Logic:
  1. Split by comma
  2. For each item: "MaterialName xQuantity"
  3. Extract material name
  4. Extract quantity
  5. Match to Material master data
  6. Create MaterialTemplateItem entries
```

---

## Error Handling

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PARSING ERROR TYPES                                   │
└─────────────────────────────────────────────────────────────────────────┘

ERROR TYPE 1: FILE CORRUPTION
───────────────────────────────
Symptom: Cannot open Excel file
Action:
  - Log error
  - Mark EmailMessage.Processed = true
  - Set ParseError: "File corrupted or invalid format"
  - Notify admin

ERROR TYPE 2: HEADER NOT FOUND
───────────────────────────────
Symptom: Cannot find header row with expected columns
Action:
  - Try alternative header detection strategies
  - If still not found:
    - Mark for manual review
    - Set ParseWarning: "Header row not found, using row 1"
    - Continue with best guess

ERROR TYPE 3: REQUIRED FIELD MISSING
──────────────────────────────────────
Symptom: Required field (e.g., Service ID) not found in row
Action:
  - Mark row as incomplete
  - Set ParseError: "Required field 'Service ID' missing"
  - Still create draft but mark confidence low
  - Require admin review

ERROR TYPE 4: INVALID FIELD FORMAT
────────────────────────────────────
Symptom: Field value doesn't match expected format
Example: Service ID "ABC123" doesn't match TBBN pattern
Action:
  - Set ParseWarning: "Service ID format invalid"
  - Store value as-is
  - Allow admin to correct

ERROR TYPE 5: MULTIPLE ROWS IN SINGLE FILE
───────────────────────────────────────────
Symptom: Excel file contains multiple orders (rows)
Action:
  - Create one ParsedOrderDraft per row
  - Link all drafts to same EmailMessage
  - Process each row independently
  - Group in review queue by EmailMessage

ERROR TYPE 6: MERGED CELLS
───────────────────────────
Symptom: Header or data cells are merged
Action:
  - Syncfusion handles merged cells automatically
  - Extract value from first cell of merged range
  - Continue parsing normally
```

---

## Data Transformation Examples

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    EXAMPLE: TIME FTTH ACTIVATION                        │
└─────────────────────────────────────────────────────────────────────────┘

EXCEL INPUT:
─────────────────────────────────────────────────────────────────────────
Row 1 (Header):
  A: Service ID
  B: Customer Name
  C: Installation Address
  D: Contact
  E: Appointment Date
  F: Appointment Time
  G: Package
  H: ONU Username
  I: ONU Password

Row 2 (Data):
  A: TBBN1234567
  B: John Doe
  C: No 1, Jalan SS15/4, ROYCE RESIDENCE, 47500 Subang Jaya, Selangor
  D: 0122334455
  E: 15/12/2025
  F: 10:00
  G: 100Mbps
  H: user123
  I: pass456

PARSED JSON OUTPUT:
─────────────────────────────────────────────────────────────────────────
{
  "serviceId": "TBBN1234567",
  "customer": {
    "name": "John Doe",
    "contactNo": "0122334455",
    "email": null
  },
  "address": {
    "fullAddress": "No 1, Jalan SS15/4, ROYCE RESIDENCE, 47500 Subang Jaya, Selangor",
    "addressLine1": "No 1, Jalan SS15/4",
    "buildingName": "ROYCE RESIDENCE",
    "city": "Subang Jaya",
    "postcode": "47500",
    "state": "Selangor"
  },
  "appointment": {
    "date": "2025-12-15",
    "time": "10:00",
    "datetime": "2025-12-15 10:00:00"
  },
  "networkInfo": {
    "package": "100Mbps",
    "bandwidth": "100"
  },
  "network": {
    "username": "user123",
    "password": "pass456",
    "onuModel": null
  },
  "materials": [],
  "confidenceScore": 0.95,
  "parseErrors": [],
  "parseWarnings": [
    "Material list not found in Excel"
  ]
}
```

---

## Integration Points

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    EXCEL PARSER INTEGRATION                              │
└─────────────────────────────────────────────────────────────────────────┘

1. EMAIL PARSER MODULE
   ┌─────────────────────────────────────┐
   │ Excel parsing triggered from        │
   │ email attachment processing         │
   │ See: EMAIL_SETUP_AND_PARSE_FLOW.md  │
   └─────────────────────────────────────┘

2. PARSER TEMPLATE MODULE
   ┌─────────────────────────────────────┐
   │ Field mappings defined in template  │
   │ Validation rules from template      │
   └─────────────────────────────────────┘

3. MATERIALS MODULE
   ┌─────────────────────────────────────┐
   │ Material codes matched to master     │
   │ Material quantities extracted       │
   └─────────────────────────────────────┘

4. BUILDINGS MODULE
   ┌─────────────────────────────────────┐
   │ Address parsed for building match   │
   │ Building name extracted              │
   └─────────────────────────────────────┘

5. ORDERS MODULE
   ┌─────────────────────────────────────┐
   │ Parsed data used to create Order    │
   │ Service ID validated                │
   └─────────────────────────────────────┘
```

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/email_parser/EMAIL_SETUP_AND_PARSE_FLOW.md` - Complete Email Flow
- `docs/02_modules/email_parser/SPECIFICATION.md` - Full Parser Specification
- `docs/02_modules/email_parser/OVERVIEW.md` - Email Parser Overview

