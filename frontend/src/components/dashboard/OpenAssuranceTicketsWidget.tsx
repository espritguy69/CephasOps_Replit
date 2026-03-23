import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertTriangle } from 'lucide-react';
import { getOrders } from '../../api/orders';
import { Card } from '../ui';

const OpenAssuranceTicketsWidget: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState<boolean>(true);
  const [count, setCount] = useState<number>(0);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const orders = await getOrders({
        orderType: 'Assurance',
        status: 'Pending,Assigned,OnTheWay,MetCustomer'
      });

      setCount(orders.length || 0);
    } catch (err) {
      console.error('Failed to load assurance tickets:', err);
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
          <AlertTriangle className="h-4 w-4 text-orange-500" />
          <h3 className="text-xs font-semibold text-foreground">Open Assurance Tickets</h3>
        </div>
        <button
          onClick={() => navigate('/orders?orderType=Assurance')}
          className="text-xs text-primary hover:underline"
        >
          View All
        </button>
      </div>

      <div className="text-2xl font-bold text-foreground">
        {count}
      </div>

      <div className="text-xs text-muted-foreground mt-1">
        Active assurance requests
      </div>
    </Card>
  );
};

export default OpenAssuranceTicketsWidget;

