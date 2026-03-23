# Splitter Relationships (Full Production)

## 1. Overview

This document describes how the **Splitter module** relates to the rest of the CephasOps domain:

- Splitters and splitter ports
- Orders and work orders
- Premises and customers
- Materials and serials
- Audit/logging

Core entities (from splitter_entities & main domain):

- `Splitter`
- `SplitterPort`
- `SplitterHistory`
- `Order`
- `WorkOrder`
- `Premises`
- `MaterialSerial`
- `SystemLog` / `AuditEvent`

---

## 2. Core Relationships

### 2.1 Splitter ↔ Premises / Location

- A `Splitter` represents a **physical splitter box** mounted at or near one or more premises.
- There is no hard FK to `Premises` because one splitter can serve **multiple premises**.
- Relationship is expressed via:
  - `Splitter.location_description`
  - `Splitter.gps_lat` / `gps_lng`
  - `SplitterPort.order_id` / `work_order_id` → which in turn link to `Premises`.

**Implication:**

- To know which premises are served by a splitter, the system:
  1. Finds all `SplitterPort` records under a `Splitter`
  2. Follows `SplitterPort.order_id` → `Order.premises_id`
  3. Aggregates distinct premises.

---

### 2.2 Splitter ↔ SplitterPort (1–N)

- 1 `Splitter` **has many** `SplitterPort`.

**Cardinality:**

- `Splitter` (1) → `SplitterPort` (N)
- `Splitter.total_ports` should match the number of active ports defined.

**Enforced by:**

- `SplitterPort.splitter_id` (FK → `Splitter.id`)

---

### 2.3 SplitterPort ↔ Order / WorkOrder

Each `SplitterPort` may be associated with **at most one active order** at a time.

- `SplitterPort.order_id` (FK → `Order.id`, nullable)
- `SplitterPort.work_order_id` (FK → `WorkOrder.id`, nullable)

**Rules:**

1. When an order is **installed** and a port is assigned:
   - `SplitterPort.status` = `Occupied`
   - `SplitterPort.order_id` = that order
   - `SplitterPort.work_order_id` = work order that performed the installation

2. When a service is **terminated / de-installed**:
   - `SplitterPort.status` updated to `Available`
   - `SplitterPort.order_id` set to NULL (or moved to `SplitterHistory` as “previous order”)

3. For **revisits / assurance** without changing port:
   - Typically, `SplitterPort` remains linked to the original `Order`
   - `work_order_id` may point to the **latest** work order that touched this port

---

### 2.4 SplitterPort ↔ MaterialSerial

Optional but recommended for deeper traceability:

- `SplitterPort.connected_serial` maps to `MaterialSerial.serial_number`.

This allows us to know **which device** (ONT/router) is hanging on a given port.

Workflow:

1. `WorkOrderMaterial` logs material usage.
2. `MaterialSerial` is marked `Installed` on an order/work order.
3. `SplitterPort.connected_serial` is set to that serial.
4. Later checks:
   - If a complaint arises, we can track back:
     - Port → Serial → Material movement history → Order → Installer.

---

## 3. History & Audit

### 3.1 SplitterPort ↔ SplitterHistory (1–N)

Every important change in a port’s state is recorded:

- `SplitterPort` (1) → `SplitterHistory` (N)

`SplitterHistory` captures:

- `previous_status`, `new_status`
- `order_id` (if port assignment change is related to a specific order)
- `changed_at`, `changed_by_user_id`

Examples:

- Port 05 changed from `Available` → `Occupied` for `Order #O-1234`.
- Port 05 moved from `Occupied` → `Faulty` after on-site check.

---

### 3.2 Splitter ↔ SystemLog / AuditEvent

Splitter operations generate logs:

- A new splitter created:
  - `SystemLog` entry with `category = "Splitter"`, `entity_type = "Splitter"`, `entity_id = splitter.id`.
- Port reassignment:
  - `SystemLog` entry for both `SplitterPort` and `Order`.

User actions (e.g., manual override) are recorded as:

- `AuditEvent` with `event_type = "SplitterChange"` or similar, linked to `user_id`.

---

## 4. Relationship Summary

1. **Splitter → SplitterPort**
   - 1–N, defines the physical ports.
2. **SplitterPort → Order / WorkOrder**
   - 0–1 active order per port at a time; FK to represent current usage.
3. **SplitterPort → SplitterHistory**
   - 1–N historical log of status and assignment changes.
4. **SplitterPort → MaterialSerial**
   - Optional link via `connected_serial`, binds port to actual device.
5. **Splitter / SplitterPort → Premises**
   - Indirect: via `Order.premises_id`.
6. **Splitter / SplitterPort → SystemLog / AuditEvent**
   - For traceability and compliance.

---

## 5. Example Use Cases

### 5.1 New FTTH Installation

1. `Order` created for a new install.
2. `WorkOrder` scheduled to visit.
3. Installer:
   - Picks splitter box.
   - Chooses free port (`SplitterPort.status = Available`).
   - Connects customer.
4. System:
   - Updates `SplitterPort.status = Occupied`.
   - Sets `SplitterPort.order_id`, `work_order_id`.
   - Adds `SplitterHistory` record.

### 5.2 Port Reassignment

- Existing port is faulty; installer moves customer to a new port:
  1. Old port: `Occupied` → `Faulty`.
  2. New port: `Available` → `Occupied`.
  3. Both changes logged in `SplitterHistory` and `SystemLog`.

