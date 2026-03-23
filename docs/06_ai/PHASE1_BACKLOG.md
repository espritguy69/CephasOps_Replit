
# PHASE1_BACKLOG.md – Full Version
This backlog contains ALL required tasks Cursor AI must complete to build CephasOps Phase 1.

## 1. Backend Infrastructure
- [ ] Initialize ASP.NET Core 8 Web API
- [ ] Setup Entity Framework Core + PostgreSQL
- [ ] Add Serilog logging pipeline
- [ ] Add JWT authentication + role-based auth filters
- [ ] Add company scoping middleware
- [ ] Add global exception filter
- [ ] Add Hangfire for background tasks

## 2. Domain Layer Tasks
- [ ] Implement full domain entities:
  - Company, User, Role, Permission
  - Order, OrderStatusLog
  - Building, Splitter, SplitterPort
  - Material, MaterialStock, MaterialMovement
  - Invoice, InvoiceLine
  - ParserRule, NotificationSetting
  - RmaRequest
- [ ] Create value objects for:
  - Address
  - SerialNumbers
  - MaterialsUsed

## 3. Application Layer Tasks
- [ ] OrderService
- [ ] SchedulerService
- [ ] InventoryService
- [ ] BillingService
- [ ] RmaService
- [ ] EmailParserService
- [ ] KpiService
- [ ] NotificationService

## 4. API Controllers
- [ ] OrdersController
- [ ] SchedulerController
- [ ] InventoryController
- [ ] DocketsController
- [ ] InvoicesController
- [ ] SettingsController
- [ ] BuildingsController
- [ ] MaterialsController
- [ ] PartnersController
- [ ] InstallersController

## 5. Frontend (Admin Web)
- [ ] Dashboard page
- [ ] Orders list + detail view
- [ ] Scheduler (drag & drop calendar)
- [ ] Inventory pages (Stock, Movements, RMA)
- [ ] Billing pages (Invoices, Uploads)
- [ ] Settings pages

## 6. SI PWA
- [ ] Login
- [ ] Today’s jobs
- [ ] Status updates (GPS + timestamp)
- [ ] Materials usage
- [ ] Photos upload
- [ ] Offline mode

## 7. QA & Documentation
- [ ] Add Postman collection
- [ ] Update API Blueprint
- [ ] Add UML diagrams
- [ ] Write code review guidelines
- [ ] Deploy staging environment

