import React, { useState, useEffect, useMemo } from 'react';
import { TrendingUp, TrendingDown, DollarSign, BarChart3 } from 'lucide-react';
import { getPnlSummary, getPnlPeriods } from '../../api/pnl';
import { LoadingSpinner, useToast, Card } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { PnlSummary, PnlPeriod, PnlSummaryFilters } from '../../types/pnl';
import { PnlWaterfallChart, PnlTrendChart } from '../../components/charts';

const PnlSummaryPage: React.FC = () => {
  const { showError } = useToast();
  const [summary, setSummary] = useState<PnlSummary | null>(null);
  const [periods, setPeriods] = useState<PnlPeriod[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedPeriod, setSelectedPeriod] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, [selectedPeriod]);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const params: PnlSummaryFilters = selectedPeriod ? { periodId: selectedPeriod } : {};
      const [summaryData, periodsData] = await Promise.all([
        getPnlSummary(params),
        getPnlPeriods()
      ]);
      setSummary(summaryData as PnlSummary);
      setPeriods(Array.isArray(periodsData) ? periodsData : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load P&L data');
      console.error('Error loading P&L data:', err);
    } finally {
      setLoading(false);
    }
  };

  // Generate trend data for charts
  const trendData = useMemo(() => generateTrendData(summary), [summary]);

  if (loading) {
    return (
      <PageShell title="P&L Summary" breadcrumbs={[{ label: 'P&L' }]}>
        <div data-testid="pnl-summary-root">
          <LoadingSpinner message="Loading P&L summary..." fullPage />
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="P&L Summary"
      breadcrumbs={[{ label: 'P&L' }]}
      actions={
        <select
          value={selectedPeriod || ''}
          onChange={(e) => setSelectedPeriod(e.target.value || null)}
          className="h-10 md:h-9 px-3 rounded-md border border-input bg-background text-sm min-h-[44px] md:min-h-0"
        >
          <option value="">All Periods</option>
          {periods.map((period) => (
            <option key={period.id} value={period.id}>
              {period.period}
            </option>
          ))}
        </select>
      }
    >
      <div data-testid="pnl-summary-root" className="max-w-full lg:max-w-7xl mx-auto">
      {summary && (
        <>
          {/* KPI Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 md:gap-4 mb-4 md:mb-6">
            <Card>
              <div className="flex items-center justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <p className="text-xs md:text-sm text-muted-foreground mb-1">Total Revenue</p>
                  <p className="text-base md:text-lg font-bold text-foreground">RM {(summary.totalRevenue || 0).toLocaleString()}</p>
                </div>
                <TrendingUp className="h-5 w-5 md:h-6 md:w-6 text-green-500 flex-shrink-0" />
              </div>
            </Card>

            <Card>
              <div className="flex items-center justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <p className="text-xs md:text-sm text-muted-foreground mb-1">Total Costs</p>
                  <p className="text-base md:text-lg font-bold text-foreground">RM {(summary.totalCosts || 0).toLocaleString()}</p>
                </div>
                <TrendingDown className="h-5 w-5 md:h-6 md:w-6 text-red-500 flex-shrink-0" />
              </div>
            </Card>

            <Card>
              <div className="flex items-center justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <p className="text-xs md:text-sm text-muted-foreground mb-1">Gross Profit</p>
                  <p className="text-base md:text-lg font-bold text-foreground">RM {(summary.grossProfit || 0).toLocaleString()}</p>
                </div>
                <DollarSign className="h-5 w-5 md:h-6 md:w-6 text-blue-500 flex-shrink-0" />
              </div>
            </Card>

            <Card>
              <div className="flex items-center justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <p className="text-xs md:text-sm text-muted-foreground mb-1">Net Profit</p>
                  <p className="text-base md:text-lg font-bold text-foreground">RM {(summary.netProfit || 0).toLocaleString()}</p>
                </div>
                <BarChart3 className="h-5 w-5 md:h-6 md:w-6 text-purple-500 flex-shrink-0" />
              </div>
            </Card>
          </div>

          {/* Syncfusion Charts - Professional Visual Analytics */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 md:gap-6 mb-4 md:mb-6">
            <PnlWaterfallChart
              revenue={summary.totalRevenue || 0}
              siCosts={(summary.totalCosts || 0) * 0.5} // Placeholder: 50% SI costs
              materialCosts={(summary.totalCosts || 0) * 0.3} // Placeholder: 30% materials
              overheads={(summary.totalCosts || 0) * 0.2} // Placeholder: 20% overhead
            />
            <PnlTrendChart data={trendData} />
          </div>
        </>
      )}
      </div>
    </PageShell>
  );
};

// Generate trend data for last 12 months
function generateTrendData(summary: PnlSummary | null): any[] {
  if (!summary) return [];
  
  // Placeholder: Generate sample trend data
  // In production, this would come from API
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  return months.map((month, index) => {
    const factor = 0.7 + (Math.random() * 0.6); // Vary between 70-130%
    return {
      month,
      revenue: (summary.totalRevenue || 0) * factor / 12,
      costs: (summary.totalCosts || 0) * factor / 12,
      profit: (summary.netProfit || 0) * factor / 12
    };
  });
}

export default PnlSummaryPage;

