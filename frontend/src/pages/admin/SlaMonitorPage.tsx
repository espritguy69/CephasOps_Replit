import React, { useState, useEffect, useCallback } from 'react';
import { RefreshCw, AlertTriangle, ExternalLink, Clock, TrendingUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import {
  getSlaBreaches,
  getSlaDashboard,
  getSlaRules,
  updateSlaBreachStatus,
  type SlaBreachDto,
  type SlaDashboardDto,
  type SlaRuleDto
} from '../../api/slaMonitor';

function severityBadgeClass(severity: string): string {
  const s = severity.toLowerCase();
  if (s === 'critical') return 'bg-red-100 text-red-800 border-red-200';
  if (s === 'breach') return 'bg-amber-100 text-amber-800 border-amber-200';
  if (s === 'warning') return 'bg-yellow-100 text-yellow-800 border-yellow-200';
  return 'bg-slate-100 text-slate-800 border-slate-200';
}

function statusBadgeClass(status: string): string {
  const s = status.toLowerCase();
  if (s === 'open') return 'bg-red-50 text-red-700';
  if (s === 'acknowledged') return 'bg-amber-50 text-amber-700';
  if (s === 'resolved') return 'bg-green-50 text-green-700';
  return 'bg-slate-100 text-slate-700';
}

function traceExplorerLink(breach: SlaBreachDto): string {
  if (breach.correlationId) {
    return `/admin/trace-explorer?correlationId=${encodeURIComponent(breach.correlationId)}`;
  }
  if (breach.targetType === 'workflow') return `/admin/trace-explorer?workflowJobId=${breach.targetId}`;
  if (breach.targetType === 'event') return `/admin/trace-explorer?eventId=${breach.targetId}`;
  if (breach.targetType === 'job') return `/admin/trace-explorer?jobRunId=${breach.targetId}`;
  return '/admin/trace-explorer';
}

const SlaMonitorPage: React.FC = () => {
  const { user } = useAuth();
  const { showError, showSuccess } = useToast();
  const [dashboard, setDashboard] = useState<SlaDashboardDto | null>(null);
  const [breaches, setBreaches] = useState<SlaBreachDto[]>([]);
  const [total, setTotal] = useState(0);
  const [rules, setRules] = useState<SlaRuleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [targetType, setTargetType] = useState<string>('');
  const [severity, setSeverity] = useState<string>('');
  const [status, setStatus] = useState<string>('');
  const [updatingId, setUpdatingId] = useState<string | null>(null);

  const canView = Boolean(
    user?.roles?.includes('SuperAdmin') ||
    user?.permissions?.includes('jobs.view') ||
    (user?.permissions?.length === 0 && user?.roles?.includes('Admin'))
  );
  const canAdmin = Boolean(
    user?.roles?.includes('SuperAdmin') ||
    user?.permissions?.includes('jobs.admin') ||
    (user?.permissions?.length === 0 && user?.roles?.includes('Admin'))
  );

  const load = useCallback(async () => {
    if (!canView) return;
    setLoading(true);
    try {
      const [dashboardRes, breachesRes, rulesRes] = await Promise.all([
        getSlaDashboard(),
        getSlaBreaches({
          page,
          pageSize,
          targetType: targetType || undefined,
          severity: severity || undefined,
          status: status || undefined
        }),
        getSlaRules()
      ]);
      setDashboard(dashboardRes);
      setBreaches(breachesRes.items);
      setTotal(breachesRes.total);
      setRules(rulesRes.items);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load SLA data';
      showError(message);
      setDashboard(null);
      setBreaches([]);
      setTotal(0);
      setRules([]);
    } finally {
      setLoading(false);
    }
  }, [canView, page, pageSize, targetType, severity, status, showError]);

  useEffect(() => {
    load();
  }, [load]);

  const handleAcknowledge = async (id: string) => {
    if (!canAdmin) return;
    setUpdatingId(id);
    try {
      await updateSlaBreachStatus(id, { status: 'Acknowledged' });
      showSuccess('Breach acknowledged');
      load();
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to acknowledge');
    } finally {
      setUpdatingId(null);
    }
  };

  const handleResolve = async (id: string) => {
    if (!canAdmin) return;
    setUpdatingId(id);
    try {
      await updateSlaBreachStatus(id, { status: 'Resolved' });
      showSuccess('Breach resolved');
      load();
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to resolve');
    } finally {
      setUpdatingId(null);
    }
  };

  if (!canView) {
    return (
      <PageShell title="SLA Monitor">
        <Card className="p-6">
          <p className="text-muted-foreground">You do not have permission to view SLA Monitor.</p>
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="SLA Monitor"
      description="Operational SLA breaches, warnings, and escalation visibility. Use Trace Explorer to investigate."
    >
      <div className="space-y-6">
        {/* Dashboard cards */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <Card className="p-4">
            <div className="flex items-center gap-2 text-muted-foreground">
              <AlertTriangle className="h-4 w-4" />
              <span className="text-sm font-medium">Open breaches</span>
            </div>
            <p className="mt-2 text-2xl font-semibold">
              {loading ? '—' : dashboard?.openBreachesCount ?? 0}
            </p>
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-2 text-muted-foreground">
              <AlertTriangle className="h-4 w-4 text-red-600" />
              <span className="text-sm font-medium">Critical</span>
            </div>
            <p className="mt-2 text-2xl font-semibold text-red-600">
              {loading ? '—' : dashboard?.criticalBreachesCount ?? 0}
            </p>
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-2 text-muted-foreground">
              <Clock className="h-4 w-4" />
              <span className="text-sm font-medium">Avg resolution (h)</span>
            </div>
            <p className="mt-2 text-2xl font-semibold">
              {loading ? '—' : dashboard?.averageResolutionTimeHours ?? '—'}
            </p>
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-2 text-muted-foreground">
              <TrendingUp className="h-4 w-4" />
              <span className="text-sm font-medium">Rules</span>
            </div>
            <p className="mt-2 text-2xl font-semibold">{loading ? '—' : rules.length}</p>
          </Card>
        </div>

        {/* Most common breached targets */}
        {dashboard?.mostCommonBreachedTargets && dashboard.mostCommonBreachedTargets.length > 0 && (
          <Card className="p-4">
            <h3 className="text-sm font-medium text-muted-foreground mb-2">Most common breached (open)</h3>
            <ul className="space-y-1">
              {dashboard.mostCommonBreachedTargets.slice(0, 5).map((t, i) => (
                <li key={i} className="flex justify-between text-sm">
                  <span>{t.targetName || t.targetType}</span>
                  <span className="font-medium">{t.count}</span>
                </li>
              ))}
            </ul>
          </Card>
        )}

        {/* Filters and table */}
        <Card className="p-4">
          <div className="flex flex-wrap items-center gap-4 mb-4">
            <Button variant="outline" size="sm" onClick={load} disabled={loading}>
              <RefreshCw className={`h-4 w-4 mr-1 ${loading ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
            <select
              className="rounded border bg-background px-2 py-1 text-sm"
              value={targetType}
              onChange={(e) => { setTargetType(e.target.value); setPage(1); }}
            >
              <option value="">All types</option>
              <option value="workflow">Workflow</option>
              <option value="event">Event</option>
              <option value="job">Job</option>
            </select>
            <select
              className="rounded border bg-background px-2 py-1 text-sm"
              value={severity}
              onChange={(e) => { setSeverity(e.target.value); setPage(1); }}
            >
              <option value="">All severities</option>
              <option value="Warning">Warning</option>
              <option value="Breach">Breach</option>
              <option value="Critical">Critical</option>
            </select>
            <select
              className="rounded border bg-background px-2 py-1 text-sm"
              value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1); }}
            >
              <option value="">All statuses</option>
              <option value="Open">Open</option>
              <option value="Acknowledged">Acknowledged</option>
              <option value="Resolved">Resolved</option>
            </select>
          </div>

          {loading ? (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-2 px-2">Detected</th>
                      <th className="text-left py-2 px-2">Severity</th>
                      <th className="text-left py-2 px-2">Status</th>
                      <th className="text-left py-2 px-2">Type</th>
                      <th className="text-left py-2 px-2">Title</th>
                      <th className="text-right py-2 px-2">Duration (s)</th>
                      <th className="text-left py-2 px-2">Trace</th>
                      {canAdmin && <th className="text-left py-2 px-2">Actions</th>}
                    </tr>
                  </thead>
                  <tbody>
                    {breaches.length === 0 ? (
                      <tr>
                        <td colSpan={canAdmin ? 8 : 7} className="py-6 text-center text-muted-foreground">
                          No breaches match the filters.
                        </td>
                      </tr>
                    ) : (
                      breaches.map((b) => (
                        <tr key={b.id} className="border-b hover:bg-muted/50">
                          <td className="py-2 px-2">
                            {new Date(b.detectedAtUtc).toLocaleString()}
                          </td>
                          <td className="py-2 px-2">
                            <span className={`inline-flex rounded border px-1.5 py-0.5 text-xs font-medium ${severityBadgeClass(b.severity)}`}>
                              {b.severity}
                            </span>
                          </td>
                          <td className="py-2 px-2">
                            <span className={`inline-flex rounded px-1.5 py-0.5 text-xs ${statusBadgeClass(b.status)}`}>
                              {b.status}
                            </span>
                          </td>
                          <td className="py-2 px-2">{b.targetType}</td>
                          <td className="py-2 px-2 max-w-[200px] truncate" title={b.title ?? undefined}>
                            {b.title ?? b.targetId}
                          </td>
                          <td className="py-2 px-2 text-right">{Math.round(b.durationSeconds)}</td>
                          <td className="py-2 px-2">
                            <Link
                              to={traceExplorerLink(b)}
                              className="inline-flex items-center gap-1 text-primary hover:underline"
                            >
                              <ExternalLink className="h-3 w-3" />
                              Trace
                            </Link>
                          </td>
                          {canAdmin && b.status === 'Open' && (
                            <td className="py-2 px-2">
                              <div className="flex gap-1">
                                <Button
                                  variant="outline"
                                  size="sm"
                                  disabled={updatingId === b.id}
                                  onClick={() => handleAcknowledge(b.id)}
                                >
                                  Ack
                                </Button>
                                <Button
                                  variant="outline"
                                  size="sm"
                                  disabled={updatingId === b.id}
                                  onClick={() => handleResolve(b.id)}
                                >
                                  Resolve
                                </Button>
                              </div>
                            </td>
                          )}
                          {canAdmin && b.status !== 'Open' && <td className="py-2 px-2" />}
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
              {total > pageSize && (
                <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
                  <span>
                    Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, total)} of {total}
                  </span>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page <= 1}
                      onClick={() => setPage((p) => p - 1)}
                    >
                      Previous
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page * pageSize >= total}
                      onClick={() => setPage((p) => p + 1)}
                    >
                      Next
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </Card>
      </div>
    </PageShell>
  );
};

export default SlaMonitorPage;
