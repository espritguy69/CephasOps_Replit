# Phase 12: Billing & Subscription Platform

## Overview

Phase 12 adds SaaS subscription billing: billing plans, tenant subscriptions, metered usage records, tenant invoices, and a payment provider abstraction. Operational billing (Invoice, SupplierInvoice, Payment) remains unchanged.

## Implemented

### Domain
- **BillingPlan**: Id, Name, Slug, BillingCycle (Monthly/Yearly), Price, Currency, IsActive, timestamps.
- **TenantSubscription**: TenantId, BillingPlanId, Status (Active/Cancelled/PastDue/Trialing), StartedAtUtc, CurrentPeriodEndUtc, ExternalSubscriptionId.
- **TenantUsageRecord**: TenantId, MetricKey, Quantity, PeriodStartUtc, PeriodEndUtc (metered usage).
- **TenantInvoice**: TenantId, TenantSubscriptionId, InvoiceNumber, period, amounts, Status, DueDateUtc, PaidAtUtc.

### Application
- **IPaymentProvider**: CreateOrUpdateSubscriptionAsync, CancelSubscriptionAsync, ChargeAsync. **NoOpPaymentProvider** for development.
- **IBillingPlanService** / **BillingPlanService**: List plans, GetBySlug.
- **ITenantSubscriptionService** / **TenantSubscriptionService**: ListByTenant, GetActive, SubscribeAsync (via payment provider), CancelAsync.

### API
- **GET /api/billing/plans**, **GET /api/billing/plans/by-slug/{slug}**: List and get plans (authorized).
- **GET /api/billing/subscriptions/tenant/{tenantId}**: List subscriptions for tenant (admin).
- **GET /api/billing/subscriptions/me**: Current tenant's active subscription (ITenantContext).
- **POST /api/billing/subscriptions/tenant/{tenantId}/subscribe**: Body `{ "planSlug": "..." }` (admin).
- **POST /api/billing/subscriptions/me/cancel**, **POST /api/billing/subscriptions/tenant/{tenantId}/cancel**: Cancel subscription.

### Permissions
- **AdminBillingPlansView**, **AdminBillingPlansEdit** in PermissionCatalog; assigned to Admin (admin.*).

### Migration
- **Phase12_SubscriptionBilling**: Tables BillingPlans, TenantSubscriptions, TenantUsageRecords, TenantInvoices and indexes. No FK to Tenants from Phase 11 (already applied).

## Usage

- Create billing plans via data seed or future admin API (POST plan).
- Admin: subscribe a tenant with `POST .../tenant/{tenantId}/subscribe` and body `{ "planSlug": "pro-monthly" }`.
- Tenant context required for `/me` endpoints; use JWT with companyId that maps to a tenant.
- Replace **NoOpPaymentProvider** with a real implementation (e.g. Stripe) and register in DI.

## Limitations

- No automated invoice generation from subscription/usage; TenantInvoice is ready for manual or job-based creation.
- No background job to renew subscriptions or set PastDue; extend when needed.
- Billing plan create/update API not added; add when needed for operator UI.
