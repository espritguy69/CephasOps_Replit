import React from 'react';
import { useDraggable } from '@dnd-kit/core';
import { cn } from '../../lib/utils';
import type { CalendarSlot } from '../../types/scheduler';

interface OrderCardProps {
  slot: CalendarSlot;
  onClick?: () => void;
  className?: string;
  draggable?: boolean;
}

/**
 * OrderCard component for scheduler grid
 * Displays order information in a compact card format with status colors
 */
const OrderCard: React.FC<OrderCardProps> = ({ slot, onClick, className, draggable = false }) => {
  const { attributes, listeners, setNodeRef, transform, isDragging } = draggable
    ? useDraggable({
        id: `slot-${slot.id}`,
        data: {
          type: 'scheduled-slot',
          slot
        }
      })
    : { attributes: {}, listeners: {}, setNodeRef: null, transform: null, isDragging: false };

  const style = transform
    ? {
        transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
        opacity: isDragging ? 0.5 : 1
      }
    : { opacity: isDragging ? 0.5 : 1 };
  const getStatusColor = (status?: string): string => {
    const statusLower = status?.toLowerCase() || '';
    if (statusLower.includes('pending')) return 'bg-gray-100 border-gray-300 text-gray-800';
    if (statusLower.includes('assigned')) return 'bg-blue-100 border-blue-300 text-blue-800';
    if (statusLower.includes('ontheway') || statusLower.includes('metcustomer')) return 'bg-yellow-100 border-yellow-300 text-yellow-800';
    if (statusLower.includes('completed')) return 'bg-green-100 border-green-300 text-green-800';
    if (statusLower.includes('blocked') || statusLower.includes('blocker')) return 'bg-red-100 border-red-300 text-red-800';
    return 'bg-gray-100 border-gray-300 text-gray-800';
  };

  const formatTime = (timeSpan?: string): string => {
    if (!timeSpan) return '';
    // Handle both "HH:mm:ss" and "HH:mm" formats
    const parts = timeSpan.split(':');
    if (parts.length >= 2) {
      return `${parts[0]}:${parts[1]}`;
    }
    return timeSpan;
  };

  const getTimeRange = (): string => {
    const from = formatTime(slot.windowFrom || slot.startTime);
    const to = formatTime(slot.windowTo || slot.endTime);
    if (from && to) {
      return `${from} - ${to}`;
    }
    return '';
  };

  const statusColor = getStatusColor(slot.status || slot.orderStatus);

  const cardContent = (
    <div
      onClick={onClick}
      className={cn(
        'p-1.5 mb-1 text-xs cursor-pointer hover:shadow-md transition-shadow border rounded',
        statusColor,
        className,
        draggable && 'cursor-move'
      )}
      title={`${slot.serviceId || 'Order'} - ${slot.customerName || 'N/A'}`}
      {...(draggable ? { ...attributes, ...listeners } : {})}
    >
      {/* Service ID + Ticket ID */}
      <div className="font-medium truncate">{slot.serviceId || slot.externalRef || 'N/A'}</div>
      {slot.ticketId && (
        <div className="text-xs opacity-75 truncate">{slot.ticketId}</div>
      )}
      
      {/* Customer name */}
      <div className="text-xs mt-0.5 truncate font-semibold">{slot.customerName || 'N/A'}</div>
      
      {/* Time range */}
      {getTimeRange() && (
        <div className="text-xs opacity-75 truncate mt-0.5">{getTimeRange()}</div>
      )}
      
      {/* Building or Partner */}
      <div className="text-xs opacity-75 truncate mt-0.5">
        {slot.buildingName || slot.derivedPartnerCategoryLabel || slot.partnerName || ''}
      </div>
      
      {/* Partner badge (if available) */}
      {(slot.derivedPartnerCategoryLabel || slot.partnerName) && (
        <div className="mt-0.5">
          <span className="inline-block px-1 py-0.5 text-xs rounded bg-white/50 border border-current/20">
            {slot.derivedPartnerCategoryLabel || slot.partnerName}
          </span>
        </div>
      )}
    </div>
  );

  if (draggable && setNodeRef) {
    return (
      <div ref={setNodeRef} style={style}>
        {cardContent}
      </div>
    );
  }

  return cardContent;
};

export default OrderCard;

