import React, { useState, useEffect, useCallback } from 'react';
import { Save, Loader2, Shield } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Button, Card, LoadingSpinner, useToast } from '../../components/ui';
import { getRoles, getRolePermissions, getPermissions, setRolePermissions } from '../../api/rbac';
import type { Role, Permission } from '../../types/rbac';

const MODULE_ORDER = [
  'Admin',
  'Payout',
  'Rates',
  'Payroll',
  'Orders',
  'Settings',
  'Inventory',
  'Buildings',
  'Billing',
  'P&L',
  'Reports',
  'Scheduler',
  'Assets',
  'Accounting',
  'Email',
  'Documents',
  'Files',
  'Workflow',
  'KPI',
];

function groupByModule(permissions: Permission[]): Record<string, Permission[]> {
  const byModule: Record<string, Permission[]> = {};
  for (const p of permissions) {
    const moduleName = p.name.split('.')[0] ?? 'Other';
    const label = moduleName.charAt(0).toUpperCase() + moduleName.slice(1);
    if (!byModule[label]) byModule[label] = [];
    byModule[label].push(p);
  }
  for (const mod of MODULE_ORDER) {
    if (!byModule[mod]) byModule[mod] = [];
  }
  return byModule;
}

const RolePermissionsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [roles, setRoles] = useState<Role[]>([]);
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const [rolePermissionNames, setRolePermissionNames] = useState<Set<string>>(new Set());
  const [dirty, setDirty] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingRole, setLoadingRole] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadRolesAndCatalog = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [rolesRes, permsRes] = await Promise.all([getRoles(), getPermissions()]);
      setRoles(Array.isArray(rolesRes) ? rolesRes : []);
      setPermissions(Array.isArray(permsRes) ? permsRes : []);
      if (!selectedRoleId && rolesRes?.length) {
        setSelectedRoleId(rolesRes[0].id);
      }
    } catch (e: any) {
      setError(e?.message ?? 'Failed to load roles and permissions');
      showError(e?.message ?? 'Failed to load');
    } finally {
      setLoading(false);
    }
  }, [selectedRoleId, showError]);

  useEffect(() => {
    loadRolesAndCatalog();
  }, []);

  const loadRolePermissions = useCallback(async (roleId: string) => {
    setLoadingRole(true);
    setError(null);
    try {
      const names = await getRolePermissions(roleId);
      setRolePermissionNames(new Set(names));
      setDirty(false);
    } catch (e: any) {
      setError(e?.message ?? 'Failed to load role permissions');
      showError(e?.message ?? 'Failed to load');
    } finally {
      setLoadingRole(false);
    }
  }, [showError]);

  useEffect(() => {
    if (selectedRoleId) loadRolePermissions(selectedRoleId);
    else setRolePermissionNames(new Set());
  }, [selectedRoleId, loadRolePermissions]);

  const togglePermission = (name: string) => {
    setRolePermissionNames((prev) => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name);
      else next.add(name);
      return next;
    });
    setDirty(true);
  };

  const handleSave = async () => {
    if (!selectedRoleId) return;
    setSaving(true);
    setError(null);
    try {
      await setRolePermissions(selectedRoleId, Array.from(rolePermissionNames));
      setDirty(false);
      showSuccess('Permissions saved.');
    } catch (e: any) {
      setError(e?.message ?? 'Failed to save');
      showError(e?.message ?? 'Failed to save');
    } finally {
      setSaving(false);
    }
  };

  const byModule = groupByModule(permissions);
  const selectedRole = roles.find((r) => r.id === selectedRoleId);

  if (loading) {
    return (
      <PageShell title="Role permissions" icon={<Shield />}>
        <div className="flex items-center justify-center py-12">
          <LoadingSpinner />
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell title="Role permissions" icon={<Shield />}>
      {error && (
        <div className="mb-4 rounded-md bg-red-50 dark:bg-red-900/20 px-4 py-2 text-sm text-red-700 dark:text-red-300">
          {error}
        </div>
      )}
      <p className="mb-4 text-sm text-muted-foreground">
        Permissions define what a role can do. Department memberships (in User Management) define which departments a user can access for department-scoped pages.
      </p>
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-4">
        <Card className="p-4 lg:col-span-1">
          <h3 className="font-medium mb-2">Roles</h3>
          <ul className="space-y-1">
            {roles.map((r) => (
              <li key={r.id}>
                <button
                  type="button"
                  onClick={() => setSelectedRoleId(r.id)}
                  className={`w-full text-left px-3 py-2 rounded-md text-sm ${
                    selectedRoleId === r.id
                      ? 'bg-primary text-primary-foreground'
                      : 'hover:bg-muted'
                  }`}
                >
                  {r.name}
                  {r.scope ? ` (${r.scope})` : ''}
                </button>
              </li>
            ))}
          </ul>
        </Card>
        <div className="lg:col-span-3 space-y-4">
          {selectedRoleId && (
            <>
              <div className="flex items-center justify-between">
                <p className="text-sm text-muted-foreground">
                  Permissions for <strong>{selectedRole?.name}</strong>
                </p>
                {dirty && (
                  <Button onClick={handleSave} disabled={saving}>
                    {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                    <span className="ml-2">Save</span>
                  </Button>
                )}
              </div>
              {loadingRole ? (
                <div className="flex justify-center py-8">
                  <LoadingSpinner />
                </div>
              ) : (
                <Card className="p-4">
                  <div className="space-y-6 max-h-[70vh] overflow-y-auto">
                    {MODULE_ORDER.filter((m) => byModule[m]?.length).map((moduleName) => (
                      <div key={moduleName}>
                        <h4 className="font-medium text-sm text-muted-foreground mb-2">{moduleName}</h4>
                        <div className="flex flex-wrap gap-3">
                          {(byModule[moduleName] ?? []).map((p) => (
                            <label
                              key={p.id}
                              className="flex items-center gap-2 cursor-pointer text-sm"
                            >
                              <input
                                type="checkbox"
                                checked={rolePermissionNames.has(p.name)}
                                onChange={() => togglePermission(p.name)}
                                className="rounded border-input"
                              />
                              <span>{p.name}</span>
                            </label>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </Card>
              )}
            </>
          )}
        </div>
      </div>
    </PageShell>
  );
};

export default RolePermissionsPage;
