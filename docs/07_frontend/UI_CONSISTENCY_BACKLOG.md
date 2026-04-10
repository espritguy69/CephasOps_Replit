# UI Consistency Backlog (P2 / P3)

**Created:** 3 February 2026  
**Purpose:** Track P2/P3 and open audit findings from [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) so they can be scheduled and not forgotten.  
**Status:** See [UI_CONSISTENCY_STATUS.md](UI_CONSISTENCY_STATUS.md) for completed work.

---

## P2 – Component consolidation

| ID | Item | Notes |
|----|------|--------|
| P2-1 | **SI missing primitives** | **Done Feb 2026.** SI has shared Modal, Toast, Skeleton, Breadcrumbs, Tabs, DataTable. See [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) § SI primitives. SI Card minimal when touching. |
| P2-2 | **Toast system unification** | **Done Feb 2026.** SI now has ToastProvider + useToast (context-based) with showToast, showSuccess, showError, showWarning, showInfo, dismissToast; optional duration (default 5s, aligned with Admin). Toasts render in SI; API aligned with Admin. |
| P2-3 | **DataTable in SI** | **Done Feb 2026.** SI has shared `DataTable` component (columns, data, loading, pagination, sortable, onRowClick; mobile cards + desktop table). Use for tabular list views when adding or refactoring SI list pages. |

---

## P3 – Legacy / consistency

| ID | Item | Notes |
|----|------|--------|
| P3-1 | **Forms – validation** | **Documented Feb 2026.** See [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) § Recommended UI Standard – Forms validation: react-hook-form + zod for non-trivial forms; shared error display. Apply when adding or refactoring forms; migrate when touching. |
| P3-2 | **Inputs – single primitive** | **Documented Feb 2026.** See [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) § Recommended UI Standard – Inputs: TextInput for form fields (label/error); Input (headless) for custom wrappers (search, filters). Apply when adding or refactoring forms. |
| P3-3 | **Design tokens usage** | **Documented Feb 2026.** See [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) § Recommended UI Standard – Design tokens: use designTokens for typography, spacing, padding, input height; SectionCard for grouped sections. Apply when adding or refactoring pages; migrate high-traffic pages when touching them. |
| P3-4 | **Breadcrumbs** | **Documented Feb 2026.** See [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) § Recommended UI Standard – Breadcrumbs: prefer PageShell breadcrumbs for content pages; use standalone Breadcrumbs component only outside PageShell (e.g. modal, nested view). Apply when adding or refactoring pages. |
| P3-5 | **Icons – size scale** | **Documented Feb 2026.** See [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) § Recommended UI Standard – Icons: sm `h-4 w-4`, md `h-5 w-5`, lg `h-6 w-6`. Apply consistently in buttons and nav when touching code. |
| P3-6 | **Syncfusion vs DataTable rule** | **Done Feb 2026.** See [SYNCFUSION_VS_DATATABLE_RULE.md](SYNCFUSION_VS_DATATABLE_RULE.md). |

---

## Audit findings still open (by number)

| # | Category | Backlog / action |
|---|----------|-------------------|
| 4 | Cards | **Done Feb 2026.** SI Card has optional title, subtitle, footer. |
| 6 | Loading | SI has Skeleton; Jobs list, Job detail, Orders tracking, Dashboard, Service Installers use it. Introduce on remaining SI list pages when touching. |
| 7 | Toasts | See P2-2. |
| 8 | Inputs | See P3-2 (standard documented Feb 2026). |
| 9 | Status badges | P0 replaced inline on key pages; Installer Scheduler slot status uses StatusBadge (Feb 2026). Replace remaining inline pills on other pages as touched. |
| 10 | Tables | P2-3 done Feb 2026; SI has DataTable. Use for tabular list views when adding/refactoring. |
| 11 | Spacing | Improved via PageShell/PageHeader and .page-pad; audit remaining pages for one-off padding. |
| 12 | Breadcrumbs | P3-4 documented; SI has Breadcrumbs component (Feb 2026). Use for nested views. |
| 13 | Section headings | Use designTokens.sectionHeader or single class; consider SectionCard. |
| 14 | Forms validation | See P3-1 (standard documented Feb 2026). |
| 16 | Modal | **Done.** P2-1; SI has shared Modal. |
| 17 | Icons | See P3-5 (size scale documented Feb 2026). |
| 18 | Design tokens | See P3-3 (standard documented Feb 2026). |
| 19 | Syncfusion vs custom | See P3-6. |
| 20 | SI missing primitives | See P2-1. |

---

## How to use this backlog

1. When planning a sprint or refactor, pull items from P2 or P3 by ID (e.g. P2-2 Toast unification).
2. When fixing a specific audit finding, use the “Audit findings still open” table and the linked P2/P3 item if any.
3. When an item is done, move it to “Completed” in [UI_CONSISTENCY_STATUS.md](UI_CONSISTENCY_STATUS.md) or add a “Done” note and date in this doc.
4. Add new items here if new UI consistency gaps are found; keep IDs unique (P2-x, P3-x).
