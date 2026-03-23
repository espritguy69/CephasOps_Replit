# Company, Department, Partner Group & Partner Relationships

**Date:** December 12, 2025  
**Purpose:** Visual representation of entity relationships in CephasOps

---

## Quick Visual Overview

```
                    COMPANY (Single)
                         |
        ┌────────────────┴────────────────┐
        |                                  |
    DEPARTMENT                        PARTNER GROUP
        |                                  |
        | DepartmentId (opt)               | GroupId (opt)
        |                                  |
        └──────────────┬───────────────────┘
                       |
                  PARTNER
        (can have both GroupId and DepartmentId)
```

## Entity Relationship Diagram (ASCII)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         CEPHASOPS (Single Company)                       │
│                    (CompanyId used for backward compatibility)            │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ CompanyId (nullable)
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │     DEPARTMENT        │      │    PARTNER GROUP      │
        │  (CompanyScopedEntity)│      │  (CompanyScopedEntity)│
        ├───────────────────────┤      ├───────────────────────┤
        │ • Id (Guid)           │      │ • Id (Guid)           │
        │ • CompanyId (Guid?)   │      │ • CompanyId (Guid?)   │
        │ • Name                 │      │ • Name                │
        │ • Code                 │      │                       │
        │ • CostCentreId (Guid?)│      │                       │
        │ • IsActive            │      │                       │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    │ DepartmentId (optional)       │ GroupId
                    │                               │
                    │                               │
                    ▼                               ▼
        ┌───────────────────────────────────────────────────────┐
        │                    PARTNER                            │
        │              (CompanyScopedEntity)                     │
        ├───────────────────────────────────────────────────────┤
        │ • Id (Guid)                                           │
        │ • CompanyId (Guid?)                                   │
        │ • Name                                                │
        │ • PartnerType (Telco, Customer, Vendor, Landlord)    │
        │ • GroupId (Guid?) → Links to PartnerGroup            │
        │ • DepartmentId (Guid?) → Links to Department         │
        │ • IsActive                                            │
        └───────────────────────────────────────────────────────┘
```

---

## Relationship Flowchart

```
                    ┌─────────────┐
                    │   COMPANY   │
                    │  (Single)   │
                    └──────┬──────┘
                           │
                           │ CompanyId (nullable)
                           │
        ┌──────────────────┴──────────────────┐
        │                                      │
        ▼                                      ▼
┌───────────────┐                    ┌───────────────┐
│  DEPARTMENT   │                    │ PARTNER GROUP │
│               │                    │               │
│ • GPON        │                    │ • TIME        │
│ • NWO         │                    │ • CELCOM_DIGI │
│ • CWO         │                    │ • U_MOBILE    │
│ • BARBERSHOP  │                    │ • etc.        │
│ • etc.        │                    │               │
└───────┬───────┘                    └───────┬───────┘
        │                                    │
        │ DepartmentId (optional)          │ GroupId
        │                                    │
        │                                    │
        └──────────────┬─────────────────────┘
                       │
                       ▼
              ┌─────────────────┐
              │    PARTNER      │
              │                 │
              │ Can belong to:  │
              │ • 1 PartnerGroup│
              │ • 1 Department  │
              │ (both optional) │
              │                 │
              │ Examples:       │
              │ • TM Direct     │
              │   (Group: TIME) │
              │ • Celcom Fibre  │
              │   (Group: CELCOM_DIGI)│
              │ • Digi SME      │
              │   (Group: CELCOM_DIGI)│
              └─────────────────┘
```

---

## Detailed Relationship Matrix

### 1. Company → Department
- **Relationship:** One-to-Many (1:N)
- **Link:** `Department.CompanyId` → `Company.Id`
- **Note:** In single-company mode, `CompanyId` is nullable but used for filtering
- **Example:**
  - Company: Cephas
  - Departments: GPON, NWO, CWO, BARBERSHOP, TRAVEL_OPS

### 2. Company → PartnerGroup
- **Relationship:** One-to-Many (1:N)
- **Link:** `PartnerGroup.CompanyId` → `Company.Id`
- **Example:**
  - Company: Cephas
  - Partner Groups: TIME, CELCOM_DIGI, U_MOBILE

### 3. PartnerGroup → Partner
- **Relationship:** One-to-Many (1:N)
- **Link:** `Partner.GroupId` → `PartnerGroup.Id`
- **Note:** Partner can optionally belong to a group
- **Example:**
  - PartnerGroup: TIME
    - Partners: TM Direct, TM Wholesale
  - PartnerGroup: CELCOM_DIGI
    - Partners: Celcom Fibre, Digi SME, Digi Enterprise

### 4. Department → Partner
- **Relationship:** One-to-Many (1:N)
- **Link:** `Partner.DepartmentId` → `Department.Id`
- **Note:** Partner can optionally be assigned to a department
- **Example:**
  - Department: GPON
    - Partners: TM Direct, Celcom Fibre, Digi SME
  - Department: NWO
    - Partners: TM Direct, U Mobile

### 5. Partner → PartnerGroup + Department
- **Relationship:** Many-to-One for both (M:1)
- **Link:** 
  - `Partner.GroupId` → `PartnerGroup.Id` (optional)
  - `Partner.DepartmentId` → `Department.Id` (optional)
- **Note:** A Partner can belong to BOTH a PartnerGroup AND a Department simultaneously
- **Example:**
  - Partner: "TM Direct"
    - GroupId: TIME (PartnerGroup)
    - DepartmentId: GPON (Department)

---

## Real-World Example Structure

```
COMPANY: Cephas
│
├─── DEPARTMENT: GPON
│    │
│    ├─── PARTNER GROUP: TIME
│    │    ├─── Partner: TM Direct (DepartmentId=GPON, GroupId=TIME)
│    │    └─── Partner: TM Wholesale (GroupId=TIME)
│    │
│    ├─── PARTNER GROUP: CELCOM_DIGI
│    │    ├─── Partner: Celcom Fibre (DepartmentId=GPON, GroupId=CELCOM_DIGI)
│    │    └─── Partner: Digi SME (DepartmentId=GPON, GroupId=CELCOM_DIGI)
│    │
│    └─── PARTNER: U Mobile Direct (DepartmentId=GPON, GroupId=U_MOBILE)
│
├─── DEPARTMENT: NWO
│    │
│    ├─── PARTNER GROUP: TIME
│    │    └─── Partner: TM Direct (DepartmentId=NWO, GroupId=TIME)
│    │
│    └─── PARTNER GROUP: U_MOBILE
│         └─── Partner: U Mobile Direct (DepartmentId=NWO, GroupId=U_MOBILE)
│
└─── DEPARTMENT: CWO
     │
     └─── PARTNER GROUP: TIME
          └─── Partner: TM Direct (DepartmentId=CWO, GroupId=TIME)
```

---

## Key Points

1. **Single Company Mode:**
   - All entities have `CompanyId` (nullable) for backward compatibility
   - In practice, all entities belong to the same company
   - `CompanyId` filtering is used for data isolation

2. **Department Assignment:**
   - Partners can be assigned to specific departments via `Partner.DepartmentId`
   - This allows department-specific partner management
   - Example: TM Direct can work with GPON, NWO, and CWO departments

3. **Partner Group Assignment:**
   - Partners can belong to a PartnerGroup via `Partner.GroupId`
   - Used for rate card grouping and billing
   - Example: All Celcom/Digi partners grouped under CELCOM_DIGI

4. **Dual Assignment:**
   - A Partner can have BOTH:
     - A `GroupId` (for rate grouping)
     - A `DepartmentId` (for operational assignment)
   - Example: "Celcom Fibre" can be in CELCOM_DIGI group AND GPON department

5. **Optional Relationships:**
   - Both `GroupId` and `DepartmentId` are optional on Partner
   - Partners can exist without group or department assignment
   - This provides flexibility for edge cases

---

## Database Schema Summary

### Tables:
- `Companies` - Single company (Cephas)
- `Departments` - GPON, NWO, CWO, etc.
- `PartnerGroups` - TIME, CELCOM_DIGI, U_MOBILE, etc.
- `Partners` - Individual partners with optional GroupId and DepartmentId

### Foreign Keys:
- `Departments.CompanyId` → `Companies.Id` (nullable)
- `PartnerGroups.CompanyId` → `Companies.Id` (nullable)
- `Partners.CompanyId` → `Companies.Id` (nullable)
- `Partners.GroupId` → `PartnerGroups.Id` (nullable)
- `Partners.DepartmentId` → `Departments.Id` (nullable)

---

## Usage in Rate Cards & Billing

Rate cards use this hierarchy for rate resolution:

```
Rate Lookup Priority:
1. Department + PartnerGroup + OrderType + InstallationMethod
2. Department + Partner + OrderType + InstallationMethod (override)
3. Department + OrderType + InstallationMethod (default)
```

Example:
- GPON Department
  - TIME PartnerGroup → Base rate for all TIME partners
  - TM Direct Partner → Override rate (if exists)
  - Default → Fallback rate

---

**Last Updated:** December 12, 2025

