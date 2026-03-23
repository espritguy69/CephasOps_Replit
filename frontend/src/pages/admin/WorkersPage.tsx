import React, { useState, useCallback, useEffect } from 'react';
import { RefreshCw, Cpu, Clock, AlertTriangle, CheckCircle, Link as LinkIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import { listWorkers, getWorker } from '../../api/systemWorkers';
import type { WorkerInstanceDto, WorkerInstanceDetailDto } from '../../api/systemWorkers';

const WorkersPage: React.FC = () => {
  const { user } = useAuth();
  const { showError } = useToast();
  const [workers, setWorkers] = useState<WorkerInstanceDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [detailId, setDetailId] = useState<string | null>(null);
  const [detail, setDetail] = useState<WorkerInstanceDetailDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

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
      const list = await listWorkers();
      setWorkers(Array.isArray(list) ? list : []);
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to load workers');
      setWorkers([]);
    } finally {
      setLoading(false);
    }
  }, [canView, showError]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!detailId || !canView) {
      setDetail(null);
      return;
    }
    let cancelled = false;
    setDetailLoading(true);
    getWorker(detailId)
      .then((w) => {
        if (!cancelled) setDetail(w ?? null);
      })
      .catch(() => {
        if (!cancelled) setDetail(null);
      })
      .finally(() => {
        if (!cancelled) setDetailLoading(false);
      });
    return () => { cancelled = true; };
  }, [detailId, canView]);

  const formatAge = (seconds: number | null) => {
    if (seconds == null) return '—';
    if (seconds < 60) return `${Math.round(seconds)}s`;
    const m = Math.floor(seconds / 60);
    const s = Math.round(seconds % 60);
    return s ? `${m}m ${s}s` : `${m}m`;
  };

  if (!canView) {
    return (
      <PageShell title="Workers" breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Workers' }]}>
        <Card className="p-6">
          <p className="text-muted-foreground">You do not have permission to view workers.</p>
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Workers"
      breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Workers' }]}
    >
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Worker instances (API/Worker/Scheduler). Heartbeat and job ownership for distributed execution.
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
        ) : (
          <div className="grid gap-4 md:grid-cols-2">
            <Card className="p-4">
              <h3 className="font-medium mb-3">Worker instances</h3>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-2">Host</th>
                      <th className="text-left py-2">Role</th>
                      <th className="text-left py-2">Heartbeat</th>
                      <th className="text-left py-2">Status</th>
                      <th className="text-left py-2"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {workers.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="py-4 text-muted-foreground text-center">
                          No workers registered
                        </td>
                      </tr>
                    ) : (
                      workers.map((w) => (
                        <tr
                          key={w.id}
                          className={`border-b hover:bg-muted/50 ${detailId === w.id ? 'bg-muted/50' : ''}`}
                        >
                          <td className="py-2 font-mono text-xs">{w.hostName}</td>
                          <td className="py-2">{w.role}</td>
                          <td className="py-2">{formatAge(w.heartbeatAgeSeconds ?? null)}</td>
                          <td className="py-2">
                            {w.isStale ? (
                              <span className="inline-flex items-center gap-1 text-amber-600">
                                <AlertTriangle className="h-3 w-3" /> Stale
                              </span>
                            ) : w.isActive ? (
                              <span className="inline-flex items-center gap-1 text-green-600">
                                <CheckCircle className="h-3 w-3" /> Active
                              </span>
                            ) : (
                              <span className="text-muted-foreground">Inactive</span>
                            )}
                          </td>
                          <td className="py-2">
                            <button
                              type="button"
                              className="text-primary hover:underline text-xs"
                              onClick={() => setDetailId(w.id)}
                            >
                              Details
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </Card>

            <Card className="p-4">
              <h3 className="font-medium mb-3 flex items-center gap-2">
                <Cpu className="h-4 w-4" />
                Worker detail
              </h3>
              {!detailId ? (
                <p className="text-sm text-muted-foreground">Select a worker to see details and owned jobs.</p>
              ) : detailLoading ? (
                <div className="flex justify-center py-8">
                  <LoadingSpinner />
                </div>
              ) : detail ? (
                <div className="space-y-3 text-sm">
                  <div className="grid grid-cols-2 gap-2">
                    <span className="text-muted-foreground">ID</span>
                    <span className="font-mono text-xs break-all">{detail.id}</span>
                    <span className="text-muted-foreground">Host</span>
                    <span>{detail.hostName}</span>
                    <span className="text-muted-foreground">Process ID</span>
                    <span>{detail.processId}</span>
                    <span className="text-muted-foreground">Role</span>
                    <span>{detail.role}</span>
                    <span className="text-muted-foreground">Started</span>
                    <span>{new Date(detail.startedAtUtc).toISOString()}</span>
                    <span className="text-muted-foreground">Last heartbeat</span>
                    <span className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {formatAge(detail.heartbeatAgeSeconds ?? null)} ago
                    </span>
                    <span className="text-muted-foreground">Status</span>
                    <span>
                      {detail.isStale ? (
                        <span className="text-amber-600">Stale</span>
                      ) : detail.isActive ? (
                        <span className="text-green-600">Active</span>
                      ) : (
                        'Inactive'
                      )}
                    </span>
                  </div>
                  {(detail.ownedReplayOperations?.length > 0 || detail.ownedRebuildOperations?.length > 0) && (
                    <div className="pt-2 border-t">
                      <p className="text-muted-foreground mb-2">Jobs owned</p>
                      <ul className="space-y-1">
                        {(detail.ownedReplayOperations ?? []).map((o) => (
                          <li key={o.operationId} className="flex items-center gap-2">
                            <LinkIcon className="h-3 w-3" />
                            <Link
                              to={`/admin/operational-replay/${o.operationId}`}
                              className="text-primary hover:underline font-mono text-xs"
                            >
                              Replay {o.operationId.slice(0, 8)}…
                            </Link>
                            <span className="text-muted-foreground">({o.state ?? '—'})</span>
                          </li>
                        ))}
                        {(detail.ownedRebuildOperations ?? []).map((o) => (
                          <li key={o.operationId} className="flex items-center gap-2">
                            <LinkIcon className="h-3 w-3" />
                            <Link
                              to="/admin/state-rebuilder"
                              className="text-primary hover:underline font-mono text-xs"
                            >
                              Rebuild {o.operationId.slice(0, 8)}…
                            </Link>
                            <span className="text-muted-foreground">({o.state ?? '—'})</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Worker not found.</p>
              )}
            </Card>
          </div>
        )}
      </div>
    </PageShell>
  );
};

export default WorkersPage;
