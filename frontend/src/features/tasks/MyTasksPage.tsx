import React, { useState, useEffect } from 'react';
import { getMyTasks, updateTaskStatus } from '../../api/tasks';
import TaskCard from './components/TaskCard';
import TaskStatusFilter from './components/TaskStatusFilter';
import { TaskStatus } from '../../constants/tasks';

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

const MyTasksPage: React.FC = () => {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [statusFilter, setStatusFilter] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadTasks();
  }, [statusFilter]);

  const loadTasks = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getMyTasks(statusFilter);
      setTasks(data);
      setError(null);
    } catch (err) {
      setError('Failed to load tasks');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = async (taskId: string, newStatus: number): Promise<void> => {
    try {
      await updateTaskStatus(taskId, newStatus);
      await loadTasks(); // Reload tasks
    } catch (err) {
      setError('Failed to update task status');
      console.error(err);
    }
  };

  if (loading) {
    return <div className="flex justify-center p-8">Loading tasks...</div>;
  }

  if (error) {
    return <div className="text-red-600 p-4">{error}</div>;
  }

  const pendingTasks = tasks.filter(t => t.status === TaskStatus.Pending);
  const inProgressTasks = tasks.filter(t => t.status === TaskStatus.InProgress);
  const completedTasks = tasks.filter(t => t.status === TaskStatus.Completed);
  const overdueTasks = tasks.filter(t => t.isOverdue);

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold mb-4">My Tasks</h1>
        <TaskStatusFilter value={statusFilter} onChange={setStatusFilter} />
      </div>

      {overdueTasks.length > 0 && (
        <div className="mb-6">
          <h2 className="text-lg font-semibold text-red-600 mb-2">
            Overdue Tasks ({overdueTasks.length})
          </h2>
          <div className="space-y-2">
            {overdueTasks.map(task => (
              <TaskCard
                key={task.id}
                task={task}
                onStatusChange={handleStatusChange}
              />
            ))}
          </div>
        </div>
      )}

      {pendingTasks.length > 0 && (
        <div className="mb-6">
          <h2 className="text-lg font-semibold mb-2">
            Pending ({pendingTasks.length})
          </h2>
          <div className="space-y-2">
            {pendingTasks.map(task => (
              <TaskCard
                key={task.id}
                task={task}
                onStatusChange={handleStatusChange}
              />
            ))}
          </div>
        </div>
      )}

      {inProgressTasks.length > 0 && (
        <div className="mb-6">
          <h2 className="text-lg font-semibold mb-2">
            In Progress ({inProgressTasks.length})
          </h2>
          <div className="space-y-2">
            {inProgressTasks.map(task => (
              <TaskCard
                key={task.id}
                task={task}
                onStatusChange={handleStatusChange}
              />
            ))}
          </div>
        </div>
      )}

      {completedTasks.length > 0 && (
        <div className="mb-6">
          <h2 className="text-lg font-semibold mb-2">
            Completed ({completedTasks.length})
          </h2>
          <div className="space-y-2">
            {completedTasks.map(task => (
              <TaskCard
                key={task.id}
                task={task}
                onStatusChange={handleStatusChange}
              />
            ))}
          </div>
        </div>
      )}

      {tasks.length === 0 && (
        <div className="text-center py-12 text-gray-500">
          No tasks found
        </div>
      )}
    </div>
  );
};

export default MyTasksPage;

