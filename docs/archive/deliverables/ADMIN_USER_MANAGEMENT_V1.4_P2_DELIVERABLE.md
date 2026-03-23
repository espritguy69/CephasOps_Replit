# CephasOps v1.4 Phase 2 – Suspicious Activity Detection (Deliverable)

**Date:** 2026-03-08  
**Status:** Complete

---

## 1. Audit findings

- **Auth audit data:** AuditLog entries with `EntityType = "Auth"` provide: **Action** (event type), **UserId**, **Timestamp**, **IpAddress**, **MetadataJson** (UserAgent and optional context). `GetSecurityActivityAsync` returns these with **UserEmail** resolved via join. No schema changes were required.
- **Support for detection:** Event type, user, timestamp, and IP are sufficient for the three rules (excessive failures, reset abuse, multiple IP login). A new method `GetAuthEventsForDetectionAsync` was added to fetch Auth events in a time window (no paging, configurable max count) for analysis.

---

## 2. Detection rules

| Rule | Trigger | Threshold (config in code) |
|------|--------|----------------------------|
| **Excessive login failures** | Same user, `LoginFailed` events in a sliding window | >10 events in 5 minutes |
| **Password reset abuse** | Same user, `PasswordResetRequested` in a sliding window | >3 events in 15 minutes |
| **Multiple IP login** | Same user, `LoginSuccess` from different IPs in a sliding window | ≥3 distinct IPs in 10 minutes |

- **Config:** `SecurityDetectionRules` (Application/Auth/SecurityDetectionRules.cs) and `SecurityAlertTypes` (Application/Auth/SecurityAlertTypes.cs). Configurable in code initially as requested.

---

## 3. Service design

- **SecurityAnomalyDetectionService** (Application/Auth/Services): Depends on `IAuditLogService`. No new DB table; alerts are computed on demand.
- **Flow:** `DetectAsync(dateFrom, dateTo, userId?, alertType?)` → calls `GetAuthEventsForDetectionAsync` → runs three rule methods over the event list → returns `List<SecurityAlertDto>` (DetectedAtUtc, UserId, UserEmail, AlertType, Description, IpSummary, EventCount, WindowMinutes).
- **Sliding window:** For each user and event type, events are ordered by time; for each event we consider a window ending at that event and count (or count distinct IPs). If the count exceeds the threshold, one alert per user per rule is emitted (using the worst window for that user).
- **Optional background job:** Not implemented; detection runs when the Security Activity page or user detail is loaded (or when the alerts API is called). Can be added later (e.g. SecurityMonitoringBackgroundService every 1–5 min) if needed.

---

## 4. Files changed

| Area | File | Change |
|------|------|--------|
| Application | `Audit/Services/IAuditLogService.cs` | Added `GetAuthEventsForDetectionAsync` |
| Application | `Audit/Services/AuditLogService.cs` | Implemented `GetAuthEventsForDetectionAsync` (Auth events in range, join User for email) |
| Application | `Audit/DTOs/AuditLogDto.cs` | Added `SecurityAlertDto` |
| Application | `Auth/SecurityAlertTypes.cs` | New: alert type constants |
| Application | `Auth/SecurityDetectionRules.cs` | New: threshold and window constants |
| Application | `Auth/Services/ISecurityAnomalyDetectionService.cs` | New interface |
| Application | `Auth/Services/SecurityAnomalyDetectionService.cs` | New: detection logic for three rules |
| Api | `Program.cs` | Registered `ISecurityAnomalyDetectionService` |
| Api | `Controllers/LogsController.cs` | Injected detection service; added `GET /api/logs/security-alerts` |
| Frontend | `api/logs.ts` | Added `SecurityAlert`, `getSecurityAlerts` |
| Frontend | `pages/admin/SecurityActivityPage.tsx` | Security Alerts panel (table, alert type filter, refresh) |
| Frontend | `pages/admin/UserManagementPage.tsx` | Security Alerts section in user view modal (last 7 days) |
| Tests | `Auth/SecurityAnomalyDetectionServiceTests.cs` | New: 5 tests (excessive failures trigger, under threshold no alert, reset abuse trigger, multiple IP trigger, normal activity no alerts) |
| Docs | `ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md` | v1.4 Phase 2 section + “Suspicious Activity Detection” |
| Docs | `dev/onboarding.md` | v1.4 Phase 2 in admin user management bullet |

---

## 5. UI enhancements

- **Security Activity page** (`/admin/security/activity`): New **Security Alerts** panel (above the activity table). Table columns: Timestamp, User, Alert type, Description, IP summary. Filter by alert type (dropdown); “Refresh alerts” button. Alerts use the same user/date range as the activity filters when “Apply” is clicked.
- **User detail (Admin User Management):** New **Security Alerts** section. When viewing a user, shows alerts for that user in the last 7 days (e.g. “⚠ Excessive login failures detected for user … (date)”). Loading and empty states included.

---

## 6. Tests summary

- **SecurityAnomalyDetectionServiceTests:**  
  - `DetectAsync_WhenExcessiveLoginFailures_ReturnsAlert` – 11 LoginFailed in short window → one ExcessiveLoginFailures alert.  
  - `DetectAsync_WhenLoginFailuresUnderThreshold_ReturnsNoExcessiveAlert` – 5 failures → no alert.  
  - `DetectAsync_WhenPasswordResetAbuse_ReturnsAlert` – 4 PasswordResetRequested in 15 min window → one PasswordResetAbuse alert.  
  - `DetectAsync_WhenMultipleIpLogin_ReturnsAlert` – 3 LoginSuccess from 3 IPs in 10 min → one MultipleIpLogin alert.  
  - `DetectAsync_WhenNormalActivity_ReturnsNoAlerts` – 2 benign events → no alerts.  
- All use mocked `IAuditLogService.GetAuthEventsForDetectionAsync`.  
- **Regression:** Full application test run: 526 passed, 5 skipped (pre-existing). No auth flow changes.

---

## 7. Docs updated

- **ADMIN_USER_MANAGEMENT_AUDIT_AND_PLAN.md:** New section “v1.4 Phase 2 – Suspicious Activity Detection” (goal, approach, rules, API, UI). New subsection “Suspicious Activity Detection” with alert types table, where to see alerts, and how to investigate (excessive failures, reset abuse, multiple IP).
- **dev/onboarding.md:** Admin user management bullet updated with v1.4 Phase 2 (anomaly detection, rules, Security Alerts on page and user detail).

---

## Regression (Phase 9)

- **Login, password reset, token refresh, lockout, admin reset, audit logging:** Unchanged. Detection is read-only: it only reads Auth audit events and returns computed alerts. No code paths in AuthService, AdminUserService, or audit logging were modified.
- **Snapshot scheduler jobs, dashboard:** Not touched; no regressions expected.

---

## Success criteria (met)

- Suspicious patterns are detected automatically (excessive failures, reset abuse, multiple IP login).
- Alerts are visible in the admin UI (Security Activity page and user detail).
- Authentication behaviour is unchanged; no new tables; impact is minimal (on-demand query when alerts are requested).
