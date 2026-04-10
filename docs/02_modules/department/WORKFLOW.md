# Departments – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Departments module, covering department creation, material allocations, cost center mapping, and filtering

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       DEPARTMENTS MODULE SYSTEM                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   DEPARTMENT CREATION  │      │   DEPARTMENT CONFIG   │
        │  (Setup, Validation)   │      │  (Allocations, Costs)  │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Create Department    │      │ • Material Allocations│
        │ • Link Cost Centre     │      │ • Cost Center Mapping │
        │ • Set Active Status    │      │ • Department Filtering│
        │ • Assign Users         │      │ • Department Context  │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   DEPARTMENT FILTERING │      │   DEPARTMENT MEMBERSHIP │
        │  (Data Scoping)        │      │  (User Assignment)     │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Department Setup

```
[STEP 1: DEPARTMENT CREATION]
         |
         v
┌────────────────────────────────────────┐
│ CREATE DEPARTMENT REQUEST                 │
│ POST /api/departments                    │
└────────────────────────────────────────┘
         |
         v
CreateDepartmentDto {
  Name: "Operations"
  Code: "OPS"
  Description: "GPON Operations Department"
  CostCentreId: "costcentre-123"
  IsActive: true
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE DEPARTMENT DATA                  │
│ DepartmentService.CreateDepartmentAsync() │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Return Validation Errors]
   |
   v
Checks:
  ✓ Name not empty
  ✓ Code not empty
  ✓ Code unique per company
  ✓ CostCentreId exists (if provided)
         |
         v
┌────────────────────────────────────────┐
│ CREATE DEPARTMENT RECORD                  │
└────────────────────────────────────────┘
         |
         v
Department {
  Id: Guid.NewGuid()
  CompanyId: Cephas
  Name: "Operations"
  Code: "OPS"
  Description: "GPON Operations Department"
  CostCentreId: "costcentre-123"
  IsActive: true
  CreatedAt: DateTime.UtcNow
  UpdatedAt: DateTime.UtcNow
}
         |
         v
[Save to Database]
  _context.Departments.Add(department)
  await _context.SaveChangesAsync()
         |
         v
[STEP 2: MATERIAL ALLOCATION]
         |
         v
┌────────────────────────────────────────┐
│ ALLOCATE MATERIALS TO DEPARTMENT          │
│ POST /api/departments/{id}/materials     │
└────────────────────────────────────────┘
         |
         v
MaterialAllocationDto {
  MaterialTemplateId: "template-123"
  AllocationPercent: 100
  IsActive: true
}
         |
         v
[Create Material Allocation]
  MaterialAllocation {
    Id: Guid.NewGuid()
    CompanyId: Cephas
    DepartmentId: department.Id
    MaterialTemplateId: "template-123"
    AllocationPercent: 100
    IsActive: true
  }
         |
         v
[Save Allocation]
  _context.MaterialAllocations.Add(allocation)
  await _context.SaveChangesAsync()
         |
         v
[STEP 3: USER ASSIGNMENT]
         |
         v
┌────────────────────────────────────────┐
│ ASSIGN USER TO DEPARTMENT                 │
│ POST /api/departments/{id}/members       │
└────────────────────────────────────────┘
         |
         v
DepartmentMembershipDto {
  UserId: "user-456"
  IsPrimary: true
}
         |
         v
[Create Membership]
  DepartmentMembership {
    Id: Guid.NewGuid()
    DepartmentId: department.Id
    UserId: "user-456"
    IsPrimary: true
    CreatedAt: DateTime.UtcNow
  }
         |
         v
[Save Membership]
  _context.DepartmentMemberships.Add(membership)
  await _context.SaveChangesAsync()
         |
         v
[STEP 4: DEPARTMENT CONTEXT ACTIVATION]
         |
         v
[User Sets Active Department]
  DepartmentContext.SetActiveDepartment(departmentId)
         |
         v
[Department Context Set]
  ICurrentUserService.DepartmentId = departmentId
         |
         v
[API Client Injects Department]
  GET /api/orders?departmentId=dept-123
         |
         v
[Data Filtered by Department]
  Orders.find(DepartmentId = dept-123)
```

---

## Department Filtering Workflow

```
[User Makes API Request]
  GET /api/orders
  Headers: {
    Authorization: "Bearer {token}"
  }
         |
         v
┌────────────────────────────────────────┐
│ GET ACTIVE DEPARTMENT                    │
│ DepartmentContext.GetActiveDepartment()  │
└────────────────────────────────────────┘
         |
         v
[Active Department]
  DepartmentId: "dept-123" (Operations)
         |
         v
┌────────────────────────────────────────┐
│ INJECT DEPARTMENT FILTER                 │
│ API Client.ensureDepartmentParam()        │
└────────────────────────────────────────┘
         |
         v
[Add Department to Query]
  GET /api/orders?departmentId=dept-123
         |
         v
┌────────────────────────────────────────┐
│ FILTER DATA BY DEPARTMENT                 │
│ OrderService.GetOrdersAsync()            │
└────────────────────────────────────────┘
         |
         v
[Query Orders]
  Orders.find(
    CompanyId = Cephas
    DepartmentId = dept-123
  )
         |
         v
[Return Filtered Results]
  Only orders in Operations department
```

---

## Material Allocation Workflow

```
[Order Uses Material]
  Order {
    DepartmentId: "dept-123" (Operations)
    MaterialTemplateId: "template-123"
  }
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE MATERIAL ALLOCATION                │
│ DepartmentService.GetMaterialAllocation() │
└────────────────────────────────────────┘
         |
         v
[Query Material Allocation]
  MaterialAllocation.find(
    DepartmentId = dept-123
    MaterialTemplateId = template-123
    IsActive = true
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Use Default Allocation]
   |           AllocationPercent: 100
   |
   v
[Allocation Found]
  AllocationPercent: 100
         |
         v
[Apply to PNL Calculation]
  Material Cost: RM 100.00
  Allocation: 100% to Operations Department
  Cost Centre: Operations Cost Centre
```

---

## Cost Center Mapping Workflow

```
[Department Has Cost Centre]
  Department {
    CostCentreId: "costcentre-123"
  }
         |
         v
┌────────────────────────────────────────┐
│ GET COST CENTRE DETAILS                   │
└────────────────────────────────────────┘
         |
         v
[Query Cost Centre]
  CostCentre.find(Id = costcentre-123)
         |
         v
CostCentre {
  Id: "costcentre-123"
  Code: "ISP_OPS"
  Name: "ISP Operations"
  CompanyId: Cephas
}
         |
         v
[Use in PNL Reporting]
  PnlDetailPerOrder {
    CostCentreId: "costcentre-123"
    CostCentreCode: "ISP_OPS"
    DepartmentId: dept-123
  }
```

---

## Entities Involved

### Department Entity
```
Department
├── Id (Guid)
├── CompanyId (Guid)
├── Name (string)
├── Code (string, unique per company)
├── Description (string?)
├── CostCentreId (Guid?)
├── IsActive (bool)
├── CreatedAt (DateTime)
└── UpdatedAt (DateTime)
```

### DepartmentMembership Entity
```
DepartmentMembership
├── Id (Guid)
├── DepartmentId (Guid)
├── UserId (Guid)
├── IsPrimary (bool)
└── CreatedAt (DateTime)
```

### MaterialAllocation Entity
```
MaterialAllocation
├── Id (Guid)
├── CompanyId (Guid)
├── DepartmentId (Guid)
├── MaterialTemplateId (Guid)
├── AllocationPercent (decimal?)
└── IsActive (bool)
```

---

## API Endpoints Involved

### Department Management
- `GET /api/departments` - List departments (optional: `?isActive=true`)
- `GET /api/departments/{id}` - Get department details
- `POST /api/departments` - Create department
- `PUT /api/departments/{id}` - Update department
- `DELETE /api/departments/{id}` - Delete department (soft delete)

### Department Members
- `GET /api/departments/{id}/members` - List department members
- `POST /api/departments/{id}/members` - Add member to department
- `DELETE /api/departments/{id}/members/{userId}` - Remove member

### Material Allocations
- `GET /api/departments/{id}/materials` - List material allocations
- `POST /api/departments/{id}/materials` - Allocate material to department
- `PUT /api/departments/{id}/materials/{allocationId}` - Update allocation
- `DELETE /api/departments/{id}/materials/{allocationId}` - Remove allocation

---

## Module Rules & Validations

### Department Creation Rules
- Name is required
- Code is required and must be unique per company
- CostCentreId must exist (if provided)
- Department code cannot be changed after creation

### Material Allocation Rules
- AllocationPercent must be 0-100 (or null for 100%)
- Multiple allocations for same material must sum to 100%
- Only one active allocation per (Department, MaterialTemplate)

### Department Membership Rules
- User can belong to multiple departments
- One department can be marked as primary
- Department membership affects data visibility

### Filtering Rules
- Active department determines data scope
- SuperAdmin/Admin can see all departments
- Regular users see only their department's data
- Department filter automatically injected in API calls

---

## Integration Points

### Orders Module
- Orders belong to departments
- Department filtering affects order visibility
- Department context set per user

### PNL Module
- Material costs allocated by department
- Cost centers linked to departments
- Department-level PNL reporting

### Materials Module
- Material allocations per department
- Department-specific material access
- Material cost allocation

### Users Module
- Users assigned to departments
- Department membership affects permissions
- Primary department for default context

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/department/OVERVIEW.md` - Department module overview
- `docs/02_modules/department/FILTERING.md` - Department filtering details

