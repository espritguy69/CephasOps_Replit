import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Calendar, Clock } from 'lucide-react';
import { getOrders } from '../../api/orders';
import { Card } from '../ui';
import type { Order } from '../../types/orders';

const UpcomingAppointmentsWidget: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState<boolean>(true);
  const [appointments, setAppointments] = useState<Order[]>([]);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const dayAfterTomorrow = new Date(today);
      dayAfterTomorrow.setDate(dayAfterTomorrow.getDate() + 2);

      const orders = await getOrders({
        fromDate: today.toISOString(),
        toDate: dayAfterTomorrow.toISOString(),
        status: 'Pending,Assigned'
      });

      // Sort by appointment date
      const sorted = (orders || []).sort((a, b) => {
        const dateA = a.appointmentDate ? new Date(a.appointmentDate).getTime() : 0;
        const dateB = b.appointmentDate ? new Date(b.appointmentDate).getTime() : 0;
        return dateA - dateB;
      });

      setAppointments(sorted.slice(0, 5)); // Show top 5
    } catch (err) {
      console.error('Failed to load upcoming appointments:', err);
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

  const formatDateTime = (dateString: string | undefined): string => {
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
          <Calendar className="h-4 w-4 text-primary" />
          <h3 className="text-xs font-semibold text-foreground">Upcoming Appointments</h3>
        </div>
        <button
          onClick={() => navigate('/scheduler')}
          className="text-xs text-primary hover:underline"
        >
          View Calendar
        </button>
      </div>

      <div className="space-y-1.5">
        {appointments.length > 0 ? (
          appointments.map((order) => (
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
                  <span>{formatDateTime(order.appointmentDate)}</span>
                </div>
              </div>
            </div>
          ))
        ) : (
          <div className="text-xs text-muted-foreground text-center py-2">
            No upcoming appointments
          </div>
        )}
      </div>
    </Card>
  );
};

export default UpcomingAppointmentsWidget;

