import React, { useState, useEffect } from 'react';
import { FileText, Filter } from 'lucide-react';
import { getPnlOrderDetails } from '../../api/pnl';
import { LoadingSpinner, EmptyState, useToast, Button, Card, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { PnlOrderDetail, PnlOrderFilters } from '../../types/pnl';

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const PnlOrdersPage: React.FC = () => {
  const { showError } = useToast();
  const [orders, setOrders] = useState<PnlOrderDetail[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [filters, setFilters] = useState<PnlOrderFilters>({});

  useEffect(() => {
    loadOrders();
  }, [filters]);

  const loadOrders = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getPnlOrderDetails(filters);
      setOrders(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load P&L orders');
      console.error('Error loading P&L orders:', err);
    } finally {
      setLoading(false);
    }
  };

  const columns: TableColumn<PnlOrderDetail>[] = [
    { key: 'orderId', label: 'Order ID' },
    { key: 'orderType', label: 'Order Type' },
    { key: 'revenue', label: 'Revenue', render: (value: unknown) => value ? `RM ${parseFloat(value as string).toFixed(2)}` : '-' },
    { key: 'costs', label: 'Costs', render: (value: unknown) => value ? `RM ${parseFloat(value as string).toFixed(2)}` : '-' },
    { key: 'profit', label: 'Profit', render: (value: unknown) => value ? `RM ${parseFloat(value as string).toFixed(2)}` : '-' },
    { key: 'period', label: 'Period' }
  ];

  if (loading) {
    return (
      <PageShell title="P&L by Order" breadcrumbs={[{ label: 'P&L', path: '/pnl' }, { label: 'Orders' }]}>
        <LoadingSpinner message="Loading P&L orders..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="P&L by Order"
      breadcrumbs={[{ label: 'P&L', path: '/pnl' }, { label: 'Orders' }]}
      actions={
        <Button variant="outline" size="sm" className="gap-1">
          <Filter className="h-4 w-4" />
          Filter
        </Button>
      }
    >
      <div className="max-w-7xl mx-auto">
      <Card>
        {orders.length > 0 ? (
          <DataTable
            data={orders}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No P&L order data found"
            message="P&L data will appear here once orders are processed."
          />
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default PnlOrdersPage;

