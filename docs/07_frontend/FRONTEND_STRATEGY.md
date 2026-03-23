\# CephasOps Admin Frontend Strategy



This document defines how the \*\*admin web app\*\* (`/frontend`) should be designed, implemented, and evolved.



The goal is to keep the frontend:

\- Consistent with backend + docs

\- Multi-company \& department aware

\- RBAC-driven

\- Easy to extend in modules (Tasks, Notifications, Email Settings, etc.)



---



\## 1. Tech Stack \& Core Principles



\*\*Tech Stack\*\*



\- Framework: React + TypeScript

\- Routing: React Router (or as documented in /docs/07\_frontend)

\- State: React Query / lightweight state management (e.g. context or Zustand) – follow existing pattern

\- UI: Shared component library (see `COMPONENT\_LIBRARY.md`)

\- Styling: Follow existing app conventions (CSS modules/Tailwind/MUI as already chosen)

\- Docs: Storybook for reusable components



\*\*Core Principles\*\*



1\. \*\*Documentation first\*\* – All behavior must follow `/docs` (especially `ARCHITECTURE\_BOOK.md`, `BUSINESS\_POLICIES.md`, `04\_api`, `05\_data\_model`, and `07\_frontend/ui`).

2\. \*\*Multi-company-aware\*\* – Every view that touches data must respect `companyId`.

3\. \*\*Department-aware\*\* – Where relevant (Orders, Tasks, Scheduler), UI must expose/use department context.

4\. \*\*RBAC-driven\*\* – Visibility and access must use permission keys (e.g. `settings.manage\_notifications`, `settings.manage\_email`, etc.).

5\. \*\*API-contract-driven\*\* – API payloads must match `/docs/04\_api/\*` exactly.

6\. \*\*Composable UI\*\* – Prefer small focused components + shared primitives.

7\. \*\*No hidden side effects\*\* – Data changes must be explicit and visible in the UI.



---



\## 2. Routing \& Layout



\*\*Structure (example):\*\*



\- `/login`

\- `/dashboard`

\- `/orders/\*`

\- `/parser/\*` (ParseSessions, Email views)

\- `/tasks/\*`

\- `/notifications`

\- `/settings/company/\*`

\- `/settings/departments/\*`

\- `/settings/email/\*` (mailboxes, rules, VIP)

\- `/settings/notifications`

\- `/reports/\*` (P\&L, KPIs – read-only if documented)



\*\*Guidelines\*\*



\- Use a primary layout with:

&nbsp; - Top navigation (company switcher, notifications bell, user menu)

&nbsp; - Left sidebar (module navigation, role-aware)

\- Use \*\*route guards\*\* that check:

&nbsp; - User authenticated

&nbsp; - Active company selected

&nbsp; - Permissions for each route



---



\## 3. State Management \& Data Fetching



\*\*Patterns\*\*



\- Use a central \*\*API client\*\* that:

&nbsp; - Attaches auth tokens

&nbsp; - Injects `companyId` into routes/headers as required

&nbsp; - Handles common error patterns (401, 403, 500) and maps to notifications/toasts

\- Use React Query (or the existing chosen tool) for:

&nbsp; - Data fetching \& caching

&nbsp; - Stale-while-revalidate patterns

\- Use context or lightweight state for:

&nbsp; - `CurrentUser`

&nbsp; - `CurrentCompany`

&nbsp; - `CurrentDepartment` (for dept-specific screens)

&nbsp; - `Notifications` summary (unread count, etc.)



\*\*Rules\*\*



\- Never call the backend directly with `fetch` scattered across components.

\- Always use typed hooks (e.g. `useOrders`, `useTasks`, `useNotifications`) layered on top of the API client.



---



\## 4. Key Domains \& UI Responsibilities



\### 4.1 Dashboard



\- Show role-appropriate widgets (based on RBAC):

&nbsp; - Company overview

&nbsp; - My Tasks summary

&nbsp; - Notifications highlights

&nbsp; - Orders in critical states (e.g. overdue, rescheduled)

&nbsp; - KPIs (if documented)

\- All widgets must be:

&nbsp; - Filtered by current `companyId`

&nbsp; - Respect department scope where applicable



\### 4.2 Orders / Parser Views



\- Screens for:

&nbsp; - Order list (filters by status, date, partner, building)

&nbsp; - Order details

&nbsp; - ParseSession list and detail (email snapshots, extracted fields, parser errors)

\- Support:

&nbsp; - “Parser Review Queue” as defined in email\_parser docs

&nbsp; - Links between email → ParseSession → Order



\### 4.3 Tasks \& Notifications



\- Tasks:

&nbsp; - “My Tasks” page

&nbsp; - Department Tasks page (Manager/HOD only)

&nbsp; - Dashboard widget (top 5 tasks)

\- Notifications:

&nbsp; - Top-bar bell with unread count

&nbsp; - Notifications list page with filters (VIP email, tasks, system)

&nbsp; - Integration with backend NotificationService API



\### 4.4 Settings Screens



\- \*\*Company Settings\*\*:

&nbsp; - Company profile, addresses, billing meta, etc.

\- \*\*Department Settings\*\*:

&nbsp; - Departments list \& configuration (name, type, cost centres where relevant)

\- \*\*Email Settings\*\*:

&nbsp; - Mailbox configs (POP3/IMAP, schedule)

&nbsp; - Email rules (matching \& VIP)

&nbsp; - VIP emails configuration

\- \*\*Notification Settings\*\*:

&nbsp; - Global/company/user preferences (if surfaced in UI)

&nbsp; - Channel selection (IN\_APP/EMAIL/BOTH) for key notification types (VIP email, tasks)



Each settings page must:



\- Call only documented endpoints in `/docs/04\_api`

\- Enforce `settings.\*` permission keys for visibility

\- Validate forms with clear error messages



---



\## 5. UX \& Component Standards



\- Follow `UX\_STANDARDS.md`:

&nbsp; - Consistent spacing, font sizes, colours

&nbsp; - Clear empty states, loading states, and error states

\- Use shared components from `COMPONENT\_LIBRARY.md`:

&nbsp; - Buttons, inputs, modals, tables, cards, status badges, etc.

\- Always show:

&nbsp; - Loading skeleton/spinner while data loads

&nbsp; - “No data found” when empty

&nbsp; - User-friendly error message on failures



Forms:



\- Client-side validation first

\- Server-side error messages surfaced clearly (e.g. under field or as form alert)



---



\## 6. Testing



Minimum expectations for new features:



\- Unit tests:

&nbsp; - For core hooks and pure logic functions

\- Component tests:

&nbsp; - For complex components (forms, tables with actions)

\- Storybook stories:

&nbsp; - Each reusable component must have stories for:

&nbsp;   - Default

&nbsp;   - Loading

&nbsp;   - Empty

&nbsp;   - Error (if visual)

\- Integration tests (optional but recommended):

&nbsp; - Critical flows such as login, order search, task creation



---



\## 7. Working with Cursor



When using Cursor for admin frontend work:



1\. Ensure `cursor/CURSOR\_ONBOARDING\_PROMPT.md` and docs are up-to-date.

2\. For focused tasks, use:

&nbsp;  - `BACKEND FEATURE DELTA PROMPT.md` (if API also changes)

&nbsp;  - `FRONTEND FEATURE DELTA PROMPT.md` (UI-only changes)

3\. Always align generated code with:

&nbsp;  - Data models in `/docs/05\_data\_model`

&nbsp;  - API specs in `/docs/04\_api`

&nbsp;  - UI flows in `/docs/07\_frontend/ui/\*`



Never merge Cursor-generated code without:

\- Reading it fully

\- Ensuring RBAC \& company logic are applied

\- Ensuring it uses existing components and patterns

---

## 8. Frontend Technology Migration Guide

**Updated from:** `frontend/FRONTEND_RULESET_MIGRATION_GUIDE.md`

This section documents the migration to the modern frontend technology stack: TailwindCSS, shadcn/ui, and Lucide icons.

### 8.1 New Technology Stack

1. **TailwindCSS** - Replace all custom CSS files with Tailwind utility classes
2. **shadcn/ui** - Use shadcn/ui components instead of custom components
3. **Lucide Icons** - Replace SVG icons with Lucide React icons
4. **Consistent Patterns** - Use LoadingSpinner, EmptyState, Toast, Breadcrumbs, Tabs on all pages

### 8.2 Migration Checklist Per Page

For **EVERY** page component, ensure:

- ✅ Uses TailwindCSS classes (remove custom CSS files)
- ✅ Uses LoadingSpinner for loading states
- ✅ Uses EmptyState for empty data
- ✅ Uses Toast for API mutations (success/error)
- ✅ Uses Breadcrumbs on detail pages
- ✅ Uses Tabs where multiple sections exist
- ✅ Uses Lucide icons (replacing SVG icons)
- ✅ Uses shadcn/ui components where applicable
- ✅ Follows desktop-first layout pattern
- ✅ Uses cards for grouping related data
- ✅ Uses tables for data-heavy views

### 8.3 Standard Page Templates

See the migration guide for complete List Page and Detail Page templates with code examples.

### 8.4 Migration Steps

1. Remove Custom CSS File
2. Import Required Components (LoadingSpinner, EmptyState, Toast, Breadcrumbs, shadcn/ui components)
3. Replace CSS Classes with TailwindCSS
4. Add Toast Notifications
5. Add Loading/Empty States
6. Add Breadcrumbs (Detail Pages)
7. Replace Icons with Lucide

### 8.5 Installation Commands

```bash
cd frontend

# Install TailwindCSS
npm install -D tailwindcss postcss autoprefixer

# Install required utilities
npm install lucide-react class-variance-authority clsx tailwind-merge

# Initialize shadcn/ui
npx shadcn-ui@latest init
```

### 8.6 Migration Status

All core pages have been migrated:
- ✅ DashboardPage, OrdersListPage, OrderDetailPage, CalendarPage, InvoicesListPage
- ✅ CompanyProfilePage, GlobalSettingsPage, MaterialTemplatesPage, DocumentTemplatesPage, KpiProfilesPage
- ✅ InventoryListPage, RMAListPage, ParseSessionReviewPage, SIAvailabilityPage
- ✅ MyTasksPage, DepartmentTasksPage, NotificationsPage, Email pages, WorkflowDefinitionsPage

**After Migration:**
- All pages use TailwindCSS (no custom CSS files)
- All pages use consistent UI patterns
- All API mutations show toast notifications
- All detail pages have breadcrumbs
- All loading/empty states are consistent
- All icons are from Lucide
- All components follow the ruleset patterns

---

