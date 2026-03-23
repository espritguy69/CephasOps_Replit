import React from 'react';
import TaskCard from '../../features/tasks/components/TaskCard';
import { TaskStatus, TaskPriority } from '../../constants/tasks';

export default {
  title: 'Tasks/TaskCard',
  component: TaskCard
};

const mockTask = {
  id: '1',
  title: 'Review Q4 financial reports',
  description: 'Complete review of Q4 financial reports and prepare summary for board meeting',
  status: TaskStatus.Pending,
  priority: TaskPriority.High,
  dueAt: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString(),
  requestedByUserName: 'John Manager',
  departmentName: 'Finance',
  isOverdue: false,
  daysUntilDue: 3
};

export const Pending = () => (
  <div className="p-4">
    <TaskCard task={{ ...mockTask, status: TaskStatus.Pending }} />
  </div>
);

export const InProgress = () => (
  <div className="p-4">
    <TaskCard task={{ ...mockTask, status: TaskStatus.InProgress }} />
  </div>
);

export const Overdue = () => (
  <div className="p-4">
    <TaskCard task={{ 
      ...mockTask, 
      status: TaskStatus.Pending,
      isOverdue: true,
      daysUntilDue: -5,
      dueAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString()
    }} />
  </div>
);

export const Completed = () => (
  <div className="p-4">
    <TaskCard task={{ 
      ...mockTask, 
      status: TaskStatus.Completed,
      completedAt: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString()
    }} />
  </div>
);

export const WithStatusChange = () => {
  const [task, setTask] = React.useState(mockTask);

  const handleStatusChange = (taskId: string, newStatus: number): void => {
    setTask({ ...task, status: newStatus });
  };

  return (
    <div className="p-4">
      <TaskCard task={task} onStatusChange={handleStatusChange} />
    </div>
  );
};

