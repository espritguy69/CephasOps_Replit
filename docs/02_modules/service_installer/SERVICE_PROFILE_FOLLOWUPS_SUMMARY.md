# Service Profile Follow-ups — Summary (2026-03-09)

## A. Audit findings

- **BaseWorkRate:** Entity has optional `ServiceProfileId`; resolution in `ResolveBaseWorkRateFromDbAsync` uses exact OrderCategoryId first, then ServiceProfileId (resolved via OrderCategoryServiceProfile), then broad fallback. **Correct.**
- **RateEngineService:** Resolution order and profile lookup are implemented; `ResolutionMatchLevel` and `MatchedRuleDetails.ServiceProfileId` are populated when BWR is matched by profile.
- **GponSiJobRate / GponSiCustomRate:** No ServiceProfileId column; lookups use OrderCategoryId (and other dimensions). **No change required.**
- **Payroll pipeline:** Uses `ResolveGponRatesAsync` with `OrderCategoryId` from the order; profile-based BWR is used automatically. **No change required.**
- **Cache key:** Base Work Rate cache key previously did **not** include ServiceProfileId. It now does (see C).

## B. Query fixes

No query fixes were required. Rate lookup queries already use the correct dimensions (OrderCategoryId; ServiceProfileId is derived for BWR from OrderCategoryServiceProfile). When ServiceProfileId is null (no mapping), the profile step is skipped; when present, it is resolved and used in BWR matching.

## C. Cache improvements

- **Base Work Rate cache key** now includes **ServiceProfileId**:
  - Format: `BWR:{CompanyId}:{RateGroupId}:{OrderCategoryId}:{ServiceProfileId}:{InstallationMethodId}:{OrderSubtypeId}:{Date}`
  - ServiceProfileId is resolved via `ResolveServiceProfileIdForOrderCategoryAsync` before building the key (or `"n"` when no mapping).
  - Avoids collisions when the same OrderCategoryId has a different profile mapping or when two categories map to different profiles.

## D. Tests added

- **`ResolveGponRatesAsync_ProfileMatch_SetsResolutionMatchLevelServiceProfile`** – When BWR is matched by profile (no exact category row), asserts `ResolutionMatchLevel` is `"ServiceProfile"` and `MatchedRuleDetails.ServiceProfileId` is set.
- **`ResolveGponRatesAsync_SameContextTwice_ConsistentCachedResult`** – Same request twice returns the same payout and source; validates cache correctness when ServiceProfileId is in the key.

Existing tests in `RateEngineServiceServiceProfileResolutionTests.cs` already cover: exact category beats profile, profile match when no exact category row, no profile mapping falls back to legacy, old category-only setup unchanged, custom rate wins over profile-based BWR.

## E. Files modified

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Rates/Services/RateEngineService.cs` | Resolve ServiceProfileId before building BWR cache key; include ServiceProfileId (or `"n"`) in key string. |
| `backend/tests/CephasOps.Application.Tests/Rates/RateEngineServiceServiceProfileResolutionTests.cs` | Add tests: `ResolveGponRatesAsync_ProfileMatch_SetsResolutionMatchLevelServiceProfile`, `ResolveGponRatesAsync_SameContextTwice_ConsistentCachedResult`. |
| `docs/SERVICE_PROFILE_AUDIT_AND_DESIGN.md` | Add §5.1 "Service Profile Rate Resolution Rules" (resolution order, when ServiceProfileId is present/null/not configured, cache key, GponSiJobRate/GponSiCustomRate, payroll). |
| `docs/SERVICE_PROFILE_FOLLOWUPS_SUMMARY.md` | This summary (A–E). |
