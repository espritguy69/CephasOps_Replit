# Order Creation Logic - Complete Documentation

**Version:** 1.0  
**Last Updated:** 2025-01-18  
**Status:** Production

This document describes the complete order creation logic in CephasOps, covering manual order creation, parser-based creation, field mappings, validation rules, partner auto-detection, and TIME partner-specific logic.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Entry Points](#2-entry-points)
3. [Backend Service Logic](#3-backend-service-logic)
4. [Field Mapping](#4-field-mapping)
5. [Order Type-Specific Logic](#5-order-type-specific-logic)
6. [Partner Auto-Detection](#6-partner-auto-detection)
7. [Validation Rules](#7-validation-rules)
8. [Business Rules](#8-business-rules)
9. [Integration Points](#9-integration-points)
10. [TIME Partner Specific Logic](#10-time-partner-specific-logic)

---

## 1. Overview

Order creation in CephasOps can occur through three primary entry points:

1. **Manual Creation** - Admin creates order via UI (`/orders/create`)
2. **Parser Upload** - Excel/PDF file uploaded via UI, parsed, draft approved
3. **Email Ingestion** - Email with attachment parsed automatically, draft approved

All three paths eventually call the same backend services:
- `OrderService.CreateOrderAsync()` - For manual creation
- `OrderService.CreateFromParsedDraftAsync()` - For parser/email creation

### Common Flow

```
Entry Point → Validation → Field Mapping → Building Resolution → 
Department Resolution → Order Type Resolution → Partner Resolution → 
Material Application → Order Creation → Response
```

---

## 2. Entry Points

### 2.1 Manual Order Creation

**Frontend:** `frontend/src/pages/orders/CreateOrderPage.tsx`  
**Backend Endpoint:** `POST /api/orders`  
**Backend Service:** `OrderService.CreateOrderAsync()`

**Features:**
- Full form with all order fields
- Real-time validation
- Partner auto-detection based on Service ID, Order Type, Building, Installation Method
- Building search and selection
- Material selection
- Order type-specific field visibility

### 2.2 Parser Upload Creation

**Frontend:** Parser Review Page → "Create Order"  
**Backend Endpoint:** `POST /api/parser/drafts/{id}/approve`  
**Backend Service:** `ParserService.ApproveParsedOrderAsync()` → `OrderService.CreateFromParsedDraftAsync()`

**Features:**
- Draft review and editing before approval
- Duplicate detection (by Service ID)
- Building matching and auto-creation
- Validation status check
- Automatic field mapping from parsed data

### 2.3 Email Ingestion Creation

**Backend Service:** `EmailIngestionService.ProcessEmailAsync()` → `ParserService.ApproveParsedOrderAsync()` → `OrderService.CreateFromParsedDraftAsync()`

**Features:**
- Automatic email processing every 1 minute
- Excel/PDF attachment parsing
- Plain text email body parsing (for Assurance)
- Partner detection from email sender/subject
- Duplicate detection (SHA256 file hash)
- Building matching

---

## 3. Backend Service Logic

### 3.1 `OrderService.CreateOrderAsync()`

**Location:** `backend/src/CephasOps.Application/Orders/Services/OrderService.cs`

**Method Signature:**
```csharp
public async Task<OrderDto> CreateOrderAsync(
    CreateOrderDto dto,
    Guid companyId,
    Guid userId,
    Guid? departmentId,
    CancellationToken cancellationToken = default)
```

**Process:**

1. **Validate OrderType**
   - Check if `OrderTypeId` exists
   - Retrieve OrderType entity

2. **Resolve Department** (Priority order):
   - OrderType's DepartmentId (from settings)
   - Explicit departmentId parameter (from UI/API)
   - Building's department (fallback)
   - If none found, order created without department (permitted in single-company, multi-department mode)

3. **Create Order Entity**
   - Map all fields from `CreateOrderDto` to `Order` entity
   - Set default values:
     - `Status = "Pending"`
     - `Priority = "Normal"`
     - `SourceSystem = "Manual"`
     - `HasReschedules = false`
     - `RescheduleCount = 0`
     - `DocketUploaded = false`
     - `PhotosUploaded = false`
     - `SerialsValidated = false`
     - `InvoiceEligible = false`

4. **Auto-Apply Materials**
   - **Activation Orders Only**: Calls `ApplyDefaultMaterialsAsync()` if order type is Activation
   - Priority:
     1. Building default materials (BuildingId + OrderTypeId)
     2. Material templates (CompanyId + PartnerId + OrderType + BuildingTypeId)
     3. Manual selection (always available)
   - **Other Order Types**: No automatic material loading; materials section available for manual addition

5. **Save and Return**
   - Save order to database
   - Return `OrderDto` with populated data

### 3.2 `OrderService.CreateFromParsedDraftAsync()`

**Location:** `backend/src/CephasOps.Application/Orders/Services/OrderService.cs`

**Method Signature:**
```csharp
public async Task<CreateOrderFromDraftResult> CreateFromParsedDraftAsync(
    CreateOrderFromDraftDto dto,
    Guid userId,
    CancellationToken cancellationToken = default)
```

**Process:**

1. **Validate Mandatory Fields**
   - Calls `ValidateDraftForOrderCreation()`
   - Returns validation errors if missing required fields

2. **Duplicate Detection**
   - **Assurance Orders:** Check by `ServiceId` + `TicketId`
   - **Other Orders:** Check by `ServiceId` only
   - Returns failure if duplicate found

3. **Address Parsing**
   - Uses `AddressParser.ParseAddress()` to extract:
     - Building name
     - Address line 1/2
     - Unit number
     - City, State, Postcode

4. **Building Resolution**
   - If `BuildingId` provided → use it
   - If building name detected → search via `BuildingService.FindBuildingByAddressAsync()`
   - If not found → return `RequiresBuilding` result (UI must create/select building)

5. **Phone Number Normalization**
   - Uses `PhoneNumberUtility.NormalizePhoneNumber()`
   - Auto-fixes common formatting issues

6. **Appointment Window Parsing**
   - Parses `AppointmentWindow` string (e.g., "09:00-11:00")
   - Default: "09:00-17:00" if missing
   - Uses `AppointmentWindowParser.TryParseAppointmentWindow()`

7. **Appointment Date**
   - Uses provided date
   - Default: Tomorrow if missing

8. **Order Type Resolution**
   - If `OrderTypeId` provided → use it
   - If `OrderTypeHint` provided → resolve via `ResolveOrderTypeIdAsync()`
   - Returns failure if cannot resolve

9. **Department Resolution** (Same priority as manual creation):
   - OrderType's DepartmentId
   - Building's department

10. **Create Order Entity**
    - Map all fields from `CreateOrderFromDraftDto` to `Order` entity
    - Encrypt ONU Password using `EncryptionService`
    - Set `SourceSystem = "EmailParser"`
    - Set `SourceEmailId` if from email

11. **Auto-Apply Materials**
    - Same logic as manual creation

12. **Return Result**
    - Success → `CreateOrderFromDraftResult.Succeeded(orderId)`
    - Failure → `CreateOrderFromDraftResult.Failed(message, errors)`
    - Requires Building → `CreateOrderFromDraftResult.RequiresBuilding(detection)`

### 3.3 `OrderService.ApplyDefaultMaterialsAsync()`

**Important:** This method is only called for **Activation order types** (Activation, FTTH, FTTO). For other order types, materials must be added manually via the UI.

**Priority Order:**

1. **Building Default Materials**
   - Query: `BuildingDefaultMaterials` WHERE `BuildingId` + `OrderTypeId` + `IsActive`
   - Create `OrderMaterialUsage` records with:
     - Quantity from building default
     - Unit cost from material catalog
     - Note: "Auto-applied from building defaults"

2. **Material Templates** (if no building defaults)
   - Query: `MaterialTemplates` via `MaterialTemplateService.GetEffectiveTemplateAsync()`
   - Key: `CompanyId` + `PartnerId` + `OrderType` + `BuildingTypeId`
   - Fallback to default template if no partner-specific
   - Create `OrderMaterialUsage` records with:
     - Quantity from template
     - Unit cost from material catalog
     - Note: "Auto-applied from material template '{TemplateName}'"

3. **Manual Selection**
   - Always available via UI after order creation
   - Required for non-Activation order types (Assurance, Modification, Value-Added Services, etc.)

---

## 4. Field Mapping

### 4.1 Manual Creation: Form → Order Entity

| Form Field | Order Entity Field | Notes |
|------------|-------------------|-------|
| `partnerId` | `PartnerId` | Required, empty GUID if not provided |
| `orderType` | `OrderTypeId` | Resolved from order type name/code |
| `serviceId` | `ServiceId` | Required for Activation orders |
| `ticketNumber` | `TicketId` | Required for Assurance orders |
| `awoNumber` | (stored in ParsedOrderDraft, not Order) | For Assurance orders |
| `customerName` | `CustomerName` | Required |
| `contactNo1` | `CustomerPhone` | Required, auto-normalized |
| `contactNo2` | `CustomerEmail` | Optional |
| `address` | `AddressLine1` | Required |
| `block`, `level`, `unit` | Parsed and stored | |
| `buildingId` | `BuildingId` | Required |
| `installationMethod` | `InstallationMethodId` | For rate keying |
| `oldAddress` | `OldAddress` | Required for Outdoor Modification |
| `indoorRemark` | `NewLocationNote` | For Indoor Modification |
| `networkPackage` | `NetworkPackage` | Network info fields |
| `networkBandwidth` | `NetworkBandwidth` | |
| `networkLoginId` | `NetworkLoginId` | |
| `networkPassword` | `NetworkPassword` | |
| `networkWanIp` | `NetworkWanIp` | |
| `networkLanIp` | `NetworkLanIp` | |
| `networkGateway` | `NetworkGateway` | |
| `networkSubnetMask` | `NetworkSubnetMask` | |
| `voipServiceId` | `VoipServiceId` | VOIP fields |
| `voipPassword` | `VoipPassword` | |
| `voipIpAddressOnu` | `VoipIpAddressOnu` | |
| `voipGatewayOnu` | `VoipGatewayOnu` | |
| `voipSubnetMaskOnu` | `VoipSubnetMaskOnu` | |
| `voipIpAddressSrp` | `VoipIpAddressSrp` | |
| `voipRemarks` | `VoipRemarks` | |
| `appointmentDateTime` | `AppointmentDate`, `AppointmentWindowFrom`, `AppointmentWindowTo` | Parsed from datetime string |
| `status` | `Status` | Default: "Pending" |
| `serviceInstallerId` | `AssignedSiId` | Optional |

### 4.2 Parser Draft → Order Entity

| ParsedOrderDraft Field | Order Entity Field | Notes |
|------------------------|-------------------|-------|
| `ServiceId` | `ServiceId` | Primary identifier |
| `TicketId` | `TicketId` | Required for Assurance |
| `AwoNumber` | (stored in draft only) | For Assurance orders |
| `CustomerName` | `CustomerName` | |
| `CustomerPhone` | `CustomerPhone` | Auto-normalized |
| `CustomerEmail` | `CustomerEmail` | |
| `AddressText` | `AddressLine1`, `BuildingName`, `UnitNo`, `City`, `State`, `Postcode` | Parsed via `AddressParser` |
| `OldAddress` | `OldAddress` | For Modification orders |
| `AppointmentDate` | `AppointmentDate` | Default: Tomorrow if missing |
| `AppointmentWindow` | `AppointmentWindowFrom`, `AppointmentWindowTo` | Parsed from string (e.g., "09:00-11:00") |
| `OrderTypeHint` | `OrderTypeId` | Resolved via `ResolveOrderTypeIdAsync()` |
| `PackageName` | `PackageName` | |
| `Bandwidth` | `Bandwidth` | |
| `OnuSerialNumber` | `OnuSerialNumber` | |
| `OnuPassword` | `OnuPasswordEncrypted` | Encrypted before storage |
| `VoipServiceId` | `VoipServiceId` | |
| `Remarks` | `PartnerNotes` | Raw remarks from source |
| `ValidationNotes` | `OrderNotesInternal` | Parser validation messages |
| `PartnerId` | `PartnerId` | Detected from email/source |
| `BuildingId` | `BuildingId` | Resolved from address |
| `SourceEmailId` | `SourceEmailId` | If from email ingestion |

### 4.3 Modification Outdoor Special Logic

For **Modification Outdoor** orders that update existing orders (detected by Service ID duplicate):

**Database-First Strategy:**

- **Customer Fields:** Use existing order's customer data (from database)
- **Old Address:** Use existing order's address (from database), allow Excel override if differs
- **New Address:** Use Excel `AddressText` (where customer is moving to)
- **ONU Password:** Use Excel `OnuPassword` (new password provided by TIME)
- **Remarks:** Append Excel `Remarks` to existing `PartnerNotes`

This ensures customer continuity while capturing the relocation details.

---

## 5. Order Type-Specific Logic

### 5.1 Activation Orders

**Order Types:** `ACTIVATION`, `FTTH`, `FTTO`

**Required Fields:**
- `ServiceId` (TBBN or Partner Service ID)
- `CustomerName`
- `CustomerPhone`
- `AddressText` (or Building + Address components)
- `BuildingId`
- `OrderTypeId`

**Optional Fields:**
- `PackageName`
- `Bandwidth`
- `OnuSerialNumber` (filled later by SI)
- `OnuPassword`
- Network info fields
- VOIP fields

**Parsing:**
- Service ID extracted from Excel/PDF
- Partner auto-detected from Service ID format
- Order category (FTTH/FTTO) detected from Excel or defaults to FTTH

**Validation:**
- Service ID format validation
- Customer phone format validation
- Building must exist or be creatable

### 5.2 Modification Orders

#### 5.2.1 Modification Outdoor

**Order Type:** `MODIFICATION_OUTDOOR`

**Required Fields:**
- `ServiceId` (existing service)
- `OldAddress` (current address)
- `AddressText` (new address - where moving to)
- `CustomerName`, `CustomerPhone` (from existing order if updating)

**Special Logic:**
- Duplicate detection finds existing order by Service ID
- Uses database-first merge strategy (see Section 4.3)
- `OldAddress` stored in `Order.OldAddress` field
- `AddressText` becomes new `AddressLine1`

#### 5.2.2 Modification Indoor

**Order Type:** `MODIFICATION_INDOOR`

**Required Fields:**
- `ServiceId` (existing service)
- `OldLocationNote` (current location/room)
- `NewLocationNote` (new location/room)
- `AddressText` (same address as existing order)

**Special Logic:**
- Same address, different unit/room
- `OldLocationNote` stored in `Order.OldLocationNote`
- `NewLocationNote` stored in `Order.NewLocationNote`

### 5.3 Assurance Orders

**Order Type:** `ASSURANCE`

**Required Fields:**
- `ServiceId` (TBBN service ID)
- `TicketId` (TTKT ticket number)
- `AwoNumber` (AWO Number - Assurance Work Order)
- `CustomerName`
- `CustomerPhone`
- `AddressText`

**Special Logic:**
- Duplicate detection by `ServiceId` + `TicketId` (not just ServiceId)
- `AwoNumber` extracted from Excel/PDF/email body
- Can be created from email body only (no attachment required)
- `ExternalRef` stores work order URL if available

**Parsing Sources:**
- Excel attachment (if present)
- PDF attachment (if present)
- Email body (plain text parsing)

### 5.4 Value Added Service Orders

**Order Type:** `VALUE_ADDED_SERVICE`

**Required Fields:**
- `ServiceId`
- `CustomerName`
- `CustomerPhone`
- `AddressText`

**Special Logic:**
- Service-specific configurations
- May require additional fields based on service type

### 5.5 Cancellation Orders

**Order Type:** `CANCELLATION`

**Status:** Detected by parser but not fully implemented in domain yet

**Note:** This order type is detected during parsing but domain logic is pending implementation.

---

## 6. Partner Auto-Detection

### 6.1 Frontend Auto-Detection

**Location:** `frontend/src/pages/orders/CreateOrderPage.tsx`  
**Function:** `autoDetectPartner()`

**Detection Priority:**

1. **Order Type: Assurance**
   - Search for partner with name containing "time" AND "assurance"
   - Returns TIME Assurance partner

2. **Installation Method: FTTO**
   - Search for partner with name containing "time" AND "ftto"
   - Returns TIME FTTO partner

3. **Service ID Patterns:**
   - `DIGI*` → Digi partner
   - `CELCOM*` → Celcom partner
   - `UMOBILE*` → U Mobile partner
   - `TBBN*` → TIME partner (exclude Assurance/FTTO)

**Usage:**
- Triggered when Service ID, Order Type, or Installation Method changes
- Auto-selects partner in dropdown if found
- User can still override manually

### 6.2 Backend Partner Resolution

**Parser Services:**
- `SyncfusionExcelParserService` sets `PartnerCode` during parsing:
  - `"TIME"` - Default
  - `"TIME-FTTH"` - FTTH activation
  - `"TIME-FTTO"` - FTTO activation
  - `"TIME-CELCOM"` - Celcom HSBB
  - `"TIME-DIGI"` - Digi HSBB
  - `"TIME-ASSURANCE"` - Assurance orders

- `EmailIngestionService` resolves `PartnerId` from:
  - Email sender domain
  - Parser template configuration
  - `PartnerCode` from parsed data

**Partner Resolution:**
- `PartnerCode` → `PartnerId` lookup via `PartnerService`
- If not found, defaults to TIME partner
- Partner must exist in database before order creation

---

## 7. Validation Rules

### 7.1 Frontend Validation

**Schema:** `ORDER_FORM_SCHEMA` in `CreateOrderPage.tsx`

**General Rules:**
- `status` - Required
- `serviceId` - Required for Activation orders
- `customerName` - Required
- `contactNo1` - Required, phone format validation
- `address` - Required
- `buildingId` - Required
- `orderType` - Required

**Order Type-Specific Rules:**

**Assurance:**
- `ticketNumber` - Required
- `awoNumber` - Required

**Modification Outdoor:**
- `oldAddress` - Required
- `newAddress` - Required (derived from `address` field)

**Modification Indoor:**
- `indoorRemark` - Required

### 7.2 Backend Validation

**Method:** `OrderService.ValidateDraftForOrderCreation()`

**Rules:**
- `CompanyId` - Required
- `ServiceId` - Required (unless Assurance with TicketId only)
- `CustomerName` - Required
- `CustomerPhone` - Required, normalized
- `AddressText` - Required
- `AppointmentDate` - Default to tomorrow if missing
- `AppointmentWindow` - Default to "09:00-17:00" if missing
- `OrderTypeId` - Must resolve from hint or be provided
- `BuildingId` - Required (or building detection result returned)

**Duplicate Detection:**
- **Assurance:** `ServiceId` + `TicketId` unique
- **Others:** `ServiceId` unique within company

---

## 8. Business Rules

### 8.1 Department Resolution Priority

1. OrderType's DepartmentId (from OrderType settings)
2. Explicit departmentId (from UI/API parameter)
3. Building's department (fallback)
4. No department (permitted, order created without department assignment)

### 8.2 Building Resolution

- If `BuildingId` provided → use it
- If building name detected in address → search for existing building
- If not found → return `RequiresBuilding` result
- UI must allow user to:
  - Create new building
  - Select existing building from similar matches

### 8.3 Material Application

**Automatic Default Material Loading:**
- **Activation Orders Only**: Default materials are automatically loaded only for Activation order types (Activation, FTTH, FTTO)
- Default materials are loaded from Building Default Materials configuration (BuildingId + OrderTypeId)
- Priority: Building defaults → Material templates → Manual

**Other Order Types (Assurance, Modification, Value-Added Services, etc.):**
- Default materials are **not** automatically loaded
- Materials section remains available for manual addition
- Users can add materials via the "+ Add" button when needed (e.g., customer lost device during modification, upgrades/downgrades, customer purchasing devices)

**General Rules:**
- Materials can be added/removed after order creation
- Material costs calculated from material catalog

### 8.4 Status Initialization

- All new orders start with `Status = "Pending"`
- Status transitions controlled by Workflow Engine
- Workflow definitions configured per company/partner/order type

### 8.5 Source System Tracking

- Manual orders: `SourceSystem = "Manual"`
- Parser upload: `SourceSystem = "Manual"` (user-initiated upload)
- Email ingestion: `SourceSystem = "EmailParser"`
- `SourceEmailId` populated for email-ingested orders

### 8.6 Duplicate Handling

**Modification Outdoor:**
- If Service ID matches existing order → Update existing order (database-first merge)
- Preserves customer data, updates address and appointment

**Other Orders:**
- If duplicate detected → Return validation error
- User must resolve duplicate before proceeding

---

## 9. Integration Points

### 9.1 Building Service

**Methods Used:**
- `FindBuildingByAddressAsync()` - Search building by address components
- Returns building detection result with matches and similar buildings

### 9.2 Material Template Service

**Methods Used:**
- `GetEffectiveTemplateAsync()` - Get material template for order context
- Key: `CompanyId` + `PartnerId` + `OrderType` + `BuildingTypeId`

### 9.3 Encryption Service

**Usage:**
- `Encrypt()` - Encrypt ONU Password before storage
- `Decrypt()` - Decrypt ONU Password for display (authorized users only)

### 9.4 Workflow Engine

**Integration:**
- Order status changes trigger workflow transitions
- Workflow definitions control:
  - Valid status transitions
  - Guard conditions (splitter, docket, photos, etc.)
  - Side effects (notifications, stock movements, status logs)

### 9.5 Address Parser

**Method:** `AddressParser.ParseAddress(addressText)`

**Extracts:**
- Building name
- Address line 1/2
- Unit number
- City, State, Postcode

**Returns:** `AddressComponents` object

### 9.6 Phone Number Utility

**Method:** `PhoneNumberUtility.NormalizePhoneNumber(phone)`

**Function:**
- Auto-fixes common formatting issues
- Standardizes to consistent format
- Handles Malaysian phone numbers (country code, area codes)

### 9.7 Appointment Window Parser

**Method:** `AppointmentWindowParser.TryParseAppointmentWindow(window, out from, out to)`

**Format:** "HH:mm-HH:mm" (e.g., "09:00-11:00")

**Returns:**
- `windowFrom` - TimeSpan
- `windowTo` - TimeSpan

---

## 10. TIME Partner Specific Logic

### 10.1 TIME Partner Variants

CephasOps supports multiple TIME partner variants:

| Partner Variant | PartnerCode | Use Case |
|----------------|-------------|----------|
| TIME | `TIME` | Standard TIME FTTH orders |
| TIME FTTH | `TIME-FTTH` | Explicit FTTH activation |
| TIME FTTO | `TIME-FTTO` | FTTO (Fiber To The Office) activation |
| TIME Celcom | `TIME-CELCOM` | Celcom HSBB orders |
| TIME Digi | `TIME-DIGI` | Digi HSBB orders |
| TIME Assurance | `TIME-ASSURANCE` | Assurance/Trouble ticket orders |

### 10.2 TIME Partner Auto-Detection

**In Parser:**
- Default `PartnerCode = "TIME"` for all parsed orders
- Set to specific variant based on:
  - Order category (FTTH vs FTTO)
  - Partner service ID format (CELCOM*, DIGI*)
  - Email subject/template matching

**In Frontend:**
- Service ID starting with `TBBN` → TIME partner
- Installation method containing `FTTO` → TIME FTTO partner
- Order type `Assurance` → TIME Assurance partner

### 10.3 TIME Billing Logic

**Principal Billing Scenario:**
- TIME, TIME-Digi, TIME-Celcom, TIME-U Mobile all use TIME Principal Billing
- `billingScenario = TIME_PRINCIPAL`
- `billingPartnerId = TIME`
- All invoices go through TIME upload portal
- Payment due = `portalUploadDate + 45 days`

### 10.4 TIME Order Type Mapping

| TIME Order Type | OrderTypeCode | Notes |
|----------------|---------------|-------|
| FTTH Activation | `ACTIVATION` or `FTTH` | Standard fiber activation |
| FTTO Activation | `FTTO` | Office fiber activation |
| Modification Outdoor | `MODIFICATION_OUTDOOR` | Address relocation |
| Modification Indoor | `MODIFICATION_INDOOR` | Same address, room change |
| Assurance/Trouble Ticket | `ASSURANCE` | Requires TTKT + AWO Number |

### 10.5 TIME Service ID Rules

**TBBN Format:**
- Pattern: `TBBN[A-Z]?\d+[A-Z]?`
- Examples: `TBBN1234567`, `TBBNA12345`, `TBBNB1234`
- `ServiceIdType = TBBN`
- Auto-detected when Service ID starts with "TBBN"

**Partner Service IDs:**
- Digi: `DIGI\d+` or `DIGI00\d+`
- Celcom: `CELCOM\d+` or `CELCOM00\d+`
- `ServiceIdType = PartnerServiceId`

### 10.6 TIME Modification Outdoor Special Rules

**Database-First Merge Strategy:**
- Customer data preserved from existing order
- Old address from existing order (database-first)
- New address from Excel (where customer is moving to)
- ONU Password from Excel (new password provided by TIME)
- Remarks appended to existing PartnerNotes

**Rationale:**
- Ensures customer continuity
- Preserves historical customer data
- Captures relocation details accurately

### 10.7 TIME Assurance Orders

**Required Fields:**
- `ServiceId` (TBBN format)
- `TicketId` (TTKT ticket number)
- `AwoNumber` (Assurance Work Order Number)

**Email Sources:**
- Excel attachment (if present)
- PDF attachment (if present)
- Email body (plain text parsing - no attachment required)

**Extraction:**
- `AwoNumber` extracted from:
  - Excel: Right-side label matching ("AWO Number", "AWO NO", "AWO")
  - PDF: Text pattern matching
  - Email body: Text pattern matching

**Duplicate Detection:**
- By `ServiceId` + `TicketId` (not just ServiceId)
- Prevents duplicate assurance tickets

### 10.8 TIME Reschedule Approval

**Business Rule:**
- TIME must approve any reschedule
- Admin cannot change date/time without TIME approval

**Flow:**
1. Admin sends reschedule request
2. Order status → `ReschedulePendingApproval`
3. TIME replies with approval email (Excel or plain text)
4. Parser updates order date/time
5. Order status → `Assigned`

### 10.9 TIME Splitter Port Usage

**Mandatory at Job Completion:**
- Installer must record Splitter ID and Port
- System enforces:
  - Port not previously used
  - Port belongs to the building
  - One port must remain as standby
- Port once used → forever locked

### 10.10 TIME Field Mapping Differences

**TIME FTTH/FTTO:**
- `uniqueId` (serviceId) → `ServiceId`
- `address.fullAddress` → `AddressLine1`
- `appointment.date` → `AppointmentDate`
- `appointment.time` → `AppointmentWindowFrom/To`
- `OrderTypeHint` → `OrderTypeId` (resolved)

**TIME-Celcom/Digi HSBB:**
- Partner Order ID (`CELCOM00xxxxx` / `DIGI00xxxx`) → `ServiceId`
- Same customer/address mapping as FTTH

**TIME Modification:**
- `relocation.type` → `OrderTypeId` (Indoor/Outdoor)
- `relocation.oldAddress` → `OldAddress`
- `relocation.newAddress` → `AddressLine1`
- `relocation.oldLocationNote` → `OldLocationNote` (Indoor)
- `relocation.newLocationNote` → `NewLocationNote` (Indoor)

**TIME Assurance:**
- `assurance.ticketId` (TTKT) → `TicketId`
- `assurance.awoNumber` → `AwoNumber` (in ParsedOrderDraft)
- `uniqueId` (serviceId) → `ServiceId`
- `workOrderUrl` → `ExternalRef`

---

## Appendix A: Field Reference

### A.1 Order Entity Fields

See `backend/src/CephasOps.Domain/Orders/Entities/Order.cs` for complete field definitions.

### A.2 ParsedOrderDraft Fields

See `backend/src/CephasOps.Domain/Parser/Entities/ParsedOrderDraft.cs` for complete field definitions.

### A.3 CreateOrderDto Fields

See backend DTOs for complete field definitions.

---

## Appendix B: Related Documentation

- `docs/02_modules/orders/OVERVIEW.md` - Order module overview
- `docs/02_modules/orders/WORKFLOW.md` - Order workflow and status transitions
- `docs/02_modules/orders/SERVICE_ID_RULES.md` - Service ID format rules
- `docs/02_modules/email_parser/SPECIFICATION.md` - Email parser specification
- `docs/02_modules/email_parser/AWO_NUMBER_EXTRACTION.md` - AWO Number extraction logic
- `docs/03_business/STORYBOARD_ISP.md` - Business rules and workflows

---

## Revision History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2025-01-18 | Initial documentation | AI Assistant |
| 1.1 | 2025-01-18 | Added rule: Default materials auto-load only for Activation orders; other order types require manual addition | AI Assistant |

