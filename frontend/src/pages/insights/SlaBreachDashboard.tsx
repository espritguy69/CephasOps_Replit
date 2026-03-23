import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertTriangle, CheckCircle, Clock, RefreshCw, XCircle } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Button, Card, LoadingSpinner, useToast, Badge } from '../../components/ui';
import { MetricCard } from '../../components/insights';
import {
  getSlaBreachSummary,
  getSlaOrdersAtRisk,
  SlaBreachState,
  type SlaBreachSummaryDto,
  type SlaBreachOrderItemDto
} from '../../api/slaBreach';

const BREACH_STATE_OPTIONS = [
  { value: '', label: 'All at-risk' },
  { value: SlaBreachState.NearingBreach, label: 'Nearing breach' },
  { value: SlaBreachState.Breached, label: 'Breached' }
];

const SEVERITY_OPTIONS = [
  { value: '', label: 'All severities' },
  { value: 'Warning', label: 'Warning' },
  { value: 'Critical', label: 'Critical' }
];

const SlaBreachDashboard: React.FC = () => {
  const { showError } = useToast();
  const [summary, setSummary] = useState<SlaBreachSummaryDto | null>(null);
  const [orders, setOrders] = useState<SlaBreachOrderItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [breachStateFilter, setBreachStateFilter] = useState<string>('');
  const [severityFilter, setSeverityFilter] = useState<string>('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [sum, ords] = await Promise.all([
        getSlaBreachSummary(),
        getSlaOrdersAtRisk(breachStateFilter || null, severityFilter || null)
      ]);
      setSummary(sum);
      setOrders(ords);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load SLA breach data';
      showError(message);
      setSummary(null);
      setOrders([]);
    } finally {
      setLoading(false);
    }
  }, [showError, breachStateFilter, severityFilter]);

  useEffect(() => {
    load();
  }, [load]);

  const formatDue = (minutes: number | undefined) => {
    if (minutes == null) return '—';
    if (minutes < 0) return `${-minutes}m overdue`;
    return `Due in ${minutes}m`;
  };

  const severityVariant = (s: string) => (s === 'Critical' ? 'destructive' : s === 'Warning' ? 'default' : 'secondary');

  return (
    <PageShell
      title="SLA Breach"
      description="Orders nearing or in SLA breach (based on KPI due time)"
    >
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-2 flex-wrap">
            <label htmlFor="breachState" className="text-sm text-muted-foreground">
              State:
            </label>
            <select
              id="breachState"
              className="border rounded px-2 py-1 text-sm bg-background"
              value={breachStateFilter}
              onChange={(e) => setBreachStateFilter(e.target.value)}
            >
              {BREACH_STATE_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
            <label htmlFor="severity" className="text-sm text-muted-foreground ml-2">
              Severity:
            </label>
            <select
              id="severity"
              className="border rounded px-2 py-1 text-sm bg-background"
              value={severityFilter}
              onChange={(e) => setSeverityFilter(e.target.value)}
            >
              {SEVERITY_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
          <Button variant="outline" size="sm" onClick={load} disabled={loading}>
            <RefreshCw className={loading ? 'animate-spin h-4 w-4 mr-2' : 'h-4 w-4 mr-2'} />
            Refresh
          </Button>
        </div>

        {loading && !summary ? (
          <LoadingSpinner />
        ) : summary ? (
          <>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <MetricCard
                title="On track"
                value={summary.distribution.onTrackCount}
                icon={CheckCircle}
                iconBg="bg-emerald-500/10"
              />
              <MetricCard
                title="Nearing breach"
                value={summary.distribution.nearingBreachCount}
                icon={Clock}
                iconBg={summary.distribution.nearingBreachCount > 0 ? 'bg-amber-500/10' : 'bg-muted'}
              />
              <MetricCard
                title="Breached"
                value={summary.distribution.breachedCount}
                icon={AlertTriangle}
                iconBg={summary.distribution.breachedCount > 0 ? 'bg-red-500/10' : 'bg-muted'}
              />
              <MetricCard
                title="No SLA"
                value={summary.distribution.noSlaCount}
                icon={XCircle}
                iconBg="bg-muted"
              />
            </div>

            <Card className="p-4">
              <h3 className="text-sm font-medium text-muted-foreground mb-3">Orders at risk (nearing or breached)</h3>
              {orders.length === 0 ? (
                <p className="text-sm text-muted-foreground">No orders match the current filters.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-2">Order</th>
                        <th className="text-left py-2">Status</th>
                        <th className="text-left py-2">State</th>
                        <th className="text-left py-2">Severity</th>
                        <th className="text-left py-2">Due / Overdue</th>
                        <th className="text-left py-2">Explanation</th>
                      </tr>
                    </thead>
                    <tbody>
                      {orders.map((row) => (
                        <tr key={row.orderId} className="border-b border-border/50">
                          <td className="py-2">
                            <Link
                              to={`/orders/${row.orderId}`}
                              className="text-primary hover:underline"
                            >
                              {row.orderRef || row.orderId.slice(0, 8)}…
                            </Link>
                          </td>
                          <td className="py-2">{row.currentStatus ?? '—'}</td>
                          <td className="py-2">
                            <Badge variant={row.breachState === SlaBreachState.Breached ? 'destructive' : 'default'}>
                              {row.breachState}
                            </Badge>
                          </td>
                          <td className="py-2">
                            <Badge variant={severityVariant(row.severity)}>{row.severity}</Badge>
                          </td>
                          <td className="py-2">{formatDue(row.minutesToDueOrOverdue)}</td>
                          <td className="py-2 text-muted-foreground max-w-xs truncate" title={row.explanation}>
                            {row.explanation}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          </>
        ) : (
          <Card className="p-6">
            <p className="text-muted-foreground">No data available. Ensure company context is set.</p>
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default SlaBreachDashboard;
