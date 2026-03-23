# Workflow Side-Effect Boundary Tests

Cross-tenant leaks can occur in side effects (notifications, billing records, job ownership, file usage) when an action in one tenant triggers work that must stay within that tenant.

## Workflows to keep tenant-bound

| Workflow | Side effect | Tenant guarantee | Automated test | Notes |
|----------|-------------|------------------|----------------|-------|
| Order status / workflow | Notifications, event-store events | Created with same CompanyId as order | No | EventStore and Notifications are tenant-scoped; add test that creating order as Tenant A does not create notifications for Tenant B |
| Billing / invoice | Invoice, line items, submissions | Same company as order/partner | Partial | `BillingServiceFinancialIsolationTests` (Application) covers service; API-level test optional |
| Job retry / replay | JobExecution, BackgroundJob | CompanyId set from request context | No | Replay/cancel are platform or tenant-scoped; AdminApiSafetyTests cover replay 404 |
| File upload / delete | File entity, usage stats | File.CompanyId = current tenant | No | Files controller uses CurrentTenantId; add test: upload as A, download as B returns 404 |
| Report run / export | Rows, export file | Department/company from request only | Partial | ReportsIntegrationTests: user in Dept A cannot run/export for Dept B |
| Guardian / analytics | Aggregations, metrics | Platform-only or tenant-scoped views | No | OperationsOverview and Guardian must not expose raw other-tenant business data to tenant users |

## Recommended tests (to add)

1. **Order workflow → notifications**: Seed order for Tenant A, trigger status change or workflow step as Tenant A; assert no notification or event-store row is created with CompanyId = B. (Requires event/store or notification seeding and query.)
2. **File cross-tenant**: As Tenant A upload a file (if endpoint available in test), then as Tenant B call get/download with that file id → expect 404 (or 403).
3. **Billing API**: As Tenant A create invoice from order A; as Tenant B call get invoice by A’s invoice id → 404. (Covered in spirit by BillingServiceFinancialIsolationTests; API test reinforces.)
4. **Job replay**: As Tenant A request replay for a job that belongs to B (if such endpoint exists) → 404/403.

## Current coverage

- **Application layer**: `BillingServiceFinancialIsolationTests` ensures invoice creation rejects line items from another company; `OrderPayoutSnapshotServiceFinancialIsolationTests` covers payout snapshot isolation.
- **API layer**: Reports run/export with department B as user in department A returns 403. Event-store list with `companyId=B` as non–SuperAdmin returns 403.
- **Not yet automated**: Order→notification side effect, file download cross-tenant, explicit invoice by-id cross-tenant.

## Summary

- Workflow side-effect boundary coverage is **partial**: billing and report/event-store have isolation tests; order→notifications and file cross-tenant are good candidates for additional tests.
- Adding these will complete the “workflow side effects remain tenant-safe” objective without redesigning the platform.
