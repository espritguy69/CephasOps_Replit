import React, { createContext, useContext, useState, useEffect, useCallback, ReactNode } from 'react';
import * as notificationApi from '../api/notifications';
import { useAuth } from './AuthContext';
import type { Notification, NotificationStatus, UnreadCountResponse } from '../types/notifications';
import type { ApiError } from '../api/client';

interface NotificationContextType {
  notifications: Notification[];
  unreadCount: number;
  loading: boolean;
  error: string | null;
  fetchNotifications: (status?: NotificationStatus | null, limit?: number | null) => Promise<void>;
  fetchUnreadCount: () => Promise<void>;
  markAsRead: (notificationId: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  archiveNotification: (notificationId: string) => Promise<void>;
  refresh: () => Promise<void>;
}

const NotificationContext = createContext<NotificationContextType | null>(null);

/**
 * Notification Provider Component
 * Manages global notification state and polling
 */
interface NotificationProviderProps {
  children: ReactNode;
  pollingInterval?: number;
}

export const NotificationProvider: React.FC<NotificationProviderProps> = ({ children, pollingInterval = 30000 }) => {
  const { isAuthenticated, loading: authLoading } = useAuth();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState<number>(0);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  /**
   * Fetch user notifications
   */
  const fetchNotifications = useCallback(async (status: NotificationStatus | null = null, limit: number | null = null) => {
    // Don't fetch if not authenticated
    if (!isAuthenticated) {
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const data = await notificationApi.getMyNotifications(status, limit);
      setNotifications(data);
    } catch (err) {
      const apiError = err as ApiError;
      // Silently handle 401 errors (user not authenticated - expected)
      if (apiError.status === 401) {
        return;
      }
      const error = err as Error;
      setError(error.message);
      console.error('Failed to fetch notifications:', err);
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated]);

  /**
   * Fetch unread count
   */
  const fetchUnreadCount = useCallback(async () => {
    // Don't fetch if not authenticated
    if (!isAuthenticated) {
      return;
    }

    try {
      const response: UnreadCountResponse = await notificationApi.getUnreadCount();
      setUnreadCount(response.count || 0);
    } catch (err) {
      const apiError = err as ApiError;
      // Silently handle 401 errors (user not authenticated - expected)
      if (apiError.status === 401) {
        return;
      }
      console.error('Failed to fetch unread count:', err);
    }
  }, [isAuthenticated]);

  /**
   * Mark notification as read
   */
  const markAsRead = useCallback(async (notificationId: string) => {
    try {
      await notificationApi.markAsRead(notificationId);
      
      // Update local state
      setNotifications(prev => 
        prev.map(n => 
          n.id === notificationId 
            ? { ...n, status: NotificationStatus.Read, readAt: new Date().toISOString() }
            : n
        )
      );
      
      // Update unread count
      setUnreadCount(prev => Math.max(0, prev - 1));
    } catch (err) {
      console.error('Failed to mark notification as read:', err);
      throw err;
    }
  }, []);

  /**
   * Mark all notifications as read
   */
  const markAllAsRead = useCallback(async () => {
    try {
      await notificationApi.markAllAsRead();
      
      // Update local state
      setNotifications(prev => 
        prev.map(n => ({ ...n, status: NotificationStatus.Read, readAt: new Date().toISOString() }))
      );
      
      setUnreadCount(0);
    } catch (err) {
      console.error('Failed to mark all as read:', err);
      throw err;
    }
  }, []);

  /**
   * Archive notification
   */
  const archiveNotification = useCallback(async (notificationId: string) => {
    try {
      await notificationApi.archiveNotification(notificationId);
      
      // Remove from local state
      setNotifications(prev => prev.filter(n => n.id !== notificationId));
      
      // Update unread count if it was unread
      const notification = notifications.find(n => n.id === notificationId);
      if (notification && notification.status === NotificationStatus.Unread) {
        setUnreadCount(prev => Math.max(0, prev - 1));
      }
    } catch (err) {
      console.error('Failed to archive notification:', err);
      throw err;
    }
  }, [notifications]);

  /**
   * Refresh notifications and unread count
   */
  const refresh = useCallback(async () => {
    await Promise.all([
      fetchNotifications(),
      fetchUnreadCount()
    ]);
  }, [fetchNotifications, fetchUnreadCount]);

  // Initial fetch - wait for auth to finish loading
  useEffect(() => {
    if (!authLoading && isAuthenticated) {
      refresh();
    }
  }, [authLoading, isAuthenticated, refresh]);

  // Set up polling - only when authenticated
  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    const interval = setInterval(() => {
      fetchUnreadCount();
    }, pollingInterval);

    return () => clearInterval(interval);
  }, [pollingInterval, fetchUnreadCount, isAuthenticated]);

  // Refresh on window focus - only when authenticated
  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    const handleFocus = () => {
      fetchUnreadCount();
    };

    window.addEventListener('focus', handleFocus);
    return () => window.removeEventListener('focus', handleFocus);
  }, [fetchUnreadCount, isAuthenticated]);

  const value: NotificationContextType = {
    notifications,
    unreadCount,
    loading,
    error,
    fetchNotifications,
    fetchUnreadCount,
    markAsRead,
    markAllAsRead,
    archiveNotification,
    refresh
  };

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  );
};

/**
 * Hook to use notification context
 */
export const useNotifications = (): NotificationContextType => {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotifications must be used within NotificationProvider');
  }
  return context;
};

