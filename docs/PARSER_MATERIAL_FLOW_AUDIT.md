# Parser pipeline – material flow audit

**Scope:** Parser draft materials → review/edit display → approve/create-from-draft mapping → final order material persistence.  
**Date:** 2026-03-09.

---

## 1. Where are parsed materials stored in the parser draft domain/entity?

**Answer:** In the **parser draft entity** only, as a single JSON column.

| Location | Detail |
|----------|--------|
| **Entity** | `ParsedOrderDraft` |
| **Property** | `ParsedMaterialsJson` (`string?`) |
| **Table** | `ParsedOrderDrafts` |
| **Files** | `backend/src/CephasOps.Domain/Parser/Entities/ParsedOrderDraft.cs` (lines 143–147), `ParsedOrderDraftConfiguration.cs`, migration `20251129161619_AddParsedMaterialsJson.cs` |

Serialization is via `ParsedMaterialsSerializer` (backend `CephasOps.Application/Parser/Utilities/ParsedMaterialsSerializer.cs`); payload is a JSON array of `ParsedDraftMaterialDto`.

---

## 2. Are parsed materials only display data, or are they persisted in the draft?

**Answer:** They are **persisted in the draft**.

- **Parser (file upload):** `ParserService` maps `ParsedOrderData.Materials` → `ParsedDraftMaterialDto` and writes with `ParsedMaterialsSerializer.Serialize` into `draft.ParsedMaterialsJson`; draft is saved (`ParserService.cs` ~1287–1303).
- **Email ingestion:** `EmailIngestionService` does the same mapping and assigns `draft.ParsedMaterialsJson` (~1229–1245); draft is saved.

So parsed materials are stored in the DB on the draft. They are **not editable** in the parser UI: `UpdateParsedOrderDraftRequest` has no `materials` field, so the review/edit form only displays them (read-only).

---

## 3. On approve/create-from-draft, are parsed materials mapped into the created order?

**Answer: No.** Parsed materials are **not** mapped into the created order.

- **CreateOrderFromDraftDto** has **no** `Materials` (or similar) property (`CreateOrderFromDraftDto.cs`).
- **ParserService.ApproveParsedOrderDraftAsync** builds `CreateOrderFromDraftDto` from the draft (~729–755) and does **not** set any materials; it never reads `draft.ParsedMaterialsJson` for the create DTO.
- **OrderService.CreateFromParsedDraftAsync** only creates the `Order` and then calls **ApplyDefaultMaterialsAsync** (building defaults and/or material templates). It never receives or uses parsed materials.

So: **draft → order creation does not pass or persist parsed materials.**

---

## 4. If yes, into which structure? (Actual answer: not applicable – they are not mapped)

Because no mapping is done, this does not apply. For completeness, the **only** order-side structure that represents “materials on an order” is:

| Structure | Purpose | Used in create-from-draft? |
|-----------|---------|----------------------------|
| **OrderMaterialUsage** | Materials required/used per order (MaterialId, Quantity, UnitCost, etc.) | Yes, but only via **ApplyDefaultMaterialsAsync** (building defaults + material templates). Parsed materials are never written here. |
| OrderMaterialReplacement | Serialised RMA replacements | No (Assurance/RMA flow). |
| OrderNonSerialisedReplacement | Non-serialised replacements | No. |
| Customer premise materials | N/A in codebase for order creation | — |
| “Missing materials” table | None; “Missing Material” is a UI label only | — |

So there is **no** customer premise materials table, no separate “missing materials” table, and no other order entity that receives parsed materials.

---

## 5. Are quantity, unit, serial number, and material name preserved?

| Field | In draft (persisted) | In create-from-draft / order |
|-------|----------------------|------------------------------|
| **Material name** | Yes – `ParsedDraftMaterialDto.Name` → stored in `ParsedMaterialsJson` | Not mapped to order. |
| **Quantity** | Yes – `Quantity` (decimal?) | Not mapped. |
| **Unit** | Yes – `UnitOfMeasure` (string?) | Not mapped. |
| **Serial number** | **No** – not in `ParsedDraftMaterialDto` or `ParsedOrderMaterialLine`. Only ONU serial exists at order/draft level (`OnuSerialNumber`). | N/A. |

So name, quantity, and unit are preserved **only in the draft**. Serial number is not part of the parsed-materials model at all.

---

## 6. Is “Missing Material” a real persisted field/workflow or only a display section?

**Answer: Display-only.**

- There is **no** “Missing Material” entity, table, or workflow in the backend.
- The parser UI uses the label **“Missing Material / Parsed materials”** for the same list that comes from `editingDraft.materials` (i.e. from `ParsedMaterialsJson`).  
  File: `frontend/src/pages/parser/ParserListingPage.tsx` (e.g. ~971–984); comment: “display-only from parser snapshot”.
- So “Missing Material” is a **section label** for parsed materials, not a separate persisted or workflow concept.

---

## 7. Parsed materials not mapped into final order – patch only if existing target exists

**Existing target:** **OrderMaterialUsage** is the only order-domain structure for “materials on an order”. It requires:

- `OrderId`, `MaterialId` (FK to `Material`), `Quantity`, optional cost/notes, etc.

**Gap:** Parsed materials are free text (e.g. `Name`) and are not linked to `MaterialId`. So we cannot copy them into `OrderMaterialUsage` without a **name → Material** resolution step.

**Conclusion:**  
- Do **not** invent a new table or workflow.  
- A **minimal patch within current architecture** would be: **optionally** map parsed materials into `OrderMaterialUsage` by resolving **Name** to **Material** (e.g. by `Material.Description` or `Material.ItemCode` within the same company), then create `OrderMaterialUsage` only for resolved materials; leave unresolved lines as display-only (still shown from draft via existing order-detail behaviour). That patch is **not** implemented in this audit; see “Optional minimal patch” below if you want to add it.

---

## 8. Verification summary

- **Open draft with parsed materials:** Draft has `ParsedMaterialsJson`; API returns `materials` from it (`ParserService` single-draft and list endpoints).
- **Approve draft:** `ParserService` builds `CreateOrderFromDraftDto` without materials → `OrderService.CreateFromParsedDraftAsync` creates order and runs only `ApplyDefaultMaterialsAsync`. Parsed materials are never read for order creation.
- **Inspect created order:** Order has no parsed materials on the order entity. When loading the order for display, **OrderService.GetOrderByIdAsync** loads **ParsedMaterials** from the **draft** where `CreatedOrderId == order.Id` and attaches them to **OrderDto** for UI. So on the Order Detail page, “parsed materials” are shown from the draft, not from any order table.
- **Materials in persisted order records:** **OrderMaterialUsage** rows exist only from building defaults / material templates. **No** rows are created from parser draft materials.
- **Regression:** No changes were made to building, partner, category, or subtype handling; this audit is read-only and does not modify Create Order page or unrelated order UI.

---

## Files audited (exact paths)

| Area | Files |
|------|--------|
| Draft domain | `backend/src/CephasOps.Domain/Parser/Entities/ParsedOrderDraft.cs` |
| Parser DTOs | `backend/src/CephasOps.Application/Parser/DTOs/ParserDto.cs` (ParsedDraftMaterialDto), `ParsedOrderData.cs` (ParsedOrderMaterialLine), `CreateOrderFromDraftDto.cs` |
| Serializer | `backend/src/CephasOps.Application/Parser/Utilities/ParsedMaterialsSerializer.cs` |
| Parser service | `backend/src/CephasOps.Application/Parser/Services/ParserService.cs` (mapping to draft, approve flow, createDto build, list/single draft DTO mapping), `EmailIngestionService.cs` (draft material mapping) |
| Order creation | `backend/src/CephasOps.Application/Orders/Services/OrderService.cs` (CreateFromParsedDraftAsync, ApplyDefaultMaterialsAsync, GetOrderByIdAsync parsed materials from draft, MapToOrderDto) |
| Order DTO | `backend/src/CephasOps.Application/Orders/DTOs/OrderDto.cs` (ParsedMaterials) |
| Order domain | `backend/src/CephasOps.Domain/Orders/Entities/Order.cs`, `OrderMaterialUsage.cs`, `OrderMaterialReplacement.cs` |
| Frontend parser | `frontend/src/pages/parser/ParserListingPage.tsx` (Missing Material / Parsed materials block), `frontend/src/api/parser.ts` (ParsedOrderDraft, ParsedDraftMaterial, UpdateParsedOrderDraftRequest) |
| Frontend order | `frontend/src/types/orders.ts` (ParsedMaterial), `frontend/src/pages/orders/OrderDetailPage.tsx` (parsed materials display) |
| Config / migrations | `ParsedOrderDraftConfiguration.cs`, `20251129161619_AddParsedMaterialsJson.cs` |

---

## Current material behaviour (summary)

| Stage | Behaviour |
|-------|-----------|
| Parse (file/email) | Materials from parser result are mapped to `ParsedDraftMaterialDto`, serialized into `ParsedOrderDraft.ParsedMaterialsJson`, and saved. |
| Parser review/edit | Materials are shown in “Missing Material / Parsed materials” from draft; not editable (no update API for materials). |
| Approve / create-from-draft | Draft is converted to `CreateOrderFromDraftDto` **without** materials. Order is created; only **ApplyDefaultMaterialsAsync** runs (building + template). Parsed materials are **not** written to any order table. |
| Order detail view | Parsed materials on the order DTO are loaded from the **draft** where `CreatedOrderId == order.Id` and displayed; they are **not** stored on the order. |

---

## Real defect(s)

1. **Parsed materials never reach the order:** Approve/create-from-draft does not pass or persist parsed materials. They remain only on the draft and are shown on the order by re-reading the draft.
2. **No serial number in parsed materials:** Parsed material lines do not carry a serial number; only ONU serial is at order/draft level.

---

## Optional minimal patch (within existing architecture)

Only if you want parsed materials to be reflected in **OrderMaterialUsage** without new tables:

1. **CreateOrderFromDraftDto**  
   Add e.g. `List<ParsedDraftMaterialDto>? Materials { get; set; }`.

2. **ParserService.ApproveParsedOrderDraftAsync**  
   When building `createDto`, set  
   `Materials = ParsedMaterialsSerializer.Deserialize(existing.ParsedMaterialsJson)`  
   (or from the same draft entity you already load).

3. **OrderService.CreateFromParsedDraftAsync**  
   After `ApplyDefaultMaterialsAsync`, if `dto.Materials != null`:  
   - For each item, try to resolve `Name` to a `Material` in the same company (e.g. by `Description` or `ItemCode` equals/contains).  
   - For each resolved material, add an **OrderMaterialUsage** with `MaterialId`, `Quantity` (from parsed line or 1), `Notes` (e.g. parsed notes or “From parser”), and existing cost/audit fields.  
   - Do **not** create rows for unresolved names (no new “free-text material” entity).

This keeps everything within the existing Order + OrderMaterialUsage + Material model and does not introduce a new workflow or table.

---

## Implementation (minimal parser-to-order material persistence)

**Date:** 2026-03-09.

### Files changed

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Parser/DTOs/CreateOrderFromDraftDto.cs` | Added `List<ParsedDraftMaterialDto>? Materials { get; set; }`. |
| `backend/src/CephasOps.Application/Parser/Services/ParserService.cs` | When building `CreateOrderFromDraftDto` in approve flow (new-order path), set `Materials = existing.Materials != null && existing.Materials.Count > 0 ? existing.Materials.ToList() : null`. |
| `backend/src/CephasOps.Application/Orders/Services/OrderService.cs` | After `ApplyDefaultMaterialsAsync`, call `ApplyParsedMaterialsAsync(orderId, companyId, userId, dto.Materials, cancellationToken)` when `dto.Materials` is non-empty. Added `NormalizeMaterialName`, `ResolveParsedMaterialToMaterialAsync`, and `ApplyParsedMaterialsAsync`. |

### Mapping strategy (material resolution)

- **Normalize parsed name:** Trim and collapse internal whitespace (single space between words). Empty after normalize → skip and log as unmatched.
- **Resolve to Material:** Company-scoped lookup. Prefer **ItemCode** match (normalized, case-insensitive); if none, then **Description** match (normalized, case-insensitive). First match only; no new Material records created.
- **Preserve:** Parsed quantity (or 1 if null/≤0), parsed unit and notes stored in `OrderMaterialUsage.Notes` (e.g. `"From parser; Unit: pcs; …"`). Name is not stored on usage (MaterialId links to Material).

### Duplicates / default overlap

- **Order of operations:** `ApplyDefaultMaterialsAsync` runs first (building defaults and/or material templates). Then `ApplyParsedMaterialsAsync` runs.
- **No duplication:** Before adding a parsed material, we load existing `OrderMaterialUsage` for the order and skip creating a row if that `MaterialId` is already present (e.g. from defaults). So the same material is not added twice.
- Parsed materials are **additive**; building default behaviour is unchanged.

### Unmatched parsed materials

- Not resolved to any Material (no ItemCode/Description match for the company). We **do not** create OrderMaterialUsage or new materials.
- Logged with `_logger.LogWarning` and company ID + list of unmatched names. Order creation still succeeds.

### Before / after summary

| Before | After |
|--------|--------|
| Parsed materials only in draft (`ParsedMaterialsJson`). Order had no OrderMaterialUsage from parser. | Draft materials passed in `CreateOrderFromDraftDto.Materials`. Resolved materials are added as `OrderMaterialUsage` after defaults. |
| Order material rows only from building defaults / material templates. | Order material rows = defaults/templates + resolved parsed materials (no duplicate MaterialId per order). |
| Unmatched parser lines had no effect. | Unmatched parser lines logged; no rows created; order creation unchanged. |

---

## Hardening pass (final)

**Date:** 2026-03-09.

### Files checked

| Area | Files |
|------|--------|
| DTO | `backend/src/CephasOps.Application/Parser/DTOs/CreateOrderFromDraftDto.cs` |
| Parser approve | `backend/src/CephasOps.Application/Parser/Services/ParserService.cs` (createDto build, Materials assignment) |
| OrderService | `backend/src/CephasOps.Application/Orders/Services/OrderService.cs` (CreateFromParsedDraftAsync, ApplyParsedMaterialsAsync, NormalizeMaterialName, ResolveParsedMaterialToMaterialAsync, OrderMaterialUsage creation) |
| Tests | `backend/tests/CephasOps.Application.Tests/Orders/OrderCreationFromDraftTests.cs`, `ParserServiceIntegrationTests.cs` |

### Verification (1–10)

1. **ItemCode match** – ResolveParsedMaterialToMaterialAsync prefers ItemCode (normalized, case-insensitive); covered by test.
2. **Description match** – Same method falls back to Description; covered by test.
3. **Existing default materials not duplicated** – existingSet built from OrderMaterialUsage before loop; skip when existingSet.Contains(material.Id). Test added: default + parsed same material → one row.
4. **Multiple parsed lines → same MaterialId** – After adding usage, existingSet.Add(material.Id); second line skipped. Test added.
5. **Empty/whitespace names** – NormalizeMaterialName returns null; we skip and add "(empty or whitespace)" to unmatched. Test added.
6. **Unmatched logged** – LogWarning with companyId and Unmatched list. No code change.
7. **Order succeeds when all unmatched** – ApplyParsedMaterialsAsync never throws. Test added.
8. **Quantity** – pm.Quantity ?? 1; if ≤0 then 1. Test added for null/zero → 1.
9. **Unit/notes** – Notes = "From parser" + Unit + parsed Notes. Acceptable and explicit. No change.
10. **No regression to default application** – ApplyDefaultMaterialsAsync unchanged and runs first. No change.

### Defects found

- **None** that required a logic fix. One **clarity improvement**: when parsed name normalizes to empty, log "(empty or whitespace)" instead of the raw value so logs are unambiguous.

### Minimal patches applied

| File | Change |
|------|--------|
| `OrderService.cs` | When normalized name is empty, add "(empty or whitespace)" to unmatched list instead of pm.Name. |
| `ParsedMaterialMappingTests.cs` | New test class: ItemCode match, Description match, all unmatched still succeeds, multiple same material no duplicate, empty/whitespace skipped, quantity null/zero → 1, null/empty materials list does not throw, default + parsed same material no duplicate. Fixed test that used same ServiceId twice (unique ServiceIds per order). |

### Production-safe statement

**Parsed material mapping is production-safe** for the in-scope flow (parser draft approve/create-from-draft only), with the following behavior and limits:

- Resolved materials (by ItemCode or normalized Description, company-scoped) are persisted as OrderMaterialUsage; quantity and notes are preserved; no duplicate rows per MaterialId; defaults are applied first and not overridden.
- Unmatched or empty/whitespace parsed lines do not create records and do not fail order creation; they are logged.
- No new tables or workflows; no changes to Create Order page or shared order forms. Default material application is unchanged.

---

## Explicit conclusion

- **Display-only vs fully persisted end-to-end**  
  Parsed materials are **persisted in the draft** (ParsedMaterialsJson) and **displayed** on parser review and on order detail (order detail still reads from draft for display). **After the implementation above,** parsed materials that resolve to an existing Material (by ItemCode or Description) are **also** persisted as **OrderMaterialUsage** when creating an order from draft. Unresolved parsed materials remain display-only (draft + order detail from draft); they are logged and do not create order records.
