import React, { useState } from 'react';
import { DollarSign, RefreshCw } from 'lucide-react';
import { Card, Skeleton, EmptyState, Button, DataTable } from '../../components/ui';
import type { DataTableColumn } from '../../components/ui';
import { PageHeader } from '../../components/layout/PageHeader';
import { Breadcrumbs } from '../../components/ui';
import { useQuery } from '@tanstack/react-query';
import { getMyEarnings } from '../../api/earnings';
import type { JobEarningRecord } from '../../types/api';

export function EarningsPage() {
  const [period, setPeriod] = useState<string>(() => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  });

  const { data: earnings = [], isLoading, error, refetch } = useQuery({
    queryKey: ['myEarnings', period],
    queryFn: () => getMyEarnings({ period }),
    staleTime: 2 * 60 * 1000,
  });

  const columns: DataTableColumn<JobEarningRecord & { orderTypeName?: string; rate?: number }>[] = [
    {
      key: 'orderId',
      label: 'Order',
      sortable: true,
      render: (_, row) => (
        <div>
          <div className="font-medium">{row.orderId}</div>
          {(row as any).orderTypeName && (
            <div className="text-muted-foreground text-xs">{(row as any).orderTypeName}</div>
          )}
        </div>
      ),
    },
    {
      key: 'period',
      label: 'Period',
      sortable: true,
    },
    {
      key: 'rate',
      label: 'Rate',
      sortable: true,
      render: (_, row) => {
        const r = row.rate ?? (row as any).rate ?? row.baseRate ?? (row as any).baseRate;
        return r != null ? `RM ${Number(r).toFixed(2)}` : '-';
      },
    },
    {
      key: 'finalPay',
      label: 'Amount',
      sortable: true,
      render: (_, row) => {
        const pay = row.finalPay ?? (row as any).finalPay ?? row.baseRate ?? (row as any).baseRate ?? 0;
        return `RM ${Number(pay).toFixed(2)}`;
      },
    },
  ];

  if (error) {
    return (
      <>
        <div className="px-3 py-2 md:px-4 lg:px-6">
          <Breadcrumbs items={[{ label: 'Earnings', active: true }]} className="mb-2" />
        </div>
        <PageHeader title="Earnings" />
        <div className="p-4">
          <EmptyState
            title="Error loading earnings"
            description={(error as Error).message || 'Failed to fetch earnings.'}
            action={{ label: 'Retry', onClick: () => refetch() }}
          />
        </div>
      </>
    );
  }

  if (isLoading) {
    return (
      <>
        <div className="px-3 py-2 md:px-4 lg:px-6">
          <Breadcrumbs items={[{ label: 'Earnings', active: true }]} className="mb-2" />
        </div>
        <PageHeader title="Earnings" />
        <div className="p-4 space-y-4">
          <div className="flex justify-between items-center">
            <Skeleton className="h-9 w-32 rounded-md" />
          </div>
          <Card className="p-4">
            <Skeleton className="h-10 w-full mb-4" />
            <div className="space-y-3">
              {[1, 2, 3, 4, 5].map((i) => (
                <Skeleton key={i} className="h-12 w-full rounded-md" />
              ))}
            </div>
          </Card>
        </div>
      </>
    );
  }

  return (
    <>
      <div className="px-3 py-2 md:px-4 lg:px-6">
        <Breadcrumbs items={[{ label: 'Earnings', active: true }]} className="mb-2" />
      </div>
      <PageHeader
        title="Earnings"
        subtitle={`Period: ${period}`}
        actions={
          <div className="flex items-center gap-2">
            <select
              value={period}
              onChange={(e) => setPeriod(e.target.value)}
              className="h-9 rounded-md border border-input bg-background px-3 py-1 text-sm"
            >
              {(() => {
                const now = new Date();
                const options: string[] = [];
                for (let m = 0; m < 12; m++) {
                  const d = new Date(now.getFullYear(), now.getMonth() - m, 1);
                  options.push(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`);
                }
                return options.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ));
              })()}
            </select>
            <Button variant="outline" size="sm" onClick={() => refetch()} className="gap-1">
              <RefreshCw className="h-4 w-4" />
              Refresh
            </Button>
          </div>
        }
      />
      <div className="p-4 space-y-4">
        <Card className="p-4">
          {earnings.length > 0 ? (
            <DataTable data={earnings} columns={columns} sortable />
          ) : (
            <EmptyState
              title="No earnings found"
              description="Earnings will appear here once payroll runs are created for your completed jobs."
              icon={<DollarSign className="h-12 w-12 text-muted-foreground" />}
            />
          )}
        </Card>
      </div>
    </>
  );
}
