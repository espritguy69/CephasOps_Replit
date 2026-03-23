# Tasks – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Tasks module, covering task creation, assignment, status transitions, and completion

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         TASKS MODULE SYSTEM                              │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   TASK CREATION        │      │   TASK MANAGEMENT     │
        │  (Request, Assignment) │      │  (Status, Updates)    │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Create Task          │      │ • Update Status       │
        │ • Assign to User        │      │ • Update Details      │
        │ • Set Priority          │      │ • Track Progress      │
        │ • Set Due Date          │      │ • Complete Task        │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   TASK STATUS FLOW     │      │   TASK VIEWS          │
        │  (New → InProgress →   │      │  (List, Kanban)       │
        │   Completed)           │      │                       │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Task Lifecycle

```
[STEP 1: TASK CREATION]
         |
         v
┌────────────────────────────────────────┐
│ CREATE TASK REQUEST                      │
│ POST /api/tasks                          │
└────────────────────────────────────────┘
         |
         v
CreateTaskDto {
  Title: "Review invoice #12345"
  Description: "Please review the invoice for accuracy"
  AssignedToUserId: "user-456"
  DepartmentId: "dept-123" (optional)
  Priority: "High"
  DueAt: 2025-12-20 17:00:00
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE TASK DATA                       │
│ TaskService.CreateTaskAsync()            │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Return Validation Errors]
   |
   v
Checks:
  ✓ Title not empty
  ✓ AssignedToUserId exists
  ✓ DepartmentId exists (if provided)
  ✓ Priority valid (Low, Normal, High, Urgent)
  ✓ DueAt in future (if provided)
         |
         v
┌────────────────────────────────────────┐
│ CREATE TASK RECORD                       │
└────────────────────────────────────────┘
         |
         v
TaskItem {
  Id: "task-789"
  CompanyId: Cephas
  DepartmentId: "dept-123"
  RequestedByUserId: "user-123" (current user)
  AssignedToUserId: "user-456"
  Title: "Review invoice #12345"
  Description: "Please review the invoice for accuracy"
  Priority: "High"
  Status: "New"
  RequestedAt: 2025-12-12 10:00:00
  DueAt: 2025-12-20 17:00:00
  CreatedAt: 2025-12-12 10:00:00
  CreatedByUserId: "user-123"
}
         |
         v
[Save to Database]
  _context.TaskItems.Add(task)
  await _context.SaveChangesAsync()
         |
         v
[STEP 2: TASK ASSIGNMENT NOTIFICATION]
         |
         v
[Optional: Send Notification]
  Notification {
    UserId: "user-456" (assigned user)
    Type: "TaskAssigned"
    Title: "New Task Assigned"
    Message: "You have been assigned: Review invoice #12345"
    RelatedEntityType: "Task"
    RelatedEntityId: "task-789"
  }
         |
         v
[STEP 3: TASK VIEWING]
         |
         v
[Assigned User Views Tasks]
  GET /api/tasks?assignedToUserId=user-456
         |
         v
┌────────────────────────────────────────┐
│ GET TASKS                                │
│ TaskService.GetTasksAsync()             │
└────────────────────────────────────────┘
         |
         v
[Query Tasks]
  TaskItem.find(
    CompanyId = Cephas
    AssignedToUserId = "user-456"
    Status = "New" | "InProgress"
  )
         |
         v
[Return Task List]
  [
    {
      Id: "task-789"
      Title: "Review invoice #12345"
      Status: "New"
      Priority: "High"
      DueAt: 2025-12-20 17:00:00
      AssignedToUserId: "user-456"
    }
  ]
         |
         v
[STEP 4: TASK STATUS UPDATE]
         |
         v
[User Starts Working on Task]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE TASK STATUS                       │
│ PUT /api/tasks/{id}                     │
└────────────────────────────────────────┘
         |
         v
UpdateTaskDto {
  Status: "InProgress"
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE STATUS TRANSITION                │
│ TaskService.UpdateTaskAsync()            │
└────────────────────────────────────────┘
         |
         v
[Check Current Status]
  Current: "New"
  Target: "InProgress"
         |
         v
[Valid Transition?]
  New → InProgress: ✓ Allowed
         |
         v
[Update Task]
  TaskItem {
    Status: "InProgress"
    StartedAt: 2025-12-12 14:00:00 (auto-set)
    UpdatedAt: 2025-12-12 14:00:00
    UpdatedByUserId: "user-456"
  }
         |
         v
[Save Changes]
  await _context.SaveChangesAsync()
         |
         v
[STEP 5: TASK PROGRESS UPDATE]
         |
         v
[User Updates Task Details]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE TASK DETAILS                      │
│ PUT /api/tasks/{id}                     │
└────────────────────────────────────────┘
         |
         v
UpdateTaskDto {
  Description: "Reviewed invoice. Found 2 discrepancies. Waiting for clarification."
  Priority: "Normal" (downgraded)
}
         |
         v
[Update Task]
  TaskItem {
    Description: "Reviewed invoice. Found 2 discrepancies. Waiting for clarification."
    Priority: "Normal"
    UpdatedAt: 2025-12-15 11:00:00
  }
         |
         v
[STEP 6: TASK COMPLETION]
         |
         v
[User Completes Task]
         |
         v
┌────────────────────────────────────────┐
│ MARK TASK AS COMPLETED                   │
│ PUT /api/tasks/{id}                     │
└────────────────────────────────────────┘
         |
         v
UpdateTaskDto {
  Status: "Completed"
}
         |
         v
[Update Task]
  TaskItem {
    Status: "Completed"
    CompletedAt: 2025-12-18 16:00:00 (auto-set)
    UpdatedAt: 2025-12-18 16:00:00
  }
         |
         v
[Save Changes]
  await _context.SaveChangesAsync()
         |
         v
[Task Lifecycle Complete]
```

---

## Task Status Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    TASK STATUS TRANSITIONS                               │
└─────────────────────────────────────────────────────────────────────────┘

[New]
  │
  ├─→ [InProgress] (when user starts work)
  │     │
  │     ├─→ [Completed] (when task finished)
  │     │
  │     └─→ [New] (if reassigned or reset)
  │
  └─→ [Cancelled] (if task no longer needed)

STATUS DEFINITIONS:
───────────────────
New: Task created, not yet started
InProgress: Task is being worked on
Completed: Task finished successfully
Cancelled: Task cancelled, no longer needed

AUTO-SET TIMESTAMPS:
────────────────────
New → InProgress: StartedAt = NOW()
Any → Completed: CompletedAt = NOW()
```

---

## Task Assignment Workflow

```
[Task Created]
  TaskItem {
    AssignedToUserId: "user-456"
  }
         |
         v
┌────────────────────────────────────────┐
│ REASSIGN TASK                            │
│ PUT /api/tasks/{id}                     │
└────────────────────────────────────────┘
         |
         v
UpdateTaskDto {
  AssignedToUserId: "user-789" (new assignee)
}
         |
         v
[Update Assignment]
  TaskItem {
    AssignedToUserId: "user-789"
    UpdatedAt: DateTime.UtcNow
  }
         |
         v
[Optional: Reset Status]
  If task was InProgress:
    Status: "New"
    StartedAt: null
         |
         v
[Save Changes]
         |
         v
[Notify New Assignee]
  Notification {
    UserId: "user-789"
    Type: "TaskAssigned"
    Title: "Task Reassigned to You"
    Message: "Task 'Review invoice #12345' has been assigned to you"
  }
```

---

## Task Priority Management

```
[Task Priority Levels]
  - Low
  - Normal (default)
  - High
  - Urgent
         |
         v
[Priority-Based Filtering]
  GET /api/tasks?priority=High
         |
         v
[Priority-Based Sorting]
  GET /api/tasks?sortBy=priority&sortOrder=desc
         |
         v
[Due Date + Priority]
  Tasks sorted by:
    1. Due Date (ascending)
    2. Priority (High → Urgent → Normal → Low)
```

---

## Kanban Board Workflow

```
[Kanban View]
  Columns:
    - New
    - InProgress
    - Completed
         |
         v
[Get Tasks by Status]
  GET /api/tasks?status=New
  GET /api/tasks?status=InProgress
  GET /api/tasks?status=Completed
         |
         v
[Display in Columns]
  New Column: [Task 1] [Task 2] [Task 3]
  InProgress Column: [Task 4] [Task 5]
  Completed Column: [Task 6] [Task 7]
         |
         v
[Drag & Drop Task]
  PUT /api/tasks/{id}
  {
    Status: "InProgress" (new status from column)
  }
         |
         v
[Update Task Status]
  TaskItem.Status = newStatus
  If InProgress: StartedAt = NOW()
  If Completed: CompletedAt = NOW()
```

---

## Entities Involved

### TaskItem Entity
```
TaskItem
├── Id (Guid)
├── CompanyId (Guid)
├── DepartmentId (Guid?)
├── RequestedByUserId (Guid)
├── AssignedToUserId (Guid)
├── Title (string)
├── Description (string?)
├── Priority (string: Low, Normal, High, Urgent)
├── Status (string: New, InProgress, Completed, Cancelled)
├── RequestedAt (DateTime)
├── DueAt (DateTime?)
├── StartedAt (DateTime?)
├── CompletedAt (DateTime?)
├── CreatedAt (DateTime)
├── CreatedByUserId (Guid)
├── UpdatedAt (DateTime)
└── UpdatedByUserId (Guid)
```

---

## API Endpoints Involved

### Task Management
- `GET /api/tasks` - List tasks with filters
  - Query params: `assignedToUserId`, `requestedByUserId`, `departmentId`, `status`, `priority`
  - Response: `List<TaskDto>`

- `GET /api/tasks/{id}` - Get task details
  - Response: `TaskDto`

- `POST /api/tasks` - Create new task
  - Request: `CreateTaskDto { Title, Description, AssignedToUserId, DepartmentId, Priority, DueAt }`
  - Response: `TaskDto`

- `PUT /api/tasks/{id}` - Update task
  - Request: `UpdateTaskDto { Title?, Description?, AssignedToUserId?, Status?, Priority?, DueAt? }`
  - Response: `TaskDto`

- `DELETE /api/tasks/{id}` - Delete task
  - Response: 204 No Content

---

## Module Rules & Validations

### Task Creation Rules
- Title is required (not empty)
- AssignedToUserId must exist and be active user
- DepartmentId must exist (if provided)
- Priority must be one of: Low, Normal, High, Urgent
- DueAt must be in future (if provided)
- RequestedByUserId is automatically set to current user

### Status Transition Rules
- New → InProgress: Allowed (auto-sets StartedAt)
- New → Completed: Allowed (auto-sets CompletedAt)
- InProgress → Completed: Allowed (auto-sets CompletedAt)
- Any → Cancelled: Allowed
- Completed → InProgress: Allowed (resets CompletedAt)
- Cancelled → Any: Not allowed (cancelled tasks are terminal)

### Assignment Rules
- Only task creator or Admin can reassign
- Reassignment can reset status to "New" if task was InProgress
- Reassignment triggers notification to new assignee

### Priority Rules
- Default priority is "Normal"
- Priority can be changed at any time
- Priority affects task sorting and filtering

### Due Date Rules
- Due date is optional
- Due date must be in future when creating
- Overdue tasks can be filtered: `GET /api/tasks?overdue=true`

---

## Integration Points

### Notifications Module
- Task assignment triggers notification
- Task reassignment triggers notification
- Task completion can trigger notification (optional)
- Due date reminders (optional, future feature)

### Users Module
- Task assignment requires valid user
- User roles may affect task visibility
- User deactivation affects task assignment

### Departments Module
- Tasks can be scoped to departments
- Department filtering affects task visibility
- Department managers can see all department tasks

### Workflow Engine
- Tasks can be created from workflow side effects
- Task completion can trigger workflow transitions
- Task status can be controlled by workflow (future)

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/tasks/OVERVIEW.md` - Tasks module overview

