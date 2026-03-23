\# KPI\_PROFILE\_MODULE.md

Configurable KPI Rules for Scheduler \& Payroll – Full Backend Specification



---



\## 1. Purpose



The KPI Profile module stores \*\*configurable KPI rules\*\* used by:



\- \*\*Scheduler\*\* – to validate job durations, lateness, and SLA

\- \*\*SI App \& Orders\*\* – to measure actual performance vs target

\- \*\*Payroll\*\* – to apply penalties/bonuses based on KPI results



It replaces hard-coded KPI values with \*\*data-driven configuration\*\*.



---



\## 2. Data Model



\### 2.1 KpiProfile



Represents a set of KPI rules for a specific context.



Fields:



\- `Id`

\- `CompanyId`

\- `Name` (e.g. “TIME Prelaid KPI”, “TIME SDU KPI”)

\- `PartnerId` (nullable)

\- `OrderType` (Activation, Assurance, FTTR, FTTC, SDU, RDFPole, etc.)

\- `BuildingTypeId` (nullable, FK to BuildingType)

\- `MaxJobDurationMinutes` (int; target from Assigned → OrderCompleted)

\- `DocketKpiMinutes` (int; target time from OrderCompleted → DocketsReceived)

\- `MaxReschedulesAllowed` (int; optional)

\- `IsDefault` (one default profile per `(CompanyId, OrderType)` if no more specific match)

\- `EffectiveFrom`, `EffectiveTo` (optional; allow future changes)

\- `CreatedAt`, `CreatedByUserId`

\- `UpdatedAt`, `UpdatedByUserId`



Indexes:



\- `(CompanyId, PartnerId, OrderType, BuildingTypeId, EffectiveFrom)`

\- `(CompanyId, IsDefault, OrderType)`



---



\## 3. KPI Resolution Logic



When evaluating a job, the system resolves KpiProfile by:



1\. Match by `CompanyId`, `PartnerId`, `OrderType`, `BuildingTypeId` (most specific)

2\. Fallback to `CompanyId`, `PartnerId`, `OrderType`

3\. Fallback to `CompanyId`, `OrderType` where `IsDefault = true`

4\. If none found, fallback to \*\*GlobalSetting\*\* values.



---



\## 4. Integration Points



\### 4.1 Scheduler



\- When job is completed, compute:

&nbsp; - `ActualJobMinutes = StatusOrderCompletedAt - StatusAssignedAt`

\- Compare against `MaxJobDurationMinutes`:

&nbsp; - Result: `OnTime`, `Late`, `ExceededSla`

\- Store KPI result in:

&nbsp; - `Order.KpiResult` or separate KPI record.



\### 4.2 SI App / Orders



\- Used to determine whether SI met the KPI for that job.

\- Can display KPI status in SI App job history.



\### 4.3 Payroll



\- PayrollCalculationService can read KPI result and:

&nbsp; - Apply bonus if `OnTime`

&nbsp; - Apply penalty or reduced rate if `Late` or `ExceededSla`

\- KPI thresholds come from KpiProfile instead of hard-coded logic.



---



\## 5. Service



\### 5.1 KpiProfileService



Methods:



\- `Task<KpiProfile?> GetEffectiveProfileAsync(companyId, partnerId, orderType, buildingTypeId, DateTime jobDate)`

\- `Task<IEnumerable<KpiProfile>> ListProfilesAsync(companyId, filters...)`

\- `Task<KpiProfile> CreateOrUpdateProfileAsync(...)`



\### 5.2 KpiEvaluationService



Methods:



\- `Task<KpiEvaluationResult> EvaluateOrderAsync(orderId)`

&nbsp; - Uses Order timestamps + KpiProfile.

&nbsp; - Returns: `KpiResult` enum + metrics (actual minutes, target, delta).



---



\## 6. API



Base path: `/api/kpi-profiles`



\- `GET /api/kpi-profiles`

&nbsp; - Filters: `companyId`, `partnerId`, `orderType`, `buildingTypeId`

\- `GET /api/kpi-profiles/{id}`

\- `POST /api/kpi-profiles`

\- `PUT /api/kpi-profiles/{id}`

\- `POST /api/kpi-profiles/{id}/set-default`



Optional:



\- `GET /api/kpi/evaluate-order/{orderId}` (for testing/debugging)



---



\## 7. Security



\- Only Admin roles with `ManageKpiProfiles` can create/update.

\- Read access for:

&nbsp; - Ops

&nbsp; - Finance (for payroll)

&nbsp; - Reporting users





