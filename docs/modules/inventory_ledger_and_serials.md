# Inventory, Ledger and Serialised Equipment (GPON – Canonical)

**Related:** [Order lifecycle and statuses](../business/order_lifecycle_and_statuses.md) | [Inventory & ledger summary (overview)](../business/inventory_ledger_summary.md) | [Billing and invoicing](billing_and_invoicing.md)

**Source of truth:** docs/_source/Codebase_Summary_SourceOfTruth.md; docs/_source/Business_Processes_SourceOfTruth.md.

This document is the **single authoritative** inventory and ledger specification for GPON. It covers materials and serialised equipment; ledger as sole source of truth; allocation, issue, return, and RMA. It does **not** cover warehouse accounting, valuation, or GL.

---

## 1. Purpose and scope

- **Purpose:** Define how materials and serialised equipment are received, stored, allocated to orders, issued to field, used or returned, and how RMA/replacements are handled. All quantity and movement truth comes from the **ledger**.
- **Scope:** **Materials + serialised equipment** (e.g. ONU, router, 2-in-1 devices). Single-company, **department-scoped**; access enforced via ResolveDepartmentScopeAsync / ResolveLedgerContextAsync (403 when not allowed).
- **Out of scope (explicit):** Warehouse accounting; inventory valuation; general ledger (GL). Stock summary and reports **derive from ledger**; no separate “balance” write path.

---

## 2. Ledger as the sole source of truth (append-only)

- **No direct balance writes:** Stock is **not** updated by direct writes to a simple “balance” quantity. All movements create **ledger entries** (append-only).
- **Balances derived:** Stock summary and report APIs (e.g. usage-summary, serial-lifecycle, stock-by-location-history) **derive from the ledger**. **LedgerBalanceCache** and **StockByLocationSnapshot** support performance and reporting; they are derived/cached, not the source of truth.
- **Department-scoped:** Ledger access and all movement APIs are department-scoped; 403 when user has no access to the department.
- **Audit:** Every movement is a ledger entry with who, when, order/location/material, quantity, and type (receive, transfer, allocate, issue, return, etc.).

---

## 3. Stock lifecycle (main flow)

- **Receive** – Incoming stock into a location; creates ledger entry. Source: partner or internal transfer.
- **Allocate** – Stock allocated to an order (e.g. for SI job). Links material/location to order; may reserve serials for serialised items.
- **Issue** – Stock issued out to order/field (e.g. to SI). Ledger entry; for serialised items, ownership/status updated (e.g. WithSI, InstalledAtCustomer).
- **Use** – Consumption on job (recorded via order material usage / completion). For serialised items, device is tied to order and customer site.
- **Return** – Stock returned from order/field (unused or faulty). Ledger entry; for serialised, may go to InWarehouse or Faulty/RMA path.
- **Transfer** – Between locations; ledger entries (out from one, in to another).

---

## 4. Serialised equipment lifecycle (ONU, 2-in-1, etc.)

- **Serialised items** are tracked by **serial number** from receipt to installation or RMA.
- **States (conceptual):** InWarehouse → WithSI (issued) → InstalledAtCustomer (used on job); or → Returned (unused); or → Faulty → RMA path (RMARequested, InTransitToPartner, AtPartner, Repaired/Replaced/Credited/Scrapped/Closed).
- **Order linkage:** One job can be linked to specific serials (allocate → issue → use). Serial lifecycle reporting tracks movement and usage by serial.
- **Assurance jobs:** SI can record old/new device swaps (old serial out, new serial in). Serialised replacements require TIME approval for RMA; non-serialised replacements require material type and quantity.

---

## 5. Order linkage rules (one job ↔ serials)

- Materials (and serials) are **allocated to an order** before or at issue. One order has many line items (materials/serials).
- **One job ↔ serials:** Serials allocated/issued to an order are tied to that order until returned or written off. No double-allocation of the same serial to another order while in use.
- **Material collection:** System can check ledger-derived balances and allocation against order requirements (e.g. SI inventory check).

---

## 6. RMA and replacement handling

- **RMA (Return Material Authorization):** Faulty or defective devices returned from customer/SI; tracked via RMA request and items. Serials move to RMA states (e.g. RMARequested, InTransitToPartner, AtPartner, Repaired/Replaced/Credited/Scrapped, RMAClosed).
- **Replacement on assurance job:** For **assurance** orders, SI/Admin records old/new material swaps. **Serialised replacements:** Old device + New device; TIME approval required (and must be present before order can move to ReadyForInvoice). **Non-serialised:** Material type + quantity; no approval needed for transition.
- **MRA / partner:** RMA may involve partner MRA documents and shipment to partner; resolution (repaired, replaced, credited, scrapped) is recorded. Details follow RMA workflow (reference: legacy inventory/RMA docs).

---

## 7. Stock-by-location snapshots and reconciliation jobs

- **StockByLocationSnapshot:** Periodic (e.g. daily) **snapshots** of stock by location for reporting and trend. Populated by a **background job** (e.g. PopulateStockByLocationSnapshots). Used by reports (stock-by-location-history, trends).
- **LedgerBalanceCache:** Cache of ledger-derived balances for performance; updated or reconciled by **ReconcileLedgerBalanceCache** background job. Not the source of truth; ledger is.
- **Reconciliation:** If cache or snapshot drifts, reconciliation job can recompute from ledger. No direct balance writes outside ledger.

---

## 8. Who can perform each action (Inventory vs Ops vs SI)

- **Receive, Transfer:** **Inventory / Warehouse** (or Ops with inventory access). Department-scoped.
- **Allocate to order:** **Ops (Admin)** or planning; links materials/serials to order. Department-scoped.
- **Issue (to SI or order):** **Ops / Inventory;** may be driven by scheduler or order assignment. Department-scoped.
- **Return, Faulty, RMA request:** **SI** (from field) or **Inventory / Ops** (from warehouse). Department-scoped.
- **Record use on job (order material usage):** **SI** (at completion) or **Ops** (admin entry). Assurance RMA data (old/new serials) from MetCustomer onward: SI or Admin.
- **View ledger, stock summary, reports:** **Inventory, Ops, Finance** (department-scoped). SI may have limited view (e.g. own allocations/returns).

---

## 9. Audit rules and non-negotiable constraints

- **Append-only ledger:** No deletion or correction of ledger entries for balance “fixes”; corrections are new movements (e.g. adjustment entry if allowed by policy).
- **Department scope:** Every ledger read/write is scoped to a department; 403 when user has no access.
- **Order linkage:** Allocations and issues tied to order must reference a valid order in the same company/department context.
- **Serial uniqueness:** A serial cannot be in two orders or two locations at once; state transitions must be consistent (e.g. WithSI → InstalledAtCustomer or Returned).
- **Audit trail:** Every movement logs who, when, what, order/location, quantity, type; available for audit and reporting.

---

## 10. Explicit out-of-scope items

- **Warehouse accounting:** Full warehouse ledger accounting (e.g. cost layers, FIFO valuation) – not in scope; default cost may be stored for P&L use but valuation is not this module’s authority.
- **Valuation:** Inventory valuation for financial statements – external or separate process.
- **General ledger (GL):** No double-entry GL in CephasOps; inventory value/cost flows to P&L/GL via exports or external system.
- **Multi-company inventory:** Current scope is single-company; cross-company transfers not in scope.
