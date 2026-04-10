import type { CalendarSlot, ConflictCheckResult } from '../types';
import { parseTimeToMinutes, getInstallerDaySlots } from './timeUtils';
import { getSchedulerConfig } from '../config/schedulerConfig';

export function hasTimeOverlap(
  existingSlots: CalendarSlot[],
  windowFrom: string,
  windowTo: string
): CalendarSlot | null {
  const newFrom = parseTimeToMinutes(windowFrom);
  const newTo = parseTimeToMinutes(windowTo);

  for (const slot of existingSlots) {
    const slotFrom = parseTimeToMinutes(slot.windowFrom || slot.startTime || '00:00');
    const slotTo = parseTimeToMinutes(slot.windowTo || slot.endTime || '00:00');
    if (newFrom < slotTo && newTo > slotFrom) {
      return slot;
    }
  }
  return null;
}

export function checkConflict(
  installerId: string,
  date: string,
  windowFrom: string,
  windowTo: string,
  allSlots: CalendarSlot[]
): ConflictCheckResult {
  const installerDaySlots = getInstallerDaySlots(allSlots, installerId, date);

  const conflict = hasTimeOverlap(installerDaySlots, windowFrom, windowTo);
  if (conflict) {
    return {
      hasConflict: true,
      conflictSlot: conflict,
      message: `Time conflict: ${conflict.customerName || conflict.serviceId || 'existing job'} (${conflict.windowFrom?.slice(0, 5)}–${conflict.windowTo?.slice(0, 5)})`,
    };
  }

  return { hasConflict: false };
}

export function isWithinWorkingHours(windowFrom: string, windowTo: string): boolean {
  const cfg = getSchedulerConfig();
  const from = parseTimeToMinutes(windowFrom);
  const to = parseTimeToMinutes(windowTo);
  const start = parseTimeToMinutes(cfg.workingHours.start);
  const end = parseTimeToMinutes(cfg.workingHours.end);
  return from >= start && to <= end;
}

export function hasExceededDailyLimit(
  installerId: string,
  date: string,
  allSlots: CalendarSlot[]
): boolean {
  const cfg = getSchedulerConfig();
  const daySlots = getInstallerDaySlots(allSlots, installerId, date);
  return daySlots.length >= cfg.maxJobsPerInstallerPerDay;
}
