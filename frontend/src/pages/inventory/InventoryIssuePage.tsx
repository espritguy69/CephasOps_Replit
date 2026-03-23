import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { issueStock } from '../../api/ledger';
import { getMaterials, getStockLocations } from '../../api/inventory';
import { Card, Button, Input, Label, useToast, Switch, EmptyState } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { LedgerIssueRequest } from '../../types/ledger';
import type { Material } from '../../types/inventory';
import type { StockLocation } from '../../types/inventory';
import { useDepartment } from '../../contexts/DepartmentContext';
import type { ApiError } from '../../api/client';
import { Textarea } from '../../components/ui/textarea';

const InventoryIssuePage: React.FC = () => {
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();
  const { departmentId } = useDepartment();
  const [materials, setMaterials] = useState<Material[]>([]);
  const [locations, setLocations] = useState<StockLocation[]>([]);
  const [orderId, setOrderId] = useState('');
  const [materialId, setMaterialId] = useState('');
  const [locationId, setLocationId] = useState('');
  const [quantity, setQuantity] = useState<number>(1);
  const [remarks, setRemarks] = useState('');
  const [serialisedMode, setSerialisedMode] = useState(false);
  const [serialLines, setSerialLines] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [accessDenied, setAccessDenied] = useState(false);

  useEffect(() => {
    if (!departmentId) return;
    Promise.all([
      getMaterials({ isActive: true }).catch(() => []),
      getStockLocations().catch(() => [])
    ]).then(([mats, locs]) => {
      setMaterials(Array.isArray(mats) ? mats : []);
      setLocations(Array.isArray(locs) ? locs : []);
    });
  }, [departmentId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!departmentId) return;
    setError(null);
    setSubmitting(true);
    try {
      if (serialisedMode && serialLines.trim()) {
        const serials = serialLines.split(/\r?\n/).map((s) => s.trim()).filter(Boolean);
        for (const serial of serials) {
          const body: LedgerIssueRequest = {
            orderId,
            materialId,
            locationId,
            quantity: 1,
            remarks: remarks || undefined,
            serialNumber: serial
          };
          await issueStock(body, departmentId);
        }
        showSuccess(`Issued ${serials.length} serialised item(s).`);
      } else {
        const body: LedgerIssueRequest = {
          orderId,
          materialId,
          locationId,
          quantity,
          remarks: remarks || undefined
        };
        await issueStock(body, departmentId);
        showSuccess('Stock issued successfully.');
      }
      navigate('/inventory/stock-summary');
    } catch (err: unknown) {
      const apiErr = err as ApiError;
      if (apiErr?.status === 403) {
        setAccessDenied(true);
        setError("You don't have access to this department.");
      } else {
        const msg = (err as Error).message || 'Failed to issue stock';
        setError(msg);
        showError(msg);
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (!departmentId) {
    return (
      <PageShell title="Issue Stock" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Issue' }]}>
        <EmptyState
          title="Department required"
          description="Please select a department from the header to issue stock."
        />
      </PageShell>
    );
  }

  if (accessDenied) {
    return (
      <PageShell title="Issue Stock" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Issue' }]}>
        <EmptyState
          title="Access denied"
          description="You don't have access to this department. Select a department you belong to from the header."
        />
      </PageShell>
    );
  }

  return (
    <PageShell title="Issue Stock" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Issue' }]}>
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">Issue stock to an order (e.g. for fulfilment).</p>
        <Card className="p-6 max-w-xl">
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="p-3 rounded-md bg-destructive/10 text-destructive text-sm">{error}</div>
          )}
          <div>
            <Label htmlFor="orderId">Order ID</Label>
            <Input
              id="orderId"
              value={orderId}
              onChange={(e) => setOrderId(e.target.value)}
              placeholder="Order GUID or identifier"
              required
              className="mt-1"
            />
          </div>
          <div>
            <Label htmlFor="materialId">Material</Label>
            <select
              id="materialId"
              value={materialId}
              onChange={(e) => setMaterialId(e.target.value)}
              required
              className="mt-1 h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            >
              <option value="">Select material</option>
              {materials.map((m) => (
                <option key={m.id} value={m.id}>{m.code || m.itemCode || m.id}</option>
              ))}
            </select>
          </div>
          <div>
            <Label htmlFor="locationId">Location</Label>
            <select
              id="locationId"
              value={locationId}
              onChange={(e) => setLocationId(e.target.value)}
              required
              className="mt-1 h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            >
              <option value="">Select location</option>
              {locations.map((loc) => (
                <option key={loc.id} value={loc.id}>{loc.name}</option>
              ))}
            </select>
          </div>
          <div className="flex items-center gap-2">
            <Switch checked={serialisedMode} onCheckedChange={setSerialisedMode} />
            <Label>Serialised (enter serial numbers)</Label>
          </div>
          {!serialisedMode ? (
            <div>
              <Label htmlFor="quantity">Quantity</Label>
              <Input
                id="quantity"
                type="number"
                min={1}
                value={quantity}
                onChange={(e) => setQuantity(Number(e.target.value) || 1)}
                className="mt-1"
              />
            </div>
          ) : (
            <div>
              <Label htmlFor="serials">Serial numbers (one per line)</Label>
              <Textarea
                id="serials"
                value={serialLines}
                onChange={(e) => setSerialLines(e.target.value)}
                placeholder="SN001&#10;SN002"
                rows={4}
                className="mt-1 font-mono text-sm"
              />
            </div>
          )}
          <div>
            <Label htmlFor="remarks">Notes (optional)</Label>
            <Input
              id="remarks"
              value={remarks}
              onChange={(e) => setRemarks(e.target.value)}
              className="mt-1"
            />
          </div>
          <div className="flex gap-2 pt-2">
            <Button type="submit" disabled={submitting || !orderId || !materialId || !locationId || (serialisedMode ? !serialLines.trim() : false)}>
              {submitting ? 'Submitting…' : 'Issue'}
            </Button>
            <Button type="button" variant="outline" onClick={() => navigate('/inventory/stock-summary')}>
              Cancel
            </Button>
          </div>
        </form>
      </Card>
      </div>
    </PageShell>
  );
};

export default InventoryIssuePage;
