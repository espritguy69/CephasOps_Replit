# Monitoring and Operational Alerts (Postgres-Only)

Monitoring uses **existing tables** only. No application telemetry code. Run the queries in `monitoring_queries.sql` on a schedule (e.g. cron or scheduled job) and evaluate the conditions below; send alerts to an ops channel or ticket system.

---

## Alert conditions

### 1. Spike in ParseError (ParseSession failures)

- **Condition:** The count of **ParseSessions** with **Status = 'Failed'** in the **last 24 hours** is **greater than 2×** the 7-day rolling average.
- **How to implement:** Run the “ParseError / ParseSession failures” query (or a variant grouped by day). Compute the 7-day rolling average of daily session failures; compare yesterday’s (or last 24h) count to that average. If count > 2 × average → alert.
- **Action:** Investigate ParseSession.ErrorMessage and EmailMessage.ParserError; follow NeedsReview SOP for ParseError (check file in storage, request resend if corrupt/password-protected).

### 2. Drop in average confidence

- **Condition:** The **daily average** of **ConfidenceScore** for the **last day** is **&lt; 0.7** **and** is **more than 10% below** the 7-day average.
- **How to implement:** Use the “Confidence trend (avg by day)” query. Compute 7-day average of daily avg(ConfidenceScore). If the most recent day’s avg is &lt; 0.7 and &lt; (7-day avg × 0.9) → alert.
- **Action:** Investigate new missing fields or sheet/layout changes; see Template Governance and NeedsReview SOP.

### 3. Surge in NeedsReview drafts

- **Condition:** The **daily count** of **ParsedOrderDrafts** with **ValidationStatus = 'NeedsReview'** in the **last 24 hours** is **greater than 2×** the 7-day rolling average.
- **How to implement:** Use the “Failure rate per day” query (or a variant that counts only NeedsReview). Compute 7-day rolling average of daily NeedsReview count. If last 24h count > 2 × average → alert.
- **Action:** Triage per NeedsReview SOP; check for vendor format changes and escalate if needed.

---

## Implementation note

Implement these alerts by running the SQL (or equivalent) in a **scheduled job**, computing the rolling averages and thresholds above, and posting to your ops channel or creating tickets. No change to application code is required.

---

## Related

- **Queries:** `docs/operations/monitoring_queries.sql`
- **NeedsReview SOP:** `docs/operations/NeedsReview_SOP.md`
- **Template Governance:** `docs/operations/Template_Governance.md`
