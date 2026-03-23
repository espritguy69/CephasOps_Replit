\# Users, Roles \& Permissions Entities  

CephasOps – RBAC Data Model  

Version 1.0



Defines:



\- User

\- Role

\- Permission

\- UserCompanyMembership

\- UserRoleAssignment



---



\## 1. User



\### 1.1 Table: `Users`



| Field        | Type     | Required | Description                |

|--------------|----------|----------|----------------------------|

| id           | uuid     | yes      | Primary key.               |

| name         | string   | yes      | Full name.                 |

| email        | string   | yes      | Login email.               |

| phone        | string   | no       | Contact number.            |

| isActive     | boolean  | yes      | Enabled/disabled.          |

| createdAt    | datetime | yes      | Created timestamp.         |



---



\## 2. Role



\### 2.1 Table: `Roles`



| Field     | Type   | Required | Description                         |

|-----------|--------|----------|-------------------------------------|

| id        | uuid   | yes      | Primary key.                        |

| name      | string | yes      | `Admin`, `Scheduler`, `Warehouse`. |

| scope     | string | yes      | `Company`, `Global`.                |



---



\## 3. Permission



\### 3.1 Table: `Permissions`



| Field        | Type   | Required | Description                  |

|--------------|--------|----------|------------------------------|

| id           | uuid   | yes      | Primary key.                 |

| name         | string | yes      | Machine name (`orders.view`).|

| description  | string | no       | Human readable.              |



\### 3.2 Table: `RolePermissions`



| Field        | Type   | Required |

|--------------|--------|----------|

| roleId       | uuid   | yes      |

| permissionId | uuid   | yes      |



---



\## 4. UserCompanyMembership



\### 4.1 Table: `UserCompanies`



| Field      | Type   | Required | Description                      |

|------------|--------|----------|----------------------------------|

| id         | uuid   | yes      | Primary key.                     |

| userId     | uuid   | yes      | FK → Users.id.                   |

| companyId  | uuid   | yes      | FK → Companies.id.               |

| isDefault  | boolean| yes      | Default company on login.        |



---



\## 5. UserRoleAssignment



\### 5.1 Table: `UserRoles`



| Field      | Type   | Required | Description                                   |

|------------|--------|----------|-----------------------------------------------|

| id         | uuid   | yes      | Primary key.                                  |

| userId     | uuid   | yes      | FK → Users.id.                                |

| companyId  | uuid   | yes      | FK → Companies.id (for company-scoped roles). |

| roleId     | uuid   | yes      | FK → Roles.id.                                |



---



\# End of Users \& RBAC Entities



