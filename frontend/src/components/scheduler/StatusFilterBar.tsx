import React from 'react';
import { Button } from '../ui';
import { cn } from '../../lib/utils';
import { ORDER_STATUSES } from '../../types/scheduler';

interface StatusFilterBarProps {
  statusFilter: string | null;
  onStatusFilterChange: (status: string | null) => void;
  showAllUnassigned: boolean;
  onViewChange: (showAllUnassigned: boolean) => void;
}

/**
 * Get button style based on status (matching status badge colors)
 */
const getStatusButtonStyle = (status: string, isActive: boolean): string => {
  if (isActive) {
    // When active, use solid background
    switch (status) {
      case 'pending':
        return 'bg-gray-600 text-white border-gray-700 hover:bg-gray-700';
      case 'assigned':
        return 'bg-blue-600 text-white border-blue-700 hover:bg-blue-700';
      case 'on_the_way':
        return 'bg-amber-500 text-white border-amber-600 hover:bg-amber-600';
      case 'met_customer':
        return 'bg-emerald-500 text-white border-emerald-600 hover:bg-emerald-600';
      case 'order_completed':
        return 'bg-lime-500 text-white border-lime-600 hover:bg-lime-600';
      case 'docket_received':
        return 'bg-teal-500 text-white border-teal-600 hover:bg-teal-600';
      case 'docket_uploaded':
        return 'bg-cyan-500 text-white border-cyan-600 hover:bg-cyan-600';
      case 'ready_to_invoice':
        return 'bg-indigo-500 text-white border-indigo-600 hover:bg-indigo-600';
      case 'invoiced':
        return 'bg-violet-500 text-white border-violet-600 hover:bg-violet-600';
      case 'completed':
        return 'bg-green-600 text-white border-green-700 hover:bg-green-700';
      case 'customer_issue':
        return 'bg-orange-500 text-white border-orange-600 hover:bg-orange-600';
      case 'building_issue':
        return 'bg-yellow-500 text-white border-yellow-600 hover:bg-yellow-600';
      case 'network_issue':
        return 'bg-pink-500 text-white border-pink-600 hover:bg-pink-600';
      case 'rescheduled':
        return 'bg-purple-500 text-white border-purple-600 hover:bg-purple-600';
      case 'withdrawn':
        return 'bg-red-500 text-white border-red-600 hover:bg-red-600';
      default:
        return 'bg-gray-600 text-white border-gray-700 hover:bg-gray-700';
    }
  }
  
  // When inactive, use light background
  switch (status) {
    case 'pending':
      return 'bg-gray-100 hover:bg-gray-200 text-gray-700 border-gray-300';
    case 'assigned':
      return 'bg-blue-100 hover:bg-blue-200 text-blue-700 border-blue-300';
    case 'on_the_way':
      return 'bg-amber-100 hover:bg-amber-200 text-amber-700 border-amber-300';
    case 'met_customer':
      return 'bg-emerald-100 hover:bg-emerald-200 text-emerald-700 border-emerald-300';
    case 'order_completed':
      return 'bg-lime-100 hover:bg-lime-200 text-lime-700 border-lime-300';
    case 'docket_received':
      return 'bg-teal-100 hover:bg-teal-200 text-teal-700 border-teal-300';
    case 'docket_uploaded':
      return 'bg-cyan-100 hover:bg-cyan-200 text-cyan-700 border-cyan-300';
    case 'ready_to_invoice':
      return 'bg-indigo-100 hover:bg-indigo-200 text-indigo-700 border-indigo-300';
    case 'invoiced':
      return 'bg-violet-100 hover:bg-violet-200 text-violet-700 border-violet-300';
    case 'completed':
      return 'bg-green-100 hover:bg-green-200 text-green-700 border-green-300';
    case 'customer_issue':
      return 'bg-orange-100 hover:bg-orange-200 text-orange-700 border-orange-300';
    case 'building_issue':
      return 'bg-yellow-100 hover:bg-yellow-200 text-yellow-700 border-yellow-300';
    case 'network_issue':
      return 'bg-pink-100 hover:bg-pink-200 text-pink-700 border-pink-300';
    case 'rescheduled':
      return 'bg-purple-100 hover:bg-purple-200 text-purple-700 border-purple-300';
    case 'withdrawn':
      return 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300';
    default:
      return 'bg-gray-100 hover:bg-gray-200 text-gray-700 border-gray-300';
  }
};

/**
 * StatusFilterBar component
 * Shows view toggle and color-coded status filter buttons
 */
const StatusFilterBar: React.FC<StatusFilterBarProps> = ({
  statusFilter,
  onStatusFilterChange,
  showAllUnassigned,
  onViewChange
}) => {
  return (
    <div className="space-y-3">
      {/* View Toggle */}
      <div className="flex gap-2">
        <Button
          variant={!showAllUnassigned ? 'default' : 'outline'}
          size="sm"
          onClick={() => onViewChange(false)}
          className="text-xs"
        >
          Daily View
        </Button>
        <Button
          variant={showAllUnassigned ? 'default' : 'outline'}
          size="sm"
          onClick={() => onViewChange(true)}
          className="text-xs"
        >
          All Unassigned
        </Button>
      </div>

      {/* Status Filter Buttons */}
      <div className="flex flex-wrap gap-2">
        <Button
          variant={statusFilter === null ? 'default' : 'outline'}
          size="sm"
          onClick={() => onStatusFilterChange(null)}
          className="text-xs"
        >
          All
        </Button>
        {ORDER_STATUSES.map((status) => (
          <button
            key={status.value}
            onClick={() => onStatusFilterChange(status.value)}
            className={cn(
              'px-2 py-1 text-xs font-medium rounded border transition-colors',
              getStatusButtonStyle(status.value, statusFilter === status.value)
            )}
          >
            {status.label}
          </button>
        ))}
      </div>
    </div>
  );
};

export default StatusFilterBar;

