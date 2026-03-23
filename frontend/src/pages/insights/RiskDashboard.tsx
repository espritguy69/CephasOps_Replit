import React, { useCallback, useEffect, useState } from 'react';
import { AlertTriangle, RefreshCw, Repeat, ShieldAlert, UserX, Wrench } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Button, Card, LoadingSpinner, useToast } from '../../components/ui';
import { MetricCard } from '../../components/insights';
import { getRiskQuality, type RiskQualityDto } from '../../api/operationalInsights';

const RiskDashboard: React.FC = () => {
  const { showError } = useToast();
  const [data, setData] = useState<RiskQualityDto | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await getRiskQuality();
      setData(result);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load risk & quality';
      showError(message);
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [showError]);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <PageShell
      title="Risk & Quality"
      description="Complaints, failures, and repeat issues"
    >
      <div className="space-y-6">
        <div className="flex justify-end">
          <Button variant="outline" size="sm" onClick={load} disabled={loading}>
            <RefreshCw className={loading ? 'animate-spin h-4 w-4 mr-2' : 'h-4 w-4 mr-2'} />
            Refresh
          </Button>
        </div>
        {loading && !data ? (
          <LoadingSpinner />
        ) : data ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
            <MetricCard
              title="Customer complaints (month)"
              value={data.customerComplaints}
              icon={UserX}
              iconBg={data.customerComplaints > 0 ? 'bg-amber-500/10' : 'bg-muted'}
            />
            <MetricCard
              title="Device failures (month)"
              value={data.deviceFailures}
              icon={Wrench}
              iconBg={data.deviceFailures > 0 ? 'bg-amber-500/10' : 'bg-muted'}
            />
            <MetricCard
              title="Rescheduled orders (month)"
              value={data.rescheduledOrders}
              icon={Repeat}
              iconBg="bg-blue-500/10"
            />
            <MetricCard
              title="Installer rating avg"
              value={data.installerRatingAverage != null ? data.installerRatingAverage.toFixed(1) : '—'}
              icon={ShieldAlert}
            />
            <MetricCard
              title="Repeat customer issues"
              value={data.repeatCustomerIssues}
              icon={AlertTriangle}
              iconBg={data.repeatCustomerIssues > 0 ? 'bg-red-500/10' : 'bg-muted'}
            />
          </div>
        ) : (
          <Card className="p-6">
            <p className="text-muted-foreground">No data available.</p>
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default RiskDashboard;
