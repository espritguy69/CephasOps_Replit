import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, RefreshCw, Filter } from 'lucide-react';
import { useOrders } from '../../hooks/useOrders';
import { useDepartment } from '../../contexts/DepartmentContext';
import { useToast } from '../../components/ui';
import { Button, Card, LoadingSpinner, EmptyState } from '../../components/ui';
import { PageShell } from '../../components/layout';

/**
 * PATTERN: List Page with Department Filtering
 * 
 * Key conventions:
 * - Use useDepartment() to get department context
 * - Wait for departmentLoading to be false before rendering data
 * - Use TanStack Query hooks for data fetching
 * - Handle loading, error, and empty states
 * - Provide filter controls
 */

interface OrdersListFilters {
  status?: string;
  partnerId?: string;
  fromDate?: string;
  toDate?: string;
}

export const OrdersListPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError } = useToast();
  
  // IMPORTANT: Get department context for filtering
  const { 
    departmentId, 
    activeDepartment, 
    loading: departmentLoading 
  } = useDepartment();
  
  const [filters, setFilters] = useState<OrdersListFilters>({});

  // PATTERN: Use custom hook that handles department injection
  const { 
    data: orders, 
    isLoading, 
    isError, 
    error,
    refetch 
  } = useOrders(filters);

  // Show error toast when query fails
  useEffect(() => {
    if (isError && error) {
      showError(error.message || 'Failed to load orders');
    }
  }, [isError, error, showError]);

  // PATTERN: Show loading while department context is loading
  // This prevents flash of unfiltered data
  if (departmentLoading) {
    return (
      <PageShell title="Orders">
        <LoadingSpinner message="Loading department..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Orders"
      subtitle={activeDepartment ? `Department: ${activeDepartment.name}` : undefined}
      actions={
        <>
          <Button variant="outline" onClick={() => refetch()}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
          <Button onClick={() => navigate('/orders/create')}>
            <Plus className="h-4 w-4 mr-2" />
            Create Order
          </Button>
        </>
      }
    >
      {/* Filters Section */}
      <Card className="mb-4 p-4">
        <div className="flex items-center gap-4">
          <Filter className="h-4 w-4 text-muted-foreground" />
          <select
            className="border rounded px-3 py-2 text-sm"
            value={filters.status || ''}
            onChange={(e) => setFilters({ ...filters, status: e.target.value || undefined })}
          >
            <option value="">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Assigned">Assigned</option>
            <option value="InProgress">In Progress</option>
            <option value="Completed">Completed</option>
          </select>
          {/* Add more filter controls as needed */}
        </div>
      </Card>

      {/* Content Section */}
      <Card>
        {/* Loading State */}
        {isLoading && (
          <div className="p-8">
            <LoadingSpinner message="Loading orders..." />
          </div>
        )}

        {/* Error State */}
        {isError && !isLoading && (
          <div className="p-8 text-center">
            <p className="text-red-500 mb-4">Failed to load orders</p>
            <Button variant="outline" onClick={() => refetch()}>
              Try Again
            </Button>
          </div>
        )}

        {/* Empty State */}
        {!isLoading && !isError && orders && orders.length === 0 && (
          <EmptyState
            title="No orders found"
            description="Try adjusting your filters or create a new order."
            action={
              <Button onClick={() => navigate('/orders/create')}>
                <Plus className="h-4 w-4 mr-2" />
                Create Order
              </Button>
            }
          />
        )}

        {/* Data Table */}
        {!isLoading && !isError && orders && orders.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-xs uppercase">
                <tr>
                  <th className="px-4 py-3 text-left">Service ID</th>
                  <th className="px-4 py-3 text-left">Customer</th>
                  <th className="px-4 py-3 text-left">Status</th>
                  <th className="px-4 py-3 text-left">Appointment</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr 
                    key={order.id}
                    className="border-b hover:bg-muted/40 cursor-pointer"
                    onClick={() => navigate(`/orders/${order.id}`)}
                  >
                    <td className="px-4 py-3 font-mono text-xs">
                      {order.serviceId || order.ticketId || '-'}
                    </td>
                    <td className="px-4 py-3">
                      {order.customerName || '-'}
                    </td>
                    <td className="px-4 py-3">
                      <span className="px-2 py-1 rounded text-xs bg-muted">
                        {order.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-xs">
                      {order.appointmentDate 
                        ? new Date(order.appointmentDate).toLocaleDateString()
                        : '-'}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Button 
                        variant="ghost" 
                        size="sm"
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(`/orders/${order.id}`);
                        }}
                      >
                        View
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </PageShell>
  );
};

export default OrdersListPage;
