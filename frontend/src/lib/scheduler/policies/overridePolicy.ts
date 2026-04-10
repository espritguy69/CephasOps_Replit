import type { OverrideRecord } from '../types';
import { getSchedulerConfig, getActiveTenant } from '../config/schedulerConfig';

const _overrideStore = new Map<string, OverrideRecord[]>();

export type RuleType = 'hard' | 'soft' | 'override';

export interface SchedulerRule {
  id: string;
  name: string;
  type: RuleType;
  check: (...args: any[]) => { passed: boolean; reason: string };
}

function getTenantLog(tenantId: string): OverrideRecord[] {
  let log = _overrideStore.get(tenantId);
  if (!log) {
    log = [];
    _overrideStore.set(tenantId, log);
  }
  return log;
}

export function logOverride(
  userId: string,
  reason: string,
  originalValue?: unknown,
  overriddenValue?: unknown,
  tenantId?: string
): OverrideRecord {
  const resolvedTenant = tenantId || getActiveTenant() || '__system__';
  const record: OverrideRecord = {
    overridden: true,
    reason,
    userId,
    timestamp: new Date().toISOString(),
    originalValue,
    overriddenValue,
  };

  getTenantLog(resolvedTenant).push(record);
  return record;
}

export function getOverrideLog(tenantId?: string): readonly OverrideRecord[] {
  const resolvedTenant = tenantId || getActiveTenant() || '__system__';
  return _overrideStore.get(resolvedTenant) || [];
}

export function clearOverrideLog(tenantId?: string): void {
  const resolvedTenant = tenantId || getActiveTenant() || '__system__';
  _overrideStore.delete(resolvedTenant);
}

export function clearAllOverrideLogs(): void {
  _overrideStore.clear();
}

export function canOverride(): boolean {
  return getSchedulerConfig().overrideRulesEnabled;
}

export function validateOverride(
  userId: string,
  reason: string
): { valid: boolean; error?: string } {
  if (!canOverride()) {
    return { valid: false, error: 'Manual overrides are disabled for this tenant' };
  }

  if (!reason || reason.trim().length < 3) {
    return { valid: false, error: 'Override reason is required (min 3 characters)' };
  }

  if (!userId) {
    return { valid: false, error: 'User ID is required for override audit' };
  }

  return { valid: true };
}
