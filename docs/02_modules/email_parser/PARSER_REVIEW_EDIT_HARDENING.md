# Parser review/edit hardening pass

## Scope

Parser listing **Edit** flow only: open edit modal → enriched get-draft-by-id → display & save. No changes to Create Order page, Order detail/edit pages, or shared order form behavior.

## Files audited

| Area | File | Notes |
|------|------|--------|
| Backend DTOs | `backend/src/CephasOps.Application/Parser/DTOs/ParserDto.cs` | ParsedOrderDraftDto, UpdateParsedOrderDraftDto |
| Backend get-draft-by-id | `backend/src/CephasOps.Application/Parser/Services/ParserService.cs` | GetParsedOrderDraftByIdAsync (partner, building resolution, building name when ID set) |
| Backend update/save | `backend/src/CephasOps.Application/Parser/Services/ParserService.cs` | UpdateParsedOrderDraftAsync |
| API controller | `backend/src/CephasOps.Api/Controllers/ParserController.cs` | GET drafts/{id}, PUT drafts/{id} |
| Frontend API types | `frontend/src/api/parser.ts` | ParsedOrderDraft, UpdateParsedOrderDraftRequest |
| Parser edit flow | `frontend/src/pages/parser/ParserListingPage.tsx` | handleOpenEdit, editForm, modal, handleSaveEdit |

**Not modified (confirmed):** CreateOrderPage, OrderDetailPage, shared order form utilities, hooks used by order create/edit.

## Defects fixed in this pass

1. **Backend – Building name vs BuildingId:** When the draft already had a persisted `BuildingId`, the API still returned `BuildingName` from the draft (parsed text). That could differ from the building’s actual name. **Fix:** In `GetParsedOrderDraftByIdAsync`, when `draft.BuildingId.HasValue`, load the building’s name from the Buildings table and set `dto.BuildingName` so the UI label always matches the persisted building.

2. **Frontend – Race when opening edit on multiple rows:** Quickly opening Edit on row A then row B could let the response for A apply after B’s modal was intended. **Fix:** `openingDraftIdRef` tracks the draft id for the current open; we only call `setEditingDraft` / `setEditForm` when the response `id` matches that ref; ref is cleared in `finally` and on modal close.

3. **Frontend – Fallback form on failed fetch:** On catch we fell back to list-row data but only set a subset of fields, so e.g. `orderTypeCode`, `ticketId`, `buildingId` could be missing. **Fix:** Fallback `setEditForm` now includes all list-row fields (ticketId, awoNumber, oldAddress, orderTypeCode, packageName, bandwidth, onuSerialNumber, onuPassword, voipServiceId, remarks, buildingId).

4. **Frontend – Partner / building display:** Empty or whitespace `partnerCode` or `buildingName` could render blank. **Fix:** Use `editingDraft.partnerCode?.trim() || '—'` and `editingDraft.buildingName?.trim() ?? '—'` so we always show a safe label.

5. **Frontend – Materials safety:** Relying only on `editingDraft.materials.length` and `m.id` could be brittle if `materials` or items were malformed. **Fix:** Guard with `Array.isArray(editingDraft.materials)`, use `m?.id ?? \`m-${idx}\`` for keys, and `m?.name ?? '—'` for display.

## Verification summary

| Check | Result |
|-------|--------|
| No regression to stale list-row data when opening edit | Edit always starts from `getParsedOrderDraft(id)`; fallback only on error and now includes all list fields. |
| Save persists buildingId, orderTypeCode, and other editable fields | UpdateParsedOrderDraftAsync and editForm payload cover buildingId, orderTypeCode, and existing editable fields. |
| Fuzzy building never overwrites existing BuildingId | Resolution runs only when `!draft.BuildingId.HasValue`. |
| Suggested building dropdown only when no BuildingId | Modal branches on `editForm.buildingId` first; dropdown only when no ID and `suggestedBuildings.length > 0`. |
| Parsed building text preserved when no match | When no ID and no suggestions, we show `editingDraft.buildingName` (read-only). |
| Partner parser-derived and read-only | Partner field is disabled/readOnly; UpdateParsedOrderDraftDto has no PartnerId. |
| Materials display-only | No edit controls for materials; section is display-only. |
| Resolved building name matches persisted buildingId | Backend now sets `dto.BuildingName` from Building when `draft.BuildingId` is set. |
| Suggested building selection persists after save | Save sends `editForm.buildingId`; backend persists it; refetch after save returns updated draft. |
| Order subtype bound to draft state | `editForm.orderTypeCode` is set from enriched draft and sent on save. |
| partnerCode null/empty handling | Display uses `?.trim() \|\| '—'`. |
| Materials null/undefined safe | Array.isArray check and optional chaining on items. |

## Order Categories (implemented)

Order Categories support was added in a focused parser-only enhancement:

- **ParsedOrderDraft** (domain): `OrderCategoryId` (nullable `Guid`).
- **ParsedOrderDraftDto** and **UpdateParsedOrderDraftDto**: `OrderCategoryId`.
- Get-draft-by-id and paged/session list mappings include `OrderCategoryId`.
- Update/save persists `OrderCategoryId` (editable in parser edit; can be set or cleared).
- Parser edit UI: dropdown bound to `orderCategoryId`, options from `getOrderCategories({ isActive: true })`.
- Approve flow: `CreateOrderFromDraftDto.OrderCategoryId` is set from the draft when creating the order.
