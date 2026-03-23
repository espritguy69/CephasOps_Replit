# Tasks Module

## Overview

The Tasks module provides a comprehensive task management system for CephasOps, allowing managers and HODs to assign tasks to users within their company/department, track task completion, and measure performance through KPIs.

## Features

- **Task Assignment**: Managers/HODs can assign tasks to users within their company/department
- **User Dashboard**: Users can view their assigned tasks on their dashboard
- **Department View**: Managers/HODs can view all tasks within their department
- **Status Tracking**: Tasks can be tracked through multiple statuses (New, InProgress, OnHold, Completed, Cancelled)
- **Priority Management**: Tasks support priority levels (Low, Normal, High)
- **KPI Measurement**: Track on-time vs late completion per user and per department
- **Notifications**: Lightweight logging hooks for task events (ready for SignalR integration)

## Multi-Company & Multi-Department Support

All tasks are scoped by `CompanyId` and optionally by `DepartmentId`, ensuring proper data isolation and access control.

## Domain Model

### TaskItem Entity

- `Id` (Guid): Unique identifier
- `CompanyId` (Guid): Company scope
- `DepartmentId` (Guid?): Optional department scope
- `RequestedByUserId` (Guid): User who assigned the task
- `AssignedToUserId` (Guid): User who must complete the task
- `Title` (string, max 256): Task title
- `Description` (string?, max 4000): Task description
- `RequestedAt` (DateTime): When the task was requested
- `DueAt` (DateTime?): Due date/time
- `Priority` (TaskPriority enum): Low, Normal, High
- `Status` (TaskStatus enum): New, InProgress, OnHold, Completed, Cancelled
- `StartedAt` (DateTime?): When work started (set when status changes to InProgress)
- `CompletedAt` (DateTime?): When task was completed
- `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`: Audit fields

### Enums

**TaskPriority:**
- Low = 0
- Normal = 1
- High = 2

**TaskStatus:**
- New = 0
- InProgress = 1
- OnHold = 2
- Completed = 3
- Cancelled = 4

## API Endpoints

### Get My Tasks
```
GET /api/companies/{companyId}/tasks/my
```
Returns all tasks assigned to the current user, optionally filtered by status.

### Get Department Tasks
```
GET /api/companies/{companyId}/tasks/department/{departmentId}
```
Returns all tasks within a department (requires Manager/HOD permissions).

### Create Task
```
POST /api/companies/{companyId}/tasks
Body: CreateTaskItemCommand
```
Creates a new task and assigns it to a user.

### Update Task Status
```
PATCH /api/companies/{companyId}/tasks/{taskId}/status
Body: UpdateTaskItemStatusCommand
```
Updates the task status. Automatically sets `StartedAt` when transitioning from New to InProgress, and `CompletedAt` when status changes to Completed.

### Get Task KPI
```
GET /api/companies/{companyId}/tasks/kpi?departmentId={id}&userId={id}&from={date}&to={date}
```
Returns KPI summary including:
- Total tasks
- Completed tasks
- On-time tasks (completed before or on due date)
- Late tasks (completed after due date)

## Service Methods

### ITaskService

- `GetMyTasksAsync(companyId, userId, status?, cancellationToken)`: Get tasks assigned to a user
- `GetDepartmentTasksAsync(companyId, departmentId, status?, cancellationToken)`: Get tasks in a department
- `CreateTaskAsync(command, requestedByUserId, cancellationToken)`: Create a new task
- `UpdateTaskStatusAsync(command, updatedByUserId, cancellationToken)`: Update task status
- `GetTaskKpiAsync(companyId, departmentId?, userId?, periodStart?, periodEnd?, cancellationToken)`: Get KPI summary
- `GetTaskByIdAsync(taskId, companyId, cancellationToken)`: Get a single task by ID
- `GetOverdueTasksAsync(companyId, departmentId?, userId?, cancellationToken)`: Get overdue tasks

## Business Rules

1. **Task Creation**:
   - `RequestedByUserId` is automatically set to the current user
   - `RequestedAt` is set to current UTC time
   - `Status` defaults to `New`
   - If `DepartmentId` is null, it may be inferred from the assigned user's primary department (future enhancement)

2. **Status Transitions**:
   - When status changes from `New` to `InProgress`, `StartedAt` is automatically set
   - When status changes to `Completed`, `CompletedAt` is automatically set
   - `UpdatedByUserId` and `UpdatedAt` are always updated on status changes

3. **KPI Calculation**:
   - Only completed tasks are considered for KPI metrics
   - On-time: `CompletedAt <= DueAt` (both must be non-null)
   - Late: `CompletedAt > DueAt` (both must be non-null)

4. **Access Control**:
   - Users can only see tasks assigned to them (via "My Tasks")
   - Managers/HODs can see all tasks in their department
   - All operations are scoped by `CompanyId`

## Integration Points

- **Department Module**: Tasks can be scoped to departments
- **Identity Module**: User information for requestors and assignees
- **KPI Module**: Task completion metrics can feed into broader KPI calculations
- **Notifications Module**: 
  - Task assignment triggers notification to assigned user
  - Task completion triggers notification to requester
  - Uses `NotificationTaskDefaultChannel` setting
  - Respects `NotificationTaskStrictMode` (always creates IN_APP if enabled)

## Future Enhancements

- Real-time notifications via SignalR
- Email notifications for task assignments and status changes
- Task templates for recurring tasks
- Task dependencies and subtasks
- File attachments
- Comments and activity log
