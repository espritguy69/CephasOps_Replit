# Template Governance — ParserTemplates (Process Only)

Operational governance for email/attachment order parsing using the existing **ParserTemplates** table. No schema changes; process and documentation discipline only.

**Existing columns:** Code, PartnerId, OrderTypeCode, ExpectedAttachmentTypes, SubjectPattern, PartnerPattern, AutoApprove, Priority, IsActive, Description.

---

## 4.1 Partner-specific template profiles

- Treat **ParserTemplate** as the single row per **“partner + order type”** profile.
- Use **PartnerId** and **Code** together (e.g. TIME_FTTH, TIME_ASSURANCE) to identify each profile.
- Use **Description** for a short, human-readable summary of the template (partner, order type, and any special expectations).
- No schema change; this is how the table is used operationally.

---

## 4.2 Sheet preference and header expectations

- **ExpectedAttachmentTypes** holds allowed attachment types (e.g. `.xls,.xlsx`). There is no column for “sheet name” or “header row.”
- **Operational documentation:** Keep sheet/header expectations in a doc or in **ParserTemplate.Description**, e.g. *“Preferred sheet: Sheet1; header usually row 2.”*
- The parser uses deterministic sheet selection (e.g. highest score). If a partner consistently uses a specific sheet name, document it so that when failures spike, you can compare **ValidationNotes** (e.g. `Sheet=...`, `HeaderRow=...`) from ParsedOrderDrafts to this description.

---

## 4.3 Label normalization rules

- The parser uses **ExcelLabelNormalizer** and synonym sets in code; there is no table-driven normalization in the current schema.
- **Governance:** Document label expectations per template in **ParserTemplate.Description**, e.g. *“Expects ‘Customer Name’ or ‘CUSTOMER NAME’.”*
- When labels or sheet behaviour change, update Description and optionally record **UpdatedByUserId** or a last-reviewed date in an external process (versioning discipline).

---

## 4.4 Preventing confidence regression and handling vendor drift

- **Confidence:** Monitor the average **ConfidenceScore** per **ParserTemplate.Code** (or PartnerId) week-over-week. If the score drops beyond an agreed threshold, investigate (e.g. new missing fields, new sheet layout) using ValidationNotes and the NeedsReview SOP.
- **Vendor format drift:** When recurring failures for a template appear (see NeedsReview SOP — detecting template changes and escalating vendor format issues), review **ValidationNotes** (Missing, Sheet, HeaderRow). If drift is confirmed:
  - Update **ParserTemplate** as needed (e.g. Description, ExpectedAttachmentTypes).
  - Document the change.
- No parser code change is required; governance is process plus optional template metadata.

---

## 4.5 Versioning templates operationally

- Use **ParserTemplate.UpdatedAt** and **UpdatedByUserId** as operational versioning when you change a template.
- **Optional:** Maintain a “Template change log” (external doc or future table) with Code, change date, and a short summary (e.g. “ExpectedAttachmentTypes updated for .xls”).
- For this plan, versioning is the discipline of updating **Description** and **UpdatedAt** whenever template behaviour or expectations change.

---

## Related docs

- **NeedsReview SOP** — `docs/operations/NeedsReview_SOP.md`
- **Monitoring** — `docs/operations/monitoring_queries.sql` and alert conditions
