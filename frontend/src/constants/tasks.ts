export const TaskStatus = {
  Pending: 1,
  InProgress: 2,
  OnHold: 3,
  Completed: 4,
  Cancelled: 5
} as const;

export type TaskStatus = typeof TaskStatus[keyof typeof TaskStatus];

export const TaskPriority = {
  Low: 1,
  Normal: 2,
  High: 3,
  Urgent: 4
} as const;

export type TaskPriority = typeof TaskPriority[keyof typeof TaskPriority];

