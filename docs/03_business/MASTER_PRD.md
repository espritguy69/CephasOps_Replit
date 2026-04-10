# CephasOps – Master Product Requirements Document (PRD)

**Version:** 1.0  
**Date:** December 2025  
**Status:** Production System  
**Audience:** Product Managers, Engineers, Stakeholders

---

## 1. Executive Summary

### 1.1 What is CephasOps?

**CephasOps** is an enterprise operations platform designed to manage end-to-end ISP (Internet Service Provider) service installation workflows. The system unifies partner management, order lifecycle automation, email ingestion and parsing, inventory management, billing, invoicing, and performance measurement into a single, configurable platform.

**Current Focus:** GPON (Gigabit Passive Optical Network) department operations for TIME and other ISP partners.

**Architecture:** Multi-tenant SaaS platform with per-company data isolation, multiple departments (functional units), and branches (physical locations).

### 1.2 Product Vision

To become the **definitive operations platform** for service installation companies, enabling:
- **Zero-code partner onboarding** – Add new partners without backend changes
- **Workflow-driven operations** – Enforce business rules through configurable workflows
- **End-to-end automation** – From email ingestion to invoice submission
- **Real-time visibility** – Dashboards, KPIs, and performance metrics
- **Audit-safe operations** – Complete audit trails and compliance

### 1.3 Core Objectives

1. **Operational Excellence**
   - Automate order lifecycle from email to completion
   - Enforce business rules through workflow engine
   - Minimize manual errors and delays

2. **Financial Control**
   - Accurate billing and invoicing
   - Real-time P&L tracking
   - Automated payroll calculation

3. **Scalability**
   - Support multiple partners without code changes
   - Department-based workflow isolation
   - Settings-driven configuration

4. **User Experience**
   - Intuitive admin portal for operations teams
   - Mobile-first SI app for field installers
   - Role-based access and permissions

---

## 2. Target Users

### 2.1 Admin / Operations Coordinator
- **Primary Functions:**
  - Review parsed orders from email
  - Assign orders to service installers
  - Manage schedules and reschedules
  - Upload dockets and manage invoices
  - Monitor KPIs and performance

- **Key Features:**
  - Order management dashboard
  - Scheduler calendar view
  - Parser review interface
  - Invoice creation and submission
  - KPI monitoring

### 2.2 Head of Department (HOD)
- **Primary Functions:**
  - Approve workflow overrides
  - Review department performance
  - Manage department settings
  - Approve reschedules and blockers

- **Key Features:**
  - Department dashboard
  - Override approval interface
  - Performance reports
  - Settings management

### 2.3 Service Installer (SI)
- **Primary Functions:**
  - View assigned jobs
  - Update job status (On The Way, Met Customer, Completed)
  - Upload photos and scan serial numbers
  - Complete checklists

- **Key Features:**
  - Mobile-optimized job list
  - Status transition interface
  - Photo upload
  - Serial number scanning
  - GPS tracking

### 2.4 Finance / Billing Team
- **Primary Functions:**
  - Create invoices from completed orders
  - Submit invoices to partner portals
  - Track payments and aging
  - Manage supplier invoices

- **Key Features:**
  - Invoice creation interface
  - Portal submission tracking
  - Payment recording
  - Aging reports

### 2.5 Director / Executive
- **Primary Functions:**
  - View cross-department performance
  - Monitor P&L and financial metrics
  - Review high-level KPIs
  - Strategic decision-making

- **Key Features:**
  - Executive dashboard
  - P&L summary and drilldown
  - Cross-department reports
  - Financial analytics

---

## 3. Core Product Pillars

### 3.1 Order Management Engine
- **Purpose:** Central hub for all service installation orders
- **Key Features:**
  - Order lifecycle management (Pending → Completed)
  - Status transition enforcement
  - Partner and customer data
  - Appointment scheduling
  - Material usage tracking
  - Docket management

### 3.2 Workflow Engine
- **Purpose:** Enforce business rules and validate transitions
- **Key Features:**
  - Configurable workflow definitions
  - Guard conditions (validation rules)
  - Side effects (automated actions)
  - Status transition validation
  - Checklist enforcement
  - Override management

### 3.3 Rate Engine
- **Purpose:** Calculate SI payouts and partner billing rates
- **Key Features:**
  - SI rate cards (by level, partner, order type)
  - Partner billing rates
  - Custom rate overrides
  - KPI-based bonuses and penalties
  - Payroll calculation

### 3.4 Materials & Inventory Engine
- **Purpose:** Track materials, serialized items, and stock movements
- **Key Features:**
  - Material catalog management
  - Serialized item tracking
  - Stock balance by location
  - Stock movement audit trail
  - RMA (Return Material Authorization) management
  - Splitter port management

### 3.5 KPI Engine
- **Purpose:** Measure and track performance metrics
- **Key Features:**
  - Configurable KPI profiles
  - On-time completion tracking
  - Docket quality metrics
  - SI performance scoring
  - Department-level KPIs

### 3.6 Finance Engine
- **Purpose:** Manage billing, invoicing, and financial reporting
- **Key Features:**
  - Invoice creation and management
  - Partner portal submission
  - Payment tracking
  - P&L calculation and reporting
  - Overhead allocation
  - Supplier invoice management

### 3.7 SI App (Service Installer Mobile App)
- **Purpose:** Mobile interface for field installers
- **Key Features:**
  - Job list and details
  - Status transitions
  - Photo upload
  - Serial number scanning
  - GPS tracking
  - Checklist completion

### 3.8 RMA Module
- **Purpose:** Track return material authorizations
- **Key Features:**
  - RMA request creation
  - Faulty item tracking
  - MRA (Material Return Authorization) document linkage
  - RMA status workflow

### 3.9 Email Parser Engine
- **Purpose:** Automate order creation from partner emails
- **Key Features:**
  - Email ingestion (POP3/IMAP/O365)
  - Email classification
  - Template-based parsing (Excel, PDF, HTML)
  - Order draft creation
  - Duplicate detection
  - VIP email notifications

### 3.10 Scheduler Module
- **Purpose:** Manage SI assignments and availability
- **Key Features:**
  - Calendar view (Day/Week/Month)
  - SI availability tracking
  - Leave management
  - Schedule slot management
  - Unassigned order tracking

---

## 4. Key Use Cases

### 4.1 New Order Creation (Email-Driven)
1. Partner sends email with order details (Excel/PDF attachment)
2. Email ingestion service fetches email
3. Email classification identifies order type (Activation/Modification/Assurance)
4. Parser template extracts structured data
5. System creates `ParsedOrderDraft`
6. Admin reviews and approves
7. System creates `Order` with status "Pending"
8. Order appears in scheduler for assignment

### 4.2 Order Assignment & Scheduling
1. Admin views unassigned orders in scheduler
2. Admin selects SI and time slot
3. System creates `ScheduledSlot`
4. Order status transitions to "Assigned"
5. SI receives notification
6. Order appears in SI app

### 4.3 Field Installation (SI Workflow)
1. SI opens job in SI app
2. SI marks "On The Way" (captures GPS)
3. SI arrives and marks "Met Customer" (captures GPS + photo)
4. SI completes installation
5. SI scans serial numbers (ONU, router, fiber)
6. SI uploads completion photos
7. SI completes status checklist
8. SI marks "Order Completed"
9. Order status transitions to "OrderCompleted"

### 4.4 Docket Management
1. SI submits docket (PDF/image)
2. Admin reviews docket
3. Admin uploads docket file
4. System validates docket (splitter, port, photos)
5. If valid: Order status → "DocketsReceived"
6. Admin validates and QA checks docket
7. Order status → "DocketsVerified"
8. Admin uploads to partner portal
9. Order status → "DocketsUploaded"
10. Order status → "ReadyForInvoice"

### 4.5 Invoice Creation & Submission
1. Order reaches "ReadyForInvoice" status
2. Finance team creates invoice
3. System generates invoice PDF
4. Order status → "Invoiced"
5. Finance team submits to partner portal (e-Invoice/MyInvois)
6. System records submission ID
7. Order status → "SubmittedToPortal"
8. Partner processes payment
9. Finance team records payment
10. Order status → "Completed"

### 4.6 Payroll Calculation
1. Payroll manager creates payroll period (e.g., "2025-01")
2. System calculates SI earnings:
   - Base rate per order type
   - On-time bonuses
   - KPI penalties
   - Custom rate overrides
3. System generates payroll run
4. Payroll manager reviews and finalizes
5. System locks payroll (immutable)
6. Payroll manager marks as paid

### 4.7 P&L Reporting
1. Director views P&L summary
2. System calculates:
   - Revenue (from invoices)
   - Material costs (from stock movements)
   - Labour costs (from payroll)
   - Overhead allocation
   - Gross and net profit
3. Director drills down by:
   - Period (month/year)
   - Partner
   - Order type
   - Department
   - Cost center

---

## 5. High-Level Business Flows

### 5.1 Order Lifecycle Flow
```
Email Ingestion → Parser → Order Draft → Review → Order Created (Pending)
    ↓
Assignment → Assigned → OnTheWay → MetCustomer → OrderCompleted
    ↓
DocketsReceived → DocketsVerified → DocketsUploaded → ReadyForInvoice
    ↓
Invoiced → SubmittedToPortal → Payment Received → Completed
```

**Complete Status Flow (17 Statuses):**
- **Main Flow (12):** Pending → Assigned → OnTheWay → MetCustomer → OrderCompleted → DocketsReceived → DocketsVerified → DocketsUploaded → ReadyForInvoice → Invoiced → SubmittedToPortal → Completed
- **Side States (5):** Blocker, ReschedulePendingApproval, Rejected, Cancelled, Reinvoice

**Note:** All status values use PascalCase. See `docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md` for complete definitions.

### 5.2 Blocker Resolution Flow
```
Order in Assigned/OnTheWay/MetCustomer → Blocker Declared
    ↓
Blocker Category (Customer/Building/Network/SI)
    ↓
Resolution Action → Blocker Resolved → Return to Previous Status
```

### 5.3 Reschedule Flow
```
Admin Requests Reschedule → Reschedule Pending Approval
    ↓
Partner Email Approval → Reschedule Approved
    ↓
New Appointment Date Set → Order Returns to Assigned
```

### 5.4 Material Usage Flow
```
Order Created → Material Requirements Identified
    ↓
Stock Check → Material Assigned to SI
    ↓
Installation → Material Used → Stock Movement Recorded
    ↓
Serial Numbers Scanned → Serialized Items Tracked
```

---

## 6. Current Limitations

### 6.1 Department Scope
- **Current:** Only GPON department is fully operational
- **Future:** CWO and NWO departments are infrastructure-ready but require workflow definitions

### 6.2 Partner Support
- **Current:** TIME and related partners (Digi, Celcom, U Mobile) are supported
- **Future:** New partners can be added via settings (no code changes)

### 6.3 Multi-Company
- **Architecture:** Multi-tenant SaaS with per-company data isolation
- **Implementation:** CompanyId scoping on all entities via EF Core global query filters

### 6.4 SI App
- **Current:** Basic functionality (job list, status updates, photos)
- **Future:** Enhanced features (offline mode, advanced scanning, real-time sync)

### 6.5 Notification Channels
- **Current:** In-app notifications, email notifications
- **Future:** SMS, WhatsApp integration (infrastructure ready)

---

## 7. Dependencies

### 7.1 External Systems
- **Partner Email Servers:** POP3/IMAP/O365 for email ingestion
- **Partner Portals:** TIME portal for invoice submission (manual process currently)
- **Payment Gateways:** Future integration for payment processing

### 7.2 Infrastructure
- **Database:** PostgreSQL 16 (Self-hosted on Debian 13 VPS)
- **File Storage:** Local file system (future: cloud storage)
- **Authentication:** JWT-based (HS256)

### 7.3 Third-Party Libraries
- **Syncfusion:** PDF/Excel generation, UI components
- **MailKit:** Email connectivity
- **QuestPDF:** Alternative PDF generation
- **Handlebars:** Template rendering

---

## 8. Open Questions

### 8.1 Business Rules
- **Q:** Should CWO and NWO departments share the same workflow engine or have separate workflows?
- **Q:** How should multi-branch inventory allocation work?
- **Q:** What are the exact KPI calculation formulas for each partner?

### 8.2 Technical
- **Q:** Should we migrate to cloud file storage (S3, Azure Blob)?
- **Q:** Should we implement real-time notifications (SignalR, WebSockets)?
- **Q:** Should we add API rate limiting and throttling?

### 8.3 Future Enhancements
- **Q:** Should we build a customer portal for order tracking?
- **Q:** Should we integrate with accounting software (QuickBooks, Xero)?
- **Q:** Should we add mobile app (native iOS/Android) or stick with PWA?

---

## 9. Success Metrics

### 9.1 Operational Metrics
- **Order Processing Time:** Average time from email to assignment
- **On-Time Completion Rate:** Percentage of orders completed within SLA
- **Docket Rejection Rate:** Percentage of dockets rejected by partners
- **Invoice Submission Time:** Average time from completion to invoice submission

### 9.2 Financial Metrics
- **Revenue Recognition:** Accuracy of invoice-to-revenue mapping
- **P&L Accuracy:** Variance between calculated and actual P&L
- **Payroll Accuracy:** Error rate in SI payout calculations

### 9.3 User Adoption
- **Admin Portal Usage:** Daily active users
- **SI App Usage:** Percentage of SIs using the app
- **Feature Adoption:** Usage of key features (scheduler, parser, etc.)

---

## 10. Product Roadmap (High-Level)

### Phase 1: Core Operations (✅ Complete)
- Order lifecycle management
- Workflow engine
- Email parser
- Basic SI app
- Inventory management
- Billing and invoicing

### Phase 2: Enhancements (🔄 In Progress)
- Enhanced UI components (Syncfusion)
- Advanced scheduler features
- KPI dashboard improvements
- Document generation enhancements

### Phase 3: Future (📋 Planned)
- CWO and NWO department workflows
- Advanced analytics and reporting
- Customer portal
- Mobile app (native)
- Accounting software integration

---

**Document Status:** This PRD reflects the current production system as of December 2025. It will be updated as the product evolves.

