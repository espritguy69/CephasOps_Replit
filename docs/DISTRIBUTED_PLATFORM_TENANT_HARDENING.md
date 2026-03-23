# CephasOps Tenant / Company Hardening (Phase 1)

**Date:** 2026-03-09

---

## 1. Current state

- **CompanyId** is nullable on many entities and DTOs after the earlier "company feature removal." Single-company deployment is the norm; multi-department with department-scoped RBAC is in use.
- **Events:** WorkflowEngineService sets **CompanyId** on all emitted events (WorkflowTransitionCompleted, OrderStatusChanged, OrderAssigned). EventStoreEntry stores CompanyId; observability and replay can filter by CompanyId.
- **Department scope:** Most data access is scoped by department (or company) via IDepartmentAccessService and request context. SuperAdmin can bypass for global access.

---

## 2. Phase 1 hardening (extraction-ready, no breaking change)

- **Events:** All new domain events must set **CompanyId** when the operation is tenant-scoped. Existing workflow events already do.
- **Commands / queries:** For any new command or query that is tenant-scoped, accept or infer **CompanyId** and apply it to filters and event emission. Do not introduce global queries that bypass tenant where the business meaning is tenant-scoped.
- **No global query filter** on DbContext for CompanyId in Phase 1 (single-company remains valid). When moving to multi-tenant SaaS, add optional global filter and ensure all entities that should be tenant-scoped have CompanyId and it is set.
- **Audit:** When adding or touching business records, ensure CompanyId is present where the entity is tenant-scoped (see DISTRIBUTED_PLATFORM_PHASE1_AUDIT.md for gaps).

---

## 3. Future multi-tenant

- Treat **CompanyId** as the tenant key. Department remains a sub-scope within a tenant.
- Re-introduce tenant resolution (e.g. from JWT or request header) when moving to multi-company SaaS; ensure no cross-tenant data leakage.
