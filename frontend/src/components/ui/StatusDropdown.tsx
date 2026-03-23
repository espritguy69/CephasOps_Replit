import React, { useState } from 'react';
import { cn } from '@/lib/utils';
import { Modal, Button, Select } from './index';
import { Input } from './input';
import { Label } from './label';
import { Textarea } from './textarea';
import {
  getStatusBadgeColor,
  getGenericStatusColor,
  getInvoiceStatusColor,
  getPayrollStatusColor,
  getRmaStatusColor,
  getStatusDotColor,
} from '../../utils/statusColors';
import { ORDER_STATUSES, RESCHEDULE_REASONS } from '../../types/scheduler';

// ============================================================================
// Types
// ============================================================================

export interface StatusOption {
  value: string;
  label: string;
}

export interface StatusDropdownProps {
  value: string;
  onChange: (newStatus: string, additionalData?: Record<string, any>) => void;
  options?: StatusOption[];
  colorScheme?: 'order' | 'generic' | 'invoice' | 'payroll' | 'rma';
  confirmStatuses?: string[];
  rescheduleStatuses?: string[];
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  className?: string;
}

// ============================================================================
// Status Dropdown Component
// ============================================================================

/**
 * StatusDropdown component
 * Color-coded dropdown for status changes with optional confirmation dialogs
 */
const StatusDropdown: React.FC<StatusDropdownProps> = ({
  value,
  onChange,
  options = ORDER_STATUSES,
  colorScheme = 'order',
  confirmStatuses = ['completed'],
  rescheduleStatuses = ['rescheduled'],
  size = 'sm',
  disabled = false,
  className,
}) => {
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [showRescheduleDialog, setShowRescheduleDialog] = useState(false);
  const [pendingStatus, setPendingStatus] = useState<string | null>(null);
  const [rescheduleDate, setRescheduleDate] = useState('');
  const [rescheduleTime, setRescheduleTime] = useState('');
  const [rescheduleReason, setRescheduleReason] = useState('customer_issue');
  const [rescheduleNotes, setRescheduleNotes] = useState('');

  // Get color function based on scheme
  const getColorFn = () => {
    switch (colorScheme) {
      case 'generic':
        return getGenericStatusColor;
      case 'invoice':
        return getInvoiceStatusColor;
      case 'payroll':
        return getPayrollStatusColor;
      case 'rma':
        return getRmaStatusColor;
      default:
        return getStatusBadgeColor;
    }
  };

  const colorFn = getColorFn();

  // Handle status change
  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newStatus = e.target.value;
    
    if (newStatus === value) return;

    // Check if confirmation is needed
    if (confirmStatuses.includes(newStatus)) {
      setPendingStatus(newStatus);
      setShowConfirmDialog(true);
      return;
    }

    // Check if reschedule dialog is needed
    if (rescheduleStatuses.includes(newStatus)) {
      setPendingStatus(newStatus);
      setShowRescheduleDialog(true);
      return;
    }

    // Direct change
    onChange(newStatus);
  };

  // Handle confirm
  const handleConfirm = () => {
    if (pendingStatus) {
      onChange(pendingStatus);
    }
    setShowConfirmDialog(false);
    setPendingStatus(null);
  };

  // Handle reschedule confirm
  const handleRescheduleConfirm = () => {
    if (pendingStatus && rescheduleDate && rescheduleTime) {
      onChange(pendingStatus, {
        rescheduleDate,
        rescheduleTime,
        rescheduleReason,
        rescheduleNotes,
      });
    }
    setShowRescheduleDialog(false);
    setPendingStatus(null);
    setRescheduleDate('');
    setRescheduleTime('');
    setRescheduleReason('customer_issue');
    setRescheduleNotes('');
  };

  // Handle cancel
  const handleCancel = () => {
    setShowConfirmDialog(false);
    setShowRescheduleDialog(false);
    setPendingStatus(null);
    setRescheduleDate('');
    setRescheduleTime('');
    setRescheduleReason('customer_issue');
    setRescheduleNotes('');
  };

  // Size classes
  const sizeClasses = {
    sm: 'h-6 text-xs px-2',
    md: 'h-8 text-sm px-3',
    lg: 'h-10 text-base px-4',
  };

  return (
    <>
      <select
        value={value}
        onChange={handleChange}
        disabled={disabled}
        className={cn(
          'rounded font-medium border cursor-pointer transition-colors',
          sizeClasses[size],
          colorFn(value),
          disabled && 'opacity-50 cursor-not-allowed',
          className
        )}
        onClick={(e) => e.stopPropagation()}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>

      {/* Confirmation Dialog */}
      <Modal
        isOpen={showConfirmDialog}
        onClose={handleCancel}
        title="Confirm Status Change"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Are you sure you want to change the status to{' '}
            <strong>{options.find(o => o.value === pendingStatus)?.label || pendingStatus}</strong>?
          </p>
          
          {pendingStatus === 'completed' && (
            <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-md">
              <p className="text-sm text-yellow-800">
                ⚠️ This action will mark the item as completed. Make sure all work has been finished and documented.
              </p>
            </div>
          )}

          <div className="flex justify-end gap-2 mt-4">
            <Button variant="outline" onClick={handleCancel}>
              Cancel
            </Button>
            <Button onClick={handleConfirm}>
              Confirm
            </Button>
          </div>
        </div>
      </Modal>

      {/* Reschedule Dialog */}
      <Modal
        isOpen={showRescheduleDialog}
        onClose={handleCancel}
        title="Reschedule"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Please provide the new date and time for rescheduling.
          </p>

          <div className="space-y-3">
            <div>
              <Label htmlFor="reschedule-reason">Reason *</Label>
              <Select
                id="reschedule-reason"
                value={rescheduleReason}
                onChange={(e) => setRescheduleReason(e.target.value)}
                options={RESCHEDULE_REASONS.map(r => ({ value: r.value, label: r.label }))}
              />
            </div>

            <div>
              <Label htmlFor="reschedule-date">New Date *</Label>
              <Input
                id="reschedule-date"
                type="date"
                value={rescheduleDate}
                onChange={(e) => setRescheduleDate(e.target.value)}
              />
            </div>

            <div>
              <Label htmlFor="reschedule-time">New Time *</Label>
              <Input
                id="reschedule-time"
                type="time"
                value={rescheduleTime}
                onChange={(e) => setRescheduleTime(e.target.value)}
              />
            </div>

            <div>
              <Label htmlFor="reschedule-notes">Notes (Optional)</Label>
              <Textarea
                id="reschedule-notes"
                value={rescheduleNotes}
                onChange={(e) => setRescheduleNotes(e.target.value)}
                placeholder="Additional notes..."
                rows={3}
              />
            </div>
          </div>

          <div className="p-3 bg-brand-50 border border-brand-200 rounded-md">
            <p className="text-sm text-brand-800">
              ℹ️ The item will be rescheduled to the new date and time.
            </p>
          </div>

          <div className="flex justify-end gap-2 mt-4">
            <Button variant="outline" onClick={handleCancel}>
              Cancel
            </Button>
            <Button 
              onClick={handleRescheduleConfirm}
              disabled={!rescheduleDate || !rescheduleTime}
            >
              Confirm Reschedule
            </Button>
          </div>
        </div>
      </Modal>
    </>
  );
};

// ============================================================================
// Status Badge Component
// ============================================================================

export interface StatusBadgeProps {
  status: string;
  colorScheme?: 'order' | 'generic' | 'invoice' | 'payroll' | 'rma';
  showDot?: boolean;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

/**
 * StatusBadge component
 * Display-only status badge with color coding
 */
export const StatusBadge: React.FC<StatusBadgeProps> = ({
  status,
  colorScheme = 'order',
  showDot = false,
  size = 'sm',
  className,
}) => {
  // Get color function based on scheme
  const getColorFn = () => {
    switch (colorScheme) {
      case 'generic':
        return getGenericStatusColor;
      case 'invoice':
        return getInvoiceStatusColor;
      case 'payroll':
        return getPayrollStatusColor;
      case 'rma':
        return getRmaStatusColor;
      default:
        return getStatusBadgeColor;
    }
  };

  const colorFn = getColorFn();

  // Format status label
  const formatLabel = (s: string): string => {
    return s
      .replace(/_/g, ' ')
      .replace(/\b\w/g, (c) => c.toUpperCase());
  };

  // Size classes
  const sizeClasses = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-3 py-1 text-sm',
    lg: 'px-4 py-1.5 text-base',
  };

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded font-medium border',
        sizeClasses[size],
        colorFn(status),
        className
      )}
    >
      {showDot && (
        <span className={cn('w-2 h-2 rounded-full', getStatusDotColor(status))} />
      )}
      {formatLabel(status)}
    </span>
  );
};

// ============================================================================
// Status Filter Buttons Component
// ============================================================================

export interface StatusFilterButtonsProps {
  options: StatusOption[];
  value: string | null;
  onChange: (value: string | null) => void;
  colorScheme?: 'order' | 'generic' | 'invoice' | 'payroll' | 'rma';
  showAll?: boolean;
  className?: string;
}

/**
 * StatusFilterButtons component
 * Color-coded filter buttons for status filtering
 */
export const StatusFilterButtons: React.FC<StatusFilterButtonsProps> = ({
  options,
  value,
  onChange,
  colorScheme = 'order',
  showAll = true,
  className,
}) => {
  // Import button colors from statusColors
  const { ORDER_STATUS_BUTTON_COLORS } = require('../../utils/statusColors');

  const getButtonColor = (status: string, isActive: boolean): string => {
    const colors = ORDER_STATUS_BUTTON_COLORS[status];
    if (!colors) {
      return isActive 
        ? 'bg-gray-600 text-white border-gray-700'
        : 'bg-gray-100 hover:bg-gray-200 text-gray-700 border-gray-300';
    }
    return isActive ? colors.active : colors.inactive;
  };

  return (
    <div className={cn('flex flex-wrap gap-2', className)}>
      {showAll && (
        <Button
          variant={value === null ? 'default' : 'outline'}
          size="sm"
          onClick={() => onChange(null)}
          className="text-xs"
        >
          All
        </Button>
      )}
      {options.map((option) => (
        <button
          key={option.value}
          onClick={() => onChange(option.value)}
          className={cn(
            'px-2 py-1 text-xs font-medium rounded border transition-colors',
            getButtonColor(option.value, value === option.value)
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
};

export default StatusDropdown;

