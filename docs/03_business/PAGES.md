\# CephasOps – Pages \& Screens Specification (PAGES.md, v2)



This document defines the \*\*main UI pages / routes\*\* for:



\- \*\*Admin Web App\*\* (`frontend/app`)

\- \*\*Service Installer PWA\*\* (`frontend/si-app`)



It is aligned with:



\- `STORYBOOK.md` (flows \& behaviour)

\- `WIREFRAMES.md` (text wireframes / layout)



Use this as the \*\*source of truth\*\* for:



\- Route names

\- Page responsibilities

\- Key data \& actions



---



\## 1. Admin Web App (`frontend/app`)



\### 1.1 Auth \& Global Layout



\#### Routes



\- `/login`

\- `/logout` (action/endpoint, no UI)

\- Authenticated shell wrapping all main pages



\#### `/login`



\- Email + password fields

\- If the user belongs to multiple companies:

&nbsp; - Company selector dropdown:

&nbsp;   - Cephas Sdn. Bhd (ISP)

&nbsp;   - Cephas Trading \& Services (ISP)

&nbsp;   - Kingsman Classic Services (Barbershop \& Spa)

&nbsp;   - Menorah Travel and Tours Sdn Bhd (Travel)

\- Error display area (invalid credentials, locked account, etc.)



\#### Authenticated Layout



Once authenticated:



\- \*\*Top bar\*\*

&nbsp; - CephasOps logo

&nbsp; - Company switcher (active company context)

&nbsp; - User menu (profile, logout)



\- \*\*Sidebar navigation\*\*

&nbsp; - Dashboard → `/dashboard`

&nbsp; - Orders → `/orders`

&nbsp; - Scheduler → `/scheduler`

&nbsp; - Inventory → `/inventory`

&nbsp; - Invoices → `/invoices`

&nbsp; - PNL → `/pnl`

&nbsp; - Settings → `/settings`

&nbsp; - Buildings → `/buildings` (ISP vertical only; optional)



All pages below are rendered inside this shell.



---



\### 1.2 Dashboard



\*\*Route:\*\* `/dashboard`  

\*\*Purpose:\*\* Overview of operational \& financial health for the active company.



\*\*Key content:\*\*



\- KPI cards:

&nbsp; - Total Orders Today (by status)

&nbsp; - Dockets KPI (DocketsReceived within X minutes of completion)

&nbsp; - Open Assurance tickets

&nbsp; - Upcoming appointments (today + tomorrow)



\- Charts:

&nbsp; - Orders by partner (TIME / others)

&nbsp; - Monthly revenue trend (invoiced vs paid)



\- Alert lists:

&nbsp; - Pending dockets

&nbsp; - Invoices nearing or past due

&nbsp; - Optional: splitter capacity / building capacity alerts



---



\### 1.3 Orders Module



\#### 1.3.1 Orders List



\*\*Route:\*\* `/orders`  

\*\*Purpose:\*\* Operational hub to see and filter work orders.



\*\*Filters:\*\*



\- Appointment date range

\- Status (Pending, Assigned, OnTheWay, MetCustomer, OrderCompleted, DocketsReceived, Rescheduled, Cancelled)

\- Partner

\- Service Installer

\- Building / Building type (Prelaid, Non-Prelaid, SDU, RDF POLE)

\- Order Type (Activation / Modification / Assurance)



\*\*Table columns:\*\*



\- Service ID

\- Order Type

\- TTKT / AWO (for Assurance)

\- Customer name

\- Building short name

\- Appointment date/time

\- Partner

\- Status

\- SI name

\- Last updated



\*\*Primary actions:\*\*



\- `View` → `/orders/:id`

\- `Open in Scheduler` → focuses the order on `/scheduler`

\- `+ Manual Order` → opens manual order creation flow (modal or separate page)



---



\#### 1.3.2 Order Detail



\*\*Route:\*\* `/orders/:id`  

\*\*Purpose:\*\* Full context \& operations on a single order.



\*\*Header:\*\*



\- ServiceId + Order Type (e.g. `TBBNB062587G – Activation`)

\- Partner, Company

\- Status pill

\- Actions:

&nbsp; - Change Status

&nbsp; - Open in Scheduler

&nbsp; - Optional: Duplicate, Cancel



\*\*Tabs:\*\*



1\. \*\*Overview\*\*

&nbsp;  - Customer info (name, contact)

&nbsp;  - Address (with map link)

&nbsp;  - Appointment date/time

&nbsp;  - Partner \& Company

&nbsp;  - For Assurance:

&nbsp;    - TTKT, AWO, description (e.g. “Link Down, fibre issue”)

&nbsp;    - Warranty info

&nbsp;    - Portal URLs (Assign, Docket, etc.)



2\. \*\*Status Timeline\*\*

&nbsp;  - Timeline: Pending → Assigned → OnTheWay → MetCustomer → OrderCompleted → DocketsReceived → DocketsUploaded → (Invoiced/ Paid)

&nbsp;  - Each entry: timestamp, actor (user/SI/system), remarks

&nbsp;  - Controls to:

&nbsp;    - Reschedule

&nbsp;    - Mark blocker statuses

&nbsp;    - Manually adjust status (according to workflow rules)



3\. \*\*Materials \& Splitter\*\*

&nbsp;  - Building info:

&nbsp;    - Building type (Prelaid, Non-Prelaid, SDU, RDF POLE)

&nbsp;  - Editable materials list:

&nbsp;    - Default materials (from building type rules)

&nbsp;    - Actual used materials

&nbsp;  - Splitter management (ISP):

&nbsp;    - Select splitter (e.g. 1:32 at MDF)

&nbsp;    - Select used port

&nbsp;    - Special handling for standby port (e.g. port 32):

&nbsp;      - Must attach partner approval before marking as used



4\. \*\*Dockets \& Attachments\*\*

&nbsp;  - Docket status:

&nbsp;    - Not received / Received / Uploaded

&nbsp;  - Timestamps:

&nbsp;    - DocketsReceivedAt

&nbsp;    - DocketsUploadedAt

&nbsp;    - KPI check against OrderCompletedAt

&nbsp;  - Upload area:

&nbsp;    - Docket scans (PDF/images)

&nbsp;    - SI site photos

&nbsp;    - Other supporting files

&nbsp;  - List of all attachments (including original TIME Excel if stored)



5\. \*\*Billing \& Invoice\*\*

&nbsp;  - Linked invoices:

&nbsp;    - Invoice number

&nbsp;    - Amount

&nbsp;    - Status (Draft, ReadyForInvoice, Invoiced, Paid, Rejected)

&nbsp;    - Portal Submission ID

&nbsp;    - Submission date

&nbsp;  - Actions:

&nbsp;    - Create invoice (if none linked)

&nbsp;    - Open invoice detail (`/invoices/:id`)



6\. \*\*Notes \& Blockers\*\*

&nbsp;  - Free-text notes (timeline)

&nbsp;  - Blocker history:

&nbsp;    - Type (Customer / Building / Network)

&nbsp;    - Opened/closed timestamps

&nbsp;    - Remarks

&nbsp;  - Controls to open/close blockers



---



\## 2. Scheduler Module



\*\*Route:\*\* `/scheduler`  

\*\*Purpose:\*\* Calendar-based planning of orders to SIs.



\*\*Features:\*\*



\- View modes:

&nbsp; - Day / Week

\- Filters:

&nbsp; - Company (or use global company context)

&nbsp; - Partner

&nbsp; - Region

&nbsp; - SI type (In-house / Subcon)



\*\*Core UI:\*\*



\- Rows: Service Installers

\- Columns: Time slots

\- Each order appears as a card with:

&nbsp; - Service ID / WO / TTKT / AWO

&nbsp; - Customer name

&nbsp; - Appointment time

&nbsp; - Building short name

&nbsp; - Partner

&nbsp; - Status color

&nbsp; - SI details (for multi-SI / multi-company scenarios)



\*\*Interactions:\*\*



\- Drag \& drop:

&nbsp; - Moving card between SIs = reassign SI

&nbsp; - Moving card in time = reschedule appointment

\- Click card:

&nbsp; - Popover:

&nbsp;   - Quick status change

&nbsp;   - Open order detail

&nbsp;   - Add note



---



\## 3. Inventory \& RMA



\*\*Route:\*\* `/inventory`  

\*\*Purpose:\*\* Track stock, movements, and RMA/MRA for materials.



\### 3.1 Stock View



\- Filters:

&nbsp; - Partner

&nbsp; - Category (Router, ONU, Cable, Connector, etc.)

&nbsp; - Serialised vs non-serial

&nbsp; - Location (Warehouse, SIs, RMA, Customer)



\- Table:

&nbsp; - Material name

&nbsp; - Category

&nbsp; - Partner

&nbsp; - Serialised? (yes/no)

&nbsp; - On hand (warehouse)

&nbsp; - On hand (SIs)

&nbsp; - Reserved

&nbsp; - Faulty

&nbsp; - Min / Max thresholds



\### 3.2 Movements View



\- Chronological list of `MaterialMovement` records.

\- Filters:

&nbsp; - Date range

&nbsp; - From / To

&nbsp; - Material

&nbsp; - SI

&nbsp; - Order



\- Columns:

&nbsp; - Date

&nbsp; - From (Warehouse / SI / Customer / RMA)

&nbsp; - To (Warehouse / SI / Customer / RMA)

&nbsp; - Material

&nbsp; - Quantity

&nbsp; - Linked order (with link)



\### 3.3 RMA / MRA View



\- List of RMA requests:

&nbsp; - RMA ID

&nbsp; - Partner

&nbsp; - Material + serial

&nbsp; - Status (Requested, In Transit, Approved, Rejected, Completed)

&nbsp; - Linked order

&nbsp; - MRA PDF link (if available)



\- Detail view:

&nbsp; - Full RMA timeline

&nbsp; - Attached documents (MRA PDF, etc.)



---



\## 4. Billing \& Invoices



\### 4.1 Invoice List



\*\*Route:\*\* `/invoices`  

\*\*Purpose:\*\* Track billing status, submission IDs, and payments.



\*\*Filters:\*\*



\- Date range (invoice date or submission date)

\- Partner

\- Status:

&nbsp; - Draft

&nbsp; - ReadyForInvoice

&nbsp; - Invoiced

&nbsp; - Paid

&nbsp; - Rejected

\- Due status (Due soon, Overdue)

\- Company (if not using global context)



\*\*Columns:\*\*



\- Invoice number

\- Partner

\- Company

\- Amount

\- Currency

\- Portal Submission ID

\- Submission date

\- Due date

\- Status



\*\*Actions:\*\*



\- View → `/invoices/:id`

\- Mark as Uploaded / Submitted

\- Mark as Paid

\- Mark as Rejected (with reason)



---



\### 4.2 Invoice Detail



\*\*Route:\*\* `/invoices/:id`  

\*\*Purpose:\*\* Manage one invoice end-to-end.



\*\*Sections:\*\*



1\. \*\*Header\*\*

&nbsp;  - Invoice number

&nbsp;  - Company

&nbsp;  - Partner

&nbsp;  - Status

&nbsp;  - Total amount



2\. \*\*Lines\*\*

&nbsp;  - Per order:

&nbsp;    - Order ID / ServiceId (link to `/orders/:id`)

&nbsp;    - Description

&nbsp;    - Quantity

&nbsp;    - Rate

&nbsp;    - Amount



3\. \*\*Submission\*\*

&nbsp;  - PortalSubmissionId (input)

&nbsp;  - Submission date

&nbsp;  - Actions:

&nbsp;    - Mark as Uploaded / Resubmitted

&nbsp;    - Mark as Paid

&nbsp;    - Mark as Rejected (with reason)



4\. \*\*Attachments\*\*

&nbsp;  - Invoice PDF

&nbsp;  - Docket PDFs

&nbsp;  - Supporting documents



5\. \*\*History\*\*

&nbsp;  - Creation

&nbsp;  - Status changes

&nbsp;  - Rejection notes

&nbsp;  - Payment confirmation



---



\## 5. PNL \& Reports



\### 5.1 Company PNL



\*\*Route:\*\* `/pnl`  

\*\*Purpose:\*\* Profit \& loss per company and period.



\*\*Filters:\*\*



\- Period (month/year)

\- Company (where applicable)

\- Cost centre



\*\*Display:\*\*



\- Revenue:

&nbsp; - Invoiced

&nbsp; - Paid

\- Costs:

&nbsp; - SI labour

&nbsp; - Materials (net of RMA)

&nbsp; - Overheads

\- Profit:

&nbsp; - Gross

&nbsp; - Net



\*\*Visuals:\*\*



\- Bar/line chart of month-by-month PNL

\- Table breakdown by partner or cost centre



---



\### 5.2 Group PNL (Director-only)



\*\*Route:\*\* `/pnl/group`  

\*\*Visible to:\*\* Directors with access to multiple companies.



\*\*Features:\*\*



\- Aggregate view of:

&nbsp; - Cephas Sdn. Bhd

&nbsp; - Cephas Trading \& Services

&nbsp; - Kingsman Classic Services

&nbsp; - Menorah Travel \& Tours



\- Per-company breakdown:

&nbsp; - Revenue, Costs, Profit



\- Combined totals across all companies.



---



\## 6. Settings



\*\*Route:\*\* `/settings`  

\*\*Purpose:\*\* Configure workflows, parsers, notifications, rates, and vertical behaviour.



\*\*Subsections (via tabs or nested routes):\*\*



1\. Workflow \& KPIs

2\. Parser Rules

3\. Notifications \& Alerts

4\. SI Rates \& Payroll

5\. Cost Centres \& Branches

6\. Localization \& Vertical Settings

7\. External Links \& Portals



---



\### 6.1 Workflow \& KPIs



Configure per partner + order type:



\- Time windows for:

&nbsp; - SI OnTheWay after assignment

&nbsp; - MetCustomer vs appointment time

&nbsp; - DocketsReceived vs OrderCompleted

\- Allowed status transitions

\- Rules for blockers (when they can be opened/closed).



---



\### 6.2 Parser Rules



Configure how the Email Parser Worker classifies \& extracts:



\- Email patterns:

&nbsp; - From addresses

&nbsp; - Subject regex/patterns for:

&nbsp;   - Activation

&nbsp;   - Modification

&nbsp;   - Assurance

&nbsp;   - VIP/RFB

&nbsp;   - MRA / RMA

\- Excel mappings:

&nbsp; - Column mapping for ServiceId, customer name, appointment date/time, address, equipment

&nbsp; - Template versioning support



---



\### 6.3 Notifications \& Alerts



\- Communication channels:

&nbsp; - Email

&nbsp; - WhatsApp

&nbsp; - SMS (future)

\- Events:

&nbsp; - New order

&nbsp; - Reschedule

&nbsp; - KPI breach (late OnTheWay, late docket, etc.)

\- Target configuration:

&nbsp; - Who receives which notifications.



---



\### 6.4 SI Rates \& Payroll



\- Tables of SI rates:

&nbsp; - Order type (Prelaid, Non-Prelaid, FTTR/FTTC/SDU/RDF POLE)

&nbsp; - Company

&nbsp; - SI type (In-house / Subcon)

&nbsp; - Base rate per job

&nbsp; - KPI multipliers (bonus/penalty tiers)



Used later by Payroll generation \& SI performance dashboards.



---



\### 6.5 Cost Centres \& Branches



\- Define cost centres:

&nbsp; - Code, name, company

\- Map branches / regions to cost centres to allocate PNL.



---



\### 6.6 Localization \& Vertical Settings



\- Localization:

&nbsp; - Language (future)

&nbsp; - Date/time formats (if needed)

\- Vertical toggles (per company):

&nbsp; - ISP features on/off (Orders, Scheduler, Assurance, Splitter)

&nbsp; - Barbershop features (appointments, packages)

&nbsp; - Travel features (tours, bookings, etc.)



These toggles control which modules \& pages appear in the UI.



---



\### 6.7 External Links \& Portals



Configure partner portal URLs:



\- TTKT / AWO templates

\- Docket upload URLs

\- RMA / MRA portal links



Used for quick navigation from Orders \& RMA screens.



---



\## 7. Service Installer PWA (`frontend/si-app`)



\### 7.1 SI Auth



\*\*Route:\*\* `/login`  

\*\*Purpose:\*\* SI logs in to see assigned jobs.



\- Username/email + password

\- Redirect to `/jobs` (Today tab)



---



\### 7.2 Jobs List



\*\*Route:\*\* `/jobs`  

\*\*Purpose:\*\* Central job list for SI.



\*\*Segments:\*\*



\- Today

\- Upcoming

\- Completed



Each job card shows:



\- ServiceId

\- Order type (Activation / Assurance)

\- Partner

\- Customer name

\- Building short name

\- Appointment time

\- Status (Assigned / OnTheWay / MetCustomer / Completed / Rescheduled)

\- Action: `Open Job`



---



\### 7.3 Job Detail



\*\*Route:\*\* `/jobs/:id`  

\*\*Purpose:\*\* Everything SI needs on-site to execute the job.



\*\*Sections:\*\*



\- Header:

&nbsp; - ServiceId – OrderType

&nbsp; - Partner

&nbsp; - Status badge



\- Customer:

&nbsp; - Name

&nbsp; - Phone (tap to call)

&nbsp; - Address (tap to open map)



\- Appointment:

&nbsp; - Date \& time



\- Status buttons:

&nbsp; - On The Way

&nbsp; - Met Customer

&nbsp; - Order Completed

\- Each status update:

&nbsp; - Captures GPS + timestamp

&nbsp; - Sends to backend (KPI tracking)



\- Materials:

&nbsp; - List of materials assigned to this job

&nbsp; - Mark used/not used

&nbsp; - Add extra materials (from in-hand stock)



\- Photos:

&nbsp; - Capture site photos

&nbsp; - Scan serial numbers (camera + OCR/barcode)

&nbsp; - Show thumbnails of captured images



\- Notes:

&nbsp; - Free-text remarks by SI



\- Completion:

&nbsp; - Confirm completion (Order Completed) → triggers final status update.



---



\### 7.4 Materials In Hand



\*\*Route:\*\* `/materials`  

\*\*Purpose:\*\* SI sees all materials issued to them and can update their status.



\*\*Data:\*\*



\- Material name

\- Serial number (if applicable)

\- Quantity

\- Issued date

\- Related orders (if linked)



\*\*Actions:\*\*



\- Mark as Used

\- Mark as Faulty (start RMA)

\- Return to Warehouse



---



\### 7.5 SI Profile \& KPI



\*\*Route:\*\* `/profile`  

\*\*Purpose:\*\* SI self-view of performance.



\*\*Content:\*\*



\- Basic info:

&nbsp; - Name

&nbsp; - SI type (In-house / Subcon)

&nbsp; - Company



\- KPI summary (current month):

&nbsp; - Jobs completed

&nbsp; - On-time arrival percentage

&nbsp; - Docket submission KPI performance

&nbsp; - Optional bonus/penalty flags



\- Optional:

&nbsp; - Read-only payroll summary (job count, base pay total)



---



This `PAGES.md` is now aligned with:



\- `STORYBOOK.md` (flows \& behaviour)

\- `WIREFRAMES.md` (layout \& interactions)



Any new feature should update \*\*all three\*\* to stay consistent.



