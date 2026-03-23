import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Boxes, TrendingDown, Wrench, AlertTriangle, PieChart } from 'lucide-react';
import { getAssetSummary, getUpcomingMaintenance } from '../../api/assets';
import { LoadingSpinner, EmptyState, useToast, Card } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { AssetSummary, MaintenanceRecord } from '../../types/assets';

interface ExtendedAssetSummary {
  totalAssets: number;
  activeAssets: number;
  totalPurchaseCost: number;
  totalCurrentBookValue: number;
  totalAccumulatedDepreciation: number;
  assetsUnderMaintenance: number;
  disposedAssets: number;
  assetsByType?: Array<{
    assetTypeId: string;
    assetTypeName: string;
    count: number;
    totalValue: number;
  }>;
}

interface ExtendedMaintenanceRecord extends MaintenanceRecord {
  assetName?: string;
  assetTag?: string;
  maintenanceTypeName?: string;
}

const AssetsDashboardPage: React.FC = () => {
  const { showError } = useToast();
  const [summary, setSummary] = useState<ExtendedAssetSummary | null>(null);
  const [upcomingMaintenance, setUpcomingMaintenance] = useState<ExtendedMaintenanceRecord[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async (): Promise<void> => {
    try {
      setLoading(true);
      const [summaryData, maintenanceData] = await Promise.all([
        getAssetSummary(),
        getUpcomingMaintenance(30)
      ]);
      // Transform the summary data to match our expected structure
      setSummary({
        totalAssets: (summaryData as any).totalAssets || 0,
        activeAssets: (summaryData as any).activeAssets || 0,
        totalPurchaseCost: (summaryData as any).totalPurchaseCost || (summaryData as any).totalValue || 0,
        totalCurrentBookValue: (summaryData as any).totalCurrentBookValue || 0,
        totalAccumulatedDepreciation: (summaryData as any).totalAccumulatedDepreciation || 0,
        assetsUnderMaintenance: (summaryData as any).assetsUnderMaintenance || 0,
        disposedAssets: (summaryData as any).disposedAssets || 0,
        assetsByType: (summaryData as any).assetsByType || []
      });
      setUpcomingMaintenance(Array.isArray(maintenanceData) ? maintenanceData : []);
    } catch (err: any) {
      console.error('Error loading dashboard:', err);
      showError('Failed to load asset dashboard');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount || 0);
  };

  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('en-MY', { year: 'numeric', month: 'short', day: 'numeric' });
  };

  if (loading) {
    return (
      <PageShell title="Asset Management" breadcrumbs={[{ label: 'Assets' }]}>
        <LoadingSpinner fullPage />
      </PageShell>
    );
  }
  if (!summary) {
    return (
      <PageShell title="Asset Management" breadcrumbs={[{ label: 'Assets' }]}>
        <EmptyState title="No data available" message="Asset dashboard could not be loaded." />
      </PageShell>
    );
  }

  const depreciationPercent = summary.totalPurchaseCost > 0 
    ? ((summary.totalAccumulatedDepreciation / summary.totalPurchaseCost) * 100).toFixed(1)
    : 0;

  return (
    <PageShell title="Asset Management" breadcrumbs={[{ label: 'Assets' }]}>
      <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-slate-400 text-sm">Total Assets</p>
              <p className="text-3xl font-bold text-white">{summary.totalAssets}</p>
              <p className="text-xs text-green-400">{summary.activeAssets} active</p>
            </div>
            <div className="p-3 bg-brand-500/10 rounded-lg">
              <Boxes className="h-6 w-6 text-brand-500" />
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-slate-400 text-sm">Total Value</p>
              <p className="text-2xl font-bold text-white">{formatCurrency(summary.totalPurchaseCost)}</p>
              <p className="text-xs text-slate-400">Purchase cost</p>
            </div>
            <div className="p-3 bg-blue-500/10 rounded-lg">
              <PieChart className="h-6 w-6 text-blue-500" />
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-slate-400 text-sm">Current Book Value</p>
              <p className="text-2xl font-bold text-green-500">{formatCurrency(summary.totalCurrentBookValue)}</p>
              <p className="text-xs text-slate-400">After depreciation</p>
            </div>
            <div className="p-3 bg-green-500/10 rounded-lg">
              <TrendingDown className="h-6 w-6 text-green-500" />
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-slate-400 text-sm">Depreciated</p>
              <p className="text-2xl font-bold text-orange-500">{depreciationPercent}%</p>
              <p className="text-xs text-slate-400">{formatCurrency(summary.totalAccumulatedDepreciation)}</p>
            </div>
            <div className="p-3 bg-orange-500/10 rounded-lg">
              <TrendingDown className="h-6 w-6 text-orange-500" />
            </div>
          </div>
        </Card>
      </div>

      {/* Status Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card className="p-4 bg-gradient-to-br from-slate-800 to-slate-900">
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-yellow-500/20 rounded">
              <Wrench className="h-5 w-5 text-yellow-500" />
            </div>
            <h3 className="text-lg font-semibold text-white">Under Maintenance</h3>
          </div>
          <p className="text-3xl font-bold text-yellow-500">{summary.assetsUnderMaintenance}</p>
          <p className="text-sm text-slate-400 mt-1">assets currently</p>
        </Card>

        <Card className="p-4 bg-gradient-to-br from-slate-800 to-slate-900">
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-red-500/20 rounded">
              <AlertTriangle className="h-5 w-5 text-red-500" />
            </div>
            <h3 className="text-lg font-semibold text-white">Disposed</h3>
          </div>
          <p className="text-3xl font-bold text-red-500">{summary.disposedAssets}</p>
          <p className="text-sm text-slate-400 mt-1">assets disposed</p>
        </Card>

        <Card className="p-4 bg-gradient-to-br from-slate-800 to-slate-900">
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-purple-500/20 rounded">
              <Wrench className="h-5 w-5 text-purple-500" />
            </div>
            <h3 className="text-lg font-semibold text-white">Upcoming Maintenance</h3>
          </div>
          <p className="text-3xl font-bold text-purple-500">{upcomingMaintenance.length}</p>
          <p className="text-sm text-slate-400 mt-1">in next 30 days</p>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Assets by Type */}
        <Card className="p-4">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-semibold text-white">Assets by Type</h3>
            <Link to="/assets/list" className="text-brand-400 text-sm hover:text-brand-300">View All</Link>
          </div>
          {!summary.assetsByType || summary.assetsByType.length === 0 ? (
            <p className="text-slate-400 text-sm">No assets found</p>
          ) : (
            <div className="space-y-3">
              {summary.assetsByType.map((type) => (
                <div key={type.assetTypeId} className="flex justify-between items-center py-2 border-b border-slate-700 last:border-0">
                  <div>
                    <p className="text-white font-medium">{type.assetTypeName}</p>
                    <p className="text-xs text-slate-400">{type.count} assets</p>
                  </div>
                  <span className="text-brand-400 font-semibold">{formatCurrency(type.totalValue)}</span>
                </div>
              ))}
            </div>
          )}
        </Card>

        {/* Upcoming Maintenance */}
        <Card className="p-4">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-semibold text-white flex items-center gap-2">
              <Wrench className="h-5 w-5 text-yellow-500" />
              Upcoming Maintenance
            </h3>
            <Link to="/assets/maintenance" className="text-brand-400 text-sm hover:text-brand-300">View All</Link>
          </div>
          {upcomingMaintenance.length === 0 ? (
            <p className="text-green-400 text-sm">No upcoming maintenance scheduled</p>
          ) : (
            <div className="space-y-3">
              {upcomingMaintenance.slice(0, 5).map((record) => (
                <div key={record.id} className="flex justify-between items-center py-2 border-b border-slate-700 last:border-0">
                  <div>
                    <p className="text-white font-medium">{record.assetName || 'Unknown Asset'}</p>
                    <p className="text-xs text-slate-400">{record.assetTag || ''} • {record.maintenanceTypeName || record.maintenanceType}</p>
                  </div>
                  <span className="text-yellow-400 text-sm">{formatDate(record.scheduledDate)}</span>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
      </div>
    </PageShell>
  );
};

export default AssetsDashboardPage;

