# Inventory & Ledger Model – Summary

**Related:** [Product overview](../overview/product_overview.md) | [Order lifecycle and statuses](order_lifecycle_and_statuses.md)

**Source of truth:** docs/_source/Codebase_Summary_SourceOfTruth.md; docs/_source/Business_Processes_SourceOfTruth.md.

---

## Authoritative document

**Full inventory, ledger and serialised-equipment specification (ledger as source of truth, stock lifecycle, serials, order linkage, RMA, snapshots, who can act, audit, out-of-scope):**  
**[docs/modules/inventory_ledger_and_serials.md](../modules/inventory_ledger_and_serials.md)**

---

## Overview

- **Ledger as source of truth:** All movements go through the **ledger** (receive, transfer, allocate, issue, return). Stock summary and reports **derive from the ledger**; no direct balance writes. LedgerBalanceCache and StockByLocationSnapshot support performance and reporting. Department-scoped; 403 when not allowed.
- **Serialised equipment:** Tracked by serial number; allocated to orders, issued to SI, used on job or returned/faulty (RMA). For assurance jobs, SI records old/new device swaps; serialised replacements require TIME approval.
- **Main operations:** Receive, Transfer, Allocate, Issue, Return; reports (usage by period, stock trend, stock by location history, serial lifecycle) – all department-scoped.

---

## Legacy references (reference only)

- [02_modules/inventory/OVERVIEW.md](../02_modules/inventory/OVERVIEW.md) – Legacy inventory and RMA module spec.
- [02_modules/inventory/WORKFLOW.md](../02_modules/inventory/WORKFLOW.md) – Legacy inventory workflow.
- [02_modules/inventory/MATERIAL_POPULATION_RULES.md](../02_modules/inventory/MATERIAL_POPULATION_RULES.md) – Legacy material population rules.
- [05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md](../05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md) – Reference types (materials, locations, etc.).
