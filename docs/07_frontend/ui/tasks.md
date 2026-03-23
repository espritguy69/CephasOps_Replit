# Tasks UI

## Overview

The Tasks UI provides a comprehensive interface for task management, allowing users to view their assigned tasks, managers to assign tasks, and track task completion through KPIs.

## User Dashboard

### My Tasks Widget

**Location**: User Dashboard (`/dashboard`)

**Purpose**: Display the next 5 tasks assigned to the current user

**Features**:
- List of tasks sorted by:
  - Status (New/InProgress first)
  - Due date (ascending)
- Display for each task:
  - Title
  - Due date (if any) with overdue indicator
  - Status badge (color-coded)
  - Priority badge (Low/Normal/High)
- Click to view full task details

**API Endpoint**: `GET /api/companies/{companyId}/tasks/my`

**Component**: `MyTasksWidget`

## My Tasks Full Screen

### Route
`/tasks/my`

### Features

**Task List**:
- Display all tasks assigned to current user
- Sortable by:
  - Status
  - Due date
  - Priority
  - Requested date

**Filters**:
- Status filter (New, InProgress, Completed, All)
- Due date range filter
- Priority filter (Low, Normal, High)

**Actions**:
- Change task status:
  - New → InProgress
  - InProgress → Completed
  - Any → OnHold
  - Any → Cancelled
- View task details
- Update due date

**API Endpoints**:
- `GET /api/companies/{companyId}/tasks/my?status={status}`
- `PATCH /api/companies/{companyId}/tasks/{taskId}/status`

**Component**: `MyTasksPage`

## Manager / HOD View

### Department Tasks

**Route**: `/tasks/department/:departmentId`

**Purpose**: View and manage all tasks within a department

### Features

**Task List**:
- Display all tasks in the department
- Group by status or assignee
- Filter by:
  - Status
  - Assignee
  - Priority
  - Due date range

**Create Task**:
- Modal or side panel for creating new tasks
- Fields:
  - Title (required)
  - Description
  - Assign to user (dropdown of department members)
  - Due date/time
  - Priority (Low/Normal/High)
- Validation:
  - Title required
  - Assigned user must be in the department

**Actions**:
- Create new task
- View task details
- Update task status (if permitted)
- Reassign task (if permitted)

**API Endpoints**:
- `GET /api/companies/{companyId}/tasks/department/{departmentId}`
- `POST /api/companies/{companyId}/tasks`
- `PATCH /api/companies/{companyId}/tasks/{taskId}/status`

**Component**: `DepartmentTasksPage`

## KPI View

### Route
`/tasks/kpi` or widget on dashboard

### Features

**Metrics Display**:
- Total tasks
- Completed tasks
- On-time completion count
- Late completion count
- On-time percentage
- Completion rate

**Filters**:
- Department (for managers/HODs)
- User (for individual view)
- Date range

**Visualization**:
- Charts/graphs for trends
- Comparison metrics (user vs department average)

**API Endpoint**: `GET /api/companies/{companyId}/tasks/kpi?departmentId={id}&userId={id}&from={date}&to={date}`

**Component**: `TaskKpiWidget` or `TaskKpiPage`

## Task Detail View

### Route
`/tasks/:taskId`

### Features

**Task Information**:
- Title
- Description
- Status (with status change history)
- Priority
- Requested by (user name)
- Assigned to (user name)
- Department (if applicable)
- Requested date
- Due date
- Started date (if in progress or completed)
- Completed date (if completed)

**Actions**:
- Update status (if permitted)
- Update due date
- Add comment (future)
- Attach file (future)

**Component**: `TaskDetailPage`

## UI Components

### TaskCard
- Displays task summary
- Status badge
- Priority indicator
- Due date with overdue warning
- Click to view details

### TaskStatusBadge
- Color-coded status display
- New: Blue
- InProgress: Yellow
- OnHold: Gray
- Completed: Green
- Cancelled: Red

### PriorityBadge
- Low: Gray
- Normal: Blue
- High: Red/Orange

### TaskForm
- Create/Edit task form
- Validation
- User selection (filtered by department if applicable)

### TaskFilters
- Status filter
- Priority filter
- Date range filter
- User filter (for managers)

## State Management

- Tasks list state
- Selected task state
- Filter state
- Loading states
- Error states

## Notifications (Future)

- Toast notifications for:
  - Task assigned
  - Task status changed
  - Task due soon
  - Task overdue
- Real-time updates via SignalR (future)

## Responsive Design

- Mobile-friendly task cards
- Collapsible filters on mobile
- Touch-friendly action buttons
- Optimized for tablet and desktop
