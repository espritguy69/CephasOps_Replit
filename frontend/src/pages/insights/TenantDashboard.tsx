import React, { useCallback, useEffect, useState } from 'react';
import { RefreshCw, TrendingUp, Users, Wrench } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Button, Card, LoadingSpinner, useToast } from '../../components/ui';
import { MetricCard } from '../../components/insights';
import { getTenantPerformance, type TenantPerformanceDto } from '../../api/operationalInsights';

const TenantDashboard: React.FC = () => {
  const { showError } = useToast();
  const [data, setData] = useState<TenantPerformanceDto | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await getTenantPerformance();
      setData(result);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load tenant performance';
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
      title="Tenant Performance"
      description="Your company performance at a glance"
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
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <MetricCard
              title="Orders this month"
              value={data.ordersThisMonth}
              icon={TrendingUp}
              iconBg="bg-primary/10"
            />
            <MetricCard
              title="Completion rate"
              value={`${data.completionRate}%`}
              icon={TrendingUp}
              iconBg="bg-emerald-500/10"
            />
            <MetricCard
              title="Avg install time"
              value={data.avgInstallTimeHours != null ? `${data.avgInstallTimeHours}h` : '—'}
              icon={TrendingUp}
            />
            <MetricCard
              title="Active installers"
              value={data.activeInstallers}
              icon={Users}
              iconBg="bg-blue-500/10"
            />
            <MetricCard
              title="Device replacements"
              value={data.deviceReplacements}
              icon={Wrench}
              iconBg="bg-amber-500/10"
            />
            <MetricCard
              title="Orders within SLA"
              value={data.ordersCompletedWithinSla ?? 0}
              subtitle={data.ordersBreachedSla != null && data.ordersBreachedSla > 0 ? `${data.ordersBreachedSla} breached` : undefined}
              icon={TrendingUp}
              iconBg="bg-emerald-500/10"
            />
            <MetricCard
              title="Installer response time"
              value={data.installerResponseTimeHours != null ? `${data.installerResponseTimeHours}h` : '—'}
              icon={TrendingUp}
            />
          </div>
        ) : (
          <Card className="p-6">
            <p className="text-muted-foreground">No data available. Ensure you have a company context.</p>
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default TenantDashboard;
