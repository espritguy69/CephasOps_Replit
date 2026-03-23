\# Logging \& Audit Entities  

CephasOps – Logging Data Model  

Version 1.0



This file focuses on \*\*auditability\*\* across the whole platform:



\- AuditLog

\- DataChangeLog

\- ApiRequestLog



All are \*\*append-only\*\*.



---



\## 1. AuditLog



High-level “who did what” actions.



\### 1.1 Table: `AuditLogs`



| Field        | Type     | Required | Description                                       |

|--------------|----------|----------|---------------------------------------------------|

| id           | uuid     | yes      | Primary key.                                      |

| companyId    | uuid     | no       | Null for global actions.                          |

| actorUserId  | uuid     | no       | User performing the action.                       |

| actorSiId    | uuid     | no       | SI performing the action (if via SI App).         |

| action       | string   | yes      | `OrderStatusChange`, `CreateInvoice`, etc.        |

| entityType   | string   | no       | `Order`, `Invoice`, `Parser`, etc.                |

| entityId     | uuid     | no       | Target entity ID.                                 |

| summary      | string   | yes      | Short text description.                           |

| metadataJson | json     | no       | Extra details (old/new status, IP, device, etc.). |

| createdAt    | datetime | yes      | Timestamp.                                        |



---



\## 2. DataChangeLog



Fine-grain column-level change tracking (optional, but useful).



\### 2.1 Table: `DataChangeLogs`



| Field        | Type     | Required | Description                           |

|--------------|----------|----------|---------------------------------------|

| id           | uuid     | yes      | Primary key.                          |

| companyId    | uuid     | no       | Nullable.                             |

| tableName    | string   | yes      | Affected table.                       |

| entityId     | uuid     | yes      | PK of the affected row.              |

| columnName   | string   | yes      | Column that changed.                 |

| oldValue     | string   | no       | Previous value (stringified).        |

| newValue     | string   | no       | New value (stringified).             |

| changedById  | uuid     | no       | User/SI who triggered change.        |

| changedAt    | datetime | yes      | Timestamp.                            |



---



\## 3. ApiRequestLog



Optional tracking for API level debugging / compliance.



\### 3.1 Table: `ApiRequestLogs`



| Field        | Type     | Required | Description                        |

|--------------|----------|----------|------------------------------------|

| id           | uuid     | yes      | Primary key.                       |

| companyId    | uuid     | no       | Detected company context.          |

| method       | string   | yes      | `GET`, `POST`, etc.                |

| path         | string   | yes      | URL path.                          |

| statusCode   | int      | yes      | HTTP status.                       |

| durationMs   | int      | yes      | Execution time.                    |

| userId       | uuid     | no       | Authenticated user (if any).       |

| ipAddress    | string   | no       | Client IP.                         |

| createdAt    | datetime | yes      | Timestamp.                         |



---



\# End of Logging \& Audit Entities



