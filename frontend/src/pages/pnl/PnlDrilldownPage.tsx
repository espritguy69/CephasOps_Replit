import React, { useState, useEffect } from 'react';
import { 
  TrendingUp, TrendingDown, DollarSign, BarChart3, Filter, 
  RefreshCcw, Download, ChevronDown, ChevronUp, Search,
  Building2, Users, Calendar, AlertCircle, CheckCircle, Clock
} from 'lucide-react';
import { getPnlDetailPerOrder, getPnlPeriods, exportPnlDetailPerOrder } from '../../api/pnl';
import { getPartners } from '../../api/partners';
import { getDepartments } from '../../api/departments';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { 
  LoadingSpinner, EmptyState, useToast, Button, Card, 
  DataTable, Select, TextInput, StatusBadge, Modal
} from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '@/lib/utils';
import type { PnlDetailPerOrder, PnlPeriod, PnlDetailPerOrderFilters } from '../../types/pnl';
import type { Partner } from '../../types/partners';
import type { Department } from '../../types/departments';

interface ServiceInstaller {
  id: string;
  name: string;
  isActive?: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const KPI_RESULTS = ['OnTime', 'Late', 'Exceeded', 'Rework'];

const PnlDrilldownPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  
  // Data state
  const [orders, setOrders] = useState<PnlDetailPerOrder[]>([]);
  const [periods, setPeriods] = useState<PnlPeriod[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [serviceInstallers, setServiceInstallers] = useState<ServiceInstaller[]>([]);
  
  // UI state
  const [loading, setLoading] = useState(true);
  const [showFilters, setShowFilters] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState<PnlDetailPerOrder | null>(null);
  
  // Filters
  const [filters, setFilters] = useState<PnlDetailPerOrderFilters>({
    period: '',
    partnerId: '',
    departmentId: '',
    serviceInstallerId: '',
    orderType: '',
    kpiResult: ''
  });
  
  // Summary stats
  const [summary, setSummary] = useState({
    totalRevenue: 0,
    totalCost: 0,
    totalProfit: 0,
    avgMargin: 0,
    orderCount: 0
  });
  
  useEffect(() => {
    loadInitialData();
  }, []);
  
  useEffect(() => {
    loadOrders();
  }, [filters]);
  
  const loadInitialData = async () => {
    try {
      const [periodsRes, partnersRes, departmentsRes, siRes] = await Promise.all([
        getPnlPeriods().catch(() => []),
        getPartners({ isActive: true }).catch(() => []),
        getDepartments().catch(() => []),
        getServiceInstallers({ isActive: true }).catch(() => [])
      ]);
      
      setPeriods(Array.isArray(periodsRes) ? periodsRes : []);
      setPartners(Array.isArray(partnersRes) ? partnersRes : []);
      setDepartments(Array.isArray(departmentsRes) ? departmentsRes : []);
      setServiceInstallers(Array.isArray(siRes) ? siRes : []);
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to load reference data');
    }
  };
  
  const loadOrders = async () => {
    try {
      setLoading(true);
      
      const apiFilters: PnlDetailPerOrderFilters = {};
      if (filters.period) apiFilters.period = filters.period;
      if (filters.partnerId) apiFilters.partnerId = filters.partnerId;
      if (filters.departmentId) apiFilters.departmentId = filters.departmentId;
      if (filters.serviceInstallerId) apiFilters.serviceInstallerId = filters.serviceInstallerId;
      if (filters.orderType) apiFilters.orderType = filters.orderType;
      if (filters.kpiResult) apiFilters.kpiResult = filters.kpiResult;
      
      const data = await getPnlDetailPerOrder(apiFilters);
      const orderList = Array.isArray(data) ? data : [];
      setOrders(orderList);
      
      // Calculate summary
      const totalRevenue = orderList.reduce((sum, o) => sum + (o.revenueAmount || 0), 0);
      const totalCost = orderList.reduce((sum, o) => sum + (o.materialCost || 0) + (o.labourCost || 0) + (o.overheadAllocated || 0), 0);
      const totalProfit = orderList.reduce((sum, o) => sum + (o.profitForOrder || 0), 0);
      const avgMargin = totalRevenue > 0 ? (totalProfit / totalRevenue) * 100 : 0;
      
      setSummary({
        totalRevenue,
        totalCost,
        totalProfit,
        avgMargin,
        orderCount: orderList.length
      });
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to load P&L data');
    } finally {
      setLoading(false);
    }
  };
  
  const handleExport = async () => {
    try {
      await exportPnlDetailPerOrder(filters);
      showSuccess('P&L data exported successfully');
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to export P&L data');
    }
  };
  
  const getKpiIcon = (kpiResult?: string) => {
    switch (kpiResult) {
      case 'OnTime':
        return <CheckCircle className="h-3 w-3 text-green-500" />;
      case 'Late':
        return <Clock className="h-3 w-3 text-yellow-500" />;
      case 'Exceeded':
        return <AlertCircle className="h-3 w-3 text-red-500" />;
      case 'Rework':
        return <AlertCircle className="h-3 w-3 text-orange-500" />;
      default:
        return null;
    }
  };
  
  const getKpiBadgeVariant = (kpiResult?: string): 'success' | 'warning' | 'error' | 'default' => {
    switch (kpiResult) {
      case 'OnTime':
        return 'success';
      case 'Late':
        return 'warning';
      case 'Exceeded':
      case 'Rework':
        return 'error';
      default:
        return 'default';
    }
  };
  
  const getProfitColor = (profit: number) => {
    if (profit > 0) return 'text-green-500';
    if (profit < 0) return 'text-red-500';
    return 'text-muted-foreground';
  };
  
  const columns: TableColumn<PnlDetailPerOrder>[] = [
    {
      key: 'orderId',
      label: 'Order',
      render: (_, row) => (
        <div>
          <div className="font-medium text-xs">{row.orderNumber || row.orderId.slice(0, 8)}</div>
          <div className="text-[10px] text-muted-foreground">{row.orderType}</div>
        </div>
      )
    },
    {
      key: 'derivedPartnerCategoryLabel',
      label: 'Partner–Category',
      render: (_v, row) => (row.derivedPartnerCategoryLabel ?? row.partnerName) || '-'
    },
    {
      key: 'period',
      label: 'Period'
    },
    {
      key: 'revenueAmount',
      label: 'Revenue',
      render: (v) => (
        <span className="text-green-500 font-medium">
          RM {((v as number) || 0).toFixed(2)}
        </span>
      )
    },
    {
      key: 'materialCost',
      label: 'Material',
      render: (v) => (
        <span className="text-red-400">
          RM {((v as number) || 0).toFixed(2)}
        </span>
      )
    },
    {
      key: 'labourCost',
      label: 'Labour',
      render: (v) => (
        <span className="text-red-400">
          RM {((v as number) || 0).toFixed(2)}
        </span>
      )
    },
    {
      key: 'grossProfit',
      label: 'Gross Profit',
      render: (v) => {
        const profit = (v as number) || 0;
        return (
          <span className={cn('font-medium', getProfitColor(profit))}>
            RM {profit.toFixed(2)}
          </span>
        );
      }
    },
    {
      key: 'profitForOrder',
      label: 'Net Profit',
      render: (v) => {
        const profit = (v as number) || 0;
        return (
          <span className={cn('font-bold', getProfitColor(profit))}>
            RM {profit.toFixed(2)}
          </span>
        );
      }
    },
    {
      key: 'kpiResult',
      label: 'KPI',
      render: (v, row) => (
        <div className="flex items-center gap-1">
          {getKpiIcon(v as string)}
          <StatusBadge variant={getKpiBadgeVariant(v as string)}>
            {(v as string) || 'N/A'}
          </StatusBadge>
          {row.rescheduleCount > 0 && (
            <span className="text-[10px] text-orange-400">+{row.rescheduleCount}R</span>
          )}
        </div>
      )
    },
    {
      key: 'serviceInstallerName',
      label: 'SI',
      render: (v) => v || '-'
    }
  ];
  
  if (loading && orders.length === 0) {
    return <LoadingSpinner message="Loading P&L data..." fullPage />;
  }
  
  return (
    <PageShell title="P&L Drilldown" breadcrumbs={[{ label: 'P&L' }, { label: 'Drilldown' }]}>
    <div className="flex-1 p-3 md:p-4 lg:p-6 max-w-full mx-auto flex flex-col h-full">
      {/* Header */}
      <div className="mb-3 md:mb-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-base md:text-lg font-bold text-foreground flex items-center gap-2">
            <BarChart3 className="h-5 w-5 text-primary" />
            P&L Drill-Down by Order
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Per-order profitability analysis with rate source tracking
          </p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <Button variant="outline" size="sm" onClick={handleExport}>
            <Download className="h-3 w-3 mr-1" />
            Export
          </Button>
          <Button variant="outline" size="sm" onClick={loadOrders}>
            <RefreshCcw className="h-3 w-3" />
          </Button>
        </div>
      </div>
      
      {/* Summary Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2 md:gap-3 mb-3 md:mb-4">
        <Card className="p-3">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-[10px] md:text-xs text-muted-foreground">Total Revenue</p>
              <p className="text-sm md:text-base font-bold text-green-500">
                RM {summary.totalRevenue.toFixed(2)}
              </p>
            </div>
            <TrendingUp className="h-4 w-4 md:h-5 md:w-5 text-green-500 flex-shrink-0" />
          </div>
        </Card>
        
        <Card className="p-3">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-[10px] md:text-xs text-muted-foreground">Total Costs</p>
              <p className="text-sm md:text-base font-bold text-red-500">
                RM {summary.totalCost.toFixed(2)}
              </p>
            </div>
            <TrendingDown className="h-4 w-4 md:h-5 md:w-5 text-red-500 flex-shrink-0" />
          </div>
        </Card>
        
        <Card className="p-3">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-[10px] md:text-xs text-muted-foreground">Net Profit</p>
              <p className={cn('text-sm md:text-base font-bold', getProfitColor(summary.totalProfit))}>
                RM {summary.totalProfit.toFixed(2)}
              </p>
            </div>
            <DollarSign className="h-4 w-4 md:h-5 md:w-5 text-blue-500 flex-shrink-0" />
          </div>
        </Card>
        
        <Card className="p-3">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-[10px] md:text-xs text-muted-foreground">Avg Margin</p>
              <p className={cn('text-sm md:text-base font-bold', summary.avgMargin >= 0 ? 'text-blue-500' : 'text-red-500')}>
                {summary.avgMargin.toFixed(1)}%
              </p>
            </div>
            <BarChart3 className="h-4 w-4 md:h-5 md:w-5 text-purple-500 flex-shrink-0" />
          </div>
        </Card>
        
        <Card className="p-3">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-[10px] md:text-xs text-muted-foreground">Orders</p>
              <p className="text-sm md:text-base font-bold text-foreground">
                {summary.orderCount}
              </p>
            </div>
            <Building2 className="h-4 w-4 md:h-5 md:w-5 text-orange-500 flex-shrink-0" />
          </div>
        </Card>
      </div>
      
      {/* Filters */}
      <Card className="mb-3">
        <div className="p-3">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-medium flex items-center gap-1">
              <Filter className="h-3 w-3" />
              Filters
            </span>
            <button 
              onClick={() => setShowFilters(!showFilters)}
              className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1"
            >
              {showFilters ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />}
              {showFilters ? 'Hide' : 'Show'}
            </button>
          </div>
          {showFilters && (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-3 pt-2 border-t border-border">
              <Select
                label="Period"
                value={filters.period || ''}
                onChange={(e) => setFilters({ ...filters, period: e.target.value })}
                options={[
                  { value: '', label: 'All Periods' },
                  ...periods.map(p => ({ value: p.periodName || p.id, label: p.periodName || p.id }))
                ]}
              />
              <Select
                label="Partner"
                value={filters.partnerId || ''}
                onChange={(e) => setFilters({ ...filters, partnerId: e.target.value })}
                options={[
                  { value: '', label: 'All Partners' },
                  ...partners.map(p => ({ value: p.id, label: p.name }))
                ]}
              />
              <Select
                label="Department"
                value={filters.departmentId || ''}
                onChange={(e) => setFilters({ ...filters, departmentId: e.target.value })}
                options={[
                  { value: '', label: 'All Departments' },
                  ...departments.map(d => ({ value: d.id, label: d.name }))
                ]}
              />
              <Select
                label="Service Installer"
                value={filters.serviceInstallerId || ''}
                onChange={(e) => setFilters({ ...filters, serviceInstallerId: e.target.value })}
                options={[
                  { value: '', label: 'All SIs' },
                  ...serviceInstallers.map(si => ({ value: si.id, label: si.name }))
                ]}
              />
              <TextInput
                label="Order Type"
                value={filters.orderType || ''}
                onChange={(e) => setFilters({ ...filters, orderType: e.target.value })}
                placeholder="e.g., ACTIVATION"
              />
              <Select
                label="KPI Result"
                value={filters.kpiResult || ''}
                onChange={(e) => setFilters({ ...filters, kpiResult: e.target.value })}
                options={[
                  { value: '', label: 'All Results' },
                  ...KPI_RESULTS.map(k => ({ value: k, label: k }))
                ]}
              />
            </div>
          )}
        </div>
      </Card>
      
      {/* Data Table */}
      <Card className="flex-1 flex flex-col min-h-0">
        {loading ? (
          <div className="flex-1 flex items-center justify-center">
            <LoadingSpinner message="Loading..." />
          </div>
        ) : orders.length > 0 ? (
          <div className="flex-1 overflow-hidden">
            <DataTable
              data={orders}
              columns={columns}
              onRowClick={(row) => setSelectedOrder(row)}
            />
          </div>
        ) : (
          <EmptyState
            title="No P&L data found"
            message="P&L data will appear here once orders are completed and processed. Try adjusting your filters."
          />
        )}
      </Card>
      
      {/* Order Detail Modal */}
      <Modal
        isOpen={selectedOrder !== null}
        onClose={() => setSelectedOrder(null)}
        title="Order P&L Detail"
        size="lg"
      >
        {selectedOrder && (
          <div className="p-4 space-y-4">
            {/* Order Info */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div>
                <p className="text-xs text-muted-foreground">Order ID</p>
                <p className="font-medium">{selectedOrder.orderNumber || selectedOrder.orderId.slice(0, 8)}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Order Type</p>
                <p className="font-medium">{selectedOrder.orderType}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Period</p>
                <p className="font-medium">{selectedOrder.period}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Completed</p>
                <p className="font-medium">
                  {selectedOrder.completedAt 
                    ? new Date(selectedOrder.completedAt).toLocaleDateString() 
                    : 'N/A'}
                </p>
              </div>
            </div>
            
            {/* Installation Info */}
            <div className="grid grid-cols-2 md:grid-cols-3 gap-4 p-3 bg-muted rounded-lg">
              <div>
                <p className="text-xs text-muted-foreground">Partner–Category</p>
                <p className="font-medium">{selectedOrder.derivedPartnerCategoryLabel || selectedOrder.partnerName || 'N/A'}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Order Category</p>
                <p className="font-medium">{selectedOrder.orderCategory || 'N/A'}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Installation Method</p>
                <p className="font-medium">{selectedOrder.installationMethod || 'N/A'}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Service Installer</p>
                <p className="font-medium">{selectedOrder.serviceInstallerName || 'N/A'}</p>
              </div>
            </div>
            
            {/* Financial Breakdown */}
            <div className="space-y-3">
              <h4 className="font-medium text-sm flex items-center gap-2">
                <DollarSign className="h-4 w-4 text-primary" />
                Financial Breakdown
              </h4>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Revenue */}
                <div className="p-3 bg-green-900/20 rounded-lg border border-green-700/30">
                  <div className="flex justify-between items-center mb-2">
                    <span className="text-sm font-medium text-green-400">Revenue</span>
                    <span className="text-lg font-bold text-green-400">
                      RM {selectedOrder.revenueAmount.toFixed(2)}
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Source: {selectedOrder.revenueRateSource || 'N/A'}
                  </p>
                </div>
                
                {/* Costs */}
                <div className="p-3 bg-red-900/20 rounded-lg border border-red-700/30">
                  <div className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Material Cost</span>
                      <span className="text-red-400">RM {selectedOrder.materialCost.toFixed(2)}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Labour Cost</span>
                      <span className="text-red-400">RM {selectedOrder.labourCost.toFixed(2)}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Overhead</span>
                      <span className="text-red-400">RM {selectedOrder.overheadAllocated.toFixed(2)}</span>
                    </div>
                    <div className="flex justify-between text-sm font-medium pt-1 border-t border-red-700/30">
                      <span className="text-red-400">Total Cost</span>
                      <span className="text-red-400">
                        RM {(selectedOrder.materialCost + selectedOrder.labourCost + selectedOrder.overheadAllocated).toFixed(2)}
                      </span>
                    </div>
                  </div>
                  <p className="text-xs text-muted-foreground mt-2">
                    Labour Source: {selectedOrder.labourRateSource || 'N/A'}
                  </p>
                </div>
              </div>
              
              {/* Profit Summary */}
              <div className="grid grid-cols-2 gap-4">
                <div className="p-3 bg-blue-900/20 rounded-lg border border-blue-700/30">
                  <div className="flex justify-between items-center">
                    <span className="text-sm font-medium text-blue-400">Gross Profit</span>
                    <span className={cn('text-lg font-bold', getProfitColor(selectedOrder.grossProfit))}>
                      RM {selectedOrder.grossProfit.toFixed(2)}
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground mt-1">
                    Revenue - Material - Labour
                  </p>
                </div>
                
                <div className="p-3 bg-purple-900/20 rounded-lg border border-purple-700/30">
                  <div className="flex justify-between items-center">
                    <span className="text-sm font-medium text-purple-400">Net Profit</span>
                    <span className={cn('text-lg font-bold', getProfitColor(selectedOrder.profitForOrder))}>
                      RM {selectedOrder.profitForOrder.toFixed(2)}
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground mt-1">
                    Gross Profit - Overhead
                  </p>
                </div>
              </div>
            </div>
            
            {/* KPI & Quality */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 p-3 bg-muted rounded-lg">
              <div>
                <p className="text-xs text-muted-foreground">KPI Result</p>
                <div className="flex items-center gap-1 mt-1">
                  {getKpiIcon(selectedOrder.kpiResult)}
                  <StatusBadge variant={getKpiBadgeVariant(selectedOrder.kpiResult)}>
                    {selectedOrder.kpiResult || 'N/A'}
                  </StatusBadge>
                </div>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Reschedules</p>
                <p className={cn('font-medium', selectedOrder.rescheduleCount > 0 ? 'text-orange-400' : '')}>
                  {selectedOrder.rescheduleCount}
                </p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Calculated At</p>
                <p className="font-medium text-xs">
                  {new Date(selectedOrder.calculatedAt).toLocaleString()}
                </p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Data Quality</p>
                <p className="font-medium text-xs">
                  {selectedOrder.dataQualityNotes || 'OK'}
                </p>
              </div>
            </div>
            
            <div className="flex justify-end pt-4 border-t">
              <Button variant="outline" onClick={() => setSelectedOrder(null)}>
                Close
              </Button>
            </div>
          </div>
        )}
      </Modal>
      </div>
    </PageShell>
  );
};

export default PnlDrilldownPage;

