import React, { useState, useEffect, useCallback } from 'react';
import {
  RefreshCw,
  BarChart3,
  AlertTriangle,
  Package,
  Wrench,
  MapPin,
  FileWarning,
  Clock,
  Link2,
  Copy,
  Shield,
  Layers,
  LayoutGrid
} from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import { PageShell } from '../../components/layout';
import { Card, Button, LoadingSpinner, useToast, Tooltip, Badge, EmptyState } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import { getSiInsights, type SiOperationalInsightsDto } from '../../api/siInsights';
import { getCompanies } from '../../api/companies';
import type { Company } from '../../types/companies';

const WINDOW_DAYS_OPTIONS = [30, 90, 180, 365];

function parseWindowDaysFromUrl(searchParams: URLSearchParams): number {
  const w = searchParams.get('windowDays');
  if (!w) return 90;
  const n = parseInt(w, 10);
  return WINDOW_DAYS_OPTIONS.includes(n) ? n : 90;
}

/** Order link with tooltip (full ID) and copy-ID action. */
function OrderLink({ orderId, children }: { orderId: string; children?: React.ReactNode }) {
  const { showSuccess } = useToast();
  const copyId = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    void navigator.clipboard.writeText(orderId).then(() => showSuccess('Order ID copied'));
  };
  return (
    <span className="inline-flex items-center gap-1">
      <Tooltip content={`Order ID: ${orderId} · Click copy to clipboard`} side="top">
        <Link to={`/orders/${orderId}`} className="text-primary hover:underline inline-flex items-center gap-1">
          <Link2 className="h-3 w-3 shrink-0" />
          {children ?? `${orderId.slice(0, 8)}…`}
        </Link>
      </Tooltip>
      <button
        type="button"
        onClick={copyId}
        className="p-0.5 rounded hover:bg-muted text-muted-foreground hover:text-foreground"
        aria-label="Copy order ID"
      >
        <Copy className="h-3 w-3" />
      </button>
    </span>
  );
}

const SiInsightsPage: React.FC = () => {
  const { user } = useAuth();
  const { showError } = useToast();
  const [searchParams, setSearchParams] = useSearchParams();
  const [data, setData] = useState<SiOperationalInsightsDto | null>(null);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [windowDays, setWindowDays] = useState(() => parseWindowDaysFromUrl(searchParams));
  const [companyId, setCompanyId] = useState(() => searchParams.get('companyId') ?? '');

  const isSuperAdmin = Boolean(user?.roles?.includes('SuperAdmin'));
  const canView = Boolean(
    user?.roles?.includes('SuperAdmin') ||
    user?.roles?.includes('Admin') ||
    user?.permissions?.includes('orders.view')
  );

  // Sync state from URL when user navigates (e.g. back/forward)
  useEffect(() => {
    setWindowDays(parseWindowDaysFromUrl(searchParams));
    setCompanyId(searchParams.get('companyId') ?? '');
  }, [searchParams]);

  const loadCompanies = useCallback(async () => {
    if (!isSuperAdmin) return;
    try {
      const list = await getCompanies();
      setCompanies(list);
      const fromUrl = searchParams.get('companyId') ?? '';
      if (list.length > 0 && !fromUrl) {
        const first = list[0].id;
        setCompanyId(first);
        setSearchParams((prev) => {
          const next = new URLSearchParams(prev);
          next.set('companyId', first);
          return next;
        });
      }
    } catch {
      setCompanies([]);
    }
  }, [isSuperAdmin, searchParams, setSearchParams]);

  const load = useCallback(async () => {
    if (!canView) return;
    setLoading(true);
    try {
      const params: { windowDays: number; companyId?: string } = {
        windowDays: Math.min(365, Math.max(1, windowDays))
      };
      if (isSuperAdmin && companyId) params.companyId = companyId;
      const insights = await getSiInsights(params);
      setData(insights);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load SI insights';
      showError(message);
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [canView, isSuperAdmin, companyId, windowDays, showError]);

  const setWindowDaysAndUrl = useCallback((value: number) => {
    setWindowDays(value);
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      next.set('windowDays', String(value));
      return next;
    });
  }, [setSearchParams]);

  const setCompanyIdAndUrl = useCallback((value: string) => {
    setCompanyId(value);
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      if (value) next.set('companyId', value);
      else next.delete('companyId');
      return next;
    });
  }, [setSearchParams]);

  useEffect(() => {
    loadCompanies();
  }, [loadCompanies]);

  useEffect(() => {
    load();
  }, [load]);

  if (!canView) {
    return (
      <PageShell title="SI Operational Insights">
        <Card className="p-6">
          <p className="text-muted-foreground">You do not have permission to view SI Operational Insights.</p>
        </Card>
      </PageShell>
    );
  }

  const cp = data?.completionPerformance;
  const rbp = data?.rescheduleBlockerPatterns;
  const mrp = data?.materialReplacementPatterns;
  const ar = data?.assuranceRework;
  const hotspots = data?.operationalHotspots;
  const buildingReliability = data?.buildingReliability;
  const orderFailurePatterns = data?.orderFailurePatterns;
  const patternClusters = data?.patternClusters;

  return (
    <PageShell
      title="SI Operational Insights"
      description="Service Installer completion performance, reschedule/blocker patterns, replacements, assurance, and hotspots. Read-only operational visibility."
    >
      <div className="space-y-6">
        {/* Filters */}
        <Card className="p-4">
          <div className="flex flex-wrap items-center gap-4">
            <Button variant="outline" size="sm" onClick={load} disabled={loading}>
              <RefreshCw className={`h-4 w-4 mr-1 ${loading ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
            <label className="flex items-center gap-2 text-sm">
              <span className="text-muted-foreground">Window</span>
              <select
                className="rounded border bg-background px-2 py-1 text-sm"
                value={windowDays}
                onChange={(e) => setWindowDaysAndUrl(Number(e.target.value))}
              >
                {WINDOW_DAYS_OPTIONS.map((d) => (
                  <option key={d} value={d}>
                    Last {d} days
                  </option>
                ))}
              </select>
            </label>
            {isSuperAdmin && companies.length > 1 && (
              <label className="flex items-center gap-2 text-sm">
                <span className="text-muted-foreground">Company</span>
                <select
                  className="rounded border bg-background px-2 py-1 text-sm min-w-[180px]"
                  value={companyId}
                  onChange={(e) => setCompanyIdAndUrl(e.target.value)}
                >
                  {companies.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.shortName || c.legalName}
                    </option>
                  ))}
                </select>
              </label>
            )}
          </div>
        </Card>

        {loading ? (
          <div className="flex justify-center py-12">
            <LoadingSpinner />
          </div>
        ) : !data ? (
          <Card className="p-6">
            <p className="text-muted-foreground">No data available. Check company context or try again.</p>
          </Card>
        ) : (
          <>
            {/* Data quality and meta */}
            {(data.dataQualityNote || (data.dataGaps && data.dataGaps.length > 0)) && (
              <Card className="p-4 border-amber-200 bg-amber-50/50 dark:bg-amber-950/20 dark:border-amber-800">
                <div className="flex items-start gap-2">
                  <FileWarning className="h-5 w-5 text-amber-600 shrink-0 mt-0.5" />
                  <div className="text-sm">
                    <p className="text-amber-800 dark:text-amber-200 mb-1">
                      Data gaps affect how you interpret these numbers. Use for trends and patterns, not absolute precision.
                    </p>
                    {data.dataQualityNote && <p className="text-amber-800 dark:text-amber-200">{data.dataQualityNote}</p>}
                    {data.dataGaps && data.dataGaps.length > 0 && (
                      <ul className="mt-2 list-disc list-inside text-amber-800 dark:text-amber-200">
                        {data.dataGaps.map((gap, i) => (
                          <li key={i}>{gap}</li>
                        ))}
                      </ul>
                    )}
                  </div>
                </div>
              </Card>
            )}

            {/* Overview summary cards */}
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
              <Card className="p-4">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Clock className="h-4 w-4" />
                  <span className="text-sm font-medium">Generated</span>
                </div>
                <p className="mt-2 text-sm font-medium">
                  {new Date(data.generatedAtUtc).toLocaleString()}
                </p>
                <p className="text-xs text-muted-foreground">Window: {data.windowDays} days</p>
              </Card>
              <Card className="p-4">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <BarChart3 className="h-4 w-4" />
                  <span className="text-sm font-medium">Completed (window)</span>
                </div>
                <p className="mt-2 text-2xl font-semibold">{cp?.ordersCompletedInWindow ?? 0}</p>
              </Card>
              <Card className="p-4">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <AlertTriangle className="h-4 w-4" />
                  <span className="text-sm font-medium">Stuck (&gt;{cp?.stuckThresholdDays ?? 7}d)</span>
                </div>
                <p className="mt-2 text-2xl font-semibold">{cp?.ordersStuckLongerThanDays?.length ?? 0}</p>
                <p className="text-xs text-muted-foreground mt-1">Orders in a non-terminal status longer than the threshold.</p>
              </Card>
              <Card className="p-4">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Wrench className="h-4 w-4" />
                  <span className="text-sm font-medium">High-churn orders</span>
                </div>
                <p className="mt-2 text-2xl font-semibold">{rbp?.ordersWithHighChurn?.length ?? 0}</p>
                <p className="text-xs text-muted-foreground mt-1">Many reschedule/blocker transitions; may need follow-up.</p>
              </Card>
            </div>

            {/* Completion performance */}
            <Card className="p-4">
              <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                <BarChart3 className="h-4 w-4" />
                Completion performance
              </h3>
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <p className="text-sm text-muted-foreground">Avg assigned → complete (hours)</p>
                  <p className="text-xl font-medium">
                    {cp?.averageAssignedToCompleteHours != null
                      ? cp.averageAssignedToCompleteHours.toFixed(1)
                      : '—'}
                  </p>
                  <p className="text-xs text-muted-foreground">Based on {cp?.ordersCompletedInWindow ?? 0} orders</p>
                </div>
              </div>
              {cp?.byInstaller && cp.byInstaller.length > 0 ? (
                <div className="mt-4">
                  <p className="text-sm font-medium text-muted-foreground mb-2">By installer (avg hours)</p>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2 px-2">Installer</th>
                          <th className="text-right py-2 px-2">Avg hours</th>
                          <th className="text-right py-2 px-2">Orders</th>
                        </tr>
                      </thead>
                      <tbody>
                        {cp.byInstaller.map((row, i) => (
                          <tr key={i} className="border-b">
                            <td className="py-2 px-2">{row.siDisplayName ?? row.siId ?? '—'}</td>
                            <td className="py-2 px-2 text-right">{row.averageHours.toFixed(1)}</td>
                            <td className="py-2 px-2 text-right">{row.orderCount}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ) : (
                cp && (
                  <div className="mt-4">
                    <EmptyState title="No installer breakdown" description="No completion-by-installer data in this window." className="py-8" />
                  </div>
                )
              )}
              {cp?.ordersStuckLongerThanDays && cp.ordersStuckLongerThanDays.length > 0 ? (
                <div className="mt-4">
                  <p className="text-sm font-medium text-muted-foreground mb-2">
                    Orders stuck &gt;{cp.stuckThresholdDays} days in current status
                  </p>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2 px-2">Order</th>
                          <th className="text-left py-2 px-2">Status</th>
                          <th className="text-right py-2 px-2">Days</th>
                        </tr>
                      </thead>
                      <tbody>
                        {cp.ordersStuckLongerThanDays.map((row) => (
                          <tr
                            key={row.orderId}
                            className={`border-b ${row.daysInCurrentStatus >= 14 ? 'bg-amber-50/70 dark:bg-amber-950/30' : ''}`}
                          >
                            <td className="py-2 px-2">
                              <OrderLink orderId={row.orderId}>View order</OrderLink>
                            </td>
                            <td className="py-2 px-2">{row.status}</td>
                            <td className="py-2 px-2 text-right">
                              <Badge variant={row.daysInCurrentStatus >= 14 ? 'warning' : 'secondary'}>
                                {row.daysInCurrentStatus}d
                              </Badge>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ) : (
                cp && (
                  <div className="mt-4">
                    <EmptyState title="No stuck orders" description={`No orders stuck longer than ${cp.stuckThresholdDays} days in this window.`} className="py-8" />
                  </div>
                )
              )}
            </Card>

            {/* Reschedule / blocker reasons and high-churn */}
            <Card className="p-4">
              <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                <AlertTriangle className="h-4 w-4" />
                Reschedule & blocker patterns
              </h3>
              <div className="grid gap-4 md:grid-cols-2">
                {rbp?.topRescheduleReasons && rbp.topRescheduleReasons.length > 0 ? (
                  <div>
                    <p className="text-sm font-medium text-muted-foreground mb-2">Top reschedule reasons</p>
                    <ul className="space-y-1 text-sm">
                      {rbp.topRescheduleReasons.map((r, i) => (
                        <li key={i} className="flex justify-between">
                          <span className="truncate mr-2">{r.reason ?? '—'}</span>
                          <Badge variant="secondary" className="shrink-0">{r.count}</Badge>
                        </li>
                      ))}
                    </ul>
                  </div>
                ) : (
                  rbp && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground mb-2">Top reschedule reasons</p>
                      <EmptyState title="No reschedule reasons" description="No reschedule data in this window." className="py-6" />
                    </div>
                  )
                )}
                {rbp?.topBlockerReasons && rbp.topBlockerReasons.length > 0 ? (
                  <div>
                    <p className="text-sm font-medium text-muted-foreground mb-2">Top blocker reasons</p>
                    <ul className="space-y-1 text-sm">
                      {rbp.topBlockerReasons.map((r, i) => (
                        <li key={i} className="flex justify-between">
                          <span className="truncate mr-2">{r.reason ?? '—'}</span>
                          <Badge variant="secondary" className="shrink-0">{r.count}</Badge>
                        </li>
                      ))}
                    </ul>
                  </div>
                ) : (
                  rbp && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground mb-2">Top blocker reasons</p>
                      <EmptyState title="No blocker reasons" description="No blocker data in this window." className="py-6" />
                    </div>
                  )
                )}
              </div>
              {rbp?.ordersWithHighChurn && rbp.ordersWithHighChurn.length > 0 ? (
                <div className="mt-4">
                  <p className="text-sm font-medium text-muted-foreground mb-2">
                    High-churn orders (≥{rbp.churnThresholdTransitions} reschedule/blocker transitions — may need follow-up)
                  </p>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2 px-2">Order</th>
                          <th className="text-right py-2 px-2">Transitions</th>
                          <th className="text-right py-2 px-2">Reschedules</th>
                          <th className="text-right py-2 px-2">Blockers</th>
                        </tr>
                      </thead>
                      <tbody>
                        {rbp.ordersWithHighChurn.map((row) => {
                          const isHighRisk = row.transitionCount >= 5;
                          return (
                            <tr
                              key={row.orderId}
                              className={`border-b ${isHighRisk ? 'bg-amber-50/70 dark:bg-amber-950/30' : ''}`}
                            >
                              <td className="py-2 px-2">
                                <OrderLink orderId={row.orderId}>View order</OrderLink>
                              </td>
                              <td className="py-2 px-2 text-right">
                                <Badge variant={isHighRisk ? 'warning' : 'secondary'}>{row.transitionCount}</Badge>
                              </td>
                              <td className="py-2 px-2 text-right">{row.rescheduleCount}</td>
                              <td className="py-2 px-2 text-right">{row.blockerCount}</td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </div>
              ) : (
                rbp && (
                  <div className="mt-4">
                    <EmptyState title="No high-churn orders" description={`No orders with ≥${rbp.churnThresholdTransitions} transitions in this window.`} className="py-8" />
                  </div>
                )
              )}
            </Card>

            {/* Material replacement patterns */}
            <Card className="p-4">
              <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                <Package className="h-4 w-4" />
                Material replacement patterns
              </h3>
              <p className="text-sm text-muted-foreground mb-2">
                Orders with multiple replacements: <Badge variant="secondary">{mrp?.ordersWithMultipleReplacements ?? 0}</Badge>
              </p>
              <div className="grid gap-4 md:grid-cols-2">
                {mrp?.topReplacementReasons && mrp.topReplacementReasons.length > 0 ? (
                  <div>
                    <p className="text-sm font-medium text-muted-foreground mb-2">Top replacement reasons</p>
                    <ul className="space-y-1 text-sm">
                      {mrp.topReplacementReasons.map((r, i) => (
                        <li key={i} className="flex justify-between">
                          <span className="truncate mr-2">{r.reason ?? '—'}</span>
                          <Badge variant="secondary" className="shrink-0">{r.count}</Badge>
                        </li>
                      ))}
                    </ul>
                  </div>
                ) : (
                  mrp && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground mb-2">Top replacement reasons</p>
                      <EmptyState title="No replacement reasons" description="No replacement data in this window." className="py-6" />
                    </div>
                  )
                )}
                {mrp?.byInstaller && mrp.byInstaller.length > 0 ? (
                  <div>
                    <p className="text-sm font-medium text-muted-foreground mb-2">By installer (count)</p>
                    <ul className="space-y-1 text-sm">
                      {mrp.byInstaller.map((row, i) => {
                        const topCount = mrp.byInstaller?.[0]?.count ?? 0;
                        const isHigh = topCount > 0 && row.count >= topCount * 0.8;
                        return (
                          <li key={i} className={`flex justify-between ${isHigh ? 'font-medium' : ''}`}>
                            <span className="truncate mr-2">{row.siDisplayName ?? row.siId ?? '—'}</span>
                            <Badge variant={isHigh ? 'warning' : 'secondary'} className="shrink-0">{row.count}</Badge>
                          </li>
                        );
                      })}
                    </ul>
                  </div>
                ) : (
                  mrp && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground mb-2">By installer (count)</p>
                      <EmptyState title="No installer replacement data" description="No replacement-by-installer data in this window." className="py-6" />
                    </div>
                  )
                )}
              </div>
            </Card>

            {/* Assurance / rework */}
            <Card className="p-4">
              <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                <Wrench className="h-4 w-4" />
                Assurance / rework
              </h3>
              <div className="flex flex-wrap gap-6 mb-3">
                <div>
                  <p className="text-xs text-muted-foreground">Assurance orders completed (window)</p>
                  <p className="text-lg font-medium">{ar?.assuranceOrdersCompletedInWindow ?? 0}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">With replacement</p>
                  <p className="text-lg font-medium">{ar?.assuranceOrdersWithReplacement ?? 0}</p>
                </div>
              </div>
              {ar?.topAssuranceIssues && ar.topAssuranceIssues.length > 0 ? (
                <div>
                  <p className="text-sm font-medium text-muted-foreground mb-2">Top assurance issues</p>
                  <ul className="space-y-1 text-sm">
                    {ar.topAssuranceIssues.map((r, i) => (
                      <li key={i} className="flex justify-between">
                        <span className="truncate mr-2">{r.reason ?? '—'}</span>
                        <Badge variant="secondary" className="shrink-0">{r.count}</Badge>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : (
                ar && (
                  <div>
                    <p className="text-sm font-medium text-muted-foreground mb-2">Top assurance issues</p>
                    <EmptyState title="No assurance issues" description="No top-issues data in this window." className="py-6" />
                  </div>
                )
              )}
            </Card>

            {/* Operational hotspots */}
            <Card className="p-4">
              <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                <MapPin className="h-4 w-4" />
                Operational hotspots (buildings)
              </h3>
              {hotspots?.coverageNote && (
                <p className="text-xs text-muted-foreground mb-2">{hotspots.coverageNote}</p>
              )}
              {hotspots?.buildingsWithMostDisruptions && hotspots.buildingsWithMostDisruptions.length > 0 ? (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-2 px-2">Building</th>
                        <th className="text-right py-2 px-2">Reschedules</th>
                        <th className="text-right py-2 px-2">Blockers</th>
                      </tr>
                    </thead>
                    <tbody>
                      {hotspots.buildingsWithMostDisruptions.map((row) => {
                        const total = row.rescheduleCount + row.blockerCount;
                        const isHigh = total >= 5;
                        return (
                          <tr
                            key={row.buildingId}
                            className={`border-b ${isHigh ? 'bg-amber-50/70 dark:bg-amber-950/30' : ''}`}
                          >
                            <td className="py-2 px-2">
                              <Link
                                to={`/buildings/${row.buildingId}`}
                                className="text-primary hover:underline"
                              >
                                {row.buildingName ?? `${row.buildingId.slice(0, 8)}…`}
                              </Link>
                            </td>
                            <td className="py-2 px-2 text-right">
                              <Badge variant={row.rescheduleCount >= 3 ? 'warning' : 'secondary'}>{row.rescheduleCount}</Badge>
                            </td>
                            <td className="py-2 px-2 text-right">
                              <Badge variant={row.blockerCount >= 3 ? 'warning' : 'secondary'}>{row.blockerCount}</Badge>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              ) : (
                <EmptyState
                  title="No building hotspots"
                  description="No building disruption data in this window."
                  className="py-8"
                />
              )}
            </Card>

            {/* Building Reliability Score */}
            {buildingReliability && (
              <Card className="p-4">
                <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                  <Shield className="h-4 w-4" />
                  Building Reliability Score
                </h3>
                <p className="text-xs text-muted-foreground mb-3">
                  {buildingReliability.interpretationNote ?? 'Score for prioritization only; not for automated enforcement.'}
                </p>
                {buildingReliability.buildings && buildingReliability.buildings.length > 0 ? (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2 px-2">Building</th>
                          <th className="text-left py-2 px-2">Band</th>
                          <th className="text-right py-2 px-2">Resched</th>
                          <th className="text-right py-2 px-2">Block</th>
                          <th className="text-right py-2 px-2">Churn</th>
                          <th className="text-right py-2 px-2">Stuck</th>
                          <th className="text-right py-2 px-2">Assur+Repl</th>
                          <th className="text-right py-2 px-2">Orders+Repl</th>
                          <th className="text-left py-2 px-2 max-w-[240px]">Summary</th>
                        </tr>
                      </thead>
                      <tbody>
                        {buildingReliability.buildings.map((row) => (
                          <tr
                            key={row.buildingId}
                            className={`border-b ${
                              row.band === 'HighRisk'
                                ? 'bg-amber-50/70 dark:bg-amber-950/30'
                                : row.band === 'ModerateRisk'
                                  ? 'bg-amber-50/30 dark:bg-amber-950/15'
                                  : ''
                            }`}
                          >
                            <td className="py-2 px-2">
                              <Link to={`/buildings/${row.buildingId}`} className="text-primary hover:underline">
                                {row.buildingName ?? `${row.buildingId.slice(0, 8)}…`}
                              </Link>
                            </td>
                            <td className="py-2 px-2">
                              <Badge
                                variant={row.band === 'HighRisk' ? 'warning' : row.band === 'ModerateRisk' ? 'warning' : 'secondary'}
                              >
                                {row.band === 'HighRisk' ? 'High risk' : row.band === 'ModerateRisk' ? 'Moderate risk' : 'Low risk'}
                              </Badge>
                            </td>
                            <td className="py-2 px-2 text-right">{row.rescheduleCount}</td>
                            <td className="py-2 px-2 text-right">{row.blockerCount}</td>
                            <td className="py-2 px-2 text-right">{row.highChurnOrderCount}</td>
                            <td className="py-2 px-2 text-right">{row.stuckOrderCount}</td>
                            <td className="py-2 px-2 text-right">{row.assuranceWithReplacementCount}</td>
                            <td className="py-2 px-2 text-right">{row.ordersWithReplacementsCount}</td>
                            <td className="py-2 px-2 text-muted-foreground max-w-[240px] truncate" title={row.reasonSummary ?? undefined}>
                              {row.reasonSummary ?? '—'}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <EmptyState
                    title="No building reliability data"
                    description="No buildings with disruption in this window, or data not yet computed."
                    className="py-8"
                  />
                )}
              </Card>
            )}

            {/* Order Failure Patterns */}
            {orderFailurePatterns && (
              <Card className="p-4">
                <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                  <Layers className="h-4 w-4" />
                  Order failure patterns
                </h3>
                <p className="text-xs text-muted-foreground mb-3">
                  {orderFailurePatterns.interpretationNote ?? 'Recurring patterns from reschedule/blocker, churn, stuck, assurance and replacement data. For operational review only.'}
                </p>
                {orderFailurePatterns.patterns && orderFailurePatterns.patterns.length > 0 ? (
                  <div className="space-y-3">
                    {orderFailurePatterns.patterns.map((p, i) => (
                      <div
                        key={p.patternId + i}
                        className="border rounded-md p-3 text-sm bg-muted/30"
                      >
                        <div className="flex flex-wrap items-center gap-2 mb-1">
                          <span className="font-medium">{p.patternName}</span>
                          <Badge variant={p.strength === 'StrongSignal' ? 'warning' : 'secondary'}>
                            {p.strength === 'StrongSignal' ? 'Strong signal' : p.strength === 'ReviewNeeded' ? 'Review needed' : 'Partial coverage'}
                          </Badge>
                          <span className="text-muted-foreground">Count: {p.count}</span>
                        </div>
                        {p.explanation && (
                          <p className="text-muted-foreground mb-2">{p.explanation}</p>
                        )}
                        {(p.sampleOrderIds?.length > 0 || p.sampleBuildingIds?.length > 0 || p.sampleInstallerIds?.length > 0) && (
                          <div className="flex flex-wrap gap-2 text-xs">
                            {p.sampleOrderIds?.length > 0 && (
                              <span className="flex items-center gap-1 flex-wrap">
                                Sample orders:
                                {p.sampleOrderIds.slice(0, 5).map((id) => (
                                  <OrderLink key={id} orderId={id}>{id.slice(0, 8)}…</OrderLink>
                                ))}
                              </span>
                            )}
                            {p.sampleBuildingIds?.length > 0 && (
                              <span className="flex items-center gap-1 flex-wrap">
                                Sample buildings:
                                {p.sampleBuildingIds.slice(0, 5).map((id) => (
                                  <Link key={id} to={`/buildings/${id}`} className="text-primary hover:underline">
                                    {id.slice(0, 8)}…
                                  </Link>
                                ))}
                              </span>
                            )}
                            {p.sampleInstallerIds?.length > 0 && (
                              <span className="flex items-center gap-1 flex-wrap">
                                Sample installers:
                                {p.sampleInstallerIds.slice(0, 5).map((id, idx) => (
                                  <span key={id} className="text-muted-foreground">
                                    {p.sampleInstallerDisplayNames?.[idx] ?? `${id.slice(0, 8)}…`}
                                    {idx < Math.min(5, p.sampleInstallerIds.length) - 1 ? ', ' : ''}
                                  </span>
                                ))}
                              </span>
                            )}
                          </div>
                        )}
                        {p.limitations && (
                          <p className="text-xs text-amber-700 dark:text-amber-400 mt-1">{p.limitations}</p>
                        )}
                      </div>
                    ))}
                  </div>
                ) : (
                  <EmptyState
                    title="No patterns detected"
                    description="No recurring failure patterns in this window, or data not yet computed."
                    className="py-8"
                  />
                )}
              </Card>
            )}

            {/* Pattern clusters */}
            {patternClusters && (
              <Card className="p-4">
                <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                  <LayoutGrid className="h-4 w-4" />
                  Pattern clusters
                </h3>
                <p className="text-xs text-muted-foreground mb-3">
                  {patternClusters.interpretationNote ?? 'Buildings where multiple operational signals align. For review only; does not prove root cause.'}
                </p>
                {patternClusters.clusters && patternClusters.clusters.length > 0 ? (
                  <div className="space-y-3">
                    {patternClusters.clusters.map((c) => (
                      <div
                        key={c.buildingId}
                        className="border rounded-md p-3 text-sm bg-muted/30"
                      >
                        <div className="flex flex-wrap items-center gap-2 mb-2">
                          <Link to={`/buildings/${c.buildingId}`} className="font-medium text-primary hover:underline">
                            {c.buildingName ?? `${c.buildingId.slice(0, 8)}…`}
                          </Link>
                          <Badge variant={c.classification === 'PossibleInfrastructureIssue' ? 'warning' : 'secondary'}>
                            {c.classification === 'PossibleInfrastructureIssue' ? 'Possible infrastructure issue' : 'Operational cluster'}
                          </Badge>
                        </div>
                        <ul className="list-disc list-inside text-muted-foreground mb-2">
                          {c.signalsPresent.map((s, i) => (
                            <li key={i}>{s}</li>
                          ))}
                        </ul>
                        {c.interpretation && (
                          <p className="text-muted-foreground mb-2">{c.interpretation}</p>
                        )}
                        {c.sampleOrderIds?.length > 0 && (
                          <div className="flex items-center gap-1 flex-wrap text-xs mb-1">
                            <span>Sample orders:</span>
                            {c.sampleOrderIds.slice(0, 5).map((id) => (
                              <OrderLink key={id} orderId={id}>{id.slice(0, 8)}…</OrderLink>
                            ))}
                          </div>
                        )}
                        {c.limitations && (
                          <p className="text-xs text-amber-700 dark:text-amber-400 mt-1">{c.limitations}</p>
                        )}
                      </div>
                    ))}
                  </div>
                ) : (
                  <EmptyState
                    title="No clusters detected"
                    description="No buildings with multiple aligned signals in this window."
                    className="py-8"
                  />
                )}
              </Card>
            )}

            {/* Data gaps (also at bottom for visibility) */}
            {data.dataGaps && data.dataGaps.length > 0 && (
              <Card className="p-4 border-muted">
                <h3 className="text-sm font-semibold mb-2 flex items-center gap-2">
                  <FileWarning className="h-4 w-4" />
                  Data gaps / quality notes
                </h3>
                <ul className="text-sm text-muted-foreground list-disc list-inside space-y-1">
                  {data.dataGaps.map((gap, i) => (
                    <li key={i}>{gap}</li>
                  ))}
                </ul>
              </Card>
            )}
          </>
        )}
      </div>
    </PageShell>
  );
};

export default SiInsightsPage;
