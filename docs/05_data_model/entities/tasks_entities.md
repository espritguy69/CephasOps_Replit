# Task Entities

## TaskItem

The `TaskItem` entity represents a task or to-do item assigned to a user within a company/department.

### Properties

| Property | Type | Required | Max Length | Description |
|----------|------|----------|------------|-------------|
| `Id` | Guid | Yes | - | Primary key |
| `CompanyId` | Guid | Yes | - | Company scope |
| `DepartmentId` | Guid? | No | - | Optional department scope |
| `RequestedByUserId` | Guid | Yes | - | User who assigned the task |
| `AssignedToUserId` | Guid | Yes | - | User who must complete the task |
| `Title` | string | Yes | 256 | Task title |
| `Description` | string? | No | 4000 | Task description |
| `RequestedAt` | DateTime | Yes | - | When the task was requested |
| `DueAt` | DateTime? | No | - | Due date/time |
| `Priority` | TaskPriority | Yes | - | Priority level (Low, Normal, High) |
| `Status` | TaskStatus | Yes | - | Current status (New, InProgress, OnHold, Completed, Cancelled) |
| `StartedAt` | DateTime? | No | - | When work started (set when status changes to InProgress) |
| `CompletedAt` | DateTime? | No | - | When task was completed |
| `CreatedAt` | DateTime | Yes | - | Creation timestamp |
| `CreatedByUserId` | Guid | Yes | - | User who created the record |
| `UpdatedAt` | DateTime | Yes | - | Last update timestamp |
| `UpdatedByUserId` | Guid | Yes | - | User who last updated the record |

### Enums

#### TaskPriority

- `Low = 0`
- `Normal = 1`
- `High = 2`

#### TaskStatus

- `New = 0`
- `InProgress = 1`
- `OnHold = 2`
- `Completed = 3`
- `Cancelled = 4`

### Indexes

- `IX_TaskItems_CompanyId_AssignedToUserId_Status`: For efficient querying of user tasks by status
- `IX_TaskItems_CompanyId_DepartmentId_Status`: For efficient querying of department tasks by status
- `IX_TaskItems_CompanyId_RequestedByUserId`: For efficient querying of tasks requested by a user

### Business Rules

1. `RequestedByUserId` is automatically set to the current user when creating a task
2. `RequestedAt` is set to current UTC time when creating a task
3. `Status` defaults to `New` when creating a task
4. `StartedAt` is automatically set when status changes from `New` to `InProgress`
5. `CompletedAt` is automatically set when status changes to `Completed`
6. All operations are scoped by `CompanyId` for multi-tenant isolation

