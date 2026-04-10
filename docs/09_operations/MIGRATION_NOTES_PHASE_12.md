# Migration Notes — Phase 12 (Subscription Billing)

## Summary

Adds tables: **BillingPlans**, **TenantSubscriptions**, **TenantUsageRecords**, **TenantInvoices**, with indexes. No changes to existing tables.

## Migration name

`Phase12_SubscriptionBilling` (file: `20260310041112_Phase12_SubscriptionBilling.cs`).

## Apply

From `backend/src/CephasOps.Api`:

- **EF**: `dotnet ef database update --project ..\CephasOps.Infrastructure\CephasOps.Infrastructure.csproj --context ApplicationDbContext`
- **Script**: `dotnet ef migrations script 20260310033559_Phase11_TenantIsolation 20260310041112_Phase12_SubscriptionBilling --project ..\CephasOps.Infrastructure\CephasOps.Infrastructure.csproj --context ApplicationDbContext --idempotent -o phase12.sql` then run phase12.sql.

## Rollback

Down migration drops the four tables. No data migration.
