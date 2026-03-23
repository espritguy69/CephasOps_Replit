
# CURSOR_ONBOARDING.md – Full Version
## Purpose
This document instructs Cursor AI (and any AI coding agent) EXACTLY how to read, understand, and modify the CephasOps codebase.

## 1. How Cursor should read the repository
1. Start at [docs/01_system/SYSTEM_OVERVIEW.md](../01_system/SYSTEM_OVERVIEW.md) or [docs/README.md](../README.md).
2. Then read:
   - [docs/01_system/ARCHITECTURE_BOOK.md](../01_system/ARCHITECTURE_BOOK.md) (or [docs/architecture/README.md](../architecture/README.md))
   - [docs/03_business/STORYBOOK.md](../03_business/STORYBOOK.md)
   - [docs/04_api/API_OVERVIEW.md](../04_api/API_OVERVIEW.md) and [docs/04_api/API_CONTRACTS_SUMMARY.md](../04_api/API_CONTRACTS_SUMMARY.md)
   - [docs/05_data_model/DATA_MODEL_INDEX.md](../05_data_model/DATA_MODEL_INDEX.md) and [docs/05_data_model/DATA_MODEL_SUMMARY.md](../05_data_model/DATA_MODEL_SUMMARY.md)
3. Only after understanding these, proceed to backend/frontend code.

## 2. Architecture Expectations
- Clean Architecture (API → Application → Domain → Infrastructure)
- Multi-company tenancy: Every request must include authenticated company scope.
- RBAC: Backend must reject actions outside user’s role.
- Event-driven behaviors: order status → inventory → invoice linkages.

## 3. Coding Rules for AI
- NEVER write inline SQL—only EF Core with proper configurations.
- Every controller must:
  - Validate `companyId`
  - Apply role-based authorization attributes
- No business logic in controllers—only in Application Services.
- Domain entities must remain pure C# classes without EF/persistence logic.

## 4. End-to-End Build Instructions (AI Workflow)
1. Scaffold EF entities from [docs/05_data_model/DATA_MODEL_INDEX.md](../05_data_model/DATA_MODEL_INDEX.md) and entity specs under [docs/05_data_model/entities/](../05_data_model/entities/)
2. Build repositories
3. Build services (OrderService, SchedulerService, BillingService, etc.)
4. Build controllers
5. Build frontend pages following [docs/03_business/PAGES.md](../03_business/PAGES.md) and [docs/07_frontend/](../07_frontend/)
6. Connect SI PWA using [docs/03_business/STORYBOOK.md](../03_business/STORYBOOK.md) and [docs/07_frontend/si_app/](../07_frontend/si_app/)

## 5. Common AI Mistakes to Avoid
- Mixing domain & infrastructure namespaces.
- Ignoring multi-company scoping.
- Forgetting audit fields.
- Forgetting to update Swagger after new endpoints.

## 6. Done Checklist Before Commit
- All new APIs added to API_BLUEPRINT.md
- EF migrations updated
- Unit tests created
- Frontend API hook created
- Docs updated

