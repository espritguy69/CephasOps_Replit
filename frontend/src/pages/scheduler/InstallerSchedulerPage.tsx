import React, { useEffect, useState, useMemo, useCallback } from 'react';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  useDraggable,
  type DragEndEvent,
} from '@dnd-kit/core';
import { Calendar, Clock, CheckCircle2, Users, Wrench, Plus, AlertTriangle } from 'lucide-react';
import {
  getUnassignedOrders,
  getCalendar,
  createSlot,
  updateSlot,
  confirmSchedule,
  postScheduleToSI,
  returnScheduleToDraft,
  getConflicts,
  approveReschedule,
  rejectReschedule,
  getSIAvailability,
  getLeaveRequests,
} from '../../api/scheduler';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { getDepartments } from '../../api/departments';
import { updateOrder } from '../../api/orders';
import { useToast, Button, Badge, StatusBadge, Skeleton, EmptyState } from '../../components/ui';
import Card from '../../components/ui/Card';
import { ScrollArea } from '../../components/ui/scroll-area';
import { PageShell } from '../../components/layout';
import { StatsCard } from '../../components/scheduler/StatsCard';
import StatusFilterBar from '../../components/scheduler/StatusFilterBar';
import SchedulerToolbar, { type SchedulerViewMode } from '../../components/scheduler/SchedulerToolbar';
import SchedulerGrid from '../../components/scheduler/SchedulerGrid';
import SchedulerDetailDrawer from '../../components/scheduler/SchedulerDetailDrawer';
import { useAuth } from '../../contexts/AuthContext';
import type { CalendarSlot, CreateSlotRequest, ScheduleConflict } from '../../types/scheduler';
import type { ServiceInstaller } from '../../types/serviceInstallers';
import type { Department } from '../../types/departments';

interface UnassignedOrderItem {
  id: string;
  orderId: string;
  name: string;
  description: string;
  serviceId?: string;
  customerName?: string;
  buildingName?: string;
  orderType?: string;
  address?: string;
  appointmentDate?: string;
  status?: string;
}

function formatTime(date: Date): string {
  const h = date.getHours().toString().padStart(2, '0');
  const m = date.getMinutes().toString().padStart(2, '0');
  return `${h}:${m}:00`;
}

/**
 * Installer Scheduler Page – Custom Fresha-style resource scheduler.
 * Horizontal installer columns, vertical time grid, drag-and-drop, availability, detail drawer.
 */
const InstallerSchedulerPage: React.FC = () => {
  const { user } = useAuth();
  const { showSuccess, showError } = useToast();

  const [loading, setLoading] = useState(true);
  const [calendarSlots, setCalendarSlots] = useState<CalendarSlot[]>([]);
  const [unassignedOrders, setUnassignedOrders] = useState<UnassignedOrderItem[]>([]);
  const [serviceInstallers, setServiceInstallers] = useState<ServiceInstaller[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [availability, setAvailability] = useState<Record<string, Awaited<ReturnType<typeof getSIAvailability>>>({});
  const [leaveRequests, setLeaveRequests] = useState<Record<string, Awaited<ReturnType<typeof getLeaveRequests>>>>({});

  const [selectedDate, setSelectedDate] = useState<Date>(new Date());
  const [viewMode, setViewMode] = useState<SchedulerViewMode>('day');
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [departmentId, setDepartmentId] = useState<string | null>(null);
  const [installerFilterId, setInstallerFilterId] = useState<string | null>(null);

  const [conflicts, setConflicts] = useState<ScheduleConflict[]>([]);
  const [processingSlotId, setProcessingSlotId] = useState<string | null>(null);
  const [detailSlot, setDetailSlot] = useState<CalendarSlot | null>(null);
  const [detailDrawerOpen, setDetailDrawerOpen] = useState(false);

  const departmentScope = user?.departmentId ?? null;

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      const fromDate = new Date(selectedDate);
      if (viewMode === 'week') fromDate.setDate(fromDate.getDate() - 7);
      const toDate = new Date(selectedDate);
      if (viewMode === 'week') toDate.setDate(toDate.getDate() + 14);
      else toDate.setDate(toDate.getDate() + 1);

      const fromStr = fromDate.toISOString().split('T')[0];
      const toStr = toDate.toISOString().split('T')[0];

      const calendarFilters: { fromDate: string; toDate: string; departmentId?: string } = {
        fromDate: fromStr,
        toDate: toStr,
      };
      if (departmentId) calendarFilters.departmentId = departmentId;

      const [slots, unassigned, installers, depts, availResp, leaveResp] = await Promise.all([
        getCalendar(calendarFilters),
        getUnassignedOrders({ fromDate: fromStr, toDate: toStr }),
        getServiceInstallers({ isActive: true, ...(departmentId ? { departmentId } : {}) }),
        getDepartments({ isActive: true }),
        getSIAvailability({ startDate: fromStr, endDate: toStr }),
        getLeaveRequests({ startDate: fromStr, endDate: toStr, status: 'Approved' }),
      ]);

      setCalendarSlots(Array.isArray(slots) ? slots : []);
      setUnassignedOrders(
        (Array.isArray(unassigned) ? unassigned : []).map((o: any) => ({
          id: o.id,
          orderId: o.id,
          name: o.serviceId || o.customerName || `Order ${(o.id || '').slice(0, 8)}`,
          description: `${o.orderType || 'Order'} - ${o.buildingName || o.address || 'Location TBD'}`,
          serviceId: o.serviceId,
          customerName: o.customerName,
          buildingName: o.buildingName,
          orderType: o.orderType,
          address: o.address || o.fullAddress,
          appointmentDate: o.appointmentDate,
          status: o.status,
        }))
      );
      setServiceInstallers(Array.isArray(installers) ? installers : []);
      setDepartments(Array.isArray(depts) ? depts : []);

      const availBySi: Record<string, Awaited<ReturnType<typeof getSIAvailability>>> = {};
      (Array.isArray(availResp) ? availResp : []).forEach((a: any) => {
        const siId = a.siId || a.serviceInstallerId;
        if (siId) {
          if (!availBySi[siId]) availBySi[siId] = [];
          availBySi[siId].push(a);
        }
      });
      setAvailability(availBySi);

      const leaveBySi: Record<string, Awaited<ReturnType<typeof getLeaveRequests>>> = {};
      (Array.isArray(leaveResp) ? leaveResp : []).forEach((l: any) => {
        const siId = l.siId || l.serviceInstallerId;
        if (siId) {
          if (!leaveBySi[siId]) leaveBySi[siId] = [];
          leaveBySi[siId].push(l);
        }
      });
      setLeaveRequests(leaveBySi);
    } catch (err) {
      console.error('Failed to load scheduler data:', err);
      showError('Failed to load scheduler data');
    } finally {
      setLoading(false);
    }
  }, [selectedDate, viewMode, departmentId, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const dateStr = selectedDate.toISOString().split('T')[0];
  const slotsForDay = useMemo(
    () => calendarSlots.filter((s) => s.date === dateStr),
    [calendarSlots, dateStr]
  );

  const filteredInstallers = useMemo(() => {
    let list = serviceInstallers;
    if (departmentId) list = list.filter((i) => i.departmentId === departmentId);
    if (installerFilterId) list = list.filter((i) => i.id === installerFilterId);
    return list;
  }, [serviceInstallers, departmentId, installerFilterId]);

  const filteredUnassigned = useMemo(() => {
    if (!statusFilter) return unassignedOrders;
    return unassignedOrders.filter((o) => o.status === statusFilter);
  }, [unassignedOrders, statusFilter]);

  const stats = useMemo(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const todayEnd = new Date(today);
    todayEnd.setHours(23, 59, 59, 999);
    const todayStr = today.toISOString().split('T')[0];
    const todayJobs = calendarSlots.filter((s) => s.date === todayStr).length;
    return {
      todayJobs,
      pending: unassignedOrders.length,
      completed: calendarSlots.filter((s) => new Date(s.date + 'T00:00:00') < today).length,
      technicians: serviceInstallers.length,
    };
  }, [calendarSlots, unassignedOrders, serviceInstallers]);

  const handleSlotClick = useCallback((slot: CalendarSlot) => {
    setDetailSlot(slot);
    setDetailDrawerOpen(true);
  }, []);

  const handleDropSlot = useCallback(
    async (
      slot: CalendarSlot,
      target: { installerId: string; date: string; windowFrom: string; windowTo: string }
    ) => {
      try {
        await updateSlot(slot.id, {
          serviceInstallerId: target.installerId,
          date: target.date,
          windowFrom: target.windowFrom,
          windowTo: target.windowTo,
        });
        showSuccess('Appointment moved');
        await loadData();
      } catch (err) {
        showError(err instanceof Error ? err.message : 'Failed to move appointment');
      }
    },
    [loadData, showSuccess, showError]
  );

  const handleDropUnassigned = useCallback(
    async (
      orderId: string,
      target: { installerId: string; date: string; windowFrom: string; windowTo: string }
    ) => {
      try {
        await createSlot({
          orderId,
          serviceInstallerId: target.installerId,
          date: target.date,
          windowFrom: target.windowFrom,
          windowTo: target.windowTo,
        });
        await updateOrder(orderId, { status: 'Assigned', assignedSiId: target.installerId });
        showSuccess('Job assigned');
        await loadData();
      } catch (err) {
        showError(err instanceof Error ? err.message : 'Failed to assign job');
      }
    },
    [loadData, showSuccess, showError]
  );

  const sensors = useSensors(useSensor(PointerSensor), useSensor(KeyboardSensor));

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over?.data?.current) return;

      const data = over.data.current as {
        type?: string;
        installerId?: string;
        date?: string;
        windowFrom?: string;
        windowTo?: string;
      };
      if (data.type !== 'scheduler-cell' || !data.installerId || !data.date || !data.windowFrom || !data.windowTo)
        return;

      const target = {
        installerId: data.installerId,
        date: data.date,
        windowFrom: data.windowFrom,
        windowTo: data.windowTo,
      };

      const activeId = String(active.id);
      if (activeId.startsWith('slot-')) {
        const slotId = activeId.replace('slot-', '');
        const slot = calendarSlots.find((s) => s.id === slotId);
        if (slot) handleDropSlot(slot, target);
      } else if (activeId.startsWith('unassigned-')) {
        const orderId = activeId.replace('unassigned-', '');
        handleDropUnassigned(orderId, target);
      }
    },
    [calendarSlots, handleDropSlot, handleDropUnassigned]
  );

  const handleConfirmSchedule = async (slotId: string) => {
    try {
      setProcessingSlotId(slotId);
      await confirmSchedule(slotId);
      showSuccess('Schedule confirmed');
      await loadData();
    } catch (err: any) {
      showError(err?.message || 'Failed to confirm schedule');
    } finally {
      setProcessingSlotId(null);
    }
  };

  const handlePostSchedule = async (slotId: string) => {
    try {
      setProcessingSlotId(slotId);
      const conflictList = await getConflicts({ slotId });
      if (conflictList.length > 0) {
        setConflicts(conflictList);
        const ok = window.confirm(
          `${conflictList.length} conflict(s) detected. Post anyway?\n\n` +
            conflictList.map((c) => c.conflictDescription).join('\n')
        );
        if (!ok) {
          setProcessingSlotId(null);
          return;
        }
      }
      await postScheduleToSI(slotId);
      showSuccess('Schedule posted to SI');
      setConflicts([]);
      await loadData();
    } catch (err: any) {
      showError(err?.message || 'Failed to post schedule');
    } finally {
      setProcessingSlotId(null);
    }
  };

  const handleReturnToDraft = async (slotId: string) => {
    try {
      setProcessingSlotId(slotId);
      await returnScheduleToDraft(slotId);
      showSuccess('Returned to draft');
      await loadData();
    } catch (err: any) {
      showError(err?.message || 'Failed to return to draft');
    } finally {
      setProcessingSlotId(null);
    }
  };

  const getScheduleStatusVariant = (
    status: string
  ): 'default' | 'success' | 'error' | 'warning' | 'info' | 'secondary' => {
    switch (status) {
      case 'Draft':
        return 'secondary';
      case 'Confirmed':
        return 'info';
      case 'Posted':
        return 'success';
      case 'RescheduleRequested':
        return 'warning';
      case 'RescheduleApproved':
        return 'success';
      case 'RescheduleRejected':
        return 'error';
      default:
        return 'secondary';
    }
  };

  if (loading && calendarSlots.length === 0 && unassignedOrders.length === 0) {
    return (
      <PageShell
        title="Service Installer Schedule"
        breadcrumbs={[{ label: 'Scheduler', path: '/scheduler' }, { label: 'Timeline' }]}
      >
        <div className="space-y-4 flex flex-col flex-1 min-h-0">
          <Skeleton className="h-10 w-full max-w-md rounded-md" />
          <div className="grid gap-3 md:grid-cols-4">
            {[1, 2, 3, 4].map((i) => (
              <Card key={i} className="p-4">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-7 w-12 mt-2" />
              </Card>
            ))}
          </div>
          <div className="flex-1 grid lg:grid-cols-4 gap-4 min-h-0">
            <Card className="lg:col-span-1 p-4">
              <Skeleton className="h-6 w-32 mb-4" />
              <div className="space-y-2">
                {[1, 2, 3, 4, 5].map((i) => (
                  <Skeleton key={i} className="h-14 w-full rounded-md" />
                ))}
              </div>
            </Card>
            <div className="lg:col-span-3 min-h-[400px]">
              <Skeleton className="h-full w-full rounded-lg min-h-[400px]" />
            </div>
          </div>
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Service Installer Schedule"
      breadcrumbs={[{ label: 'Scheduler', path: '/scheduler' }, { label: 'Timeline' }]}
      actions={
        <Button size="sm">
          <Plus className="h-4 w-4 mr-2" />
          Assign job
        </Button>
      }
    >
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <div className="flex flex-col flex-1 min-h-0">
          {conflicts.length > 0 && (
            <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4 flex items-start gap-3">
              <AlertTriangle className="h-5 w-5 text-amber-600 shrink-0 mt-0.5" />
              <div className="flex-1">
                <h3 className="text-sm font-semibold text-amber-800 dark:text-amber-200">
                  Scheduling conflicts ({conflicts.length})
                </h3>
                <ul className="text-sm text-amber-700 dark:text-amber-300 mt-1 space-y-1">
                  {conflicts.map((c, i) => (
                    <li key={i}>• {c.conflictDescription}</li>
                  ))}
                </ul>
              </div>
              <Button variant="outline" size="sm" onClick={() => setConflicts([])}>
                Dismiss
              </Button>
            </div>
          )}

          <StatusFilterBar
            statusFilter={statusFilter}
            onStatusFilterChange={setStatusFilter}
            showAllUnassigned={false}
            onViewChange={() => {}}
          />

          <SchedulerToolbar
            selectedDate={selectedDate}
            onDateChange={setSelectedDate}
            onToday={() => setSelectedDate(new Date())}
            onRefresh={loadData}
            onAssignJob={() => {}}
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            departments={departments}
            departmentId={departmentId}
            onDepartmentChange={setDepartmentId}
            installers={serviceInstallers}
            installerId={installerFilterId}
            onInstallerFilterChange={setInstallerFilterId}
            isLoading={loading}
          />

          <div className="grid lg:grid-cols-4 gap-4 flex-1 min-h-0">
            <Card className="lg:col-span-1 flex flex-col overflow-hidden">
              <div className="p-4 border-b flex items-center gap-2">
                <Wrench className="h-4 w-4 text-primary" />
                <h2 className="text-base font-semibold">Unassigned jobs</h2>
                <Badge variant="secondary" className="ml-auto">
                  {filteredUnassigned.length}
                </Badge>
              </div>
              <ScrollArea className="flex-1">
                <div className="p-3 space-y-2">
                  {filteredUnassigned.length === 0 ? (
                    <EmptyState
                      title="No unassigned jobs"
                      description="All jobs are assigned or filtered out."
                    />
                  ) : (
                    filteredUnassigned.map((order) => (
                      <UnassignedCard
                        key={order.id}
                        order={order}
                        onAssignClick={() => {}}
                      />
                    ))
                  )}
                </div>
              </ScrollArea>
            </Card>

            <div data-testid="scheduler-timeline-root" className="lg:col-span-3 flex flex-col min-h-0 overflow-hidden border rounded-lg bg-card">
              <SchedulerGrid
                slots={slotsForDay}
                installers={filteredInstallers}
                date={dateStr}
                availabilityBySi={availability}
                leaveBySi={leaveRequests}
                onSlotClick={handleSlotClick}
                onDropSlot={handleDropSlot}
                onDropUnassigned={handleDropUnassigned}
                columnWidth={220}
              />
            </div>
          </div>
        </div>

        <SchedulerDetailDrawer
          slot={detailSlot}
          open={detailDrawerOpen}
          onClose={() => {
            setDetailDrawerOpen(false);
            setDetailSlot(null);
          }}
          actions={{
            onConfirmSchedule: handleConfirmSchedule,
            onPostSchedule: handlePostSchedule,
            onReturnToDraft: handleReturnToDraft,
            onApproveReschedule: async (slotId) => {
              try {
                setProcessingSlotId(slotId);
                await approveReschedule(slotId);
                showSuccess('Reschedule approved');
                await loadData();
                setDetailDrawerOpen(false);
                setDetailSlot(null);
              } catch (err: any) {
                showError(err?.message || 'Failed to approve reschedule');
              } finally {
                setProcessingSlotId(null);
              }
            },
            onRejectReschedule: async (slotId, reason) => {
              try {
                setProcessingSlotId(slotId);
                await rejectReschedule(slotId, reason);
                showSuccess('Reschedule rejected');
                await loadData();
                setDetailDrawerOpen(false);
                setDetailSlot(null);
              } catch (err: any) {
                showError(err?.message || 'Failed to reject reschedule');
              } finally {
                setProcessingSlotId(null);
              }
            },
            processingSlotId,
          }}
        />
      </DndContext>
    </PageShell>
  );
};

function UnassignedCard({
  order,
  onAssignClick,
}: {
  order: UnassignedOrderItem;
  onAssignClick: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: `unassigned-${order.orderId}`,
    data: { type: 'unassigned-order', orderId: order.orderId, order },
  });
  const style = transform
    ? { transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`, opacity: isDragging ? 0.6 : 1 }
    : { opacity: isDragging ? 0.6 : 1 };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className="rounded-lg border bg-card p-3 cursor-grab active:cursor-grabbing hover:shadow-md transition-shadow text-left"
    >
      <div className="font-medium text-sm truncate">{order.name}</div>
      <div className="text-xs text-muted-foreground truncate mt-0.5">{order.description}</div>
      {order.customerName && (
        <div className="text-xs text-muted-foreground truncate mt-0.5">{order.customerName}</div>
      )}
    </div>
  );
}

export default InstallerSchedulerPage;
