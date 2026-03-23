# Payout Anomaly Governance — Deliverable

## Executive summary

**Lightweight governance** for payout anomalies is in place so operations and finance can acknowledge, assign, resolve, and comment on anomalies without changing payout or snapshot logic. All governance is **operational metadata only**; no automatic payroll or calculation changes.

- **Entity:** `PayoutAnomalyReview` stores status, assignee, and comment thread per anomaly (keyed by fingerprint id).
- **API:** POST actions (acknowledge, assign, resolve, false-positive, comment) and GET anomaly-reviews + summary.
- **UI:** Payout Anomalies page has governance summary cards (Open, Investigating, Resolved today), Status and Assigned To columns, row actions (Acknowledge, Assign, Resolve, Mark false positive, Comment), and a comment drawer for investigation notes.

---

## Entity design: PayoutAnomalyReview

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | PK |
| AnomalyFingerprintId | string(64) | Stable fingerprint for the anomaly (from detection); unique index |
| AnomalyType | string(64) | Copied from anomaly when review is created |
| OrderId | Guid? | From anomaly |
| InstallerId | Guid? | From anomaly |
| PayoutSnapshotId | Guid? | From anomaly |
| Severity | string(32) | From anomaly |
| DetectedAt | DateTime | From anomaly |
| Status | string(32) | Open \| Acknowledged \| Investigating \| Resolved \| FalsePositive |
| AssignedToUserId | Guid? | User assigned to investigate |
| NotesJson | text | JSON array of { at, userId, userName, text } for comment thread |
| CreatedAt | DateTime | When review was first created |
| UpdatedAt | DateTime? | Last status/assign/comment change |

Reviews are created on first action (e.g. acknowledge); optional anomaly snapshot in the request body populates AnomalyType, OrderId, etc.

---

## Migration

- **File:** `20260311120000_AddPayoutAnomalyReview.cs`
- **Table:** `PayoutAnomalyReviews` with unique index on `AnomalyFingerprintId`, indexes on `Status` and `DetectedAt`.
- **Apply:** Run EF migrations or apply the migration SQL against the target database.

---

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/payout-health/anomalies/{id}/acknowledge` | Set review status to Acknowledged (body: optional anomaly snapshot) |
| POST | `/api/payout-health/anomalies/{id}/assign` | Set AssignedToUserId (body: `{ assignedToUserId?: Guid }`) |
| POST | `/api/payout-health/anomalies/{id}/resolve` | Set status to Resolved |
| POST | `/api/payout-health/anomalies/{id}/false-positive` | Set status to FalsePositive |
| POST | `/api/payout-health/anomalies/{id}/comment` | Append note to NotesJson (body: `{ text }`); current user recorded |
| GET | `/api/payout-health/anomaly-reviews` | List reviews (query: from, to, status, page, pageSize) |
| GET | `/api/payout-health/anomaly-reviews/summary` | Summary: openCount, investigatingCount, resolvedTodayCount |
| GET | `/api/payout-health/anomalies/{id}/review` | Get single review by anomaly fingerprint id |

The `{id}` in anomaly routes is the **anomaly fingerprint id** returned in each anomaly’s `id` field from the anomalies list.

---

## Anomaly list enrichment

- The anomalies list API now returns **review metadata** for each row when a review exists: `reviewStatus`, `assignedToUserId`, `assignedToUserName`.
- Each anomaly has a stable `id` (fingerprint) used for all governance calls.

---

## UI changes

- **Governance summary cards:** Open anomalies, Investigating, Resolved today (from GET anomaly-reviews/summary).
- **Table columns:** Status, Assigned To, plus existing columns; new **Actions** column.
- **Row actions:** Acknowledge (only when status is Open), Assign, Resolve, Mark false positive, Comment (opens drawer).
- **Assign modal:** Dropdown of users (from admin users list); submit calls assign API.
- **Comment drawer:** Slide-out with existing notes (from NotesJson) and “Add note” textarea + button; new notes call comment API and refresh review.

---

## Example workflow

1. **Operations** opens Payout Anomalies, sees “Open anomalies” count and the recent anomalies table.
2. For a given row, they click **Acknowledge** → review is created/updated with status Acknowledged; row shows Status “Acknowledged”.
3. They click **Assign** → modal opens, they pick a user → review is updated; “Assigned To” column shows that user.
4. They click the **comment** icon → drawer opens with notes; they add “Checking with finance on rate card” and submit → note is appended and drawer refreshes.
5. After investigation they click **Resolve** or **Mark false positive** → status updates; “Resolved today” summary increments if Resolved.

---

## Confirmation: payout logic unchanged

- **Payout calculation (RateEngineService):** Not modified.
- **Snapshot creation / snapshot math:** Not modified.
- **Anomaly detection rules (PayoutAnomalyService):** Not modified.
- **Payroll:** No automatic block or hold; governance is metadata only.

---

## Files added/updated

### Backend

- **Domain:** `Rates/Entities/PayoutAnomalyReview.cs`
- **Constants:** `PayoutAnomalyReviewStatus` in `PayoutAnomalyConstants.cs`
- **Infrastructure:** `PayoutAnomalyReviewConfiguration.cs`, `ApplicationDbContext` (DbSet), `20260311120000_AddPayoutAnomalyReview.cs`
- **Application:** DTOs in `PayoutAnomalyDto.cs` (review DTOs, Assign/Comment requests; `PayoutAnomalyDto` + Id, ReviewStatus, AssignedTo*), `IPayoutAnomalyReviewService`, `PayoutAnomalyReviewService`, `PayoutAnomalyService` (fingerprint id + merge review into list)
- **Api:** `Program.cs` (register review service), `PayoutHealthController.cs` (governance endpoints)

### Frontend

- **Types:** `payoutHealth.ts` (review DTOs, summary, request types; anomaly `id`, `reviewStatus`, `assignedTo*`)
- **API:** `payoutHealth.ts` (getReviewSummary, getAnomalyReviews, getAnomalyReview, postAcknowledge, postAssign, postResolve, postFalsePositive, postComment)
- **Page:** `PayoutAnomaliesPage.tsx` (governance cards, Status/Assigned To/Actions columns, comment drawer, assign modal, mutations)
