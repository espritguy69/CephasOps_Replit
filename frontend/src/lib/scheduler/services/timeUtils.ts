import type { CalendarSlot } from '../types';

export function parseTimeToMinutes(timeStr: string): number {
  const parts = timeStr.split(':').map(Number);
  return (parts[0] ?? 0) * 60 + (parts[1] ?? 0);
}

export function minutesToTime(minutes: number): string {
  const h = Math.floor(minutes / 60).toString().padStart(2, '0');
  const m = (minutes % 60).toString().padStart(2, '0');
  return `${h}:${m}:00`;
}

export function getSlotDuration(slot: CalendarSlot): number {
  const from = parseTimeToMinutes(slot.windowFrom || slot.startTime || '09:00');
  const to = parseTimeToMinutes(slot.windowTo || slot.endTime || '11:00');
  return Math.max(to - from, 0);
}

export function getInstallerDaySlots(
  allSlots: CalendarSlot[],
  installerId: string,
  date: string
): CalendarSlot[] {
  return allSlots.filter(
    (s) =>
      ((s.serviceInstallerId || s.siId) === installerId) &&
      s.date === date
  );
}

export function getInstallerAllSlots(
  allSlots: CalendarSlot[],
  installerId: string
): CalendarSlot[] {
  return allSlots.filter(
    (s) => (s.serviceInstallerId || s.siId) === installerId
  );
}
