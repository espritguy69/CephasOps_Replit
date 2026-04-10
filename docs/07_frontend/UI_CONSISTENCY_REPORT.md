# CephasOps UI Consistency Report (Audit)

**Scope:** Admin Portal (`/frontend`) + SI App (`/frontend-si`).  
**Step:** Audit only — no code changes.

---

## UI Stack Summary

### Admin portal (`frontend`)

| Area | Stack |
|------|--------|
| **Styling** | Tailwind CSS v4; `tailwind.config.js` + `index.css` with `@theme` and CSS variables (shadcn-style: `--background`, `--foreground`, `--primary`, `--border`, `--radius`, etc.); `design-tokens.ts` (spacing, typography, shadows); brand/layout tokens in config; Syncfusion Material CSS imports commented out in `index.css`. |
| **Components** | Custom shadcn-style `components/ui` (Button, Card, DataTable, StandardListTable, EmptyState, Modal, Select, Tabs, Breadcrumbs, StatusBadge, Badge, Input, Textarea, Label, Skeleton, etc.); Syncfusion (ScheduleComponent, GridComponent, TreeGridComponent, KanbanComponent, charts, PDF viewer, RichTextEditor, etc.); lucide-react; class-variance-authority; tailwind-merge; clsx. |
| **Layout** | `MainLayout` (TopNav + Sidebar); `PageShell` (title, breadcrumbs, actions) used on ~29 pages; `SectionCard`, `FormLayout`/`FormField`/`FormRow` exported but not used by any page. |
| **Tables** | Custom `DataTable` (sortable, pagination, empty/loading); custom `StandardListTable` (selection, actions, compact); Syncfusion `GridComponent` (BusinessHours, Integrations, SmsWhatsApp, GuardConditionDefinitions, SideEffectDefinitions, ApprovalWorkflows, AutomationRules, EscalationRules, SlaConfiguration); Syncfusion `TreeGridComponent` (BuildingsTreeGridPage); wrapper `SyncfusionGrid.tsx`. |
| **Forms** | Mixed: `react-hook-form` + `@hookform/resolvers` + `zod` only in CreateOrderPage and DocumentTemplateEditorPage (doc-templates); elsewhere raw form state. Input primitives: both `Input` (from `input.tsx`, forwardRef, size compact/default/large) and `TextInput` (label, error, value/onChange); Label, Textarea, Select, Switch, DatePicker, TimePicker. |

### SI app (`frontend-si`)

| Area | Stack |
|------|--------|
| **Styling** | Tailwind CSS v4; `index.css` only (no `tailwind.config.js`); same CSS variable names (shadcn-compat) but different primary/ring values (blue 221.2 vs admin purple 263); `@theme` block mapping vars to Tailwind colors. |
| **Components** | `components/ui`: Button, Card, TextInput, Textarea, EmptyState, LoadingSpinner, useToast, **Modal, DataTable, Tabs, TabPanel, Breadcrumbs, Skeleton, StatusBadge**; lucide-react. No Syncfusion. *Updated Feb 2026: SI primitives added; Job detail uses Tabs + Breadcrumbs.* |
| **Layout** | Single `MainLayout` (sticky top bar, nav links, no sidebar); no PageShell; pages use PageHeader + ad-hoc wrappers (`p-4 space-y-4`). Job detail: PageHeader + Breadcrumbs + Tabs. |
| **Tables** | DataTable in SI (Orders tracking uses it); other lists are Card-based or custom (JobsListPage, ServiceInstallersPage). |
| **Forms** | No react-hook-form/zod; raw state; TextInput, Textarea, Button; no shared Select, Label, or FormLayout. |

---

## Inconsistency Findings (Top 20)

| # | Category | Evidence | Why it's inconsistent | Suggested standard |
|---|----------|----------|------------------------|---------------------|
| 1 | **Page layout** | Admin: ~31 pages use `PageShell` (OrdersListPage, ParserListingPage, TasksListPage, **ReportsHubPage, ReportRunnerPage**, etc.); 78+ pages do not (BuildingsPage, all inventory/*, OrderDetailPage, CreateOrderPage, InstallerSchedulerPage, DashboardPage uses PageShell). *Reports Hub pages aligned Feb 2026.* | Some pages have title + breadcrumbs + actions bar; others use raw `<h1>` and ad-hoc padding. | Use PageShell (or a single shared page wrapper) on every content page with title + optional breadcrumbs + actions. |
| 2 | **Typography – page title** | Admin: PageShell uses `designTokens.typography.pageTitle` (`text-base font-semibold`); BuildingsPage uses `h1 text-lg font-bold`; OrderDetailPage uses `h1 text-xl md:text-2xl lg:text-3xl font-bold`; inventory pages use `h1 text-xl font-bold`. *ReportsHubPage and ReportRunnerPage now use PageShell (Feb 2026).* SI: JobsListPage `h2 text-2xl font-bold`; DashboardPage `h2 text-2xl font-bold`. | Page title element and size vary (h1 vs h2, text-base vs text-lg vs text-xl vs text-2xl). | One token: page title = `text-xl font-semibold` (or designTokens.pageTitle) on h1; use only in PageShell or shared PageHeader. |
| 3 | **Buttons** | Admin Button: variants default/destructive/outline/secondary/ghost/link; sizes default `h-9 px-3 py-1 text-xs`, sm `h-8`, lg `h-10`, icon `h-8 w-8`. SI Button: no `link`; sizes default `h-10 px-4 py-2`, sm `h-9 px-3`, lg `h-11 px-8`, icon `h-10 w-10`. | Different size scale and missing variant in SI. | Single Button API: same variants (include link) and same size classes (e.g. default h-9, sm h-8, lg h-10, icon h-8 w-8). Prefer admin as source; align SI. |
| 4 | **Cards** | Admin Card: title, subtitle, header, footer, hoverable, variant (default/bordered/elevated/frosted/outlined), compact. SI Card: only `children` + `className`; no title/subtitle/footer. | SI cards are minimal; admin has rich Card API. | Standardize on one Card: optional title, subtitle, footer; default variant; same border/radius/shadow. Use in both apps. |
| 5 | **EmptyState** | Admin: title, description/message, icon (default Inbox), action (ReactNode or `{ label, onClick }`). SI: title, description, icon only; no action. | SI EmptyState cannot show actions; different default icon (AlertCircle vs Inbox). | One EmptyState: title, description, optional icon, optional action (object or node). Same default icon. |
| 6 | **Loading** | Admin LoadingSpinner: size sm/default/lg, message, fullPage. SI LoadingSpinner: size sm/md/lg; no message, no fullPage. Admin uses Skeleton in inventory reports; SI has no Skeleton. | Different loading API and no skeleton in SI. | One LoadingSpinner (message + fullPage optional); introduce Skeleton in SI where needed; same size names (sm/default/lg). |
| 7 | **Toasts** | Admin: ToastProvider + useToast with showToast, showSuccess, showError, showWarning, showInfo, dismissToast (duration param). SI: useToast (module state, no provider) with showSuccess, showError, showInfo only; fixed 3s; no showWarning. | Different API (duration, warning) and different implementation (context vs module state). | Single toast system: Provider + useToast with success/error/warning/info and configurable duration; use in both apps. |
| 8 | **Inputs** | Admin: two primitives – `Input` (input.tsx, forwardRef, size) and `TextInput` (label, error, value/onChange). Some pages use Input (OrdersListPage, TasksListPage, StatusDropdown, DateFilter, SchedulerDialogs), others TextInput. SI: only TextInput. | Admin mixes two input components; SI has one. | Prefer one controlled primitive (e.g. TextInput with optional label/error) or clearly separate “headless” Input vs “form field” TextInput and use consistently. |
| 9 | **Status badges** | Admin: StatusBadge (variant/size) + OrderStatusBadge (uses getStatusColor + StatusBadge); utils/statusColors (ORDER_STATUS_COLORS, getStatusBadgeColor). Many pages use inline classes (BuildingsPage building type colors, OrderDetailPage reschedule/blocker/splitter, OrdersListPage getStatusBadgeColor, DocumentTemplatesPage template type colors). SI: no StatusBadge; JobsListPage uses inline `px-2 py-1 rounded-full text-xs` + bg-*-100 text-*-800. | Inline status colors scattered; SI has no shared badge. | Use StatusBadge (or shared status pill) everywhere; map status → variant via central statusColors/getStatusBadgeColor; remove inline status classes. |
| 10 | **Tables** | Admin: DataTable (generic list); StandardListTable (selection, actions); Syncfusion Grid/TreeGrid/Kanban for specific features. SI: no table component; lists are Card-based or custom divs. | SI has no reusable table; list UIs differ. | Introduce shared DataTable (or thin wrapper) in SI for list views; keep Syncfusion where it adds value (scheduler, complex grids). |
| 11 | **Spacing – page content** | Admin: PageShell controls header padding (compact vs standard); content area padding varies (some pages rely on child padding). InstallerSchedulerPage uses `p-4 space-y-4`; inventory pages use custom padding. SI: all pages use `p-4 space-y-4` or `p-4`. | No single content padding rule. | Standardize content padding (e.g. p-4 md:p-6) in one place (PageShell or layout); use same in SI. |
| 12 | **Breadcrumbs** | Admin: Breadcrumbs used on OrderDetailPage, ParserSnapshotViewerPage; many other pages have no breadcrumbs. PageShell has its own breadcrumb block (Home + ChevronRight + links). | Two breadcrumb patterns (PageShell vs Breadcrumbs component). | Use one breadcrumb pattern: prefer PageShell breadcrumbs for all pages that need navigation; or always use Breadcrumbs component with same styling. |
| 13 | **Section headings** | Admin: OrderDetailPage uses `h2 text-xl font-semibold` for sections; designTokens has sectionHeader `text-sm font-semibold`. SectionCard exists but is unused. | Section title size/weight inconsistent; SectionCard not adopted. | Use designTokens.sectionHeader or single class (e.g. text-sm font-semibold) for section titles; consider SectionCard for grouped form/content. |
| 14 | **Forms – validation** | Admin: react-hook-form + zod only in CreateOrderPage and DocumentTemplateEditorPage (doc-templates); TemplateMetaForm uses it. All other forms are uncontrolled or local state with ad-hoc validation. | Most forms lack shared validation pattern. | Standardize on react-hook-form + zod (or one schema layer) for all non-trivial forms; shared error display (e.g. under field + FormField wrapper). |
| 15 | **Primary color** | Admin: primary 263 (purple); SI: primary 221.2 (blue). Same variable names, different HSL. | Apps look different (purple vs blue). | Decide one primary (e.g. admin purple for brand); align SI CSS vars to match. |
| 16 | **Modal / dialogs** | Admin: Modal, ConfirmDialog, AlertDialog; used across settings and features. SI: no shared Modal; RescheduleRequestModal, MarkFaultyModal, etc. are custom. | SI has no shared modal component. | Provide one Modal (and confirm/alert) in shared UI; use in SI for dialogs. |
| 17 | **Icons** | Both use lucide-react. Admin: consistent icon sizes (h-4 w-4, h-5 w-5 in nav). SI: h-5 w-5 in nav, mixed elsewhere. | Minor; could standardize icon size scale (sm/md/lg). | Document icon sizes (e.g. sm 4, md 5, lg 6) and use consistently in buttons and nav. |
| 18 | **Design tokens usage** | Admin: designTokens used in PageShell, FormLayout, SectionCard only; FormLayout/SectionCard not used by any page. | Tokens exist but are underused; many pages use raw Tailwind. | Use designTokens (typography, spacing, input height) in PageShell, form fields, and new components; migrate high-traffic pages to tokens. |
| 19 | **Syncfusion vs custom** | Admin: Some list/crud pages use DataTable/StandardListTable; others use Syncfusion Grid (e.g. BusinessHours, Integrations, GuardConditions). Schedule, TreeGrid, Kanban are Syncfusion. | Unclear when to use custom table vs Syncfusion Grid. | Rule: use DataTable/StandardListTable for simple list/crud; use Syncfusion Grid only for inline edit, complex toolbar, or heavy data; wrap Syncfusion in same page chrome (PageShell, buttons, typography). |
| 20 | **SI app missing primitives** | SI: no Select, no Modal, no DataTable, no Breadcrumbs, no StatusBadge, no Skeleton, no Tabs. | SI rebuilds patterns (e.g. filters, lists) differently. | Add or share Select, Modal, StatusBadge, and optionally DataTable/Breadcrumbs from admin (or shared package) so SI matches admin patterns. |

---

## Recommended UI Standard (CephasOps UI Playbook)

- **Page layout**  
  - One shell per content page: title (optional), optional breadcrumbs, optional actions.  
  - Content padding: e.g. `p-4 md:p-6` (or designTokens).  
  - Use existing PageShell as the standard; SI gets equivalent (same padding and title area).

- **Breadcrumbs**  
  - Prefer **PageShell breadcrumbs** (pass `breadcrumbs` prop) for all content pages that need back/navigation context. Use the standalone `Breadcrumbs` component only when breadcrumbs are required outside PageShell (e.g. inside a modal or nested view). Same styling (e.g. Home + ChevronRight + links); avoid two different breadcrumb UIs on the same page.

- **Design tokens**  
  - Use `designTokens` (`frontend/src/lib/design-tokens.ts`) for typography (pageTitle, sectionHeader, fieldLabel, body, helper), spacing (tight/compact/standard), padding, input height (compact/standard/large), and border radius/shadows where applicable. Prefer tokens over raw Tailwind when adding or refactoring pages. PageShell and FormLayout already use tokens; use SectionCard for grouped form/content sections. Migrate high-traffic pages to tokens when touching them.

- **Typography**  
  - Page title: one class/token (e.g. `text-xl font-semibold` on h1).  
  - Section title: `text-sm font-semibold` (designTokens.sectionHeader).  
  - Body: `text-sm`; helper/caption: `text-xs` / `text-[10px]` muted.

- **Buttons**  
  - Variants: primary (default), secondary, destructive, outline, ghost, link.  
  - Sizes: sm (h-8), default (h-9), lg (h-10), icon (h-8 w-8).  
  - Use one Button component in both apps (admin as reference).

- **Tables**  
  - Simple lists / CRUD: DataTable or StandardListTable (sort, pagination, empty, loading).  
  - Inline edit / complex toolbar / very large data: Syncfusion Grid, wrapped with same page chrome.  
  - Do not replace Syncfusion Scheduler; standardize header, filters, and buttons around it.

- **SI list views (DataTable in SI)**  
  - Card-based lists (e.g. SI jobs list, orders tracking) are acceptable and match mobile-first UX; keep them when the content is card-shaped.  
  - When adding or refactoring an SI list that is clearly tabular (many columns, sortable, dense rows), introduce DataTable (or a thin wrapper shared with Admin) and use the same pattern (sort, pagination, empty, loading). Apply when touching SI list pages; no obligation to convert existing card lists to tables.

- **SI primitives (P2-1)**  
  - SI has Modal and Toast aligned with Admin. When adding or refactoring SI UI, introduce or share **DataTable** (for tabular lists; see SI list views above), **Tabs**, **Breadcrumbs**, **Skeleton** as needed so SI matches Admin patterns. **SI Card** can stay minimal (children + className) unless a page needs title/subtitle/footer—then align with Admin Card. Apply when touching SI; no obligation to add all primitives at once.

- **Inputs**  
  - **Form fields:** Use **TextInput** (label, error, value/onChange) for any field that needs a label or validation message.  
  - **Headless:** Use **Input** (from `input.tsx`, forwardRef, size) only when you need a bare input inside a custom wrapper (e.g. search box with icon, filter bar) and you handle label/error elsewhere. Prefer TextInput for new forms; use Input only where label/error are not needed or are custom. Same pattern in Admin and SI.

- **Forms validation**  
  - Use **react-hook-form** + **zod** (with `@hookform/resolvers/zodResolver`) for any non-trivial form (multiple fields, validation rules, or submit payload).  
  - Shared error display: show field-level errors under each input (e.g. via TextInput `error` or FormField); optional form-level error summary at top.  
  - Reference implementations: CreateOrderPage, DocumentTemplateEditorPage, TemplateMetaForm. Prefer this pattern for new forms; migrate high-traffic forms when touching them.

- **Forms**  
  - react-hook-form + zod for validation; shared error display.  
  - One input primitive for form fields (label + error + optional required); spacing via FormField or designTokens.  
  - Same Select, Label, Textarea, Switch, DatePicker pattern across forms.

- **States**  
  - **Loading rule:** Use **Skeleton** when the final layout is known (list, table, cards, dashboard); use **LoadingSpinner** when layout is unknown or a simple centered spinner is preferred (e.g. full-page modal, async-heavy screen). Both Admin and SI `skeleton.tsx` and `LoadingSpinner.tsx` document this rule in JSDoc.  
  - LoadingSpinner: optional message, fullPage.  
  - Empty: EmptyState with optional action.  
  - Error: inline error + optional error state component.  
  - Permission denied: consistent message + optional CTA.

- **Notifications**  
  - Single toast system: success, error, warning, info; configurable duration; Provider in root.

- **Icons**  
  - lucide-react only. Use a consistent size scale: **sm** `h-4 w-4` (16px) for inline text and small buttons; **md** `h-5 w-5` (20px) for nav and default buttons; **lg** `h-6 w-6` (24px) for feature icons and empty states. Prefer md in nav and buttons unless the design calls for sm/lg.

---

## Prioritized Fix Plan

- **P0 – Quick wins**  
  - Unify page title: use one class/token (e.g. designTokens.pageTitle or `text-xl font-semibold`) for all page titles; prefer inside PageShell or shared PageHeader.  
  - Unify content padding: e.g. `p-4 md:p-6` in PageShell and SI layout.  
  - Replace inline status badge classes with StatusBadge + getStatusBadgeColor (or equivalent) on top 5–10 pages.  
  - Align SI primary color (and key CSS vars) with admin.

- **P1 – Worst offenders (top 5 pages)**  
  - Orders list, Order detail, Inventory (e.g. Stock Summary, Receive), Scheduler (InstallerSchedulerPage), Buildings list: ensure each uses PageShell (or shared wrapper), same title style, same buttons/cards/tables and loading/empty states.

- **P2 – Component consolidation**  
  - Add shared PageHeader (title + breadcrumbs + actions) if needed; use in both apps.  
  - Add shared EmptyState (with action) and LoadingState (spinner + optional skeleton) used everywhere.  
  - Introduce DataTable (or wrapper) in SI for Jobs list, Service Installers, etc.  
  - Align Button/Card/EmptyState/LoadingSpinner APIs between admin and SI (single source or shared package).

- **P3 – Replace / limit legacy patterns**  
  - Prefer one input primitive (TextInput with label/error) for new forms; migrate high-traffic forms to react-hook-form + zod.  
  - Use FormLayout/FormField/SectionCard where they fit; remove duplicate patterns.  
  - Document “when to use DataTable vs Syncfusion Grid” and refactor 1–2 Syncfusion-heavy pages to use custom table where appropriate.  
  - Standardize toast API and implementation (one provider, same methods in both apps).

---

*End of audit report. No code or .css/.scss/.md edits were made; evidence is from file paths and component usage only.*
