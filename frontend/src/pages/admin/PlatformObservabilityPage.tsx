import React, { useCallback, useEffect, useState } from 'react';
import {
  Activity,
  AlertTriangle,
  CheckCircle,
  ChevronRight,
  RefreshCw,
  Server,
  Bell,
  Mail,
  XCircle
} from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast, Badge } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import {
  getPlatformOperationsSummary,
  getTenantOperationsOverview,
  getTenantOperationsDetail,
  type PlatformOperationsSummaryDto,
  type TenantOperationsOverviewItemDto,
  type TenantOperationsDetailDto
} from '../../api/platformObservability';

const PlatformObservabilityPage: React.FC = () => {
  const { user } = useAuth();
  const { showError } = useToast();
  const [summary, setSummary] = useState<PlatformOperationsSummaryDto | null>(null);
  const [overview, setOverview] = useState<TenantOperationsOverviewItemDto[]>([]);
  const [detail, setDetail] = useState<TenantOperationsDetailDto | null>(null);
  const [detailTenantId, setDetailTenantId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);

  const isSuperAdmin = Boolean(user?.roles?.includes('SuperAdmin'));

  const load = useCallback(async () => {
    if (!isSuperAdmin) return;
    setLoading(true);
    try {
      const [summaryRes, overviewRes] = await Promise.all([
        getPlatformOperationsSummary(),
        getTenantOperationsOverview()
      ]);
      setSummary(summaryRes);
      setOverview(overviewRes ?? []);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load platform observability';
      showError(message);
      setSummary(null);
      setOverview([]);
    } finally {
      setLoading(false);
    }
  }, [isSuperAdmin, showError]);

  useEffect(() => {
    load();
  }, [load]);

  const openDetail = useCallback(async (tenantId: string) => {
    setDetailTenantId(tenantId);
    setDetailLoading(true);
    setDetail(null);
    try {
      const d = await getTenantOperationsDetail(tenantId);
      setDetail(d ?? null);
    } catch {
      setDetail(null);
    } finally {
      setDetailLoading(false);
    }
  }, []);

  const closeDetail = useCallback(() => {
    setDetailTenantId(null);
    setDetail(null);
  }, []);

  if (!isSuperAdmin) {
    return (
      <PageShell title="Platform Observability" description="Tenant-aware operational dashboard">
        <Card className="p-6">
          <p className="text-muted-foreground">
            You do not have permission to view platform observability. Only platform administrators can access this page.
          </p>
        </Card>
      </PageShell>
    );
  }

  const formatDate = (s: string | null) =>
    s ? new Date(s).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' }) : '—';

  return (
    <PageShell
      title="Platform Observability"
      description="Tenant-aware operational dashboard for platform health and fairness"
    >
      <div className="space-y-6">
        {/* Summary cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-6 gap-4">
          <Card className="p-4">
            <div className="flex items-center gap-2 text-muted-foreground mb-1">
              <Server className="h-4 w-4" />
              <span className="text-sm font-medium">Active tenants</span>
            </div>
            {loading ? (
              <LoadingSpinner className="h-6 w-6" />
            ) : (
              <span className="text-2xl font-semibold">{summary?.activeTenantsCount ?? '—'}</span>
            )}
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-2 text-muted-foreground mb-1">
              <XCircle className="h-4 w-4" />
              <span className="text-sm font-medium">Failed jobs today</span>
            </div>
            {loading ? (
              <LoadingSpinner className="h-6 w-6" />
            ) : (
              <span className="text-2xl font-semibold">{summary?.failedJobsToday ?? '—'}</span>
            )}
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-2 text-muted-foreground mb-1">
              <Bell className="h-4 w-4" />
              <span className="text-sm font-medium">Failed notifications</span>
            </div>
            {loading ? (
              <LoadingSpinner className="h-6 w-6" />
            ) : (
              <span className="text-2xl font-semibold">{summary?.failedNotificationsToday ?? '—'}</span>
            )}
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-2 text-muted-foreground mb-1">
              <Mail className="h-4 w-4" />
              <span className="text-sm font-medium">Failed integrations</span>
            </div>
            {loading ? (
              <LoadingSpinner className="h-6 w-6" />
            ) : (
              <span className="text-2xl font-semibold">{summary?.failedIntegrationsToday ?? '—'}</span>
            )}
          </Card>
          <Card className="p-4">
            <div className="flex items-center gap-2 text-muted-foreground mb-1">
              <AlertTriangle className="h-4 w-4" />
              <span className="text-sm font-medium">Tenants with warnings</span>
            </div>
            {loading ? (
              <LoadingSpinner className="h-6 w-6" />
            ) : (
              <span className="text-2xl font-semibold">{summary?.tenantsWithWarningsCount ?? '—'}</span>
            )}
          </Card>
          <Card className="p-4 flex flex-col justify-center">
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`h-4 w-4 mr-1 ${loading ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
          </Card>
        </div>

        {/* Tenant operations table */}
        <Card>
          <div className="p-4 border-b flex items-center justify-between">
            <h2 className="font-semibold">Tenant operations (last 24h)</h2>
          </div>
          {loading ? (
            <div className="p-8 flex justify-center">
              <LoadingSpinner />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="text-left p-3 font-medium">Tenant</th>
                    <th className="text-left p-3 font-medium">Status</th>
                    <th className="text-right p-3 font-medium">Requests</th>
                    <th className="text-right p-3 font-medium">Errors (jobs)</th>
                    <th className="text-right p-3 font-medium">Jobs ok / fail</th>
                    <th className="text-right p-3 font-medium">Notif ok / fail</th>
                    <th className="text-right p-3 font-medium">Integr ok / fail</th>
                    <th className="text-left p-3 font-medium">Last activity</th>
                    <th className="text-left p-3 font-medium">Warning</th>
                    <th className="w-10" />
                  </tr>
                </thead>
                <tbody>
                  {overview.map((row) => (
                    <tr
                      key={row.tenantId}
                      className="border-b hover:bg-muted/30 cursor-pointer"
                      onClick={() => openDetail(row.tenantId)}
                    >
                      <td className="p-3 font-medium">{row.tenantName}</td>
                      <td className="p-3">
                        {row.isActive ? (
                          <Badge variant="default" className="bg-green-600">Active</Badge>
                        ) : (
                          <Badge variant="secondary">Inactive</Badge>
                        )}
                      </td>
                      <td className="p-3 text-right">{row.requestCountLast24h}</td>
                      <td className="p-3 text-right">{row.jobFailuresLast24h}</td>
                      <td className="p-3 text-right">
                        {row.jobsOkLast24h} / {row.jobFailuresLast24h}
                      </td>
                      <td className="p-3 text-right">
                        {row.notificationsSentLast24h} / {row.notificationsFailedLast24h}
                      </td>
                      <td className="p-3 text-right">
                        {row.integrationsDeliveredLast24h} / {row.integrationsFailedLast24h}
                      </td>
                      <td className="p-3 text-muted-foreground">{formatDate(row.lastActivityUtc)}</td>
                      <td className="p-3">
                        {row.hasWarnings ? (
                          <Badge variant="destructive">{row.healthStatus}</Badge>
                        ) : (
                          <span className="text-muted-foreground flex items-center gap-1">
                            <CheckCircle className="h-4 w-4 text-green-600" />
                            {row.healthStatus}
                          </span>
                        )}
                      </td>
                      <td className="p-2">
                        <ChevronRight className="h-4 w-4 text-muted-foreground" />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {overview.length === 0 && !loading && (
                <div className="p-8 text-center text-muted-foreground">No tenant data.</div>
              )}
            </div>
          )}
        </Card>
      </div>

      {/* Tenant detail drawer */}
      {detailTenantId && (
        <div
          className="fixed inset-0 z-50 bg-black/30 flex justify-end"
          onClick={(e) => e.target === e.currentTarget && closeDetail()}
          onKeyDown={(e) => e.key === 'Escape' && closeDetail()}
          role="dialog"
          aria-label="Tenant detail"
        >
          <div
            className="w-full max-w-xl bg-background shadow-xl overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="p-4 border-b flex items-center justify-between sticky top-0 bg-background">
              <h3 className="font-semibold">
                {detail?.tenantName ?? overview.find((t) => t.tenantId === detailTenantId)?.tenantName ?? detailTenantId}
              </h3>
              <Button variant="ghost" size="sm" onClick={closeDetail}>
                Close
              </Button>
            </div>
            <div className="p-4 space-y-4">
              {detailLoading ? (
                <div className="flex justify-center py-8">
                  <LoadingSpinner />
                </div>
              ) : detail ? (
                <>
                  <div>
                    <h4 className="text-sm font-medium text-muted-foreground mb-2">Last 7 days</h4>
                    <div className="space-y-2 max-h-64 overflow-y-auto">
                      {detail.dailyBuckets.map((b) => (
                        <div
                          key={b.dateUtc}
                          className="flex items-center justify-between text-sm border-b pb-2"
                        >
                          <span className="text-muted-foreground">
                            {new Date(b.dateUtc).toLocaleDateString(undefined, { weekday: 'short', dateStyle: 'short' })}
                          </span>
                          <span>
                            Req: {b.requestCount} · Jobs: {b.jobsOk} ok, {b.jobFailures} fail · Notif: {b.notificationsSent}/{b.notificationsFailed} · Integ: {b.integrationsDelivered}/{b.integrationsFailed}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                  {detail.recentAnomalies.length > 0 && (
                    <div>
                      <h4 className="text-sm font-medium text-muted-foreground mb-2">Recent anomalies</h4>
                      <ul className="space-y-1 text-sm">
                        {detail.recentAnomalies.map((a) => (
                          <li key={a.id} className="flex items-center gap-2">
                            <AlertTriangle className="h-4 w-4 text-amber-500 shrink-0" />
                            <span>
                              {a.kind} ({a.severity}) — {formatDate(a.occurredAtUtc)}
                            </span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </>
              ) : (
                <p className="text-muted-foreground">Could not load tenant detail.</p>
              )}
            </div>
          </div>
        </div>
      )}
    </PageShell>
  );
};

export default PlatformObservabilityPage;
