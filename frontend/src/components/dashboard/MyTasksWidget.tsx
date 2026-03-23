import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertTriangle } from 'lucide-react';
import { getMyTasks, getTaskKpi } from '../../api/tasks';
import TaskCard from '../../features/tasks/components/TaskCard';
import type { Task, TaskKpiSummary, TaskStatus } from '../../types/tasks';

interface MyTasksWidgetProps {
  userId?: string;
}

interface ExtendedTask extends Task {
  isOverdue?: boolean;
  dueAt?: string;
}

const MyTasksWidget: React.FC<MyTasksWidgetProps> = ({ userId }) => {
  const navigate = useNavigate();
  const [tasks, setTasks] = useState<ExtendedTask[]>([]);
  const [kpi, setKpi] = useState<TaskKpiSummary | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    if (userId) {
      loadData();
    }
  }, [userId]);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const [tasksData, kpiData] = await Promise.all([
        getMyTasks(null), // Get all statuses
        getTaskKpi(null, userId)
      ]);
      
      // Show only pending and in-progress tasks
      const activeTasks = tasksData.filter(
        (t) => t.status === TaskStatus.Pending || t.status === TaskStatus.InProgress
      ).slice(0, 5) as ExtendedTask[];
      
      setTasks(activeTasks);
      setKpi(kpiData);
    } catch (err) {
      console.error('Failed to load tasks widget data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = async (taskId: string, newStatus: TaskStatus): Promise<void> => {
    try {
      const { updateTaskStatus } = await import('../../api/tasks');
      await updateTaskStatus(taskId, newStatus);
      await loadData();
    } catch (err) {
      console.error('Failed to update task status:', err);
    }
  };

  if (loading) {
    return (
      <div className="bg-white rounded-lg shadow p-4">
        <div className="animate-pulse">Loading tasks...</div>
      </div>
    );
  }

  const overdueCount = tasks.filter((t) => t.isOverdue).length;
  const pendingCount = tasks.filter((t) => t.status === TaskStatus.Pending).length;
  const inProgressCount = tasks.filter((t) => t.status === TaskStatus.InProgress).length;

  return (
    <div className="bg-white rounded-lg shadow p-4">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-lg font-semibold">My Tasks</h2>
        <button
          onClick={() => navigate(`/tasks/my`)}
          className="text-blue-600 hover:text-blue-800 text-sm"
        >
          View All
        </button>
      </div>

      {kpi && (
        <div className="grid grid-cols-3 gap-2 mb-4 text-sm">
          <div className="text-center">
            <div className="font-semibold text-gray-600">Total</div>
            <div className="text-lg">{kpi.total}</div>
          </div>
          <div className="text-center">
            <div className="font-semibold text-gray-600">Completed</div>
            <div className="text-lg text-green-600">{kpi.completed}</div>
          </div>
          <div className="text-center">
            <div className="font-semibold text-gray-600">Pending</div>
            <div className="text-lg">{kpi.pending}</div>
          </div>
        </div>
      )}

      {overdueCount > 0 && (
        <div className="mb-2 p-2 bg-red-50 border border-red-200 rounded text-sm text-red-800 flex items-center gap-2">
          <AlertTriangle className="h-4 w-4 flex-shrink-0" />
          <span>{overdueCount} overdue task{overdueCount > 1 ? 's' : ''}</span>
        </div>
      )}

      <div className="space-y-2">
        {tasks.length > 0 ? (
          tasks.map((task) => (
            <div key={task.id} className="border-b pb-2 last:border-b-0">
              <div className="flex justify-between items-start">
                <div className="flex-1">
                  <h3 className="font-medium text-sm">{task.title}</h3>
                  {task.dueDate && (
                    <p className={`text-xs ${task.isOverdue ? 'text-red-600' : 'text-gray-500'}`}>
                      Due: {new Date(task.dueDate).toLocaleDateString()}
                    </p>
                  )}
                </div>
                <span className={`text-xs px-2 py-1 rounded ${
                  task.status === TaskStatus.Pending ? 'bg-gray-100 text-gray-800' :
                  task.status === TaskStatus.InProgress ? 'bg-blue-100 text-blue-800' :
                  'bg-green-100 text-green-800'
                }`}>
                  {TaskStatus[task.status]}
                </span>
              </div>
            </div>
          ))
        ) : (
          <div className="text-center text-gray-500 text-sm py-4">
            No active tasks
          </div>
        )}
      </div>

      {(pendingCount > 0 || inProgressCount > 0) && (
        <div className="mt-4 pt-4 border-t">
          <div className="flex justify-between text-xs text-gray-600">
            <span>Pending: {pendingCount}</span>
            <span>In Progress: {inProgressCount}</span>
          </div>
        </div>
      )}
    </div>
  );
};

export default MyTasksWidget;

