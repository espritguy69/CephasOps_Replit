---

### `material_template_relationships.md`

```markdown
# Material Template Relationships (Full Production)

## 1. Overview

This document focuses on how **material templates** connect to:

- Material master data
- Job types (FTTH/FTTB/etc.)
- Orders & work orders
- Warehouse and material movement
- Installer issuance and returns

Core entities:

- `MaterialTemplate`
- `MaterialTemplateItem`
- `MaterialAssignmentPreset`
- `MaterialItem`
- `WorkOrderMaterial`
- `MaterialMovement`
- `MaterialSerial`
- `Warehouse`

---

## 2. Template-Level Relationships

### 2.1 MaterialTemplate ↔ MaterialTemplateItem (1–N)

- 1 `MaterialTemplate` → many `MaterialTemplateItem`.

`MaterialTemplate` defines a **bundle** (e.g. "FTTH Standard Install"), while `MaterialTemplateItem` defines each item and its default quantity.

Relationships:

- `MaterialTemplateItem.material_template_id` (FK → `MaterialTemplate.id`).

---

### 2.2 MaterialTemplateItem ↔ MaterialItem (N–1)

Each item line in a template references the master item:

- `MaterialTemplateItem.material_item_id` (FK → `MaterialItem.id`).

**Usage:**

- Ensures templates reference core catalog entries (codes, descriptions, UoM).
- Template changes do **not** duplicate material definitions.

---

### 2.3 MaterialTemplate ↔ MaterialAssignmentPreset

`MaterialAssignmentPreset` links job types to templates:

- `MaterialAssignmentPreset.job_type` (e.g. FTTH, FTTC, WiFi7 Upgrade).
- `MaterialAssignmentPreset.material_template_id` (FK → `MaterialTemplate.id`).

Cardinality:

- 1 `MaterialTemplate` → many `MaterialAssignmentPreset` (over different periods or job types).
- 1 `job_type` can map to **one active preset** at a time (enforced via business logic + date ranges).

---

## 3. From Templates to Work Orders

### 3.1 Order / WorkOrder ↔ MaterialAssignmentPreset

When an order/work order is created:

1. Determine `job_type`:
   - From `Order.order_type`, `Order.sub_type`, or dedicated field.
2. Lookup active `MaterialAssignmentPreset` for that `job_type`.
3. Expand template items to `WorkOrderMaterial`.

Relationships:

- `WorkOrderMaterial.order_id` (FK → `Order.id`).
- `WorkOrderMaterial.work_order_id` (FK → `WorkOrder.id`).
- `WorkOrderMaterial.material_item_id` (FK → `MaterialItem.id`).

---

### 3.2 MaterialTemplate ↔ WorkOrderMaterial (Indirect)

There is no direct FK from `WorkOrderMaterial` to `MaterialTemplate` in the base design to keep it simple; the link is **logical**:

- At creation time, `WorkOrderMaterial` lines are derived from:
  - `MaterialTemplate` + `MaterialTemplateItem`.

Optionally, an audit field can be added:

- `WorkOrderMaterial.source_template_id` (FK → `MaterialTemplate.id`, nullable).

---

## 4. Materials, Warehouse & Movements

### 4.1 MaterialItem ↔ Warehouse ↔ StockLevel

- 1 `Warehouse` → many `StockLevel`.
- 1 `MaterialItem` → many `StockLevel`.

This deals with on-hand quantities and reservation.

---

### 4.2 WorkOrderMaterial ↔ MaterialMovement

Work-order-level material consumption is reflected in movements:

- `MaterialMovement.work_order_id` (FK → `WorkOrder.id`).
- `MaterialMovement.material_item_id` (FK → `MaterialItem.id`).
- `MaterialMovement.from_warehouse_id` / `to_warehouse_id` (FK → `Warehouse.id`).

Typical flows:

1. **Issue to installer:**
   - `from_warehouse = MainStore`
   - `to_warehouse = InstallerVan`
   - `movement_type = IssueToWorkOrder` (or separate "IssueToInstaller" + later "IssueToWorkOrder").
2. **Install at customer:**
   - `from_warehouse = InstallerVan`
   - `to_warehouse = CustomerLocation` (logical, or set `to_warehouse` = null, with `work_order_id` filled).
3. **Return unused:**
   - `from_warehouse = InstallerVan`
   - `to_warehouse = MainStore`
   - `movement_type = ReturnFromWorkOrder`.

`WorkOrderMaterial` should always align with net issue to job after returns.

---

### 4.3 MaterialSerial ↔ WorkOrderMaterial

For serialised items (routers, ONTs):

- `MaterialSerial.material_item_id` (FK).
- `MaterialSerial.order_id` / `work_order_id` (FK) indicate where it ended up.
- When generating `WorkOrderMaterial` lines, serial information can be:
  - Stored as a JSON array in `WorkOrderMaterial.serial_numbers`, or
  - Linked via a separate join table (`WorkOrderMaterialSerial`) if fine-grained tracking is needed.

---

## 5. Behaviour Rules & Constraints

### 5.1 Template Integrity

- `MaterialTemplateItem` cannot reference inactive `MaterialItem` **unless**:
  - It is explicitly allowed for historical reasons (e.g. old router still in stock).
- When a `MaterialItem` is deactivated, templates should be reviewed:
  - Either soft-block new jobs from using that template, or
  - Automatically swap to a replacement item.

---

### 5.2 Preset Validity

- At most **one** active `MaterialAssignmentPreset` per `job_type` **and** `effective_from`–`effective_to` range.
- If no preset exists:
  - System may:
    - Use a fallback default template, or
    - Force manual material selection.

---

### 5.3 Template Customisation Per Work Order

Even when a template provides defaults:

- Dispatcher / planner can:
  - Override `quantity_planned`.
  - Add additional `WorkOrderMaterial` rows not in the template.
  - Remove optional items (`is_optional = true`).

`MaterialTemplate` defines **baseline**, not a strict lock.

---

## 6. Example Flows

### 6.1 New Standard FTTH Order

1. `Order` created with `order_type = Activation`, `sub_type = FTTH`.
2. `MaterialAssignmentPreset` found where `job_type = FTTH_STD`.
3. `MaterialTemplateItem` expanded into `WorkOrderMaterial`:
   - Router x 1
   - ONT x 1
   - Fiber cable x 1 roll
4. Warehouse issues items via `MaterialMovement`.
5. Serial numbers are scanned and attached to `WorkOrderMaterial`.

---

### 6.2 WiFi7 Upgrade (New Job Type)

1. New job type `WIFI7_UPGRADE` defined.
2. New `MaterialTemplate` created with specific router model, additional cable.
3. `MaterialAssignmentPreset` links `WIFI7_UPGRADE` → that template.
4. From that point on, WiFi7 upgrade orders automatically get the right material plan.

---

## 7. Relationship Summary

1. `MaterialTemplate` (1) → `MaterialTemplateItem` (N)
2. `MaterialTemplateItem` (N) → `MaterialItem` (1)
3. `MaterialTemplate` (1) → `MaterialAssignmentPreset` (N)
4. `MaterialAssignmentPreset` (logical) → `WorkOrderMaterial` (via creation process)
5. `WorkOrderMaterial` (N) → `MaterialItem` (1)
6. `WorkOrderMaterial` (N) → `Order` / `WorkOrder` (1)
7. `WorkOrderMaterial` ↔ `MaterialMovement` (consumption and returns)
8. `WorkOrderMaterial` ↔ `MaterialSerial` (for serialised hardware, optionally via additional join table)

