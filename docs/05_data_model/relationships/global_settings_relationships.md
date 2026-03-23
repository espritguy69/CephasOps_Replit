Global Settings – Data Model Relationships



This document defines how the Global Settings module relates to the rest of CephasOps, including:



System-wide and per-company configuration



Module toggles



Background jobs



Email parser: email servers, parser rules, VIP senders, invoice/billing senders



1\. Core Entities (from entities file)



Global settings are modelled through:



1.1 GlobalSetting



System-wide configuration entry.



Key fields (conceptually):



Id



Key



Category (enum SettingCategory)



Value



ValueType (enum SettingValueType)



IsCompanyOverridable



IsActive



1.2 CompanySetting



Per-company override of a global setting.



CompanyId



Key



Value



ValueType



1.3 ModuleToggle



Enables/disables modules per company.



CompanyId



ModuleName



IsEnabled



1.4 SettingCategory (enum)



Examples:



System



EmailParser



EmailIngestion



Billing



Workflow



Scheduler



Inventory



Rma



SiApp



Payroll



Pnl



Templates



Logging



1.5 SettingValueType (enum)



Boolean



Number



String



Json



TimeSpan



CronExpression



2\. Basic Relationships

2.1 GlobalSetting → CompanySetting



Relationship: GlobalSetting (1) → (n) CompanySetting



Keys:



CompanySetting.Key must match a GlobalSetting.Key



Rule:



If IsCompanyOverridable = false, no CompanySetting should exist for that key.



Resolution order:



CompanySetting (if exists)



GlobalSetting



2.2 CompanySetting → Company



Relationship: Company (1) → (n) CompanySetting



CompanySetting.CompanyId → Companies.Id



2.3 ModuleToggle → Company



Relationship: Company (1) → (n) ModuleToggle



ModuleToggle.CompanyId → Companies.Id



(Company, ModuleName) must be unique.



3\. Cascading Rules (Lookup Behaviour)

3.1 GetSetting

GetSetting(companyId, key):

&nbsp;   cs = CompanySetting\[companyId, key]

&nbsp;   if cs exists: return cs.Value

&nbsp;   gs = GlobalSetting\[key]

&nbsp;   return gs.Value



3.2 Module Enablement

IsModuleEnabled(companyId, moduleName):

&nbsp;   toggle = ModuleToggle\[companyId, moduleName]

&nbsp;   if toggle exists: return toggle.IsEnabled

&nbsp;   // fallback: enabled by default unless GlobalSetting forces it off



3.3 Non-overridable Keys



If GlobalSetting.IsCompanyOverridable == false,

the service must ignore or block creation of CompanySetting for that key.



4\. Index Requirements

Table	Columns	Purpose

GlobalSettings	Key	Fast lookup by key

CompanySettings	CompanyId, Key	Fast per-company lookup

ModuleToggle	CompanyId, ModuleName	Fast module status lookup

5\. Relationships With Major Modules (Overview)



High-level usage:



Workflow Engine – reads workflow behaviour flags.



Background Jobs – reads cron/enable flags.



Billing – reads invoice formats, auto-invoice flags, tax provider.



Scheduler – reads default working hours and constraints.



Inventory/RMA – reads strict serial usage / RMA rules.



SI App – reads GPS, photo and scan requirements.



Logging/Audit – reads logging level and retention.



Email parser \& ingestion are detailed separately in §6.



6\. Email Parser \& Email Ingestion Relationships (DETAILED)



The Email Parser + Ingestion pipeline uses Global / Company settings to know:



Which email server to connect to



How often to poll mailboxes



Which senders are VIP



Which senders carry invoices / billing emails



Which parser ruleset to apply for work orders, services, RMAs, etc.



6.1 Related Modules \& Entities



From the Email Parser / Email Pipeline module:



Entities (example names):



ParseSession



EmailSnapshot



ParseRuleSet



ParseRule



VipSender



InvoiceSenderMapping



Background jobs:



Email ingestion worker



Snapshot cleanup job



Process:



Email → Snapshot → ParseSession → Approve → Order / Billing record



Global / Company settings do not store emails themselves; they store references and config that the parser module uses.



6.2 Email Server Configuration



Goal: point the ingestion worker at the correct mailbox (ServerFreak POP3/IMAP, etc).



Settings keys (conceptual examples):



Email.Ingestion.DefaultProfile (string, profile name/ID)



Email.Ingestion.\[ProfileName].ServerType (POP3/IMAP)



Email.Ingestion.\[ProfileName].Host



Email.Ingestion.\[ProfileName].Port



Email.Ingestion.\[ProfileName].UseSsl (bool)



Email.Ingestion.\[ProfileName].Username



Email.Ingestion.\[ProfileName].Password (stored securely / external secret)



Email.Ingestion.\[ProfileName].Folder (e.g. INBOX)



Relationships:



GlobalSetting (System category) holds default email ingestion profiles.



CompanySetting (EmailIngestion category) can override which profile a company uses, if allowed.



Runtime behaviour:



Background job reads Email.Ingestion.Enabled + profile config.



It connects to the mailbox and creates ParseSession + EmailSnapshot rows.



6.3 Parser Rule Set Selection



Goal: decide which parser rules to apply to an email stream.



Settings keys:



Email.Parser.DefaultRuleSetId



Email.Parser.WorkOrderRuleSetId



Email.Parser.ServiceRuleSetId



Email.Parser.BillingRuleSetId



Relationships:



GlobalSetting points to IDs in ParseRuleSet (Email Parser module).



CompanySetting can override rule set IDs per company (e.g. different partner formats).



Runtime behaviour:



Email ingestion worker:



Loads rule set ID from settings



Passes it to the parser engine



Parser module:



Uses the referenced ParseRuleSet and associated ParseRule entities.



6.4 VIP Sender Configuration



Goal: mark certain emails as VIP (priority) so they are surfaced differently or assigned faster.



Settings keys:



Email.Parser.VipSenders (JSON list)

e.g. \["noc@time.com.my","vip.partner@isp.com"]



Or keys that point to a VipSender table seeded per company.



Relationships:



GlobalSetting.Category = EmailParser provides global VIP list defaults.



CompanySetting.Category = EmailParser can override/extend VIP senders for that company.



Parser module may still have VipSender table, but settings define which list is active per company.



Runtime behaviour:



When a new ParseSession is created:



If From address matches VIP list, ParseSession.IsVip = true.



Scheduler or Orders module can then:



Flag it



Route it to special queues



Apply tighter KPIs.



6.5 Billing / Invoice Sender Configuration



Goal: detect incoming invoices and link them to Billing module flows.



Settings keys (conceptual):



Email.Parser.InvoiceSenders (JSON list of known invoice senders)



Email.Parser.InvoiceSubjectPatterns (JSON or text with patterns)



Billing.Invoices.EmailToPartnerMapping (JSON mapping email → PartnerId)



Examples:



{

&nbsp; "noc@time.com.my": "TIME",

&nbsp; "billing@celcom.com": "CELCOM"

}





Relationships:



GlobalSetting.Category = EmailParser holds default invoice sender patterns.



CompanySetting can override or extend sender → partner mappings.



Billing module uses:



Detected invoice emails to:



Create InvoiceReceived logs



Trigger workflows for manual verification / auto-reconcile



Runtime behaviour:



Email Parser:



Checks sender against InvoiceSenders from settings.



If match:



Tags ParseSession.Type = InvoiceEmail



Attaches PartnerId via mapping from Billing.Invoices.EmailToPartnerMapping.



Billing module:



Lists “Invoice emails received but not processed.”



Can use attachments to generate Billing entries.



6.6 Parser Behaviour Flags \& Safety Settings



Additional per-company/global switches, for example:



Email.Parser.AllowAutoOrderCreation (bool)



Email.Parser.RequireApprovalBeforeOrder (bool)



Email.Parser.SnapshotRetentionDays (int, used by background cleanup job)



Email.Parser.MaxAttachmentSizeMb (int)



Relationships:



Background Jobs (snapshot cleanup) reads Email.Parser.SnapshotRetentionDays



Orders module reads Email.Parser.RequireApprovalBeforeOrder to decide:



Email → ParseSession → Approve → Order



Or Email → ParseSession → Auto-Order (if explicitly allowed)



6.7 Email Ingestion Job Enabling / Scheduling



Settings keys:



Jobs.EmailIngestion.Enabled (bool)



Jobs.EmailIngestion.Cron (cron expression)



Jobs.EmailIngestion.MaxEmailsPerRun (int)



Relationships:



Background Jobs module:



Reads GlobalSetting/CompanySetting for enable + schedule.



Does not run ingestion if:



Job disabled or



Email module toggled off for that company.



7\. Interaction with ModuleToggle



For Email extras:



ModuleToggle.ModuleName = "EmailParser"



ModuleToggle.ModuleName = "EmailIngestion"



ModuleToggle.ModuleName = "Billing"



Rules:



If EmailParser toggle is off:



Parser endpoints and parse jobs must refuse work.



If EmailIngestion toggle is off:



Background ingestion worker must not connect to mailboxes for that company.



If Billing toggle is off:



Invoice emails may still be parsed, but integration to Billing module must be skipped.



8\. Security \& RBAC (Email Settings)



Global email server settings



Editable only by System Admin.



Typically stored with passwords outside DB or using secrets provider.



Company-level email parser settings



Editable by Company Admin.



Cannot override system-only keys (e.g. POP3 password, core server host).



VIP and Invoice mappings



Editable by roles with access to NOC/Operations and Finance/Billing respectively.



9\. Summary



With this extension, the Global Settings relationships now clearly cover:



Email server configuration (POP3/IMAP), per profile / company.



Email parser ruleset selection, per company.



VIP email senders and how they flag ParseSessions.



Invoice/Billing sender mappings and how they connect to Billing.



How all these interact with:



Background email ingestion jobs



Parser pipeline Email → ParseSession → Approve → Order



Billing and partner mapping



Module toggles and RBAC.



This gives Cursor (and you) a precise contract for how GlobalSettings + Email Parser work together across the system.

