# Task Relationships

## Overview

The Tasks module integrates with several other modules to provide a comprehensive task management system.

## Entity Relationships

### TaskItem Relationships

#### Company (Many-to-One)
- **Relationship**: `TaskItem` → `Company`
- **Foreign Key**: `TaskItem.CompanyId` → `Company.Id`
- **Cardinality**: Many tasks belong to one company
- **Behavior**: All tasks are scoped by company for multi-tenant isolation
- **Cascade**: Restrict (cannot delete company with tasks)

#### Department (Many-to-One, Optional)
- **Relationship**: `TaskItem` → `Department`
- **Foreign Key**: `TaskItem.DepartmentId` → `Department.Id`
- **Cardinality**: Many tasks belong to one department (optional)
- **Behavior**: Tasks can be scoped to a department for better organization
- **Cascade**: Restrict (cannot delete department with tasks)

#### RequestedByUser (Many-to-One)
- **Relationship**: `TaskItem` → `User` (Requestor)
- **Foreign Key**: `TaskItem.RequestedByUserId` → `User.Id`
- **Cardinality**: Many tasks can be requested by one user
- **Behavior**: Tracks who assigned the task
- **Cascade**: Restrict (cannot delete user who requested tasks)

#### AssignedToUser (Many-to-One)
- **Relationship**: `TaskItem` → `User` (Assignee)
- **Foreign Key**: `TaskItem.AssignedToUserId` → `User.Id`
- **Cardinality**: Many tasks can be assigned to one user
- **Behavior**: Tracks who must complete the task
- **Cascade**: Restrict (cannot delete user with assigned tasks)

## Integration Points

### Department Module
- Tasks can be scoped to departments
- Department managers/HODs can view all tasks in their department
- Department-level KPI aggregation

### Identity Module
- User information for requestors and assignees
- User names are resolved for display in DTOs

### KPI Module
- Task completion metrics feed into broader KPI calculations
- On-time vs late completion tracking
- Department and user-level performance metrics

### Notifications (Future)
- Task assignment events
- Status change events
- Due date reminders
- Completion notifications

## Data Flow

1. **Task Creation**:
   - Manager/HOD creates task via API
   - `RequestedByUserId` set to current user
   - `RequestedAt` set to current time
   - `Status` set to `New`
   - Task saved to database

2. **Task Status Update**:
   - User updates task status via API
   - If transitioning to `InProgress`, `StartedAt` is set
   - If transitioning to `Completed`, `CompletedAt` is set
   - `UpdatedByUserId` and `UpdatedAt` are updated
   - Task updated in database

3. **KPI Calculation**:
   - Query tasks by company/department/user
   - Filter to completed tasks
   - Calculate on-time vs late based on `DueAt` and `CompletedAt`
   - Return aggregated metrics

## Access Control

- **User Level**: Users can only see tasks assigned to them (`AssignedToUserId = currentUserId`)
- **Department Level**: Managers/HODs can see all tasks in their department (`DepartmentId = userDepartmentId`)
- **Company Level**: All operations are scoped by `CompanyId` for multi-tenant isolation

