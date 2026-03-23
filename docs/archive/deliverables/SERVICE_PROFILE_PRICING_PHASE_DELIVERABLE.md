# Service Profile Pricing Integration — Phase Deliverable

## Executive summary

Base Work Rate now supports **optional Service Profile** pricing alongside existing Order Category pricing. The rollout is **additive and production-safe**: existing OrderCategoryId-based pricing and legacy GponSiJobRate behaviour are unchanged. Exact category pricing always wins over service profile pricing; service profile is used only when there is no matching category row, enabling shared pricing across multiple order categories.

**Validation rule:** On each Base Work Rate row, **at most one** of Order Category or Service Profile may be set (or both null for broad fallback). **Both are not allowed on the same row.**

---

## Resolution precedence (deterministic order)

When resolving Base Work Rate in the rate engine:

1. **GponSiCustomRate** — unchanged; still wins when present.
2. **BaseWorkRate — exact OrderCategoryId match** (four tiers: full → category+method → category+subtype → category-only).
3. **BaseWorkRate — ServiceProfileId match** (after resolving profile from the request’s OrderCategoryId via OrderCategoryServiceProfile mapping; same four tiers with `OrderCategoryId == null`, `ServiceProfileId == resolved`).
4. **BaseWorkRate — broad** (OrderCategoryId and ServiceProfileId both null).
5. **Legacy GponSiJobRate** — unchanged fallback.
6. Rate modifiers applied after base amount — unchanged.

Exact category beats service profile; service profile beats broad generic base only when there is no category row.

---

## Files changed

| Area | File |
|------|------|
| **Domain** | `backend/src/CephasOps.Domain/Rates/Entities/BaseWorkRate.cs` — nullable `ServiceProfileId`, `ServiceProfile` navigation |
| **Infrastructure** | `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Rates/BaseWorkRateConfiguration.cs` — FK, index |
| **Infrastructure** | `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260308134742_AddServiceProfileIdToBaseWorkRate.cs` |
| **Application DTOs** | `backend/src/CephasOps.Application/Rates/DTOs/BaseWorkRateDto.cs` — ServiceProfileId, ServiceProfileName, ServiceProfileCode; Create/Update/ListFilter |
| **Application service** | `backend/src/CephasOps.Application/Rates/Services/BaseWorkRateService.cs` — validation, CRUD, list ordering, MapToDto |
| **Application engine** | `backend/src/CephasOps.Application/Rates/Services/RateEngineService.cs` — ResolveBaseWorkRateFromDbAsync (category → profile → broad), ResolveServiceProfileIdForOrderCategoryAsync |
| **API** | `backend/src/CephasOps.Api/Controllers/GponBaseWorkRatesController.cs` — list query `serviceProfileId` |
| **Frontend types** | `frontend/src/types/rateGroups.ts` — BaseWorkRateDto, Create/Update/Filter ServiceProfile fields |
| **Frontend UI** | `frontend/src/pages/settings/RateGroupsPage.tsx` — Service Profile dropdown, mutual exclusivity, “Applies To” column (exact / shared / broad) |
| **Tests** | `backend/tests/CephasOps.Application.Tests/Rates/BaseWorkRateServiceTests.cs` — CreateAsync_WithBothCategoryAndProfile_Throws, CreateAsync_WithServiceProfileIdOnly_Persists |
| **Tests** | `backend/tests/CephasOps.Application.Tests/Rates/RateEngineServiceServiceProfileResolutionTests.cs` — exact category beats profile, profile fallback, no mapping, legacy, custom rate |

---

## Migration added

- **Name:** `20260308134742_AddServiceProfileIdToBaseWorkRate`
- **Contents:** Add nullable column `ServiceProfileId`, FK to `ServiceProfiles`, index `IX_BaseWorkRates_ServiceProfileLookup` (RateGroupId, ServiceProfileId, InstallationMethodId, OrderSubtypeId). No destructive schema changes.

---

## Both OrderCategoryId and ServiceProfileId on same row?

**No.** Validation enforces at most one of Order Category or Service Profile (or both null). If both are set, create/update throws `ArgumentException` (backend) and the frontend shows: “Use either Order Category (exact pricing) or Service Profile (shared pricing), not both.”

---

## Tests added

- **BaseWorkRateService:** `CreateAsync_WithBothCategoryAndProfile_Throws`, `CreateAsync_WithServiceProfileIdOnly_Persists`, list filter by ServiceProfileId.
- **RateEngineService (Service Profile resolution):** ExactCategoryBeatsProfileMatch, ProfileMatchWhenNoExactCategoryRow, NoProfileMapping_FallsBackToLegacy, OldSetupCategoryOnly_UnchangedPayout, CustomRateStillWinsOverProfileBasedBaseWorkRate (and related).

---

## Confirmation: old setups still behave the same

- Existing Base Work Rate rows have only OrderCategoryId or neither; they are unchanged.
- Resolution still tries exact OrderCategoryId first; only when no category row matches does the engine resolve ServiceProfileId from OrderCategoryServiceProfile and try profile-based rows.
- Cache key is unchanged (companyId, rateGroupId, orderCategoryId, installationMethodId, orderSubtypeId); resolution inside the cache can return a profile-based rate only when there is no category match.
- Legacy GponSiJobRate remains below BaseWorkRate in precedence; GponSiCustomRate remains above. Payout for existing customers with no Service Profile–based rates is unchanged.

---

## Frontend wording

- **Order Category (exact pricing)** — one category; exact match wins in resolution.
- **Service Profile (shared pricing)** — shared pricing family; used when no exact category row exists and the order’s category is mapped to that profile.
- Table column “Applies To” shows: “Order Category: X (exact)”, “Service Profile: Y (shared)”, or “— (broad)”.
