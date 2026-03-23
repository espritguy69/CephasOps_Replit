import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getDepartmentTasks, createTask } from '../../api/tasks';
import TaskCard from './components/TaskCard';
import TaskCreateModal from './components/TaskCreateModal';
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

const DepartmentTasksPage: React.FC = () => {
  const { departmentId } = useParams<{ departmentId: string }>();
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [statusFilter, setStatusFilter] = useState<number | null>(null);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (departmentId) {
      loadTasks();
    }
  }, [departmentId, statusFilter]);

  const loadTasks = async (): Promise<void> => {
    if (!departmentId) return;
    try {
      setLoading(true);
      const data = await getDepartmentTasks(departmentId, statusFilter);
      setTasks(data);
      setError(null);
    } catch (err) {
      setError('Failed to load department tasks');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTask = async (taskData: any): Promise<void> => {
    try {
      await createTask({ ...taskData, departmentId });
      setShowCreateModal(false);
      await loadTasks();
    } catch (err) {
      setError('Failed to create task');
      console.error(err);
    }
  };

  if (loading) {
    return <div className="flex justify-center p-8">Loading tasks...</div>;
  }

  return (
    <div className="p-6">
      <div className="mb-6 flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold mb-4">Department Tasks</h1>
          <TaskStatusFilter value={statusFilter} onChange={setStatusFilter} />
        </div>
        <button
          onClick={() => setShowCreateModal(true)}
          className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
        >
          Create Task
        </button>
      </div>

      {error && <div className="text-red-600 p-4 mb-4">{error}</div>}

      <div className="space-y-2">
        {tasks.map(task => (
          <TaskCard key={task.id} task={task} />
        ))}
      </div>

      {tasks.length === 0 && (
        <div className="text-center py-12 text-gray-500">
          No tasks found for this department
        </div>
      )}

      {showCreateModal && (
        <TaskCreateModal
          departmentId={departmentId}
          onClose={() => setShowCreateModal(false)}
          onSubmit={handleCreateTask}
        />
      )}
    </div>
  );
};

export default DepartmentTasksPage;

