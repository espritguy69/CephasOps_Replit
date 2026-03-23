import React, { useState, useCallback, useEffect } from 'react';
import { RefreshCw, Clock, Activity, CheckCircle, XCircle, Link as LinkIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import { getSchedulerDiagnostics } from '../../api/systemScheduler';
import type { SchedulerDiagnosticsDto } from '../../api/systemScheduler';

const SchedulerPage: React.FC = () => {
  const { user } = useAuth();
  const { showError } = useToast();
  const [data, setData] = useState<SchedulerDiagnosticsDto | null>(null);
  const [loading, setLoading] = useState(true);

  const roles = user?.roles ?? [];
  const permissions = user?.permissions ?? [];
  const canView = Boolean(
    roles.includes('SuperAdmin') ||
    permissions.includes('jobs.admin') ||
    (permissions.length === 0 && roles.includes('Admin'))
  );

  const load = useCallback(async () => {
    if (!canView) return;
    setLoading(true);
    try {
      const d = await getSchedulerDiagnostics();
      setData(d);
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to load scheduler status');
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [canView, showError]);

  useEffect(() => {
    load();
  }, [load]);

  const lastPoll = data?.lastPollUtc ? new Date(data.lastPollUtc) : null;
  const lastPollAgo = lastPoll ? Math.round((Date.now() - lastPoll.getTime()) / 1000) : null;

  if (!canView) {
    return (
      <PageShell title="Scheduler" breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Scheduler' }]}>
        <Card className="p-6">
          <p className="text-muted-foreground">You do not have permission to view scheduler diagnostics.</p>
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Scheduler"
      breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Scheduler' }]}
    >
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Job polling coordinator: discovers runnable jobs and claims them for this worker. Execution is handled by the background job processor.
          </p>
          <Button variant="outline" size="sm" onClick={load} disabled={loading}>
            <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>

        {loading ? (
          <div className="flex justify-center py-12">
            <LoadingSpinner />
          </div>
        ) : data ? (
          <div className="grid gap-4 md:grid-cols-2">
            <Card className="p-4">
              <h3 className="font-medium mb-3 flex items-center gap-2">
                <Activity className="h-4 w-4" />
                Status
              </h3>
              <dl className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Poll interval</span>
                  <span>{data.pollIntervalSeconds}s</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Max jobs per poll</span>
                  <span>{data.maxJobsPerPoll}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Worker</span>
                  <span className="font-mono text-xs">
                    {data.workerId ? (
                      <Link to={`/admin/workers`} className="text-primary hover:underline">
                        {data.workerId.slice(0, 8)}…
                      </Link>
                    ) : (
                      '—'
                    )}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground flex items-center gap-1">
                    <Clock className="h-3 w-3" /> Last poll
                  </span>
                  <span>
                    {lastPoll
                      ? lastPollAgo != null
                        ? lastPollAgo < 60
                          ? `${lastPollAgo}s ago`
                          : `${Math.floor(lastPollAgo / 60)}m ago`
                        : lastPoll.toISOString()
                      : '—'}
                  </span>
                </div>
              </dl>
            </Card>

            <Card className="p-4">
              <h3 className="font-medium mb-3">Claim totals</h3>
              <dl className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Discovered</span>
                  <span>{data.totalDiscovered}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Claim attempts</span>
                  <span>{data.totalClaimAttempts}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground text-green-600 flex items-center gap-1">
                    <CheckCircle className="h-3 w-3" /> Success
                  </span>
                  <span>{data.totalClaimSuccess}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground text-amber-600 flex items-center gap-1">
                    <XCircle className="h-3 w-3" /> Failure
                  </span>
                  <span>{data.totalClaimFailure}</span>
                </div>
              </dl>
            </Card>

            <Card className="p-4 md:col-span-2">
              <h3 className="font-medium mb-3">Recent claim attempts</h3>
              {!data.recentClaimAttempts?.length ? (
                <p className="text-sm text-muted-foreground">None yet.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-2">Job ID</th>
                        <th className="text-left py-2">Result</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.recentClaimAttempts.slice(0, 20).map((a, i) => (
                        <tr key={`${a.jobId}-${i}`} className="border-b">
                          <td className="py-2 font-mono text-xs">
                            <Link to="/admin/background-jobs" className="text-primary hover:underline">
                              {a.jobId.slice(0, 8)}…
                            </Link>
                          </td>
                          <td className="py-2">
                            {a.success ? (
                              <span className="text-green-600 flex items-center gap-1">
                                <CheckCircle className="h-3 w-3" /> Claimed
                              </span>
                            ) : (
                              <span className="text-amber-600">Missed</span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          </div>
        ) : (
          <Card className="p-6">
            <p className="text-muted-foreground">No scheduler data available.</p>
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default SchedulerPage;
