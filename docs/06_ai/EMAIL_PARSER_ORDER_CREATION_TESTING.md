# Email Parser Order Creation Testing – Phase 2

This document defines the test plan, field mappings, and test scenarios for the email parser's order creation flow.

---

## 1. Scope

**In-scope:**
- Flow from parsed email → `ParsedOrderDraft` → approved → `Order` created
- Field mappings and validation rules
- Core validation and error handling

**Out-of-scope (Phase 3):**
- VIP email behavior
- Notification routing
- Advanced rule conditions

---

## 2. Field Mapping: ParsedOrderDraft → Order

### 2.1 Source Entities

| Source | Location |
|--------|----------|
| `ParsedOrderDraftDto` | `backend/src/CephasOps.Application/Parser/DTOs/ParserDto.cs` |
| `Order` entity | `backend/src/CephasOps.Domain/Orders/Entities/Order.cs` |
| `CreateOrderDto` | `backend/src/CephasOps.Application/Orders/DTOs/OrderDto.cs` |

### 2.2 Field Mapping Table

| ParsedOrderDraft Field | Order Field | Required | Default Value | Notes |
|------------------------|-------------|----------|---------------|-------|
| `Id` | — | — | — | Draft ID, not copied |
| `CompanyId` | `CompanyId` | Yes | — | Inherited from parse session |
| `ParseSessionId` | — | — | — | Reference only, not on Order |
| `PartnerId` | `PartnerId` | Yes | — | Detected from email sender/subject |
| `BuildingId` | `BuildingId` | Yes | — | Resolved from address matching |
| — | `SourceSystem` | Yes | `"EmailParser"` | Always set to EmailParser |
| (via ParseSession.EmailMessageId) | `SourceEmailId` | Yes | — | Link to source email |
| `OrderTypeHint` | `OrderTypeId` | Yes | — | Resolved from hint (FTTH/FTTO/Modification/Assurance) |
| `ServiceId` | `ServiceId` | Yes* | — | Primary key for activation orders |
| `TicketId` | `TicketId` | Conditional | — | Required for assurance orders (TTKT) |
| — | `ExternalRef` | No | `null` | Partner reference if available |
| — | `Status` | Yes | `"Pending"` | Initial status |
| — | `StatusReason` | No | `null` | — |
| — | `Priority` | No | `"Normal"` | Default priority |
| `AddressText` | `AddressLine1` | Yes | — | Parsed from full address |
| — | `AddressLine2` | No | `null` | Secondary address line |
| (parsed from AddressText) | `City` | Yes | — | Extracted from address |
| (parsed from AddressText) | `State` | Yes | — | Extracted from address |
| (parsed from AddressText) | `Postcode` | Yes | — | Extracted from address |
| — | `BuildingName` | No | — | From Building lookup |
| — | `UnitNo` | No | `null` | Extracted if present |
| — | `Latitude` | No | `null` | From Building or geocoding |
| — | `Longitude` | No | `null` | From Building or geocoding |
| `CustomerName` | `CustomerName` | Yes | — | — |
| `CustomerPhone` | `CustomerPhone` | Yes | — | Auto-fixed per contact rules |
| — | `CustomerEmail` | No | `null` | If available in email |
| `ValidationNotes` | `OrderNotesInternal` | No | `null` | Parser notes |
| — | `PartnerNotes` | No | `null` | — |
| — | `RequestedAppointmentAt` | No | `null` | Original requested time |
| `AppointmentDate` | `AppointmentDate` | Yes | — | Parsed date |
| `AppointmentWindow` | `AppointmentWindowFrom` | Yes | — | Parsed from window string |
| `AppointmentWindow` | `AppointmentWindowTo` | Yes | — | Parsed from window string |
| — | `AssignedSiId` | No | `null` | Not assigned at creation |
| — | `AssignedTeamId` | No | `null` | Not assigned at creation |
| — | `KpiCategory` | No | `null` | Determined by order type |
| — | `KpiDueAt` | No | `null` | Calculated from SLA rules |
| — | `KpiBreachedAt` | No | `null` | — |
| — | `HasReschedules` | Yes | `false` | Initial value |
| — | `RescheduleCount` | Yes | `0` | Initial value |
| — | `DocketUploaded` | Yes | `false` | Initial value |
| — | `PhotosUploaded` | Yes | `false` | Initial value |
| — | `SerialsValidated` | Yes | `false` | Initial value |
| — | `InvoiceEligible` | Yes | `false` | Initial value |
| — | `InvoiceId` | No | `null` | — |
| — | `PayrollPeriodId` | No | `null` | — |
| — | `PnlPeriod` | No | `null` | — |
| (approving user) | `CreatedByUserId` | Yes | — | User who approved draft |
| — | `CancelledAt` | No | `null` | — |
| — | `CancelledByUserId` | No | `null` | — |

### 2.3 Partner-Specific Mapping Variations

#### TIME FTTH / FTTO
| Parser Field | Order Field | Notes |
|--------------|-------------|-------|
| `customer.name` | `CustomerName` | Direct map |
| `customer.contactNo` | `CustomerPhone` | Auto-fixed |
| `uniqueId` (serviceId) | `ServiceId` | Primary identifier |
| `address.fullAddress` | `AddressLine1` | Full address text |
| `appointment.date` | `AppointmentDate` | Parsed date |
| `appointment.time` | `AppointmentWindowFrom/To` | Converted to TimeSpan window |
| `OrderTypeHint` = "FTTH" or "FTTO" | `OrderTypeId` | Resolved to OrderType entity |

#### TIME–Digi HSBB
| Parser Field | Order Field | Notes |
|--------------|-------------|-------|
| Digi Order ID (DIGI00xxxx) | `ServiceId` | Partner order ID |
| `customer.name` | `CustomerName` | — |
| `customer.contactNo` | `CustomerPhone` | Auto-fixed |
| `address.fullAddress` | `AddressLine1` | Installation address |
| `appointment.date` | `AppointmentDate` | Preferred date |
| `appointment.time` | `AppointmentWindowFrom/To` | Preferred slot |
| `OrderTypeHint` = "HSBB" | `OrderTypeId` | Resolved to HSBB type |

#### TIME–Celcom HSBB
Same as Digi HSBB except:
- Partner Order ID format: `CELCOM00xxxxx`
- Contact may be in email body rather than Excel

#### TIME Modification (Indoor/Outdoor)
| Parser Field | Order Field | Notes |
|--------------|-------------|-------|
| `relocation.type` | `OrderTypeHint` → `OrderTypeId` | "Indoor" or "Outdoor" modification |
| `relocation.oldAddress` | `OrderNotesInternal` | Stored as note for outdoor |
| `relocation.newAddress` | `AddressLine1` | New installation address |
| `relocation.oldLocationNote` | `OrderNotesInternal` | For indoor moves |
| `relocation.newLocationNote` | `PartnerNotes` | Target location |

#### TIME Assurance (TTKT)
| Parser Field | Order Field | Notes |
|--------------|-------------|-------|
| `assurance.ticketId` (TTKT) | `TicketId` | Required for assurance |
| `uniqueId` (serviceId) | `ServiceId` | Service being repaired |
| `assurance.issueCategory` | `OrderNotesInternal` | LOSi/LOBi etc. |
| `workOrderUrl` | `ExternalRef` | TIME work order URL |
| `appointment.date/time` | `AppointmentDate`, `AppointmentWindow*` | Scheduled appointment |

---

## 3. Test Scenarios

### 3.1 Flow A – Happy Path Order Creation

#### Scenario A1: TIME FTTH Activation (Full Data)
**Preconditions:**
- Valid `ParsedOrderDraft` with all fields populated
- `PartnerId` resolved to TIME partner
- `BuildingId` resolved from address
- `OrderTypeHint` = "FTTH"

**Input Draft:**
```json
{
  "id": "draft-uuid-1",
  "companyId": "company-uuid",
  "parseSessionId": "session-uuid",
  "partnerId": "time-partner-uuid",
  "buildingId": "building-uuid",
  "serviceId": "TBBN620278G",
  "ticketId": null,
  "customerName": "KUAN TE SIANG",
  "customerPhone": "0166587158",
  "addressText": "Block B, Level 33A, Unit 20, UNITED POINT, 47400 Petaling Jaya, Selangor",
  "appointmentDate": "2025-11-29",
  "appointmentWindow": "11:00-13:00",
  "orderTypeHint": "FTTH",
  "confidenceScore": 0.95,
  "validationStatus": "Pending"
}
```

**Steps:**
1. Call `ParserService.ApproveParsedOrderAsync(draftId, approveDto, companyId, userId)`
2. Service validates all required fields present
3. Service calls `OrderService.CreateFromParsedDraftAsync()` (to be implemented)
4. Order is created with correct mappings

**Expected Order:**
```json
{
  "id": "new-order-uuid",
  "companyId": "company-uuid",
  "partnerId": "time-partner-uuid",
  "sourceSystem": "EmailParser",
  "sourceEmailId": "email-uuid",
  "orderTypeId": "ftth-ordertype-uuid",
  "serviceId": "TBBN620278G",
  "ticketId": null,
  "status": "Pending",
  "priority": "Normal",
  "buildingId": "building-uuid",
  "addressLine1": "Block B, Level 33A, Unit 20, UNITED POINT, 47400 Petaling Jaya, Selangor",
  "city": "Petaling Jaya",
  "state": "Selangor",
  "postcode": "47400",
  "customerName": "KUAN TE SIANG",
  "customerPhone": "0166587158",
  "appointmentDate": "2025-11-29",
  "appointmentWindowFrom": "11:00:00",
  "appointmentWindowTo": "13:00:00",
  "hasReschedules": false,
  "rescheduleCount": 0,
  "docketUploaded": false,
  "photosUploaded": false,
  "serialsValidated": false,
  "invoiceEligible": false
}
```

**Assertions:**
- [ ] Order row created in database
- [ ] `ParsedOrderDraft.CreatedOrderId` set to new order ID
- [ ] `ParsedOrderDraft.ValidationStatus` = "Valid"
- [ ] `Order.SourceSystem` = "EmailParser"
- [ ] `Order.Status` = "Pending"
- [ ] All boolean flags initialized to `false`
- [ ] `RescheduleCount` = 0

#### Scenario A2: TIME–Digi HSBB Activation
**Preconditions:**
- Valid draft with Digi-specific fields
- `OrderTypeHint` = "HSBB"

**Input Draft:**
```json
{
  "serviceId": "DIGI0016775",
  "customerName": "ADIB OMAR",
  "customerPhone": "0178819201",
  "addressText": "123 Jalan Example, 50000 Kuala Lumpur, WP",
  "appointmentDate": "2025-11-26",
  "appointmentWindow": "10:00-12:00",
  "orderTypeHint": "HSBB"
}
```

**Expected:**
- Order created with `ServiceId` = "DIGI0016775"
- `OrderTypeId` resolved to HSBB type

#### Scenario A3: TIME Assurance (TTKT)
**Preconditions:**
- Valid draft with assurance-specific fields
- `TicketId` populated with TTKT number

**Input Draft:**
```json
{
  "serviceId": "TBBN620278G",
  "ticketId": "TTKT202511178606510",
  "customerName": "KUAN TE SIANG",
  "customerPhone": "0166587158",
  "addressText": "Block B, Level 33A, Unit 20, UNITED POINT...",
  "appointmentDate": "2025-11-29",
  "appointmentWindow": "11:00-13:00",
  "orderTypeHint": "Assurance"
}
```

**Expected:**
- Order created with both `ServiceId` and `TicketId`
- `OrderTypeId` resolved to Assurance type

---

### 3.2 Flow B – Minimal-but-Valid Data

#### Scenario B1: Minimum Required Fields Only
**Preconditions:**
- Draft has only mandatory fields, all optional fields null

**Input Draft:**
```json
{
  "companyId": "company-uuid",
  "parseSessionId": "session-uuid",
  "partnerId": "time-partner-uuid",
  "buildingId": "building-uuid",
  "serviceId": "TBBN123456A",
  "customerName": "TEST CUSTOMER",
  "customerPhone": "0123456789",
  "addressText": "123 Test Street, 50000 Kuala Lumpur, WP",
  "appointmentDate": "2025-12-01",
  "appointmentWindow": "09:00-11:00",
  "orderTypeHint": "FTTH"
}
```

**Expected:**
- Order created successfully
- Optional fields default:
  - `Priority` = "Normal"
  - `TicketId` = null
  - `ExternalRef` = null
  - `CustomerEmail` = null
  - `OrderNotesInternal` = null
  - `Latitude/Longitude` = null
  - `UnitNo` = null
  - `AddressLine2` = null

#### Scenario B2: Phone Number Auto-Fix Applied
**Input Draft:**
```json
{
  "customerPhone": "+60126556688"
}
```

**Expected:**
- `Order.CustomerPhone` = "0126556688" (auto-fixed)

#### Scenario B3: Phone Number Edge Cases
| Input | Expected Output |
|-------|-----------------|
| `+60126556688` | `0126556688` |
| `122164657` | `0122164657` |
| `016-663-9910` | `0166639910` |
| `1234567890` | `01234567890` |

---

### 3.3 Flow C – Validation and Rejection of Invalid Drafts

#### Scenario C1: Missing Mandatory Field – CustomerName
**Input Draft:**
```json
{
  "serviceId": "TBBN123456A",
  "customerName": null,
  "customerPhone": "0123456789",
  "addressText": "123 Test Street",
  "appointmentDate": "2025-12-01",
  "appointmentWindow": "09:00-11:00"
}
```

**Expected:**
- Approval fails with validation error
- No Order row created
- `ParsedOrderDraft.ValidationStatus` = "Invalid"
- Error message indicates missing `CustomerName`

#### Scenario C2: Missing Mandatory Field – AppointmentDate
**Input Draft:**
```json
{
  "serviceId": "TBBN123456A",
  "customerName": "TEST CUSTOMER",
  "customerPhone": "0123456789",
  "addressText": "123 Test Street",
  "appointmentDate": null,
  "appointmentWindow": "09:00-11:00"
}
```

**Expected:**
- Approval fails
- Error message: "AppointmentDate is required"

#### Scenario C3: Missing Mandatory Field – CustomerPhone
**Expected:**
- Approval fails
- Error message: "CustomerPhone is required"

#### Scenario C4: Missing Mandatory Field – AddressText
**Expected:**
- Approval fails
- Error message: "AddressText is required"

#### Scenario C5: Missing Mandatory Field – ServiceId (for Activation)
**Expected:**
- Approval fails for activation orders
- Error message: "ServiceId is required for activation orders"

#### Scenario C6: Missing TicketId for Assurance Order
**Input Draft:**
```json
{
  "orderTypeHint": "Assurance",
  "serviceId": "TBBN123456A",
  "ticketId": null
}
```

**Expected:**
- Approval fails
- Error message: "TicketId is required for assurance orders"

#### Scenario C7: Invalid AppointmentWindow Format
**Input Draft:**
```json
{
  "appointmentWindow": "invalid-format"
}
```

**Expected:**
- Approval fails
- Error message: "Invalid appointment window format"

#### Scenario C8: Unresolved BuildingId
**Input Draft:**
```json
{
  "buildingId": null,
  "addressText": "Unknown Address That Cannot Be Matched"
}
```

**Expected:**
- Approval fails or order created with placeholder/manual resolution flag
- Behavior depends on business rules (configurable)

---

### 3.4 Flow D – Idempotency and Duplicate Handling

#### Scenario D1: Approve Same Draft Twice
**Preconditions:**
- Draft already approved, `CreatedOrderId` is set

**Steps:**
1. First approval: Success, Order created
2. Second approval: Should return existing order, not create duplicate

**Expected:**
- Only ONE Order row exists
- Second call returns the same `CreatedOrderId`
- No error thrown (idempotent)

#### Scenario D2: Duplicate Detection by ServiceId
**Preconditions:**
- Order already exists with `ServiceId` = "TBBN123456A"
- New draft has same `ServiceId`

**Expected Behavior (per EMAIL_PARSER.md):**
- If match found → update existing order (not create new)
- Parser should flag as potential duplicate before approval

#### Scenario D3: Duplicate Detection by ServiceId + TicketId (Assurance)
**Preconditions:**
- Assurance order exists with `ServiceId` + `TicketId` combination

**Expected:**
- Same order updated, not duplicated

#### Scenario D4: Secondary Match (Customer + Address + Appointment)
**Preconditions:**
- Order exists with same customer name, address, and appointment date

**Expected:**
- Parser flags as potential duplicate
- Approval may require manual override or update existing

---

### 3.5 Flow E – Status and KPI Defaults

#### Scenario E1: Initial Status = "Pending"
**Expected:**
- New order `Status` = "Pending"
- `StatusReason` = null

#### Scenario E2: Initial Flags All False
**Expected:**
- `HasReschedules` = false
- `RescheduleCount` = 0
- `DocketUploaded` = false
- `PhotosUploaded` = false
- `SerialsValidated` = false
- `InvoiceEligible` = false

#### Scenario E3: KPI Fields Not Set at Creation
**Expected:**
- `KpiCategory` = null (set later by workflow)
- `KpiDueAt` = null (calculated by SLA engine)
- `KpiBreachedAt` = null

#### Scenario E4: Assignment Fields Null at Creation
**Expected:**
- `AssignedSiId` = null
- `AssignedTeamId` = null

---

## 4. Unit Test Specifications

### 4.1 Test Class: `OrderCreationFromDraftTests`

Location: `backend/tests/CephasOps.Application.Tests/Orders/OrderCreationFromDraftTests.cs`

```csharp
public class OrderCreationFromDraftTests
{
    // Happy Path Tests
    [Fact] Task CreateOrder_FromValidFtthDraft_CreatesOrderWithCorrectMappings();
    [Fact] Task CreateOrder_FromValidDigiHsbbDraft_CreatesOrderWithCorrectMappings();
    [Fact] Task CreateOrder_FromValidAssuranceDraft_CreatesOrderWithTicketId();
    [Fact] Task CreateOrder_FromValidModificationDraft_CreatesOrderWithRelocationType();
    
    // Minimal Data Tests
    [Fact] Task CreateOrder_WithMinimalRequiredFields_AppliesDefaults();
    [Fact] Task CreateOrder_WithPhoneNumberNeedingFix_AutoFixesPhone();
    [Theory]
    [InlineData("+60126556688", "0126556688")]
    [InlineData("122164657", "0122164657")]
    [InlineData("016-663-9910", "0166639910")]
    Task CreateOrder_PhoneNumberAutoFix_VariousFormats(string input, string expected);
    
    // Validation Tests
    [Fact] Task CreateOrder_MissingCustomerName_ThrowsValidationException();
    [Fact] Task CreateOrder_MissingAppointmentDate_ThrowsValidationException();
    [Fact] Task CreateOrder_MissingCustomerPhone_ThrowsValidationException();
    [Fact] Task CreateOrder_MissingAddressText_ThrowsValidationException();
    [Fact] Task CreateOrder_MissingServiceIdForActivation_ThrowsValidationException();
    [Fact] Task CreateOrder_MissingTicketIdForAssurance_ThrowsValidationException();
    [Fact] Task CreateOrder_InvalidAppointmentWindowFormat_ThrowsValidationException();
    
    // Idempotency Tests
    [Fact] Task CreateOrder_DraftAlreadyApproved_ReturnsExistingOrder();
    [Fact] Task CreateOrder_DuplicateServiceId_HandlesCorrectly();
    
    // Default Value Tests
    [Fact] Task CreateOrder_SetsSourceSystemToEmailParser();
    [Fact] Task CreateOrder_SetsInitialStatusToPending();
    [Fact] Task CreateOrder_SetsAllBooleanFlagsToFalse();
    [Fact] Task CreateOrder_SetsRescheduleCountToZero();
    [Fact] Task CreateOrder_LeavesKpiFieldsNull();
    [Fact] Task CreateOrder_LeavesAssignmentFieldsNull();
}
```

### 4.2 Test Class: `AppointmentWindowParsingTests`

```csharp
public class AppointmentWindowParsingTests
{
    [Theory]
    [InlineData("09:00-11:00", "09:00:00", "11:00:00")]
    [InlineData("11:00-13:00", "11:00:00", "13:00:00")]
    [InlineData("14:00-16:00", "14:00:00", "16:00:00")]
    [InlineData("9am-11am", "09:00:00", "11:00:00")]
    [InlineData("2pm-4pm", "14:00:00", "16:00:00")]
    Task ParseAppointmentWindow_ValidFormats_ReturnsCorrectTimeSpans(
        string input, string expectedFrom, string expectedTo);
    
    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(null)]
    Task ParseAppointmentWindow_InvalidFormats_ThrowsException(string input);
}
```

### 4.3 Test Class: `AddressParsingTests`

```csharp
public class AddressParsingTests
{
    [Fact] Task ParseAddress_FullMalaysianAddress_ExtractsCityStatePostcode();
    [Fact] Task ParseAddress_WithUnitNumber_ExtractsUnitNo();
    [Fact] Task ParseAddress_WithBuildingName_ExtractsBuildingName();
}
```

---

## 5. Integration Test Specifications

### 5.1 Test Class: `ParserToOrderIntegrationTests`

Location: `backend/tests/CephasOps.Integration.Tests/Parser/ParserToOrderIntegrationTests.cs`

```csharp
public class ParserToOrderIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    // End-to-end flow tests
    [Fact] Task ApproveParsedOrder_ValidDraft_CreatesOrderInDatabase();
    [Fact] Task ApproveParsedOrder_ValidDraft_SetsCreatedOrderIdOnDraft();
    [Fact] Task ApproveParsedOrder_ValidDraft_LinksSourceEmailId();
    [Fact] Task ApproveParsedOrder_ValidDraft_LinksForeignKeys();
    
    // Idempotency tests
    [Fact] Task ApproveParsedOrder_CalledTwice_CreatesOnlyOneOrder();
    [Fact] Task ApproveParsedOrder_DuplicateServiceId_UpdatesExistingOrder();
    
    // Rejection tests
    [Fact] Task ApproveParsedOrder_InvalidDraft_DoesNotCreateOrder();
    [Fact] Task RejectParsedOrder_SetsValidationStatusToRejected();
    
    // Database state assertions
    [Fact] Task ApproveParsedOrder_VerifyOrderTableState();
    [Fact] Task ApproveParsedOrder_VerifyParsedOrderDraftTableState();
}
```

### 5.2 Test Fixtures

```csharp
public class TestDatabaseFixture : IAsyncLifetime
{
    public ApplicationDbContext Context { get; private set; }
    
    public async Task InitializeAsync()
    {
        // Set up in-memory or test database
        // Seed required reference data (Partners, OrderTypes, Buildings)
    }
    
    public async Task DisposeAsync()
    {
        // Clean up
    }
    
    // Helper methods
    public ParsedOrderDraft CreateValidFtthDraft();
    public ParsedOrderDraft CreateValidAssuranceDraft();
    public ParsedOrderDraft CreateInvalidDraft_MissingCustomerName();
    // etc.
}
```

---

## 6. API Test Specifications

### 6.1 Endpoint: `POST /api/parser/parsed-orders/{id}/approve`

```csharp
public class ParserApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact] Task ApproveEndpoint_ValidDraft_Returns200WithOrder();
    [Fact] Task ApproveEndpoint_InvalidDraft_Returns400WithErrors();
    [Fact] Task ApproveEndpoint_NotFound_Returns404();
    [Fact] Task ApproveEndpoint_Unauthorized_Returns401();
    [Fact] Task ApproveEndpoint_ForbiddenRole_Returns403();
    
    // Response structure tests
    [Fact] Task ApproveEndpoint_ResponseIncludesCreatedOrderId();
    [Fact] Task ApproveEndpoint_ResponseIncludesValidationStatus();
}
```

---

## 7. Test Data Examples

### 7.1 Valid TIME FTTH Draft
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "companyId": "company-uuid",
  "parseSessionId": "session-uuid",
  "partnerId": "time-partner-uuid",
  "buildingId": "building-uuid",
  "serviceId": "TBBN620278G",
  "ticketId": null,
  "customerName": "KUAN TE SIANG",
  "customerPhone": "0166587158",
  "addressText": "Block B, Level 33A, Unit 20, UNITED POINT, Jalan Taman Batu Permai, 47400 Petaling Jaya, Selangor",
  "appointmentDate": "2025-11-29T00:00:00Z",
  "appointmentWindow": "11:00-13:00",
  "orderTypeHint": "FTTH",
  "confidenceScore": 0.95,
  "validationStatus": "Pending",
  "validationNotes": null,
  "createdOrderId": null,
  "createdByUserId": null,
  "createdAt": "2025-11-26T10:00:00Z"
}
```

### 7.2 Valid TIME Assurance Draft
```json
{
  "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "serviceId": "TBBN620278G",
  "ticketId": "TTKT202511178606510",
  "customerName": "KUAN TE SIANG",
  "customerPhone": "0166587158",
  "addressText": "Block B, Level 33A, Unit 20, UNITED POINT...",
  "appointmentDate": "2025-11-29T00:00:00Z",
  "appointmentWindow": "11:00-13:00",
  "orderTypeHint": "Assurance",
  "confidenceScore": 0.90
}
```

### 7.3 Invalid Draft – Missing Required Fields
```json
{
  "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
  "serviceId": "TBBN123456A",
  "customerName": null,
  "customerPhone": null,
  "addressText": null,
  "appointmentDate": null,
  "appointmentWindow": null,
  "orderTypeHint": "FTTH"
}
```

---

## 8. Required vs Nice-to-Have Tests

### 8.1 Required for Go-Live (P0)
- [ ] A1: TIME FTTH happy path
- [ ] A3: TIME Assurance happy path
- [ ] B1: Minimal required fields with defaults
- [ ] C1-C5: All mandatory field validation
- [ ] D1: Idempotency (approve twice)
- [ ] E1-E2: Initial status and flags

### 8.2 Nice-to-Have (P1)
- [ ] A2: Digi HSBB happy path
- [ ] B2-B3: Phone number auto-fix variations
- [ ] C6-C8: Edge case validations
- [ ] D2-D4: Advanced duplicate detection
- [ ] E3-E4: KPI and assignment field verification

### 8.3 Extended (P2)
- [ ] Modification order scenarios (Indoor/Outdoor)
- [ ] Celcom HSBB scenarios
- [ ] Address parsing edge cases
- [ ] Performance tests for bulk approval

---

## 9. Implementation Dependencies

Before these tests can be fully executed:

1. **`OrderService.CreateFromParsedDraftAsync()`** method must be implemented
2. **`ParserService.ApproveParsedOrderAsync()`** must call order creation (currently has TODO)
3. **Validation logic** for mandatory fields must be added
4. **Phone number auto-fix** utility must be implemented/integrated
5. **Appointment window parsing** utility must be implemented
6. **Address parsing** for city/state/postcode extraction must be implemented

---

## 10. Related Documentation

- [EMAIL_PARSER.md](../01_system/EMAIL_PARSER.md) – Full parser specification
- [EMAIL_PARSER_VIP_IMPLEMENTATION_SUMMARY.md](./EMAIL_PARSER_VIP_IMPLEMENTATION_SUMMARY.md) – VIP email handling (Phase 3)
- [parser_entities.md](../05_data_model/entities/parser_entities.md) – Data model
- [testing_guidelines.md](../../backend/docs/09_backend/testing_guidelines.md) – Backend testing guidelines

