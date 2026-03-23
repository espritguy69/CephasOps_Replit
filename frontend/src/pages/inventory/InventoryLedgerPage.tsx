import React, { useState, useEffect, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Search } from 'lucide-react';
import { getLedger } from '../../api/ledger';
import { getStockLocations } from '../../api/inventory';
import { getMaterials } from '../../api/inventory';
import { Card, Button, Input, useToast, EmptyState, Skeleton } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { LedgerEntryDto, LedgerFilterParams } from '../../types/ledger';
import type { StockLocation } from '../../types/inventory';
import type { Material } from '../../types/inventory';
import { useDepartment } from '../../contexts/DepartmentContext';
import type { ApiError } from '../../api/client';

const ENTRY_TYPES = ['Receive', 'Transfer', 'Allocate', 'Issue', 'Return', 'Adjust', 'Scrap'];

const InventoryLedgerPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const { showError } = useToast();
  const { departmentId } = useDepartment();
  const [items, setItems] = useState<LedgerEntryDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(50);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [accessDenied, setAccessDenied] = useState(false);
  const [locations, setLocations] = useState<StockLocation[]>([]);
  const [materials, setMaterials] = useState<Material[]>([]);

  const [materialId, setMaterialId] = useState<string>(searchParams.get('materialId') ?? '');
  const [locationId, setLocationId] = useState<string>(searchParams.get('locationId') ?? '');
  const [orderId, setOrderId] = useState<string>(searchParams.get('orderId') ?? '');
  const [entryType, setEntryType] = useState<string>(searchParams.get('entryType') ?? '');
  const [fromDate, setFromDate] = useState<string>(searchParams.get('fromDate') ?? '');
  const [toDate, setToDate] = useState<string>(searchParams.get('toDate') ?? '');

  const params: LedgerFilterParams = useMemo(() => ({
    departmentId: departmentId ?? undefined,
    materialId: materialId || undefined,
    locationId: locationId || undefined,
    orderId: orderId || undefined,
    entryType: entryType || undefined,
    fromDate: fromDate || undefined,
    toDate: toDate || undefined,
    page,
    pageSize
  }), [departmentId, materialId, locationId, orderId, entryType, fromDate, toDate, page, pageSize]);

  useEffect(() => {
    if (!departmentId) {
      setLoading(false);
      setError('Please select a department.');
      return;
    }
    loadLocationsAndMaterials();
  }, [departmentId]);

  useEffect(() => {
    if (!departmentId) return;
    loadLedger();
  }, [departmentId, page, pageSize, materialId, locationId, orderId, entryType, fromDate, toDate]);

  const loadLocationsAndMaterials = async () => {
    if (!departmentId) return;
    try {
      const [locs, mats] = await Promise.all([
        getStockLocations().catch(() => []),
        getMaterials({ isActive: true }).catch(() => [])
      ]);
      setLocations(Array.isArray(locs) ? locs : []);
      setMaterials(Array.isArray(mats) ? mats : []);
    } catch {
      // non-blocking
    }
  };

  const loadLedger = async () => {
    if (!departmentId) return;
    try {
      setLoading(true);
      setError(null);
      setAccessDenied(false);
      const result = await getLedger(params);
      setItems(result?.items ?? []);
      setTotalCount(result?.totalCount ?? 0);
      setPage(result?.page ?? page);
    } catch (err: unknown) {
      const apiErr = err as ApiError;
      if (apiErr?.status === 403) {
        setAccessDenied(true);
        setError("You don't have access to this department.");
      } else {
        setError((err as Error).message || 'Failed to load ledger');
        showError((err as Error).message || 'Failed to load ledger');
      }
      setItems([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = () => {
    setPage(1);
  };

  if (!departmentId) {
    return (
      <div className="p-6">
        <Card className="p-6 border-amber-500/50 bg-amber-500/5">
          <p className="text-amber-600 dark:text-amber-400">Please select a department from the header to view the ledger.</p>
        </Card>
      </div>
    );
  }

  if (accessDenied) {
    return (
      <div className="p-6">
        <Card className="p-6 border-destructive/50 bg-destructive/5">
          <p className="text-destructive font-medium">You don&apos;t have access to this department.</p>
          <p className="text-muted-foreground text-sm mt-1">Select a department you belong to from the header.</p>
        </Card>
      </div>
    );
  }

  return (
    <PageShell title="Ledger">
      <Card className="p-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-6 gap-3 items-end">
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
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">Entry type</label>
            <select
              value={entryType}
              onChange={(e) => setEntryType(e.target.value)}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            >
              <option value="">All</option>
              {ENTRY_TYPES.map((t) => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">Order ID</label>
            <Input
              value={orderId}
              onChange={(e) => setOrderId(e.target.value)}
              placeholder="Optional"
              className="h-9"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">From date</label>
            <Input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="h-9"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">To date</label>
            <Input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="h-9"
            />
          </div>
        </div>
        <div className="mt-3 flex justify-end">
          <Button variant="default" size="sm" onClick={applyFilters} className="gap-1">
            <Search className="h-4 w-4" /> Apply filters
          </Button>
        </div>
      </Card>

      {error && !accessDenied && (
        <Card className="p-4 border-destructive/50 bg-destructive/5">
          <p className="text-destructive text-sm">{error}</p>
        </Card>
      )}

      <Card className="p-4">
        {loading && !items.length ? (
          <div className="space-y-2">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-64 w-full" />
          </div>
        ) : items.length === 0 ? (
          <EmptyState
            title="No ledger entries"
            description="No entries match the current filters."
            action={{ label: 'Clear filters', onClick: () => { setMaterialId(''); setLocationId(''); setOrderId(''); setEntryType(''); setFromDate(''); setToDate(''); setPage(1); } }}
          />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-2 px-2 font-medium">Date</th>
                    <th className="text-left py-2 px-2 font-medium">Entry type</th>
                    <th className="text-left py-2 px-2 font-medium">Material</th>
                    <th className="text-left py-2 px-2 font-medium">Location</th>
                    <th className="text-right py-2 px-2 font-medium">Qty</th>
                    <th className="text-left py-2 px-2 font-medium">Order</th>
                    <th className="text-left py-2 px-2 font-medium">Notes / Reference</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((row) => (
                    <tr key={row.id} className="border-b border-border/50">
                      <td className="py-2 px-2 text-muted-foreground">{row.createdAt ? new Date(row.createdAt).toLocaleString() : '-'}</td>
                      <td className="py-2 px-2">{row.entryType}</td>
                      <td className="py-2 px-2">{row.materialCode ?? row.materialId}</td>
                      <td className="py-2 px-2">{row.locationName ?? row.locationId}</td>
                      <td className="py-2 px-2 text-right">{row.quantity}</td>
                      <td className="py-2 px-2">{row.orderId ?? '-'}</td>
                      <td className="py-2 px-2 text-muted-foreground max-w-[200px] truncate" title={row.remarks ?? row.referenceId ?? ''}>{row.remarks ?? row.referenceId ?? '-'}</td>
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
    </PageShell>
  );
};

export default InventoryLedgerPage;
