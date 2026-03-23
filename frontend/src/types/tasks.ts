/**
 * Task Types - Shared type definitions for Tasks module
 */

export enum TaskStatus {
  New = 0,
  InProgress = 1,
  OnHold = 2,
  Completed = 3,
  Cancelled = 4
}

// Legacy enum values for backward compatibility (mapped to new values)
export const LegacyTaskStatus = {
  Pending: TaskStatus.New,
  InProgress: TaskStatus.InProgress,
  OnHold: TaskStatus.OnHold,
  Completed: TaskStatus.Completed,
  Cancelled: TaskStatus.Cancelled
} as const;

export enum TaskPriority {
  Low = 0,
  Normal = 1,
  High = 2
}

// Legacy priority enum values for backward compatibility
export const LegacyTaskPriority = {
  Low: TaskPriority.Low,
  Normal: TaskPriority.Normal,
  High: TaskPriority.High,
  Urgent: TaskPriority.High // Map Urgent to High for backend compatibility
} as const;

/**
 * Task interface matching backend TaskDto structure
 * Backend returns Status and Priority as strings
 */
export interface Task {
  id: string;
  companyId?: string;
  departmentId?: string;
  requestedByUserId: string;
  assignedToUserId: string;
  title: string;
  description?: string;
  requestedAt: string;
  dueAt?: string;
  priority: string; // Backend returns "Low", "Normal", "High"
  status: string; // Backend returns "New", "InProgress", "OnHold", "Completed", "Cancelled"
  startedAt?: string;
  completedAt?: string;
  createdAt: string;
  createdByUserId: string;
  updatedAt: string;
  updatedByUserId: string;
  
  // Optional computed/display fields
  assignedToUserName?: string;
  requestedByUserName?: string;
  departmentName?: string;
  daysUntilDue?: number | null;
  isOverdue?: boolean;
}

export interface CreateTaskRequest {
  departmentId?: string;
  assignedToUserId: string;
  title: string;
  description?: string;
  dueAt?: string;
  priority?: string; // "Low", "Normal", "High"
}

export interface UpdateTaskRequest {
  assignedToUserId?: string;
  title?: string;
  description?: string;
  dueAt?: string;
  priority?: string;
  status?: string;
}

export interface UpdateTaskStatusRequest {
  status: TaskStatus;
  notes?: string;
}

export interface TasksListFilters {
  assignedToUserId?: string; // Use "current" for authenticated user
  requestedByUserId?: string; // Use "current" for authenticated user
  departmentId?: string;
  status?: string; // Backend status string: "New", "InProgress", "OnHold", "Completed", "Cancelled"
  priority?: string; // Backend priority string: "Low", "Normal", "High"
}

export interface TaskFilters {
  status?: TaskStatus;
  departmentId?: string;
  assignedToUserId?: string;
  periodStart?: string;
  periodEnd?: string;
}

export interface TaskKpiSummary {
  total: number;
  pending: number;
  inProgress: number;
  completed: number;
  overdue: number;
  averageCompletionTime?: number;
}

/**
 * Helper functions to map between backend string values and frontend enums
 */
export const mapStatusStringToEnum = (status: string): TaskStatus => {
  const statusMap: Record<string, TaskStatus> = {
    'New': TaskStatus.New,
    'InProgress': TaskStatus.InProgress,
    'OnHold': TaskStatus.OnHold,
    'Completed': TaskStatus.Completed,
    'Cancelled': TaskStatus.Cancelled
  };
  return statusMap[status] ?? TaskStatus.New;
};

export const mapStatusEnumToString = (status: TaskStatus): string => {
  const statusMap: Record<TaskStatus, string> = {
    [TaskStatus.New]: 'New',
    [TaskStatus.InProgress]: 'InProgress',
    [TaskStatus.OnHold]: 'OnHold',
    [TaskStatus.Completed]: 'Completed',
    [TaskStatus.Cancelled]: 'Cancelled'
  };
  return statusMap[status] ?? 'New';
};

export const mapPriorityStringToEnum = (priority: string): TaskPriority => {
  const priorityMap: Record<string, TaskPriority> = {
    'Low': TaskPriority.Low,
    'Normal': TaskPriority.Normal,
    'High': TaskPriority.High
  };
  return priorityMap[priority] ?? TaskPriority.Normal;
};

export const mapPriorityEnumToString = (priority: TaskPriority): string => {
  const priorityMap: Record<TaskPriority, string> = {
    [TaskPriority.Low]: 'Low',
    [TaskPriority.Normal]: 'Normal',
    [TaskPriority.High]: 'High'
  };
  return priorityMap[priority] ?? 'Normal';
};

