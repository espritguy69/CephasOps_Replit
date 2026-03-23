ChatGPT said:



Here is your full-production version of



background\_jobs\_infrastructure.md



No placeholders. No coding. Fully aligned with your backend architecture, Cursor-ready, production-grade.



background\_jobs\_infrastructure.md

CephasOps – Background Jobs Infrastructure (Full Production Specification)

1\. Purpose



The Background Jobs Infrastructure provides all asynchronous, scheduled, and long-running task execution across the CephasOps backend.

It ensures:



Email ingestion (IMAP/POP3/API polling)



Snapshot cleanup (auto-delete old snapshot records)



P\&L rebuild jobs



Billing/e-Invoice auto-submission batches



Inventory audits / stock reconciliation



Any partner-specific nightly or hourly tasks



This module decouples operational workloads from the API request-response lifecycle.



2\. Responsibilities

What Background Jobs Do



Run scheduled CRON jobs



Execute queued asynchronous tasks



Track past job executions



Handle retries \& failure logs



Allow admin control (enable/disable jobs, change schedule)



Store configuration JSON for flexible job parameters



What Background Jobs Do NOT Do



Do not replace real-time API flows



Do not store business logic

(Jobs call the Application Services to execute logic)



Do not directly modify tables outside application rules



3\. Core Components

3.1 BackgroundJob (Registry)



Defines a scheduled or manually triggered job.



Field	Type	Description

Id	GUID	Job identifier

Name	string	Human-readable job name

JobType	string	e.g., EmailIngestion, SnapshotCleanup, PnlRebuild

ScheduleCron	string	Cron expression (0 \* \* \* \*)

IsEnabled	bool	Whether job is active

LastRunAt	DateTime	When job last ran

LastRunStatus	enum	Success, Failed, Running

LastRunMessage	string	Log notes

ConfigJson	jsonb	Job parameters

CreatedAt	DateTime	Creation timestamp

CreatedByUserId	GUID	FK → User

UpdatedAt	DateTime	Update timestamp

UpdatedByUserId	GUID	FK → User

3.2 BackgroundJobExecution (Run History)



Stores each execution run.



Field	Type	Description

Id	GUID	Execution ID

BackgroundJobId	GUID	FK → BackgroundJob

StartedAt	DateTime	Start time

FinishedAt	DateTime	End time

Status	enum	Success / Failed

Message	string	Result message

RunConfigJson	jsonb	Actual parameters used

CorrelationId	string	For cross-module tracing

4\. Supported Job Types (Production List)

4.1 Email Processing



EmailIngestionJob



Pulls emails from IMAP/POP3/API



Stores messages + attachments



Triggers ParseSession creation



EmailParserJob



Executes parsing template logic



Creates ParsedOrderDraft



Logs failures and creates ParseSession history



EmailArchiveJob



Moves processed email to archive folder in IMAP/POP3



4.2 Snapshot Maintenance



Snapshots are taken for:



ParsedOrderDraft Excel input



Emails



Orders (optional, based on config)



SnapshotCleanupJob



Reads all SnapshotRecord entries



Deletes expired snapshots after X days



Configuration: "retentionDays": 7



4.3 P\&L Jobs

PnlRebuildJob



Calls PnlRebuildService



Rebuilds PnlOrderDetail + PnlFact



Configurable:



{ "periodFrom": "2025-01", "periodTo": "2025-12", "companyId": "…" }



4.4 e-Invoice Jobs

EInvoiceBatchSubmitJob



Takes invoices in PendingSubmission



Batches submission by partner



Updates EinvoiceSubmission with results



EInvoiceRetryJob



Retries rejected invoices after cooldown



4.5 Inventory Jobs

StockAuditJob



Reconciles StockBalance with SerialisedItem counts



Flags mismatches



LowStockAlertJob



Checks stock levels and pushes alerts (email/push-notifications)



4.6 Scheduler Jobs

StaleSlotsCleanupJob



Clears ScheduledSlots that have no Order or are abandoned



OverdueJobsAlertJob



Sends notifications for SI running late / overdue jobs



5\. Infrastructure Architecture

5.1 Execution Engine



CephasOps uses:



Hosted Background Services



Cron Scheduler (e.g., Quartz.NET, Hangfire — based on production choice)



Execution flow:

BackgroundJob (Registry)

&nbsp;    ↓

CronScheduler → checks enabled jobs

&nbsp;    ↓

Job Dispatcher → executes job handler

&nbsp;    ↓

Application Service (e.g., EmailService)

&nbsp;    ↓

Record BackgroundJobExecution





Each job handler is independent:



EmailJobHandler



SnapshotCleanupJobHandler



PnlRebuildJobHandler



BillingJobHandler



InventoryJobHandler



6\. Configuration Structure



All jobs support JSON config:



{

&nbsp; "companyId": "GUID",

&nbsp; "partnerId": "GUID",

&nbsp; "regionId": "GUID",

&nbsp; "retentionDays": 7,

&nbsp; "periodFrom": "2025-01",

&nbsp; "periodTo": "2025-12"

}





Each job validates its required fields.



7\. Job Handler Requirements (Production Rules)

Every job handler must:



Respect company boundary (companyId)



Create BackgroundJobExecution at start



Update execution status at end



Log exceptions into:



BackgroundJobExecution.Message



SystemLog (Error level)



Use correlationId for distributed tracing



Job safety rules:



No destructive actions unless explicitly configured



No cross-company processing



Must call appropriate Application Services



Must NOT write directly into the database bypassing domain rules



8\. Admin Controls (Future Web UI)



The Background Jobs Admin UI will allow:



View all jobs



Enable/disable a job



Edit cron schedule



Edit config JSON with validation



View run history



Manually trigger job



9\. Integration Points



Email Parser → Background Jobs



Email ingestion



Parsing workflows



Snapshot creation \& cleanup



Orders → Background Jobs



Order ageing alerts



Docket delays monitoring



P\&L → Background Jobs



Monthly rebuild



Daily quick refresh



Billing → Background Jobs



Auto e-Invoice submission batches



Retry failed submissions



Inventory → Background Jobs



Stock reconciliation



Low stock warning job



Scheduler → Background Jobs



Overdue slot check



Stale slot cleanup



10\. Multi-Company Handling



Every job runs per company, never global, except system jobs:



Job Type	Scope

Email ingestion	company-specific

Snapshot cleanup	company-specific

P\&L rebuild	company-specific

e-Invoice	company-specific

Scheduler checks	company-specific

GlobalSettings sync	global

11\. Logging \& Auditing



Background jobs write to:



SystemLog



Execution details



Exceptions



Performance timings



AuditLog



Changes to BackgroundJob registry



Manual triggers



12\. Health Monitoring



Each job must expose:



LastRunAt



LastRunStatus



LastRunMessage



NextRunTime (computed from cron)



Ready for DevOps dashboards (Grafana/Prometheus).



13\. Security



Only roles with permission System.Jobs.Manage may modify jobs.



Read-only access for System.Jobs.View.



Jobs themselves cannot escalate privileges.



14\. Extensibility



Job Handlers are pluggable:



Add a new class



Register in JobDispatcher



Define JobType



Add registry entry



15\. File Retention Policies



Snapshot cleanup must support:



hard delete (default)



soft delete (configurable)



retentionDays = configurable per partner



16\. Failure Handling



Auto-retry for transient errors (configurable)



Do not retry permanent validation errors



Retry count stored in BackgroundJobExecution



17\. Testing Requirements



Unit tests must validate:



Cron schedule parsing



Execution flow (registry → handler → log history)



Failure handling



Handling of disabled jobs



Multi-company filtering enforcement



18\. Summary



This module provides:



✔ Fully isolated multi-company job execution

✔ CRON-based scheduling

✔ Full auditability

✔ Multi-module integration

✔ Configurable JSON-based parameters

✔ Scalable architecture for heavy workloads



It is ready for Cursor-based backend generation, API expansion, or DevOps tooling.

