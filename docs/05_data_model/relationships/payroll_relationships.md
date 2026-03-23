\### `payroll\_relationships.md`



```md

\# Payroll – Relationships  

CephasOps Data Model – Payroll Relationships  

Version 1.0



This document describes how payroll entities relate to Orders, SIs and P\&L.



---



\## 1. High-Level Overview



Core relationships:



\- Company 1—\* PayrollPeriods

\- PayrollPeriod 1—\* PayrollRuns

\- PayrollRun 1—\* PayrollItems

\- ServiceInstaller 1—\* PayrollItems

\- Order 1—\* PayrollItems (job-based earnings)

\- SiRatePlan 1—\* SiRatePlanRules



---



\## 2. ERD (Payroll Scope)



```mermaid

erDiagram

&nbsp;   Company ||--o{ PayrollPeriod : defines

&nbsp;   PayrollPeriod ||--o{ PayrollRun : has

&nbsp;   PayrollRun ||--o{ PayrollItem : includes

&nbsp;   ServiceInstaller ||--o{ PayrollItem : earns

&nbsp;   Order ||--o{ PayrollItem : source\_for

&nbsp;   SiRatePlan ||--o{ SiRatePlanRule : configures

3\. PayrollPeriod \& PayrollRun

PayrollPeriods.id → PayrollRuns.payrollPeriodId (1–\*)



Each period can have multiple runs (draft, recomputation, etc.).



PayrollRuns.status = Finalised or Paid marks them immutable.



4\. PayrollRun \& PayrollItem

PayrollRuns.id → PayrollItems.payrollRunId (1–\*)



PayrollPeriods.id → PayrollItems.payrollPeriodId (redundant but helps queries).



Business rule:



Once a PayrollRun is marked Finalised, PayrollItems must not be changed.



5\. PayrollItem ↔ Orders \& SIs

PayrollItems.serviceInstallerId → ServiceInstallers.id



PayrollItems.orderId → Orders.id (optional for non-job items)



This bridge:



Connects job-level SI costs to:



Orders (operational view)



P\&L (financial view)



6\. Rate Plans \& Rules

SiRatePlans.id → SiRatePlanRules.siRatePlanId



ServiceInstallers.ratePlanId → SiRatePlans.id



Use case:



Job completed.



Determine SI → find their ratePlanId.



Find matching SiRatePlanRule for orderType.



Compute PayrollItem from rule and KPI result.



7\. P\&L Link

PayrollItems.netAmount aggregated into:



PnlFacts.directLabourCost



PnlOrderDetails.labourCost



Described in pnl\_relationships.md.



End of Payroll Relationships

