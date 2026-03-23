# Plan: Remove Companies and Multi-Company Features

## Overview
This document outlines the plan to remove all company-related features and multi-company support from CephasOps to simplify the system.

## Scope of Changes

### 1. Controllers to Update
- ✅ **CompaniesController** - Remove entirely
- ⚠️ **AuthController** - Remove `switch-company` endpoint, remove companyId from JWT
- ⚠️ **All other controllers** - Remove companyId checks and filtering

### 2. Services to Update
- Remove `companyId` parameters from all service methods
- Remove company filtering from all queries
- Remove company validation logic

### 3. Domain Entities
- Make `CompanyId` nullable OR remove it entirely
- Entities affected:
  - Orders
  - Departments
  - Materials
  - Invoices
  - RMA Requests
  - StockBalances
  - StockMovements
  - ServiceInstallers
  - Buildings
  - Splitters
  - And many more...

### 4. Authentication Changes
- Remove `companyId` from JWT claims
- Remove `switch-company` endpoint
- Simplify `UserDto` to remove `Companies` and `CurrentCompanyId`
- Remove `UserCompany` entity relationships

### 5. Database Changes
- Make `CompanyId` columns nullable (or remove via migration)
- Remove `Companies` table (or keep for future use)
- Remove `UserCompanies` junction table

## Implementation Steps

### Phase 1: Remove Company Management
1. Delete `CompaniesController`
2. Remove company service interfaces and implementations
3. Remove company DTOs

### Phase 2: Simplify Authentication
1. Remove `switch-company` endpoint
2. Remove `companyId` from JWT generation
3. Update `CurrentUserService` to not require company context
4. Simplify `UserDto`

### Phase 3: Remove Company Filtering
1. Update all controllers to remove companyId checks
2. Update all services to remove companyId parameters
3. Remove company filtering from all queries

### Phase 4: Database Schema
1. Create migration to make CompanyId nullable
2. Or create migration to remove CompanyId columns entirely

## Risks & Considerations

⚠️ **Data Loss Risk**: If CompanyId is removed entirely, existing data will lose company association
⚠️ **Breaking Changes**: All existing API clients will break
⚠️ **Future Re-introduction**: If companies are needed later, this will require re-implementation

## Recommendation

**Option A: Make CompanyId Optional (Recommended)**
- Keep CompanyId in database but make it nullable
- Remove all company filtering and validation
- Keep Companies table for future use
- Easier to re-enable later if needed

**Option B: Remove CompanyId Completely**
- Remove CompanyId columns via migration
- Cleaner schema but harder to re-enable later
- Requires data migration if existing data exists

## Next Steps

1. Confirm which option (A or B)
2. Start with Phase 1 (remove company management)
3. Then Phase 2 (simplify authentication)
4. Then Phase 3 (remove filtering)
5. Finally Phase 4 (database changes)

