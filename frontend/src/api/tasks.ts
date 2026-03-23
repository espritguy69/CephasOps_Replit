import apiClient from './client';
import type {
  Task,
  TaskStatus,
  CreateTaskRequest,
  UpdateTaskRequest,
  UpdateTaskStatusRequest,
  TasksListFilters,
  TaskFilters,
  TaskKpiSummary
} from '../types/tasks';

/**
 * Tasks API
 * Handles task management, status updates, and KPI tracking
 * 
 * Note: Backend returns List<TaskDto> directly (not wrapped in envelope)
 */

// Export enum (re-export from types)
export { TaskStatus } from '../types/tasks';

/**
 * Get all tasks with comprehensive filtering
 * @param filters - Filter options (assignedToUserId, requestedByUserId, departmentId, status)
 * @returns Array of task items
 */
export const getAllTasks = async (filters: TasksListFilters = {}): Promise<Task[]> => {
  const params: Record<string, any> = {};
  
  if (filters.assignedToUserId) {
    params.assignedToUserId = filters.assignedToUserId;
  }
  if (filters.requestedByUserId) {
    params.requestedByUserId = filters.requestedByUserId;
  }
  if (filters.departmentId) {
    params.departmentId = filters.departmentId;
  }
  if (filters.status) {
    params.status = filters.status;
  }
  if (filters.priority) {
    params.priority = filters.priority;
  }
  
  // Backend returns List<TaskDto> directly, not wrapped in envelope
  const response = await apiClient.get<Task[]>('/tasks', { params });
  return response;
};

/**
 * Get tasks assigned to the current user
 * @param status - Optional status filter (TaskStatus enum value)
 * @returns Array of task items
 */
export const getMyTasks = async (status: TaskStatus | null = null): Promise<Task[]> => {
  const params: Record<string, any> = { assignedToUserId: 'current' };
  if (status !== null) {
    // Convert enum to backend string format
    const statusMap: Record<number, string> = {
      0: 'New',
      1: 'InProgress',
      2: 'OnHold',
      3: 'Completed',
      4: 'Cancelled'
    };
    params.status = statusMap[status] || 'New';
  }
  const response = await apiClient.get<Task[]>('/tasks', { params });
  return response;
};

/**
 * Get tasks for a department
 * @param departmentId - Department ID
 * @param status - Optional status filter
 * @returns Array of task items
 */
export const getDepartmentTasks = async (
  departmentId: string,
  status: TaskStatus | null = null
): Promise<Task[]> => {
  const params: Record<string, any> = { departmentId };
  if (status !== null) {
    // Convert enum to backend string format
    const statusMap: Record<number, string> = {
      0: 'New',
      1: 'InProgress',
      2: 'OnHold',
      3: 'Completed',
      4: 'Cancelled'
    };
    params.status = statusMap[status] || 'New';
  }
  const response = await apiClient.get<Task[]>('/tasks', { params });
  return response;
};

/**
 * Get a single task by ID
 * @param taskId - Task ID
 * @returns Task item
 */
export const getTask = async (taskId: string): Promise<Task> => {
  const response = await apiClient.get<Task>(`/tasks/${taskId}`);
  return response;
};

/**
 * Create a new task
 * @param taskData - Task creation data
 * @returns Created task item
 */
export const createTask = async (taskData: CreateTaskRequest): Promise<Task> => {
  // Ensure priority is set (default to "Normal")
  const payload: CreateTaskRequest = {
    ...taskData,
    priority: taskData.priority || 'Normal'
  };
  const response = await apiClient.post<Task>('/tasks', payload);
  return response;
};

/**
 * Update an existing task
 * @param taskId - Task ID
 * @param taskData - Updated task data
 * @returns Updated task item
 */
export const updateTask = async (taskId: string, taskData: UpdateTaskRequest): Promise<Task> => {
  const response = await apiClient.put<Task>(`/tasks/${taskId}`, taskData);
  return response;
};

/**
 * Update task status
 * @param taskId - Task ID
 * @param status - New status (TaskStatus enum value or backend string)
 * @param notes - Optional notes
 * @returns Updated task item
 */
export const updateTaskStatus = async (
  taskId: string,
  status: TaskStatus | string,
  notes: string | null = null
): Promise<Task> => {
  // Convert enum to backend string if needed
  let statusString: string;
  if (typeof status === 'number') {
    const statusMap: Record<number, string> = {
      0: 'New',
      1: 'InProgress',
      2: 'OnHold',
      3: 'Completed',
      4: 'Cancelled'
    };
    statusString = statusMap[status] || 'New';
  } else {
    statusString = status;
  }
  
  // Backend expects status in UpdateTaskDto format (PUT /tasks/{id})
  // But also supports PATCH /tasks/{id}/status endpoint
  // Use PUT endpoint with UpdateTaskDto for consistency
  const request: UpdateTaskRequest = { status: statusString };
  const response = await apiClient.put<Task>(`/tasks/${taskId}`, request);
  return response;
};

/**
 * Get task KPI summary
 * @param filters - Optional filters (departmentId, assignedToUserId, periodStart, periodEnd)
 * @returns KPI summary data
 */
export const getTaskKpi = async (filters: TaskFilters = {}): Promise<TaskKpiSummary> => {
  const params: Record<string, any> = {};
  if (filters.departmentId) params.departmentId = filters.departmentId;
  if (filters.assignedToUserId) params.assignedToUserId = filters.assignedToUserId;
  if (filters.periodStart) params.periodStart = filters.periodStart;
  if (filters.periodEnd) params.periodEnd = filters.periodEnd;

  const response = await apiClient.get<TaskKpiSummary>('/tasks/kpi', { params });
  return response;
};

/**
 * Get overdue tasks
 * @param departmentId - Optional department ID filter
 * @param assignedToUserId - Optional user ID filter
 * @returns Array of overdue task items
 */
export const getOverdueTasks = async (
  departmentId: string | null = null,
  assignedToUserId: string | null = null
): Promise<Task[]> => {
  const params: Record<string, any> = {};
  if (departmentId) params.departmentId = departmentId;
  if (assignedToUserId) params.assignedToUserId = assignedToUserId;

  const response = await apiClient.get<Task[]>('/tasks/overdue', { params });
  return response;
};

