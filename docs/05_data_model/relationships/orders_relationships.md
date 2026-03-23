# Orders Domain – Relationships  
CephasOps Data Model – Orders Relationships  
Version 1.0

This document describes how **Orders** and related entities connect inside the Orders domain.

---

## 1. High-Level Overview

Main relationships:

- Company 1—* Orders
- Order 1—* OrderStatusLogs
- Order 1—* OrderReschedules
- Order 1—* OrderBlockers
- Order 1—* OrderDockets
- Order 1—* OrderMaterialUsage
- Order 1—* PnlOrderDetails (P&L)
- Order 0–1 Invoice (Billing)
- Order *–1 ServiceInstaller (assigned SI)
- Order *–1 Building

---

## 2. Entity Relationship Diagram (Orders Scope)

```mermaid
erDiagram
    Company ||--o{ Order : owns
    Order ||--o{ OrderStatusLog : has
    Order ||--o{ OrderReschedule : may_have
    Order ||--o{ OrderBlocker : may_have
    Order ||--o{ OrderDocket : has
    Order ||--o{ OrderMaterialUsage : consumes
    Order ||--o{ PnlOrderDetail : analysed_as
    Order }o--o| Invoice : may_be_billed_as
    Order }o--|| Building : belongs_to
    Order }o--|| ServiceInstaller : assigned_to
Diagram references:
OrderStatusLog → table OrderStatusLogs
OrderReschedule → table OrderReschedules
OrderBlocker → table OrderBlockers
OrderDocket → table OrderDockets
OrderMaterialUsage → table OrderMaterialUsage
PnlOrderDetail → table PnlOrderDetails

3. Order → Status Logs
Order.id → OrderStatusLogs.orderId

1–* relationship.

Logs are append-only, ordered by createdAt.

Usage:

Audit trail for Workflow Engine.

Debug KPI breaches (kpiBreachedAt vs status change times).

4. Order → Reschedules
Order.id → OrderReschedules.orderId

1–* relationship.

Rules:

Only OrderReschedules with status = Approved update:

Orders.appointmentDate

Orders.appointmentWindowFrom

Orders.appointmentWindowTo

Reschedule count is denormalised into Orders.rescheduleCount.

5. Order → Blockers
Order.id → OrderBlockers.orderId

1–* relationship.

Usage:

Any active OrderBlockers (where resolved = false) may:

prevent transition to Completed

block invoiceEligible from being set to true

6. Order → Dockets
Order.id → OrderDockets.orderId

1–* relationship.

Business rule:

At least one OrderDockets.isFinal = true is required for:

Orders.docketUploaded = true

Billing eligibility

7. Order → MaterialUsage
Order.id → OrderMaterialUsage.orderId

1–* relationship.

Downstream:

OrderMaterialUsage → StockMovements (Inventory)

P&L uses sums of OrderMaterialUsage.totalCost per order.

8. Order → Building
Order.buildingId → Buildings.id

Each order belongs to exactly one building.

Validation:

Building-level policies (allowed order types, partner mapping, etc.) are read from:

Buildings

BuildingTypes

CompanySettings/Settings module.

9. Order → SI & Scheduler
Order.assignedSiId → ServiceInstallers.id

Order.id → ScheduledSlots.orderId (Scheduler module)

Typical pattern:

ScheduledSlots acts as the calendar

Orders.assignedSiId acts as the canonical assignment

10. Order → Billing & Payroll
Covered in more detail in billing_relationships.md and payroll_relationships.md, but summary:

Orders.id → InvoiceLines.orderId

Orders.id → PayrollItems.orderId

Ensures each job’s revenue and SI cost can be matched in P&L.

End of Orders Relationships