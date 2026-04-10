\# CephasOps Service Installer (SI) Mobile App Strategy



This document defines how the \*\*SI Mobile App\*\* (`/frontend-si`) should be designed and implemented.



The goal is to give installers a \*\*fast, offline-first, low-friction\*\* experience that matches backend rules and dispatcher workflows.



---



\## 1. Tech Stack \& Core Principles



\*\*Tech Stack\*\*



\- Framework: React Native + TypeScript (or hybrid shell as per documentation)

\- Navigation: React Navigation (stack + tabs) or existing routing choice

\- Storage: Local encrypted storage (e.g. SQLite/AsyncStorage) for:

&nbsp; - Orders

&nbsp; - Dockets

&nbsp; - Sync queue

\- Network: Centralized API client using the SI endpoints in `/docs/04\_api/si/\*`



\*\*Core Principles\*\*



1\. \*\*Offline-first (ROADMAP)\*\* – App should eventually work with weak or no network. **Status: NOT YET IMPLEMENTED.** Currently requires active network connection. See SI_APP_OVERVIEW.md for current implementation state.

2\. \*\*Three-tap rule\*\* – Status updates should take at most three taps.

3\. \*\*Read-only for sensitive data\*\* – Installer only sees what they need to execute the job.

4\. \*\*Device-bound \& Company-bound\*\* – Every API call includes CompanyId + Installer + DeviceId.

5\. \*\*Strict workflow alignment\*\* – Status flow must match backend Orders/Workflow module exactly.



---



\## 2. Architecture \& Folders



Suggested structure:



```text

/frontend-si/src

&nbsp; api/           ← SI API clients

&nbsp; auth/          ← login, device binding, token handling

&nbsp; screens/       ← feature screens

&nbsp; components/    ← shared UI components

&nbsp; state/         ← global state (installer, orders, sync)

&nbsp; offline/       ← cache logic (orders, dockets)

&nbsp; sync/          ← sync queue, retry logic

&nbsp; notifications/ ← installer notifications UI

&nbsp; utils/

Guidelines



Keep screens “thin”; move most logic into hooks and state modules.



The sync engine must be centralized (not per-screen).



All IO (network + storage) should go through dedicated layer(s).



3\. Authentication \& Device Binding

Flow (must match backend rules):



Installer enters phone number / ID.



Installer receives or enters an activation code tied to a device.



Backend validates device + company + installer mapping.



App receives:



Access token (JWT)



Refresh token (if used)



Installer profile



App stores tokens in secure storage (not plain AsyncStorage).



Every API call must send:



CompanyId



InstallerId



DeviceId



Auth token



The SI app must not show non-SI admin areas (settings, finance, etc.).



4\. Core Flows

4.1 Home / Dashboard

Home screen MUST show:



List of Assigned Jobs (today + upcoming)



Basic KPI widget:



Jobs today / completed / pending



Pending sync indicator:



Number of local updates waiting to be synced



Notifications preview:



Recent unread Installer notifications



4.2 Orders / Jobs

Per /docs/03\_business and orders-related docs:



Show:



Job reference



Customer name



Site address



Appointment date/time



Type (FTTH/FTTO/Modification/Assurance, etc.)



Building instructions (gate, access, lift, etc.)



Actions:



Accept (if required)



Status transitions strictly following defined workflow



Navigate using preferred map app (deeplink)



Call customer (tap-to-call)



Status lifecycle must match backend (example):



Assigned → OnTheWay → MetCustomer → WorkInProgress → Completed → DocketSubmitted



All transitions must:



Be validated locally



Be queued if offline



Call the exact SI endpoints when online



4.3 Docket \& Evidence

For every completed job:



Capture:



Start/End times



GPS at site (if documented)



Required photos (per job type)



Customer signature (if required)



Installer remarks



Material usage + serials (if module enabled)



UI rules:



Photo capture should:



Show checklist (e.g. ONU, router, DB board, overall)



Compress images before upload



Docket submit:



Saves locally first



Pushes to server in background via sync engine



4.4 Reschedule \& Issues

If reschedule is allowed for SI:



Provide simple flow:



Select reason



Select new slot or mark as “customer to call back”



Attach photos/notes if required



Submit → becomes a reschedule request for backoffice



App should NOT:



Approve its own reschedules unless explicitly allowed in docs.



5\. Offline \& Sync Strategy

This is crucial for SI.



Local Cache



Cache:



Assigned orders



Recently completed orders (for a limited period)



Pending dockets



Pending status updates



Sync Queue



All writes (status updates, dockets, reschedule requests) go into a queue:



Marked with:



LocalId



Entity type (OrderStatusUpdate, Docket, etc.)



Payload



Retry count



Last attempted time



Sync worker:



Runs periodically (e.g. every 30s, as documented)



Retries failed entries with exponential backoff



Marks items as succeeded/failed based on server response



UX for Offline



Orders must still be visible.



Actions should:



Give immediate local confirmation (e.g. “Queued for sync”)



Show a small “offline/queued” badge



Sync status should be visible:



E.g. banner “You’re offline – changes will sync later.”



6\. Notifications (SI)

Per Notification rules \& SI API:



Installer receives:



New job assignment



Approved reschedule



Cancelled job



Urgent site/management notice



VIP job flag



UI:



Bell icon or dedicated tab



List of notifications with:



Type



Job reference (if linked)



Time



Tap opens job detail or message detail



All accessed through SI notification endpoints in /docs/04\_api/si/\*.



7\. Security \& Privacy

Tokens must be stored securely (e.g. SecureStore / Keychain).



Never store raw passwords.



GPS and photo data should be stored only as long as required.



Cache must be wiped on logout and device unbind.



App must respect company isolation – no cross-company data.



8\. Testing

Minimum:



Unit tests:



Sync queue logic



Order status state machine



Local cache logic



Integration tests:



Basic flows with mocked API:



Login



View assigned jobs



Change status



Submit docket



UI tests (optional but recommended):



Navigation between home → job → docket



9\. Working with Cursor (SI)

When using Cursor for SI work:



Make sure cursor/ONBOARDING\_SI.md is up to date.



Prompt Cursor specifically that you are working on frontend-si.



Always align code with:



API specs under /docs/04\_api/si/\*



Entities under /docs/05\_data\_model



Flows under /docs/07\_frontend/si\_app/\*



Business rules under /docs/03\_business/\*



For complex tasks, copy from:



FULL STACK DELTA PROMPT.md or a SI-specific delta prompt (if added).



No SI feature should be merged if:



It bypasses sync queue



It breaks documented status flows



It surfaces admin-only data

