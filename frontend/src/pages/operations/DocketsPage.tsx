import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { FileText, CheckCircle, Upload, AlertTriangle, XCircle } from 'lucide-react';
import { getOrdersPaged, changeOrderStatus } from '../../api/orders';
import { uploadFile } from '../../api/files';
import { LoadingSpinner, EmptyState, Button, Card, useToast, Modal, Textarea, Label } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Order } from '../../types/orders';

const DOCKET_STATUSES = ['DocketsReceived', 'DocketsVerified', 'DocketsRejected'];

/**
 * Dockets Admin UI - Ops-first docket receive/verify/upload flow.
 * Route: /operations/dockets
 * Functions: List orders in docket phase, verify checklist, transition status, upload docket file.
 */
const DocketsPage: React.FC = () => {
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [transitioning, setTransitioning] = useState<string | null>(null);
  const [uploading, setUploading] = useState<string | null>(null);
  const [filterStatus, setFilterStatus] = useState<string>('DocketsReceived');
  const [rejectModalOrder, setRejectModalOrder] = useState<Order | null>(null);
  const [rejectReason, setRejectReason] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);
  const pendingUploadOrderId = useRef<string | null>(null);

  useEffect(() => {
    loadOrders();
  }, [filterStatus]);

  const loadOrders = async () => {
    setLoading(true);
    try {
      const result = await getOrdersPaged({
        status: filterStatus,
        pageSize: 100
      });
      setOrders(result.items || []);
    } catch (err) {
      showError('Failed to load orders');
      setOrders([]);
    } finally {
      setLoading(false);
    }
  };

  const handleTransition = async (orderId: string, toStatus: string) => {
    setTransitioning(orderId);
    try {
      await changeOrderStatus(orderId, toStatus, 'Docket admin action');
      showSuccess(`Order moved to ${toStatus.replace(/([A-Z])/g, ' $1').trim()}`);
      loadOrders();
    } catch (err: any) {
      showError(err?.message || 'Failed to change status');
    } finally {
      setTransitioning(null);
    }
  };

  const handleReject = async () => {
    if (!rejectModalOrder || !rejectReason.trim()) return;
    setTransitioning(rejectModalOrder.id);
    try {
      await changeOrderStatus(rejectModalOrder.id, 'DocketsRejected', rejectReason.trim());
      showSuccess('Docket rejected. SI will be notified to correct and resubmit.');
      setRejectModalOrder(null);
      setRejectReason('');
      loadOrders();
    } catch (err: any) {
      showError(err?.message || 'Failed to reject docket');
    } finally {
      setTransitioning(null);
    }
  };

  const handleUploadDocket = async (orderId: string, file: File) => {
    setUploading(orderId);
    try {
      await uploadFile(file, {
        module: 'Orders',
        entityId: orderId,
        entityType: 'Order'
      });
      showSuccess('Docket file uploaded');
      loadOrders();
    } catch (err: any) {
      showError(err?.message || 'Failed to upload file');
    } finally {
      setUploading(null);
    }
  };

  const getChecklistStatus = (order: Order) => {
    const hasSplitter = !!(order.splitterNumber || order.splitterId);
    const hasPort = !!order.splitterPort;
    const hasOnu = !!order.onuSerialNumber;
    const hasPhotos = order.photosUploaded === true;
    const complete = hasSplitter && hasPort && hasOnu && hasPhotos;
    return { hasSplitter, hasPort, hasOnu, hasPhotos, complete };
  };

  return (
    <PageShell
      title="Docket Management"
      breadcrumbs={[
        { label: 'Operations', path: '/orders' },
        { label: 'Dockets', path: '/operations/dockets' }
      ]}
    >
      <div className="space-y-4">
        <Card className="p-4">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-medium text-muted-foreground">Status:</span>
            <div className="flex gap-2">
              {DOCKET_STATUSES.map((s) => (
                <Button
                  key={s}
                  variant={filterStatus === s ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => setFilterStatus(s)}
                >
                  {s === 'DocketsReceived' ? 'Received' : s === 'DocketsVerified' ? 'Verified' : 'Rejected'}
                </Button>
              ))}
            </div>
          </div>
        </Card>

        {loading ? (
          <div className="flex justify-center py-12">
            <LoadingSpinner />
          </div>
        ) : orders.length === 0 ? (
          <EmptyState
            icon={FileText}
            title="No orders in docket phase"
            description={`No orders with status "${filterStatus}". Orders move here after SI completes the job.`}
          />
        ) : (
          <div className="space-y-3">
            {orders.map((order) => {
              const checklist = getChecklistStatus(order);
              return (
                <Card key={order.id} className="p-4">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-2">
                        <button
                          onClick={() => navigate(`/orders/${order.id}`)}
                          className="font-medium text-primary hover:underline"
                        >
                          {order.serviceId || order.ticketId || order.id}
                        </button>
                        <span className="text-sm text-muted-foreground">
                          {order.customerName} · {order.derivedPartnerCategoryLabel || order.partnerName || 'N/A'}
                        </span>
                      </div>
                      <div className="flex flex-wrap gap-4 text-sm">
                        <span title="Splitter">
                          {checklist.hasSplitter ? (
                            <CheckCircle className="inline h-4 w-4 text-green-600" />
                          ) : (
                            <AlertTriangle className="inline h-4 w-4 text-amber-500" />
                          )}{' '}
                          Splitter
                        </span>
                        <span title="Port">
                          {checklist.hasPort ? (
                            <CheckCircle className="inline h-4 w-4 text-green-600" />
                          ) : (
                            <AlertTriangle className="inline h-4 w-4 text-amber-500" />
                          )}{' '}
                          Port
                        </span>
                        <span title="ONU">
                          {checklist.hasOnu ? (
                            <CheckCircle className="inline h-4 w-4 text-green-600" />
                          ) : (
                            <AlertTriangle className="inline h-4 w-4 text-amber-500" />
                          )}{' '}
                          ONU
                        </span>
                        <span title="Photos">
                          {checklist.hasPhotos ? (
                            <CheckCircle className="inline h-4 w-4 text-green-600" />
                          ) : (
                            <AlertTriangle className="inline h-4 w-4 text-amber-500" />
                          )}{' '}
                          Photos
                        </span>
                      </div>
                    </div>
                    <div className="flex flex-wrap items-center gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        disabled={!!uploading}
                        onClick={() => {
                          pendingUploadOrderId.current = order.id;
                          fileInputRef.current?.click();
                        }}
                      >
                        {uploading === order.id ? <LoadingSpinner size="sm" /> : <Upload className="h-4 w-4 mr-1" />}
                        Upload docket
                      </Button>
                      {order.status === 'DocketsReceived' && (
                        <>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setRejectModalOrder(order);
                              setRejectReason('');
                            }}
                            disabled={!!transitioning}
                          >
                            {transitioning === order.id ? <LoadingSpinner size="sm" /> : <><XCircle className="h-4 w-4 mr-1" /> Reject</>}
                          </Button>
                          <Button
                            size="sm"
                            onClick={() => handleTransition(order.id, 'DocketsVerified')}
                            disabled={!!transitioning || !checklist.complete}
                            title={!checklist.complete ? 'Complete splitter, port, ONU, photos before verify' : ''}
                          >
                            {transitioning === order.id ? <LoadingSpinner size="sm" /> : 'Verify'}
                          </Button>
                        </>
                      )}
                      {order.status === 'DocketsRejected' && (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleTransition(order.id, 'DocketsReceived')}
                          disabled={!!transitioning}
                          title="Accept corrected docket from SI"
                        >
                          {transitioning === order.id ? <LoadingSpinner size="sm" /> : 'Accept corrected'}
                        </Button>
                      )}
                      {order.status === 'DocketsVerified' && (
                        <Button
                          size="sm"
                          onClick={() => handleTransition(order.id, 'DocketsUploaded')}
                          disabled={!!transitioning}
                        >
                          {transitioning === order.id ? <LoadingSpinner size="sm" /> : 'Mark uploaded'}
                        </Button>
                      )}
                    </div>
                  </div>
                </Card>
              );
            })}
          </div>
        )}
      </div>
      <input
        ref={fileInputRef}
        type="file"
        accept=".pdf,image/*"
        className="hidden"
        onChange={(e) => {
          const f = e.target.files?.[0];
          const orderId = pendingUploadOrderId.current;
          e.target.value = '';
          pendingUploadOrderId.current = null;
          if (f && orderId) handleUploadDocket(orderId, f);
        }}
      />
      {rejectModalOrder && (
        <Modal
          isOpen={!!rejectModalOrder}
          onClose={() => setRejectModalOrder(null)}
          title="Reject Docket"
        >
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Rejecting docket for <strong>{rejectModalOrder.serviceId || rejectModalOrder.ticketId || rejectModalOrder.id}</strong>. 
              SI must correct and resubmit. Provide a reason (required).
            </p>
            <div>
              <Label htmlFor="reject-reason">Rejection reason *</Label>
              <Textarea
                id="reject-reason"
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
                placeholder="e.g. Wrong splitter, missing ONU serial, incorrect customer details..."
                rows={3}
                className="mt-1"
              />
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setRejectModalOrder(null)}>Cancel</Button>
              <Button onClick={handleReject} disabled={!rejectReason.trim() || transitioning === rejectModalOrder?.id}>
                {transitioning === rejectModalOrder?.id ? <LoadingSpinner size="sm" /> : 'Reject docket'}
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </PageShell>
  );
};

export default DocketsPage;
