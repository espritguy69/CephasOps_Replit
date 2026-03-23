# Frontend tenant-boundary audit (2026-03-13)

**Scope:** Full frontend tenant-boundary audit for the multi-tenant SaaS transition.  
**Objective:** Confirm that tenant users cannot see, request, cache, route to, export, download, or infer another tenant’s data through the frontend.  
**Approach:** Stage A = audit and evidence gathering; Stage B = minimal remediation only where clearly required.

---

## 1. Tenancy model (mapped)

| Area | Implementation | Verdict |
|------|----------------|--------|
| **API client** | Single fetch-based client in `frontend/src/api/client.ts`. Auth via `setAuthTokenGetter`; department via `setDepartmentGetter` → `X-Department-Id`; company via `setCompanyIdGetter` → `X-Company-Id`. All non-login requests use `buildHeaders()`. | **Safe** – tenant context sent from single source. |
| **Auth/session** | `AuthContext` sets token getter; token in localStorage. No tenant in token payload used for UI routing. | **Safe** |
| **Tenant/company selector** | Effective company = `activeDepartment?.companyId ?? departments[0]?.companyId` in `DepartmentContext`; `companyIdRef` registered with API client. Department chosen via `selectDepartment(id)`; persisted to `cephasops.activeDepartmentId`. | **Safe** |
| **X-Company-Id propagation** | `buildHeaders()` calls `getCompanyIdFn()` and adds `X-Company-Id` when non-null. Getter set in `DepartmentContext` from `companyIdRef.current`. | **Safe** |
| **Route guards** | `ProtectedRoute` (auth + optional permission); `SettingsProtectedRoute` (settings.view or legacy roles). Admin routes under `/admin/*` use `SettingsProtectedRoute`. | **Safe** – no platform-only route guard; backend enforces. |
| **Query client** | TanStack Query; default stale/gc. Many hooks include `companyId` or `departmentId` in `queryKey` (e.g. `useOrders`, `usePaymentTerms`). Some hooks did **not** scope keys by tenant (see Findings). | **Risks found** – see cache invalidation fix. |
| **File upload/download** | `frontend/src/api/files.ts`: upload and download use raw `fetch` with `Authorization` only (no X-Company-Id). Backend resolves tenant from JWT. | **Safe** – backend scopes; optional improvement to send X-Company-Id for SuperAdmin consistency. |
| **Parser upload/export** | `parser.ts` used `localStorage.getItem('activeDepartmentId')` instead of `cephasops.activeDepartmentId`. | **Fixed** – key aligned with DepartmentContext. |

---

## 2. High-risk pattern review

- **X-Company-Id / companyId / departmentId:** Used in client and DepartmentContext; URL-driven `companyId` only in SiInsightsPage for SuperAdmin; backend rejects non–SuperAdmin use. **Safe.**
- **localStorage:** `cephasops.activeDepartmentId`, `cephasops.landingPageRoute`, `authToken`, `refreshToken`, theme, view-mode prefs. Only active department is tenant-sensitive; parser was using wrong key. **Fixed.**
- **queryKey:** Many list/detail hooks include `companyId` or `departmentId`; buildings, assets, warehouses, and several others did not include tenant scope in key. Invalidation on department switch added to avoid serving stale data. **Fixed.**
- **SiInsightsPage:** Reads `companyId` from URL; passes to API only when `isSuperAdmin && companyId`. Backend returns 400 if non–SuperAdmin requests another company. **Safe.**

---

## 3. API client and tenant header propagation

- All non-login requests use `buildHeaders()` (auth + X-Department-Id + X-Company-Id when getters set).
- Parser upload and parser logs export used raw fetch and wrong localStorage key for department; corrected to `cephasops.activeDepartmentId`.
- No direct fetch/axios bypass of centralized client for tenant-scoped data except files (auth-only; backend scopes by JWT).

---

## 4. Routing and page-level boundaries

- Platform-style routes under `/admin/*` (si-insights, background-jobs, etc.) protected by `SettingsProtectedRoute`; backend enforces roles/permissions.
- Tenant routes rely on department/company from context; no route param trusted as tenant override without backend check.
- SiInsightsPage companyId from URL is enforced by backend for SuperAdmin-only.

---

## 5. Caching and state isolation

- **Issue:** When user switched department (and thus company), React Query cache was not invalidated. Hooks that do not include department/company in `queryKey` (e.g. buildings, assets, warehouses, several settings lists) could show previous tenant’s data until refetch.
- **Fix:** In `DepartmentContext`, when `activeDepartmentId` changes (and we have a previous value), call `queryClient.invalidateQueries()` so all cached data is invalidated and refetched under the new context.
- **localStorage:** Only `cephasops.activeDepartmentId` is tenant-sensitive; parser now reads the same key.

---

## 6. List, detail, search, dashboard

- Orders: `useOrders` / `useOrder` use `departmentId` in queryKey; safe.
- Buildings, assets, and other unscoped keys are now safe due to global invalidation on department switch; loading/empty states show until refetch.
- Dashboards and summaries that use unscoped keys benefit from the same invalidation.

---

## 7. Forms, mutations, create/edit/delete

- Mutations use API client (tenant headers applied). Invalidation on department switch prevents optimistic or post-mutation cache from wrong tenant being shown.

---

## 8. Files, downloads, exports

- File upload/download: auth only; backend scopes by JWT. No change made.
- Parser upload and parser logs export: fixed to use `cephasops.activeDepartmentId` so correct department (and thus tenant) is sent.

---

## 9. Platform admin vs tenant admin

- Admin pages under `/admin/*` use `SettingsProtectedRoute`. SiInsightsPage company selector and URL companyId are SuperAdmin-only; backend enforces.

---

## 10. Tenant switching UX

- **Fix:** Cache invalidation on department switch ensures no stale tenant data is shown; components refetch with new context. No speculative UI redesign.

---

## 11. Tests

- No existing frontend tests were found that explicitly assert tenant header propagation, cache invalidation on switch, or parser storage key. Focused tests were not added in this pass; recommend adding in a follow-up for: (1) department switch invalidates queries, (2) parser uses `cephasops.activeDepartmentId`.

---

## Confirmed findings and remediations

| # | Finding | Severity | Remediation |
|---|---------|----------|-------------|
| 1 | Query cache not invalidated on department/company switch; unscoped keys (buildings, assets, warehouses, etc.) could show previous tenant data. | **High** | In `DepartmentContext`, on `activeDepartmentId` change (with a previous value set), call `queryClient.invalidateQueries()`. |
| 2 | Parser upload and parser logs export used `localStorage.getItem('activeDepartmentId')` instead of `cephasops.activeDepartmentId`, so department/tenant context could be wrong or stale. | **Medium** | Use `cephasops.activeDepartmentId` in both places in `frontend/src/api/parser.ts`. |

---

## Files changed

1. **frontend/src/contexts/DepartmentContext.tsx**
   - Import `useQueryClient` from `@tanstack/react-query`.
   - Added ref `prevActiveDepartmentIdRef` to track previous `activeDepartmentId`.
   - Added `useEffect` that, when authenticated and `activeDepartmentId` changes from a prior value, calls `queryClient.invalidateQueries()`.

2. **frontend/src/api/parser.ts**
   - Replaced `localStorage.getItem('activeDepartmentId')` with `localStorage.getItem('cephasops.activeDepartmentId')` for upload and for logs export.

---

## Tests added

- None in this pass. Recommended: (1) test that switching department invalidates queries, (2) test that parser uses `cephasops.activeDepartmentId` (or that header matches selected department).

---

## Remaining risks / follow-up

- **Low:** File upload/download do not send X-Company-Id; backend uses JWT. Optional: send X-Company-Id for SuperAdmin consistency.
- **Low:** Some hooks still have query keys without explicit company/department (e.g. `['buildings', filters]`). Invalidation on switch mitigates; adding scope to keys would be defense-in-depth.
- **Docs:** SaaS remediation changelog updated with frontend audit reference.

---

## Verdict

**Frontend tenant boundaries are verified across the reviewed surfaces.** Two confirmed issues were fixed: (1) cache invalidation on department switch to prevent stale cross-tenant data, and (2) parser use of the correct localStorage key for department so upload and export use the active tenant context. No critical or high tenant-boundary leak remains unfixed. Optional follow-ups (tests, X-Company-Id on file requests, more granular query-key scoping) are documented above.
