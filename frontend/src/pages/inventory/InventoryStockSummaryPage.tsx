import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Link2, ArrowRight } from 'lucide-react';
import { getStockSummary } from '../../api/ledger';
import { getStockLocations } from '../../api/inventory';
import { LoadingSpinner, useToast, Card, Button, EmptyState, Skeleton } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { StockByLocationDto, StockSummaryResultDto } from '../../types/ledger';
import type { StockLocation } from '../../types/inventory';
import { useDepartment } from '../../contexts/DepartmentContext';
import type { ApiError } from '../../api/client';

const InventoryStockSummaryPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();
  const { departmentId, activeDepartment } = useDepartment();
  const [data, setData] = useState<StockSummaryResultDto | null>(null);
  const [locations, setLocations] = useState<StockLocation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [accessDenied, setAccessDenied] = useState(false);
  const [locationFilter, setLocationFilter] = useState<string>('');

  useEffect(() => {
    if (!departmentId) {
      setLoading(false);
      setError('Please select a department.');
      return;
    }
    loadData();
  }, [departmentId]);

  const loadData = async () => {
    if (!departmentId) return;
    try {
      setLoading(true);
      setError(null);
      setAccessDenied(false);
      const [summaryRes, locsRes] = await Promise.all([
        getStockSummary({ departmentId, locationId: locationFilter || undefined }),
        getStockLocations().catch(() => [])
      ]);
      setData(summaryRes ?? { byLocation: [], serialisedItems: [] });
      setLocations(Array.isArray(locsRes) ? locsRes : []);
    } catch (err: unknown) {
      const apiErr = err as ApiError;
      if (apiErr?.status === 403) {
        setAccessDenied(true);
        setError("You don't have access to this department.");
      } else {
        setError((err as Error).message || 'Failed to load stock summary');
        showError((err as Error).message || 'Failed to load stock summary');
      }
      setData(null);
    } finally {
      setLoading(false);
    }
  };

  const rows = data?.byLocation ?? [];
  const filteredRows = locationFilter
    ? rows.filter((r) => r.locationId === locationFilter)
    : rows;

  const handleViewLedger = (materialId: string, locationId: string) => {
    navigate(`/inventory/ledger?materialId=${materialId}&locationId=${locationId}`);
  };

  const handleAllocate = (materialId: string, locationId: string) => {
    navigate(`/inventory/allocate?materialId=${materialId}&locationId=${locationId}`);
  };

  if (!departmentId) {
    return (
      <div data-testid="inventory-stock-summary-root" className="p-6">
        <Card className="p-6 border-amber-500/50 bg-amber-500/5">
          <p className="text-amber-600 dark:text-amber-400">Please select a department from the header to view stock summary.</p>
        </Card>
      </div>
    );
  }

  if (accessDenied) {
    return (
      <div data-testid="inventory-stock-summary-root" className="p-6">
        <Card className="p-6 border-destructive/50 bg-destructive/5">
          <p className="text-destructive font-medium">You don&apos;t have access to this department.</p>
          <p className="text-muted-foreground text-sm mt-1">Select a department you belong to from the header.</p>
        </Card>
      </div>
    );
  }

  if (loading && !data) {
    return (
      <PageShell title="Stock Summary">
        <div data-testid="inventory-stock-summary-root">
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-64 w-full" />
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Stock Summary"
      actions={
        <>
          <select
            value={locationFilter}
            onChange={(e) => setLocationFilter(e.target.value)}
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
          >
            <option value="">All locations</option>
            {locations.map((loc) => (
              <option key={loc.id} value={loc.id}>{loc.name}</option>
            ))}
          </select>
          <Button variant="outline" size="sm" onClick={loadData}>Refresh</Button>
        </>
      }
    >
      <div data-testid="inventory-stock-summary-root">
      {error && !accessDenied && (
        <Card className="p-4 border-destructive/50 bg-destructive/5">
          <p className="text-destructive text-sm">{error}</p>
        </Card>
      )}

      <Card className="p-4">
        {filteredRows.length === 0 ? (
          <EmptyState
            title="No stock"
            description="No stock on hand for the selected department and location."
            action={{ label: 'Go to Ledger', onClick: () => navigate('/inventory/ledger') }}
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2 px-2 font-medium">Material</th>
                  <th className="text-left py-2 px-2 font-medium">Location</th>
                  <th className="text-right py-2 px-2 font-medium">On hand</th>
                  <th className="text-right py-2 px-2 font-medium">Reserved</th>
                  <th className="text-right py-2 px-2 font-medium">Available</th>
                  <th className="text-center py-2 px-2 font-medium">Serialised</th>
                  <th className="text-right py-2 px-2 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredRows.map((row: StockByLocationDto) => (
                  <tr key={`${row.materialId}-${row.locationId}`} className="border-b border-border/50">
                    <td className="py-2 px-2">
                      <span className="font-medium">{row.materialCode ?? row.materialId}</span>
                      {row.materialDescription && (
                        <p className="text-xs text-muted-foreground truncate max-w-[200px]">{row.materialDescription}</p>
                      )}
                    </td>
                    <td className="py-2 px-2">{row.locationName ?? row.locationId}</td>
                    <td className="py-2 px-2 text-right">{row.quantityOnHand}</td>
                    <td className="py-2 px-2 text-right">{row.quantityReserved}</td>
                    <td className="py-2 px-2 text-right font-medium">{row.quantityAvailable}</td>
                    <td className="py-2 px-2 text-center">{row.isSerialised ? 'Yes' : 'No'}</td>
                    <td className="py-2 px-2 text-right">
                      <div className="flex justify-end gap-1">
                        <Button variant="ghost" size="sm" onClick={() => handleViewLedger(row.materialId, row.locationId)} className="gap-1">
                          <Link2 className="h-3.5 w-3.5" /> View Ledger
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => handleAllocate(row.materialId, row.locationId)} className="gap-1">
                          <ArrowRight className="h-3.5 w-3.5" /> Allocate
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {data?.serialisedItems && data.serialisedItems.length > 0 && (
        <Card className="p-4 mt-4">
          <h3 className="font-semibold mb-2">Serialised items</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2 px-2 font-medium">Serial</th>
                  <th className="text-left py-2 px-2 font-medium">Material</th>
                  <th className="text-left py-2 px-2 font-medium">Location</th>
                  <th className="text-left py-2 px-2 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {data.serialisedItems.map((s) => (
                  <tr key={s.serialisedItemId} className="border-b border-border/50">
                    <td className="py-2 px-2 font-mono">{s.serialNumber}</td>
                    <td className="py-2 px-2">{s.materialCode ?? s.materialId}</td>
                    <td className="py-2 px-2">{s.currentLocationName ?? '-'}</td>
                    <td className="py-2 px-2">{s.status}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
      </div>
    </PageShell>
  );
};

export default InventoryStockSummaryPage;
