import React from 'react';
import { TaskPriority } from '../../../constants/tasks';

interface TaskPriorityBadgeProps {
  priority: number;
}

const TaskPriorityBadge: React.FC<TaskPriorityBadgeProps> = ({ priority }) => {
  const priorityConfig: Record<number, { bg: string; text: string; label: string }> = {
    [TaskPriority.Low]: { bg: 'bg-gray-100', text: 'text-gray-800', label: 'Low' },
    [TaskPriority.Normal]: { bg: 'bg-blue-100', text: 'text-blue-800', label: 'Normal' },
    [TaskPriority.High]: { bg: 'bg-orange-100', text: 'text-orange-800', label: 'High' },
    [TaskPriority.Urgent]: { bg: 'bg-red-100', text: 'text-red-800', label: 'Urgent' }
  };

  const config = priorityConfig[priority] || priorityConfig[TaskPriority.Normal];

  return (
    <span className={`px-2 py-1 rounded text-xs font-medium ${config.bg} ${config.text}`}>
      {config.label}
    </span>
  );
};

export default TaskPriorityBadge;

