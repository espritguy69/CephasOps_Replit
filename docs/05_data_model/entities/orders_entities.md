\# Orders Entities  

CephasOps – Orders Domain Data Model  

Version 1.0



This file defines the core entities for the \*\*Orders Domain\*\*:



\- Order (root aggregate)

\- OrderStatusLog

\- OrderReschedule

\- OrderBlocker

\- OrderDocket

\- OrderMaterialUsage

\- OrderTag (optional)

\- Building (reference, read-only in this context)



All entities are \*\*company-scoped\*\* via `companyId`.



---



\## 1. Order



Represents a single job / work order (Activation, Assurance, Modification, Relocation, etc.) coming from partners like TIME, Celcom, etc.



\### 1.1 Table: `Orders`



| Field                  | Type       | Required | Description                                                                                  |

|------------------------|-----------|----------|----------------------------------------------------------------------------------------------|

| id                     | uuid      | yes      | Primary key.                                                                                 |

| companyId              | uuid      | yes      | FK → Companies.id. Multi-company boundary.                                                  |

| partnerId              | uuid      | yes      | FK → Partners.id (TIME, Celcom, etc.).                                                      |

| sourceSystem           | string    | yes      | Origin: `EmailParser`, `Manual`, `API`, `Import`.                                           |

| sourceEmailId          | uuid      | no       | FK → EmailMessage.id when parsed from email.                                                |

| orderTypeId            | uuid      | yes      | FK → OrderTypes.id (Activation, Assurance, Relocation, FTTR, etc.).                         |

| serviceIdType          | enum      | no       | Service ID type: `Tbbn` (1) or `PartnerServiceId` (2). Auto-detected from serviceId value.   |

| serviceId              | string    | yes      | Unique service identifier from partner (TBBN or Partner Service ID).                        |

| ticketId               | string    | no       | TTKT / AWO / trouble ticket reference.                                                      |

| awoNumber              | string    | no       | AWO number for Assurance orders.                                                             |

| externalRef            | string    | no       | Generic external reference (portal ID, batch ID, etc.).                                     |

| status                 | enum      | yes      | FK → enums/order\_status (Pending, Assigned, OTW, MetCustomer, Completed, Blocked, etc.).    |

| statusReason           | string    | no       | Short reason for current status (e.g. “Customer not home”).                                 |

| priority               | enum      | no       | `Low`, `Normal`, `High`, `Critical`.                                                        |

| buildingId             | uuid      | yes      | FK → Buildings.id.                                                                          |

| buildingName           | string    | no       | Cached building label for reporting.                                                        |

| unitNo                 | string    | no       | Customer unit / house number.                                                               |

| addressLine1           | string    | yes      | Installation address line 1.                                                                |

| addressLine2           | string    | no       | Address line 2.                                                                             |

| city                   | string    | yes      | City / town.                                                                                |

| state                  | string    | yes      | State / region.                                                                             |

| postcode               | string    | yes      | Postcode.                                                                                   |

| latitude               | decimal   | no       | Optional GPS latitude.                                                                      |

| longitude              | decimal   | no       | Optional GPS longitude.                                                                     |

| customerName           | string    | yes      | End-customer full name.                                                                     |

| customerPhone          | string    | yes      | Contact number (normalised format where possible).                                         |

| customerPhone2         | string    | no       | Secondary contact number (auto-split from "0123456789/0129876543" format).                  |

| customerEmail          | string    | no       | Email address.                                                                              |

| orderNotesInternal     | text      | no       | Internal notes for admins / schedulers.                                                     |

| partnerNotes           | text      | no       | Notes copied from partner email / Excel.                                                    |

| requestedAppointmentAt | datetime  | no       | Time requested by partner / customer (original).                                            |

| appointmentDate        | date      | yes      | Confirmed appointment date.                                                                 |

| appointmentWindowFrom  | time      | yes      | Start of time window (e.g. 10:00).                                                          |

| appointmentWindowTo    | time      | yes      | End of time window (e.g. 12:00).                                                            |

| assignedSiId           | uuid      | no       | FK → ServiceInstallers.id. Null until assigned.                                             |

| assignedTeamId         | uuid      | no       | FK → Teams.id if jobs handled by teams.                                                     |

| kpiCategory            | string    | no       | KPI group (e.g. `FTTH`, `FTTR`, `Assurance`, `Relocation`).                                 |

| kpiDueAt               | datetime  | no       | Time by which job should be completed for KPI compliance.                                   |

| kpiBreachedAt          | datetime  | no       | When KPI was breached (if applicable).                                                      |

| hasReschedules         | boolean   | yes      | True if any reschedule records exist.                                                       |

| rescheduleCount        | int       | yes      | Number of reschedules.                                                                      |

| docketUploaded         | boolean   | yes      | True when final docket is attached.                                                         |

| photosUploaded         | boolean   | yes      | True when minimum required photos exist.                                                    |

| serialsValidated       | boolean   | yes      | True when ONU/router/other serials validated.                                               |

| invoiceEligible        | boolean   | yes      | Derived flag for billing eligibility.                                                       |

| invoiceId              | uuid      | no       | FK → Invoice.id if single-invoice per order.                                                |

| payrollPeriodId        | uuid      | no       | FK → PayrollPeriod.id indicating which payroll run includes this job.                       |

| pnlPeriod              | string    | no       | Friendly period label used in P\&L (`2025-01`, etc.).                                       |

| splitterNumber         | string    | no       | Splitter number used for this order (required before Docket Verification).                 |

| splitterLocation       | string    | no       | Splitter location (e.g., "MDF Level 1", "Riser Room Floor 5").                            |

| splitterPort           | string    | no       | Splitter port number used (required before Docket Verification).                          |

| splitterId             | uuid      | no       | FK → Splitters.id if linked to Splitter entity.                                             |

| packageName            | string    | no       | Package/Plan name from partner (legacy field).                                              |

| bandwidth              | string    | no       | Bandwidth (e.g., "600 Mbps") (legacy field).                                               |

| onuSerialNumber        | string    | no       | ONU Serial Number.                                                                          |

| networkPackage         | string    | no       | Network package/plan name (multi-line, from FIBER INTERNET section).                       |

| networkBandwidth       | string    | no       | Network bandwidth (e.g., "600 Mbps").                                                      |

| networkLoginId         | string    | no       | Network login ID.                                                                          |

| networkPassword        | string    | no       | Network password (masked in UI).                                                             |

| networkWanIp           | string    | no       | WAN IP address.                                                                             |

| networkLanIp           | string    | no       | LAN IP address.                                                                             |

| networkGateway         | string    | no       | Gateway IP address.                                                                         |

| networkSubnetMask      | string    | no       | Subnet mask.                                                                                |

| voipServiceId          | string    | no       | VOIP Service ID (split from "Service ID / Password" format).                                |

| voipPassword           | string    | no       | VOIP password (split from "Service ID / Password" format).                                 |

| voipIpAddressOnu      | string    | no       | VOIP ONU IP address.                                                                        |

| voipGatewayOnu         | string    | no       | VOIP ONU Gateway.                                                                          |

| voipSubnetMaskOnu      | string    | no       | VOIP ONU Subnet Mask.                                                                       |

| voipIpAddressSrp       | string    | no       | VOIP SRP IP address.                                                                        |

| voipRemarks            | string    | no       | VOIP remarks/notes (multi-line).                                                            |

| createdByUserId        | uuid      | yes      | FK → Users.id.                                                                              |

| createdAt              | datetime  | yes      | Creation timestamp.                                                                         |

| updatedAt              | datetime  | yes      | Last modification timestamp.                                                                |

| cancelledAt            | datetime  | no       | When order was cancelled (if applicable).                                                   |

| cancelledByUserId      | uuid      | no       | Who cancelled the order.                                                                    |



\### 1.2 Constraints \& Indexes



\- \*\*Unique index:\*\* `(companyId, serviceId)` to prevent exact duplicates per company.  

\- \*\*Search index:\*\* `(companyId, status, appointmentDate)` for scheduler views.  

\- \*\*Search index:\*\* `(companyId, assignedSiId, appointmentDate)` for SI job lists.  



\### 1.3 Notes



\- `status` must be mutated only by the \*\*Workflow Engine\*\*, never directly in ad-hoc code.

\- `invoiceEligible` is derived from:

&nbsp; - `status = Completed`

&nbsp; - `docketUploaded = true`

&nbsp; - `photosUploaded = true`

&nbsp; - `serialsValidated = true`

&nbsp; - No active blockers



---



\## 2. OrderStatusLog



Tracks every status change and important lifecycle events.



\### 2.1 Table: `OrderStatusLogs`



| Field             | Type      | Required | Description                                                           |

|-------------------|-----------|----------|-----------------------------------------------------------------------|

| id                | uuid      | yes      | Primary key.                                                          |

| companyId         | uuid      | yes      | FK → Companies.id.                                                    |

| orderId           | uuid      | yes      | FK → Orders.id.                                                       |

| fromStatus        | enum      | no       | Previous status (null if first).                                     |

| toStatus          | enum      | yes      | New status.                                                           |

| transitionReason  | string    | no       | Short explanation (e.g. `Customer not home`).                         |

| triggeredByUserId | uuid      | no       | FK → Users.id if transition done by staff.                            |

| triggeredBySiId   | uuid      | no       | FK → ServiceInstallers.id if done by SI.                              |

| source            | string    | yes      | `SIApp`, `AdminPortal`, `Scheduler`, `System`, `Parser`.              |

| metadataJson      | json      | no       | Extra structured details (GPS, device, IP, etc.).                     |

| createdAt         | datetime  | yes      | When the change happened.                                            |



\### 2.2 Notes



\- Append-only: no updates, no deletes.  

\- This table is the primary audit trail for order lifecycle.



---



\## 3. OrderReschedule



Stores reschedule requests and approvals from partners or customers.



\### 3.1 Table: `OrderReschedules`



| Field                 | Type      | Required | Description                                                                 |

|-----------------------|-----------|----------|-----------------------------------------------------------------------------|

| id                    | uuid      | yes      | Primary key.                                                                |

| companyId             | uuid      | yes      | FK → Companies.id.                                                          |

| orderId               | uuid      | yes      | FK → Orders.id.                                                             |

| requestedByUserId     | uuid      | no       | FK → Users.id if reschedule initiated by staff.                             |

| requestedBySiId       | uuid      | no       | FK → ServiceInstallers.id if requested by SI in field.                      |

| requestedBySource     | string    | yes      | `Customer`, `Partner`, `Internal`, `SI`.                                    |

| requestedAt           | datetime  | yes      | When reschedule was requested.                                              |

| originalDate          | date      | yes      | Original appointment date.                                                  |

| originalWindowFrom    | time      | yes      | Original start time.                                                        |

| originalWindowTo      | time      | yes      | Original end time.                                                          |

| newDate               | date      | yes      | Proposed new date.                                                          |

| newWindowFrom         | time      | yes      | Proposed new start.                                                         |

| newWindowTo           | time      | yes      | Proposed new end.                                                           |

| reason                | text      | yes      | Explanation (customer request, building issue, etc.).                       |

| approvalSource        | string    | no       | `EmailParser`, `Manual`, `AutoPolicy`.                                      |

| approvalEmailId       | uuid      | no       | FK → EmailMessage.id (TIME approval email).                                 |

| status                | enum      | yes      | `Pending`, `Approved`, `Rejected`, `Cancelled`.                             |

| statusChangedByUserId | uuid      | no       | Who approved/rejected.                                                      |

| statusChangedAt       | datetime  | no       | When status was updated.                                                    |

| createdAt             | datetime  | yes      | Created timestamp.                                                          |



\### 3.2 Notes



\- Only \*\*Approved\*\* reschedules should modify the `Orders` appointment fields.

\- For history, original and new slots are never overwritten here.



---



\## 4. OrderBlocker



Represents conditions that prevent the job from progressing.



\### 4.1 Table: `OrderBlockers`



| Field             | Type      | Required | Description                                                                |

|-------------------|-----------|----------|----------------------------------------------------------------------------|

| id                | uuid      | yes      | Primary key.                                                               |

| companyId         | uuid      | yes      | FK → Companies.id.                                                         |

| orderId           | uuid      | yes      | FK → Orders.id.                                                            |

| blockerType       | enum      | yes      | `CustomerNotHome`, `BuildingAccess`, `NetworkIssue`, `MaterialShortage`, `SIIssue`, etc. |

| description       | text      | yes      | Detailed explanation.                                                      |

| raisedBySiId      | uuid      | no       | FK → ServiceInstallers.id if raised in SI App.                             |

| raisedByUserId    | uuid      | no       | FK → Users.id if raised by admin/scheduler.                                |

| raisedAt          | datetime  | yes      | Blocker start time.                                                        |

| resolved          | boolean   | yes      | True when blocker is closed.                                               |

| resolvedAt        | datetime  | no       | When blocker was resolved.                                                 |

| resolvedByUserId  | uuid      | no       | Who resolved it.                                                           |

| resolutionNotes   | text      | no       | What was done to solve it.                                                 |

| createdAt         | datetime  | yes      | Created timestamp.                                                         |



\### 4.2 Notes



\- An active blocker may prevent:

&nbsp; - Status transition to Completed  

&nbsp; - Invoice eligibility  



---



\## 5. OrderDocket



Links orders to final dockets / completion documents.



\### 5.1 Table: `OrderDockets`



| Field           | Type      | Required | Description                                      |

|-----------------|-----------|----------|--------------------------------------------------|

| id              | uuid      | yes      | Primary key.                                     |

| companyId       | uuid      | yes      | FK → Companies.id.                               |

| orderId         | uuid      | yes      | FK → Orders.id.                                  |

| fileId          | uuid      | yes      | FK → Files.id (PDF/image of docket).             |

| uploadedBySiId  | uuid      | no       | FK → ServiceInstallers.id if SI uploaded.        |

| uploadedByUserId| uuid      | no       | FK → Users.id if office staff uploaded.          |

| uploadSource    | string    | yes      | `SIApp`, `AdminPortal`, `Import`.                |

| isFinal         | boolean   | yes      | True if this is the final accepted docket.       |

| notes           | text      | no       | Any comments.                                    |

| createdAt       | datetime  | yes      | Uploaded time.                                   |



\### 5.2 Notes



\- There may be multiple docket uploads; `isFinal = true` marks the official one.

\- `Orders.docketUploaded` should be true if at least one final docket exists.



---



\## 6. OrderMaterialUsage



Captures materials and devices used for each order.



\### 6.1 Table: `OrderMaterialUsage`



| Field             | Type      | Required | Description                                                             |

|-------------------|-----------|----------|-------------------------------------------------------------------------|

| id                | uuid      | yes      | Primary key.                                                            |

| companyId         | uuid      | yes      | FK → Companies.id.                                                      |

| orderId           | uuid      | yes      | FK → Orders.id.                                                         |

| materialId        | uuid      | yes      | FK → Materials.id.                                                      |

| serialisedItemId  | uuid      | no       | FK → SerialisedItems.id (ONU/router/etc.).                              |

| quantity          | decimal   | yes      | Quantity used (1 for serialised items).                                 |

| unitCost          | decimal   | no       | Cost at time of usage (copied from inventory for P\&L audit).            |

| totalCost         | decimal   | no       | `quantity \* unitCost`.                                                  |

| sourceLocationId  | uuid      | no       | StockLocation id from which material was taken (SI bag, warehouse, etc).|

| stockMovementId   | uuid      | no       | FK → StockMovements.id if linked to inventory movement.                 |

| recordedBySiId    | uuid      | no       | Who marked usage in field.                                              |

| recordedByUserId  | uuid      | no       | Who corrected/confirmed in office.                                      |

| recordedAt        | datetime  | yes      | When usage was recorded.                                                |

| notes             | text      | no       | Any special remarks.                                                    |



\### 6.2 Notes



\- For serialised items (ONU/router), `serialisedItemId` should always be present.

\- These records are the \*\*bridge\*\* between Orders, Inventory, and P\&L.



---



\## 7. OrderTag (Optional)



Allows flexible tagging of orders for reporting.



\### 7.1 Table: `OrderTags`



| Field      | Type   | Required | Description                                      |

|------------|--------|----------|--------------------------------------------------|

| id         | uuid   | yes      | Primary key.                                     |

| companyId  | uuid   | yes      | FK → Companies.id.                               |

| name       | string | yes      | Tag name (e.g. `VIP`, `TrialSite`, `Promo2025`). |

| color      | string | no       | Optional UI color hint.                          |

| createdAt  | datetime | yes    | Created timestamp.                               |



\### 7.2 Table: `OrderTagAssignments`



| Field      | Type   | Required | Description                  |

|------------|--------|----------|------------------------------|

| id         | uuid   | yes      | Primary key.                 |

| companyId  | uuid   | yes      | FK → Companies.id.           |

| orderId    | uuid   | yes      | FK → Orders.id.              |

| tagId      | uuid   | yes      | FK → OrderTags.id.           |

| createdAt  | datetime | yes    | Assigned timestamp.          |



---



\## 8. Building (Read-Only in Orders Context)



Orders reference buildings which are managed in another module, but for completeness:



\### 8.1 Table: `Buildings` (Reference)



| Field        | Type    | Required | Description                             |

|--------------|---------|----------|-----------------------------------------|

| id           | uuid    | yes      | Primary key.                            |

| companyId    | uuid    | yes      | FK → Companies.id (if company-scoped).  |

| name         | string  | yes      | Building name.                          |

| code         | string  | no       | Internal building code.                 |

| addressLine1 | string  | yes      | Address line 1.                         |

| addressLine2 | string  | no       | Address line 2.                         |

| city         | string  | yes      | City.                                   |

| state        | string  | yes      | State.                                  |

| postcode     | string  | yes      | Postcode.                               |

| latitude     | decimal | no       | GPS lat.                                |

| longitude    | decimal | no       | GPS lng.                                |



---



\## 9. Cross-Module Links (Summary)



\- `Order` → `Invoice` (Billing)  

\- `Order` → `PayrollItem` (Payroll)  

\- `Order` → `PnlDetailPerOrder` (P\&L)  

\- `Order` → `Schedule` (Scheduler)  

\- `Order` → `OrderMaterialUsage` → `StockMovement` (Inventory)  

\- `Order` → `OrderDocket` → `File` (Documents)  

\- `Order` → `OrderStatusLog` (Audit)  

\- `Order` → `OrderReschedule` (Rescheduling history)  

\- `Order` → `OrderBlocker` (Blockers)  



These links are described in more detail in:



\- `relationships/orders\_relationships.md`  

\- `relationships/cross\_module\_relationships.md`



---



\# End of Orders Entities



