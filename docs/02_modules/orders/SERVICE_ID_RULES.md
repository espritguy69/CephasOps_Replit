# Service ID Selection Rules

**Last Updated:** December 2025  
**Status:** Production Specification

---

## Overview

CephasOps supports two types of Service IDs for order identification:

1. **TBBN** - TIME direct customer identifiers
2. **Partner Service ID** - Wholesale partner identifiers (Digi, Celcom, U Mobile, etc.)

The system automatically detects the Service ID type and can auto-select the appropriate partner based on the Service ID value, order type, building type, or installation method.

---

## 1. Service ID Types

### 1.1 TBBN (TIME Direct Customers)

**Format:** `TBBN[A-Z]?\d+[A-Z]?`

**Examples:**
- `TBBN1234567`
- `TBBNA12345`
- `TBBNB1234`
- `TBBN12345G`

**Pattern Rules:**
- Must start with "TBBN" (case-insensitive)
- Optional single letter (A, B, etc.) after "TBBN"
- Followed by one or more digits
- Optional single letter suffix

**Auto-Detection:**
- System automatically detects TBBN format using regex pattern: `^TBBN[A-Z]?\d+[A-Z]?$`
- If Service ID starts with "TBBN" but doesn't match exact pattern, still treated as TBBN

**Partner Assignment:**
- TBBN → Automatically selects **TIME** partner (if not already selected)

---

### 1.2 Partner Service ID (Wholesale Partners)

**Supported Partners:**
- **Digi:** `DIGI\d+` or `DIGI00\d+`
- **Celcom:** `CELCOM\d+` or `CELCOM00\d+`
- **CelcomDigi:** `CELCOMDIGI\d+` or `CELCOMDIGI00\d+`
- **U Mobile:** `UMOBILE\d+` or `UMOBILE00\d+`

**Examples:**
- `CELCOM0016996`
- `DIGI0012345`
- `UMOBILE001234`

**Auto-Detection:**
- System matches Service ID against partner-specific patterns
- If no pattern matches but value doesn't start with "TBBN", defaults to Partner Service ID type

**Partner Assignment:**
- Digi patterns → Automatically selects **Digi** partner
- Celcom patterns → Automatically selects **Celcom** partner
- U Mobile patterns → Automatically selects **U Mobile** partner

---

## 2. Auto-Selection Rules

The system automatically selects a partner based on the following priority:

### 2.1 Order Type Based

**Assurance Orders:**
- If Order Type contains "Assurance" → Auto-selects **TIME ASSURANCE** partner

### 2.2 Installation Method / Building Type Based

**FTTO Installation:**
- If Installation Method contains "FTTO" → Auto-selects **TIME FTTO** partner
- If Building Type contains "FTTO" → Auto-selects **TIME FTTO** partner

### 2.3 Service ID Based

**Service ID Detection:**
- System analyzes the Service ID value
- Matches against TBBN or Partner Service ID patterns
- Auto-selects corresponding partner if detected

**Priority:**
1. Order Type (Assurance → TIME ASSURANCE)
2. Installation Method / Building Type (FTTO → TIME FTTO)
3. Service ID pattern matching

---

## 3. UI Behavior

### 3.1 Service ID Input Field

**Label:** "Service ID / TBBN *"

**Helper Text:**
- **TBBN:** "Format: TBBN[A-Z]? + digits (e.g., TBBN12345, TBBNA1234)"
- **Partner Service ID:** "Partner Service ID (e.g., CELCOM0016996, DIGI0012345)"
- **Unknown:** "Enter TBBN (TIME direct) or Partner Service ID (wholesale)"

**Placeholder:**
- TBBN: "TBBN1234567 or TBBNA12345"
- Partner Service ID: "CELCOM0016996 or DIGI0012345"
- Unknown: "TBBN000000 or Partner ID"

**Auto-Detection:**
- As user types, system automatically detects Service ID type
- Type indicator appears next to label: "(TBBN)" or "(Partner Service ID)"
- Partner dropdown auto-populates if detected

---

## 4. Backend Implementation

### 4.1 ServiceIdType Enum

```csharp
public enum ServiceIdType
{
    Tbbn = 1,
    PartnerServiceId = 2
}
```

### 4.2 ServiceIdHelper Utility

**Location:** `CephasOps.Application/Orders/Utilities/ServiceIdHelper.cs`

**Methods:**
- `DetectServiceIdType(string? serviceId)` - Detects type from value
- `DetectPartnerFromServiceId(string? serviceId)` - Detects partner from Service ID
- `AutoSelectPartner(...)` - Auto-selects partner based on multiple factors
- `IsValidServiceId(...)` - Validates Service ID format

### 4.3 Order Entity

**Fields:**
- `ServiceIdType?` - Enum indicating TBBN or Partner Service ID
- `ServiceId` - The actual Service ID value

---

## 5. Parser Integration

### 5.1 Excel Parser

The TIME Excel parser automatically:
- Detects TBBN patterns in Excel files
- Extracts Service ID from "Service ID" or "TBBN" labeled cells
- Falls back to pattern search if label not found

**Pattern Search:**
- Searches entire sheet for pattern: `TBBN[A-Z]?\d{5,}[A-Z]?`
- Matches TBBN with optional letter prefix/suffix

### 5.2 Partner Service ID Detection

For partner orders:
- Parser extracts Partner Service ID from partner-specific fields
- Examples: "Digi Order ID", "Celcom Service ID", etc.

---

## 6. Validation Rules

### 6.1 TBBN Validation

- Must match pattern: `^TBBN[A-Z]?\d+[A-Z]?$`
- Case-insensitive matching
- Minimum 5 digits after "TBBN"

### 6.2 Partner Service ID Validation

- Must not be empty
- Format varies by partner (no strict pattern enforcement)
- System accepts any non-empty value for Partner Service ID type

### 6.3 Required Fields

- **Service ID** is required for all order types (except SDU without Service ID)
- **Service ID Type** is optional but auto-detected when Service ID is provided

---

## 7. Examples

### Example 1: TBBN Detection

**Input:** `TBBN1234567`

**Result:**
- ServiceIdType: `Tbbn`
- Partner: Auto-selected "TIME" (if available)

### Example 2: Partner Service ID Detection

**Input:** `CELCOM0016996`

**Result:**
- ServiceIdType: `PartnerServiceId`
- Partner: Auto-selected "Celcom" (if available)

### Example 3: Assurance Order

**Input:**
- Order Type: "Assurance"
- Service ID: `TBBN1234567`

**Result:**
- ServiceIdType: `Tbbn`
- Partner: Auto-selected "TIME ASSURANCE" (priority: Order Type over Service ID)

### Example 4: FTTO Installation

**Input:**
- Installation Method: "FTTO"
- Service ID: `TBBN1234567`

**Result:**
- ServiceIdType: `Tbbn`
- Partner: Auto-selected "TIME FTTO" (priority: Installation Method over Service ID)

---

## 8. Related Documentation

- [Orders Overview](./OVERVIEW.md) - Complete order specification
- [Settings Module](../global_settings/SETTINGS_MODULE.md) - Partner configuration
- [Email Parser](../email_parser/SPECIFICATION.md) - Service ID extraction from emails
- [Order Lifecycle](../../01_system/ORDER_LIFECYCLE.md) - Order status workflow

---

**Last updated for Unified Order Workflow + FTTO Integration**

