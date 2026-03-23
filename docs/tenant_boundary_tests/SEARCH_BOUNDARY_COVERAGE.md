# Search / Lookup / Autocomplete Boundary Coverage

This document summarizes which search-like surfaces were tested for tenant isolation and which remain for manual review.

## Surfaces identified

| Surface | Endpoint / area | Tenant risk | Automated test | Notes |
|--------|------------------|-------------|----------------|-------|
| **Users** | `GET api/users?search=` | High | Yes | `Users_List_WithSearch_ReturnsOnlySameTenantUsers` – Tenant A search does not return Tenant B users |
| **Orders** | `GET api/orders` with `keyword` | High | No | Keyword search; list already tenant-scoped via service/filters |
| **Inventory materials** | `GET api/inventory/materials?search=` | High | No | Department + company scoped; add test if desired |
| **Assets** | `GET api/assets?search=` | High | No | CompanyId from TenantProvider; add test if desired |
| **Service profiles** | `GET api/service-profiles?search=` | High | No | Tenant-scoped; add test if desired |
| **Reports** | Definitions / run with search | High | Partial | Reports definitions tested; run/export use department scope |
| **Buildings** | List/import lookups | High | No | Lookups are company-scoped in controller |
| **Rates** | Batch lookups, resolve | High | No | Company-scoped in code; add test for resolve/lookup if desired |
| **Companies** | `GET api/companies?search=` | Medium | No | Platform/admin or tenant list; clarify who can call |
| **Admin users** | `GET api/admin/users?search=` | Medium | No | Platform admin; ensure policy restricts to intended callers |
| **Parser** | Search by serviceId/customerName | High | No | Tenant context from request; add test if parser is tenant-facing |

## Tests added

- **Users search**: `TenantBoundaryTests.Users_List_WithSearch_ReturnsOnlySameTenantUsers` – asserts that `api/users?search=User` returns only same-tenant users (no cross-tenant leak).

## Expectations

- **List + search**: Any endpoint that supports both list and search (e.g. users, orders, materials) must apply the same tenant scope to the search path so Tenant A cannot discover Tenant B records by partial match or shared names.
- **Lookup dropdowns**: Lookups that return options for dropdowns (e.g. partners, order types, departments) must be keyed by current tenant (from `ITenantProvider` or equivalent); no test yet for each lookup.
- **By-id helpers**: Any “get by id” used for enrichment must use tenant-scoped lookup (or 404 for other tenant); covered by by-id boundary tests where applicable.

## Remaining manual review

- Orders keyword search: confirm service layer applies tenant filter before keyword.
- Inventory materials search: confirm department + company scope in `GetMaterialsAsync`.
- Assets search: confirm company filter in `GetAssetsAsync`.
- Ratecard/rates resolve and batch lookups: already company-scoped in code; optional automated test.
- Parser search (serviceId/customerName): if tenant-facing, add boundary test.

## Summary

- **Tested**: Users list with search (automatic).
- **Covered by list isolation**: Any list endpoint that is tenant-scoped (users, warehouses, departments, etc.) and supports search uses the same query; search boundary is implied by list boundary where the same action is used.
- **Recommended**: Add explicit search tests for orders (keyword), inventory (search), and assets (search) when touching those modules.
