\# SPLITTERS\_MANAGEMENT\_MODULE.md

Splitters \& Ports Management – Full Backend Specification



---



\## 1. Purpose



This module tracks network splitters and their ports, enforcing TIME’s requirements



\- Each building has registered splitters

\- Ports are tracked per order

\- Standby ports are respected

\- Splitter usage required before completing jobs



---



\## 2. Data Model



\### 2.1 Splitter



Fields



\- `Id`

\- `CompanyId`

\- `BuildingId`

\- `Name` (e.g. B-1, MDF-1)

\- `SplitterType` (enum; 18, 116, 132, etc.)

\- `TotalPorts` (int)

\- `StandbyPortNumber` (nullable; e.g. 32)

\- `LocationDescription` (text riser level, MDF room, etc.)

\- `IsActive`

\- `CreatedAt`, `CreatedByUserId`

\- `UpdatedAt`, `UpdatedByUserId`



Indexes



\- `(CompanyId, BuildingId)`

\- `(CompanyId, Name)`



---



\### 2.2 SplitterPort



Fields



\- `Id`

\- `CompanyId`

\- `SplitterId`

\- `PortNumber` (1..TotalPorts)

\- `IsStandbyPort` (bool)

\- `Status` (enum Available, Reserved, Used, Faulty)

\- `CurrentOrderId` (nullable)

\- `AssignedAt` (nullable)

\- `AssignedByUserId` (nullable)

\- `LockedAt` (nullable – when permanently used)

\- `MetadataJson` (optional)



Unique constraint



\- `(SplitterId, PortNumber)`



---



\### 2.3 SplitterUsageLog



Tracks historical usage.



Fields



\- `Id`

\- `CompanyId`

\- `OrderId`

\- `BuildingId`

\- `SplitterId`

\- `PortNumber`

\- `ServiceId` (from order)

\- `Status` (Assigned, Released)

\- `Timestamp`

\- `ChangedByUserId`

\- `Notes`



Indexes



\- `(CompanyId, OrderId)`

\- `(CompanyId, SplitterId, PortNumber)`



---



\## 3. Integration with Orders



`OrderSplitterAllocation` (already documented under Orders) links Orders to Splitter \& Port. This module is the master data; Orders stores the relationship.



Workflow



\- When SI completes job in SI App (MetCustomer → OrderCompleted transition)

&nbsp; - SI supplies `splitterId` + `portNumber`.

&nbsp; - WorkflowEngine validates using Splitter + SplitterPort data (see rules below).

&nbsp; - If valid

&nbsp;   - Mark SplitterPort

&nbsp;     - `Status = Used`

&nbsp;     - `CurrentOrderId = orderId`

&nbsp;     - `LockedAt = now`

&nbsp;   - Create SplitterUsageLog.



---



\## 4. Business Rules



1\. Port must exist

&nbsp;  - `PortNumber` between 1 and `TotalPorts`.

2\. Port must belong to building

&nbsp;  - Splitter.BuildingId = Order.BuildingId.

3\. Port availability

&nbsp;  - `Status` must be `Available` before assignment.

4\. Standby port rule

&nbsp;  - If port is marked `IsStandbyPort`

&nbsp;    - Require approval

&nbsp;      - `StandbyOverrideApproved = true`

&nbsp;      - `ApprovalAttachmentId` provided (DocumentTemplatesFile integration).

5\. Uniqueness

&nbsp;  - A port once `Used` cannot be reassigned unless explicitly released (for rare reconfiguration).



---



\## 5. Services



\### 5.1 SplitterService



\- CRUD for Splitter

\- Auto-generate SplitterPort records for `TotalPorts` on creation

\- Manage standby port settings



\### 5.2 SplitterPortService



\- Query available ports for building

\- Reserve a port for an upcoming job (future use)

\- Mark port as used when job completed

\- Release port (if rollback or migration)



\### 5.3 SplitterUsageService



\- Log usage

\- Provide history for audits.



---





