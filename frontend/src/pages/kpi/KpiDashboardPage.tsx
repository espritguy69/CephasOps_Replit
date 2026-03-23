import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  Target, TrendingUp, TrendingDown, Clock, CheckCircle2, 
  XCircle, AlertCircle, RefreshCw, Calendar, Users
} from 'lucide-react';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, Card, Button, useToast, Select, EmptyState } from '../../components/ui';
import { getJobEarningRecords } from '../../api/payroll';
import { getKpiProfiles } from '../../api/kpiProfiles';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { useDepartment } from '../../contexts/DepartmentContext';
import KpiCard from '../../components/dashboard/KpiCard';

interface KpiStats {
  totalJobs: number;
  onTimeJobs: number;
  lateJobs: number;
  exceededSlaJobs: number;
  averageCompletionTime: number;
  onTimeRate: number;
}

const KpiDashboardPage: React.FC = () => {
  const { showError } = useToast();
  const { activeDepartment } = useDepartment();
  const [selectedPeriod, setSelectedPeriod] = useState<string>(
    new Date().toISOString().slice(0, 7) // Current month (YYYY-MM)
  );
  const [selectedSiId, setSelectedSiId] = useState<string>('');

  const companyId = activeDepartment?.companyId || '';

  // Fetch job earning records for KPI calculations
  const { data: earningRecords = [], isLoading: isLoadingRecords, refetch } = useQuery({
    queryKey: ['jobEarningRecords', companyId, selectedPeriod, selectedSiId || null],
    queryFn: async () => {
      const records = await getJobEarningRecords({
        period: selectedPeriod,
        siId: selectedSiId || undefined
      } as any);
      return records;
    },
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  // Fetch KPI profiles for reference
  const { data: kpiProfiles = [], isLoading: isLoadingProfiles } = useQuery({
    queryKey: ['kpiProfiles', companyId],
    queryFn: async () => {
      const profiles = await getKpiProfiles({ isActive: true } as any);
      return profiles;
    },
    enabled: !!companyId,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });

  // Fetch service installers for filter dropdown
  const { data: serviceInstallers = [] } = useQuery({
    queryKey: ['serviceInstallers', companyId],
    queryFn: async () => {
      const installers = await getServiceInstallers({ isActive: true });
      return installers;
    },
    enabled: !!companyId,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });

  // Calculate KPI statistics
  const calculateStats = (): KpiStats => {
    if (earningRecords.length === 0) {
      return {
        totalJobs: 0,
        onTimeJobs: 0,
        lateJobs: 0,
        exceededSlaJobs: 0,
        averageCompletionTime: 0,
        onTimeRate: 0
      };
    }

    const onTimeJobs = earningRecords.filter(r => {
      const kpi = r.kpiResult?.toLowerCase() || '';
      return kpi === 'ontime';
    }).length;
    const lateJobs = earningRecords.filter(r => {
      const kpi = r.kpiResult?.toLowerCase() || '';
      return kpi === 'late';
    }).length;
    const exceededSlaJobs = earningRecords.filter(r => {
      const kpi = r.kpiResult?.toLowerCase() || '';
      return kpi === 'exceededsla' || kpi === 'exceeded sla';
    }).length;
    const totalJobs = earningRecords.length;
    const onTimeRate = totalJobs > 0 ? (onTimeJobs / totalJobs) * 100 : 0;

    // Note: Average completion time would need to be calculated from order data
    // For now, we'll use a placeholder
    const averageCompletionTime = 0;

    return {
      totalJobs,
      onTimeJobs,
      lateJobs,
      exceededSlaJobs,
      averageCompletionTime,
      onTimeRate
    };
  };

  const stats = calculateStats();

  // Generate period options (last 12 months)
  const generatePeriodOptions = () => {
    const options = [];
    const today = new Date();
    for (let i = 0; i < 12; i++) {
      const date = new Date(today.getFullYear(), today.getMonth() - i, 1);
      const period = date.toISOString().slice(0, 7);
      const label = date.toLocaleDateString('en-US', { year: 'numeric', month: 'long' });
      options.push({ value: period, label });
    }
    return options;
  };

  const handleRefresh = () => {
    refetch();
  };

  if (isLoadingRecords || isLoadingProfiles) {
    return <LoadingSpinner message="Loading KPI dashboard..." fullPage />;
  }

  return (
    <PageShell
      title="KPI Dashboard"
      actions={
        <div className="flex gap-2">
          <Button size="sm" variant="outline" className="gap-2" onClick={handleRefresh}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
        </div>
      }
    >
      {/* Filters */}
      <Card className="mb-6">
        <div className="flex flex-wrap gap-4 items-end">
          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Period
            </label>
            <Select
              value={selectedPeriod}
              onChange={(e) => setSelectedPeriod(e.target.value)}
              options={generatePeriodOptions()}
            />
          </div>
          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Service Installer (Optional)
            </label>
            <Select
              value={selectedSiId}
              onChange={(e) => setSelectedSiId(e.target.value)}
              options={[
                { value: '', label: 'All Installers' },
                ...serviceInstallers.map(si => ({ value: si.id, label: si.name }))
              ]}
            />
          </div>
        </div>
      </Card>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <KpiCard
          title="Total Jobs"
          value={stats.totalJobs}
          subtitle={`Period: ${selectedPeriod}`}
          icon={Target}
        />
        <KpiCard
          title="On-Time Rate"
          value={`${stats.onTimeRate.toFixed(1)}%`}
          subtitle={`${stats.onTimeJobs} of ${stats.totalJobs} jobs`}
          icon={CheckCircle2}
          trend={stats.onTimeRate >= 80 ? 'up' : stats.onTimeRate >= 60 ? 'neutral' : 'down'}
          trendValue={stats.totalJobs > 0 ? `${stats.onTimeJobs}/${stats.totalJobs}` : 'N/A'}
        />
        <KpiCard
          title="Late Jobs"
          value={stats.lateJobs}
          subtitle={`${stats.totalJobs > 0 ? ((stats.lateJobs / stats.totalJobs) * 100).toFixed(1) : 0}% of total`}
          icon={Clock}
          trend={stats.lateJobs === 0 ? 'up' : stats.lateJobs < stats.totalJobs * 0.1 ? 'neutral' : 'down'}
        />
        <KpiCard
          title="SLA Exceeded"
          value={stats.exceededSlaJobs}
          subtitle={`${stats.totalJobs > 0 ? ((stats.exceededSlaJobs / stats.totalJobs) * 100).toFixed(1) : 0}% of total`}
          icon={AlertCircle}
          trend={stats.exceededSlaJobs === 0 ? 'up' : 'down'}
        />
      </div>

      {/* KPI Breakdown by Order Type */}
      <Card className="mb-6">
        <h3 className="text-lg font-semibold text-slate-900 mb-4">KPI Breakdown by Order Type</h3>
        {earningRecords.length === 0 ? (
          <EmptyState
            title="No KPI Data"
            description={`No job earning records found for period ${selectedPeriod}`}
            icon={<Target className="h-12 w-12" />}
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-slate-200">
                  <th className="text-left py-3 px-4 text-sm font-semibold text-slate-700">Order Type</th>
                  <th className="text-right py-3 px-4 text-sm font-semibold text-slate-700">Total Jobs</th>
                  <th className="text-right py-3 px-4 text-sm font-semibold text-slate-700">On-Time</th>
                  <th className="text-right py-3 px-4 text-sm font-semibold text-slate-700">Late</th>
                  <th className="text-right py-3 px-4 text-sm font-semibold text-slate-700">SLA Exceeded</th>
                  <th className="text-right py-3 px-4 text-sm font-semibold text-slate-700">On-Time Rate</th>
                </tr>
              </thead>
              <tbody>
                {Object.entries(
                  earningRecords.reduce((acc, record) => {
                    const orderType = record.orderTypeName || record.orderTypeCode || 'Unknown';
                    if (!acc[orderType]) {
                      acc[orderType] = { total: 0, onTime: 0, late: 0, exceeded: 0 };
                    }
                    acc[orderType].total++;
                    const kpiResult = record.kpiResult?.toLowerCase() || '';
                    if (kpiResult === 'ontime') acc[orderType].onTime++;
                    else if (kpiResult === 'late') acc[orderType].late++;
                    else if (kpiResult === 'exceededsla' || kpiResult === 'exceeded sla') acc[orderType].exceeded++;
                    return acc;
                  }, {} as Record<string, { total: number; onTime: number; late: number; exceeded: number }>)
                ).map(([orderType, data]) => {
                  const onTimeRate = data.total > 0 ? (data.onTime / data.total) * 100 : 0;
                  return (
                    <tr key={orderType} className="border-b border-slate-100 hover:bg-slate-50">
                      <td className="py-3 px-4 text-sm text-slate-900">{orderType}</td>
                      <td className="py-3 px-4 text-sm text-slate-600 text-right">{data.total}</td>
                      <td className="py-3 px-4 text-sm text-green-600 text-right font-medium">{data.onTime}</td>
                      <td className="py-3 px-4 text-sm text-yellow-600 text-right font-medium">{data.late}</td>
                      <td className="py-3 px-4 text-sm text-red-600 text-right font-medium">{data.exceeded}</td>
                      <td className="py-3 px-4 text-sm text-slate-900 text-right font-semibold">
                        {onTimeRate.toFixed(1)}%
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Active KPI Profiles */}
      <Card>
        <h3 className="text-lg font-semibold text-slate-900 mb-4">Active KPI Profiles</h3>
        {kpiProfiles.length === 0 ? (
          <EmptyState
            title="No KPI Profiles"
            description="No active KPI profiles configured. Configure profiles in Settings → KPI Profiles."
            icon={<Target className="h-12 w-12" />}
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {kpiProfiles.map((profile) => (
              <div key={profile.id} className="p-4 border border-slate-200 rounded-lg">
                <div className="flex items-start justify-between mb-2">
                  <h4 className="font-semibold text-slate-900">{profile.name}</h4>
                  {profile.isDefault && (
                    <span className="px-2 py-0.5 text-xs font-medium bg-blue-100 text-blue-700 rounded">
                      Default
                    </span>
                  )}
                </div>
                {profile.description && (
                  <p className="text-sm text-slate-600 mb-3">{profile.description}</p>
                )}
                <div className="space-y-1 text-sm">
                  {(profile.orderType || profile.orderTypeName) && (
                    <div className="flex justify-between">
                      <span className="text-slate-600">Order Type:</span>
                      <span className="font-medium text-slate-900">{profile.orderTypeName || profile.orderType || 'All'}</span>
                    </div>
                  )}
                  {(profile as any).maxJobDurationMinutes && (
                    <div className="flex justify-between">
                      <span className="text-slate-600">Max Duration:</span>
                      <span className="font-medium text-slate-900">
                        {(profile as any).maxJobDurationMinutes ? `${(profile as any).maxJobDurationMinutes} min` : 'N/A'}
                      </span>
                    </div>
                  )}
                  {(profile as any).maxReschedulesAllowed !== undefined && (
                    <div className="flex justify-between">
                      <span className="text-slate-600">Max Reschedules:</span>
                      <span className="font-medium text-slate-900">
                        {(profile as any).maxReschedulesAllowed ?? 'Unlimited'}
                      </span>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>
    </PageShell>
  );
};

export default KpiDashboardPage;

