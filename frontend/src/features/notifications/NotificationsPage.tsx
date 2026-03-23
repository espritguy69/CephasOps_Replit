import React, { useState, useEffect } from 'react';
import { CheckCircle2, Filter } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useNotifications } from '../../contexts/NotificationContext';
import { NotificationStatus, NotificationType } from '../../api/notifications';
import NotificationCard from '../../components/notifications/NotificationCard';
import { LoadingSpinner, EmptyState, Button, Card, Select, Breadcrumbs } from '../../components/ui';

/**
 * Notifications Page
 * Full page view of all user notifications with filters
 */
const NotificationsPage: React.FC = () => {
  const {
    notifications,
    loading,
    error,
    fetchNotifications,
    markAsRead,
    markAllAsRead,
    archiveNotification
  } = useNotifications();

  const navigate = useNavigate();
  const [statusFilter, setStatusFilter] = useState<number | null>(null);
  const [typeFilter, setTypeFilter] = useState<string | null>(null);

  useEffect(() => {
    fetchNotifications(statusFilter, null);
  }, [statusFilter, fetchNotifications]);

  const handleMarkAsRead = async (notificationId: string): Promise<void> => {
    try {
      await markAsRead(notificationId);
    } catch (err) {
      console.error('Failed to mark notification as read:', err);
    }
  };

  const handleArchive = async (notificationId: string): Promise<void> => {
    try {
      await archiveNotification(notificationId);
    } catch (err) {
      console.error('Failed to archive notification:', err);
    }
  };

  const handleNavigate = (url: string): void => {
    navigate(url);
  };

  const handleMarkAllAsRead = async (): Promise<void> => {
    try {
      await markAllAsRead();
    } catch (err) {
      console.error('Failed to mark all as read:', err);
    }
  };

  const filteredNotifications = notifications.filter((notification) => {
    if (statusFilter !== null && notification.status !== statusFilter) {
      return false;
    }
    if (typeFilter !== null && notification.type !== typeFilter) {
      return false;
    }
    return true;
  });

  const unreadCount = notifications.filter(n => n.status === NotificationStatus.Unread).length;

  if (loading) {
    return (
      <div className="flex-1 p-6">
        <LoadingSpinner message="Loading notifications..." fullPage />
      </div>
    );
  }

  return (
    <div className="flex-1 p-6 max-w-7xl mx-auto">
      <Breadcrumbs
        items={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'Notifications', active: true }
        ]}
        className="mb-6"
      />

      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Notifications</h1>
        {unreadCount > 0 && (
          <Button onClick={handleMarkAllAsRead}>
            <CheckCircle2 className="h-4 w-4 mr-2" />
            Mark all as read
          </Button>
        )}
      </div>

      <Card className="p-4 mb-6">
        <div className="flex items-center gap-2 mb-4">
          <Filter className="h-5 w-5 text-muted-foreground" />
          <h3 className="font-semibold">Filters</h3>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Select
            label="Status"
            value={statusFilter === null ? '' : String(statusFilter)}
            onChange={(e) => setStatusFilter(e.target.value === '' ? null : parseInt(e.target.value))}
            options={[
              { value: '', label: 'All' },
              { value: String(NotificationStatus.Unread), label: 'Unread' },
              { value: String(NotificationStatus.Read), label: 'Read' },
              { value: String(NotificationStatus.Archived), label: 'Archived' }
            ]}
          />

          <Select
            label="Type"
            value={typeFilter === null ? '' : typeFilter}
            onChange={(e) => setTypeFilter(e.target.value === '' ? null : e.target.value)}
            options={[
              { value: '', label: 'All' },
              { value: NotificationType.VipEmailReceived, label: 'VIP Email' },
              { value: NotificationType.TaskAssigned, label: 'Task Assigned' },
              { value: NotificationType.TaskCompleted, label: 'Task Completed' },
              { value: NotificationType.System, label: 'System' }
            ]}
          />
        </div>
      </Card>

      {error && (
        <div className="mb-6 rounded-lg border border-red-200 bg-red-50 p-4 text-red-800" role="alert">
          Error: {error}
        </div>
      )}

      {!loading && !error && (
        <div className="space-y-4">
          {filteredNotifications.length === 0 ? (
            <EmptyState
              title="No notifications found"
              description="Notifications will appear here when available."
            />
          ) : (
            filteredNotifications.map((notification) => (
              <NotificationCard
                key={notification.id}
                notification={notification}
                onMarkAsRead={handleMarkAsRead}
                onArchive={handleArchive}
                onNavigate={handleNavigate}
              />
            ))
          )}
        </div>
      )}
    </div>
  );
};

export default NotificationsPage;

