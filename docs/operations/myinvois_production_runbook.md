# MyInvois Production Runbook

**Related:** [Billing & MyInvois](../business/billing_myinvois_flow.md) | [Integrations overview](../integrations/overview.md) | [Scope not handled](../operations/scope_not_handled.md)

**Source of truth:** Codebase; InvoiceSubmissionService; IntegrationSettingsController; GlobalSettings.

---

## 1. Purpose

This runbook guides finance and ops through MyInvois (LHDN e-invoice) configuration, submission, status polling, and rejection handling for GPON production.

---

## 2. Credential Setup

### 2.1 Required values

| Setting | Description | Example |
|---------|-------------|---------|
| MyInvois_Enabled | Enable MyInvois integration | `true` |
| MyInvois_BaseUrl | API base URL (sandbox vs production) | Sandbox: `https://api-sandbox.myinvois.hasil.gov.my`; Production: `https://api.myinvois.hasil.gov.my` |
| MyInvois_ClientId | LHDN-issued Client ID | (from LHDN portal) |
| MyInvois_ClientSecret | LHDN-issued Client Secret | (encrypted in DB) |
| EInvoice_Enabled | Enable e-invoice submission | `true` |
| EInvoice_Provider | Provider name | `MyInvois` (or `Null` to disable) |

### 2.2 Where to configure

- **UI:** Settings → Integrations → MyInvois tab (`/settings/integrations`). Enter Base URL, Client ID, Client Secret; enable MyInvois; use "Save" and "Test connection".
- **API:** `POST /api/integration-settings/myinvois` (UpdateMyInvoisSettings).
- **Database:** GlobalSettings table (keys above). Values may be encrypted via IEncryptionService.

### 2.3 Environment variables

Production should **not** rely on fallback keys. Ensure:

- `SYNCFUSION_LICENSE_KEY` is set (no fallback in production).
- No hardcoded secrets in appsettings or code.

---

## 3. Submission Flow

1. **Invoice created** → Order status ReadyForInvoice → Invoiced.
2. **Admin submits** → Invoice submission via MyInvois provider.
3. **Record submission** → InvoiceSubmissionService.RecordSubmissionAsync stores SubmissionId, Status=Submitted.
4. **Order status** → May transition to SubmittedToPortal when submission succeeds.
5. **Status poll job** → MyInvoisStatusPoll job runs on schedule; polls LHDN for status updates.

### 3.1 Submission API

- **Provider:** MyInvoisApiProvider (Infrastructure.Services.External).
- **Flow:** Build XML → POST to MyInvois API → Parse response → Record submission history.
- **Invoice fields:** Customer details, line items, tax, amounts per LHDN schema.

---

## 4. Status Polling Behavior

- **Job type:** `myinvoisstatuspoll`.
- **Payload:** Submission history ID.
- **Schedule:** Configured via BackgroundJobProcessorService / job scheduler.
- **Action:** Calls InvoiceSubmissionService or provider to get current status from MyInvois.
- **Outcomes:** Accepted, Rejected, Pending. Status written to InvoiceSubmissionHistory; order may transition to Rejected (Invoice Rejected) or Completed.

---

## 5. Rejection Handling

| Scenario | Action |
|----------|--------|
| MyInvois rejects invoice | Order status → Rejected (Invoice Rejected). Admin: correct details. |
| Path 1: Full regeneration | Rejected → ReadyForInvoice → regenerate invoice → Invoiced → resubmit. |
| Path 2: Simple correction | Rejected → Reinvoice → correct in TIME/partner portal → Reinvoice → Invoiced. |
| Agent mode | AgentModeService.HandlePaymentRejectionAsync for payment rejection; transitions order to Reinvoice. |

### 5.1 Rejection reasons (common)

- Wrong BOQ, rate, or job category.
- Missing docket/details/documents.
- Duplicate submission.
- Incorrect splitter/ONU/submissionId.

---

## 6. How Finance Verifies Success

1. **Invoice detail page** → Check submission history (SubmissionId, Status, ResponseMessage).
2. **Order status** → SubmittedToPortal → Completed when payment matched.
3. **MyInvois portal** → Log in to LHDN MyInvois portal; verify invoice appears and status.
4. **Background jobs** → Admin → Background Jobs; ensure MyInvoisStatusPoll runs and completes.

---

## 7. Known Failure Modes

| Failure | Cause | Mitigation |
|---------|-------|------------|
| Connection timeout | Network, firewall | Check outbound HTTPS to MyInvois; whitelist API host. |
| 401 Unauthorized | Invalid ClientId/ClientSecret | Re-enter credentials; ensure no extra whitespace. |
| 400 Bad Request | Invalid XML / schema | Check InvoiceXmlBuilder; validate against LHDN schema. |
| Duplicate submission | Same invoice submitted twice | Ensure only one active submission per invoice. |
| Status poll never runs | Job not scheduled | Verify BackgroundJobProcessorService; add MyInvoisStatusPoll to job types. |
| Provider is Null | EInvoice_Provider = Null | Set to MyInvois in IntegrationSettings. |

---

## 8. Test Connection

- **API:** `POST /api/integration-settings/myinvois/test` (TestMyInvoisConnection).
- **UI:** Integrations page → "Test connection" button.
- **Expect:** Success or failure message; no actual submission.

---

## 9. Out of Scope

- Partner portal (TIME) API: no integration; manual docket/invoice submission at partner portal.
- Payment gateway: payment tracking is internal; no Stripe/PayPal.
- Statutory payroll: EPF, SOCSO, PCB not calculated.

---

**Last updated:** 2026-02-09
