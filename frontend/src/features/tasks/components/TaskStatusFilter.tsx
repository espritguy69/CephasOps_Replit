import React from 'react';
import { TaskStatus } from '../../../constants/tasks';

interface TaskStatusFilterProps {
  value: number | null;
  onChange: (value: number | null) => void;
}

const TaskStatusFilter: React.FC<TaskStatusFilterProps> = ({ value, onChange }) => {
  const statusOptions = [
    { value: null, label: 'All' },
    { value: TaskStatus.Pending, label: 'Pending' },
    { value: TaskStatus.InProgress, label: 'In Progress' },
    { value: TaskStatus.OnHold, label: 'On Hold' },
    { value: TaskStatus.Completed, label: 'Completed' },
    { value: TaskStatus.Cancelled, label: 'Cancelled' }
  ];

  return (
    <div className="flex gap-2">
      {statusOptions.map(option => (
        <button
          key={option.value ?? 'all'}
          onClick={() => onChange(option.value)}
          className={`px-3 py-1 rounded text-sm ${
            value === option.value
              ? 'bg-blue-600 text-white'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
};

export default TaskStatusFilter;

