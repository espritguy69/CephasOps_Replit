import React, { useCallback, useEffect, useState } from 'react';
import { Activity, AlertTriangle, Building2, CheckCircle, RefreshCw, XCircle, Zap, Clock, ListTodo } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Button, Card, LoadingSpinner, useToast } from '../../components/ui';
import { MetricCard, StatusDistribution } from '../../components/insights';
import { useAuth } from '../../contexts/AuthContext';
import { getPlatformHealth, type PlatformHealthDto } from '../../api/operationalInsights';

const PlatformDashboard: React.FC = () => {
  const { user } = useAuth();
  const { showError } = useToast();
  const [data, setData] = useState<PlatformHealthDto | null>(null);
  const [loading, setLoading] = useState(true);

  const hasPermission = Boolean(
    user?.roles?.includes('SuperAdmin') || user?.roles?.includes('Admin')
  );

  const load = useCallback(async () => {
    if (!hasPermission) return;
    setLoading(true);
    try {
      const result = await getPlatformHealth();
      setData(result);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load platform health';
      showError(message);
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [hasPermission, showError]);

  useEffect(() => {
    load();
  }, [load]);

  if (!hasPermission) {
    return (
      <PageShell title="Platform Health" description="Command center for platform admins">
        <Card className="p-6">
          <p className="text-muted-foreground">
            You do not have permission to view the platform health dashboard. Platform administrators only.
          </p>
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Platform Health"
      description="Aggregated health across all tenants"
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
          <>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <MetricCard
                title="Active Tenants"
                value={data.activeTenants}
                icon={Building2}
                iconBg="bg-blue-500/10"
              />
              <MetricCard
                title="Orders Today"
                value={data.ordersToday}
                icon={Activity}
                iconBg="bg-emerald-500/10"
              />
              <MetricCard
                title="Completion Rate"
                value={`${data.completionRate}%`}
                icon={CheckCircle}
                iconBg="bg-emerald-500/10"
              />
              <MetricCard
                title="Failed Orders"
                value={data.failedOrders}
                icon={data.failedOrders > 0 ? XCircle : AlertTriangle}
                iconBg={data.failedOrders > 0 ? 'bg-red-500/10' : 'bg-amber-500/10'}
              />
            </div>
            <h3 className="text-sm font-medium text-muted-foreground mt-4">Event health (SRE)</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <MetricCard
                title="Events processed (24h)"
                value={data.eventsProcessed ?? 0}
                icon={Zap}
                iconBg="bg-primary/10"
              />
              <MetricCard
                title="Event failures (24h)"
                value={data.eventFailures ?? 0}
                icon={data.eventFailures > 0 ? XCircle : CheckCircle}
                iconBg={data.eventFailures > 0 ? 'bg-red-500/10' : 'bg-muted'}
              />
              <MetricCard
                title="Retry queue size"
                value={data.retryQueueSize ?? 0}
                icon={ListTodo}
                iconBg={data.retryQueueSize > 0 ? 'bg-amber-500/10' : 'bg-muted'}
              />
              <MetricCard
                title="Event lag"
                value={data.eventLagSeconds != null ? `${Math.round(data.eventLagSeconds)}s` : '—'}
                subtitle={data.eventLagSeconds != null ? 'Oldest pending event' : undefined}
                icon={Clock}
                iconBg={data.eventLagSeconds != null && data.eventLagSeconds > 60 ? 'bg-amber-500/10' : 'bg-muted'}
              />
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mt-4">
              <StatusDistribution
                title="Tenant health distribution"
                items={(data.tenantHealthDistribution || []).map((t) => ({
                  status: t.status,
                  count: t.count
                }))}
              />
            </div>
          </>
        ) : (
          <Card className="p-6">
            <p className="text-muted-foreground">No data available.</p>
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default PlatformDashboard;
