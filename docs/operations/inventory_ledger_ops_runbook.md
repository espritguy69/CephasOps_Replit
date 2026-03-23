# Inventory & Ledger Operations Runbook (GPON)

**Related:** [Inventory, Ledger and Serials (canonical)](../modules/inventory_ledger_and_serials.md) | [Background jobs](background_jobs.md) | [Inventory & ledger summary](../business/inventory_ledger_summary.md)

**Source of truth:** docs/modules/inventory_ledger_and_serials.md.

---

## 1. Ledger integrity

| Item | Implementation | Verification |
|------|----------------|--------------|
| **Ledger append-only** | StockLedgerEntry; no direct balance writes | Ledger is source of truth; balances derived |
| **LedgerBalanceCache** | Cached balances; reconciled periodically | ReconcileLedgerBalanceCache job |
| **Reconcile job schedule** | LedgerReconciliationSchedulerService (every 12h check) | Admin → Background Jobs; look for reconcileledgerbalancecache |

---

## 2. Stock-by-location snapshots

| Item | Implementation | Verification |
|------|----------------|--------------|
| **Snapshot job** | PopulateStockByLocationSnapshots | StockSnapshotSchedulerService enqueues daily (every 6h check) |
| **Reports** | StockByLocationHistoryReport | Inventory → Reports → Stock by Location History |
| **Stock trend** | Stock trend report | /inventory/reports/stock-trend |

---

## 3. Serial lifecycle

| Flow | States | UI/API |
|------|--------|--------|
| **Receive → Allocate → Issue → Use** | InWarehouse → WithSI → InstalledAtCustomer | Allocate, Issue, Return pages |
| **Return (unused)** | WithSI → Returned | Inventory Return page |
| **Faulty / RMA** | Faulty → RMARequested → InTransitToPartner → AtPartner → Repaired/Replaced/Credited/Scrapped → RMAClosed | RMA flow; RMAListPage |

**Serial lifecycle report:** /inventory/reports/serial-lifecycle.

---

## 4. RMA path (summary)

- **RMA request:** SI or Admin creates RMA for faulty device.
- **Partner MRA:** MRA document received and attached.
- **Resolution:** Repaired, Replaced, Credited, Scrapped, or RMAClosed.
- **Assurance jobs:** Serialised replacements require TIME approval before ReadyForInvoice.

---

## 5. Inventory reports (department-scoped)

| Report | Route | Department scope |
|--------|-------|------------------|
| Usage by period | /inventory/reports/usage | Yes |
| Serial lifecycle | /inventory/reports/serial-lifecycle | Yes |
| Stock trend | /inventory/reports/stock-trend | Yes |
| Stock by location history | Via InventoryController | Yes |

---

## 6. Movement validation

- **MovementValidationService** validates all movements before ledger write.
- Prevents invalid allocations, negative balances, duplicate serial allocation.
- InventoryService calls ValidateMovementAsync before each movement.

---

**Last updated:** 2026-02-09
