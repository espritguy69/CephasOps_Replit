# Parser Material Resolution Architecture — Deliverables

## 1. Architecture decision

**Why an alias/normalization layer was chosen**

- **Current limitation:** Parser materials come from emails/files with inconsistent names (vendor-specific, abbreviated, misspelled). Exact ItemCode/Description matching leaves many lines unmatched and forces one-off manual fixes that do not help future drafts.
- **Why not warning-only:** Warnings alone do not reduce future unmatched counts; every draft with the same noisy name stays unmatched.
- **Why not “fix current draft only”:** Fixing only the current order/draft means the next draft with the same parsed name is unmatched again. There is no learning.
- **Why an alias table:** A company-scoped alias table maps **noisy parsed name → canonical Material**. Once a user (or admin) resolves e.g. “Legacy ONU Plug” → Material X, that mapping is stored. All future drafts with the same normalized name resolve automatically. The system **learns once, reuses forever**, without changing the rest of the inventory or order domain.

**Why it is better than current-only manual resolve**

- Manual resolve now **creates a reusable alias** (not only updates the current draft/order). So:
  - Current draft/order is fixed.
  - Future drafts with the same parsed text auto-resolve via the alias.
- Unmatched count can **trend downward** as aliases accumulate, while behavior stays deterministic and auditable (no fuzzy/AI in the resolution path).

---

## 2. Files created

| Path | Purpose |
|------|---------|
| `backend/src/CephasOps.Domain/Parser/Entities/ParsedMaterialAlias.cs` | Entity: company-scoped alias (AliasText, NormalizedAliasText, MaterialId, Source, IsActive, etc.). |
| `backend/src/CephasOps.Application/Parser/Utilities/MaterialNameNormalizer.cs` | Deterministic normalization (trim, collapse whitespace) used for matching and alias lookup. |
| `backend/src/CephasOps.Application/Parser/Services/IParsedMaterialAliasService.cs` | Interface: CreateAliasAsync, ListAliasesAsync. |
| `backend/src/CephasOps.Application/Parser/Services/ParsedMaterialAliasService.cs` | Implementation: create alias (normalize, validate material company), list aliases. |
| `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260311120000_AddParsedMaterialAlias.cs` | Migration: creates `ParsedMaterialAliases` table, FKs, index on (CompanyId, NormalizedAliasText). |
| `frontend/src/components/parser/MatchMaterialModal.tsx` | Shared modal: “Match material: ‘{parsedName}'” — select Material, Save (creates alias). Used in parser edit and order detail. |

---

## 3. Files modified

| Path | Purpose |
|------|---------|
| `backend/src/CephasOps.Application/Orders/Services/OrderService.cs` | Resolution order: ItemCode → Description → **alias lookup**; uses `MaterialNameNormalizer` everywhere; `ResolveParsedMaterialToMaterialAsync` and `ApplyParsedMaterialsAsync` / `GetUnmatchedParsedMaterialNamesAsync` updated. |
| `backend/src/CephasOps.Application/Parser/DTOs/ParserDto.cs` | Added: `CreateParsedMaterialAliasRequest`, `ParsedMaterialAliasDto`, `UnmatchedMaterialReviewItemDto`. |
| `backend/src/CephasOps.Application/Parser/Services/IParserService.cs` | Added: `GetRecentUnmatchedMaterialNamesAsync(companyId, limit, ct)`. |
| `backend/src/CephasOps.Application/Parser/Services/ParserService.cs` | Implemented: aggregate recent unmatched names from drafts (UnmatchedMaterialNamesJson, last 90 days), return as `UnmatchedMaterialReviewItemDto`. |
| `backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs` | Registered `DbSet<ParsedMaterialAlias>` and entity config. |
| `backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContextModelSnapshot.cs` | Snapshot updated for ParsedMaterialAlias. |
| `backend/src/CephasOps.Api/Program.cs` | Registered `IParsedMaterialAliasService` → `ParsedMaterialAliasService`. |
| `backend/src/CephasOps.Api/Controllers/ParserController.cs` | POST `api/parser/material-aliases` (create alias), GET `api/parser/material-aliases` (list), GET `api/parser/unmatched-review` (recent unmatched, optional limit). |
| `frontend/src/api/parser.ts` | `createParsedMaterialAlias`, `listParsedMaterialAliases`, types for alias and unmatched review. |
| `frontend/src/pages/parser/ParserListingPage.tsx` | Unmatched list: each name has “Match Material” button; `MatchMaterialModal`; on Save → create alias, refetch draft, toast, close. |
| `frontend/src/pages/orders/OrderDetailPage.tsx` | Parser-origin unmatched block: each name has “Match Material” button; `MatchMaterialModal`; on Save → create alias, success toast (no order refetch). |
| `backend/tests/CephasOps.Application.Tests/Orders/ParsedMaterialMappingTests.cs` | New test: `CreateFromParsedDraft_WithAliasMatch_ResolvesToMaterial` (alias seeded → draft resolves to material). |

---

## 4. Data model changes

**New entity / table**

- **ParsedMaterialAlias** (table `ParsedMaterialAliases`)
  - Id (PK), CompanyId (FK), AliasText, NormalizedAliasText, MaterialId (FK → Materials), CreatedAt, CreatedByUserId, Source (e.g. ParserManualResolve, Seeded, Imported), IsActive.
  - Inherits company-scoped soft-delete where applicable (e.g. IsDeleted on base).

**New fields**

- None on existing entities. Unmatched audit data (UnmatchedMaterialCount, UnmatchedMaterialNames) already existed on order/draft.

**Migration**

- `20260311120000_AddParsedMaterialAlias`: creates table, FK to Materials, index on (CompanyId, NormalizedAliasText), index on MaterialId.

---

## 5. Resolution flow

Final resolution order (unchanged for direct match; alias is additive):

1. **ItemCode** — exact normalized match on Material.ItemCode.
2. **Description** — exact normalized match on Material.Description.
3. **Alias** — lookup in ParsedMaterialAliases by CompanyId + NormalizedAliasText (normalize parsed name first); if found and IsActive, use MaterialId (with company check).
4. **Unmatched** — if still no match, line is recorded as unmatched (audit); no OrderMaterialUsage for that line; order creation still succeeds.

All comparisons use the same `MaterialNameNormalizer.Normalize` (trim, collapse whitespace); description/alias comparison is case-insensitive (e.g. OrdinalIgnoreCase).

---

## 6. UI behavior

**Parser Review / Edit**

- Unmatched parsed materials: warning with count and list of names.
- Each unmatched name has a **“Match Material”** button.
- Click → `MatchMaterialModal` opens with parsed name; user selects Material and saves.
- On Save: create alias via API, refetch current draft, show success toast, close modal; unmatched warning updates (fewer or none if all resolved).

**Parser-origin Order Detail**

- If order has unmatched parsed materials: warning with count and list of names.
- Each name has **“Match Material”** button.
- Click → same `MatchMaterialModal`; on Save: create alias, success toast (“Alias saved. Future drafts will resolve this name automatically.”), close modal. Order snapshot is not refetched (historical snapshot remains).

**Manual resolve**

- Always creates a **reusable alias** (AliasText → MaterialId) for the company.
- Current context (draft or order) is updated where applicable (draft refetched; order detail does not refetch).
- Future drafts with the same parsed name resolve via the alias layer.

---

## 7. Verification results

| Scenario | Result |
|----------|--------|
| **Direct match unchanged** | ItemCode exact match creates OrderMaterialUsage (ParsedMaterialMappingTests). |
| **Description exact match** | Description match creates OrderMaterialUsage (ParsedMaterialMappingTests). |
| **Alias match** | With saved alias for “Legacy ONU Plug” → MaterialId, draft with that name creates single OrderMaterialUsage (CreateFromParsedDraft_WithAliasMatch_ResolvesToMaterial). |
| **Unmatched safe** | All-unmatched draft still creates order; no OrderMaterialUsage for unmatched lines (CreateFromParsedDraft_AllUnmatched_OrderStillSucceeds). |
| **No duplicate rows** | Multiple parsed lines same material → single usage row (CreateFromParsedDraft_MultipleLinesSameMaterial_NoDuplicateRows). |
| **Default + parsed no duplicate** | Building default same material + parsed same material → single row (CreateFromParsedDraft_WhenDefaultWouldAddSameMaterial_DoesNotDuplicate). |
| **Empty/null materials** | Null or empty materials list does not throw (CreateFromParsedDraft_NullOrEmptyMaterials_DoesNotThrow). |
| **Parser edit Match Material** | UI: Match Material opens modal, save creates alias, draft refetched, warning updates (implemented in ParserListingPage). |
| **Order detail Match Material** | UI: Match Material on order detail creates alias, success toast (implemented in OrderDetailPage). |

All 9 ParsedMaterialMappingTests pass (including the new alias test).

---

## 8. Scope confirmation

- **Create Order page (manual flow):** Not touched. No changes to manual Create Order flow.
- **Default material logic:** Preserved. Default materials still apply first; alias-resolved parsed material does not duplicate an existing OrderMaterialUsage.
- **No unrelated refactors:** Changes are limited to parser-origin material resolution, alias entity/service/API, normalization utility, and the two UIs (parser edit, parser-origin order detail). No inventory domain redesign, no new workflow engine, no fuzzy/AI matching in production path.
- **Manual / non-parser orders:** Unaffected. Parser alias UI only appears where parser-origin unmatched materials are shown (parser draft edit and parser-origin order detail).

---

## Anti-drift checklist

- No giant rules engine added.
- No fuzzy matching with hidden behavior.
- No automatic alias generation from guesses (alias only from explicit manual resolve or seeded/imported data).
- Inventory/material master not redesigned.
- Non-parser order flows not touched.
- Existing parser material mapping (ItemCode/Description) not rewritten; alias layer is additive.
- Minimal new surface: alias table, one normalizer, alias service + three parser API endpoints, one shared modal, and two call sites (parser edit + order detail).
