import React, { useState, useEffect } from 'react';
import { Plus, Edit, Calendar } from 'lucide-react';
import { getPayrollPeriods, createPayrollPeriod, getPayrollPeriod } from '../../api/payroll';
import { LoadingSpinner, EmptyState, useToast, Button, Card, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { PayrollPeriod, PayrollPeriodFilters } from '../../types/payroll';

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const PayrollPeriodsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [periods, setPeriods] = useState<PayrollPeriod[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [filters, setFilters] = useState<{ year?: string }>({ year: new Date().getFullYear().toString() });

  useEffect(() => {
    loadPeriods();
  }, [filters]);

  const loadPeriods = async (): Promise<void> => {
    try {
      setLoading(true);
      const params: PayrollPeriodFilters = {};
      if (filters.year) params.year = parseInt(filters.year);
      const data = await getPayrollPeriods(params);
      setPeriods(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load payroll periods');
      console.error('Error loading payroll periods:', err);
    } finally {
      setLoading(false);
    }
  };

  const columns: TableColumn<PayrollPeriod>[] = [
    { key: 'period', label: 'Period' },
    { key: 'startDate', label: 'Start Date', render: (value: unknown) => value ? new Date(value as string).toLocaleDateString() : '-' },
    { key: 'endDate', label: 'End Date', render: (value: unknown) => value ? new Date(value as string).toLocaleDateString() : '-' },
    { key: 'status', label: 'Status' }
  ];

  if (loading) {
    return (
      <PageShell title="Payroll Periods" breadcrumbs={[{ label: 'Payroll', path: '/payroll' }, { label: 'Periods' }]}>
        <div data-testid="payroll-periods-root">
          <LoadingSpinner message="Loading payroll periods..." fullPage />
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Payroll Periods"
      breadcrumbs={[{ label: 'Payroll', path: '/payroll' }, { label: 'Periods' }]}
      actions={
        <div className="flex items-center gap-2">
          <input
            type="number"
            placeholder="Year"
            value={filters.year || ''}
            onChange={(e) => setFilters({ ...filters, year: e.target.value })}
            className="h-9 px-2 rounded border border-input bg-background text-xs"
          />
          <Button size="sm" className="gap-1">
            <Plus className="h-4 w-4" />
            Create Period
          </Button>
        </div>
      }
    >
      <div data-testid="payroll-periods-root" className="max-w-7xl mx-auto">
      <Card>
        {periods.length > 0 ? (
          <DataTable
            data={periods}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No payroll periods found"
            message="Create your first payroll period to get started."
          />
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default PayrollPeriodsPage;

