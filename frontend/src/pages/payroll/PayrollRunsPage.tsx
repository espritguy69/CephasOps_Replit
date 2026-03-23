import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Eye, CheckCircle, DollarSign, Download, Search, RefreshCcw } from 'lucide-react';
import { getPayrollRuns, createPayrollRun, finalizePayrollRun, markPayrollRunPaid } from '../../api/payroll';
import { LoadingSpinner, EmptyState, useToast, Button, Card, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { getPayrollStatusColor } from '../../utils/statusColors';
import { exportPayrollToExcel } from '../../utils/excelExport';
import { formatDate } from '../../utils/dateHelpers';
import type { PayrollRun, PayrollRunFilters } from '../../types/payroll';

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const PayrollRunsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [runs, setRuns] = useState<PayrollRun[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [filters, setFilters] = useState<PayrollRunFilters>({});

  useEffect(() => {
    loadRuns();
  }, [filters]);

  // Filtered runs
  const filteredRuns = useMemo(() => {
    let result = runs;

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(r =>
        r.period?.toLowerCase().includes(query) ||
        r.status?.toLowerCase().includes(query)
      );
    }

    // Apply status filter
    if (statusFilter !== 'all') {
      result = result.filter(r => r.status?.toLowerCase() === statusFilter.toLowerCase());
    }

    return result;
  }, [runs, searchQuery, statusFilter]);

  const loadRuns = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getPayrollRuns(filters);
      setRuns(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load payroll runs');
      console.error('Error loading payroll runs:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleFinalize = async (runId: string): Promise<void> => {
    try {
      await finalizePayrollRun(runId);
      showSuccess('Payroll run finalized successfully!');
      loadRuns();
    } catch (err: any) {
      showError(err.message || 'Failed to finalize payroll run');
    }
  };

  const handleMarkPaid = async (runId: string): Promise<void> => {
    try {
      await markPayrollRunPaid(runId);
      showSuccess('Payroll run marked as paid!');
      loadRuns();
    } catch (err: any) {
      showError(err.message || 'Failed to mark payroll run as paid');
    }
  };

  const handleExport = (): void => {
    exportPayrollToExcel(filteredRuns);
    showSuccess('Payroll runs exported successfully!');
  };

  // Stats
  const stats = {
    total: runs.length,
    draft: runs.filter(r => r.status?.toLowerCase() === 'draft').length,
    pending: runs.filter(r => r.status?.toLowerCase() === 'pending').length,
    finalized: runs.filter(r => r.status?.toLowerCase() === 'finalized').length,
    paid: runs.filter(r => r.status?.toLowerCase() === 'paid').length,
    totalAmount: runs.reduce((sum, r) => sum + (r.totalAmount || 0), 0),
    paidAmount: runs.filter(r => r.status?.toLowerCase() === 'paid').reduce((sum, r) => sum + (r.totalAmount || 0), 0)
  };

  const columns: TableColumn<PayrollRun>[] = [
    { 
      key: 'period', 
      label: 'Period',
      render: (value: unknown) => (
        <span className="font-medium">{value as string || '-'}</span>
      )
    },
    { 
      key: 'runDate', 
      label: 'Run Date', 
      render: (value: unknown) => formatDate(value as string)
    },
    { 
      key: 'status', 
      label: 'Status',
      render: (value: unknown) => {
        const status = (value as string) || 'draft';
        return (
          <span className={`px-2 py-1 rounded text-xs font-medium border ${getPayrollStatusColor(status)}`}>
            {status.charAt(0).toUpperCase() + status.slice(1)}
          </span>
        );
      }
    },
    { 
      key: 'totalAmount', 
      label: 'Total Amount', 
      render: (value: unknown) => (
        <span className="font-semibold text-emerald-600">
          {value ? `RM ${parseFloat(value as string).toFixed(2)}` : '-'}
        </span>
      )
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_value: unknown, row: PayrollRun) => (
        <div className="flex gap-1">
          <button
            onClick={(e) => {
              e.stopPropagation();
              // View details
            }}
            title="View Details"
            className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
          >
            <Eye className="h-4 w-4" />
          </button>
          {row.status === 'Draft' && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleFinalize(row.id);
              }}
              title="Finalize"
              className="p-1 rounded text-green-600 hover:text-green-700 hover:bg-muted transition-colors"
            >
              <CheckCircle className="h-4 w-4" />
            </button>
          )}
          {row.status === 'Finalized' && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleMarkPaid(row.id);
              }}
              title="Mark as Paid"
              className="p-1 rounded text-emerald-600 hover:text-emerald-700 hover:bg-muted transition-colors"
            >
              <DollarSign className="h-4 w-4" />
            </button>
          )}
        </div>
      )
    }
  ];

  if (loading && runs.length === 0) {
    return (
      <PageShell title="Payroll Runs" breadcrumbs={[{ label: 'Payroll', path: '/payroll' }, { label: 'Runs' }]}>
        <LoadingSpinner message="Loading payroll runs..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Payroll Runs"
      breadcrumbs={[{ label: 'Payroll', path: '/payroll' }, { label: 'Runs' }]}
      actions={
        <div className="flex items-center gap-2">
          <span className="px-2 py-1 bg-muted rounded text-xs text-muted-foreground">
            {filteredRuns.length} of {runs.length}
          </span>
          <Button variant="outline" size="sm" onClick={loadRuns} title="Refresh">
            <RefreshCcw className="h-4 w-4" />
          </Button>
          <Button variant="outline" size="sm" onClick={handleExport} className="gap-1">
            <Download className="h-4 w-4" />
            Export
          </Button>
          <Button size="sm" className="gap-1">
            <Plus className="h-4 w-4" />
            Create Run
          </Button>
        </div>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      {/* Stats Cards */}
      <div className="grid grid-cols-5 gap-4">
        <Card className="p-4">
          <div className="text-xs text-muted-foreground">Total Runs</div>
          <div className="text-2xl font-bold text-foreground">{stats.total}</div>
        </Card>
        <Card className="p-4 border-l-4 border-l-gray-400">
          <div className="text-xs text-muted-foreground">Draft</div>
          <div className="text-2xl font-bold text-gray-600">{stats.draft}</div>
        </Card>
        <Card className="p-4 border-l-4 border-l-amber-400">
          <div className="text-xs text-muted-foreground">Pending</div>
          <div className="text-2xl font-bold text-amber-600">{stats.pending}</div>
        </Card>
        <Card className="p-4 border-l-4 border-l-blue-400">
          <div className="text-xs text-muted-foreground">Finalized</div>
          <div className="text-2xl font-bold text-blue-600">{stats.finalized}</div>
        </Card>
        <Card className="p-4 border-l-4 border-l-green-400">
          <div className="text-xs text-muted-foreground">Paid</div>
          <div className="text-2xl font-bold text-green-600">{stats.paid}</div>
        </Card>
      </div>

      {/* Revenue Summary */}
      <div className="grid grid-cols-2 gap-4">
        <Card className="p-4 bg-gradient-to-r from-blue-50 to-indigo-50 border-blue-200">
          <div className="flex items-center gap-2 text-xs text-blue-600 mb-1">
            <DollarSign className="h-4 w-4" />
            Total Payroll Value
          </div>
          <div className="text-2xl font-bold text-blue-800">
            RM {stats.totalAmount.toFixed(2)}
          </div>
        </Card>
        <Card className="p-4 bg-gradient-to-r from-emerald-50 to-green-50 border-emerald-200">
          <div className="flex items-center gap-2 text-xs text-emerald-600 mb-1">
            <DollarSign className="h-4 w-4" />
            Total Paid Out
          </div>
          <div className="text-2xl font-bold text-emerald-800">
            RM {stats.paidAmount.toFixed(2)}
          </div>
        </Card>
      </div>

      {/* Filters */}
      <Card className="p-4">
        <div className="flex flex-wrap items-center gap-4">
          {/* Search */}
          <div className="flex-1 min-w-[200px]">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search by period..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          {/* Status Filter */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Status:</span>
            <div className="flex gap-1">
              <button
                onClick={() => setStatusFilter('all')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'all'
                    ? 'bg-gray-600 text-white border-gray-700'
                    : 'bg-gray-100 text-gray-700 border-gray-300 hover:bg-gray-200'
                }`}
              >
                All
              </button>
              <button
                onClick={() => setStatusFilter('draft')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'draft'
                    ? 'bg-gray-600 text-white border-gray-700'
                    : 'bg-gray-100 text-gray-700 border-gray-300 hover:bg-gray-200'
                }`}
              >
                Draft
              </button>
              <button
                onClick={() => setStatusFilter('pending')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'pending'
                    ? 'bg-amber-600 text-white border-amber-700'
                    : 'bg-amber-100 text-amber-700 border-amber-300 hover:bg-amber-200'
                }`}
              >
                Pending
              </button>
              <button
                onClick={() => setStatusFilter('finalized')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'finalized'
                    ? 'bg-blue-600 text-white border-blue-700'
                    : 'bg-blue-100 text-blue-700 border-blue-300 hover:bg-blue-200'
                }`}
              >
                Finalized
              </button>
              <button
                onClick={() => setStatusFilter('paid')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'paid'
                    ? 'bg-green-600 text-white border-green-700'
                    : 'bg-green-100 text-green-700 border-green-300 hover:bg-green-200'
                }`}
              >
                Paid
              </button>
            </div>
          </div>
        </div>
      </Card>

      {/* Data Table */}
      <Card>
        {filteredRuns.length > 0 ? (
          <DataTable
            data={filteredRuns}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No payroll runs found"
            message={searchQuery || statusFilter !== 'all'
              ? "No payroll runs match your search criteria."
              : "Create your first payroll run to get started."}
          />
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default PayrollRunsPage;
