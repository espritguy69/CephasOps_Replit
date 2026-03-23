import React, { useState, useEffect, useCallback } from 'react';
import {
  RefreshCw,
  Activity,
  CheckCircle,
  XCircle,
  Clock,
  AlertCircle,
  Copy,
  RotateCcw,
  ExternalLink
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import {
  getBackgroundJobsHealth,
  getBackgroundJobsSummary,
  getJobRunsDashboard,
  listFailedJobRuns,
  listRunningJobRuns,
  listStuckJobRuns,
  listJobRuns,
  getJobRun,
  retryJobRun
} from '../../api/backgroundJobs';
import type {
  BackgroundJobHealthDto,
  BackgroundJobsSummaryDto,
  JobRunDashboardDto,
  JobRunDto
} from '../../api/backgroundJobs';

const JOB_STATE_LABELS: Record<number, string> = {
  0: 'Queued',
  1: 'Running',
  2: 'Succeeded',
  3: 'Failed'
};

type TabId = 'overview' | 'failed' | 'running' | 'stuck' | 'recent';
type RecentFilter = '24h' | '7d' | 'failed' | 'all';

const BackgroundJobsPage: React.FC = () => {
  const { user } = useAuth();
  const { showError, showSuccess } = useToast();
  const [health, setHealth] = useState<BackgroundJobHealthDto | null>(null);
  const [summary, setSummary] = useState<BackgroundJobsSummaryDto | null>(null);
  const [dashboard, setDashboard] = useState<JobRunDashboardDto | null>(null);
  const [failedRuns, setFailedRuns] = useState<JobRunDto[]>([]);
  const [runningRuns, setRunningRuns] = useState<JobRunDto[]>([]);
  const [stuckRuns, setStuckRuns] = useState<JobRunDto[]>([]);
  const [recentRuns, setRecentRuns] = useState<JobRunDto[]>([]);
  const [recentTotal, setRecentTotal] = useState(0);
  const [loading, setLoading] = useState<boolean>(true);
  const [tab, setTab] = useState<TabId>('overview');
  const [recentFilter, setRecentFilter] = useState<RecentFilter>('24h');
  const [detailRun, setDetailRun] = useState<JobRunDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [retryingId, setRetryingId] = useState<string | null>(null);
  const pageSize = 20;
  const stuckIds = new Set(stuckRuns.map((r) => r.id));

  const roles = user?.roles ?? [];
  const permissions = user?.permissions ?? [];
  const canViewJobs = Boolean(
    roles.includes('SuperAdmin') ||
    permissions.includes('jobs.view') ||
    (permissions.length === 0 && roles.includes('Admin'))
  );
  const canRunJobs = Boolean(
    roles.includes('SuperAdmin') ||
    permissions.includes('jobs.run') ||
    (permissions.length === 0 && roles.includes('Admin'))
  );

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [healthRes, summaryRes, dashboardRes, failedRes, runningRes, stuckRes] = await Promise.all([
        getBackgroundJobsHealth(),
        getBackgroundJobsSummary(),
        getJobRunsDashboard(),
        listFailedJobRuns(50),
        listRunningJobRuns(),
        listStuckJobRuns(2)
      ]);
      setHealth(healthRes as BackgroundJobHealthDto);
      setSummary(summaryRes as BackgroundJobsSummaryDto);
      setDashboard(dashboardRes as JobRunDashboardDto);
      setFailedRuns((failedRes as { items: JobRunDto[] }).items ?? []);
      setRunningRuns((runningRes as { items: JobRunDto[] }).items ?? []);
      setStuckRuns((stuckRes as { items: JobRunDto[] }).items ?? []);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load job status';
      showError(message);
      setHealth(null);
      setSummary(null);
      setDashboard(null);
      setFailedRuns([]);
      setRunningRuns([]);
      setStuckRuns([]);
    } finally {
      setLoading(false);
    }
  }, [showError]);

  const loadRecent = useCallback(async () => {
    try {
      const now = new Date();
      const params: Parameters<typeof listJobRuns>[0] = { page: 1, pageSize };
      if (recentFilter === '24h') {
        params.fromUtc = new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString();
        params.toUtc = now.toISOString();
      } else if (recentFilter === '7d') {
        params.fromUtc = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString();
        params.toUtc = now.toISOString();
      } else if (recentFilter === 'failed') {
        params.status = 'Failed';
      }
      const res = await listJobRuns(params);
      setRecentRuns(res.items ?? []);
      setRecentTotal(res.total ?? 0);
    } catch {
      setRecentRuns([]);
      setRecentTotal(0);
    }
  }, [recentFilter]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (tab === 'recent') loadRecent();
  }, [tab, loadRecent]);

  const handleRefresh = () => {
    showSuccess('Refreshing…');
    load();
    if (tab === 'recent') loadRecent();
    setDetailRun(null);
  };

  const handleCopyCorrelationId = (id: string | null) => {
    if (!id) return;
    navigator.clipboard.writeText(id);
    showSuccess('Correlation ID copied');
  };

  const openDetail = async (run: JobRunDto) => {
    setDetailRun(run);
    setDetailLoading(true);
    try {
      const full = await getJobRun(run.id);
      setDetailRun(full);
    } catch {
      showError('Failed to load job run details');
    } finally {
      setDetailLoading(false);
    }
  };

  const handleRetry = async (id: string) => {
    setRetryingId(id);
    try {
      await retryJobRun(id);
      showSuccess('Job re-queued for retry');
      load();
      if (detailRun?.id === id) setDetailRun(null);
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Retry failed');
    } finally {
      setRetryingId(null);
    }
  };

  if (!canViewJobs) {
    return (
      <PageShell title="Background Jobs" breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Background Jobs' }]}>
        <Card className="p-6 text-center">
          <p className="text-muted-foreground">You do not have permission to view background jobs.</p>
        </Card>
      </PageShell>
    );
  }

  if (loading && !health && !summary) {
    return (
      <PageShell title="Background Jobs" breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Background Jobs' }]}>
        <div className="flex justify-center py-12">
          <LoadingSpinner />
        </div>
      </PageShell>
    );
  }

  const tabs: { id: TabId; label: string }[] = [
    { id: 'overview', label: 'Overview' },
    { id: 'failed', label: 'Failed' },
    { id: 'running', label: 'Running' },
    { id: 'stuck', label: 'Stuck' },
    { id: 'recent', label: 'Recent runs' }
  ];

  const renderRunRow = (run: JobRunDto, showRetry = false, isStuck = false) => (
    <tr
      key={run.id}
      className="border-b last:border-0 hover:bg-muted/50 cursor-pointer"
      onClick={() => openDetail(run)}
    >
      <td className="py-2 font-medium">{run.jobName || run.jobType}</td>
      <td className="py-2">
        <span
          className={
            run.status === 'Succeeded'
              ? 'text-green-600'
              : run.status === 'Failed' || run.status === 'DeadLetter'
                ? 'text-red-600'
                : run.status === 'Running'
                  ? 'text-blue-600'
                  : ''
          }
        >
          {run.status}
        </span>
        {isStuck && (
          <span className="ml-2 inline-flex items-center rounded bg-amber-100 text-amber-800 text-xs px-1.5 py-0.5 font-medium">
            Stuck
          </span>
        )}
      </td>
      <td className="py-2 text-muted-foreground text-sm">
        {run.startedAtUtc ? new Date(run.startedAtUtc).toLocaleString() : '—'}
      </td>
      <td className="py-2 text-muted-foreground text-sm">
        {run.durationMs != null ? `${(run.durationMs / 1000).toFixed(1)}s` : '—'}
      </td>
      <td className="py-2 max-w-[200px] truncate text-red-600 text-sm" title={run.errorMessage ?? ''}>
        {run.errorMessage ?? '—'}
      </td>
      {showRetry && (
        <td className="py-2" onClick={(e) => e.stopPropagation()}>
          {run.canRetry && canRunJobs && (
            <Button
              variant="outline"
              size="sm"
              disabled={retryingId === run.id}
              onClick={() => handleRetry(run.id)}
            >
              {retryingId === run.id ? <RefreshCw className="animate-spin h-4 w-4" /> : <RotateCcw className="h-4 w-4" />}
              Retry
            </Button>
          )}
        </td>
      )}
    </tr>
  );

  return (
    <PageShell
      title="Background Jobs"
      breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Background Jobs' }]}
      actions={
        <Button variant="outline" size="sm" onClick={handleRefresh} disabled={loading}>
          <RefreshCw className={loading ? 'animate-spin' : ''} />
          Refresh
        </Button>
      }
    >
      <div className="flex gap-2 border-b mb-4">
        {tabs.map((t) => (
          <button
            key={t.id}
            className={`px-3 py-2 text-sm font-medium border-b-2 -mb-px ${
              tab === t.id ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'overview' && (
        <div className="space-y-6">
          {dashboard && (
            <>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-6 gap-4">
                <Card className="p-4">
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Activity />
                    <span className="text-sm font-medium">Runs (24h)</span>
                  </div>
                  <p className="text-2xl font-semibold mt-1">{dashboard.totalRunsLast24h}</p>
                </Card>
                <Card className="p-4">
                  <div className="flex items-center gap-2 text-green-600">
                    <CheckCircle />
                    <span className="text-sm font-medium">Success rate</span>
                  </div>
                  <p className="text-2xl font-semibold mt-1">{dashboard.successRateLast24h}%</p>
                </Card>
                <Card className="p-4">
                  <div className="flex items-center gap-2 text-blue-600">
                    <Clock />
                    <span className="text-sm font-medium">Running / Queued</span>
                  </div>
                  <p className="text-2xl font-semibold mt-1">
                    {dashboard.runningNow} / {dashboard.queuedNow}
                  </p>
                </Card>
                <Card className="p-4">
                  <div className="flex items-center gap-2 text-red-600">
                    <XCircle />
                    <span className="text-sm font-medium">Failed / Stuck</span>
                  </div>
                  <p className="text-2xl font-semibold mt-1">
                    {dashboard.failedLast24h} / {dashboard.stuckCount}
                  </p>
                </Card>
                {dashboard.p95DurationMsLast24h != null && (
                  <Card className="p-4">
                    <div className="flex items-center gap-2 text-muted-foreground">
                      <Clock />
                      <span className="text-sm font-medium">P95 duration (24h)</span>
                    </div>
                    <p className="text-2xl font-semibold mt-1">{(dashboard.p95DurationMsLast24h / 1000).toFixed(1)}s</p>
                  </Card>
                )}
                {(dashboard.jobsPerHourLast24h != null || dashboard.retryRateLast24h != null) && (
                  <Card className="p-4">
                    <div className="flex items-center gap-2 text-muted-foreground">
                      <Activity />
                      <span className="text-sm font-medium">Jobs/h · Retry %</span>
                    </div>
                    <p className="text-2xl font-semibold mt-1">
                      {(dashboard.jobsPerHourLast24h ?? 0).toFixed(1)} · {(dashboard.retryRateLast24h ?? 0).toFixed(1)}%
                    </p>
                  </Card>
                )}
              </div>
              {dashboard.recentFailures && dashboard.recentFailures.length > 0 && (
                <Card className="p-4">
                  <h3 className="font-medium mb-3">Recent failures</h3>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2">Job</th>
                          <th className="text-left py-2">Status</th>
                          <th className="text-left py-2">Started</th>
                          <th className="text-left py-2">Error</th>
                          <th className="text-left py-2"></th>
                        </tr>
                      </thead>
                      <tbody>
                        {dashboard.recentFailures.map((run) => renderRunRow(run, true))}
                      </tbody>
                    </table>
                  </div>
                </Card>
              )}
            </>
          )}
          {health && (
            <Card className="p-4">
              <h3 className="font-medium flex items-center gap-2">
                {health.status === 'Healthy' ? (
                  <CheckCircle className="text-green-600" />
                ) : (
                  <AlertCircle className="text-amber-600" />
                )}
                Worker status: {health.status}
              </h3>
              <p className="text-sm text-muted-foreground mt-1">
                Checked at {health.timestamp ? new Date(health.timestamp).toLocaleString() : '—'}
              </p>
            </Card>
          )}
        </div>
      )}

      {tab === 'failed' && (
        <Card className="p-4">
          <h3 className="font-medium mb-3">Failed job runs</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2">Job</th>
                  <th className="text-left py-2">Status</th>
                  <th className="text-left py-2">Started</th>
                  <th className="text-left py-2">Duration</th>
                  <th className="text-left py-2">Error</th>
                  <th className="text-left py-2"></th>
                </tr>
              </thead>
              <tbody>
                {failedRuns.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="py-6 text-center text-muted-foreground">
                      No failed runs.
                    </td>
                  </tr>
                ) : (
                  failedRuns.map((run) => renderRunRow(run, true))
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {tab === 'running' && (
        <Card className="p-4">
          <h3 className="font-medium mb-3">Running job runs</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2">Job</th>
                  <th className="text-left py-2">Status</th>
                  <th className="text-left py-2">Started</th>
                  <th className="text-left py-2">Duration</th>
                  <th className="text-left py-2">Error</th>
                </tr>
              </thead>
              <tbody>
                {runningRuns.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-6 text-center text-muted-foreground">
                      No running jobs.
                    </td>
                  </tr>
                ) : (
                  runningRuns.map((run) => renderRunRow(run, false, stuckIds.has(run.id)))
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {tab === 'stuck' && (
        <Card className="p-4">
          <h3 className="font-medium mb-3">Stuck job runs (running longer than threshold)</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2">Job</th>
                  <th className="text-left py-2">Status</th>
                  <th className="text-left py-2">Started</th>
                  <th className="text-left py-2">Threshold</th>
                  <th className="text-left py-2">Error</th>
                </tr>
              </thead>
              <tbody>
                {stuckRuns.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-6 text-center text-muted-foreground">
                      No stuck runs.
                    </td>
                  </tr>
                ) : (
                  stuckRuns.map((run) => (
                    <tr
                      key={run.id}
                      className="border-b last:border-0 hover:bg-muted/50 cursor-pointer"
                      onClick={() => openDetail(run)}
                    >
                      <td className="py-2 font-medium">{run.jobName || run.jobType}</td>
                      <td className="py-2">
                        <span className="text-blue-600">{run.status}</span>
                        <span className="ml-2 inline-flex items-center rounded bg-amber-100 text-amber-800 text-xs px-1.5 py-0.5 font-medium">
                          Stuck
                        </span>
                      </td>
                      <td className="py-2 text-muted-foreground text-sm">
                        {run.startedAtUtc ? new Date(run.startedAtUtc).toLocaleString() : '—'}
                      </td>
                      <td className="py-2 text-muted-foreground text-sm">
                        {run.effectiveStuckThresholdSeconds != null
                          ? `${(run.effectiveStuckThresholdSeconds / 60).toFixed(0)}m`
                          : '—'}
                      </td>
                      <td className="py-2 max-w-[200px] truncate text-red-600 text-sm" title={run.errorMessage ?? ''}>
                        {run.errorMessage ?? '—'}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {tab === 'recent' && (
        <Card className="p-4">
          <div className="flex flex-wrap items-center gap-2 mb-3">
            <h3 className="font-medium">Recent runs</h3>
            <div className="flex gap-1">
              {(['24h', '7d', 'failed', 'all'] as const).map((f) => (
                <button
                  key={f}
                  className={`px-2 py-1 text-sm rounded border ${
                    recentFilter === f ? 'bg-primary text-primary-foreground border-primary' : 'border-border'
                  }`}
                  onClick={() => setRecentFilter(f)}
                >
                  {f === 'all' ? 'All' : f === '24h' ? '24h' : f === '7d' ? '7 days' : 'Failed only'}
                </button>
              ))}
            </div>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2">Job</th>
                  <th className="text-left py-2">Status</th>
                  <th className="text-left py-2">Started</th>
                  <th className="text-left py-2">Duration</th>
                  <th className="text-left py-2">Error</th>
                </tr>
              </thead>
              <tbody>
                {recentRuns.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-6 text-center text-muted-foreground">
                      No recent runs.
                    </td>
                  </tr>
                ) : (
                  recentRuns.map((run) => renderRunRow(run, false))
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {/* Detail drawer */}
      {detailRun && (
        <div
          className="fixed inset-0 z-50 bg-black/50 flex justify-end"
          onClick={() => setDetailRun(null)}
        >
          <div
            className="w-full max-w-lg bg-background shadow-lg overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="p-4 border-b flex justify-between items-center">
              <h3 className="font-semibold">Job run details</h3>
              <Button variant="ghost" size="sm" onClick={() => setDetailRun(null)}>
                Close
              </Button>
            </div>
            <div className="p-4 space-y-3 text-sm">
              {detailLoading ? (
                <LoadingSpinner />
              ) : (
                <>
                  <div><span className="text-muted-foreground">Job:</span> {detailRun.jobName || detailRun.jobType}</div>
                  <div><span className="text-muted-foreground">Status:</span> {detailRun.status}</div>
                  {detailRun.parentJobRunId && (
                    <div className="flex items-center gap-2">
                      <span className="text-muted-foreground">Retry of:</span>
                      <button
                        type="button"
                        className="text-primary underline"
                        onClick={() => {
                          setDetailLoading(true);
                          getJobRun(detailRun.parentJobRunId!)
                            .then(setDetailRun)
                            .finally(() => setDetailLoading(false));
                        }}
                      >
                        {detailRun.parentJobRunId.slice(0, 8)}…
                      </button>
                    </div>
                  )}
                  <div className="border rounded p-2 space-y-1">
                    <div className="text-muted-foreground text-xs font-medium">Timeline</div>
                    <div className="flex items-center gap-2">
                      <span className="text-green-600">Started</span>
                      <span>{detailRun.startedAtUtc ? new Date(detailRun.startedAtUtc).toLocaleString() : '—'}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      {detailRun.completedAtUtc ? (
                        <>
                          <span className="text-blue-600">Completed</span>
                          <span>{new Date(detailRun.completedAtUtc).toLocaleString()}</span>
                        </>
                      ) : (
                        <>
                          <span className="text-blue-600">Running</span>
                          <span>—</span>
                        </>
                      )}
                    </div>
                  </div>
                  <div><span className="text-muted-foreground">Duration:</span> {detailRun.durationMs != null ? `${(detailRun.durationMs / 1000).toFixed(1)}s` : '—'}</div>
                  <div><span className="text-muted-foreground">Trigger:</span> {detailRun.triggerSource}</div>
                  <div className="flex items-center gap-2">
                    <span className="text-muted-foreground">Correlation ID:</span>
                    <code className="text-xs bg-muted px-1 rounded">{detailRun.correlationId ?? '—'}</code>
                    {detailRun.correlationId && (
                      <Button variant="ghost" size="sm" onClick={() => handleCopyCorrelationId(detailRun.correlationId!)}>
                        <Copy className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                  <div>
                    <div className="flex flex-wrap gap-3">
                      <Link
                        to={`/admin/trace-explorer?jobRunId=${encodeURIComponent(detailRun.id)}`}
                        className="text-primary hover:underline inline-flex items-center gap-1 text-sm"
                      >
                        View full trace in Trace Explorer <ExternalLink className="h-3 w-3" />
                      </Link>
                      {detailRun.correlationId && (
                        <Link
                          to={`/admin/trace-explorer?correlationId=${encodeURIComponent(detailRun.correlationId)}`}
                          className="text-primary hover:underline inline-flex items-center gap-1 text-sm"
                        >
                          View trace by Correlation ID <ExternalLink className="h-3 w-3" />
                        </Link>
                      )}
                    </div>
                  </div>
                  {detailRun.errorMessage && (
                    <div>
                      <span className="text-muted-foreground">Error:</span>
                      <p className="mt-1 text-red-600 break-words">{detailRun.errorMessage}</p>
                      {detailRun.errorDetails && (
                        <pre className="mt-1 p-2 bg-muted rounded text-xs overflow-auto max-h-40">{detailRun.errorDetails}</pre>
                      )}
                    </div>
                  )}
                  {detailRun.canRetry && canRunJobs && (
                    <Button
                      variant="outline"
                      disabled={retryingId === detailRun.id}
                      onClick={() => handleRetry(detailRun.id)}
                    >
                      {retryingId === detailRun.id ? <RefreshCw className="animate-spin h-4 w-4" /> : <RotateCcw className="h-4 w-4" />}
                      Retry job
                    </Button>
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      )}
    </PageShell>
  );
};

export default BackgroundJobsPage;
