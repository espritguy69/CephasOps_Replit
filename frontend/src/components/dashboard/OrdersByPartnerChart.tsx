import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { BarChart3, TrendingUp } from 'lucide-react';
import { getOrders } from '../../api/orders';
import { Card } from '../ui';
import type { Order } from '../../types/orders';

interface PartnerOrderData {
  name: string;
  count: number;
}

const OrdersByPartnerChart: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState<boolean>(true);
  const [data, setData] = useState<PartnerOrderData[]>([]);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      // Get orders from last 30 days
      const thirtyDaysAgo = new Date();
      thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);

      const orders = await getOrders({
        fromDate: thirtyDaysAgo.toISOString()
      });

      // Group by partner
      const partnerMap: Record<string, number> = {};
      orders.forEach((order) => {
        const partnerName = order.partnerName || order.partner?.name || 'Unknown';
        partnerMap[partnerName] = (partnerMap[partnerName] || 0) + 1;
      });

      const chartData = Object.entries(partnerMap)
        .map(([name, count]) => ({ name, count }))
        .sort((a, b) => b.count - a.count)
        .slice(0, 5); // Top 5 partners

      setData(chartData);
    } catch (err) {
      console.error('Failed to load orders by partner:', err);
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

  const maxCount = data.length > 0 ? Math.max(...data.map((d) => d.count)) : 1;

  return (
    <Card className="p-3">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <BarChart3 className="h-4 w-4 text-primary" />
          <h3 className="text-xs font-semibold text-foreground">Orders by Partner</h3>
        </div>
        <button
          onClick={() => navigate('/orders')}
          className="text-xs text-primary hover:underline"
        >
          View All
        </button>
      </div>

      <div className="space-y-2 mt-3">
        {data.length > 0 ? (
          data.map((item) => (
            <div key={item.name} className="space-y-1">
              <div className="flex items-center justify-between text-xs">
                <span className="text-foreground font-medium truncate flex-1">{item.name}</span>
                <span className="text-muted-foreground ml-2">{item.count}</span>
              </div>
              <div className="w-full bg-muted rounded-full h-2">
                <div
                  className="bg-primary h-2 rounded-full transition-all"
                  style={{ width: `${(item.count / maxCount) * 100}%` }}
                />
              </div>
            </div>
          ))
        ) : (
          <div className="text-xs text-muted-foreground text-center py-4">
            No orders data available
          </div>
        )}
      </div>
    </Card>
  );
};

export default OrdersByPartnerChart;

