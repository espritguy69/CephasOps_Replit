# SI Mobile – Web audit checklist (evaluate in browser before Expo)

Use this in **browser mode** (`npm run web`) to evaluate the app before testing on device with Expo Go.

---

## 1. Start web mode

```bash
cd si-mobile
npm run web
```

Use the browser tab that opens (e.g. `http://localhost:8081`).  
Optional: start the backend so login and data work:  
`cd backend/src/CephasOps.Api && dotnet run`

---

## 2. Authentication

| # | Check | Pass? |
|---|--------|-------|
| 2.1 | Login screen loads (email, password, Login button) | ☐ |
| 2.2 | Invalid credentials show an error message | ☐ |
| 2.3 | Valid login (with API running) succeeds and navigates to Home | ☐ |
| 2.4 | After login, refresh the page – session persists (no redirect to Login) | ☐ |
| 2.5 | Profile → Logout returns to Login; after refresh, still at Login | ☐ |

---

## 3. Navigation & layout

| # | Check | Pass? |
|---|--------|-------|
| 3.1 | Bottom tabs visible: Home, Jobs, Scan, Earnings, Profile | ☐ |
| 3.2 | Each tab opens the correct screen (no blank or wrong content) | ☐ |
| 3.3 | Headers show correct titles (Home, Jobs, etc.) | ☐ |
| 3.4 | No layout overflow / horizontal scroll on desktop width | ☐ |

---

## 4. Home

| # | Check | Pass? |
|---|--------|-------|
| 4.1 | Greeting and name (or “there”) display | ☐ |
| 4.2 | Today’s Jobs, Completed, Remaining, Problems counts show (or 0) | ☐ |
| 4.3 | If there is an active job, “Current Job” card appears with customer/address | ☐ |
| 4.4 | Today’s Earnings shows (RM 0.00 if none) | ☐ |

---

## 5. Jobs

| # | Check | Pass? |
|---|--------|-------|
| 5.1 | Tabs: Today, Upcoming, History | ☐ |
| 5.2 | Job list loads (or “No jobs” when empty) | ☐ |
| 5.3 | Each job shows customer name, address, status badge | ☐ |
| 5.4 | Tapping a job opens Job Detail | ☐ |

---

## 6. Job Detail & workflow

| # | Check | Pass? |
|---|--------|-------|
| 6.1 | Customer name, address, phone (if any), appointment, job type, partner | ☐ |
| 6.2 | Status badge matches current status | ☐ |
| 6.3 | “Next action” section shows allowed transition buttons | ☐ |
| 6.4 | Tapping a transition (e.g. On the Way) calls API and updates (or shows error) | ☐ |
| 6.5 | “Back to Jobs” returns to Jobs list | ☐ |

---

## 7. Current Job bar

| # | Check | Pass? |
|---|--------|-------|
| 7.1 | When there is an active job, bar appears above tab content | ☐ |
| 7.2 | Bar shows customer name and “OPEN” | ☐ |
| 7.3 | Tapping bar opens that job’s Job Detail | ☐ |

---

## 8. Earnings

| # | Check | Pass? |
|---|--------|-------|
| 8.1 | Tabs: Today, Week, Month | ☐ |
| 8.2 | Summary shows period earnings and job count | ☐ |
| 8.3 | Breakdown list shows (or “No earnings in this period”) | ☐ |

---

## 9. Profile

| # | Check | Pass? |
|---|--------|-------|
| 9.1 | Name, email, phone (if any), partner/team (if any) | ☐ |
| 9.2 | Logout button works and returns to Login | ☐ |

---

## 10. Scan (placeholder)

| # | Check | Pass? |
|---|--------|-------|
| 10.1 | Scan tab opens; placeholder text visible | ☐ |

---

## 11. API & errors

| # | Check | Pass? |
|---|--------|-------|
| 11.1 | With API stopped: login shows a clear error (no white screen / crash) | ☐ |
| 11.2 | With API running: no CORS errors in browser console (F12 → Console) | ☐ |
| 11.3 | Invalid workflow transition shows alert/error, app stays usable | ☐ |

---

## 12. Quick pass/fail

- **Pass:** All sections above behave as described; no blocking bugs in browser.
- **Fail:** Note the section and check number, fix in web mode, then re-run this audit before moving to Expo.

---

## After web audit

When the checklist passes in the browser:

1. Stop the web server (Ctrl+C).
2. Run `npm start` (no `--web`).
3. Scan the QR code with Expo Go to test on device.

Use the same checklist on device later if you want to compare web vs native behavior.
