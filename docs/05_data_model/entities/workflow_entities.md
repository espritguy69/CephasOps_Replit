\# Workflow \& Logs Entities  

CephasOps – Workflow \& Logging Data Model  

Version 1.0



This complements other modules with:



\- WorkflowJob

\- BackgroundJob

\- SystemLog (generic logging)



(WorkflowDefinitions \& WorkflowTransitions are in `settings\_entities.md`.)



---



\## 1. WorkflowJob



Represents an in-progress workflow action or scheduled step.



\### 1.1 Table: `WorkflowJobs`



| Field          | Type     | Required | Description                              |

|----------------|----------|----------|------------------------------------------|

| id             | uuid     | yes      | Primary key.                             |

| companyId      | uuid     | yes      | FK → Companies.id.                       |

| workflowDefId  | uuid     | yes      | FK → WorkflowDefinitions.id.             |

| entityType     | string   | yes      | E.g. `Order`.                            |

| entityId       | uuid     | yes      | Entity being processed.                  |

| currentStatus  | string   | yes      | Current business status.                 |

| targetStatus   | string   | yes      | Intended next status.                    |

| state          | enum     | yes      | `Pending`, `Running`, `Succeeded`, `Failed`. |

| lastError      | string   | no       | Last error message if failed.            |

| createdAt      | datetime | yes      | Created timestamp.                       |

| updatedAt      | datetime | yes      | Last update.                             |



---



\## 2. BackgroundJob



Generic for email ingestion, P\&L rebuild, etc.



\### 2.1 Table: `BackgroundJobs`



| Field        | Type     | Required | Description                                  |

|--------------|----------|----------|----------------------------------------------|

| id           | uuid     | yes      | Primary key.                                 |

| jobType      | string   | yes      | `EmailIngest`, `PnlRebuild`, etc.            |

| payloadJson  | json     | yes      | Serialized job data.                         |

| state        | enum     | yes      | `Queued`, `Running`, `Succeeded`, `Failed`.  |

| retryCount   | int      | yes      | Number of attempts.                          |

| lastError    | string   | no       | Last error message.                          |

| createdAt    | datetime | yes      | Created timestamp.                           |

| updatedAt    | datetime | yes      | Last update.                                 |



---



\## 3. SystemLog



Application-level structured logging.



\### 3.1 Table: `SystemLogs`



| Field       | Type     | Required | Description                                |

|-------------|----------|----------|--------------------------------------------|

| id          | uuid     | yes      | Primary key.                               |

| companyId   | uuid     | no       | Nullable for global events.                |

| severity    | enum     | yes      | `Info`, `Warning`, `Error`, `Critical`.    |

| category    | string   | yes      | `Orders`, `Billing`, `Parser`, etc.        |

| message     | string   | yes      | Short description.                         |

| detailsJson | json     | no       | Additional structured info.                |

| createdAt   | datetime | yes      | Timestamp.                                 |



---



\# End of Workflow \& Logs Entities



