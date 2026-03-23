import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { TrendingUp, DollarSign } from 'lucide-react';
import { getInvoices } from '../../api/billing';
import { Card } from '../ui';
import type { Invoice } from '../../types/billing';

interface MonthlyRevenueData {
  name: string;
  invoiced: number;
  paid: number;
}

const MonthlyRevenueTrendChart: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState<boolean>(true);
  const [data, setData] = useState<MonthlyRevenueData[]>([]);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      // Get invoices from last 6 months
      const sixMonthsAgo = new Date();
      sixMonthsAgo.setMonth(sixMonthsAgo.getMonth() - 6);

      const invoices = await getInvoices({
        fromDate: sixMonthsAgo.toISOString()
      });

      // Group by month
      const monthMap: Record<string, MonthlyRevenueData> = {};
      invoices.forEach((invoice) => {
        if (invoice.invoiceDate) {
          const date = new Date(invoice.invoiceDate);
          const monthKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
          const monthName = date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
          
          if (!monthMap[monthKey]) {
            monthMap[monthKey] = { name: monthName, invoiced: 0, paid: 0 };
          }
          
          monthMap[monthKey].invoiced += invoice.totalAmount || 0;
          if (invoice.status === 'Paid') {
            monthMap[monthKey].paid += invoice.totalAmount || 0;
          }
        }
      });

      const chartData = Object.values(monthMap)
        .sort((a, b) => {
          const dateA = new Date(a.name).getTime();
          const dateB = new Date(b.name).getTime();
          return dateA - dateB;
        })
        .slice(-6); // Last 6 months

      setData(chartData);
    } catch (err) {
      console.error('Failed to load revenue trend:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <Card className="p-3">
        <div className="animate-pulse text-xs text-muted-foreground">Loading...</div>
      </Card>
    );
  }

  const maxAmount = data.length > 0 
    ? Math.max(...data.map((d) => Math.max(d.invoiced, d.paid))) 
    : 1;

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-MY', {
      style: 'currency',
      currency: 'MYR',
      minimumFractionDigits: 0
    }).format(amount);
  };

  return (
    <Card className="p-3">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <TrendingUp className="h-4 w-4 text-primary" />
          <h3 className="text-xs font-semibold text-foreground">Monthly Revenue Trend</h3>
        </div>
        <button
          onClick={() => navigate('/billing/invoices')}
          className="text-xs text-primary hover:underline"
        >
          View All
        </button>
      </div>

      <div className="space-y-2 mt-3">
        {data.length > 0 ? (
          data.map((item, index) => (
            <div key={index} className="space-y-1">
              <div className="flex items-center justify-between text-xs">
                <span className="text-foreground font-medium">{item.name}</span>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <span className="text-xs">Inv: {formatCurrency(item.invoiced)}</span>
                  <span className="text-xs">Paid: {formatCurrency(item.paid)}</span>
                </div>
              </div>
              <div className="flex gap-1">
                <div className="flex-1 space-y-0.5">
                  <div className="text-xs text-muted-foreground">Invoiced</div>
                  <div className="w-full bg-muted rounded-full h-2">
                    <div
                      className="bg-primary h-2 rounded-full transition-all"
                      style={{ width: `${(item.invoiced / maxAmount) * 100}%` }}
                    />
                  </div>
                </div>
                <div className="flex-1 space-y-0.5">
                  <div className="text-xs text-muted-foreground">Paid</div>
                  <div className="w-full bg-muted rounded-full h-2">
                    <div
                      className="bg-accent h-2 rounded-full transition-all"
                      style={{ width: `${(item.paid / maxAmount) * 100}%` }}
                    />
                  </div>
                </div>
              </div>
            </div>
          ))
        ) : (
          <div className="text-xs text-muted-foreground text-center py-4">
            No revenue data available
          </div>
        )}
      </div>
    </Card>
  );
};

export default MonthlyRevenueTrendChart;

