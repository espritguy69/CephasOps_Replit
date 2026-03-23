import React, { useState } from 'react';
import { BarChart3, Package, Briefcase, TrendingUp, AlertCircle, CheckCircle, Clock, Users, RefreshCw, WifiOff } from 'lucide-react';
import { Card, Skeleton, EmptyState, Button, StatusBadge, getOrderStatusVariant } from '../../components/ui';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../../contexts/AuthContext';
import { getAllOrders } from '../../api/orders';
import { getAssignedJobs } from '../../api/si-app';
import { getStockLevels } from '../../api/inventory';
import { format } from 'date-fns';

export function DashboardPage() {
  const { user, isAdmin, serviceInstaller } = useAuth();
  const [dateRange, setDateRange] = useState<'today' | 'week' | 'month'>('week');

  // Get SI ID for non-admin users
  const siId = ((user as any)?.siId || serviceInstaller?.id || (serviceInstaller as any)?.Id) as string | undefined;

  // Fetch orders (all for admin, assigned for SI)
  const { data: orders, isLoading: isLoadingOrders, error: ordersError, refetch: refetchOrders } = useQuery({
    queryKey: ['dashboardOrders', isAdmin, siId, dateRange],
    queryFn: () => {
      if (isAdmin) {
        const now = new Date();
        let fromDate: Date;
        if (dateRange === 'today') {
          fromDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        } else if (dateRange === 'week') {
          fromDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        } else {
          fromDate = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
        }
        return getAllOrders({
          fromDate: fromDate.toISOString(),
          toDate: now.toISOString(),
        });
      } else if (siId) {
        return getAssignedJobs(null, siId, {});
      }
      return Promise.resolve([]);
    },
    enabled: !!user?.id,
  });

  // Fetch stock levels (admin only)
  const { data: stockLevels, isLoading: isLoadingStock } = useQuery({
    queryKey: ['dashboardStock'],
    queryFn: () => getStockLevels({ lowStockOnly: true }),
    enabled: !!user?.id && isAdmin,
  });

  // Calculate KPIs
  const totalOrders = orders?.length || 0;
  const pendingOrders = orders?.filter((o: any) => 
    ['Pending', 'Assigned', 'ReschedulePendingApproval'].includes(o.status)
  ).length || 0;
  const completedOrders = orders?.filter((o: any) => 
    ['Completed', 'OrderCompleted'].includes(o.status)
  ).length || 0;
  const inProgressOrders = orders?.filter((o: any) => 
    ['OnTheWay', 'MetCustomer', 'Installing', 'InProgress'].includes(o.status)
  ).length || 0;
  const cancelledOrders = orders?.filter((o: any) => 
    ['Cancelled', 'OrderCancelled'].includes(o.status)
  ).length || 0;
  const blockerOrders = orders?.filter((o: any) => 
    ['Blocker', 'Rejected'].includes(o.status) || (o.status || '').includes('Blocker')
  ).length || 0;

  const lowStockCount = stockLevels?.filter((s: any) => 
    s.availableQuantity !== undefined && s.availableQuantity < 10
  ).length || 0;

  if (isLoadingOrders) {
    return (
      <div className="p-4 space-y-4">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-40" />
          {isAdmin && (
            <div className="flex gap-2">
              <Skeleton className="h-8 w-16 rounded-md" />
              <Skeleton className="h-8 w-14 rounded-md" />
              <Skeleton className="h-8 w-16 rounded-md" />
            </div>
          )}
        </div>
        <div className="grid grid-cols-2 gap-4">
          {[1, 2, 3, 4].map((i) => (
            <Card key={i} className="p-4">
              <div className="flex items-center justify-between">
                <div className="space-y-1">
                  <Skeleton className="h-4 w-20" />
                  <Skeleton className="h-8 w-12" />
                </div>
                <Skeleton className="h-8 w-8 rounded" />
              </div>
            </Card>
          ))}
        </div>
        <Card className="p-4">
          <Skeleton className="h-6 w-32 mb-4" />
          <div className="space-y-2">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex justify-between items-start border-b border-border pb-2">
                <div className="space-y-1 flex-1">
                  <Skeleton className="h-4 w-full" />
                  <Skeleton className="h-3 w-3/4" />
                </div>
                <Skeleton className="h-5 w-14 rounded-full" />
              </div>
            ))}
          </div>
        </Card>
      </div>
    );
  }

  if (ordersError) {
    return (
      <div className="p-4 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-foreground flex items-center gap-2">
            <BarChart3 className="h-6 w-6" />
            Dashboard
          </h2>
        </div>
        <Card className="p-6">
          <EmptyState
            title="Unable to load dashboard"
            description={(ordersError as Error).message || 'Something went wrong. Please check your connection and try again.'}
            icon={<WifiOff className="h-12 w-12 text-muted-foreground" />}
            action={
              <Button variant="outline" onClick={() => refetchOrders()} className="min-h-[44px]">
                <RefreshCw className="h-4 w-4 mr-2" />
                Retry
              </Button>
            }
          />
        </Card>
      </div>
    );
  }

  return (
    <div className="p-4 space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-foreground flex items-center gap-2">
          <BarChart3 className="h-6 w-6" />
          Dashboard
        </h2>
        {isAdmin && (
          <div className="flex gap-2">
            <button
              onClick={() => setDateRange('today')}
              className={`px-3 py-1 text-sm rounded-md ${
                dateRange === 'today'
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-muted-foreground'
              }`}
            >
              Today
            </button>
            <button
              onClick={() => setDateRange('week')}
              className={`px-3 py-1 text-sm rounded-md ${
                dateRange === 'week'
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-muted-foreground'
              }`}
            >
              Week
            </button>
            <button
              onClick={() => setDateRange('month')}
              className={`px-3 py-1 text-sm rounded-md ${
                dateRange === 'month'
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-muted-foreground'
              }`}
            >
              Month
            </button>
          </div>
        )}
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 gap-4">
        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Total {isAdmin ? 'Orders' : 'Jobs'}</p>
              <p className="text-2xl font-bold">{totalOrders}</p>
            </div>
            <Briefcase className="h-8 w-8 text-primary" />
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Pending</p>
              <p className="text-2xl font-bold text-yellow-600">{pendingOrders}</p>
            </div>
            <Clock className="h-8 w-8 text-yellow-600" />
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">In Progress</p>
              <p className="text-2xl font-bold text-blue-600">{inProgressOrders}</p>
            </div>
            <TrendingUp className="h-8 w-8 text-blue-600" />
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Completed</p>
              <p className="text-2xl font-bold text-green-600">{completedOrders}</p>
            </div>
            <CheckCircle className="h-8 w-8 text-green-600" />
          </div>
        </Card>

        {(blockerOrders > 0 || cancelledOrders > 0) && (
          <Card className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">
                  {blockerOrders > 0 && cancelledOrders > 0 
                    ? 'Blocked / Cancelled' 
                    : blockerOrders > 0 ? 'Blocked' : 'Cancelled'}
                </p>
                <p className="text-2xl font-bold text-red-600">{blockerOrders + cancelledOrders}</p>
              </div>
              <AlertCircle className="h-8 w-8 text-red-600" />
            </div>
          </Card>
        )}

        {isAdmin && (
          <>
            <Card className="p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Low Stock Items</p>
                  <p className="text-2xl font-bold text-red-600">{lowStockCount}</p>
                </div>
                <AlertCircle className="h-8 w-8 text-red-600" />
              </div>
            </Card>

            <Card className="p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Total Materials</p>
                  <p className="text-2xl font-bold">{stockLevels?.length || 0}</p>
                </div>
                <Package className="h-8 w-8 text-primary" />
              </div>
            </Card>
          </>
        )}
      </div>

      {/* Recent Orders/Jobs */}
      <Card className="p-4">
        <h3 className="font-semibold text-lg mb-4">
          Recent {isAdmin ? 'Orders' : 'Jobs'}
        </h3>
        {!orders || orders.length === 0 ? (
          <EmptyState
            title={`No ${isAdmin ? 'orders' : 'jobs'} found`}
            description={`No ${isAdmin ? 'orders' : 'jobs'} in the selected period`}
          />
        ) : (
          <div className="space-y-2">
            {orders.slice(0, 5).map((order: any) => (
              <div
                key={order.id}
                className="border-b border-border pb-2 last:border-b-0 last:pb-0"
              >
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <p className="font-medium">{order.customerName || order.orderNumber}</p>
                    <p className="text-sm text-muted-foreground">
                      {order.addressLine1}, {order.city}
                    </p>
                    {order.appointmentDate && (
                      <p className="text-xs text-muted-foreground mt-1">
                        {format(new Date(order.appointmentDate), 'MMM dd, yyyy')}
                      </p>
                    )}
                  </div>
                  <StatusBadge variant={getOrderStatusVariant(order.status)} size="sm">
                    {order.status}
                  </StatusBadge>
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>

      {/* Low Stock Alert (Admin Only) */}
      {isAdmin && lowStockCount > 0 && (
        <Card className="p-4 bg-yellow-50 border-yellow-200">
          <div className="flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-yellow-600" />
            <div>
              <p className="font-semibold text-yellow-900">Low Stock Alert</p>
              <p className="text-sm text-yellow-700">
                {lowStockCount} material{lowStockCount !== 1 ? 's' : ''} have low stock levels
              </p>
            </div>
          </div>
        </Card>
      )}
    </div>
  );
}
