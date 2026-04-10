# Service Profile Engine Integration — Status and Optional Follow-up

**Source:** [SERVICE_PROFILE_AUDIT_AND_DESIGN.md](SERVICE_PROFILE_AUDIT_AND_DESIGN.md) §5 and §10 (Risks / follow-up).  
**Purpose:** Record current implementation status and remaining optional work.

---

## 1. Current status

The **engine integration** described in the audit as "follow-up" is **already implemented** in the codebase.

| Follow-up item | Status | Notes |
|----------------|--------|--------|
| (a) Add optional ServiceProfileId to BaseWorkRate and resolution logic with fallback | **Done** | [BaseWorkRate.cs](backend/src/CephasOps.Domain/Rates/Entities/BaseWorkRate.cs) has `ServiceProfileId`. [RateEngineService.ResolveBaseWorkRateFromDbAsync](backend/src/CephasOps.Application/Rates/Services/RateEngineService.cs) implements order: exact OrderCategoryId → ServiceProfileId match (via `ResolveServiceProfileIdForOrderCategoryAsync`) → broad fallback. |
| (b) Cache key/expiry when profile-based resolution added | **Done** | Cache key in `ResolveBaseWorkRateAsync` is by request dimensions `(companyId, rateGroupId, orderCategoryId, installationMethodId, orderSubtypeId, date)`. Resolution inside the cache callback may use category or profile; key does not need to change. |
| (c) Tests for "exact category wins over profile" and "profile fallback" | **Done** | [RateEngineServiceServiceProfileResolutionTests.cs](backend/tests/CephasOps.Application.Tests/Rates/RateEngineServiceServiceProfileResolutionTests.cs) covers exact category precedence and profile fallback when no category row. |
| (d) Optional feature flag or company setting for profile-based resolution | **Pending** | Not implemented. Optional hardening if you want to disable profile resolution per company or via feature flag. |

---

## 2. Optional follow-up

- **Doc update:** In [SERVICE_PROFILE_AUDIT_AND_DESIGN.md](SERVICE_PROFILE_AUDIT_AND_DESIGN.md) §10, you can mark follow-up (a), (b), (c) as done and leave (d) as optional.
- **Feature flag / company setting:** If required, add a company-level or global setting (e.g. "UseServiceProfileInBaseWorkRate") and in `ResolveBaseWorkRateFromDbAsync` skip the ServiceProfileId resolution branch when the flag is false.

---

## 3. Cursor todos alignment

- **serviceprofile-bwr:** Can be marked **completed**; implemented.
- **serviceprofile-cache:** Can be marked **completed**; cache key is correct.
- **serviceprofile-tests:** Can be marked **completed**; tests exist.
- **serviceprofile-flag:** Leave **pending** until you implement the optional feature flag/company setting.
