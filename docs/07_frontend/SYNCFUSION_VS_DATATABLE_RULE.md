# When to Use DataTable vs Syncfusion Grid (Admin Portal)

**Purpose:** One rule so list/table pages use the right component and stay consistent.  
**Scope:** Admin Portal (`frontend`) only. SI app does not use Syncfusion.

---

## Rule

| Use case | Component | Why |
|----------|-----------|-----|
| **Simple list / CRUD** (sort, filter, pagination, no inline edit) | **DataTable** or **StandardListTable** | Matches app theme; lighter; same page chrome (PageShell, buttons, typography). |
| **Inline edit, complex toolbar, or very large data** | **Syncfusion Grid** (via `SyncfusionGrid` wrapper or `GridComponent`) | Built-in inline edit, Excel export, advanced filter, grouping; worth the extra dependency. |
| **Scheduler, TreeGrid, Kanban** | **Syncfusion** (ScheduleComponent, TreeGridComponent, KanbanComponent) | No custom equivalent; keep Syncfusion and style via `index.css` overrides. |

---

## Do

- Use **DataTable** or **StandardListTable** for: materials list, stock levels, simple settings lists, any list that only needs sort/pagination/empty/loading.
- Use **Syncfusion Grid** when you need: per-cell or row inline editing, Excel/PDF export from the grid, drag-to-group columns, or very large datasets with virtual scrolling.
- **Always** wrap Syncfusion pages in **PageShell** (same title, breadcrumbs, actions) and use the same buttons/typography as the rest of the app.
- Apply Syncfusion theme overrides in **one place**: `frontend/src/index.css` (e.g. `.e-grid`, `.e-treegrid`, `.e-kanban`, `.modern-scheduler`). Do not add new inline `<style>` blocks in components.

---

## Don't

- Don't add **new** Syncfusion Grid usage for a simple list that DataTable can do; the gate in `frontend/src/dev/uiConsistencyGate.ts` discourages new Syncfusion unless justified.
- Don't replace existing Syncfusion Scheduler/TreeGrid/Kanban with custom components; standardize the chrome and styling instead.
- Don't scatter Syncfusion CSS in component-level `<style>` tags; keep overrides in `index.css` using existing CSS variables.

---

## Refactoring existing pages

When touching a page that already uses Syncfusion Grid, consider:

- If the page only needs sort/filter/pagination and no inline edit or export, **refactoring to DataTable** is allowed and improves consistency.
- If the page relies on inline edit, Excel export, or grouping, **keep Syncfusion Grid** and ensure it uses PageShell and `index.css` overrides.

---

## References

- [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) – Audit and recommended standards (Tables, Syncfusion vs custom).
- [SYNCFUSION_STYLING_FIX_SUMMARY.md](SYNCFUSION_STYLING_FIX_SUMMARY.md) – Grid/TreeGrid/Kanban/Scheduler overrides in `index.css`.
- [frontend/src/dev/uiConsistencyGate.ts](../frontend/src/dev/uiConsistencyGate.ts) – PR checklist (Syncfusion usage, PageShell, etc.).
