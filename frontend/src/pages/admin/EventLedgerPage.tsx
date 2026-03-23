import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { BookOpen, ExternalLink } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import {
  listLedgerFamilies,
  listLedgerEntries,
  getLedgerEntry,
  getOrderTimelineFromLedger,
  getUnifiedOrderHistoryFromLedger,
  type LedgerFamilyDescriptorDto,
  type LedgerEntryDto,
  type ListLedgerEntriesParams,
  type OrderTimelineItemDto,
  type UnifiedOrderHistoryItemDto
} from '../../api/eventStore';

const EventLedgerPage: React.FC = () => {
  const { user } = useAuth();
  const { showError } = useToast();
  const [families, setFamilies] = useState<LedgerFamilyDescriptorDto[]>([]);
  const [entries, setEntries] = useState<LedgerEntryDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [filters, setFilters] = useState<ListLedgerEntriesParams>({});
  const [loading, setLoading] = useState(false);
  const [detail, setDetail] = useState<LedgerEntryDto | null>(null);
  const [detailId, setDetailId] = useState<string | null>(null);
  const [orderTimelineOrderId, setOrderTimelineOrderId] = useState('');
  const [orderTimeline, setOrderTimeline] = useState<OrderTimelineItemDto[]>([]);
  const [orderTimelineLoading, setOrderTimelineLoading] = useState(false);
  const [unifiedOrderId, setUnifiedOrderId] = useState('');
  const [unifiedHistory, setUnifiedHistory] = useState<UnifiedOrderHistoryItemDto[]>([]);
  const [unifiedHistoryLoading, setUnifiedHistoryLoading] = useState(false);

  const canAdminJobs = Boolean(
    user?.permissions?.includes('jobs.admin') || (user?.roles ?? []).includes('SuperAdmin') || (user?.roles ?? []).includes('Admin')
  );

  const loadFamilies = useCallback(async () => {
    if (!canAdminJobs) return;
    try {
      const list = await listLedgerFamilies();
      setFamilies(Array.isArray(list) ? list : []);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load ledger families');
    }
  }, [canAdminJobs, showError]);

  const loadEntries = useCallback(async () => {
    if (!canAdminJobs) return;
    setLoading(true);
    try {
      const res = await listLedgerEntries({ ...filters, page, pageSize });
      setEntries(res.items ?? []);
      setTotal(res.total ?? 0);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load ledger entries');
    } finally {
      setLoading(false);
    }
  }, [canAdminJobs, filters, page, pageSize, showError]);

  useEffect(() => { loadFamilies(); }, [loadFamilies]);
  useEffect(() => { loadEntries(); }, [loadEntries]);

  const openDetail = useCallback(async (id: string) => {
    setDetailId(id);
    setDetail(null);
    try {
      const res = await getLedgerEntry(id);
      setDetail(res as LedgerEntryDto);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load entry');
    }
  }, [showError]);

  const loadOrderTimeline = useCallback(async () => {
    const orderId = orderTimelineOrderId.trim();
    if (!orderId || !canAdminJobs) return;
    setOrderTimelineLoading(true);
    setOrderTimeline([]);
    try {
      const list = await getOrderTimelineFromLedger({ orderId, limit: 100 });
      setOrderTimeline(Array.isArray(list) ? list : []);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load order timeline');
    } finally {
      setOrderTimelineLoading(false);
    }
  }, [orderTimelineOrderId, canAdminJobs, showError]);

  const loadUnifiedHistory = useCallback(async () => {
    const orderId = unifiedOrderId.trim();
    if (!orderId || !canAdminJobs) return;
    setUnifiedHistoryLoading(true);
    setUnifiedHistory([]);
    try {
      const list = await getUnifiedOrderHistoryFromLedger({ orderId, limit: 100 });
      setUnifiedHistory(Array.isArray(list) ? list : []);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load unified history');
    } finally {
      setUnifiedHistoryLoading(false);
    }
  }, [unifiedOrderId, canAdminJobs, showError]);

  return (
    <PageShell
      title="Event Ledger"
      description="Append-only operational event ledger: workflow transitions, order lifecycle, replay operations."
    >
      <div className="space-y-4">
        {families.length > 0 && (
          <Card className="p-4">
            <h3 className="font-medium mb-2">Ledger families</h3>
            <ul className="text-sm text-muted-foreground space-y-1">
              {families.map((f) => (
                <li key={f.id}>
                  <span className="font-medium text-foreground">{f.displayName}</span>
                  {f.orderingGuaranteeLevel && ` · ${f.orderingGuaranteeLevel}`}
                  {f.description && ` — ${f.description}`}
                </li>
              ))}
            </ul>
          </Card>
        )}

        <Card className="p-4">
          <div className="flex items-center justify-between mb-4">
            <h3 className="font-medium">Ledger entries</h3>
            <div className="flex gap-2">
              <select
                className="border rounded px-2 py-1 text-sm"
                value={filters.ledgerFamily ?? ''}
                onChange={(e) => setFilters((f) => ({ ...f, ledgerFamily: e.target.value || undefined }))}
              >
                <option value="">All families</option>
                {families.map((f) => (
                  <option key={f.id} value={f.id}>{f.displayName}</option>
                ))}
              </select>
              <Button variant="outline" size="sm" onClick={() => setPage(1) || loadEntries()}>Apply</Button>
            </div>
          </div>
          {loading ? (
            <LoadingSpinner />
          ) : (
            <>
              <div className="overflow-x-auto border rounded">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="text-left p-2">Occurred (UTC)</th>
                      <th className="text-left p-2">Recorded (UTC)</th>
                      <th className="text-left p-2">Family</th>
                      <th className="text-left p-2">Event type</th>
                      <th className="text-left p-2">Entity</th>
                      <th className="text-left p-2">Source event / Replay op</th>
                      <th className="text-left p-2">Ordering</th>
                      <th className="text-left p-2">Detail</th>
                    </tr>
                  </thead>
                  <tbody>
                    {entries.map((e) => (
                      <tr key={e.id} className="border-b last:border-0">
                        <td className="p-2">{e.occurredAtUtc ? new Date(e.occurredAtUtc).toLocaleString() : '-'}</td>
                        <td className="p-2">{e.recordedAtUtc ? new Date(e.recordedAtUtc).toLocaleString() : '-'}</td>
                        <td className="p-2"><span className="px-1.5 py-0.5 rounded bg-muted text-xs">{e.ledgerFamily}</span></td>
                        <td className="p-2 text-xs">{e.eventType}</td>
                        <td className="p-2 text-xs">{e.entityType ?? '-'} {e.entityId ? `(${String(e.entityId).slice(0, 8)}…)` : ''}</td>
                        <td className="p-2 text-xs">
                          {e.sourceEventId && (
                            <Link to={`/admin/event-bus?eventId=${e.sourceEventId}`} className="text-primary hover:underline">{String(e.sourceEventId).slice(0, 8)}…</Link>
                          )}
                          {e.replayOperationId && (
                            <Link to={`/admin/operational-replay/${e.replayOperationId}`} className="text-primary hover:underline inline-flex items-center gap-0.5"><ExternalLink className="w-3 h-3" /> {String(e.replayOperationId).slice(0, 8)}…</Link>
                          )}
                          {!e.sourceEventId && !e.replayOperationId && '—'}
                        </td>
                        <td className="p-2 text-xs">{e.orderingStrategyId ?? '—'}</td>
                        <td className="p-2">
                          <Button variant="ghost" size="sm" className="h-7 text-xs" onClick={() => openDetail(e.id)}>View</Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {total > pageSize && (
                <div className="flex justify-between items-center mt-2">
                  <span className="text-sm text-muted-foreground">Total {total} entries</span>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
                    <Button variant="outline" size="sm" disabled={page * pageSize >= total} onClick={() => setPage((p) => p + 1)}>Next</Button>
                  </div>
                </div>
              )}
            </>
          )}
        </Card>

        {canAdminJobs && (
          <>
            <Card className="p-4">
              <h3 className="font-medium mb-2">Unified order history</h3>
              <p className="text-sm text-muted-foreground mb-2">Merged WorkflowTransition + OrderLifecycle ledger entries for one order (single ordered timeline).</p>
              <div className="flex gap-2 items-center mb-2">
                <input
                  type="text"
                  placeholder="Order ID (GUID)"
                  className="border rounded px-2 py-1.5 text-sm font-mono w-80"
                  value={unifiedOrderId}
                  onChange={(e) => setUnifiedOrderId(e.target.value)}
                />
                <Button variant="outline" size="sm" onClick={loadUnifiedHistory} disabled={unifiedHistoryLoading || !unifiedOrderId.trim()}>
                  {unifiedHistoryLoading ? 'Loading…' : 'Load history'}
                </Button>
              </div>
              {unifiedHistory.length > 0 && (
                <div className="overflow-x-auto border rounded mt-2">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="text-left p-2">Occurred (UTC)</th>
                        <th className="text-left p-2">Family</th>
                        <th className="text-left p-2">Event type</th>
                        <th className="text-left p-2">Category</th>
                        <th className="text-left p-2">Prior → New status</th>
                        <th className="text-left p-2">Source event</th>
                      </tr>
                    </thead>
                    <tbody>
                      {unifiedHistory.map((u) => (
                        <tr key={u.ledgerEntryId} className="border-b last:border-0">
                          <td className="p-2">{u.occurredAtUtc ? new Date(u.occurredAtUtc).toLocaleString() : '-'}</td>
                          <td className="p-2"><span className="px-1.5 py-0.5 rounded bg-muted text-xs">{u.ledgerFamily}</span></td>
                          <td className="p-2">{u.eventType}</td>
                          <td className="p-2">{u.category ?? '—'}</td>
                          <td className="p-2">{u.priorStatus ?? '—'} → {u.newStatus ?? '—'}</td>
                          <td className="p-2 text-xs">
                            {u.sourceEventId ? (
                              <Link to={`/admin/event-bus?eventId=${u.sourceEventId}`} className="text-primary hover:underline">{String(u.sourceEventId).slice(0, 8)}…</Link>
                            ) : '—'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
            <Card className="p-4">
              <h3 className="font-medium mb-2">Order timeline (OrderLifecycle only)</h3>
              <p className="text-sm text-muted-foreground mb-2">Order lifecycle timeline from OrderLifecycle ledger entries (category-enriched).</p>
              <div className="flex gap-2 items-center mb-2">
                <input
                  type="text"
                  placeholder="Order ID (GUID)"
                  className="border rounded px-2 py-1.5 text-sm font-mono w-80"
                  value={orderTimelineOrderId}
                  onChange={(e) => setOrderTimelineOrderId(e.target.value)}
                />
                <Button variant="outline" size="sm" onClick={loadOrderTimeline} disabled={orderTimelineLoading || !orderTimelineOrderId.trim()}>
                  {orderTimelineLoading ? 'Loading…' : 'Load timeline'}
                </Button>
              </div>
              {orderTimeline.length > 0 && (
                <div className="overflow-x-auto border rounded mt-2">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="text-left p-2">Occurred (UTC)</th>
                        <th className="text-left p-2">Event type</th>
                        <th className="text-left p-2">Category</th>
                        <th className="text-left p-2">Prior → New status</th>
                        <th className="text-left p-2">Source event</th>
                      </tr>
                    </thead>
                    <tbody>
                      {orderTimeline.map((t) => (
                        <tr key={t.ledgerEntryId} className="border-b last:border-0">
                          <td className="p-2">{t.occurredAtUtc ? new Date(t.occurredAtUtc).toLocaleString() : '-'}</td>
                          <td className="p-2">{t.eventType}</td>
                          <td className="p-2">{t.category ?? '—'}</td>
                          <td className="p-2">{t.priorStatus ?? '—'} → {t.newStatus ?? '—'}</td>
                          <td className="p-2 text-xs">
                            {t.sourceEventId ? (
                              <Link to={`/admin/event-bus?eventId=${t.sourceEventId}`} className="text-primary hover:underline">{String(t.sourceEventId).slice(0, 8)}…</Link>
                            ) : '—'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          </>
        )}

        {detailId && detail && (
          <Card className="p-4">
            <h3 className="font-medium mb-2">Entry detail</h3>
            <dl className="grid grid-cols-2 gap-2 text-sm">
              <dt className="text-muted-foreground">Id</dt><dd className="font-mono text-xs">{detail.id}</dd>
              <dt className="text-muted-foreground">Family</dt><dd>{detail.ledgerFamily}</dd>
              <dt className="text-muted-foreground">Event type</dt><dd>{detail.eventType}</dd>
              <dt className="text-muted-foreground">Occurred (UTC)</dt><dd>{detail.occurredAtUtc ? new Date(detail.occurredAtUtc).toLocaleString() : '-'}</dd>
              <dt className="text-muted-foreground">Recorded (UTC)</dt><dd>{detail.recordedAtUtc ? new Date(detail.recordedAtUtc).toLocaleString() : '-'}</dd>
              <dt className="text-muted-foreground">Company</dt><dd>{detail.companyId ?? '—'}</dd>
              <dt className="text-muted-foreground">Entity</dt><dd>{detail.entityType ?? '—'} {detail.entityId ?? ''}</dd>
              <dt className="text-muted-foreground">Source event</dt><dd>{detail.sourceEventId ? <Link to={`/admin/event-bus?eventId=${detail.sourceEventId}`} className="text-primary hover:underline">{detail.sourceEventId}</Link> : '—'}</dd>
              <dt className="text-muted-foreground">Replay operation</dt><dd>{detail.replayOperationId ? <Link to={`/admin/operational-replay/${detail.replayOperationId}`} className="text-primary hover:underline">{detail.replayOperationId}</Link> : '—'}</dd>
              <dt className="text-muted-foreground">Ordering</dt><dd>{detail.orderingStrategyId ?? '—'}</dd>
            </dl>
            {detail.payloadSnapshot && (
              <pre className="mt-2 p-2 bg-muted rounded text-xs overflow-auto max-h-40">{detail.payloadSnapshot}</pre>
            )}
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default EventLedgerPage;
