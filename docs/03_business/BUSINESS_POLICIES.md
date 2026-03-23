his is a business rules document, not technical.

It defines what must happen, what cannot happen, and what requires approval across the entire CephasOps ecosystem.



You can paste this DIRECTLY into your repo.



BUSINESS\_POLICIES.md



CephasOps – Business Policies \& Operational Rules

Version 1.0



This document outlines the business-level rules that guide operational behaviour across all CephasOps companies and verticals.



These rules must be enforced through:



Workflow Engine



Email Parser



Scheduler



Inventory \& RMA



Billing



Payroll



P\&L



Admin Portal policies



SI Mobile App behaviour



No coding logic is defined here — only business expectations.



1\. Multi-Company Governance Policies



Each company operates independently:



Cephas Sdn. Bhd



Cephas Trading \& Services



Kingsman Classic Services



Menorah Travel \& Tours



Data must NEVER mix between companies.



Directors may see:



Cross-company P\&L



Consolidated KPIs

But cannot perform operations on other companies.



Staff roles apply per-company:

Example:



A Scheduler in Cephas cannot schedule for Menorah.



Company-switching must be explicit via UI.



2\. Order Management Policies (ISP Operations)

2.1 Order Creation Rules



Orders originate from:



Email Parser



Excel attachment from TIME



Assurance emails (TTKT/AWO/LinkDown)



Manual entry (admin)



Every new Order must have:



Partner



Order Type



Building



Customer details



Appointment date



Duplicate prevention:



Service ID + Partner must be unique per active order.



2.2 Status \& Workflow Rules



No direct manual editing of Order.status.



All transitions must follow the lifecycle:

Pending → Assigned → OTW → MetCustomer → Completed → DocketsUploaded → ReadyForInvoice → Invoiced → Completed



Blockers must be documented:



Customer not home



Building access



SI issue



Network issue



Only when resolved, order may continue.



No order may skip required states:



Completed cannot occur before MetCustomer.



Invoiced cannot occur before DocketsUploaded.



2.3 Reschedule Policies



All reschedules must have documented reason.



TIME partner email approval is required unless:



Customer requests reschedule >24 hours before.



Parser must detect approved emails.



System rejects rescheduling if SI is unavailable.



3\. Scheduling \& SI Assignment Policies



Schedulers may assign jobs only for their company.



SI assignment must respect:



Availability



Daily capacity



Skill level (e.g., FTTR required senior SI)



SI cannot be double-booked.



SI Leave must override scheduling.



Reassignment requires reason.



4\. Service Installer (SI) Behaviour Policies

4.1 Mandatory Actions



SI must follow the sequence:



Accept job



Start On-The-Way



Met Customer



Perform Installation



Upload Photos



Scan Serials



Complete Job



4.2 Mandatory Evidence



Every completion must include:



Installation photos



ONU/Router serials



Fibre serial where applicable



Splitter port photo



Speedtest (optional but recommended)



4.3 Behaviour Expectations



Must arrive within scheduled window.



Must notify if running late.



Must maintain customer service standards.



Must not modify partner equipment without authorisation.



5\. Inventory \& RMA Policies

5.1 Material Issuance



All materials must come from official stock.



Warehouse → SI bag movements are mandatory before SI can use them.



Serialised items MUST be scanned before and after job.



5.2 Splitter \& Port Rules



A port can be used ONLY ONCE unless:



Verified re-installation



Admin override



Standby ports (e.g. port 32) require approval.



Splitter must match customer building.



5.3 Faulty Equipment \& RMA



Faulty units must be returned to RMA bin.



RMA ticket required for each faulty serial.



MRA PDF from partner must be uploaded.



SI cannot throw away equipment.



6\. Billing \& Finance Policies

6.1 Invoice Eligibility



Order must be completed and validated.



Dockets must be uploaded.



Materials \& serials must be validated.



Assurance jobs follow partner-specific billing rules.



6.2 Invoice Finality



After e-Invoice submission:



Invoice becomes read-only.



Corrections require:



Credit note



New invoice



6.3 Payment Recording



Payments must include:



Amount



Date



Reference No



Partial payments allowed.



Overdue tracking must alert finance.



7\. Payroll Policies (Service Installers)



SI earnings depend on:



Order type



SI level (Junior/Senior/Subcon)



KPI performance



Reworks \& penalties



Payroll runs must be locked before payment.



Locked payroll cannot be edited.



SI can view:



Job list



Earnings per job



Monthly earnings summary



SI cannot see other SIs’ earnings.



8\. P\&L Accounting Policies



P\&L is based only on:



Invoices



Materials cost



SI labour cost



Overheads



P\&L is recalculable:



Daily



On-demand



Overheads may be allocated to:



Cost centres



Verticals



Companies



Directors have read-only P\&L access across companies.



9\. Data \& Compliance Policies



All financial documents must be stored with metadata:



Invoice



Docket



RMA PDF



Agreement



Personal data (IC number, phone number, address) must follow PDPA.



Email attachments must be parsed \& stored securely.



Logs must be immutable.



10\. Approval Policies

Require approval:



Time-sensitive reschedules



Standby port usage



Modification/Relocation that changes building



Price override or manual invoice



High-value RMA



Payroll adjustment



Auto-approved:



Parser-approved email reschedule



Low-risk assurance jobs



Standard RMA returned with matching serial



11\. Exception Policies



System must record exceptions:



Partner mistake



Building issue



SI misconduct



Customer no-show



Exceptions affect:



KPIs



Payroll



P\&L



12\. Change Control Policies



Any change to:



Workflow



Parser structure



Billing rules



Pay rates



Materials

MUST go through Configuration / Settings (not code).



Code changes affecting workflow require:



Documentation update



Team approval



Version bump



13\. Service Level Agreements (SLA)

Internal SLA Targets:



Activation completion: same day unless customer delays



Assurance response: within 2–4 hours



RMA replacement: same job session if stock available



Invoice submission: within 24 hours of docket



14\. Kingsman \& Menorah Policies (Future)



These verticals follow their own rules, but:



Still company-scoped



Still use stock, billing, payroll



Custom modules will attach to the same core system



Separate storybooks may be added later.

