# GLOBAL_SETTINGS_MODULE.md

CephasOps Global Settings – Full Backend Specification

---

## 1. Purpose

Global Settings hold **system-wide configuration** that is:

- Not tied to a single company; or
- Acts as a default if company-specific setting is missing.

Examples:

- Default snapshot retention days
- Default KPI values (if no company override)
- Default tax rate sources
- System flags (enable/disable features)
- Default email worker behaviour

Company-specific settings remain in `CompanySetting`; this module is for **global or default** values.

---

## 2. Data Model

### 2.1 GlobalSetting

Simple, generic key/value store.

Fields:

- `Id`
- `Key` (string; unique, e.g. `SnapshotRetentionDays`)
- `Value` (string; raw; may store JSON)
- `ValueType` (enum: `String`, `Int`, `Decimal`, `Bool`, `Json`)
- `Description`
- `Module` (optional string: `Parser`, `PnL`, `Email`, `Inventory`, `Security`, `UI`, etc.)
- `CreatedAt`, `CreatedByUserId`
- `UpdatedAt`, `UpdatedByUserId`

Constraints:

- Unique on `Key`.

Examples of keys:

- `SnapshotRetentionDays`
- `PnlDefaultRebuildDays`
- `EmailPollingIntervalMinutes`
- `DefaultInvoiceDueDays`
- `DefaultKpiPrelaidHours`
- `DepartmentMaxPerCompany`
- `DepartmentKpiDefaults`
- `DefaultDepartmentForNewOrders`

---
### 2.2 Department-Related Global Keys

Although Departments are configured per company (in `CompanySetting` and department tables), some **defaults** and **system-wide limits** live in `GlobalSetting`.

Examples:

```
- `DepartmentMaxPerCompany` (int)
  - Hard limit on how many active departments a single company can have.
- `DepartmentKpiDefaults` (Json)
  - JSON blob with default SLA targets if a company/department doesn’t override:
  - Example:
    ```json
    {
      "orders.responseMinutes": 30,
      "orders.completionHours": 24,
      "assurance.responseMinutes": 15
    }
    ```
- `DepartmentColorPalette` (Json)
  - Optional default colours for department tags on the UI (scheduler boards, dashboards).
- `DefaultDepartmentForNewOrders` (string)
  - Fallback department key if an order comes in without a mapped department
    and the company hasn't set its own default.
- `MaterialRateStrictMode` (Bool)
  - If `true`: throw configuration error when no `CompanyMaterialRate` is found for a material used in billing/payout.
  - If `false`: allow zero/placeholder pricing but log warning.
- `DefaultMaterialMarginPercent` (Decimal)
  - Used if company doesn't define client price for a material but defines cost.
  - Example: if cost is RM 100 and margin is 30%, client price = RM 130.
- `DefaultCostCenterCode` (String)
  - Fallback cost centre code for finance/P&L when no `DepartmentMaterialCostCenter` mapping exists.
  - Used by P&L builder and billing logic when material cost centre cannot be resolved.
- `CostCenterStrictMode` (Bool)
  - If `true`: throw configuration error when no cost centre can be resolved for a material.
  - If `false`: allow unassigned cost centre but log warning.
```

### Department Scope (Aligned to Current Architecture)

In the current phase of CephasOps:

- There is **one active vertical**: ISP
- There is **one active department**: GPON
- Parser, Email Pipeline, Workflow Engine, Inventory, KPI, and Billing apply only to GPON

Future departments (CWO, NWO) will use:

- The same GlobalSetting framework
- Their own Department configuration
- Their own Parser Templates and EmailAccounts
- Their own KPI & workflow definitions

No changes to GlobalSetting architecture are required when new departments are introduced.


### 2.3 Notification & Email Behavior Global Keys

Global settings for notification channels and email behavior:

```
- `NotificationDefaultChannel` (String)
  - Default notification channel: "IN_APP", "EMAIL", or "BOTH"
  - Used when no type-specific or company override exists.
  - Can be overridden by CompanySetting.

- `NotificationVipEmailDefaultChannel` (String)
  - Channel for VIP email notifications: "IN_APP", "EMAIL", or "BOTH"
  - Can be overridden by CompanySetting.

- `NotificationTaskDefaultChannel` (String)
  - Channel for task-related notifications (assigned, completed): "IN_APP", "EMAIL", or "BOTH"
  - Can be overridden by CompanySetting.

- `EmailVipStrictMode` (Bool)
  - If `true`: always create at least an IN_APP notification for VIP emails even if settings are missing.
  - If `false`: only create notifications if channel settings are configured.

- `NotificationTaskStrictMode` (Bool)
  - If `true`: always create at least an IN_APP notification for task events even if settings are missing.
  - If `false`: only create notifications if channel settings are configured.

- `NotificationMaxItemsPerUser` (Int, optional)
  - Maximum number of notifications to keep per user (older ones may be auto-archived).
  - If not set, no automatic cleanup occurs.

- `EmailPollingIntervalMinutes` (Int)
  - How often to poll email mailboxes (default: 15 minutes during business hours, 60 minutes otherwise).
  - Can be overridden per mailbox in EmailAccount configuration.
```

---
## 3. Service

### 3.1 GlobalSettingsService

Responsibilities:

- Read/write global settings
- Provide typed getters for common keys
- Fallback logic for modules

Methods:

- `Task<string?> GetValueAsync(string key)`
- `Task<T?> GetValueAsync<T>(string key)`
- `Task SetValueAsync(string key, object value, string? description = null)`
- `Task<IDictionary<string, string>> GetAllAsync()`

Usage pattern:

- P&L uses `PnlDefaultRebuildDays` if not overridden.
- Email worker uses `EmailPollingIntervalMinutes`.
- Parser uses `SnapshotRetentionDays` for snapshot cleanup.
- UI uses `EnableMultiCompany` / `EnableParserV2` flags.

---

## 4. API

Base path: `/api/global-settings`

- `GET /api/global-settings`
  - Returns list of key/value pairs (optionally grouped by `Module`).
- `GET /api/global-settings/{key}`
  - Returns a single setting.
- `PUT /api/global-settings/{key}`
  - Body: `value`, optional `description`, `valueType`.

Example payload:

```json
{
  "key": "SnapshotRetentionDays",
  "value": "7",
  "valueType": "Int",
  "description": "Number of days to retain email snapshots before cleanup",
  "module": "Parser"
}

5. Integration with CompanySetting

Resolution order for a setting that has both global and company-level variants:

Check CompanySetting (if exists for companyId + key).

If not found, check GlobalSetting.

If still not found, use hard-coded default in code.

Example:

SnapshotRetentionDays:

Company-specific can override global.

If company has no override → use GlobalSetting("SnapshotRetentionDays").

If even that is missing → fallback to e.g. 7 days in code.

Example:

- `SnapshotRetentionDays`:
  - Company-specific can override global.

- `DefaultDepartmentForNewOrders`:
  - Resolution:
    1. Check company-specific setting (e.g. `CompanySetting` for that company).
    2. If missing, use `GlobalSetting.DefaultDepartmentForNewOrders`.
    3. If still missing, backend must decide a safe fallback (e.g. reject order or
       assign to a system-defined "Unassigned" department queue).

- `DefaultCostCenterCode`:
  - Resolution:
    1. Check `DepartmentMaterialCostCenter` for (CompanyId, MaterialTemplateId, DepartmentId).
    2. If missing, check `CompanyMaterialRate.DefaultCostCenterCode` for (CompanyId, MaterialTemplateId).
    3. If missing, use `GlobalSetting.DefaultCostCenterCode`.
    4. If still missing and `CostCenterStrictMode` is `true`, throw configuration error.
    5. If `CostCenterStrictMode` is `false`, allow unassigned cost centre but log warning.


6. Security / RBAC

Only SuperAdmin or equivalent role can edit GlobalSettings.

Reads:

Can be open to Admin roles and system modules.

Optionally restricted per Module (e.g. Security module visible only to platform admins).

7. Audit

All changes to GlobalSetting must be logged to AuditLog (see Logging module):

Logged fields:

Key

Old value

New value

UpdatedByUserId

Timestamp

8. Standard Keys by Domain (Recommended Catalog)

This section defines standard keys so that all modules and UI speak the same language.
You don’t have to implement all at once, but this is the agreed namespace.

8.1 Parser & Email

#### Department Binding for Parser & Email

All Parser and Email Worker settings currently apply to the **ISP Vertical → GPON Department**.

When future departments (CWO, NWO) go live:
- Each department can specify its own EmailAccounts
- Each partner under that department will map to its own ParserTemplate
- Global keys here remain department-agnostic and act as shared defaults

Module = Parser or Email

SnapshotRetentionDays (Int)

Default days to keep email snapshots.

EmailPollingIntervalMinutes (Int)

How often the email worker polls the mailbox.

EmailMaxAttachmentsMb (Int)

EmailAllowedAttachmentTypes (Json array string, e.g. [".xls",".xlsx",".pdf",".htm"])

ParserEnableHumanReplyParsing (Bool)

ParserEnableRescheduleDetection (Bool)

ParserEnableAssuranceParsing (Bool)

ParserNlpLocale (String, e.g. en_MY)

ParserDefaultTimezone (String, e.g. Asia/Kuala_Lumpur)

ParserVipPriorityEnabled (Bool)

EmailPollingIntervalMinutes (Int)

- How often the email worker polls mailboxes (default: 5 minutes).

EmailParserDefaultMailboxLimit (Int)

- Maximum number of mailboxes per company (default: 10).

EmailVipStrictMode (Bool)

- If `true`, VIP emails must always trigger a notification action.
- If `false`, VIP emails are flagged but notifications are optional.

EmailDefaultNotificationChannel (String)

- Default notification channel for VIP emails: "in-app", "email", "both" (default: "in-app").

NotificationDefaultChannel (String)

- Default notification channel for all notifications: "in-app", "email", "both" (default: "in-app").
- Used as fallback when type-specific channels are not configured.

NotificationVipEmailChannel (String)

- Notification channel for VIP email notifications: "in-app", "email", "both" (default: "in-app").
- Overrides `NotificationDefaultChannel` for VIP emails only.

NotificationTaskAssignmentChannel (String)

- Notification channel for task assignment notifications: "in-app", "email", "both" (default: "in-app").
- Used when tasks are assigned to users.

NotificationTaskStatusChannel (String)

- Notification channel for task status change notifications: "in-app", "email", "both" (default: "in-app").
- Used when task status changes (e.g., completed).

NotificationRetentionDays (Int)

- Number of days to retain notifications before auto-archiving (default: 90).
- Notifications older than this will be automatically archived.

VipEmailDefaultRecipients (Json)

- Default recipients for VIP emails when no specific target is configured.
- JSON array of strings: user IDs (GUIDs) or role names (e.g., "Director", "CEO").
- Example: `["550e8400-e29b-41d4-a716-446655440000", "Director", "CompanyAdmin"]`
- Resolution: User IDs are used directly; role names are resolved to all users with that role in the company.
- Default: empty array (no default recipients).

8.2 Multi-Company Defaults

Module = MultiCompany

EnableMultiCompany (Bool)

Master switch for multi-company features in UI.

DefaultCurrency (String, e.g. MYR)

DefaultLanguage (String, e.g. en)

DefaultRateProfileCode (String, hint for initial rate profile selection)

DefaultInvoicePrefix (String, fallback if Company has none)

8.3 Departments & Workflows
### Active Workflow Context (GPON)

Department workflow strict mode is evaluated only against **GPON Workflow Rules** in the current phase.
If `DepartmentWorkflowStrictMode = true`, GPON transitions must match the defined GPON workflow.

Other departments (CWO, NWO) will inherit this same strict mode behavior when their workflows are added.

**Module = `Department` or `Workflow`**

These keys control **global defaults** for departmental behaviour.  
Companies can still override via `CompanySetting` or their own Department configurations, but these act as system-wide baselines.

- `EnableDepartments` (Bool)  
  - Master switch to turn the Department module on/off across the platform.
  - When `false`, `currentDepartmentId` is optional and department-based views can be hidden.

- `DepartmentDefaultTemplateCode` (String)  
  - Name of a default department template to apply when a new company is created.
  - Example value: `DEFAULT_SERVICE_INSTALLER_TEMPLATE`
  - Template would typically include OPS, INSTALLER, WAREHOUSE, FINANCE, BILLING.

- `DepartmentRequiredForOrders` (Bool)  
  - If `true`, every Order **must** have a `currentDepartmentId`.
  - Used to enforce strict departmental ownership of work.

- `DepartmentRequiredForMaterialMovements` (Bool)  
  - If `true`, all material movements must include `fromDepartmentId` and/or `toDepartmentId`.
  - Ensures accountability when materials move between teams.

- `DepartmentSlaDefaultHours` (Json)  
  - JSON object with default SLA per department code when no Company/Dept override exists.
  - Example:
    ```json
    {
      "OPS": 4,
      "INST": 24,
      "FIN": 48
    }
    ```
  - Used by KPI engine to evaluate SLA when `DepartmentKpiConfig` is missing.

- `DepartmentWorkflowStrictMode` (Bool)  
  - If `true`, CephasOps will:
    - Block status transitions that do **not** have a matching `DepartmentWorkflowRule`.
    - Force administrators to configure workflow → department mapping explicitly.
  - If `false`, the system can fall back to:
    - A default department, or
    - No department (soft mode) depending on business rules.

- `DepartmentKpiEnabled` (Bool)  
  - Enables or disables department-specific KPIs globally.
  - When `false`, KPI dashboards can still exist but may ignore department slicing.

These keys ensure the Department module is **centrally controllable** while leaving room for company-specific overrides.

```
GlobalSetting(Key/Value)
  ↓
GlobalSettingsService
  ↓
  - Email Worker
  - ParserService
  - Jobs (SnapshotCleanupJob, KpiJob, BillingJob)
  - KPI / SLA evaluation
  - UI feature toggles
  - CompanySetting resolution (fallback)
  - Department module behaviour (EnableDepartments, workflow strict mode, default templates)

```

8.4 Rates, P&L & Billing

Module = PnL or Billing

PnlDefaultRebuildDays (Int)

BillingDefaultInvoiceDueDays (Int)

BillingAutoGenerateInvoice (Bool)

BillingRoundToNearestCent (Bool)

8.5 KPI & SLA
### KPI Binding for Current Phase (GPON)

KPI and SLA parameters currently apply only to the GPON department.

Special KPI routing:
- **Docket Rejection** → SI KPI
- **Invoice Rejection** → Admin KPI

Future departments (CWO, NWO) can define their own KPI routing without changing backend architecture.

Module = Kpi

KpiInstallationSlaHours (Int)

KpiAssuranceSlaHours (Int)

KpiPrelaidDefaultHours (Int) (your DefaultKpiPrelaidHours)

KpiWarnThresholdPercent (Int) – e.g. 80% of SLA.

8.6 Jobs & Scheduler

Module = Jobs

SnapshotCleanupCron (String, e.g. "0 3 * * *")

KpiDailyCron

BillingRunCron

MaxConcurrentJobs (Int)

JobDefaultRetryBackoffSeconds (Int)

JobDefaultMaxRetries (Int)

8.7 Security & Auth

Module = Security

AuthRequire2faForAdmins (Bool)

PasswordMinLength (Int)

PasswordRequireUppercase (Bool)

PasswordRequireDigit (Bool)

PasswordRequireSpecialChar (Bool)

PasswordExpiryDays (Int)

AuthMaxConcurrentSessionsPerUser (Int)

(You can keep JWT and secret values in environment/secrets instead of here.)

8.8 Feature Flags & UI

Module = UI or FeatureFlags

EnableParserV2 (Bool)

EnableInstallerAppV2 (Bool)

EnableBillingModule (Bool)

EnableAdvancedKpiDashboards (Bool)

EnableEmailParserDebugPanel (Bool)

9. Relationships with Other Modules (Conceptual)
#### Parser & Email Pipeline Scope

The ParserService and Email Worker currently operate exclusively for the GPON department.

Department resolution uses:
- Partner → Department mapping
- Or explicit EmailRule routing
- Or fallback to `DefaultDepartmentForNewOrders` if configured

This ensures all parsed orders enter the GPON workflow until additional departments are activated.


Even though GlobalSetting is a flat table, it still has logical relationships with other modules:

Email Worker

Uses: EmailPollingIntervalMinutes, EmailMaxAttachmentsMb, EmailAllowedAttachmentTypes.

ParserService

Uses: SnapshotRetentionDays, ParserEnableRescheduleDetection, ParserNlpLocale, ParserDefaultTimezone.

SnapshotCleanupJob

Uses: SnapshotRetentionDays, SnapshotCleanupCron.

Order & Billing

Use: BillingDefaultInvoiceDueDays, BillingAutoGenerateInvoice.

KPI Worker

Uses: KpiInstallationSlaHours, KpiAssuranceSlaHours.

UI / Frontend

Uses: EnableMultiCompany, EnableParserV2, EnableBillingModule.

Conceptually:

GlobalSetting(Key/Value)
  ↓
GlobalSettingsService
  ↓
  - Email Worker
  - ParserService
  - Jobs (SnapshotCleanupJob, KpiJob, BillingJob)
  - KPI / SLA evaluation
  - UI feature toggles
  - CompanySetting resolution (fallback)

10. UI Integration – Global Settings Admin

A simple admin screen should exist under something like:

/admin/global-settings

10.1 UI Behaviour

Fetches GET /api/global-settings and groups keys by Module.

Displays a table:

Key	Value	Type	Module	Description	Last Updated
SnapshotRetentionDays	7	Int	Parser	Days to keep email snapshots	2025-11-20
EmailPollingIntervalMinutes	5	Int	Email	Poll interval for email worker	2025-11-20
EnableMultiCompany	true	Bool	UI	Turn on multi-company UI	2025-11-20
KpiInstallationSlaHours	48	Int	Kpi	SLA for installation completion	2025-11-20

SuperAdmin can:

Click a row → edit value, value type, description.

Add new key (careful; usually done by backend team).

10.2 UX Suggestions

Filters:

By Module (Parser, UI, Jobs, etc.)

By ValueType (Int, Bool, etc.)

Warnings:

Changing certain keys (e.g. SnapshotRetentionDays, SnapshotCleanupCron) should display a “this affects background jobs” warning.

10.3 Frontend Model
type GlobalSettingDto = {
  key: string;
  value: string;
  valueType: "String" | "Int" | "Decimal" | "Bool" | "Json";
  module?: string;
  description?: string;
  updatedAt?: string;
  updatedByUser?: string;
};

11. Example: Using Global + Company Settings Together

Example: resolve snapshot retention for a given company:

public async Task<int> GetSnapshotRetentionDaysAsync(Guid companyId)
{
    // 1. Try company-specific override
    var companyOverride = await _companySettingsService
        .GetValueAsync<int?>("SnapshotRetentionDays", companyId);
    if (companyOverride.HasValue)
        return companyOverride.Value;

    // 2. Fallback to global
    var global = await _globalSettingsService
        .GetValueAsync<int?>("SnapshotRetentionDays");
    if (global.HasValue)
        return global.Value;

    // 3. Hard-coded default
    return 7;
}


This pattern should be reused for any setting that has both global and company variants.

12. Summary
### Architectural Alignment (2025 Phase)

GlobalSetting powers all system-wide defaults, but in the current implementation:

- Only **GPON** uses parser, email ingestion, workflow enforcement, KPI, inventory and billing
- Future departments will reuse the same GlobalSetting framework without modifications


GlobalSetting is a simple key/value registry.

It drives parser, email worker, jobs, KPI, P&L, UI flags, etc.

CompanySetting can override it on a per-company basis.

GlobalSettingsService is the central accessor with typed getters.

A small Global Settings Admin UI lets SuperAdmins change behaviour without redeploys.


---

This keeps your **simple GlobalSetting table** but gives you:

- A consistent **key catalog**.
- Clear **relationships** to other modules.
- A defined **UI pattern** to control everything.