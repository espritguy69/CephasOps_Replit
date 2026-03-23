import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { FileX, Clock } from 'lucide-react';
import { getOrders } from '../../api/orders';
import { Card } from '../ui';
import type { Order } from '../../types/orders';

const PendingDocketsWidget: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState<boolean>(true);
  const [pendingOrders, setPendingOrders] = useState<Order[]>([]);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      // Get completed orders without dockets
      const orders = await getOrders({
        status: 'OrderCompleted'
      });

      // Filter orders that are completed but don't have dockets received
      const pending = orders.filter((order) => {
        return order.orderCompletedAt && !order.docketsReceivedAt;
      }).slice(0, 5); // Show top 5

      setPendingOrders(pending);
    } catch (err) {
      console.error('Failed to load pending dockets:', err);
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

  const formatDate = (dateString: string | undefined): string => {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  return (
    <Card className="p-3">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <FileX className="h-4 w-4 text-orange-500" />
          <h3 className="text-xs font-semibold text-foreground">Pending Dockets</h3>
        </div>
        <button
          onClick={() => navigate('/orders?status=OrderCompleted')}
          className="text-xs text-primary hover:underline"
        >
          View All
        </button>
      </div>

      <div className="space-y-1.5">
        {pendingOrders.length > 0 ? (
          pendingOrders.map((order) => (
            <div
              key={order.id}
              className="flex items-center justify-between text-xs p-1.5 rounded hover:bg-muted cursor-pointer"
              onClick={() => navigate(`/orders/${order.id}`)}
            >
              <div className="flex-1 min-w-0">
                <div className="font-medium text-foreground truncate">
                  {order.serviceId || order.customerName || 'N/A'}
                </div>
                <div className="flex items-center gap-1 text-muted-foreground">
                  <Clock className="h-3 w-3" />
                  <span>Completed: {formatDate(order.orderCompletedAt)}</span>
                </div>
              </div>
            </div>
          ))
        ) : (
          <div className="text-xs text-muted-foreground text-center py-2">
            No pending dockets
          </div>
        )}
      </div>
    </Card>
  );
};

export default PendingDocketsWidget;

