import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { FileText, Clock, CheckCircle, UserCheck, AlertCircle } from 'lucide-react';
import { getOrders } from '../../api/orders';
import { Card } from '../ui';
import type { Order } from '../../types/orders';

interface OrdersTodayStats {
  total: number;
  pending: number;
  assigned: number;
  onTheWay: number;
  completed: number;
}

const TotalOrdersTodayWidget: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState<boolean>(true);
  const [stats, setStats] = useState<OrdersTodayStats>({
    total: 0,
    pending: 0,
    assigned: 0,
    onTheWay: 0,
    completed: 0
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const tomorrow = new Date(today);
      tomorrow.setDate(tomorrow.getDate() + 1);

      const orders = await getOrders({
        fromDate: today.toISOString(),
        toDate: tomorrow.toISOString()
      });

      const statsData: OrdersTodayStats = {
        total: orders.length || 0,
        pending: 0,
        assigned: 0,
        onTheWay: 0,
        completed: 0
      };

      orders.forEach((order) => {
        const status = order.status?.toLowerCase() || '';
        if (status === 'pending') statsData.pending++;
        else if (status === 'assigned') statsData.assigned++;
        else if (status === 'ontheway' || status === 'on the way') statsData.onTheWay++;
        else if (status === 'ordercompleted' || status === 'completed') statsData.completed++;
      });

      setStats(statsData);
    } catch (err) {
      console.error('Failed to load orders today:', err);
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

  return (
    <Card className="p-3">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <FileText className="h-4 w-4 text-primary" />
          <h3 className="text-xs font-semibold text-foreground">Total Orders Today</h3>
        </div>
        <button
          onClick={() => navigate('/orders')}
          className="text-xs text-primary hover:underline"
        >
          View All
        </button>
      </div>

      <div className="text-2xl font-bold text-foreground mb-2">
        {stats.total}
      </div>

      <div className="grid grid-cols-2 gap-2 mt-2">
        <div className="flex items-center gap-1.5">
          <Clock className="h-3 w-3 text-muted-foreground" />
          <span className="text-xs text-muted-foreground">Pending:</span>
          <span className="text-xs font-semibold text-foreground">{stats.pending}</span>
        </div>
        <div className="flex items-center gap-1.5">
          <UserCheck className="h-3 w-3 text-blue-500" />
          <span className="text-xs text-muted-foreground">Assigned:</span>
          <span className="text-xs font-semibold text-foreground">{stats.assigned}</span>
        </div>
        <div className="flex items-center gap-1.5">
          <AlertCircle className="h-3 w-3 text-yellow-500" />
          <span className="text-xs text-muted-foreground">On The Way:</span>
          <span className="text-xs font-semibold text-foreground">{stats.onTheWay}</span>
        </div>
        <div className="flex items-center gap-1.5">
          <CheckCircle className="h-3 w-3 text-green-500" />
          <span className="text-xs text-muted-foreground">Completed:</span>
          <span className="text-xs font-semibold text-foreground">{stats.completed}</span>
        </div>
      </div>
    </Card>
  );
};

export default TotalOrdersTodayWidget;

