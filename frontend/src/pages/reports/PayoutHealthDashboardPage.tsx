import React from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  CheckCircle2,
  AlertCircle,
  FileWarning,
  DollarSign,
  TrendingUp,
  Database,
  RefreshCw,
  AlertTriangle,
  MinusCircle,
  BarChart3,
  Wrench,
  History,
  Bell
} from 'lucide-react';
import { Card, Skeleton } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { getPayoutHealthDashboard, getAlertResponseSummary } from '../../api/payoutHealth';
import type { PayoutHealthDashboardDto } from '../../types/payoutHealth';
import { cn } from '@/lib/utils';

function formatPercent(n: number): string {
  return `${Number(n).toFixed(1)}%`;
}

function formatCurrency(amount: number, currency: string = 'MYR'): string {
  return new Intl.NumberFormat('en-MY', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount);
}

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString(undefined, {
      dateStyle: 'short',
      timeStyle: 'short'
    });
  } catch {
    return iso;
  }
}

const StatCard: React.FC<{
  title: string;
  value: string | number;
  icon: React.ElementType;
  iconBg?: string;
  loading?: boolean;
}> = ({ title, value, icon: Icon, iconBg = 'bg-primary/10', loading }) => (
  <div className="rounded-lg border border-border bg-card p-4 shadow-sm">
    <div className="flex items-start justify-between gap-3">
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-muted-foreground">{title}</p>
        {loading ? (
          <div className="mt-1 h-8 w-20 animate-pulse rounded bg-muted" />
        ) : (
          <p className="mt-1 text-2xl font-bold tracking-tight text-foreground">{value}</p>
        )}
      </div>
      <div className={cn('flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl', iconBg)}>
        <Icon className="h-5 w-5 text-primary" />
      </div>
    </div>
  </div>
);

const PayoutHealthDashboardPage: React.FC = () => {
  const {
    data,
    isLoading,
    error,
    refetch,
    isFetching
  } = useQuery<PayoutHealthDashboardDto>({
    queryKey: ['payout-health', 'dashboard'],
    queryFn: getPayoutHealthDashboard
  });

  const alertResponseQuery = useQuery({
    queryKey: ['payout-health', 'alert-response-summary', 'dashboard'],
    queryFn: () => getAlertResponseSummary({})
  });

  const h = data?.snapshotHealth;
  const a = data?.anomalySummary;
  const alertResp = alertResponseQuery.data;

  if (error) {
    return (
      <PageShell
        title="Payout Health Dashboard"
        breadcrumbs={[
          { label: 'Reports', path: '/reports' },
          { label: 'Payout Health', path: '/reports/payout-health' }
        ]}
      >
        <Card className="border-destructive/50 bg-destructive/5 p-6">
          <p className="text-destructive">Failed to load dashboard: {(error as Error).message}</p>
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Payout Health Dashboard"
      breadcrumbs={[
        { label: 'Reports', path: '/reports' },
        { label: 'Payout Health', path: '/reports/payout-health' }
      ]}
    >
      <p className="mb-4 text-sm text-muted-foreground">
        Snapshot coverage and payout anomaly visibility. Read-only; no payout logic is changed.
      </p>

      <div className="mb-4 flex items-center gap-2">
        <button
          type="button"
          onClick={() => refetch()}
          disabled={isFetching}
          className="inline-flex items-center gap-2 rounded-md border border-border bg-background px-3 py-2 text-sm font-medium text-foreground hover:bg-muted disabled:opacity-50"
        >
          <RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin')} />
          Refresh
        </button>
      </div>

      {/* Snapshot health */}
      <section className="mb-8">
        <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold">
          <Database className="h-5 w-5" />
          Snapshot health
        </h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard
            title="Coverage"
            value={h ? formatPercent(h.coveragePercent) : '—'}
            icon={BarChart3}
            iconBg="bg-emerald-500/10"
            loading={isLoading}
          />
          <StatCard
            title="Completed with snapshot"
            value={h?.completedWithSnapshot ?? '—'}
            icon={CheckCircle2}
            iconBg="bg-emerald-500/10"
            loading={isLoading}
          />
          <StatCard
            title="Completed missing snapshot"
            value={h?.completedMissingSnapshot ?? '—'}
            icon={AlertCircle}
            iconBg={h && h.completedMissingSnapshot > 0 ? 'bg-amber-500/10' : 'bg-muted'}
            loading={isLoading}
          />
          <StatCard
            title="Total completed orders"
            value={h?.totalCompleted ?? '—'}
            icon={Database}
            loading={isLoading}
          />
        </div>
        <div className="mt-4">
          <h3 className="mb-2 text-sm font-medium text-muted-foreground">Snapshot provenance</h3>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
            <StatCard
              title="Normal flow"
              value={h?.normalFlowCount ?? '—'}
              icon={CheckCircle2}
              iconBg="bg-emerald-500/10"
              loading={isLoading}
            />
            <StatCard
              title="Repaired later"
              value={h?.repairJobCount ?? '—'}
              icon={Wrench}
              iconBg="bg-blue-500/10"
              loading={isLoading}
            />
            <StatCard
              title="Unknown (pre-provenance)"
              value={h?.unknownProvenanceCount ?? '—'}
              icon={AlertCircle}
              iconBg="bg-muted"
              loading={isLoading}
            />
            <StatCard title="Backfill" value={h?.backfillCount ?? '—'} icon={Database} loading={isLoading} />
            <StatCard title="Manual backfill" value={h?.manualBackfillCount ?? '—'} icon={Wrench} loading={isLoading} />
          </div>
        </div>
      </section>

      {/* Alert response (compact) */}
      {alertResponseQuery.isSuccess && alertResp && (
        <section className="mb-8">
          <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold">
            <Bell className="h-5 w-5" />
            Alert response
          </h2>
          <Card className="p-4">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div className="flex flex-wrap items-center gap-6">
                <div>
                  <p className="text-xs text-muted-foreground">Alerted · Open</p>
                  <p className="text-xl font-bold">{alertResp.alertedOpen}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Stale (no action)</p>
                  <p className={cn('text-xl font-bold', alertResp.staleCount > 0 && 'text-amber-600 dark:text-amber-400')}>
                    {alertResp.staleCount}
                  </p>
                </div>
                {alertResp.averageTimeToFirstActionMinutes != null && (
                  <div>
                    <p className="text-xs text-muted-foreground">Avg time to first action</p>
                    <p className="text-xl font-bold">{Math.round(alertResp.averageTimeToFirstActionMinutes)} min</p>
                  </div>
                )}
              </div>
              <Link
                to="/reports/payout-health/anomalies"
                className="rounded-md border border-border bg-background px-3 py-2 text-sm font-medium text-foreground hover:bg-muted"
              >
                View anomalies →
              </Link>
            </div>
          </Card>
        </section>
      )}

      {/* Latest repair run */}
      {data?.latestRepairRun && (
        <section className="mb-8">
          <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold">
            <History className="h-5 w-5" />
            Latest repair run
          </h2>
          <Card className="p-4">
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-7">
              <div>
                <p className="text-xs text-muted-foreground">Started</p>
                <p className="font-medium">{formatDate(data.latestRepairRun.startedAt)}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Completed</p>
                <p className="font-medium">
                  {data.latestRepairRun.completedAt ? formatDate(data.latestRepairRun.completedAt) : '—'}
                </p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Processed</p>
                <p className="font-medium">{data.latestRepairRun.totalProcessed}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Created</p>
                <p className="font-medium text-emerald-600">{data.latestRepairRun.createdCount}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Skipped</p>
                <p className="font-medium">{data.latestRepairRun.skippedCount}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Errors</p>
                <p className="font-medium">{data.latestRepairRun.errorCount}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Trigger</p>
                <p className="font-medium">{data.latestRepairRun.triggerSource}</p>
              </div>
            </div>
            {data.latestRepairRun.notes && (
              <p className="mt-2 text-sm text-muted-foreground">{data.latestRepairRun.notes}</p>
            )}
          </Card>
        </section>
      )}

      {/* Recent repair runs */}
      {data?.recentRepairRuns && data.recentRepairRuns.length > 0 && (
        <section className="mb-8">
          <h2 className="mb-3 text-lg font-semibold">Recent repair runs</h2>
          <div className="overflow-x-auto rounded-lg border border-border">
            <table className="w-full min-w-[600px] text-left text-sm">
              <thead className="border-b border-border bg-muted/50">
                <tr>
                  <th className="px-4 py-3 font-medium">Started</th>
                  <th className="px-4 py-3 font-medium">Processed</th>
                  <th className="px-4 py-3 font-medium">Created</th>
                  <th className="px-4 py-3 font-medium">Skipped</th>
                  <th className="px-4 py-3 font-medium">Errors</th>
                  <th className="px-4 py-3 font-medium">Trigger</th>
                </tr>
              </thead>
              <tbody>
                {data.recentRepairRuns.map((run) => (
                  <tr key={run.id} className="border-b border-border last:border-0">
                    <td className="px-4 py-2">{formatDate(run.startedAt)}</td>
                    <td className="px-4 py-2">{run.totalProcessed}</td>
                    <td className="px-4 py-2 text-emerald-600">{run.createdCount}</td>
                    <td className="px-4 py-2">{run.skippedCount}</td>
                    <td className="px-4 py-2">{run.errorCount}</td>
                    <td className="px-4 py-2 text-muted-foreground">{run.triggerSource}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* Anomaly summary */}
      <section className="mb-8">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="flex items-center gap-2 text-lg font-semibold">
            <AlertTriangle className="h-5 w-5" />
            Anomaly summary
          </h2>
          <Link
            to="/reports/payout-health/anomalies"
            className="text-sm font-medium text-primary hover:underline"
          >
            View Payout Anomalies →
          </Link>
        </div>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
          <StatCard
            title="Legacy fallback"
            value={a?.legacyFallbackCount ?? '—'}
            icon={FileWarning}
            iconBg="bg-amber-500/10"
            loading={isLoading}
          />
          <StatCard
            title="Custom override"
            value={a?.customOverrideCount ?? '—'}
            icon={DollarSign}
            loading={isLoading}
          />
          <StatCard
            title="Orders with warnings"
            value={a?.ordersWithWarningsCount ?? '—'}
            icon={AlertTriangle}
            iconBg={a && a.ordersWithWarningsCount > 0 ? 'bg-amber-500/10' : 'bg-muted'}
            loading={isLoading}
          />
          <StatCard
            title="Zero payout (completed)"
            value={a?.zeroPayoutCount ?? '—'}
            icon={MinusCircle}
            iconBg={a && a.zeroPayoutCount > 0 ? 'bg-amber-500/10' : 'bg-muted'}
            loading={isLoading}
          />
          <StatCard
            title="Negative margin (P&amp;L)"
            value={a?.negativeMarginCount ?? '—'}
            icon={TrendingUp}
            iconBg={a && a.negativeMarginCount > 0 ? 'bg-red-500/10' : 'bg-muted'}
            loading={isLoading}
          />
        </div>
      </section>

      {/* Top unusual payouts */}
      <section className="mb-8">
        <h2 className="mb-3 text-lg font-semibold">Top unusual payouts</h2>
        <p className="mb-3 text-sm text-muted-foreground">
          Orders whose payout is more than 2× the average for the same rate group/path.
        </p>
        {isLoading ? (
          <Skeleton className="h-48 w-full rounded-lg" />
        ) : !data?.topUnusualPayouts?.length ? (
          <Card className="p-6">
            <p className="text-muted-foreground">No unusual payouts in this period.</p>
          </Card>
        ) : (
          <div className="overflow-x-auto rounded-lg border border-border">
            <table className="w-full min-w-[600px] text-left text-sm">
              <thead className="border-b border-border bg-muted/50">
                <tr>
                  <th className="px-4 py-3 font-medium">Order</th>
                  <th className="px-4 py-3 font-medium">Final payout</th>
                  <th className="px-4 py-3 font-medium">Path</th>
                  <th className="px-4 py-3 font-medium">Group avg</th>
                  <th className="px-4 py-3 font-medium">Multiple</th>
                  <th className="px-4 py-3 font-medium">Calculated</th>
                </tr>
              </thead>
              <tbody>
                {data.topUnusualPayouts.map((row) => (
                  <tr key={row.orderId} className="border-b border-border last:border-0">
                    <td className="px-4 py-2">
                      <Link
                        to={`/orders/${row.orderId}`}
                        className="text-primary hover:underline"
                      >
                        {row.orderId.slice(0, 8)}…
                      </Link>
                    </td>
                    <td className="px-4 py-2 font-medium">
                      {formatCurrency(row.finalPayout, row.currency)}
                    </td>
                    <td className="px-4 py-2 text-muted-foreground">{row.payoutPath ?? '—'}</td>
                    <td className="px-4 py-2">{formatCurrency(row.groupAveragePayout, row.currency)}</td>
                    <td className="px-4 py-2">{row.multipleOfAverage.toFixed(2)}×</td>
                    <td className="px-4 py-2 text-muted-foreground">{formatDate(row.calculatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* Recent snapshots */}
      <section>
        <h2 className="mb-3 text-lg font-semibold">Recent snapshots</h2>
        {isLoading ? (
          <Skeleton className="h-48 w-full rounded-lg" />
        ) : !data?.recentSnapshots?.length ? (
          <Card className="p-6">
            <p className="text-muted-foreground">No snapshots yet.</p>
          </Card>
        ) : (
          <div className="overflow-x-auto rounded-lg border border-border">
            <table className="w-full min-w-[500px] text-left text-sm">
              <thead className="border-b border-border bg-muted/50">
                <tr>
                  <th className="px-4 py-3 font-medium">Order</th>
                  <th className="px-4 py-3 font-medium">Final payout</th>
                  <th className="px-4 py-3 font-medium">Path</th>
                  <th className="px-4 py-3 font-medium">Calculated</th>
                </tr>
              </thead>
              <tbody>
                {data.recentSnapshots.map((row) => (
                  <tr key={`${row.orderId}-${row.calculatedAt}`} className="border-b border-border last:border-0">
                    <td className="px-4 py-2">
                      <Link
                        to={`/orders/${row.orderId}`}
                        className="text-primary hover:underline"
                      >
                        {row.orderId.slice(0, 8)}…
                      </Link>
                    </td>
                    <td className="px-4 py-2 font-medium">
                      {formatCurrency(row.finalPayout, row.currency)}
                    </td>
                    <td className="px-4 py-2 text-muted-foreground">{row.payoutPath ?? '—'}</td>
                    <td className="px-4 py-2 text-muted-foreground">{formatDate(row.calculatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </PageShell>
  );
};

export default PayoutHealthDashboardPage;
