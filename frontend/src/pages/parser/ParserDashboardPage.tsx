import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  FileText, Clock, AlertTriangle, CheckCircle, XCircle,
  RefreshCw, TrendingUp, Eye, ArrowRight, Mail, Settings,
  FileCheck, Loader2, Download
} from 'lucide-react';
import { getParserStatistics, getParserAnalytics, getParseSessions, exportParserLogs, type ParseSession } from '../../api/parser';
import { pollAllEmailAccounts } from '../../api/email';
import { Button, Card, LoadingSpinner, Badge, useToast } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '../../lib/utils';
import { formatLocalDateTime } from '../../utils/dateUtils';

interface StatCardProps {
  title: string;
  value: number | string;
  icon: React.ElementType;
  iconBg?: string;
  trend?: 'up' | 'down';
  loading?: boolean;
  onClick?: () => void;
}

const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  icon: Icon,
  iconBg,
  trend,
  loading,
  onClick
}) => {
  return (
    <Card
      className={cn(
        "p-4 hover:shadow-md transition-shadow cursor-pointer",
        onClick && "hover:border-primary"
      )}
      onClick={onClick}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-muted-foreground mb-2">{title}</p>
          <div className="flex items-baseline gap-2">
            {loading ? (
              <div className="h-8 w-20 bg-muted animate-pulse rounded" />
            ) : (
              <span className="text-2xl font-bold text-foreground tracking-tight">{value}</span>
            )}
          </div>
        </div>
        <div className={cn(
          "h-10 w-10 rounded-lg flex items-center justify-center flex-shrink-0",
          iconBg || "bg-primary/10"
        )}>
          <Icon className={cn(
            "h-5 w-5",
            iconBg?.includes('emerald') ? "text-emerald-600" :
            iconBg?.includes('amber') ? "text-amber-600" :
            iconBg?.includes('red') ? "text-red-600" :
            iconBg?.includes('blue') ? "text-blue-600" :
            "text-primary"
          )} />
        </div>
      </div>
    </Card>
  );
};

const ParserDashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();
  const [isPolling, setIsPolling] = useState(false);

  // Fetch statistics
  const { data: statistics, isLoading: statsLoading, refetch: refetchStats } = useQuery({
    queryKey: ['parserStatistics'],
    queryFn: getParserStatistics,
    refetchInterval: 30000, // Refetch every 30 seconds
  });

  // Fetch recent parse sessions
  const { data: recentSessions = [], isLoading: sessionsLoading } = useQuery({
    queryKey: ['parseSessions', 'recent'],
    queryFn: () => getParseSessions(),
  });

  // Analytics (last 30 days)
  const toDate = new Date();
  const fromDate = new Date(toDate);
  fromDate.setDate(fromDate.getDate() - 30);
  const { data: analytics, isLoading: analyticsLoading } = useQuery({
    queryKey: ['parserAnalytics', fromDate.toISOString().slice(0, 10), toDate.toISOString().slice(0, 10)],
    queryFn: () => getParserAnalytics(fromDate.toISOString().slice(0, 10), toDate.toISOString().slice(0, 10)),
    refetchInterval: 60000,
  });

  // Manual parse trigger mutation
  const pollMutation = useMutation({
    mutationFn: pollAllEmailAccounts,
    onSuccess: (results) => {
      const totalEmails = results.reduce((sum, r) => sum + (r.emailsFetched || 0), 0);
      const totalSessions = results.reduce((sum, r) => sum + (r.parseSessionsCreated || 0), 0);
      showSuccess(`Email polling completed: ${totalEmails} emails fetched, ${totalSessions} parse sessions created`);
      queryClient.invalidateQueries({ queryKey: ['parserStatistics'] });
      queryClient.invalidateQueries({ queryKey: ['parseSessions'] });
      queryClient.invalidateQueries({ queryKey: ['parsedOrderDrafts'] });
    },
    onError: (error: Error) => {
      showError(`Failed to poll emails: ${error.message}`);
    },
    onSettled: () => {
      setIsPolling(false);
    }
  });

  const handleParseNow = () => {
    setIsPolling(true);
    pollMutation.mutate();
  };

  const handleExportLogs = async (format: 'csv' | 'json' = 'csv') => {
    try {
      await exportParserLogs(format);
      showSuccess(`Parser logs exported as ${format.toUpperCase()}`);
    } catch (error) {
      const err = error as Error;
      showError(`Failed to export logs: ${err.message}`);
    }
  };

  const stats = statistics || {
    totalSessionsToday: 0,
    successfulSessionsToday: 0,
    failedSessionsToday: 0,
    totalDrafts: 0,
    pendingDrafts: 0,
    validDrafts: 0,
    needsReviewDrafts: 0,
    rejectedDrafts: 0,
    approvedDrafts: 0,
    averageConfidenceScore: 0,
    totalSessionsAllTime: 0,
    totalDraftsAllTime: 0
  };

  const successRate = stats.totalSessionsToday > 0
    ? Math.round((stats.successfulSessionsToday / stats.totalSessionsToday) * 100)
    : 0;

  const recentSessionsList = recentSessions.slice(0, 5);

  return (
    <PageShell
      title="Parser Dashboard"
      description="Overview of email parsing activity and statistics"
      actions={
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate('/orders/parser')}
          >
            <Eye className="h-4 w-4 mr-2" />
            View All Drafts
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate('/settings/email')}
          >
            <Settings className="h-4 w-4 mr-2" />
            Parser Settings
          </Button>
          <Button
            size="sm"
            onClick={handleParseNow}
            disabled={isPolling}
          >
            {isPolling ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Polling...
              </>
            ) : (
              <>
                <Mail className="h-4 w-4 mr-2" />
                Parse Now
              </>
            )}
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => refetchStats()}
          >
            <RefreshCw className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExportLogs('csv')}
            title="Export logs as CSV"
          >
            <Download className="h-4 w-4 mr-2" />
            Export Logs
          </Button>
        </div>
      }
    >
      {statsLoading ? (
        <div className="flex items-center justify-center py-12">
          <LoadingSpinner size="lg" />
        </div>
      ) : (
        <>
          {/* Statistics Cards */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            <StatCard
              title="Sessions Today"
              value={stats.totalSessionsToday}
              icon={FileText}
              iconBg="bg-blue-100 dark:bg-blue-900/30"
              loading={statsLoading}
              onClick={() => navigate('/orders/parser?status=all')}
            />
            <StatCard
              title="Success Rate"
              value={`${successRate}%`}
              icon={TrendingUp}
              iconBg={successRate >= 80 ? "bg-emerald-100 dark:bg-emerald-900/30" : "bg-amber-100 dark:bg-amber-900/30"}
              loading={statsLoading}
            />
            <StatCard
              title="Pending Review"
              value={stats.needsReviewDrafts}
              icon={Clock}
              iconBg="bg-amber-100 dark:bg-amber-900/30"
              loading={statsLoading}
              onClick={() => navigate('/orders/parser?validationStatus=NeedsReview')}
            />
            <StatCard
              title="Total Drafts"
              value={stats.totalDrafts}
              icon={FileCheck}
              iconBg="bg-primary/10"
              loading={statsLoading}
              onClick={() => navigate('/orders/parser')}
            />
          </div>

          {/* Secondary Statistics */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            <StatCard
              title="Successful Sessions"
              value={stats.successfulSessionsToday}
              icon={CheckCircle}
              iconBg="bg-emerald-100 dark:bg-emerald-900/30"
              loading={statsLoading}
            />
            <StatCard
              title="Failed Sessions"
              value={stats.failedSessionsToday}
              icon={XCircle}
              iconBg="bg-red-100 dark:bg-red-900/30"
              loading={statsLoading}
            />
            <StatCard
              title="Valid Drafts"
              value={stats.validDrafts}
              icon={CheckCircle}
              iconBg="bg-emerald-100 dark:bg-emerald-900/30"
              loading={statsLoading}
              onClick={() => navigate('/orders/parser?validationStatus=Valid')}
            />
            <StatCard
              title="Avg Confidence"
              value={`${Math.round(stats.averageConfidenceScore)}%`}
              icon={TrendingUp}
              iconBg={stats.averageConfidenceScore >= 80 ? "bg-emerald-100 dark:bg-emerald-900/30" : "bg-amber-100 dark:bg-amber-900/30"}
              loading={statsLoading}
            />
          </div>

          {/* Recent Activity */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Recent Parse Sessions */}
            <Card className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold">Recent Parse Sessions</h3>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => navigate('/orders/parser')}
                >
                  View All
                  <ArrowRight className="h-4 w-4 ml-2" />
                </Button>
              </div>
              {sessionsLoading ? (
                <div className="flex items-center justify-center py-8">
                  <LoadingSpinner />
                </div>
              ) : recentSessionsList.length === 0 ? (
                <div className="text-center py-8 text-muted-foreground">
                  <FileText className="h-12 w-12 mx-auto mb-2 opacity-50" />
                  <p>No parse sessions yet</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {recentSessionsList.map((session) => (
                    <div
                      key={session.id}
                      className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted/50 transition-colors cursor-pointer"
                      onClick={() => navigate(`/orders/parser/sessions/${session.id}`)}
                    >
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <Badge
                            variant={
                              session.status === 'Completed' ? 'default' :
                              session.status === 'Failed' || session.status === 'Error' ? 'destructive' :
                              'secondary'
                            }
                            className="text-xs"
                          >
                            {session.status}
                          </Badge>
                          <span className="text-sm font-medium text-muted-foreground">
                            {session.sourceType || 'Unknown'}
                          </span>
                        </div>
                        <p className="text-sm text-foreground truncate">
                          {session.sourceDescription || 'No description'}
                        </p>
                        <p className="text-xs text-muted-foreground mt-1">
                          {formatLocalDateTime(session.createdAt)}
                        </p>
                      </div>
                      <div className="flex items-center gap-2 ml-4">
                        <span className="text-sm font-medium">
                          {session.parsedOrdersCount} orders
                        </span>
                        <ArrowRight className="h-4 w-4 text-muted-foreground" />
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </Card>

            {/* Draft Status Summary */}
            <Card className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold">Draft Status Summary</h3>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => navigate('/orders/parser')}
                >
                  View All
                  <ArrowRight className="h-4 w-4 ml-2" />
                </Button>
              </div>
              <div className="space-y-3">
                <div
                  className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted/50 transition-colors cursor-pointer"
                  onClick={() => navigate('/orders/parser?validationStatus=Pending')}
                >
                  <div className="flex items-center gap-3">
                    <Clock className="h-5 w-5 text-amber-600" />
                    <span className="text-sm font-medium">Pending</span>
                  </div>
                  <span className="text-sm font-bold">{stats.pendingDrafts}</span>
                </div>
                <div
                  className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted/50 transition-colors cursor-pointer"
                  onClick={() => navigate('/orders/parser?validationStatus=Valid')}
                >
                  <div className="flex items-center gap-3">
                    <CheckCircle className="h-5 w-5 text-emerald-600" />
                    <span className="text-sm font-medium">Valid</span>
                  </div>
                  <span className="text-sm font-bold">{stats.validDrafts}</span>
                </div>
                <div
                  className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted/50 transition-colors cursor-pointer"
                  onClick={() => navigate('/orders/parser?validationStatus=NeedsReview')}
                >
                  <div className="flex items-center gap-3">
                    <AlertTriangle className="h-5 w-5 text-amber-600" />
                    <span className="text-sm font-medium">Needs Review</span>
                  </div>
                  <span className="text-sm font-bold">{stats.needsReviewDrafts}</span>
                </div>
                <div
                  className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted/50 transition-colors cursor-pointer"
                  onClick={() => navigate('/orders/parser?validationStatus=Rejected')}
                >
                  <div className="flex items-center gap-3">
                    <XCircle className="h-5 w-5 text-red-600" />
                    <span className="text-sm font-medium">Rejected</span>
                  </div>
                  <span className="text-sm font-bold">{stats.rejectedDrafts}</span>
                </div>
                <div className="flex items-center justify-between p-3 rounded-lg border border-border bg-muted/30">
                  <div className="flex items-center gap-3">
                    <CheckCircle className="h-5 w-5 text-emerald-600" />
                    <span className="text-sm font-medium">Approved (Orders Created)</span>
                  </div>
                  <span className="text-sm font-bold">{stats.approvedDrafts}</span>
                </div>
              </div>
            </Card>
          </div>

          {/* Parser Analytics (period metrics) */}
          {analyticsLoading ? (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          ) : analytics ? (
            <div className="mt-6 space-y-6">
              <h3 className="text-lg font-semibold">Parser analytics (last 30 days)</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <StatCard
                  title="Parse success rate (period)"
                  value={`${Math.round(analytics.parseSuccessRate)}%`}
                  icon={TrendingUp}
                  iconBg={analytics.parseSuccessRate >= 80 ? "bg-emerald-100 dark:bg-emerald-900/30" : "bg-amber-100 dark:bg-amber-900/30"}
                />
                <StatCard
                  title="Auto-match rate (period)"
                  value={`${Math.round(analytics.autoMatchRate)}%`}
                  icon={CheckCircle}
                  iconBg={analytics.autoMatchRate >= 80 ? "bg-emerald-100 dark:bg-emerald-900/30" : "bg-amber-100 dark:bg-amber-900/30"}
                />
                <StatCard title="Sessions (period)" value={analytics.totalSessions} icon={FileText} iconBg="bg-blue-100 dark:bg-blue-900/30" />
                <StatCard title="Drafts (period)" value={analytics.totalDrafts} icon={FileCheck} iconBg="bg-primary/10" />
              </div>
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <Card className="p-6">
                  <h4 className="text-sm font-semibold mb-3">Confidence distribution</h4>
                  <div className="space-y-2">
                    {analytics.confidenceDistribution.map((b) => (
                      <div key={b.label} className="flex items-center gap-3">
                        <span className="text-sm text-muted-foreground w-16">{b.label}</span>
                        <div className="flex-1 h-6 bg-muted rounded overflow-hidden">
                          <div
                            className="h-full bg-primary/70 rounded"
                            style={{ width: `${analytics.totalDrafts ? (100 * b.count) / analytics.totalDrafts : 0}%` }}
                          />
                        </div>
                        <span className="text-sm font-medium w-8">{b.count}</span>
                      </div>
                    ))}
                  </div>
                </Card>
                <Card className="p-6">
                  <h4 className="text-sm font-semibold mb-3">Orders created per day (sample)</h4>
                  <div className="max-h-48 overflow-y-auto space-y-1">
                    {analytics.ordersCreatedPerDay.slice(-14).reverse().map((d) => (
                      <div key={d.date} className="flex justify-between text-sm">
                        <span className="text-muted-foreground">{d.date}</span>
                        <span className="font-medium">{d.count}</span>
                      </div>
                    ))}
                    {analytics.ordersCreatedPerDay.length === 0 && (
                      <p className="text-sm text-muted-foreground">No orders created from parser in this period.</p>
                    )}
                  </div>
                </Card>
              </div>
              {analytics.commonErrors.length > 0 && (
                <Card className="p-6">
                  <h4 className="text-sm font-semibold mb-3">Common errors</h4>
                  <ul className="space-y-2">
                    {analytics.commonErrors.map((e, i) => (
                      <li key={i} className="flex justify-between gap-2 text-sm">
                        <span className="text-muted-foreground truncate flex-1" title={e.message}>{e.message}</span>
                        <span className="font-medium flex-shrink-0">{e.count}</span>
                      </li>
                    ))}
                  </ul>
                </Card>
              )}
            </div>
          ) : null}
        </>
      )}
    </PageShell>
  );
};

export default ParserDashboardPage;

