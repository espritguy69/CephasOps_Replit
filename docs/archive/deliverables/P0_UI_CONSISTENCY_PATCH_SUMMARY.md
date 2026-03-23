# P0 UI Consistency Patch Summary

**Scope:** P0 Quick Wins from UI Consistency Report.  
**Rules:** Only `.ts`/`.tsx` changes in `/frontend` and `/frontend-si`; no `.css`/`.scss`/`.md`/config edits; Syncfusion Scheduler unchanged.

---

## Files Changed

### Admin Portal (`frontend`)

| Path | What changed |
|------|----------------|
| `src/utils/statusColors.ts` | Added `getPriorityBadgeVariant(priority)`, `getBuildingTypeBadgeColor(type)`, and `StatusBadgeVariant` type for shared status/priority/building-type badges. |
| `src/pages/orders/OrdersListPage.tsx` | Replaced inline priority pill with `StatusBadge` + `getPriorityBadgeVariant`. Removed local `getPriorityBadgeColor`; use shared statusColors. |
| `src/pages/orders/OrderDetailPage.tsx` | Wrapped in `PageShell` with title, breadcrumbs, actions (Back, OrderStatusBadge, WorkflowTransitionButton). Loading and error use PageShell + LoadingSpinner / EmptyState. |
| `src/pages/inventory/InventoryStockSummaryPage.tsx` | Wrapped in `PageShell` with title "Stock Summary" and actions (location select + Refresh). Loading uses PageShell + Skeleton. |
| `src/pages/inventory/InventoryLedgerPage.tsx` | Wrapped in `PageShell` with title "Ledger". Removed custom header block. |
| `src/pages/scheduler/InstallerSchedulerPage.tsx` | Wrapped in `PageShell` with title "Service Installer Schedule" and actions (New Appointment). Loading/init use PageShell + layout-matched Skeleton (filter bar, stat cards, sidebar + calendar). "No unassigned jobs" uses `EmptyState`. *Updated Feb 2026: Skeleton replaces fullPage LoadingSpinner.* |
| `src/pages/settings/BuildingsPage.tsx` | Wrapped in `PageShell` with title "Buildings" and actions (count badge, Merge, Export, Add Building). Building type column uses `StatusBadge` + `getBuildingTypeBadgeColor`. Status column uses `StatusBadge` (success/secondary). Removed local `getBuildingTypeColor`; loading uses PageShell + LoadingSpinner. |

### SI App (`frontend-si`)

| Path | What changed |
|------|----------------|
| `src/components/layout/PageHeader.tsx` | **New.** Header bar with title, optional subtitle, actions; same padding/typography idea as Admin PageShell header. |
| `src/components/ui/StatusBadge.tsx` | **New.** StatusBadge with variant/size/className/children; `getOrderStatusVariant(status)` for order status → variant. |
| `src/components/ui/Button.tsx` | Added `link` variant; aligned sizes with Admin (default h-9, sm h-8, lg h-10, icon h-8 w-8). |
| `src/components/ui/EmptyState.tsx` | Added `action` (object or node), `message` alias for description, default icon Inbox; aligned with Admin API. |
| `src/components/ui/LoadingSpinner.tsx` | Added `message`, `fullPage`; size `default` (and `md` alias); aligned with Admin API. |
| `src/components/ui/index.ts` | Exported `StatusBadge`, `getOrderStatusVariant`. |
| `src/pages/jobs/JobsListPage.tsx` | Uses `PageHeader` "Assigned Jobs"; loading/error/empty use PageHeader + LoadingSpinner/EmptyState; content padding `p-4 md:p-6`. Job status uses `StatusBadge` + `getOrderStatusVariant`. |
| `src/pages/orders/OrdersTrackingPage.tsx` | Uses `PageHeader` "All Orders"; loading/error/access-denied use PageHeader + LoadingSpinner/EmptyState; content padding `p-4 md:p-6`. Order status uses `StatusBadge` + `getOrderStatusVariant`. Removed local `getStatusColor`. |

---

## What Changed and Why

- **Headers:** Admin high-traffic pages (Orders list/detail, Stock Summary, Ledger, Scheduler, Buildings) now use `PageShell` for a single title + breadcrumbs + actions bar and shared content padding. SI Jobs list and Orders tracking use `PageHeader` for the same idea without full shell.
- **Status pills:** Inline `span`/`className` status and priority pills replaced with `StatusBadge` (and, where relevant, OrderStatusBadge) using shared helpers: `getStatusBadgeColor`/`getPriorityBadgeVariant`/`getBuildingTypeBadgeColor` (Admin), `getOrderStatusVariant` (SI).
- **Empty/loading:** All touched pages use `EmptyState` and `LoadingSpinner` (or Skeleton where already used); no ad-hoc "No data" or custom spinners. Scheduler "No unassigned jobs" and error/loading states use EmptyState/LoadingSpinner.
- **SI alignment:** SI Button, EmptyState, and LoadingSpinner props/variants aligned with Admin. New PageHeader and StatusBadge give SI the same header and status-pill patterns as Admin.

---

## How to Manually Verify

### Admin Portal

1. **Orders list** (`/orders`)  
   - Page has PageShell title "Orders", actions (Export, Import, Create).  
   - Priority column shows pill via StatusBadge (same look, from shared variant).  
   - Loading: spinner with message; empty: EmptyState.

2. **Order detail** (`/orders/:id`)  
   - PageShell title = order id, breadcrumbs "Orders", actions Back + status badge + workflow button.  
   - Loading: PageShell + full-page spinner; error: PageShell + EmptyState with "Back to Orders".

3. **Stock Summary** (`/inventory/stock-summary`)  
   - PageShell title "Stock Summary", actions location select + Refresh.  
   - Loading: PageShell + Skeleton; empty table: EmptyState with "Go to Ledger".

4. **Ledger** (`/inventory/ledger`)  
   - PageShell title "Ledger"; filters and table unchanged.  
   - Empty: EmptyState "No ledger entries" with "Clear filters".

5. **Scheduler** (`/scheduler/timeline` or installer scheduler route)  
   - PageShell title "Service Installer Schedule", actions "New Appointment".  
   - Loading/init: full-page LoadingSpinner.  
   - Unassigned panel: "No unassigned jobs" via EmptyState when empty.  
   - Syncfusion Scheduler unchanged.

6. **Buildings** (`/settings/buildings`)  
   - PageShell title "Buildings", actions count + Merge + Export + Add Building.  
   - Table: Type column = StatusBadge with building type color; Status column = StatusBadge Active/Inactive.  
   - Loading: PageShell + full-page spinner.

### SI App

1. **Assigned Jobs** (`/jobs`)  
   - PageHeader "Assigned Jobs".  
   - Loading: header + spinner with message; error/empty: header + EmptyState.  
   - Job cards: status via StatusBadge (same colors as before).  
   - Content area padding `p-4 md:p-6`.

2. **All Orders** (`/orders`)  
   - PageHeader "All Orders".  
   - Loading/error/access denied: header + LoadingSpinner/EmptyState.  
   - Order cards: status via StatusBadge.  
   - Content area padding `p-4 md:p-6`.

---

## TypeScript Build

- **Admin (`frontend`):** `npx tsc --noEmit` completes with exit code 0.
- **SI (`frontend-si`):** Project has pre-existing TS errors (e.g. `ImportMeta.env`, unused vars, other modules). This patch does not fix those. Only P0-touched SI files were adjusted (e.g. unused `Button` import removed in OrdersTrackingPage).

---

*End of P0 UI Consistency Patch Summary.*
