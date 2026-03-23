/**
 * Notification Types - Shared type definitions for Notifications module
 */

export enum NotificationStatus {
  Unread = 0,
  Read = 1,
  Archived = 2
}

export enum NotificationType {
  VipEmailReceived = 'VipEmailReceived',
  TaskAssigned = 'TaskAssigned',
  TaskCompleted = 'TaskCompleted',
  System = 'System'
}

export enum NotificationPriority {
  Low = 0,
  Normal = 1,
  High = 2
}

export interface Notification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  status: NotificationStatus;
  priority: NotificationPriority;
  userId: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  createdAt: string;
  readAt?: string;
  archivedAt?: string;
}

export interface UnreadCountResponse {
  count: number;
}

