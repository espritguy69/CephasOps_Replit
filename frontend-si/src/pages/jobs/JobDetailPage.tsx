import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, Skeleton, EmptyState, Button, useToast, Tabs, TabPanel, Breadcrumbs } from '../../components/ui';
import { PageHeader } from '../../components/layout/PageHeader';
import { AlertTriangle, Calendar, Camera, Package, RefreshCw, User } from 'lucide-react';
import { getAllowedTransitions, executeTransition } from '../../api/workflow';
import { getOrder, getRequiredMaterials, getOrderChecklist } from '../../api/orders';
import { ChecklistDisplay } from '../../components/checklist/ChecklistDisplay';
import { PhotoUpload } from '../../components/photos/PhotoUpload';
import { LocationDisplay } from '../../components/gps/LocationDisplay';
import { SerialScanner } from '../../components/scanner/SerialScanner';
import { MaterialsDisplay } from '../../components/materials/MaterialsDisplay';
import { NonSerialisedMaterialEntry } from '../../components/materials/NonSerialisedMaterialEntry';
import { MaterialCollectionAlert } from '../../components/materials/MaterialCollectionAlert';
import { MarkFaultyModal } from '../../components/materials/MarkFaultyModal';
import { ReplacementForm } from '../../components/materials/ReplacementForm';
import { RescheduleRequestModal } from '../../components/scheduler/RescheduleRequestModal';
import { useAuth } from '../../contexts/AuthContext';
import { getScheduleSlotForOrder } from '../../api/scheduler';
import type { Location } from '../../types/api';
import type { ScheduleSlot } from '../../api/scheduler';

export function JobDetailPage() {
  const { orderId } = useParams<{ orderId: string }>();
  const { user, serviceInstaller } = useAuth();
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();
  const [transitioning, setTransitioning] = useState(false);
  const [markFaultyModalOpen, setMarkFaultyModalOpen] = useState(false);
  const [replacementModalOpen, setReplacementModalOpen] = useState(false);
  const [rescheduleModalOpen, setRescheduleModalOpen] = useState(false);
  const [selectedSerialNumber, setSelectedSerialNumber] = useState<string>('');
  const [selectedMaterialName, setSelectedMaterialName] = useState<string>('');
  const [scheduleSlot, setScheduleSlot] = useState<ScheduleSlot | null>(null);
  
  // Get SI ID from service installer profile or user object
  const siId = ((user as any)?.siId || serviceInstaller?.id || (serviceInstaller as any)?.Id) as string | undefined;

  // Check if order type is Assurance
  const orderTypeStr = (order?.orderType || '').toLowerCase();
  const isAssuranceOrder = orderTypeStr.includes('assurance') || orderTypeStr.includes('assur');

  // Check if order status allows material actions
  const canMarkFaulty = order?.status && ['MetCustomer', 'InProgress', 'OrderCompleted'].includes(order.status);

  const { data: order, isLoading, error } = useQuery({
    queryKey: ['jobDetails', orderId],
    queryFn: () => getOrder(orderId || ''),
    enabled: !!orderId,
  });

  const { data: allowedTransitions } = useQuery({
    queryKey: ['allowedTransitions', orderId, order?.status],
    queryFn: () => getAllowedTransitions(orderId || ''),
    enabled: !!orderId && !!order?.status,
  });

  const { data: requiredMaterials } = useQuery({
    queryKey: ['requiredMaterials', orderId],
    queryFn: () => getRequiredMaterials(orderId || ''),
    enabled: !!orderId,
  });

  // Get schedule slot for this order
  const { data: slot } = useQuery({
    queryKey: ['scheduleSlot', orderId],
    queryFn: () => getScheduleSlotForOrder(orderId || ''),
    enabled: !!orderId && !!order && order.status === 'Assigned',
  });

  // Update schedule slot state when data changes
  React.useEffect(() => {
    if (slot) {
      setScheduleSlot(slot);
    } else {
      setScheduleSlot(null);
    }
  }, [slot]);

  const executeTransitionMutation = useMutation({
    mutationFn: async ({ toStatus, metadata }: { toStatus: string; metadata?: any }) => {
      return executeTransition(orderId || '', toStatus, metadata);
    },
    onSuccess: () => {
      showSuccess('Status updated successfully');
      queryClient.invalidateQueries(['jobDetails', orderId]);
      queryClient.invalidateQueries(['assignedJobs']);
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to update status');
    },
  });

  const getCurrentLocation = (): Promise<Location | null> => {
    return new Promise((resolve) => {
      if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
          (position) => {
            resolve({
              latitude: position.coords.latitude,
              longitude: position.coords.longitude,
              accuracy: position.coords.accuracy,
            });
          },
          (error) => {
            console.warn('Error getting location:', error);
            resolve(null);
          },
          { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
        );
      } else {
        resolve(null);
      }
    });
  };

  const handleStatusTransition = async (newStatus: string) => {
    if (!orderId) {
      showError('Order ID missing');
      return;
    }

    // ⚠️ ENHANCED: Validate checklist, serial numbers, and photos before OrderCompleted
    if (newStatus === 'OrderCompleted') {
      try {
        // 1. Validate checklist - all required items must be completed
        const checklistData = await getOrderChecklist(orderId, order?.status || '');
        if (checklistData && checklistData.items) {
          const requiredItems = checklistData.items.filter(item => item.isRequired);
          const answeredItems = checklistData.answers || [];
          
          const missingRequiredItems = requiredItems.filter(item => {
            const answer = answeredItems.find(a => a.itemId === item.id);
            return !answer || (item.answerType === 'YesNo' && answer.answerValue !== 'Yes');
          });

          if (missingRequiredItems.length > 0) {
            showError(
              `Cannot complete order: ${missingRequiredItems.length} required checklist item(s) not completed. ` +
              `Please complete: ${missingRequiredItems.map(i => i.question).join(', ')}`
            );
            return;
          }
        }

        // 2. Validate serial numbers - if required materials are serialised, they must be scanned
        if (requiredMaterials && requiredMaterials.some(m => m.isSerialised)) {
          // Check if serial numbers are scanned (this would need to be implemented via material usage API)
          // For now, we rely on backend validation via workflow engine guard conditions
        }

        // 3. Validate photos - if required, photos must be uploaded
        // This would need to be checked via photos API
        // For now, we rely on backend validation via workflow engine guard conditions
      } catch (err: any) {
        showError(`Validation failed: ${err.message || 'Unable to validate checklist requirements'}`);
        return;
      }
    }

    try {
      setTransitioning(true);
      const location = await getCurrentLocation();
      await executeTransitionMutation.mutateAsync({
        toStatus: newStatus,
        metadata: {
          latitude: location?.latitude,
          longitude: location?.longitude,
          accuracy: location?.accuracy,
        },
      });
    } catch (err: any) {
      showError(err.message || `Failed to transition status to ${newStatus}`);
    } finally {
      setTransitioning(false);
    }
  };

  const breadcrumbsLoading = [{ label: 'Jobs', path: '/jobs' }, { label: 'Job Details', active: true }];
  const breadcrumbsReady = [
    { label: 'Jobs', path: '/jobs' },
    { label: order?.customerName || 'Job Details', active: true },
  ];

  if (isLoading) {
    return (
      <>
        <div className="px-3 py-2 md:px-4 lg:px-6">
          <Breadcrumbs items={breadcrumbsLoading} className="mb-2" />
        </div>
        <PageHeader title="Job Details" />
        <div className="p-4 md:p-6 space-y-4">
          <Card className="p-4">
            <div className="flex justify-between items-start mb-3">
              <Skeleton className="h-7 w-48" />
              <Skeleton className="h-6 w-20 rounded-full" />
            </div>
            <Skeleton className="h-4 w-full mb-2" />
            <Skeleton className="h-4 w-3/4 mb-2" />
            <Skeleton className="h-4 w-1/2" />
          </Card>
          <div className="flex gap-2">
            <Skeleton className="h-10 w-28 rounded-md" />
            <Skeleton className="h-10 w-28 rounded-md" />
          </div>
          <Card className="p-4">
            <Skeleton className="h-5 w-24 mb-3" />
            <Skeleton className="h-4 w-full mb-2" />
            <Skeleton className="h-4 w-full mb-2" />
            <Skeleton className="h-20 w-full rounded-md" />
          </Card>
        </div>
      </>
    );
  }

  if (error) {
    return (
      <>
        <div className="px-3 py-2 md:px-4 lg:px-6">
          <Breadcrumbs items={breadcrumbsLoading} className="mb-2" />
        </div>
        <PageHeader title="Job Details" />
        <div className="p-4">
          <EmptyState
            title="Error loading job"
            description={(error as Error).message || 'Failed to fetch job details.'}
          />
        </div>
      </>
    );
  }

  if (!order) {
    return (
      <>
        <div className="px-3 py-2 md:px-4 lg:px-6">
          <Breadcrumbs items={breadcrumbsLoading} className="mb-2" />
        </div>
        <PageHeader title="Job Details" />
        <div className="p-4">
          <EmptyState title="Job Not Found" description="The requested job could not be found." />
        </div>
      </>
    );
  }

  return (
    <>
      <div className="px-3 py-2 md:px-4 lg:px-6">
        <Breadcrumbs items={breadcrumbsReady} className="mb-2" />
      </div>
      <PageHeader title={order.customerName || 'Job Details'} />
      <div className="p-4 space-y-4">
        <Tabs defaultActiveTab={0}>
          <TabPanel label="Details" icon={<User className="h-4 w-4" />}>
            <div className="space-y-4">
              {/* Customer Information */}
              <Card className="p-4">
                <h3 className="text-lg font-semibold mb-2">{order.customerName || 'N/A'}</h3>
                <p className="text-muted-foreground text-sm mb-1">
                  {order.addressLine1 || ''}, {order.city || ''}
                </p>
                {order.customerPhone && (
                  <p className="text-sm text-muted-foreground">Phone: {order.customerPhone}</p>
                )}
                <p className="text-sm mt-2">
                  Status: <span className="font-medium">{order.status || 'N/A'}</span>
                </p>
              </Card>

              {/* Appointment & Reschedule */}
              {order.status === 'Assigned' && scheduleSlot && scheduleSlot.status === 'Posted' && (
                <Card className="p-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-semibold mb-1">Appointment</h3>
                      <div className="text-sm text-muted-foreground space-y-1">
                        <div className="flex items-center gap-2">
                          <Calendar className="h-4 w-4" />
                          <span>{new Date(scheduleSlot.date).toLocaleDateString()}</span>
                        </div>
                        <div className="flex items-center gap-2">
                          <RefreshCw className="h-4 w-4" />
                          <span>
                            {scheduleSlot.windowFrom.substring(0, 5)} - {scheduleSlot.windowTo.substring(0, 5)}
                          </span>
                        </div>
                        {scheduleSlot.status === 'RescheduleRequested' && (
                          <div className="mt-2 p-2 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded text-xs">
                            <p className="text-yellow-800 dark:text-yellow-200 font-medium">
                              Reschedule Request Pending Approval
                            </p>
                            {scheduleSlot.rescheduleReason && (
                              <p className="text-yellow-700 dark:text-yellow-300 mt-1">
                                Reason: {scheduleSlot.rescheduleReason}
                              </p>
                            )}
                          </div>
                        )}
                      </div>
                    </div>
                    {scheduleSlot.status === 'Posted' && (
                      <Button
                        variant="outline"
                        onClick={() => setRescheduleModalOpen(true)}
                        className="gap-2"
                      >
                        <RefreshCw className="h-4 w-4" />
                        Request Reschedule
                      </Button>
                    )}
                  </div>
                </Card>
              )}

              {/* Status Transitions */}
              {allowedTransitions && allowedTransitions.length > 0 && (
                <Card className="p-4">
                  <h3 className="font-semibold mb-3">Status Actions</h3>
                  <div className="flex flex-wrap gap-2">
                    {allowedTransitions.map((transition: any) => (
                      <Button
                        key={transition.toStatus}
                        onClick={() => handleStatusTransition(transition.toStatus)}
                        disabled={transitioning}
                        variant="outline"
                      >
                        {transition.name || transition.toStatus}
                      </Button>
                    ))}
                  </div>
                </Card>
              )}

              {/* Checklist */}
              {order.status && (
                <ChecklistDisplay orderId={orderId || ''} statusCode={order.status} />
              )}

              {/* Location Display */}
              <LocationDisplay />
            </div>
          </TabPanel>

          <TabPanel label="Materials" icon={<Package className="h-4 w-4" />}>
            <div className="space-y-4">
              {order.status === 'Assigned' && (
                <MaterialCollectionAlert orderId={orderId || ''} />
              )}
              {siId && requiredMaterials && requiredMaterials.some(m => m.isSerialised) && (
                <SerialScanner orderId={orderId || ''} sessionId={orderId || ''} existingScans={[]} />
              )}
              {requiredMaterials && requiredMaterials.some(m => !m.isSerialised) && (
                <NonSerialisedMaterialEntry orderId={orderId || ''} requiredMaterials={requiredMaterials} />
              )}
              <MaterialsDisplay
                orderId={orderId || ''}
                onMarkFaulty={(serialNumber, materialName) => {
                  setSelectedSerialNumber(serialNumber);
                  setSelectedMaterialName(materialName);
                  setMarkFaultyModalOpen(true);
                }}
              />
              {canMarkFaulty && (
                <Card className="p-4">
                  <h3 className="font-semibold mb-3">Material Actions</h3>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      variant="outline"
                      onClick={() => setMarkFaultyModalOpen(true)}
                      className="flex items-center gap-2"
                    >
                      <AlertTriangle className="h-4 w-4" />
                      Mark Device as Faulty
                    </Button>
                    {isAssuranceOrder && (
                      <Button
                        variant="outline"
                        onClick={() => setReplacementModalOpen(true)}
                        className="flex items-center gap-2"
                      >
                        <RefreshCw className="h-4 w-4" />
                        Record Replacement
                      </Button>
                    )}
                  </div>
                </Card>
              )}
            </div>
          </TabPanel>

          <TabPanel label="Photos" icon={<Camera className="h-4 w-4" />}>
            <PhotoUpload orderId={orderId || ''} existingPhotos={[]} maxPhotos={10} />
          </TabPanel>
        </Tabs>
      </div>

      {/* Mark Faulty Modal */}
      <MarkFaultyModal
        orderId={orderId || ''}
        serialNumber={selectedSerialNumber}
        materialName={selectedMaterialName}
        isOpen={markFaultyModalOpen}
        onClose={() => {
          setMarkFaultyModalOpen(false);
          setSelectedSerialNumber('');
          setSelectedMaterialName('');
        }}
      />

      {/* Replacement Form Modal */}
      {isAssuranceOrder && (
        <ReplacementForm
          orderId={orderId || ''}
          isOpen={replacementModalOpen}
          onClose={() => setReplacementModalOpen(false)}
        />
      )}

      {/* Reschedule Request Modal */}
      {rescheduleModalOpen && scheduleSlot && (
        <RescheduleRequestModal
          isOpen={rescheduleModalOpen}
          onClose={() => setRescheduleModalOpen(false)}
          slotId={scheduleSlot.id}
          currentDate={scheduleSlot.date}
          currentWindowFrom={scheduleSlot.windowFrom}
          currentWindowTo={scheduleSlot.windowTo}
          onSuccess={() => {
            queryClient.invalidateQueries(['scheduleSlot', orderId]);
            queryClient.invalidateQueries(['jobDetails', orderId]);
          }}
        />
      )}
    </>
  );
}
