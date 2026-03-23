import React from 'react';
import { Bell, Mail, CheckCircle2, X, ArrowRight } from 'lucide-react';
import { NotificationType } from '../../api/notifications';
import { Card, StatusBadge, Button } from '../ui';
import { cn } from '../../lib/utils';
import type { Notification } from '../../types/notifications';

interface ExtendedNotification extends Notification {
  actionUrl?: string;
  actionText?: string;
}

interface NotificationCardProps {
  notification: ExtendedNotification;
  onMarkAsRead?: (id: string) => void;
  onArchive?: (id: string) => void;
  onNavigate?: (url: string) => void;
}

/**
 * Notification Card Component
 * Displays a single notification in a card format
 */
const NotificationCard: React.FC<NotificationCardProps> = ({ 
  notification, 
  onMarkAsRead, 
  onArchive, 
  onNavigate 
}) => {
  const isUnread = notification.status === 0;

  const handleClick = (): void => {
    if (isUnread && onMarkAsRead) {
      onMarkAsRead(notification.id);
    }

    if (notification.actionUrl && onNavigate) {
      onNavigate(notification.actionUrl);
    }
  };

  const getNotificationIcon = (type: NotificationType): React.ReactNode => {
    switch (type) {
      case NotificationType.VipEmailReceived:
        return <Mail className="h-5 w-5 text-blue-500" />;
      case NotificationType.TaskAssigned:
      case NotificationType.TaskCompleted:
        return <CheckCircle2 className="h-5 w-5 text-green-500" />;
      default:
        return <Bell className="h-5 w-5 text-muted-foreground" />;
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

  const getPriorityBadge = (priority: number): React.ReactNode => {
    switch (priority) {
      case 2: // High
        return <StatusBadge status="High" variant="error" size="sm" />;
      case 1: // Normal
        return <StatusBadge status="Normal" variant="info" size="sm" />;
      case 0: // Low
        return <StatusBadge status="Low" variant="secondary" size="sm" />;
      default:
        return null;
    }
  };

  return (
    <Card
      className={cn(
        "p-4 cursor-pointer transition-shadow hover:shadow-md",
        isUnread && "border-l-4 border-l-primary bg-accent/50"
      )}
      onClick={handleClick}
    >
      <div className="flex gap-4">
        <div className="flex-shrink-0">
          {getNotificationIcon(notification.type)}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2 mb-2">
            <div className="flex items-center gap-2">
              <h4 className={cn(
                "font-semibold text-base",
                isUnread && "font-bold"
              )}>
                {notification.title}
              </h4>
              {isUnread && (
                <span className="h-2 w-2 rounded-full bg-primary flex-shrink-0" />
              )}
            </div>
            {getPriorityBadge(notification.priority)}
          </div>
          <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
            {notification.message}
          </p>
          <div className="flex items-center justify-between">
            <span className="text-xs text-muted-foreground">
              {formatTimeAgo(notification.createdAt)}
            </span>
            <div className="flex items-center gap-2">
              {notification.actionText && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={(e) => {
                    e.stopPropagation();
                    if (notification.actionUrl && onNavigate) {
                      onNavigate(notification.actionUrl);
                    }
                  }}
                >
                  {notification.actionText}
                  <ArrowRight className="h-3 w-3 ml-1" />
                </Button>
              )}
              {onArchive && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={(e) => {
                    e.stopPropagation();
                    onArchive(notification.id);
                  }}
                  aria-label="Archive notification"
                >
                  <X className="h-4 w-4" />
                </Button>
              )}
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
};

export default NotificationCard;

