import React, { useState, useEffect, useCallback } from 'react';
import {
  RefreshCw,
  Activity,
  CheckCircle,
  XCircle,
  AlertCircle,
  Copy,
  RotateCcw,
  ExternalLink,
  Bus
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import {
  listEvents,
  listFailedEvents,
  listDeadLetterEvents,
  getEvent,
  getEventRelatedLinks,
  getEventStoreDashboard,
  retryEvent,
  replayEvent,
  getReplayPolicy,
  listProcessingLog,
  getEventProcessing
} from '../../api/eventStore';
import type {
  EventStoreListItemDto,
  EventStoreDetailDto,
  EventStoreDashboardDto,
  EventStoreRelatedLinksDto,
  EventProcessingLogItemDto
} from '../../api/eventStore';

type TabId = 'overview' | 'recent' | 'failed' | 'deadletter' | 'processing';

const PAGE_SIZE = 20;

const EventBusMonitorPage: React.FC = () => {
  const { user } = useAuth();
  const { showError, showSuccess } = useToast();
  const [dashboard, setDashboard] = useState<EventStoreDashboardDto | null>(null);
  const [recentData, setRecentData] = useState<{ items: EventStoreListItemDto[]; total: number }>({ items: [], total: 0 });
  const [failedData, setFailedData] = useState<{ items: EventStoreListItemDto[]; total: number }>({ items: [], total: 0 });
  const [deadLetterData, setDeadLetterData] = useState<{ items: EventStoreListItemDto[]; total: number }>({ items: [], total: 0 });
  const [loading, setLoading] = useState(true);
  const [tab, setTab] = useState<TabId>('overview');
  const [detailEvent, setDetailEvent] = useState<EventStoreDetailDto | null>(null);
  const [relatedLinks, setRelatedLinks] = useState<EventStoreRelatedLinksDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [actioningId, setActioningId] = useState<string | null>(null);
  const [replayAllowed, setReplayAllowed] = useState<boolean | null>(null);

  // Filters for "Recent events"
  const [filters, setFilters] = useState({
    fromUtc: '',
    toUtc: '',
    eventType: '',
    status: '',
    companyId: '',
    correlationId: '',
    entityType: '',
    entityId: ''
  });
  const [recentPage, setRecentPage] = useState(1);

  // Handler processing (observability) tab
  const [processingData, setProcessingData] = useState<{ items: EventProcessingLogItemDto[]; total: number }>({ items: [], total: 0 });
  const [processingPage, setProcessingPage] = useState(1);
  const [processingFilters, setProcessingFilters] = useState({
    failedOnly: false,
    eventId: '',
    replayOperationId: '',
    correlationId: ''
  });
  const [detailProcessingLogs, setDetailProcessingLogs] = useState<EventProcessingLogItemDto[]>([]);

  const roles = user?.roles ?? [];
  const permissions = user?.permissions ?? [];
  const canViewJobs = Boolean(
    roles.includes('SuperAdmin') ||
    permissions.includes('jobs.view') ||
    (permissions.length === 0 && roles.includes('Admin'))
  );
  const canAdminJobs = Boolean(
    roles.includes('SuperAdmin') ||
    permissions.includes('jobs.admin') ||
    (permissions.length === 0 && roles.includes('Admin'))
  );

  const loadDashboard = useCallback(async () => {
    try {
      const from = new Date();
      from.setHours(0, 0, 0, 0);
      const to = new Date();
      const d = await getEventStoreDashboard(from.toISOString(), to.toISOString());
      setDashboard(d);
    } catch {
      setDashboard(null);
    }
  }, []);

  const loadRecent = useCallback(async () => {
    try {
      const params: Parameters<typeof listEvents>[0] = {
        page: recentPage,
        pageSize: PAGE_SIZE
      };
      if (filters.fromUtc) params.fromUtc = filters.fromUtc;
      if (filters.toUtc) params.toUtc = filters.toUtc;
      if (filters.eventType) params.eventType = filters.eventType;
      if (filters.status) params.status = filters.status;
      if (filters.companyId) params.companyId = filters.companyId;
      if (filters.correlationId) params.correlationId = filters.correlationId;
      if (filters.entityType) params.entityType = filters.entityType;
      if (filters.entityId) params.entityId = filters.entityId;
      const res = await listEvents(params);
      setRecentData({ items: res.items, total: res.total });
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to load events');
      setRecentData({ items: [], total: 0 });
    }
  }, [recentPage, filters, showError]);

  const loadFailed = useCallback(async () => {
    try {
      const res = await listFailedEvents(1, 50);
      setFailedData({ items: res.items, total: res.total });
    } catch {
      setFailedData({ items: [], total: 0 });
    }
  }, []);

  const loadDeadLetter = useCallback(async () => {
    try {
      const res = await listDeadLetterEvents(1, 50);
      setDeadLetterData({ items: res.items, total: res.total });
    } catch {
      setDeadLetterData({ items: [], total: 0 });
    }
  }, []);

  const loadProcessing = useCallback(async (overrides?: { page?: number }) => {
    try {
      const page = overrides?.page ?? processingPage;
      const params: Parameters<typeof listProcessingLog>[0] = {
        page,
        pageSize: 50,
        failedOnly: processingFilters.failedOnly
      };
      if (processingFilters.eventId) params.eventId = processingFilters.eventId;
      if (processingFilters.replayOperationId) params.replayOperationId = processingFilters.replayOperationId;
      if (processingFilters.correlationId) params.correlationId = processingFilters.correlationId;
      const res = await listProcessingLog(params);
      setProcessingData({ items: res.items, total: res.total });
      if (overrides?.page !== undefined) setProcessingPage(overrides.page);
    } catch {
      setProcessingData({ items: [], total: 0 });
    }
  }, [processingPage, processingFilters]);

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      await Promise.all([loadDashboard(), loadRecent(), loadFailed(), loadDeadLetter()]);
    } finally {
      setLoading(false);
    }
  }, [loadDashboard, loadRecent, loadFailed, loadDeadLetter]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  useEffect(() => {
    if (tab === 'recent') loadRecent();
  }, [tab, recentPage, loadRecent]);

  useEffect(() => {
    if (tab === 'processing') loadProcessing();
  }, [tab, processingPage, loadProcessing]);

  const handleRefresh = () => {
    showSuccess('Refreshing…');
    loadDashboard();
    loadFailed();
    loadDeadLetter();
    if (tab === 'recent') loadRecent();
    if (tab === 'processing') loadProcessing();
    setDetailEvent(null);
    setRelatedLinks(null);
    setDetailProcessingLogs([]);
  };

  const handleCopy = (text: string | null, label: string) => {
    if (!text) return;
    navigator.clipboard.writeText(text);
    showSuccess(`${label} copied`);
  };

  const openDetail = async (item: EventStoreListItemDto | { eventId: string }) => {
    setDetailEvent(null);
    setRelatedLinks(null);
    setDetailProcessingLogs([]);
    setDetailLoading(true);
    try {
      const [detail, links, processing] = await Promise.all([
        getEvent(item.eventId),
        getEventRelatedLinks(item.eventId).catch(() => null),
        getEventProcessing(item.eventId).catch(() => [] as EventProcessingLogItemDto[])
      ]);
      setDetailEvent(detail);
      setRelatedLinks(links ?? null);
      setDetailProcessingLogs(processing);
      const policy = await getReplayPolicy(detail.eventType).catch(() => null);
      setReplayAllowed(policy?.allowed ?? null);
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to load event details');
    } finally {
      setDetailLoading(false);
    }
  };

  const handleRetry = async (eventId: string) => {
    setActioningId(eventId);
    try {
      const result = await retryEvent(eventId);
      if (result.success) {
        showSuccess(result.message ?? 'Event queued for retry');
        loadFailed();
        loadDeadLetter();
        loadDashboard();
        if (detailEvent?.eventId === eventId) setDetailEvent(null);
      } else {
        showError(result.errorMessage ?? 'Retry failed');
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Retry failed');
    } finally {
      setActioningId(null);
    }
  };

  const handleReplay = async (eventId: string) => {
    setActioningId(eventId);
    try {
      const result = await replayEvent(eventId);
      if (result.success) {
        showSuccess(result.message ?? 'Event replayed');
        loadDashboard();
        loadRecent();
        if (detailEvent?.eventId === eventId) setDetailEvent(null);
      } else {
        showError(result.blockedReason ?? result.errorMessage ?? 'Replay failed');
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Replay failed');
    } finally {
      setActioningId(null);
    }
  };

  const renderEventRow = (
    item: EventStoreListItemDto,
    showActions = false
  ) => (
    <tr
      key={item.eventId}
      className="border-b last:border-0 hover:bg-muted/50 cursor-pointer"
      onClick={() => openDetail(item)}
    >
      <td className="py-2 font-medium">{item.eventType}</td>
      <td className="py-2">
        <span
          className={
            item.status === 'Processed'
              ? 'text-green-600'
              : item.status === 'Failed' || item.status === 'DeadLetter'
                ? 'text-red-600'
                : item.status === 'Processing'
                  ? 'text-blue-600'
                  : ''
          }
        >
          {item.status}
        </span>
      </td>
      <td className="py-2 text-muted-foreground text-sm">
        {item.occurredAtUtc ? new Date(item.occurredAtUtc).toLocaleString() : '—'}
      </td>
      <td className="py-2 text-muted-foreground text-sm max-w-[120px] truncate" title={item.companyId ?? ''}>
        {item.companyId ? `${item.companyId.slice(0, 8)}…` : '—'}
      </td>
      <td className="py-2 text-muted-foreground text-sm max-w-[140px] truncate" title={item.correlationId ?? ''}>
        {item.correlationId ? `${item.correlationId.slice(0, 8)}…` : '—'}
      </td>
      <td className="py-2 text-muted-foreground text-sm">{item.retryCount}</td>
      <td className="py-2 text-muted-foreground text-sm max-w-[120px] truncate" title={item.lastHandler ?? ''}>
        {item.lastHandler ?? '—'}
      </td>
      <td className="py-2 text-muted-foreground text-sm">{item.entityType ?? '—'}</td>
      <td className="py-2 text-muted-foreground text-sm max-w-[100px] truncate" title={item.entityId ?? ''}>
        {item.entityId ? `${String(item.entityId).slice(0, 8)}…` : '—'}
      </td>
      {showActions && canAdminJobs && (
        <td className="py-2" onClick={(e) => e.stopPropagation()}>
          {(item.status === 'Failed' || item.status === 'DeadLetter') && (
            <div className="flex gap-1">
              <Button
                variant="outline"
                size="sm"
                disabled={actioningId === item.eventId}
                onClick={() => handleRetry(item.eventId)}
              >
                {actioningId === item.eventId ? <RefreshCw className="animate-spin h-4 w-4" /> : <RotateCcw className="h-4 w-4" />}
                Retry
              </Button>
            </div>
          )}
        </td>
      )}
    </tr>
  );

  const tableHeader = (showActions: boolean) => (
    <thead>
      <tr className="border-b">
        <th className="text-left py-2">Event type</th>
        <th className="text-left py-2">Status</th>
        <th className="text-left py-2">Occurred (UTC)</th>
        <th className="text-left py-2">Company</th>
        <th className="text-left py-2">Correlation</th>
        <th className="text-left py-2">Retries</th>
        <th className="text-left py-2">Last handler</th>
        <th className="text-left py-2">Entity type</th>
        <th className="text-left py-2">Entity id</th>
        {showActions && <th className="text-left py-2"></th>}
      </tr>
    </thead>
  );

  if (!canViewJobs) {
    return (
      <PageShell title="Event Bus Monitor" breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Event Bus Monitor' }]}>
        <Card className="p-6 text-center">
          <p className="text-muted-foreground">You do not have permission to view the event bus.</p>
        </Card>
      </PageShell>
    );
  }

  const tabs: { id: TabId; label: string }[] = [
    { id: 'overview', label: 'Overview' },
    { id: 'recent', label: 'Recent events' },
    { id: 'processing', label: 'Handler processing' },
    { id: 'failed', label: 'Failed' },
    { id: 'deadletter', label: 'Dead-letter' }
  ];

  return (
    <PageShell
      title="Event Bus Monitor"
      breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Event Bus Monitor' }]}
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
                    <Bus />
                    <span className="text-sm font-medium">Events today</span>
                  </div>
                  <p className="text-2xl font-semibold mt-1">{dashboard.eventsToday}</p>
                </Card>
                <Card className="p-4">
                  <div className="flex items-center gap-2 text-green-600">
                    <CheckCircle />
                    <span className="text-sm font-medium">Processed %</span>
                  </div>
                  <p className="text-2xl font-semibold mt-1">{dashboard.processedPercent.toFixed(1)}%</p>
                </Card>
                <Card className="p-4">
                  <div className="flex items-center gap-2 text-red-600">
                    <XCircle />
                    <span className="text-sm font-medium">Failed %</span>
                  </div>
                  <p className="text-2xl font-semibold mt-1">{dashboard.failedPercent.toFixed(1)}%</p>
                </Card>
                <Card className="p-4">
                  <div className="flex items-center gap-2 text-amber-600">
                    <AlertCircle />
                    <span className="text-sm font-medium">Dead-letter</span>
                  </div>
                  <p className="text-2xl font-semibold mt-1">{dashboard.deadLetterCount}</p>
                </Card>
                <Card className="p-4">
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Activity />
                    <span className="text-sm font-medium">Total retries</span>
                  </div>
                  <p className="text-2xl font-semibold mt-1">{dashboard.totalRetryCount}</p>
                </Card>
              </div>
              {dashboard.topFailingEventTypes && dashboard.topFailingEventTypes.length > 0 && (
                <Card className="p-4">
                  <h3 className="font-medium mb-3">Top failing event types</h3>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2">Event type</th>
                          <th className="text-left py-2">Count</th>
                        </tr>
                      </thead>
                      <tbody>
                        {dashboard.topFailingEventTypes.map((x) => (
                          <tr key={x.eventType} className="border-b last:border-0">
                            <td className="py-2">{x.eventType}</td>
                            <td className="py-2">{x.count}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </Card>
              )}
              {dashboard.topFailingCompanies && dashboard.topFailingCompanies.length > 0 && (
                <Card className="p-4">
                  <h3 className="font-medium mb-3">Top failing companies</h3>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2">Company ID</th>
                          <th className="text-left py-2">Failed</th>
                          <th className="text-left py-2">Dead-letter</th>
                        </tr>
                      </thead>
                      <tbody>
                        {dashboard.topFailingCompanies.map((x, i) => (
                          <tr key={x.companyId ?? i} className="border-b last:border-0">
                            <td className="py-2 font-mono text-xs">{x.companyId ? `${x.companyId.slice(0, 8)}…` : '—'}</td>
                            <td className="py-2">{x.failedCount}</td>
                            <td className="py-2">{x.deadLetterCount}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </Card>
              )}
            </>
          )}
        </div>
      )}

      {tab === 'recent' && (
        <Card className="p-4">
          <h3 className="font-medium mb-3">Filters</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
            <input
              type="datetime-local"
              className="border rounded px-2 py-1.5 text-sm"
              placeholder="From (UTC)"
              value={filters.fromUtc}
              onChange={(e) => setFilters((f) => ({ ...f, fromUtc: e.target.value }))}
            />
            <input
              type="datetime-local"
              className="border rounded px-2 py-1.5 text-sm"
              placeholder="To (UTC)"
              value={filters.toUtc}
              onChange={(e) => setFilters((f) => ({ ...f, toUtc: e.target.value }))}
            />
            <input
              type="text"
              className="border rounded px-2 py-1.5 text-sm"
              placeholder="Event type"
              value={filters.eventType}
              onChange={(e) => setFilters((f) => ({ ...f, eventType: e.target.value }))}
            />
            <input
              type="text"
              className="border rounded px-2 py-1.5 text-sm"
              placeholder="Status"
              value={filters.status}
              onChange={(e) => setFilters((f) => ({ ...f, status: e.target.value }))}
            />
            <input
              type="text"
              className="border rounded px-2 py-1.5 text-sm"
              placeholder="Company ID"
              value={filters.companyId}
              onChange={(e) => setFilters((f) => ({ ...f, companyId: e.target.value }))}
            />
            <input
              type="text"
              className="border rounded px-2 py-1.5 text-sm"
              placeholder="Correlation ID"
              value={filters.correlationId}
              onChange={(e) => setFilters((f) => ({ ...f, correlationId: e.target.value }))}
            />
            <input
              type="text"
              className="border rounded px-2 py-1.5 text-sm"
              placeholder="Entity type"
              value={filters.entityType}
              onChange={(e) => setFilters((f) => ({ ...f, entityType: e.target.value }))}
            />
            <input
              type="text"
              className="border rounded px-2 py-1.5 text-sm"
              placeholder="Entity ID"
              value={filters.entityId}
              onChange={(e) => setFilters((f) => ({ ...f, entityId: e.target.value }))}
            />
          </div>
          <div className="flex justify-between items-center mb-3">
            <Button variant="outline" size="sm" onClick={() => { setRecentPage(1); loadRecent(); }}>
              Apply filters
            </Button>
            <span className="text-sm text-muted-foreground">Total: {recentData.total}</span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              {tableHeader(false)}
              <tbody>
                {recentData.items.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="py-6 text-center text-muted-foreground">
                      No events.
                    </td>
                  </tr>
                ) : (
                  recentData.items.map((item) => renderEventRow(item, false))
                )}
              </tbody>
            </table>
          </div>
          {recentData.total > PAGE_SIZE && (
            <div className="flex gap-2 mt-3">
              <Button
                variant="outline"
                size="sm"
                disabled={recentPage <= 1}
                onClick={() => setRecentPage((p) => p - 1)}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={recentPage * PAGE_SIZE >= recentData.total}
                onClick={() => setRecentPage((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          )}
        </Card>
      )}

      {tab === 'processing' && (
        <Card className="p-4">
          <h3 className="font-medium mb-3">Handler processing (observability)</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={processingFilters.failedOnly}
                onChange={(e) => setProcessingFilters((f) => ({ ...f, failedOnly: e.target.checked }))}
              />
              <span className="text-sm">Failed only</span>
            </label>
            <input
              type="text"
              className="border rounded px-2 py-1.5 text-sm font-mono"
              placeholder="Event ID (filter)"
              value={processingFilters.eventId}
              onChange={(e) => setProcessingFilters((f) => ({ ...f, eventId: e.target.value }))}
            />
            <input
              type="text"
              className="border rounded px-2 py-1.5 text-sm font-mono"
              placeholder="Replay operation ID"
              value={processingFilters.replayOperationId}
              onChange={(e) => setProcessingFilters((f) => ({ ...f, replayOperationId: e.target.value }))}
            />
            <input
              type="text"
              className="border rounded px-2 py-1.5 text-sm"
              placeholder="Correlation ID"
              value={processingFilters.correlationId}
              onChange={(e) => setProcessingFilters((f) => ({ ...f, correlationId: e.target.value }))}
            />
          </div>
          <div className="flex justify-between items-center mb-3">
            <Button variant="outline" size="sm" onClick={() => loadProcessing({ page: 1 })}>
              Apply filters
            </Button>
            <span className="text-sm text-muted-foreground">Total: {processingData.total}</span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2">Event ID</th>
                  <th className="text-left py-2">Handler</th>
                  <th className="text-left py-2">State</th>
                  <th className="text-left py-2">Started (UTC)</th>
                  <th className="text-left py-2">Completed (UTC)</th>
                  <th className="text-left py-2">Attempts</th>
                  <th className="text-left py-2">Error</th>
                  <th className="text-left py-2">Replay op.</th>
                  <th className="text-left py-2">Correlation</th>
                </tr>
              </thead>
              <tbody>
                {processingData.items.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="py-6 text-center text-muted-foreground">
                      No handler processing rows.
                    </td>
                  </tr>
                ) : (
                  processingData.items.map((row) => (
                    <tr
                      key={row.id}
                      className="border-b last:border-0 hover:bg-muted/50 cursor-pointer"
                      onClick={() => openDetail({ eventId: row.eventId })}
                    >
                      <td className="py-2 font-mono text-xs max-w-[120px] truncate" title={row.eventId}>{row.eventId.slice(0, 8)}…</td>
                      <td className="py-2">{row.handlerName}</td>
                      <td className="py-2">
                        <span
                          className={
                            row.state === 'Completed'
                              ? 'text-green-600'
                              : row.state === 'Failed'
                                ? 'text-red-600'
                                : 'text-blue-600'
                          }
                        >
                          {row.state}
                        </span>
                      </td>
                      <td className="py-2 text-muted-foreground">{row.startedAtUtc ? new Date(row.startedAtUtc).toLocaleString() : '—'}</td>
                      <td className="py-2 text-muted-foreground">{row.completedAtUtc ? new Date(row.completedAtUtc).toLocaleString() : '—'}</td>
                      <td className="py-2">{row.attemptCount}</td>
                      <td className="py-2 text-red-600 max-w-[180px] truncate" title={row.error ?? ''}>{row.error ?? '—'}</td>
                      <td className="py-2 text-muted-foreground font-mono text-xs max-w-[100px] truncate" title={row.replayOperationId ?? ''}>
                        {row.replayOperationId ? `${row.replayOperationId.slice(0, 8)}…` : '—'}
                      </td>
                      <td className="py-2 text-muted-foreground font-mono text-xs max-w-[100px] truncate" title={row.correlationId ?? ''}>
                        {row.correlationId ? `${row.correlationId.slice(0, 8)}…` : '—'}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
          {processingData.total > 50 && (
            <div className="flex gap-2 mt-3">
              <Button
                variant="outline"
                size="sm"
                disabled={processingPage <= 1}
                onClick={() => setProcessingPage((p) => p - 1)}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={processingPage * 50 >= processingData.total}
                onClick={() => setProcessingPage((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          )}
        </Card>
      )}

      {tab === 'failed' && (
        <Card className="p-4">
          <h3 className="font-medium mb-3">Failed events</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              {tableHeader(true)}
              <tbody>
                {failedData.items.length === 0 ? (
                  <tr>
                    <td colSpan={10} className="py-6 text-center text-muted-foreground">
                      No failed events.
                    </td>
                  </tr>
                ) : (
                  failedData.items.map((item) => renderEventRow(item, true))
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {tab === 'deadletter' && (
        <Card className="p-4">
          <h3 className="font-medium mb-3">Dead-letter events</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              {tableHeader(true)}
              <tbody>
                {deadLetterData.items.length === 0 ? (
                  <tr>
                    <td colSpan={10} className="py-6 text-center text-muted-foreground">
                      No dead-letter events.
                    </td>
                  </tr>
                ) : (
                  deadLetterData.items.map((item) => renderEventRow(item, true))
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {/* Event detail drawer */}
      {(detailEvent || detailLoading) && (
        <div
          className="fixed inset-0 z-50 bg-black/50 flex justify-end"
          onClick={() => { setDetailEvent(null); setRelatedLinks(null); }}
        >
          <div
            className="w-full max-w-2xl bg-background shadow-lg overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="p-4 border-b flex justify-between items-center">
              <h3 className="font-semibold">Event detail</h3>
              <Button variant="ghost" size="sm" onClick={() => { setDetailEvent(null); setRelatedLinks(null); }}>
                Close
              </Button>
            </div>
            <div className="p-4 space-y-4 text-sm">
              {detailLoading ? (
                <LoadingSpinner />
              ) : detailEvent ? (
                <>
                  <div><span className="text-muted-foreground">Event ID:</span> <code className="text-xs bg-muted px-1 rounded">{detailEvent.eventId}</code></div>
                  <div><span className="text-muted-foreground">Type:</span> {detailEvent.eventType}</div>
                  <div><span className="text-muted-foreground">Status:</span> {detailEvent.status}</div>
                  <div><span className="text-muted-foreground">Occurred (UTC):</span> {detailEvent.occurredAtUtc ? new Date(detailEvent.occurredAtUtc).toLocaleString() : '—'}</div>
                  <div className="flex items-center gap-2">
                    <span className="text-muted-foreground">Correlation ID:</span>
                    <code className="text-xs bg-muted px-1 rounded truncate max-w-[200px]" title={detailEvent.correlationId ?? ''}>{detailEvent.correlationId ?? '—'}</code>
                    {detailEvent.correlationId && (
                      <Button variant="ghost" size="sm" onClick={() => handleCopy(detailEvent.correlationId, 'Correlation ID')}>
                        <Copy className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-3">
                    <Link
                      to={`/admin/trace-explorer?eventId=${encodeURIComponent(detailEvent.eventId)}`}
                      className="text-primary hover:underline inline-flex items-center gap-1 text-sm"
                    >
                      View full trace in Trace Explorer <ExternalLink className="h-3 w-3" />
                    </Link>
                    {detailEvent.correlationId && (
                      <Link
                        to={`/admin/trace-explorer?correlationId=${encodeURIComponent(detailEvent.correlationId)}`}
                        className="text-primary hover:underline inline-flex items-center gap-1 text-sm"
                      >
                        View trace by Correlation ID <ExternalLink className="h-3 w-3" />
                      </Link>
                    )}
                  </div>
                  <div><span className="text-muted-foreground">Last handler:</span> {detailEvent.lastHandler ?? '—'}</div>
                  {detailEvent.lastError && (
                    <div>
                      <span className="text-muted-foreground">Last error:</span>
                      <p className="mt-1 text-red-600 break-words">{detailEvent.lastError}</p>
                    </div>
                  )}
                  {detailEvent.lastErrorAtUtc && (
                    <div><span className="text-muted-foreground">Last error at (UTC):</span> {new Date(detailEvent.lastErrorAtUtc).toLocaleString()}</div>
                  )}
                  <div>
                    <span className="text-muted-foreground">Payload (JSON):</span>
                    <pre className="mt-1 p-2 bg-muted rounded text-xs overflow-auto max-h-48">{detailEvent.payload || '{}'}</pre>
                  </div>

                  {/* Correlation links: related JobRuns and WorkflowJobs */}
                  {relatedLinks && (relatedLinks.jobRuns.length > 0 || relatedLinks.workflowJobs.length > 0) && (
                    <div className="space-y-3">
                      <h4 className="font-medium">Related (same correlation)</h4>
                      {relatedLinks.jobRuns.length > 0 && (
                        <div>
                          <p className="text-muted-foreground text-xs mb-1">Job runs</p>
                          <div className="overflow-x-auto border rounded">
                            <table className="w-full text-xs">
                              <thead>
                                <tr className="border-b bg-muted/50">
                                  <th className="text-left py-1.5 px-2">Job</th>
                                  <th className="text-left py-1.5 px-2">Status</th>
                                  <th className="text-left py-1.5 px-2">Started</th>
                                  <th className="text-left py-1.5 px-2"></th>
                                </tr>
                              </thead>
                              <tbody>
                                {relatedLinks.jobRuns.map((r) => (
                                  <tr key={r.id} className="border-b last:border-0">
                                    <td className="py-1.5 px-2">{r.jobName || r.jobType}</td>
                                    <td className="py-1.5 px-2">{r.status}</td>
                                    <td className="py-1.5 px-2">{new Date(r.startedAtUtc).toLocaleString()}</td>
                                    <td className="py-1.5 px-2">
                                      <a
                                        href="/admin/background-jobs"
                                        className="text-primary underline inline-flex items-center gap-0.5"
                                        target="_blank"
                                        rel="noopener noreferrer"
                                      >
                                        Open Background Jobs <ExternalLink className="h-3 w-3" />
                                      </a>
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          </div>
                        </div>
                      )}
                      {relatedLinks.workflowJobs.length > 0 && (
                        <div>
                          <p className="text-muted-foreground text-xs mb-1">Workflow jobs</p>
                          <div className="overflow-x-auto border rounded">
                            <table className="w-full text-xs">
<thead>
                              <tr className="border-b bg-muted/50">
                                  <th className="text-left py-1.5 px-2">Entity</th>
                                  <th className="text-left py-1.5 px-2">State</th>
                                  <th className="text-left py-1.5 px-2">Created</th>
                                  <th className="text-left py-1.5 px-2"></th>
                                </tr>
                              </thead>
                              <tbody>
                                {relatedLinks.workflowJobs.map((w) => (
                                  <tr key={w.id} className="border-b last:border-0">
                                    <td className="py-1.5 px-2">{w.entityType} {w.entityId}</td>
                                    <td className="py-1.5 px-2">{w.state}</td>
                                    <td className="py-1.5 px-2">{new Date(w.createdAt).toLocaleString()}</td>
                                    <td className="py-1.5 px-2">
                                      <Link
                                        to={`/admin/trace-explorer?workflowJobId=${encodeURIComponent(w.id)}`}
                                        className="text-primary underline inline-flex items-center gap-0.5"
                                      >
                                        View trace <ExternalLink className="h-3 w-3" />
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
                  )}

                  {/* Handler processing (observability) */}
                  {detailProcessingLogs.length > 0 && (
                    <div className="space-y-2">
                      <h4 className="font-medium">Handler processing</h4>
                      <div className="overflow-x-auto border rounded">
                        <table className="w-full text-xs">
                          <thead>
                            <tr className="border-b bg-muted/50">
                              <th className="text-left py-1.5 px-2">Handler</th>
                              <th className="text-left py-1.5 px-2">State</th>
                              <th className="text-left py-1.5 px-2">Started</th>
                              <th className="text-left py-1.5 px-2">Completed</th>
                              <th className="text-left py-1.5 px-2">Attempts</th>
                              <th className="text-left py-1.5 px-2">Replay op.</th>
                              <th className="text-left py-1.5 px-2">Error</th>
                            </tr>
                          </thead>
                          <tbody>
                            {detailProcessingLogs.map((p) => (
                              <tr key={p.id} className="border-b last:border-0">
                                <td className="py-1.5 px-2">{p.handlerName}</td>
                                <td className="py-1.5 px-2">
                                  <span className={p.state === 'Completed' ? 'text-green-600' : p.state === 'Failed' ? 'text-red-600' : 'text-blue-600'}>
                                    {p.state}
                                  </span>
                                </td>
                                <td className="py-1.5 px-2 text-muted-foreground">{new Date(p.startedAtUtc).toLocaleString()}</td>
                                <td className="py-1.5 px-2 text-muted-foreground">{p.completedAtUtc ? new Date(p.completedAtUtc).toLocaleString() : '—'}</td>
                                <td className="py-1.5 px-2">{p.attemptCount}</td>
                                <td className="py-1.5 px-2 font-mono text-muted-foreground" title={p.replayOperationId ?? ''}>
                                  {p.replayOperationId ? `${p.replayOperationId.slice(0, 8)}…` : '—'}
                                </td>
                                <td className="py-1.5 px-2 text-red-600 break-all max-w-[200px]" title={p.error ?? ''}>{p.error ?? '—'}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}

                  {canAdminJobs && (detailEvent.status === 'Failed' || detailEvent.status === 'DeadLetter') && (
                    <div className="flex gap-2 pt-2 border-t">
                      <Button
                        variant="outline"
                        disabled={actioningId === detailEvent.eventId}
                        onClick={() => handleRetry(detailEvent.eventId)}
                      >
                        {actioningId === detailEvent.eventId ? <RefreshCw className="animate-spin h-4 w-4" /> : <RotateCcw className="h-4 w-4" />}
                        Retry event
                      </Button>
                      {replayAllowed === true && (
                        <Button
                          variant="outline"
                          disabled={actioningId === detailEvent.eventId}
                          onClick={() => handleReplay(detailEvent.eventId)}
                        >
                          Replay event
                        </Button>
                      )}
                    </div>
                  )}
                </>
              ) : null}
            </div>
          </div>
        </div>
      )}
    </PageShell>
  );
};

export default EventBusMonitorPage;
