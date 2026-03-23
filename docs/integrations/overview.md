# Integrations Overview

**Related:** [Product overview](../overview/product_overview.md) | [Billing & MyInvois](../business/billing_myinvois_flow.md) | [Background jobs](../operations/background_jobs.md) | [02_modules/email_parser/SETUP](../02_modules/email_parser/SETUP.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. Email

- **Inbound:** Partner work orders (e.g. TIME) received via **POP3/IMAP**; parser creates order drafts. Email accounts and templates configurable; ingestion scheduled via background job.
- **Outbound:** Notifications (when configured); optional email delivery for alerts.
- **Reschedule:** TIME approval is received by email (no API); admin mirrors in system.

---

## 2. WhatsApp

- **Docket submission:** SI or admin can receive/send dockets via WhatsApp. Same-day reschedule evidence can be screenshot/voice note.
- **Templates:** WhatsApp templates configurable (e.g. SmsWhatsAppSettings). Providers: **Twilio**, **WhatsApp Cloud API**; **null provider** to disable.

---

## 3. SMS

- **Alerts and notifications** (e.g. low stock, critical alerts) when SMS channel enabled.
- **SMS gateways** and **templates** configurable; **Twilio** or generic gateway; **null provider** to disable.

---

## 4. E-invoicing (MyInvois)

- **Invoice submission** to LHDN MyInvois.
- **Status polling** via background job after submission.
- Compliant e-invoice generation and tracking; rejection/reinvoice handled in billing flow.

---

## 5. File storage / OneDrive

- **Local file** storage for uploads.
- **OneDrive sync** (IOneDriveSyncService): File entity has OneDriveFileId, OneDriveSyncStatus, OneDriveSyncedAt, OneDriveWebUrl, etc. Used for document/file sync where configured.

---

## 6. Partner portals (e.g. TIME)

- **No API integration.** TIME portal is reference only; admin updates statuses in CephasOps manually with evidence from the portal. Docket upload and invoice submission to partner are manual steps at the portal; CephasOps tracks readiness and (for invoice) MyInvois submission.
- **Manual process:** See [docs/operations/partner_portal_manual_process.md](../operations/partner_portal_manual_process.md) for step-by-step docket and invoice submission to partner.
