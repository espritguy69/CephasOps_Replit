import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Clock, XCircle, FileText, Calendar, Package, DollarSign, StickyNote, ListChecks, AlertTriangle, CheckCircle, MessageSquare, Send, Key, Eye, EyeOff } from 'lucide-react';
import { getOrder, getOrderStatusLogs, getOrderReschedules, getOrderBlockers, getOrderDockets, getOrderMaterials, checkMaterialCollection, getRequiredMaterials, getOnuPassword } from '../../api/orders';
import { getInvoice } from '../../api/billing';
import { getBuilding } from '../../api/buildings';
import { updateOrder } from '../../api/orders';
import { createRmaFromOrder, getRmaRequestsByOrder } from '../../api/rma';
import { sendJobUpdate, sendSiOnTheWay, sendTtkt } from '../../api/messaging';
import OrderStatusBadge from '../../components/orders/OrderStatusBadge';
import WorkflowTransitionButton from '../../components/workflow/WorkflowTransitionButton';
import OrderStatusChecklistDisplay from '../../components/checklist/OrderStatusChecklistDisplay';
import { CreateRmaModal } from '../../components/rma/CreateRmaModal';
import { ReplacementWarningBanner, isReplacementIncomplete, getReplacementIssues, type IncompleteReplacement } from '../../components/orders/ReplacementWarningBanner';
import { MatchMaterialModal } from '../../components/parser/MatchMaterialModal';
import { createParsedMaterialAlias } from '../../api/parser';
import { LoadingSpinner, EmptyState, Tabs, TabPanel, Button, Card, useToast, Textarea, Label } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useDepartment } from '../../contexts/DepartmentContext';
import type { Order, OrderStatusLog, OrderReschedule, OrderBlocker, OrderDocket, OrderMaterialReplacement } from '../../types/orders';
import { formatLocalDateTime, formatLocalDate } from '../../utils/dateUtils';

// ============================================================================
// Types
// ============================================================================

interface RouteParams {
  orderId: string;
}

// ============================================================================
// Main OrderDetailPage Component
// ============================================================================

const OrderDetailPage: React.FC = () => {
  const { orderId } = useParams<keyof RouteParams>();
  const navigate = useNavigate();
  const { departmentId, loading: departmentLoading } = useDepartment();
  const [order, setOrder] = useState<Order | null>(null);
  const [statusLogs, setStatusLogs] = useState<OrderStatusLog[]>([]);
  const [reschedules, setReschedules] = useState<OrderReschedule[]>([]);
  const [blockers, setBlockers] = useState<OrderBlocker[]>([]);
  const [dockets, setDockets] = useState<OrderDocket[]>([]);
  const [materials, setMaterials] = useState<any[]>([]);
  const [invoice, setInvoice] = useState<any | null>(null);
  const [building, setBuilding] = useState<any | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState(0); // Use index instead of string
  const [editingNotes, setEditingNotes] = useState<{ internal?: string; partner?: string } | null>(null);
  const [materialCollectionCheck, setMaterialCollectionCheck] = useState<any | null>(null);
  const [requiredMaterials, setRequiredMaterials] = useState<any[]>([]);
  const [rmaRequests, setRmaRequests] = useState<any[]>([]);
  const [showCreateRmaModal, setShowCreateRmaModal] = useState(false);
  const [sendingMessage, setSendingMessage] = useState<string | null>(null);
  const [onuPassword, setOnuPassword] = useState<string | null>(null);
  const [loadingOnuPassword, setLoadingOnuPassword] = useState(false);
  const [showOnuPassword, setShowOnuPassword] = useState(false);
  const [matchMaterialName, setMatchMaterialName] = useState<string | null>(null);
  const [matchMaterialSelectedId, setMatchMaterialSelectedId] = useState('');
  const [matchMaterialSaving, setMatchMaterialSaving] = useState(false);
  const { showSuccess, showError } = useToast();

  useEffect(() => {
    // Wait for department context to be ready before fetching
    if (departmentLoading) {
      return;
    }

    if (orderId) {
      loadOrderData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orderId, departmentId, departmentLoading]);

  const handleSendJobUpdate = async () => {
    if (!order.customerPhone) {
      showError('Customer phone number is required');
      return;
    }

    setSendingMessage('job-update');
    try {
      const result = await sendJobUpdate({
        customerPhone: order.customerPhone,
        orderNumber: order.serviceId || order.uniqueId || order.id,
        status: order.status,
        appointmentDate: order.appointmentDate ? formatLocalDate(order.appointmentDate) : undefined,
        installerName: order.assignedSiName,
        isUrgent: false
      });

      if (result.success) {
        const channels = [];
        if (result.smsSent) channels.push('SMS');
        if (result.whatsAppSent) channels.push('WhatsApp');
        showSuccess(`Job update sent successfully via ${channels.join(' and ')}`);
      } else {
        showError(result.errorMessage || 'Failed to send job update');
      }
    } catch (error: any) {
      showError(error.message || 'Failed to send job update');
    } finally {
      setSendingMessage(null);
    }
  };

  const handleSendSiOnTheWay = async () => {
    if (!order.customerPhone) {
      showError('Customer phone number is required');
      return;
    }

    if (!order.assignedSiName) {
      showError('Service installer is not assigned to this order');
      return;
    }

    setSendingMessage('si-on-the-way');
    try {
      const result = await sendSiOnTheWay({
        customerPhone: order.customerPhone,
        orderNumber: order.serviceId || order.uniqueId || order.id,
        installerName: order.assignedSiName,
        estimatedArrival: undefined, // Could be calculated or entered by user
        isUrgent: false
      });

      if (result.success) {
        const channels = [];
        if (result.smsSent) channels.push('SMS');
        if (result.whatsAppSent) channels.push('WhatsApp');
        showSuccess(`SI on-the-way alert sent successfully via ${channels.join(' and ')}`);
      } else {
        showError(result.errorMessage || 'Failed to send SI alert');
      }
    } catch (error: any) {
      showError(error.message || 'Failed to send SI alert');
    } finally {
      setSendingMessage(null);
    }
  };

  const loadOrderData = async () => {
    if (!orderId) return;
    try {
      setLoading(true);
      setError(null);

      const [orderData, statusLogsData, reschedulesData, blockersData, docketsData, materialsData] = await Promise.all([
        getOrder(orderId, departmentId ? { departmentId } : {}),
        getOrderStatusLogs(orderId).catch(() => []),
        getOrderReschedules(orderId).catch(() => []),
        getOrderBlockers(orderId).catch(() => []),
        getOrderDockets(orderId).catch(() => []),
        getOrderMaterials(orderId).catch(() => [])
      ]);

      const order = orderData as Order;
      
      // Load invoice if order has invoiceId
      let invoiceData = null;
      if (order.invoiceId) {
        try {
          invoiceData = await getInvoice(order.invoiceId);
        } catch (err) {
          console.error('Error loading invoice:', err);
        }
      }

      // Load building info if order has buildingId
      let buildingData = null;
      if (order.buildingId) {
        try {
          buildingData = await getBuilding(order.buildingId);
        } catch (err) {
          console.error('Error loading building:', err);
        }
      }

      const rmaData = await getRmaRequestsByOrder(orderId).catch(() => []);

      setOrder(order);
      setStatusLogs(Array.isArray(statusLogsData) ? statusLogsData : []);
      setReschedules(Array.isArray(reschedulesData) ? reschedulesData : []);
      setBlockers(Array.isArray(blockersData) ? blockersData : []);
      setDockets(Array.isArray(docketsData) ? docketsData : []);
      setMaterials(Array.isArray(materialsData) ? materialsData : []);
      setInvoice(invoiceData);
      setBuilding(buildingData);
      setRmaRequests(Array.isArray(rmaData) ? rmaData : []);

      // Load material collection check and required materials if order is assigned
      if (order.status === 'Assigned' && order.assignedSiId) {
        try {
          const [collectionCheck, requiredMaterialsData] = await Promise.all([
            checkMaterialCollection(orderId).catch(() => null),
            getRequiredMaterials(orderId).catch(() => [])
          ]);
          setMaterialCollectionCheck(collectionCheck);
          setRequiredMaterials(Array.isArray(requiredMaterialsData) ? requiredMaterialsData : []);
        } catch (err) {
          console.error('Error loading material collection check:', err);
        }
      } else if (order.status === 'Assigned') {
        // Even if no SI assigned, show required materials
        try {
          const requiredMaterialsData = await getRequiredMaterials(orderId).catch(() => []);
          setRequiredMaterials(Array.isArray(requiredMaterialsData) ? requiredMaterialsData : []);
        } catch (err) {
          console.error('Error loading required materials:', err);
        }
      }
    } catch (err) {
      const errorObj = err as { message?: string };
      setError(errorObj.message || 'Failed to load order details');
      console.error('Error loading order:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <PageShell title="Order">
        <LoadingSpinner message="Loading order details..." fullPage />
      </PageShell>
    );
  }

  if (error || !order) {
    return (
      <PageShell title="Order">
        <EmptyState
          title="Error"
          description={error || 'Order not found'}
          action={{ label: 'Back to Orders', onClick: () => navigate('/orders') }}
        />
      </PageShell>
    );
  }

  const pageTitle = order.serviceId || order.uniqueId || order.id;
  return (
    <PageShell
      title={pageTitle}
      breadcrumbs={[{ label: 'Orders', path: '/orders' }]}
      actions={
        <>
          <Button variant="ghost" size="sm" onClick={() => navigate('/orders')}>
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Orders
          </Button>
          <OrderStatusBadge status={order.status} />
          <WorkflowTransitionButton
            entityType="Order"
            entityId={orderId!}
            currentStatus={order.status}
            onTransitionExecuted={loadOrderData}
          />
        </>
      }
    >
      {order.partnerOrderId && (
        <p className="text-sm text-muted-foreground mb-4">{order.partnerOrderId}</p>
      )}

      {/* Tabs */}
      <Tabs
        defaultActiveTab={activeTab}
        onTabChange={(index: number) => setActiveTab(index)}
        className="mb-4 md:mb-6"
      >
        <TabPanel label="Details" icon={<FileText className="h-4 w-4" />}>
          <Card className="p-3 md:p-4 lg:p-6">
            <div className="flex flex-col gap-8">
              {/* Customer Information */}
              <div className="pb-8 border-b">
                <div className="flex items-center justify-between mb-4">
                  <h2 className="text-xl font-semibold">Customer Information</h2>
                  {order.customerPhone && (
                    <div className="flex items-center gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleSendJobUpdate}
                        disabled={sendingMessage !== null}
                        className="flex items-center gap-2"
                      >
                        {sendingMessage === 'job-update' ? (
                          <LoadingSpinner size="sm" />
                        ) : (
                          <MessageSquare className="h-4 w-4" />
                        )}
                        Send Job Update
                      </Button>
                      {order.assignedSiName && (order.status === 'Assigned' || order.status === 'OnTheWay') && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={handleSendSiOnTheWay}
                          disabled={sendingMessage !== null}
                          className="flex items-center gap-2"
                        >
                          {sendingMessage === 'si-on-the-way' ? (
                            <LoadingSpinner size="sm" />
                          ) : (
                            <Send className="h-4 w-4" />
                          )}
                          SI On The Way
                        </Button>
                      )}
                    </div>
                  )}
                </div>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Customer Name:</span>
                    <span className="text-base">{order.customerName || 'N/A'}</span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Phone:</span>
                    <span className="text-base">{order.customerPhone || 'N/A'}</span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Email:</span>
                    <span className="text-base">{order.customerEmail || 'N/A'}</span>
                  </div>
                </div>
              </div>

              {/* Order Information */}
              <div className="pb-8 border-b">
                <h2 className="text-xl font-semibold mb-4">Order Information</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Type:</span>
                    <span className="text-base">{order.orderType || order.partnerOrderType || 'N/A'}</span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Status:</span>
                    <OrderStatusBadge status={order.status} />
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Created:</span>
                    <span className="text-base">
                      {order.createdAt ? formatLocalDateTime(order.createdAt) : 'N/A'}
                    </span>
                  </div>
                </div>
                {/* Technical Details - Partner–Category, Installation Type, Method (from backend DTOs) */}
                <div className="mt-4 pt-4 border-t border-border">
                  <h3 className="text-sm font-medium text-muted-foreground mb-2">Technical Details</h3>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="flex flex-col gap-1">
                      <span className="text-xs text-muted-foreground font-medium">Partner–Category:</span>
                      <span className="text-base">{order.derivedPartnerCategoryLabel || order.partnerName || '—'}</span>
                    </div>
                    <div className="flex flex-col gap-1">
                      <span className="text-xs text-muted-foreground font-medium">Installation Type (Order Category):</span>
                      <span className="text-base">{order.orderCategoryCode || '—'}</span>
                    </div>
                    <div className="flex flex-col gap-1">
                      <span className="text-xs text-muted-foreground font-medium">Installation Method:</span>
                      <span className="text-base">{order.installationMethodName || '—'}</span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Address */}
              <div className="pb-8 border-b">
                <h2 className="text-xl font-semibold mb-4">Address</h2>
                <div className="flex flex-col gap-1">
                  <span className="text-sm text-muted-foreground font-medium">Address:</span>
                  <span className="text-base">{order.address || order.fullAddress || 'N/A'}</span>
                </div>
              </div>

              {/* Material Collection Alert - Show when materials are missing */}
              {order.status === 'Assigned' && materialCollectionCheck?.requiresCollection && (
                <div className="pb-8 border-b">
                  <Card className="p-4 bg-yellow-50 border-yellow-200 border-2">
                    <div className="flex items-start gap-3">
                      <AlertTriangle className="h-6 w-6 text-yellow-600 flex-shrink-0 mt-0.5" />
                      <div className="flex-1">
                        <h3 className="font-semibold text-yellow-900 mb-2">Materials Required for Collection</h3>
                        <p className="text-sm text-yellow-800 mb-3">
                          {materialCollectionCheck.message}
                        </p>
                        <div className="space-y-2">
                          {materialCollectionCheck.missingMaterials?.map((material: any) => (
                            <div key={material.materialId} className="bg-white rounded p-2 text-sm">
                              <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                  <Package className="h-4 w-4 text-yellow-600" />
                                  <span className="font-medium">{material.materialName}</span>
                                  <span className="text-muted-foreground">({material.materialCode})</span>
                                </div>
                                <div className="text-right">
                                  <span className="text-yellow-700 font-medium">
                                    Need: {material.missingQuantity} {material.unitOfMeasure}
                                  </span>
                                  <span className="text-muted-foreground text-xs block">
                                    Have: {material.availableQuantity} / Required: {material.requiredQuantity}
                                  </span>
                                </div>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    </div>
                  </Card>
                </div>
              )}

              {/* RMA Section - Show for Assurance orders or orders with faulty materials */}
              {(order.orderType?.toLowerCase().includes('assurance') || rmaRequests.length > 0) && (
                <div className="pb-8 border-b">
                  <div className="flex items-center justify-between mb-4">
                    <h2 className="text-xl font-semibold flex items-center gap-2">
                      <AlertTriangle className="h-5 w-5" />
                      RMA Requests
                    </h2>
                    {order.orderType?.toLowerCase().includes('assurance') && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setShowCreateRmaModal(true)}
                      >
                        Create RMA
                      </Button>
                    )}
                  </div>
                  {rmaRequests.length > 0 ? (
                    <div className="space-y-2">
                      {rmaRequests.map((rma: any) => (
                        <div key={rma.id} className="p-3 bg-muted rounded border">
                          <div className="flex justify-between items-center">
                            <div>
                              <p className="font-medium">RMA #{rma.id.slice(0, 8)}</p>
                              <p className="text-sm text-muted-foreground">{rma.reason}</p>
                            </div>
                            <span className={`text-xs px-2 py-1 rounded-full ${
                              rma.status === 'Approved' ? 'bg-green-100 text-green-700' :
                              rma.status === 'Pending' ? 'bg-yellow-100 text-yellow-700' :
                              'bg-gray-100 text-gray-700'
                            }`}>
                              {rma.status}
                            </span>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground">No RMA requests yet.</p>
                  )}
                </div>
              )}

              {/* Incomplete Replacements Warning Banner */}
              {(() => {
                if (!order.materialReplacements || order.materialReplacements.length === 0 || dismissedReplacementWarning) {
                  return null;
                }

                const incompleteReplacements: IncompleteReplacement[] = order.materialReplacements
                  .filter((replacement: OrderMaterialReplacement) => isReplacementIncomplete(replacement))
                  .map((replacement: OrderMaterialReplacement) => ({
                    id: replacement.id,
                    oldMaterialName: replacement.oldMaterialName,
                    oldSerialNumber: replacement.oldSerialNumber,
                    newMaterialName: replacement.newMaterialName,
                    newSerialNumber: replacement.newSerialNumber,
                    replacementReason: replacement.replacementReason,
                    approvedBy: replacement.approvedBy,
                    approvalNotes: replacement.approvalNotes,
                    approvedAt: replacement.approvedAt,
                    recordedAt: replacement.recordedAt,
                    issues: getReplacementIssues(replacement)
                  }));

                if (incompleteReplacements.length === 0) {
                  return null;
                }

                return (
                  <div className="pb-8 border-b">
                    <ReplacementWarningBanner
                      incompleteReplacements={incompleteReplacements}
                      onCompleteReplacement={(replacementId) => {
                        // Navigate to Materials tab (index 5) and scroll to replacement section
                        setActiveTab(5);
                        setTimeout(() => {
                          const replacementElement = document.getElementById(`replacement-${replacementId}`);
                          if (replacementElement) {
                            replacementElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
                            replacementElement.classList.add('ring-2', 'ring-blue-500', 'ring-offset-2');
                            setTimeout(() => {
                              replacementElement.classList.remove('ring-2', 'ring-blue-500', 'ring-offset-2');
                            }, 3000);
                          }
                        }, 100);
                      }}
                      onDismiss={() => setDismissedReplacementWarning(true)}
                    />
                  </div>
                );
              })()}

              {/* Required Materials - Always show for Assigned orders */}
              {order.status === 'Assigned' && (
                <div className="pb-8 border-b">
                  <h2 className="text-xl font-semibold mb-4 flex items-center gap-2">
                    <Package className="h-5 w-5" />
                    Required Materials
                  </h2>
                  {requiredMaterials && requiredMaterials.length > 0 ? (
                    <div className="overflow-x-auto">
                      <table className="min-w-full text-sm">
                        <thead>
                          <tr className="text-left text-muted-foreground border-b">
                            <th className="py-2 pr-4">Material</th>
                            <th className="py-2 pr-4">Code</th>
                            <th className="py-2 pr-4">Quantity</th>
                            <th className="py-2">Type</th>
                          </tr>
                        </thead>
                        <tbody>
                          {requiredMaterials.map((material: any) => (
                            <tr key={material.materialId} className="border-b last:border-none">
                              <td className="py-2 pr-4 font-medium">{material.materialName}</td>
                              <td className="py-2 pr-4 text-muted-foreground">{material.materialCode}</td>
                              <td className="py-2 pr-4">
                                {material.quantity} {material.unitOfMeasure}
                              </td>
                              <td className="py-2">
                                {material.isSerialised ? (
                                  <span className="text-xs px-2 py-1 rounded-full bg-blue-100 text-blue-700">
                                    Serialised
                                  </span>
                                ) : (
                                  <span className="text-xs px-2 py-1 rounded-full bg-gray-100 text-gray-700">
                                    Non-Serialised
                                  </span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <div className="text-sm text-muted-foreground py-4">
                      No material template configured for this order type.
                    </div>
                  )}
                </div>
              )}

              {/* Parser-origin: unmatched materials audit warning + Match Material */}
              {(order.unmatchedParsedMaterialCount ?? 0) > 0 && (
                <div className="rounded-md border border-amber-500/60 bg-amber-500/10 p-3 text-sm text-amber-800 dark:text-amber-200 mb-6">
                  <span className="font-medium">⚠ {order.unmatchedParsedMaterialCount} parsed material(s) could not be matched to Material master</span>
                  {Array.isArray(order.unmatchedParsedMaterialNames) && order.unmatchedParsedMaterialNames.length > 0 && (
                    <ul className="mt-1 list-none space-y-1">
                      {order.unmatchedParsedMaterialNames.map((name, i) => (
                        <li key={i} className="flex items-center justify-between gap-2">
                          <span>{name}</span>
                          <Button
                            type="button"
                            size="sm"
                            variant="outline"
                            className="shrink-0 h-7 text-xs"
                            onClick={() => { setMatchMaterialSelectedId(''); setMatchMaterialName(name); }}
                          >
                            Match Material
                          </Button>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              )}

              {/* Parsed Materials */}
              {order.parsedMaterials && order.parsedMaterials.length > 0 && (
                <div className="pb-8 border-b">
                  <h2 className="text-xl font-semibold mb-4">Materials (from Parser)</h2>
                  <div className="overflow-x-auto">
                    <table className="min-w-full text-sm">
                      <thead>
                        <tr className="text-left text-muted-foreground border-b">
                          <th className="py-2 pr-4">Item</th>
                          <th className="py-2 pr-4">Quantity</th>
                          <th className="py-2 pr-4">Action</th>
                          <th className="py-2">Notes</th>
                        </tr>
                      </thead>
                      <tbody>
                        {order.parsedMaterials.map((material) => (
                          <tr key={material.id} className="border-b last:border-none">
                            <td className="py-2 pr-4 font-medium">{material.name}</td>
                            <td className="py-2 pr-4">
                              {material.quantity != null
                                ? `${material.quantity}${material.unitOfMeasure ? ` ${material.unitOfMeasure}` : ''}`
                                : '—'}
                            </td>
                            <td className="py-2 pr-4">
                              {material.actionTag ? (
                                <span className="text-xs px-2 py-1 rounded-full bg-primary/10 text-primary font-semibold uppercase tracking-wide">
                                  {material.actionTag}
                                </span>
                              ) : (
                                '—'
                              )}
                            </td>
                            <td className="py-2 text-muted-foreground">{material.notes || '—'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {/* Appointment */}
              {order.appointmentDate && (
                <div className="pb-8 border-b">
                  <h2 className="text-xl font-semibold mb-4">Appointment</h2>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="flex flex-col gap-1">
                      <span className="text-sm text-muted-foreground font-medium">Date:</span>
                      <span className="text-base">
                        {formatLocalDate(order.appointmentDate)}
                      </span>
                    </div>
                    <div className="flex flex-col gap-1">
                      <span className="text-sm text-muted-foreground font-medium">Time:</span>
                      <span className="text-base">{order.appointmentTime || 'N/A'}</span>
                    </div>
                  </div>
                </div>
              )}

              {/* Assignment */}
              {order.assignedTo && (
                <div className="pb-8 border-b">
                  <h2 className="text-xl font-semibold mb-4">Assignment</h2>
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Assigned To:</span>
                    <span className="text-base">{order.assignedToName || order.assignedTo || 'N/A'}</span>
                  </div>
                </div>
              )}

              {/* Technical Details / Network Information */}
              {(order.onuSerialNumber || order.networkLoginId || order.networkWanIp || order.voipServiceId) && (
                <div className="pb-8 border-b">
                  <h2 className="text-xl font-semibold mb-4 flex items-center gap-2">
                    <Key className="h-5 w-5" />
                    Technical Details
                  </h2>
                  <div className="space-y-4">
                    {/* ONU Information */}
                    {(order.onuSerialNumber || onuPassword !== null) && (
                      <div className="p-4 bg-muted/50 rounded-lg border border-border">
                        <h3 className="text-sm font-semibold mb-3 text-foreground">ONU Information</h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                          {order.onuSerialNumber && (
                            <div className="flex flex-col gap-1">
                              <span className="text-xs text-muted-foreground font-medium">ONU Serial Number:</span>
                              <span className="text-sm font-mono">{order.onuSerialNumber}</span>
                            </div>
                          )}
                          <div className="flex flex-col gap-1">
                            <span className="text-xs text-muted-foreground font-medium">ONU Password:</span>
                            <div className="flex items-center gap-2">
                              {loadingOnuPassword ? (
                                <span className="text-sm text-muted-foreground">Loading...</span>
                              ) : onuPassword === null ? (
                                <Button
                                  variant="outline"
                                  size="sm"
                                  onClick={async () => {
                                    if (!orderId) return;
                                    try {
                                      setLoadingOnuPassword(true);
                                      const password = await getOnuPassword(orderId);
                                      setOnuPassword(password || '');
                                      setShowOnuPassword(true);
                                    } catch (err: any) {
                                      if (err.status === 403) {
                                        showError('You do not have permission to view ONU passwords');
                                      } else {
                                        showError(err.message || 'Failed to load ONU password');
                                      }
                                    } finally {
                                      setLoadingOnuPassword(false);
                                    }
                                  }}
                                  className="h-8"
                                >
                                  <Key className="h-3 w-3 mr-1" />
                                  View Password
                                </Button>
                              ) : (
                                <div className="flex items-center gap-2">
                                  <span className="text-sm font-mono bg-background px-2 py-1 rounded border">
                                    {showOnuPassword ? onuPassword : '••••••••'}
                                  </span>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => setShowOnuPassword(!showOnuPassword)}
                                    className="h-8 w-8 p-0"
                                  >
                                    {showOnuPassword ? (
                                      <EyeOff className="h-4 w-4" />
                                    ) : (
                                      <Eye className="h-4 w-4" />
                                    )}
                                  </Button>
                                </div>
                              )}
                            </div>
                          </div>
                        </div>
                      </div>
                    )}

                    {/* Network Information */}
                    {(order.networkLoginId || order.networkWanIp || order.networkLanIp) && (
                      <div className="p-4 bg-muted/50 rounded-lg border border-border">
                        <h3 className="text-sm font-semibold mb-3 text-foreground">Network Information</h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                          {order.networkPackage && (
                            <div className="flex flex-col gap-1">
                              <span className="text-xs text-muted-foreground font-medium">Package:</span>
                              <span className="text-sm">{order.networkPackage}</span>
                            </div>
                          )}
                          {order.networkBandwidth && (
                            <div className="flex flex-col gap-1">
                              <span className="text-xs text-muted-foreground font-medium">Bandwidth:</span>
                              <span className="text-sm">{order.networkBandwidth}</span>
                            </div>
                          )}
                          {order.networkLoginId && (
                            <div className="flex flex-col gap-1">
                              <span className="text-xs text-muted-foreground font-medium">Login ID:</span>
                              <span className="text-sm font-mono">{order.networkLoginId}</span>
                            </div>
                          )}
                          {order.networkWanIp && (
                            <div className="flex flex-col gap-1">
                              <span className="text-xs text-muted-foreground font-medium">WAN IP:</span>
                              <span className="text-sm font-mono">{order.networkWanIp}</span>
                            </div>
                          )}
                          {order.networkLanIp && (
                            <div className="flex flex-col gap-1">
                              <span className="text-xs text-muted-foreground font-medium">LAN IP:</span>
                              <span className="text-sm font-mono">{order.networkLanIp}</span>
                            </div>
                          )}
                          {order.networkGateway && (
                            <div className="flex flex-col gap-1">
                              <span className="text-xs text-muted-foreground font-medium">Gateway:</span>
                              <span className="text-sm font-mono">{order.networkGateway}</span>
                            </div>
                          )}
                        </div>
                      </div>
                    )}

                    {/* VOIP Information */}
                    {order.voipServiceId && (
                      <div className="p-4 bg-muted/50 rounded-lg border border-border">
                        <h3 className="text-sm font-semibold mb-3 text-foreground">VOIP Information</h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                          <div className="flex flex-col gap-1">
                            <span className="text-xs text-muted-foreground font-medium">VOIP Service ID:</span>
                            <span className="text-sm font-mono">{order.voipServiceId}</span>
                          </div>
                          {order.voipIpAddressOnu && (
                            <div className="flex flex-col gap-1">
                              <span className="text-xs text-muted-foreground font-medium">ONU IP Address:</span>
                              <span className="text-sm font-mono">{order.voipIpAddressOnu}</span>
                            </div>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          </Card>
        </TabPanel>

        <TabPanel label="Timeline" icon={<Clock className="h-4 w-4" />}>
          <Card className="p-3 md:p-4 lg:p-6">
            <h2 className="text-xl font-semibold mb-4">Status History</h2>
            {statusLogs.length > 0 ? (
              <div className="flex flex-col gap-6 relative pl-8">
                {statusLogs.map((log, index) => (
                  <div key={log.id || index} className="relative flex gap-4">
                    <div className="absolute -left-[1.125rem] top-1 w-3 h-3 rounded-full bg-primary border-2 border-background shadow-sm" />
                    <div className="flex-1">
                      <div className="flex justify-between items-center mb-2">
                        <span className="font-semibold">{log.status}</span>
                        <span className="text-sm text-muted-foreground">
                          {log.timestamp ? formatLocalDateTime(log.timestamp) : 'N/A'}
                        </span>
                      </div>
                      {log.notes && (
                        <p className="text-sm text-foreground my-2">{log.notes}</p>
                      )}
                      {log.changedBy && (
                        <p className="text-xs text-muted-foreground">Changed by: {log.changedBy}</p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <EmptyState title="No status history available" />
            )}
          </Card>
        </TabPanel>

        <TabPanel label={`Reschedules (${reschedules.length})`} icon={<Calendar className="h-4 w-4" />}>
          <Card className="p-3 md:p-4 lg:p-6">
            <h2 className="text-xl font-semibold mb-4">Reschedule History</h2>
            {reschedules.length > 0 ? (
              <div className="flex flex-col gap-4">
                {reschedules.map((reschedule, index) => (
                  <div key={reschedule.id || index} className="p-4 bg-muted rounded-lg border">
                    <div className="flex justify-between items-center mb-2">
                      <span className="text-sm text-muted-foreground">
                        {reschedule.requestedDate ? formatLocalDate(reschedule.requestedDate) : 'N/A'}
                      </span>
                      <span className={`text-xs px-3 py-1 rounded-full font-medium ${
                        reschedule.status?.toLowerCase() === 'approved' ? 'bg-green-100 text-green-800' :
                        reschedule.status?.toLowerCase() === 'rejected' ? 'bg-red-100 text-red-800' :
                        'bg-yellow-100 text-yellow-800'
                      }`}>
                        {reschedule.status || 'Pending'}
                      </span>
                    </div>
                    {reschedule.reason && (
                      <p className="text-sm text-foreground my-2">{reschedule.reason}</p>
                    )}
                    {reschedule.newAppointmentDate && (
                      <p className="text-xs text-muted-foreground">
                        New appointment: {formatLocalDateTime(reschedule.newAppointmentDate)}
                      </p>
                    )}
                  </div>
                ))}
              </div>
            ) : (
              <EmptyState title="No reschedules" />
            )}
          </Card>
        </TabPanel>

        <TabPanel label={`Blockers (${blockers.length})`} icon={<XCircle className="h-4 w-4" />}>
          <Card className="p-3 md:p-4 lg:p-6">
            <h2 className="text-xl font-semibold mb-4">Blockers</h2>
            {blockers.length > 0 ? (
              <div className="flex flex-col gap-4">
                {blockers.map((blocker, index) => (
                  <div key={blocker.id || index} className="p-4 bg-muted rounded-lg border">
                    <div className="flex justify-between items-center mb-2">
                      <span className="font-semibold">{blocker.type || 'Unknown'}</span>
                      <span className={`text-xs px-3 py-1 rounded-full font-medium ${
                        blocker.resolved ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'
                      }`}>
                        {blocker.resolved ? 'Resolved' : 'Active'}
                      </span>
                    </div>
                    {blocker.description && (
                      <p className="text-sm text-foreground my-2">{blocker.description}</p>
                    )}
                    {blocker.createdAt && (
                      <p className="text-xs text-muted-foreground">
                        Created: {formatLocalDateTime(blocker.createdAt)}
                      </p>
                    )}
                  </div>
                ))}
              </div>
            ) : (
              <EmptyState title="No blockers" />
            )}
          </Card>
        </TabPanel>

        <TabPanel label={`Dockets (${dockets.length})`} icon={<FileText className="h-4 w-4" />}>
          <Card className="p-3 md:p-4 lg:p-6">
            <h2 className="text-xl font-semibold mb-4">Dockets</h2>
            {dockets.length > 0 ? (
              <div className="flex flex-col gap-4">
                {dockets.map((docket, index) => (
                  <div key={docket.id || index} className="p-4 bg-muted rounded-lg border">
                    <div className="flex justify-between items-center mb-2">
                      <span className="font-semibold">{docket.docketNumber || `Docket #${index + 1}`}</span>
                      <span className="text-xs text-muted-foreground">
                        {docket.createdAt ? formatLocalDate(docket.createdAt) : 'N/A'}
                      </span>
                    </div>
                    {docket.summary && (
                      <p className="text-sm text-foreground">{docket.summary}</p>
                    )}
                  </div>
                ))}
              </div>
            ) : (
              <EmptyState title="No dockets" />
            )}
          </Card>
        </TabPanel>

        <TabPanel label="Materials & Splitter" icon={<Package className="h-4 w-4" />}>
          <Card className="p-3 md:p-4 lg:p-6">
            <h2 className="text-xl font-semibold mb-4">Materials Used</h2>
            {materials.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="min-w-full text-sm">
                  <thead>
                    <tr className="text-left text-muted-foreground border-b">
                      <th className="py-2 pr-4">Material</th>
                      <th className="py-2 pr-4">Quantity</th>
                      <th className="py-2 pr-4">Unit Cost</th>
                      <th className="py-2 pr-4">Total Cost</th>
                      <th className="py-2">Recorded At</th>
                    </tr>
                  </thead>
                  <tbody>
                    {materials.map((material: any, index: number) => (
                      <tr key={material.id || index} className="border-b last:border-none">
                        <td className="py-2 pr-4 font-medium">{material.materialName || 'N/A'}</td>
                        <td className="py-2 pr-4">{material.quantity || '—'}</td>
                        <td className="py-2 pr-4">{material.unitCost ? `RM ${parseFloat(material.unitCost).toFixed(2)}` : '—'}</td>
                        <td className="py-2 pr-4">{material.totalCost ? `RM ${parseFloat(material.totalCost).toFixed(2)}` : '—'}</td>
                        <td className="py-2 text-muted-foreground">
                          {material.recordedAt ? formatLocalDateTime(material.recordedAt) : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <EmptyState title="No materials recorded" description="Materials will appear here once they are recorded for this order" />
            )}

            {/* Material Replacements Section */}
            {order?.materialReplacements && order.materialReplacements.length > 0 && (
              <div className="mt-8">
                <h2 className="text-xl font-semibold mb-4">Material Replacements</h2>
                <div className="overflow-x-auto">
                  <table className="min-w-full text-sm">
                    <thead>
                      <tr className="text-left text-muted-foreground border-b">
                        <th className="py-2 pr-4">Old Material</th>
                        <th className="py-2 pr-4">Old Serial</th>
                        <th className="py-2 pr-4">New Material</th>
                        <th className="py-2 pr-4">New Serial</th>
                        <th className="py-2 pr-4">Reason</th>
                        <th className="py-2 pr-4">Status</th>
                        <th className="py-2">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {order.materialReplacements.map((replacement: OrderMaterialReplacement) => (
                        <tr 
                          key={replacement.id} 
                          id={`replacement-${replacement.id}`}
                          className="border-b last:border-none"
                        >
                          <td className="py-2 pr-4 font-medium">{replacement.oldMaterialName || 'N/A'}</td>
                          <td className="py-2 pr-4 font-mono text-xs">{replacement.oldSerialNumber || '—'}</td>
                          <td className="py-2 pr-4 font-medium">{replacement.newMaterialName || 'N/A'}</td>
                          <td className="py-2 pr-4 font-mono text-xs">{replacement.newSerialNumber || '—'}</td>
                          <td className="py-2 pr-4">{replacement.replacementReason || '—'}</td>
                          <td className="py-2 pr-4">
                            {replacement.approvedAt ? (
                              <span className="text-xs px-2 py-1 rounded-full bg-green-100 text-green-700">
                                Approved
                              </span>
                            ) : (
                              <span className="text-xs px-2 py-1 rounded-full bg-yellow-100 text-yellow-700">
                                Pending
                              </span>
                            )}
                          </td>
                          <td className="py-2">
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                // Navigate to RMA or replacement edit form
                                navigate(`/rma?orderId=${orderId}&replacementId=${replacement.id}`);
                              }}
                            >
                              Edit
                            </Button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {building && (building as any).splitters && (building as any).splitters.length > 0 && (
              <div className="mt-8">
                <h2 className="text-xl font-semibold mb-4">Splitter Information</h2>
                <div className="overflow-x-auto">
                  <table className="min-w-full text-sm">
                    <thead>
                      <tr className="text-left text-muted-foreground border-b">
                        <th className="py-2 pr-4">Splitter Name</th>
                        <th className="py-2 pr-4">Type</th>
                        <th className="py-2 pr-4">Ports</th>
                        <th className="py-2">Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(building as any).splitters.map((splitter: any, index: number) => (
                        <tr key={splitter.id || index} className="border-b last:border-none">
                          <td className="py-2 pr-4 font-medium">{splitter.name || 'N/A'}</td>
                          <td className="py-2 pr-4">{splitter.type || '—'}</td>
                          <td className="py-2 pr-4">{splitter.ports ? `${splitter.usedPorts || 0}/${splitter.ports}` : '—'}</td>
                          <td className="py-2">
                            <span className={`text-xs px-2 py-1 rounded-full ${
                              splitter.status === 'Active' ? 'bg-green-100 text-green-800' :
                              splitter.status === 'Full' ? 'bg-yellow-100 text-yellow-800' :
                              'bg-gray-100 text-gray-800'
                            }`}>
                              {splitter.status || '—'}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </Card>
        </TabPanel>

        <TabPanel label="Billing" icon={<DollarSign className="h-4 w-4" />}>
          <Card className="p-3 md:p-4 lg:p-6">
            <h2 className="text-xl font-semibold mb-4">Invoice Information</h2>
            {invoice ? (
              <div className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Invoice Number:</span>
                    <span className="text-base">{invoice.invoiceNumber || 'N/A'}</span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Status:</span>
                    <span className="text-base">{invoice.status || 'N/A'}</span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Amount:</span>
                    <span className="text-base">{invoice.totalAmount ? `RM ${parseFloat(invoice.totalAmount).toFixed(2)}` : 'N/A'}</span>
                  </div>
                  <div className="flex flex-col gap-1">
                    <span className="text-sm text-muted-foreground font-medium">Due Date:</span>
                    <span className="text-base">
                      {invoice.dueDate ? formatLocalDate(invoice.dueDate) : 'N/A'}
                    </span>
                  </div>
                </div>
                <div className="mt-4">
                  <Button
                    onClick={() => navigate(`/billing/invoices/${invoice.id}`)}
                  >
                    View Full Invoice
                  </Button>
                </div>
              </div>
            ) : order?.invoiceId ? (
              <EmptyState 
                title="Invoice not found" 
                description={`Invoice ID: ${order.invoiceId}`}
              />
            ) : (
              <EmptyState 
                title="No invoice" 
                description="This order does not have an associated invoice yet"
              />
            )}
          </Card>
        </TabPanel>

        <TabPanel label="Process Checklist" icon={<ListChecks className="h-4 w-4" />}>
          {order && (
            <OrderStatusChecklistDisplay
              orderId={order.id}
              statusCode={order.status}
              readonly={false}
            />
          )}
        </TabPanel>

        <TabPanel label="Notes" icon={<StickyNote className="h-4 w-4" />}>
          <Card className="p-3 md:p-4 lg:p-6">
            <h2 className="text-xl font-semibold mb-4">Order Notes</h2>
            {editingNotes ? (
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="internalNotes">Internal Notes</Label>
                  <Textarea
                    id="internalNotes"
                    value={editingNotes.internal || ''}
                    onChange={(e) => setEditingNotes({ ...editingNotes, internal: e.target.value })}
                    rows={6}
                    placeholder="Internal notes (visible to team only)..."
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="partnerNotes">Partner Notes</Label>
                  <Textarea
                    id="partnerNotes"
                    value={editingNotes.partner || ''}
                    onChange={(e) => setEditingNotes({ ...editingNotes, partner: e.target.value })}
                    rows={6}
                    placeholder="Partner notes..."
                  />
                </div>
                <div className="flex gap-2">
                  <Button
                    onClick={async () => {
                      try {
                        await updateOrder(orderId!, {
                          orderNotesInternal: editingNotes.internal,
                          partnerNotes: editingNotes.partner
                        });
                        showSuccess('Notes updated successfully');
                        setEditingNotes(null);
                        await loadOrderData();
                      } catch (err) {
                        showError((err as Error).message || 'Failed to update notes');
                      }
                    }}
                  >
                    Save Notes
                  </Button>
                  <Button
                    variant="outline"
                    onClick={() => setEditingNotes(null)}
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            ) : (
              <div className="space-y-6">
                <div>
                  <div className="flex justify-between items-center mb-2">
                    <h3 className="text-lg font-medium">Internal Notes</h3>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setEditingNotes({
                        internal: order?.orderNotesInternal || '',
                        partner: order?.partnerNotes || ''
                      })}
                    >
                      Edit
                    </Button>
                  </div>
                  <div className="p-4 bg-muted rounded-lg border min-h-[100px]">
                    {order?.orderNotesInternal ? (
                      <p className="text-sm whitespace-pre-wrap">{order.orderNotesInternal}</p>
                    ) : (
                      <p className="text-sm text-muted-foreground">No internal notes</p>
                    )}
                  </div>
                </div>
                <div>
                  <div className="flex justify-between items-center mb-2">
                    <h3 className="text-lg font-medium">Partner Notes</h3>
                  </div>
                  <div className="p-4 bg-muted rounded-lg border min-h-[100px]">
                    {order?.partnerNotes ? (
                      <p className="text-sm whitespace-pre-wrap">{order.partnerNotes}</p>
                    ) : (
                      <p className="text-sm text-muted-foreground">No partner notes</p>
                    )}
                  </div>
                </div>
              </div>
            )}
          </Card>
        </TabPanel>
      </Tabs>

      {/* Create RMA Modal */}
      {order && (
        <CreateRmaModal
          orderId={order.id}
          partnerId={order.partnerId}
          isOpen={showCreateRmaModal}
          onClose={() => setShowCreateRmaModal(false)}
          onSuccess={() => {
            // Reload RMA requests
            if (orderId) {
              getRmaRequestsByOrder(orderId)
                .then((data) => setRmaRequests(Array.isArray(data) ? data : []))
                .catch(() => {});
            }
          }}
        />
      )}

      {/* Match Material (parser-origin unmatched) — saves alias for future drafts */}
      <MatchMaterialModal
        open={matchMaterialName != null}
        parsedName={matchMaterialName ?? ''}
        selectedMaterialId={matchMaterialSelectedId}
        onSelectedMaterialIdChange={setMatchMaterialSelectedId}
        onClose={() => {
          setMatchMaterialName(null);
          setMatchMaterialSelectedId('');
        }}
        onSave={async () => {
          if (!matchMaterialName || !matchMaterialSelectedId) return;
          setMatchMaterialSaving(true);
          try {
            await createParsedMaterialAlias({ aliasText: matchMaterialName, materialId: matchMaterialSelectedId });
            showSuccess('Alias saved. Future drafts will resolve this name automatically.');
            setMatchMaterialName(null);
            setMatchMaterialSelectedId('');
          } catch (e: any) {
            showError(e?.message ?? 'Failed to save alias');
          } finally {
            setMatchMaterialSaving(false);
          }
        }}
        saving={matchMaterialSaving}
      />
    </PageShell>
  );
};

export default OrderDetailPage;

