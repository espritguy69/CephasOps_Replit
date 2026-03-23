# Parser List Not Showing Updates – Evidence & Fix Plan

**Goal:** Prove whether the issue is (A) No new drafts in DB, (B) API returns drafts but UI does not render, (C) API query wrong (filters/department/company/paging), or (D) Caching/stale query / polling not happening.

**Rules:** Evidence-first. Do not change code unless evidence proves it.

---

## 1) Evidence: DB

**Query:** Use script `scripts/test/parser_list_evidence_db.sql` against database `cephasops`.

```bash
# From project root, with connection string set (e.g. PGPASSWORD=...)
psql -h localhost -p 5432 -U postgres -d cephasops -f scripts/test/parser_list_evidence_db.sql
```

**What to record:**
- Latest 20 drafts: `CreatedAt`, `ConfidenceScore`, `ValidationStatus`, `ParseSessionId`.
- Count of drafts in last 24h.

**Conclusion:** Drafts exist recently? **YES** / **NO**.

---

## 2) Evidence: API

**Endpoint the UI calls:** `GET /api/parser/drafts` (see `frontend/src/api/parser.ts`: `getParsedOrderDraftsWithFilters` → `apiClient.get('/parser/drafts', { params })`).

**Typical request:**
- **URL:** `{API_BASE_URL}/parser/drafts?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc`
- **Query params:** `page`, `pageSize`, `sortBy`, `sortOrder`; optional: `validationStatus`, `sourceType`, `status`, `serviceId`, `customerName`, `partnerId`, `buildingStatus`, `confidenceMin`, `buildingMatched`, `fromDate`, `toDate`
- **Headers:** `Authorization: Bearer <token>`; `X-Department-Id: <id>` if active department is set (apiClient sends it; backend parser endpoint does **not** use it for company – company comes from JWT `companyId` / `company_id` claim, or `Guid.Empty` in single-company mode)

**Capture:**
1. Exact request URL and query string from browser DevTools → Network.
2. Response: `totalCount` and first 5 items’ `createdAt` (or `CreatedAt` if API uses PascalCase).

**Example (curl, replace BASE_URL and TOKEN):**
```bash
curl -s -X GET "%BASE_URL%/parser/drafts?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc" \
  -H "Authorization: Bearer %TOKEN%" \
  -H "Content-Type: application/json"
```

**Backend behaviour (for reference):**
- `ParserController.GetParsedOrderDraftsWithFilters` uses `_currentUserService.CompanyId` (from JWT; if missing, `Guid.Empty`). If `CompanyId == null` → 401.
- `ParserService.GetParsedOrderDraftsWithFiltersAsync` does **not** filter by company (comment: “Single-company mode: Don't filter by companyId”). So all drafts are returned regardless of company.

**Conclusion:** API returns new drafts? **YES** / **NO**.

**Applied fix (proceed):** Parser List now uses `refetchInterval: 60_000` and `refetchIntervalInBackground: false` so the list refreshes every 60s while the tab is visible, and `refetchOnWindowFocus: true` so returning to the tab triggers a refetch. If the issue persists, continue with evidence steps above to confirm DB/API/frontend.

---

## 3) Evidence: Frontend

**Page:** Parser List = `ParserListingPage` at route `/orders/parser` (or `/orders/parser/list`).

**In browser DevTools on Parser List page:**

1. **Network**
   - Filter by “drafts” or “parser”.
   - On page load: does a request to `.../parser/drafts?...` fire? Note time.
   - After leaving tab and returning (or switching window and back): does the same request fire again?
   - Response: copy `totalCount` and first 5 items’ `createdAt`. Does it match the API test above?

2. **TanStack Query**
   - Query key: `['parsedOrderDrafts', filters, refreshKey]` (see `ParserListingPage.tsx`).
   - Options: `staleTime: 0`, `gcTime: 0`, `refetchOnWindowFocus: false`, `refetchOnMount: true`. So: no interval polling; refetch only on mount, and (via useEffect) on `visibilitychange` and window `focus`.

3. **Client-side behaviour**
   - No extra client-side filter that would hide items after fetch (grid uses `drafts = pagedResult?.items || []`).
   - Check Console for errors; check if any error boundary catches and hides content.

4. **Response vs grid**
   - If the response has `totalCount > 0` and items with recent `createdAt`, does the grid show them?

**Conclusion:**
- Request fires on load? **YES** / **NO**
- Request refetches on tab/window focus? **YES** / **NO**
- Response payload matches API test? **YES** / **NO**
- Grid shows empty despite response having data? **YES** / **NO**

---

## 4) Root cause

*(Fill after evidence.)*

**One sentence:** _e.g. “No new drafts in DB in last 24h” / “API returns drafts but UI never refetches because user keeps tab focused” / “API returns 401 so frontend shows empty” / “Response uses PascalCase and frontend expects camelCase so items are undefined”._

---

## 5) Fix plan

**Order of interventions (ops/config first, minimal code only if required):**

1. **UI filter/paging/sort fix** – Only if evidence shows wrong params (e.g. default filter excluding new drafts, or wrong sort so new ones not on page 1).
2. **Query cache/refetch fix** – If evidence shows data is correct but UI does not update: e.g. enable `refetchOnWindowFocus: true`, or add a short `refetchInterval` for this list only, or refetch on visibility.
3. **Department header mismatch fix** – If backend ever starts filtering by department and frontend sends wrong or no `X-Department-Id`; currently parser/drafts does not use department for filtering.
4. **API endpoint mismatch fix** – If frontend calls wrong URL or wrong query shape.
5. **Minimal code change** – Only after the above are ruled out or addressed (e.g. add polling only if evidence shows “no refetch” is the cause).

---

## 6) Verify steps

After applying the fix:

1. **DB:** Insert or trigger a new draft (or wait for parser to create one). Confirm it appears in `ParsedOrderDrafts` with recent `CreatedAt`.
2. **API:** Call `GET /parser/drafts?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc` with valid auth. Confirm response includes the new draft (e.g. in first page, or note page where it appears).
3. **UI:** Open Parser List, then either:
   - Leave the tab open and rely on new refetch (e.g. interval or visibility/focus), or
   - Navigate away and back to the list.
   Confirm the new draft appears in the grid without a full page reload.

---

## Reference: Code locations

| Layer   | What | Where |
|--------|------|--------|
| DB     | Tables | `ParseSessions`, `ParsedOrderDrafts` (see `scripts/test/monitor_email_flow.sql`) |
| API    | Endpoint | `ParserController.GetParsedOrderDraftsWithFilters` → `[HttpGet("drafts")]` → `GetParsedOrderDraftsWithFiltersAsync` |
| API    | Company | `CurrentUserService.CompanyId` (JWT claims; no filter by company in service) |
| Frontend | Call | `getParsedOrderDraftsWithFilters(filters)` in `frontend/src/api/parser.ts` |
| Frontend | Page | `frontend/src/pages/parser/ParserListingPage.tsx` |
| Frontend | Query | `useQuery` key `['parsedOrderDrafts', filters, refreshKey]`, no refetch interval |
