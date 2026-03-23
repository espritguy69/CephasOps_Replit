import React from 'react';
import { TaskStatus } from '../../../constants/tasks';

interface TaskStatusBadgeProps {
  status: number;
}

const TaskStatusBadge: React.FC<TaskStatusBadgeProps> = ({ status }) => {
  const statusConfig: Record<number, { bg: string; text: string; label: string }> = {
    [TaskStatus.Pending]: { bg: 'bg-gray-100', text: 'text-gray-800', label: 'Pending' },
    [TaskStatus.InProgress]: { bg: 'bg-blue-100', text: 'text-blue-800', label: 'In Progress' },
    [TaskStatus.OnHold]: { bg: 'bg-yellow-100', text: 'text-yellow-800', label: 'On Hold' },
    [TaskStatus.Completed]: { bg: 'bg-green-100', text: 'text-green-800', label: 'Completed' },
    [TaskStatus.Cancelled]: { bg: 'bg-red-100', text: 'text-red-800', label: 'Cancelled' }
  };

  const config = statusConfig[status] || statusConfig[TaskStatus.Pending];

  return (
    <span className={`px-2 py-1 rounded text-xs font-medium ${config.bg} ${config.text}`}>
      {config.label}
    </span>
  );
};

export default TaskStatusBadge;

