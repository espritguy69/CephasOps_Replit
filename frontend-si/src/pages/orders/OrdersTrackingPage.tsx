import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { Search } from 'lucide-react';
import { Card, Skeleton, EmptyState, TextInput, StatusBadge, getOrderStatusVariant, DataTable } from '../../components/ui';
import type { DataTableColumn } from '../../components/ui';
import { PageHeader } from '../../components/layout/PageHeader';
import { getAllOrders } from '../../api/orders';
import { useAuth } from '../../contexts/AuthContext';
import type { OrderFilters } from '../../api/orders';

export function OrdersTrackingPage() {
  const navigate = useNavigate();
  const { user, isAdmin } = useAuth();
  const [searchParams] = useSearchParams();
  const assignedSiIdFromUrl = searchParams.get('assignedSiId');
  
  const [filters, setFilters] = useState<OrderFilters>({
    status: undefined,
    fromDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    toDate: new Date().toISOString().split('T')[0],
    assignedSiId: assignedSiIdFromUrl || undefined,
  });
  const [searchTerm, setSearchTerm] = useState('');

  // Update filters when URL param changes
  useEffect(() => {
    if (assignedSiIdFromUrl) {
      setFilters(prev => ({ ...prev, assignedSiId: assignedSiIdFromUrl }));
    }
  }, [assignedSiIdFromUrl]);

  if (!isAdmin) {
    return (
      <>
        <PageHeader title="All Orders" />
        <div className="p-4">
          <EmptyState
            title="Access Denied"
            description="This page is only available to administrators"
          />
        </div>
      </>
    );
  }

  const { data: orders, isLoading, error } = useQuery({
    queryKey: ['allOrders', filters],
    queryFn: () => getAllOrders(filters),
    enabled: !!user?.id && isAdmin,
  });

  const filteredOrders = orders?.filter((order: any) => {
    if (!searchTerm) return true;
    const searchLower = searchTerm.toLowerCase();
    return (
      order.customerName?.toLowerCase().includes(searchLower) ||
      order.orderNumber?.toLowerCase().includes(searchLower) ||
      order.tbbn?.toLowerCase().includes(searchLower) ||
      order.addressLine1?.toLowerCase().includes(searchLower) ||
      order.city?.toLowerCase().includes(searchLower)
    );
  }) || [];

  const orderColumns: DataTableColumn<any>[] = [
    {
      key: 'customer',
      label: 'Customer / Order',
      sortable: true,
      sortValue: (row) => row.customerName || row.orderNumber || '',
      render: (_, row) => (
        <div>
          <div className="font-medium">{row.customerName || row.orderNumber}</div>
          {row.orderNumber && <div className="text-muted-foreground text-xs">Order: {row.orderNumber}</div>}
          {row.tbbn && <div className="text-muted-foreground text-xs">TBBN: {row.tbbn}</div>}
          {row.partnerName && <div className="text-muted-foreground text-xs">Partner: {row.partnerName}</div>}
        </div>
      ),
    },
    {
      key: 'status',
      label: 'Status',
      sortable: true,
      render: (_, row) => (
        <StatusBadge variant={getOrderStatusVariant(row.status)} size="sm">
          {row.status}
        </StatusBadge>
      ),
    },
    {
      key: 'address',
      label: 'Address',
      sortable: true,
      sortValue: (row) => [row.addressLine1, row.city, row.postcode].filter(Boolean).join(' '),
      render: (_, row) => (
        <span className="text-muted-foreground text-xs">
          {[row.addressLine1, row.city, row.postcode].filter(Boolean).join(', ')}
        </span>
      ),
    },
    {
      key: 'appointmentDate',
      label: 'Appointment',
      sortable: true,
      sortValue: (row) => (row.appointmentDate ? new Date(row.appointmentDate).getTime() : 0),
      render: (_, row) =>
        row.appointmentDate ? (
          <span className="text-muted-foreground text-xs">
            {format(new Date(row.appointmentDate), 'MMM dd, yyyy')}
            {row.appointmentWindowFrom && row.appointmentWindowTo && (
              <> {row.appointmentWindowFrom} – {row.appointmentWindowTo}</>
            )}
          </span>
        ) : (
          '—'
        ),
    },
    {
      key: 'assignedSiName',
      label: 'Assigned to',
      sortable: true,
      render: (_, row) => (row.assignedSiName ? <span className="text-xs">{row.assignedSiName}</span> : '—'),
    },
  ];

  if (isLoading) {
    return (
      <>
        <PageHeader title="All Orders" />
        <div className="p-4 md:p-6 space-y-4">
          <Card className="p-4">
            <Skeleton className="h-10 w-full mb-4" />
            <div className="grid grid-cols-2 gap-2">
              <Skeleton className="h-9 w-full" />
              <Skeleton className="h-9 w-full" />
            </div>
          </Card>
          {[1, 2, 3].map((i) => (
            <Card key={i} className="p-4">
              <div className="flex justify-between items-start mb-2">
                <div className="flex-1 space-y-1">
                  <Skeleton className="h-6 w-40" />
                  <Skeleton className="h-4 w-24" />
                </div>
                <Skeleton className="h-5 w-16 rounded-full" />
              </div>
              <Skeleton className="h-4 w-full mb-1" />
              <Skeleton className="h-4 w-3/4" />
            </Card>
          ))}
        </div>
      </>
    );
  }

  if (error) {
    return (
      <>
        <PageHeader title="All Orders" />
        <div className="p-4">
          <EmptyState
            title="Error loading orders"
            description={(error as Error).message || 'Failed to fetch orders.'}
          />
        </div>
      </>
    );
  }

  return (
    <>
      <PageHeader title="All Orders" />
      <div className="p-4 md:p-6 space-y-4">
      {/* Filters */}
      <Card className="p-4">
        <div className="space-y-4">
          <div className="flex gap-2">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <TextInput
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Search by customer, order number, address..."
                className="pl-10"
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-sm font-medium mb-1">From Date</label>
              <input
                type="date"
                value={filters.fromDate || ''}
                onChange={(e) => setFilters({ ...filters, fromDate: e.target.value })}
                className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">To Date</label>
              <input
                type="date"
                value={filters.toDate || ''}
                onChange={(e) => setFilters({ ...filters, toDate: e.target.value })}
                className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground"
              />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Status</label>
            <select
              value={filters.status || ''}
              onChange={(e) => setFilters({ ...filters, status: e.target.value || undefined })}
              className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground"
            >
              <option value="">All Statuses</option>
              <option value="Pending">Pending</option>
              <option value="Assigned">Assigned</option>
              <option value="OnTheWay">On The Way</option>
              <option value="MetCustomer">Met Customer</option>
              <option value="Installing">Installing</option>
              <option value="OrderCompleted">Order Completed</option>
              <option value="Completed">Completed</option>
            </select>
          </div>
        </div>
      </Card>

      {/* Orders List */}
      <DataTable
        columns={orderColumns}
        data={filteredOrders}
        sortable
        pagination
        pageSize={10}
        emptyMessage={searchTerm ? 'No orders match your search' : 'No orders match the selected criteria'}
        onRowClick={(row) => navigate(`/jobs/${row.id}`)}
      />

      {filteredOrders.length > 0 && (
        <p className="text-sm text-muted-foreground text-center">
          Showing {filteredOrders.length} of {orders?.length || 0} orders
        </p>
      )}
      </div>
    </>
  );
}

