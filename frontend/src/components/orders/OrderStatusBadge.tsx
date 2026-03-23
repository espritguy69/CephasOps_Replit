import React from 'react';
import { getStatusColor } from '../../constants/orders';
import { StatusBadge } from '../ui';

interface OrderStatusBadgeProps {
  status: string;
}

const OrderStatusBadge: React.FC<OrderStatusBadgeProps> = ({ status }) => {
  const color = getStatusColor(status);
  const displayStatus = status || 'Unknown';

  const variantMap: Record<string, 'secondary' | 'info' | 'warning' | 'success' | 'error' | 'default'> = {
    gray: 'secondary',
    blue: 'info',
    yellow: 'warning',
    orange: 'warning',
    purple: 'info',
    green: 'success',
    red: 'error',
    amber: 'warning'
  };

  return (
    <StatusBadge 
      status={displayStatus}
      variant={variantMap[color] || 'default'}
      size="sm"
    />
  );
};

export default OrderStatusBadge;

