import type { OverrideRecord } from '../types';
import { getSchedulerConfig } from '../config/schedulerConfig';

const _overrideLog: OverrideRecord[] = [];

export type RuleType = 'hard' | 'soft' | 'override';

export interface SchedulerRule {
  id: string;
  name: string;
  type: RuleType;
  check: (...args: any[]) => { passed: boolean; reason: string };
}

export function logOverride(
  userId: string,
  reason: string,
  originalValue?: unknown,
  overriddenValue?: unknown
): OverrideRecord {
  const record: OverrideRecord = {
    overridden: true,
    reason,
    userId,
    timestamp: new Date().toISOString(),
    originalValue,
    overriddenValue,
  };

  _overrideLog.push(record);
  return record;
}

export function getOverrideLog(): readonly OverrideRecord[] {
  return _overrideLog;
}

export function clearOverrideLog(): void {
  _overrideLog.length = 0;
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
