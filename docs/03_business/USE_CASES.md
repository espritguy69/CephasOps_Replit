📌 Place this file here:
cephasops/docs/03_business/USE_CASES.md

This file documents real-world business scenarios that the system must support.
They link directly to modules, workflows, and policies — giving Cursor AI clear behavioural guidance.

USE_CASES.md

CephasOps – Business Use Cases
Version 1.0

This document describes the core business scenarios across all CephasOps companies and verticals.
These use cases define how the system is expected to behave in real operations.

1. ISP Operations (Cephas / Cephas Trading)

This is the main operational vertical today.

1.1 Activation Order – Email → Parser → Scheduling → SI Completion → Invoice
Actors

TIME Partner

Email Parser

Admin

Scheduler

Service Installer (SI)

Finance

Flow

TIME sends an email with Activation Excel.

Email Pipeline ingests it, Parser maps Excel → Parsed JSON.

Parser Resolver creates a new Order.

Admin reviews and confirms building, appointment.

Scheduler assigns SI based on availability.

SI updates statuses: OTW → Met → Completed.

SI uploads photos + scans serials.

Admin receives docket.

Finance generates invoice & submits to TIME portal.

Payment recorded → P&L & Payroll updated.

Success Result

Customer installation completed

Invoice uploaded

Revenue recognised

1.2 Modification / Relocation Order
Key logic

Order includes old and new addresses.

SI may need to recover old ONU and replace new one.

Splitter validation must be applied twice.

Flow

Email Parser identifies modification.

Order created with both addresses.

Scheduler assigns SI.

SI uninstalls old equipment → moves to RMA bin if faulty.

SI installs new ONU → scans serials.

Photos for both sites required.

Billing rules may differ for modification.

1.3 Assurance Order (TTKT / AWO / Link Down)
Flow

Assurance email received (TTKT / AWO).

Parser extracts TTKT, AWO, URLs, timestamps.

Order type = Assurance.

Scheduler assigns SI (priority job).

SI visits customer:

Checks ONU/router

Replaces faulty parts

Performs troubleshooting

Faulty parts → RMA.

Invoice created according to assurance rate card.

Outcome

Network restored

Faulty equipment tracked

RMA and billing aligned

1.4 Rescheduling by TIME (Email Approval)
Flow

Customer cancels or building delays job.

Admin requests reschedule in system.

TIME replies with email approval.

Parser detects approval.

System auto-approves reschedule.

Scheduler selects new slot based on SI availability.

Policy

No reschedule is allowed without:

Approved email OR

Customer-initiated early request

1.5 Customer No-Show / Building Access Issue
Flow

SI marks job as “Blocked – Customer Not Home” or “Blocked – Building Access”.

System requires:

Photo proof

Reason notes

Scheduler reviews blocker.

Customer contacted → new appointment set.

Impact

KPI deduction for customer-based reasons only if SI is not at fault.

Internal analysis through P&L.

1.6 Duplicate Order Prevention
Trigger

Same Service ID or Ticket No appears again in a new email.

Flow

Parser Resolver checks duplicates.

If active order exists:

System alerts admin.

No new order is created unless approved.

2. Inventory Use Cases
2.1 Warehouse Issues Material to SI
Flow

Admin prepares material list for SI.

Warehouse moves stock → SI Bag Location.

SI scans serials upon receiving.

Updated stock reflects changes.

2.2 SI Installs ONU/Router/Faceplate
Flow

SI scans device serial via mobile app.

System checks:

Serial belongs to company

Serial not on another customer

Serial not marked faulty

OrderMaterialUsage updated.

StockMovement created automatically.

2.3 SI Returns Faulty ONU
Flow

SI marks ONU as faulty in SI App.

Serialised item status → Faulty.

SI returns item → Warehouse RMA location.

RMA ticket created.

RMA PDF received later and attached.

3. Billing & Finance Use Cases
3.1 Invoice Generation – Activation
Flow

Admin marks dockets uploaded.

Finance opens invoice creation page.

System:

Loads ratecard based on partner + order type

Applies tax rules

Generates invoice PDF

Finance uploads invoice to TIME portal.

Submission ID stored.

3.2 Batch Invoice – Principal Billing
Flow

Finance selects multiple orders.

System groups by:

Partner

Company

Rate type

Single invoice generated with multiple invoice lines.

3.3 Payment Recording
Flow

TIME/partner pays.

Finance records payment with:

Date

Amount

Ref No

Invoice marked as Paid.

P&L updated.

4. Payroll Use Cases (Service Installers)
4.1 Monthly Payroll Calculation
Flow

Finance opens payroll period.

System collects:

All completed jobs

SI rates

KPI bonuses & penalties

PayrollItem generated per order.

Payroll run summarised.

Finance locks period.

4.2 SI Earnings View (Mobile App)
Flow

SI opens Earnings.

System shows:

Per-order earnings

Monthly summary

SI cannot see:

Other SIs

Company financials

5. P&L Use Cases
5.1 Monthly P&L Report
Flow

Director/Admin chooses month.

System aggregates:

Invoice revenue

Material usage cost

Labour cost

Overheads

Shows:

Net profit

Cost breakdown

Partner comparison

5.2 Profitability per Order Type
Flow

Director views P&L by category.

System shows:

Activation margin

Assurance margin

Modification margin

Helps analyse business strategy.

6. SI App Use Cases
6.1 SI Performs Job With Limited Network
Flow

SI app caches job list locally.

SI performs job:

Takes photos

Scans serial

When connected, app syncs all pending actions.

6.2 SI Requests Help
Flow

SI submits internal note.

Scheduler sees alert.

Admin responds or assigns support SI.

7. Kingsman (Barbershop) Future Use Cases
7.1 Customer Walk-in + POS Ticket
Flow

Customer walks in.

Staff opens POS ticket.

Select services + add-ons.

Payment processed via POS.

Daily revenue → P&L.

7.2 Staff Commission Calculation
Flow

Hairstylist completes service.

Commission auto-tracked.

Monthly payout exported via Payroll Module.

8. Menorah Travel Future Use Cases
8.1 Booking Inquiry → Quotation → Trip Management
Flow

Customer asks for quotation.

Admin creates trip proposal.

Customer accepts & makes deposit.

Trip itinerary prepared.

Final invoice issued.

9. Cross-Company Use Cases
9.1 Director Dashboard (Group Level)
Flow

Director logs in.

System shows group P&L:

Cephas

Cephas Trading

Kingsman

Menorah

Drill-down to each company.

9.2 Shared Inventory (Optional Future)
Flow

Item marked as “Transferable”.

StockMovement allowed between:

Cephas → Cephas Trading

Must log supervisor approval.