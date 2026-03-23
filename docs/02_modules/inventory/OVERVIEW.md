
# INVENTORY_AND_RMA_MODULE.md
CephasOps Inventory, Stock Movements & RMA Architecture – Full Version

---

## 1. Purpose

The Inventory & RMA Module manages **all materials and devices** used in CephasOps operations:

- Stock received from TIME and partners.
- Stock stored in Cephas / Cephas Trading warehouses.
- Stock issued to Service Installers (SIs).
- Stock installed at customer premises.
- Stock returned (unused / faulty).
- RMA / MRA workflows back to partners.

It must support:

- **Serialised items** (e.g. routers, ONUs, ONTs, WiFi mesh devices).
- **Non-serialised items** (e.g. fibre cable, patch cords, connectors).
- **Multiple companies** (Cephas Sdn. Bhd, Cephas Trading, Kingsman, Menorah).
- Integration with:
  - Orders Module (materials per order).
  - Scheduler (availability check).
  - Billing & Finance (materials cost).
  - PNL & Payroll (cost allocation per job / SI).
  - Email Parser (MRA emails with PDFs).

> Documentation only – this file describes behaviour, entities, and API contracts but no implementation code.

### Current implementation (single-company)

- **Ledger as source of truth:** All quantity changes go through `StockLedgerEntry`; no direct `StockBalance.Quantity` writes. Balances are derived from ledger (and `LedgerBalanceCache`).
- **GET `/api/inventory/stock`:** Returns ledger-derived balances (material/location, on-hand). Used by dashboard, inventory list, bins.
- **Ledger APIs:** Receive, Transfer, Allocate, Issue, Return; GET ledger, GET stock-summary. Department-scoped via `ResolveDepartmentScopeAsync` / `ResolveLedgerContextAsync`.
- **Reports:** GET `reports/usage-summary`, `reports/serial-lifecycle`, `reports/stock-by-location-history` (JSON + export); department RBAC. Material collection check (SI inventory) uses ledger-derived balances.
- **Legacy:** `POST /api/inventory/stock/movements` is deprecated; prefer ledger endpoints.

---

## 2. Core Responsibilities

### 2.1 Material Master

Maintain a **central catalogue** of items:

- Item code
- Description
- Category (Router, ONU, Cable, Patchcord, Connector, Splitter, etc.)
- Serial tracking flag (Yes/No)
- Unit of measure
- Default cost (for P&L)
- Linked partner/device type (TIME, Celcom, etc.)
- Vertical flags (ISP / Barbershop / Travel)

### 2.2 Stock Locations

Support multiple locations:

- Main warehouse(s)
- Sub-warehouses or regional stores
- SI “in-hand” stock
- RMA holding area (faulty stock)
- Customer site (installed devices)

### 2.3 Stock Movements

Track movements as transactions:

1. Partner → Warehouse (GRN: goods received note).
2. Warehouse → SI (Issue / Assignment).
3. SI → Customer (Installed / Used).
4. SI → Warehouse (Return unused).
5. Customer → SI → Warehouse (Faulty / defective).
6. Warehouse → Partner RMA (MRA shipment).

Each movement must:

- Be tied to:
  - Company
  - Partner (if applicable)
  - Order (if applicable)
  - SI (if applicable)
- Update quantities and, for serialised items, **ownership/status**.

---

## 3. Material Types & Rules

### 3.1 Serialised vs Non-Serialised

**Serialised**:
- One device = one unique serial number.
- E.g. routers, ONUs, modems, mesh routers.
- Tracked per-piece from partner to customer or RMA.

**Non-serialised**:
- Quantitative tracking only (m, pcs, etc.).
- E.g. 80m fibre cable, connectors, patch cords.

### 3.2 Default Materials per Building Type

From Settings & Buildings:

- Building Type:
  - PRELAID
  - NON-PRELAID
  - SDU
  - RDF POLE
  - Others (future)
- Default materials:
  - **Prelaid**:
    - PatchCord 6m
    - PatchCord 10m
  - **Non-Prelaid**:
    - 80m 2-core cable
    - 1 × UPC connector
    - 1 × APC connector
  - **SDU**:
    - 80m RDF cable
    - 1 × UPC connector
    - 1 × APC connector

When an order is created and building type is known:

- System pre-populates **planned materials** from this default list.
- Planner/SI can adjust based on actual on-site need (within rules).

---

## 4. Inventory States

Each item can exist in one of several states:

For serialised items:

1. `InWarehouse`
2. `WithSI`
3. `InstalledAtCustomer`
4. `FaultyInWarehouse`
5. `RMARequested`
6. `InTransitToPartner`
7. `RMAClosed` (repaired/replaced/credit note)
8. `Scrapped`

For non-serialised items:

- We primarily track **location + quantity** by:
  - Warehouse
  - SI
  - Order usage

---

## 5. Processes

### 5.1 Goods Receipt from Partner

Trigger:
- Partner sends shipment + document (could be Excel/PDF).
- Warehouse receives physical stock.

Steps:

1. Create a **GRN** record:
   - Partner
   - Delivery note number
   - Date
   - Receiving warehouse
   - Items and quantities
2. For serialised items:
   - Capture each serial (manual entry, scanner, or import).
3. Update stock levels:
   - Warehouse stock increased.
4. Optional:
   - Attach partner documentation (PDF, Excel, emails).
5. Link to:
   - Partner contract or rate card (Billing & P&L context).

### 5.2 Issue Stock to SI

Trigger:
- SI prepares for jobs.
- Admin/Warehouse issues materials.

Steps:

1. Create **Issue To SI** transaction:
   - From: Warehouse
   - To: SI
   - Items and quantities (+ serials for serialised items).
2. Update stock states:
   - Warehouse stock decreased.
   - SI “in-hand” stock increased.
3. Link optionally to:
   - Planned orders or generic standby stock.

### 5.3 Installation / Job Completion

During/after job:

- SI marks which materials were:
  - **Used at customer**:
    - Moves from SI stock → CustomerInstalled stock.
  - **Unused**:
    - Optionally returned to warehouse later.

The SI app should allow:

- Scan serials (camera) for devices installed.
- Confirm used quantities of cable/consumables.

Backend updates:

- For serialised:
  - `WithSI` → `InstalledAtCustomer`
  - Link to `OrderId`, `ServiceId`, `Address`.
- For non-serialised:
  - Reduce SI quantity; record usage per order.

### 5.4 Unused Material Return

If job not completed or materials not used:

Scenarios:

1. **Warehouse → SI → Customer job NOT completed – must return.**
2. **Assurance replacement**:
   - New device installed.
   - Old device must be brought back.

Process:

1. SI returns material to warehouse via **Return From SI** transaction.
2. For serialised:
   - `WithSI` → `InWarehouse` (if OK)
   - Or `WithSI` → `FaultyInWarehouse` (if faulty).
3. For non-serialised:
   - SI stock decreased.
   - Warehouse stock increased.

This enforces discipline – SI cannot keep stock floating without trace.

---

## 6. RMA / MRA Workflow

### 6.1 RMA Initiation

Trigger:
- Faulty device detected (usually in Assurance jobs).
- Partner sends **MRA email** with a PDF document or RMA instructions.

Steps:

1. **Email Parser** detects MRA/RMA email type.
2. Extracts:
   - Service ID / Ticket ID / AWO
   - Device serial(s)
   - RMA number (if present)
3. Downloads and saves PDF attachments:
   - Stores in file storage.
   - Links to `RMARequest`.

Admin/Warehouse:

4. Reviews RMA request, confirms faulty device serials.
5. Creates `RMARequest` record with:
   - Partner
   - Serial numbers
   - Reason for return
   - Linked order(s)
   - MRA PDF reference.

### 6.2 RMA Shipment to Partner

Steps:

1. Prepare shipment; print MRA PDF to send with devices.
2. Create **RMA Shipment** transaction:
   - From: `FaultyInWarehouse`
   - To: `InTransitToPartner`
3. Update RMA status:
   - `RMARequested` → `InTransitToPartner`.

### 6.3 RMA Closure

Partner either:

- Returns repaired devices.
- Sends replacement devices.
- Issues credit note.
- Declares warranty void and instructs scrap.

We model as:

- **RMAResult**:
  - Repaired
  - Replaced
  - Credited
  - Scrapped

Stock impact:

- For repaired/replaced:
  - `InTransitToPartner` → `InWarehouse` (new serial if replacement).
- For credited:
  - Financial module records credit note (affects P&L).
- For scrapped:
  - Stock removed, scrap loss for P&L.

---

## 7. Splitters & Standby Ports

Splitters are special inventory items linked to buildings.

### 7.1 Splitter Types

- 1:8
- 1:12
- 1:32

Each splitter has:

- Unique SplitterId
- BuildingId
- Physical location description (e.g. MDF OLT 1_0/11/8, FDF:029 CORE:041)
- Total ports
- Ports mapping:
  - Port number
  - Used / Reserved / Standby
  - Linked ServiceId where applicable

### 7.2 Standby Port Rule

For **1:32 splitters**:

- Port 32 is **reserved as standby**.
- Using port 32 **requires prior partner approval**.

Process:

1. Admin marks plan to use standby port.
2. Must attach **approval evidence**:
   - Email screenshot or letter (PDF, image).
3. System enforces:
   - Cannot mark port 32 as “Used” unless:
     - ApprovalAttachmentId present.
4. All usage of standby port should be easily reportable.

---

## 8. Data Model (Conceptual)

### 8.1 Material

- `Id`
- `CompanyId`
- `ItemCode`
- `Description`
- `Category`
- `IsSerialised`
- `UnitOfMeasure`
- `DefaultCost`
- `PartnerId` (optional)
- `VerticalFlags` (ISP, Barbershop, Travel)
- `IsActive`

### 8.2 StockLocation

- `Id`
- `CompanyId`
- `Name`
- `Type` (Warehouse, SI, RMA, CustomerSite)
- `LinkedServiceInstallerId` (if SI)
- `LinkedBuildingId` (if CustomerSite)
- `IsActive`

### 8.3 StockBalance

- `Id`
- `MaterialId`
- `StockLocationId`
- `Quantity`

> For serialised items, StockBalance may be quantity-based but **Serials** are tracked separately.

### 8.4 SerialisedItem

- `Id`
- `MaterialId`
- `SerialNumber`
- `CompanyId`
- `CurrentLocationId`
- `Status` (InWarehouse, WithSI, InstalledAtCustomer, FaultyInWarehouse, InTransitToPartner, RMAClosed, Scrapped)
- `LastOrderId` (if installed)
- `LastServiceId`
- `Notes`

### 8.5 StockMovement

- `Id`
- `CompanyId`
- `FromLocationId`
- `ToLocationId`
- `MaterialId`
- `Quantity`
- `MovementType` (GRN, IssueToSI, ReturnFromSI, InstallAtCustomer, ReturnFaulty, RMAOutbound, RMAInbound, Adjust)
- `OrderId` (optional)
- `ServiceInstallerId` (optional)
- `PartnerId` (optional)
- `Remarks`
- `CreatedAt`
- `CreatedByUserId`

For serialised items, a separate link table `StockMovementSerial` might reference individual serial numbers.

### 8.6 RMARequest

- `Id`
- `CompanyId`
- `PartnerId`
- `RmaNumber` (from partner, if given)
- `RequestDate`
- `Reason`
- `Status` (Requested, InTransit, Closed)
- `MraDocumentId` (file reference)
- Collection of:
  - `RMARequestItem`:
    - `SerialisedItemId`
    - `OriginalOrderId`
    - `Notes`
    - `Result` (Repaired, Replaced, Credited, Scrapped)

---

## 9. API Contracts (Docs Only)

**Base path:** `/api/inventory` and `/api/rma`

### 9.1 Inventory

1. **List Materials**
   - `GET /api/inventory/materials`
   - Query: `companyId`, optional filters.
   - Response: paginated list of Material DTOs.

2. **Get Stock by Location**
   - `GET /api/inventory/stock`
   - Query: `companyId`, `locationId`
   - Response: list of `{ materialId, quantity, materialCode, description }`.

3. **Get Serialized Items**
   - `GET /api/inventory/serials`
   - Query: `companyId`, optional `status`, `materialId`, `orderId`, `serviceId`.
   - Response: list of SerialisedItem DTOs.

4. **Create Stock Movement**
   - `POST /api/inventory/movements`
   - Body: StockMovementCreateRequest
   - Response: StockMovementDto.

5. **Get Stock Movements (Audit)**
   - `GET /api/inventory/movements`
   - Query: date range, company, movement type, order, SI, etc.
   - Response: paginated.

6. **Get Splitters & Ports (per Building)**
   - `GET /api/inventory/splitters`
   - Query: `buildingId`
   - Response: list including ports and usage.

7. **Update Splitter Port Usage**
   - `POST /api/inventory/splitters/{splitterId}/ports/{portNumber}/use`
   - Body:
     - `serviceId`
     - `orderId`
     - `approvalAttachmentId` (required for standby port 32)
   - Enforce standby rules.

---

### 9.2 RMA

1. **Create RMA Request**
   - `POST /api/rma/requests`
   - Body:
     - Partner
     - Serialised items
     - Reason
     - Linked orders / service IDs
   - Response: RMARequestDto

2. **Attach MRA Document**
   - `POST /api/rma/requests/{id}/attachments`
   - Body: reference to uploaded file (from file storage).

3. **Update RMA Status**
   - `PUT /api/rma/requests/{id}`
   - Body: status & results; apply stock changes accordingly.

4. **List RMA Requests**
   - `GET /api/rma/requests`
   - Query: `companyId`, `partnerId`, `status`, date ranges.
   - Response: Paginated list.

---

## 10. Integration With Other Modules

### 10.1 Orders Module

- Orders specify **planned materials** (from building type).
- On completion:
  - Actual materials used are confirmed and posted as movements.
- Inventory updates:
  - For serialised: mark which serial is at customer.
  - For non-serialised: reduce SI stock.

### 10.2 Scheduler Module

- Before assigning:
  - Scheduler may optionally check material availability.
- For Assurance:
  - Scheduler can see if replacement devices are available.

### 10.3 Billing & Finance

- Materials have `DefaultCost` and possibly partner cost.
- Material usage per order is sent to:
  - Billing for chargeable items.
  - P&L for cost of goods sold.

### 10.4 Email Parser

- MRA emails:
  - Parser identifies email type and extracts:
    - RMA number
    - Service ID
    - Serial(s)
  - Downloads MRA PDF and associates with RMARequest.
- Other partner stock-related emails can later be supported via Settings → Parser Rules.

---

## 11. Multi-Company Behaviour

- All Material, Stock, RMA, and Movement records are scoped by `companyId`.
- For ISP vertical:
  - Cephas Sdn. Bhd & Cephas Trading.
- For Kingsman (barbershop) & Menorah (travel):
  - Inventory model can be reused for products, consumables, or packages.

Directors may see cross-company inventory dashboards without mixing stocks.

---

## 12. Error Handling & Validation

Examples:

- Cannot move more quantity than available at `FromLocationId`.
- Cannot re-use a serial already marked as `InstalledAtCustomer` at a different site without:
  - A corresponding Return / RMA history.
- Standby port 32 usage is blocked without **approvalAttachmentId**.
- RMA closure must match quantity and serials involved.

---

## 13. Notes for Cursor / Devs

- Implement Inventory as a **separate bounded context** with:
  - Clear APIs to Orders, Scheduler, Billing.
- Do not mix material cost logic into Orders; keep it in Inventory + P&L.
- Prefer **event-driven** hooks (e.g. `OrderCompleted` event triggers inventory updates).
- Strong audit logging for:
  - StockMovement
  - SerialisedItem status changes
  - RMA lifecycle.

