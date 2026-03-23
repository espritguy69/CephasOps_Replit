# CephasOps – Product Storybook

**File Location:** `docs/03_business/STORYBOOK.md`

This is the **master business storybook** for CephasOps. It describes how the business operates in real life and ties together all modules: Parser → Orders → Scheduler → SI App → Inventory → Billing → Payroll → P&L → Directors.

This document is meant to be read by:
- Human developers
- AI coding assistants (Cursor, etc.)

…so that everyone understands **how the system should behave end-to-end** before writing code.

---

## Purpose of This Storybook

The Storybook defines:
- How the business behaves
- How users interact with the system
- What real-world scenarios the system must support
- What rules, workflows, approvals, and outcomes matter
- How people, partners, and documents flow across the organisation

It gives context for:
- Parser
- Order Lifecycle
- Workflow Engine
- Scheduler
- Inventory
- Billing
- Payroll
- P&L
- Directors' dashboards

Think of it as the **"movie script"** for the entire CephasOps system.

---

## Characters (Actors)

### External Parties
- **TIME** (Main ISP partner)
- **Celcom / Digi / U Mobile** (Other ISP partners)
- **Customers** (Residential / Commercial)
- **Building Management**
- **Vendors** (Material suppliers)
- **RMA Partners**
- **Banks / Payment channels**

### Internal Roles
- **Admin** / Operations coordinators
- **Scheduler**
- **Service Installer (SI)** – in-house & subcon
- **Warehouse / Inventory Team**
- **Finance / Billing team**
- **Payroll Manager**
- **Directors** for multiple companies:
  - Cephas Sdn. Bhd (ISP)
  - Cephas Trading & Services (ISP)
  - Kingsman Classic Services (Barbershop & Spa)
  - Menorah Travel and Tours Sdn Bhd (Travel)

Each interacts with the system differently.

---

## Current Architecture: Single Company with Multiple Departments and Branches

**Important:** CephasOps operates in **single-company mode** with **multiple departments** (functional units) and **multiple branches** (physical locations) for organizational structure, workflow ownership, and data scoping.

### Company Context

- There is **one global company** context for all operations
- All data (orders, inventory, invoices, PNL) belongs to this single company
- No company switching or multi-company isolation layers
- The `companyId` field exists for backward compatibility but represents the implicit root company

### Department Structure (Functional Units)

Within the single company, operations are organized into **departments**:

**Example Departments:**
- **Operations (OPS)** – Handles order assignment, scheduling, reschedules
- **Installer Team** – Manages service installers (SI), field operations
- **Material/Warehouse** – Inventory management, material allocations
- **Finance** – Billing, invoicing, payment tracking
- **Billing** – Invoice creation, portal submissions
- **Customer Service** – Customer support, issue resolution
- **QA/QC** – Quality assurance and quality control
- **Project Management** – Project coordination
- **Technical/Network** – Technical support, network issues

### Branch Structure (Physical Locations)

Branches represent **physical locations** or **operational sites** within the company:

**Example Branches:**
- **HQ Subang** – Main headquarters office
- **Warehouse** – Central warehouse facility
- **Kelana Jaya Branch** – Regional service branch
- **HICOM Branch** – Regional service branch
- **North Region** – Regional operations center
- **South Region** – Regional operations center

**Branch Characteristics:**
- Physical address and location (with optional GPS coordinates)
- Can represent retail locations, warehouses, regional offices, or service areas
- Optional scoping for orders, inventory, and installer assignments
- Can have branch-specific settings or configurations

### How Departments Work

#### 1. Order Ownership & Workflow

- Each **Order** has a `currentDepartmentId` that indicates which department "owns" the order at any given time
- As orders move through statuses, the **Workflow Engine** can reassign ownership to different departments based on `DepartmentWorkflowRule`
- Example flow:
  - New order → Assigned to **Operations** department
  - Order completed → Transferred to **Finance** department for billing
  - Invoice created → Transferred to **Billing** department for submission

#### 2. Department-Based Filtering & Visibility

- Users see data filtered by their **active department**
- A user can belong to multiple departments (primary + secondary)
- **Department Managers** can see all orders in their department
- **Company Admins / Directors** can see all departments (cross-department view)
- Frontend automatically injects `departmentId` into API calls via the API client

#### 2b. Branch-Based Scoping (Optional)

- Orders can optionally be assigned to a **branch** (`branchId`) for physical location tracking
- Inventory can be scoped by branch for multi-warehouse scenarios
- Installers can be assigned to branches for regional operations
- Branch filtering can be combined with department filtering
- Useful for:
  - Multi-location retail operations (e.g., Kingsman branches)
  - Regional service areas
  - Warehouse locations
  - Physical asset tracking

#### 3. Material Allocations by Department

- **DepartmentMaterialAllocation** defines which materials each department can access
- Material movements track `fromDepartmentId` and `toDepartmentId`
- Warehouse can allocate materials to specific departments
- Departments can request materials from warehouse

#### 4. Cost Center Tracking

- Each department can have associated **cost centers** for P&L tracking
- Material costs are allocated to departments via `DepartmentMaterialCostCenter`
- P&L reports can show profit/loss by department
- Department-level KPIs and performance metrics

#### 5. Department Workflow Rules

- **DepartmentWorkflowRule** determines department ownership based on:
  - Order Type (Activation, Modification, Assurance)
  - Order Status (Pending, Assigned, Completed, etc.)
  - Priority rules (which department takes precedence)
- If no rule matches, system falls back to company's default department

#### 6. User-Department Assignment

- Users are assigned to departments via **DepartmentMember**
- A user has:
  - **Primary Department** – Main department for filtering
  - **Secondary Departments** – Additional departments for cross-functional access
  - **IsManager** flag – Determines approval permissions
- Users can switch their active department context (if they belong to multiple)

### Department & Branch Flow Example

**Scenario:** New TIME Activation Order

1. **Email Parser** receives order → Creates `ParsedOrder`
2. **Order Created** → 
   - `currentDepartmentId` = **Operations** (default for new orders)
   - `branchId` = **HQ Subang** (optional, based on order location or default)
3. **Operations Department** sees the order in their queue
4. **Scheduler** (in Operations) assigns SI → Order status: `Assigned`
   - SI may be assigned from a specific branch (e.g., **North Region** branch)
5. **SI completes job** → Order status: `OrderCompleted`
   - Materials used are tracked from branch warehouse if applicable
6. **Workflow Engine** checks rules → Transfers to **Finance** department
7. **Finance Department** sees order → Creates invoice
8. **Billing Department** receives invoice → Submits to TIME portal
9. **Payment received** → P&L updated with department cost allocation and branch tracking

**Branch-Specific Scenario:** Kingsman Barbershop

1. **Customer walks in** to **Kelana Jaya Branch**
2. **Order created** → 
   - `currentDepartmentId` = **Operations**
   - `branchId` = **Kelana Jaya Branch**
3. **Service completed** → Order status: `Completed`
4. **Payment processed** → Linked to **Kelana Jaya Branch** for branch-level sales reporting
5. **P&L shows** revenue and costs by branch for performance comparison

### Department & Branch Permissions & Access

- **Department Members** see:
  - Orders owned by their department
  - Materials allocated to their department
  - KPIs for their department
  - Department-specific dashboards
  - Optionally filtered by branch if assigned to specific branches

- **Department Managers** can:
  - Approve department-level actions
  - View all department data
  - Reassign orders within department
  - Access department reports
  - View branch-level breakdowns within their department

- **Branch Managers** can:
  - View all orders for their branch
  - Manage branch inventory
  - View branch-specific KPIs and reports
  - Coordinate branch operations

- **Company Admins / Directors** can:
  - View all departments and branches
  - Switch between department and branch contexts
  - Configure department and branch settings
  - View consolidated P&L across departments and branches
  - Compare branch performance

### API & Frontend Integration

- **Frontend API Client** automatically injects `departmentId` into query parameters
- Uses active department from **DepartmentContext**
- Backend endpoints filter by `departmentId` when provided
- Users can override department filter when needed (with proper permissions)
- **Branch filtering** is optional and can be applied alongside department filtering
- Branch selection can be stored in user preferences or context

### Key Business Rules

1. **No Cross-Department Data Leakage**
   - Users in one department cannot see another department's data unless explicitly granted
   - Department boundaries are strictly enforced

2. **Department Workflow Ownership**
   - Orders belong to one department at a time
   - Workflow rules determine department transitions
   - Manual reassignment requires appropriate permissions

3. **Material Department Scoping**
   - Materials are allocated to departments
   - Material movements track department transfers
   - Warehouse manages cross-department material flows

4. **Cost Center Allocation**
   - Each department can have multiple cost centers
   - P&L tracks revenue and costs by department
   - Department-level profitability analysis

5. **Branch Scoping (Optional)**
   - Branches provide physical location context
   - Orders can optionally be assigned to branches
   - Inventory can be tracked by branch for multi-warehouse scenarios
   - Branch-level reporting enables location-based performance analysis
   - Branch and department filters can work together (e.g., "Operations department at Kelana Jaya branch")

6. **Hierarchy: Company → Departments & Branches**
   - Both departments and branches belong to the single company
   - Departments are functional units (Operations, Finance, etc.)
   - Branches are physical locations (HQ, Warehouse, Regional offices)
   - A user can belong to a department and work at a branch
   - Orders can have both `departmentId` and `branchId` for complete organizational tracking

---

## High-Level Story: The Daily Flow of CephasOps

Here is the entire business at a glance:

```
Email from TIME → Parser
         ↓
   ParsedOrder
         ↓
    Validation
         ↓
      Order
         ↓
Scheduler assigns SI
         ↓
SI performs job (App)
         ↓
Photos + Serials + Docket
         ↓
Inventory adjustments
         ↓
Invoice eligibility confirmed
         ↓
Billing generates invoice
         ↓
MyInvois e-Invoice submitted
         ↓
Payment recorded
         ↓
Payroll calculated
         ↓
P&L aggregated
         ↓
Director sees full picture
```

This story repeats every day across all companies.

---

## Detailed Stories

### 1. Story: TIME Activation Order → Completed → Paid

**Actors**
- Partner: TIME dotCom
- Subcon: CEPHAS SDN BHD / Cephas Trading & Services
- Admin / Scheduler (office)
- Service Installer (SI) on site
- Warehouse staff
- Finance (billing & payment tracking)

**Order Type:** Activation (FTTH / FTTO / FTTR / SDU / RDF POLE)  
**Input Source:** Email with Excel attachment from TIME portal

#### 1.1 Email Received & Parsed

1. TIME portal sends an **activation email** to Cephas' mailbox.
   - Subject & body follow TIME's standard format.
   - An **Excel attachment** contains the Breakdown Notification (activation details).

2. The **Email Parser Worker** (separate process) does:
   - Connects via POP3/IMAP.
   - Downloads the email.
   - Identifies it as an **Activation** (not Modification / Assurance).
   - Parses the Excel based on "Activation Mapping Rules" (configured in Settings → Parser Rules).

3. Parser calls backend:
   - `POST /internal/parsed-orders`
   - Payload includes:
     - ServiceId (e.g. `TBBNB062587G`)
     - Partner = TIME
     - Customer info
     - Service address
     - Appointment date & time (normalized from any format)
     - Package, bandwidth
     - Installer = CEPHAS TRADING & SERVICES (if provided)
     - CPE / Equipment details (router model, serial if available)
     - Raw file metadata (file path / storage ID)

#### 1.2 Order Creation & Default Materials

4. Backend validates payload and creates an **Order**:
   - Status = `Pending`
   - Type = `Activation`
   - Company = Cephas Trading (or configured default)
   - Links:
     - Building (matched from address / building short name)
     - Partner (TIME)

5. The Building has a **Building Type**:
   - Prelaid
   - Non-Prelaid
   - SDU
   - RDF POLE

6. **Default Materials Application (Activation Orders Only)**:
   
   For **Activation** order types only, the system auto-applies **default materials** based on Building Type (from Settings → Buildings → Default Materials):
   
   Examples:
   - **Prelaid**:
     - PatchCord 6m
     - PatchCord 10m
   - **Non-Prelaid**:
     - 80m 2-core fibre cable
     - 1 × UPC connector
     - 1 × APC connector
   - **SDU / RDF POLE**:
     - 80m RDF cable
     - 1 × UPC connector
     - 1 × APC connector
   
   **Note**: For other order types (Assurance, Modification, Value-Added Services, etc.), default materials are **not** auto-loaded. Users can manually add materials via the "+ Add" button when needed (e.g., customer lost device during modification, upgrades/downgrades, customer purchasing devices).

7. These become the **initial material plan** for the order:
   - Can be edited before completion.
   - Will later drive stock deduction.

---

### 2. Story: Admin & Scheduler Assign an SI from the Calendar

#### 2.1 Admin Reviews New Orders

1. Admin opens **Admin App → Orders → List**:
   - Sees all `Pending` orders for today/tomorrow.
   - Filters by partner (TIME), company, or building.

2. Admin checks:
   - Building details
   - Any blockers or requirements in remarks
   - That the appointment time is correct.

#### 2.2 Scheduler Uses Calendar View

3. Scheduler opens **Scheduler → Calendar**:
   - Mode: Day / Week.
   - Time slots show for each SI (in-house & subcon).

4. Each unscheduled order appears either:
   - In a side "Unassigned Orders" list, or
   - On the day, but without an SI assigned yet.

5. Order card shows:
   - Service ID & Work order ID (WO / AWO / TTKT if any)
   - Customer name
   - Time window
   - Building (short name)
   - Partner
   - Status color (Pending / Assigned / etc.)
   - Current SI (if assigned)

#### 2.3 Assignment Flow

6. Scheduler **drags the order card** onto the SI's row at the correct time or uses an "Assign" dialog.

7. Backend:
   - Validates SI availability:
     - No overlapping jobs.
     - Within working hours.
   - Sets status → `Assigned`
   - Creates an `OrderStatusLog` entry:
     - OldStatus: Pending
     - NewStatus: Assigned
     - Actor: Scheduler user
     - Timestamp.

**Business Rule:** Scheduler assigns jobs based on:
- SI availability
- SI skill
- Distance
- Job complexity
- Partner SLA

SI receives job instantly in their mobile app.

If SI is too busy:
→ Scheduler reassigns
→ Or SI decline (with reason)

---

### 3. Story: SI Performs the Job On-Site (Mobile PWA)

#### 3.1 SI Job List

1. SI logs into **SI App** (PWA):
   - Sees list of **Today's jobs**.
   - Each card shows:
     - ServiceId
     - Time
     - Building short name
     - Partner
     - Status pill (Assigned / On The Way / etc.)

2. SI taps on a job to open **Job Detail**:
   - Customer name & contact
   - Full address (with map link)
   - Appointment time
   - Partner
   - Building notes
   - Materials planned
   - Any special instructions.

#### 3.2 Status: On The Way

3. When SI starts traveling to site:
   - Taps **On The Way**.

4. SI app:
   - Captures GPS location, timestamp.
   - Sends to backend.

5. Backend:
   - Validates previous status (must be `Assigned`).
   - Updates order status → `OnTheWay`.
   - Logs timestamp for KPI.

#### 3.3 Status: Met Customer

6. On arrival & after meeting customer:
   - SI taps **Met Customer**.

7. SI app:
   - Again captures GPS + timestamp.

8. Backend:
   - Updates status → `MetCustomer`.
   - Logs KPI for on-time arrival.

#### 3.4 Completing Job & Materials

9. SI installs equipment / completes work.

10. In SI app:
    - Opens **Materials** section:
      - Marks which materials were actually used.
      - Adjusts cable length used (if needed; can log exact meters).
    - Uses **Camera**:
      - Takes photos of:
        - Installed router/ONU
        - Fibre tray / termination points
        - Wall outlets.
      - Optionally scans **router serial** (via OCR / barcode).

11. When done:
    - SI taps **Order Completed**.

12. Backend:
    - Updates status → `OrderCompleted`.
    - Logs completion timestamp.
    - Creates material movements:
      - `Warehouse → SI → Customer` for used materials.
    - Updates **Splitter port** if required (see separate story).

**Business Rule:** App works offline in low-signal buildings. Everything syncs when connection returns.

**If job cannot proceed:**
SI raises a Blocker:
- Customer not home
- Building access
- Material shortage
- Network issue
- Wrong appointment info

Scheduler reviews blocker and resolves.

---

### 4. Story: Splitter & Standby Port Management

#### 4.1 Building & Splitter Setup

1. Building is configured in **Settings → Buildings**:
   - Name, short name, address.
   - Type (Prelaid, Non-Prelaid, SDU, RDF POLE, etc.).
   - Splitter configuration:
     - 1:8 / 1:12 / 1:32
     - Splitter locations (MDF, floor, riser, etc.)
     - Ports and their states (available / used / reserved / standby).

2. Special rule:
   - For **1:32 splitters**, **port 32 is reserved as Standby** by default.

#### 4.2 Assigning Splitter Port on Completion

3. When Admin reviews a completed order:
   - Opens `Order → Materials & Splitter` tab.
   - Selects the splitter used:
     - Example: Splitter S01 – 1:32 – MDF Rack 01.
   - Selects port number used (e.g. port 05).

4. Backend:
   - Marks splitter port 05 as **used**.
   - Links that port to this order and ServiceId.

#### 4.3 Using Standby Port

5. If **port 32** is needed (standby):
   - Admin must first get **written approval from partner** (TIME / Celcom / etc.).
   - They attach:
     - Email screenshot/PDF or approval letter.

6. System:
   - Requires attached approval before allowing port 32 to be set as used.
   - Marks port 32 as `used (standby override)` and logs:
     - Who approved usage.
     - When.

7. Splitter capacity views:
   - Show used / free / standby usage.
   - Help planning future orders and expansions.

---

### 5. Story: Docket Review, Upload & Invoice Submission ID

#### 5.1 Dockets Received & Reviewed

1. After SI finishes:
   - Physical or digital **docket** is handed to Admin.

2. Admin opens the Order:
   - Checks that job is `OrderCompleted`.

3. Admin marks **Dockets Received**:
   - Records timestamp for **Docket KPI**:
     - Must be within **30 minutes** from completion (configurable KPI).

4. Admin uploads docket scan (PDF/photo):
   - Status → `DocketsUploaded`.

#### 5.2 Invoice Creation

5. Once docket is validated:
   - Admin marks order **Ready for Invoice**.

6. Finance (or Admin) opens **Billing → Create Invoice**:
   - System:
     - Uses **rate sheet** from Settings (per partner & order type).
     - Builds invoice lines from:
       - Standard activation rate
       - Additional charges (e.g., extra fiber length, booster, special work).

7. Invoice is generated:
   - Status = `Draft` → `ReadyForInvoice` → `Invoiced`.

#### 5.3 Portal Submission ID

8. Admin logs in to TIME portal and uploads:
   - Invoice PDF
   - Any required supporting documents (docket, photo, etc.).

9. TIME portal returns a **Submission ID**, e.g.:
   ```
   SUBM-20251122-987654
   ```

10. Admin returns to CephasOps:
    - Opens invoice.
    - Clicks Mark as Uploaded / Submitted.
    - Enters or pastes Submission ID.

11. Backend:
    - Stores portalSubmissionId.
    - Starts tracking:
      - Due date (e.g. +45 days from submission).
      - Aging (overdue, etc.)

#### 5.4 Rejection & Resubmission

If TIME rejects the invoice:
- Admin receives email or portal note.
- In CephasOps, sets invoice → Rejected with reason.
- Admin corrects:
  - Rate / description / doc issue.
  - Regenerates invoice PDF.
- Admin re-uploads to TIME portal:
  - Usually keeping the same Submission ID.
- Back in CephasOps:
  - Admin clicks Mark as Uploaded again.
  - System:
    - Keeps same portalSubmissionId.
    - Logs a new event in invoice history:
      - ReuploadedOn timestamp.
      - Notes.

---

### 6. Story: Assurance (Troubleshooting) from TTKT Email

**Type:** Assurance / Repair  
**Scenario:** Customer internet is down – fibre issue.

#### 6.1 Email & Parsing

TIME sends Assurance email to Cephas:

**Subject includes:**
```
APP M T - <CEPHAS TRADING & SERVICES><TBBNA261593G><Chow Yu Yang><TTKT202511138603863><AWO437884>
```

**Body includes:**
- Customer details
- Address
- ServiceId
- TTKT ID
- AWO ID
- Description: "Link Down, fibre issue"
- Equipment: HG8145V5 router, warranty status
- Appointment date and time
- 2 URLs:
  - Assign SI
  - Docket upload

**Email Parser:**
- Recognises pattern as Assurance type.
- Extracts:
  - ServiceId, TTKT, AWO
  - Customer & address
  - Appointment time
  - Equipment model
  - Warranty info
  - Assign & docket URLs.

**Parser calls:**
- `POST /internal/parsed-orders` with type = Assurance.

**Backend:**
- Creates order with:
  - Unique ID = ServiceId + TTKT + AWO.
  - Type = Assurance.
  - Status = Pending.
  - Additional fields:
    - Assign URL
    - Docket URL.

#### 6.2 Assurance Flow

- Admin & Scheduler see this as high priority in Orders & Scheduler.
- Scheduler assigns SI using internal scheduler, and/or uses TIME's Assign URL if required by process.
- SI visits site, diagnoses fibre issue:
  - May replace ONU/router.
  - If router replaced:
    - Old router returned to warehouse as Faulty.
    - RMA request created in system.
- If MRA PDF is received from TIME by email:
  - Email Parser can:
    - Download PDF.
    - Link it to the RMA record.
- Docket & billing:
  - If the job is billable:
    - Same docket → invoice → submission ID flow.
  - If under warranty / no-charge, still recorded for KPI & RMA.

---

### 7. Story: SI Performance Dashboard & Payroll

#### 7.1 SI Dashboard

SI opens SI App → Profile / Dashboard:

**Sees for current month:**
- Jobs completed
- On-time arrival percentage
- Docket submission KPI (within 30 minutes)
- Any penalties or bonus flags.

**This is driven by:**
- Order statuses & timestamps:
  - Assigned → OnTheWay → MetCustomer → OrderCompleted
  - Completed → DocketsReceived (time difference).

#### 7.2 Payroll Generation

In Admin app, Payroll module uses:

**SI rates from Settings:**
- In-house vs Subcon
- Per order type (Prelaid, Non-Prelaid, FTTR/FTTC/SDU/RDF POLE).
- KPI multipliers (bonus / penalty).

**Completed orders in given period.**

Admin runs Generate Payroll for a company & month:

**System calculates:**
- Base pay per job
- Adders or deductions for KPI performance
- Total per SI.

**Output:**
- Payroll summary per SI.
- Export (CSV/Excel) for accounting.

**Business Rule:** Payroll runs: Draft → Review → Finalised → Paid. After finalisation, nothing can be edited.

---

### 8. Story: Multi-Company & Multi-Vertical View (Director)

Director logs into Admin app.

**Has access to multiple companies:**
- Cephas Sdn. Bhd (ISP)
- Cephas Trading & Services (ISP)
- Kingsman Classic Services (Barbershop & Spa)
- Menorah Travel and Tours Sdn Bhd (Travel)

**Using company switcher, director can:**
- Change active company context.
- All data (orders, inventory, invoices, PNL) is filtered by companyId.

**A Group PNL screen allows:**
- Viewing combined high-level numbers across all companies.

**Vertical-specific toggles (from Settings):**
- For ISP: show Orders, Scheduler, Inventory, RMA, Billing, Assurance.
- For Kingsman (barbershop): later reuse orders/inventory modules for services.
- For Menorah (travel): later reuse billing/PNL modules for tours.

---

## Business Scenarios in Storybook Form

The system must support these scenarios:

- **Customer reschedules** → email approval → parser detects → scheduler updates
- **Building blocks installation** → SI flags → scheduler reassigns
- **Wrong information in email** → parser mismatch → admin corrects
- **Duplicate service ID** → system auto-detects
- **Faulty ONU** → SI replaces → RMA ticket created
- **TIME disputes invoice** → credit note issued
- **SI late** → KPI penalty applied
- **SI excellent** → performance bonus
- **Invoice unpaid for 60 days** → shown in Ageing
- **Director compares Cephas vs Cephas Trading P&L**
- **Kingsman uses same platform for POS & payroll**
- **Menorah generates travel invoices**

This storybook ensures all edge cases are understood.

---

## Multi-Company Story Logic

### Cephas Sdn. Bhd
- Full ISP operation
- Parser
- Scheduler
- Inventory
- Billing
- Payroll
- P&L

### Cephas Trading
- Inventory-heavy
- RMA
- Payroll (field assistants)
- P&L

### Kingsman
- Retail POS
- Staff commission
- Basic inventory

### Menorah Travel
- Travel bookings
- Multi-stage invoices

---

## Storybook → Requirements Mapping

| Story Element | Module |
|--------------|--------|
| Emails → Parser | Email Pipeline + Parser Module |
| ParsedOrder → Order | Orders Module |
| Scheduler assigns SI | Scheduler Module |
| SI updates status | SI App |
| Inventory movement | Inventory & RMA |
| Billing generates invoice | Billing Module |
| e-Invoice to LHDN | Tax & eInvoice Module |
| Payroll | Payroll Module |
| P&L | P&L Module |
| Director dashboards | Analytics |

This mapping ensures Cursor AI builds features with full business context.

---

## Style & Behaviour Rules

### 8.1 Do Not Over-Automate

Every automation must follow:
- Business rule
- Human review when needed

### 8.2 Clear Ownership

Each step belongs to:
- Admin
- Scheduler
- SI
- Warehouse
- Finance
- Director

### 8.3 Strict Approval Points

Reschedules, credit notes, RMA, payroll adjustments.

### 8.4 Multi-company boundaries

No cross-mixing of any operations.

---

## Additional Business Flow Details

### Inventory Adjusts Automatically

When SI finishes job:
- Serial numbers reduce from SI inventory
- Replacement serials auto-transfer
- Faulty ONU returned to warehouse
- RMA ticket created

Stock movements are automatic based on:
- SI bag → Customer
- SI bag → RMA
- Warehouse → SI

Cephas Trading handles bulk inventory.  
Cephas Sdn Bhd handles deployment inventory.

### e-Invoice Submitted to LHDN

System sends invoice to LHDN:
- JSON payload
- Invoice metadata
- Tax breakdown
- Supplier + customer info

Platform receives:
- QR Code
- Validation result
- UUID

Invoice becomes LOCKED. No changes allowed after submission.

### Partner Pays

When TIME or customer pays:
- Finance records payment
- Payment allocated to invoices
- Overpayments create credit balance
- SOA updated
- Ageing recalculated

### P&L Shows The Whole Picture

P&L aggregates:
- Revenue (Invoices)
- Material Cost (Inventory)
- SI Labour Cost (Payroll)
- Overheads (Cost Centres)

Directors view:
- Profit by partner
- Profit by order type
- Profit per SI
- Monthly + yearly summaries
- Across all companies (with consolidated view)

### Directors Lead The Business

Directors use CephasOps to:
- Identify bad partners
- Identify repeated RMA patterns
- Spot SI performance issues
- Find high-margin order types
- Reduce material waste
- Reduce blockers
- Improve installation speed
- Increase profitability

CephasOps is now the central decision platform.

---

## Related Documentation

For detailed technical specifications, see:

- **Workflow Engine**: `docs/01_system/WORKFLOW_ENGINE.md`
  - Complete status transition rules
  - Validation requirements
  - Automatic actions and triggers
  - Department workflow rules
  - Permission matrix

- **Order Lifecycle**: `docs/01_system/ORDER_LIFECYCLE.md`
  - All order statuses and definitions
  - Status transition diagrams
  - Lifecycle flows

- **System Overview**: `docs/01_system/SYSTEM_OVERVIEW.md`
  - High-level architecture
  - Module relationships
  - System design principles

---

## Storybook Maintenance

This STORYBOOK is a living document.

**When new flows or rules are added:**
- Add new sections instead of rewriting existing ones.

**Cursor & developers should always read this before:**
- Designing new endpoints
- Changing workflows
- Building new UI pages.

---

## Summary

This Storybook is the narrative backbone of CephasOps. It ensures all modules work together to reflect real-world business behaviour across four different companies.

It explains:
- How operations run
- How information flows
- What outcomes matter
- What business rules impact technology
- How events in one module affect another

This is the **"human version"** of the system architecture — essential for Cursor AI to generate correct logic.

---

**End of Storybook**
