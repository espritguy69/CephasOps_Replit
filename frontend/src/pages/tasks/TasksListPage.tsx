import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Plus, RefreshCw, Filter, ArrowUp, ArrowDown, ArrowUpDown, Search, X } from 'lucide-react';
import { useTasks, useUpdateTaskStatus, useCreateTask } from '../../hooks/useTasks';
import { useDepartment } from '../../contexts/DepartmentContext';
import { useToast, Button, Card, LoadingSpinner, EmptyState, Select, Label } from '../../components/ui';
import apiClient from '../../api/client';
import { Input } from '../../components/ui/input';
import { PageShell } from '../../components/layout';
import TaskCreateModal from '../../features/tasks/components/TaskCreateModal';
import TaskStatusBadge from '../../features/tasks/components/TaskStatusBadge';
import TaskPriorityBadge from '../../features/tasks/components/TaskPriorityBadge';
import type { TasksListFilters, Task } from '../../types/tasks';
import { TaskStatus, TaskPriority } from '../../constants/tasks';
import { cn } from '@/lib/utils';

/**
 * Tasks List Page
 * 
 * Comprehensive task management page for operations users to view and manage all team tasks
 */

interface TasksListPageProps {}

const TasksListPage: React.FC<TasksListPageProps> = () => {
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();
  
  // Department context
  const { 
    departmentId, 
    activeDepartment, 
    loading: departmentLoading,
    departments 
  } = useDepartment();
  
  // Filter state
  const [filters, setFilters] = useState<TasksListFilters>({});
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [priorityFilter, setPriorityFilter] = useState<string>('all');
  const [assignedUserFilter, setAssignedUserFilter] = useState<string>('all');
  const [departmentFilter, setDepartmentFilter] = useState<string>('all');

  // Fetch users for filter dropdown
  const { data: users = [] } = useQuery({
    queryKey: ['users', 'active'],
    queryFn: async () => {
      const response = await apiClient.get<{ data?: Array<{ id: string; name: string; email: string }> } | Array<{ id: string; name: string; email: string }>>('/users', { 
        params: { isActive: true } 
      });
      // Handle ApiResponse envelope or direct array
      if (Array.isArray(response)) {
        return response;
      }
      return (response as { data?: Array<{ id: string; name: string; email: string }> })?.data || [];
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
  
  // Modal state
  const [showCreateModal, setShowCreateModal] = useState(false);
  
  // Sorting state
  const [sortColumn, setSortColumn] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  
  // Build filters object
  const queryFilters: TasksListFilters = useMemo(() => {
    const result: TasksListFilters = {};
    
    if (statusFilter !== 'all') {
      result.status = statusFilter;
    }
    
    if (priorityFilter !== 'all') {
      result.priority = priorityFilter;
    }
    
    if (assignedUserFilter !== 'all') {
      if (assignedUserFilter === 'current') {
        result.assignedToUserId = 'current';
      } else {
        result.assignedToUserId = assignedUserFilter;
      }
    }
    
    if (departmentFilter !== 'all') {
      result.departmentId = departmentFilter;
    } else if (departmentId) {
      // Use active department if no filter is set
      result.departmentId = departmentId;
    }
    
    return result;
  }, [statusFilter, priorityFilter, assignedUserFilter, departmentFilter, departmentId]);
  
  // Fetch tasks
  const { 
    data: tasks = [], 
    isLoading, 
    isError, 
    error,
    refetch 
  } = useTasks(queryFilters);
  
  // Mutation hooks
  const updateTaskStatusMutation = useUpdateTaskStatus();
  const createTaskMutation = useCreateTask();
  
  // Handle status change
  const handleStatusChange = async (taskId: string, newStatus: string) => {
    try {
      await updateTaskStatusMutation.mutateAsync({
        id: taskId,
        status: newStatus
      });
    } catch (err) {
      // Error is handled by the mutation hook (toast shown)
      console.error('Error updating task status:', err);
    }
  };
  
  // Handle create task
  const handleCreateTask = async (taskData: any) => {
    try {
      // Convert priority number to string if needed
      const priorityMap: Record<number, string> = {
        1: 'Low',
        2: 'Normal',
        3: 'High',
        4: 'High' // Urgent maps to High for backend
      };
      
      const createPayload = {
        ...taskData,
        priority: typeof taskData.priority === 'number' 
          ? priorityMap[taskData.priority] || 'Normal'
          : taskData.priority || 'Normal',
        dueAt: taskData.dueAt || undefined
      };
      
      await createTaskMutation.mutateAsync(createPayload);
      setShowCreateModal(false);
    } catch (err) {
      // Error is handled by the mutation hook (toast shown)
      console.error('Error creating task:', err);
    }
  };
  
  // Calculate overdue tasks
  const calculateDaysUntilDue = (dueAt?: string): number | null => {
    if (!dueAt) return null;
    const due = new Date(dueAt);
    const now = new Date();
    const diffTime = due.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  };
  
  // Filter and sort tasks
  const filteredAndSortedTasks = useMemo(() => {
    let result = [...tasks];
    
    // Search filter
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      result = result.filter(task => 
        (task.title || '').toLowerCase().includes(query) ||
        (task.description || '').toLowerCase().includes(query) ||
        (task.assignedToUserName || task.assignedToUserId || '').toLowerCase().includes(query) ||
        (task.departmentName || task.departmentId || '').toLowerCase().includes(query)
      );
    }
    
    // Sorting
    if (sortColumn) {
      result.sort((a, b) => {
        let aValue: any;
        let bValue: any;
        
        switch (sortColumn) {
          case 'title':
            aValue = (a.title || '').toLowerCase();
            bValue = (b.title || '').toLowerCase();
            break;
          case 'status':
            aValue = a.status.toLowerCase();
            bValue = b.status.toLowerCase();
            break;
          case 'priority':
            aValue = a.priority.toLowerCase();
            bValue = b.priority.toLowerCase();
            break;
          case 'dueAt':
            aValue = a.dueAt ? new Date(a.dueAt).getTime() : 0;
            bValue = b.dueAt ? new Date(b.dueAt).getTime() : 0;
            break;
          case 'assignedTo':
            aValue = (a.assignedToUserName || a.assignedToUserId || '').toLowerCase();
            bValue = (b.assignedToUserName || b.assignedToUserId || '').toLowerCase();
            break;
          case 'department':
            aValue = (a.departmentName || a.departmentId || '').toLowerCase();
            bValue = (b.departmentName || b.departmentId || '').toLowerCase();
            break;
          case 'requestedAt':
            aValue = a.requestedAt ? new Date(a.requestedAt).getTime() : 0;
            bValue = b.requestedAt ? new Date(b.requestedAt).getTime() : 0;
            break;
          default:
            return 0;
        }
        
        if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
        if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
        return 0;
      });
    }
    
    return result;
  }, [tasks, searchQuery, sortColumn, sortDirection]);
  
  // Handle sort
  const handleSort = (column: string) => {
    if (sortColumn === column) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortColumn(column);
      setSortDirection('asc');
    }
  };
  
  // Sort icon component
  const SortIcon: React.FC<{ column: string }> = ({ column }) => {
    if (sortColumn !== column) {
      return <ArrowUpDown className="h-3 w-3 ml-1" />;
    }
    return sortDirection === 'asc' 
      ? <ArrowUp className="h-3 w-3 ml-1" /> 
      : <ArrowDown className="h-3 w-3 ml-1" />;
  };
  
  
  // Status options for filter
  const statusOptions = [
    { value: 'all', label: 'All Statuses' },
    { value: 'New', label: 'New' },
    { value: 'InProgress', label: 'In Progress' },
    { value: 'OnHold', label: 'On Hold' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];
  
  // Priority options for filter
  const priorityOptions = [
    { value: 'all', label: 'All Priorities' },
    { value: 'Low', label: 'Low' },
    { value: 'Normal', label: 'Normal' },
    { value: 'High', label: 'High' }
  ];
  
  // Show loading while department context is loading
  if (departmentLoading) {
    return (
      <PageShell title="Tasks">
        <LoadingSpinner message="Loading department..." />
      </PageShell>
    );
  }
  
  return (
    <PageShell
      title="Tasks"
      actions={
        <div className="flex flex-col sm:flex-row gap-2">
          <Button variant="outline" onClick={() => refetch()} className="w-full sm:w-auto">
            <RefreshCw className="h-4 w-4 mr-2" />
            <span className="hidden sm:inline">Refresh</span>
          </Button>
          <Button 
            className="flex items-center gap-2 w-full sm:w-auto"
            onClick={() => setShowCreateModal(true)}
          >
            <Plus className="h-4 w-4" />
            <span className="hidden sm:inline">Create Task</span>
            <span className="sm:hidden">Create</span>
          </Button>
        </div>
      }
      compact
    >
      {/* Filters Section */}
      <Card className="p-3 md:p-4 lg:p-6 mb-3 md:mb-4 lg:mb-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3 md:gap-4">
          {/* Search */}
          <div>
            <Label className="text-sm font-medium mb-2 block">Search</Label>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                type="text"
                placeholder="Search by Title, Description, Assignee..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 pr-10"
              />
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  className="absolute right-2 top-1/2 -translate-y-1/2 p-1 hover:bg-gray-100 rounded-full"
                >
                  <X className="h-4 w-4 text-gray-500" />
                </button>
              )}
            </div>
          </div>

          {/* Status Filter */}
          <div>
            <Label className="text-sm font-medium mb-2 block">Status</Label>
            <Select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              options={statusOptions}
            />
          </div>
          
          {/* Priority Filter */}
          <div>
            <Label className="text-sm font-medium mb-2 block">Priority</Label>
            <Select
              value={priorityFilter}
              onChange={(e) => setPriorityFilter(e.target.value)}
              options={priorityOptions}
            />
          </div>
          
          {/* Assigned User Filter */}
          <div>
            <Label className="text-sm font-medium mb-2 block">Assigned To</Label>
            <Select
              value={assignedUserFilter}
              onChange={(e) => setAssignedUserFilter(e.target.value)}
              options={[
                { value: 'all', label: 'All Users' },
                { value: 'current', label: 'Me' },
                ...users.map(user => ({ value: user.id, label: user.name }))
              ]}
            />
          </div>
        </div>
      </Card>
      
      {/* Stats */}
      <div className="mb-3 md:mb-4 flex flex-col sm:flex-row gap-2 sm:gap-4 py-1 text-xs md:text-sm text-muted-foreground">
        <span>
          Showing: <strong className="text-foreground">{filteredAndSortedTasks.length}</strong> of {tasks.length} tasks
        </span>
        {statusFilter !== 'all' && (
          <span>
            Status: <strong className="text-foreground">{statusOptions.find(s => s.value === statusFilter)?.label || statusFilter}</strong>
          </span>
        )}
      </div>

      {/* Error Banner */}
      {isError && !isLoading && (
        <div className="mb-2 rounded border border-red-200 bg-red-50 p-2 text-xs text-red-800">
          {error?.message || 'Failed to load tasks'}
        </div>
      )}
      
      {/* Tasks Table */}
      <Card>
        {isLoading && tasks.length === 0 ? (
          <div className="p-8">
            <LoadingSpinner message="Loading tasks..." />
          </div>
        ) : filteredAndSortedTasks.length === 0 ? (
          <div className="p-8">
            <EmptyState
              title="No tasks found"
              description={searchQuery || statusFilter !== 'all' || priorityFilter !== 'all' ? "Try adjusting your filters" : "Create your first task to get started"}
            />
          </div>
        ) : (
        
          <div className="overflow-x-auto -mx-4 sm:mx-0">
            <div className="inline-block min-w-full align-middle">
              <div className="overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-muted/50">
                    <tr>
                      <th className="px-2 sm:px-3 py-2 text-left whitespace-nowrap">
                        <button onClick={() => handleSort('title')} className="flex items-center hover:text-primary text-xs sm:text-sm">
                          <span className="hidden sm:inline">Title</span>
                          <span className="sm:hidden">Title</span>
                          <SortIcon column="title" />
                        </button>
                      </th>
                      <th className="px-2 sm:px-3 py-2 text-left whitespace-nowrap hidden md:table-cell">
                        <button onClick={() => handleSort('assignedTo')} className="flex items-center hover:text-primary text-xs sm:text-sm">
                          Assigned To <SortIcon column="assignedTo" />
                        </button>
                      </th>
                      <th className="px-2 sm:px-3 py-2 text-left whitespace-nowrap">
                        <button onClick={() => handleSort('status')} className="flex items-center hover:text-primary text-xs sm:text-sm">
                          Status <SortIcon column="status" />
                        </button>
                      </th>
                      <th className="px-2 sm:px-3 py-2 text-left whitespace-nowrap hidden lg:table-cell">
                        <button onClick={() => handleSort('priority')} className="flex items-center hover:text-primary text-xs sm:text-sm">
                          Priority <SortIcon column="priority" />
                        </button>
                      </th>
                      <th className="px-2 sm:px-3 py-2 text-left whitespace-nowrap hidden sm:table-cell">
                        <button onClick={() => handleSort('dueAt')} className="flex items-center hover:text-primary text-xs sm:text-sm">
                          Due Date <SortIcon column="dueAt" />
                        </button>
                      </th>
                      <th className="px-2 sm:px-3 py-2 text-left whitespace-nowrap hidden lg:table-cell">
                        <button onClick={() => handleSort('department')} className="flex items-center hover:text-primary text-xs sm:text-sm">
                          Department <SortIcon column="department" />
                        </button>
                      </th>
                      <th className="px-2 sm:px-3 py-2 text-left whitespace-nowrap hidden xl:table-cell">
                        <button onClick={() => handleSort('requestedAt')} className="flex items-center hover:text-primary text-xs sm:text-sm">
                          Requested <SortIcon column="requestedAt" />
                        </button>
                      </th>
                      <th className="px-2 sm:px-3 py-2 text-center whitespace-nowrap">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y bg-card">
                {filteredAndSortedTasks.map((task) => {
                  const daysUntilDue = calculateDaysUntilDue(task.dueAt);
                  const isOverdue = daysUntilDue !== null && daysUntilDue < 0;
                  
                  // Map backend string status to constants enum
                  const statusMap: Record<string, number> = {
                    'New': TaskStatus.Pending, // Map "New" to Pending for display
                    'InProgress': TaskStatus.InProgress,
                    'OnHold': TaskStatus.OnHold,
                    'Completed': TaskStatus.Completed,
                    'Cancelled': TaskStatus.Cancelled
                  };
                  const statusEnum = statusMap[task.status] ?? TaskStatus.Pending;
                  
                  // Map backend string priority to constants enum
                  const priorityMap: Record<string, number> = {
                    'Low': TaskPriority.Low,
                    'Normal': TaskPriority.Normal,
                    'High': TaskPriority.High
                  };
                  const priorityEnum = priorityMap[task.priority] ?? TaskPriority.Normal;
                  
                  // Get status badge color (matching OrdersListPage pattern)
                  const getStatusBadgeColor = (status: string): string => {
                    const statusLower = status.toLowerCase();
                    if (statusLower === 'new' || statusLower === 'pending') return 'bg-gray-100 text-gray-800 border-gray-300';
                    if (statusLower === 'inprogress' || statusLower === 'in progress') return 'bg-blue-100 text-blue-800 border-blue-300';
                    if (statusLower === 'onhold' || statusLower === 'on hold') return 'bg-yellow-100 text-yellow-800 border-yellow-300';
                    if (statusLower === 'completed') return 'bg-green-100 text-green-800 border-green-300';
                    if (statusLower === 'cancelled') return 'bg-red-100 text-red-800 border-red-300';
                    return 'bg-gray-100 text-gray-800 border-gray-300';
                  };
                  
                  return (
                    <>
                      {/* Mobile Card View */}
                      <tr 
                        key={`mobile-${task.id}`}
                        className="md:hidden hover:bg-muted/30"
                      >
                        <td colSpan={8} className="px-3 py-3">
                          <div className="space-y-2">
                            <div className="flex items-start justify-between">
                              <div className="flex-1 min-w-0">
                                <div className="font-medium text-sm truncate">{task.title || '-'}</div>
                                {task.description && (
                                  <div className="text-xs text-muted-foreground mt-1 line-clamp-2">{task.description}</div>
                                )}
                              </div>
                              <div className="ml-2 flex-shrink-0">
                                <TaskStatusBadge status={statusEnum} />
                              </div>
                            </div>
                            <div className="grid grid-cols-2 gap-2 text-xs">
                              <div>
                                <span className="text-muted-foreground">Assigned:</span>{' '}
                                <span className="font-medium">{task.assignedToUserName || task.assignedToUserId || 'Unassigned'}</span>
                              </div>
                              <div>
                                <span className="text-muted-foreground">Priority:</span>{' '}
                                <TaskPriorityBadge priority={priorityEnum} />
                              </div>
                              {task.dueAt && (
                                <div className={isOverdue ? 'text-red-600 font-semibold' : ''}>
                                  <span className="text-muted-foreground">Due:</span>{' '}
                                  <span className="font-medium">{new Date(task.dueAt).toLocaleDateString()}</span>
                                  {daysUntilDue !== null && (
                                    <span className="ml-1 text-xs">
                                      {isOverdue 
                                        ? `(${Math.abs(daysUntilDue)} days overdue)`
                                        : daysUntilDue === 0 
                                          ? '(Due today)'
                                          : `(${daysUntilDue} days)`
                                      }
                                    </span>
                                  )}
                                </div>
                              )}
                              {task.departmentName && (
                                <div>
                                  <span className="text-muted-foreground">Dept:</span>{' '}
                                  <span className="font-medium">{task.departmentName}</span>
                                </div>
                              )}
                            </div>
                            <div className="flex items-center justify-between pt-2 border-t">
                              {task.status !== 'Completed' && task.status !== 'Cancelled' ? (
                                <select
                                  value={task.status}
                                  onChange={(e) => handleStatusChange(task.id, e.target.value)}
                                  className={cn(
                                    'px-2 py-1 rounded text-xs font-medium border cursor-pointer flex-1',
                                    getStatusBadgeColor(task.status)
                                  )}
                                  onClick={(e) => e.stopPropagation()}
                                >
                                  {statusOptions.filter(opt => opt.value !== 'all').map(opt => (
                                    <option key={opt.value} value={opt.value}>
                                      {opt.label}
                                    </option>
                                  ))}
                                </select>
                              ) : (
                                <div className="flex-1" />
                              )}
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  navigate(`/tasks/${task.id}`);
                                }}
                                title="View Details"
                                className="h-7 px-2"
                              >
                                <span className="text-xs">View</span>
                              </Button>
                            </div>
                          </div>
                        </td>
                      </tr>
                      
                      {/* Desktop Table View */}
                      <tr 
                        key={`desktop-${task.id}`}
                        className="hidden md:table-row hover:bg-muted/30 cursor-pointer"
                        onClick={() => navigate(`/tasks/${task.id}`)}
                      >
                        <td className="px-2 sm:px-3 py-2 font-medium text-xs sm:text-sm">
                          <div className="truncate max-w-[200px]">{task.title || '-'}</div>
                        </td>
                        <td className="px-2 sm:px-3 py-2 hidden md:table-cell">
                          <div className="font-medium text-xs sm:text-sm">{task.assignedToUserName || task.assignedToUserId || 'Unassigned'}</div>
                          {task.description && (
                            <div className="text-xs text-muted-foreground truncate max-w-[150px]">{task.description}</div>
                          )}
                        </td>
                        <td className="px-2 sm:px-3 py-2" onClick={(e) => e.stopPropagation()}>
                          {task.status !== 'Completed' && task.status !== 'Cancelled' ? (
                            <select
                              value={task.status}
                              onChange={(e) => handleStatusChange(task.id, e.target.value)}
                              className={cn(
                                'px-2 py-1 rounded text-xs font-medium border cursor-pointer w-full',
                                getStatusBadgeColor(task.status)
                              )}
                            >
                              {statusOptions.filter(opt => opt.value !== 'all').map(opt => (
                                <option key={opt.value} value={opt.value}>
                                  {opt.label}
                                </option>
                              ))}
                            </select>
                          ) : (
                            <TaskStatusBadge status={statusEnum} />
                          )}
                        </td>
                        <td className="px-2 sm:px-3 py-2 text-center hidden lg:table-cell">
                          <TaskPriorityBadge priority={priorityEnum} />
                        </td>
                        <td className="px-2 sm:px-3 py-2 hidden sm:table-cell">
                          {task.dueAt ? (
                            <div>
                              <div className={`text-xs font-medium ${isOverdue ? 'text-red-600' : ''}`}>
                                {new Date(task.dueAt).toLocaleDateString()}
                              </div>
                              {daysUntilDue !== null && (
                                <div className={`text-xs ${isOverdue ? 'text-red-600' : 'text-muted-foreground'}`}>
                                  {isOverdue 
                                    ? `${Math.abs(daysUntilDue)} days overdue`
                                    : daysUntilDue === 0 
                                      ? 'Due today'
                                      : `${daysUntilDue} days remaining`
                                  }
                                </div>
                              )}
                            </div>
                          ) : (
                            <span className="text-muted-foreground text-xs">-</span>
                          )}
                        </td>
                        <td className="px-2 sm:px-3 py-2 text-muted-foreground text-xs sm:text-sm hidden lg:table-cell">
                          {task.departmentName || task.departmentId || '-'}
                        </td>
                        <td className="px-2 sm:px-3 py-2 hidden xl:table-cell">
                          <div className="text-xs font-medium">{task.requestedAt ? new Date(task.requestedAt).toLocaleDateString() : '-'}</div>
                        </td>
                        <td className="px-2 sm:px-3 py-2 text-center" onClick={(e) => e.stopPropagation()}>
                          <div className="flex items-center justify-center gap-1">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => navigate(`/tasks/${task.id}`)}
                              title="View Details"
                              className="h-7 w-7 p-0"
                            >
                              <Plus className="h-3.5 w-3.5" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    </>
                  );
                })}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}
      </Card>
      
      {/* Create Task Modal */}
      {showCreateModal && (
        <TaskCreateModal
          departmentId={departmentId || undefined}
          onClose={() => setShowCreateModal(false)}
          onSubmit={handleCreateTask}
        />
      )}
    </PageShell>
  );
};

export default TasksListPage;

