Below is your full, final, production-ready IMPLEMENTATION\_NOTES\_FOR\_DEVELOPERS\_AND\_CURSOR\_AI.md file.



📌 Place this file here:

cephasops/docs/06\_guidance/IMPLEMENTATION\_NOTES\_FOR\_DEVELOPERS\_AND\_CURSOR\_AI.md



This file tells Cursor AI exactly how to interpret the architecture, how to scaffold the project, how to avoid breaking the multi-company logic, how to structure backend code, and how to integrate modules safely.



This is the master implementation guide.



IMPLEMENTATION\_NOTES\_FOR\_DEVELOPERS\_AND\_CURSOR\_AI.md



CephasOps – Implementation Notes for Developers \& Cursor AI

Version 1.0 (Final)



This document provides non-technical guidance for developers and for AI coding assistants (Cursor AI, GitHub Copilot, etc.) to implement the CephasOps system correctly and safely.



It explains HOW to interpret the architecture, HOW modules should interact, HOW to avoid breaking multi-company isolation, and WHAT rules must always be followed during implementation.



This is NOT code.

This is the rulebook for implementing code.



1\. Core Implementation Principles



The CephasOps platform must always follow these principles:



✔ Multi-company isolation first

✔ Workflow-driven logic (never skip states)

✔ Single source of truth for each domain

✔ Strong validation before actions

✔ Immutable audit logs

✔ Scalable, modular services

✔ Predictable behaviour under load

✔ Clean separation of “business rules” vs “implementation”

2\. Critical Rules for Cursor AI



Cursor AI must follow these rules when generating backend or frontend code:



2.1 Never assume global access



Every API must enforce:



companyId scope

role scope

permissions



2.2 Never hardcode business logic



All business rules live in:



Settings Module



Workflow Engine



Ratecards



Parser templates



Company configurations



2.3 Never bypass workflows



Order status changes must use workflow engine, not manual updates.



2.4 Never write invoicing logic inside Orders



Billing module owns:



Rate calculation



Tax logic



e-Invoice



Document generation



Orders only inform eligibility.



2.5 Never mix inventory across companies



Each inventory movement must include:



companyId, fromLocation, toLocation, userId



2.6 Never calculate P\&L inside Billing



Billing only provides revenue.

P\&L aggregates everything.



3\. Folder / Project Structure Guidelines



The recommended implementation structure inside src/:



src/

&nbsp;├── Domain/

&nbsp;│     ├── Orders/

&nbsp;│     ├── Parser/

&nbsp;│     ├── Workflow/

&nbsp;│     ├── Scheduler/

&nbsp;│     ├── Inventory/

&nbsp;│     ├── Billing/

&nbsp;│     ├── Payroll/

&nbsp;│     ├── PnL/

&nbsp;│     ├── Documents/

&nbsp;│     ├── Companies/

&nbsp;│     └── Settings/

&nbsp;│

&nbsp;├── Application/

&nbsp;│     ├── Commands/

&nbsp;│     ├── Queries/

&nbsp;│     ├── Handlers/

&nbsp;│     ├── DTOs/

&nbsp;│     └── Validators/

&nbsp;│

&nbsp;├── Infrastructure/

&nbsp;│     ├── Persistence/

&nbsp;│     ├── EmailPipeline/

&nbsp;│     ├── FileStorage/

&nbsp;│     ├── MyInvois/

&nbsp;│     ├── Notification/

&nbsp;│     └── BackgroundJobs/

&nbsp;│

&nbsp;├── Api/

&nbsp;│     ├── Controllers/

&nbsp;│     ├── Models/

&nbsp;│     └── Middleware/

&nbsp;│

&nbsp;└── Web/

&nbsp;      ├── AdminPortal/

&nbsp;      └── SIApp/





Each Domain module is fully isolated with its own rules and entities.



4\. Backend Implementation Rules

4.1 Always Validate Company Context



Every request must confirm:



user belongs to activeCompanyId

entity.companyId matches activeCompanyId



4.2 Use Repository per Module



Do NOT use giant shared repositories.

Example:



OrderRepository

InvoiceRepository

StockMovementRepository

PayrollRepository



4.3 Application Layer Handles Business Use Cases



Controllers must stay thin.

Application layer must contain:



Use cases



Validation



Workflow logic (via Workflow Engine)



Notifications



External integrations



4.4 Avoid Cross-Module Logic



Examples:



❌ Inventory must not compute billing amounts

❌ Scheduler must not compute payrates

❌ Orders must not compute P\&L

❌ Payroll must not assign SI to jobs



4.5 Background Jobs



Use background jobs for:



P\&L recalculation



e-Invoice submission



Email ingestion



Parser processing



Daily health checks



5\. Workflow Engine Implementation Notes

5.1 All status changes must go through Workflow Engine



Never modify status manually.



5.2 Workflow must be configurable per company



Workflow steps differ:



Cephas: Activation → Assigned → OTW → Met → Completed



Kingsman: ServiceStart → Completed



Menorah: Booking → Confirm → Paid



5.3 Workflow merges business rules + transitions



Each state must validate:



Actor permissions



Required fields



Blocking conditions



Required documents (photos, serials, docket)



6\. Parser Implementation Notes (Email + Excel)

6.1 Parser must be template-driven



No hardcoding for TIME, Celcom, etc.



6.2 Parser must store snapshots



Because TIME Excel formats constantly change.



6.3 Parser must support manual override



Admins must correct misparsed values.



6.4 Parser → Order Resolver must handle duplicates



Check:



Service ID



Ticket ID



Customer phone



Building



Appointment date



7\. Orders Implementation Notes

7.1 Orders belong to a company



Cannot view orders across companies.



7.2 Orders must validate building compatibility



Building validation rules live in Settings.



7.3 No invoice until:



Completed



Photos uploaded



Serials scanned



Docket uploaded



No blockers



7.4 Reschedules require:



Parser-detected approval email

or



Customer-initiated early change

or



Admin override with reason



8\. Scheduler Implementation Notes

8.1 SI capacity rules



Each SI has:



Max daily jobs



Break times



Working hours



Skill levels



8.2 Scheduler cannot assign job if SI unavailable



Must validate:



Leave



OTW from previous job



Travel time buffer



Skill level



8.3 Reassignment rules



Must log reason + notify SI.



9\. SI App Implementation Notes

9.1 Offline mode required



Store:



Job details



Photos



Serial scans



Logs



9.2 Status updates must sync in order



No skipping steps.



9.3 Mandatory evidence



SI must upload:



Photos



Serial numbers



Splitter port photo



Speedtest (optional but recommended)



10\. Inventory Implementation Notes

10.1 All serialised items must track lifecycle

warehouse → SI bag → customer → RMA → vendor → closed



10.2 All stock movements must be explicit



No hidden stocking.



10.3 Automatic movements



After job completion, the system should:



Deduct SI materials



Add faulty items to RMA



Log all movement



11\. Billing Implementation Notes

11.1 Billing must operate ONLY after Order validation



Billing cannot override:



Missing docket



Missing serial



Blocked order



Incomplete job



11.2 Ratecards must be fully company-specific



Never hardcode partner rates.



11.3 e-Invoice submission must lock invoice



No further editing allowed.



11.4 Credit notes must reference original invoice



Required for:



Partner disputes



Wrong rate



Wrong quantity



12\. Payroll Implementation Notes

12.1 Payroll runs are immutable after finalisation



No editing.



12.2 SI earnings must match Orders \& Scheduler



Cannot overpay or underpay.



12.3 SI cannot see other SIs’ earnings



Privacy mandatory.



13\. P\&L Implementation Notes

13.1 P\&L is derived data



Do NOT store manually; always calculate from:



Invoice revenue



Material usage cost



SI labour cost



Overheads



13.2 P\&L recalculation should use background job



Heavy computation should not block UI.



13.3 Multi-company P\&L is only for Directors



Cannot show to normal admins.



14\. Logging \& Audit Notes



Every action must log:



Actor



Company



Timestamp



Before \& after values



Reference ID



Nothing may be overwritten or deleted.



15\. File Storage Notes

15.1 Storage Pattern

files/{companyId}/{module}/{year}/{month}/{fileId}.pdf



15.2 Files are immutable after locking



Invoices, dockets, RMAs cannot be edited.



16\. Testing Notes



Test the system under:



Email format changes



Badly formatted Excel



Missing serials



Blocked buildings



Wrong ratecards



RMA failures



Partial payments



Duplicate Service IDs



SI offline mode



This ensures robustness.



17\. Cursor AI Guidance (Very Important)



Cursor AI must:



✔ Follow Storybook + Modules + Architecture

✔ Use config files for business rules

✔ Generate clean modular code

✔ Use company scopes everywhere

✔ Confirm assumptions if unclear

✔ Ask the developer before generating risky migrations

✔ Generate tests for each business rule

✔ Never invent business rules

✔ Never simplify multi-company logic

✔ Always map features to modules

✔ Generate consistent API naming

✔ Ensure compliance with document templates

18\. Summary



This document guides implementation behaviour, not technical details.

It ensures that:



Multi-company logic stays intact



Business workflows remain correct



Code stays clean, scalable, and modular



Cursor AI follows best practices



Every module integrates safely



No accidental assumptions break the system



This file must always be read before writing any code.



✔ End of File

