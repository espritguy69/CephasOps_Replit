/**
 * RBAC v3 — Field-level permission checks for UI visibility.
 * Use to hide columns/fields when the user lacks the corresponding permission.
 * Backend still masks values; this keeps the UI aligned (e.g. hide column instead of showing "—").
 */

import type { User } from '../types/auth';

const FIELD_PERMISSIONS = {
  ordersViewPrice: 'orders.view.price',
  payrollViewPayout: 'payroll.view.payout',
  payrollEditPayout: 'payroll.edit.payout',
  ratesViewAmounts: 'rates.view.amounts',
  inventoryViewCost: 'inventory.view.cost',
  inventoryEditCost: 'inventory.edit.cost',
  reportsViewFinancial: 'reports.view.financial',
} as const;

function hasPermission(user: User | null | undefined, permission: string): boolean {
  if (!user) return false;
  if (user.roles?.includes('SuperAdmin')) return true;
  return Boolean(user.permissions?.includes(permission));
}

export function canViewOrderPrice(user: User | null | undefined): boolean {
  return hasPermission(user, FIELD_PERMISSIONS.ordersViewPrice);
}

export function canViewPayrollPayout(user: User | null | undefined): boolean {
  return hasPermission(user, FIELD_PERMISSIONS.payrollViewPayout);
}

export function canEditPayrollPayout(user: User | null | undefined): boolean {
  return hasPermission(user, FIELD_PERMISSIONS.payrollEditPayout);
}

export function canViewRatesAmounts(user: User | null | undefined): boolean {
  return hasPermission(user, FIELD_PERMISSIONS.ratesViewAmounts);
}

export function canViewInventoryCost(user: User | null | undefined): boolean {
  return hasPermission(user, FIELD_PERMISSIONS.inventoryViewCost);
}

export function canEditInventoryCost(user: User | null | undefined): boolean {
  return hasPermission(user, FIELD_PERMISSIONS.inventoryEditCost);
}

export function canViewReportsFinancial(user: User | null | undefined): boolean {
  return hasPermission(user, FIELD_PERMISSIONS.reportsViewFinancial);
}
