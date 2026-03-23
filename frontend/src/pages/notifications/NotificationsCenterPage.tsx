import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Bell, CheckCircle2, Archive, Filter, RefreshCw, Trash2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { PageShell } from '../../components/layout';
import { 
  LoadingSpinner, Card, Button, useToast, EmptyState, Select, Badge 
} from '../../components/ui';
import { 
  getMyNotifications, markAsRead, markAllAsRead, archiveNotification, getUnreadCount,
  NotificationStatus, NotificationType 
} from '../../api/notifications';
import NotificationCard from '../../components/notifications/NotificationCard';
import type { Notification } from '../../types/notifications';

/**
 * Notifications Center Page
 * Dedicated page for viewing and managing all notifications
 */
const NotificationsCenterPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<NotificationStatus | null>(null);
  const [typeFilter, setTypeFilter] = useState<string | null>(null);

  // Fetch notifications
  const { data: notifications = [], isLoading, refetch } = useQuery({
    queryKey: ['notifications', statusFilter, typeFilter],
    queryFn: async () => {
      const notifs = await getMyNotifications(statusFilter, null);
      return notifs;
    },
    staleTime: 30 * 1000, // 30 seconds
    refetchInterval: 60 * 1000, // Refetch every minute
  });

  // Fetch unread count
  const { data: unreadCount = { count: 0 } } = useQuery({
    queryKey: ['notifications', 'unread-count'],
    queryFn: async () => {
      const count = await getUnreadCount();
      return count;
    },
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000,
  });

  // Mark as read mutation
  const markAsReadMutation = useMutation({
    mutationFn: (id: string) => markAsRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to mark notification as read');
    },
  });

  // Mark all as read mutation
  const markAllAsReadMutation = useMutation({
    mutationFn: () => markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });
      showSuccess('All notifications marked as read');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to mark all notifications as read');
    },
  });

  // Archive mutation
  const archiveMutation = useMutation({
    mutationFn: (id: string) => archiveNotification(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });
      showSuccess('Notification archived');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to archive notification');
    },
  });

  const handleMarkAsRead = (notificationId: string) => {
    markAsReadMutation.mutate(notificationId);
  };

  const handleMarkAllAsRead = () => {
    if (unreadCount.count > 0) {
      markAllAsReadMutation.mutate();
    }
  };

  const handleArchive = (notificationId: string) => {
    archiveMutation.mutate(notificationId);
  };

  const handleNavigate = (url: string) => {
    navigate(url);
  };

  // Filter notifications
  const filteredNotifications = notifications.filter((notif: Notification) => {
    if (statusFilter !== null && notif.status !== statusFilter) {
      return false;
    }
    if (typeFilter !== null && notif.type !== typeFilter) {
      return false;
    }
    return true;
  });

  const unreadNotifications = filteredNotifications.filter(
    (notif: Notification) => notif.status === NotificationStatus.Unread
  );
  const readNotifications = filteredNotifications.filter(
    (notif: Notification) => notif.status === NotificationStatus.Read
  );
  const archivedNotifications = filteredNotifications.filter(
    (notif: Notification) => notif.status === NotificationStatus.Archived
  );

  if (isLoading) {
    return (
      <PageShell title="Notifications Center" breadcrumbs={[{ label: 'Notifications', path: '/notifications' }]}>
        <LoadingSpinner message="Loading notifications..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title={`Notifications Center - ${unreadCount.count} unread notification${unreadCount.count !== 1 ? 's' : ''}`}
      breadcrumbs={[{ label: 'Notifications', path: '/notifications' }]}
      actions={
        <div className="flex gap-2">
          <Button
            size="sm"
            variant="outline"
            className="gap-2"
            onClick={() => refetch()}
          >
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          {unreadCount.count > 0 && (
            <Button
              size="sm"
              variant="outline"
              className="gap-2"
              onClick={handleMarkAllAsRead}
              disabled={markAllAsReadMutation.isPending}
            >
              <CheckCircle2 className="h-4 w-4" />
              Mark All Read
            </Button>
          )}
        </div>
      }
    >
      {/* Filters */}
      <Card className="mb-6">
        <div className="flex flex-wrap gap-4 items-end">
          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Status
            </label>
            <Select
              value={statusFilter?.toString() || ''}
              onChange={(e) => setStatusFilter(e.target.value ? parseInt(e.target.value) as NotificationStatus : null)}
              options={[
                { value: '', label: 'All Statuses' },
                { value: NotificationStatus.Unread.toString(), label: 'Unread' },
                { value: NotificationStatus.Read.toString(), label: 'Read' },
                { value: NotificationStatus.Archived.toString(), label: 'Archived' },
              ]}
            />
          </div>
          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Type
            </label>
            <Select
              value={typeFilter || ''}
              onChange={(e) => setTypeFilter(e.target.value || null)}
              options={[
                { value: '', label: 'All Types' },
                { value: NotificationType.VipEmailReceived, label: 'VIP Email' },
                { value: NotificationType.TaskAssigned, label: 'Task Assigned' },
                { value: NotificationType.TaskCompleted, label: 'Task Completed' },
                { value: NotificationType.System, label: 'System' },
              ]}
            />
          </div>
        </div>
      </Card>

      {/* Notifications List */}
      {filteredNotifications.length === 0 ? (
          <EmptyState
            title="No Notifications"
            description={
              statusFilter !== null || typeFilter !== null
                ? "No notifications match your current filters"
                : "You're all caught up! No notifications to display."
            }
            icon={<Bell className="h-12 w-12" />}
          />
      ) : (
        <div className="space-y-6">
          {/* Unread Section */}
          {unreadNotifications.length > 0 && (
            <div>
              <div className="flex items-center gap-2 mb-4">
                <h3 className="text-lg font-semibold text-slate-900">Unread</h3>
                <Badge variant="default" className="bg-red-100 text-red-700 border-red-300">{unreadNotifications.length}</Badge>
              </div>
              <div className="space-y-2">
                {unreadNotifications.map((notif: Notification) => (
                  <NotificationCard
                    key={notif.id}
                    notification={notif}
                    onMarkAsRead={() => handleMarkAsRead(notif.id)}
                    onArchive={() => handleArchive(notif.id)}
                    onNavigate={handleNavigate}
                  />
                ))}
              </div>
            </div>
          )}

          {/* Read Section */}
          {readNotifications.length > 0 && (
            <div>
              <div className="flex items-center gap-2 mb-4">
                <h3 className="text-lg font-semibold text-slate-700">Read</h3>
                <Badge variant="secondary">{readNotifications.length}</Badge>
              </div>
              <div className="space-y-2">
                {readNotifications.map((notif: Notification) => (
                  <NotificationCard
                    key={notif.id}
                    notification={notif}
                    onMarkAsRead={() => handleMarkAsRead(notif.id)}
                    onArchive={() => handleArchive(notif.id)}
                    onNavigate={handleNavigate}
                  />
                ))}
              </div>
            </div>
          )}

          {/* Archived Section */}
          {archivedNotifications.length > 0 && (
            <div>
              <div className="flex items-center gap-2 mb-4">
                <h3 className="text-lg font-semibold text-slate-500">Archived</h3>
                <Badge variant="outline">{archivedNotifications.length}</Badge>
              </div>
              <div className="space-y-2">
                {archivedNotifications.map((notif: Notification) => (
                  <NotificationCard
                    key={notif.id}
                    notification={notif}
                    onMarkAsRead={() => handleMarkAsRead(notif.id)}
                    onArchive={() => handleArchive(notif.id)}
                    onNavigate={handleNavigate}
                  />
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </PageShell>
  );
};

export default NotificationsCenterPage;

