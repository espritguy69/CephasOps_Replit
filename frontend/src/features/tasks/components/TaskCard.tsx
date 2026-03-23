import React from 'react';
import TaskStatusBadge from './TaskStatusBadge';
import TaskPriorityBadge from './TaskPriorityBadge';
import { TaskStatus } from '../../../constants/tasks';

interface Task {
  id: string;
  title: string;
  description?: string;
  status: number;
  priority?: number;
  dueAt?: string;
  completedAt?: string;
  requestedByUserName?: string;
  departmentName?: string;
  daysUntilDue?: number | null;
  isOverdue?: boolean;
}

interface TaskCardProps {
  task: Task;
  onStatusChange?: (taskId: string, newStatus: number) => void;
}

const TaskCard: React.FC<TaskCardProps> = ({ task, onStatusChange }) => {
  const formatDate = (date?: string): string | null => {
    if (!date) return null;
    return new Date(date).toLocaleDateString();
  };

  const getDaysUntilDue = (): string | null => {
    if (!task.dueAt) return null;
    const days = task.daysUntilDue;
    if (days === null) return null;
    if (days < 0) return `${Math.abs(days)} days overdue`;
    if (days === 0) return 'Due today';
    return `${days} days remaining`;
  };

  const canChangeStatus = onStatusChange && task.status !== TaskStatus.Completed && task.status !== TaskStatus.Cancelled;

  return (
    <div className={`border rounded-lg p-4 ${task.isOverdue ? 'border-red-500 bg-red-50' : 'border-gray-200'}`}>
      <div className="flex justify-between items-start mb-2">
        <div className="flex-1">
          <h3 className="text-lg font-semibold">{task.title}</h3>
          {task.description && (
            <p className="text-gray-600 text-sm mt-1">{task.description}</p>
          )}
        </div>
        <div className="flex gap-2">
          <TaskPriorityBadge priority={task.priority || 2} />
          <TaskStatusBadge status={task.status} />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-2 text-sm text-gray-600 mb-3">
        <div>
          <span className="font-medium">Requested by:</span> {task.requestedByUserName || 'N/A'}
        </div>
        <div>
          <span className="font-medium">Department:</span> {task.departmentName || 'N/A'}
        </div>
        {task.dueAt && (
          <div>
            <span className="font-medium">Due:</span> {formatDate(task.dueAt)}
            {task.daysUntilDue !== null && (
              <span className={`ml-2 ${task.isOverdue ? 'text-red-600 font-semibold' : ''}`}>
                ({getDaysUntilDue()})
              </span>
            )}
          </div>
        )}
        {task.completedAt && (
          <div>
            <span className="font-medium">Completed:</span> {formatDate(task.completedAt)}
          </div>
        )}
      </div>

      {canChangeStatus && (
        <div className="flex gap-2 mt-3">
          {task.status === TaskStatus.Pending && (
            <button
              onClick={() => onStatusChange?.(task.id, TaskStatus.InProgress)}
              className="px-3 py-1 bg-blue-600 text-white text-sm rounded hover:bg-blue-700"
            >
              Start
            </button>
          )}
          {task.status === TaskStatus.InProgress && (
            <>
              <button
                onClick={() => onStatusChange?.(task.id, TaskStatus.OnHold)}
                className="px-3 py-1 bg-yellow-600 text-white text-sm rounded hover:bg-yellow-700"
              >
                Hold
              </button>
              <button
                onClick={() => onStatusChange?.(task.id, TaskStatus.Completed)}
                className="px-3 py-1 bg-green-600 text-white text-sm rounded hover:bg-green-700"
              >
                Complete
              </button>
            </>
          )}
          {task.status === TaskStatus.OnHold && (
            <button
              onClick={() => onStatusChange?.(task.id, TaskStatus.InProgress)}
              className="px-3 py-1 bg-blue-600 text-white text-sm rounded hover:bg-blue-700"
            >
              Resume
            </button>
          )}
        </div>
      )}
    </div>
  );
};

export default TaskCard;

