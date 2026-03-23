\# README.md – CephasOps Project Overview



\*\*Project Name:\*\* CephasOps  

\*\*Project Type:\*\* Multi-Tenant SaaS Operations Platform  

\*\*Architecture:\*\* Multi-tenant SaaS with per-company data isolation, role-based module enablement, and department-scoped operations.



\## Vision



To build the industry-leading, all-in-one operational platform for service installer operations, starting with ISPs and telco partners (TIME, Digi, Celcom, etc.), and expanding to other field-service–driven businesses.



\## Core Problem to Solve



Service installers and contractors in the telecom and utilities space manage highly complex operations using disconnected tools (Excel, WhatsApp, email, ad-hoc trackers). This causes:



\- Inefficient job assignment and follow-up  

\- Lost or untracked materials and assets  

\- Poor visibility on installer performance and customer experience  

\- Slow, error-prone billing and payout reconciliation  



\*\*CephasOps\*\* solves this by:



\- Automating the full order lifecycle from email → ParseSession → Approve → Order  

\- Providing rigorous asset and material control across warehouse → installer → customer  

\- Delivering real-time operational and financial insights for both management and finance teams  



\## Core Pillars



\### 1. Multi-Company / Multi-Tenant Architecture

CephasOps is architected as a multi-tenant SaaS platform with per-company data isolation.

A secure platform that can serve:



\- Multiple legal entities (e.g. Cephas, Menorah, Kingsman)  

\- Multiple branches per company  

\- Multiple upstream partners (TIME, Digi, Celcom, etc.)



All data is always scoped by `companyId` (and optionally `branchId`), with strict separation between tenants.



\### 2. Self-Service Configuration



Customers (operators/contractors) can configure their own:



\- Companies, branches, and building profiles  

\- Rate profiles (installer payout, client billing, materials, travel, etc.)  

\- Document templates (work orders, dockets, invoices, emails, WhatsApp messages)  

\- Workflows and status codes  

\- Email parser rules and partner mappings (at a business level)  



…without needing developer intervention.



\### 3. End-to-End Workflow



CephasOps manages the \*\*entire\*\* operations lifecycle:



1\. \*\*Order Intake\*\*

&nbsp;  - Email ingestion and parsing (Excel / PDF / HTML / human replies)

&nbsp;  - Partner identification and mapping to the correct company

&nbsp;  - Duplicate detection and ParseSession approvals



2\. \*\*Job \& Field Operations\*\*

&nbsp;  - Assignment to installers / subcontractors  

&nbsp;  - Status tracking (Assigned → On the Way → Met Customer → Completed, etc.)  

&nbsp;  - Material issuance and consumption at job level  



3\. \*\*Materials \& Asset Control\*\*

&nbsp;  - Warehouse → installer → customer tracking  

&nbsp;  - Serial number management and variance checks  

&nbsp;  - Missing serials and reconciliation reports  



4\. \*\*Invoicing \& Finance\*\*

&nbsp;  - Rate profile–driven billing and installer payouts  

&nbsp;  - Exportable statements for finance and external accounting  

&nbsp;  - Support for multiple companies and partner-specific formats  



5\. \*\*Analytics \& KPI\*\*

&nbsp;  - SLA tracking (installations, assurance, modifications)  

&nbsp;  - Installer performance metrics  

&nbsp;  - Company-level and portfolio-level dashboards  



\### 4. Security First



\- Role-based access control across companies and modules  

\- Device-level access via controlled activation (for installer / field apps)  

\- Audit logging for sensitive operations:

&nbsp; - Order state changes

&nbsp; - Parser approvals

&nbsp; - Billing and payout edits  



---



\## Technology Stack



> The technology stack for CephasOps is defined in the Architecture and Backend/Frontend modules.  

> This README focuses on \*\*product and operational scope\*\*. For implementation details, please refer to:



\- `docs/01\_architecture/ARCHITECTURE\_OVERVIEW.md`

\- `docs/02\_modules/\*` (backend modules)

\- `docs/07\_frontend/\*` (UI \& frontend modules)



---



\## Project Package



This repository contains:



\- The full Product Requirements Document (PRD)  

\- Epic and user stories for all major modules  

\- Detailed module breakdowns:

&nbsp; - Email Parser \& ParseSession

&nbsp; - Orders \& Workflow

&nbsp; - Multi-Company \& Rates

&nbsp; - Materials \& Serial Tracking

&nbsp; - Billing \& Payouts

&nbsp; - Global Settings \& Feature Flags

&nbsp; - Frontend UI \& Storybook guidelines  



\*\*Start with the PRD\*\* for a complete business and functional overview before diving into architecture or implementation details.



