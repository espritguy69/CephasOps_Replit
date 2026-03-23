# Service Installers – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Service Installers module, covering SI creation, deduplication, assignment, job completion tracking, and material usage

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    SERVICE INSTALLERS MODULE SYSTEM                      │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   SERVICE INSTALLERS   │      │   SI CONTACTS          │
        │  (Field Workers)       │      │  (Emergency Contacts) │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Name                 │      │ • Name                 │
        │ • Employee ID          │      │ • Phone                │
        │ • Phone/Email          │      │ • Email                │
        │ • SI Level             │      │ • Contact Type         │
        │ • Department           │      │ • Is Primary           │
        │ • Is Subcontractor     │      └───────────────────────┘
        └───────────────────────┘
                    │
                    ▼
        ┌───────────────────────┐
        │   INVENTORY LOCATION   │
        │  (Auto-Created)        │
        └───────────────────────┘
```

---

## Complete Workflow: Service Installer Management

```
[STEP 1: CREATE SERVICE INSTALLER]
         |
         v
┌────────────────────────────────────────┐
│ CREATE SERVICE INSTALLER                  │
│ POST /api/service-installers              │
└────────────────────────────────────────┘
         |
         v
CreateServiceInstallerDto {
  Name: "Ahmad bin Abdullah"
  EmployeeId: "SI-001"
  Phone: "0123456789"
  Email: "ahmad@example.com"
  SiLevel: "Senior"
  DepartmentId: "dept-123"
  IsSubcontractor: false
  IsActive: true
}
         |
         v
┌────────────────────────────────────────┐
│ NORMALIZE INPUT DATA                     │
└────────────────────────────────────────┘
         |
         v
Normalized Data {
  Name: "ahmad bin abdullah" (trimmed, lowercased for comparison)
  Phone: "0123456789" (digits only)
  Email: "ahmad@example.com" (lowercased, trimmed)
  EmployeeId: "SI-001" (uppercased)
}
         |
         v
┌────────────────────────────────────────┐
│ CHECK FOR DUPLICATES                     │
└────────────────────────────────────────┘
         |
         v
[Check 1: Exact Employee ID Match]
  ServiceInstaller.find(
    EmployeeId = "SI-001"
    CompanyId = Cephas
    IsDeleted = false
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Check 2: Normalized Name + Phone/Email]
   |           ServiceInstaller.find(
   |             NormalizedName = "ahmad bin abdullah"
   |             AND (NormalizedPhone = "0123456789" OR NormalizedEmail = "ahmad@example.com")
   |             CompanyId = Cephas
   |             IsDeleted = false
   |           )
   |
   v
[Reject: Duplicate Employee ID]
  Error: "A service installer with employee ID 'SI-001' already exists"
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Check 3: Fuzzy Name Match (85% similarity)]
   |           For each existing SI:
   |             NameSimilarity = CalculateSimilarity("ahmad bin abdullah", existing.Name)
   |             IF NameSimilarity >= 0.85 AND (Phone matches OR Email matches):
   |               → Potential duplicate
   |
   v
[Reject: Duplicate Name + Contact]
  Error: "A service installer with similar name and contact details already exists"
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NO DUPLICATE]
   |            |
   |            v
   |       [Proceed with Creation]
   |
   v
[Reject: Potential Duplicate]
  Error: "A potentially duplicate service installer found: 'Ahmad Abdullah' (Similarity: 90%)"
         |
         v
┌────────────────────────────────────────┐
│ CREATE SERVICE INSTALLER                  │
└────────────────────────────────────────┘
         |
         v
ServiceInstaller {
  Id: "SI-123"
  CompanyId: Cephas
  DepartmentId: "dept-123"
  Name: "Ahmad bin Abdullah"
  EmployeeId: "SI-001"
  Phone: "0123456789"
  Email: "ahmad@example.com"
  SiLevel: "Senior"
  IsSubcontractor: false
  IsActive: true
  CreatedAt: 2025-12-12
}
         |
         v
┌────────────────────────────────────────┐
│ AUTO-CREATE STOCK LOCATION                │
│ LocationAutoCreateService.CreateLocationForSI()│
└────────────────────────────────────────┘
         |
         v
StockLocation {
  Id: "location-456"
  CompanyId: Cephas
  Name: "SI-123 - Ahmad bin Abdullah"
  Type: "SI"
  LinkedServiceInstallerId: "SI-123"
}
         |
         v
[Service Installer Created]
         |
         v
[STEP 2: UPDATE SERVICE INSTALLER]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE SERVICE INSTALLER                  │
│ PUT /api/service-installers/{id}          │
└────────────────────────────────────────┘
         |
         v
[Re-run Duplicate Checks]
  - Check Employee ID (if changed)
  - Check Normalized Name + Phone/Email (if changed)
  - Check Fuzzy Name Match (if name changed)
         |
         v
[Update Service Installer]
  ServiceInstaller {
    Name: "Ahmad Abdullah" (updated)
    Phone: "0123456789" (updated)
    ...
  }
         |
         v
[STEP 3: ASSIGNMENT TO ORDERS]
         |
         v
[Order Assignment]
  Order {
    AssignedSiId: "SI-123"
    Status: "Assigned"
  }
         |
         v
[Scheduled Slot Created]
  ScheduledSlot {
    ServiceInstallerId: "SI-123"
    OrderId: "order-456"
    Date: 2025-12-15
  }
         |
         v
[STEP 4: JOB COMPLETION]
         |
         v
[SI Completes Job]
  Order {
    Status: "OrderCompleted"
    AssignedSiId: "SI-123"
  }
         |
         v
[Material Usage Recorded]
  OrderMaterialUsage {
    OrderId: "order-456"
    MaterialId: "ONU-HG8240H"
    ActualQuantity: 1
    SerialNumber: "SN001"
    IsUsed: true
  }
         |
         v
[Inventory Updated]
  SerializedItem {
    SerialNumber: "SN001"
    Status: "InstalledAtCustomer"
    LastOrderId: "order-456"
  }
```

---

## Deduplication Flow

```
[Service Installer Creation/Update]
         |
         v
┌────────────────────────────────────────┐
│ STEP 1: NORMALIZE DATA                    │
└────────────────────────────────────────┘
         |
         v
NameNormalizer.Normalize(name)
  - Trim whitespace
  - Remove extra spaces
  - Convert to lowercase (for comparison)
         |
         v
PhoneNumberNormalizer.Normalize(phone)
  - Remove non-digits
  - Standardize prefixes
         |
         v
EmailNormalizer.Normalize(email)
  - Trim whitespace
  - Convert to lowercase
         |
         v
┌────────────────────────────────────────┐
│ STEP 2: CHECK EXACT EMPLOYEE ID            │
└────────────────────────────────────────┘
         |
         v
[If EmployeeId provided]
  ServiceInstaller.find(
    EmployeeId = normalizedEmployeeId (uppercased)
    CompanyId = companyId
    IsDeleted = false
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Continue to Step 3]
   |
   v
[Reject: Exact Employee ID Match]
  Error: "A service installer with employee ID 'SI-001' already exists: Ahmad Abdullah (ID: SI-123)"
         |
         v
┌────────────────────────────────────────┐
│ STEP 3: CHECK NORMALIZED NAME + CONTACT    │
└────────────────────────────────────────┘
         |
         v
[For each existing SI]
  IF NormalizedName matches AND (Phone matches OR Email matches):
    → Duplicate found
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Continue to Step 4]
   |
   v
[Reject: Normalized Match]
  Error: "A service installer with similar name and contact details already exists: Ahmad Abdullah (ID: SI-123)"
         |
         v
┌────────────────────────────────────────┐
│ STEP 4: CHECK FUZZY NAME MATCH (85% threshold)│
└────────────────────────────────────────┘
         |
         v
[For each existing SI]
  NameSimilarity = NameNormalizer.CalculateSimilarity(
    normalizedName,
    existingNormalizedName
  )
         |
         v
  IF NameSimilarity >= 0.85 AND (Phone matches OR Email matches):
    → Potential duplicate
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [No Duplicate - Proceed]
   |
   v
[Reject: Fuzzy Match]
  Error: "A potentially duplicate service installer found: 'Ahmad Abdullah' (ID: SI-123, Similarity: 90%). Please verify if this is the same person."
```

---

## Contact Management Flow

```
[STEP 1: CREATE SI CONTACT]
         |
         v
┌────────────────────────────────────────┐
│ CREATE CONTACT                            │
│ POST /api/service-installers/{id}/contacts│
└────────────────────────────────────────┘
         |
         v
CreateServiceInstallerContactDto {
  Name: "Emergency Contact"
  Phone: "0198765432"
  Email: "emergency@example.com"
  ContactType: "Emergency"
  IsPrimary: true
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE CONTACT                         │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Reject: Duplicate Contact]
   |    Error: "A contact with the same type and contact details already exists"
   |
   v
Checks:
  ✓ SI exists
  ✓ No duplicate (ContactType + Phone/Email)
         |
         v
┌────────────────────────────────────────┐
│ CREATE CONTACT                            │
└────────────────────────────────────────┘
         |
         v
ServiceInstallerContact {
  Id: "contact-789"
  CompanyId: Cephas
  ServiceInstallerId: "SI-123"
  Name: "Emergency Contact"
  Phone: "0198765432"
  Email: "emergency@example.com"
  ContactType: "Emergency"
  IsPrimary: true
}
```

---

## Entities Involved

### ServiceInstaller Entity
```
ServiceInstaller
├── Id (Guid)
├── CompanyId (Guid)
├── DepartmentId (Guid?)
├── Name (string)
├── EmployeeId (string?)
├── Phone (string)
├── Email (string?)
├── SiLevel (string: Junior, Senior, Lead)
├── IsSubcontractor (bool)
├── IsActive (bool)
├── UserId (Guid?, linked user account)
└── CreatedAt, UpdatedAt
```

### ServiceInstallerContact Entity
```
ServiceInstallerContact
├── Id (Guid)
├── CompanyId (Guid)
├── ServiceInstallerId (Guid)
├── Name (string)
├── Phone (string?)
├── Email (string?)
├── ContactType (string: Emergency, Family, etc.)
├── IsPrimary (bool)
└── CreatedAt, UpdatedAt
```

---

## API Endpoints Involved

### Service Installers
- `GET /api/service-installers` - List service installers with filters
- `GET /api/service-installers/{id}` - Get SI details
- `POST /api/service-installers` - Create service installer
- `PUT /api/service-installers/{id}` - Update service installer
- `DELETE /api/service-installers/{id}` - Delete service installer

### SI Contacts
- `GET /api/service-installers/{id}/contacts` - Get SI contacts
- `POST /api/service-installers/{id}/contacts` - Create contact
- `PUT /api/service-installers/{id}/contacts/{contactId}` - Update contact
- `DELETE /api/service-installers/{id}/contacts/{contactId}` - Delete contact

---

## Module Rules & Validations

### Service Installer Creation Rules
- Name is required
- Phone is required
- Employee ID must be unique per company (if provided)
- Name + Phone/Email combination must be unique (normalized)
- Fuzzy name matching (85% threshold) with same phone/email triggers warning
- Department must exist (if provided)
- SI Level must be valid (Junior, Senior, Lead)

### Deduplication Rules
- **Exact Employee ID Match**: Rejects immediately if Employee ID matches
- **Normalized Match**: Rejects if normalized name matches AND (phone OR email matches)
- **Fuzzy Match**: Warns if name similarity >= 85% AND (phone OR email matches)
- All checks are case-insensitive and normalize whitespace

### Contact Management Rules
- Contact Type + Phone/Email combination must be unique per SI
- At least one contact method (Phone or Email) required
- Multiple primary contacts allowed (no single primary constraint)

### Inventory Integration Rules
- Stock location auto-created on SI creation
- Location name: "SI-{Id} - {Name}"
- Location type: "SI"
- Location linked to ServiceInstallerId

### Assignment Rules
- Only active SIs can be assigned to orders
- SI availability checked in Scheduler module
- SI capacity (MaxJobs) enforced in Scheduler

---

## Integration Points

### Orders Module
- Orders assigned to SIs via AssignedSiId
- Order completion tracked per SI
- Material usage recorded per SI

### Scheduler Module
- ScheduledSlots linked to ServiceInstallerId
- SI availability managed in Scheduler
- SI capacity and working hours checked

### Inventory Module
- Stock location auto-created for each SI
- Material movements tracked to/from SI locations
- SI in-hand stock managed

### Payroll Module
- SI details (SiLevel) used for rate resolution
- SI rate plans provide bonus/penalty amounts
- Job earning records linked to ServiceInstallerId

### KPI Module
- SI performance evaluated per order
- KPI results affect payroll adjustments
- SI-level performance metrics tracked

### Departments Module
- SIs assigned to departments
- Department filtering applied
- Department material allocations considered

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/service_installer/OVERVIEW.md` - Service Installers overview
- `docs/02_modules/scheduler/WORKFLOW.md` - Scheduler workflow
- `docs/02_modules/payroll/WORKFLOW.md` - Payroll workflow

