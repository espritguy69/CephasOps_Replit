import React, { useState, useEffect, useMemo } from 'react';
import { BarChart3, Search, Download } from 'lucide-react';
import { getUsageSummary, exportUsageSummaryReport } from '../../api/inventoryReports';
import { getMaterials } from '../../api/inventory';
import { getStockLocations } from '../../api/inventory';
import { Card, Button, useToast, EmptyState, Skeleton } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { UsageSummaryReportResultDto, UsageSummaryRowDto } from '../../types/inventoryReports';
import type { Material } from '../../types/inventory';
import type { StockLocation } from '../../types/inventory';
import { useDepartment } from '../../contexts/DepartmentContext';
import { isForbiddenError } from '../../api/inventoryReports';

const GROUP_BY_OPTIONS = ['Material', 'Location', 'Department'] as const;

const InventoryUsageByPeriodPage: React.FC = () => {
  const { showError } = useToast();
  const { departmentId } = useDepartment();
  const [result, setResult] = useState<UsageSummaryReportResultDto | null>(null);
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
  const [groupBy, setGroupBy] = useState<string>('');
  const [materialId, setMaterialId] = useState<string>('');
  const [locationId, setLocationId] = useState<string>('');
  const [page, setPage] = useState(1);
  const [exporting, setExporting] = useState(false);
  const pageSize = 50;

  const params = useMemo(() => ({
    fromDate,
    toDate,
    groupBy: groupBy ? (groupBy as 'Material' | 'Location' | 'Department') : undefined,
    materialId: materialId || undefined,
    locationId: locationId || undefined,
    departmentId: departmentId ?? undefined,
    page,
    pageSize
  }), [fromDate, toDate, groupBy, materialId, locationId, departmentId, page, pageSize]);

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
    getUsageSummary(params)
      .then((data) => {
        if (!cancelled) {
          setResult(data);
        }
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        if (isForbiddenError(err)) {
          setAccessDenied(true);
          setError("You don't have access to this department.");
        } else {
          setError((err as Error).message || 'Failed to load usage summary');
          showError((err as Error).message || 'Failed to load usage summary');
        }
        setResult(null);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, [departmentId, fromDate, toDate, params.fromDate, params.toDate, params.groupBy, params.materialId, params.locationId, params.page]);

  const applyFilters = () => setPage(1);

  const handleExport = async () => {
    if (!departmentId || !fromDate || !toDate) return;
    setExporting(true);
    try {
      await exportUsageSummaryReport({ fromDate, toDate, groupBy: groupBy || undefined, materialId: materialId || undefined, locationId: locationId || undefined, departmentId: departmentId ?? undefined });
    } catch (e) {
      showError((e as Error).message || 'Export failed');
    } finally {
      setExporting(false);
    }
  };

  if (!departmentId) {
    return (
      <PageShell title="Usage by Period" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports', path: '/inventory/reports' }, { label: 'Usage' }]}>
        <EmptyState title="Department required" description="Please select a department from the header to view usage by period." />
      </PageShell>
    );
  }

  if (accessDenied) {
    return (
      <PageShell title="Usage by Period" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports', path: '/inventory/reports' }, { label: 'Usage' }]}>
        <EmptyState title="Access denied" description="You don't have access to this department. Select a department you belong to from the header." />
      </PageShell>
    );
  }

  const items = result?.items ?? [];
  const totals = result?.totals;
  const totalCount = result?.totalCount ?? 0;
  const hasGroupBy = !!groupBy;

  return (
    <PageShell
      title="Usage by Period"
      breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports', path: '/inventory/reports' }, { label: 'Usage' }]}
      actions={
        <Button variant="outline" size="sm" onClick={handleExport} disabled={exporting} className="gap-1">
          <Download className="h-4 w-4" /> Export CSV
        </Button>
      }
    >
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">Received, transferred, issued, and returned totals in a date range.</p>
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
            <label className="text-xs font-medium text-muted-foreground mb-1 block">Group by</label>
            <select
              value={groupBy}
              onChange={(e) => setGroupBy(e.target.value)}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
            >
              <option value="">None (totals only)</option>
              {GROUP_BY_OPTIONS.map((o) => (
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

      {error && !accessDenied && (
        <Card className="p-4 border-destructive/50 bg-destructive/5">
          <p className="text-destructive text-sm">{error}</p>
        </Card>
      )}

      <Card className="p-4">
        {loading && !result ? (
          <div className="space-y-2">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-64 w-full" />
          </div>
        ) : hasGroupBy && items.length === 0 ? (
          <EmptyState
            title="No data"
            description="No usage data for the selected filters and date range."
            action={{ label: 'Clear filters', onClick: () => { setGroupBy(''); setMaterialId(''); setLocationId(''); setPage(1); } }}
          />
        ) : !hasGroupBy && !totals ? (
          <EmptyState
            title="No totals"
            description="No usage totals for the selected date range. The report API may not be implemented yet."
          />
        ) : (
          <>
            {!hasGroupBy && totals && (
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-4 mb-4">
                <div className="rounded-lg border p-3">
                  <p className="text-xs text-muted-foreground">Received</p>
                  <p className="text-lg font-semibold">{Number(totals.received).toLocaleString()}</p>
                </div>
                <div className="rounded-lg border p-3">
                  <p className="text-xs text-muted-foreground">Transferred</p>
                  <p className="text-lg font-semibold">{Number(totals.transferred).toLocaleString()}</p>
                </div>
                <div className="rounded-lg border p-3">
                  <p className="text-xs text-muted-foreground">Issued</p>
                  <p className="text-lg font-semibold">{Number(totals.issued).toLocaleString()}</p>
                </div>
                <div className="rounded-lg border p-3">
                  <p className="text-xs text-muted-foreground">Returned</p>
                  <p className="text-lg font-semibold">{Number(totals.returned).toLocaleString()}</p>
                </div>
                {(totals.adjusted !== undefined) && (
                  <div className="rounded-lg border p-3">
                    <p className="text-xs text-muted-foreground">Adjusted</p>
                    <p className="text-lg font-semibold">{Number(totals.adjusted).toLocaleString()}</p>
                  </div>
                )}
                {(totals.scrapped !== undefined) && (
                  <div className="rounded-lg border p-3">
                    <p className="text-xs text-muted-foreground">Scrapped</p>
                    <p className="text-lg font-semibold">{Number(totals.scrapped).toLocaleString()}</p>
                  </div>
                )}
              </div>
            )}
            {hasGroupBy && items.length > 0 && (
              <>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-2 px-2 font-medium">{groupBy}</th>
                        <th className="text-right py-2 px-2 font-medium">Received</th>
                        <th className="text-right py-2 px-2 font-medium">Transferred</th>
                        <th className="text-right py-2 px-2 font-medium">Issued</th>
                        <th className="text-right py-2 px-2 font-medium">Returned</th>
                      </tr>
                    </thead>
                    <tbody>
                      {items.map((row: UsageSummaryRowDto) => (
                        <tr key={row.keyId} className="border-b border-border/50">
                          <td className="py-2 px-2">{row.keyName ?? row.keyId}</td>
                          <td className="py-2 px-2 text-right">{Number(row.received).toLocaleString()}</td>
                          <td className="py-2 px-2 text-right">{Number(row.transferred).toLocaleString()}</td>
                          <td className="py-2 px-2 text-right">{Number(row.issued).toLocaleString()}</td>
                          <td className="py-2 px-2 text-right">{Number(row.returned).toLocaleString()}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <div className="flex items-center justify-between mt-3 pt-3 border-t text-sm text-muted-foreground">
                  <span>Total: {totalCount} · Page {result?.page ?? page} (size {pageSize})</span>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
                    <Button variant="outline" size="sm" disabled={(result?.page ?? page) * pageSize >= totalCount} onClick={() => setPage((p) => p + 1)}>Next</Button>
                  </div>
                </div>
              </>
            )}
          </>
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default InventoryUsageByPeriodPage;
