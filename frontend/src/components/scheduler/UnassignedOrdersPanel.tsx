import React from 'react';
import { X, GripVertical } from 'lucide-react';
import { useDraggable } from '@dnd-kit/core';
import { Card, Button, EmptyState } from '../ui';
import type { UnassignedOrderFilters } from '../../types/scheduler';
import type { Order } from '../../types/orders';

interface UnassignedOrdersPanelProps {
  orders: Order[];
  filters?: UnassignedOrderFilters;
  onClose?: () => void;
  onOrderClick?: (order: Order) => void;
  className?: string;
}

interface DraggableOrderCardProps {
  order: Order;
  onClick?: () => void;
}

/**
 * Draggable order card for unassigned orders panel
 */
const DraggableOrderCard: React.FC<DraggableOrderCardProps> = ({ order, onClick }) => {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: `unassigned-${order.id}`,
    data: {
      type: 'unassigned-order',
      order
    }
  });

  const style = transform
    ? {
        transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
        opacity: isDragging ? 0.5 : 1
      }
    : { opacity: isDragging ? 0.5 : 1 };

  return (
    <div ref={setNodeRef} style={style}>
      <Card
        className="p-2 cursor-move hover:shadow-md transition-shadow text-xs"
        onClick={onClick}
      >
        <div className="flex items-start gap-2">
          <div
            {...attributes}
            {...listeners}
            className="cursor-grab active:cursor-grabbing text-muted-foreground hover:text-foreground mt-0.5"
          >
            <GripVertical className="h-4 w-4" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="font-medium text-xs truncate">{order.serviceId || order.uniqueId || 'N/A'}</div>
            {order.ticketId && (
              <div className="text-muted-foreground text-xs truncate">{order.ticketId}</div>
            )}
            <div className="text-muted-foreground text-xs mt-1 truncate">{order.customerName || 'N/A'}</div>
            <div className="text-muted-foreground text-xs truncate">
              {order.buildingName || order.city || 'N/A'}
            </div>
            <div className="text-muted-foreground text-xs truncate">{order.derivedPartnerCategoryLabel || order.partnerName || 'N/A'}</div>
            {order.appointmentDate && (
              <div className="text-muted-foreground text-xs mt-1">
                {new Date(order.appointmentDate).toLocaleDateString()}
              </div>
            )}
          </div>
        </div>
      </Card>
    </div>
  );
};

/**
 * UnassignedOrdersPanel component
 * Shows pending/unassigned orders that can be dragged to schedule
 */
const UnassignedOrdersPanel: React.FC<UnassignedOrdersPanelProps> = ({
  orders,
  filters,
  onClose,
  onOrderClick,
  className
}) => {
  return (
    <div className={`w-64 border-r bg-gray-50 overflow-y-auto flex flex-col ${className || ''}`}>
      <div className="p-2 border-b bg-white flex items-center justify-between sticky top-0 z-10">
        <h2 className="text-sm font-semibold">Unassigned Orders</h2>
        {onClose && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onClose}
            className="h-6 w-6 p-0"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>
      <div className="p-2 space-y-2 flex-1 overflow-y-auto">
        {orders.length === 0 ? (
          <EmptyState
            title="No unassigned orders"
            description="All orders are scheduled"
            className="py-8"
          />
        ) : (
          orders.map(order => (
            <DraggableOrderCard
              key={order.id}
              order={order}
              onClick={() => onOrderClick?.(order)}
            />
          ))
        )}
      </div>
      {orders.length > 0 && (
        <div className="p-2 border-t bg-white text-xs text-muted-foreground text-center">
          {orders.length} unassigned {orders.length === 1 ? 'order' : 'orders'}
        </div>
      )}
    </div>
  );
};

export default UnassignedOrdersPanel;

