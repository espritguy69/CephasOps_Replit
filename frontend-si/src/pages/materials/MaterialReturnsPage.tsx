import React, { useState, useMemo } from 'react';
import { Package, AlertTriangle, Calendar, Filter, RefreshCw, CheckCircle, XCircle, Clock } from 'lucide-react';
import { Card, Button, TextInput, Skeleton, EmptyState, useToast } from '../../components/ui';
import { useQuery } from '@tanstack/react-query';
import { getMaterialReturns, type MaterialReturn, type MaterialReturnsQuery } from '../../api/si-app';
import { useAuth } from '../../contexts/AuthContext';
import { format } from 'date-fns';

export function MaterialReturnsPage() {
  const { user } = useAuth();
  const { showError } = useToast();
  const [dateFrom, setDateFrom] = useState<string>('');
  const [dateTo, setDateTo] = useState<string>('');
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [filterReturnType, setFilterReturnType] = useState<string>('all');

  // Build query filters
  const queryFilters: MaterialReturnsQuery = useMemo(() => {
    const filters: MaterialReturnsQuery = {};
    if (dateFrom) filters.dateFrom = dateFrom;
    if (dateTo) filters.dateTo = dateTo;
    if (filterStatus !== 'all') filters.status = filterStatus as 'faulty' | 'returned' | 'rma';
    if (filterReturnType !== 'all') {
      filters.returnType = filterReturnType as 'faulty' | 'replacement' | 'nonserialised';
    }
    return filters;
  }, [dateFrom, dateTo, filterStatus, filterReturnType]);

  // Fetch material returns
  const { data: returns = [], isLoading, refetch } = useQuery({
    queryKey: ['materialReturns', queryFilters],
    queryFn: () => getMaterialReturns(queryFilters),
    enabled: !!user?.id,
  });

  const handleRefresh = () => {
    refetch();
  };

  return (
    <div className="p-4 space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-foreground">Material Returns</h2>
        <Button variant="outline" size="sm" onClick={handleRefresh} className="min-h-[44px]">
          <RefreshCw className="h-4 w-4 mr-2" />
          Refresh
        </Button>
      </div>

      {/* Filters */}
      <Card className="p-4">
        <div className="flex items-center gap-2 mb-4">
          <Filter className="h-5 w-5 text-muted-foreground" />
          <h3 className="font-semibold">Filters</h3>
        </div>
        <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
          <div>
            <label className="block text-sm font-medium mb-1">From Date</label>
            <input
              type="date"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
              className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground min-h-[44px] text-base"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">To Date</label>
            <input
              type="date"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
              className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground min-h-[44px] text-base"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Status</label>
            <select
              value={filterStatus}
              onChange={(e) => setFilterStatus(e.target.value)}
              className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground min-h-[44px] text-base"
            >
              <option value="all">All</option>
              <option value="faulty">Faulty</option>
              <option value="returned">Returned</option>
              <option value="rma">RMA Created</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Return Type</label>
            <select
              value={filterReturnType}
              onChange={(e) => setFilterReturnType(e.target.value)}
              className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground min-h-[44px] text-base"
            >
              <option value="all">All</option>
              <option value="faulty">Faulty</option>
              <option value="replacement">Replacement</option>
              <option value="nonserialised">Non-Serialised</option>
            </select>
          </div>
          <div className="flex items-end">
            <Button variant="outline" onClick={handleRefresh} className="w-full">
              <RefreshCw className="h-4 w-4 mr-2" />
              Refresh
            </Button>
          </div>
        </div>
      </Card>

      {/* Returns List */}
      {isLoading ? (
        <Card className="p-4">
          <div className="space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="p-4 border border-border rounded-lg">
                <div className="flex items-start justify-between">
                  <div className="flex-1 space-y-2">
                    <div className="flex items-center gap-2">
                      <Skeleton className="h-4 w-4 rounded" />
                      <Skeleton className="h-5 w-32" />
                    </div>
                    <Skeleton className="h-4 w-2/3" />
                    <Skeleton className="h-3 w-24" />
                  </div>
                  <Skeleton className="h-6 w-20 rounded-md" />
                </div>
              </div>
            ))}
          </div>
        </Card>
      ) : returns.length === 0 ? (
        <Card className="p-4">
          <EmptyState
            title="No Material Returns"
            description="You haven't returned any materials yet. Materials marked as faulty or replaced will appear here."
          />
        </Card>
      ) : (
        <Card className="p-4">
          <div className="space-y-3">
            {returns.map((returnItem) => (
              <div
                key={returnItem.id}
                className="p-4 border border-border rounded-lg hover:bg-muted/50 transition-colors"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <Package className="h-4 w-4 text-muted-foreground" />
                      <span className="font-semibold">{returnItem.materialName}</span>
                      {returnItem.serialNumber && (
                        <span className="text-xs text-muted-foreground">
                          (SN: {returnItem.serialNumber})
                        </span>
                      )}
                    </div>
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-sm text-muted-foreground">
                      {returnItem.orderServiceId && (
                        <div>
                          <span className="font-medium">Order:</span> {returnItem.orderServiceId}
                        </div>
                      )}
                      <div>
                        <span className="font-medium">Quantity:</span> {returnItem.quantity}
                      </div>
                      <div>
                        <span className="font-medium">Date:</span>{' '}
                        {format(new Date(returnItem.returnedAt), 'dd MMM yyyy HH:mm')}
                      </div>
                      <div className="flex items-center gap-1">
                        <span className="font-medium">Type:</span>
                        <span className="text-xs px-2 py-0.5 rounded bg-blue-100 text-blue-700">
                          {returnItem.returnType}
                        </span>
                      </div>
                    </div>
                    {returnItem.reason && (
                      <div className="mt-2 text-sm">
                        <span className="font-medium">Reason:</span> {returnItem.reason}
                      </div>
                    )}
                    {returnItem.notes && (
                      <div className="mt-1 text-sm text-muted-foreground">
                        {returnItem.notes}
                      </div>
                    )}
                  </div>
                  <div className="ml-4">
                    {returnItem.status === 'RMA Created' ? (
                      <span className="flex items-center gap-1 px-2 py-1 rounded text-xs font-medium bg-green-100 text-green-700">
                        <CheckCircle className="h-3 w-3" />
                        RMA Created
                      </span>
                    ) : returnItem.status === 'Faulty' ? (
                      <span className="flex items-center gap-1 px-2 py-1 rounded text-xs font-medium bg-red-100 text-red-700">
                        <AlertTriangle className="h-3 w-3" />
                        Faulty
                      </span>
                    ) : (
                      <span className="flex items-center gap-1 px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-700">
                        <Clock className="h-3 w-3" />
                        Returned
                      </span>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  );
}

