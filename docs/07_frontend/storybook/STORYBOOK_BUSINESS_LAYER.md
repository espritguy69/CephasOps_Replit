Below is your full, final, production-ready STORYBOOK\_BUSINESS\_LAYER.md file.



📌 Place this file here:

cephasops/docs/03\_business/STORYBOOK\_BUSINESS\_LAYER.md



This is the master storybook that sits above all modules and describes how the business actually operates in real life.

It ties together Parser → Orders → Scheduler → SI App → Inventory → Billing → Payroll → P\&L → Directors.



It gives Cursor AI the narrative of the system so it can generate correct behaviour.



STORYBOOK\_BUSINESS\_LAYER.md



CephasOps – Storybook (Business Layer)

Version 1.0 (Final)



This storybook explains how the business runs across Cephas Sdn. Bhd, Cephas Trading, Kingsman Classic Services, and Menorah Travel.

It is the connecting layer between everyday operations and the modules that support them.



This is not a technical document.

It is the business behaviour narrative required for building a real-world system.



1\. Purpose of the Storybook



The Storybook defines:



How the business behaves



How users interact with the system



What real-world scenarios the system must support



What rules, workflows, approvals, and outcomes matter



How people, partners, and documents flow across the organisation



It gives context for:



Parser



Order Lifecycle



Workflow Engine



Scheduler



Inventory



Billing



Payroll



P\&L



Directors’ dashboards



Think of it as the “movie script” for the entire CephasOps system.



2\. Characters (Actors)

External Parties



TIME (Main ISP partner)



Celcom / Digi / U Mobile



Customers (Residential / Commercial)



Building Management



Vendors (Material suppliers)



RMA Partners



Banks / Payment channels



Internal Roles



Admin



Scheduler



Service Installer (SI)



Warehouse / Inventory Team



Finance



Payroll Manager



Directors



Multi-company owners



Each interacts with the system differently.



3\. High-Level Story: The Daily Flow of CephasOps



Here is the entire business at a glance:



Email from TIME → Parser

&nbsp;                  ↓

&nbsp;           ParsedOrder

&nbsp;                  ↓

&nbsp;              Validation

&nbsp;                  ↓

&nbsp;                Order

&nbsp;                  ↓

&nbsp;           Scheduler assigns SI

&nbsp;                  ↓

&nbsp;           SI performs job (App)

&nbsp;                  ↓

&nbsp;       Photos + Serials + Docket

&nbsp;                  ↓

&nbsp;          Inventory adjustments

&nbsp;                  ↓

&nbsp;     Invoice eligibility confirmed

&nbsp;                  ↓

&nbsp;           Billing generates invoice

&nbsp;                  ↓

&nbsp;         MyInvois e-Invoice submitted

&nbsp;                  ↓

&nbsp;              Payment recorded

&nbsp;                  ↓

&nbsp;             Payroll calculated

&nbsp;                  ↓

&nbsp;              P\&L aggregated

&nbsp;                  ↓

&nbsp;        Director sees full picture





This story repeats every day across all companies.



4\. Story Sequence (Narrative)



Below is the long-form story that defines the behaviour of all modules.



4.1 STORY: Emails Arrive \& Parser Starts the Day



Every day begins with emails:



TIME Activation Excel files



TTKT / AWO Assurance alerts



Customer change-of-date emails



Reschedule approvals



RMA confirmations



Partner disputes



The Email Pipeline:



Downloads email



Extracts attachments



Passes to Parser



Parser matches template to partner + building



Extracts structured data



Produces ParsedOrder draft



Admins review parsed output.



If correct → Approved → becomes an Order.

If wrong → Rejected → Admin adjusts manually.



4.2 STORY: Order Comes to Life (Order Lifecycle)



An approved parsed order becomes:



Order {

&nbsp; partner: TIME,

&nbsp; orderType: Activation,

&nbsp; building: Sri Penara,

&nbsp; appointment: 10am–12pm,

&nbsp; serviceId: <value>,

&nbsp; customer: <info>

}





Business rules automatically run:



Building validation



Duplicate ServiceID check



Port validation



Material recommendations



If everything is ok → Scheduler handles it.

If missing details → Admin fixes.



4.3 STORY: Scheduler Organises the Day



Scheduler sees all jobs:



Assigned

Pending

RescheduleRequested

Blocked





Scheduler assigns jobs based on:



SI availability



SI skill



Distance



Job complexity



Partner SLA



SI receives job instantly in their mobile app.



If SI is too busy:

→ Scheduler reassigns

→ Or SI decline (with reason)



4.4 STORY: SI Performs Job (Field Operations)



SI performs the job with the SI App:



Start OTW



Arrive at unit (Met Customer)



Take photos (before/after)



Scan ONU / Router / Faceplate / Fibre



Test speed



Upload evidence



Complete job



App works offline in low-signal buildings.

Everything syncs when connection returns.



If job cannot proceed:

SI raises a Blocker:



Customer not home



Building access



Material shortage



Network issue



Wrong appointment info



Scheduler reviews blocker and resolves.



4.5 STORY: Inventory Adjusts Automatically



When SI finishes job:



Serial numbers reduce from SI inventory



Replacement serials auto-transfer



Faulty ONU returned to warehouse



RMA ticket created



Stock movements are automatic based on:



SI bag → Customer



SI bag → RMA



Warehouse → SI



Cephas Trading handles bulk inventory.

Cephas Sdn Bhd handles deployment inventory.



4.6 STORY: Dockets Arrive (Operational Evidence)



After job, SI uploads:



Docket (PDF/photo)



Equipment list



Splitter port photo



Before/after photos



Speed test



Admin reviews and marks job DocketUploaded.



This unlocks Billing.



4.7 STORY: Billing Prepares Invoice (FINANCE)



Finance team opens Billing:



System shows:



Eligible for Invoice

Blocked (Missing docket / serial)

Missing Approval

Assurance pending MRA





Invoice created:



Ratecard loaded



SST applied (if applicable)



Multi-company numbering



Partner billing rules (TIME)



RMA deductions handled



Once done → PDF generated.



4.8 STORY: e-Invoice Submitted to LHDN



System sends invoice to LHDN:



JSON payload



Invoice metadata



Tax breakdown



Supplier + customer info



Platform receives:



QR Code



Validation result



UUID



Invoice becomes LOCKED.



No changes allowed after submission.



4.9 STORY: Partner Pays



When TIME or customer pays:



Finance records payment



Payment allocated to invoices



Overpayments create credit balance



SOA updated



Ageing recalculated



4.10 STORY: Payroll Prepares SI Payments



SIs receive payment for completed jobs:



Rate per job



KPI adjustments



Penalties for repeat jobs or rework



Bonuses for high performance



Payroll runs:



Draft → Review → Finalised → Paid





After finalisation, nothing can be edited.



4.11 STORY: P\&L Shows The Whole Picture



P\&L aggregates:



Revenue (Invoices)



Material Cost (Inventory)



SI Labour Cost (Payroll)



Overheads (Cost Centres)



Directors view:



Profit by partner



Profit by order type



Profit per SI



Monthly + yearly summaries



Across all companies (with consolidated view)



4.12 STORY: Directors Lead The Business



Directors use CephasOps to:



Identify bad partners



Identify repeated RMA patterns



Spot SI performance issues



Find high-margin order types



Reduce material waste



Reduce blockers



Improve installation speed



Increase profitability



CephasOps is now the central decision platform.



5\. Business Scenarios in Storybook Form



The system must support these scenarios:



Customer reschedules → email approval → parser detects → scheduler updates



Building blocks installation → SI flags → scheduler reassigns



Wrong information in email → parser mismatch → admin corrects



Duplicate service ID → system auto-detects



Faulty ONU → SI replaces → RMA ticket created



TIME disputes invoice → credit note issued



SI late → KPI penalty applied



SI excellent → performance bonus



Invoice unpaid for 60 days → shown in Ageing



Director compares Cephas vs Cephas Trading P\&L



Kingsman uses same platform for POS \& payroll



Menorah generates travel invoices



This storybook ensures all edge cases are understood.



6\. Multi-Company StoryLogic

Cephas Sdn. Bhd



Full ISP operation



Parser



Scheduler



Inventory



Billing



Payroll



P\&L



Cephas Trading



Inventory-heavy



RMA



Payroll (field assistants)



P\&L



Kingsman



Retail POS



Staff commission



Basic inventory



Menorah Travel



Travel bookings



Multi-stage invoices



7\. Storybook → Requirements Mapping

Story Element	Module

Emails → Parser	Email Pipeline + Parser Module

ParsedOrder → Order	Orders Module

Scheduler assigns SI	Scheduler Module

SI updates status	SI App

Inventory movement	Inventory \& RMA

Billing generates invoice	Billing Module

e-Invoice to LHDN	Tax \& eInvoice Module

Payroll	Payroll Module

P\&L	P\&L Module

Director dashboards	Analytics



This mapping ensures Cursor AI builds features with full business context.



8\. Style \& Behaviour Rules

8.1 Do Not Over-Automate



Every automation must follow:



Business rule



Human review when needed



8.2 Clear Ownership



Each step belongs to:



Admin



Scheduler



SI



Warehouse



Finance



Director



8.3 Strict Approval Points



Reschedules, credit notes, RMA, payroll adjustments.



8.4 Multi-company boundaries



No cross-mixing of any operations.



9\. Summary



This Storybook is the narrative backbone of CephasOps.

It ensures all modules work together to reflect real-world business behaviour across four different companies.



It explains:



How operations run



How information flows



What outcomes matter



What business rules impact technology



How events in one module affect another



This is the “human version” of the system architecture — essential for Cursor AI to generate correct logic.



✔ End of File

