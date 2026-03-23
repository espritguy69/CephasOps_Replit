import React, { useState, useEffect, useRef } from 'react';
import {
  Search,
  ExternalLink,
  ChevronDown,
  ChevronRight,
  Activity,
  Workflow,
  Zap,
  Clock
} from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import {
  getTraceByCorrelationId,
  getTraceByEventId,
  getTraceByJobRunId,
  getTraceByWorkflowJobId,
  getTraceByEntity,
  getTraceMetrics
} from '../../api/trace';
import type { TraceTimelineDto, TraceTimelineItemDto, TraceMetricsDto } from '../../api/trace';

const GUID_REGEX = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

function isGuid(s: string): boolean {
  return GUID_REGEX.test(s.trim());
}

function itemTypeIcon(itemType: string) {
  if (itemType.startsWith('Workflow')) return <Workflow className="h-4 w-4 text-blue-600" />;
  if (itemType.startsWith('Event')) return <Zap className="h-4 w-4 text-amber-600" />;
  if (itemType === 'BackgroundJobQueued') return <Clock className="h-4 w-4 text-slate-500" />;
  if (itemType.includes('Handler') || itemType.includes('Job')) return <Activity className="h-4 w-4 text-violet-600" />;
  return <Clock className="h-4 w-4 text-muted-foreground" />;
}

function statusBadgeClass(status: string | null): string {
  if (!status) return 'bg-muted text-muted-foreground';
  const s = status.toLowerCase();
  if (s === 'succeeded' || s === 'processed') return 'bg-green-100 text-green-800';
  if (s === 'failed' || s === 'deadletter') return 'bg-red-100 text-red-800';
  if (s === 'running' || s === 'processing' || s === 'pending') return 'bg-blue-100 text-blue-800';
  return 'bg-muted text-muted-foreground';
}

const TraceExplorerPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const { user } = useAuth();
  const { showError, showSuccess } = useToast();
  const [searchInput, setSearchInput] = useState('');
  const [entityType, setEntityType] = useState('Order');
  const [entityId, setEntityId] = useState('');
  const [timeline, setTimeline] = useState<TraceTimelineDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [metrics, setMetrics] = useState<TraceMetricsDto | null>(null);

  const roles = user?.roles ?? [];
  const permissions = user?.permissions ?? [];
  const canView = Boolean(
    roles.includes('SuperAdmin') ||
    permissions.includes('jobs.view') ||
    (permissions.length === 0 && roles.includes('Admin'))
  );

  // Run lookup from URL params (e.g. /admin/trace-explorer?eventId=... or ?correlationId=...)
  useEffect(() => {
    const eventId = searchParams.get('eventId');
    const jobRunId = searchParams.get('jobRunId');
    const workflowJobId = searchParams.get('workflowJobId');
    const correlationId = searchParams.get('correlationId');
    const entityTypeParam = searchParams.get('entityType');
    const entityIdParam = searchParams.get('entityId');
    if (eventId) {
      setSearchInput(eventId);
      return;
    }
    if (jobRunId) {
      setSearchInput(jobRunId);
      return;
    }
    if (workflowJobId) {
      setSearchInput(workflowJobId);
      return;
    }
    if (correlationId) {
      setSearchInput(correlationId);
      return;
    }
    if (entityTypeParam && entityIdParam) {
      setEntityType(entityTypeParam);
      setEntityId(entityIdParam);
      return;
    }
  }, [searchParams]);

  // Load minimal metrics (last 24h)
  useEffect(() => {
    if (!canView) return;
    getTraceMetrics().then(setMetrics).catch(() => setMetrics(null));
  }, [canView]);

  // Auto-run search when we have a value from URL (one-shot)
  const initialRunDone = useRef(false);
  useEffect(() => {
    if (!canView) return;
    const eventId = searchParams.get('eventId');
    const jobRunId = searchParams.get('jobRunId');
    const workflowJobId = searchParams.get('workflowJobId');
    const correlationId = searchParams.get('correlationId');
    const entityTypeParam = searchParams.get('entityType');
    const entityIdParam = searchParams.get('entityId');
    if (initialRunDone.current) return;
    const runByParams = async () => {
      if (eventId) {
        initialRunDone.current = true;
        setLoading(true);
        setTimeline(null);
        try {
          const t = await getTraceByEventId(eventId);
          if (t) setTimeline(t);
          else showError('Trace not found for this Event ID');
        } catch {
          showError('Trace not found for this Event ID');
        } finally {
          setLoading(false);
        }
        return;
      }
      if (jobRunId) {
        initialRunDone.current = true;
        setLoading(true);
        setTimeline(null);
        try {
          const t = await getTraceByJobRunId(jobRunId);
          if (t) setTimeline(t);
          else showError('Trace not found for this Job Run ID');
        } catch {
          showError('Trace not found for this Job Run ID');
        } finally {
          setLoading(false);
        }
        return;
      }
      if (workflowJobId) {
        initialRunDone.current = true;
        setLoading(true);
        setTimeline(null);
        try {
          const t = await getTraceByWorkflowJobId(workflowJobId);
          if (t) setTimeline(t);
          else showError('Trace not found for this Workflow Job ID');
        } catch {
          showError('Trace not found for this Workflow Job ID');
        } finally {
          setLoading(false);
        }
        return;
      }
      if (correlationId) {
        initialRunDone.current = true;
        setLoading(true);
        setTimeline(null);
        try {
          const t = await getTraceByCorrelationId(correlationId);
          setTimeline(t);
        } catch {
          showError('Trace not found for this Correlation ID');
        } finally {
          setLoading(false);
        }
        return;
      }
      if (entityTypeParam && entityIdParam) {
        initialRunDone.current = true;
        setLoading(true);
        setTimeline(null);
        try {
          const t = await getTraceByEntity(entityTypeParam, entityIdParam);
          setTimeline(t);
        } catch {
          showError('Trace not found for this entity');
        } finally {
          setLoading(false);
        }
      }
    };
    runByParams();
  }, [canView, searchParams, showError, showSuccess]);

  const runSearch = async () => {
    const trimmed = searchInput.trim();
    if (!trimmed) {
      showError('Enter a Correlation ID, Event ID, Job Run ID, or Workflow Job ID');
      return;
    }
    setLoading(true);
    setTimeline(null);
    try {
      if (isGuid(trimmed)) {
        let t = await getTraceByEventId(trimmed);
        if (t) {
          setTimeline(t);
          showSuccess('Timeline loaded by Event ID');
          return;
        }
        t = await getTraceByJobRunId(trimmed);
        if (t) {
          setTimeline(t);
          showSuccess('Timeline loaded by Job Run ID');
          return;
        }
        t = await getTraceByWorkflowJobId(trimmed);
        if (t) {
          setTimeline(t);
          showSuccess('Timeline loaded by Workflow Job ID');
          return;
        }
      }
      const t = await getTraceByCorrelationId(trimmed);
      setTimeline(t);
      showSuccess('Timeline loaded by Correlation ID');
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Trace not found');
      setTimeline(null);
    } finally {
      setLoading(false);
    }
  };

  const runEntitySearch = async () => {
    const eid = entityId.trim();
    if (!eid) {
      showError('Enter an Entity ID');
      return;
    }
    if (!GUID_REGEX.test(eid)) {
      showError('Entity ID must be a valid GUID');
      return;
    }
    setLoading(true);
    setTimeline(null);
    try {
      const t = await getTraceByEntity(entityType.trim(), eid);
      setTimeline(t);
      showSuccess(`Timeline loaded for ${entityType} ${eid}`);
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Trace not found');
      setTimeline(null);
    } finally {
      setLoading(false);
    }
  };

  const linkToRelated = (item: TraceTimelineItemDto) => {
    if (item.relatedIdKind === 'Event' && item.relatedId)
      return { to: '/admin/event-bus', label: 'Event Bus Monitor', q: `?eventId=${item.relatedId}` };
    if (item.relatedIdKind === 'JobRun' && item.relatedId)
      return { to: '/admin/trace-explorer', label: 'Trace Explorer', q: `?jobRunId=${item.relatedId}` };
    if (item.relatedIdKind === 'WorkflowJob' && item.relatedId)
      return { to: '/admin/trace-explorer', label: 'Trace Explorer', q: `?workflowJobId=${item.relatedId}` };
    return null;
  };

  if (!canView) {
    return (
      <PageShell title="Trace Explorer" breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Trace Explorer' }]}>
        <Card className="p-6 text-center">
          <p className="text-muted-foreground">You do not have permission to view the trace explorer.</p>
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Trace Explorer"
      breadcrumbs={[{ label: 'Admin', path: '/admin' }, { label: 'Trace Explorer' }]}
    >
      {metrics != null && (
        <Card className="p-3 mb-4">
          <p className="text-sm text-muted-foreground">
            Last 24h: <span className={metrics.failedEventsCount + metrics.deadLetterEventsCount > 0 ? 'text-amber-600' : ''}>{metrics.failedEventsCount} failed</span> / {metrics.deadLetterEventsCount} dead-letter events;
            <span className={metrics.failedJobRunsCount + metrics.deadLetterJobRunsCount > 0 ? ' text-amber-600' : ''}> {metrics.failedJobRunsCount} failed</span> / {metrics.deadLetterJobRunsCount} dead-letter job runs;
            {metrics.correlationChainsWithFailuresCount} correlation chain(s) with failures.
          </p>
        </Card>
      )}

      <Card className="p-4 mb-4">
        <h3 className="font-medium mb-3">Search by ID</h3>
        <p className="text-sm text-muted-foreground mb-2">
          Enter a Correlation ID, Event ID, Job Run ID, or Workflow Job ID to see the full execution chain.
        </p>
        <div className="flex flex-wrap gap-2">
          <input
            type="text"
            className="border rounded px-3 py-2 text-sm min-w-[280px]"
            placeholder="Correlation ID or GUID (Event / Job Run / Workflow Job)"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && runSearch()}
          />
          <Button onClick={runSearch} disabled={loading}>
            {loading ? <LoadingSpinner className="mr-2" /> : <Search className="h-4 w-4 mr-2" />}
            Search
          </Button>
        </div>
      </Card>

      <Card className="p-4 mb-4">
        <h3 className="font-medium mb-3">Search by Entity</h3>
        <div className="flex flex-wrap gap-2 items-center">
          <input
            type="text"
            className="border rounded px-2 py-1.5 text-sm w-28"
            placeholder="Type"
            value={entityType}
            onChange={(e) => setEntityType(e.target.value)}
          />
          <input
            type="text"
            className="border rounded px-3 py-1.5 text-sm min-w-[280px]"
            placeholder="Entity ID (GUID)"
            value={entityId}
            onChange={(e) => setEntityId(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && runEntitySearch()}
          />
          <Button variant="outline" onClick={runEntitySearch} disabled={loading}>
            Load timeline
          </Button>
        </div>
      </Card>

      {timeline && (
        <Card className="p-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-medium">
              Timeline ({timeline.lookupKind}: {timeline.lookupValue ?? '—'})
              {timeline.totalCount != null && timeline.items.length < timeline.totalCount && (
                <span className="text-muted-foreground font-normal ml-2">
                  (showing {timeline.items.length} of {timeline.totalCount})
                </span>
              )}
            </h3>
            <div className="flex gap-2">
              <Link to="/admin/event-bus">
                <Button variant="outline" size="sm">
                  Event Bus Monitor <ExternalLink className="h-3 w-3 ml-1" />
                </Button>
              </Link>
              <Link to="/admin/background-jobs">
                <Button variant="outline" size="sm">
                  Background Jobs <ExternalLink className="h-3 w-3 ml-1" />
                </Button>
              </Link>
            </div>
          </div>

          {timeline.items.length === 0 ? (
            <p className="text-muted-foreground py-6 text-center">No timeline items found.</p>
          ) : (
            <ul className="space-y-0">
              {timeline.items.map((item, idx) => {
                const key = `${item.itemType}-${item.relatedIdKind}-${item.relatedId}-${item.timestampUtc}-${idx}`;
                const expanded = expandedId === key;
                const link = linkToRelated(item);
                return (
                  <li key={key} className="border-b last:border-0">
                    <div
                      className="flex items-start gap-2 py-3 px-2 hover:bg-muted/30 cursor-pointer"
                      onClick={() => setExpandedId(expanded ? null : key)}
                    >
                      <span className="mt-0.5">{expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}</span>
                      <span className="mt-0.5">{itemTypeIcon(item.itemType)}</span>
                      <div className="flex-1 min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="font-medium text-sm">{item.title}</span>
                          {item.status && (
                            <span className={`text-xs px-1.5 py-0.5 rounded ${statusBadgeClass(item.status)}`}>
                              {item.status}
                            </span>
                          )}
                          <span className="text-xs text-muted-foreground">
                            {new Date(item.timestampUtc).toLocaleString()} UTC
                          </span>
                        </div>
                        {(item.detailSummary ?? item.summary) && (
                          <p className="text-sm text-muted-foreground mt-0.5 truncate max-w-xl">{item.detailSummary ?? item.summary}</p>
                        )}
                        {expanded && (
                          <div className="mt-2 text-xs text-muted-foreground space-y-1">
                            {item.correlationId && <div>Correlation: <code className="bg-muted px-1 rounded">{item.correlationId}</code></div>}
                            {item.source && <div>Source: {item.source}</div>}
                            {item.entityType && item.entityId && <div>Entity: {item.entityType} {item.entityId}</div>}
                            {item.handlerName && <div>Handler: {item.handlerName}</div>}
                            {item.relatedIdKind && item.relatedId && (
                              <div>
                                {link ? (
                                  <Link to={link.to + (link.q || '')} className="text-primary hover:underline" onClick={(e) => e.stopPropagation()}>
                                    Open in {link.label} →
                                  </Link>
                                ) : (
                                  <span>{item.relatedIdKind}: {item.relatedId}</span>
                                )}
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                      {link && (
                        <Link to={link.to + (link.q || '')} onClick={(e) => e.stopPropagation()}>
                          <Button variant="ghost" size="sm"><ExternalLink className="h-4 w-4" /></Button>
                        </Link>
                      )}
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </Card>
      )}
    </PageShell>
  );
};

export default TraceExplorerPage;
