import React, { useState, useEffect, useCallback } from 'react';
import { ChevronLeft, ChevronRight, AlertTriangle, Monitor } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast, Select, TextInput, ConfirmDialog } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import { getSecurityActivity, getSecurityAlerts } from '../../api/logs';
import { getAdminUserList } from '../../api/adminUsers';
import { getSessions, revokeSession } from '../../api/adminSessions';
import type { SecurityActivityEntry, SecurityAlert } from '../../api/logs';
import type { AdminUserListItem } from '../../api/adminUsers';
import type { UserSession } from '../../api/adminSessions';

const EVENT_LABELS: Record<string, string> = {
  LoginSuccess: 'Login success',
  LoginFailed: 'Login failed',
  AccountLocked: 'Account locked',
  PasswordChanged: 'Password changed',
  PasswordResetRequested: 'Password reset requested',
  PasswordResetCompleted: 'Password reset completed',
  AdminPasswordReset: 'Admin password reset',
  TokenRefresh: 'Token refresh'
};

const ALERT_TYPE_LABELS: Record<string, string> = {
  ExcessiveLoginFailures: 'Excessive login failures',
  PasswordResetAbuse: 'Password reset abuse',
  MultipleIpLogin: 'Multiple IP login'
};

const PAGE_SIZE = 20;

function formatDate(iso: string): string {
  try {
    const d = new Date(iso);
    return d.toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'medium' });
  } catch {
    return iso;
  }
}

const SecurityActivityPage: React.FC = () => {
  const { user } = useAuth();
  const { showError } = useToast();
  const [items, setItems] = useState<SecurityActivityEntry[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [users, setUsers] = useState<AdminUserListItem[]>([]);
  const [userIdFilter, setUserIdFilter] = useState<string>('');
  const [actionFilter, setActionFilter] = useState<string>('');
  const [dateFrom, setDateFrom] = useState<string>('');
  const [dateTo, setDateTo] = useState<string>('');
  const [alerts, setAlerts] = useState<SecurityAlert[]>([]);
  const [alertsLoading, setAlertsLoading] = useState(false);
  const [alertTypeFilter, setAlertTypeFilter] = useState<string>('');
  const [sessions, setSessions] = useState<UserSession[]>([]);
  const [sessionsLoading, setSessionsLoading] = useState(false);
  const [activeOnlySessions, setActiveOnlySessions] = useState<boolean>(true);
  const [sessionToRevoke, setSessionToRevoke] = useState<UserSession | null>(null);
  const [revoking, setRevoking] = useState(false);

  const canView = Boolean(user?.roles?.includes('SuperAdmin') || user?.roles?.includes('Admin'));

  const loadUsers = useCallback(async () => {
    if (!canView) return;
    try {
      const res = await getAdminUserList({ page: 1, pageSize: 500 });
      setUsers(res.items ?? []);
    } catch {
      setUsers([]);
    }
  }, [canView]);

  const loadActivity = useCallback(async () => {
    if (!canView) return;
    setLoading(true);
    try {
      const res = await getSecurityActivity({
        userId: userIdFilter || undefined,
        action: actionFilter || undefined,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
        page,
        pageSize: PAGE_SIZE
      });
      setItems(res.items);
      setTotalCount(res.totalCount);
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to load security activity');
      setItems([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  }, [canView, userIdFilter, actionFilter, dateFrom, dateTo, page, showError]);

  const loadAlerts = useCallback(async () => {
    if (!canView) return;
    setAlertsLoading(true);
    try {
      const from = dateFrom || undefined;
      const to = dateTo || undefined;
      const list = await getSecurityAlerts({
        dateFrom: from,
        dateTo: to,
        userId: userIdFilter || undefined,
        alertType: alertTypeFilter || undefined
      });
      setAlerts(list);
    } catch {
      setAlerts([]);
    } finally {
      setAlertsLoading(false);
    }
  }, [canView, dateFrom, dateTo, userIdFilter, alertTypeFilter]);

  const loadSessions = useCallback(async () => {
    if (!canView) return;
    setSessionsLoading(true);
    try {
      const list = await getSessions({
        userId: userIdFilter || undefined,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
        activeOnly: activeOnlySessions
      });
      setSessions(list);
    } catch {
      setSessions([]);
    } finally {
      setSessionsLoading(false);
    }
  }, [canView, userIdFilter, dateFrom, dateTo, activeOnlySessions]);

  const handleRevokeSession = useCallback(async () => {
    if (!sessionToRevoke) return;
    setRevoking(true);
    try {
      await revokeSession(sessionToRevoke.sessionId);
      setSessionToRevoke(null);
      await loadSessions();
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to revoke session');
    } finally {
      setRevoking(false);
    }
  }, [sessionToRevoke, loadSessions, showError]);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  useEffect(() => {
    loadActivity();
  }, [loadActivity]);

  useEffect(() => {
    loadAlerts();
  }, [loadAlerts]);

  useEffect(() => {
    loadSessions();
  }, [loadSessions]);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const alertTypeOptions = [
    { value: '', label: 'All alert types' },
    ...Object.entries(ALERT_TYPE_LABELS).map(([value, label]) => ({ value, label }))
  ];
  const actionOptions = [
    { value: '', label: 'All events' },
    ...Object.entries(EVENT_LABELS).map(([value, label]) => ({ value, label }))
  ];

  if (!canView) {
    return (
      <PageShell title="Security Activity" breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Security Activity' }]}>
        <Card className="p-6 text-center">
          <p className="text-muted-foreground">You do not have permission to view security activity. Only SuperAdmin and Admin roles can access this page.</p>
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="User Security Activity"
      breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Security Activity' }]}
    >
      <Card className="p-4 space-y-4">
        <div className="flex flex-wrap gap-4 items-end">
          <div className="min-w-[200px]">
            <label className="block text-sm font-medium mb-1">User</label>
            <Select
              value={userIdFilter}
              onChange={(e) => setUserIdFilter(e.target.value)}
              options={[
                { value: '', label: 'All users' },
                ...users.map((u) => ({ value: u.id, label: `${u.name} (${u.email})` }))
              ]}
            />
          </div>
          <div className="min-w-[180px]">
            <label className="block text-sm font-medium mb-1">Event type</label>
            <Select
              value={actionFilter}
              onChange={(e) => setActionFilter(e.target.value)}
              options={actionOptions}
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">From date</label>
            <TextInput
              type="date"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">To date</label>
            <TextInput
              type="date"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
            />
          </div>
          <Button onClick={() => { setPage(1); loadActivity(); loadAlerts(); loadSessions(); }}>Apply</Button>
        </div>

        <div className="border-t border-border pt-4 mt-4">
          <h3 className="text-sm font-semibold flex items-center gap-2 mb-2">
            <Monitor className="h-4 w-4" />
            Active Sessions
          </h3>
          {sessionsLoading ? (
            <div className="flex justify-center py-4">
              <LoadingSpinner />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm border-collapse">
                <thead>
                  <tr className="border-b border-border">
                    <th className="text-left py-2 px-2">User</th>
                    <th className="text-left py-2 px-2">Created</th>
                    <th className="text-left py-2 px-2">Expires</th>
                    <th className="text-left py-2 px-2">IP</th>
                    <th className="text-left py-2 px-2">Device</th>
                    <th className="text-left py-2 px-2">Status</th>
                    <th className="text-left py-2 px-2">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {sessions.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="py-4 text-center text-muted-foreground">
                        No sessions found.
                      </td>
                    </tr>
                  ) : (
                    sessions.map((s) => (
                      <tr key={s.sessionId} className="border-b border-border/50">
                        <td className="py-2 px-2">{s.userEmail ?? s.userId}</td>
                        <td className="py-2 px-2 whitespace-nowrap">{formatDate(s.createdAtUtc)}</td>
                        <td className="py-2 px-2 whitespace-nowrap">{formatDate(s.expiresAtUtc)}</td>
                        <td className="py-2 px-2">{s.ipAddress ?? '—'}</td>
                        <td className="py-2 px-2 max-w-[180px] truncate" title={s.userAgent ?? undefined}>{s.userAgent ?? '—'}</td>
                        <td className="py-2 px-2">{s.isRevoked ? 'Revoked' : 'Active'}</td>
                        <td className="py-2 px-2">
                          {!s.isRevoked && (
                            <Button variant="outline" size="sm" onClick={() => setSessionToRevoke(s)}>
                              Revoke
                            </Button>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
          <div className="flex flex-wrap gap-2 items-center mt-2">
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={activeOnlySessions} onChange={(e) => setActiveOnlySessions(e.target.checked)} />
              Active only
            </label>
            <Button variant="outline" size="sm" onClick={loadSessions}>Refresh sessions</Button>
          </div>
        </div>

        <div className="border-t border-border pt-4 mt-4">
          <h3 className="text-sm font-semibold flex items-center gap-2 mb-2">
            <AlertTriangle className="h-4 w-4 text-amber-500" />
            Security Alerts
          </h3>
          {alertsLoading ? (
            <div className="flex justify-center py-4">
              <LoadingSpinner />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm border-collapse">
                <thead>
                  <tr className="border-b border-border">
                    <th className="text-left py-2 px-2">Timestamp</th>
                    <th className="text-left py-2 px-2">User</th>
                    <th className="text-left py-2 px-2">Alert type</th>
                    <th className="text-left py-2 px-2">Description</th>
                    <th className="text-left py-2 px-2">IP summary</th>
                  </tr>
                </thead>
                <tbody>
                  {alerts.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-4 text-center text-muted-foreground">
                        No security alerts in the selected range.
                      </td>
                    </tr>
                  ) : (
                    alerts.map((alert, idx) => (
                      <tr key={`${alert.detectedAtUtc}-${alert.userId ?? 'u'}-${alert.alertType}-${idx}`} className="border-b border-border/50">
                        <td className="py-2 px-2 whitespace-nowrap">{formatDate(alert.detectedAtUtc)}</td>
                        <td className="py-2 px-2">{alert.userEmail ?? '—'}</td>
                        <td className="py-2 px-2">{ALERT_TYPE_LABELS[alert.alertType] ?? alert.alertType}</td>
                        <td className="py-2 px-2">{alert.description}</td>
                        <td className="py-2 px-2 max-w-[200px] truncate" title={alert.ipSummary ?? undefined}>
                          {alert.ipSummary ?? '—'}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
          <div className="flex flex-wrap gap-2 items-center mt-2">
            <span className="text-sm text-muted-foreground">Filter alerts:</span>
            <Select
              value={alertTypeFilter}
              onChange={(e) => setAlertTypeFilter(e.target.value)}
              options={alertTypeOptions}
            />
            <Button variant="outline" size="sm" onClick={loadAlerts}>Refresh alerts</Button>
          </div>
        </div>

        {loading ? (
          <div className="flex justify-center py-8">
            <LoadingSpinner />
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm border-collapse">
                <thead>
                  <tr className="border-b border-border">
                    <th className="text-left py-2 px-2">Timestamp</th>
                    <th className="text-left py-2 px-2">User</th>
                    <th className="text-left py-2 px-2">Event</th>
                    <th className="text-left py-2 px-2">IP</th>
                    <th className="text-left py-2 px-2">User agent</th>
                  </tr>
                </thead>
                <tbody>
                  {items.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-6 text-center text-muted-foreground">
                        No security events found.
                      </td>
                    </tr>
                  ) : (
                    items.map((row) => (
                      <tr key={row.id} className="border-b border-border/50">
                        <td className="py-2 px-2 whitespace-nowrap">{formatDate(row.timestamp)}</td>
                        <td className="py-2 px-2">{row.userEmail ?? '—'}</td>
                        <td className="py-2 px-2">{EVENT_LABELS[row.action] ?? row.action}</td>
                        <td className="py-2 px-2">{row.ipAddress ?? '—'}</td>
                        <td className="py-2 px-2 max-w-[200px] truncate" title={row.userAgent ?? undefined}>
                          {row.userAgent ?? '—'}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
            <div className="flex items-center justify-between pt-2">
              <span className="text-sm text-muted-foreground">
                {totalCount} total · page {page} of {totalPages}
              </span>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  <ChevronLeft className="h-4 w-4" /> Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                >
                  Next <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </>
        )}
      </Card>

      <ConfirmDialog
        isOpen={!!sessionToRevoke}
        onClose={() => setSessionToRevoke(null)}
        onConfirm={handleRevokeSession}
        title="Revoke session"
        message={sessionToRevoke && user && sessionToRevoke.userId === user.id
          ? 'This will log you out on that device. Continue?'
          : 'Revoke this session? The user will need to sign in again.'}
        confirmText="Revoke"
        variant="warning"
        isLoading={revoking}
      />
    </PageShell>
  );
};

export default SecurityActivityPage;
