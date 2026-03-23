import React, { useState, useEffect, useMemo } from 'react';
import { Activity, Search, Download } from 'lucide-react';
import { getSerialLifecycle, exportSerialLifecycleReport } from '../../api/inventoryReports';
import { getMaterials } from '../../api/inventory';
import { Card, Button, useToast, EmptyState, Skeleton } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { SerialLifecycleDto, SerialLifecycleEventDto } from '../../types/inventoryReports';
import type { Material } from '../../types/inventory';
import { useDepartment } from '../../contexts/DepartmentContext';
import { isForbiddenError } from '../../api/inventoryReports';

const InventorySerialLifecyclePage: React.FC = () => {
  const { showError } = useToast();
  const { departmentId } = useDepartment();
  const [lifecycles, setLifecycles] = useState<SerialLifecycleDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [accessDenied, setAccessDenied] = useState(false);
  const [materials, setMaterials] = useState<Material[]>([]);

  const [serialInput, setSerialInput] = useState('');
  const [materialId, setMaterialId] = useState<string>('');
  const [exporting, setExporting] = useState(false);

  const params = useMemo(() => ({
    serialNumber: serialInput.trim() || undefined,
    serialNumbers: serialInput.trim().includes(',') ? serialInput.trim() : undefined,
    materialId: materialId || undefined,
    departmentId: departmentId ?? undefined
  }), [serialInput, materialId, departmentId]);

  useEffect(() => {
    if (!departmentId) return;
    getMaterials({ isActive: true }).then((m) => setMaterials(Array.isArray(m) ? m : [])).catch(() => {});
  }, [departmentId]);

  const handleExport = async () => {
    const trimmed = serialInput.trim();
    if (!departmentId || !trimmed) {
      showError('Enter at least one serial number to export.');
      return;
    }
    const serials = trimmed.split(',').map((s) => s.trim()).filter(Boolean);
    if (serials.length > 50) {
      showError('Maximum 50 serial numbers per export.');
      return;
    }
    setExporting(true);
    try {
      await exportSerialLifecycleReport({
        serialNumber: serials.length === 1 ? serials[0] : undefined,
        serialNumbers: serials.length > 1 ? serials.join(',') : undefined,
        materialId: materialId || undefined,
        departmentId: departmentId ?? undefined
      });
    } catch (e) {
      showError((e as Error).message || 'Export failed');
    } finally {
      setExporting(false);
    }
  };

  const runSearch = () => {
    if (!departmentId) return;
    const trimmed = serialInput.trim();
    if (!trimmed) {
      setError('Enter at least one serial number.');
      return;
    }
    const serials = trimmed.split(',').map((s) => s.trim()).filter(Boolean);
    if (serials.length > 50) {
      setError('Maximum 50 serial numbers per request.');
      return;
    }
    setLoading(true);
    setError(null);
    setAccessDenied(false);
    getSerialLifecycle({
      ...params,
      serialNumber: serials.length === 1 ? serials[0] : undefined,
      serialNumbers: serials.length > 1 ? serials.join(',') : undefined
    })
      .then((data) => {
        setLifecycles(data?.serialLifecycles ?? []);
      })
      .catch((err: unknown) => {
        if (isForbiddenError(err)) {
          setAccessDenied(true);
          setError("You don't have access to this department.");
        } else {
          setError((err as Error).message || 'Failed to load serial lifecycle');
          showError((err as Error).message || 'Failed to load serial lifecycle');
        }
        setLifecycles([]);
      })
      .finally(() => setLoading(false));
  };

  if (!departmentId) {
    return (
      <PageShell title="Serial Lifecycle" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports', path: '/inventory/reports' }, { label: 'Serial lifecycle' }]}>
        <EmptyState title="Department required" description="Please select a department from the header to look up serial lifecycle." />
      </PageShell>
    );
  }

  if (accessDenied) {
    return (
      <PageShell title="Serial Lifecycle" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports', path: '/inventory/reports' }, { label: 'Serial lifecycle' }]}>
        <EmptyState title="Access denied" description="You don't have access to this department. Select a department you belong to from the header." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Serial Lifecycle"
      breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports', path: '/inventory/reports' }, { label: 'Serial lifecycle' }]}
      actions={
        <Button variant="outline" size="sm" onClick={handleExport} disabled={exporting || !serialInput.trim()} className="gap-1">
          <Download className="h-4 w-4" /> Export CSV
        </Button>
      }
    >
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">Received → allocated → issued → returned, with timestamps and order refs.</p>
      <Card className="p-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 items-end">
          <div className="sm:col-span-2">
            <label className="text-xs font-medium text-muted-foreground mb-1 block">Serial number(s), comma-separated (max 50)</label>
            <input
              type="text"
              value={serialInput}
              onChange={(e) => setSerialInput(e.target.value)}
              placeholder="e.g. SN001 or SN001, SN002"
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">Material (optional)</label>
            <select
              value={materialId}
              onChange={(e) => setMaterialId(e.target.value)}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            >
              <option value="">All</option>
              {materials.map((m) => (
                <option key={m.id} value={m.id}>{m.code || m.itemCode || m.id}</option>
              ))}
            </select>
          </div>
          <div>
            <Button variant="default" size="sm" onClick={runSearch} className="gap-1" disabled={loading}>
              <Search className="h-4 w-4" /> Look up
            </Button>
          </div>
        </div>
      </Card>

      {error && (
        <Card className="p-4 border-destructive/50 bg-destructive/5">
          <p className="text-destructive text-sm">{error}</p>
        </Card>
      )}

      <Card className="p-4">
        {loading ? (
          <div className="space-y-2">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-64 w-full" />
          </div>
        ) : lifecycles.length === 0 && !error ? (
          <EmptyState
            title="No serials queried"
            description="Enter one or more serial numbers and click Look up to see lifecycle events."
          />
        ) : lifecycles.length === 0 && error ? null : (
          <div className="space-y-6">
            {lifecycles.map((lc) => (
              <div key={lc.serialNumber} className="rounded-lg border p-4">
                <h3 className="font-medium mb-2">{lc.serialNumber} {lc.materialCode && `· ${lc.materialCode}`}</h3>
                {lc.events && lc.events.length > 0 ? (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2 px-2 font-medium">Date</th>
                          <th className="text-left py-2 px-2 font-medium">Type</th>
                          <th className="text-right py-2 px-2 font-medium">Qty</th>
                          <th className="text-left py-2 px-2 font-medium">Location</th>
                          <th className="text-left py-2 px-2 font-medium">Order</th>
                          <th className="text-left py-2 px-2 font-medium">Remarks</th>
                        </tr>
                      </thead>
                      <tbody>
                        {lc.events.map((ev: SerialLifecycleEventDto) => (
                          <tr key={ev.ledgerEntryId} className="border-b border-border/50">
                            <td className="py-2 px-2 text-muted-foreground">{ev.createdAt ? new Date(ev.createdAt).toLocaleString() : '-'}</td>
                            <td className="py-2 px-2">{ev.entryType}</td>
                            <td className="py-2 px-2 text-right">{ev.quantity}</td>
                            <td className="py-2 px-2">{ev.locationName ?? ev.locationId ?? '-'}</td>
                            <td className="py-2 px-2">{ev.orderReference ?? ev.orderId ?? '-'}</td>
                            <td className="py-2 px-2 text-muted-foreground max-w-[180px] truncate" title={ev.remarks ?? ''}>{ev.remarks ?? '-'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <p className="text-muted-foreground text-sm">No events for this serial in your department scope.</p>
                )}
              </div>
            ))}
          </div>
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default InventorySerialLifecyclePage;
