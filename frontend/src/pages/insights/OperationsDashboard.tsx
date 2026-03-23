import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertTriangle, CheckCircle, Clock, RefreshCw, UserCheck, Users } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Button, Card, LoadingSpinner, useToast } from '../../components/ui';
import { MetricCard, StatusDistribution } from '../../components/insights';
import { getOperationsControl, type OperationsControlDto } from '../../api/operationalInsights';

const OperationsDashboard: React.FC = () => {
  const { showError } = useToast();
  const [data, setData] = useState<OperationsControlDto | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await getOperationsControl();
      setData(result);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load operations control';
      showError(message);
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [showError]);

  useEffect(() => {
    load();
  }, [load]);

  const formatDate = (s: string | null) =>
    s ? new Date(s).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' }) : '—';

  return (
    <PageShell
      title="Operations Control"
      description="Daily operations and stuck orders"
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
                title="Orders assigned today"
                value={data.ordersAssignedToday}
                icon={UserCheck}
                iconBg="bg-primary/10"
              />
              <MetricCard
                title="Orders completed today"
                value={data.ordersCompletedToday}
                icon={CheckCircle}
                iconBg="bg-emerald-500/10"
              />
              <MetricCard
                title="Installers active"
                value={data.installersActive}
                icon={Users}
                iconBg="bg-blue-500/10"
              />
              <MetricCard
                title="Stuck orders"
                value={data.stuckOrders}
                icon={data.stuckOrders > 0 ? AlertTriangle : Clock}
                iconBg={data.stuckOrders > 0 ? 'bg-amber-500/10' : 'bg-muted'}
              />
              <MetricCard
                title="Exceptions (7d)"
                value={data.exceptions}
                icon={AlertTriangle}
                iconBg={data.exceptions > 0 ? 'bg-red-500/10' : 'bg-muted'}
              />
              <MetricCard
                title="Avg install time (today)"
                value={data.avgInstallTimeHours != null ? `${data.avgInstallTimeHours}h` : '—'}
                icon={Clock}
              />
              <MetricCard
                title="Within SLA today"
                value={data.ordersCompletedWithinSlaToday ?? 0}
                subtitle={data.ordersBreachedSlaToday != null && data.ordersBreachedSlaToday > 0 ? `${data.ordersBreachedSlaToday} breached` : undefined}
                icon={CheckCircle}
                iconBg="bg-emerald-500/10"
              />
            </div>
            <p className="text-sm text-muted-foreground">
              <Link to="/insights/sla" className="text-primary hover:underline">View SLA breach status (nearing / breached)</Link>
            </p>
            {data.stuckOrdersList != null && data.stuckOrdersList.length > 0 && (
              <Card className="p-4">
                <h3 className="text-sm font-medium text-muted-foreground mb-3">Recent stuck orders</h3>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-2">Order</th>
                        <th className="text-left py-2">Status</th>
                        <th className="text-left py-2">Last updated</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.stuckOrdersList.map((row) => (
                        <tr key={row.orderId} className="border-b border-border/50">
                          <td className="py-2">
                            <Link
                              to={`/orders/${row.orderId}`}
                              className="text-primary hover:underline"
                            >
                              {row.orderId.slice(0, 8)}…
                            </Link>
                          </td>
                          <td className="py-2">{row.status}</td>
                          <td className="py-2 text-muted-foreground">{formatDate(row.updatedAtUtc)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </Card>
            )}
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

export default OperationsDashboard;
