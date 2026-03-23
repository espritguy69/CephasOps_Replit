# SI Mobile – Implementation Notes

Version: 1.0  
Status: Phase 1 foundation  

---

## 1. Scope (Phase 1)

- **Framework:** React Native with Expo (TypeScript).
- **Purpose:** Production-grade foundation for the Service Installer native app (iOS & Android), aligned with `frontend-si/ARCHITECTURE.md` and `frontend-si/SI_APP_DATA_MODEL_MAPPING.md`.

---

## 2. What Was Implemented

### 2.1 Project setup

- **Location:** `si-mobile/`
- **Config:** `app.config.js` with `extra.apiBaseUrl` (env: `EXPO_PUBLIC_API_BASE_URL`, default `http://localhost:5000/api`).
- **Dependencies:** Expo SDK 55, React Navigation (native-stack, bottom-tabs), expo-secure-store, expo-constants, @tanstack/react-query.

### 2.2 Navigation

- **Auth:** Single screen stack – Login (no OTP/password reset in this phase).
- **Main:** Stack with:
  - **MainTabs:** Home, Jobs, Scan, Earnings, Profile.
  - **JobDetail:** Push from Jobs tab (and from Current Job bar).
- **Current Job bar:** Shown above tab content when there is an active job (status not Assigned/Pending/Complete/Problem/Reschedule). Tapping opens Job Detail.

### 2.3 API layer

- **Client:** `src/api/client.ts` – base URL from config, Bearer token from secure store via `setAuthTokenGetter`, response unwrap for `{ success, data }`.
- **Endpoints used:**  
  - Auth: `POST /auth/login`, `GET /auth/me`.  
  - Orders: `GET /orders`, `GET /orders/:id`.  
  - Workflow: `POST /workflow/execute`, `GET /workflow/allowed-transitions`.  
  - Earnings: `GET /payroll/earnings`.  
  - Service installers: `GET /service-installers` (to resolve current SI after login).

### 2.4 Auth

- **Storage:** expo-secure-store (`authToken`, `refreshToken`).
- **Flow:** Login (Email/Password, PascalCase for backend) → store tokens → fetch `/auth/me` and service installers → set user + serviceInstaller; `siId` derived for API usage.
- **Token getter:** Registered with API client on startup so all requests send Bearer when authenticated.

### 2.5 Theme

- **File:** `src/theme/colors.ts` – primary, background, card, status colors (Assigned, On the Way, Met Customer, Complete, Problem).
- **Usage:** Status badges and buttons use these; no global theme provider beyond direct imports.

### 2.6 Screens implemented

| Screen        | Purpose |
|---------------|--------|
| Login         | Email/password; calls existing backend login. |
| Home          | Greeting, today’s job count, completed/remaining/problems, active job card, today earnings. |
| Jobs          | Tabs: Today / Upcoming / History; list of assigned orders; tap → Job Detail. |
| Job Detail    | Customer, address, appointment, type, partner; “Next action” from allowed transitions; execute transition (Assigned → On the Way → Met Customer → Start Work → Complete, or Problem). |
| Earnings      | Tabs: Today / Week / Month; summary + list from payroll/earnings. |
| Profile       | Name, email, phone, partner/team; Logout. |
| Scan          | **Placeholder** – copy for future material/serial scanning. |

### 2.7 Workflow actions

- **Source of truth:** Backend `allowed-transitions` for each order.
- **UI:** Job Detail shows buttons for each allowed transition; on press, `POST /workflow/execute` with `entityType: "Order"`, `entityId`, `targetStatus`. No client-side status enum; labels mapped in `StatusBadge` (e.g. OnTheWay → “On the Way”, OrderCompleted → “Complete”).

### 2.8 Placeholders (clean architecture for later)

- **Scan tab:** `ScanScreen.tsx` – placeholder text; intended for material/device scan (Install, Remove, Return, Faulty, Lookup).
- **Not yet implemented but documented:** Materials & device scan flow, Assurance replacement, Extra work, Proof capture, Completion review, Reschedule. See `GAPS_AND_REFERENCE.md` and `PlaceholderScreen.tsx` for a generic placeholder component.

---

## 3. Alignment with docs

- **frontend-si/ARCHITECTURE.md:** 5 tabs (Home, Jobs, Scan, Earnings, Profile), Current Job bar, screen list and job flow concepts respected.
- **frontend-si/SI_APP_DATA_MODEL_MAPPING.md:** Status codes and UI labels, screen–domain mapping, API capabilities and validations referenced; no backend changes.
- **Backend:** No changes to business logic; reuse of existing auth, orders, workflow, payroll, service-installers APIs.

---

## 4. How to run

- **Web (laptop browser):** `npm run web` – uses `http://localhost:5000/api` by default. No extra config if the API runs on the same machine.
- **iOS or Android device / simulator:**  
  On device, `localhost` is the device itself, not your laptop. Set your **laptop’s LAN IP** so the app can reach the API:
  - **Windows:** `ipconfig` → use the IPv4 address (e.g. `192.168.1.100`).
  - **Mac/Linux:** `ifconfig` or `ip addr` → use the LAN address.
  - Then: `set EXPO_PUBLIC_API_BASE_URL=http://YOUR_IP:5000/api` (Windows) or `export EXPO_PUBLIC_API_BASE_URL=http://YOUR_IP:5000/api` (Mac/Linux), and restart Expo (`npm start`).
- **Commands:**  
  - `npm start` – Expo dev server (then choose web / iOS / Android).  
  - `npm run web` – open in browser.  
  - `npm run android` / `npm run ios` – run on device/emulator.
- **Backend:** Ensure the API is running and CORS allows the Expo dev origin (same as frontend-si).

---

## 5. Gaps and next steps

- See **GAPS_AND_REFERENCE.md** for:
  - API contract summary and workflow status alignment.
  - Gaps between frontend-si and si-mobile (material scanning, assurance, extra work, proof, completion review, reschedule).
- **Suggested order for next phase:** Material scanning + serial validation → Proof capture + completion checklist → Assurance replacement → Extra work → Reschedule.

---

## 6. File layout (summary)

```
si-mobile/
├── App.tsx                    # QueryClient + AuthProvider + RootNavigator
├── app.config.js              # Expo config + apiBaseUrl
├── index.ts                   # registerRootComponent(App)
├── GAPS_AND_REFERENCE.md      # API ref + frontend-si vs si-mobile gaps
├── IMPLEMENTATION_NOTES.md    # This file
├── src/
│   ├── api/                   # client, auth, workflow, orders, earnings, serviceInstallers
│   ├── components/            # ui (Button, Card, StatusBadge), CurrentJobBar
│   ├── contexts/              # AuthContext
│   ├── navigation/            # RootNavigator, AuthStack, MainStack, MainTabs, types
│   ├── screens/               # Login, Home, JobsList, JobDetail, Earnings, Profile, Scan, Placeholder
│   ├── theme/                 # colors
│   └── types/                 # api (User, Order, WorkflowTransition, etc.)
```
