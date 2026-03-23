import React, { useState, useCallback, useEffect } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { RotateCcw, List, FileSearch, Play, ExternalLink, RefreshCw, Repeat } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import {
  previewReplay,
  executeReplay,
  listReplayOperations,
  getReplayOperation,
  listReplayTargets,
  getReplayOperationProgress,
  resumeReplayOperation,
  rerunFailedReplayOperation,
  cancelReplayOperation
} from '../../api/eventStore';
import type {
  ReplayRequestDto,
  ReplayPreviewResultDto,
  ReplayOperationListItemDto,
  ReplayOperationDetailDto,
  OperationalReplayExecutionResultDto,
  ReplayTargetDescriptorDto,
  ReplayOperationProgressDto
} from '../../api/eventStore';

type TabId = 'preview' | 'history';

const STATE_BADGE: Record<string, { label: string; className: string }> = {
  Pending: { label: 'Pending', className: 'bg-slate-100 text-slate-800' },
  Running: { label: 'Running', className: 'bg-blue-100 text-blue-800' },
  PartiallyCompleted: { label: 'Partially completed', className: 'bg-amber-100 text-amber-800' },
  Completed: { label: 'Completed', className: 'bg-green-100 text-green-800' },
  Failed: { label: 'Failed', className: 'bg-red-100 text-red-800' },
  Cancelled: { label: 'Cancelled', className: 'bg-slate-100 text-slate-600' }
};

function StateBadge({ state, resumeRequired }: { state: string | null | undefined; resumeRequired?: boolean }) {
  const s = state ?? 'Pending';
  const badge = STATE_BADGE[s] ?? { label: s, className: 'bg-muted text-muted-foreground' };
  return (
    <span className="inline-flex items-center gap-1">
      <span className={`px-2 py-0.5 rounded text-xs font-medium ${badge.className}`}>{badge.label}</span>
      {resumeRequired && <span className="px-2 py-0.5 rounded text-xs font-medium bg-amber-100 text-amber-800">Resume</span>}
    </span>
  );
}

const OperationalReplayPage: React.FC = () => {
  const { user } = useAuth();
  const { showError, showSuccess } = useToast();
  const navigate = useNavigate();
  const { id: operationId } = useParams<{ id: string }>();

  const [tab, setTab] = useState<TabId>('preview');
  const [filters, setFilters] = useState<ReplayRequestDto>({
    dryRun: true,
    maxEvents: 500
  });
  const [targets, setTargets] = useState<ReplayTargetDescriptorDto[]>([]);
  const [targetsLoading, setTargetsLoading] = useState(false);
  const [previewResult, setPreviewResult] = useState<ReplayPreviewResultDto | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [executeLoading, setExecuteLoading] = useState(false);
  const [historyData, setHistoryData] = useState<{ items: ReplayOperationListItemDto[]; total: number }>({ items: [], total: 0 });
  const [historyPage, setHistoryPage] = useState(1);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [detail, setDetail] = useState<ReplayOperationDetailDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [progress, setProgress] = useState<ReplayOperationProgressDto | null>(null);
  const [progressPolling, setProgressPolling] = useState(false);
  const [resumeLoading, setResumeLoading] = useState(false);
  const [rerunFailedLoading, setRerunFailedLoading] = useState(false);
  const [cancelLoading, setCancelLoading] = useState(false);

  const permissions = user?.permissions ?? [];
  const canAdminJobs = Boolean(
    permissions.includes('jobs.admin') || (user?.roles ?? []).includes('SuperAdmin') || (user?.roles ?? []).includes('Admin')
  );

  const runPreview = useCallback(async () => {
    if (!canAdminJobs) return;
    setPreviewLoading(true);
    setPreviewResult(null);
    try {
      const req: ReplayRequestDto = { ...filters, dryRun: true };
      const result = await previewReplay(req);
      setPreviewResult(result);
      showSuccess('Preview completed.');
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Preview failed');
    } finally {
      setPreviewLoading(false);
    }
  }, [filters, canAdminJobs, showSuccess, showError]);

  const runExecute = useCallback(async (asyncMode = false) => {
    if (!canAdminJobs) return;
    const msg = asyncMode
      ? 'Queue replay to run in the background? You can track progress on the operation detail page.'
      : 'Execute replay for eligible events? This will run handlers for each event.';
    if (!window.confirm(msg)) return;
    setExecuteLoading(true);
    try {
      const req: ReplayRequestDto = { ...filters, dryRun: false };
      const result = await executeReplay(req, asyncMode);
      const opId = 'replayOperationId' in result ? result.replayOperationId : (result as OperationalReplayExecutionResultDto).replayOperationId;
      if (result && 'errorMessage' in result && (result as OperationalReplayExecutionResultDto).errorMessage && opId === '00000000-0000-0000-0000-000000000000') {
        showError((result as OperationalReplayExecutionResultDto).errorMessage ?? 'Execute failed');
        return;
      }
      if (asyncMode && 'message' in result) {
        showSuccess(result.message ?? 'Replay queued for background execution.');
        setTab('history');
        setHistoryPage(1);
        loadHistory();
        if (opId) navigate(`/admin/operational-replay/${opId}`);
        return;
      }
      const r = result as OperationalReplayExecutionResultDto;
      showSuccess(`Replay completed. Executed: ${r.totalExecuted}, Succeeded: ${r.totalSucceeded}, Failed: ${r.totalFailed}`);
      setTab('history');
      setHistoryPage(1);
      loadHistory();
      if (opId) navigate(`/admin/operational-replay/${opId}`);
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Execute failed');
    } finally {
      setExecuteLoading(false);
    }
  }, [filters, canAdminJobs, showSuccess, showError, navigate]);

  const loadHistory = useCallback(async () => {
    setHistoryLoading(true);
    try {
      const res = await listReplayOperations(historyPage, 20);
      setHistoryData({ items: res.items, total: res.total });
    } catch {
      setHistoryData({ items: [], total: 0 });
    } finally {
      setHistoryLoading(false);
    }
  }, [historyPage]);

  const loadDetail = useCallback(async (id: string) => {
    setDetailLoading(true);
    setDetail(null);
    setProgress(null);
    try {
      const d = await getReplayOperation(id);
      setDetail(d);
      if (d.state === 'Running' || d.state === 'PartiallyCompleted') {
        const p = await getReplayOperationProgress(id);
        setProgress(p ?? null);
      }
    } catch {
      setDetail(null);
      setProgress(null);
    } finally {
      setDetailLoading(false);
    }
  }, []);

  const loadTargets = useCallback(async () => {
    setTargetsLoading(true);
    try {
      const list = await listReplayTargets();
      setTargets(list);
    } catch {
      setTargets([]);
    } finally {
      setTargetsLoading(false);
    }
  }, []);

  const handleResume = useCallback(async (id: string, asyncMode: boolean) => {
    if (!canAdminJobs) return;
    setResumeLoading(true);
    try {
      const result = await resumeReplayOperation(id, asyncMode);
      if ('message' in result) {
        showSuccess(result.message ?? 'Resume queued.');
        if (operationId === id) loadDetail(id);
        loadHistory();
      } else {
        showSuccess('Resume completed.');
        if (operationId === id) loadDetail(id);
        loadHistory();
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Resume failed');
    } finally {
      setResumeLoading(false);
    }
  }, [canAdminJobs, operationId, loadDetail, loadHistory, showSuccess, showError]);

  const handleRerunFailed = useCallback(async (id: string, reason?: string) => {
    if (!canAdminJobs) return;
    if (!window.confirm('Create a new operation that replays only the failed events from this run?')) return;
    setRerunFailedLoading(true);
    try {
      const result = await rerunFailedReplayOperation(id, { rerunReason: reason ?? undefined });
      showSuccess('Rerun-failed operation created.');
      if (result.replayOperationId) navigate(`/admin/operational-replay/${result.replayOperationId}`);
      loadHistory();
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Rerun failed');
    } finally {
      setRerunFailedLoading(false);
    }
  }, [canAdminJobs, navigate, loadHistory, showSuccess, showError]);

  const canCancel = (state: string | null | undefined) =>
    state === 'Pending' || state === 'Running' || state === 'PartiallyCompleted';

  const handleCancel = useCallback(async (id: string) => {
    if (!canAdminJobs) return;
    if (!window.confirm('Cancel this replay? If it is running, it will stop at the next checkpoint.')) return;
    setCancelLoading(true);
    try {
      await cancelReplayOperation(id);
      showSuccess('Cancel requested.');
      if (operationId === id) loadDetail(id);
      loadHistory();
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Cancel failed');
    } finally {
      setCancelLoading(false);
    }
  }, [canAdminJobs, operationId, loadDetail, loadHistory, showSuccess, showError]);

  useEffect(() => {
    if (tab === 'history') loadHistory();
  }, [tab, historyPage, loadHistory]);

  useEffect(() => {
    if (operationId) loadDetail(operationId);
  }, [operationId, loadDetail]);

  useEffect(() => {
    if (tab === 'preview' && targets.length === 0 && !targetsLoading) loadTargets();
  }, [tab, targets.length, targetsLoading, loadTargets]);

  const terminalStates = ['Completed', 'Failed', 'Cancelled'];
  const isTerminalState = (s: string | null | undefined) => s != null && terminalStates.includes(s);

  // Progress polling: only when viewing a Running operation; stop on terminal state or API failure (keep last progress).
  useEffect(() => {
    if (!operationId || detail?.state !== 'Running') {
      setProgressPolling(false);
      return;
    }
    setProgressPolling(true);
    const t = setInterval(async () => {
      try {
        const p = await getReplayOperationProgress(operationId);
        if (p) {
          setProgress(p);
          if (isTerminalState(p.state)) {
            clearInterval(t);
            setProgressPolling(false);
            loadDetail(operationId);
          }
        }
      } catch {
        // Keep last known progress on temporary API failure
      }
    }, 4000);
    return () => clearInterval(t);
  }, [operationId, detail?.state, loadDetail]);

  if (!canAdminJobs) {
    return (
      <PageShell title="Operational Replay" breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Operational Replay' }]}>
        <Card className="p-6 text-center">
          <p className="text-muted-foreground">You need Jobs Admin permission to use operational replay.</p>
        </Card>
      </PageShell>
    );
  }

  const showDetail = operationId && (detail || detailLoading);

  return (
    <PageShell
      title="Operational Replay"
      breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Operational Replay' }]}
    >
      <div className="flex gap-2 border-b mb-4">
        <button
          className={`px-3 py-2 text-sm font-medium border-b-2 -mb-px ${
            tab === 'preview' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
          onClick={() => { setTab('preview'); setPreviewResult(null); }}
        >
          <FileSearch className="inline-block w-4 h-4 mr-1" />
          Preview & Execute
        </button>
        <button
          className={`px-3 py-2 text-sm font-medium border-b-2 -mb-px ${
            tab === 'history' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
          onClick={() => setTab('history')}
        >
          <List className="inline-block w-4 h-4 mr-1" />
          Operations History
        </button>
      </div>

      {tab === 'preview' && (
        <div className="space-y-6">
          <Card className="p-4">
            <h3 className="font-medium mb-3">Filters</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 text-sm">
              <label>
                <span className="text-muted-foreground block">Replay target</span>
                <select
                  className="border rounded px-2 py-1 w-full bg-background"
                  value={filters.replayTarget ?? ''}
                  onChange={(e) => setFilters((f) => ({ ...f, replayTarget: e.target.value || undefined }))}
                >
                  <option value="">EventStore (default)</option>
                  {targets.map((t) => (
                    <option key={t.id} value={t.id} disabled={!t.supported}>
                      {t.displayName}{t.supported ? '' : ' (not supported)'}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span className="text-muted-foreground block">Company ID</span>
                <input
                  type="text"
                  className="border rounded px-2 py-1 w-full"
                  value={filters.companyId ?? ''}
                  onChange={(e) => setFilters((f) => ({ ...f, companyId: e.target.value || undefined }))}
                  placeholder="Guid"
                />
              </label>
              <label>
                <span className="text-muted-foreground block">Event type</span>
                <input
                  type="text"
                  className="border rounded px-2 py-1 w-full"
                  value={filters.eventType ?? ''}
                  onChange={(e) => setFilters((f) => ({ ...f, eventType: e.target.value || undefined }))}
                  placeholder="e.g. WorkflowTransitionCompleted"
                />
              </label>
              <label>
                <span className="text-muted-foreground block">Status</span>
                <input
                  type="text"
                  className="border rounded px-2 py-1 w-full"
                  value={filters.status ?? ''}
                  onChange={(e) => setFilters((f) => ({ ...f, status: e.target.value || undefined }))}
                  placeholder="Pending, Processed, Failed, DeadLetter"
                />
              </label>
              <label>
                <span className="text-muted-foreground block">From (UTC)</span>
                <input
                  type="datetime-local"
                  className="border rounded px-2 py-1 w-full"
                  value={filters.fromOccurredAtUtc ? filters.fromOccurredAtUtc.slice(0, 16) : ''}
                  onChange={(e) => setFilters((f) => ({ ...f, fromOccurredAtUtc: e.target.value ? new Date(e.target.value).toISOString() : undefined }))}
                />
              </label>
              <label>
                <span className="text-muted-foreground block">To (UTC)</span>
                <input
                  type="datetime-local"
                  className="border rounded px-2 py-1 w-full"
                  value={filters.toOccurredAtUtc ? filters.toOccurredAtUtc.slice(0, 16) : ''}
                  onChange={(e) => setFilters((f) => ({ ...f, toOccurredAtUtc: e.target.value ? new Date(e.target.value).toISOString() : undefined }))}
                />
              </label>
              <label>
                <span className="text-muted-foreground block">Max events</span>
                <input
                  type="number"
                  className="border rounded px-2 py-1 w-full"
                  value={filters.maxEvents ?? 500}
                  onChange={(e) => setFilters((f) => ({ ...f, maxEvents: parseInt(e.target.value, 10) || undefined }))}
                  min={1}
                  max={10000}
                />
              </label>
              <label className="md:col-span-2">
                <span className="text-muted-foreground block">Replay reason (audit)</span>
                <input
                  type="text"
                  className="border rounded px-2 py-1 w-full"
                  value={filters.replayReason ?? ''}
                  onChange={(e) => setFilters((f) => ({ ...f, replayReason: e.target.value || undefined }))}
                  placeholder="Optional reason for audit trail"
                />
              </label>
            </div>
            <div className="flex gap-2 mt-4">
              <Button variant="outline" onClick={runPreview} disabled={previewLoading}>
                {previewLoading ? <LoadingSpinner className="w-4 h-4" /> : <RotateCcw className="w-4 h-4" />}
                Run preview (dry-run)
              </Button>
              {(() => {
                const targetId = filters.replayTarget ?? 'EventStore';
                const descriptor = targets.find((t) => t.id === targetId);
                const canExecute = !descriptor || descriptor.supported;
                return (
                  <>
                    <Button variant="default" onClick={() => runExecute(false)} disabled={executeLoading || !canExecute} title={!canExecute ? 'Selected target is not supported for execute' : ''}>
                      {executeLoading ? <LoadingSpinner className="w-4 h-4" /> : <Play className="w-4 h-4" />}
                      Execute replay (sync)
                    </Button>
                    <Button variant="outline" onClick={() => runExecute(true)} disabled={executeLoading || !canExecute} title={!canExecute ? 'Selected target is not supported for execute' : ''}>
                      Execute in background
                    </Button>
                  </>
                );
              })()}
            </div>
          </Card>

          {previewResult && (
            <Card className="p-4">
              <h3 className="font-medium mb-3">Preview result</h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm mb-4">
                <div>
                  <span className="text-muted-foreground block">Total matched</span>
                  <span className="font-medium">{previewResult.totalMatched}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Evaluated</span>
                  <span className="font-medium">{previewResult.evaluatedCount}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Eligible</span>
                  <span className="font-medium text-green-600">{previewResult.eligibleCount}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Blocked</span>
                  <span className="font-medium text-amber-600">{previewResult.blockedCount}</span>
                </div>
              </div>
              {(previewResult.orderingStrategyId || previewResult.orderingStrategyDescription || previewResult.orderingGuaranteeLevel) && (
                <div className="mb-4 text-sm">
                  <span className="text-muted-foreground block">Ordering</span>
                  <span>{previewResult.orderingStrategyDescription ?? previewResult.orderingStrategyId ?? '—'}{previewResult.orderingGuaranteeLevel ? ` · ${previewResult.orderingGuaranteeLevel}` : ''}</span>
                  {previewResult.orderingDegradedReason && <p className="text-amber-700 mt-1">{previewResult.orderingDegradedReason}</p>}
                </div>
              )}
              {previewResult.estimatedAffectedEntityTypes && previewResult.estimatedAffectedEntityTypes.length > 0 && (
                <p className="text-sm text-muted-foreground mb-2">
                  Estimated affected entity types: {previewResult.estimatedAffectedEntityTypes.join(', ')}
                </p>
              )}
              {previewResult.limitations && previewResult.limitations.length > 0 && (
                <div className="mb-4">
                  <span className="text-muted-foreground text-sm block mb-1">Limitations</span>
                  <ul className="list-disc list-inside text-sm text-muted-foreground">
                    {previewResult.limitations.map((l, i) => (
                      <li key={i}>{l}</li>
                    ))}
                  </ul>
                </div>
              )}
              {(previewResult.projectionPreviewQuality != null || (previewResult.affectedProjectionCategories && previewResult.affectedProjectionCategories.length > 0)) && (
                <div className="mb-4 rounded border bg-muted/20 p-3">
                  <span className="text-muted-foreground text-sm font-medium block mb-1">Projection preview</span>
                  {previewResult.projectionPreviewQuality === 'Estimated' && (
                    <>
                      <p className="text-sm">Quality: Estimated (count/categories only)</p>
                      {previewResult.affectedProjectionCategories && previewResult.affectedProjectionCategories.length > 0 && (
                        <p className="text-sm mt-1">Affected categories: {previewResult.affectedProjectionCategories.join(', ')}</p>
                      )}
                      {previewResult.estimatedChangedEntityCount != null && (
                        <p className="text-sm mt-1">Estimated changed rows: {previewResult.estimatedChangedEntityCount}</p>
                      )}
                    </>
                  )}
                  {previewResult.projectionPreviewQuality === 'Unavailable' && previewResult.projectionPreviewUnavailableReason && (
                    <p className="text-sm text-amber-700">{previewResult.projectionPreviewUnavailableReason}</p>
                  )}
                </div>
              )}
              {(previewResult.ledgerWritesExpected != null || (previewResult.ledgerFamiliesAffected && previewResult.ledgerFamiliesAffected.length > 0) || previewResult.ledgerPreviewUnavailableReason) && (
                <div className="mb-4 rounded border bg-muted/20 p-3">
                  <span className="text-muted-foreground text-sm font-medium block mb-1">Ledger impact</span>
                  {previewResult.ledgerWritesExpected === true && (
                    <>
                      <p className="text-sm">Ledger writes expected (idempotent).</p>
                      {previewResult.ledgerFamiliesAffected && previewResult.ledgerFamiliesAffected.length > 0 && (
                        <p className="text-sm mt-1">Families: {previewResult.ledgerFamiliesAffected.join(', ')}</p>
                      )}
                      {previewResult.ledgerDerivedProjectionsImpacted && previewResult.ledgerDerivedProjectionsImpacted.length > 0 && (
                        <p className="text-sm mt-1">Ledger-derived views that may be updated: {previewResult.ledgerDerivedProjectionsImpacted.join(', ')}</p>
                      )}
                    </>
                  )}
                  {previewResult.ledgerPreviewUnavailableReason && !previewResult.ledgerWritesExpected && (
                    <p className="text-sm text-muted-foreground">{previewResult.ledgerPreviewUnavailableReason}</p>
                  )}
                </div>
              )}
              {previewResult.blockedReasons && previewResult.blockedReasons.length > 0 && (
                <div className="mb-4">
                  <span className="text-muted-foreground text-sm block mb-1">Blocked reasons</span>
                  <ul className="list-disc list-inside text-sm text-amber-700 flex flex-wrap gap-x-4">
                    {previewResult.blockedReasons.map((r, i) => (
                      <li key={i}>{r}</li>
                    ))}
                  </ul>
                </div>
              )}
              {previewResult.eventTypesAffected && previewResult.eventTypesAffected.length > 0 && (
                <p className="text-sm text-muted-foreground">
                  Event types affected: {previewResult.eventTypesAffected.join(', ')}
                </p>
              )}
              {previewResult.sampleEvents && previewResult.sampleEvents.length > 0 && (
                <div className="mt-4">
                  <span className="text-sm font-medium block mb-2">Sample events</span>
                  <div className="overflow-x-auto border rounded">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b bg-muted/50">
                          <th className="text-left p-2">Event ID</th>
                          <th className="text-left p-2">Type</th>
                          <th className="text-left p-2">Occurred (UTC)</th>
                          <th className="text-left p-2">Status</th>
                        </tr>
                      </thead>
                      <tbody>
                        {previewResult.sampleEvents.slice(0, 10).map((e) => (
                          <tr key={e.eventId} className="border-b last:border-0">
                            <td className="p-2">
                              <Link to={`/admin/event-bus?eventId=${e.eventId}`} className="text-primary hover:underline">
                                {e.eventId.slice(0, 8)}…
                              </Link>
                            </td>
                            <td className="p-2">{e.eventType}</td>
                            <td className="p-2">{e.occurredAtUtc ? new Date(e.occurredAtUtc).toLocaleString() : '-'}</td>
                            <td className="p-2">{e.status}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </Card>
          )}
        </div>
      )}

      {tab === 'history' && (
        <div className="space-y-4">
          {historyLoading ? (
            <LoadingSpinner />
          ) : (
            <>
              <div className="overflow-x-auto border rounded">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="text-left p-2">Requested (UTC)</th>
                      <th className="text-left p-2">Target / Mode</th>
                      <th className="text-left p-2">Reason</th>
                      <th className="text-left p-2">Matched</th>
                      <th className="text-left p-2">Eligible</th>
                      <th className="text-left p-2">Executed</th>
                      <th className="text-left p-2">Succeeded</th>
                      <th className="text-left p-2">Failed</th>
                      <th className="text-left p-2">Skipped</th>
                      <th className="text-left p-2">Duration</th>
                      <th className="text-left p-2">State</th>
                      <th className="text-left p-2">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {historyData.items.map((o) => (
                      <tr key={o.id} className="border-b last:border-0">
                        <td className="p-2">{o.requestedAtUtc ? new Date(o.requestedAtUtc).toLocaleString() : '-'}</td>
                        <td className="p-2 text-xs">
                          {o.replayTarget ?? 'EventStore'} / {o.replayMode ?? 'Apply'}
                          {o.retriedFromOperationId && (
                            <span className="block text-muted-foreground" title="Rerun of another operation">
                              ← <Link to={`/admin/operational-replay/${o.retriedFromOperationId}`} className="text-primary hover:underline">from {o.retriedFromOperationId.slice(0, 8)}…</Link>
                            </span>
                          )}
                        </td>
                        <td className="p-2 max-w-[140px] truncate" title={o.replayReason ?? ''}>{o.replayReason ?? '-'}</td>
                        <td className="p-2">{o.totalMatched ?? '-'}</td>
                        <td className="p-2">{o.totalEligible ?? '-'}</td>
                        <td className="p-2">{o.totalExecuted ?? '-'}</td>
                        <td className="p-2 text-green-600">{o.totalSucceeded ?? '-'}</td>
                        <td className="p-2 text-red-600">{o.totalFailed ?? '-'}</td>
                        <td className="p-2">{o.skippedCount ?? '-'}</td>
                        <td className="p-2">{o.durationMs != null ? `${(o.durationMs / 1000).toFixed(1)}s` : '-'}</td>
                        <td className="p-2"><StateBadge state={o.state} resumeRequired={o.resumeRequired} /></td>
                        <td className="p-2">
                          <div className="flex flex-wrap items-center gap-1">
                            <Link to={`/admin/operational-replay/${o.id}`} className="text-primary hover:underline inline-flex items-center gap-1">
                              Detail <ExternalLink className="w-3 h-3" />
                            </Link>
                            {(o.state === 'PartiallyCompleted' || o.state === 'Pending') && (
                              <Button variant="outline" size="sm" className="h-7 text-xs" disabled={resumeLoading} onClick={() => handleResume(o.id, true)}>
                                <RefreshCw className="w-3 h-3 mr-0.5" /> Resume
                              </Button>
                            )}
                            {(o.state === 'Failed' || o.state === 'Completed') && (o.totalFailed ?? 0) > 0 && (
                              <Button variant="outline" size="sm" className="h-7 text-xs" disabled={rerunFailedLoading} onClick={() => handleRerunFailed(o.id)}>
                                <Repeat className="w-3 h-3 mr-0.5" /> Rerun failed
                              </Button>
                            )}
                            {canCancel(o.state) && (
                              <Button variant="outline" size="sm" className="h-7 text-xs" disabled={cancelLoading} onClick={() => handleCancel(o.id)}>
                                Cancel
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {historyData.total > 20 && (
                <div className="flex justify-between items-center">
                  <span className="text-sm text-muted-foreground">Total {historyData.total} operations</span>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={historyPage <= 1} onClick={() => setHistoryPage((p) => p - 1)}>Previous</Button>
                    <Button variant="outline" size="sm" disabled={historyPage * 20 >= historyData.total} onClick={() => setHistoryPage((p) => p + 1)}>Next</Button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {showDetail && (
        <Card className="p-4 mt-6">
          <h3 className="font-medium mb-3">Replay operation detail</h3>
          {detailLoading ? (
            <LoadingSpinner />
          ) : detail ? (
            <div className="space-y-4">
              <div className="flex flex-wrap items-center gap-2 mb-2">
                <StateBadge state={detail.state} resumeRequired={detail.resumeRequired} />
                {detail.retriedFromOperationId && (
                  <span className="text-sm text-muted-foreground">
                    Rerun of <Link to={`/admin/operational-replay/${detail.retriedFromOperationId}`} className="text-primary hover:underline">{detail.retriedFromOperationId.slice(0, 8)}…</Link>
                    {detail.rerunReason && ` — ${detail.rerunReason}`}
                  </span>
                )}
                {(detail.state === 'PartiallyCompleted' || detail.state === 'Pending') && (
                  <Button variant="outline" size="sm" disabled={resumeLoading} onClick={() => handleResume(detail.id, false)}>
                    <RefreshCw className="w-3 h-3 mr-1" /> Resume (sync)
                  </Button>
                )}
                {(detail.state === 'PartiallyCompleted' || detail.state === 'Pending') && (
                  <Button variant="outline" size="sm" disabled={resumeLoading} onClick={() => handleResume(detail.id, true)}>
                    Resume (background)
                  </Button>
                )}
                {(detail.state === 'Failed' || detail.state === 'Completed') && (detail.totalFailed ?? 0) > 0 && (
                  <Button variant="outline" size="sm" disabled={rerunFailedLoading} onClick={() => handleRerunFailed(detail.id)}>
                    <Repeat className="w-3 h-3 mr-1" /> Rerun failed only
                  </Button>
                )}
                {canCancel(detail.state) && (
                  <Button variant="outline" size="sm" disabled={cancelLoading} onClick={() => handleCancel(detail.id)}>
                    Cancel
                  </Button>
                )}
              </div>
              {(detail.state === 'Running' || detail.state === 'PartiallyCompleted') && (progress || detail.processedCountAtLastCheckpoint != null) && (
                <div className="rounded border bg-muted/30 p-3 text-sm">
                  <span className="font-medium block mb-1">Progress</span>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
                    <span className="text-muted-foreground">Processed</span>
                    <span>{progress?.totalExecuted ?? detail.processedCountAtLastCheckpoint ?? detail.totalExecuted ?? '-'} / {progress?.totalEligible ?? detail.totalEligible ?? '?'}</span>
                    {progress?.progressPercent != null && (
                      <>
                        <span className="text-muted-foreground">Progress</span>
                        <span>{progress.progressPercent}%</span>
                      </>
                    )}
                    <span className="text-muted-foreground">Last checkpoint</span>
                    <span>
                      {progress?.lastCheckpointAtUtc || detail.lastCheckpointAtUtc
                        ? (() => {
                            const utc = progress?.lastCheckpointAtUtc || detail.lastCheckpointAtUtc!;
                            const d = new Date(utc);
                            const ago = Math.round((Date.now() - d.getTime()) / 60000);
                            return ago < 1 ? 'Just now' : ago < 60 ? `${ago} min ago` : `${Math.round(ago / 60)} h ago`;
                          })()
                        : '-'}
                      {progress?.lastCheckpointAtUtc || detail.lastCheckpointAtUtc ? ` (${new Date((progress?.lastCheckpointAtUtc || detail.lastCheckpointAtUtc)!).toLocaleString()})` : ''}
                    </span>
                  </div>
                  {progressPolling && <span className="text-muted-foreground text-xs">Polling…</span>}
                </div>
              )}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                <div><span className="text-muted-foreground block">ID</span><span className="font-mono text-xs">{detail.id}</span></div>
                <div><span className="text-muted-foreground block">Requested</span><span>{detail.requestedAtUtc ? new Date(detail.requestedAtUtc).toLocaleString() : '-'}</span></div>
                <div><span className="text-muted-foreground block">Target / Mode</span><span>{detail.replayTarget ?? 'EventStore'} / {detail.replayMode ?? 'Apply'}</span></div>
                <div><span className="text-muted-foreground block">Started (UTC)</span><span>{detail.startedAtUtc ? new Date(detail.startedAtUtc).toLocaleString() : '-'}</span></div>
                <div><span className="text-muted-foreground block">Executed</span><span>{detail.totalExecuted ?? '-'}</span></div>
                <div><span className="text-muted-foreground block">Succeeded / Failed</span><span>{detail.totalSucceeded ?? 0} / {detail.totalFailed ?? 0}</span></div>
                <div><span className="text-muted-foreground block">Skipped</span><span>{detail.skippedCount ?? '-'}</span></div>
                <div><span className="text-muted-foreground block">Duration</span><span>{detail.durationMs != null ? `${(detail.durationMs / 1000).toFixed(1)}s` : '-'}</span></div>
                {detail.orderingStrategyId && (
                  <div className="md:col-span-2"><span className="text-muted-foreground block">Ordering</span><span>{detail.orderingStrategyId}{detail.orderingGuaranteeLevel ? ` · ${detail.orderingGuaranteeLevel}` : ''}</span></div>
                )}
                {detail.orderingDegradedReason && (
                  <div className="md:col-span-2"><span className="text-muted-foreground block">Ordering note</span><span className="text-amber-700">{detail.orderingDegradedReason}</span></div>
                )}
              </div>
              {detail.errorSummary && (
                <p className="text-sm text-amber-700">Error summary: {detail.errorSummary}</p>
              )}
              {detail.replayCorrelationId && (
                <p className="text-sm text-muted-foreground">Replay correlation: {detail.replayCorrelationId}</p>
              )}
              {detail.retriedFromOperationId && (
                <p className="text-sm text-muted-foreground">
                  Retry lineage: this run retried only failed events from{' '}
                  <Link to={`/admin/operational-replay/${detail.retriedFromOperationId}`} className="text-primary hover:underline font-mono">
                    {detail.retriedFromOperationId.slice(0, 8)}…
                  </Link>
                  {detail.rerunReason ? ` — ${detail.rerunReason}` : ''}
                </p>
              )}
              {detail.eventResults && detail.eventResults.length > 0 && (
                <div>
                  <span className="text-sm font-medium block mb-2">Per-event results ({detail.eventResults.length})</span>
                  <div className="overflow-x-auto border rounded max-h-60 overflow-y-auto">
                    <table className="w-full text-sm">
                      <thead className="sticky top-0 bg-muted/50">
                        <tr className="border-b">
                          <th className="text-left p-2">Event ID</th>
                          <th className="text-left p-2">Type</th>
                          <th className="text-left p-2">Entity</th>
                          <th className="text-left p-2">Succeeded</th>
                          <th className="text-left p-2">Error / Skipped</th>
                          <th className="text-left p-2">Duration</th>
                          <th className="text-left p-2">Link</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detail.eventResults.map((e) => (
                          <tr key={e.eventId} className="border-b last:border-0">
                            <td className="p-2 font-mono text-xs">{e.eventId.slice(0, 8)}…</td>
                            <td className="p-2 text-xs">{e.eventType ?? '-'}</td>
                            <td className="p-2 text-xs">{e.entityType ?? '-'} {e.entityId ? `(${e.entityId.slice(0, 8)}…)` : ''}</td>
                            <td className="p-2">{e.succeeded ? 'Yes' : 'No'}</td>
                            <td className="p-2 max-w-[180px] truncate text-amber-700" title={e.errorMessage ?? e.skippedReason ?? ''}>{e.errorMessage ?? e.skippedReason ?? '-'}</td>
                            <td className="p-2">{e.durationMs != null ? `${e.durationMs}ms` : '-'}</td>
                            <td className="p-2">
                              <Link to={`/admin/event-bus?eventId=${e.eventId}`} className="text-primary hover:underline inline-flex items-center gap-1">
                                Event store <ExternalLink className="w-3 h-3" />
                              </Link>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          ) : (
            <p className="text-muted-foreground">Operation not found.</p>
          )}
        </Card>
      )}
    </PageShell>
  );
};

export default OperationalReplayPage;
