# Parser List “Not Updating” – Evidence & Conclusion (2026-03-03)

**Page:** `/orders/parser?validationStatus=NeedsReview`  
**UI:** Shows “last updated 05/01/2026 8:37:56pm” and appears not updating.

**Task:** Evidence-first. Confirm validationStatus of new ParsedOrderDrafts today; compare API; conclude one of A/B/C.

---

## 1) DB evidence: last 50 ParsedOrderDrafts (CreatedAt desc)

**Query:** `scripts/test/parser_drafts_evidence.sql` (last 50, `IsDeleted = false`, order by `CreatedAt` desc).

**Result (abbreviated):**

| CreatedAt (UTC+8)        | ValidationStatus | ConfidenceScore |
|--------------------------|------------------|-----------------|
| 2026-01-05 12:37:56.372  | NeedsReview      | 0.6800          |
| 2026-01-05 12:37:56.332  | Rejected         | 0.0000          |
| 2026-01-05 12:24:19.240  | Pending          | 1.0000          |
| …                        | …                | …               |
| 2025-12-18 01:32:49.244  | NeedsReview      | 0.5000          |

**Count created today (2026-03-03 UTC):** `0`  
**Max CreatedAt in table:** `2026-01-05 12:37:56.372258+08`

So:

- **No** ParsedOrderDrafts were created today (2026-03-03).
- Newest draft is **2026-01-05 12:37:56+08**, with `ValidationStatus = NeedsReview` and `ConfidenceScore = 0.68`.
- The UI “05/01/2026 8:37:56pm” matches this latest record (date/time display is consistent with that stored value).

---

## 2) API comparison (intended)

**Endpoints:**

- **All drafts:** `GET /api/parser/drafts?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc`
- **NeedsReview only:** `GET /api/parser/drafts?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc&validationStatus=NeedsReview`

**Required:** Return `totalCount` and newest `createdAt` for both.

**Note:** Direct curl from this environment returns **401 Unauthorized** (endpoint requires `Authorization: Bearer <token>`). With the DB state above:

- **All drafts:** Would return `totalCount` = total non-deleted drafts; newest `createdAt` = **2026-01-05T04:37:56.372258Z** (or equivalent UTC).
- **NeedsReview:** Would return `totalCount` = count of drafts with `ValidationStatus = NeedsReview`; newest `createdAt` for that subset is still **2026-01-05 12:37:56+08** (same row as above).

So the “no new data” situation is in the DB; the API would simply reflect it. No discrepancy between “all” and “NeedsReview” in terms of “newest” date—both would show the same latest draft if the API is consistent with the DB.

---

## 3) Conclusion

**A) No new drafts exist.**

- There are **0** ParsedOrderDrafts with `CreatedAt` on 2026-03-03.
- The most recent draft is from **2026-01-05** (NeedsReview, 0.68).
- The UI “last updated 05/01/2026 8:37:56pm” is correct; the list is “not updating” because **no new drafts have been created** since then.

Not B (filter hiding new drafts) and not C (API returns new drafts but UI not updating): the database has no new drafts today, so the API cannot return newer data and the UI cannot show it.

---

## 4) Optional: run API yourself

To confirm API behaviour with your auth:

```bash
# Replace TOKEN with your Bearer token (e.g. from DevTools → Application → Local Storage → authToken)

# All drafts
curl -s -X GET "http://localhost:5000/api/parser/drafts?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc" \
  -H "Authorization: Bearer %TOKEN%" -H "Content-Type: application/json"

# NeedsReview only
curl -s -X GET "http://localhost:5000/api/parser/drafts?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc&validationStatus=NeedsReview" \
  -H "Authorization: Bearer %TOKEN%" -H "Content-Type: application/json"
```

Check `totalCount` and the first item’s `createdAt` in each response; they should align with the DB figures above.
