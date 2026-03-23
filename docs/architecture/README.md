# Architecture Documentation

**Location:** `docs/architecture/`  
**Purpose:** Visual documentation of CephasOps system architecture, workflows, data flows, and UI component library

---

## Diagram Index

### 📊 [00_company-systems-overview.md](./00_company-systems-overview.md)
**Company → Systems → External Services**

High-level overview showing:
- Company structure (single company, multiple departments)
- Internal systems (Admin Portal, SI App, Backend API, Email Parser)
- External systems (Partner Email Servers, Partner Portals)
- Database and storage systems
- ISP partners and their relationships

**Use this when:** Understanding the overall system landscape, stakeholder presentations, onboarding new team members.

---

### 🏗️ [10_system-architecture-flow.md](./10_system-architecture-flow.md)
**Technical Architecture: Clean Architecture Layers**

Technical details showing:
- Clean Architecture layers (API, Application, Domain, Infrastructure)
- Dependency flow and module interactions
- Request/response flow from frontend to database
- Module dependencies and relationships

**Use this when:** Implementing new features, understanding code organization, debugging technical issues.

---

### 📧 [20_workflow_email_to_order.md](./20_workflow_email_to_order.md)
**Email to Order Creation Workflow**

Complete email processing pipeline:
- Email ingestion from partner servers
- Classification and parsing (Excel/PDF/Body)
- Data normalization and order resolution
- Draft creation and admin review
- Order creation process

**Use this when:** Troubleshooting email parsing issues, onboarding new partners, understanding order intake process.

---

### 🔄 [21_workflow_order_lifecycle.md](./21_workflow_order_lifecycle.md)
**Order Lifecycle: Complete Journey**

End-to-end order lifecycle:
- All 17 order statuses and transitions
- Field work flow (SI app interactions)
- Docket management process
- Invoicing and payment flow
- Material/inventory tracking
- Financial flow (Billing → Payroll → P&L)

**Use this when:** Understanding order progression, training operations staff, troubleshooting workflow issues, designing new features that affect order status.

---

## Codebase intelligence (architecture maps)

**Hub:** [CODEBASE_INTELLIGENCE_MAP.md](./CODEBASE_INTELLIGENCE_MAP.md) – Repository shape, runtime architecture, major domains, core flows, architecture hotspots, governance cross-links.

| Map | Purpose |
|-----|---------|
| [controller_service_map.md](./controller_service_map.md) | Controller → main service(s), domain, canonical docs |
| [module_dependency_map.md](./module_dependency_map.md) | Upstream/downstream per module; ASCII dependency overview |
| [background_worker_map.md](./background_worker_map.md) | Hosted services and job types |
| [integration_map.md](./integration_map.md) | External and internal integrations |
| [entity_domain_map.md](./entity_domain_map.md) | Entities grouped by business area |

**Report:** [../CODEBASE_INTELLIGENCE_REPORT.md](../CODEBASE_INTELLIGENCE_REPORT.md) – Summary of intelligence layer and findings.

---

## Refactor safety (Level 14)

**Report:** [../REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md) – Coupling, fragility, safe/danger zones, worker risks, refactor sequence.

| Doc | Purpose |
|-----|---------|
| [high_coupling_modules.md](./high_coupling_modules.md) | Modules ranked by coupling risk |
| [hidden_dependencies.md](./hidden_dependencies.md) | Service/service and DbContext cross-access; GetRequiredService |
| [module_fragility_map.md](./module_fragility_map.md) | Per-module fragility |
| [safe_refactor_zones.md](./safe_refactor_zones.md) | Lower-risk refactor areas |
| [refactor_danger_zones.md](./refactor_danger_zones.md) | High-risk refactor areas |
| [refactor_sequence_plan.md](./refactor_sequence_plan.md) | Suggested refactor order |
| [worker_dependency_risks.md](./worker_dependency_risks.md) | Worker dependency and coupling risks |

---

## Architecture watchdog (Level 15)

**Report:** [../ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md) – Drift, sprawl, dependency leaks, worker coupling, module boundaries.

**Systems index:** [ARCHITECTURE_GOVERNANCE_SYSTEMS.md](./ARCHITECTURE_GOVERNANCE_SYSTEMS.md) – Change Impact Predictor, Architecture Policy Engine, Auto Documentation Sync, Architecture Risk Dashboard, Self-Maintaining Architecture, Portal navigation, Governance logs.

| Doc | Purpose |
|-----|---------|
| [service_sprawl_watch.md](./service_sprawl_watch.md) | Oversized or centralizing services |
| [controller_sprawl_watch.md](./controller_sprawl_watch.md) | Controller families growing too broad |
| [dependency_leak_watch.md](./dependency_leak_watch.md) | Hidden links, cycles, cross-domain leakage |
| [worker_coupling_watch.md](./worker_coupling_watch.md) | Worker coupling and risk trend |
| [module_boundary_regression.md](./module_boundary_regression.md) | Module boundary status: stable / drifting |

---

## Integration Summary

### How It All Connects

1. **Email Intake** (Diagram: `20_workflow_email_to_order.md`)
   - Partners send emails → Email Parser processes → Creates ParsedOrderDraft → Admin approves → Order created

2. **Order Execution** (Diagram: `21_workflow_order_lifecycle.md`)
   - Order created (Pending) → Assigned → Field work (OnTheWay → MetCustomer → OrderCompleted) → Docket management → Invoicing → Payment

3. **System Integration** (Diagram: `10_system-architecture-flow.md`)
   - All operations flow through Clean Architecture layers: Frontend → API → Application → Domain → Infrastructure → Database

4. **Company Context** (Diagram: `00_company-systems-overview.md`)
   - Single company with multiple departments (GPON active, CWO/NWO future)
   - All systems support department-based routing and workflows

### Key Integration Points

1. **Email → Orders**
   - Email Parser (Background Worker) → Order Service → Order Entity
   - Integration via Domain Events and Application Services

2. **Orders → Scheduler**
   - Order status: Assigned → Scheduler creates ScheduledSlot
   - Integration via Application Service calls

3. **Orders → Inventory**
   - Order status: OrderCompleted → Material usage recorded
   - Integration via Application Service calls

4. **Orders → Billing**
   - Order status: ReadyForInvoice → Invoice created
   - Integration via Application Service calls

5. **Billing + Payroll → P&L**
   - Invoice created + PayrollRun completed → P&L calculation
   - Integration via Background Jobs

6. **Partner Portals**
   - Manual integration: Admin uploads dockets/invoices to TIME X Portal
   - Future: API integration for automated submission

---

## How to Use These Diagrams

### For Developers
- Start with `10_system-architecture-flow.md` to understand code structure
- Use `20_workflow_email_to_order.md` when working on parser features
- Reference `21_workflow_order_lifecycle.md` when implementing order status transitions

### For Operations Staff
- Start with `00_company-systems-overview.md` for system overview
- Use `21_workflow_order_lifecycle.md` to understand order progression
- Reference `20_workflow_email_to_order.md` to understand how orders are created

### For Management/Stakeholders
- Use `00_company-systems-overview.md` for high-level system view
- Reference `21_workflow_order_lifecycle.md` to understand business processes

---

## Rendering Mermaid Diagrams

These diagrams use Mermaid syntax and can be rendered in:
- **GitHub**: Automatically renders `.md` files with Mermaid code blocks
- **VS Code**: Install "Markdown Preview Mermaid Support" extension
- **Documentation Sites**: Most modern documentation tools support Mermaid (GitBook, Docusaurus, etc.)
- **Online**: Copy Mermaid code to [mermaid.live](https://mermaid.live) for interactive viewing

---

## Updating These Diagrams

When system changes occur:
1. Update the relevant diagram(s) in this folder
2. Update this README if new diagrams are added or existing ones are reorganized
3. Ensure diagrams reflect actual implementation (not just plans)
4. Add comments in Mermaid code for assumptions or future changes

---

## UI Component Library

### 📚 [ui/storybook.md](./ui/storybook.md)
**Complete Frontend Component Library & Screen Documentation**

Comprehensive Storybook-style documentation covering:
- **35+ UI Components** - All primitives with props, variants, and usage examples
- **20+ Completed Screens** - Orders, Email, Scheduler, Tasks, Inventory, Settings
- **5 Interaction Flow Diagrams** - Mermaid diagrams for key workflows
- **Complete Styling System** - Design tokens, Tailwind patterns, color scheme
- **Architecture Overview** - Feature-based structure, API integration, state management
- **Future Roadmap** - Improvements, component extraction, documentation gaps

**Use this when:** Building new UI components, understanding existing screens, implementing new features, onboarding frontend developers.

---

## Related Documentation

- [System Overview](../01_system/SYSTEM_OVERVIEW.md) - Detailed system documentation
- [Order Lifecycle Spec](../01_system/ORDER_LIFECYCLE.md) - Complete order lifecycle specification
- [Email Pipeline](../01_system/EMAIL_PIPELINE.md) - Email processing architecture
- [Technical Architecture](../01_system/TECHNICAL_ARCHITECTURE.md) - Clean Architecture details
- [Frontend Strategy](../07_frontend/FRONTEND_STRATEGY.md) - Frontend architecture overview

---

**Last Updated:** December 2025  
**Maintained By:** Documentation Architect

