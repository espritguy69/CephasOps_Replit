import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  AlertTriangle,
  Building2,
  ChevronDown,
  ChevronRight,
  RefreshCw,
  UserCheck,
  ClipboardList
} from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Button, Card, LoadingSpinner, useToast, Badge } from '../../components/ui';
import { MetricCard } from '../../components/insights';
import {
  getOperationalIntelligenceSummary,
  getOrdersAtRisk,
  getInstallersAtRisk,
  getBuildingsAtRisk,
  type OperationalIntelligenceSummaryDto,
  type OrderRiskSignalDto,
  type InstallerRiskSignalDto,
  type BuildingRiskSignalDto,
  type IntelligenceExplanationDto
} from '../../api/operationalIntelligence';

const SEVERITY_OPTIONS = [
  { value: '', label: 'All severities' },
  { value: 'Critical', label: 'Critical' },
  { value: 'Warning', label: 'Warning' },
  { value: 'Info', label: 'Info' }
];

function ReasonList({ reasons }: { reasons: IntelligenceExplanationDto[] }) {
  return (
    <ul className="list-disc list-inside text-sm text-muted-foreground space-y-1 mt-2">
      {reasons.map((r, i) => (
        <li key={i}>
          <span className="font-medium text-foreground">{r.ruleCode}:</span> {r.summary}
          {r.detail && <span className="block ml-4 mt-0.5">{r.detail}</span>}
          {r.sourceCount != null && (
            <span className="text-muted-foreground ml-1"> (count: {r.sourceCount})</span>
          )}
        </li>
      ))}
    </ul>
  );
}

const OperationalIntelligenceDashboard: React.FC = () => {
  const { showError } = useToast();
  const [summary, setSummary] = useState<OperationalIntelligenceSummaryDto | null>(null);
  const [orders, setOrders] = useState<OrderRiskSignalDto[]>([]);
  const [installers, setInstallers] = useState<InstallerRiskSignalDto[]>([]);
  const [buildings, setBuildings] = useState<BuildingRiskSignalDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [severityFilter, setSeverityFilter] = useState<string>('');
  const [expandedOrder, setExpandedOrder] = useState<string | null>(null);
  const [expandedInstaller, setExpandedInstaller] = useState<string | null>(null);
  const [expandedBuilding, setExpandedBuilding] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [sum, ords, inst, blds] = await Promise.all([
        getOperationalIntelligenceSummary(),
        getOrdersAtRisk(severityFilter || null),
        getInstallersAtRisk(severityFilter || null),
        getBuildingsAtRisk(severityFilter || null)
      ]);
      setSummary(sum);
      setOrders(ords);
      setInstallers(inst);
      setBuildings(blds);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load operational intelligence';
      showError(message);
      setSummary(null);
      setOrders([]);
      setInstallers([]);
      setBuildings([]);
    } finally {
      setLoading(false);
    }
  }, [showError, severityFilter]);

  useEffect(() => {
    load();
  }, [load]);

  const severityBadgeVariant = (s: string) =>
    s === 'Critical' ? 'destructive' : s === 'Warning' ? 'default' : 'secondary';

  return (
    <PageShell
      title="Operational Intelligence"
      description="At-risk orders, installers, and buildings with explainable rules"
    >
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <label htmlFor="severity" className="text-sm text-muted-foreground">
              Severity:
            </label>
            <select
              id="severity"
              className="border rounded px-2 py-1 text-sm bg-background"
              value={severityFilter}
              onChange={(e) => setSeverityFilter(e.target.value)}
            >
              {SEVERITY_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
          <Button variant="outline" size="sm" onClick={load} disabled={loading}>
            <RefreshCw className={loading ? 'animate-spin h-4 w-4 mr-2' : 'h-4 w-4 mr-2'} />
            Refresh
          </Button>
        </div>

        {loading && !summary ? (
          <LoadingSpinner />
        ) : summary ? (
          <>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-6 gap-4">
              <MetricCard
                title="Orders at risk"
                value={summary.ordersAtRiskCount}
                icon={ClipboardList}
                iconBg={summary.ordersAtRiskCount > 0 ? 'bg-amber-500/10' : 'bg-muted'}
              />
              <MetricCard
                title="Installers at risk"
                value={summary.installersAtRiskCount}
                icon={UserCheck}
                iconBg={summary.installersAtRiskCount > 0 ? 'bg-amber-500/10' : 'bg-muted'}
              />
              <MetricCard
                title="Buildings at risk"
                value={summary.buildingsAtRiskCount}
                icon={Building2}
                iconBg={summary.buildingsAtRiskCount > 0 ? 'bg-amber-500/10' : 'bg-muted'}
              />
              <MetricCard title="Critical" value={summary.criticalCount} icon={AlertTriangle} iconBg="bg-red-500/10" />
              <MetricCard title="Warning" value={summary.warningCount} icon={AlertTriangle} iconBg="bg-amber-500/10" />
              <MetricCard title="Info" value={summary.infoCount} icon={AlertTriangle} iconBg="bg-muted" />
            </div>

            <Card className="p-4">
              <h3 className="text-sm font-medium text-muted-foreground mb-3">At-risk orders</h3>
              {orders.length === 0 ? (
                <p className="text-sm text-muted-foreground">No orders match the current filters.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-2 w-8" />
                        <th className="text-left py-2">Order</th>
                        <th className="text-left py-2">Status</th>
                        <th className="text-left py-2">Severity</th>
                        <th className="text-left py-2">Reasons</th>
                      </tr>
                    </thead>
                    <tbody>
                      {orders.map((row) => (
                        <React.Fragment key={row.orderId}>
                          <tr
                            className="border-b border-border/50 cursor-pointer hover:bg-muted/50"
                            onClick={() => setExpandedOrder(expandedOrder === row.orderId ? null : row.orderId)}
                          >
                            <td className="py-2">
                              {expandedOrder === row.orderId ? (
                                <ChevronDown className="h-4 w-4" />
                              ) : (
                                <ChevronRight className="h-4 w-4" />
                              )}
                            </td>
                            <td className="py-2">
                              <Link
                                to={`/orders/${row.orderId}`}
                                className="text-primary hover:underline"
                                onClick={(e) => e.stopPropagation()}
                              >
                                {row.orderRef || row.orderId.slice(0, 8)}…
                              </Link>
                            </td>
                            <td className="py-2">{row.status ?? '—'}</td>
                            <td className="py-2">
                              <Badge variant={severityBadgeVariant(row.severity)}>{row.severity}</Badge>
                            </td>
                            <td className="py-2">{row.reasons.length} reason(s)</td>
                          </tr>
                          {expandedOrder === row.orderId && (
                            <tr className="border-b border-border/50 bg-muted/30">
                              <td colSpan={5} className="py-2 px-4">
                                <ReasonList reasons={row.reasons} />
                              </td>
                            </tr>
                          )}
                        </React.Fragment>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>

            <Card className="p-4">
              <h3 className="text-sm font-medium text-muted-foreground mb-3">At-risk installers</h3>
              {installers.length === 0 ? (
                <p className="text-sm text-muted-foreground">No installers match the current filters.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-2 w-8" />
                        <th className="text-left py-2">Installer</th>
                        <th className="text-left py-2">Severity</th>
                        <th className="text-left py-2">Reasons</th>
                      </tr>
                    </thead>
                    <tbody>
                      {installers.map((row) => (
                        <React.Fragment key={row.installerId}>
                          <tr
                            className="border-b border-border/50 cursor-pointer hover:bg-muted/50"
                            onClick={() =>
                              setExpandedInstaller(expandedInstaller === row.installerId ? null : row.installerId)
                            }
                          >
                            <td className="py-2">
                              {expandedInstaller === row.installerId ? (
                                <ChevronDown className="h-4 w-4" />
                              ) : (
                                <ChevronRight className="h-4 w-4" />
                              )}
                            </td>
                            <td className="py-2">{row.installerDisplayName || row.installerId.slice(0, 8)}…</td>
                            <td className="py-2">
                              <Badge variant={severityBadgeVariant(row.severity)}>{row.severity}</Badge>
                            </td>
                            <td className="py-2">{row.reasons.length} reason(s)</td>
                          </tr>
                          {expandedInstaller === row.installerId && (
                            <tr className="border-b border-border/50 bg-muted/30">
                              <td colSpan={4} className="py-2 px-4">
                                <ReasonList reasons={row.reasons} />
                              </td>
                            </tr>
                          )}
                        </React.Fragment>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>

            <Card className="p-4">
              <h3 className="text-sm font-medium text-muted-foreground mb-3">At-risk buildings / sites</h3>
              {buildings.length === 0 ? (
                <p className="text-sm text-muted-foreground">No buildings match the current filters.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-2 w-8" />
                        <th className="text-left py-2">Building</th>
                        <th className="text-left py-2">Severity</th>
                        <th className="text-left py-2">Reasons</th>
                      </tr>
                    </thead>
                    <tbody>
                      {buildings.map((row) => (
                        <React.Fragment key={row.buildingId}>
                          <tr
                            className="border-b border-border/50 cursor-pointer hover:bg-muted/50"
                            onClick={() =>
                              setExpandedBuilding(expandedBuilding === row.buildingId ? null : row.buildingId)
                            }
                          >
                            <td className="py-2">
                              {expandedBuilding === row.buildingId ? (
                                <ChevronDown className="h-4 w-4" />
                              ) : (
                                <ChevronRight className="h-4 w-4" />
                              )}
                            </td>
                            <td className="py-2">{row.buildingDisplayName || row.buildingId.slice(0, 8)}…</td>
                            <td className="py-2">
                              <Badge variant={severityBadgeVariant(row.severity)}>{row.severity}</Badge>
                            </td>
                            <td className="py-2">{row.reasons.length} reason(s)</td>
                          </tr>
                          {expandedBuilding === row.buildingId && (
                            <tr className="border-b border-border/50 bg-muted/30">
                              <td colSpan={4} className="py-2 px-4">
                                <ReasonList reasons={row.reasons} />
                              </td>
                            </tr>
                          )}
                        </React.Fragment>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          </>
        ) : (
          <Card className="p-6">
            <p className="text-muted-foreground">No data available. Ensure company context is set.</p>
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default OperationalIntelligenceDashboard;
