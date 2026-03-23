import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  AlertTriangle,
  DollarSign,
  FileWarning,
  MinusCircle,
  RefreshCw,
  TrendingUp,
  User,
  BarChart3,
  ExternalLink,
  MessageSquare,
  CheckCircle2,
  XCircle,
  Mail,
  Bell
} from 'lucide-react';
import { Button, Card, Skeleton } from '../../components/ui';
import { PageShell } from '../../components/layout';
import {
  getPayoutAnomalySummary,
  getPayoutAnomalies,
  getPayoutAnomalyClusters,
  getPayoutAnomalyReviewSummary,
  getPayoutAnomalyReview,
  postAcknowledgeAnomaly,
  postAssignAnomaly,
  postResolveAnomaly,
  postFalsePositiveAnomaly,
  postAnomalyComment,
  postRunAnomalyAlerts,
  getLatestAlertRun,
  getAlertResponseSummary,
  getStaleAlertedAnomalies
} from '../../api/payoutHealth';
import { getUsers } from '../../api/rbac';
import { useAuth } from '../../contexts/AuthContext';
import type {
  PayoutAnomalyDetectionSummaryDto,
  PayoutAnomalyDto,
  PayoutAnomalyClusterDto,
  PayoutAnomalyFilterParams,
  AlertResponseSummaryDto
} from '../../types/payoutHealth';
import { cn } from '@/lib/utils';

const ANOMALY_TYPES = [
  'HighPayoutVsPeer',
  'ExcessiveCustomOverride',
  'ExcessiveLegacyFallback',
  'RepeatedWarnings',
  'ZeroPayout',
  'NegativeMarginCluster',
  'InstallerDeviation'
] as const;

const SEVERITIES = ['Low', 'Medium', 'High'] as const;

/** Filter snapshot for presets (no page; page resets to 1 on apply). */
export interface PayoutAnomalyFilterPresetSnapshot {
  from: string;
  to: string;
  anomalyType: string;
  severity: string;
  staleThresholdHours: number;
}

export interface PayoutAnomalyFilterPreset extends PayoutAnomalyFilterPresetSnapshot {
  id: string;
  name: string;
  builtIn?: boolean;
}

const PRESETS_STORAGE_KEY = 'cephasops-payout-anomaly-filter-presets';

function todayYMD(): string {
  return new Date().toISOString().slice(0, 10);
}

function daysAgoYMD(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}

function getBuiltInPresets(): PayoutAnomalyFilterPreset[] {
  const today = todayYMD();
  const sevenDaysAgo = daysAgoYMD(7);
  const thirtyDaysAgo = daysAgoYMD(30);
  return [
    { id: 'ops-today', name: 'Ops Today', builtIn: true, from: today, to: today, anomalyType: '', severity: '', staleThresholdHours: 24 },
    { id: 'high-severity', name: 'High Severity', builtIn: true, from: '', to: '', anomalyType: '', severity: 'High', staleThresholdHours: 24 },
    { id: 'stale-24h', name: 'Stale > 24h', builtIn: true, from: sevenDaysAgo, to: today, anomalyType: '', severity: '', staleThresholdHours: 24 },
    { id: 'stale-48h', name: 'Stale > 48h', builtIn: true, from: sevenDaysAgo, to: today, anomalyType: '', severity: '', staleThresholdHours: 48 },
    { id: 'finance-review', name: 'Finance Review', builtIn: true, from: thirtyDaysAgo, to: today, anomalyType: '', severity: '', staleThresholdHours: 24 }
  ];
}

function loadCustomPresets(): PayoutAnomalyFilterPreset[] {
  try {
    const raw = localStorage.getItem(PRESETS_STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as PayoutAnomalyFilterPreset[];
    return Array.isArray(parsed)
      ? parsed.filter((p) => p && p.id && p.name && typeof p.staleThresholdHours === 'number')
      : [];
  } catch {
    return [];
  }
}

function saveCustomPresets(presets: PayoutAnomalyFilterPreset[]): void {
  try {
    localStorage.setItem(PRESETS_STORAGE_KEY, JSON.stringify(presets));
  } catch {
    // ignore
  }
}

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString(undefined, {
      dateStyle: 'short',
      timeStyle: 'short'
    });
  } catch {
    return iso;
  }
}

function formatCurrency(amount: number, currency: string = 'MYR'): string {
  return new Intl.NumberFormat('en-MY', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount);
}

function formatAnomalyType(s: string): string {
  return s
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (c) => c.toUpperCase())
    .trim();
}

const StatCard: React.FC<{
  title: string;
  value: string | number;
  icon: React.ElementType;
  iconBg?: string;
  loading?: boolean;
}> = ({ title, value, icon: Icon, iconBg = 'bg-primary/10', loading }) => (
  <div className="rounded-lg border border-border bg-card p-4 shadow-sm">
    <div className="flex items-start justify-between gap-3">
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-muted-foreground">{title}</p>
        {loading ? (
          <div className="mt-1 h-8 w-20 animate-pulse rounded bg-muted" />
        ) : (
          <p className="mt-1 text-2xl font-bold tracking-tight text-foreground">{value}</p>
        )}
      </div>
      <div className={cn('flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl', iconBg)}>
        <Icon className="h-5 w-5 text-primary" />
      </div>
    </div>
  </div>
);

const PERMISSION_ANOMALIES_REVIEW = 'payout.anomalies.review';

export const PayoutAnomaliesPage: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canReviewAnomalies =
    !!user?.permissions?.includes(PERMISSION_ANOMALIES_REVIEW) ||
    user?.roles?.includes('Admin') ||
    user?.roles?.includes('SuperAdmin');
  const [from, setFrom] = useState<string>('');
  const [to, setTo] = useState<string>('');
  const [anomalyType, setAnomalyType] = useState<string>('');
  const [severity, setSeverity] = useState<string>('');
  const [staleThresholdHours, setStaleThresholdHours] = useState<number>(24);
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [customPresets, setCustomPresets] = useState<PayoutAnomalyFilterPreset[]>(() => loadCustomPresets());
  const [selectedPresetId, setSelectedPresetId] = useState<string | null>(null);
  const [commentDrawerAnomalyId, setCommentDrawerAnomalyId] = useState<string | null>(null);
  const [assignModalAnomaly, setAssignModalAnomaly] = useState<PayoutAnomalyDto | null>(null);
  const [assignUserId, setAssignUserId] = useState<string>('');
  const [newCommentText, setNewCommentText] = useState('');

  const filterParams: PayoutAnomalyFilterParams = useMemo(() => {
    const p: PayoutAnomalyFilterParams = { page, pageSize };
    if (from) p.from = from;
    if (to) p.to = to;
    if (anomalyType) p.anomalyType = anomalyType;
    if (severity) p.severity = severity;
    return p;
  }, [from, to, anomalyType, severity, page, pageSize]);

  const summaryQuery = useQuery({
    queryKey: ['payout-health', 'anomaly-summary', filterParams],
    queryFn: () => getPayoutAnomalySummary(filterParams)
  });

  const listQuery = useQuery({
    queryKey: ['payout-health', 'anomalies', filterParams],
    queryFn: () => getPayoutAnomalies(filterParams)
  });

  const clustersQuery = useQuery({
    queryKey: ['payout-health', 'anomaly-clusters', { from: filterParams.from, to: filterParams.to }],
    queryFn: () =>
      getPayoutAnomalyClusters({
        from: filterParams.from ?? undefined,
        to: filterParams.to ?? undefined,
        top: 10
      })
  });

  const reviewSummaryQuery = useQuery({
    queryKey: ['payout-health', 'anomaly-review-summary'],
    queryFn: getPayoutAnomalyReviewSummary
  });

  const latestAlertRunQuery = useQuery({
    queryKey: ['payout-health', 'alert-runs', 'latest'],
    queryFn: getLatestAlertRun
  });

  const alertResponseSummaryQuery = useQuery({
    queryKey: ['payout-health', 'alert-response-summary', { from: filterParams.from, to: filterParams.to, staleThresholdHours }],
    queryFn: () =>
      getAlertResponseSummary({
        from: filterParams.from ?? undefined,
        to: filterParams.to ?? undefined,
        staleThresholdHours
      })
  });

  const staleAnomaliesQuery = useQuery({
    queryKey: ['payout-health', 'stale-alerted-anomalies', { from: filterParams.from, to: filterParams.to, staleThresholdHours }],
    queryFn: () =>
      getStaleAlertedAnomalies({
        from: filterParams.from ?? undefined,
        to: filterParams.to ?? undefined,
        limit: 20,
        staleThresholdHours
      })
  });

  const commentDrawerReviewQuery = useQuery({
    queryKey: ['payout-health', 'anomaly-review', commentDrawerAnomalyId],
    queryFn: () => getPayoutAnomalyReview(commentDrawerAnomalyId!),
    enabled: !!commentDrawerAnomalyId
  });

  const usersQuery = useQuery({
    queryKey: ['admin', 'users'],
    queryFn: () => getUsers(),
    enabled: !!assignModalAnomaly
  });

  const invalidateAll = () => {
    queryClient.invalidateQueries({ queryKey: ['payout-health'] });
  };
  const latestRun = latestAlertRunQuery.data;

  const allPresets = useMemo(() => [...getBuiltInPresets(), ...customPresets], [customPresets]);
  const applyPreset = useCallback(
    (preset: PayoutAnomalyFilterPreset) => {
      setFrom(preset.from);
      setTo(preset.to);
      setAnomalyType(preset.anomalyType);
      setSeverity(preset.severity);
      setStaleThresholdHours(preset.staleThresholdHours);
      setPage(1);
      setSelectedPresetId(preset.id);
    },
    []
  );
  const saveCurrentAsPreset = useCallback(() => {
    const name = window.prompt('Preset name');
    if (!name?.trim()) return;
    const preset: PayoutAnomalyFilterPreset = {
      id: crypto.randomUUID(),
      name: name.trim(),
      builtIn: false,
      from,
      to,
      anomalyType,
      severity,
      staleThresholdHours
    };
    setCustomPresets((prev) => {
      const next = [...prev, preset];
      saveCustomPresets(next);
      return next;
    });
    setSelectedPresetId(preset.id);
  }, [from, to, anomalyType, severity, staleThresholdHours]);
  const deleteSelectedPreset = useCallback(() => {
    if (!selectedPresetId) return;
    const preset = customPresets.find((p) => p.id === selectedPresetId);
    if (!preset || preset.builtIn) return;
    setCustomPresets((prev) => {
      const next = prev.filter((p) => p.id !== selectedPresetId);
      saveCustomPresets(next);
      return next;
    });
    setSelectedPresetId(null);
  }, [selectedPresetId, customPresets]);
  const selectedPreset = selectedPresetId ? allPresets.find((p) => p.id === selectedPresetId) : null;
  const isCustomSelected = selectedPreset ? !selectedPreset.builtIn : false;

  const acknowledgeMutation = useMutation({
    mutationFn: ({ id, body }: { id: string; body?: PayoutAnomalyDto }) => postAcknowledgeAnomaly(id, body),
    onSuccess: invalidateAll
  });
  const assignMutation = useMutation({
    mutationFn: ({ id, userId }: { id: string; userId: string | null }) =>
      postAssignAnomaly(id, { assignedToUserId: userId || undefined }),
    onSuccess: () => {
      invalidateAll();
      setAssignModalAnomaly(null);
      setAssignUserId('');
    }
  });
  const resolveMutation = useMutation({
    mutationFn: ({ id, body }: { id: string; body?: PayoutAnomalyDto }) => postResolveAnomaly(id, body),
    onSuccess: invalidateAll
  });
  const falsePositiveMutation = useMutation({
    mutationFn: ({ id, body }: { id: string; body?: PayoutAnomalyDto }) => postFalsePositiveAnomaly(id, body),
    onSuccess: invalidateAll
  });
  const commentMutation = useMutation({
    mutationFn: ({ id, text }: { id: string; text: string }) => postAnomalyComment(id, { text }),
    onSuccess: () => {
      invalidateAll();
      commentDrawerAnomalyId && queryClient.invalidateQueries({ queryKey: ['payout-health', 'anomaly-review', commentDrawerAnomalyId] });
    }
  });
  const runAlertsMutation = useMutation({
    mutationFn: (req?: { recipientEmails?: string[]; includeMediumRepeated?: boolean }) => postRunAnomalyAlerts(req),
    onSuccess: invalidateAll
  });

  const summary = summaryQuery.data;
  const listResult = listQuery.data;
  const clusters = clustersQuery.data ?? [];
  const isLoading = summaryQuery.isLoading || listQuery.isLoading;
  const error = summaryQuery.error || listQuery.error;

  if (error) {
    return (
      <PageShell
        title="Payout Anomalies"
        breadcrumbs={[
          { label: 'Reports', path: '/reports' },
          { label: 'Payout Health', path: '/reports/payout-health' },
          { label: 'Payout Anomalies', path: '/reports/payout-health/anomalies' }
        ]}
      >
        <Card className="border-destructive/50 bg-destructive/5 p-6">
          <p className="text-destructive">
            Failed to load anomalies: {(error as Error).message}
          </p>
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Payout Anomalies"
      breadcrumbs={[
        { label: 'Reports', path: '/reports' },
        { label: 'Payout Health', path: '/reports/payout-health' },
        { label: 'Payout Anomalies', path: '/reports/payout-health/anomalies' }
      ]}
    >
      <p className="mb-4 text-sm text-muted-foreground">
        Read-only anomaly detection for payout patterns and possible rate misconfiguration. No
        payout logic or payroll is changed.
      </p>

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Link
          to="/reports/payout-health"
          className="inline-flex items-center gap-2 rounded-md border border-border bg-background px-3 py-2 text-sm font-medium text-foreground hover:bg-muted"
        >
          <BarChart3 className="h-4 w-4" />
          Payout Health Dashboard
        </Link>
        <Link
          to="/settings/gpon/rate-designer"
          className="inline-flex items-center gap-2 rounded-md border border-border bg-background px-3 py-2 text-sm font-medium text-foreground hover:bg-muted"
        >
          <ExternalLink className="h-4 w-4" />
          Rate Designer
        </Link>
        <button
          type="button"
          onClick={() => {
            summaryQuery.refetch();
            listQuery.refetch();
            clustersQuery.refetch();
          }}
          disabled={summaryQuery.isFetching || listQuery.isFetching}
          className="inline-flex items-center gap-2 rounded-md border border-border bg-background px-3 py-2 text-sm font-medium text-foreground hover:bg-muted disabled:opacity-50"
        >
          <RefreshCw className={cn('h-4 w-4', (summaryQuery.isFetching || listQuery.isFetching) && 'animate-spin')} />
          Refresh
        </button>
        {canReviewAnomalies && (
          <>
            <button
              type="button"
              onClick={() => runAlertsMutation.mutate()}
              disabled={runAlertsMutation.isPending}
              className="inline-flex items-center gap-2 rounded-md border border-border bg-background px-3 py-2 text-sm font-medium text-foreground hover:bg-muted disabled:opacity-50"
              title="Send email alerts for new high-severity anomalies (no duplicate within 24h)"
            >
              <Mail className={cn('h-4 w-4', runAlertsMutation.isPending && 'animate-pulse')} />
              Run anomaly alerts
            </button>
            {runAlertsMutation.isSuccess && runAlertsMutation.data && (
              <span className="text-sm text-muted-foreground">
                {runAlertsMutation.data.alertsSent > 0
                  ? `Alerts sent: ${runAlertsMutation.data.anomaliesAlerted} anomaly(ies) via ${runAlertsMutation.data.channelsUsed?.join(', ') ?? 'email'}.`
                  : runAlertsMutation.data.anomaliesConsidered === 0
                    ? 'No high-severity anomalies to alert.'
                    : 'No new anomalies (already alerted in window).'}
                {runAlertsMutation.data.errors?.length ? ` Errors: ${runAlertsMutation.data.errors.join('; ')}` : ''}
              </span>
            )}
          </>
        )}
      </div>

      {/* Summary cards */}
      <section className="mb-8">
        <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold">
          <AlertTriangle className="h-5 w-5" />
          Anomaly summary
        </h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-7">
          <StatCard
            title="High payout vs peer"
            value={summary?.highPayoutVsPeerCount ?? '—'}
            icon={DollarSign}
            iconBg="bg-amber-500/10"
            loading={summaryQuery.isLoading}
          />
          <StatCard
            title="Legacy fallback"
            value={summary?.excessiveLegacyFallbackCount ?? '—'}
            icon={FileWarning}
            iconBg="bg-amber-500/10"
            loading={summaryQuery.isLoading}
          />
          <StatCard
            title="Custom override"
            value={summary?.excessiveCustomOverrideCount ?? '—'}
            icon={User}
            iconBg="bg-amber-500/10"
            loading={summaryQuery.isLoading}
          />
          <StatCard
            title="Zero payout"
            value={summary?.zeroPayoutCount ?? '—'}
            icon={MinusCircle}
            iconBg="bg-amber-500/10"
            loading={summaryQuery.isLoading}
          />
          <StatCard
            title="Negative margin"
            value={summary?.negativeMarginClusterCount ?? '—'}
            icon={TrendingUp}
            iconBg="bg-red-500/10"
            loading={summaryQuery.isLoading}
          />
          <StatCard
            title="Repeated warnings"
            value={summary?.repeatedWarningsCount ?? '—'}
            icon={AlertTriangle}
            iconBg="bg-muted"
            loading={summaryQuery.isLoading}
          />
          <StatCard
            title="Installer deviation"
            value={summary?.installerDeviationCount ?? '—'}
            icon={BarChart3}
            iconBg="bg-muted"
            loading={summaryQuery.isLoading}
          />
        </div>
        <p className="mt-2 text-xs text-muted-foreground">
          Total: {summary?.totalCount ?? 0} (High: {summary?.highSeverityCount ?? 0}, Medium:{' '}
          {summary?.mediumSeverityCount ?? 0}, Low: {summary?.lowSeverityCount ?? 0})
        </p>
      </section>

      {/* Governance summary */}
      <section className="mb-8">
        <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold">
          <CheckCircle2 className="h-5 w-5" />
          Review governance
        </h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <StatCard
            title="Open anomalies"
            value={reviewSummaryQuery.data?.openCount ?? '—'}
            icon={AlertTriangle}
            iconBg="bg-amber-500/10"
            loading={reviewSummaryQuery.isLoading}
          />
          <StatCard
            title="Investigating"
            value={reviewSummaryQuery.data?.investigatingCount ?? '—'}
            icon={User}
            iconBg="bg-blue-500/10"
            loading={reviewSummaryQuery.isLoading}
          />
          <StatCard
            title="Resolved today"
            value={reviewSummaryQuery.data?.resolvedTodayCount ?? '—'}
            icon={CheckCircle2}
            iconBg="bg-emerald-500/10"
            loading={reviewSummaryQuery.isLoading}
          />
        </div>
      </section>

      {/* Alert response summary */}
      {alertResponseSummaryQuery.isSuccess && alertResponseSummary && (
        <section className="mb-8">
          <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold">
            <Bell className="h-5 w-5" />
            Alert response
          </h2>
          <p className="mb-3 text-sm text-muted-foreground">
            Counts of alerted anomalies by review status (based on current filters). Stale = alerted, still open, no action after threshold.
          </p>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-7">
            <StatCard
              title="Alerted · Open"
              value={alertResponseSummary.alertedOpen}
              icon={AlertTriangle}
              iconBg="bg-amber-500/10"
              loading={alertResponseSummaryQuery.isLoading}
            />
            <StatCard
              title="Alerted · Acknowledged"
              value={alertResponseSummary.alertedAcknowledged}
              icon={CheckCircle2}
              iconBg="bg-slate-500/10"
              loading={alertResponseSummaryQuery.isLoading}
            />
            <StatCard
              title="Alerted · Investigating"
              value={alertResponseSummary.alertedInvestigating}
              icon={User}
              iconBg="bg-blue-500/10"
              loading={alertResponseSummaryQuery.isLoading}
            />
            <StatCard
              title="Alerted · Resolved"
              value={alertResponseSummary.alertedResolved}
              icon={CheckCircle2}
              iconBg="bg-emerald-500/10"
              loading={alertResponseSummaryQuery.isLoading}
            />
            <StatCard
              title="Alerted · False +"
              value={alertResponseSummary.alertedFalsePositive}
              icon={XCircle}
              iconBg="bg-slate-500/10"
              loading={alertResponseSummaryQuery.isLoading}
            />
            <StatCard
              title="Stale (no action)"
              value={alertResponseSummary.staleCount}
              icon={AlertTriangle}
              iconBg="bg-red-500/10"
              loading={alertResponseSummaryQuery.isLoading}
            />
            <div className="rounded-lg border border-border bg-card p-4 shadow-sm">
              <p className="text-sm font-medium text-muted-foreground">Avg time to first action</p>
              {alertResponseSummaryQuery.isLoading ? (
                <div className="mt-1 h-8 w-20 animate-pulse rounded bg-muted" />
              ) : alertResponseSummary.averageTimeToFirstActionMinutes != null ? (
                <p className="mt-1 text-lg font-bold text-foreground">
                  {Math.round(alertResponseSummary.averageTimeToFirstActionMinutes)} min
                </p>
              ) : (
                <p className="mt-1 text-sm text-muted-foreground">—</p>
              )}
            </div>
          </div>
        </section>
      )}

      {/* Latest alert run */}
      {latestAlertRunQuery.isSuccess && latestRun && (
        <section className="mb-8">
          <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold">
            <Bell className="h-5 w-5" />
            Latest alert run
          </h2>
          <Card className="p-4">
            <div className="flex flex-wrap items-center gap-4 text-sm">
              <span className="text-muted-foreground">
                {formatDate(latestRun.startedAt)}
                {latestRun.completedAt && ` – ${formatDate(latestRun.completedAt)}`}
              </span>
              <span
                className={cn(
                  'rounded px-2 py-0.5 text-xs font-medium',
                  latestRun.triggerSource === 'Scheduler' ? 'bg-blue-500/20 text-blue-700 dark:text-blue-400' : 'bg-slate-500/20 text-slate-700 dark:text-slate-400'
                )}
              >
                {latestRun.triggerSource}
              </span>
              <span>Evaluated: {latestRun.evaluatedCount}</span>
              <span>Sent: {latestRun.sentCount}</span>
              <span>Skipped (duplicate window): {latestRun.skippedCount}</span>
              {latestRun.errorCount > 0 && (
                <span className="text-destructive">Errors: {latestRun.errorCount}</span>
              )}
            </div>
          </Card>
        </section>
      )}

      {/* Stale alerted anomalies */}
      {staleAnomalies.length > 0 && (
        <section className="mb-8">
          <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold text-amber-700 dark:text-amber-400">
            <AlertTriangle className="h-5 w-5" />
            Stale alerted anomalies ({staleAnomalies.length})
          </h2>
          <p className="mb-3 text-sm text-muted-foreground">
            Alerted with no review action after {staleThresholdHours}h (see Stale threshold in filters); consider acknowledging or assigning.
          </p>
          <div className="overflow-x-auto rounded-lg border border-amber-500/30 bg-amber-500/5">
            <table className="w-full min-w-[700px] text-left text-sm">
              <thead className="border-b border-border bg-muted/50">
                <tr>
                  <th className="px-4 py-2 font-medium">Severity</th>
                  <th className="px-4 py-2 font-medium">Type</th>
                  <th className="px-4 py-2 font-medium">Last alert</th>
                  <th className="px-4 py-2 font-medium">Reason</th>
                  <th className="px-4 py-2 font-medium">Order</th>
                </tr>
              </thead>
              <tbody>
                {staleAnomalies.map((a: PayoutAnomalyDto, i: number) => (
                  <tr key={a.id ?? i} className="border-b border-border last:border-0 hover:bg-muted/30">
                    <td className="px-4 py-2">
                      <span
                        className={cn(
                          'rounded px-2 py-0.5 text-xs font-medium',
                          a.severity === 'High' && 'bg-red-500/20 text-red-700 dark:text-red-400',
                          a.severity === 'Medium' && 'bg-amber-500/20 text-amber-700 dark:text-amber-400'
                        )}
                      >
                        {a.severity}
                      </span>
                    </td>
                    <td className="px-4 py-2">{formatAnomalyType(a.anomalyType)}</td>
                    <td className="px-4 py-2">{a.lastAlertedAt ? formatDate(a.lastAlertedAt) : '—'}</td>
                    <td className="max-w-[200px] truncate px-4 py-2" title={a.reason}>{a.reason}</td>
                    <td className="px-4 py-2">
                      {a.orderId ? (
                        <Link to={`/orders/${a.orderId}`} className="text-primary underline hover:no-underline">
                          {String(a.orderId).slice(0, 8)}…
                        </Link>
                      ) : (
                        '—'
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* Filters */}
      <section className="mb-6 rounded-lg border border-border bg-muted/30 p-4">
        <h3 className="mb-3 text-sm font-medium">Filters</h3>
        <div className="mb-4 flex flex-wrap items-center gap-2 border-b border-border pb-4">
          <span className="mr-1 text-xs text-muted-foreground">Presets</span>
          <div className="flex flex-wrap items-center gap-1.5">
            <button
              type="button"
              onClick={() => setSelectedPresetId(null)}
              className={cn(
                'rounded-full px-3 py-1 text-xs font-medium transition-colors',
                !selectedPresetId
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-muted-foreground hover:bg-muted/80'
              )}
              title="No preset applied"
            >
              None
            </button>
            {getBuiltInPresets().map((p) => {
              const active = selectedPresetId === p.id;
              return (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => applyPreset(p)}
                  className={cn(
                    'rounded-full px-3 py-1 text-xs font-medium transition-colors',
                    active ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground hover:bg-muted/80'
                  )}
                  title={p.name}
                >
                  {p.name}
                </button>
              );
            })}
            {customPresets.map((p) => {
              const active = selectedPresetId === p.id;
              return (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => applyPreset(p)}
                  className={cn(
                    'rounded-full px-3 py-1 text-xs font-medium transition-colors',
                    active ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground hover:bg-muted/80'
                  )}
                  title={p.name}
                >
                  {p.name}
                </button>
              );
            })}
          </div>
          <div className="ml-2 flex items-center gap-1 border-l border-border pl-2">
            <Button variant="outline" size="sm" onClick={saveCurrentAsPreset} title="Save current filters as a preset">
              Save current
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={deleteSelectedPreset}
              disabled={!isCustomSelected}
              title="Delete selected custom preset"
            >
              Delete
            </Button>
          </div>
        </div>
        <div className="flex flex-wrap items-end gap-4">
          <div>
            <label className="mb-1 block text-xs text-muted-foreground">From date</label>
            <input
              type="date"
              value={from}
              onChange={(e) => {
                setFrom(e.target.value);
                setPage(1);
                setSelectedPresetId(null);
              }}
              className="rounded border border-input bg-background px-2 py-1.5 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs text-muted-foreground">To date</label>
            <input
              type="date"
              value={to}
              onChange={(e) => {
                setTo(e.target.value);
                setPage(1);
                setSelectedPresetId(null);
              }}
              className="rounded border border-input bg-background px-2 py-1.5 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs text-muted-foreground">Anomaly type</label>
            <select
              value={anomalyType}
              onChange={(e) => {
                setAnomalyType(e.target.value);
                setPage(1);
                setSelectedPresetId(null);
              }}
              className="rounded border border-input bg-background px-2 py-1.5 text-sm"
            >
              <option value="">All</option>
              {ANOMALY_TYPES.map((t) => (
                <option key={t} value={t}>
                  {formatAnomalyType(t)}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs text-muted-foreground">Severity</label>
            <select
              value={severity}
              onChange={(e) => {
                setSeverity(e.target.value);
                setPage(1);
                setSelectedPresetId(null);
              }}
              className="rounded border border-input bg-background px-2 py-1.5 text-sm"
            >
              <option value="">All</option>
              {SEVERITIES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs text-muted-foreground">Stale threshold (hours)</label>
            <input
              type="number"
              min={1}
              max={720}
              value={staleThresholdHours}
              onChange={(e) => {
                const v = parseInt(e.target.value, 10);
                if (!Number.isNaN(v)) {
                  setStaleThresholdHours(Math.min(720, Math.max(1, v)));
                  setSelectedPresetId(null);
                }
              }}
              className="w-20 rounded border border-input bg-background px-2 py-1.5 text-sm"
              title="Alerted anomalies with no action after this many hours are counted as stale"
            />
          </div>
        </div>
      </section>

      {/* Recent anomalies table */}
      <section className="mb-8">
        <h2 className="mb-3 text-lg font-semibold">Recent anomalies</h2>
        {listQuery.isLoading ? (
          <Skeleton className="h-64 w-full rounded-lg" />
        ) : !listResult?.items?.length ? (
          <Card className="p-6 text-center text-muted-foreground">No anomalies match the filters.</Card>
        ) : (
          <div className="overflow-x-auto rounded-lg border border-border">
            <table className="w-full min-w-[900px] text-left text-sm">
              <thead className="border-b border-border bg-muted/50">
                <tr>
                  <th className="px-4 py-3 font-medium">Severity</th>
                  <th className="px-4 py-3 font-medium">Type</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Assigned To</th>
                  <th className="px-4 py-3 font-medium">Alerted</th>
                  <th className="px-4 py-3 font-medium">Order</th>
                  <th className="px-4 py-3 font-medium">Installer</th>
                  <th className="px-4 py-3 font-medium">Payout</th>
                  <th className="px-4 py-3 font-medium">Baseline</th>
                  <th className="px-4 py-3 font-medium">Reason</th>
                  <th className="px-4 py-3 font-medium">Path</th>
                  <th className="px-4 py-3 font-medium">Date</th>
                  <th className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {listResult.items.map((a: PayoutAnomalyDto, i: number) => (
                  <tr key={a.id ?? i} className="border-b border-border last:border-0 hover:bg-muted/30">
                    <td className="px-4 py-2">
                      <span
                        className={cn(
                          'rounded px-2 py-0.5 text-xs font-medium',
                          a.severity === 'High' && 'bg-red-500/20 text-red-700 dark:text-red-400',
                          a.severity === 'Medium' && 'bg-amber-500/20 text-amber-700 dark:text-amber-400',
                          a.severity === 'Low' && 'bg-muted text-muted-foreground'
                        )}
                      >
                        {a.severity}
                      </span>
                    </td>
                    <td className="px-4 py-2">{formatAnomalyType(a.anomalyType)}</td>
                    <td className="px-4 py-2">
                      <span
                        className={cn(
                          'rounded px-2 py-0.5 text-xs',
                          a.reviewStatus === 'Resolved' && 'bg-emerald-500/20 text-emerald-700 dark:text-emerald-400',
                          a.reviewStatus === 'FalsePositive' && 'bg-slate-500/20 text-slate-600 dark:text-slate-400',
                          a.reviewStatus === 'Investigating' && 'bg-blue-500/20 text-blue-700 dark:text-blue-400',
                          a.reviewStatus === 'Acknowledged' && 'bg-amber-500/20 text-amber-700 dark:text-amber-400',
                          (!a.reviewStatus || a.reviewStatus === 'Open') && 'text-muted-foreground'
                        )}
                      >
                        {a.reviewStatus || '—'}
                      </span>
                    </td>
                    <td className="px-4 py-2">{a.assignedToUserName ?? '—'}</td>
                    <td className="px-4 py-2" title={a.lastAlertedAt ? `Last alerted: ${formatDate(a.lastAlertedAt)}` : undefined}>
                      {a.alerted ? (
                        <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400">
                          <Bell className="h-4 w-4" />
                          {a.lastAlertedAt ? formatDate(a.lastAlertedAt) : 'Yes'}
                        </span>
                      ) : (
                        '—'
                      )}
                    </td>
                    <td className="px-4 py-2">
                      {a.orderId ? (
                        <Link
                          to={`/orders/${a.orderId}`}
                          className="text-primary underline hover:no-underline"
                        >
                          {a.orderId.slice(0, 8)}…
                        </Link>
                      ) : (
                        '—'
                      )}
                    </td>
                    <td className="px-4 py-2">{a.installerName ?? (a.installerId ? `${a.installerId.slice(0, 8)}…` : '—')}</td>
                    <td className="px-4 py-2">
                      {a.payoutAmount != null ? formatCurrency(Number(a.payoutAmount)) : '—'}
                    </td>
                    <td className="px-4 py-2">
                      {a.baselineAmount != null ? formatCurrency(Number(a.baselineAmount)) : '—'}
                    </td>
                    <td className="max-w-[200px] truncate px-4 py-2" title={a.reason}>
                      {a.reason}
                    </td>
                    <td className="px-4 py-2">{a.payoutPath ?? '—'}</td>
                    <td className="px-4 py-2">{formatDate(a.detectedAt)}</td>
                    <td className="px-4 py-2" title={a.lastActionAt ? formatDate(a.lastActionAt) : undefined}>
                      {a.lastActionAt ? formatDate(a.lastActionAt) : '—'}
                    </td>
                    <td className="px-4 py-2">
                      {canReviewAnomalies ? (
                        <div className="flex flex-wrap items-center gap-1">
                          {(!a.reviewStatus || a.reviewStatus === 'Open') && (
                            <button
                              type="button"
                              onClick={() => acknowledgeMutation.mutate({ id: a.id, body: a })}
                              disabled={acknowledgeMutation.isPending}
                              className="rounded border border-border px-2 py-1 text-xs hover:bg-muted"
                            >
                              Acknowledge
                            </button>
                          )}
                          <button
                            type="button"
                            onClick={() => setAssignModalAnomaly(a)}
                            className="rounded border border-border px-2 py-1 text-xs hover:bg-muted"
                          >
                            Assign
                          </button>
                          <button
                            type="button"
                            onClick={() => resolveMutation.mutate({ id: a.id, body: a })}
                            disabled={resolveMutation.isPending}
                            className="rounded border border-border px-2 py-1 text-xs hover:bg-muted"
                          >
                            Resolve
                          </button>
                          <button
                            type="button"
                            onClick={() => falsePositiveMutation.mutate({ id: a.id, body: a })}
                            disabled={falsePositiveMutation.isPending}
                            className="rounded border border-border px-2 py-1 text-xs hover:bg-muted"
                          >
                            False +
                          </button>
                          <button
                            type="button"
                            onClick={() => setCommentDrawerAnomalyId(a.id)}
                            className="rounded border border-border p-1 hover:bg-muted"
                            title="Comments"
                          >
                            <MessageSquare className="h-4 w-4" />
                          </button>
                        </div>
                      ) : (
                        <span className="text-muted-foreground text-xs">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {listResult && listResult.totalCount > pageSize && (
          <div className="mt-3 flex items-center justify-between text-sm text-muted-foreground">
            <span>
              Page {page} of {Math.ceil(listResult.totalCount / pageSize)} ({listResult.totalCount}{' '}
              total)
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="rounded border border-border px-2 py-1 disabled:opacity-50 hover:bg-muted"
              >
                Previous
              </button>
              <button
                type="button"
                onClick={() => setPage((p) => p + 1)}
                disabled={page >= Math.ceil(listResult.totalCount / pageSize)}
                className="rounded border border-border px-2 py-1 disabled:opacity-50 hover:bg-muted"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </section>

      {/* Top clusters */}
      {clusters.length > 0 && (
        <section className="mb-8">
          <h2 className="mb-3 text-lg font-semibold">Top clusters</h2>
          <p className="mb-3 text-sm text-muted-foreground">
            Installers or contexts with the most anomalies in the period.
          </p>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {clusters.map((c: PayoutAnomalyClusterDto, i: number) => (
              <Card key={i} className="p-4">
                <p className="text-xs font-medium text-muted-foreground">{c.clusterKind}</p>
                <p className="mt-1 font-semibold">{c.label}</p>
                <p className="mt-1 text-sm">
                  Anomalies: {c.anomalyCount}
                  {c.extraCount != null && (
                    <span className="text-muted-foreground"> (e.g. count in period: {c.extraCount})</span>
                  )}
                </p>
              </Card>
            ))}
          </div>
        </section>
      )}

      {/* Comment drawer */}
      {commentDrawerAnomalyId && (
        <div className="fixed inset-0 z-50 flex justify-end bg-black/30" onClick={() => setCommentDrawerAnomalyId(null)}>
          <div
            className="w-full max-w-md bg-card shadow-xl sm:max-w-lg"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-border p-4">
              <h3 className="text-lg font-semibold">Investigation notes</h3>
              <button
                type="button"
                onClick={() => setCommentDrawerAnomalyId(null)}
                className="rounded p-1 hover:bg-muted"
              >
                <XCircle className="h-5 w-5" />
              </button>
            </div>
            <div className="max-h-[70vh] overflow-y-auto p-4">
              {commentDrawerReviewQuery.data?.notesJson ? (
                (() => {
                  try {
                    const notes = JSON.parse(commentDrawerReviewQuery.data.notesJson) as Array<{ at: string; userName: string; text: string }>;
                    return (
                      <ul className="space-y-2">
                        {notes.map((n, idx) => (
                          <li key={idx} className="rounded border border-border bg-muted/30 p-2 text-sm">
                            <p className="text-xs text-muted-foreground">
                              {n.userName} · {formatDate(n.at)}
                            </p>
                            <p className="mt-1">{n.text}</p>
                          </li>
                        ))}
                      </ul>
                    );
                  } catch {
                    return <p className="text-sm text-muted-foreground">No notes yet.</p>;
                  }
                })()
              ) : (
                <p className="text-sm text-muted-foreground">No notes yet.</p>
              )}
              <div className="mt-4">
                <label className="mb-1 block text-sm font-medium">Add note</label>
                <textarea
                  value={newCommentText}
                  onChange={(e) => setNewCommentText(e.target.value)}
                  placeholder="Investigation note..."
                  rows={3}
                  className="w-full rounded border border-input bg-background px-2 py-1.5 text-sm"
                />
                <button
                  type="button"
                  onClick={() => {
                    if (newCommentText.trim()) {
                      commentMutation.mutate({ id: commentDrawerAnomalyId, text: newCommentText.trim() });
                      setNewCommentText('');
                    }
                  }}
                  disabled={!newCommentText.trim() || commentMutation.isPending}
                  className="mt-2 rounded bg-primary px-3 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
                >
                  Add comment
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Assign modal */}
      {assignModalAnomaly && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={() => setAssignModalAnomaly(null)}>
          <div
            className="w-full max-w-sm rounded-lg border border-border bg-card p-4 shadow-xl"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-lg font-semibold">Assign anomaly</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              {formatAnomalyType(assignModalAnomaly.anomalyType)} · {assignModalAnomaly.reason.slice(0, 50)}…
            </p>
            <div className="mt-4">
              <label className="mb-1 block text-sm font-medium">Assign to</label>
              <select
                value={assignUserId}
                onChange={(e) => setAssignUserId(e.target.value)}
                className="w-full rounded border border-input bg-background px-2 py-1.5 text-sm"
              >
                <option value="">— Unassigned —</option>
                {usersQuery.data?.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.name || u.email}
                  </option>
                ))}
              </select>
            </div>
            <div className="mt-4 flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setAssignModalAnomaly(null)}
                className="rounded border border-border px-3 py-1.5 text-sm hover:bg-muted"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => assignMutation.mutate({ id: assignModalAnomaly.id, userId: assignUserId || null })}
                disabled={assignMutation.isPending}
                className="rounded bg-primary px-3 py-1.5 text-sm text-primary-foreground hover:opacity-90 disabled:opacity-50"
              >
                Assign
              </button>
            </div>
          </div>
        </div>
      )}
    </PageShell>
  );
};

export default PayoutAnomaliesPage;
