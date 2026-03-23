import React, { useState, useEffect, useMemo } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import type { LucideIcon } from 'lucide-react';
import { 
  FileText, Clock, Users, AlertTriangle, TrendingUp, TrendingDown,
  ArrowRight, Filter, CalendarDays, RefreshCw,
  Eye, Edit, ChevronDown, Package, Receipt, DollarSign,
  Calendar, CheckSquare, Inbox, BarChart3, ShieldAlert,
  ArrowDownToLine, AlertCircle, ExternalLink
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '../components/ui';
import { PageShell } from '../components/layout';
import { getOrders } from '../api/orders';
import type { Order } from '../types/orders';
import { OrdersTrendChart, OrdersByPartnerChart } from '../components/charts';
import { usePermissions } from '../hooks/usePermissions';
import { getInvoices } from '../api/billing';
import type { Invoice } from '../types/billing';
import { getStockByLocation } from '../api/inventory';
import type { StockBalance } from '../types/inventory';
import { getParserStatistics } from '../api/parser';
import { getUnassignedOrders } from '../api/scheduler';

interface StatCardProps {
  title: string;
  value: number | string;
  change?: string;
  changeLabel?: string;
  icon: LucideIcon;
  iconBg?: string;
  trend?: 'up' | 'down';
  loading?: boolean;
  linkTo?: string;
}

const StatCard: React.FC<StatCardProps> = ({ 
  title, 
  value, 
  change, 
  changeLabel,
  icon: Icon, 
  iconBg,
  trend,
  loading,
  linkTo
}) => {
  const navigate = useNavigate();
  return (
    <div 
      className={cn(
        "bg-card rounded-lg border border-border p-3 md:p-4 lg:p-6 shadow-sm hover-lift transition-smooth",
        linkTo && "cursor-pointer"
      )}
      onClick={linkTo ? () => navigate(linkTo) : undefined}
    >
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
            iconBg?.includes('purple') ? "text-purple-600" :
            "text-primary"
          )} />
        </div>
      </div>
    </div>
  );
};

interface SummaryCardProps {
  title: string;
  icon: LucideIcon;
  iconBg: string;
  items: Array<{ label: string; value: string | number; linkTo?: string; severity?: 'normal' | 'warning' | 'critical' }>;
  loading?: boolean;
}

const SummaryCard: React.FC<SummaryCardProps> = ({ title, icon: Icon, iconBg, items, loading }) => {
  const navigate = useNavigate();
  return (
    <div className="bg-card rounded-lg border border-border shadow-sm">
      <div className="flex items-center gap-3 p-4 border-b border-border">
        <div className={cn("h-8 w-8 rounded-lg flex items-center justify-center flex-shrink-0", iconBg)}>
          <Icon className="h-4 w-4 text-white" />
        </div>
        <h3 className="text-sm font-semibold text-foreground">{title}</h3>
      </div>
      <div className="divide-y divide-border">
        {loading ? (
          [...Array(3)].map((_, i) => (
            <div key={i} className="flex items-center justify-between px-4 py-3">
              <div className="h-4 w-32 bg-muted animate-pulse rounded" />
              <div className="h-5 w-8 bg-muted animate-pulse rounded" />
            </div>
          ))
        ) : items.length === 0 ? (
          <div className="px-4 py-6 text-center text-sm text-muted-foreground">No items to display</div>
        ) : (
          items.map((item, idx) => (
            <div
              key={idx}
              className={cn(
                "flex items-center justify-between px-4 py-3 text-sm",
                item.linkTo && "cursor-pointer hover:bg-muted/50 transition-colors"
              )}
              onClick={item.linkTo ? () => navigate(item.linkTo!) : undefined}
            >
              <span className="text-muted-foreground">{item.label}</span>
              <div className="flex items-center gap-2">
                <span className={cn(
                  "font-semibold",
                  item.severity === 'critical' && "text-destructive",
                  item.severity === 'warning' && "text-amber-600",
                  item.severity === 'normal' && "text-foreground",
                  !item.severity && "text-foreground"
                )}>
                  {item.value}
                </span>
                {item.linkTo && <ExternalLink className="h-3 w-3 text-muted-foreground" />}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

interface AlertItem {
  id: string;
  message: string;
  severity: 'info' | 'warning' | 'critical';
  linkTo?: string;
  linkLabel?: string;
  module: string;
}

interface AlertsSectionProps {
  alerts: AlertItem[];
  loading?: boolean;
}

const AlertsSection: React.FC<AlertsSectionProps> = ({ alerts, loading }) => {
  const navigate = useNavigate();

  if (loading) {
    return (
      <div className="bg-card rounded-lg border border-border shadow-sm p-4">
        <div className="flex items-center gap-2 mb-4">
          <ShieldAlert className="h-5 w-5 text-amber-500" />
          <h3 className="text-sm font-semibold text-foreground">Alerts & Action Items</h3>
        </div>
        <div className="space-y-3">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="h-12 bg-muted animate-pulse rounded-lg" />
          ))}
        </div>
      </div>
    );
  }

  if (alerts.length === 0) return null;

  const severityConfig = {
    critical: { bg: 'bg-red-50 dark:bg-red-950/30', border: 'border-red-200 dark:border-red-800', icon: AlertCircle, iconColor: 'text-red-500' },
    warning: { bg: 'bg-amber-50 dark:bg-amber-950/30', border: 'border-amber-200 dark:border-amber-800', icon: AlertTriangle, iconColor: 'text-amber-500' },
    info: { bg: 'bg-blue-50 dark:bg-blue-950/30', border: 'border-blue-200 dark:border-blue-800', icon: Inbox, iconColor: 'text-blue-500' },
  };

  return (
    <div className="bg-card rounded-lg border border-border shadow-sm p-4">
      <div className="flex items-center gap-2 mb-4">
        <ShieldAlert className="h-5 w-5 text-amber-500" />
        <h3 className="text-sm font-semibold text-foreground">Alerts & Action Items</h3>
        <span className="ml-auto text-xs text-muted-foreground">{alerts.length} active</span>
      </div>
      <div className="space-y-2">
        {alerts.map((alert) => {
          const config = severityConfig[alert.severity];
          const AlertIcon = config.icon;
          return (
            <div
              key={alert.id}
              className={cn(
                "flex items-center gap-3 p-3 rounded-lg border",
                config.bg, config.border,
                alert.linkTo && "cursor-pointer hover:opacity-90 transition-opacity"
              )}
              onClick={alert.linkTo ? () => navigate(alert.linkTo!) : undefined}
            >
              <AlertIcon className={cn("h-4 w-4 flex-shrink-0", config.iconColor)} />
              <div className="flex-1 min-w-0">
                <p className="text-sm text-foreground">{alert.message}</p>
                <p className="text-xs text-muted-foreground mt-0.5">{alert.module}</p>
              </div>
              {alert.linkTo && (
                <Button variant="ghost" size="sm" className="flex-shrink-0 h-7 text-xs gap-1">
                  {alert.linkLabel || 'View'}
                  <ArrowRight className="h-3 w-3" />
                </Button>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};

interface OrderStatusBadgeProps {
  status: string;
}

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
  const { hasPermission, isAdmin, isSuperAdmin, isFinance, isOperations, isWarehouse } = usePermissions();
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

  const [billingData, setBillingData] = useState<{ overdue: number; pending: number; totalRevenue: string }>({ overdue: 0, pending: 0, totalRevenue: '-' });
  const [inventoryData, setInventoryData] = useState<{ lowStock: number }>({ lowStock: 0 });
  const [parserData, setParserData] = useState<{ newDrafts: number; lowConfidence: number; pendingReview: number }>({ newDrafts: 0, lowConfidence: 0, pendingReview: 0 });
  const [schedulerData, setSchedulerData] = useState<{ unassignedCount: number }>({ unassignedCount: 0 });

  const canViewOrders = hasPermission('orders.view');
  const canViewScheduler = hasPermission('scheduler.view');
  const canViewBilling = hasPermission('billing.view');
  const canViewInventory = hasPermission('inventory.view');
  const canViewKpi = hasPermission('kpi.view');
  const showOrderWidgets = isAdmin || canViewOrders;
  const showSchedulerWidgets = isAdmin || canViewScheduler;
  const showBillingWidgets = isAdmin || isFinance || canViewBilling;
  const showInventoryWidgets = isAdmin || isWarehouse || canViewInventory;
  const showParserWidgets = isAdmin || canViewOrders;

  useEffect(() => {
    const loadData = async (): Promise<void> => {
      setLoading(true);

      const fetchPromises: Promise<void>[] = [];

      if (showOrderWidgets) {
        fetchPromises.push(
          getOrders({})
            .then(ordersData => {
              const allOrders = Array.isArray(ordersData) ? ordersData : [];
              if (allOrders.length > 0) {
                const today = new Date();
                today.setHours(0, 0, 0, 0);
                const todayOrders = allOrders.filter(o => {
                  if (!o.createdAt) return false;
                  const orderDate = new Date(o.createdAt);
                  orderDate.setHours(0, 0, 0, 0);
                  return orderDate.getTime() === today.getTime();
                });
                setStats({
                  todaysOrders: todayOrders.length,
                  activeInstallers: new Set(allOrders.map(o => o.assignedSiId).filter(Boolean)).size,
                  pendingNWO: allOrders.filter(o => o.status === 'Pending' || o.status === 'New').length,
                  overdueCWO: allOrders.filter(o => o.status === 'OnHold' || o.status === 'Cancelled').length
                });

                const displayOrders = statusFilter
                  ? allOrders.filter(o => o.status === statusFilter)
                  : allOrders;
                const sortedOrders = [...displayOrders].sort((a, b) => {
                  const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
                  const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
                  return dateB - dateA;
                });
                setOrders(sortedOrders.slice(0, 10));
              } else {
                setOrders([]);
                setStats({ todaysOrders: 0, activeInstallers: 0, pendingNWO: 0, overdueCWO: 0 });
              }
            })
            .catch(() => {
              setOrders([]);
              setStats({ todaysOrders: 0, activeInstallers: 0, pendingNWO: 0, overdueCWO: 0 });
            })
        );
      }

      if (showBillingWidgets) {
        fetchPromises.push(
          getInvoices({})
            .then((invoices: Invoice[]) => {
              const arr = Array.isArray(invoices) ? invoices : [];
              const overdue = arr.filter(inv => inv.status === 'Overdue' || inv.status === 'PastDue').length;
              const pending = arr.filter(inv => inv.status === 'Draft' || inv.status === 'Pending').length;
              const paidTotal = arr
                .filter(inv => inv.status === 'Paid' || inv.status === 'Completed')
                .reduce((sum, inv) => sum + (inv.totalAmount || 0), 0);
              setBillingData({
                overdue,
                pending,
                totalRevenue: paidTotal > 0 ? `RM ${paidTotal.toLocaleString()}` : '-',
              });
            })
            .catch(() => setBillingData({ overdue: 0, pending: 0, totalRevenue: '-' }))
        );
      }

      if (showInventoryWidgets) {
        fetchPromises.push(
          getStockByLocation({})
            .then((stockData: StockBalance[]) => {
              const arr = Array.isArray(stockData) ? stockData : [];
              const lowStock = arr.filter(s => s.availableQuantity <= 0).length;
              setInventoryData({ lowStock });
            })
            .catch(() => setInventoryData({ lowStock: 0 }))
        );
      }

      if (showParserWidgets) {
        fetchPromises.push(
          getParserStatistics()
            .then(parserStats => {
              setParserData({
                newDrafts: parserStats.pendingDrafts || 0,
                lowConfidence: parserStats.needsReviewDrafts || 0,
                pendingReview: parserStats.validDrafts || 0,
              });
            })
            .catch(() => setParserData({ newDrafts: 0, lowConfidence: 0, pendingReview: 0 }))
        );
      }

      if (showSchedulerWidgets) {
        fetchPromises.push(
          getUnassignedOrders({})
            .then(unassigned => {
              const arr = Array.isArray(unassigned) ? unassigned : [];
              setSchedulerData({ unassignedCount: arr.length });
            })
            .catch(() => setSchedulerData({ unassignedCount: 0 }))
        );
      }

      await Promise.allSettled(fetchPromises);
      setLoading(false);
    };

    loadData();
  }, [statusFilter, dateRange, showOrderWidgets, showBillingWidgets, showInventoryWidgets, showParserWidgets, showSchedulerWidgets]);

  const filteredOrders = orders;

  const trendData = useMemo(() => {
    const days = 30;
    const today = new Date();
    const trend = [];
    
    for (let i = days - 1; i >= 0; i--) {
      const date = new Date(today);
      date.setDate(date.getDate() - i);
      const dateStr = date.toLocaleDateString('en-MY', { day: 'numeric', month: 'short' });
      
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
    const partnerCounts = new Map<string, number>();
    
    orders.forEach(order => {
      const partner = order.partnerName || 'Unknown';
      partnerCounts.set(partner, (partnerCounts.get(partner) || 0) + 1);
    });
    
    return Array.from(partnerCounts.entries())
      .map(([partner, count]) => ({ partner, count }))
      .sort((a, b) => b.count - a.count);
  }, [orders]);

  const alerts = useMemo<AlertItem[]>(() => {
    const items: AlertItem[] = [];

    if (showOrderWidgets && stats.overdueCWO > 0) {
      items.push({
        id: 'overdue-cwo',
        message: `${stats.overdueCWO} overdue change work orders need immediate attention`,
        severity: 'critical',
        linkTo: '/orders?status=OnHold',
        linkLabel: 'View Orders',
        module: 'Operations',
      });
    }

    if (showOrderWidgets && stats.pendingNWO > 5) {
      items.push({
        id: 'high-pending',
        message: `${stats.pendingNWO} pending new work orders awaiting assignment`,
        severity: 'warning',
        linkTo: '/orders?status=New',
        linkLabel: 'Assign Orders',
        module: 'Operations',
      });
    }

    if (showSchedulerWidgets && schedulerData.unassignedCount > 0) {
      items.push({
        id: 'unassigned-orders',
        message: `${schedulerData.unassignedCount} order${schedulerData.unassignedCount > 1 ? 's are' : ' is'} unassigned and need${schedulerData.unassignedCount === 1 ? 's' : ''} scheduling`,
        severity: 'warning',
        linkTo: '/scheduler',
        linkLabel: 'Open Scheduler',
        module: 'Scheduler',
      });
    }

    if (showBillingWidgets && billingData.overdue > 0) {
      items.push({
        id: 'billing-overdue',
        message: `${billingData.overdue} overdue invoice${billingData.overdue > 1 ? 's' : ''} require${billingData.overdue === 1 ? 's' : ''} follow-up`,
        severity: 'critical',
        linkTo: '/billing/invoices',
        linkLabel: 'View Invoices',
        module: 'Billing',
      });
    }

    if (showBillingWidgets && billingData.pending > 0) {
      items.push({
        id: 'billing-pending',
        message: `${billingData.pending} pending invoice submission${billingData.pending > 1 ? 's' : ''} awaiting review`,
        severity: 'warning',
        linkTo: '/billing/invoices',
        linkLabel: 'Review',
        module: 'Billing',
      });
    }

    if (showInventoryWidgets && inventoryData.lowStock > 0) {
      items.push({
        id: 'inventory-low-stock',
        message: `${inventoryData.lowStock} material${inventoryData.lowStock > 1 ? 's' : ''} at or below minimum stock level`,
        severity: 'warning',
        linkTo: '/inventory/stock-summary',
        linkLabel: 'Stock Summary',
        module: 'Inventory',
      });
    }

    if (showParserWidgets && parserData.lowConfidence > 0) {
      items.push({
        id: 'parser-low-confidence',
        message: `${parserData.lowConfidence} parsed order${parserData.lowConfidence > 1 ? 's' : ''} flagged for manual review (low confidence)`,
        severity: 'warning',
        linkTo: '/orders/parser',
        linkLabel: 'Review',
        module: 'Parser',
      });
    }

    return items;
  }, [showOrderWidgets, showSchedulerWidgets, showBillingWidgets, showInventoryWidgets, showParserWidgets, stats, orders, billingData, inventoryData, parserData, schedulerData]);

  const dashboardTitle = isSuperAdmin ? 'Admin Dashboard' :
    isAdmin ? 'Admin Dashboard' :
    isFinance && !isOperations ? 'Finance Dashboard' :
    isWarehouse && !isOperations ? 'Warehouse Dashboard' :
    'Operations Dashboard';

  return (
    <PageShell
      title={dashboardTitle}
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

      {showOrderWidgets && (
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
            linkTo="/orders"
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
            linkTo="/orders?status=Pending"
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
            linkTo="/orders?status=OnHold"
          />
        </div>
      )}

      <AlertsSection alerts={alerts} loading={loading} />

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
        {showSchedulerWidgets && (
          <SummaryCard
            title="Scheduler"
            icon={Calendar}
            iconBg="bg-blue-600"
            loading={loading}
            items={[
              { label: 'Unassigned Orders', value: schedulerData.unassignedCount, linkTo: '/scheduler', severity: schedulerData.unassignedCount > 0 ? 'warning' : 'normal' },
              { label: 'Active Installers', value: stats.activeInstallers, linkTo: '/scheduler' },
            ]}
          />
        )}

        {showBillingWidgets && (
          <SummaryCard
            title="Billing"
            icon={Receipt}
            iconBg="bg-emerald-600"
            loading={loading}
            items={[
              { label: 'Overdue Invoices', value: billingData.overdue, linkTo: '/billing/invoices', severity: billingData.overdue > 0 ? 'critical' : 'normal' },
              { label: 'Pending Submissions', value: billingData.pending, linkTo: '/billing/invoices', severity: billingData.pending > 0 ? 'warning' : 'normal' },
              { label: 'This Month Revenue', value: billingData.totalRevenue, linkTo: '/pnl/summary' },
            ]}
          />
        )}

        {showInventoryWidgets && (
          <SummaryCard
            title="Inventory"
            icon={Package}
            iconBg="bg-amber-600"
            loading={loading}
            items={[
              { label: 'Low / Out of Stock', value: inventoryData.lowStock, linkTo: '/inventory/stock-summary', severity: inventoryData.lowStock > 0 ? 'warning' : 'normal' },
              { label: 'Total Tracked Items', value: '-', linkTo: '/inventory/stock-summary' },
            ]}
          />
        )}

        {showParserWidgets && (
          <SummaryCard
            title="Parser Intake"
            icon={ArrowDownToLine}
            iconBg="bg-purple-600"
            loading={loading}
            items={[
              { label: 'New Drafts', value: parserData.newDrafts, linkTo: '/orders/parser', severity: parserData.newDrafts > 0 ? 'warning' : 'normal' },
              { label: 'Low Confidence Items', value: parserData.lowConfidence, linkTo: '/orders/parser', severity: parserData.lowConfidence > 0 ? 'warning' : 'normal' },
              { label: 'Pending Review', value: parserData.pendingReview, linkTo: '/orders/parser' },
            ]}
          />
        )}
      </div>

      {showOrderWidgets && !loading && orders.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 md:gap-6">
          <OrdersTrendChart data={trendData} />
          <OrdersByPartnerChart data={partnerData} />
        </div>
      )}

      {showOrderWidgets && (
        <div className="bg-card rounded-xl border border-border shadow-sm">
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
      )}

      {!showOrderWidgets && !showBillingWidgets && !showInventoryWidgets && !showSchedulerWidgets && (
        <div className="bg-card rounded-lg border border-border shadow-sm p-8 text-center">
          <BarChart3 className="h-12 w-12 text-muted-foreground/50 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-foreground mb-2">Welcome to CephasOps</h3>
          <p className="text-sm text-muted-foreground max-w-md mx-auto">
            Your dashboard will show relevant operational data based on your role and permissions. Contact your administrator if you need access to additional modules.
          </p>
        </div>
      )}
      </div>
    </PageShell>
  );
};

export default DashboardPage;
