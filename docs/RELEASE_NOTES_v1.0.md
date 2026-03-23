# CephasOps Release Notes v1.0

**Release Date:** February 9, 2026  
**Version:** 1.0  
**Codename:** GPON Production Readiness

---

## Overview

This release brings CephasOps to production-ready completion for the GPON / Fibre Operations Platform. The canonical flow—Email → Order → Scheduling → SI Execution → Docket → Invoice → MyInvois → Payment → Completed—is fully supported with workflow validation, docket administration, and billing integration.

---

## New Features

### Docket Rejection Flow

Admin docket administration now supports full reject-and-correct handling:

- **DocketsRejected status** – New order status for dockets that fail verification
- **Reject with reason** – Admin can reject a docket from the Dockets page with a required reason (e.g., wrong splitter, missing ONU serial, incorrect customer details)
- **Accept corrected** – Admin can move an order from DocketsRejected back to DocketsReceived when the SI resubmits a corrected docket
- **Audit trail** – Rejection reason is stored in OrderStatusLog (TransitionReason)
- **UI** – Reject button, modal with reason input, Accept corrected button, and Rejected filter tab on `/operations/dockets`

---

## Enhancements

### Workflow Engine

- **Docket rejection transitions** – `DocketsReceived ↔ DocketsRejected` transitions added to the GPON order workflow
- **Workflow seed** – `07_gpon_order_workflow.sql` seeds all transitions including the docket rejection loop
- **Fallback controller** – OrderStatusesController fallback graph updated with DocketsRejected and its allowed transitions

### Frontend

- **Status constants** – DocketsRejected added to `ORDER_STATUS`, `ORDER_STATUSES`, `workflowStatuses`, and `statusColors`
- **Create/Edit order** – DocketsRejected included in status options and editable-status list on Order detail
- **Scheduler** – DocketsRejected added to scheduler status dropdowns

---

## Technical Details

### Backend

| Component | Change |
|-----------|--------|
| `OrderStatus.cs` | Added `DocketsRejected` constant and to `AllStatuses` |
| `07_gpon_order_workflow.sql` | Added `DocketsReceived → DocketsRejected`, `DocketsRejected → DocketsReceived` transitions |
| `OrderStatusesController.cs` | Added DocketsRejected to OrderWorkflowStatuses and fallback transitions |
| `OrderService` | Rejection reason passed via `ChangeOrderStatusDto.Reason` into workflow payload |
| `CreateOrderStatusLogSideEffectExecutor` | Stores `payload["reason"]` in `OrderStatusLog.TransitionReason` |

### Frontend

| Component | Change |
|-----------|--------|
| `DocketsPage.tsx` | Reject button, modal (Textarea for reason), Accept corrected button, DocketsRejected filter |
| `constants/orders.ts` | DocketsRejected status and color |
| `constants/workflowStatuses.ts` | DocketsRejected in SIDE_STATES, Documentation phase, ORDER_FLOW_SEQUENCE |
| `utils/statusColors.ts` | DocketsRejected badge and scheduler styles |
| `types/scheduler.ts` | DocketsRejected in ORDER_STATUSES |
| `CreateOrderPage.tsx` | DocketsRejected in STATUS_OPTIONS and editableStatuses |

### Database

- No schema changes required. DocketsRejected is a string status; transitions are stored in `WorkflowTransitions`.
- Run `07_gpon_order_workflow.sql` (included in `run-all-seeds.ps1`) to add new transitions on existing databases.

---

## Documentation Updates

- **business/docket_process.md** – Implementation note for DocketsPage
- **business/order_lifecycle_and_statuses.md** – DocketsRejected status, transitions, §8 Docket verification
- **operations/db_workflow_baseline_spec.md** – Docket rejection loop documented
- **_discrepancies.md** – Restructured into Closed, Open – Must Fix, Accepted Gaps, Deferred; workflow items closed

---

## Known Limitations

1. **SI notification** – Reject action shows "SI will be notified" in the UI; automated SI notification is not yet implemented.
2. **MyInvois** – Requires credentials via IntegrationSettings; runbook: `docs/operations/myinvois_production_runbook.md`.

---

## Upgrade Instructions

1. Pull the latest code.
2. Run database seeds (if not already applied):
   ```powershell
   cd backend\scripts\postgresql-seeds
   .\run-all-seeds.ps1
   ```
   Or apply only the workflow seed:
   ```powershell
   psql -h localhost -p 5432 -U postgres -d cephasops -f 07_gpon_order_workflow.sql
   ```
3. Restart backend and frontend services.
4. Verify Dockets page at `/operations/dockets` shows Reject and Accept corrected actions.

---

## Contributors

CephasOps Development Team
