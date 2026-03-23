import React, { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import type { LucideIcon } from 'lucide-react';
import { 
  FileText, Clock, Users, AlertTriangle, TrendingUp, TrendingDown,
  ArrowRight, Filter, CalendarDays, RefreshCw,
  Eye, Edit, ChevronDown
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '../components/ui';
import { PageShell } from '../components/layout';
import { getOrders } from '../api/orders';
import type { Order } from '../types/orders';
import { OrdersTrendChart, OrdersByPartnerChart } from '../components/charts';

interface StatCardProps {
  title: string;
  value: number | string;
  change?: string;
  changeLabel?: string;
  icon: LucideIcon;
  iconBg?: string;
  trend?: 'up' | 'down';
  loading?: boolean;
}

// Stat Card Component
const StatCard: React.FC<StatCardProps> = ({ 
  title, 
  value, 
  change, 
  changeLabel,
  icon: Icon, 
  iconBg,
  trend,
  loading 
}) => {
  return (
    <div className="bg-card rounded-lg border border-border p-3 md:p-4 lg:p-6 shadow-sm hover-lift transition-smooth">
      <div className="flex items-start justify-between gap-3">
        <div className="flex-1 min-w-0">
          <p className="text-xs md:text-sm font-medium text-muted-foreground mb-2">{title}</p>
          <div className="flex items-baseline gap-2">
            {loading ? (
              <div className="h-6 md:h-8 w-16 md:w-20 bg-muted animate-pulse rounded" />
            ) : (
              <span className="text-xl md:text-2xl lg:text-3xl font-bold text-foreground tracking-tight">{value}</span>
            )}
          </div>
          {change !== undefined && (
            <div className="mt-2 md:mt-3 flex items-center gap-1.5 flex-wrap">
              {trend === 'up' ? (
                <TrendingUp className="h-3.5 w-3.5 md:h-4 md:w-4 text-emerald-500 flex-shrink-0" />
              ) : trend === 'down' ? (
                <TrendingDown className="h-3.5 w-3.5 md:h-4 md:w-4 text-red-500 flex-shrink-0" />
              ) : null}
              <span className={cn(
                "text-xs md:text-sm font-medium",
                trend === 'up' && "text-emerald-600",
                trend === 'down' && "text-red-600",
                !trend && "text-muted-foreground"
              )}>
                {change}
              </span>
              {changeLabel && (
                <span className="text-xs text-muted-foreground">{changeLabel}</span>
              )}
            </div>
          )}
        </div>
        <div className={cn(
          "h-10 w-10 md:h-12 md:w-12 rounded-xl flex items-center justify-center flex-shrink-0",
          iconBg || "bg-primary/10"
        )}>
          <Icon className={cn(
            "h-5 w-5 md:h-6 md:w-6",
            iconBg?.includes('emerald') ? "text-emerald-600" :
            iconBg?.includes('amber') ? "text-amber-600" :
            iconBg?.includes('red') ? "text-red-600" :
            iconBg?.includes('blue') ? "text-blue-600" :
            "text-primary"
          )} />
        </div>
      </div>
    </div>
  );
};

interface OrderStatusBadgeProps {
  status: string;
}

// Order Status Badge
const OrderStatusBadge: React.FC<OrderStatusBadgeProps> = ({ status }) => {
  const statusConfig: Record<string, { bg: string; text: string }> = {
    'New': { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-700 dark:text-blue-400' },
    'Pending': { bg: 'bg-amber-100 dark:bg-amber-900/30', text: 'text-amber-700 dark:text-amber-400' },
    'InProgress': { bg: 'bg-purple-100 dark:bg-purple-900/30', text: 'text-purple-700 dark:text-purple-400' },
    'Completed': { bg: 'bg-emerald-100 dark:bg-emerald-900/30', text: 'text-emerald-700 dark:text-emerald-400' },
    'Cancelled': { bg: 'bg-red-100 dark:bg-red-900/30', text: 'text-red-700 dark:text-red-400' },
    'OnHold': { bg: 'bg-slate-100 dark:bg-slate-800', text: 'text-slate-700 dark:text-slate-400' }
  };

  const config = statusConfig[status] || statusConfig['Pending'];

  return (
    <span className={cn(
      "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium",
      config.bg,
      config.text
    )}>
      {status?.replace(/([A-Z])/g, ' $1').trim() || 'Unknown'}
    </span>
  );
};

interface DateRangeFilterProps {
  value: string;
  onChange: (value: string) => void;
}

// Date Filter Component
const DateRangeFilter: React.FC<DateRangeFilterProps> = ({ value, onChange }) => {
  const options = [
    { value: 'today', label: 'Today' },
    { value: 'yesterday', label: 'Yesterday' },
    { value: 'last7days', label: 'Last 7 days' },
    { value: 'last30days', label: 'Last 30 days' },
    { value: 'thisMonth', label: 'This month' },
    { value: 'lastMonth', label: 'Last month' },
    { value: 'custom', label: 'Custom range' }
  ];

  return (
    <div className="relative">
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="h-9 pl-9 pr-8 text-sm bg-background border border-input rounded-lg appearance-none cursor-pointer focus:outline-none focus:ring-2 focus:ring-ring"
      >
        {options.map(opt => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>
      <CalendarDays className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
      <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
    </div>
  );
};

interface StatusFilterProps {
  value: string;
  onChange: (value: string) => void;
}

// Status Filter Component  
const StatusFilter: React.FC<StatusFilterProps> = ({ value, onChange }) => {
  const options = [
    { value: '', label: 'All Statuses' },
    { value: 'New', label: 'New' },
    { value: 'Pending', label: 'Pending' },
    { value: 'InProgress', label: 'In Progress' },
    { value: 'Completed', label: 'Completed' },
    { value: 'OnHold', label: 'On Hold' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];

  return (
    <div className="relative">
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="h-9 pl-9 pr-8 text-sm bg-background border border-input rounded-lg appearance-none cursor-pointer focus:outline-none focus:ring-2 focus:ring-ring"
      >
        {options.map(opt => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>
      <Filter className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
      <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
    </div>
  );
};

interface DashboardStats {
  todaysOrders: number;
  activeInstallers: number;
  pendingNWO: number;
  overdueCWO: number;
}

// Sample orders data for demo
const SAMPLE_ORDERS: Partial<Order>[] = [
  { id: '1', orderNumber: 'NWO-2024-0001', customerName: 'Ahmad bin Hassan', addressLine1: '12 Jalan Ampang, KL', status: 'InProgress', assignedSiName: 'Mohd Rizal', appointmentDate: '2024-01-15', orderType: 'New Installation' },
  { id: '2', orderNumber: 'CWO-2024-0042', customerName: 'Sarah Tan Wei Ling', addressLine1: '45 Persiaran KLCC', status: 'Pending', assignedSiName: 'Kumar Rajan', appointmentDate: '2024-01-15', orderType: 'Change Order' },
  { id: '3', orderNumber: 'NWO-2024-0003', customerName: 'Lee Chong Wei', addressLine1: '88 Bukit Bintang', status: 'New', assignedSiName: 'Unassigned', appointmentDate: '2024-01-16', orderType: 'New Installation' },
  { id: '4', orderNumber: 'CWO-2024-0044', customerName: 'Fatimah Abdullah', addressLine1: '23 Bangsar South', status: 'Completed', assignedSiName: 'Ali Rahman', appointmentDate: '2024-01-14', orderType: 'Change Order' },
  { id: '5', orderNumber: 'NWO-2024-0005', customerName: 'Raj Kumar', addressLine1: '67 Mont Kiara', status: 'OnHold', assignedSiName: 'Mohd Rizal', appointmentDate: '2024-01-17', orderType: 'New Installation' },
  { id: '6', orderNumber: 'CWO-2024-0046', customerName: 'Wong Mei Hua', addressLine1: '34 Damansara Heights', status: 'InProgress', assignedSiName: 'Kumar Rajan', appointmentDate: '2024-01-15', orderType: 'Relocation' },
];

const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState<boolean>(true);
  const [orders, setOrders] = useState<Order[]>([]);
  const [dateRange, setDateRange] = useState<string>('today');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [stats, setStats] = useState<DashboardStats>({
    todaysOrders: 0,
    activeInstallers: 0,
    pendingNWO: 0,
    overdueCWO: 0
  });

  // Load data
  useEffect(() => {
    const loadData = async (): Promise<void> => {
      setLoading(true);
      try {
        // Try to fetch real orders
        const ordersData = await getOrders({ 
          status: statusFilter || undefined 
        });
        
        // Backend returns array directly, ensure we have an array
        const ordersArray = Array.isArray(ordersData) ? ordersData : [];
        
        if (ordersArray.length > 0) {
          // Show most recent orders first (sorted by createdAt descending)
          const sortedOrders = [...ordersArray].sort((a, b) => {
            const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
            const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
            return dateB - dateA; // Most recent first
          });
          
          setOrders(sortedOrders.slice(0, 10));
          
          // Calculate stats from real data
          const today = new Date();
          today.setHours(0, 0, 0, 0);
          const todayOrders = ordersArray.filter(o => {
            if (!o.createdAt) return false;
            const orderDate = new Date(o.createdAt);
            orderDate.setHours(0, 0, 0, 0);
            return orderDate.getTime() === today.getTime();
          });
          
          setStats({
            todaysOrders: todayOrders.length,
            activeInstallers: new Set(ordersArray.map(o => o.assignedSiId).filter(Boolean)).size,
            pendingNWO: ordersArray.filter(o => o.status === 'Pending' || o.status === 'New').length,
            overdueCWO: ordersArray.filter(o => o.status === 'OnHold' || o.status === 'Cancelled').length
          });
        } else {
          // Use sample data if no real data
          setOrders(SAMPLE_ORDERS as Order[]);
          setStats({
            todaysOrders: 24,
            activeInstallers: 12,
            pendingNWO: 8,
            overdueCWO: 3
          });
        }
      } catch (error) {
        console.error('Failed to load orders from database:', error);
        // Fallback to sample data only on error
        setOrders(SAMPLE_ORDERS as Order[]);
        setStats({
          todaysOrders: 24,
          activeInstallers: 12,
          pendingNWO: 8,
          overdueCWO: 3
        });
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [statusFilter, dateRange]);

  // Filter orders by status
  const filteredOrders = statusFilter 
    ? orders.filter(o => o.status === statusFilter)
    : orders;

  // Generate chart data
  const trendData = useMemo(() => {
    // Generate last 30 days trend
    const days = 30;
    const today = new Date();
    const trend = [];
    
    for (let i = days - 1; i >= 0; i--) {
      const date = new Date(today);
      date.setDate(date.getDate() - i);
      const dateStr = date.toLocaleDateString('en-MY', { day: 'numeric', month: 'short' });
      
      // Count orders for this date
      const dayOrders = orders.filter(o => {
        if (!o.createdAt) return false;
        const orderDate = new Date(o.createdAt);
        return orderDate.toDateString() === date.toDateString();
      });
      
      trend.push({ date: dateStr, count: dayOrders.length });
    }
    
    return trend;
  }, [orders]);

  const partnerData = useMemo(() => {
    // Group orders by partner
    const partnerCounts = new Map<string, number>();
    
    orders.forEach(order => {
      const partner = order.partnerName || 'Unknown';
      partnerCounts.set(partner, (partnerCounts.get(partner) || 0) + 1);
    });
    
    return Array.from(partnerCounts.entries())
      .map(([partner, count]) => ({ partner, count }))
      .sort((a, b) => b.count - a.count);
  }, [orders]);

  return (
    <PageShell
      title="Operations Dashboard"
      breadcrumbs={[{ label: 'Dashboard' }]}
      actions={
        <Button
          variant="outline"
          size="sm"
          onClick={() => window.location.reload()}
          className="gap-2"
        >
          <RefreshCw className="h-4 w-4" />
          Refresh
        </Button>
      }
    >
      <div className="space-y-3 md:space-y-4 lg:space-y-6">

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 md:gap-4">
        <StatCard
          title="Today's Orders"
          value={stats.todaysOrders}
          change="+12%"
          changeLabel="vs yesterday"
          trend="up"
          icon={FileText}
          iconBg="bg-blue-100 dark:bg-blue-900/30"
          loading={loading}
        />
        <StatCard
          title="Active Installers"
          value={stats.activeInstallers}
          change="+2"
          changeLabel="from last week"
          trend="up"
          icon={Users}
          iconBg="bg-emerald-100 dark:bg-emerald-900/30"
          loading={loading}
        />
        <StatCard
          title="Pending NWO"
          value={stats.pendingNWO}
          change="-3"
          changeLabel="from yesterday"
          trend="down"
          icon={Clock}
          iconBg="bg-amber-100 dark:bg-amber-900/30"
          loading={loading}
        />
        <StatCard
          title="Overdue CWO"
          value={stats.overdueCWO}
          change="+1"
          changeLabel="needs attention"
          trend="up"
          icon={AlertTriangle}
          iconBg="bg-red-100 dark:bg-red-900/30"
          loading={loading}
        />
      </div>

      {/* Charts Section */}
      {!loading && orders.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 md:gap-6">
          <OrdersTrendChart data={trendData} />
          <OrdersByPartnerChart data={partnerData} />
        </div>
      )}

      {/* Recent Orders Table */}
      <div className="bg-card rounded-xl border border-border shadow-sm">
        {/* Table Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 md:gap-4 p-3 md:p-4 lg:p-6 border-b border-border">
          <div>
            <h2 className="text-base md:text-lg font-semibold text-foreground">Recent Orders</h2>
            <p className="text-xs md:text-sm text-muted-foreground mt-1">Track and manage incoming orders</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <DateRangeFilter value={dateRange} onChange={setDateRange} />
            <StatusFilter value={statusFilter} onChange={setStatusFilter} />
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-border bg-muted/30">
                <th className="text-left py-2 md:py-3 px-2 md:px-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Order #</th>
                <th className="text-left py-2 md:py-3 px-2 md:px-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Customer</th>
                <th className="text-left py-2 md:py-3 px-2 md:px-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider hidden md:table-cell">Type</th>
                <th className="text-left py-2 md:py-3 px-2 md:px-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Status</th>
                <th className="text-left py-2 md:py-3 px-2 md:px-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider hidden lg:table-cell">Installer</th>
                <th className="text-left py-2 md:py-3 px-2 md:px-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider hidden sm:table-cell">Scheduled</th>
                <th className="text-right py-2 md:py-3 px-2 md:px-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {loading ? (
                // Loading skeleton
                [...Array(5)].map((_, i) => (
                  <tr key={i}>
                    <td className="py-2 md:py-3 px-2 md:px-4"><div className="h-4 w-24 bg-muted animate-pulse rounded" /></td>
                    <td className="py-2 md:py-3 px-2 md:px-4"><div className="h-4 w-32 bg-muted animate-pulse rounded" /></td>
                    <td className="py-2 md:py-3 px-2 md:px-4 hidden md:table-cell"><div className="h-4 w-20 bg-muted animate-pulse rounded" /></td>
                    <td className="py-2 md:py-3 px-2 md:px-4"><div className="h-5 w-20 bg-muted animate-pulse rounded-full" /></td>
                    <td className="py-2 md:py-3 px-2 md:px-4 hidden lg:table-cell"><div className="h-4 w-24 bg-muted animate-pulse rounded" /></td>
                    <td className="py-2 md:py-3 px-2 md:px-4 hidden sm:table-cell"><div className="h-4 w-20 bg-muted animate-pulse rounded" /></td>
                    <td className="py-2 md:py-3 px-2 md:px-4"><div className="h-4 w-16 bg-muted animate-pulse rounded ml-auto" /></td>
                  </tr>
                ))
              ) : filteredOrders.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center">
                    <div className="flex flex-col items-center gap-2">
                      <FileText className="h-10 w-10 text-muted-foreground/50" />
                      <p className="text-sm text-muted-foreground">No orders found</p>
                      <Button variant="outline" size="sm" onClick={() => setStatusFilter('')}>
                        Clear filters
                      </Button>
                    </div>
                  </td>
                </tr>
              ) : (
                filteredOrders.map((order) => (
                  <tr 
                    key={order.id} 
                    className="hover:bg-muted/50 transition-colors cursor-pointer"
                    onClick={() => navigate(`/orders/${order.id}`)}
                  >
                    <td className="py-2 md:py-3 px-2 md:px-4">
                      <span className="text-xs md:text-sm font-medium text-foreground">
                        {order.orderNumber || `ORD-${order.id}`}
                      </span>
                    </td>
                    <td className="py-2 md:py-3 px-2 md:px-4">
                      <div>
                        <p className="text-xs md:text-sm font-medium text-foreground">{order.customerName || 'Unknown'}</p>
                        <p className="text-xs text-muted-foreground truncate max-w-[200px]">{order.addressLine1 || order.buildingAddress || '-'}</p>
                      </div>
                    </td>
                    <td className="py-2 md:py-3 px-2 md:px-4 hidden md:table-cell">
                      <span className="text-xs md:text-sm text-muted-foreground">{order.orderType || order.orderTypeName || '-'}</span>
                    </td>
                    <td className="py-2 md:py-3 px-2 md:px-4">
                      <OrderStatusBadge status={order.status || 'Pending'} />
                    </td>
                    <td className="py-2 md:py-3 px-2 md:px-4 hidden lg:table-cell">
                      <span className="text-xs md:text-sm text-muted-foreground">{order.assignedSiName || 'Unassigned'}</span>
                    </td>
                    <td className="py-2 md:py-3 px-2 md:px-4 hidden sm:table-cell">
                      <span className="text-xs md:text-sm text-muted-foreground">
                        {order.appointmentDate 
                          ? new Date(order.appointmentDate).toLocaleDateString('en-MY', { day: 'numeric', month: 'short' })
                          : '-'
                        }
                      </span>
                    </td>
                    <td className="py-2 md:py-3 px-2 md:px-4">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate(`/orders/${order.id}`);
                          }}
                          className="p-1.5 rounded-md hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
                          title="View details"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate(`/orders/${order.id}?edit=true`);
                          }}
                          className="p-1.5 rounded-md hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
                          title="Edit order"
                        >
                          <Edit className="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Table Footer */}
        <div className="flex items-center justify-between px-4 py-3 border-t border-border bg-muted/20">
          <p className="text-sm text-muted-foreground">
            Showing <span className="font-medium">{filteredOrders.length}</span> of <span className="font-medium">{orders.length}</span> orders
          </p>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => navigate('/orders')}
            className="gap-1"
          >
            View all orders
            <ArrowRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
      </div>
    </PageShell>
  );
};

export default DashboardPage;

