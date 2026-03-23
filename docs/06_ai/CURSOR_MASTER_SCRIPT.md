\# CURSOR\_MASTER\_SCRIPT – How AI Must Build CephasOps



You are an AI coding assistant working on the CephasOps platform.



\## 0. Golden Rules



\- \*\*Do not invent requirements.\*\*  

&nbsp; Always follow `/docs` – especially:

&nbsp; - `/docs/EXEC\_SUMMARY.md`

&nbsp; - `/docs/architecture/ARCHITECTURE\_OVERVIEW.md`

&nbsp; - `/docs/spec/api/API\_BLUEPRINT.md`

&nbsp; - `/docs/spec/database/DATABASE\_SCHEMA.md`

&nbsp; - `/docs/storybook/STORYBOOK.md`

&nbsp; - `/docs/storybook/PAGES.md`

&nbsp; - `/docs/ai/CURSOR\_ONBOARDING.md`

&nbsp; - `/docs/ai/PHASE1\_BACKLOG.md`

&nbsp; - `/docs/governance/AI\_RULES\_FOR\_CURSOR.md`



\- \*\*Do not touch `main` branch directly.\*\*

&nbsp; - Work only on `feature/\*` branches.



\- \*\*Do not bypass multi-company scoping, auth, or RBAC.\*\*



\- \*\*Do not skip audit fields\*\* (`CompanyId`, `CreatedAt`, `UpdatedAt`, etc.).



\- \*\*Do not leave APIs undocumented.\*\*

&nbsp; - Any new endpoint must be added to `API\_BLUEPRINT.md`.



---



\## 1. Initial Setup (Once)



When starting CephasOps implementation in a fresh repo:



1\. Read these files in this order:

&nbsp;  1. `/docs/EXEC\_SUMMARY.md`

&nbsp;  2. `/docs/architecture/ARCHITECTURE\_OVERVIEW.md`

&nbsp;  3. `/docs/storybook/STORYBOOK.md`

&nbsp;  4. `/docs/spec/api/API\_BLUEPRINT.md`

&nbsp;  5. `/docs/spec/database/DATABASE\_SCHEMA.md`

&nbsp;  6. `/docs/ai/PHASE1\_BACKLOG.md`

&nbsp;  7. `/docs/governance/AI\_RULES\_FOR\_CURSOR.md`

&nbsp;  8. `/docs/governance/REVIEW\_CHECKLIST.md`



2\. Confirm:

&nbsp;  - You understand the \*\*Phase 1 scope\*\* (ISP vertical).

&nbsp;  - You understand the \*\*layered architecture\*\*:

&nbsp;    - API → Application → Domain → Infrastructure.

&nbsp;  - You understand \*\*multi-company\*\* and \*\*RBAC\*\* requirements.



---



\## 2. Work Only in Feature Branches



Assume a human has created a branch like:



\- `feature/phase1-backend-bootstrap`

\- `feature/phase1-frontend-bootstrap`

\- `feature/phase1-si-pwa-bootstrap`



You must:



\- Generate and modify code \*\*only\*\* on this branch.

\- Never touch `main` or delete branches.



Mention branch name in your initial message when you start work.



---



\## 3. Implementation Order for Phase 1 (Backend)



Follow this rough sequence (one feature branch at a time):



1\. \*\*Backend Bootstrap\*\*

&nbsp;  - Create `/backend` solution with:

&nbsp;    - `CephasOps.Api`

&nbsp;    - `CephasOps.Application`

&nbsp;    - `CephasOps.Domain`

&nbsp;    - `CephasOps.Infrastructure`

&nbsp;  - Wire up:

&nbsp;    - ASP.NET Core 10 Web API

&nbsp;    - EF Core + PostgreSQL

&nbsp;    - JWT auth

&nbsp;    - Serilog logging

&nbsp;    - Basic health check endpoint



2\. \*\*Domain Entities\*\*

&nbsp;  - Implement all entities from `PHASE1\_DOMAIN\_MODELS.md`.

&nbsp;  - Keep Domain pure (no EF attributes).



3\. \*\*DbContext \& EF Configuration\*\*

&nbsp;  - Implement `CephasOpsDbContext` in Infrastructure.

&nbsp;  - Add Fluent configurations matching `DATABASE\_SCHEMA.md`.

&nbsp;  - Generate initial migration.



4\. \*\*Multi-Company \& RBAC\*\*

&nbsp;  - Add multi-company middleware or filters.

&nbsp;  - Implement `ICompanyScopedRepository<>`.

&nbsp;  - Ensure every query filters by `CompanyId`.



5\. \*\*Orders Module\*\*

&nbsp;  - Application services for Orders (list, detail, create, status updates).

&nbsp;  - API controllers for Orders:

&nbsp;    - Implement only endpoints documented in `API\_BLUEPRINT.md`.



6\. \*\*Scheduler / Inventory / Billing / RMA\*\*

&nbsp;  - Implement step by step according to `PHASE1\_BACKLOG.md`.



After each feature branch:



\- Update `API\_BLUEPRINT.md` if endpoints were added/changed.

\- Ensure code passes review checklist.



---



\## 4. Implementation Order for Phase 1 (Frontend Admin)



When working on `/frontend`:



1\. Setup React + Vite + TypeScript.

2\. Add React Query, React Router.

3\. Implement pages according to `/docs/storybook/PAGES.md`:

&nbsp;  - `/login`

&nbsp;  - `/dashboard`

&nbsp;  - `/orders`

&nbsp;  - `/orders/:id`

&nbsp;  - `/scheduler`

&nbsp;  - `/inventory`

&nbsp;  - `/invoices`

&nbsp;  - `/pnl`

&nbsp;  - `/settings`

4\. Use only API endpoints from:

&nbsp;  - `/docs/spec/api/API\_BLUEPRINT.md`

&nbsp;  - `/docs/ai/API\_EXAMPLES.md`



Do not invent fields; follow DTOs from backend or docs.



---



\## 5. Implementation Order for Phase 1 (SI PWA)



When working on `/frontend-si`:



1\. Setup React PWA (mobile-first) with:

&nbsp;  - Routing

&nbsp;  - Offline support (service worker)

2\. Implement pages:

&nbsp;  - `/login`

&nbsp;  - `/jobs` (today’s jobs)

&nbsp;  - `/jobs/:id` (job details)

3\. Implement:

&nbsp;  - Status buttons: `OnTheWay`, `MetCustomer`, `OrderCompleted`

&nbsp;  - GPS capture when status changes

&nbsp;  - Photo capture for site completion

&nbsp;  - Materials usage screen



Use APIs documented in:

\- `API\_BLUEPRINT.md`

\- `STORYBOOK.md` SI flow sections.



---



\## 6. Behaviour on Every Change



For each set of changes:



1\. \*\*Check Governance:\*\*

&nbsp;  - Apply `/docs/governance/REVIEW\_CHECKLIST.md`.



2\. \*\*Update Docs if needed:\*\*

&nbsp;  - If API changed → update `API\_BLUEPRINT.md`.

&nbsp;  - If flows changed → update `STORYBOOK.md` or `PAGES.md`.



3\. \*\*Never leave partial implementations undocumented.\*\*



4\. \*\*Do not auto-merge.\*\*

&nbsp;  - Leave final merge decision to human owner.



---



\## 7. When in Doubt



If something is \*\*missing\*\* in `/docs`:



\- Ask explicit questions like:

&nbsp; - “DATABASE\_SCHEMA doesn’t define X. Should I infer it or leave placeholder?”

\- Prefer placeholders with `TODO` comments over guessing.

\- Do not silently invent business rules.



---



This script is the \*\*single source of truth for AI behavior\*\* when building CephasOps.



