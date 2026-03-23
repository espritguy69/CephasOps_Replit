import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  X,
  ExternalLink,
  User,
  CalendarClock,
  MapPin,
  Building2,
  FileText,
  Phone,
  Mail,
  Wrench,
  Send,
  RotateCcw,
  FileCheck,
  Check,
  XCircle,
} from 'lucide-react';
import { Button } from '../ui';
import type { CalendarSlot } from '../../types/scheduler';
import { cn } from '../../lib/utils';

export interface SchedulerDetailDrawerActions {
  onConfirmSchedule?: (slotId: string) => void;
  onPostSchedule?: (slotId: string) => void;
  onReturnToDraft?: (slotId: string) => void;
  onApproveReschedule?: (slotId: string) => void;
  onRejectReschedule?: (slotId: string, reason: string) => void;
  processingSlotId?: string | null;
}

function formatTime(timeSpan?: string): string {
  if (!timeSpan) return '—';
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

export interface SchedulerDetailDrawerProps {
  slot: CalendarSlot | null;
  open: boolean;
  onClose: () => void;
  actions?: SchedulerDetailDrawerActions;
  className?: string;
}

/**
 * Side panel showing order/slot details and quick actions (open order, etc.).
 */
const SchedulerDetailDrawer: React.FC<SchedulerDetailDrawerProps> = ({ slot, open, onClose, actions, className }) => {
  const navigate = useNavigate();
  const [rejectReason, setRejectReason] = useState('');

  const handleOpenOrder = () => {
    if (slot?.orderId) {
      navigate(`/orders/${slot.orderId}`);
      onClose();
    }
  };

  if (!slot) return null;

  const from = formatTime(slot.windowFrom || slot.startTime);
  const to = formatTime(slot.windowTo || slot.endTime);
  const timeRange = from && to ? `${from} – ${to}` : '—';

  return (
    <>
      <div
        className={cn(
          'fixed inset-0 z-40 bg-black/20 transition-opacity',
          open ? 'opacity-100' : 'opacity-0 pointer-events-none'
        )}
        onClick={onClose}
        aria-hidden
      />
      <aside
        className={cn(
          'fixed top-0 right-0 z-50 h-full w-full max-w-md bg-background border-l shadow-xl flex flex-col transition-transform duration-200',
          open ? 'translate-x-0' : 'translate-x-full',
          className
        )}
        aria-label="Appointment details"
      >
        <div className="flex items-center justify-between border-b px-4 py-3">
          <h2 className="text-lg font-semibold">Appointment details</h2>
          <Button variant="ghost" size="sm" onClick={onClose} aria-label="Close">
            <X className="h-5 w-5" />
          </Button>
        </div>
        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          <div>
            <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
              <FileText className="h-4 w-4" />
              Order number
            </div>
            <p className="font-mono font-medium">{slot.orderNumber || slot.serviceId || slot.externalRef || '—'}</p>
          </div>
          <div>
            <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
              <User className="h-4 w-4" />
              Customer
            </div>
            <p className="font-medium">{slot.customerName || '—'}</p>
            {(slot.customerPhone || slot.customerEmail) && (
              <div className="mt-1 text-sm text-muted-foreground space-y-0.5">
                {slot.customerPhone && (
                  <div className="flex items-center gap-2">
                    <Phone className="h-3 w-3" />
                    {slot.customerPhone}
                  </div>
                )}
                {slot.customerEmail && (
                  <div className="flex items-center gap-2">
                    <Mail className="h-3 w-3" />
                    {slot.customerEmail}
                  </div>
                )}
              </div>
            )}
          </div>
          <div>
            <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
              <CalendarClock className="h-4 w-4" />
              Time
            </div>
            <p>{timeRange}</p>
            <p className="text-sm text-muted-foreground">{slot.date}</p>
          </div>
          <div>
            <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
              <Wrench className="h-4 w-4" />
              Job type / Status
            </div>
            <p className="font-medium">{slot.serviceType || slot.derivedPartnerCategoryLabel || '—'}</p>
            <p className="text-sm text-muted-foreground">
              {slot.orderStatus || slot.status || '—'}
            </p>
          </div>
          <div>
            <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
              <User className="h-4 w-4" />
              Installer
            </div>
            <p className="font-medium">{slot.serviceInstallerName || slot.siName || '—'}</p>
          </div>
          <div>
            <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
              <Building2 className="h-4 w-4" />
              <MapPin className="h-4 w-4" />
              Building / Address
            </div>
            <p className="text-sm">{slot.buildingName || slot.address || slot.fullAddress || '—'}</p>
          </div>
        </div>
        <div className="border-t p-4 space-y-3">
          {actions && slot && (
            <div className="space-y-2">
              {slot.status === 'Draft' && actions.onConfirmSchedule && (
                <Button
                  className="w-full"
                  variant="outline"
                  size="sm"
                  onClick={() => actions.onConfirmSchedule?.(slot.id)}
                  disabled={actions.processingSlotId === slot.id}
                >
                  <FileCheck className="h-4 w-4 mr-2" />
                  Confirm schedule
                </Button>
              )}
              {slot.status === 'Confirmed' && (
                <>
                  {actions.onPostSchedule && (
                    <Button
                      className="w-full"
                      size="sm"
                      onClick={() => actions.onPostSchedule?.(slot.id)}
                      disabled={actions.processingSlotId === slot.id}
                    >
                      <Send className="h-4 w-4 mr-2" />
                      Post to SI
                    </Button>
                  )}
                  {actions.onReturnToDraft && (
                    <Button
                      className="w-full"
                      variant="outline"
                      size="sm"
                      onClick={() => actions.onReturnToDraft?.(slot.id)}
                      disabled={actions.processingSlotId === slot.id}
                    >
                      <RotateCcw className="h-4 w-4 mr-2" />
                      Return to draft
                    </Button>
                  )}
                </>
              )}
              {slot.status === 'RescheduleRequested' && (
                <>
                  {actions.onApproveReschedule && (
                    <Button
                      className="w-full"
                      size="sm"
                      onClick={() => actions.onApproveReschedule?.(slot.id)}
                      disabled={actions.processingSlotId === slot.id}
                    >
                      <Check className="h-4 w-4 mr-2" />
                      Approve reschedule
                    </Button>
                  )}
                  {actions.onRejectReschedule && (
                    <div className="flex gap-2">
                      <input
                        type="text"
                        placeholder="Rejection reason"
                        className="flex-1 h-9 rounded-md border px-2 text-sm"
                        value={rejectReason}
                        onChange={(e) => setRejectReason(e.target.value)}
                      />
                      <Button
                        variant="destructive"
                        size="sm"
                        onClick={() => {
                          if (rejectReason.trim()) {
                            actions.onRejectReschedule?.(slot.id, rejectReason.trim());
                            setRejectReason('');
                          }
                        }}
                        disabled={actions.processingSlotId === slot.id || !rejectReason.trim()}
                      >
                        <XCircle className="h-4 w-4 mr-1" />
                        Reject
                      </Button>
                    </div>
                  )}
                </>
              )}
            </div>
          )}
          <Button className="w-full" onClick={handleOpenOrder}>
            <ExternalLink className="h-4 w-4 mr-2" />
            Open order detail
          </Button>
        </div>
      </aside>
    </>
  );
};

export default SchedulerDetailDrawer;
