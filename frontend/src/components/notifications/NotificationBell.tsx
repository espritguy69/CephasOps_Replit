import React, { useState, useRef, useEffect } from 'react';
import { Bell, Mail, CheckCircle2, ArrowRight } from 'lucide-react';
import { useNotifications } from '../../contexts/NotificationContext';
import { NotificationType } from '../../api/notifications';
import { Dropdown, DropdownItem, DropdownHeader, Button } from '../ui';
import { cn } from '../../lib/utils';
import type { Notification } from '../../types/notifications';

/**
 * Notification Bell Component
 * Displays notification bell icon with unread count and dropdown
 */
const NotificationBell: React.FC = () => {
  const { notifications, unreadCount, markAsRead, fetchNotifications } = useNotifications();
  const [isOpen, setIsOpen] = useState<boolean>(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Fetch latest notifications when dropdown opens
  useEffect(() => {
    if (isOpen) {
      fetchNotifications(null, 10); // Fetch latest 10 notifications
    }
  }, [isOpen, fetchNotifications]);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent): void => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  const handleNotificationClick = async (notification: Notification): Promise<void> => {
    if (notification.status === 0) {
      await markAsRead(notification.id);
    }

    // Navigate to action URL if available
    const extendedNotification = notification as Notification & { actionUrl?: string };
    if (extendedNotification.actionUrl) {
      window.location.href = extendedNotification.actionUrl;
    }

    setIsOpen(false);
  };

  const getNotificationIcon = (type: NotificationType): React.ReactNode => {
    switch (type) {
      case NotificationType.VipEmailReceived:
        return <Mail className="h-3 w-3" />;
      case NotificationType.TaskAssigned:
      case NotificationType.TaskCompleted:
        return <CheckCircle2 className="h-3 w-3" />;
      default:
        return <Bell className="h-3 w-3" />;
    }
  };

  const formatTimeAgo = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  };

  return (
    <div className="relative" ref={dropdownRef}>
      <Dropdown
        trigger={
          <button
            className="relative p-1 rounded hover:bg-accent transition-colors"
            aria-label="Notifications"
          >
            <Bell className="h-4 w-4" />
            {unreadCount > 0 && (
              <span className="absolute -top-0.5 -right-0.5 h-4 w-4 rounded-full bg-destructive text-destructive-foreground text-xs font-medium flex items-center justify-center">
                {unreadCount > 9 ? '9+' : unreadCount}
              </span>
            )}
          </button>
        }
        placement="bottom-right"
        className="w-[400px]"
      >
        <DropdownHeader>
          <div className="flex justify-between items-center">
            <h3 className="text-xs font-semibold">Notifications</h3>
            {unreadCount > 0 && (
              <span className="text-xs text-muted-foreground">{unreadCount} unread</span>
            )}
          </div>
        </DropdownHeader>
        
        <div className="max-h-[400px] overflow-y-auto">
          {notifications.length === 0 ? (
            <div className="p-2 text-center text-xs text-muted-foreground">
              No notifications
            </div>
          ) : (
            notifications.map((notification) => (
              <DropdownItem
                key={notification.id}
                onClick={() => handleNotificationClick(notification)}
                className={cn(
                  "p-2 flex gap-2 cursor-pointer",
                  notification.status === 0 && "bg-accent"
                )}
              >
                <div className="flex-shrink-0 mt-0.5 text-muted-foreground">
                  {getNotificationIcon(notification.type)}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-2 mb-0.5">
                    <span className={cn(
                      "text-xs font-medium",
                      notification.status === 0 && "font-semibold"
                    )}>
                      {notification.title}
                    </span>
                    {notification.status === 0 && (
                      <span className="h-1.5 w-1.5 rounded-full bg-primary flex-shrink-0 mt-1" />
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground mb-0.5 line-clamp-2">
                    {notification.message}
                  </p>
                  <span className="text-xs text-muted-foreground">
                    {formatTimeAgo(notification.createdAt)}
                  </span>
                </div>
              </DropdownItem>
            ))
          )}
        </div>
        
        <div className="p-2 border-t">
          <a
            href="/notifications"
            className="flex items-center justify-center gap-1 text-xs text-primary hover:underline w-full py-1"
          >
            View all notifications
            <ArrowRight className="h-3 w-3" />
          </a>
        </div>
      </Dropdown>
    </div>
  );
};

export default NotificationBell;

