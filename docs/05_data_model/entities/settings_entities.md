# Settings & Configuration Entities  
CephasOps – Settings Domain Data Model  
Version 1.0

Defines:

- GlobalSetting (system-wide)
- CompanySetting
- ParserTemplate
- WorkflowDefinition
- WorkflowTransition
- BuildingType
- KpiProfile

---

## 1. GlobalSetting

### 1.1 Table: `GlobalSettings`

| Field   | Type   | Required | Description                        |
|---------|--------|----------|------------------------------------|
| key     | string | yes      | Primary key (unique).              |
| value   | string | yes      | JSON/string value.                 |
| notes   | string | no       | Description.                       |

---

## 2. CompanySetting

### 2.1 Table: `CompanySettings`

| Field     | Type     | Required | Description                           |
|-----------|----------|----------|---------------------------------------|
| id        | uuid     | yes      | Primary key.                          |
| companyId | uuid     | yes      | FK → Companies.id.                    |
| key       | string   | yes      | Setting key (`billing.sstEnabled`).   |
| value     | string   | yes      | JSON/string payload.                  |
| createdAt | datetime | yes      | Created timestamp.                    |
| updatedAt | datetime | yes      | Last modification.                    |

---

## 3. ParserTemplate

### 3.1 Table: `ParserTemplates`

| Field            | Type     | Required | Description                                   |
|------------------|----------|----------|-----------------------------------------------|
| id               | uuid     | yes      | Primary key.                                  |
| companyId        | uuid     | yes      | FK → Companies.id.                            |
| partnerId        | uuid     | no       | FK → Partners.id (TIME, etc.).               |
| templateName     | string   | yes      | E.g. `TIME_activation_v3`.                    |
| templateType     | enum     | yes      | `Email`, `Excel`, `Pdf`.                      |
| matchCriteria    | json     | yes      | Subject keywords, from-address, etc.         |
| mappingConfig    | json     | yes      | Column-to-field mapping, sheet names, etc.   |
| isActive         | boolean  | yes      | Active/inactive.                              |
| version          | int      | yes      | Template version.                             |
| createdAt        | datetime | yes      | Created timestamp.                            |

---

## 4. WorkflowDefinition & WorkflowTransition

### 4.1 Table: `WorkflowDefinitions`

| Field           | Type     | Required | Description                           |
|-----------------|----------|----------|---------------------------------------|
| id              | uuid     | yes      | Primary key.                          |
| companyId       | uuid     | yes      | FK → Companies.id.                    |
| name            | string   | yes      | E.g. `ISP_Order_Workflow`.            |
| entityType      | string   | yes      | `Order`, `Invoice`, etc.              |
| isActive        | boolean  | yes      | Active/inactive.                      |
| createdAt       | datetime | yes      | Created timestamp.                    |

### 4.2 Table: `WorkflowTransitions`

| Field             | Type     | Required | Description                                  |
|-------------------|----------|----------|----------------------------------------------|
| id                | uuid     | yes      | Primary key.                                 |
| workflowDefId     | uuid     | yes      | FK → WorkflowDefinitions.id.                 |
| companyId         | uuid     | yes      | FK → Companies.id.                           |
| fromStatus        | enum     | no       | Starting status (null = initial).           |
| toStatus          | enum     | yes      | Target status.                               |
| allowedRoles      | json     | yes      | List of roles allowed to trigger.           |
| guardConditions   | json     | no       | Rules (photos required, docket uploaded…).  |
| sideEffectsConfig | json     | no       | Notifications, logging, etc.                |
| createdAt         | datetime | yes      | Created timestamp.                           |

---

## 5. BuildingType

### 5.1 Table: `BuildingTypes`

| Field       | Type     | Required | Description                     |
|-------------|----------|----------|---------------------------------|
| id          | uuid     | yes      | Primary key.                    |
| companyId   | uuid     | yes      | FK → Companies.id.             |
| name        | string   | yes      | `HighRise`, `Landed`, etc.     |
| description | string   | no       | Extra info.                     |

---

## 6. KpiProfile

### 6.1 Table: `KpiProfiles`

| Field         | Type     | Required | Description                          |
|---------------|----------|----------|--------------------------------------|
| id            | uuid     | yes      | Primary key.                         |
| companyId     | uuid     | yes      | FK → Companies.id.                   |
| name          | string   | yes      | KPI name.                            |
| entityType    | string   | yes      | Usually `Order`.                     |
| orderTypeId   | uuid     | no       | Optional filter.                     |
| slaHours      | int      | yes      | Target completion time in hours.     |
| isActive      | boolean  | yes      | Active flag.                         |
| createdAt     | datetime | yes      | Created timestamp.                   |

---

# End of Settings Entities
