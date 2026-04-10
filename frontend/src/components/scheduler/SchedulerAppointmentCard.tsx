import React from 'react';
import { useDraggable } from '@dnd-kit/core';
import { AlertTriangle, CalendarClock, MapPin } from 'lucide-react';
import { getSchedulerCardStatusClass } from './schedulerConstants';
import type { CalendarSlot } from '../../types/scheduler';
import { cn } from '../../lib/utils';

export interface SchedulerAppointmentCardProps {
  slot: CalendarSlot;
  onClick?: () => void;
  className?: string;
  draggable?: boolean;
  showBlockerBadge?: boolean;
  showRescheduleBadge?: boolean;
  showSlaRiskBadge?: boolean;
}

function formatTime(timeSpan?: string): string {
  if (!timeSpan) return '';
  const parts = timeSpan.split(':');
  if (parts.length >= 2) {
    const h = parseInt(parts[0], 10);
    const m = parts[1];
    const am = h < 12 ? 'AM' : 'PM';
    const hour = h === 0 ? 12 : h > 12 ? h - 12 : h;
    return `${hour}:${m} ${am}`;
  }
  return timeSpan;
}

/**
 * Compact appointment card for the custom scheduler grid.
 * Shows customer, order type, time, building, status; supports blocker/reschedule/SLA badges.
 */
const SchedulerAppointmentCard: React.FC<SchedulerAppointmentCardProps> = ({
  slot,
  onClick,
  className,
  draggable = true,
  showBlockerBadge = false,
  showRescheduleBadge = false,
  showSlaRiskBadge = false,
}) => {
  const isBlocker = (slot.orderStatus || '').toLowerCase().includes('blocker');
  const isReschedule = (slot.status || '').toLowerCase().includes('reschedule');
  const isSlaRisk = showSlaRiskBadge;

  const { attributes, listeners, setNodeRef, transform, isDragging } = draggable
    ? useDraggable({
        id: `slot-${slot.id}`,
        data: { type: 'scheduled-slot', slot },
      })
    : {
        attributes: {},
        listeners: {},
        setNodeRef: null,
        transform: null,
        isDragging: false,
      };

  const style = transform
    ? { transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`, opacity: isDragging ? 0.6 : 1 }
    : { opacity: isDragging ? 0.6 : 1 };

  const statusClass = getSchedulerCardStatusClass(slot.orderStatus, slot.status);
  const from = formatTime(slot.windowFrom || slot.startTime);
  const to = formatTime(slot.windowTo || slot.endTime);
  const timeRange = from && to ? `${from} – ${to}` : '';

  const card = (
    <div
      onClick={(e) => {
        e.stopPropagation();
        onClick?.();
      }}
      className={cn(
        'rounded-lg border p-2 text-left shadow-sm transition-all duration-150 hover:shadow-md hover:-translate-y-px cursor-pointer',
        statusClass,
        draggable && 'cursor-grab active:cursor-grabbing',
        className
      )}
      title={`${slot.customerName || slot.serviceId || 'Order'} – ${timeRange}`}
      {...(draggable ? { ...attributes, ...listeners } : {})}
    >
      <div className="flex items-start gap-1">
        <div className="min-w-0 flex-1">
          <div className="font-medium truncate text-sm">{slot.customerName || slot.serviceId || '—'}</div>
          <div className="text-xs text-muted-foreground truncate">
            {slot.serviceType || slot.derivedPartnerCategoryLabel || slot.partnerName || 'Order'}
          </div>
          {timeRange && (
            <div className="flex items-center gap-1 text-xs text-muted-foreground mt-0.5">
              <CalendarClock className="h-3 w-3 shrink-0" />
              {timeRange}
            </div>
          )}
          {(slot.buildingName || slot.address) && (
            <div className="flex items-center gap-1 text-xs text-muted-foreground truncate mt-0.5">
              <MapPin className="h-3 w-3 shrink-0" />
              {slot.buildingName || slot.address}
            </div>
          )}
        </div>
        <div className="flex flex-col gap-0.5 shrink-0">
          {(isBlocker || showBlockerBadge) && (
            <span className="inline-flex items-center gap-0.5 rounded px-1 py-0.5 text-[10px] font-medium bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-200">
              <AlertTriangle className="h-2.5 w-2.5" /> Blocker
            </span>
          )}
          {(isReschedule || showRescheduleBadge) && (
            <span className="inline-flex rounded px-1 py-0.5 text-[10px] font-medium bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-200">
              Reschedule
            </span>
          )}
          {isSlaRisk && (
            <span className="inline-flex items-center gap-0.5 rounded px-1 py-0.5 text-[10px] font-medium bg-orange-100 text-orange-800 dark:bg-orange-900/50 dark:text-orange-200">
              <AlertTriangle className="h-2.5 w-2.5" /> At risk
            </span>
          )}
        </div>
      </div>
    </div>
  );

  if (draggable && setNodeRef) {
    return (
      <div ref={setNodeRef} style={style}>
        {card}
      </div>
    );
  }
  return card;
};

export default SchedulerAppointmentCard;
