import React from 'react';
import { SCHEDULER_START_HOUR, SCHEDULER_END_HOUR, SCHEDULER_HOUR_HEIGHT } from './schedulerConstants';
import type { SIAvailability } from '../../types/scheduler';
import type { LeaveRequest } from '../../types/scheduler';
import { cn } from '../../lib/utils';

function parseTimeToMinutes(timeStr: string): number {
  const parts = timeStr.split(':').map(Number);
  const h = parts[0] ?? 0;
  const m = parts[1] ?? 0;
  return h * 60 + m;
}

/** Block type for rendering */
interface Block {
  startMinutes: number;
  endMinutes: number;
  label?: string;
  isLeave?: boolean;
}

export interface SchedulerAvailabilityLayerProps {
  /** Date for the column (YYYY-MM-DD) */
  date: string;
  /** Availability records where isAvailable === false = blocked */
  availability: SIAvailability[];
  /** Leave requests (Approved) = blocked */
  leaveRequests: LeaveRequest[];
  startHour?: number;
  endHour?: number;
  hourHeight?: number;
  className?: string;
}

/**
 * Renders blocked (unavailable / leave) segments in an installer column.
 */
const SchedulerAvailabilityLayer: React.FC<SchedulerAvailabilityLayerProps> = ({
  date,
  availability,
  leaveRequests,
  startHour = SCHEDULER_START_HOUR,
  endHour = SCHEDULER_END_HOUR,
  hourHeight = SCHEDULER_HOUR_HEIGHT,
  className,
}) => {
  const dayStartMinutes = startHour * 60;
  const dayEndMinutes = endHour * 60;
  const totalHeight = (endHour - startHour) * hourHeight;

  const blocks: Block[] = [];

  availability.forEach((a) => {
    if (a.date !== date) return;
    const startM = parseTimeToMinutes(a.startTime);
    const endM = parseTimeToMinutes(a.endTime);
    if (!a.isAvailable) {
      blocks.push({
        startMinutes: Math.max(startM, dayStartMinutes),
        endMinutes: Math.min(endM, dayEndMinutes),
        label: a.notes,
      });
    }
  });

  const dateObj = new Date(date + 'T00:00:00');
  leaveRequests.forEach((lr) => {
    if (lr.status !== 'Approved') return;
    const from = new Date(lr.startDate + 'T00:00:00');
    const to = new Date(lr.endDate + 'T23:59:59');
    if (dateObj < from || dateObj > to) return;
    blocks.push({
      startMinutes: dayStartMinutes,
      endMinutes: dayEndMinutes,
      label: lr.notes || 'Leave',
      isLeave: true,
    });
  });

  const toTop = (minutes: number) => {
    const offset = (minutes - dayStartMinutes) / (dayEndMinutes - dayStartMinutes);
    return Math.max(0, Math.min(1, offset)) * 100;
  };
  const toHeight = (startM: number, endM: number) => {
    const span = (Math.min(endM, dayEndMinutes) - Math.max(startM, dayStartMinutes)) / (dayEndMinutes - dayStartMinutes);
    return Math.max(0, Math.min(1, span)) * 100;
  };

  return (
    <div
      className={cn('absolute inset-0 pointer-events-none flex flex-col', className)}
      style={{ height: totalHeight }}
      aria-hidden
    >
      {blocks.map((b, i) => (
        <div
          key={i}
          className={cn(
            'absolute left-0 right-0 bg-muted/60 dark:bg-muted/40 border-y border-border/50',
            b.isLeave && 'bg-amber-100/80 dark:bg-amber-900/30'
          )}
          style={{
            top: `${toTop(b.startMinutes)}%`,
            height: `${toHeight(b.startMinutes, b.endMinutes)}%`,
          }}
          title={b.label}
        />
      ))}
    </div>
  );
};

export default SchedulerAvailabilityLayer;
