import React from 'react';
import TaskCreateModal from '../../features/tasks/components/TaskCreateModal';

export default {
  title: 'Tasks/TaskCreateModal',
  component: TaskCreateModal
};

export const Default = () => {
  const [show, setShow] = React.useState<boolean>(true);

  const handleSubmit = (taskData: any): void => {
    console.log('Task created:', taskData);
    setShow(false);
  };

  if (!show) {
    return (
      <button onClick={() => setShow(true)}>
        Open Create Modal
      </button>
    );
  }

  return (
    <TaskCreateModal
      departmentId="456"
      onClose={() => setShow(false)}
      onSubmit={handleSubmit}
    />
  );
};

