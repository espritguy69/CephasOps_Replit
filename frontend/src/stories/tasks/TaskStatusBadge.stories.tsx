import React from 'react';
import TaskStatusBadge from '../../features/tasks/components/TaskStatusBadge';
import { TaskStatus } from '../../constants/tasks';

export default {
  title: 'Tasks/TaskStatusBadge',
  component: TaskStatusBadge
};

export const Pending = () => <TaskStatusBadge status={TaskStatus.Pending} />;
export const InProgress = () => <TaskStatusBadge status={TaskStatus.InProgress} />;
export const OnHold = () => <TaskStatusBadge status={TaskStatus.OnHold} />;
export const Completed = () => <TaskStatusBadge status={TaskStatus.Completed} />;
export const Cancelled = () => <TaskStatusBadge status={TaskStatus.Cancelled} />;

export const AllStatuses = () => (
  <div className="flex gap-2">
    <TaskStatusBadge status={TaskStatus.Pending} />
    <TaskStatusBadge status={TaskStatus.InProgress} />
    <TaskStatusBadge status={TaskStatus.OnHold} />
    <TaskStatusBadge status={TaskStatus.Completed} />
    <TaskStatusBadge status={TaskStatus.Cancelled} />
  </div>
);

