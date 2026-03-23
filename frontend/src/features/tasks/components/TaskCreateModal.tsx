import React, { useState, FormEvent, ChangeEvent } from 'react';
import { X } from 'lucide-react';
import { TaskPriority } from '../../../constants/tasks';

interface TaskFormData {
  assignedToUserId: string;
  title: string;
  description: string;
  dueAt: string;
  priority: number;
}

interface TaskCreateModalProps {
  departmentId?: string;
  onClose: () => void;
  onSubmit: (taskData: any) => void | Promise<void>;
}

const TaskCreateModal: React.FC<TaskCreateModalProps> = ({ departmentId, onClose, onSubmit }) => {
  const [formData, setFormData] = useState<TaskFormData>({
    assignedToUserId: '',
    title: '',
    description: '',
    dueAt: '',
    priority: TaskPriority.Normal
  });
  const [errors, setErrors] = useState<Record<string, string | null>>({});

  const handleSubmit = (e: FormEvent<HTMLFormElement>): void => {
    e.preventDefault();
    
    const newErrors: Record<string, string> = {};
    if (!formData.title.trim()) {
      newErrors.title = 'Title is required';
    }
    if (!formData.assignedToUserId) {
      newErrors.assignedToUserId = 'Assigned user is required';
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    const taskData = {
      ...formData,
      departmentId,
      dueAt: formData.dueAt ? new Date(formData.dueAt).toISOString() : null,
      priority: parseInt(String(formData.priority))
    };

    onSubmit(taskData);
  };

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>): void => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: null }));
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-xl font-bold">Create New Task</h2>
          <button
            onClick={onClose}
            className="text-gray-500 hover:text-gray-700"
            aria-label="Close"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="mb-4">
            <label className="block text-sm font-medium mb-1">
              Assigned To User ID <span className="text-red-600">*</span>
            </label>
            <input
              type="text"
              name="assignedToUserId"
              value={formData.assignedToUserId}
              onChange={handleChange}
              className={`w-full px-3 py-2 border rounded ${errors.assignedToUserId ? 'border-red-500' : 'border-gray-300'}`}
              placeholder="Enter user ID"
            />
            {errors.assignedToUserId && (
              <p className="text-red-600 text-sm mt-1">{errors.assignedToUserId}</p>
            )}
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium mb-1">
              Title <span className="text-red-600">*</span>
            </label>
            <input
              type="text"
              name="title"
              value={formData.title}
              onChange={handleChange}
              className={`w-full px-3 py-2 border rounded ${errors.title ? 'border-red-500' : 'border-gray-300'}`}
              placeholder="Enter task title"
              maxLength={256}
            />
            {errors.title && (
              <p className="text-red-600 text-sm mt-1">{errors.title}</p>
            )}
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium mb-1">Description</label>
            <textarea
              name="description"
              value={formData.description}
              onChange={handleChange}
              className="w-full px-3 py-2 border border-gray-300 rounded"
              rows={4}
              placeholder="Enter task description"
              maxLength={4000}
            />
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium mb-1">Due Date</label>
            <input
              type="datetime-local"
              name="dueAt"
              value={formData.dueAt}
              onChange={handleChange}
              className="w-full px-3 py-2 border border-gray-300 rounded"
            />
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium mb-1">Priority</label>
            <select
              name="priority"
              value={formData.priority}
              onChange={handleChange}
              className="w-full px-3 py-2 border border-gray-300 rounded"
            >
              <option value={TaskPriority.Low}>Low</option>
              <option value={TaskPriority.Normal}>Normal</option>
              <option value={TaskPriority.High}>High</option>
              <option value={TaskPriority.Urgent}>Urgent</option>
            </select>
          </div>

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 border border-gray-300 rounded hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
            >
              Create Task
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default TaskCreateModal;

