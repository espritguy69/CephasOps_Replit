import React, { useEffect, useState, useMemo, useCallback, useRef } from 'react';
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
import { Calendar, Clock, CheckCircle2, Users, Wrench, Plus, AlertTriangle, X, Search, CheckSquare, Square, Wand2 } from 'lucide-react';
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
import { useToast, Button, Badge, StatusBadge, Skeleton, EmptyState, Modal } from '../../components/ui';
import Card from '../../components/ui/Card';
import { ScrollArea } from '../../components/ui/scroll-area';
import { PageShell } from '../../components/layout';
import { StatsCard } from '../../components/scheduler/StatsCard';
import StatusFilterBar from '../../components/scheduler/StatusFilterBar';
import SchedulerToolbar, { type SchedulerViewMode } from '../../components/scheduler/SchedulerToolbar';
import SchedulerGrid from '../../components/scheduler/SchedulerGrid';
import SchedulerDetailDrawer from '../../components/scheduler/SchedulerDetailDrawer';
import QuickAssignPanel from '../../components/scheduler/QuickAssignPanel';
import SchedulerSummaryBar, { computeSummaryStats } from '../../components/scheduler/SchedulerSummaryBar';
import {
  computeWorkloads,
  rankInstallers,
  checkConflict,
  autoAssignJobs,
  smartDistribute,
  type InstallerWorkload,
  type JobContext,
  type ScoringResult,
  type AutoAssignResult,
} from '../../lib/scheduler/scoringEngine';
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

  const [quickAssignOrderId, setQuickAssignOrderId] = useState<string | null>(null);
  const [quickAssignSubmitting, setQuickAssignSubmitting] = useState(false);
  const quickAssignRef = useRef<HTMLDivElement>(null);

  const [unassignedSearch, setUnassignedSearch] = useState('');

  const [bulkMode, setBulkMode] = useState(false);
  const [selectedOrderIds, setSelectedOrderIds] = useState<Set<string>>(new Set());
  const [bulkAssignOpen, setBulkAssignOpen] = useState(false);
  const [bulkAssignSubmitting, setBulkAssignSubmitting] = useState(false);
  const bulkAssignRef = useRef<HTMLDivElement>(null);

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
          description: `${o.orderType || 'Order'} - ${o.buildingName || o.address || 'Address pending'}`,
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
    let list = unassignedOrders;
    if (statusFilter) list = list.filter((o) => o.status === statusFilter);
    if (unassignedSearch.trim()) {
      const q = unassignedSearch.toLowerCase();
      list = list.filter(
        (o) =>
          (o.name || '').toLowerCase().includes(q) ||
          (o.customerName || '').toLowerCase().includes(q) ||
          (o.serviceId || '').toLowerCase().includes(q) ||
          (o.buildingName || '').toLowerCase().includes(q) ||
          (o.address || '').toLowerCase().includes(q) ||
          (o.description || '').toLowerCase().includes(q)
      );
    }
    return list;
  }, [unassignedOrders, statusFilter, unassignedSearch]);

  const workloads = useMemo(
    () => computeWorkloads(filteredInstallers, slotsForDay),
    [filteredInstallers, slotsForDay]
  );

  const workloadMap = useMemo(() => {
    const map: Record<string, InstallerWorkload> = {};
    for (const w of workloads) map[w.installerId] = w;
    return map;
  }, [workloads]);

  const activeJobContext = useMemo((): JobContext | null => {
    if (!quickAssignOrderId) return null;
    const order = unassignedOrders.find((o) => o.orderId === quickAssignOrderId);
    if (!order) return null;
    return {
      orderId: order.orderId,
      orderType: order.orderType,
      buildingName: order.buildingName,
      address: order.address,
      appointmentDate: order.appointmentDate,
      customerName: order.customerName,
    };
  }, [quickAssignOrderId, unassignedOrders]);

  const rankedScores = useMemo((): ScoringResult[] => {
    if (!activeJobContext) return rankInstallers(filteredInstallers, { orderId: '', orderType: '' }, slotsForDay, workloads, dateStr);
    return rankInstallers(filteredInstallers, activeJobContext, slotsForDay, workloads, dateStr);
  }, [filteredInstallers, activeJobContext, slotsForDay, workloads, dateStr]);

  const summaryStats = useMemo(
    () => computeSummaryStats(workloads, slotsForDay.length, unassignedOrders.length),
    [workloads, slotsForDay, unassignedOrders]
  );

  const [autoAssigning, setAutoAssigning] = useState(false);

  const handleSlotClick = useCallback((slot: CalendarSlot) => {
    setDetailSlot(slot);
    setDetailDrawerOpen(true);
  }, []);

  const handleDropSlot = useCallback(
    async (
      slot: CalendarSlot,
      target: { installerId: string; date: string; windowFrom: string; windowTo: string }
    ) => {
      const otherSlots = calendarSlots.filter((s) => s.id !== slot.id);
      const conflict = checkConflict(target.installerId, target.date, target.windowFrom, target.windowTo, otherSlots);
      if (conflict.hasConflict) {
        showError(conflict.message || 'Installer is busy at this time — move rejected');
        return;
      }

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
    [calendarSlots, loadData, showSuccess, showError]
  );

  const handleDropUnassigned = useCallback(
    async (
      orderId: string,
      target: { installerId: string; date: string; windowFrom: string; windowTo: string }
    ) => {
      const conflict = checkConflict(target.installerId, target.date, target.windowFrom, target.windowTo, calendarSlots);
      if (conflict.hasConflict) {
        showError(conflict.message || 'Installer is busy at this time — drop rejected');
        return;
      }

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
    [calendarSlots, loadData, showSuccess, showError]
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

  const handleQuickAssign = useCallback(
    async (installerId: string) => {
      if (!quickAssignOrderId) return;

      const windowFrom = '09:00:00';
      const windowTo = '11:00:00';
      const conflict = checkConflict(installerId, dateStr, windowFrom, windowTo, slotsForDay);
      if (conflict.hasConflict) {
        showError(conflict.message || 'Installer is busy at this time');
        return;
      }

      const score = rankedScores.find((r) => r.installerId === installerId);
      if (score?.blocked) {
        showError(score.blockReason || 'Cannot assign to this installer');
        return;
      }

      try {
        setQuickAssignSubmitting(true);
        await createSlot({
          orderId: quickAssignOrderId,
          serviceInstallerId: installerId,
          date: dateStr,
          windowFrom,
          windowTo,
        });
        await updateOrder(quickAssignOrderId, { status: 'Assigned', assignedSiId: installerId });
        const installer = filteredInstallers.find((i) => i.id === installerId);
        showSuccess(`Assigned to ${installer?.name || 'installer'}${score ? ` (score: ${Math.round(score.score)})` : ''}`);
        setQuickAssignOrderId(null);
        await loadData();
      } catch (err: any) {
        const msg = err?.message || 'Failed to assign job';
        if (msg.includes('conflict') || msg.includes('overlap')) {
          showError(`Assignment blocked: ${msg}`);
        } else if (msg.includes('skill') || msg.includes('mismatch')) {
          showError(`Skill mismatch: ${msg}`);
        } else {
          showError(msg);
        }
      } finally {
        setQuickAssignSubmitting(false);
      }
    },
    [quickAssignOrderId, dateStr, slotsForDay, rankedScores, filteredInstallers, loadData, showSuccess, showError]
  );

  const handleBulkSmartDistribute = useCallback(
    async () => {
      if (selectedOrderIds.size === 0) return;
      try {
        setBulkAssignSubmitting(true);
        const jobs: JobContext[] = Array.from(selectedOrderIds).map((orderId) => {
          const order = unassignedOrders.find((o) => o.orderId === orderId);
          return {
            orderId,
            orderType: order?.orderType,
            buildingName: order?.buildingName,
            address: order?.address,
            appointmentDate: order?.appointmentDate,
            customerName: order?.customerName,
          };
        });

        const result = smartDistribute(jobs, filteredInstallers, slotsForDay, dateStr);

        for (const assignment of result.assignments) {
          await createSlot({
            orderId: assignment.orderId,
            serviceInstallerId: assignment.installerId,
            date: dateStr,
            windowFrom: '09:00:00',
            windowTo: '11:00:00',
          });
          await updateOrder(assignment.orderId, { status: 'Assigned', assignedSiId: assignment.installerId });
        }

        if (result.unassignable.length > 0) {
          showError(`${result.unassignable.length} job(s) could not be assigned: ${result.unassignable.map((u) => u.reason).join(', ')}`);
        }
        if (result.assignments.length > 0) {
          showSuccess(`Smart distributed ${result.assignments.length} job(s) across ${new Set(result.assignments.map((a) => a.installerId)).size} installer(s)`);
        }

        setSelectedOrderIds(new Set());
        setBulkMode(false);
        setBulkAssignOpen(false);
        await loadData();
      } catch (err: any) {
        showError(err?.message || 'Failed to distribute jobs');
      } finally {
        setBulkAssignSubmitting(false);
      }
    },
    [selectedOrderIds, unassignedOrders, filteredInstallers, slotsForDay, dateStr, loadData, showSuccess, showError]
  );

  const handleAutoAssignAll = useCallback(
    async () => {
      if (unassignedOrders.length === 0) return;
      try {
        setAutoAssigning(true);
        const jobs: JobContext[] = unassignedOrders.map((o) => ({
          orderId: o.orderId,
          orderType: o.orderType,
          buildingName: o.buildingName,
          address: o.address,
          appointmentDate: o.appointmentDate,
          customerName: o.customerName,
        }));

        const result = autoAssignJobs(jobs, filteredInstallers, slotsForDay, dateStr);

        for (const assignment of result.assignments) {
          await createSlot({
            orderId: assignment.orderId,
            serviceInstallerId: assignment.installerId,
            date: dateStr,
            windowFrom: '09:00:00',
            windowTo: '11:00:00',
          });
          await updateOrder(assignment.orderId, { status: 'Assigned', assignedSiId: assignment.installerId });
        }

        if (result.unassignable.length > 0) {
          showError(`${result.unassignable.length} job(s) could not be auto-assigned`);
        }
        if (result.assignments.length > 0) {
          showSuccess(`Auto-assigned ${result.assignments.length} job(s) across ${new Set(result.assignments.map((a) => a.installerId)).size} installer(s)`);
        }

        await loadData();
      } catch (err: any) {
        showError(err?.message || 'Auto-assign failed');
      } finally {
        setAutoAssigning(false);
      }
    },
    [unassignedOrders, filteredInstallers, slotsForDay, dateStr, loadData, showSuccess, showError]
  );

  const toggleOrderSelection = useCallback((orderId: string) => {
    setSelectedOrderIds((prev) => {
      const next = new Set(prev);
      if (next.has(orderId)) next.delete(orderId);
      else next.add(orderId);
      return next;
    });
  }, []);

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

  useEffect(() => {
    if (!quickAssignOrderId) return;
    const handleClickOutside = (e: MouseEvent) => {
      if (quickAssignRef.current && !quickAssignRef.current.contains(e.target as Node)) {
        setQuickAssignOrderId(null);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [quickAssignOrderId]);

  useEffect(() => {
    if (!bulkAssignOpen) return;
    const handleClickOutside = (e: MouseEvent) => {
      if (bulkAssignRef.current && !bulkAssignRef.current.contains(e.target as Node)) {
        setBulkAssignOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [bulkAssignOpen]);

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
        <div className="flex items-center gap-2">
          <Button
            size="sm"
            variant="outline"
            onClick={handleAutoAssignAll}
            disabled={autoAssigning || unassignedOrders.length === 0}
          >
            <Wand2 className="h-4 w-4 mr-1.5" />
            {autoAssigning ? 'Assigning...' : 'Auto Assign All'}
          </Button>
          <Button
            size="sm"
            variant={bulkMode ? 'default' : 'outline'}
            onClick={() => {
              const entering = !bulkMode;
              setBulkMode(entering);
              if (entering) {
                setQuickAssignOrderId(null);
              } else {
                setSelectedOrderIds(new Set());
                setBulkAssignOpen(false);
              }
            }}
          >
            <CheckSquare className="h-4 w-4 mr-1.5" />
            {bulkMode ? 'Exit Bulk' : 'Bulk Assign'}
          </Button>
        </div>
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
                    <li key={i}>{c.conflictDescription}</li>
                  ))}
                </ul>
              </div>
              <Button variant="outline" size="sm" onClick={() => setConflicts([])}>
                Dismiss
              </Button>
            </div>
          )}

          <SchedulerSummaryBar
            totalJobs={summaryStats.totalJobs}
            unassignedCount={summaryStats.unassignedCount}
            overloadedInstallers={summaryStats.overloadedInstallers}
            totalInstallers={summaryStats.totalInstallers}
            avgUtilization={summaryStats.avgUtilization}
            className="mb-2"
          />

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
              <div className="p-3 border-b space-y-2">
                <div className="flex items-center gap-2">
                  <Wrench className="h-4 w-4 text-primary" />
                  <h2 className="text-base font-semibold">Unassigned jobs</h2>
                  <Badge variant="secondary" className="ml-auto">
                    {filteredUnassigned.length}
                  </Badge>
                </div>
                <div className="relative">
                  <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
                  <input
                    type="text"
                    placeholder="Search jobs..."
                    value={unassignedSearch}
                    onChange={(e) => setUnassignedSearch(e.target.value)}
                    className="w-full pl-8 pr-3 py-1.5 text-sm bg-muted/50 border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring transition-colors placeholder:text-muted-foreground"
                  />
                  {unassignedSearch && (
                    <button
                      onClick={() => setUnassignedSearch('')}
                      className="absolute right-2 top-1/2 -translate-y-1/2 p-0.5 rounded hover:bg-muted"
                    >
                      <X className="h-3 w-3 text-muted-foreground" />
                    </button>
                  )}
                </div>
              </div>

              {bulkMode && selectedOrderIds.size > 0 && (
                <div className="px-3 py-2 border-b bg-primary/5">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-medium text-primary">
                      {selectedOrderIds.size} selected
                    </span>
                    <div className="flex items-center gap-1.5">
                      <Button
                        size="sm"
                        variant="outline"
                        className="text-xs h-7"
                        onClick={handleBulkSmartDistribute}
                        disabled={bulkAssignSubmitting}
                      >
                        <Wand2 className="h-3 w-3 mr-1" />
                        {bulkAssignSubmitting ? 'Distributing...' : 'Smart Distribute'}
                      </Button>
                    </div>
                  </div>
                </div>
              )}

              <ScrollArea className="flex-1">
                <div className="p-3 space-y-2">
                  {filteredUnassigned.length === 0 ? (
                    <EmptyState
                      title="No unassigned jobs"
                      description={unassignedSearch ? 'No jobs match your search.' : 'All jobs are assigned or filtered out.'}
                    />
                  ) : (
                    filteredUnassigned.map((order) => (
                      <div key={order.id} className="relative">
                        <UnassignedCard
                          order={order}
                          bulkMode={bulkMode}
                          isSelected={selectedOrderIds.has(order.orderId)}
                          onToggleSelect={() => toggleOrderSelection(order.orderId)}
                          onAssignClick={() =>
                            setQuickAssignOrderId(
                              quickAssignOrderId === order.orderId ? null : order.orderId
                            )
                          }
                          isAssignActive={quickAssignOrderId === order.orderId}
                        />
                        {quickAssignOrderId === order.orderId && (
                          <div className="absolute left-full top-0 ml-2 z-50" ref={quickAssignRef}>
                            <QuickAssignPanel
                              installers={filteredInstallers}
                              workloads={workloads}
                              rankedScores={rankedScores}
                              onAssign={handleQuickAssign}
                              onClose={() => setQuickAssignOrderId(null)}
                              isSubmitting={quickAssignSubmitting}
                            />
                          </div>
                        )}
                      </div>
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
                workloadMap={workloadMap}
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
  bulkMode = false,
  isSelected = false,
  onToggleSelect,
  isAssignActive = false,
}: {
  order: UnassignedOrderItem;
  onAssignClick: () => void;
  bulkMode?: boolean;
  isSelected?: boolean;
  onToggleSelect?: () => void;
  isAssignActive?: boolean;
}) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: `unassigned-${order.orderId}`,
    data: { type: 'unassigned-order', orderId: order.orderId, order },
    disabled: bulkMode,
  });
  const style = transform
    ? { transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`, opacity: isDragging ? 0.6 : 1 }
    : { opacity: isDragging ? 0.6 : 1 };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...(bulkMode ? {} : { ...attributes, ...listeners })}
      className={`rounded-xl border bg-card p-3 transition-all duration-150 text-left ${
        bulkMode ? 'cursor-pointer' : 'cursor-grab active:cursor-grabbing'
      } ${isAssignActive ? 'ring-2 ring-primary shadow-lg' : 'hover:shadow-md'} ${
        isSelected ? 'border-primary bg-primary/5' : ''
      }`}
      onClick={bulkMode ? onToggleSelect : undefined}
    >
      <div className="flex items-start gap-2">
        {bulkMode && (
          <div className="shrink-0 mt-0.5">
            {isSelected ? (
              <CheckSquare className="h-4 w-4 text-primary" />
            ) : (
              <Square className="h-4 w-4 text-muted-foreground" />
            )}
          </div>
        )}
        <div className="min-w-0 flex-1">
          <div className="font-medium text-sm truncate">{order.name}</div>
          <div className="text-xs text-muted-foreground truncate mt-0.5">{order.description}</div>
          {order.customerName && (
            <div className="text-xs text-muted-foreground truncate mt-0.5">{order.customerName}</div>
          )}
        </div>
        {!bulkMode && (
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onAssignClick();
            }}
            onPointerDown={(e) => e.stopPropagation()}
            className={`shrink-0 mt-0.5 p-1.5 sm:p-1.5 p-2.5 rounded-lg transition-colors touch-manipulation ${
              isAssignActive
                ? 'bg-primary text-primary-foreground'
                : 'hover:bg-primary/10 text-primary'
            }`}
            title="Quick assign"
          >
            <Plus className="h-3.5 w-3.5 sm:h-3.5 sm:w-3.5 h-5 w-5" />
          </button>
        )}
      </div>
    </div>
  );
}

export default InstallerSchedulerPage;
