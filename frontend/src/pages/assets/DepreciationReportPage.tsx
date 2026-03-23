import React, { useState, useEffect } from 'react';
import { TrendingDown, Calendar, DollarSign, Play, FileText, Download } from 'lucide-react';
import { 
  getDepreciationEntries, runDepreciation, postDepreciation, getAssets 
} from '../../api/assets';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, SelectInput, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { DepreciationEntry, RunDepreciationRequest, DepreciationFilters } from '../../types/assets';
import type { Asset } from '../../types/assets';

interface ExtendedDepreciationEntry extends DepreciationEntry {
  assetName?: string;
  assetTag?: string;
  assetTypeName?: string;
}

interface DepreciationSummary {
  totalDepreciation: number;
  totalEntries: number;
  periods: number;
  latestPeriod: string;
}

interface RunDepreciationForm {
  period: string;
  preview: boolean;
}

interface PeriodData {
  entries: ExtendedDepreciationEntry[];
  totalDepreciation: number;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const DepreciationReportPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [entries, setEntries] = useState<ExtendedDepreciationEntry[]>([]);
  const [summary, setSummary] = useState<DepreciationSummary | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [showRunModal, setShowRunModal] = useState<boolean>(false);
  const [runningDepreciation, setRunningDepreciation] = useState<boolean>(false);
  const [periodFilter, setPeriodFilter] = useState<string>('');
  const [assetFilter, setAssetFilter] = useState<string>('');
  const [assets, setAssets] = useState<Asset[]>([]);

  const currentPeriod = new Date().toISOString().slice(0, 7); // YYYY-MM

  const [runForm, setRunForm] = useState<RunDepreciationForm>({
    period: currentPeriod,
    preview: true
  });

  useEffect(() => {
    loadData();
  }, [periodFilter, assetFilter]);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const params: DepreciationFilters = {};
      if (periodFilter) params.period = periodFilter;
      if (assetFilter) params.assetId = assetFilter;

      const [entriesData, assetsData] = await Promise.all([
        getDepreciationEntries(params),
        getAssets({ status: 'Active' as any })
      ]);
      
      const entriesList = Array.isArray(entriesData) ? entriesData : ((entriesData as any)?.entries || []);
      setEntries(entriesList);
      setAssets(Array.isArray(assetsData) ? assetsData : []);
      
      // Calculate summary from entries
      if (entriesList.length > 0) {
        const totalDepreciation = entriesList.reduce((sum: number, e: ExtendedDepreciationEntry) => sum + (e.amount || 0), 0);
        const periods = [...new Set(entriesList.map((e: ExtendedDepreciationEntry) => e.period))];
        setSummary({
          totalDepreciation,
          totalEntries: entriesList.length,
          periods: periods.length,
          latestPeriod: periods.sort().pop() || '-'
        });
      } else {
        setSummary(null);
      }
    } catch (err: any) {
      console.error('Error loading data:', err);
      showError('Failed to load depreciation data');
    } finally {
      setLoading(false);
    }
  };

  const handleRunDepreciation = async (): Promise<void> => {
    try {
      setRunningDepreciation(true);
      const request: RunDepreciationRequest = {
        period: runForm.period,
        assetIds: undefined
      };
      const result = await runDepreciation(request);
      
      if (runForm.preview) {
        showSuccess(`Preview complete: ${(result as any).entriesCount || 0} entries would be created`);
      } else {
        showSuccess(`Depreciation run complete: ${(result as any).entriesCount || 0} entries created`);
        loadData();
      }
    } catch (err: any) {
      showError(err.message || 'Failed to run depreciation');
    } finally {
      setRunningDepreciation(false);
    }
  };

  const handlePostDepreciation = async (): Promise<void> => {
    if (!window.confirm(`Are you sure you want to post depreciation for ${periodFilter || currentPeriod}? This action cannot be undone.`)) return;
    
    try {
      await postDepreciation(periodFilter || currentPeriod);
      showSuccess('Depreciation posted successfully');
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to post depreciation');
    }
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount || 0);
  };

  const formatPeriod = (period: string | null | undefined): string => {
    if (!period) return '-';
    const [year, month] = period.split('-');
    const date = new Date(parseInt(year), parseInt(month) - 1);
    return date.toLocaleDateString('en-MY', { year: 'numeric', month: 'long' });
  };

  // Generate period options (last 24 months)
  const getPeriodOptions = (): Array<{ value: string; label: string }> => {
    const options = [{ value: '', label: 'All Periods' }];
    const now = new Date();
    for (let i = 0; i < 24; i++) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const period = d.toISOString().slice(0, 7);
      options.push({ value: period, label: formatPeriod(period) });
    }
    return options;
  };

  const assetOptions = [
    { value: '', label: 'All Assets' },
    ...assets.map(a => ({ value: a.id, label: `${(a as any).assetTag || a.assetNumber} - ${a.name}` }))
  ];

  if (loading) {
    return (
      <PageShell title="Depreciation Report" breadcrumbs={[{ label: 'Assets', path: '/assets' }, { label: 'Depreciation Report' }]}>
        <LoadingSpinner fullPage />
      </PageShell>
    );
  }

  const columns: TableColumn<ExtendedDepreciationEntry>[] = [
    {
      key: 'assetName',
      label: 'Asset',
      render: (v: unknown, row: ExtendedDepreciationEntry) => (
        <div>
          <span className="text-white font-medium">{v as string || 'Unknown'}</span>
          <p className="text-xs text-slate-400">{row.assetTag || ''}</p>
        </div>
      )
    },
    { key: 'assetTypeName', label: 'Type' },
    { key: 'period', label: 'Period', render: (v: unknown) => formatPeriod(v as string) },
    { 
      key: 'amount', 
      label: 'Depreciation',
      render: (v: unknown) => <span className="text-red-400">{formatCurrency(v as number)}</span>
    },
    { 
      key: 'accumulatedDepreciation', 
      label: 'Accumulated',
      render: (v: unknown) => <span className="text-orange-400">{formatCurrency(v as number)}</span>
    },
    { 
      key: 'bookValue', 
      label: 'Book Value',
      render: (v: unknown) => <span className="text-green-400">{formatCurrency(v as number)}</span>
    },
    {
      key: 'isPosted',
      label: 'Status',
      render: (v: unknown) => (
        <span className={`px-2 py-1 text-xs rounded ${v ? 'bg-green-600' : 'bg-yellow-600'} text-white`}>
          {v ? 'Posted' : 'Draft'}
        </span>
      )
    }
  ];

  // Group entries by period for summary view
  const entriesByPeriod = entries.reduce((acc: Record<string, PeriodData>, entry: ExtendedDepreciationEntry) => {
    const period = entry.period || 'Unknown';
    if (!acc[period]) {
      acc[period] = { entries: [], totalDepreciation: 0 };
    }
    acc[period].entries.push(entry);
    acc[period].totalDepreciation += entry.amount || 0;
    return acc;
  }, {});

  return (
    <PageShell title="Depreciation Report" breadcrumbs={[{ label: 'Assets', path: '/assets' }, { label: 'Depreciation Report' }]}>
    <div className="p-6 space-y-6">
      <div className="flex justify-between items-center">
        <div className="flex items-center gap-3">
          <TrendingDown className="h-6 w-6 text-brand-500" />
          <h1 className="text-2xl font-bold text-white">Depreciation Report</h1>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => setShowRunModal(true)}>
            <Play className="h-4 w-4 mr-2" />
            Run Depreciation
          </Button>
          {entries.some(e => !e.isPosted) && (
            <Button onClick={handlePostDepreciation}>
              <FileText className="h-4 w-4 mr-2" />
              Post to GL
            </Button>
          )}
        </div>
      </div>

      {/* Summary Cards */}
      {summary && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <Card className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-red-500/20 rounded">
                <TrendingDown className="h-5 w-5 text-red-500" />
              </div>
              <div>
                <p className="text-slate-400 text-sm">Total Depreciation</p>
                <p className="text-xl font-bold text-red-500">{formatCurrency(summary.totalDepreciation)}</p>
              </div>
            </div>
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-500/20 rounded">
                <FileText className="h-5 w-5 text-blue-500" />
              </div>
              <div>
                <p className="text-slate-400 text-sm">Total Entries</p>
                <p className="text-xl font-bold text-white">{summary.totalEntries}</p>
              </div>
            </div>
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-purple-500/20 rounded">
                <Calendar className="h-5 w-5 text-purple-500" />
              </div>
              <div>
                <p className="text-slate-400 text-sm">Periods</p>
                <p className="text-xl font-bold text-white">{summary.periods}</p>
              </div>
            </div>
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-green-500/20 rounded">
                <Calendar className="h-5 w-5 text-green-500" />
              </div>
              <div>
                <p className="text-slate-400 text-sm">Latest Period</p>
                <p className="text-xl font-bold text-white">{formatPeriod(summary.latestPeriod)}</p>
              </div>
            </div>
          </Card>
        </div>
      )}

      {/* Filters */}
      <div className="flex gap-4">
        <SelectInput
          value={periodFilter}
          onChange={(e) => setPeriodFilter(e.target.value)}
          options={getPeriodOptions()}
          className="w-48"
        />
        <SelectInput
          value={assetFilter}
          onChange={(e) => setAssetFilter(e.target.value)}
          options={assetOptions}
          className="w-64"
        />
      </div>

      {/* Period Summary */}
      {Object.keys(entriesByPeriod).length > 0 && !assetFilter && (
        <Card className="p-4">
          <h3 className="text-lg font-semibold text-white mb-4">Period Summary</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {Object.entries(entriesByPeriod)
              .sort(([a], [b]) => b.localeCompare(a))
              .slice(0, 8)
              .map(([period, data]) => (
                <div 
                  key={period} 
                  className="p-3 bg-slate-700/50 rounded-lg cursor-pointer hover:bg-slate-700 transition-colors"
                  onClick={() => setPeriodFilter(period)}
                >
                  <p className="text-white font-medium">{formatPeriod(period)}</p>
                  <p className="text-red-400 text-lg font-bold">{formatCurrency(data.totalDepreciation)}</p>
                  <p className="text-slate-400 text-sm">{data.entries.length} assets</p>
                </div>
              ))}
          </div>
        </Card>
      )}

      {/* Detailed Entries */}
      <Card>
        <div className="p-4 border-b border-slate-700">
          <h3 className="text-lg font-semibold text-white">
            Depreciation Entries
            {periodFilter && ` - ${formatPeriod(periodFilter)}`}
          </h3>
        </div>
        {entries.length === 0 ? (
          <EmptyState message="No depreciation entries found" />
        ) : (
          <DataTable data={entries} columns={columns} />
        )}
      </Card>

      {/* Run Depreciation Modal */}
      <Modal
        isOpen={showRunModal}
        onClose={() => setShowRunModal(false)}
        title="Run Depreciation"
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-slate-300 text-sm">
            Calculate depreciation for all active assets for the selected period.
          </p>
          <TextInput
            label="Period"
            type="month"
            value={runForm.period}
            onChange={(e) => setRunForm({ ...runForm, period: e.target.value })}
          />
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="preview"
              checked={runForm.preview}
              onChange={(e) => setRunForm({ ...runForm, preview: e.target.checked })}
              className="rounded"
            />
            <label htmlFor="preview" className="text-sm text-slate-300">
              Preview only (don't create entries)
            </label>
          </div>
          <div className="p-3 bg-blue-600/20 border border-blue-600 rounded-lg text-blue-300 text-sm">
            {runForm.preview 
              ? "Preview mode: This will show what entries would be created without actually creating them."
              : "This will create depreciation entries for all active assets that haven't been depreciated for this period."
            }
          </div>
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="ghost" onClick={() => setShowRunModal(false)}>Cancel</Button>
            <Button onClick={handleRunDepreciation} disabled={runningDepreciation}>
              {runningDepreciation ? 'Running...' : (runForm.preview ? 'Preview' : 'Run Depreciation')}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default DepreciationReportPage;

