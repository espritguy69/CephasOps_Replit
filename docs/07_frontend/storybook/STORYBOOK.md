\# CephasOps – Product Storybook



This STORYBOOK describes \*\*real-life usage flows\*\* of the CephasOps platform:



\- Admin / Operations coordinators

\- Schedulers

\- Warehouse \& Inventory staff

\- Finance / Billing team

\- Service Installers (SI) – in-house \& subcon

\- Directors for multiple companies:

&nbsp; - Cephas Sdn. Bhd (ISP)

&nbsp; - Cephas Trading \& Services (ISP)

&nbsp; - Kingsman Classic Services (Barbershop \& Spa)

&nbsp; - Menorah Travel and Tours Sdn Bhd (Travel)



It is meant to be read by:



\- Human developers

\- AI coding assistants (Cursor, etc.)



…so that everyone understands \*\*how the system should behave end-to-end\*\* before writing code.



---



\## 1. Story: TIME Activation Order → Completed → Paid



\*\*Actors\*\*



\- Partner: TIME dotCom

\- Subcon: CEPHAS SDN BHD / Cephas Trading \& Services

\- Admin / Scheduler (office)

\- Service Installer (SI) on site

\- Warehouse staff

\- Finance (billing \& payment tracking)



\*\*Order Type:\*\* Activation (FTTH / FTTO / FTTR / SDU / RDF POLE)  

\*\*Input Source:\*\* Email with Excel attachment from TIME portal



\### 1.1 Email Received \& Parsed



1\. TIME portal sends an \*\*activation email\*\* to Cephas’ mailbox.

&nbsp;  - Subject \& body follow TIME’s standard format.

&nbsp;  - An \*\*Excel attachment\*\* contains the Breakdown Notification (activation details).

2\. The \*\*Email Parser Worker\*\* (separate process) does:

&nbsp;  - Connects via POP3/IMAP.

&nbsp;  - Downloads the email.

&nbsp;  - Identifies it as an \*\*Activation\*\* (not Modification / Assurance).

&nbsp;  - Parses the Excel based on “Activation Mapping Rules” (configured in Settings → Parser Rules).

3\. Parser calls backend:

&nbsp;  - `POST /internal/parsed-orders`

&nbsp;  - Payload includes:

&nbsp;    - ServiceId (e.g. `TBBNB062587G`)

&nbsp;    - Partner = TIME

&nbsp;    - Customer info

&nbsp;    - Service address

&nbsp;    - Appointment date \& time (normalized from any format)

&nbsp;    - Package, bandwidth

&nbsp;    - Installer = CEPHAS TRADING \& SERVICES (if provided)

&nbsp;    - CPE / Equipment details (router model, serial if available)

&nbsp;    - Raw file metadata (file path / storage ID)



\### 1.2 Order Creation \& Default Materials



4\. Backend validates payload and creates an \*\*Order\*\*:

&nbsp;  - Status = `Pending`

&nbsp;  - Type = `Activation`

&nbsp;  - Company = Cephas Trading (or configured default)

&nbsp;  - Links:

&nbsp;    - Building (matched from address / building short name)

&nbsp;    - Partner (TIME)

5\. The Building has a \*\*Building Type\*\*:

&nbsp;  - Prelaid

&nbsp;  - Non-Prelaid

&nbsp;  - SDU

&nbsp;  - RDF POLE

6\. Based on Building Type, system auto-applies \*\*default materials\*\* (from Settings → Materials \& Building Types):



&nbsp;  Examples:

&nbsp;  - \*\*Prelaid\*\*:

&nbsp;    - PatchCord 6m

&nbsp;    - PatchCord 10m

&nbsp;  - \*\*Non-Prelaid\*\*:

&nbsp;    - 80m 2-core fibre cable

&nbsp;    - 1 × UPC connector

&nbsp;    - 1 × APC connector

&nbsp;  - \*\*SDU / RDF POLE\*\*:

&nbsp;    - 80m RDF cable

&nbsp;    - 1 × UPC connector

&nbsp;    - 1 × APC connector



7\. These become the \*\*initial material plan\*\* for the order:

&nbsp;  - Can be edited before completion.

&nbsp;  - Will later drive stock deduction.



---



\## 2. Story: Admin \& Scheduler Assign an SI from the Calendar



\### 2.1 Admin Reviews New Orders



1\. Admin opens \*\*Admin App → Orders → List\*\*:

&nbsp;  - Sees all `Pending` orders for today/tomorrow.

&nbsp;  - Filters by partner (TIME), company, or building.

2\. Admin checks:

&nbsp;  - Building details

&nbsp;  - Any blockers or requirements in remarks

&nbsp;  - That the appointment time is correct.



\### 2.2 Scheduler Uses Calendar View



3\. Scheduler opens \*\*Scheduler → Calendar\*\*:

&nbsp;  - Mode: Day / Week.

&nbsp;  - Time slots show for each SI (in-house \& subcon).

4\. Each unscheduled order appears either:

&nbsp;  - In a side “Unassigned Orders” list, or

&nbsp;  - On the day, but without an SI assigned yet.



5\. Order card shows:

&nbsp;  - Service ID \& Work order ID (WO / AWO / TTKT if any)

&nbsp;  - Customer name

&nbsp;  - Time window

&nbsp;  - Building (short name)

&nbsp;  - Partner

&nbsp;  - Status color (Pending / Assigned / etc.)

&nbsp;  - Current SI (if assigned)



\### 2.3 Assignment Flow



6\. Scheduler \*\*drags the order card\*\* onto the SI’s row at the correct time or uses an “Assign” dialog.

7\. Backend:

&nbsp;  - Validates SI availability:

&nbsp;    - No overlapping jobs.

&nbsp;    - Within working hours.

&nbsp;  - Sets status → `Assigned`

&nbsp;  - Creates an `OrderStatusLog` entry:

&nbsp;    - OldStatus: Pending

&nbsp;    - NewStatus: Assigned

&nbsp;    - Actor: Scheduler user

&nbsp;    - Timestamp.



---



\## 3. Story: SI Performs the Job On-Site (Mobile PWA)



\### 3.1 SI Job List



1\. SI logs into \*\*SI App\*\* (PWA):

&nbsp;  - Sees list of \*\*Today’s jobs\*\*.

&nbsp;  - Each card shows:

&nbsp;    - ServiceId

&nbsp;    - Time

&nbsp;    - Building short name

&nbsp;    - Partner

&nbsp;    - Status pill (Assigned / On The Way / etc.)

2\. SI taps on a job to open \*\*Job Detail\*\*:

&nbsp;  - Customer name \& contact

&nbsp;  - Full address (with map link)

&nbsp;  - Appointment time

&nbsp;  - Partner

&nbsp;  - Building notes

&nbsp;  - Materials planned

&nbsp;  - Any special instructions.



\### 3.2 Status: On The Way



3\. When SI starts traveling to site:

&nbsp;  - Taps \*\*On The Way\*\*.

4\. SI app:

&nbsp;  - Captures GPS location, timestamp.

&nbsp;  - Sends to backend.

5\. Backend:

&nbsp;  - Validates previous status (must be `Assigned`).

&nbsp;  - Updates order status → `OnTheWay`.

&nbsp;  - Logs timestamp for KPI.



\### 3.3 Status: Met Customer



6\. On arrival \& after meeting customer:

&nbsp;  - SI taps \*\*Met Customer\*\*.

7\. SI app:

&nbsp;  - Again captures GPS + timestamp.

8\. Backend:

&nbsp;  - Updates status → `MetCustomer`.

&nbsp;  - Logs KPI for on-time arrival.



\### 3.4 Completing Job \& Materials



9\. SI installs equipment / completes work.

10\. In SI app:

&nbsp;   - Opens \*\*Materials\*\* section:

&nbsp;     - Marks which materials were actually used.

&nbsp;     - Adjusts cable length used (if needed; can log exact meters).

&nbsp;   - Uses \*\*Camera\*\*:

&nbsp;     - Takes photos of:

&nbsp;       - Installed router/ONU

&nbsp;       - Fibre tray / termination points

&nbsp;       - Wall outlets.

&nbsp;     - Optionally scans \*\*router serial\*\* (via OCR / barcode).

11\. When done:

&nbsp;   - SI taps \*\*Order Completed\*\*.

12\. Backend:

&nbsp;   - Updates status → `OrderCompleted`.

&nbsp;   - Logs completion timestamp.

&nbsp;   - Creates material movements:

&nbsp;     - `Warehouse → SI → Customer` for used materials.

&nbsp;   - Updates \*\*Splitter port\*\* if required (see separate story).



---



\## 4. Story: Splitter \& Standby Port Management



\### 4.1 Building \& Splitter Setup



1\. Building is configured in \*\*Settings → Buildings\*\*:

&nbsp;  - Name, short name, address.

&nbsp;  - Type (Prelaid, Non-Prelaid, SDU, RDF POLE, etc.).

&nbsp;  - Splitter configuration:

&nbsp;    - 1:8 / 1:12 / 1:32

&nbsp;    - Splitter locations (MDF, floor, riser, etc.)

&nbsp;    - Ports and their states (available / used / reserved / standby).



2\. Special rule:

&nbsp;  - For \*\*1:32 splitters\*\*, \*\*port 32 is reserved as Standby\*\* by default.



\### 4.2 Assigning Splitter Port on Completion



3\. When Admin reviews a completed order:

&nbsp;  - Opens `Order → Materials \& Splitter` tab.

&nbsp;  - Selects the splitter used:

&nbsp;    - Example: Splitter S01 – 1:32 – MDF Rack 01.

&nbsp;  - Selects port number used (e.g. port 05).

4\. Backend:

&nbsp;  - Marks splitter port 05 as \*\*used\*\*.

&nbsp;  - Links that port to this order and ServiceId.



\### 4.3 Using Standby Port



5\. If \*\*port 32\*\* is needed (standby):

&nbsp;  - Admin must first get \*\*written approval from partner\*\* (TIME / Celcom / etc.).

&nbsp;  - They attach:

&nbsp;    - Email screenshot/PDF or approval letter.

6\. System:

&nbsp;  - Requires attached approval before allowing port 32 to be set as used.

&nbsp;  - Marks port 32 as `used (standby override)` and logs:

&nbsp;    - Who approved usage.

&nbsp;    - When.

7\. Splitter capacity views:

&nbsp;  - Show used / free / standby usage.

&nbsp;  - Help planning future orders and expansions.



---



\## 5. Story: Docket Review, Upload \& Invoice Submission ID



\### 5.1 Dockets Received \& Reviewed



1\. After SI finishes:

&nbsp;  - Physical or digital \*\*docket\*\* is handed to Admin.

2\. Admin opens the Order:

&nbsp;  - Checks that job is `OrderCompleted`.

3\. Admin marks \*\*Dockets Received\*\*:

&nbsp;  - Records timestamp for \*\*Docket KPI\*\*:

&nbsp;    - Must be within \*\*30 minutes\*\* from completion (configurable KPI).

4\. Admin uploads docket scan (PDF/photo):

&nbsp;  - Status → `DocketsUploaded`.



\### 5.2 Invoice Creation



5\. Once docket is validated:

&nbsp;  - Admin marks order \*\*Ready for Invoice\*\*.

6\. Finance (or Admin) opens \*\*Billing → Create Invoice\*\*:

&nbsp;  - System:

&nbsp;    - Uses \*\*rate sheet\*\* from Settings (per partner \& order type).

&nbsp;    - Builds invoice lines from:

&nbsp;      - Standard activation rate

&nbsp;      - Additional charges (e.g., extra fiber length, booster, special work).

7\. Invoice is generated:

&nbsp;  - Status = `Draft` → `ReadyForInvoice` → `Invoiced`.



\### 5.3 Portal Submission ID



8\. Admin logs in to TIME portal and uploads:

&nbsp;  - Invoice PDF

&nbsp;  - Any required supporting documents (docket, photo, etc.).

9\. TIME portal returns a \*\*Submission ID\*\*, e.g.:



&nbsp;  ```text

&nbsp;  SUBM-20251122-987654



10\. Admin returns to CephasOps:



Opens invoice.



Clicks Mark as Uploaded / Submitted.



Enters or pastes Submission ID.



Backend:



Stores portalSubmissionId.



Starts tracking:



Due date (e.g. +45 days from submission).



Aging (overdue, etc.)



5.4 Rejection \& Resubmission

If TIME rejects the invoice:



Admin receives email or portal note.



In CephasOps, sets invoice → Rejected with reason.



Admin corrects:



Rate / description / doc issue.



Regenerates invoice PDF.



Admin re-uploads to TIME portal:



Usually keeping the same Submission ID.



Back in CephasOps:



Admin clicks Mark as Uploaded again.



System:



Keeps same portalSubmissionId.



Logs a new event in invoice history:



ReuploadedOn timestamp.



Notes.



6\. Story: Assurance (Troubleshooting) from TTKT Email

Type: Assurance / Repair

Scenario: Customer internet is down – fibre issue.



6.1 Email \& Parsing

TIME sends Assurance email to Cephas:



Subject includes:



APP M T - <CEPHAS TRADING \& SERVICES><TBBNA261593G><Chow Yu Yang><TTKT202511138603863><AWO437884>



Body includes:



Customer details



Address



ServiceId



TTKT ID



AWO ID



Description: “Link Down, fibre issue”



Equipment: HG8145V5 router, warranty status



Appointment date and time



2 URLs:



Assign SI



Docket upload



Email Parser:



Recognises pattern as Assurance type.



Extracts:



ServiceId, TTKT, AWO



Customer \& address



Appointment time



Equipment model



Warranty info



Assign \& docket URLs.



Parser calls:



POST /internal/parsed-orders with type = Assurance.



Backend:



Creates order with:



Unique ID = ServiceId + TTKT + AWO.



Type = Assurance.



Status = Pending.



Additional fields:



Assign URL



Docket URL.



6.2 Assurance Flow

Admin \& Scheduler see this as high priority in Orders \& Scheduler.



Scheduler assigns SI using internal scheduler, and/or uses TIME’s Assign URL if required by process.



SI visits site, diagnoses fibre issue:



May replace ONU/router.



If router replaced:



Old router returned to warehouse as Faulty.



RMA request created in system.



If MRA PDF is received from TIME by email:



Email Parser can:



Download PDF.



Link it to the RMA record.



Docket \& billing:



If the job is billable:



Same docket → invoice → submission ID flow.



If under warranty / no-charge, still recorded for KPI \& RMA.



7\. Story: SI Performance Dashboard \& Payroll

7.1 SI Dashboard

SI opens SI App → Profile / Dashboard:



Sees for current month:



Jobs completed



On-time arrival percentage



Docket submission KPI (within 30 minutes)



Any penalties or bonus flags.



This is driven by:



Order statuses \& timestamps:



Assigned → OnTheWay → MetCustomer → OrderCompleted



Completed → DocketsReceived (time difference).



7.2 Payroll Generation

In Admin app, Payroll module uses:



SI rates from Settings:



In-house vs Subcon



Per order type (Prelaid, Non-Prelaid, FTTR/FTTC/SDU/RDF POLE).



KPI multipliers (bonus / penalty).



Completed orders in given period.



Admin runs Generate Payroll for a company \& month:



System calculates:



Base pay per job



Adders or deductions for KPI performance



Total per SI.



Output:



Payroll summary per SI.



Export (CSV/Excel) for accounting.



8\. Story: Multi-Company \& Multi-Vertical View (Director)

Director logs into Admin app.



Has access to multiple companies:



Cephas Sdn. Bhd (ISP)



Cephas Trading \& Services (ISP)



Kingsman Classic Services (Barbershop \& Spa)



Menorah Travel and Tours Sdn Bhd (Travel)



Using company switcher, director can:



Change active company context.



All data (orders, inventory, invoices, PNL) is filtered by companyId.



A Group PNL screen allows:



Viewing combined high-level numbers across all companies.



Vertical-specific toggles (from Settings):



For ISP: show Orders, Scheduler, Inventory, RMA, Billing, Assurance.



For Kingsman (barbershop): later reuse orders/inventory modules for services.



For Menorah (travel): later reuse billing/PNL modules for tours.



9\. Storybook Maintenance

This STORYBOOK is a living document.



When new flows or rules are added:



Add new sections instead of rewriting existing ones.



Cursor \& developers should always read this before:



Designing new endpoints



Changing workflows



Building new UI pages.





---



\### `docs/storybook/PAGES.md`



```markdown

\# CephasOps – Pages \& Screens Specification



This document lists the \*\*main UI pages / routes\*\* for:



\- Admin Web App (`frontend/app`)

\- Service Installer PWA (`frontend/si-app`)



It is intentionally \*\*high-level but concrete\*\* so that frontend routing and components can be aligned with the backend APIs and workflows.



---



\## 1. Admin Web App (`frontend/app`)



\### 1.1 Auth \& Layout



\*\*Routes\*\*



\- `/login`

&nbsp; - Email + password fields

&nbsp; - If user has access to multiple companies:

&nbsp;   - Company selector dropdown.

&nbsp; - Error display area (invalid credentials, locked, etc.)

\- `/logout` (action/endpoint)

\- Global layout (once authenticated):

&nbsp; - Top bar:

&nbsp;   - Company switcher

&nbsp;   - User menu (profile, logout)

&nbsp; - Sidebar navigation:

&nbsp;   - Dashboard

&nbsp;   - Orders

&nbsp;   - Scheduler

&nbsp;   - Inventory

&nbsp;   - Invoices

&nbsp;   - PNL

&nbsp;   - Settings

&nbsp;   - Buildings (ISP vertical only, optional)



\### 1.2 Dashboard



\*\*Route:\*\* `/dashboard`



\*\*Purpose:\*\* Quick overview of operations and finance.



\*\*Widgets:\*\*



\- Cards:

&nbsp; - Total Orders Today (by status)

&nbsp; - Dockets KPI (DocketsReceived within 30 minutes)

&nbsp; - Open Assurance tickets

&nbsp; - Upcoming Appointments (today + tomorrow)

\- Charts:

&nbsp; - Orders by partner (TIME / Celcom / Digi / U-Mobile)

&nbsp; - Monthly revenue trend

&nbsp; - Splitter capacity alerts (optional)

\- Shortcuts:

&nbsp; - “View today’s schedule”

&nbsp; - “View pending dockets”

&nbsp; - “View invoices nearing due date”



\### 1.3 Orders Module



\*\*Route:\*\* `/orders`



\#### 1.3.1 Orders List



\- Filters:

&nbsp; - Date range (appointment date)

&nbsp; - Status (Pending, Assigned, OnTheWay, MetCustomer, etc.)

&nbsp; - Partner

&nbsp; - Service Installer

&nbsp; - Building / Building Type

\- Table columns:

&nbsp; - Service ID

&nbsp; - Order Type (Activation / Modification / Assurance)

&nbsp; - TTKT / AWO (if Assurance)

&nbsp; - Customer Name

&nbsp; - Building short name

&nbsp; - Appointment date/time

&nbsp; - Partner

&nbsp; - Status (with color pill)

&nbsp; - SI Name

&nbsp; - Last updated



Click row → navigates to \*\*Order Detail\*\*.



\#### 1.3.2 Order Detail



\*\*Route:\*\* `/orders/:id`



Tabs:



1\. \*\*Overview\*\*

&nbsp;  - Service ID, order type

&nbsp;  - Partner

&nbsp;  - Company

&nbsp;  - Customer info (name, contact)

&nbsp;  - Address (with map link)

&nbsp;  - Appointment date \& time

&nbsp;  - Links to partner portal (TTKT/AWO URLs, docket URL etc., if Assurance).



2\. \*\*Status Timeline\*\*

&nbsp;  - Visual timeline (Pending → Assigned → OnTheWay → MetCustomer → …).

&nbsp;  - Each step:

&nbsp;    - Timestamp

&nbsp;    - Actor (User / SI / system)

&nbsp;    - Remarks

&nbsp;  - Controls:

&nbsp;    - Change status (dropdown) with appropriate form:

&nbsp;      - OnTheWay / MetCustomer / OrderCompleted timestamps come from SI or admin.

&nbsp;      - Blocker reasons (Customer, Building, Network) + remarks.

&nbsp;      - Reschedule: new date \& time.



3\. \*\*Materials \& Splitter\*\*

&nbsp;  - Building type \& default materials (preloaded).

&nbsp;  - Editable materials list:

&nbsp;    - Item, quantity, serial (if any).

&nbsp;  - Splitter selection:

&nbsp;    - Splitter ID, type (1:8 / 1:12 / 1:32).

&nbsp;    - Port used.

&nbsp;    - Standby port handling (port 32 for 1:32 with approval upload).



4\. \*\*Dockets \& Attachments\*\*

&nbsp;  - Docket status:

&nbsp;    - Not received / Received / Uploaded.

&nbsp;  - Upload section:

&nbsp;    - Docket PDF or image.

&nbsp;    - Additional photos.

&nbsp;  - Timestamps:

&nbsp;    - DocketsReceivedAt

&nbsp;    - DocketsUploadedAt



5\. \*\*Billing \& Invoice\*\*

&nbsp;  - Linked invoice(s):

&nbsp;    - Invoice number

&nbsp;    - Amount

&nbsp;    - Submission ID

&nbsp;    - Status (Draft, ReadyForInvoice, Invoiced, Paid, Rejected).

&nbsp;  - Create / view invoice actions.



6\. \*\*Notes \& Blockers\*\*

&nbsp;  - Free text notes.

&nbsp;  - Blocker history:

&nbsp;    - Customer / Building / Network

&nbsp;    - Open/close timestamps

&nbsp;    - Remarks.



---



\## 2. Scheduler Module



\*\*Route:\*\* `/scheduler`



\### 2.1 Calendar View



\- View modes:

&nbsp; - Day / Week

\- Left side:

&nbsp; - SI list (rows) with:

&nbsp;   - Name

&nbsp;   - Type (In-house / Subcon)

&nbsp;   - Skill tags, region.

\- Top:

&nbsp; - Date chooser, filter by:

&nbsp;   - Company

&nbsp;   - Partner

&nbsp;   - Region



\### 2.2 Order Cards



Each scheduled order appears as a \*\*card\*\* in the SI’s row at the time slot:



\- Service ID + WO/TTKT/AWO (tiny secondary text)

\- Customer name

\- Time (start–end)

\- Building short name

\- Partner

\- Status color:

&nbsp; - Pending

&nbsp; - Assigned

&nbsp; - OnTheWay

&nbsp; - MetCustomer

&nbsp; - OrderCompleted

&nbsp; - Rescheduled

\- Small SI avatar or initials (optional if multi-SI view)



\*\*Actions:\*\*



\- Drag \& drop:

&nbsp; - Reassign SI

&nbsp; - Reschedule time

\- Click:

&nbsp; - Opens small popover with quick actions:

&nbsp;   - Change Status (dropdown)

&nbsp;   - Open full order detail in new page

&nbsp;   - Add note



---



\## 3. Inventory \& RMA



\### 3.1 Inventory Home



\*\*Route:\*\* `/inventory`



Tabs:



1\. \*\*Stock\*\*

&nbsp;  - Filters:

&nbsp;    - Partner

&nbsp;    - Material type (Router, ONU, Cable, Connector, etc.)

&nbsp;    - Serial vs Non-serial

&nbsp;  - Table:

&nbsp;    - Material name

&nbsp;    - Partner

&nbsp;    - Serial? (yes/no)

&nbsp;    - On Hand (warehouse)

&nbsp;    - On Hand (in SIs’ possession)

&nbsp;    - Reserved

&nbsp;    - Faulty

&nbsp;    - Min/Max thresholds



2\. \*\*Movements\*\*

&nbsp;  - Chronological list:

&nbsp;    - Date

&nbsp;    - From (Warehouse / SI / Customer / RMA)

&nbsp;    - To (Warehouse / SI / Customer / RMA)

&nbsp;    - Material

&nbsp;    - Quantity

&nbsp;    - Order link (if related)

&nbsp;  - Filters by date, material, SI, order.



3\. \*\*RMA / MRA\*\*

&nbsp;  - List of RMA Requests:

&nbsp;    - ID

&nbsp;    - Partner

&nbsp;    - Material (serial)

&nbsp;    - Status (Requested / In Transit / Approved / Rejected / Completed)

&nbsp;    - Linked order

&nbsp;  - Detail view:

&nbsp;    - MRA PDF link

&nbsp;    - Shipping details

&nbsp;    - Notes.



---



\## 4. Billing \& Invoices



\### 4.1 Invoice List



\*\*Route:\*\* `/invoices`



\- Filters:

&nbsp; - Date range (invoice date or submission date)

&nbsp; - Partner

&nbsp; - Status (Draft, ReadyForInvoice, Invoiced, Paid, Rejected)

&nbsp; - Due status (Due soon, Overdue)

\- Columns:

&nbsp; - Invoice number

&nbsp; - Partner

&nbsp; - Company

&nbsp; - Amount

&nbsp; - Currency

&nbsp; - Submission ID (portal)

&nbsp; - Submission date

&nbsp; - Due date

&nbsp; - Status



\### 4.2 Invoice Detail



\*\*Route:\*\* `/invoices/:id`



Sections:



\- Header:

&nbsp; - Invoice number

&nbsp; - Company

&nbsp; - Partner

&nbsp; - Status

&nbsp; - Amount

\- Lines:

&nbsp; - Per order line:

&nbsp;   - Order ID / ServiceId

&nbsp;   - Description

&nbsp;   - Qty

&nbsp;   - Rate

&nbsp;   - Amount

\- Submission:

&nbsp; - PortalSubmissionId input field

&nbsp; - Submission date

\- Actions:

&nbsp; - Mark as Uploaded / Resubmitted

&nbsp; - Mark as Paid

&nbsp; - Mark as Rejected (with Reason)

\- Attachments:

&nbsp; - Invoice PDF

&nbsp; - Dockets

&nbsp; - Supporting docs.



---



\## 5. PNL \& Reports



\### 5.1 Company PNL



\*\*Route:\*\* `/pnl`



\- Filters:

&nbsp; - Period (month/year)

&nbsp; - Company (if user has multiple)

&nbsp; - Cost centre

\- Display:

&nbsp; - Revenue:

&nbsp;   - Invoiced

&nbsp;   - Paid

&nbsp; - Costs:

&nbsp;   - SI labour

&nbsp;   - Materials (net of RMA)

&nbsp;   - Overheads (configured)

&nbsp; - Profit:

&nbsp;   - Gross

&nbsp;   - Net

\- Visuals:

&nbsp; - Simple bar/line graphs by month.



\### 5.2 Group PNL (Director-only)



\*\*Route:\*\* `/pnl/group`



\- Shows aggregated PNL across:

&nbsp; - Cephas Sdn. Bhd

&nbsp; - Cephas Trading \& Services

&nbsp; - Kingsman

&nbsp; - Menorah

\- Allows:

&nbsp; - Per-company breakdown

&nbsp; - Combined totals.



---



\## 6. Settings



\*\*Route:\*\* `/settings`



Sections / subroutes (could be tabs):



1\. \*\*Workflow \& KPIs\*\*

&nbsp;  - Per partner / order type:

&nbsp;    - Time allowed for:

&nbsp;      - SI OnTheWay from assignment

&nbsp;      - MetCustomer from appointment

&nbsp;      - DocketsReceived from completion (e.g. 30 minutes)

&nbsp;  - Blocking rules \& allowed transitions.



2\. \*\*Parser Rules\*\*

&nbsp;  - Email rules:

&nbsp;    - From addresses

&nbsp;    - Subject patterns (Activation / Modification / Assurance / MRA).

&nbsp;  - Excel mappings:

&nbsp;    - Row/column mapping per template version.

&nbsp;  - RFB/VIP email patterns:

&nbsp;    - Highlight urgent or important jobs.



3\. \*\*Notifications \& Alerts\*\*

&nbsp;  - Channels:

&nbsp;    - Email

&nbsp;    - WhatsApp

&nbsp;    - SMS (future)

&nbsp;  - Which events trigger:

&nbsp;    - New order

&nbsp;    - Reschedule

&nbsp;    - Near-Miss or KPI violation.



4\. \*\*SI Rates \& Payroll\*\*

&nbsp;  - Rates per:

&nbsp;    - Order type (Prelaid, Non-Prelaid, FTTR/FTTC/SDU/RDF POLE)

&nbsp;    - Company

&nbsp;    - SI type (In-house / Subcon)

&nbsp;    - Seniority / KPI tier.



5\. \*\*Cost Centres \& Branches\*\*

&nbsp;  - Definitions for PNL allocation.



6\. \*\*Localization \& Vertical Settings\*\*

&nbsp;  - Language options (later).

&nbsp;  - Vertical toggles:

&nbsp;    - ISP features on/off.

&nbsp;    - Barbershop features (appointments, packages).

&nbsp;    - Travel features (tours, booking, etc.).



7\. \*\*External Links \& Portals\*\*

&nbsp;  - TTKT / AWO URL templates

&nbsp;  - Docket upload URLs

&nbsp;  - RMA / MRA portal URLs

&nbsp;  - For each partner.



---



\## 7. Service Installer PWA (`frontend/si-app`)



\### 7.1 Auth



\*\*Route:\*\* `/login`



\- SI login with:

&nbsp; - Username/email

&nbsp; - Password

\- Basic profile fetch after login.



\### 7.2 Jobs List



\*\*Route:\*\* `/jobs`



\- Filters / segments:

&nbsp; - Today

&nbsp; - Upcoming

&nbsp; - Completed (recent)

\- Each job card:

&nbsp; - ServiceId

&nbsp; - Time

&nbsp; - Partner logo / name

&nbsp; - Building short name

&nbsp; - Status pill (Assigned / OnTheWay / MetCustomer / Completed / Rescheduled).



\### 7.3 Job Detail



\*\*Route:\*\* `/jobs/:id`



Sections:



\- Header:

&nbsp; - ServiceId

&nbsp; - Partner

&nbsp; - Order type

\- Customer:

&nbsp; - Name

&nbsp; - Phone (tap-to-call)

&nbsp; - Address (tap-to-open map)

\- Appointment:

&nbsp; - Date/time

\- Status controls:

&nbsp; - Buttons:

&nbsp;   - On The Way

&nbsp;   - Met Customer

&nbsp;   - Order Completed

&nbsp; - Each triggers:

&nbsp;   - GPS + timestamp capture

&nbsp;   - API call to backend.



\- Materials:

&nbsp; - List of materials issued for this order.

&nbsp; - For serial items:

&nbsp;   - Show serial and status (Used / Unused).

&nbsp; - Option to mark extra materials used or return unused ones.



\- Photos:

&nbsp; - Capture completion photos using camera.

&nbsp; - List of already captured images.



\- Notes:

&nbsp; - SI remarks (optional).



\### 7.4 Materials In Hand



\*\*Route:\*\* `/materials`



\- List:

&nbsp; - Material name

&nbsp; - Serial (if applicable)

&nbsp; - Quantity

&nbsp; - Issued on which orders.

\- Actions:

&nbsp; - Mark as:

&nbsp;   - Used (if not already linked to order)

&nbsp;   - Faulty (trigger RMA flow)

&nbsp;   - Returned to warehouse.



\### 7.5 SI Profile \& KPI



\*\*Route:\*\* `/profile`



\- Basic SI info.

\- KPI summary:

&nbsp; - Jobs completed this month

&nbsp; - On-time arrival rate

&nbsp; - Docket submission KPI

\- Optional:

&nbsp; - Link to view payroll summary (read-only).



---



This PAGES document should stay in sync with:



\- `WIREFRAMES.md`

\- `API\_BLUEPRINT.md`

\- `STORYBOOK.md`



so that frontend and backend implementations always match the planned flows.




