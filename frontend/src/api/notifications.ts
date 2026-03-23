import apiClient from './client';
import type {
  Notification,
  NotificationStatus,
  NotificationType,
  NotificationPriority,
  UnreadCountResponse
} from '../types/notifications';

/**
 * Notifications API
 * Handles user notifications, read status, and archiving
 */

// Re-export types for convenience (these are enums, so they can be exported as values)
export { NotificationStatus, NotificationType, NotificationPriority } from '../types/notifications';

/**
 * Get current user's notifications
 * @param status - Optional status filter (NotificationStatus enum value)
 * @param limit - Optional limit on number of notifications
 * @returns Array of notification items
 */
export const getMyNotifications = async (
  status: NotificationStatus | null = null,
  limit: number | null = null
): Promise<Notification[]> => {
  const params: Record<string, any> = {};
  if (status !== null) params.status = status;
  if (limit !== null) params.limit = limit;

  const response = await apiClient.get<Notification[]>(`/notifications/my`, { params });
  return response;
};

/**
 * Get unread notification count for current user
 * @returns Object with count property
 */
export const getUnreadCount = async (): Promise<UnreadCountResponse> => {
  const response = await apiClient.get<UnreadCountResponse>(`/notifications/my/unread-count`);
  return response;
};

/**
 * Mark a notification as read
 * @param notificationId - Notification ID
 * @returns Promise that resolves when notification is marked as read
 */
export const markAsRead = async (notificationId: string): Promise<void> => {
  await apiClient.put(`/notifications/${notificationId}/read`);
};

/**
 * Mark all notifications as read for current user
 * @returns Promise that resolves when all notifications are marked as read
 */
export const markAllAsRead = async (): Promise<void> => {
  await apiClient.put(`/notifications/my/read-all`);
};

/**
 * Archive a notification
 * @param notificationId - Notification ID
 * @returns Promise that resolves when notification is archived
 */
export const archiveNotification = async (notificationId: string): Promise<void> => {
  await apiClient.put(`/notifications/${notificationId}/archive`);
};

