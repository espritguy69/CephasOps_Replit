import React from 'react';
import { Link } from 'react-router-dom';
import { MapPin, Calendar, User, Building } from 'lucide-react';
import OrderStatusBadge from './OrderStatusBadge';
import { Card } from '../ui';
import { cn } from '../../lib/utils';
import type { Order } from '../../types/orders';

interface OrderCardProps {
  order: Order;
}

const OrderCard: React.FC<OrderCardProps> = ({ order }) => {
  
  return (
    <Link to={`/orders/${order.id}`} className="block">
      <Card className="p-4 hover:shadow-md transition-shadow cursor-pointer h-full">
        <div className="flex justify-between items-start mb-4">
          <div className="flex flex-col gap-1">
            <span className="font-semibold text-lg">{order.serviceId || order.uniqueId}</span>
            {order.partnerOrderId && (
              <span className="text-sm text-muted-foreground">{order.partnerOrderId}</span>
            )}
          </div>
          <OrderStatusBadge status={order.status} />
        </div>

        <div className="space-y-2 mb-4">
          <div className="flex gap-4 text-sm">
            <span className="text-muted-foreground font-medium min-w-[80px] flex items-center gap-1">
              <User className="h-3 w-3" />
              Customer:
            </span>
            <span>{order.customerName || 'N/A'}</span>
          </div>
          <div className="flex gap-4 text-sm">
            <span className="text-muted-foreground font-medium min-w-[80px]">Type:</span>
            <span>{order.orderType || order.partnerOrderType || 'N/A'}</span>
          </div>
          <div className="flex gap-4 text-sm">
            <span className="text-muted-foreground font-medium min-w-[80px] flex items-center gap-1">
              <Building className="h-3 w-3" />
              Partner:
            </span>
            <span>{order.partnerName || order.partnerGroup || 'N/A'}</span>
          </div>
          {order.appointmentDate && (
            <div className="flex gap-4 text-sm">
              <span className="text-muted-foreground font-medium min-w-[80px] flex items-center gap-1">
                <Calendar className="h-3 w-3" />
                Appointment:
              </span>
              <span>
                {new Date(order.appointmentDate).toLocaleDateString()} {order.appointmentTime || ''}
              </span>
            </div>
          )}
          {order.address && (
            <div className="flex gap-4 text-sm">
              <span className="text-muted-foreground font-medium min-w-[80px] flex items-center gap-1">
                <MapPin className="h-3 w-3" />
                Address:
              </span>
              <span className="line-clamp-2">{order.address}</span>
            </div>
          )}
        </div>

        {order.assignedTo && (
          <div className="pt-4 border-t text-sm text-muted-foreground">
            Assigned to: {order.assignedToName || order.assignedTo}
          </div>
        )}
      </Card>
    </Link>
  );
};

export default OrderCard;

