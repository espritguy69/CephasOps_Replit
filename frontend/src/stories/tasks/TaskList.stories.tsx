import React from 'react';
import TaskCard from '../../features/tasks/components/TaskCard';
import { TaskStatus, TaskPriority } from '../../constants/tasks';

export default {
  title: 'Tasks/TaskList',
  component: TaskCard
};

const mockTasks = [
  {
    id: '1',
    title: 'Review Q4 financial reports',
    description: 'Complete review of Q4 financial reports and prepare summary',
    status: TaskStatus.Pending,
    priority: TaskPriority.High,
    dueAt: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString(),
    requestedByUserName: 'John Manager',
    departmentName: 'Finance',
    isOverdue: false,
    daysUntilDue: 3
  },
  {
    id: '2',
    title: 'Update customer database',
    description: 'Sync customer data from external system',
    status: TaskStatus.InProgress,
    priority: TaskPriority.Normal,
    dueAt: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString(),
    requestedByUserName: 'John Manager',
    departmentName: 'Operations',
    isOverdue: true,
    daysUntilDue: -1
  },
  {
    id: '3',
    title: 'Prepare presentation slides',
    description: 'Create slides for upcoming board meeting',
    status: TaskStatus.Completed,
    priority: TaskPriority.Urgent,
    dueAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
    completedAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
    requestedByUserName: 'John Manager',
    departmentName: 'Executive',
    isOverdue: false,
    daysUntilDue: null
  }
];

export const Default = () => (
  <div className="space-y-4 p-4">
    {mockTasks.map((task) => (
      <TaskCard key={task.id} task={task} />
    ))}
  </div>
);

export const WithStatusChange = () => {
  const [tasks, setTasks] = React.useState(mockTasks);

  const handleStatusChange = (taskId: string, newStatus: number): void => {
    setTasks(tasks.map((t) => 
      t.id === taskId ? { ...t, status: newStatus } : t
    ));
  };

  return (
    <div className="space-y-4 p-4">
      {tasks.map((task) => (
        <TaskCard 
          key={task.id} 
          task={task} 
          onStatusChange={handleStatusChange}
        />
      ))}
    </div>
  );
};

export const EmptyState = () => (
  <div className="text-center py-12 text-gray-500">
    No tasks found
  </div>
);

