# Reporting & P&L Operations Runbook (GPON)

**Related:** [Background jobs](background_jobs.md) | [P&L boundaries](../business/pnl_boundaries.md) | [Reports Hub](../02_modules/reports_hub/OVERVIEW.md)

**Source of truth:** docs/business/pnl_boundaries.md; ReportRegistry; PnlService.

---

## 1. P&L rebuild

| Item | Implementation | Verification |
|------|----------------|--------------|
| **PnlRebuild job** | Rebuilds P&L aggregation from invoices, costs, overheads | BackgroundJobProcessorService processes "pnlrebuild" |
| **Schedule** | PnlRebuildSchedulerService enqueues daily (24h check) | Admin → Background Jobs; look for pnlrebuild |
| **Payload** | companyId, period (YYYY-MM) | Uses first active company; period = current month |
| **P&L pages** | PnlSummaryPage, PnlDrilldownPage, PnlOverheadsPage, PnlOrdersPage | /pnl/summary, /pnl/drilldown, etc. |

---

## 2. Reports Hub

| Report | Key | Export formats | Department scope |
|--------|-----|----------------|------------------|
| Orders list | orders-list | CSV, XLSX, PDF | Yes |
| Materials list | materials-list | CSV, XLSX, PDF | Yes |
| Stock summary | stock-summary | CSV, XLSX, PDF | Yes |
| Ledger | ledger | CSV, XLSX, PDF | Yes |
| Scheduler utilization | scheduler-utilization | CSV, XLSX, PDF | Yes |

**Run report:** POST /api/reports/{reportKey}/run with DepartmentId.  
**Export:** GET /api/reports/{reportKey}/export?format=csv|xlsx|pdf&departmentId=...

---

## 3. KPI profiles

| Item | Implementation |
|------|----------------|
| **KpiProfilesPage** | /settings/kpi-profiles or /kpi/profiles |
| **DocketKpiMinutes** | Target for docket receive → upload SLA |
| **InstallationKpi** | MaxJobDurationMinutes; SI completion SLA |
| **Scheduler** | SchedulerService uses GetEffectiveProfileAsync for KPI context |

---

## 4. Dashboard

| Item | Route | Content |
|------|-------|---------|
| **DashboardPage** | / (or /dashboard) | Orders stats, trends, charts; refreshes on load |

---

**Last updated:** 2026-02-09
