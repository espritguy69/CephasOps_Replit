# Data Model Overview

**Related:** [05_data_model/DATA_MODEL_INDEX](../05_data_model/DATA_MODEL_INDEX.md) | [05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS](../05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md) | [Product overview](../overview/product_overview.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. Database

- **PostgreSQL** (Npgsql.EntityFrameworkCore.PostgreSQL).  
- **In-memory** provider used only when `EnvironmentName == "Testing"`.

---

## 2. Key entity groups

| Area | Key entities |
|------|----------------------|
| **Companies & partners** | Company, Partner, PartnerGroup, Vertical |
| **Users & RBAC** | User, RefreshToken, Role, UserRole, Permission, RolePermission |
| **Departments** | Department, DepartmentMembership, MaterialAllocation |
| **Orders** | Order, OrderType, OrderCategory, OrderStatusLog, OrderReschedule, OrderBlocker, OrderDocket, OrderMaterialUsage, OrderMaterialReplacement, OrderStatusChecklistItem/Answer |
| **Buildings** | Building, BuildingType, InstallationMethod, BuildingContact, BuildingSplitter, Street, HubBox, Pole, Splitter, SplitterType, SplitterPort, BuildingDefaultMaterial |
| **Service installers** | ServiceInstaller, ServiceInstallerContact, Skill, ServiceInstallerSkill |
| **Scheduler** | ScheduledSlot, SiAvailability, SiLeaveRequest |
| **Parser** | ParseSession, ParsedOrderDraft, EmailMessage, EmailAttachment, EmailAccount, ParserTemplate, EmailTemplate |
| **Inventory** | Material, MaterialCategory, StockLedgerEntry, StockBalance, StockAllocation, StockMovement, StockLocation, SerialisedItem, LedgerBalanceCache, StockByLocationSnapshot |
| **Billing** | Invoice, InvoiceLineItem, InvoiceSubmissionHistory, Payment, BillingRatecard, SupplierInvoice |
| **Rates & payroll** | GponPartnerJobRate, GponSiJobRate, GponSiCustomRate, RateCard, JobEarningRecord |
| **P&L** | PnlDetailPerOrder, PnlFact, PnlPeriod, PnlType, OverheadEntry |
| **Workflow** | WorkflowDefinition, WorkflowTransition, WorkflowJob, BackgroundJob, SystemLog |
| **Files** | File (with OneDrive fields) |
| **Audit** | AuditLog, AuditOverride |

---

## 3. Relationships (short)

- **Order** → OrderType, OrderCategory, InstallationMethod, Building, Partner, ServiceInstaller (assigned); Department (context).  
- **Building** → BuildingType, InstallationMethod.  
- **Department** → scopes OrderTypes, BuildingTypes, InstallationMethods, SplitterTypes, and other reference data.  
- **Ledger** is source of truth for stock; no direct StockBalance.Quantity writes for movements.  
- **Invoice** → Order(s), Partner; InvoiceLineItem; InvoiceSubmissionHistory (MyInvois).  
- **BackgroundJob** → JobType (string), PayloadJson, State (Queued/Running/Succeeded/Failed).

---

## 4. Reference types

- **Departments, Building types, Order types, Order categories, Installation methods, Splitter types** are reference/lookup data; many are department-scoped.  
- Full list and relationships: **[05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md](../05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md)**.

## 5. Full table list

- For the complete list of tables and reference data (seeded vs configurable), see **[DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md](../DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md)** or `ApplicationDbContextModelSnapshot.cs`.
