# Billing Architecture

**Date:** 2026-03-13

This document describes the billing integration layer and how to plug in external providers (e.g. Stripe).

---

## 1. Abstraction layer

- **Interface:** `IBillingProviderService` (Application layer).
- **Default implementation:** `StubBillingProviderService` – no external calls; returns stub IDs and "active" status for development.

**Capabilities:**

| Method | Purpose |
|--------|--------|
| `CreateCustomerAsync(tenantId, email, companyName)` | Create a billing customer; returns external customer ID. |
| `AttachSubscriptionAsync(tenantId, externalCustomerId, planSlug)` | Attach a subscription (plan) to the customer; returns external subscription ID. |
| `GetBillingStatusAsync(tenantId, externalSubscriptionId)` | Get current billing status (e.g. active, past_due, cancelled). |
| `HandleWebhookAsync(payload, signatureHeader, webhookSecret)` | Handle incoming payment/webhook; verify signature and process. |

**Result types:**
- `BillingProviderResult` – Success, ExternalId, ErrorCode, ErrorMessage.
- `BillingStatusResult` – Success, Status, CurrentPeriodEndUtc, ErrorMessage.
- `WebhookHandleResult` – Processed, HttpStatus, ErrorMessage.

---

## 2. Integration approach

1. **Implement** `IBillingProviderService` for your provider (e.g. Stripe).
2. **Register** your implementation in DI instead of `StubBillingProviderService`.
3. **Create customer** when a tenant is provisioned or when they first add a paid plan (call `CreateCustomerAsync`).
4. **Attach subscription** when the tenant subscribes to a plan (e.g. after trial or on plan change); store the returned external subscription ID on `TenantSubscription.ExternalSubscriptionId`.
5. **Sync status** – Use `GetBillingStatusAsync` when needed (e.g. before allowing access) or rely on webhooks to update `TenantSubscription` (status, period end).
6. **Webhook endpoint** – Expose an HTTP endpoint that reads the raw body and signature header, then calls `HandleWebhookAsync`. Return the `HttpStatus` from the result. In the implementation, verify the provider’s signature and update `TenantSubscription` / invoices as needed.

---

## 3. Current behaviour

- **Stub:** No external API calls. `CreateCustomerAsync` and `AttachSubscriptionAsync` return synthetic IDs; `GetBillingStatusAsync` returns "active" and a period end one month ahead; `HandleWebhookAsync` returns 200 without processing.
- **Existing subscription flow:** `TenantSubscriptionService` and `IPaymentProvider` (subscription create/cancel) remain the primary subscription persistence; the new `IBillingProviderService` is the abstraction for customer lifecycle, subscription attachment, status, and webhooks. You can align `IPaymentProvider` with `IBillingProviderService` or keep both (e.g. one for subscriptions, one for one-off charges).

---

## 4. References

- [SAAS_SCALING_ARCHITECTURE.md](../saas_scaling/SAAS_SCALING_ARCHITECTURE.md) – Subscription and usage.
- [SAAS_OPERATIONS_HARDENING_REPORT.md](../saas_scaling/SAAS_OPERATIONS_HARDENING_REPORT.md) – Subscription admin and limits.
