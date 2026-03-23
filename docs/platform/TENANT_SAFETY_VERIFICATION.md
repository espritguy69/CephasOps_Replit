# Tenant Safety Verification

## Rule

Every module must enforce **CompanyId == current tenant** for non–global-admins. No cross-tenant data access or event leakage.

## Verification Summary

| Area | Enforcement |
|------|-------------|
| **Orders** | OrderService: GetOrdersAsync, GetOrderByIdAsync, CreateOrderAsync, etc. filter or require companyId. CreateOrderAsync and CreateFromParsedDraftAsync set order.CompanyId from parameter/dto. OrderCreatedEvent and event bus use that CompanyId. |
| **Event platform** | EventStore queries and replay accept scopeCompanyId; controllers set scope from current user (ScopeCompanyId()). Event payload and store record CompanyId. Replay rejects when entry.CompanyId != scopeCompanyId. |
| **Automation** | AutomationRule is CompanyScopedEntity. GetApplicableRulesAsync(companyId, ...). OrderCompletedAutomationHandler loads order with o.CompanyId == companyId and creates invoice for that company. |
| **Billing** | BillingService methods take companyId; invoice and subscription are tenant-scoped. TenantSubscriptions filtered by TenantId. |
| **Reporting** | ReportsController uses department/company scope; 403 when user has no access (IDepartmentAccessService). |
| **Integration** | OutboundIntegrationDelivery and InboundWebhookReceipt have CompanyId. Connector registry resolves by company. |
| **Control plane** | Admin-only; tenant diagnostics (when added) must filter by tenant. |

## Event Publishing

- When publishing domain events, callers must set **evt.CompanyId** to the current tenant (e.g. from ICurrentUserService.CompanyId or request context). OrderService sets CompanyId on OrderCreatedEvent from the order's company. WorkflowEngineService sets CompanyId from workflow context.

## Recommendations

- In any new API that returns or mutates tenant data, resolve current tenant (e.g. ICurrentUserService.CompanyId) and filter or assert by it unless the user is a global admin.
- In any new event handler, use domainEvent.CompanyId for all queries and writes; do not switch to another tenant.
