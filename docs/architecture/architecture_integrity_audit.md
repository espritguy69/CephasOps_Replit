# Architecture Integrity Audit

**Date:** March 2026  
**Purpose:** Validate Clean Architecture boundaries (Domain, Application, Infrastructure, API) and document violations. Analysis only; no code changes.

**Related:** [level1_code_integrity.md](../engineering/level1_code_integrity.md) | [service_dependency_graph.md](service_dependency_graph.md) | [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md)

---

## 1. Executive summary

**Architecture health: Moderate with known violations.** **Domain** has no Infrastructure or API references (clean). **Application** correctly depends on Domain; it also references **CephasOps.Infrastructure.Persistence** (ApplicationDbContext) in many services and **Microsoft.AspNetCore.Http** (IFormFile) in Parser and Settings—both are boundary leaks. **API** references Application, Domain, and **Infrastructure** (ApplicationDbContext) in **14 controllers**—controllers must not own persistence. **Infrastructure** correctly references Domain and Application (interfaces). No **Application → API** references. The main refactor-safety actions: remove DbContext from controllers; confine ApplicationDbContext to Infrastructure and inject only abstractions (e.g. IOrderStore) into Application where possible; accept IFormFile in API and pass streams/DTOs to Application to avoid spreading AspNetCore in Application.

---

## 2. Intended boundaries (Clean Architecture)

| Layer | May reference | Must not reference |
|-------|----------------|---------------------|
| **Domain** | Nothing (or minimal shared kernel) | Application, Infrastructure, API |
| **Application** | Domain | API, Infrastructure (prefer interfaces in Domain) |
| **Infrastructure** | Domain, Application (interfaces) | API (except hosting) |
| **API** | Application, Domain (DTOs/contracts), Infrastructure (DI registration only) | DbContext in controllers (should use Application only) |

---

## 3. Violations list

### 3.1 API → Infrastructure (controllers referencing DbContext)

| Location | Violation | Risk |
|----------|-----------|------|
| OrdersController | Injects ApplicationDbContext; uses _context for queries | P1 – bypasses application layer |
| InventoryController | Same | P1 |
| PayrollController | Same | P1 |
| RatesController | Same | P1 |
| BuildingsController | Same | P1 |
| BackgroundJobsController | Same | P1 |
| BillingRatecardController | Same | P1 |
| DiagnosticsController | Same | P2 |
| EmailsController | Same | P2 |
| UsersController | Same | P2 |
| SlaMonitorController | Same | P2 |
| AdminRolesController | Same | P2 |
| BinsController | Same | P2 |

**Rule:** Controllers must not inject or use ApplicationDbContext. All data access must go through Application services.

---

### 3.2 Application → Infrastructure (ApplicationDbContext, persistence)

| Location | Violation | Risk |
|----------|-----------|------|
| DocumentGenerationService | Injects ApplicationDbContext directly | P1 – Application sees Infrastructure type |
| WorkflowEngineService | Injects ApplicationDbContext | P2 – common pattern; many Application services use DbContext |
| OrderService | Injects ApplicationDbContext | P2 |
| SchedulerService, BillingService, BuildingService, StockLedgerService, etc. | Same | P2 |

**Note:** The codebase uses **Application services that take DbContext** as the dominant pattern (not Repository per aggregate). This is an **acceptable exception** for pragmatic reasons but is a **boundary leak**. Ideal state: Domain defines IOrderStore, IInvoiceStore, etc.; Infrastructure implements them; Application uses only interfaces. Refactor is large; document as technical debt and confine new code to interfaces where feasible.

---

### 3.3 Application → Microsoft.AspNetCore (API / hosting)

| Location | Violation | Risk |
|----------|-----------|------|
| DocumentGenerationService | using Microsoft.AspNetCore.Http; (obsolete Internal) | P2 |
| ParserService, EmailIngestionService, IExcelToPdfService, IParserService, IParsedOrderDraftEnrichmentService, IEmailSendingService, ITimeExcelParserService, CompanyDeploymentService, DepartmentDeploymentService, GponDeploymentService | IFormFile / IFormFileCollection in method signatures | P2 – Application depends on web stack |

**Note:** IFormFile is a convenience for file uploads; it lives in Microsoft.AspNetCore.Http. **Acceptable shortcut** if API is the only consumer; better long-term: API converts IFormFile to Stream or byte[] and passes to Application. Document as acceptable exception with refactor option.

---

### 3.4 Domain → Infrastructure / API

| Finding | Result |
|---------|--------|
| Domain references to CephasOps.Infrastructure | **None** (grep: no matches) |
| Domain references to CephasOps.Api or Microsoft.AspNetCore | **None** |

**Domain boundary: Clean.**

---

### 3.5 Application → API

| Finding | Result |
|---------|--------|
| Application references to CephasOps.Api | **None** |

**No Application → API references.**

---

### 3.6 Cross-domain service coupling (module ownership)

| Pattern | Example | Risk |
|---------|---------|------|
| Billing → Workflow | InvoiceSubmissionService injects IWorkflowEngineService | P2 – documented; keep transitions minimal |
| Scheduler → Workflow (runtime) | SchedulerService GetRequiredService&lt;IWorkflowEngineService&gt; | P2 – hidden; make explicit via constructor |
| Building → Orders | BuildingService queries _context.Orders | P2 – cross-domain DbContext |
| Orders → many modules | OrderService injects Settings, Notifications, Inventory, Rates, Workflow, Buildings | P1 – god service; module ownership blurred |

---

## 4. Dependency map (simplified)

```
  API (Controllers)
    │ → Application (services, DTOs)
    │ → Domain (entities, interfaces)
    │ → Infrastructure [VIOLATION: 14 controllers inject DbContext]
    │
  Application
    │ → Domain
    │ → Infrastructure [LEAK: ApplicationDbContext in many services]
    │ → Microsoft.AspNetCore.Http [LEAK: IFormFile in Parser/Settings]
    │
  Infrastructure
    │ → Domain
    │ → Application (implements interfaces)
    │
  Domain
    │ → (none)
```

---

## 5. Acceptable exceptions vs problematic shortcuts

| Exception | Verdict | Action |
|-----------|---------|--------|
| Application services injecting ApplicationDbContext | **Acceptable** for current scale; documented as technical debt | Prefer I*Store abstractions for new features; gradual refactor |
| Application using IFormFile | **Acceptable** for file upload flows | Optional: API maps IFormFile → Stream before calling Application |
| Controllers injecting ApplicationDbContext | **Problematic** | Remove; introduce or use query services |

---

## 6. Refactor-safety guidance

- **Do not** add new controller dependencies on ApplicationDbContext; add or use Application query/command services instead.
- **Do not** add new Application dependencies on API assemblies or ASP.NET Core MVC types beyond IFormFile where already used.
- **Prefer** Domain interfaces (I*Store) for new persistence needs so Application stays free of Infrastructure types where possible.
- **Protect:** Domain must remain free of Infrastructure and API; Application must not reference API.

---

## 7. Related artifacts

- [Level 1 code integrity](../engineering/level1_code_integrity.md)
- [Service dependency graph](service_dependency_graph.md)
- [REFACTOR_SAFETY_REPORT](../REFACTOR_SAFETY_REPORT.md)
- [High coupling modules](high_coupling_modules.md)
