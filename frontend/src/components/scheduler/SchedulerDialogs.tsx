import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { formatDistanceToNow, format } from 'date-fns';
import { Modal, Button, Select, Label, Textarea } from '../ui';
import { Input } from '../ui/input';
import { RESCHEDULE_REASONS } from '../../types/scheduler';
import type { 
  TimeChangeDialogState, 
  CompletionConfirmDialogState, 
  RescheduleDialogState,
  RescheduleReason 
} from '../../types/scheduler';
import type { TimeSlot } from '../../types/timeSlots';
import type { OrderStatusLog, OrderReschedule } from '../../types/orders';
import { getOrderStatusLogs, getOrderReschedules } from '../../api/orders';

// ============================================
// Time Change Dialog
// ============================================

interface TimeChangeDialogProps {
  state: TimeChangeDialogState;
  newTime: string;
  timeSlots: TimeSlot[];
  onNewTimeChange: (time: string) => void;
  onSubmit: () => void;
  onClose: () => void;
}

export const TimeChangeDialog: React.FC<TimeChangeDialogProps> = ({
  state,
  newTime,
  timeSlots,
  onNewTimeChange,
  onSubmit,
  onClose
}) => {
  return (
    <Modal
      isOpen={state.open}
      onClose={onClose}
      title="Change Appointment Time"
    >
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Select a new time slot for this appointment
        </p>
        <div className="space-y-2">
          <Label htmlFor="newTime">New Time Slot</Label>
          <Select
            id="newTime"
            value={newTime}
            onChange={(e) => onNewTimeChange(e.target.value)}
            options={[
              { value: '', label: 'Choose a time slot' },
              ...timeSlots.map(slot => ({ value: slot.time, label: slot.time }))
            ]}
          />
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={onSubmit} disabled={!newTime}>
            Update Time
          </Button>
        </div>
      </div>
    </Modal>
  );
};

// ============================================
// Completion Confirm Dialog
// ============================================

interface CompletionConfirmDialogProps {
  state: CompletionConfirmDialogState;
  onConfirm: () => void;
  onClose: () => void;
}

export const CompletionConfirmDialog: React.FC<CompletionConfirmDialogProps> = ({
  state,
  onConfirm,
  onClose
}) => {
  return (
    <Modal
      isOpen={state.open}
      onClose={onClose}
      title="Confirm Order Completion"
    >
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Are you sure you want to mark this order as completed?
        </p>
        <div className="space-y-2 py-2">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-sm">Order Number:</span>
            <span className="text-sm">{state.orderNumber}</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="font-semibold text-sm">Customer:</span>
            <span className="text-sm">{state.customerName}</span>
          </div>
        </div>
        <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-md">
          <p className="text-sm text-yellow-800">
            ⚠️ This action will mark the order as completed. Make sure all work has been finished and documented.
          </p>
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={onConfirm}>
            Confirm Completion
          </Button>
        </div>
      </div>
    </Modal>
  );
};

// ============================================
// Reschedule Dialog
// ============================================

interface RescheduleDialogProps {
  state: RescheduleDialogState;
  rescheduleDate: string;
  rescheduleTime: string;
  rescheduleReason: RescheduleReason;
  timeSlots: TimeSlot[];
  onDateChange: (date: string) => void;
  onTimeChange: (time: string) => void;
  onReasonChange: (reason: RescheduleReason) => void;
  onConfirm: () => void;
  onClose: () => void;
}

export const RescheduleDialog: React.FC<RescheduleDialogProps> = ({
  state,
  rescheduleDate,
  rescheduleTime,
  rescheduleReason,
  timeSlots,
  onDateChange,
  onTimeChange,
  onReasonChange,
  onConfirm,
  onClose
}) => {
  return (
    <Modal
      isOpen={state.open}
      onClose={onClose}
      title="Reschedule Order"
    >
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Select a new date and time for this order
        </p>
        
        <div className="space-y-2">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-sm">Order:</span>
            <span className="text-sm">{state.orderNumber}</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="font-semibold text-sm">Customer:</span>
            <span className="text-sm">{state.customerName}</span>
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="reschedule-reason">Reschedule Reason</Label>
          <Select
            id="reschedule-reason"
            value={rescheduleReason}
            onChange={(e) => onReasonChange(e.target.value as RescheduleReason)}
            options={RESCHEDULE_REASONS.map(r => ({ value: r.value, label: r.label }))}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="reschedule-date">New Date</Label>
          <Input
            id="reschedule-date"
            type="date"
            value={rescheduleDate}
            onChange={(e) => onDateChange(e.target.value)}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="reschedule-time">New Time</Label>
          <Select
            id="reschedule-time"
            value={rescheduleTime}
            onChange={(e) => onTimeChange(e.target.value)}
            options={[
              { value: '', label: 'Select time' },
              ...timeSlots.map(slot => ({ value: slot.time, label: slot.time }))
            ]}
          />
        </div>

        <div className="p-3 bg-blue-50 border border-blue-200 rounded-md">
          <p className="text-sm text-blue-800">
            ℹ️ The order will be rescheduled to the new date and time. The installer assignment will be updated automatically.
          </p>
        </div>

        <div className="flex justify-end gap-2 mt-4">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button 
            onClick={onConfirm}
            disabled={!rescheduleDate || !rescheduleTime}
          >
            Confirm Reschedule
          </Button>
        </div>
      </div>
    </Modal>
  );
};

// ============================================
// Order History Dialog
// ============================================

interface OrderHistoryDialogProps {
  orderId: string | null;
  orderNumber: string;
  open: boolean;
  onClose: () => void;
}

export const OrderHistoryDialog: React.FC<OrderHistoryDialogProps> = ({
  orderId,
  orderNumber,
  open,
  onClose
}) => {
  const { data: statusLogs = [], isLoading: logsLoading } = useQuery<OrderStatusLog[]>({
    queryKey: ['orderStatusLogs', orderId],
    queryFn: () => getOrderStatusLogs(orderId!),
    enabled: !!orderId && open,
  });

  const { data: reschedules = [], isLoading: reschedulesLoading } = useQuery<OrderReschedule[]>({
    queryKey: ['orderReschedules', orderId],
    queryFn: () => getOrderReschedules(orderId!),
    enabled: !!orderId && open,
  });

  const isLoading = logsLoading || reschedulesLoading;

  // Combine and sort all history events by date
  const historyEvents = React.useMemo(() => {
    const events: Array<{
      type: 'status' | 'reschedule';
      date: Date;
      data: OrderStatusLog | OrderReschedule;
    }> = [];

    statusLogs.forEach(log => {
      events.push({
        type: 'status',
        date: new Date(log.createdAt),
        data: log,
      });
    });

    reschedules.forEach(reschedule => {
      events.push({
        type: 'reschedule',
        date: new Date(reschedule.requestedAt),
        data: reschedule,
      });
    });

    return events.sort((a, b) => b.date.getTime() - a.date.getTime());
  }, [statusLogs, reschedules]);

  const formatStatusChange = (log: OrderStatusLog) => {
    if (log.fromStatus) {
      return `${log.fromStatus} → ${log.toStatus}`;
    }
    return `Set to ${log.toStatus}`;
  };

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      'Pending': 'bg-gray-100 text-gray-700',
      'Assigned': 'bg-blue-100 text-blue-700',
      'OnTheWay': 'bg-yellow-100 text-yellow-700',
      'MetCustomer': 'bg-yellow-100 text-yellow-700',
      'OrderCompleted': 'bg-green-100 text-green-700',
      'Blocker': 'bg-red-100 text-red-700',
      'Cancelled': 'bg-red-100 text-red-700',
    };
    return colors[status] || 'bg-gray-100 text-gray-700';
  };

  const getRescheduleStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      'Pending': 'bg-yellow-100 text-yellow-700',
      'Approved': 'bg-green-100 text-green-700',
      'Rejected': 'bg-red-100 text-red-700',
      'Cancelled': 'bg-gray-100 text-gray-700',
    };
    return colors[status] || 'bg-gray-100 text-gray-700';
  };

  return (
    <Modal
      isOpen={open}
      onClose={onClose}
      title={`Order History - ${orderNumber}`}
    >
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Complete history for order {orderNumber}
        </p>
        
        <div className="space-y-2 max-h-96 overflow-y-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
              <span className="ml-2 text-sm text-muted-foreground">Loading history...</span>
            </div>
          ) : historyEvents.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No history available for this order.
            </div>
          ) : (
            historyEvents.map((event, index) => (
              <div key={`${event.type}-${index}`} className="p-3 bg-gray-50 rounded-lg border">
                {event.type === 'status' ? (
                  <>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className={`px-2 py-0.5 rounded text-xs font-medium ${getStatusColor((event.data as OrderStatusLog).toStatus)}`}>
                          Status Change
                        </span>
                        <span className="font-medium text-sm">
                          {formatStatusChange(event.data as OrderStatusLog)}
                        </span>
                      </div>
                    </div>
                    {(event.data as OrderStatusLog).transitionReason && (
                      <p className="text-sm text-muted-foreground mt-1">
                        Reason: {(event.data as OrderStatusLog).transitionReason}
                      </p>
                    )}
                    <div className="flex items-center gap-2 mt-2 text-xs text-muted-foreground">
                      <span>
                        {(event.data as OrderStatusLog).triggeredByUserName || 
                         (event.data as OrderStatusLog).triggeredBySiName || 
                         (event.data as OrderStatusLog).source || 'System'}
                      </span>
                      <span>•</span>
                      <span title={format(event.date, 'PPpp')}>
                        {formatDistanceToNow(event.date, { addSuffix: true })}
                      </span>
                    </div>
                  </>
                ) : (
                  <>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className={`px-2 py-0.5 rounded text-xs font-medium ${getRescheduleStatusColor((event.data as OrderReschedule).status)}`}>
                          Reschedule {(event.data as OrderReschedule).status}
                        </span>
                        {(event.data as OrderReschedule).isSameDayReschedule && (
                          <span className="px-2 py-0.5 rounded text-xs font-medium bg-orange-100 text-orange-700">
                            Same Day
                          </span>
                        )}
                      </div>
                    </div>
                    <div className="text-sm mt-1">
                      <span className="text-muted-foreground">From:</span>{' '}
                      {format(new Date((event.data as OrderReschedule).originalDate), 'PP')}
                      {' → '}
                      <span className="text-muted-foreground">To:</span>{' '}
                      {format(new Date((event.data as OrderReschedule).newDate), 'PP')}
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">
                      Reason: {(event.data as OrderReschedule).reason}
                    </p>
                    <div className="flex items-center gap-2 mt-2 text-xs text-muted-foreground">
                      <span>
                        {(event.data as OrderReschedule).requestedByUserName || 
                         (event.data as OrderReschedule).requestedBySiName || 
                         (event.data as OrderReschedule).requestedBySource}
                      </span>
                      <span>•</span>
                      <span title={format(event.date, 'PPpp')}>
                        {formatDistanceToNow(event.date, { addSuffix: true })}
                      </span>
                    </div>
                  </>
                )}
              </div>
            ))
          )}
        </div>

        <div className="flex justify-end mt-4">
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>
    </Modal>
  );
};

