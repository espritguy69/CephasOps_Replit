import React, { useState, useEffect, useMemo } from 'react';
import { DollarSign, Filter } from 'lucide-react';
import { getJobEarningRecords } from '../../api/payroll';
import { LoadingSpinner, EmptyState, useToast, Card, DataTable, Button } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useAuth } from '../../contexts/AuthContext';
import { canViewPayrollPayout } from '../../utils/fieldPermissions';
import type { JobEarningRecord, JobEarningFilters } from '../../types/payroll';

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const PayrollEarningsPage: React.FC = () => {
  const { showError } = useToast();
  const { user } = useAuth();
  const [earnings, setEarnings] = useState<JobEarningRecord[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [filters, setFilters] = useState<JobEarningFilters>({});

  const showPayoutColumn = canViewPayrollPayout(user);

  const columns: TableColumn<JobEarningRecord>[] = useMemo(() => {
    const base = [
      { key: 'siName', label: 'Service Installer' },
      { key: 'orderId', label: 'Order ID' },
      { key: 'orderType', label: 'Order Type' },
      ...(showPayoutColumn ? [{ key: 'rate' as const, label: 'Rate', render: (value: unknown) => value ? `RM ${parseFloat(value as string).toFixed(2)}` : '-' }] : []),
      { key: 'period', label: 'Period' }
    ];
    return base;
  }, [showPayoutColumn]);

  useEffect(() => {
    loadEarnings();
  }, [filters]);

  const loadEarnings = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getJobEarningRecords(filters);
      setEarnings(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load earnings');
      console.error('Error loading earnings:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <PageShell title="Job Earnings" breadcrumbs={[{ label: 'Payroll', path: '/payroll' }, { label: 'Earnings' }]}>
        <LoadingSpinner message="Loading earnings..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Job Earnings"
      breadcrumbs={[{ label: 'Payroll', path: '/payroll' }, { label: 'Earnings' }]}
      actions={
        <Button variant="outline" size="sm" className="gap-1">
          <Filter className="h-4 w-4" />
          Filter
        </Button>
      }
    >
      <div className="max-w-7xl mx-auto">
      <Card className="p-6">
        {earnings.length > 0 ? (
          <DataTable
            data={earnings}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No earnings found"
            message="Earnings will appear here once payroll runs are created."
          />
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default PayrollEarningsPage;

