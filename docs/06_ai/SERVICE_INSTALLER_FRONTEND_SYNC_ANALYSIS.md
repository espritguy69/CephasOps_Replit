# Service Installer Frontend Sync - Inventory & Gap Analysis

**Date:** 2026-01-05  
**Objective:** Analyze backend Service Installer implementation and identify frontend gaps

---

## 1. BACKEND INVENTORY

### 1.1 Entity Structure

**File:** `backend/src/CephasOps.Domain/ServiceInstallers/Entities/ServiceInstaller.cs`

**Properties:**
- `Id` (Guid) - Primary key
- `CompanyId` (Guid) - Company scope (from CompanyScopedEntity)
- `DepartmentId` (Guid?) - Optional department assignment
- `Name` (string, required, max 200) - Installer name
- `EmployeeId` (string?, max 50) - Employee ID
- `Phone` (string?, max 50) - Phone number
- `Email` (string?, max 255) - Email address
- `SiLevel` (string, required, max 50) - Level (Junior, Senior, Subcon)
- `IsSubcontractor` (bool) - Whether subcontractor
- `IsActive` (bool) - Active status
- `UserId` (Guid?) - Link to User if SI has login access
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime?) - Update timestamp
- `Contacts` (ICollection<ServiceInstallerContact>) - Related contacts

**Table Name:** `ServiceInstallers`

**Indexes:**
- `IX_ServiceInstallers_CompanyId_EmployeeId`
- `IX_ServiceInstallers_CompanyId_IsActive`
- `IX_ServiceInstallers_UserId`

---

### 1.2 Controller Endpoints

**File:** `backend/src/CephasOps.Api/Controllers/ServiceInstallersController.cs`  
**Base Route:** `/api/service-installers`

**Endpoints:**

| Method | Route | Description | Request Body | Response |
|--------|-------|-------------|--------------|----------|
| GET | `/api/service-installers` | Get all service installers | Query: `departmentId?`, `isActive?` | `List<ServiceInstallerDto>` |
| GET | `/api/service-installers/{id}` | Get service installer by ID | - | `ServiceInstallerDto` |
| POST | `/api/service-installers` | Create new service installer | `CreateServiceInstallerDto` | `ServiceInstallerDto` (201) |
| PUT | `/api/service-installers/{id}` | Update service installer | `UpdateServiceInstallerDto` | `ServiceInstallerDto` |
| DELETE | `/api/service-installers/{id}` | Delete service installer | - | 204 No Content |
| GET | `/api/service-installers/{serviceInstallerId}/contacts` | Get contacts for SI | - | `List<ServiceInstallerContactDto>` |
| POST | `/api/service-installers/{serviceInstallerId}/contacts` | Create contact for SI | `CreateServiceInstallerContactDto` | `ServiceInstallerContactDto` (201) |
| PUT | `/api/service-installers/contacts/{contactId}` | Update contact | `UpdateServiceInstallerContactDto` | `ServiceInstallerContactDto` |
| DELETE | `/api/service-installers/contacts/{contactId}` | Delete contact | - | 204 No Content |
| GET | `/api/service-installers/export` | Export to CSV | Query: `departmentId?`, `isActive?` | CSV file |
| GET | `/api/service-installers/template` | Download CSV template | - | CSV file |
| POST | `/api/service-installers/import` | Import from CSV | `IFormFile` | `ImportResult` (NOT IMPLEMENTED) |

---

### 1.3 DTOs

**File:** `backend/src/CephasOps.Application/ServiceInstallers/DTOs/ServiceInstallerDto.cs`

**DTOs:**
1. **ServiceInstallerDto** (Response)
   - `Id`, `CompanyId`, `DepartmentId`, `DepartmentName`
   - `Name`, `EmployeeId`, `Phone`, `Email`
   - `SiLevel`, `IsSubcontractor`, `IsActive`
   - `UserId`, `CreatedAt`

2. **CreateServiceInstallerDto** (Request)
   - `DepartmentId?`, `Name` (required)
   - `EmployeeId?`, `Phone?`, `Email?`
   - `SiLevel` (default: "Junior")
   - `IsSubcontractor`, `IsActive` (default: true)
   - `UserId?`

3. **UpdateServiceInstallerDto** (Request)
   - All fields optional (nullable)

4. **ServiceInstallerContactDto** (Response)
   - `Id`, `ServiceInstallerId`
   - `Name`, `Phone?`, `Email?`
   - `ContactType` (default: "Backup")
   - `IsPrimary`

5. **CreateServiceInstallerContactDto** (Request)
   - `Name` (required)
   - `Phone?`, `Email?`
   - `ContactType` (default: "Backup")
   - `IsPrimary`

6. **UpdateServiceInstallerContactDto** (Request)
   - All fields optional

---

### 1.4 Service Layer Methods

**File:** `backend/src/CephasOps.Application/ServiceInstallers/Services/IServiceInstallerService.cs`

**Interface Methods:**
- `GetServiceInstallersAsync(companyId, departmentId?, isActive?)` → `List<ServiceInstallerDto>`
- `GetServiceInstallerByIdAsync(id, companyId)` → `ServiceInstallerDto?`
- `CreateServiceInstallerAsync(dto, companyId)` → `ServiceInstallerDto`
- `UpdateServiceInstallerAsync(id, dto, companyId)` → `ServiceInstallerDto`
- `DeleteServiceInstallerAsync(id, companyId)` → `void`
- `GetContactsAsync(serviceInstallerId, companyId)` → `List<ServiceInstallerContactDto>`
- `CreateContactAsync(serviceInstallerId, dto, companyId)` → `ServiceInstallerContactDto`
- `UpdateContactAsync(contactId, dto, companyId)` → `ServiceInstallerContactDto`
- `DeleteContactAsync(contactId, companyId)` → `void`

**Implementation:** `ServiceInstallerService.cs`

**Validation Rules:**
- Duplicate prevention:
  - Exact match on `EmployeeId` (if provided)
  - Normalized match on `Name` + `Phone`/`Email`
  - Fuzzy match (85% similarity) on `Name` + `Phone`/`Email`
- Name normalization (removes special chars, case-insensitive)
- Phone normalization (removes spaces, dashes)
- Email normalization (lowercase, trim)

---

### 1.5 Database Table Structure

**Table:** `ServiceInstallers`

| Column | Type | Nullable | Max Length | Description |
|--------|------|----------|------------|-------------|
| Id | uuid | No | - | Primary key |
| CompanyId | uuid | No | - | Company scope |
| DepartmentId | uuid | Yes | - | Department FK |
| Name | varchar | No | 200 | Installer name |
| EmployeeId | varchar | Yes | 50 | Employee ID |
| Phone | varchar | Yes | 50 | Phone number |
| Email | varchar | Yes | 255 | Email address |
| SiLevel | varchar | No | 50 | Level (Junior, Senior, Subcon) |
| IsSubcontractor | boolean | No | - | Subcontractor flag |
| IsActive | boolean | No | - | Active status |
| UserId | uuid | Yes | - | User account FK |
| CreatedAt | timestamptz | No | - | Creation timestamp |
| UpdatedAt | timestamptz | Yes | - | Update timestamp |
| IsDeleted | boolean | No | - | Soft delete flag |
| DeletedAt | timestamptz | Yes | - | Deletion timestamp |

**Related Table:** `ServiceInstallerContacts`
- `Id`, `ServiceInstallerId` (FK, required)
- `Name`, `Phone?`, `Email?`
- `ContactType` (default: "Backup")
- `IsPrimary`

---

## 2. FRONTEND GAP ANALYSIS

### 2.1 What EXISTS

#### ✅ API Service Module
**File:** `frontend/src/api/serviceInstallers.ts`
- ✅ `getServiceInstallers(filters)` - GET all
- ✅ `getServiceInstaller(id)` - GET by ID
- ✅ `createServiceInstaller(data)` - POST
- ✅ `updateServiceInstaller(id, data)` - PUT
- ✅ `deleteServiceInstaller(id)` - DELETE
- ✅ `getServiceInstallerContacts(siId)` - GET contacts
- ✅ `createServiceInstallerContact(siId, data)` - POST contact
- ✅ `deleteServiceInstallerContact(contactId)` - DELETE contact
- ✅ `exportServiceInstallers(filters)` - Export CSV
- ✅ `downloadServiceInstallersTemplate()` - Download template
- ✅ `importServiceInstallers(file)` - Import CSV (API not implemented)

#### ✅ TypeScript Types
**File:** `frontend/src/types/serviceInstallers.ts`
- ✅ `ServiceInstaller` interface
- ✅ `ServiceInstallerContact` interface
- ✅ `CreateServiceInstallerRequest` interface
- ✅ `UpdateServiceInstallerRequest` interface
- ✅ `CreateServiceInstallerContactRequest` interface
- ✅ `ServiceInstallerFilters` interface
- ✅ `ImportResult` interface

**Gap:** Types don't match backend DTOs exactly:
- ❌ Missing `SiLevel` field
- ❌ Missing `IsSubcontractor` field
- ❌ Missing `EmployeeId` field
- ❌ Missing `UserId` field
- ❌ Missing `ContactType` field in contact

#### ✅ Page Components (2 versions exist)
1. **ServiceInstallersPage.tsx**
   - **Location:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`
   - **Status:** ✅ Exists, full-featured
   - **Features:**
     - List view with search/filter
     - Create/Edit modal
     - Contact management (tabs)
     - Import/Export buttons
     - Status toggle
     - Delete functionality

2. **ServiceInstallersPageEnhanced.tsx**
   - **Location:** `frontend/src/pages/serviceInstallers/ServiceInstallersPageEnhanced.tsx`
   - **Status:** ⚠️ Exists but incomplete
   - **Features:**
     - Syncfusion Grid
     - Inline editing (TODO: API calls)
     - Excel export
     - Missing: Create/Update/Delete API integration

#### ✅ Routes Configured
**Files:**
- `frontend/src/App.tsx` - Multiple routes defined
- `frontend/src/routes/settingsRoutes.tsx` - Enhanced route defined

**Routes:**
- ✅ `/settings/service-installers-enhanced` → `ServiceInstallersPageEnhanced`
- ✅ `/settings/service-installers` → `ServiceInstallersPage`
- ✅ `/gpon/service-installers` → `ServiceInstallersPage` (with department wrapper)
- ✅ `/cwo/service-installers` → `ServiceInstallersPage` (with department wrapper)
- ✅ `/nwo/service-installers` → `ServiceInstallersPage` (with department wrapper)

#### ✅ Navigation Links
**File:** `frontend/src/components/layout/Sidebar.tsx`
- ✅ `/settings/gpon/service-installers` - GPON section
- ✅ `/settings/cwo/service-installers` - CWO section
- ✅ `/settings/nwo/service-installers` - NWO section

**File:** `frontend/src/pages/settings/SettingsIndexPage.tsx`
- ✅ `/settings/service-installers-enhanced` - Settings index card

---

### 2.2 What's MISSING

#### ❌ React Query Hooks
**Missing File:** `frontend/src/hooks/useServiceInstallers.ts`

**Required Hooks:**
- ❌ `useServiceInstallers(filters)` - Query hook
- ❌ `useServiceInstaller(id)` - Query hook
- ❌ `useCreateServiceInstaller()` - Mutation hook
- ❌ `useUpdateServiceInstaller()` - Mutation hook
- ❌ `useDeleteServiceInstaller()` - Mutation hook
- ❌ `useServiceInstallerContacts(siId)` - Query hook
- ❌ `useCreateServiceInstallerContact()` - Mutation hook
- ❌ `useUpdateServiceInstallerContact()` - Mutation hook
- ❌ `useDeleteServiceInstallerContact()` - Mutation hook

**Reference:** `frontend/src/hooks/useInstallationMethods.ts`

---

#### ⚠️ Type Mismatches

**File:** `frontend/src/types/serviceInstallers.ts`

**Missing Fields:**
- ❌ `ServiceInstaller.employeeId` (backend has `EmployeeId`)
- ❌ `ServiceInstaller.siLevel` (backend has `SiLevel`)
- ❌ `ServiceInstaller.isSubcontractor` (backend has `IsSubcontractor`)
- ❌ `ServiceInstaller.userId` (backend has `UserId`)
- ❌ `ServiceInstallerContact.contactType` (backend has `ContactType`)

**Mismatched Fields:**
- ⚠️ `ServiceInstaller.code` (frontend) vs `EmployeeId` (backend) - Should be `employeeId`

---

#### ⚠️ Enhanced Page Incomplete

**File:** `frontend/src/pages/serviceInstallers/ServiceInstallersPageEnhanced.tsx`

**Missing:**
- ❌ API integration for create/update (TODO comment on line 72)
- ❌ Form fields for all properties (SiLevel, IsSubcontractor, etc.)
- ❌ Contact management
- ❌ Department filter
- ❌ Status filter
- ❌ Import/Export functionality

---

#### ❌ Update Contact Endpoint Missing

**File:** `frontend/src/api/serviceInstallers.ts`

**Missing:**
- ❌ `updateServiceInstallerContact(contactId, data)` - PUT endpoint

**Backend has:** `PUT /api/service-installers/contacts/{contactId}`

---

## 3. REFERENCE IMPLEMENTATION

### 3.1 Best Reference: InstallationMethodsPage

**File:** `frontend/src/pages/settings/InstallationMethodsPage.tsx`

**Why it's a good reference:**
- ✅ Uses React Query hooks (`useInstallationMethods`, `useCreateInstallationMethod`, etc.)
- ✅ Similar structure (list, create/edit modal, filters)
- ✅ Uses TanStack Query for data fetching
- ✅ Proper error handling with toast notifications
- ✅ Department context integration
- ✅ Form validation
- ✅ Status toggle functionality

**Structure to copy:**
1. React Query hooks usage
2. Form data structure
3. Modal create/edit pattern
4. Filter/search implementation
5. Status toggle pattern
6. Department integration

---

### 3.2 Alternative Reference: BuildingTypesPage

**File:** `frontend/src/pages/settings/BuildingTypesPage.tsx`

**Why it's useful:**
- ✅ Similar reference data pattern
- ✅ Display order management
- ✅ Guide section (How-To)
- ✅ Simple CRUD operations

**Less relevant because:**
- Doesn't use React Query (uses useState/useEffect)
- No department context
- Simpler structure

---

## 4. IMPLEMENTATION PLAN

### Phase 1: Fix Type Definitions

**File to modify:** `frontend/src/types/serviceInstallers.ts`

**Changes:**
- [ ] Add `employeeId?: string` to `ServiceInstaller`
- [ ] Add `siLevel?: string` to `ServiceInstaller`
- [ ] Add `isSubcontractor?: boolean` to `ServiceInstaller`
- [ ] Add `userId?: string` to `ServiceInstaller`
- [ ] Remove or deprecate `code?: string` (use `employeeId` instead)
- [ ] Add `contactType?: string` to `ServiceInstallerContact`
- [ ] Update `CreateServiceInstallerRequest` to include:
  - `employeeId?`, `siLevel?`, `isSubcontractor?`, `userId?`
- [ ] Update `UpdateServiceInstallerRequest` to include all fields
- [ ] Add `UpdateServiceInstallerContactRequest` interface

---

### Phase 2: Create React Query Hooks

**File to create:** `frontend/src/hooks/useServiceInstallers.ts`

**Hooks to implement:**
- [ ] `useServiceInstallers(filters)` - Query hook
  - Query key: `['serviceInstallers', filters]`
  - Query function: `getServiceInstallers(filters)`
  - Stale time: 5 minutes

- [ ] `useServiceInstaller(id)` - Query hook
  - Query key: `['serviceInstaller', id]`
  - Query function: `getServiceInstaller(id)`
  - Enabled: only when `id` exists

- [ ] `useCreateServiceInstaller()` - Mutation hook
  - Mutation function: `createServiceInstaller(data)`
  - On success: invalidate `['serviceInstallers']`, show success toast
  - On error: show error toast

- [ ] `useUpdateServiceInstaller()` - Mutation hook
  - Mutation function: `updateServiceInstaller(id, data)`
  - On success: invalidate `['serviceInstallers']` and `['serviceInstaller', id]`, show success toast
  - On error: show error toast

- [ ] `useDeleteServiceInstaller()` - Mutation hook
  - Mutation function: `deleteServiceInstaller(id)`
  - On success: invalidate `['serviceInstallers']`, show success toast
  - On error: show error toast

- [ ] `useServiceInstallerContacts(siId)` - Query hook
  - Query key: `['serviceInstallerContacts', siId]`
  - Query function: `getServiceInstallerContacts(siId)`
  - Enabled: only when `siId` exists

- [ ] `useCreateServiceInstallerContact()` - Mutation hook
  - Mutation function: `createServiceInstallerContact(siId, data)`
  - On success: invalidate `['serviceInstallerContacts', siId]`, show success toast

- [ ] `useUpdateServiceInstallerContact()` - Mutation hook
  - Mutation function: `updateServiceInstallerContact(contactId, data)`
  - On success: invalidate contacts query, show success toast

- [ ] `useDeleteServiceInstallerContact()` - Mutation hook
  - Mutation function: `deleteServiceInstallerContact(contactId)`
  - On success: invalidate contacts query, show success toast

**Reference:** Copy pattern from `frontend/src/hooks/useInstallationMethods.ts`

---

### Phase 3: Add Missing API Function

**File to modify:** `frontend/src/api/serviceInstallers.ts`

**Function to add:**
- [ ] `updateServiceInstallerContact(contactId, data)`
  - Method: PUT
  - Route: `/service-installers/contacts/${contactId}`
  - Body: `UpdateServiceInstallerContactRequest`
  - Returns: `ServiceInstallerContact`

---

### Phase 4: Update ServiceInstallersPage.tsx

**File to modify:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`

**Changes:**
- [ ] Replace `useState` + `useEffect` with React Query hooks
- [ ] Use `useServiceInstallers()` instead of manual `loadServiceInstallers()`
- [ ] Use mutation hooks for create/update/delete
- [ ] Update form to include all fields:
  - `employeeId` (instead of `code`)
  - `siLevel` (dropdown: Junior, Senior, Subcon)
  - `isSubcontractor` (checkbox)
  - `userId` (optional user selection)
- [ ] Update contact form to include `contactType` field
- [ ] Add `updateServiceInstallerContact` API call for contact updates
- [ ] Ensure department context integration (if needed)

**Reference:** Follow pattern from `InstallationMethodsPage.tsx`

---

### Phase 5: Complete ServiceInstallersPageEnhanced.tsx

**File to modify:** `frontend/src/pages/serviceInstallers/ServiceInstallersPageEnhanced.tsx`

**Changes:**
- [ ] Replace `useState` + `useEffect` with React Query hooks
- [ ] Implement `actionComplete` handler to call API (remove TODO on line 72)
- [ ] Add all form fields in grid columns:
  - `Name`, `EmployeeId`, `Phone`, `Email`
  - `SiLevel` (dropdown), `IsSubcontractor` (checkbox)
  - `DepartmentId`, `IsActive`
- [ ] Add department filter
- [ ] Add status filter
- [ ] Add contact management (separate section or modal)
- [ ] Add import/export functionality
- [ ] Ensure proper error handling

**Reference:** Follow pattern from other Enhanced pages (e.g., `AssetTypesPageEnhanced.tsx`)

---

### Phase 6: Verify Routes

**Files to check:**
- `frontend/src/App.tsx`
- `frontend/src/routes/settingsRoutes.tsx`
- `frontend/src/components/layout/Sidebar.tsx`

**Verification:**
- [ ] All routes are properly configured
- [ ] Navigation links point to correct routes
- [ ] Settings index page includes Service Installers card

**Status:** ✅ Routes already exist, just verify they work

---

## 5. FILES TO CREATE/MODIFY

### Files to Create
1. `frontend/src/hooks/useServiceInstallers.ts` - **NEW FILE**

### Files to Modify
1. `frontend/src/types/serviceInstallers.ts` - Add missing fields
2. `frontend/src/api/serviceInstallers.ts` - Add `updateServiceInstallerContact`
3. `frontend/src/pages/settings/ServiceInstallersPage.tsx` - Migrate to React Query
4. `frontend/src/pages/serviceInstallers/ServiceInstallersPageEnhanced.tsx` - Complete implementation

### Files to Verify (No Changes Expected)
1. `frontend/src/App.tsx` - Routes already configured
2. `frontend/src/routes/settingsRoutes.tsx` - Route already configured
3. `frontend/src/components/layout/Sidebar.tsx` - Links already exist
4. `frontend/src/pages/settings/SettingsIndexPage.tsx` - Card already exists

---

## 6. IMPLEMENTATION CHECKLIST

### Step 1: Type Definitions ✅
- [ ] Update `ServiceInstaller` interface with all backend fields
- [ ] Update `ServiceInstallerContact` interface
- [ ] Update request/response interfaces
- [ ] Remove deprecated `code` field (use `employeeId`)

### Step 2: API Client ✅
- [ ] Add `updateServiceInstallerContact()` function
- [ ] Verify all other functions match backend endpoints

### Step 3: React Query Hooks ✅
- [ ] Create `useServiceInstallers.ts` file
- [ ] Implement all query hooks
- [ ] Implement all mutation hooks
- [ ] Add proper error handling
- [ ] Add query invalidation

### Step 4: Update ServiceInstallersPage.tsx ✅
- [ ] Replace manual data fetching with React Query hooks
- [ ] Update form to include all fields (SiLevel, IsSubcontractor, etc.)
- [ ] Update contact form to include ContactType
- [ ] Add update contact functionality
- [ ] Test create/update/delete operations

### Step 5: Complete ServiceInstallersPageEnhanced.tsx ✅
- [ ] Replace manual data fetching with React Query hooks
- [ ] Implement API calls in `actionComplete` handler
- [ ] Add all form fields to grid
- [ ] Add filters (department, status)
- [ ] Add contact management
- [ ] Test inline editing

### Step 6: Testing ✅
- [ ] Test list view loads correctly
- [ ] Test create new installer
- [ ] Test update installer
- [ ] Test delete installer
- [ ] Test status toggle
- [ ] Test contact CRUD operations
- [ ] Test export CSV
- [ ] Test import CSV (when backend implements)
- [ ] Test filters (department, status)
- [ ] Test search functionality

---

## 7. VERIFICATION STEPS

### 7.1 Backend Verification
- [ ] All endpoints return correct response envelope
- [ ] Validation rules work (duplicate prevention)
- [ ] Department filtering works
- [ ] Status filtering works

### 7.2 Frontend Verification
- [ ] Page loads without errors
- [ ] List displays all installers
- [ ] Create form includes all fields
- [ ] Edit form pre-populates correctly
- [ ] Delete shows confirmation
- [ ] Status toggle works
- [ ] Contact management works
- [ ] Export downloads CSV file
- [ ] Filters work (department, status)
- [ ] Search works

### 7.3 Integration Verification
- [ ] Create installer → appears in list
- [ ] Update installer → changes reflected
- [ ] Delete installer → removed from list
- [ ] Toggle status → status updates
- [ ] Add contact → appears in contacts list
- [ ] Update contact → changes reflected
- [ ] Delete contact → removed from list

---

## 8. SUMMARY

### Backend Status: ✅ Complete
- Entity: ✅ Defined
- Controller: ✅ All endpoints implemented
- DTOs: ✅ All DTOs defined
- Service: ✅ Full CRUD + contacts + import/export
- Validation: ✅ Duplicate prevention, normalization

### Frontend Status: ⚠️ Partial
- API Client: ✅ Mostly complete (missing update contact)
- Types: ⚠️ Missing some fields
- Hooks: ❌ Not created (need React Query hooks)
- Pages: ⚠️ Exist but need updates
  - `ServiceInstallersPage.tsx` - Needs React Query migration
  - `ServiceInstallersPageEnhanced.tsx` - Needs API integration
- Routes: ✅ Configured
- Navigation: ✅ Configured

### Priority Actions
1. **HIGH:** Create React Query hooks (`useServiceInstallers.ts`)
2. **HIGH:** Update type definitions to match backend
3. **MEDIUM:** Migrate `ServiceInstallersPage.tsx` to use React Query
4. **MEDIUM:** Complete `ServiceInstallersPageEnhanced.tsx` API integration
5. **LOW:** Add missing `updateServiceInstallerContact` API function

---

## 9. REFERENCE FILES

### Backend Reference
- Entity: `backend/src/CephasOps.Domain/ServiceInstallers/Entities/ServiceInstaller.cs`
- Controller: `backend/src/CephasOps.Api/Controllers/ServiceInstallersController.cs`
- DTOs: `backend/src/CephasOps.Application/ServiceInstallers/DTOs/ServiceInstallerDto.cs`
- Service: `backend/src/CephasOps.Application/ServiceInstallers/Services/ServiceInstallerService.cs`

### Frontend Reference (For Implementation)
- Hooks Pattern: `frontend/src/hooks/useInstallationMethods.ts`
- Page Pattern: `frontend/src/pages/settings/InstallationMethodsPage.tsx`
- Enhanced Page Pattern: `frontend/src/pages/settings/AssetTypesPageEnhanced.tsx`

### Frontend Existing (To Update)
- API: `frontend/src/api/serviceInstallers.ts`
- Types: `frontend/src/types/serviceInstallers.ts`
- Page: `frontend/src/pages/settings/ServiceInstallersPage.tsx`
- Enhanced Page: `frontend/src/pages/serviceInstallers/ServiceInstallersPageEnhanced.tsx`

---

**Status:** Ready for implementation  
**Estimated Effort:** Medium (4-6 hours)  
**Dependencies:** None (backend is complete)

