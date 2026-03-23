import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowDownToLine } from 'lucide-react';
import { receiveStock } from '../../api/ledger';
import { getMaterials, getStockLocations } from '../../api/inventory';
import { Card, Button, Input, Label, useToast, Switch, EmptyState } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { LedgerReceiveRequest } from '../../types/ledger';
import type { Material } from '../../types/inventory';
import type { StockLocation } from '../../types/inventory';
import { useDepartment } from '../../contexts/DepartmentContext';
import type { ApiError } from '../../api/client';
import { Textarea } from '../../components/ui/textarea';

const InventoryReceivePage: React.FC = () => {
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();
  const { departmentId } = useDepartment();
  const [materials, setMaterials] = useState<Material[]>([]);
  const [locations, setLocations] = useState<StockLocation[]>([]);
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
    if (!departmentId) {
      setError('Please select a department.');
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      if (serialisedMode && serialLines.trim()) {
        const serials = serialLines.split(/\r?\n/).map((s) => s.trim()).filter(Boolean);
        for (const serial of serials) {
          const body: LedgerReceiveRequest = {
            materialId,
            locationId,
            quantity: 1,
            remarks: remarks || undefined,
            serialNumber: serial
          };
          await receiveStock(body, departmentId);
        }
        showSuccess(`Received ${serials.length} serialised item(s).`);
      } else {
        const body: LedgerReceiveRequest = {
          materialId,
          locationId,
          quantity,
          remarks: remarks || undefined
        };
        await receiveStock(body, departmentId);
        showSuccess('Stock received successfully.');
      }
      navigate('/inventory/stock-summary');
    } catch (err: unknown) {
      const apiErr = err as ApiError;
      if (apiErr?.status === 403) {
        setAccessDenied(true);
        setError("You don't have access to this department.");
      } else {
        const msg = (err as Error).message || 'Failed to receive stock';
        setError(msg);
        showError(msg);
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (!departmentId) {
    return (
      <div className="p-6">
        <Card className="p-6 border-amber-500/50 bg-amber-500/5">
          <p className="text-amber-600 dark:text-amber-400">Please select a department from the header.</p>
        </Card>
      </div>
    );
  }

  if (accessDenied) {
    return (
      <div className="p-6">
        <Card className="p-6 border-destructive/50 bg-destructive/5">
          <p className="text-destructive font-medium">You don&apos;t have access to this department.</p>
        </Card>
      </div>
    );
  }

  return (
    <PageShell title="Receive Stock" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Receive' }]}>
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        <ArrowDownToLine className="h-7 w-7 text-primary" />
        <div>
          <h1 className="text-xl font-bold">Receive Stock</h1>
          <p className="text-sm text-muted-foreground">Record stock received into a location.</p>
        </div>
      </div>

      <Card className="p-6 max-w-xl">
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="p-3 rounded-md bg-destructive/10 text-destructive text-sm">{error}</div>
          )}
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
                <option key={m.id} value={m.id}>{m.code || m.itemCode || m.id} {m.description ? `– ${m.description}` : ''}</option>
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
            <Switch
              checked={serialisedMode}
              onCheckedChange={setSerialisedMode}
            />
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
                rows={5}
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
              placeholder="Reference or notes"
              className="mt-1"
            />
          </div>
          <div className="flex gap-2 pt-2">
            <Button type="submit" disabled={submitting || !materialId || !locationId || (serialisedMode ? !serialLines.trim() : false)}>
              {submitting ? 'Submitting…' : 'Receive'}
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

export default InventoryReceivePage;
