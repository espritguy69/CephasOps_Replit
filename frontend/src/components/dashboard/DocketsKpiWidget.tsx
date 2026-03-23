import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { FileCheck, Clock } from 'lucide-react';
import { getOrders } from '../../api/orders';
import { Card } from '../ui';
import type { Order } from '../../types/orders';

interface DocketsKpi {
  percentage: number;
  total: number;
  withinThreshold: number;
  thresholdMinutes: number;
}

const DocketsKpiWidget: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState<boolean>(true);
  const [kpi, setKpi] = useState<DocketsKpi>({
    percentage: 0,
    total: 0,
    withinThreshold: 0,
    thresholdMinutes: 30 // Default KPI threshold
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      // Get orders completed in the last 30 days
      const thirtyDaysAgo = new Date();
      thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);

      const orders = await getOrders({
        fromDate: thirtyDaysAgo.toISOString(),
        status: 'OrderCompleted'
      });

      let withinThreshold = 0;
      const thresholdMs = kpi.thresholdMinutes * 60 * 1000;

      orders.forEach((order) => {
        if (order.orderCompletedAt && order.docketsReceivedAt) {
          const completedTime = new Date(order.orderCompletedAt).getTime();
          const receivedTime = new Date(order.docketsReceivedAt).getTime();
          const diff = receivedTime - completedTime;
          
          if (diff >= 0 && diff <= thresholdMs) {
            withinThreshold++;
          }
        }
      });

      const total = orders.length || 0;
      const percentage = total > 0 ? Math.round((withinThreshold / total) * 100) : 0;

      setKpi({
        percentage,
        total,
        withinThreshold,
        thresholdMinutes: kpi.thresholdMinutes
      });
    } catch (err) {
      console.error('Failed to load dockets KPI:', err);
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

  const isGood = kpi.percentage >= 80;
  const isWarning = kpi.percentage >= 60 && kpi.percentage < 80;

  return (
    <Card className="p-3">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <FileCheck className="h-4 w-4 text-primary" />
          <h3 className="text-xs font-semibold text-foreground">Dockets KPI</h3>
        </div>
        <button
          onClick={() => navigate('/orders')}
          className="text-xs text-primary hover:underline"
        >
          View Details
        </button>
      </div>

      <div className="flex items-baseline gap-2 mb-2">
        <span className={`text-2xl font-bold ${
          isGood ? 'text-green-600' : isWarning ? 'text-yellow-600' : 'text-red-600'
        }`}>
          {kpi.percentage}%
        </span>
        <span className="text-xs text-muted-foreground">
          within {kpi.thresholdMinutes} min
        </span>
      </div>

      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <Clock className="h-3 w-3" />
        <span>{kpi.withinThreshold} of {kpi.total} orders (last 30 days)</span>
      </div>
    </Card>
  );
};

export default DocketsKpiWidget;

