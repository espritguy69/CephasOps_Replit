import React, { useMemo } from 'react';
import { useDroppable } from '@dnd-kit/core';
import TimeAxis from './TimeAxis';
import InstallerColumnHeader from './InstallerColumnHeader';
import SchedulerAppointmentCard from './SchedulerAppointmentCard';
import SchedulerAvailabilityLayer from './SchedulerAvailabilityLayer';
import {
  SCHEDULER_START_HOUR,
  SCHEDULER_END_HOUR,
  SCHEDULER_HOUR_HEIGHT,
} from './schedulerConstants';
import type { CalendarSlot } from '../../types/scheduler';
import type { SIAvailability, LeaveRequest } from '../../types/scheduler';
import type { ServiceInstaller } from '../../types/serviceInstallers';
import { cn } from '../../lib/utils';
import type { InstallerWorkload } from './QuickAssignPanel';

function parseTimeToMinutes(timeStr: string): number {
  const parts = timeStr.split(':').map(Number);
  return (parts[0] ?? 0) * 60 + (parts[1] ?? 0);
}

function formatTimeFromMinutes(minutes: number): string {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:00`;
}

export interface SchedulerGridProps {
  slots: CalendarSlot[];
  installers: ServiceInstaller[];
  date: string;
  availabilityBySi: Record<string, SIAvailability[]>;
  leaveBySi: Record<string, LeaveRequest[]>;
  onSlotClick: (slot: CalendarSlot) => void;
  onDropSlot?: (slot: CalendarSlot, target: { installerId: string; date: string; windowFrom: string; windowTo: string }) => void;
  onDropUnassigned?: (orderId: string, target: { installerId: string; date: string; windowFrom: string; windowTo: string }) => void;
  columnWidth?: number;
  startHour?: number;
  endHour?: number;
  hourHeight?: number;
  workloadMap?: Record<string, InstallerWorkload>;
  className?: string;
}

const SchedulerGrid: React.FC<SchedulerGridProps> = ({
  slots,
  installers,
  date,
  availabilityBySi,
  leaveBySi,
  onSlotClick,
  onDropSlot,
  onDropUnassigned,
  columnWidth = 200,
  startHour = SCHEDULER_START_HOUR,
  endHour = SCHEDULER_END_HOUR,
  hourHeight = SCHEDULER_HOUR_HEIGHT,
  workloadMap,
  className,
}) => {
  const dayStartMinutes = startHour * 60;
  const dayEndMinutes = endHour * 60;
  const totalMinutes = dayEndMinutes - dayStartMinutes;
  const totalHeight = totalMinutes * (hourHeight / 60);

  const slotsByInstaller = useMemo(() => {
    const map: Record<string, CalendarSlot[]> = {};
    installers.forEach((i) => (map[i.id] = []));
    slots.forEach((s) => {
      const key = s.serviceInstallerId || s.siId || '';
      if (map[key]) map[key].push(s);
    });
    return map;
  }, [slots, installers]);

  return (
    <div className={cn('flex flex-col min-h-0', className)}>
      <div className="flex shrink-0" style={{ height: 40 }}>
        <div className="w-[52px] shrink-0 border-r border-b bg-muted/50" aria-hidden />
        <div className="flex flex-1 overflow-x-auto border-b bg-muted/30">
          {installers.map((inst) => (
            <div key={inst.id} className="shrink-0" style={{ width: columnWidth }}>
              <InstallerColumnHeader
                installer={inst}
                jobCount={slotsByInstaller[inst.id]?.length ?? 0}
                availabilitySummary={undefined}
                workloadLevel={workloadMap?.[inst.id]?.level}
              />
            </div>
          ))}
        </div>
      </div>
      <div className="flex flex-1 min-h-0 overflow-auto">
        <TimeAxis
          startHour={startHour}
          endHour={endHour}
          hourHeight={hourHeight}
          noTopSpacer
          className="shrink-0"
        />
        <div className="flex flex-1 overflow-x-auto">
          {installers.map((inst) => (
            <InstallerColumn
              key={inst.id}
              installer={inst}
              date={date}
              slots={slotsByInstaller[inst.id] ?? []}
              availability={availabilityBySi[inst.id] ?? []}
              leaveRequests={leaveBySi[inst.id] ?? []}
              dayStartMinutes={dayStartMinutes}
              dayEndMinutes={dayEndMinutes}
              totalHeight={totalHeight}
              hourHeight={hourHeight}
              columnWidth={columnWidth}
              onSlotClick={onSlotClick}
              onDropSlot={onDropSlot}
              onDropUnassigned={onDropUnassigned}
            />
          ))}
        </div>
      </div>
    </div>
  );
};

interface InstallerColumnProps {
  installer: ServiceInstaller;
  date: string;
  slots: CalendarSlot[];
  availability: SIAvailability[];
  leaveRequests: LeaveRequest[];
  dayStartMinutes: number;
  dayEndMinutes: number;
  totalHeight: number;
  hourHeight: number;
  columnWidth: number;
  onSlotClick: (slot: CalendarSlot) => void;
  onDropSlot?: (slot: CalendarSlot, target: { installerId: string; date: string; windowFrom: string; windowTo: string }) => void;
  onDropUnassigned?: (orderId: string, target: { installerId: string; date: string; windowFrom: string; windowTo: string }) => void;
}

const InstallerColumn: React.FC<InstallerColumnProps> = ({
  installer,
  date,
  slots,
  availability,
  leaveRequests,
  dayStartMinutes,
  dayEndMinutes,
  totalHeight,
  hourHeight,
  columnWidth,
  onSlotClick,
  onDropSlot,
  onDropUnassigned,
}) => {
  const slotsWithPosition = useMemo(() => {
    return slots.map((slot) => {
      const fromM = parseTimeToMinutes(slot.windowFrom || slot.startTime || '00:00');
      const toM = parseTimeToMinutes(slot.windowTo || slot.endTime || '00:00');
      const start = Math.max(fromM, dayStartMinutes);
      const end = Math.min(toM, dayEndMinutes);
      const topPct = ((start - dayStartMinutes) / (dayEndMinutes - dayStartMinutes)) * 100;
      const heightPct = ((end - start) / (dayEndMinutes - dayStartMinutes)) * 100;
      return { slot, topPct, heightPct };
    });
  }, [slots, dayStartMinutes, dayEndMinutes]);

  return (
    <div
      className="relative shrink-0 border-r bg-background"
      style={{ width: columnWidth, minHeight: totalHeight }}
    >
      <SchedulerAvailabilityLayer
        date={date}
        availability={availability}
        leaveRequests={leaveRequests}
        startHour={dayStartMinutes / 60}
        endHour={dayEndMinutes / 60}
        hourHeight={hourHeight}
        className="rounded-none"
      />
      <div className="relative" style={{ height: totalHeight }}>
        {slotsWithPosition.map(({ slot, topPct, heightPct }) => (
          <div
            key={slot.id}
            className="absolute left-1 right-1 z-10"
            style={{
              top: `${topPct}%`,
              height: `${heightPct}%`,
              minHeight: 24,
            }}
          >
            <SchedulerAppointmentCard
              slot={slot}
              draggable={!!onDropSlot}
              onClick={() => onSlotClick(slot)}
              showBlockerBadge={(slot.orderStatus || '').toLowerCase().includes('blocker')}
              showRescheduleBadge={(slot.status || '').toLowerCase().includes('reschedule')}
              className="h-full min-h-[48px]"
            />
          </div>
        ))}
        {/* Droppable time cells: one per hour */}
        {Array.from({ length: (dayEndMinutes - dayStartMinutes) / 60 }, (_, i) => {
          const slotStart = dayStartMinutes + i * 60;
          const slotEnd = slotStart + 60;
          const cellId = `cell-${installer.id}-${date}-${slotStart}`;
          const topPct = (i * 60 / (dayEndMinutes - dayStartMinutes)) * 100;
          const heightPct = (60 / (dayEndMinutes - dayStartMinutes)) * 100;
          return (
            <DroppableCell
              key={cellId}
              id={cellId}
              installerId={installer.id}
              date={date}
              windowFrom={formatTimeFromMinutes(slotStart)}
              windowTo={formatTimeFromMinutes(slotEnd)}
              style={{ top: `${topPct}%`, height: `${heightPct}%` }}
              onDropSlot={onDropSlot}
              onDropUnassigned={onDropUnassigned}
            />
          );
        })}
      </div>
    </div>
  );
};

interface DroppableCellProps {
  id: string;
  installerId: string;
  date: string;
  windowFrom: string;
  windowTo: string;
  style: React.CSSProperties;
  onDropSlot?: (slot: CalendarSlot, target: { installerId: string; date: string; windowFrom: string; windowTo: string }) => void;
  onDropUnassigned?: (orderId: string, target: { installerId: string; date: string; windowFrom: string; windowTo: string }) => void;
}

function DroppableCell({ id, installerId, date, windowFrom, windowTo, style, onDropSlot, onDropUnassigned }: DroppableCellProps) {
  const { setNodeRef, isOver } = useDroppable({
    id,
    data: {
      type: 'scheduler-cell',
      installerId,
      date,
      windowFrom,
      windowTo,
    },
  });
  return (
    <div
      ref={setNodeRef}
      className={cn(
        'absolute left-0 right-0 border-b border-dashed border-transparent transition-colors duration-150',
        isOver && 'bg-primary/15 border-primary/40 shadow-inner'
      )}
      style={style}
      aria-hidden
    />
  );
}

export default SchedulerGrid;
