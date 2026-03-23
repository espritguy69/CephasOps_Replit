# WIREFRAMES.md  
CephasOps – UI Wireframes (Textual, v1 Aligned with STORYBOOK & PAGES)

This file describes the **main screens in text wireframe form**, aligned with:

- `STORYBOOK.md`
- `docs/storybook/PAGES.md`

Use this as a guide for:

- React routing & components
- Figma layout & UX behaviour

There are **two main frontends**:

- **Admin Web App** – `frontend/app`
- **Service Installer PWA (SI App)** – `frontend/si-app`

---

## 1. Admin Web App – Global Layout & Auth

### 1.1 Login

**Route:** `/login`

**Layout:**

- Centered card:
  - Title: `CephasOps – Login`
  - Fields:
    - `Email`
    - `Password`
  - If user has access to multiple companies:
    - `Company` dropdown:
      - Cephas Sdn. Bhd (ISP)
      - Cephas Trading & Services (ISP)
      - Kingsman Classic Services (Barbershop & Spa)
      - Menorah Travel and Tours Sdn Bhd (Travel)
  - Button: `Login`
  - Error area (below button):
    - Shows validation and auth errors:
      - “Invalid credentials”
      - “Account locked”
      - Etc.

### 1.2 Authenticated Shell / Layout

Once logged in, all main routes share a common layout:

- **Top bar:**
  - Left:
    - CephasOps logo
  - Center:
    - **Company switcher** (dropdown):
      - Shows current active company
      - Switching company reloads data context:
        - Orders, inventory, invoices, PNL, etc.
  - Right:
    - User menu:
      - `Profile`
      - `Logout`

- **Sidebar navigation:**
  - `Dashboard` → `/dashboard`
  - `Orders` → `/orders`
  - `Scheduler` → `/scheduler`
  - `Inventory` → `/inventory`
  - `Invoices` → `/invoices`
  - `PNL` → `/pnl`
  - `Settings` → `/settings`
  - (Optionally) `Buildings` (ISP vertical only) → `/buildings`

- **Content area:**
  - The active route content renders here.

---

## 2. Dashboard

**Route:** `/dashboard`

**Purpose:**  
Quick overview of **operations + finance**, tuned by selected company (and, for directors, group-level via `/pnl/group`).

### 2.1 Layout

- **Top row – metrics cards:**
  - `Total Orders Today`  
    - Breakdown badges for `Pending / Assigned / OnTheWay / Completed`
  - `Dockets KPI`  
    - % of jobs where `DocketsReceivedAt` is within configured KPI (e.g. 30 minutes) from `OrderCompletedAt`
  - `Open Assurance Tickets`
  - `Upcoming Appointments` (today + tomorrow)

- **Middle – charts:**
  - `Orders by Partner` (TIME / others)
  - `Monthly Revenue Trend` (invoiced vs paid)

- **Bottom – alerts / lists:**
  - `Pending Dockets` list:
    - Orders completed but dockets not yet marked as received
  - `Invoices Nearing Due Date` list:
    - Shows submission date, due date, partner, amount
  - Optional:
    - `Splitter Capacity Alerts` (e.g. buildings near splitter port capacity)

---

## 3. Orders Module

**Route:** `/orders`  
**Detail route:** `/orders/:id`

### 3.1 Orders List Page

**Layout:**

- **Top:**
  - Page title: `Orders`
  - Filters (row of controls):
    - `Date range` – Appointment date
    - `Status` (multi-select pills):
      - Pending
      - Assigned
      - OnTheWay
      - MetCustomer
      - OrderCompleted
      - DocketsReceived
      - Rescheduled
      - Cancelled
    - `Partner` dropdown
    - `Service Installer` dropdown
    - `Building` search (typeahead)
    - `Order Type` dropdown:
      - Activation
      - Modification
      - Assurance
    - Optional: `Building Type` (Prelaid, Non-Prelaid, SDU, RDF POLE)
  - Right-aligned button:
    - `+ Manual Order` (for manually created work orders)

- **Main – table of orders:**
  - Columns:
    - `Service ID`
    - `Order Type` (Activation / Modification / Assurance)
    - `TTKT / AWO` (if Assurance)
    - `Customer Name`
    - `Building (short)`
    - `Appointment Date/Time`
    - `Partner`
    - `Status` (colored pill)
    - `SI Name`
    - `Last Updated`
    - `Actions`:
      - `View`
      - `Open in Scheduler`

- **Order Row Example (text):**

  `[TBBNB062587G]  Activation  |  COBNB SDN. BHD.  | ROYCE RESIDENCE | TIME | 14 Nov 2025 10:00 | [Pending] | (Unassigned) | [View] [Open in Scheduler]`

- Clicking `View`:
  - Navigates to: `/orders/:id`

*(Optional: you can still show a mini right-side drawer for quick preview, but `/orders/:id` is the primary detailed route.)*

---

### 3.2 Order Detail Page

**Route:** `/orders/:id`

Header area:

- Left:
  - `ServiceId – OrderType`  
  - Example: `TBBNB062587G – Activation`
- Subheader:
  - Partner (TIME)
  - Company (Cephas Trading & Services)
  - Status pill
- Actions (top-right):
  - `Change Status` (dropdown with allowed transitions)
  - `Open in Scheduler`
  - `More` (…) menu for:
    - `Duplicate`
    - `Cancel Order`
    - etc., if needed.

**Tabs:**

1. ### Summary

   Shows key details:

   - **Customer:**
     - Name
     - Phone (click-to-call)
     - Email (optional)
   - **Address:**
     - Full address
     - `Open in Maps` link
   - **Appointment:**
     - Date & time
   - **Partner & Company:**
     - Partner: TIME
     - Company: (from company switcher context)
   - **Assurance-only (if type = Assurance):**
     - TTKT ID
     - AWO ID
     - Description (e.g., “Link down, fibre issue”)
     - Warranty info
     - `Assign URL`, `Docket URL` (clickable links to partner portal)

2. ### Status Timeline

   Vertical or horizontal timeline showing:

   - Stages:
     - Pending → Assigned → OnTheWay → MetCustomer → OrderCompleted → DocketsReceived → DocketsUploaded → (optional) Billed/Invoiced
   - Each event row shows:
     - `Status name`
     - Timestamp
     - Actor (user / SI / system)
     - Remarks (optional)

   **Controls:**

   - On the right/top:
     - `Change Status` form:
       - For status transitions that the admin is allowed to trigger:
         - e.g. Reschedule (date/time picker)
         - Mark as Cancelled (with reason)
         - Mark as Blocked (Customer / Building / Network + remarks)

3. ### Materials & Splitter

   - **Building info:**
     - Building type (Prelaid / Non-Prelaid / SDU / RDF POLE)
     - Building short name
   - **Materials list:**
     - Table columns:
       - Material name
       - Category
       - Planned Qty (from default materials rules)
       - Actual Used Qty
       - Serial (for serialized items)
   - **Splitter section (for ISP):**
     - Dropdown: `Splitter`:
       - Example: `S01 – 1:32 – MDF Rack 01`
     - Once splitter selected:
       - Grid/list of `ports` with states:
         - Available
         - Used
         - Reserved
         - Standby (e.g. port 32)
     - UI to select:
       - `Port used`: dropdown or clickable port grid
     - Standby rule:
       - If user selects standby port (e.g. port 32 on 1:32):
         - Show warning:
           - “Standby port requires partner approval.”
         - Require file upload:
           - `Approval letter / email screenshot`
         - After upload, allow saving.

4. ### Dockets & Attachments

   - **Docket status:**
     - `Not Received` / `Received` / `Uploaded`
   - Fields:
     - `DocketsReceivedAt` timestamp (auto when marked received)
     - `DocketsUploadedAt` timestamp
     - KPI hint:
       - If `DocketsReceivedAt – OrderCompletedAt` > configured minutes:
         - Show KPI breach indicator (e.g., red text).
   - Upload area:
     - `Upload Docket` (PDF / image)
     - `Upload Additional Site Photos`
   - List of attachments:
     - Original Excel from TIME (optional)
     - Site photos (from SI)
     - Docket scans

5. ### Billing & Invoice

   - Shows linked invoice(s) created for this order:
     - `Invoice No`
     - `Partner`
     - `Company`
     - `Amount`
     - `Status` (Draft, ReadyForInvoice, Invoiced, Paid, Rejected)
     - `Portal Submission ID`
     - `Submission Date`
   - Actions:
     - If no invoice:
       - `Create Invoice` button → navigates to invoice creation or opens modal.
     - If invoice exists:
       - `Open Invoice` → `/invoices/:id`

6. ### Notes & Blockers

   - **Notes:**
     - Free-text notes list (timeline-style)
     - `Add note` text area + button
   - **Blockers section:**
     - List:
       - Blocker type (Customer / Building / Network)
       - Opened at
       - Closed at
       - Remarks
     - Button:
       - `Add Blocker` → form with type + remarks
     - For open blockers:
       - `Close Blocker` action.

---

## 4. Scheduler Module

**Route:** `/scheduler`

### 4.1 Calendar View

**Layout:**

- **Top bar:**
  - `Date` switcher:
    - Today / previous / next
  - `View mode`:
    - Day / Week
  - Filters:
    - `Company` (optional – may piggyback on top bar)
    - `Partner`
    - `Region`
    - `SI type` (In-house / Subcon)

- **Left side (optional panel):**
  - `Service Installer (SI) list`:
    - Each row:
      - SI Name
      - Type (In-house / Subcon)
      - Skills / region tags
      - Status indicator: Active / Off duty

- **Main panel:**
  - Calendar grid:
    - Rows: SIs
    - Columns: time slots within the selected day/week
  - Each order appears as a **card**:

    **Order Card fields:**
    - `Service ID / WO / TTKT / AWO` (smaller)
    - `Customer name`
    - `Time` (start–end)
    - `Building short`
    - `Partner`
    - `Status` color (background or left strip)
    - SI initials/avatar (optional if in multi-SI view)

### 4.2 Interactions

- **Drag & Drop:**
  - Drag card from one SI row to another:
    - Reassigns SI
  - Drag card horizontally:
    - Reschedules appointment time
- **Click on card:**
  - Opens a small popover:
    - Status dropdown (quick update)
    - `Open Order Detail` (navigates to `/orders/:id`)
    - `Add note`

---

## 5. Inventory & RMA

**Route:** `/inventory`

### 5.1 Inventory Home

**Layout:**

- **Top filters:**
  - `Partner`
  - `Material category` (Router, ONU, Fibre Cable, Connector, etc.)
  - `Serialised / Non-serial` toggle
  - `Location` (Warehouse / SI / RMA / Customer)

- **Tabs:**
  1. `Stock`
  2. `Movements`
  3. `RMA / MRA`

---

### 5.2 Stock Tab

**Stock table columns:**

- `Material name`
- `Category`
- `Partner`
- `Serialised?` (Yes / No)
- `On Hand (Warehouse)`
- `On Hand (SIs)`
- `Reserved`
- `Faulty`
- `Min Threshold`
- `Max Threshold`

Optional actions:

- `Adjust Stock` (for admin users)
- `Issue to SI` / `Return from SI`

---

### 5.3 Movements Tab

List of `MaterialMovement` records:

- Filters:
  - Date range
  - From / To location
  - Material
  - SI
  - Order

- Columns:
  - `Date`
  - `From` (Warehouse / SI / Customer / RMA)
  - `To` (Warehouse / SI / Customer / RMA)
  - `Material`
  - `Quantity`
  - `Related Order` (link to `/orders/:id`)

---

### 5.4 RMA / MRA Tab

- Table:
  - `RMA ID`
  - `Partner`
  - `Material`
  - `Serial Number`
  - `Status`:
    - Requested
    - In Transit
    - Approved
    - Rejected
    - Completed
  - `Linked Order`
  - `MRA PDF` (link if available)

- Detail view (modal or separate route):
  - Shows history of RMA:
    - When requested
    - When MRA received
    - Shipping details
  - Attachments:
    - MRA PDF from partner (e.g., TIME)

---

## 6. Invoices & Billing

**Route (list):** `/invoices`  
**Route (detail):** `/invoices/:id`

### 6.1 Invoice List

**Layout:**

- **Top filters:**
  - `Date range` (invoice or submission date)
  - `Partner`
  - `Status`:
    - Draft
    - ReadyForInvoice
    - Invoiced
    - Paid
    - Rejected
  - `Due Status`:
    - Due soon
    - Overdue
  - `Company` (optional if not using top-level company switcher)

- **Table columns:**
  - `Invoice number`
  - `Partner`
  - `Company`
  - `Amount`
  - `Currency`
  - `Submission ID` (portal)
  - `Submission date`
  - `Due date`
  - `Status`
  - `Actions`:
    - `View`
    - `Mark as Uploaded`
    - `Mark as Paid`
    - `Mark as Rejected`

---

### 6.2 Invoice Detail

**Route:** `/invoices/:id`

**Layout:**

- **Header:**
  - `Invoice number`
  - `Company`
  - `Partner`
  - `Status`
  - `Total Amount`

- **Sections:**

1. **Invoice Lines:**
   - Table:
     - `Order ID / ServiceId` (link to `/orders/:id`)
     - `Description`
     - `Quantity`
     - `Rate`
     - `Amount`

2. **Submission Info:**
   - Fields:
     - `PortalSubmissionId` (text input)
     - `Submission date` (date picker)
   - Actions:
     - `Mark as Uploaded`
       - Or `Resubmitted` if already uploaded once.
     - `Mark as Paid`
     - `Mark as Rejected` (with reason input)

3. **Attachments:**
   - `Invoice PDF` (download link)
   - `Docket PDFs` (linked from orders)
   - `Supporting docs` (RMA, photos, approvals)

4. **Timeline / History (optional):**
   - Events like:
     - Created
     - Marked ReadyForInvoice
     - Submitted (with Submission ID)
     - Rejected (with reason)
     - Resubmitted
     - Marked Paid

---

## 7. PNL & Reports

### 7.1 Company PNL

**Route:** `/pnl`

**Layout:**

- **Filters:**
  - `Period`:
    - Month / Year
  - `Company`:
    - If user has multiple; otherwise defaults from company switcher
  - `Cost centre`

- **Display:**

  - **Revenue:**
    - `Invoiced`
    - `Paid`
  - **Costs:**
    - `SI Labour`
    - `Materials` (net of RMA/refunds)
    - `Overheads` (as configured)
  - **Profit:**
    - `Gross`
    - `Net`

- **Visuals:**
  - Bar / line chart:
    - Revenue vs cost per month
  - Table view for:
    - Per-partner or per-cost-centre breakdown

---

### 7.2 Group PNL (Director View)

**Route:** `/pnl/group`  
(Visible to director-level roles.)

**Layout:**

- **Filters:**
  - `Period` (month/year)
  - Option to include/exclude companies

- **Cards / table:**
  - For each company:
    - `Revenue`
    - `Costs`
    - `Profit` (Gross & Net)
  - Combined totals row or card.

---

## 8. Settings

**Route:** `/settings`

Left navigation (sections):

1. `Workflow & KPIs`
2. `Parser Rules`
3. `Notifications & Alerts`
4. `SI Rates & Payroll`
5. `Cost Centres & Branches`
6. `Localization & Vertical Settings`
7. `External Links & Portals`

---

### 8.1 Workflow & KPIs

Form-like UI:

- Per `Partner` & `OrderType`:
  - `Time allowed for SI OnTheWay from assignment`
  - `Time allowed for MetCustomer from appointment`
  - `Time allowed for DocketsReceived from completion` (e.g. 30 minutes)
- Blocking rules:
  - Allowed status transitions
  - When blockers can be opened/closed.

---

### 8.2 Parser Rules

For activation, modification, assurance, MRA emails:

- **Email rules:**
  - Table:
    - `From address`
    - `Subject pattern`
    - `Type` (Activation / Assurance / MRA / VIP / RFB)
- **Excel mappings:**
  - For each template version:
    - Map:
      - `ServiceId` column
      - `Customer Name` column
      - `Appointment date/time` format
      - `Address` fields
      - `Equipment` fields
- **VIP / RFB:**
  - Rules to highlight emails/orders as urgent.

---

### 8.3 Notifications & Alerts

- Toggle per channel:
  - Email
  - WhatsApp
  - SMS (future)
- Events:
  - New order
  - Reschedule
  - KPI violation (e.g., docket late)
- Configuration for:
  - Who receives which notifications.

---

### 8.4 SI Rates & Payroll

- Table of rates:
  - `OrderType` (Prelaid / Non-Prelaid / FTTR/FTTC/SDU/RDF POLE)
  - `Company`
  - `SI Type` (In-house / Subcon)
  - `Base rate per job`
  - `KPI multipliers` (bonus/penalty tiers)
- Used by Payroll generation logic (not fully wired in this wireframe, but consistent with STORYBOOK).

---

### 8.5 Cost Centres & Branches

- List of cost centres:
  - Name
  - Code
  - Company
- Option to map branches to cost centres.

---

### 8.6 Localization & Vertical Settings

- `Language` options (future)
- `Vertical toggles`:
  - Checkboxes for:
    - ISP features (Orders, Scheduler, Splitter, Assurance)
    - Barbershop features (appointments, packages – for Kingsman)
    - Travel features (tours, bookings – for Menorah)
- UI determines which modules show up in sidebar based on vertical.

---

### 8.7 External Links & Portals

Configuration for:

- TTKT / AWO URL templates
- Docket upload URLs
- RMA / MRA portal URLs
- Per partner (TIME, others)

---

## 9. Service Installer PWA (SI App)

**Base:** `frontend/si-app`

### 9.1 SI Login

**Route:** `/login`

- Simple full-screen form:
  - `Username / Email`
  - `Password`
  - `Login` button
- After login → redirect to `/jobs` (default to Today tab).

---

### 9.2 Jobs List

**Route:** `/jobs`

**Layout:**

- **Header:**
  - `Hi, {SI Name}`
  - Current date:
    - e.g. `Today – 14 Nov 2025`
- **Tabs / Segments:**
  - `Today`
  - `Upcoming`
  - `Completed`

- **List of job cards:**

Card fields:

- `ServiceId`
- `Order type` (Activation / Assurance)
- `Customer name`
- `Building short`
- `Appointment time`
- `Partner`
- `Status badge`:
  - Assigned / OnTheWay / MetCustomer / Completed / Rescheduled
- Action button: `Open Job`

---

### 9.3 Job Detail

**Route:** `/jobs/:id`

**Layout sections:**

- **Header:**
  - `ServiceId – OrderType`
  - Partner logo/name
  - Status badge

- **Customer:**
  - Name
  - Phone (tap-to-call)
  - Address (tap-to-open map)

- **Appointment:**
  - Date & time

- **Status controls:**
  - Large buttons:
    - `On The Way`
    - `Met Customer`
    - `Order Completed`
  - Each button:
    - Captures:
      - GPS location
      - Timestamp
    - Sends to backend to update order status & KPI.

- **GPS Indicator:**
  - Shows:
    - “Location captured at 10:02 AM” or
    - “Tap to capture location”

- **Materials:**
  - List of materials for this order:
    - `Material name`
    - `Quantity`
    - `Serial` (if applicable)
  - Controls:
    - Mark items as `Used` or `Not used`
    - Add extra materials used (from SI inventory)

- **Photos:**
  - Buttons:
    - `Take Site Photo` (opens camera)
    - `Scan Serial Number` (camera with OCR/barcode)
  - Below: thumbnails of already captured images.

- **Notes:**
  - Simple text area:
    - `Add Remarks`
  - List of previously added remarks.

- **Complete Job:**
  - When `Order Completed` is tapped:
    - Confirm dialog:
      - “Are you sure this job is completed?”
    - After confirm:
      - Sends completion event with timestamp to backend.

---

### 9.4 Materials In Hand

**Route:** `/materials`

**Purpose:**  
SI sees all materials currently issued to them.

**Layout:**

- Filters:
  - `Material category`
  - `Serialised / Non-serial`
- Table / list:
  - `Material name`
  - `Serial` (if any)
  - `Quantity`
  - `Issued on` (date)
  - `Related Order` (if linked)
- Actions (per item/row):
  - `Mark as Used` (if not already linked to an order)
  - `Mark as Faulty` (start RMA flow)
  - `Return to Warehouse`

---

### 9.5 SI Profile & KPI

**Route:** `/profile`

**Layout:**

- **Basic info:**
  - Name
  - SI type (In-house / Subcon)
  - Company

- **KPI Summary (for current month):**
  - `Jobs completed`
  - `On-time arrival rate` (based on OnTheWay/MetCustomer timestamps vs appointments)
  - `Docket submission KPI`:
    - Percentage of jobs where `DocketsReceivedAt` is within KPI window
  - Optional:
    - `Penalty / Bonus flags` (icons or badges)

- Optional section:
  - `Payroll summary` (read-only):
    - Number of billable jobs
    - Total base pay (no need to show full finance details)

---

These wireframes are now **fully aligned with**:

- The flows described in `STORYBOOK.md`
- The pages & routes defined in `docs/storybook/PAGES.md`

Any further changes to flows or routes should be kept in sync across:

- `STORYBOOK.md`
- `PAGES.md`
- `WIREFRAMES.md`
