import React, { useState } from 'react';
import { useDroppable } from '@dnd-kit/core';
import { Clock, MapPin, User, X, History, Phone, Mail, Briefcase, FileText } from 'lucide-react';
import { cn } from '../../lib/utils';
import { Button, Select } from '../ui';
import { ORDER_STATUSES } from '../../types/scheduler';
import { getStatusBadgeColor as getStatusBadgeColorUtil, getCardBackgroundColor as getCardBackgroundColorUtil } from '../../utils/statusColors';
import type { SchedulerOrder, OrderStatusValue } from '../../types/scheduler';

interface SchedulerOrderCardProps {
  order: SchedulerOrder;
  assignedInstaller: string | null;
  onAssign?: (orderId: string, installerId: string, installerName: string) => void;
  onUnassign?: (orderId: string) => void;
  onTimeChange?: (orderId: string) => void;
  onStatusChange?: (orderId: string, newStatus: string) => void;
  onHistoryClick?: (orderId: string) => void;
  bulkMode?: boolean;
  isSelected?: boolean;
  onToggleSelection?: (orderId: string) => void;
}

/**
 * Get card background color based on WO type
 * Uses centralized utility from statusColors.ts
 */
const getCardBackgroundColor = (order: SchedulerOrder): string => {
  return getCardBackgroundColorUtil(order.orderNumber, order.status);
};

/**
 * Get status badge color based on actual status
 * Uses centralized utility from statusColors.ts
 * Per SCHEDULER_MODULE.md: Grey=Pending, Blue=Assigned, Yellow=OnTheWay/MetCustomer, 
 * Green=OrderCompleted, Red=Blocked/Overdue
 */
const getStatusBadgeColor = (status: string): string => {
  return getStatusBadgeColorUtil(status);
};

/**
 * SchedulerOrderCard component
 * Displays order information with color coding, hover details, and status dropdown
 */
const SchedulerOrderCard: React.FC<SchedulerOrderCardProps> = ({
  order,
  assignedInstaller,
  onAssign,
  onUnassign,
  onTimeChange,
  onStatusChange,
  onHistoryClick,
  bulkMode,
  isSelected,
  onToggleSelection
}) => {
  const [isHovered, setIsHovered] = useState(false);
  
  // Droppable for installer drag-and-drop
  const { setNodeRef, isOver } = useDroppable({
    id: `order-${order.id}`,
    data: {
      type: 'order-card',
      orderId: order.id,
      order
    }
  });

  const isAssigned = !!assignedInstaller;
  const cardBgColor = getCardBackgroundColor(order);

  const handleStatusChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newStatus = e.target.value;
    if (onStatusChange && newStatus !== order.status) {
      onStatusChange(order.id, newStatus);
    }
  };

  return (
    <div
      ref={setNodeRef}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      className={cn(
        'p-3 rounded-lg border-2 transition-all relative',
        cardBgColor,
        isOver && 'ring-2 ring-primary ring-offset-2',
        isHovered && 'shadow-lg scale-105 z-10',
        isSelected && 'ring-2 ring-blue-500'
      )}
    >
      {/* Bulk mode checkbox */}
      {bulkMode && (
        <div className="absolute top-2 left-2 z-20">
          <input
            type="checkbox"
            checked={isSelected}
            onChange={() => onToggleSelection?.(order.id)}
            className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 cursor-pointer"
            onClick={(e) => e.stopPropagation()}
          />
        </div>
      )}

      {/* Header: Service Number + WO Number + Unassign button */}
      <div className="flex items-start justify-between gap-2 mb-2">
        <div className={cn('flex items-center gap-3 flex-1 min-w-0', bulkMode && 'ml-6')}>
          <div className="font-semibold text-xs truncate" title={order.serviceId || 'N/A'}>
            {order.serviceId || 'N/A'}
          </div>
          {order.orderNumber && order.orderNumber.trim() !== '' && (
            <div className="text-xs text-muted-foreground truncate" title={order.orderNumber}>
              {order.orderNumber}
            </div>
          )}
        </div>
        {isAssigned && onUnassign && (
          <Button
            variant="ghost"
            size="sm"
            className="h-6 w-6 p-0 hover:bg-red-100 hover:text-red-600 transition-colors flex-shrink-0"
            onClick={() => onUnassign(order.id)}
            title="Remove assignment"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      {/* Order details */}
      <div className="text-xs space-y-1">
        <div className="font-medium">{order.customerName || 'Unknown Customer'}</div>
        <div className="flex items-center gap-1 text-muted-foreground">
          <Clock className="h-3 w-3" />
          {order.appointmentTime || 'No time'}
        </div>
        <div className="flex items-center gap-1 text-muted-foreground">
          <MapPin className="h-3 w-3" />
          {order.buildingName || 'No building'}
        </div>

        {/* Status dropdown */}
        <div className="mt-1">
          <select
            value={order.status}
            onChange={handleStatusChange}
            className={cn(
              'h-6 text-xs w-full font-medium border rounded px-2 cursor-pointer',
              getStatusBadgeColor(order.status)
            )}
          >
            {ORDER_STATUSES.map((status) => (
              <option key={status.value} value={status.value}>
                {status.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Assigned installer badge */}
      {isAssigned && (
        <div className="mt-2 pt-2 border-t">
          <div className="flex items-center gap-1 text-xs font-semibold text-gray-900 bg-white/80 px-2 py-1 rounded">
            <User className="h-3 w-3" />
            {assignedInstaller}
          </div>
        </div>
      )}

      {/* Action buttons */}
      <div className="mt-2 flex gap-1">
        {onTimeChange && (
          <Button
            variant="outline"
            size="sm"
            className="h-6 text-xs flex-1"
            onClick={() => onTimeChange(order.id)}
          >
            Change Time
          </Button>
        )}
        {onHistoryClick && (
          <Button
            variant="outline"
            size="sm"
            className="h-6 text-xs px-2"
            onClick={() => onHistoryClick(order.id)}
            title="View History"
          >
            <History className="h-3 w-3" />
          </Button>
        )}
      </div>

      {/* Expanded details on hover */}
      {isHovered && (
        <div className="absolute left-0 right-0 top-full mt-1 p-3 bg-white border-2 border-primary rounded-lg shadow-xl z-20 text-xs space-y-2">
          <div className="font-semibold text-sm border-b pb-1 mb-2">Additional Details</div>
          
          {order.orderNumber && (
            <div className="flex items-center gap-2">
              <FileText className="h-3 w-3 text-muted-foreground" />
              <span className="font-medium">WO No.:</span> {order.orderNumber}
            </div>
          )}
          
          {order.ticketId && (
            <div className="flex items-center gap-2">
              <FileText className="h-3 w-3 text-muted-foreground" />
              <span className="font-medium">Ticket:</span> {order.ticketId}
            </div>
          )}
          
          {(order.address || order.fullAddress) && (
            <div>
              <div className="flex items-center gap-2">
                <MapPin className="h-3 w-3 text-muted-foreground" />
                <span className="font-medium">Full Address:</span>
              </div>
              <div className="text-muted-foreground mt-0.5 ml-5">{order.fullAddress || order.address}</div>
            </div>
          )}
          
          {order.customerPhone && (
            <div className="flex items-center gap-2">
              <Phone className="h-3 w-3 text-muted-foreground" />
              <span className="font-medium">Phone:</span> {order.customerPhone}
            </div>
          )}
          
          {order.customerEmail && (
            <div className="flex items-center gap-2">
              <Mail className="h-3 w-3 text-muted-foreground" />
              <span className="font-medium">Email:</span> {order.customerEmail}
            </div>
          )}
          
          {order.serviceType && (
            <div className="flex items-center gap-2">
              <Briefcase className="h-3 w-3 text-muted-foreground" />
              <span className="font-medium">Service Type:</span> {order.serviceType}
            </div>
          )}

          {(order.derivedPartnerCategoryLabel || order.partnerName) && (
            <div className="flex items-center gap-2">
              <User className="h-3 w-3 text-muted-foreground" />
              <span className="font-medium">Partner:</span> {order.derivedPartnerCategoryLabel || order.partnerName}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default SchedulerOrderCard;

