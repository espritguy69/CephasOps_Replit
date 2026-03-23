import React, { useCallback, useEffect, useState } from 'react';
import { DollarSign, RefreshCw, TrendingUp, Wallet } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Button, Card, LoadingSpinner, useToast } from '../../components/ui';
import { MetricCard } from '../../components/insights';
import { getFinancialOverview, type FinancialOverviewDto } from '../../api/operationalInsights';

const formatCurrency = (n: number) =>
  new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR', minimumFractionDigits: 0 }).format(n);

const FinancialDashboard: React.FC = () => {
  const { showError } = useToast();
  const [data, setData] = useState<FinancialOverviewDto | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await getFinancialOverview();
      setData(result);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load financial overview';
      showError(message);
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [showError]);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <PageShell
      title="Financial Overview"
      description="Revenue and payouts at a glance"
    >
      <div className="space-y-6">
        <div className="flex justify-end">
          <Button variant="outline" size="sm" onClick={load} disabled={loading}>
            <RefreshCw className={loading ? 'animate-spin h-4 w-4 mr-2' : 'h-4 w-4 mr-2'} />
            Refresh
          </Button>
        </div>
        {loading && !data ? (
          <LoadingSpinner />
        ) : data ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
            <MetricCard
              title="Revenue today"
              value={formatCurrency(data.revenueToday)}
              icon={DollarSign}
              iconBg="bg-emerald-500/10"
            />
            <MetricCard
              title="Revenue this month"
              value={formatCurrency(data.revenueMonth)}
              icon={TrendingUp}
              iconBg="bg-primary/10"
            />
            <MetricCard
              title="Installer payouts (month)"
              value={formatCurrency(data.installerPayouts)}
              icon={Wallet}
              iconBg="bg-blue-500/10"
            />
            <MetricCard
              title="Profit margin"
              value={data.profitMarginPercent != null ? `${data.profitMarginPercent}%` : '—'}
              icon={TrendingUp}
              iconBg="bg-emerald-500/10"
            />
            <MetricCard
              title="Pending payouts"
              value={formatCurrency(data.pendingPayouts)}
              icon={Wallet}
              iconBg="bg-amber-500/10"
            />
          </div>
        ) : (
          <Card className="p-6">
            <p className="text-muted-foreground">No data available.</p>
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default FinancialDashboard;
