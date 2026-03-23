import { useAuth } from '../contexts/AuthContext';

export interface PermissionCheck {
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  hasRole: (role: string) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
  isAdmin: boolean;
  isSuperAdmin: boolean;
  isFinance: boolean;
  isOperations: boolean;
  isWarehouse: boolean;
  userRoles: string[];
  userPermissions: string[];
}

const FINANCE_PERMISSIONS = ['billing.view', 'payroll.view', 'pnl.view', 'accounting.view'];
const OPERATIONS_PERMISSIONS = ['orders.view', 'scheduler.view'];
const WAREHOUSE_PERMISSIONS = ['inventory.view'];

const ELEVATED_ROLES = ['Director', 'HeadOfDepartment', 'Supervisor'];

const ELEVATED_ROLE_PERMISSIONS = [
  'orders.view', 'orders.edit', 'scheduler.view',
  'inventory.view', 'billing.view', 'pnl.view', 'payroll.view',
  'assets.view', 'buildings.view', 'documents.view',
  'files.view', 'email.view', 'workflow.view',
  'kpi.view', 'reports.view', 'accounting.view', 'settings.view',
];

const MEMBER_PERMISSIONS = [
  'orders.view', 'scheduler.view', 'inventory.view',
  'documents.view', 'files.view',
];

export function usePermissions(): PermissionCheck {
  const { user } = useAuth();

  const userRoles = user?.roles || [];
  const userPermissions = user?.permissions || [];
  const isSuperAdmin = userRoles.includes('SuperAdmin');
  const isAdmin = isSuperAdmin || userRoles.includes('Admin');

  const hasPermission = (permission: string): boolean => {
    if (!user) return false;
    if (isSuperAdmin) return true;

    if (Array.isArray(userPermissions) && userPermissions.length > 0) {
      return userPermissions.includes(permission);
    }

    if (isAdmin) return true;

    if (permission === 'admin.view' || permission === 'admin.tenants.view') return false;
    if (permission === 'admin.security.view' || permission === 'admin.roles.view') return false;
    if (permission === 'jobs.view' || permission === 'jobs.admin') return false;

    const hasElevatedRole = userRoles.some(r => ELEVATED_ROLES.includes(r));
    if (hasElevatedRole) {
      return ELEVATED_ROLE_PERMISSIONS.includes(permission);
    }

    if (userRoles.length > 0) {
      return MEMBER_PERMISSIONS.includes(permission);
    }

    return false;
  };

  const hasAnyPermission = (permissions: string[]): boolean => {
    return permissions.some(p => hasPermission(p));
  };

  const hasRole = (role: string): boolean => userRoles.includes(role);

  const hasAnyRole = (roles: string[]): boolean => roles.some(r => userRoles.includes(r));

  const isFinance = isSuperAdmin || isAdmin || hasAnyPermission(FINANCE_PERMISSIONS);
  const isOperations = isSuperAdmin || isAdmin || hasAnyPermission(OPERATIONS_PERMISSIONS);
  const isWarehouse = isSuperAdmin || isAdmin || hasAnyPermission(WAREHOUSE_PERMISSIONS);

  return {
    hasPermission,
    hasAnyPermission,
    hasRole,
    hasAnyRole,
    isAdmin,
    isSuperAdmin,
    isFinance,
    isOperations,
    isWarehouse,
    userRoles,
    userPermissions,
  };
}
