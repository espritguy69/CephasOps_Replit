import React, { useState, useEffect, useMemo } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors
} from '@dnd-kit/core';
import type { DragEndEvent } from '@dnd-kit/core';
import { getOrders, updateOrder } from '../../api/orders';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { getTimeSlots } from '../../api/timeSlots';
import { createSlot, deleteSlot } from '../../api/scheduler';
import { PageShell } from '../../components/layout';
import { useToast, Button, Card, Skeleton } from '../../components/ui';
import InstallerPanel from '../../components/scheduler/InstallerPanel';
import SchedulerOrderCard from '../../components/scheduler/SchedulerOrderCard';
import StatusFilterBar from '../../components/scheduler/StatusFilterBar';
import { 
  TimeChangeDialog, 
  CompletionConfirmDialog, 
  RescheduleDialog,
  OrderHistoryDialog 
} from '../../components/scheduler/SchedulerDialogs';
import type { ServiceInstaller } from '../../types/serviceInstallers';
import type { Order } from '../../types/orders';
import type { TimeSlot } from '../../types/timeSlots';
import type { 
  SchedulerOrder,
  TimeChangeDialogState,
  CompletionConfirmDialogState,
  RescheduleDialogState,
  RescheduleReason
} from '../../types/scheduler';

/**
 * Parse appointment date from various formats
 */
const parseAppointmentDate = (dateStr: string | null | undefined): Date | null => {
  if (!dateStr) return null;
  
  // Try ISO format first
  let date = new Date(dateStr);
  if (!isNaN(date.getTime())) return date;
  
  // Try DD/MM/YYYY format
  const parts = dateStr.split('/');
  if (parts.length === 3) {
    const [day, month, year] = parts;
    date = new Date(parseInt(year), parseInt(month) - 1, parseInt(day));
    if (!isNaN(date.getTime())) return date;
  }
  
  return null;
};

/**
 * Normalize time format (e.g., "02:30 PM" -> "2:30 PM")
 */
const normalizeTimeFormat = (time: string): string => {
  return time.replace(/^0(\d)/, '$1');
};

/**
 * Parse time to 24-hour format for comparison
 */
const parseTimeTo24Hour = (timeStr: string): number => {
  const match = timeStr.match(/(\d{1,2}):(\d{2})\s*(AM|PM)?/i);
  if (!match) return 0;
  
  let hours = parseInt(match[1]);
  const minutes = parseInt(match[2]);
  const period = match[3]?.toUpperCase();
  
  if (period === 'PM' && hours !== 12) hours += 12;
  if (period === 'AM' && hours === 12) hours = 0;
  
  return hours * 100 + minutes;
};

/**
 * Convert Order to SchedulerOrder
 */
const toSchedulerOrder = (order: Order): SchedulerOrder => ({
  id: order.id,
  orderNumber: order.partnerOrderId || order.serviceId,
  serviceId: order.serviceId || order.uniqueId,
  ticketId: order.ticketId,
  customerName: order.customerName,
  customerPhone: order.customerPhone,
  customerEmail: order.customerEmail,
  buildingName: order.buildingName,
  address: order.address,
  fullAddress: order.fullAddress || order.address,
  appointmentDate: order.appointmentDate,
  appointmentTime: order.appointmentTime || order.appointmentWindowFrom,
  status: order.status,
  serviceType: order.orderType,
  partnerName: order.partnerName,
  derivedPartnerCategoryLabel: order.derivedPartnerCategoryLabel,
  partnerId: order.partnerId,
  assignedSiId: order.assignedSiId,
  assignedToName: order.assignedToName
});

const CalendarPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  
  // Data state
  const [orders, setOrders] = useState<Order[]>([]);
  const [serviceInstallers, setServiceInstallers] = useState<ServiceInstaller[]>([]);
  const [timeSlots, setTimeSlots] = useState<TimeSlot[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  
  // View state
  const [selectedDate, setSelectedDate] = useState<Date>(new Date());
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [showAllUnassigned, setShowAllUnassigned] = useState<boolean>(false);
  
  // Bulk assign state
  const [bulkAssignMode, setBulkAssignMode] = useState<boolean>(false);
  const [selectedOrders, setSelectedOrders] = useState<Set<string>>(new Set());
  
  // Draft/Confirm mode
  const [isScheduleConfirmed, setIsScheduleConfirmed] = useState<boolean>(false);
  
  // Dialog states
  const [timeChangeDialog, setTimeChangeDialog] = useState<TimeChangeDialogState>({
    open: false,
    orderId: null,
    currentTime: ''
  });
  const [newTime, setNewTime] = useState<string>('');
  
  const [completionConfirmDialog, setCompletionConfirmDialog] = useState<CompletionConfirmDialogState>({
    open: false,
    orderId: null,
    orderNumber: '',
    customerName: ''
  });
  
  const [rescheduleDialog, setRescheduleDialog] = useState<RescheduleDialogState>({
    open: false,
    orderId: null,
    orderNumber: '',
    customerName: '',
    currentDate: '',
    currentTime: ''
  });
  const [rescheduleDate, setRescheduleDate] = useState<string>('');
  const [rescheduleTime, setRescheduleTime] = useState<string>('');
  const [rescheduleReason, setRescheduleReason] = useState<RescheduleReason>('customer_issue');
  
  const [historyDialog, setHistoryDialog] = useState<{
    open: boolean;
    orderId: string | null;
    orderNumber: string;
  }>({
    open: false,
    orderId: null,
    orderNumber: ''
  });

  // Drag and drop sensors
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor)
  );

  // Get active time slots sorted by time
  const activeTimeSlots = useMemo(() => {
    return timeSlots
      .filter(ts => ts.isActive)
      .sort((a, b) => parseTimeTo24Hour(a.time) - parseTimeTo24Hour(b.time));
  }, [timeSlots]);

  // Filter orders for selected date or all unassigned
  const filteredOrders = useMemo(() => {
    return orders.filter((order) => {
      if (!order.appointmentDate || !order.appointmentTime) return false;
      
      // If showing all unassigned, only show orders without assignments
      if (showAllUnassigned) {
        const isAssigned = !!order.assignedSiId;
        const statusMatches = !statusFilter || order.status === statusFilter;
        return !isAssigned && statusMatches;
      }
      
      // Parse appointment date
      const orderDate = parseAppointmentDate(order.appointmentDate);
      if (!orderDate) return false;
      
      // Compare with selected date (year, month, day only)
      const dateMatches = orderDate.getFullYear() === selectedDate.getFullYear() &&
             orderDate.getMonth() === selectedDate.getMonth() &&
             orderDate.getDate() === selectedDate.getDate();
      
      // Apply status filter if set
      const statusMatches = !statusFilter || order.status === statusFilter;
      
      return dateMatches && statusMatches;
    });
  }, [orders, selectedDate, showAllUnassigned, statusFilter]);

  // Group orders by time slot
  const ordersByTimeSlot = useMemo(() => {
    const grouped: Record<string, SchedulerOrder[]> = {};
    
    activeTimeSlots.forEach((slot) => {
      grouped[slot.time] = [];
    });
    
    filteredOrders.forEach((order) => {
      if (order.appointmentTime) {
        const normalizedOrderTime = normalizeTimeFormat(order.appointmentTime);
        
        const matchingSlot = activeTimeSlots.find((slot) => {
          const normalizedSlot = normalizeTimeFormat(slot.time);
          return normalizedOrderTime.startsWith(normalizedSlot) ||
                 normalizedOrderTime.toLowerCase().includes(normalizedSlot.toLowerCase());
        });
        
        if (matchingSlot) {
          grouped[matchingSlot.time].push(toSchedulerOrder(order));
        }
      }
    });
    
    return grouped;
  }, [filteredOrders, activeTimeSlots]);

  // Create map of orderId to installer name
  const orderInstallerMap = useMemo(() => {
    const map: Record<string, string> = {};
    orders.forEach((order) => {
      if (order.assignedSiId) {
        const installer = serviceInstallers.find(i => i.id === order.assignedSiId);
        if (installer) {
          map[order.id] = installer.name;
        }
      }
    });
    return map;
  }, [orders, serviceInstallers]);

  // Load data
  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      
      const [ordersResponse, siResponse, timeSlotsResponse] = await Promise.all([
        getOrders(),
        getServiceInstallers({ isActive: true }),
        getTimeSlots()
      ]);
      
      setOrders(Array.isArray(ordersResponse) ? ordersResponse : []);
      setServiceInstallers(Array.isArray(siResponse) ? siResponse : []);
      setTimeSlots(Array.isArray(timeSlotsResponse) ? timeSlotsResponse : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load scheduler data');
      console.error('Error loading scheduler data:', err);
    } finally {
      setLoading(false);
    }
  };

  // Navigation handlers
  const handlePreviousDay = (): void => {
    const newDate = new Date(selectedDate);
    newDate.setDate(newDate.getDate() - 1);
    setSelectedDate(newDate);
  };

  const handleNextDay = (): void => {
    const newDate = new Date(selectedDate);
    newDate.setDate(newDate.getDate() + 1);
    setSelectedDate(newDate);
  };

  // Assignment handlers
  const handleAssign = async (orderId: string, installerId: string, installerName: string): Promise<void> => {
    try {
      const order = orders.find(o => o.id === orderId);
      if (!order || !order.appointmentDate || !order.appointmentTime) {
        showError('Order missing appointment details');
        return;
      }

      // Check if already assigned to this installer
      if (order.assignedSiId === installerId) {
        showError('Already assigned to this installer');
        return;
      }

      // Parse appointment time
      const match = order.appointmentTime.match(/(\d{1,2}):(\d{2})\s*(AM|PM)?/i);
      if (!match) {
        showError('Invalid time format');
        return;
      }

      let hours = parseInt(match[1]);
      const minutes = match[2];
      const period = match[3]?.toUpperCase();

      if (period === 'PM' && hours !== 12) hours += 12;
      if (period === 'AM' && hours === 12) hours = 0;

      const scheduledStartTime = `${hours.toString().padStart(2, '0')}:${minutes}`;
      const endHour = (hours + 2) % 24;
      const scheduledEndTime = `${endHour.toString().padStart(2, '0')}:${minutes}`;

      // Create slot assignment
      await createSlot({
        orderId,
        serviceInstallerId: installerId,
        date: order.appointmentDate,
        windowFrom: scheduledStartTime,
        windowTo: scheduledEndTime
      });

      // Update order status to assigned
      await updateOrder(orderId, {
        status: 'assigned',
        assignedSiId: installerId
      });

      showSuccess(`Assigned to ${installerName}`);
      await loadData();
    } catch (error) {
      console.error('Failed to assign installer:', error);
      showError('Failed to assign installer');
    }
  };

  const handleUnassign = async (orderId: string): Promise<void> => {
    try {
      // Update order to remove assignment
      await updateOrder(orderId, {
        status: 'pending',
        assignedSiId: undefined
      });

      showSuccess('Installer unassigned');
      await loadData();
    } catch (error) {
      console.error('Failed to unassign:', error);
      showError('Failed to unassign installer');
    }
  };

  // Time change handlers
  const handleTimeChange = (orderId: string): void => {
    const order = orders.find(o => o.id === orderId);
    if (order) {
      setTimeChangeDialog({
        open: true,
        orderId,
        currentTime: order.appointmentTime || ''
      });
      setNewTime(order.appointmentTime || '');
    }
  };

  const handleTimeChangeSubmit = async (): Promise<void> => {
    if (!timeChangeDialog.orderId || !newTime) return;

    try {
      await updateOrder(timeChangeDialog.orderId, {
        appointmentTime: newTime
      });

      showSuccess('Appointment time updated');
      setTimeChangeDialog({ open: false, orderId: null, currentTime: '' });
      await loadData();
    } catch (error) {
      console.error('Failed to update time:', error);
      showError('Failed to update time');
    }
  };

  // Status change handler
  const handleStatusChange = async (orderId: string, newStatus: string): Promise<void> => {
    // If changing to completed, show confirmation dialog
    if (newStatus === 'completed') {
      const order = orders.find(o => o.id === orderId);
      if (order) {
        setCompletionConfirmDialog({
          open: true,
          orderId,
          orderNumber: order.partnerOrderId || order.serviceId || 'N/A',
          customerName: order.customerName || 'Unknown Customer'
        });
      }
      return;
    }

    // If changing to rescheduled, show reschedule dialog
    if (newStatus === 'rescheduled') {
      const order = orders.find(o => o.id === orderId);
      if (order) {
        setRescheduleDialog({
          open: true,
          orderId,
          orderNumber: order.partnerOrderId || order.serviceId || 'N/A',
          customerName: order.customerName || 'Unknown Customer',
          currentDate: order.appointmentDate || '',
          currentTime: order.appointmentTime || ''
        });
        setRescheduleDate(order.appointmentDate || '');
        setRescheduleTime(order.appointmentTime || '');
      }
      return;
    }

    // For other statuses, update directly
    try {
      await updateOrder(orderId, { status: newStatus });
      showSuccess('Status updated');
      await loadData();
    } catch (error) {
      console.error('Failed to update status:', error);
      showError('Failed to update status');
    }
  };

  // Completion confirm handler
  const handleConfirmCompletion = async (): Promise<void> => {
    const { orderId } = completionConfirmDialog;
    if (!orderId) return;

    try {
      await updateOrder(orderId, { status: 'completed' });
      showSuccess('Order marked as completed');
      setCompletionConfirmDialog({
        open: false,
        orderId: null,
        orderNumber: '',
        customerName: ''
      });
      await loadData();
    } catch (error) {
      showError('Failed to mark order as completed');
    }
  };

  // Reschedule confirm handler
  const handleConfirmReschedule = async (): Promise<void> => {
    const { orderId } = rescheduleDialog;
    if (!orderId || !rescheduleDate || !rescheduleTime) {
      showError('Please select both date and time');
      return;
    }

    try {
      await updateOrder(orderId, {
        status: 'rescheduled',
        appointmentDate: rescheduleDate,
        appointmentTime: rescheduleTime
      });

      showSuccess('Order rescheduled successfully');
      setRescheduleDialog({
        open: false,
        orderId: null,
        orderNumber: '',
        customerName: '',
        currentDate: '',
        currentTime: ''
      });
      setRescheduleDate('');
      setRescheduleTime('');
      setRescheduleReason('customer_issue');
      await loadData();
    } catch (error) {
      showError('Failed to reschedule order');
    }
  };

  // History handler
  const handleHistoryClick = (orderId: string): void => {
    const order = orders.find(o => o.id === orderId);
    if (order) {
      setHistoryDialog({
        open: true,
        orderId,
        orderNumber: order.partnerOrderId || order.serviceId || 'N/A'
      });
    }
  };

  // Bulk assign handlers
  const toggleOrderSelection = (orderId: string): void => {
    setSelectedOrders((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(orderId)) {
        newSet.delete(orderId);
      } else {
        newSet.add(orderId);
      }
      return newSet;
    });
  };

  const toggleBulkAssignMode = (): void => {
    setBulkAssignMode(!bulkAssignMode);
    if (bulkAssignMode) {
      setSelectedOrders(new Set());
    }
  };

  const handleBulkAssign = async (installerId: string): Promise<void> => {
    if (selectedOrders.size === 0) {
      showError('No orders selected');
      return;
    }

    try {
      const installer = serviceInstallers.find(i => i.id === installerId);
      if (!installer) {
        showError('Installer not found');
        return;
      }

      for (const orderId of selectedOrders) {
        const order = orders.find(o => o.id === orderId);
        if (!order) continue;

        await updateOrder(orderId, {
          status: 'assigned',
          assignedSiId: installerId
        });
      }

      showSuccess(`Assigned ${selectedOrders.size} orders to ${installer.name}`);
      setSelectedOrders(new Set());
      setBulkAssignMode(false);
      await loadData();
    } catch (error) {
      console.error('Failed to bulk assign:', error);
      showError('Failed to assign orders');
    }
  };

  // Drag end handler
  const handleDragEnd = async (event: DragEndEvent): Promise<void> => {
    const { active, over } = event;
    
    if (!over) return;
    
    // Check if dragging installer onto order card
    if (active.id.toString().startsWith('installer-') && over.id.toString().startsWith('order-')) {
      const installerId = active.id.toString().replace('installer-', '');
      const orderId = over.id.toString().replace('order-', '');
      const installer = serviceInstallers.find(i => i.id === installerId);
      
      if (installer) {
        await handleAssign(orderId, installerId, installer.name);
      }
    }
  };

  // Schedule confirm handler
  const handleScheduleConfirm = (): void => {
    if (isScheduleConfirmed) {
      setIsScheduleConfirmed(false);
      showSuccess('Schedule returned to draft mode');
    } else {
      const assignedCount = filteredOrders.filter(o => o.assignedSiId).length;
      if (assignedCount === 0) {
        showError('No assignments to confirm');
        return;
      }
      setIsScheduleConfirmed(true);
      showSuccess(`Schedule confirmed! ${assignedCount} assignments ready for notification.`);
    }
  };

  if (loading) {
    return (
      <PageShell title="Scheduler" breadcrumbs={[{ label: 'Scheduler', path: '/scheduler' }, { label: 'Calendar' }]}>
        <div className="flex-1 flex flex-col p-3 md:p-4 lg:p-6 gap-4">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mb-4">
            <div className="flex items-center gap-2">
              <Skeleton className="h-9 w-9 rounded-md" />
              <Skeleton className="h-9 w-16 rounded-md" />
              <Skeleton className="h-9 w-9 rounded-md" />
              <Skeleton className="h-6 w-48 ml-2" />
            </div>
          </div>
          <Skeleton className="h-9 w-full max-w-md rounded-md" />
          <div className="flex-1 flex flex-col lg:flex-row gap-4 md:gap-6">
            <div className="w-full lg:w-64 flex-shrink-0 space-y-2">
              <Skeleton className="h-8 w-32 rounded-md" />
              {[1, 2, 3, 4].map((i) => (
                <Skeleton key={i} className="h-14 w-full rounded-md" />
              ))}
            </div>
            <div className="flex-1 min-w-0 space-y-4">
              {[1, 2, 3].map((i) => (
                <Skeleton key={i} className="h-24 w-full rounded-lg" />
              ))}
            </div>
          </div>
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell title="Scheduler" breadcrumbs={[{ label: 'Scheduler', path: '/scheduler' }, { label: 'Calendar' }]}>
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragEnd={handleDragEnd}
    >
      <div className="flex-1 flex flex-col h-full overflow-hidden bg-background">
        <div className="p-3 md:p-4 border-b bg-white">
          {/* Date Navigation */}
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 sm:gap-0 mb-4">
            <div className="flex items-center gap-2 flex-wrap">
              <Button variant="outline" size="sm" onClick={handlePreviousDay} disabled={showAllUnassigned} className="min-h-[44px] min-w-[44px] md:min-h-0 md:min-w-0">
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button variant="outline" size="sm" onClick={() => setSelectedDate(new Date())} className="min-h-[44px] md:min-h-0">
                Today
              </Button>
              <Button variant="outline" size="sm" onClick={handleNextDay} disabled={showAllUnassigned} className="min-h-[44px] min-w-[44px] md:min-h-0 md:min-w-0">
                <ChevronRight className="h-4 w-4" />
              </Button>
              <div className="ml-0 sm:ml-2 text-base md:text-lg font-semibold">
                {showAllUnassigned ? 'All Unassigned Orders' : selectedDate.toLocaleDateString('en-US', {
                  weekday: 'long',
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric'
                })}
              </div>
            </div>
          </div>

          {/* Status Filter Bar */}
          <StatusFilterBar
            statusFilter={statusFilter}
            onStatusFilterChange={setStatusFilter}
            showAllUnassigned={showAllUnassigned}
            onViewChange={setShowAllUnassigned}
          />
          </div>

        {/* Main Content */}
        <div className="flex-1 flex flex-col lg:flex-row overflow-hidden p-3 md:p-4 lg:p-6 gap-4 md:gap-6">
          {/* Installer Panel */}
          <div className="w-full lg:w-64 xl:w-80 flex-shrink-0">
            <InstallerPanel
              installers={serviceInstallers}
              bulkMode={bulkAssignMode}
              selectedOrdersCount={selectedOrders.size}
              onBulkAssign={handleBulkAssign}
            />
          </div>

          {/* Time Slots Grid */}
          <div className="flex-1 overflow-auto min-w-0">
            <div className="space-y-4">
              {activeTimeSlots.length === 0 ? (
                <Card className="p-8 text-center text-muted-foreground">
                  No time slots configured. Please add time slots in Settings.
                </Card>
              ) : (
                activeTimeSlots.map((timeSlot) => (
                  <Card key={timeSlot.id} className="p-4">
                    <div className="flex gap-4">
                      <div className="w-24 font-semibold text-lg flex-shrink-0">
                        {timeSlot.time}
                      </div>
                      <div className="flex-1">
                        {ordersByTimeSlot[timeSlot.time]?.length > 0 ? (
                          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
                            {ordersByTimeSlot[timeSlot.time].map((order) => (
                              <SchedulerOrderCard
                                key={order.id}
                                order={order}
                                assignedInstaller={orderInstallerMap[order.id] || null}
                                onAssign={handleAssign}
                                onUnassign={handleUnassign}
                                onTimeChange={handleTimeChange}
                                onStatusChange={handleStatusChange}
                                onHistoryClick={handleHistoryClick}
                                bulkMode={bulkAssignMode}
                                isSelected={selectedOrders.has(order.id)}
                                onToggleSelection={toggleOrderSelection}
                              />
                            ))}
                          </div>
                        ) : (
                          <div className="text-muted-foreground text-sm italic py-4">
                            No orders for this time slot
                          </div>
                        )}
                      </div>
                    </div>
                  </Card>
                ))
              )}
            </div>
          </div>
        </div>

        {/* Dialogs */}
        <TimeChangeDialog
          state={timeChangeDialog}
          newTime={newTime}
          timeSlots={activeTimeSlots}
          onNewTimeChange={setNewTime}
          onSubmit={handleTimeChangeSubmit}
          onClose={() => setTimeChangeDialog({ open: false, orderId: null, currentTime: '' })}
        />

        <CompletionConfirmDialog
          state={completionConfirmDialog}
          onConfirm={handleConfirmCompletion}
          onClose={() => setCompletionConfirmDialog({
            open: false,
            orderId: null,
            orderNumber: '',
            customerName: ''
          })}
        />

        <RescheduleDialog
          state={rescheduleDialog}
          rescheduleDate={rescheduleDate}
          rescheduleTime={rescheduleTime}
          rescheduleReason={rescheduleReason}
          timeSlots={activeTimeSlots}
          onDateChange={setRescheduleDate}
          onTimeChange={setRescheduleTime}
          onReasonChange={setRescheduleReason}
          onConfirm={handleConfirmReschedule}
          onClose={() => {
            setRescheduleDialog({
              open: false,
              orderId: null,
              orderNumber: '',
              customerName: '',
              currentDate: '',
              currentTime: ''
            });
            setRescheduleDate('');
            setRescheduleTime('');
            setRescheduleReason('customer_issue');
          }}
        />

        <OrderHistoryDialog
          orderId={historyDialog.orderId}
          orderNumber={historyDialog.orderNumber}
          open={historyDialog.open}
          onClose={() => setHistoryDialog({ open: false, orderId: null, orderNumber: '' })}
        />
        </div>
      </DndContext>
    </PageShell>
  );
};

export default CalendarPage;
