\# P\&L (Profit \& Loss) Entities  

CephasOps – P\&L Domain Data Model  

Version 1.0



This file defines:



\- PnlFact

\- PnlDetailPerOrder

\- OverheadEntry



All \*\*company-scoped\*\*.



---



\## 1. PnlFact



Aggregated P\&L per period \& slice.



\### 1.1 Table: `PnlFacts`



| Field              | Type     | Required | Description                                               |

|--------------------|----------|----------|-----------------------------------------------------------|

| id                 | uuid     | yes      | Primary key.                                              |

| companyId          | uuid     | yes      | FK → Companies.id.                                        |

| period             | string   | yes      | E.g. `2025-01`, `2025-Q1`, `2025`.                        |

| partnerId          | uuid     | no       | Partner if aggregated by partner.                         |

| vertical           | string   | no       | `ISP`, `Barbershop`, `Travel`.                            |

| costCentreId       | uuid     | no       | FK → CostCentres.id.                                      |

| orderTypeId        | uuid     | no       | FK → OrderTypes.id.                                       |

| revenueAmount      | decimal  | yes      | Total revenue.                                            |

| directMaterialCost | decimal  | yes      | Material COGS.                                            |

| directLabourCost   | decimal  | yes      | SI payroll.                                               |

| indirectCost       | decimal  | yes      | Allocated overheads.                                      |

| grossProfit        | decimal  | yes      | `revenueAmount - directMaterialCost - directLabourCost`.  |

| netProfit          | decimal  | yes      | `grossProfit - indirectCost`.                             |

| jobsCount          | int      | yes      | Total jobs included.                                      |

| assuranceJobsCount | int      | yes      | Assurance jobs count.                                     |

| reschedulesCount   | int      | yes      | Reschedules count.                                        |

| createdAt          | datetime | yes      | Created timestamp.                                        |

| lastRecalculatedAt | datetime | yes      | Last rebuild time.                                        |



---



\## 2. PnlDetailPerOrder



Fine-grained per-order profitability.



\### 2.1 Table: `PnlOrderDetails`



| Field            | Type     | Required | Description                                  |

|------------------|----------|----------|----------------------------------------------|

| id               | uuid     | yes      | Primary key.                                 |

| companyId        | uuid     | yes      | FK → Companies.id.                           |

| orderId          | uuid     | yes      | FK → Orders.id.                              |

| period           | string   | yes      | E.g. `2025-01`.                              |

| partnerId        | uuid     | yes      | FK → Partners.id.                            |

| orderTypeId      | uuid     | yes      | FK → OrderTypes.id.                          |

| revenueAmount    | decimal  | yes      | Invoiced amount for this order.             |

| materialCost     | decimal  | yes      | Material cost charged to job.               |

| labourCost       | decimal  | yes      | SI payroll cost for job.                    |

| overheadAllocated| decimal  | yes      | Allocated overhead portion.                 |

| profitForOrder   | decimal  | yes      | `revenueAmount - materialCost - labourCost - overheadAllocated`. |

| kpiResult        | enum     | no       | `OnTime`, `Late`, `Rework`, etc.            |

| rescheduleCount  | int      | yes      | Number of reschedules.                      |

| createdAt        | datetime | yes      | Created timestamp.                           |



---



\## 3. OverheadEntry



Manual or imported overheads for allocation.



\### 3.1 Table: `OverheadEntries`



| Field          | Type     | Required | Description                                  |

|----------------|----------|----------|----------------------------------------------|

| id             | uuid     | yes      | Primary key.                                 |

| companyId      | uuid     | yes      | FK → Companies.id.                           |

| period         | string   | yes      | Period like `2025-01`.                       |

| costCentreId   | uuid     | no       | FK → CostCentres.id.                         |

| vertical       | string   | no       | Optional vertical.                           |

| amount         | decimal  | yes      | Amount of overhead.                          |

| description    | string   | yes      | Rent, salary, utilities, etc.                |

| source         | enum     | yes      | `Manual`, `Imported`, `System`.              |

| createdByUserId| uuid     | yes      | FK → Users.id.                               |

| createdAt      | datetime | yes      | Created timestamp.                           |



---



\## 4. Cross-Module Links



\- `PnlOrderDetails.orderId` ↔ Orders  

\- `PnlFacts` aggregated from Invoice, OrderMaterialUsage, PayrollItems, OverheadEntries  



---



\# End of P\&L Entities



