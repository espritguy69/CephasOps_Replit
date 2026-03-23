# NeedsReview Workflow — Standard Operating Procedure

Process-only guidance for triaging parsed order drafts in **NeedsReview** or **Rejected** status. No schema or parser changes; uses existing **ParsedOrderDrafts**, **ParseSessions**, **EmailMessage**, and **EmailAttachment** tables.

---

## 3.1 Triage by outcome

Classify each NeedsReview/Rejected draft by the **ParseStatus** or failure type (from **ValidationNotes** or **ValidationStatus**), then follow the actions below.

### FailedRequiredFields

**What it is:** Required fields are missing (e.g. ServiceId/TicketId, CustomerName, ServiceAddress, CustomerPhone). The draft may include an audit line in ValidationNotes like `ParseStatus=FailedRequiredFields; Missing=ServiceId,CustomerPhone`.

**Actions:**

1. **Confirm the attachment** — Ensure the source file (EmailAttachment via FileId/StoragePath, or upload) is correct and not corrupted.
2. **If template/partner expected** — Check whether the vendor changed format (sheet name, header row, or column labels). Compare ValidationNotes (e.g. `Sheet=`, `HeaderRow=`, `Missing=`) with the parser template’s documented expectations (see Template Governance).
3. **If one-off** — Use manual entry or request a resend from the sender.

### FailedValidation

**What it is:** Required fields are present but validation failed (e.g. phone format, extra business rules). ValidationNotes usually list the validation issues.

**Actions:**

1. **Fix in UI** — Correct the draft in the application and resubmit.
2. **If the pattern repeats** — Consider adjusting validation rules or template (e.g. label normalization, expected formats) and document in Template Governance.

### ParseError

**What it is:** The parser threw an exception (file open failed, password-protected, corrupt file). ParseSession.Status may be **Failed** and ParseSession.ErrorMessage or EmailMessage.ParserError will have details.

**Actions:**

1. **Check file in storage** — Use EmailAttachment.FileId / StoragePath (or upload equivalent) to inspect the blob. Confirm whether the file is password-protected or corrupt.
2. **If password-protected or corrupt** — Request a new file from the sender; document the outcome in the runbook.

---

## 3.2 Detecting template changes

**Goal:** Spot when a partner or template’s format has changed so you can update templates or escalate.

- **Frequency:** Run daily or weekly.
- **Method:** Use SQL (or reports built from it) on **ParsedOrderDrafts**:
  - Group by **OrderTypeCode** and/or **SourceFileName** pattern (or **PartnerId** when set).
  - Inspect **ValidationNotes** for patterns such as `Missing=...` or `Sheet=...` or `HeaderRow=...`.
- **Red flag:** A **new** `Missing=...` or `Sheet=` (or HeaderRow) pattern for a given partner/template that didn’t appear before → possible vendor format change.
- **Cross-check:** Use **ParserTemplates** (Code, ExpectedAttachmentTypes, SubjectPattern). When failure rate for a template code spikes, compare ValidationNotes and **ConfidenceScore** to the previous week. If new missing fields or different sheet names appear, treat as template/format drift and follow Template Governance (Part 4).

---

## 3.3 Escalating vendor format issues

- **Threshold (example):** Same partner (PartnerId or inferred from template) with **more than N failures in 7 days** with the **same** MissingRequiredFields set or the **same** ValidationNotes pattern (e.g. same `Missing=...`).
- **Action:** Create a ticket (or runbook entry) for **“Vendor format change / template update”**.
- **Record outcome:** Store the escalation in your runbook or ticket system. Optionally record in **ParserTemplate.Description** or an external doc: “Known issue as of date X” and what was done.

---

## 3.4 Tracking recurring missing fields

**Goal:** Identify which missing-field combinations happen most often so you can prioritise template or label normalization updates.

- **Data source:** **ParsedOrderDrafts** with **ValidationStatus = 'NeedsReview'** (and optionally Rejected).
- **Method:** Parse **ValidationNotes** for tokens like `Missing=...` or “required” and group by:
  - **OrderTypeCode**
  - **SourceFileName** (or first attachment name pattern)
  - The set of missing fields (e.g. from `Missing=ServiceId,CustomerPhone`).
- **Output:** A report of “Top missing-field combinations by partner/template” (see Monitoring SQL for a related query).
- **Use:** Decide which template or label normalization updates to do next (Part 4 — Template Governance).

---

## Related docs

- **Template Governance** — `docs/operations/Template_Governance.md`
- **Monitoring** — `docs/operations/monitoring_queries.sql` and alert conditions
- **Secrets and connection string** — `docs/operations/SECRETS_AND_CONNECTION_STRING.md`
