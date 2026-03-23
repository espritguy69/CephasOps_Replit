This is the master API index – not code, not endpoints definitions — but a documentation map that guides Cursor AI when building the backend.



API\_CONTRACTS\_SUMMARY.md



CephasOps – API Contracts Summary (Master Index)

Version 1.0



This document provides a clean, high-level summary of all API groups required to build CephasOps.

It is NOT a code specification — each endpoint will be fully defined by Cursor AI based on module documentation.



This index ensures:



Completeness



Correct module grouping



Uniform naming



Separation of concerns



Clear developer guidance



1\. API Principles



All CephasOps APIs must follow:



✔ RESTful conventions

✔ JSON payloads

✔ JWT-based authentication

✔ companyId must be enforced everywhere

✔ Role-based + permission-based access

✔ No direct status changes (must use workflow engine)

✔ No inventory updates without stock movement rules

✔ No direct financial entity editing after locking (invoice, payroll, P\&L)



APIs are grouped by functional domain.



2\. Authentication \& User Context API

POST /api/auth/login



Login \& retrieve JWT.



POST /api/auth/refresh



Refresh token.



GET /api/auth/me



User info + roles + companies.



GET /api/auth/switch-company/{companyId}



Switch active company (if multi-company user).



3\. Settings \& Master Data API



These APIs manage system-level configurations and are used by multiple modules.



GET /api/settings



Returns global + company settings.



PUT /api/settings



Update selected settings.



GET /api/settings/parser-template/{partnerId}



Parser mapping rules.



GET /api/settings/ratecards



SI ratecards, billing rates, etc.



GET /api/settings/building-types



Building type metadata.



GET /api/settings/materials



Master materials list.



These settings drive:

Parser → Orders → Billing → Payroll.



4\. Email Pipeline \& Parser API

4.1 Email Storage

POST /api/email/ingest



Store email, attachments, metadata.



GET /api/email/{id}



Retrieve email \& attachments.



4.2 Email Classification

POST /api/email/classify



Detects category:



Activation



Modification



Assurance



Reschedule



MRA



Unknown



4.3 Email Parser

POST /api/parser/parse



Transform raw message into structured JSON using settings templates.



GET /api/parser/parsed/{id}



Retrieve parsed body.



4.4 Parser Review

POST /api/parser/review/{parsedId}/approve



Human/AI approval.



POST /api/parser/review/{parsedId}/reject



Reject or request correction.



4.5 Order Resolver

POST /api/parser/resolve



Convert parsedOrder → actual Order.



GET /api/parser/duplicates?serviceId=



Check for repeated orders.



5\. Orders API (Core of ISP Operations)

5.1 Basic Order Management

GET /api/orders



Filter by:



status



date range



partner



SI



building



type



GET /api/orders/{id}



Full order detail.



POST /api/orders



Manual creation.



PUT /api/orders/{id}



Update non-workflow fields.



5.2 Workflow Engine Endpoints



(All status changes MUST use these — never direct updates.)



POST /api/orders/{id}/assign



Assign SI.



POST /api/orders/{id}/start-otw



Start “On The Way”.



POST /api/orders/{id}/met



Met customer.



POST /api/orders/{id}/complete



Finish job.



POST /api/orders/{id}/cancel



Cancel order.



POST /api/orders/{id}/block



Set blocker (Building / Customer / SI / Network).



POST /api/orders/{id}/block/resolve



Clear blocker.



5.3 Rescheduling

POST /api/orders/{id}/reschedule/request



Admin requests new slot.



POST /api/orders/{id}/reschedule/approve



Parser/Partner email approves.



POST /api/orders/{id}/reschedule/reject



Reject request.



5.4 Dockets \& Files

POST /api/orders/{id}/dockets



Upload docket file.



GET /api/orders/{id}/dockets



List dockets.



5.5 Material Usage

POST /api/orders/{id}/validate-splitter



Validate splitter port usage.



POST /api/orders/{id}/validate-materials



Validate building rules.



6\. Scheduler API

GET /api/scheduler/calendar



Full timeline of SIs \& orders.



POST /api/scheduler/assign



Assign job to SI.



POST /api/scheduler/reassign



Reassign SI.



POST /api/scheduler/reschedule



Update appointment window.



GET /api/scheduler/si-availability/{siId}



Get SI working hours.



POST /api/scheduler/si-leave



SI leave application.



7\. Service Installer App API (SI PWA)

GET /api/si/jobs/today



Today’s assigned jobs.



GET /api/si/jobs/{id}



Single job detail.



POST /api/si/jobs/{id}/start-otw



Start OTW.



POST /api/si/jobs/{id}/met



Met customer.



POST /api/si/jobs/{id}/upload-photo



Upload installation photos.



POST /api/si/jobs/{id}/complete



Mark job completed.



POST /api/si/jobs/{id}/scan-serial



Scan ONU/router/fibre serial.



8\. Inventory \& RMA API

8.1 Materials \& Stock

GET /api/inventory/materials



Material catalog.



GET /api/inventory/stock



Stock by location.



POST /api/inventory/movement



Move stock between:



Warehouse



SI bag



Customer



RMA



POST /api/inventory/grn



Goods received.



8.2 Serialised Items

GET /api/inventory/serial/{serialNo}



Lookup serial.



POST /api/inventory/serial/assign



Assign to SI or order.



POST /api/inventory/serial/mark-faulty



Mark faulty → RMA.



8.3 RMA Management

POST /api/inventory/rma



Create RMA ticket.



GET /api/inventory/rma/{id}



View RMA.



POST /api/inventory/rma/{id}/close



Close RMA.



9\. Billing, Tax \& e-Invoice API

9.1 Invoice Creation

POST /api/billing/create/order/{orderId}



Create single invoice.



POST /api/billing/create/batch



Batch principal invoicing.



9.2 Invoice Management

GET /api/billing/invoices



Filter invoices.



GET /api/billing/invoices/{id}



Single invoice.



PUT /api/billing/invoices/{id}



Editable only before submission.



9.3 e-Invoice (LHDN MyInvois)

POST /api/billing/einvoice/submit/{invoiceId}



Push invoice to MyInvois.



GET /api/billing/einvoice/status/{invoiceId}



Check validation state.



POST /api/billing/einvoice/retry/{invoiceId}



Retry failed submission.



9.4 Payments

POST /api/billing/payments



Record payment.



GET /api/billing/payments/{invoiceId}



List payments.



GET /api/billing/ageing



Ageing report.



10\. Payroll API

GET /api/payroll/earnings



View SI earnings for a period.



POST /api/payroll/calc/{period}



Calculate monthly payroll.



GET /api/payroll/runs



List payroll runs.



POST /api/payroll/runs



Create payroll run.



POST /api/payroll/runs/{id}/finalise



Lock payroll.



POST /api/payroll/runs/{id}/pay



Mark as paid.



11\. P\&L (Profit \& Loss) API

GET /api/pnl/summary



High-level monthly P\&L.



GET /api/pnl/orders



Profit per job.



POST /api/pnl/recalculate



Rebuild P\&L for range.



GET /api/pnl/overheads



List overheads.



POST /api/pnl/overheads



Add overhead.



DELETE /api/pnl/overheads/{id}



Remove if unlocked.



12\. File API

POST /api/files/upload



Upload images, PDFs, dockets.



GET /api/files/{id}



Download.



DELETE /api/files/{id}



Delete.



13\. Logs \& Diagnostics API

GET /api/logs/orders/{orderId}



Order history.



GET /api/logs/si/{siId}



SI performance logs.



GET /api/logs/errors



Parser \& workflow issues.



14\. Admin Utilities API

POST /api/admin/reindex



Rebuild search indexes.



POST /api/admin/cache/flush



Clear settings cache.



GET /api/admin/health



System status.



15\. Developer Notes



Cursor AI must follow:



Storybook



Workflow Engine rules



Inventory rules



Billing rules



Multi-company isolation



API naming consistency



No business logic in controllers (only application layer)



This summary guides Cursor when scaffolding backend controllers.

