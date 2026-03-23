# Entity Domain Map

**Related:** [05_data_model/DATA_MODEL_INDEX.md](../05_data_model/DATA_MODEL_INDEX.md) | [CODEBASE_INTELLIGENCE_MAP.md](CODEBASE_INTELLIGENCE_MAP.md)

**Purpose:** Group domain entities by business area for governance and refactoring. Not a full ERD; focus on domain grouping and relationships that matter for architecture.

---

## Domain grouping (by CephasOps.Domain folders)

| Business area | Domain folder(s) | Key entities (examples) | Docs |
|---------------|------------------|--------------------------|------|
| **Orders** | Orders | Order, OrderStatus, OrderType, OrderCategory, OrderStatusChecklistItem | entities/orders_entities.md |
| **Buildings** | Buildings | Building, BuildingType, InstallationMethod, InstallationType, Splitter, SplitterType | 02_modules/buildings, settings_entities |
| **Inventory** | Inventory | StockLedgerEntry, Material, Bin, Warehouse, StockByLocationSnapshot | entities/inventory_entities.md |
| **Billing** | Billing | Invoice, InvoiceLineItem, Payment, InvoiceSubmission (MyInvois) | entities/billing_entities.md |
| **Payroll / Rates** | Payroll, Rates | PayrollRun, PayrollEarning, RatePlan, BillingRatecard, GponRateGroup | payroll_entities, rate_engine |
| **P&L** | Pnl | Pnl aggregation entities; rebuild inputs from Orders/Payroll/Overheads | entities/pnl_entities.md |
| **Scheduler** | Scheduler | ScheduledSlot, SiAvailability, Leave, Utilization | entities/scheduler_entities.md |
| **Parser** | Parser | ParseSession, ParsedOrderDraft, ParserRule, EmailAccount, EmailTemplate | entities/parser_entities.md |
| **Workflow** | Workflow | WorkflowDefinition, GuardConditionDefinition, SideEffectDefinition, WorkflowTransition | WORKFLOW_ENGINE |
| **Notifications** | Notifications | Notification, NotificationDispatch (outbound queue) | 02_modules/notifications |
| **Events** | Events | EventStoreEntry, EventLedger (if present), envelope metadata | PHASE_8_PLATFORM_EVENT_BUS |
| **Service installers** | ServiceInstallers | ServiceInstaller, SI skills/rate plans | settings_entities, users |
| **Users / RBAC** | Users, Authorization | User, Role, Department, DepartmentMembership, Permission | entities/users_rbac_entities.md |
| **Companies** | Companies | Company (single-company) | settings_entities |
| **Settings / Reference** | Settings | OrderType, BuildingType, GlobalSetting, DocumentTemplate, KpiProfile, etc. | entities/settings_entities.md, REFERENCE_TYPES |
| **Files / Documents** | Files | File, Document; OneDrive fields on File | document_templates_entities, integrations |
| **RMA** | RMA | RmaRequest, RmaItem | entities/inventory_entities.md |
| **Background jobs** | Workers | BackgroundJob, JobExecution (orchestrated) | entities/background_jobs_entities.md |
| **Audit / Logging** | Audit | AuditLog, etc. | entities/logging_entities.md |
| **Tasks** | Tasks | Task (Kanban) | — |
| **Assets** | Assets | Asset, depreciation, maintenance | — |
| **SLA** | Sla | SlaRule, SlaEvaluation, breach types | — |

---

## Cross-domain relationships (governance-relevant)

- **Order** → Building, OrderType, OrderStatus, ScheduledSlot, ServiceInstaller, Department, Invoice (when billed).  
- **StockLedgerEntry** → Material, Bin, Order (allocation), Department.  
- **Invoice** → Order, Company, Department; **InvoiceSubmission** → MyInvois.  
- **EventStoreEntry** → All domains (event payloads reference aggregates).  
- **NotificationDispatch** → Notification, delivery channel (SMS/WhatsApp/email).

---

**Refresh:** When adding new entity folders or major new entities, update this map and 05_data_model/DATA_MODEL_INDEX.md.
