import React from 'react';
import { X } from 'lucide-react';
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
  assignedToUserName?: string;
  departmentName?: string;
  daysUntilDue?: number | null;
  isOverdue?: boolean;
  requestedAt?: string;
  createdAt?: string;
  startedAt?: string;
}

interface TaskDetailsModalProps {
  task: Task;
  onClose: () => void;
  onStatusChange?: (taskId: string, newStatus: number) => void;
}

const TaskDetailsModal: React.FC<TaskDetailsModalProps> = ({ task, onClose, onStatusChange }) => {
  const formatDate = (date?: string): string => {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString();
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 w-full max-w-3xl max-h-[90vh] overflow-y-auto">
        <div className="flex justify-between items-start mb-4">
          <h2 className="text-2xl font-bold">{task.title}</h2>
          <button
            onClick={onClose}
            className="text-gray-500 hover:text-gray-700"
            aria-label="Close"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="flex gap-2 mb-4">
          <TaskPriorityBadge priority={task.priority || 2} />
          <TaskStatusBadge status={task.status} />
          {task.isOverdue && (
            <span className="px-2 py-1 bg-red-100 text-red-800 rounded text-sm">
              Overdue
            </span>
          )}
        </div>

        {task.description && (
          <div className="mb-4">
            <h3 className="font-semibold mb-2">Description</h3>
            <p className="text-gray-700 whitespace-pre-wrap">{task.description}</p>
          </div>
        )}

        <div className="grid grid-cols-2 gap-4 mb-4">
          <div>
            <h3 className="font-semibold mb-1">Requested By</h3>
            <p className="text-gray-600">{task.requestedByUserName || 'N/A'}</p>
          </div>
          <div>
            <h3 className="font-semibold mb-1">Assigned To</h3>
            <p className="text-gray-600">{task.assignedToUserName || 'N/A'}</p>
          </div>
          <div>
            <h3 className="font-semibold mb-1">Department</h3>
            <p className="text-gray-600">{task.departmentName || 'N/A'}</p>
          </div>
          <div>
            <h3 className="font-semibold mb-1">Due Date</h3>
            <p className="text-gray-600">{formatDate(task.dueAt)}</p>
            {task.daysUntilDue !== null && task.daysUntilDue !== undefined && (
              <p className={`text-sm ${task.isOverdue ? 'text-red-600' : 'text-gray-500'}`}>
                {task.isOverdue 
                  ? `${Math.abs(task.daysUntilDue)} days overdue`
                  : `${task.daysUntilDue} days remaining`}
              </p>
            )}
          </div>
          <div>
            <h3 className="font-semibold mb-1">Requested At</h3>
            <p className="text-gray-600">{formatDate(task.requestedAt)}</p>
          </div>
          <div>
            <h3 className="font-semibold mb-1">Created At</h3>
            <p className="text-gray-600">{formatDate(task.createdAt)}</p>
          </div>
          {task.startedAt && (
            <div>
              <h3 className="font-semibold mb-1">Started At</h3>
              <p className="text-gray-600">{formatDate(task.startedAt)}</p>
            </div>
          )}
          {task.completedAt && (
            <div>
              <h3 className="font-semibold mb-1">Completed At</h3>
              <p className="text-gray-600">{formatDate(task.completedAt)}</p>
            </div>
          )}
        </div>

        {onStatusChange && task.status !== TaskStatus.Completed && task.status !== TaskStatus.Cancelled && (
          <div className="border-t pt-4 mt-4">
            <h3 className="font-semibold mb-2">Change Status</h3>
            <div className="flex gap-2">
              {task.status === TaskStatus.Pending && (
                <button
                  onClick={() => {
                    onStatusChange(task.id, TaskStatus.InProgress);
                    onClose();
                  }}
                  className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
                >
                  Start Task
                </button>
              )}
              {task.status === TaskStatus.InProgress && (
                <>
                  <button
                    onClick={() => {
                      onStatusChange(task.id, TaskStatus.OnHold);
                      onClose();
                    }}
                    className="px-4 py-2 bg-yellow-600 text-white rounded hover:bg-yellow-700"
                  >
                    Put On Hold
                  </button>
                  <button
                    onClick={() => {
                      onStatusChange(task.id, TaskStatus.Completed);
                      onClose();
                    }}
                    className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700"
                  >
                    Mark Complete
                  </button>
                </>
              )}
              {task.status === TaskStatus.OnHold && (
                <button
                  onClick={() => {
                    onStatusChange(task.id, TaskStatus.InProgress);
                    onClose();
                  }}
                  className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
                >
                  Resume Task
                </button>
              )}
            </div>
          </div>
        )}

        <div className="flex justify-end mt-6">
          <button
            onClick={onClose}
            className="px-4 py-2 border border-gray-300 rounded hover:bg-gray-50"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default TaskDetailsModal;

