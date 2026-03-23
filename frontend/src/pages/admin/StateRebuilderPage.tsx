import React, { useCallback, useEffect, useState } from 'react';
import { Database, List, Play, RefreshCw } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import {
  listRebuildTargets,
  previewRebuild,
  executeRebuild,
  listRebuildOperations,
  getRebuildProgress,
  resumeRebuildOperation
} from '../../api/operationalRebuild';
import type {
  RebuildTargetDescriptorDto,
  RebuildRequestDto,
  RebuildPreviewResultDto,
  RebuildExecutionResultDto,
  RebuildOperationSummaryDto
} from '../../api/operationalRebuild';

type TabId = 'run' | 'history';

const StateBadge: Record<string, { label: string; className: string }> = {
  Pending: { label: 'Queued', className: 'bg-slate-100 text-slate-800' },
  Running: { label: 'Running', className: 'bg-blue-100 text-blue-800' },
  PartiallyCompleted: { label: 'Partially done', className: 'bg-amber-100 text-amber-800' },
  Completed: { label: 'Completed', className: 'bg-green-100 text-green-800' },
  Failed: { label: 'Failed', className: 'bg-red-100 text-red-800' }
};

const StateRebuilderPage: React.FC = () => {
  const { user } = useAuth();
  const { showError, showSuccess } = useToast();
  const [tab, setTab] = useState<TabId>('run');
  const [targets, setTargets] = useState<RebuildTargetDescriptorDto[]>([]);
  const [targetsLoading, setTargetsLoading] = useState(false);
  const [request, setRequest] = useState<RebuildRequestDto>({
    rebuildTargetId: '',
    dryRun: true
  });
  const [previewResult, setPreviewResult] = useState<RebuildPreviewResultDto | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [executeResult, setExecuteResult] = useState<RebuildExecutionResultDto | null>(null);
  const [executeLoading, setExecuteLoading] = useState(false);
  const [historyData, setHistoryData] = useState<{ items: RebuildOperationSummaryDto[]; total: number }>({ items: [], total: 0 });
  const [historyPage, setHistoryPage] = useState(1);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyStateFilter, setHistoryStateFilter] = useState<string>('');
  const [progress, setProgress] = useState<{ processedCount: number; checkpointCount: number; state: string } | null>(null);
  const [progressId, setProgressId] = useState<string | null>(null);
  const [resumeLoading, setResumeLoading] = useState<string | null>(null);

  const permissions = user?.permissions ?? [];
  const canAdminJobs = Boolean(
    permissions.includes('jobs.admin') || (user?.roles ?? []).includes('SuperAdmin') || (user?.roles ?? []).includes('Admin')
  );

  const loadTargets = useCallback(async () => {
    if (!canAdminJobs) return;
    setTargetsLoading(true);
    try {
      const list = await listRebuildTargets();
      setTargets(list);
      if (list.length > 0 && !request.rebuildTargetId) setRequest((r) => ({ ...r, rebuildTargetId: list[0].id }));
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to load targets');
    } finally {
      setTargetsLoading(false);
    }
  }, [canAdminJobs, showError]);

  const loadHistory = useCallback(async () => {
    if (!canAdminJobs) return;
    setHistoryLoading(true);
    try {
      const { items, total } = await listRebuildOperations(historyPage, 20, historyStateFilter || undefined, undefined);
      setHistoryData({ items, total });
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to load history');
    } finally {
      setHistoryLoading(false);
    }
  }, [canAdminJobs, historyPage, historyStateFilter, showError]);

  useEffect(() => {
    loadTargets();
  }, [loadTargets]);

  useEffect(() => {
    if (tab === 'history') loadHistory();
  }, [tab, loadHistory]);

  const runPreview = useCallback(async () => {
    if (!canAdminJobs || !request.rebuildTargetId) return;
    setPreviewLoading(true);
    setPreviewResult(null);
    try {
      const result = await previewRebuild(request);
      setPreviewResult(result);
      showSuccess('Preview completed.');
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Preview failed');
    } finally {
      setPreviewLoading(false);
    }
  }, [request, canAdminJobs, showSuccess, showError]);

  const runExecute = useCallback(
    async (dryRun: boolean, asyncMode = false) => {
      if (!canAdminJobs || !request.rebuildTargetId) return;
      const msg = asyncMode
        ? 'Queue rebuild to run in the background?'
        : dryRun
          ? 'Run rebuild in dry-run mode (no changes)?'
          : 'Execute rebuild? This will replace target state from the source.';
      if (!window.confirm(msg)) return;
      setExecuteLoading(true);
      setExecuteResult(null);
      try {
        const req: RebuildRequestDto = { ...request, dryRun };
        const result = await executeRebuild(req, asyncMode);
        const opId = 'rebuildOperationId' in result ? result.rebuildOperationId : (result as RebuildExecutionResultDto).rebuildOperationId;
        const errMsg = 'errorMessage' in result ? (result as RebuildExecutionResultDto).errorMessage : null;
        if (errMsg && opId === '00000000-0000-0000-0000-000000000000') {
          showError(errMsg);
        } else if (asyncMode && 'message' in result) {
          showSuccess(result.message ?? 'Rebuild queued.');
          setTab('history');
          setHistoryPage(1);
          setProgressId(opId ?? null);
          loadHistory();
        } else {
          setExecuteResult(result as RebuildExecutionResultDto);
          showSuccess(dryRun ? 'Dry run completed.' : 'Rebuild completed.');
          setTab('history');
          setHistoryPage(1);
          loadHistory();
        }
      } catch (err) {
        showError(err instanceof Error ? err.message : 'Execute failed');
      } finally {
        setExecuteLoading(false);
      }
    },
    [request, canAdminJobs, showSuccess, showError, loadHistory]
  );

  const runResume = useCallback(
    async (id: string, asyncMode = false) => {
      if (!canAdminJobs) return;
      if (!window.confirm(asyncMode ? 'Queue resume for background?' : 'Resume this rebuild now?')) return;
      setResumeLoading(id);
      try {
        const result = await resumeRebuildOperation(id, asyncMode);
        const errMsg = 'errorMessage' in result ? (result as RebuildExecutionResultDto).errorMessage : null;
        if (errMsg && (result as RebuildExecutionResultDto).state === 'Failed') {
          showError(errMsg);
        } else {
          showSuccess(asyncMode && 'message' in result ? result.message ?? 'Resume queued.' : 'Resume completed.');
          loadHistory();
        }
      } catch (err) {
        showError(err instanceof Error ? err.message : 'Resume failed');
      } finally {
        setResumeLoading(null);
      }
    },
    [canAdminJobs, showSuccess, showError, loadHistory]
  );

  useEffect(() => {
    if (!progressId || !canAdminJobs) return;
    let cancelled = false;
    const fetchProgress = async () => {
      try {
        const p = await getRebuildProgress(progressId);
        if (cancelled) return;
        setProgress({ processedCount: p.processedCountAtLastCheckpoint, checkpointCount: p.checkpointCount, state: p.state });
        if (p.state === 'Completed' || p.state === 'Failed') {
          setProgressId(null);
          loadHistory();
        }
      } catch {
        if (!cancelled) setProgressId(null);
      }
    };
    fetchProgress();
    const t = setInterval(fetchProgress, 3000);
    return () => {
      cancelled = true;
      clearInterval(t);
    };
  }, [progressId, canAdminJobs, loadHistory]);

  if (!canAdminJobs) {
    return (
      <PageShell title="State Rebuilder" breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'State Rebuilder' }]}>
        <Card className="p-6 text-center">
          <p className="text-muted-foreground">You need Jobs Admin permission to use the State Rebuilder.</p>
        </Card>
      </PageShell>
    );
  }

  const selectedTarget = targets.find((t) => t.id === request.rebuildTargetId);

  return (
    <PageShell
      title="State Rebuilder"
      breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'State Rebuilder' }]}
    >
      <div className="flex gap-2 border-b mb-4">
        <button
          className={`px-3 py-2 text-sm font-medium border-b-2 -mb-px ${
            tab === 'run' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
          onClick={() => setTab('run')}
        >
          <Database className="inline-block w-4 h-4 mr-1" />
          Rebuild
        </button>
        <button
          className={`px-3 py-2 text-sm font-medium border-b-2 -mb-px ${
            tab === 'history' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
          onClick={() => setTab('history')}
        >
          <List className="inline-block w-4 h-4 mr-1" />
          History
        </button>
      </div>

      {tab === 'run' && (
        <div className="space-y-6">
          <Card className="p-4">
            <h3 className="font-medium mb-3">Target & scope</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3 text-sm">
              <label className="lg:col-span-2">
                <span className="text-muted-foreground block">Rebuild target</span>
                <select
                  className="border rounded px-2 py-1 w-full bg-background"
                  value={request.rebuildTargetId}
                  onChange={(e) => {
                    setRequest((r) => ({ ...r, rebuildTargetId: e.target.value }));
                    setPreviewResult(null);
                    setExecuteResult(null);
                  }}
                  disabled={targetsLoading}
                >
                  <option value="">Select target</option>
                  {targets.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.displayName}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span className="text-muted-foreground block">Company ID (optional)</span>
                <input
                  type="text"
                  className="border rounded px-2 py-1 w-full font-mono text-xs"
                  value={request.companyId ?? ''}
                  onChange={(e) => setRequest((r) => ({ ...r, companyId: e.target.value || undefined }))}
                  placeholder="Guid"
                />
              </label>
              <label>
                <span className="text-muted-foreground block">From occurred (UTC, optional)</span>
                <input
                  type="datetime-local"
                  className="border rounded px-2 py-1 w-full"
                  value={request.fromOccurredAtUtc ? request.fromOccurredAtUtc.slice(0, 16) : ''}
                  onChange={(e) => setRequest((r) => ({ ...r, fromOccurredAtUtc: e.target.value ? new Date(e.target.value).toISOString() : undefined }))}
                />
              </label>
              <label>
                <span className="text-muted-foreground block">To occurred (UTC, optional)</span>
                <input
                  type="datetime-local"
                  className="border rounded px-2 py-1 w-full"
                  value={request.toOccurredAtUtc ? request.toOccurredAtUtc.slice(0, 16) : ''}
                  onChange={(e) => setRequest((r) => ({ ...r, toOccurredAtUtc: e.target.value ? new Date(e.target.value).toISOString() : undefined }))}
                />
              </label>
            </div>
            {selectedTarget && (
              <p className="text-muted-foreground text-xs mt-2">
                Source: {selectedTarget.sourceOfTruth} · Strategy: {selectedTarget.rebuildStrategy}
              </p>
            )}
            <div className="flex gap-2 mt-4">
              <Button variant="outline" onClick={runPreview} disabled={previewLoading || !request.rebuildTargetId}>
                {previewLoading ? <LoadingSpinner className="w-4 h-4" /> : <RefreshCw className="w-4 h-4" />}
                Preview
              </Button>
              <Button variant="outline" onClick={() => runExecute(true)} disabled={executeLoading || !request.rebuildTargetId}>
                Execute (dry-run)
              </Button>
              <Button variant="default" onClick={() => runExecute(false)} disabled={executeLoading || !request.rebuildTargetId}>
                {executeLoading ? <LoadingSpinner className="w-4 h-4" /> : <Play className="w-4 h-4" />}
                Execute
              </Button>
              <Button variant="secondary" onClick={() => runExecute(false, true)} disabled={executeLoading || !request.rebuildTargetId}>
                Execute (background)
              </Button>
            </div>
          </Card>

          {previewResult && (
            <Card className="p-4">
              <h3 className="font-medium mb-3">Preview</h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                <div>
                  <span className="text-muted-foreground block">Source records</span>
                  <span className="font-medium">{previewResult.sourceRecordCount}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Current target rows</span>
                  <span className="font-medium">{previewResult.currentTargetRowCount ?? '—'}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Strategy</span>
                  <span className="font-medium">{previewResult.rebuildStrategy}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Scope</span>
                  <span className="font-medium">{previewResult.scopeDescription ?? 'Full'}</span>
                </div>
              </div>
            </Card>
          )}

          {executeResult && (
            <Card className="p-4">
              <h3 className="font-medium mb-3">Result</h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm mb-2">
                <div>
                  <span className="text-muted-foreground block">State</span>
                  <span className={`font-medium ${executeResult.state === 'Failed' ? 'text-red-600' : 'text-green-600'}`}>
                    {executeResult.state}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Rows deleted</span>
                  <span className="font-medium">{executeResult.rowsDeleted}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Rows inserted</span>
                  <span className="font-medium">{executeResult.rowsInserted}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Rows updated</span>
                  <span className="font-medium">{executeResult.rowsUpdated}</span>
                </div>
                {executeResult.durationMs != null && (
                  <div>
                    <span className="text-muted-foreground block">Duration (ms)</span>
                    <span className="font-medium">{executeResult.durationMs}</span>
                  </div>
                )}
              </div>
              {executeResult.errorMessage && (
                <p className="text-sm text-red-600 mt-2">{executeResult.errorMessage}</p>
              )}
            </Card>
          )}
        </div>
      )}

      {tab === 'history' && (
        <Card className="p-4">
          {progressId && progress && (
            <div className="mb-4 p-3 rounded bg-blue-50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-800 text-sm">
              <span className="font-medium">Background run: </span>
              <span>{progress.state}</span>
              {progress.checkpointCount > 0 && (
                <span className="ml-2 text-muted-foreground">
                  Checkpoints: {progress.checkpointCount}, processed: {progress.processedCount}
                </span>
              )}
            </div>
          )}
          <div className="flex flex-wrap justify-between items-center gap-2 mb-3">
            <div className="flex items-center gap-2">
              <h3 className="font-medium">Rebuild operations</h3>
              <select
                className="border rounded px-2 py-1 text-sm bg-background"
                value={historyStateFilter}
                onChange={(e) => {
                  setHistoryStateFilter(e.target.value);
                  setHistoryPage(1);
                }}
              >
                <option value="">All states</option>
                <option value="Pending">Queued</option>
                <option value="Running">Running</option>
                <option value="PartiallyCompleted">Partially done</option>
                <option value="Completed">Completed</option>
                <option value="Failed">Failed</option>
              </select>
            </div>
            <Button variant="outline" size="sm" onClick={loadHistory} disabled={historyLoading}>
              {historyLoading ? <LoadingSpinner className="w-4 h-4" /> : <RefreshCw className="w-4 h-4" />}
              Refresh
            </Button>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2">Target</th>
                  <th className="text-left py-2">Requested</th>
                  <th className="text-left py-2">State</th>
                  <th className="text-right py-2">Deleted</th>
                  <th className="text-right py-2">Inserted</th>
                  <th className="text-right py-2">Duration (ms)</th>
                  <th className="text-left py-2">Actions</th>
                </tr>
              </thead>
              <tbody>
                {historyData.items.length === 0 && !historyLoading && (
                  <tr>
                    <td colSpan={7} className="py-4 text-center text-muted-foreground">
                      No rebuild operations yet.
                    </td>
                  </tr>
                )}
                {historyData.items.map((op) => {
                  const badge = StateBadge[op.state] ?? { label: op.state, className: 'bg-muted text-muted-foreground' };
                  const canResume =
                    (op.state === 'PartiallyCompleted' || op.state === 'Failed' || op.state === 'Pending') &&
                    (op.resumeRequired === true || op.state === 'PartiallyCompleted');
                  return (
                    <tr key={op.id} className="border-b">
                      <td className="py-2 font-mono text-xs">{op.rebuildTargetId}</td>
                      <td className="py-2">{new Date(op.requestedAtUtc).toLocaleString()}</td>
                      <td className="py-2">
                        <span className={`px-2 py-0.5 rounded text-xs ${badge.className}`}>{badge.label}</span>
                        {op.dryRun && <span className="ml-1 text-muted-foreground text-xs">(dry)</span>}
                        {op.resumeRequired && <span className="ml-1 text-amber-600 text-xs">Resume</span>}
                      </td>
                      <td className="py-2 text-right">{op.rowsDeleted}</td>
                      <td className="py-2 text-right">{op.rowsInserted}</td>
                      <td className="py-2 text-right">{op.durationMs ?? '—'}</td>
                      <td className="py-2">
                        {canResume && (
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={resumeLoading === op.id}
                            onClick={() => runResume(op.id, false)}
                          >
                            {resumeLoading === op.id ? <LoadingSpinner className="w-4 h-4" /> : 'Resume'}
                          </Button>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          {historyData.total > 20 && (
            <div className="flex gap-2 mt-3 text-sm">
              <Button
                variant="outline"
                size="sm"
                disabled={historyPage <= 1 || historyLoading}
                onClick={() => setHistoryPage((p) => Math.max(1, p - 1))}
              >
                Previous
              </Button>
              <span className="py-1">
                Page {historyPage} of {Math.ceil(historyData.total / 20)}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={historyPage >= Math.ceil(historyData.total / 20) || historyLoading}
                onClick={() => setHistoryPage((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          )}
        </Card>
      )}
    </PageShell>
  );
};

export default StateRebuilderPage;
