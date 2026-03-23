import React, { useState, useEffect, useMemo } from 'react';
import { TrendingUp, Search } from 'lucide-react';
import { getStockByLocationHistory } from '../../api/inventoryReports';
import { getMaterials } from '../../api/inventory';
import { getStockLocations } from '../../api/inventory';
import { Card, Button, useToast, EmptyState, Skeleton } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { StockByLocationHistoryRowDto } from '../../types/inventoryReports';
import type { Material } from '../../types/inventory';
import type { StockLocation } from '../../types/inventory';
import { useDepartment } from '../../contexts/DepartmentContext';
import { isForbiddenError } from '../../api/inventoryReports';

const SNAPSHOT_OPTIONS = ['Daily', 'Weekly', 'Monthly'] as const;

const InventoryStockTrendPage: React.FC = () => {
  const { showError } = useToast();
  const { departmentId } = useDepartment();
  const [items, setItems] = useState<StockByLocationHistoryRowDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [accessDenied, setAccessDenied] = useState(false);
  const [materials, setMaterials] = useState<Material[]>([]);
  const [locations, setLocations] = useState<StockLocation[]>([]);

  const [fromDate, setFromDate] = useState<string>(() => {
    const d = new Date();
    d.setMonth(d.getMonth() - 1);
    return d.toISOString().slice(0, 10);
  });
  const [toDate, setToDate] = useState<string>(() => new Date().toISOString().slice(0, 10));
  const [snapshotType, setSnapshotType] = useState<string>('Daily');
  const [materialId, setMaterialId] = useState<string>('');
  const [locationId, setLocationId] = useState<string>('');
  const pageSize = 50;

  const params = useMemo(() => ({
    fromDate,
    toDate,
    snapshotType: snapshotType as 'Daily' | 'Weekly' | 'Monthly',
    materialId: materialId || undefined,
    locationId: locationId || undefined,
    departmentId: departmentId ?? undefined,
    page,
    pageSize
  }), [fromDate, toDate, snapshotType, materialId, locationId, departmentId, page, pageSize]);

  useEffect(() => {
    if (!departmentId) {
      setLoading(false);
      setError('Please select a department.');
      return;
    }
    (async () => {
      try {
        const [mats, locs] = await Promise.all([
          getMaterials({ isActive: true }).catch(() => []),
          getStockLocations().catch(() => [])
        ]);
        setMaterials(Array.isArray(mats) ? mats : []);
        setLocations(Array.isArray(locs) ? locs : []);
      } catch {
        // non-blocking
      }
    })();
  }, [departmentId]);

  useEffect(() => {
    if (!departmentId || !fromDate || !toDate) return;
    let cancelled = false;
    setLoading(true);
    setError(null);
    setAccessDenied(false);
    getStockByLocationHistory(params)
      .then((data) => {
        if (!cancelled) {
          setItems(data?.items ?? []);
          setTotalCount(data?.totalCount ?? 0);
        }
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        if (isForbiddenError(err)) {
          setAccessDenied(true);
          setError("You don't have access to this department.");
        } else {
          setError((err as Error).message || 'Failed to load stock trend');
          showError((err as Error).message || 'Failed to load stock trend');
        }
        setItems([]);
        setTotalCount(0);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, [departmentId, params.fromDate, params.toDate, params.snapshotType, params.materialId, params.locationId, params.page]);

  const applyFilters = () => setPage(1);

  if (!departmentId) {
    return (
      <PageShell title="Stock Trend by Location" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports', path: '/inventory/reports' }, { label: 'Stock trend' }]}>
        <EmptyState title="Department required" description="Please select a department from the header to view stock trend." />
      </PageShell>
    );
  }

  if (accessDenied) {
    return (
      <PageShell title="Stock Trend by Location" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports', path: '/inventory/reports' }, { label: 'Stock trend' }]}>
        <EmptyState title="Access denied" description="You don't have access to this department. Select a department you belong to from the header." />
      </PageShell>
    );
  }

  return (
    <PageShell title="Stock Trend by Location" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports', path: '/inventory/reports' }, { label: 'Stock trend' }]}>
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">Daily, weekly, or monthly snapshots of quantity on hand by material and location.</p>
      <Card className="p-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3 items-end">
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">From date</label>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">To date</label>
            <input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">Period</label>
            <select
              value={snapshotType}
              onChange={(e) => setSnapshotType(e.target.value)}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            >
              {SNAPSHOT_OPTIONS.map((o) => (
                <option key={o} value={o}>{o}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">Material</label>
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
            <label className="text-xs font-medium text-muted-foreground mb-1 block">Location</label>
            <select
              value={locationId}
              onChange={(e) => setLocationId(e.target.value)}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            >
              <option value="">All</option>
              {locations.map((loc) => (
                <option key={loc.id} value={loc.id}>{loc.name}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="mt-3 flex justify-end">
          <Button variant="default" size="sm" onClick={applyFilters} className="gap-1">
            <Search className="h-4 w-4" /> Apply
          </Button>
        </div>
      </Card>

      {error && (
        <Card className="p-4 border-destructive/50 bg-destructive/5">
          <p className="text-destructive text-sm">{error}</p>
        </Card>
      )}

      <Card className="p-4">
        {loading && items.length === 0 ? (
          <div className="space-y-2">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-64 w-full" />
          </div>
        ) : items.length === 0 ? (
          <EmptyState
            title="No stock history"
            description="No snapshot data for the selected filters and date range. The report API may not be implemented yet."
            action={{ label: 'Clear filters', onClick: () => { setMaterialId(''); setLocationId(''); setPage(1); } }}
          />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-2 px-2 font-medium">Period start</th>
                    <th className="text-left py-2 px-2 font-medium">Material</th>
                    <th className="text-left py-2 px-2 font-medium">Location</th>
                    <th className="text-right py-2 px-2 font-medium">On hand</th>
                    <th className="text-right py-2 px-2 font-medium">Reserved</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((row, idx) => (
                    <tr key={`${row.periodStart}-${row.materialId}-${row.locationId}-${idx}`} className="border-b border-border/50">
                      <td className="py-2 px-2 text-muted-foreground">{row.periodStart ? new Date(row.periodStart).toLocaleDateString() : '-'}</td>
                      <td className="py-2 px-2">{row.materialCode ?? row.materialId}</td>
                      <td className="py-2 px-2">{row.locationName ?? row.locationId}</td>
                      <td className="py-2 px-2 text-right">{Number(row.quantityOnHand).toLocaleString()}</td>
                      <td className="py-2 px-2 text-right">{Number(row.quantityReserved ?? 0).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="flex items-center justify-between mt-3 pt-3 border-t text-sm text-muted-foreground">
              <span>Total: {totalCount} · Page {page} (size {pageSize})</span>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
                <Button variant="outline" size="sm" disabled={page * pageSize >= totalCount} onClick={() => setPage((p) => p + 1)}>Next</Button>
              </div>
            </div>
          </>
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default InventoryStockTrendPage;
