# Task Entities

CephasOps – Tasks Domain Data Model

Version 1.0

---

## 1. TaskItem

Represents a single task or to-do item assigned to a user.

### 1.1 Table: `TaskItems`

| Field              | Type      | Required | Description                                    |
|--------------------|-----------|----------|------------------------------------------------|
| id                 | uuid      | yes      | Primary key                                    |
| companyId          | uuid      | yes      | FK → Companies.id                              |
| departmentId       | uuid      | no       | FK → Departments.id (nullable)                 |
| requestedByUserId  | uuid      | yes      | FK → Users.id (who created/requested the task) |
| assignedToUserId  | uuid      | yes      | FK → Users.id (who must complete the task)    |
| title              | string    | yes      | Task title (max 256 chars)                    |
| description        | string    | no       | Task description (max 4000 chars)              |
| requestedAt        | datetime  | yes      | When task was requested/created               |
| dueAt              | datetime  | no       | Optional due date                             |
| priority           | enum      | yes      | TaskPriority (Low, Normal, High, Urgent)      |
| status             | enum      | yes      | TaskStatus (Pending, InProgress, OnHold, Completed, Cancelled) |
| startedAt          | datetime  | no       | When task was started (set on InProgress)     |
| completedAt        | datetime  | no       | When task was completed (set on Completed)    |
| createdAt          | datetime  | yes      | Created timestamp                             |
| updatedAt          | datetime  | yes      | Last update timestamp                         |
| createdByUserId    | uuid      | yes      | FK → Users.id                                 |
| updatedByUserId    | uuid      | yes      | FK → Users.id                                 |

### 1.2 Relationships

- **Company** (many-to-one): `TaskItem.CompanyId → Company.Id`
- **Department** (many-to-one, optional): `TaskItem.DepartmentId → Department.Id`
- **RequestedByUser** (many-to-one): `TaskItem.RequestedByUserId → User.Id`
- **AssignedToUser** (many-to-one): `TaskItem.AssignedToUserId → User.Id`

### 1.3 Indexes

1. `IX_TaskItems_CompanyId_AssignedToUserId_Status`
   - For: "My Tasks" queries filtered by status
   - Columns: `(CompanyId, AssignedToUserId, Status)`

2. `IX_TaskItems_CompanyId_DepartmentId_Status`
   - For: Department task views
   - Columns: `(CompanyId, DepartmentId, Status)`

3. `IX_TaskItems_CompanyId_RequestedByUserId`
   - For: Tasks requested by a user
   - Columns: `(CompanyId, RequestedByUserId)`

4. `IX_TaskItems_CompanyId_DueAt`
   - For: Overdue task queries
   - Columns: `(CompanyId, DueAt)`

### 1.4 Constraints

- **Unique**: None (multiple tasks can have same title, assigned user, etc.)
- **Foreign Keys**: All enforced with `ON DELETE RESTRICT`
- **Check Constraints**: None (enforced in application layer)

---

## 2. Enums

### 2.1 TaskPriority

```csharp
public enum TaskPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}
```

**Usage:**
- `Low`: Non-urgent, can be deferred
- `Normal`: Standard priority (default)
- `High`: Important, should be prioritized
- `Urgent`: Critical, requires immediate attention

### 2.2 TaskStatus

```csharp
public enum TaskStatus
{
    Pending = 1,
    InProgress = 2,
    OnHold = 3,
    Completed = 4,
    Cancelled = 5
}
```

**Status Flow:**
- `Pending` → `InProgress` → `Completed`
- `InProgress` → `OnHold` → `InProgress`
- Any status → `Cancelled` (final)

---

## 3. Computed Fields (Application Layer)

These are calculated in the service layer, not stored:

- `IsOverdue`: `DueAt < Now && Status != Completed && Status != Cancelled`
- `DaysUntilDue`: `(DueAt - Now).Days` (can be negative for overdue)

---

## 4. Data Integrity Rules

1. **CompanyId Required**: Every task must belong to a company
2. **AssignedToUserId Required**: Every task must be assigned to a user
3. **RequestedByUserId Required**: Every task must have a requester
4. **DepartmentId Optional**: Tasks can exist without department
5. **DueAt Optional**: Tasks can exist without due dates
6. **StartedAt Set Automatically**: Set when status changes to InProgress
7. **CompletedAt Set Automatically**: Set when status changes to Completed
8. **Status Immutability**: Completed and Cancelled tasks cannot change status

---

## 5. Multi-Company Isolation

- All queries MUST filter by `CompanyId`
- Tasks cannot be viewed across companies
- Department filtering respects company boundaries
- User assignment must be within the same company

---

## 6. Example Data

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "companyId": "123e4567-e89b-12d3-a456-426614174000",
  "departmentId": "789e0123-e45b-67c8-d901-234567890abc",
  "requestedByUserId": "111e2222-e33b-44c5-d666-777888999aaa",
  "assignedToUserId": "222e3333-e44b-55c6-d777-888999000bbb",
  "title": "Review Q4 Financial Reports",
  "description": "Complete review of Q4 financial reports and prepare summary for board meeting",
  "requestedAt": "2025-01-15T10:00:00Z",
  "dueAt": "2025-01-22T17:00:00Z",
  "priority": 3,
  "status": 1,
  "startedAt": null,
  "completedAt": null,
  "createdAt": "2025-01-15T10:00:00Z",
  "updatedAt": "2025-01-15T10:00:00Z",
  "createdByUserId": "111e2222-e33b-44c5-d666-777888999aaa",
  "updatedByUserId": "111e2222-e33b-44c5-d666-777888999aaa"
}
```

---

## End of Task Entities

