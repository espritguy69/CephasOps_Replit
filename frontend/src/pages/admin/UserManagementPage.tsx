import React, { useState, useEffect, useCallback } from 'react';
import {
  Plus,
  Search,
  Eye,
  Edit,
  Key,
  Power,
  Shield,
  Loader2,
  UserPlus,
} from 'lucide-react';
import { PageShell } from '../../components/layout';
import {
  Button,
  Card,
  LoadingSpinner,
  EmptyState,
  Modal,
  ConfirmDialog,
  useToast,
  TextInput,
  Select,
  Badge,
} from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import {
  getAdminUserList,
  getAdminUserRoles,
  getAdminUserById,
  createAdminUser,
  updateAdminUser,
  setAdminUserActive,
  setAdminUserRoles,
  resetAdminUserPassword,
} from '../../api/adminUsers';
import { getSecurityActivity, getSecurityAlerts } from '../../api/logs';
import { getSessionsForUser, revokeSession, revokeAllSessionsForUser } from '../../api/adminSessions';
import type { SecurityActivityEntry, SecurityAlert } from '../../api/logs';
import type { UserSession } from '../../api/adminSessions';
import { getDepartments } from '../../api/departments';
import type {
  AdminUserListItem,
  AdminUserDetail,
  CreateAdminUserRequest,
  UpdateAdminUserRequest,
  AdminUserDepartmentMembership,
} from '../../api/adminUsers';
import type { Department } from '../../types/departments';

const PAGE_SIZE = 20;

const SECURITY_EVENT_LABELS: Record<string, string> = {
  LoginSuccess: 'Login success',
  LoginFailed: 'Login failed',
  AccountLocked: 'Account locked',
  PasswordChanged: 'Password changed',
  PasswordResetRequested: 'Password reset requested',
  PasswordResetCompleted: 'Password reset completed',
  AdminPasswordReset: 'Admin password reset',
  TokenRefresh: 'Token refresh',
};

function securityEventLabel(action: string): string {
  return SECURITY_EVENT_LABELS[action] ?? action;
}

const UserManagementPage: React.FC = () => {
  const { user: currentUser } = useAuth();
  const { showSuccess, showError } = useToast();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [items, setItems] = useState<AdminUserListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [roles, setRoles] = useState<string[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);

  const [modalMode, setModalMode] = useState<'closed' | 'view' | 'create' | 'edit'>('closed');
  const [selectedUser, setSelectedUser] = useState<AdminUserDetail | null>(null);
  const [formLoading, setFormLoading] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    phone: '',
    password: '',
    roleNames: [] as string[],
    departmentMemberships: [] as AdminUserDepartmentMembership[],
  });

  const [confirmActive, setConfirmActive] = useState<{ user: AdminUserListItem; isActive: boolean } | null>(null);
  const [roleModal, setRoleModal] = useState<AdminUserListItem | null>(null);
  const [roleModalSelected, setRoleModalSelected] = useState<string[]>([]);
  const [roleModalSaving, setRoleModalSaving] = useState(false);
  const [passwordModal, setPasswordModal] = useState<AdminUserListItem | null>(null);
  const [passwordValue, setPasswordValue] = useState('');
  const [passwordForceMustChange, setPasswordForceMustChange] = useState(true);
  const [passwordSaving, setPasswordSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [userSecurityActivity, setUserSecurityActivity] = useState<SecurityActivityEntry[]>([]);
  const [userSecurityActivityLoading, setUserSecurityActivityLoading] = useState(false);
  const [userSecurityAlerts, setUserSecurityAlerts] = useState<SecurityAlert[]>([]);
  const [userSecurityAlertsLoading, setUserSecurityAlertsLoading] = useState(false);
  const [userSessions, setUserSessions] = useState<UserSession[]>([]);
  const [userSessionsLoading, setUserSessionsLoading] = useState(false);
  const [sessionToRevoke, setSessionToRevoke] = useState<UserSession | null>(null);
  const [revokeAllConfirm, setRevokeAllConfirm] = useState<AdminUserDetail | null>(null);
  const [revokingSession, setRevokingSession] = useState(false);

  const canAccess =
    currentUser?.roles?.includes('SuperAdmin') || currentUser?.roles?.includes('Admin');

  const loadList = useCallback(async () => {
    if (!canAccess) return;
    setLoading(true);
    setError(null);
    try {
      const result = await getAdminUserList({
        page,
        pageSize: PAGE_SIZE,
        search: search.trim() || undefined,
        role: roleFilter || undefined,
        isActive:
          statusFilter === 'all' ? undefined : statusFilter === 'active',
      });
      setItems(result.items ?? []);
      setTotalCount(result.totalCount ?? 0);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Failed to load users';
      setError(msg);
      showError(msg);
      setItems([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  }, [canAccess, page, search, roleFilter, statusFilter, showError]);

  useEffect(() => {
    if (canAccess) {
      loadList();
    }
  }, [canAccess, loadList]);

  useEffect(() => {
    if (modalMode !== 'view' || !selectedUser) {
      setUserSecurityActivity([]);
      return;
    }
    let cancelled = false;
    setUserSecurityActivityLoading(true);
    getSecurityActivity({ userId: selectedUser.id, page: 1, pageSize: 10 })
      .then((res) => {
        if (!cancelled) setUserSecurityActivity(res.items ?? []);
      })
      .catch(() => {
        if (!cancelled) setUserSecurityActivity([]);
      })
      .finally(() => {
        if (!cancelled) setUserSecurityActivityLoading(false);
      });
    return () => { cancelled = true; };
  }, [modalMode, selectedUser?.id]);

  useEffect(() => {
    if (modalMode !== 'view' || !selectedUser) {
      setUserSecurityAlerts([]);
      return;
    }
    let cancelled = false;
    setUserSecurityAlertsLoading(true);
    const to = new Date();
    const from = new Date(to);
    from.setDate(from.getDate() - 7);
    getSecurityAlerts({
      userId: selectedUser.id,
      dateFrom: from.toISOString().slice(0, 10),
      dateTo: to.toISOString().slice(0, 10)
    })
      .then((list) => {
        if (!cancelled) setUserSecurityAlerts(list ?? []);
      })
      .catch(() => {
        if (!cancelled) setUserSecurityAlerts([]);
      })
      .finally(() => {
        if (!cancelled) setUserSecurityAlertsLoading(false);
      });
    return () => { cancelled = true; };
  }, [modalMode, selectedUser?.id]);

  useEffect(() => {
    if (modalMode !== 'view' || !selectedUser) {
      setUserSessions([]);
      return;
    }
    let cancelled = false;
    setUserSessionsLoading(true);
    getSessionsForUser(selectedUser.id)
      .then((list) => {
        if (!cancelled) setUserSessions(list ?? []);
      })
      .catch(() => {
        if (!cancelled) setUserSessions([]);
      })
      .finally(() => {
        if (!cancelled) setUserSessionsLoading(false);
      });
    return () => { cancelled = true; };
  }, [modalMode, selectedUser?.id]);

  const handleRevokeSession = useCallback(async () => {
    if (!sessionToRevoke) return;
    setRevokingSession(true);
    try {
      await revokeSession(sessionToRevoke.sessionId);
      setSessionToRevoke(null);
      if (selectedUser) {
        const list = await getSessionsForUser(selectedUser.id);
        setUserSessions(list);
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to revoke session');
    } finally {
      setRevokingSession(false);
    }
  }, [sessionToRevoke, selectedUser, showError]);

  const handleRevokeAllSessions = useCallback(async () => {
    if (!revokeAllConfirm) return;
    const userId = revokeAllConfirm.id;
    setRevokingSession(true);
    try {
      await revokeAllSessionsForUser(userId);
      setRevokeAllConfirm(null);
      const list = await getSessionsForUser(userId);
      setUserSessions(list);
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to revoke sessions');
    } finally {
      setRevokingSession(false);
    }
  }, [revokeAllConfirm, showError]);

  useEffect(() => {
    if (!canAccess) return;
    getAdminUserRoles()
      .then(setRoles)
      .catch(() => setRoles([]));
    getDepartments()
      .then((list) => setDepartments(Array.isArray(list) ? list : []))
      .catch(() => setDepartments([]));
  }, [canAccess]);

  const openView = async (user: AdminUserListItem) => {
    setFormLoading(true);
    setModalMode('view');
    try {
      const detail = await getAdminUserById(user.id);
      setSelectedUser(detail ?? null);
    } catch {
      showError('Failed to load user details');
      setModalMode('closed');
    } finally {
      setFormLoading(false);
    }
  };

  const openEdit = async (user: AdminUserListItem) => {
    setFormLoading(true);
    setFormError(null);
    setModalMode('edit');
    try {
      const detail = await getAdminUserById(user.id);
      if (detail) {
        setSelectedUser(detail);
        setFormData({
          name: detail.name,
          email: detail.email,
          phone: detail.phone ?? '',
          password: '',
          roleNames: [...(detail.roles ?? [])],
          departmentMemberships: (detail.departments ?? []).map((d) => ({
            departmentId: d.departmentId,
            role: d.role ?? 'Member',
            isDefault: false,
          })),
        });
      }
    } catch {
      showError('Failed to load user details');
      setModalMode('closed');
    } finally {
      setFormLoading(false);
    }
  };

  const openCreate = () => {
    setSelectedUser(null);
    setFormError(null);
    setFormData({
      name: '',
      email: '',
      phone: '',
      password: '',
      roleNames: [],
      departmentMemberships: [],
    });
    setModalMode('create');
  };

  const closeModal = () => {
    setModalMode('closed');
    setSelectedUser(null);
    setFormError(null);
  };

  const handleCreate = async () => {
    setFormError(null);
    if (!formData.email?.trim()) {
      setFormError('Email is required.');
      showError('Email is required');
      return;
    }
    if (!formData.password || formData.password.length < 6) {
      setFormError('Password must be at least 6 characters.');
      showError('Password must be at least 6 characters');
      return;
    }
    if (!formData.roleNames.length) {
      setFormError('At least one role is required.');
      showError('At least one role is required');
      return;
    }
    setFormLoading(true);
    try {
      const req: CreateAdminUserRequest = {
        name: formData.name.trim(),
        email: formData.email.trim().toLowerCase(),
        phone: formData.phone?.trim() || undefined,
        password: formData.password,
        roleNames: formData.roleNames,
        departmentMemberships:
          formData.departmentMemberships.length > 0
            ? formData.departmentMemberships
            : undefined,
      };
      await createAdminUser(req);
      showSuccess('User created');
      closeModal();
      loadList();
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Failed to create user';
      setFormError(msg);
      showError(msg);
    } finally {
      setFormLoading(false);
    }
  };

  const handleUpdate = async () => {
    if (!selectedUser) return;
    setFormError(null);
    if (!formData.email?.trim()) {
      setFormError('Email is required.');
      showError('Email is required');
      return;
    }
    if (!formData.roleNames.length) {
      setFormError('At least one role is required.');
      showError('At least one role is required');
      return;
    }
    setFormLoading(true);
    try {
      const req: UpdateAdminUserRequest = {
        name: formData.name.trim(),
        email: formData.email.trim().toLowerCase(),
        phone: formData.phone?.trim() || undefined,
        roleNames: formData.roleNames,
        departmentMemberships:
          formData.departmentMemberships.length > 0
            ? formData.departmentMemberships
            : undefined,
      };
      await updateAdminUser(selectedUser.id, req);
      showSuccess('User updated');
      closeModal();
      loadList();
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Failed to update user';
      setFormError(msg);
      showError(msg);
    } finally {
      setFormLoading(false);
    }
  };

  const handleConfirmActive = async () => {
    if (!confirmActive) return;
    try {
      await setAdminUserActive(confirmActive.user.id, confirmActive.isActive);
      showSuccess(confirmActive.isActive ? 'User activated' : 'User deactivated');
      setConfirmActive(null);
      loadList();
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to update status');
    }
  };

  const handleSaveRoles = async () => {
    if (!roleModal) return;
    setRoleModalSaving(true);
    try {
      await setAdminUserRoles(roleModal.id, roleModalSelected);
      showSuccess('Roles updated');
      setRoleModal(null);
      loadList();
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to update roles');
    } finally {
      setRoleModalSaving(false);
    }
  };

  const handleResetPassword = async () => {
    if (!passwordModal || !passwordValue || passwordValue.length < 6) {
      showError('Password must be at least 6 characters');
      return;
    }
    setPasswordSaving(true);
    try {
      await resetAdminUserPassword(passwordModal.id, passwordValue, passwordForceMustChange);
      showSuccess('Password reset');
      setPasswordModal(null);
      setPasswordValue('');
      setPasswordForceMustChange(true);
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to reset password');
    } finally {
      setPasswordSaving(false);
    }
  };

  const formatDate = (s: string) => {
    try {
      return new Date(s).toLocaleDateString(undefined, {
        dateStyle: 'short',
      });
    } catch {
      return s;
    }
  };

  if (!canAccess) {
    return (
      <PageShell
        title="User Management"
        breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'User Management' }]}
      >
        <Card className="p-6 text-center">
          <p className="text-muted-foreground">
            You do not have permission to manage users. Only SuperAdmin and Admin roles can access this page.
          </p>
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="User Management"
      breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'User Management' }]}
      actions={
        <Button size="sm" onClick={openCreate} className="gap-1">
          <UserPlus className="h-4 w-4" />
          Add User
        </Button>
      }
    >
      <p className="text-sm text-muted-foreground mb-4">
        Manage user accounts, roles, and access. If an action fails, check the message shown; you cannot deactivate yourself or remove the last active administrator.
      </p>

      <div className="flex flex-wrap items-center gap-2 mb-4">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search by name or email..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && loadList()}
            className="w-full pl-8 pr-3 py-2 border rounded-md text-sm"
          />
        </div>
        <Select
          value={roleFilter}
          onChange={(e) => setRoleFilter(e.target.value)}
          options={[{ value: '', label: 'All roles' }, ...roles.map((r) => ({ value: r, label: r }))]}
          className="w-[160px]"
        />
        <Select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as 'all' | 'active' | 'inactive')}
          options={[
            { value: 'all', label: 'All statuses' },
            { value: 'active', label: 'Active' },
            { value: 'inactive', label: 'Inactive' },
          ]}
          className="w-[140px]"
        />
        <Button variant="outline" size="sm" onClick={loadList} disabled={loading}>
          Search
        </Button>
      </div>

      {error && (
        <div className="mb-4 p-3 rounded-md bg-destructive/10 text-destructive text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <div className="flex justify-center py-12">
          <LoadingSpinner />
        </div>
      ) : items.length === 0 ? (
        <EmptyState
          title="No users found"
          message="Try adjusting search or filters, or add a new user."
        />
      ) : (
        <Card className="overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="text-left p-3 font-medium">Full Name</th>
                  <th className="text-left p-3 font-medium">Email</th>
                  <th className="text-left p-3 font-medium">Role</th>
                  <th className="text-left p-3 font-medium">Departments</th>
                  <th className="text-left p-3 font-medium">Status</th>
                  <th className="text-left p-3 font-medium">Last Login</th>
                  <th className="text-left p-3 font-medium">Must change</th>
                  <th className="text-left p-3 font-medium">Created</th>
                  <th className="text-right p-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {items.map((u) => (
                  <tr key={u.id} className="border-b hover:bg-muted/30">
                    <td className="p-3">{u.name || '—'}</td>
                    <td className="p-3">{u.email}</td>
                    <td className="p-3">
                      <div className="flex flex-wrap gap-1">
                        {(u.roles ?? []).map((r) => (
                          <Badge key={r} variant="secondary" className="text-xs">
                            {r}
                          </Badge>
                        ))}
                        {(!u.roles || u.roles.length === 0) && '—'}
                      </div>
                    </td>
                    <td className="p-3">
                      <div className="flex flex-wrap gap-1">
                        {(u.departments ?? []).map((d) => (
                          <Badge key={d.departmentId} variant="outline" className="text-xs">
                            {d.departmentName}
                            {d.role ? ` (${d.role})` : ''}
                          </Badge>
                        ))}
                        {(!u.departments || u.departments.length === 0) && '—'}
                      </div>
                    </td>
                    <td className="p-3">
                      <Badge variant={u.isActive ? 'default' : 'secondary'}>
                        {u.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </td>
                    <td className="p-3 text-muted-foreground">
                      {u.lastLoginAtUtc ? formatDate(u.lastLoginAtUtc) : '—'}
                    </td>
                    <td className="p-3">
                      {u.mustChangePassword ? (
                        <Badge variant="outline" className="text-amber-600 border-amber-300">Yes</Badge>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </td>
                    <td className="p-3 text-muted-foreground">{formatDate(u.createdAt)}</td>
                    <td className="p-3 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          type="button"
                          onClick={() => openView(u)}
                          className="p-1.5 rounded hover:bg-muted"
                          title="View"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => openEdit(u)}
                          className="p-1.5 rounded hover:bg-muted"
                          title="Edit"
                        >
                          <Edit className="h-4 w-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => {
                            setRoleModal(u);
                            setRoleModalSelected(u.roles ?? []);
                          }}
                          className="p-1.5 rounded hover:bg-muted"
                          title="Change Role"
                        >
                          <Shield className="h-4 w-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() =>
                            setConfirmActive({ user: u, isActive: !u.isActive })
                          }
                          className="p-1.5 rounded hover:bg-muted"
                          title={u.isActive ? 'Deactivate' : 'Activate'}
                        >
                          <Power className="h-4 w-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => {
                            setPasswordModal(u);
                            setPasswordValue('');
                          }}
                          className="p-1.5 rounded hover:bg-muted"
                          title="Reset Password"
                        >
                          <Key className="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {totalCount > PAGE_SIZE && (
            <div className="flex items-center justify-between px-3 py-2 border-t text-sm text-muted-foreground">
              <span>
                Showing {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, totalCount)} of {totalCount}
              </span>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page * PAGE_SIZE >= totalCount}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </Card>
      )}

      {/* View / Create / Edit Modal */}
      <Modal
        isOpen={modalMode !== 'closed'}
        onClose={closeModal}
        title={
          modalMode === 'view'
            ? 'User details'
            : modalMode === 'create'
              ? 'Add User'
              : 'Edit User'
        }
        size="medium"
      >
        {formLoading ? (
          <div className="flex justify-center py-8">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : modalMode === 'view' && selectedUser ? (
          <div className="space-y-4 text-sm">
            <div className="space-y-3">
              <p><span className="font-medium">Name:</span> {selectedUser.name}</p>
              <p><span className="font-medium">Email:</span> {selectedUser.email}</p>
              <p><span className="font-medium">Phone:</span> {selectedUser.phone || '—'}</p>
              <p><span className="font-medium">Status:</span> {selectedUser.isActive ? 'Active' : 'Inactive'}</p>
              <p><span className="font-medium">Last login:</span> {selectedUser.lastLoginAtUtc ? formatDate(selectedUser.lastLoginAtUtc) : '—'}</p>
              <p><span className="font-medium">Must change password:</span> {selectedUser.mustChangePassword ? 'Yes' : 'No'}</p>
              <p><span className="font-medium">Roles:</span> {(selectedUser.roles ?? []).join(', ') || '—'}</p>
              <p><span className="font-medium">Departments:</span>{' '}
                {(selectedUser.departments ?? []).map((d) => d.departmentName + (d.role ? ' (' + d.role + ')' : '')).join(', ') || '—'}
              </p>
              <p><span className="font-medium">Created:</span> {formatDate(selectedUser.createdAt)}</p>
            </div>
            <div>
              <p className="font-medium mb-2">Security Activity (last 10)</p>
              {userSecurityActivityLoading ? (
                <p className="text-muted-foreground">Loading…</p>
              ) : userSecurityActivity.length === 0 ? (
                <p className="text-muted-foreground">No recent security events.</p>
              ) : (
                <ul className="border border-border rounded-md divide-y divide-border text-xs">
                  {userSecurityActivity.map((ev) => (
                    <li key={ev.id} className="px-2 py-1.5 flex justify-between gap-2">
                      <span>{formatDate(ev.timestamp)}</span>
                      <span>{securityEventLabel(ev.action)}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
            <div>
              <p className="font-medium mb-2">Security Alerts</p>
              {userSecurityAlertsLoading ? (
                <p className="text-muted-foreground">Loading…</p>
              ) : userSecurityAlerts.length === 0 ? (
                <p className="text-muted-foreground">No alerts in the last 7 days.</p>
              ) : (
                <ul className="border border-border rounded-md divide-y divide-border text-xs space-y-1">
                  {userSecurityAlerts.map((alert, idx) => (
                    <li key={`${alert.detectedAtUtc}-${alert.alertType}-${idx}`} className="px-2 py-1.5 flex items-start gap-2">
                      <span className="text-amber-600 shrink-0" aria-hidden>⚠</span>
                      <span>{alert.description} ({formatDate(alert.detectedAtUtc)})</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
            <div>
              <p className="font-medium mb-2">Active Sessions</p>
              {userSessionsLoading ? (
                <p className="text-muted-foreground">Loading…</p>
              ) : userSessions.length === 0 ? (
                <p className="text-muted-foreground">No sessions.</p>
              ) : (
                <>
                  <ul className="border border-border rounded-md divide-y divide-border text-xs">
                    {userSessions.map((s) => (
                      <li key={s.sessionId} className="px-2 py-1.5 flex justify-between items-center gap-2">
                        <span>{formatDate(s.createdAtUtc)} · {s.ipAddress ?? '—'} · {s.userAgent ? s.userAgent.slice(0, 30) + (s.userAgent.length > 30 ? '…' : '') : '—'} · {s.isRevoked ? 'Revoked' : 'Active'}</span>
                        {!s.isRevoked && (
                          <Button variant="outline" size="sm" onClick={() => setSessionToRevoke(s)}>Revoke</Button>
                        )}
                      </li>
                    ))}
                  </ul>
                  {userSessions.some((s) => !s.isRevoked) && (
                    <Button variant="outline" size="sm" className="mt-2" onClick={() => setRevokeAllConfirm(selectedUser)}>
                      Revoke all sessions
                    </Button>
                  )}
                </>
              )}
            </div>
          </div>
        ) : (modalMode === 'create' || modalMode === 'edit') && (
          <div className="space-y-4">
            {formError && (
              <div className="p-3 rounded-md bg-destructive/10 text-destructive text-sm">
                {formError}
              </div>
            )}
            <TextInput
              label="Full Name"
              value={formData.name}
              onChange={(e) => setFormData((f) => ({ ...f, name: e.target.value }))}
            />
            <TextInput
              label="Email"
              type="email"
              value={formData.email}
              onChange={(e) => setFormData((f) => ({ ...f, email: e.target.value }))}
              disabled={modalMode === 'edit'}
            />
            <TextInput
              label="Phone"
              value={formData.phone}
              onChange={(e) => setFormData((f) => ({ ...f, phone: e.target.value }))}
            />
            {modalMode === 'create' && (
              <TextInput
                label="Password"
                type="password"
                value={formData.password}
                onChange={(e) => setFormData((f) => ({ ...f, password: e.target.value }))}
                placeholder="Min 6 characters"
              />
            )}
            <div>
              <label className="block text-sm font-medium mb-1">Roles</label>
              <div className="flex flex-wrap gap-2">
                {roles.map((r) => (
                  <label key={r} className="flex items-center gap-1.5 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={formData.roleNames.includes(r)}
                      onChange={(e) =>
                        setFormData((f) => ({
                          ...f,
                          roleNames: e.target.checked
                            ? [...f.roleNames, r]
                            : f.roleNames.filter((x) => x !== r),
                        }))
                      }
                    />
                    <span className="text-sm">{r}</span>
                  </label>
                ))}
              </div>
              {modalMode === 'create' && (
                <p className="text-xs text-muted-foreground mt-1">At least one role is required.</p>
              )}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Departments</label>
              <p className="text-xs text-muted-foreground mb-2">Assign user to departments. Optional per-department role.</p>
              <div className="flex flex-wrap gap-3">
                {(departments || []).filter((d) => d.isActive !== false).map((d) => {
                  const membership = formData.departmentMemberships.find((m) => m.departmentId === d.id);
                  const checked = !!membership;
                  return (
                    <div key={d.id} className="flex items-center gap-2">
                      <label className="flex items-center gap-1.5 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={checked}
                          onChange={(e) => {
                            if (e.target.checked) {
                              setFormData((f) => ({
                                ...f,
                                departmentMemberships: [...f.departmentMemberships, { departmentId: d.id, role: 'Member', isDefault: false }],
                              }));
                            } else {
                              setFormData((f) => ({
                                ...f,
                                departmentMemberships: f.departmentMemberships.filter((m) => m.departmentId !== d.id),
                              }));
                            }
                          }}
                        />
                        <span className="text-sm">{d.name}</span>
                      </label>
                      {checked && (
                        <Select
                          value={membership?.role ?? 'Member'}
                          onChange={(ev) =>
                            setFormData((f) => ({
                              ...f,
                              departmentMemberships: f.departmentMemberships.map((m) =>
                                m.departmentId === d.id ? { ...m, role: ev.target.value } : m
                              ),
                            }))
                          }
                          options={[
                            { value: 'Member', label: 'Member' },
                            { value: 'HOD', label: 'HOD' },
                          ]}
                          className="w-24 text-xs py-1"
                        />
                      )}
                    </div>
                  );
                })}
              </div>
              {((departments || []).filter((d) => d.isActive !== false).length === 0) && (
                <p className="text-xs text-muted-foreground">No departments available.</p>
              )}
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={closeModal} disabled={formLoading}>
                Cancel
              </Button>
              {modalMode === 'create' ? (
                <Button onClick={handleCreate} disabled={formLoading}>
                  {formLoading ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin mr-1" />
                      Creating…
                    </>
                  ) : (
                    'Create'
                  )}
                </Button>
              ) : (
                <Button onClick={handleUpdate} disabled={formLoading}>
                  {formLoading ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin mr-1" />
                      Saving…
                    </>
                  ) : (
                    'Save'
                  )}
                </Button>
              )}
            </div>
          </div>
        )}
      </Modal>

      <ConfirmDialog
        isOpen={!!confirmActive}
        onClose={() => setConfirmActive(null)}
        onConfirm={handleConfirmActive}
        title={confirmActive?.isActive ? 'Activate user' : 'Deactivate user'}
        message={
          confirmActive
            ? 'Are you sure you want to ' +
              (confirmActive.isActive ? 'activate' : 'deactivate') +
              ' ' +
              (confirmActive.user.name || confirmActive.user.email) +
              '?'
            : ''
        }
        confirmText={confirmActive?.isActive ? 'Activate' : 'Deactivate'}
        variant="warning"
      />

      <ConfirmDialog
        isOpen={!!sessionToRevoke}
        onClose={() => setSessionToRevoke(null)}
        onConfirm={handleRevokeSession}
        title="Revoke session"
        message={sessionToRevoke && currentUser && sessionToRevoke.userId === currentUser.id
          ? 'This will log you out. Continue?'
          : 'Revoke this session? The user will need to sign in again.'}
        confirmText="Revoke"
        variant="warning"
        isLoading={revokingSession}
      />

      <ConfirmDialog
        isOpen={!!revokeAllConfirm}
        onClose={() => setRevokeAllConfirm(null)}
        onConfirm={handleRevokeAllSessions}
        title="Revoke all sessions"
        message={revokeAllConfirm && currentUser && revokeAllConfirm.id === currentUser.id
          ? 'This will log you out. Revoke all your sessions?'
          : `Revoke all sessions for ${revokeAllConfirm?.name ?? revokeAllConfirm?.email ?? 'this user'}? They will need to sign in again.`}
        confirmText="Revoke all"
        variant="warning"
        isLoading={revokingSession}
      />

      <Modal
        isOpen={!!roleModal}
        onClose={() => setRoleModal(null)}
        title="Change role"
        size="small"
      >
        {roleModal && (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              {roleModal.name || roleModal.email}
            </p>
            <div className="flex flex-wrap gap-2">
              {roles.map((r) => (
                <label key={r} className="flex items-center gap-1.5 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={roleModalSelected.includes(r)}
                    onChange={(e) =>
                      setRoleModalSelected((prev) =>
                        e.target.checked ? [...prev, r] : prev.filter((x) => x !== r)
                      )
                    }
                  />
                  <span className="text-sm">{r}</span>
                </label>
              ))}
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setRoleModal(null)}>
                Cancel
              </Button>
              <Button onClick={handleSaveRoles} disabled={roleModalSaving}>
                {roleModalSaving ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        isOpen={!!passwordModal}
        onClose={() => {
          setPasswordModal(null);
          setPasswordValue('');
          setPasswordForceMustChange(true);
        }}
        title="Reset password"
        size="small"
      >
        {passwordModal && (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Set a new temporary password for {passwordModal.name || passwordModal.email}.
            </p>
            <TextInput
              label="New password"
              type="password"
              value={passwordValue}
              onChange={(e) => setPasswordValue(e.target.value)}
              placeholder="Min 6 characters"
            />
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={passwordForceMustChange}
                onChange={(e) => setPasswordForceMustChange(e.target.checked)}
              />
              <span className="text-sm">Require password change on next login</span>
            </label>
            <div className="flex justify-end gap-2">
              <Button
                variant="outline"
                onClick={() => {
                  setPasswordModal(null);
                  setPasswordValue('');
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={handleResetPassword}
                disabled={passwordSaving || passwordValue.length < 6}
              >
                {passwordSaving ? 'Resetting...' : 'Reset password'}
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </PageShell>
  );
};

export default UserManagementPage;
