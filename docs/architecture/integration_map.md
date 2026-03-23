# Integration Map

**Related:** [integrations/overview.md](../integrations/overview.md) | [CODEBASE_INTELLIGENCE_MAP.md](CODEBASE_INTELLIGENCE_MAP.md)

**Purpose:** All meaningful external and internal integration touchpoints: purpose, implementation point, module ownership, docs, current vs optional/future.

---

## External integrations

| Integration | Purpose | Implementation point | Module | Docs | State |
|-------------|---------|------------------------|--------|------|------|
| **Email (POP3/IMAP)** | Inbound partner work orders; parser creates drafts | IEmailIngestionService; EmailIngestionService; EmailIngestionSchedulerService | Parser | 02_modules/email_parser, integrations/overview | Current |
| **Email (SMTP)** | Outbound notifications; reschedule requests; auth (e.g. reset) | IEmailSendingService; Parser.Services; AuthService (optional) | Parser, Auth | integrations/overview | Current |
| **WhatsApp** | Docket submission; job updates; SI on-the-way; TTKT notifications | IWhatsAppMessagingService; IUnifiedMessagingService; TwilioWhatsAppProvider / WhatsApp Cloud API | Notifications | integrations/overview | Current; null provider to disable |
| **SMS** | Alerts and notifications | ISmsMessagingService; ISmsGatewayService; ISmsTemplateService | Notifications, Settings | integrations/overview | Current; null provider to disable |
| **MyInvois (LHDN)** | E-invoice submission and status | Billing/Invoice submission; MyInvoisStatusPoll job | Billing | billing_myinvois_flow, integrations/overview | Current |
| **OneDrive** | File sync (File entity: OneDriveFileId, OneDriveSyncStatus, etc.) | IOneDriveSyncService; OneDriveSyncService | Files | integrations/overview | Current (optional) |
| **Partner portals (e.g. TIME)** | No API; admin uses portal for reference; docket/invoice submission to partner manual | Manual process; docs only | Operations | partner_portal_manual_process | Current |

---

## Internal / platform

| Integration | Purpose | Implementation point | Module | Docs | State |
|-------------|---------|------------------------|--------|------|------|
| **Event store (outbox)** | Append domain events; dispatch to handlers | IEventStore; EventStoreDispatcherHostedService; DomainEventDispatcher | Events | PHASE_8_PLATFORM_EVENT_BUS, EVENT_BUS_OPERATIONS_RUNBOOK | Current |
| **Job orchestration** | Enqueue/run orchestrated jobs (e.g. PnlRebuild, OperationalReplay) | JobOrchestrationController; JobExecutionWorkerHostedService; IJobExecutorRegistry | Workers | background_jobs.md | Current |
| **Unified messaging** | Single facade for job update / SI on-the-way / TTKT (SMS/WhatsApp) | IUnifiedMessagingService | Notifications | integrations/overview | Current |
| **Notification dispatch pipeline** | Event-driven outbound; NotificationDispatch table; worker sends via INotificationDeliverySender | NotificationDispatchWorkerHostedService; INotificationDeliverySender | Notifications | 02_modules/notifications, background_jobs | Current |

---

## Optional / future-state

- **Multi-company / SaaS:** Docs (e.g. MULTI_COMPANY_STORYBOOK) marked future-state; single-company current.  
- **Additional WhatsApp/SMS providers:** Template and gateway config in Settings; provider implementations in Infrastructure.  
- **Further partner APIs:** Currently manual; any future API integration should be added here and to integrations/overview.

---

**Refresh:** When adding or changing integrations, update this map and integrations/overview.md.
