\# Payroll Entities  

CephasOps – Payroll Domain Data Model  

Version 1.0



This file defines entities for \*\*Service Installer \& staff payroll\*\*:



\- PayrollPeriod

\- PayrollRun

\- PayrollItem

\- SiRatePlan

\- SiRatePlanRule

\- ServiceInstaller (summary for payroll side)



All are \*\*company-scoped\*\* via `companyId`.



---



\## 1. PayrollPeriod



Represents a monthly or custom pay period.



\### 1.1 Table: `PayrollPeriods`



| Field          | Type     | Required | Description                     |

|----------------|----------|----------|---------------------------------|

| id             | uuid     | yes      | Primary key.                    |

| companyId      | uuid     | yes      | FK → Companies.id.              |

| label          | string   | yes      | e.g. `2025-01`, `2025-02`.     |

| dateFrom       | date     | yes      | Start date.                     |

| dateTo         | date     | yes      | End date.                       |

| status         | enum     | yes      | `Open`, `Locked`.               |

| createdAt      | datetime | yes      | Created timestamp.              |



---



\## 2. PayrollRun



A single calculation run for a period.



\### 2.1 Table: `PayrollRuns`



| Field           | Type     | Required | Description                           |

|-----------------|----------|----------|---------------------------------------|

| id              | uuid     | yes      | Primary key.                          |

| companyId       | uuid     | yes      | FK → Companies.id.                    |

| payrollPeriodId | uuid     | yes      | FK → PayrollPeriods.id.               |

| runNo           | int      | yes      | Incrementing per period (1,2,3...).   |

| status          | enum     | yes      | `Draft`, `Finalised`, `Paid`.         |

| totalGross      | decimal  | yes      | Sum of gross pay for all SIs/staff.   |

| totalNet        | decimal  | yes      | Sum after adjustments.                |

| notes           | text     | no       | Finance notes.                        |

| createdByUserId | uuid     | yes      | Who triggered.                        |

| createdAt       | datetime | yes      | Created timestamp.                    |

| finalisedAt     | datetime | no       | When locked.                          |

| paidAt          | datetime | no       | When paid.                            |



---



\## 3. PayrollItem



One row per SI per job or per earning/adjustment.



\### 3.1 Table: `PayrollItems`



| Field            | Type     | Required | Description                                        |

|------------------|----------|----------|----------------------------------------------------|

| id               | uuid     | yes      | Primary key.                                       |

| companyId        | uuid     | yes      | FK → Companies.id.                                 |

| payrollRunId     | uuid     | yes      | FK → PayrollRuns.id.                               |

| payrollPeriodId  | uuid     | yes      | FK → PayrollPeriods.id.                            |

| serviceInstallerId| uuid    | yes      | FK → ServiceInstallers.id.                         |

| orderId          | uuid     | no       | FK → Orders.id (for job-based pay).                |

| itemType         | enum     | yes      | `JobEarning`, `Bonus`, `Penalty`, `Adjustment`.    |

| description      | string   | yes      | Summary of line.                                   |

| quantity         | decimal  | yes      | e.g. 1 job, number of days, etc.                   |

| rate             | decimal  | yes      | Rate per unit.                                     |

| grossAmount      | decimal  | yes      | `quantity \* rate`.                                 |

| adjustmentAmount | decimal  | yes      | Negative for penalty, positive for bonus.          |

| netAmount        | decimal  | yes      | `grossAmount + adjustmentAmount`.                  |

| kpiCategory      | string   | no       | KPI type for reporting.                            |

| kpiResult        | enum     | no       | `OnTime`, `Late`, `Rework`, etc.                   |

| createdAt        | datetime | yes      | Created timestamp.                                 |



---



\## 4. SiRatePlan



Defines how much to pay for jobs.



\### 4.1 Table: `SiRatePlans`



| Field         | Type     | Required | Description                                      |

|---------------|----------|----------|--------------------------------------------------|

| id            | uuid     | yes      | Primary key.                                     |

| companyId     | uuid     | yes      | FK → Companies.id.                               |

| name          | string   | yes      | E.g. `TIME\_activation\_senior`, `Junior\_plan`.    |

| description   | string   | no       | Detail explanation.                              |

| isDefault     | boolean  | yes      | Default for new SIs.                             |

| isActive      | boolean  | yes      | Active/inactive.                                 |

| createdAt     | datetime | yes      | Created timestamp.                               |



\### 4.2 Table: `SiRatePlanRules`



| Field           | Type     | Required | Description                                          |

|-----------------|----------|----------|------------------------------------------------------|

| id              | uuid     | yes      | Primary key.                                         |

| companyId       | uuid     | yes      | FK → Companies.id.                                   |

| siRatePlanId    | uuid     | yes      | FK → SiRatePlans.id.                                 |

| orderTypeId     | uuid     | yes      | FK → OrderTypes.id.                                  |

| kpiCategory     | string   | no       | Optional grouping.                                   |

| baseRate        | decimal  | yes      | Pay per completed job on-time.                       |

| latePenalty     | decimal  | yes      | Negative amount or percentage.                       |

| reworkPenalty   | decimal  | yes      | For repeated visits.                                 |

| bonusOnTime     | decimal  | yes      | Bonus for solid performance (optional).              |

| effectiveFrom   | date     | no       | Start date.                                          |

| effectiveTo     | date     | no       | End date.                                            |



---



\## 5. ServiceInstaller (Payroll view)



Defined more fully in another module, but here for reference:



\### 5.1 Table: `ServiceInstallers` (relevant fields)



| Field        | Type   | Required | Description              |

|--------------|--------|----------|--------------------------|

| id           | uuid   | yes      | Primary key.             |

| companyId    | uuid   | yes      | FK → Companies.id.       |

| name         | string | yes      | Installer name.          |

| level        | enum   | no       | `Junior`, `Senior`, etc. |

| ratePlanId   | uuid   | no       | FK → SiRatePlans.id.     |



---



\## 6. Cross-Module Links



\- `PayrollItem.orderId` ↔ `Orders`  

\- `PayrollItem` feeds P\&L (labour cost)  

\- `SiRatePlanRules` used when job is completed and payroll line is generated  



---



\# End of Payroll Entities



