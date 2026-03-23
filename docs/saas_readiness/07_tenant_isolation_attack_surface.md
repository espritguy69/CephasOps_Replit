# Tenant Isolation Attack-Surface Checklist

**Date:** 2026-03-13

Dedicated checklist of places where cross-tenant leakage commonly appears. Use for security-focused testing and code review. Each item should be verified for CephasOps (test or audit).

---

## 1. List endpoints

| Area | Risk | CephasOps surface | Verification |
|------|------|--------------------|--------------|
| **Orders** | List returns all tenants’ orders | GET /api/orders | Two-tenant test; assert only current tenant’s IDs. |
| **Buildings** | Same | GET /api/buildings | Same. |
| **Service installers** | Same | GET /api/service-installers | Same. |
| **Partners** | Same | GET /api/partners | Same. |
| **Invoices** | Same | GET /api/invoices (or billing list) | Same. |
| **Materials** | Same | GET /api/inventory/materials or materials list | Same. |
| **Departments** | List includes other tenant’s departments | GET /api/departments | Restrict to current tenant’s departments. |
| **Email accounts** | Same | GET /api/email-accounts | Same. |
| **Parser drafts/sessions** | Same | ParserController list endpoints | Same. |
| **Notifications** | Same | GET /api/notifications | Same. |
| **Background jobs** | Job list shows other tenant’s jobs to non-admin | GET /api/background-jobs or SystemWorkers | Admin-only or filter by tenant. |
| **Tasks** | Same | GET /api/tasks | Same. |
| **Scheduler slots** | Same | SchedulerController calendar/slots | Same. |
| **Files** | Same | GET /api/files (if list exists) | Same. |
| **Report definitions** | Usually global; report *run* must be scoped | GET /api/reports/definitions | Run uses _tenantProvider + department. |

**Action:** For each list endpoint, run as tenant A and tenant B; assert no overlap in primary keys or companyId.

---

## 2. Detail by ID

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Order** | GET /api/orders/{id} returns B’s order to A | OrdersController Get(id) | 404 when id belongs to B and user is A. |
| **Invoice** | Same | BillingController or Invoices | Same. |
| **Building** | Same | BuildingsController | Same. |
| **Service installer** | Same | ServiceInstallersController | Same. |
| **Partner** | Same | PartnersController | Same. |
| **Material** | Same | InventoryController | Same. |
| **File** | Same | FilesController Get(id) / download | Same. |
| **Document** | Same | DocumentsController | Same. |
| **Notification** | Same | NotificationsController | Same. |
| **Parse session / draft** | Same | ParserController | Same. |
| **Job execution** | Detail shows other tenant’s job run | ObservabilityController / job run detail | Filter or 404 for other tenant. |

**Action:** For each detail endpoint, as user A call with a valid ID that belongs to tenant B; expect 404 (or 403).

---

## 3. Related entity lookups

| Area | Risk | Example | Verification |
|------|------|--------|--------------|
| **Order → Building** | Loading building for order loads B’s building for A’s order | Order detail includes building | Building must be same tenant as order (FK). |
| **Order → Partner** | Same | Order detail includes partner | Same. |
| **Invoice → Order** | Same | Invoice detail includes order | Same. |
| **Dropdowns / autocomplete** | Options include other tenant’s entities | Partner, building, SI dropdowns | All options from current tenant only. |

**Action:** Ensure all related entities are loaded via tenant-scoped query or same-tenant FK; no IgnoreQueryFilters for tenant-scoped entities without bypass.

---

## 4. Search / autocomplete

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Order search** | Keyword or filter returns B’s orders to A | Orders search API | Results filtered by companyId. |
| **Material search** | Same | Materials autocomplete / search | Same. |
| **Building search** | Same | Building autocomplete | Same. |
| **Partner search** | Same | Partner autocomplete | Same. |
| **SI search** | Same | Service installer search | Same. |

**Action:** Search with term that matches only tenant B; as A get zero B results.

---

## 5. Reports

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Orders list report** | Rows from all tenants | ReportsController run "orders-list" | companyId from _tenantProvider only. |
| **Stock summary** | Same | "stock-summary" | Same. |
| **Ledger** | Same | "ledger" | Same. |
| **Materials list report** | Same | "materials-list" | Same. |
| **Scheduler utilization** | Same | "scheduler-utilization" | Same. |
| **Custom/definition-based** | Any report that runs with context | Report run path | All use resolved companyId + department. |

**Action:** Run each report as A and B; assert row set disjoint; no companyId query parameter override.

---

## 6. Exports

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Orders list export** | CSV/Excel/PDF contains B’s data for A | ReportsController export orders-list | File contains only A. |
| **Stock summary export** | Same | Export stock-summary | Same. |
| **Ledger export** | Same | Export ledger | Same. |
| **Materials export** | Same | Export materials-list | Same. |
| **Scheduler utilization export** | Same | Export scheduler-utilization | Same. |
| **Pagination** | Export uses wrong page or scope | Large export (e.g. 10k rows) | All pages from same tenant. |

**Action:** Export as A; parse file; assert every row belongs to A (by companyId or ID range).

---

## 7. Dashboards

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **KPI / counts** | Dashboard shows counts from all tenants | Dashboard API or home | Counts from current tenant only. |
| **Charts** | Same | Any chart data | Same. |
| **Recent activity** | Same | Recent orders, notifications | Same. |

**Action:** Dashboard as A and B; numbers and items must be tenant-scoped.

---

## 8. Background jobs

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Job execution** | Job runs without tenant scope or wrong company | BackgroundJobProcessorService ProcessJobAsync | RunWithTenantScopeOrBypassAsync(job.CompanyId ?? payload). |
| **Enqueue** | New job created without CompanyId | All enqueue call sites | job.CompanyId set from current context. |
| **Reap** | Reap updates or reads wrong tenant’s data | ReapStaleRunningJobsAsync | Only job state updated; platform bypass; no business data mixed. |
| **Scheduler enumeration** | Scheduler loops over tenants but runs work without scope | EmailIngestion, PnlRebuild, etc. | Per-tenant work under RunWithTenantScopeAsync or job with CompanyId. |

**Action:** See [09_background_job_tenant_safety.md](09_background_job_tenant_safety.md); integration tests for job scope and enqueue CompanyId.

---

## 9. Notifications

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Create** | Notification created for wrong tenant or with null company | NotificationService.CreateNotificationAsync | CompanyId required for tenant-owned; skip or fail when null. |
| **Dispatch** | Recipients include other tenant’s users | NotificationDispatchRequestService | Dispatch list filtered by tenant. |
| **List** | User sees other tenant’s notifications | GET /api/notifications | List scoped to current tenant. |
| **Retention** | Retention deletes or updates wrong tenant’s data | NotificationRetentionService | RunWithTenantScopeOrBypassAsync(companyId) per tenant. |

**Action:** Create notification as A; assert only A’s users can receive; retention run does not touch B.

---

## 10. File / document access

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Download by ID** | File ID from B returns file to A | FilesController, DocumentsController | Lookup by id must filter by tenant or 404. |
| **Upload** | File stored with wrong tenant | Upload endpoint | File.CompanyId from current context. |
| **Template** | Document template from B used for A (or vice versa) | DocumentTemplatesController | Template list and use scoped. |
| **Generated document** | Generated doc contains B’s data for A’s order | Document generation job | Job runs under order’s company. |

**Action:** Download file with B’s file id as A → 404; upload as A → file has A’s CompanyId.

---

## 11. Audit / history views

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Order history** | Order status log or history shows entries from B | Order history API | Filter by order’s tenant. |
| **Event store** | Event list or ledger returns B’s events to A | EventStoreController, EventLedgerController | Filter by companyId or scopeCompanyId. |
| **Audit log** | Audit list returns B’s audit rows to A | Audit log endpoint | Filter by tenant. |
| **Command processing log** | Same | CommandOrchestrationController or similar | Same. |

**Action:** List events or audit as A; assert only A’s records.

---

## 12. Retries / replays

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Event replay** | Replay dispatches under wrong scope | EventReplayService | RunWithTenantScopeOrBypassAsync(entry.CompanyId). |
| **Retry failed event** | Same | Retry one event | Same. |
| **Operational rebuild** | Rebuild reads/writes wrong tenant | Rebuild service | Scope per tenant or per entity’s company. |

**Action:** Replay B’s event; handler must run with B’s scope; no A data modified.

---

## 13. Deep links / direct URLs

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Order detail URL** | /orders/{id} with B’s id as A | Frontend route + API | 404 or forbidden page. |
| **Invoice detail URL** | Same | Same | Same. |
| **SI app job URL** | /job/{id} with B’s order id | SI app + SiAppController | 404. |

**Action:** Paste or navigate to URL with other tenant’s id; expect 404 or error page, not data.

---

## 14. Cache / state reuse

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **In-memory cache** | Cache keyed by non-tenant key returns B’s data to A | Any IMemoryCache or static cache | Key includes companyId where data is tenant-specific. |
| **Response cache** | Cached response from A returned to B | Response caching (if any) | Cache key includes tenant. |
| **Frontend state** | Stale company context after switch | Admin company switch | Clear or refresh state on switch. |

**Action:** Code review; no shared cache for tenant-specific data without tenant in key.

---

## 15. Admin / support tools

| Area | Risk | CephasOps surface | Verification |
|------|------|-------------------|--------------|
| **Company list** | Intended to list all tenants for admin | TenantProvisioningController, CompaniesController | OK to list companies; operational data must not mix. |
| **User list** | User list shows all tenants’ users to non-super-admin | AdminUsersController | Filter by company or require SuperAdmin. |
| **Diagnostics** | Diagnostic endpoint returns other tenant’s data | DiagnosticsController, ControlPlaneController | Scope or restrict to current tenant / admin-only. |
| **Operations overview** | Platform health shows tenant-specific data | OperationsOverviewController | Tenant-specific sections scoped or admin-only. |
| **X-Company-Id** | Override used to access B as A (non-SuperAdmin) | TenantProvider | Override only when SuperAdmin. |

**Action:** As non-SuperAdmin, ensure no way to list or access other tenant’s operational data via admin endpoints; X-Company-Id ignored.

---

## Sign-off

Use this checklist in conjunction with [01_master_checklist.md](01_master_checklist.md). Each section can be signed off when verification (test or audit) is complete:

- [ ] List endpoints  
- [ ] Detail by ID  
- [ ] Related entity lookups  
- [ ] Search / autocomplete  
- [ ] Reports  
- [ ] Exports  
- [ ] Dashboards  
- [ ] Background jobs  
- [ ] Notifications  
- [ ] File / document access  
- [ ] Audit / history views  
- [ ] Retries / replays  
- [ ] Deep links / direct URLs  
- [ ] Cache / state reuse  
- [ ] Admin / support tools  
