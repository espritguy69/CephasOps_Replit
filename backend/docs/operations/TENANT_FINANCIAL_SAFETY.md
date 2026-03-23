# Tenant Financial Safety

**Date:** 2026-03-13  
**Purpose:** Audit and hardening of billing, payment, payout, and rate paths so tenant financial execution remains safe, auditable, and resistant to cross-tenant or duplicate execution issues. Platform safety only; no business feature expansion.

---

## 1. Financial paths audited

| Path | Location | Tenant scoping | Duplicate protection | Audit log |
|------|----------|----------------|----------------------|-----------|
| **Invoice CRUD** | BillingService | Get/Update/Delete/GetById/GeneratePdf filter by id + companyId; Create requires company (FinancialIsolationGuard), validates order company | Optional client IdempotencyKey; CommandProcessingLogStore; replay returns existing | tenantId, invoiceId, operation, success |
| **GetInvoiceCompanyIdAsync** | BillingService | When tenant scope set and not bypass, returns null if invoice.CompanyId ≠ CurrentTenantId (no cross-tenant leak) | N/A | N/A |
| **Payment CRUD** | PaymentService | All reads/updates filter by companyId; Create requires company (FinancialIsolationGuard) | Optional client IdempotencyKey; CommandProcessingLogStore; replay returns existing | tenantId, paymentId, operation, success |
| **Payout snapshot** | OrderPayoutSnapshotService | Create requires tenant or bypass; GetPayoutWithSnapshotOrLive uses RequireSameCompany when snapshot found; GetSnapshotByOrderId uses EF filter when scope set | CreateSnapshotForOrderIfEligible: AnyAsync(orderId) — one snapshot per order | tenantId, orderId, snapshotId, operation |
| **Rate resolution** | BillingService.ResolveInvoiceLineFromOrderAsync, BuildInvoiceLinesFromOrdersAsync | Explicit companyId; BillingRatecard queries scoped by companyId | N/A | Existing |
| **BillingRatecardService** | BillingRatecardService | Previously hardened (effectiveCompanyId; no Guid.Empty “all tenants”) | N/A | Existing |
| **PnlService.RebuildPnlAsync** | PnlService | FinancialIsolationGuard.RequireCompany | N/A | Existing |

---

## 2. Safety fixes applied

- **PaymentService.CreatePaymentAsync:** Added `FinancialIsolationGuard.RequireTenantOrBypass("CreatePayment")` and `RequireCompany(companyId, "CreatePayment")` so payment creation never proceeds without valid tenant or bypass and company.
- **BillingService.GetInvoiceCompanyIdAsync:** When tenant scope is set (and not platform bypass), return null if the invoice’s CompanyId does not match `TenantScope.CurrentTenantId`, so callers cannot use this to resolve another tenant’s company from an invoice id.
- **Structured audit logs:** BillingService (Create/Update/Delete invoice) and PaymentService (Create/Update/Delete/Void/Reconcile payment) and OrderPayoutSnapshotService (Create snapshot) now log with `tenantId`, `invoiceId`/`paymentId`/`orderId`/`snapshotId`, `operation`, and `success=true` for financial writes.

---

## 3. Duplicate execution protections

- **Payout snapshot:** `CreateSnapshotForOrderIfEligibleAsync` checks `AnyAsync(s => s.OrderId == orderId)` before creating; at most one snapshot per order. No change.
- **Invoice creation (2026-03-13):** Optional client-supplied **IdempotencyKey** on `CreateInvoiceDto`. When provided (and companyId is set), the key is stored as `{companyId:N}:CreateInvoice:{key}` in the existing **CommandProcessingLog** store. Replay of the same request returns the existing invoice (no duplicate). Automation (OrderCompletedAutomationHandler) uses `IdempotencyKey = "order-invoice-{orderId}"` so the same order does not get two invoices from automation. **Without** idempotency key, behavior is unchanged (each request creates a new invoice).
- **Payment creation (2026-03-13):** Optional client-supplied **IdempotencyKey** on `CreatePaymentDto`. When provided (and companyId is set), the key is stored as `{companyId:N}:CreatePayment:{key}` in **CommandProcessingLog**. Replay returns the existing payment. Email ingestion uses `IdempotencyKey = "email-payment-{emailMessage.Id}"` so reprocessing the same email does not create a duplicate payment. **Without** key, behavior is unchanged.
- **Financial jobs:** Pnl rebuild and other financial jobs run under TenantScopeExecutor with company from job context; no duplicate guard added in this pass.

**Idempotency design:** Reuses existing `ICommandProcessingLogStore` (CommandProcessingLog table). No schema change. Key is tenant-scoped (companyId prefix), so the same logical key in different companies does not collide. Failed attempts can be retried (store marks Failed and allows re-claim).

---

## 4. Remaining manual-review items

- **Idempotency key optional:** Callers that do not send `IdempotencyKey` (e.g. legacy UI or one-off scripts) still get one record per request. Only flows that supply a key (or use automation/email with built-in keys) are protected from duplicate creation. To enforce idempotency for a path, the caller must set the key.
- **SuperAdmin and GetInvoices/GetInvoiceById:** When companyId is null (SuperAdmin), list and get-by-id return all companies’ data; by design. No change.
- **BillingController CreateInvoice:** Allows SuperAdmin with companyId null; BillingService.CreateInvoiceAsync then throws due to RequireCompany — i.e. SuperAdmin cannot create invoice without providing company (e.g. via header or body). Document if product expects SuperAdmin to create invoices on behalf of a company.

---

## 5. Files changed

| File | Change |
|------|--------|
| Application/Billing/Services/PaymentService.cs | FinancialIsolationGuard on CreatePayment; audit log for Create/Update/Delete/Void/Reconcile; idempotency via ICommandProcessingLogStore when CreatePaymentDto.IdempotencyKey set |
| Application/Billing/Services/BillingService.cs | GetInvoiceCompanyIdAsync cross-tenant leak fix; audit log for Create/Update/Delete invoice; idempotency via ICommandProcessingLogStore when CreateInvoiceDto.IdempotencyKey set |
| Application/Billing/DTOs/PaymentDto.cs, InvoiceDto.cs | Optional IdempotencyKey on CreatePaymentDto and CreateInvoiceDto |
| Application/Rates/Services/OrderPayoutSnapshotService.cs | Audit log for CreatePayoutSnapshot |
| Application/Parser/Services/EmailIngestionService.cs | CreatePaymentDto.IdempotencyKey = "email-payment-{emailMessage.Id}" for payment-advice emails |
| Application/Automation/Handlers/OrderCompletedAutomationHandler.cs | CreateInvoiceDto.IdempotencyKey = "order-invoice-{orderId}" for automation-created invoices |
| Application.Tests/Billing/PaymentServiceFinancialSafetyTests.cs | CreatePayment company required; cross-tenant get; same-tenant get; **idempotency:** same key twice returns same payment; different keys create separate payments; same key different company creates separate payments (tenant-scoped) |
| Application.Tests/Billing/BillingServiceFinancialIsolationTests.cs | GetInvoiceById other-tenant returns null; GetInvoiceCompanyIdAsync cross-tenant returns null; **idempotency:** same key twice returns same invoice; different keys create separate invoices |
| Api/Controllers/BillingController.cs, PaymentsController.cs | CreateInvoice/CreatePayment: XML docs and remarks for IdempotencyKey; optional X-Idempotency-Key header (used when body key not set); null-dto guard |
| Api.Tests/Integration/FinancialIdempotencyApiTests.cs | API-level tests: same key twice returns same payment; same key different tenant creates separate payments; X-Idempotency-Key header replay (require DB with ExecuteUpdate support) |

---

## 6. API adoption and consumer guidance

### API paths verified

| Endpoint | Method | Idempotency support | Notes |
|----------|--------|----------------------|-------|
| `POST /api/billing/invoices` | CreateInvoice | Yes | DTO has `idempotencyKey`; optional header `X-Idempotency-Key` (body takes precedence) |
| `POST /api/payments` | CreatePayment | Yes | DTO has `idempotencyKey`; optional header `X-Idempotency-Key` (body takes precedence) |

- **DTOs:** `CreateInvoiceDto` and `CreatePaymentDto` expose optional `IdempotencyKey` (camelCase in JSON: `idempotencyKey`). Controllers pass the DTO through to the service unchanged; no mapping layer drops the key.
- **Validation:** No validation contradicts idempotency; request body is required; idempotency key is optional.
- **Swagger/OpenAPI:** Controller XML summary and remarks describe idempotency; Swagger shows the optional property and header for API consumers.

### Recommended usage for API consumers

- **Retries / double submit:** For create-invoice and create-payment, send a stable **idempotency key** (e.g. client-generated UUID or deterministic key from your workflow) in the request body as `idempotencyKey`, or in the `X-Idempotency-Key` header. Repeating the same request with the same key (same tenant) returns the existing resource (201 with same `id`); no duplicate is created.
- **Key scope:** Keys are scoped by tenant (company). The same key value in a different company creates a separate invoice/payment.
- **Key choice:** Use a unique value per logical “create” operation (e.g. one key per order you are invoicing, or one key per payment instruction). Do not reuse a key for a different amount or payload; the server will return the first created resource.
- **Without a key:** If you omit `idempotencyKey` and do not send `X-Idempotency-Key`, each request creates a new record (legacy behavior).

---

## 7. Verdict

**Tenant financial operations are hardened and idempotency is in place for invoice and payment creation.** Company is required for financial writes; cross-tenant reads return null; payout snapshot remains one-per-order; audit logs include tenantId and operation. **Duplicate prevention:** When callers supply an optional `IdempotencyKey` (or use automation/email flows that set it), repeated identical requests return the existing invoice or payment instead of creating duplicates. Idempotency is tenant-scoped (key includes companyId). No schema change; existing CommandProcessingLog store is reused. **Partially protected:** Requests without an idempotency key still create a new record per call; protection depends on callers supplying the key for critical paths.

**API rollout:** Controllers pass DTOs through unchanged; optional `X-Idempotency-Key` header is supported; API docs and consumer guidance are in section 6. Integration tests for payment idempotency exist (FinancialIdempotencyApiTests) and require a database provider that supports ExecuteUpdate (e.g. PostgreSQL); in-memory test DB does not support it.
