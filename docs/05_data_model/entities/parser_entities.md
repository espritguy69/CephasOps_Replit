\# Parser \& Email Ingestion Entities  

CephasOps – Parser Domain Data Model  

Version 1.0



This file defines the entities for \*\*email ingestion + parsing\*\*:



\- EmailAccount

\- EmailMessage

\- EmailAttachment

\- EmailRule

\- VipEmail

\- ParseSession

\- ParsedOrderDraft

\- ParserTemplate (core fields – detailed in settings\_entities)



All \*\*company-scoped\*\* where applicable.



---



\## 1. EmailAccount



Represents a mailbox we pull orders from (e.g. TIME NOC mailbox).



\### 1.1 Table: `EmailAccounts`



| Field           | Type     | Required | Description                             |

|-----------------|----------|----------|-----------------------------------------|

| id              | uuid     | yes      | Primary key.                            |

| companyId       | uuid     | yes      | FK → Companies.id.                      |

| name            | string   | yes      | Display name (`TIME Orders Mailbox`).   |

| provider        | string   | yes      | `IMAP`, `O365`, `Gmail`, etc.          |

| host            | string   | no       | Mail server hostname (if needed).       |

| username        | string   | yes      | Login username/email.                   |

| isActive        | boolean  | yes      | True if actively polled.                |

| pollIntervalSec | int      | yes      | How often to poll.                      |

| lastPolledAt    | datetime | no       | When last checked.                      |

| createdAt       | datetime | yes      | Created timestamp.                      |



> Credentials are stored in secure config/secret store outside this table.



---



\## 2. EmailMessage



Normalised copy/metadata for each relevant email.



\### 2.1 Table: `EmailMessages`



| Field            | Type     | Required | Description                                      |

|------------------|----------|----------|--------------------------------------------------|

| id               | uuid     | yes      | Primary key.                                     |

| companyId        | uuid     | yes      | FK → Companies.id.                               |

| emailAccountId   | uuid     | yes      | FK → EmailAccounts.id.                           |

| messageId        | string   | yes      | Provider message-id (for idempotency).           |

| fromAddress      | string   | yes      | Sender email.                                    |

| toAddresses      | string   | yes      | Comma-separated list.                            |

| ccAddresses      | string   | no       | Comma-separated list.                            |

| subject          | string   | yes      | Subject line.                                    |

| bodyPreview      | string   | no       | Short snippet for UI.                            |

| receivedAt       | datetime | yes      | When email received in mailbox.                  |

| rawStoragePath   | string   | no       | Where full raw MIME is stored (if needed).       |

| hasAttachments   | boolean  | yes      | True if any file attached.                       |

| parserStatus     | enum     | yes      | `Pending`, `Parsed`, `Error`, `Ignored`.         |

| parserError      | string   | no       | Last error message (if any).                     |

| createdAt        | datetime | yes      | Ingest timestamp.                                |



\### 2.2 Indexes



\- Unique: `(companyId, messageId)`

\- Search: `(companyId, parserStatus, receivedAt)`

\- VIP: `(companyId, isVip, receivedAt)`



---



\## 3. EmailAttachment



Metadata and linkage for email attachments.



\### 3.1 Table: `EmailAttachments`



| Field           | Type     | Required | Description                              |

|-----------------|----------|----------|------------------------------------------|

| id              | uuid     | yes      | Primary key.                             |

| companyId       | uuid     | yes      | FK → Companies.id.                       |

| emailMessageId  | uuid     | yes      | FK → EmailMessages.id.                   |

| fileId          | uuid     | yes      | FK → Files.id (binary contents).         |

| fileName        | string   | yes      | Original filename.                       |

| contentType     | string   | yes      | MIME type.                               |

| sizeBytes       | bigint   | yes      | File size.                               |

| isPrimary       | boolean  | yes      | If this is likely the main Excel/PDF.    |

| createdAt       | datetime | yes      | Created timestamp.                       |



---



\## 4. EmailRule

Defines filtering and routing rules for incoming emails. Rules are evaluated in priority order to determine how emails should be processed.



\### 4.1 Table: `EmailRules`



| Field              | Type     | Required | Description                                      |

|--------------------|----------|----------|--------------------------------------------------|

| id                 | uuid     | yes      | Primary key.                                     |

| companyId          | uuid     | yes      | FK → Companies.id.                               |

| emailAccountId     | uuid     | no       | FK → EmailAccounts.id (null = applies to all).   |

| fromAddressPattern | string   | no       | Pattern to match FROM email (wildcard supported).|

| domainPattern      | string   | no       | Domain pattern (e.g. "@time.com.my").            |

| subjectContains    | string   | no       | Subject must contain this text.                  |

| isVip              | boolean  | yes      | Whether this rule marks emails as VIP.           |

| targetDepartmentId | uuid     | no       | FK → Departments.id (if routing to department).  |

| targetUserId       | uuid     | no       | FK → Users.id (if routing to user).              |

| actionType         | enum     | yes      | Action when rule matches (see EmailRuleActionType).|

| priority           | int      | yes      | Priority for conflict resolution (higher = first).|

| isActive           | boolean  | yes      | Whether this rule is active.                     |

| description        | string   | no       | Optional description/notes.                      |

| createdByUserId    | uuid     | yes      | User who created this rule.                      |

| updatedByUserId    | uuid     | no       | User who last updated this rule.                 |

| createdAt          | datetime | yes      | Created timestamp.                               |

| updatedAt          | datetime | yes      | Last update timestamp.                           |



\### 4.2 EmailRuleActionType Enum



- `RouteToDepartment` - Route email to a specific department

- `RouteToUser` - Route email to a specific user

- `MarkVipOnly` - Mark as VIP only (no routing)

- `Ignore` - Ignore this email (skip processing)

- `MarkVipAndRouteToDepartment` - Mark as VIP and route to department

- `MarkVipAndRouteToUser` - Mark as VIP and route to user



\### 4.3 Indexes



- Priority lookup: `(companyId, priority, isActive)`

- Mailbox-specific: `(companyId, emailAccountId, isActive)`



---



\## 5. VipEmail

Represents a VIP email address that should receive special treatment. VIP emails are flagged and trigger notifications to designated users.



\### 5.1 Table: `VipEmails`



| Field           | Type     | Required | Description                              |

|-----------------|----------|----------|------------------------------------------|

| id              | uuid     | yes      | Primary key.                             |

| companyId       | uuid     | yes      | FK → Companies.id.                       |

| emailAddress    | string   | yes      | VIP email address (exact match).         |

| displayName     | string   | yes      | Display name (e.g. "CEO", "Director").   |

| notifyUserId    | uuid     | no       | FK → Users.id (specific user to notify). |

| notifyRole      | string   | no       | Role to notify (e.g. "CEO", "Director"). |

| notes           | string   | no       | Optional notes/description.              |

| isActive        | boolean  | yes      | Whether this VIP entry is active.        |

| createdByUserId | uuid     | yes      | User who created this entry.             |

| updatedByUserId | uuid     | no       | User who last updated this entry.        |

| createdAt       | datetime | yes      | Created timestamp.                       |

| updatedAt       | datetime | yes      | Last update timestamp.                   |



\### 5.2 Indexes



- Unique: `(companyId, emailAddress)`

- Active lookup: `(companyId, isActive)`



---



\## 6. ParseSession



Represents one attempt to parse an email (and/or attachment) into structured data.



\### 4.1 Table: `ParseSessions`



| Field              | Type     | Required | Description                                      |

|--------------------|----------|----------|--------------------------------------------------|

| id                 | uuid     | yes      | Primary key.                                     |

| companyId          | uuid     | yes      | FK → Companies.id.                               |

| emailMessageId     | uuid     | yes      | FK → EmailMessages.id.                           |

| parserTemplateId   | uuid     | no       | FK → ParserTemplates.id used (if matched).       |

| status             | enum     | yes      | `Pending`, `Running`, `Success`, `Failed`.       |

| errorMessage       | string   | no       | Last failure reason.                             |

| snapshotFileId     | uuid     | no       | FK → Files.id snapshot of original attachment.   |

| parsedOrdersCount  | int      | yes      | Number of ParsedOrderDraft created.              |

| createdAt          | datetime | yes      | Session started.                                 |

| completedAt        | datetime | no       | Session finished.                                |



> Snapshot may be deleted by a cleanup job after X days.



---



\## 7. ParsedOrderDraft



Intermediate representation between parsed data and a real Order.



\### 7.1 Table: `ParsedOrderDrafts`



| Field              | Type     | Required | Description                                            |

|--------------------|----------|----------|--------------------------------------------------------|

| id                 | uuid     | yes      | Primary key.                                           |

| companyId          | uuid     | yes      | FK → Companies.id.                                     |

| parseSessionId     | uuid     | yes      | FK → ParseSessions.id.                                 |

| partnerId          | uuid     | no       | FK → Partners.id (if recognised).                      |

| buildingId         | uuid     | no       | FK → Buildings.id (if recognised).                     |

| serviceId          | string   | no       | Parsed service ID.                                     |

| ticketId           | string   | no       | Parsed TTKT / AWO ID.                                  |

| customerName       | string   | no       | Parsed customer name.                                  |

| customerPhone      | string   | no       | Parsed phone.                                          |

| addressText        | text     | no       | Raw/parsed address.                                    |

| appointmentDate    | date     | no       | Parsed date.                                           |

| appointmentWindow  | string   | no       | Raw window text (e.g. `10-12`).                        |

| orderTypeHint      | string   | no       | `Activation`, `Assurance`, `Relocation`, etc.          |

| confidenceScore    | decimal  | yes      | 0–1, parser confidence.                                |

| validationStatus   | enum     | yes      | `Pending`, `Valid`, `NeedsReview`, `Rejected`.         |

| validationNotes    | text     | no       | Admin comments.                                        |

| createdOrderId     | uuid     | no       | FK → Orders.id if converted.                           |

| createdByUserId    | uuid     | no       | Admin who reviewed \& approved.                         |

| createdAt          | datetime | yes      | Created timestamp.                                     |

| updatedAt          | datetime | yes      | Last update.                                           |



---



\## 8. ParserTemplate (Reference)



Full definition lives in `settings\_entities.md`, but from parser POV:



\- Each `ParseSession` links to the specific template used.

\- Template defines:

&nbsp; - how to detect matching emails  

&nbsp; - how to map Excel columns to fields  

&nbsp; - how to map values (e.g. “FTTH” → order type id)



---



\## 9. Cross-Module Links



\- `EmailMessage` → `ParseSession` → `ParsedOrderDraft` → `Order`  

\- `EmailMessage.matchedRuleId` → `EmailRules.id`  

\- `EmailMessage.matchedVipEmailId` → `VipEmails.id`  

\- `EmailRule.targetDepartmentId` → `Departments.id`  

\- `EmailRule.targetUserId` → `Users.id`  

\- `EmailRule.emailAccountId` → `EmailAccounts.id`  

\- `VipEmail.notifyUserId` → `Users.id`  

\- `EmailAttachment.fileId` → `Files`  

\- Snapshot cleanup job configured in `email\_parser.md` / scheduler.



---



\# End of Parser Entities



