import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Filter } from 'lucide-react';
import { getRmaRequests } from '../../api/inventory';
import { LoadingSpinner, EmptyState, useToast, Button, Card, Select, StatusBadge } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { RmaRequest } from '../../types/inventory';

interface RMAFilters {
  status: string | null;
}

const RMAListPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError } = useToast();
  const [rmaRequests, setRmaRequests] = useState<RmaRequest[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<RMAFilters>({ status: null });

  useEffect(() => {
    loadRmaRequests();
  }, [filters]);

  const loadRmaRequests = async (): Promise<void> => {
    try {
      setLoading(true);
      setError(null);
      const data = await getRmaRequests(filters);
      setRmaRequests(Array.isArray(data) ? data : []);
    } catch (err) {
      const errorMessage = (err as Error).message || 'Failed to load RMA requests';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Error loading RMA requests:', err);
    } finally {
      setLoading(false);
    }
  };

  const getStatusVariant = (status?: string): 'success' | 'error' | 'warning' => {
    const statusLower = status?.toLowerCase() || 'pending';
    if (statusLower === 'approved' || statusLower === 'completed') return 'success';
    if (statusLower === 'rejected') return 'error';
    return 'warning';
  };

  if (loading && rmaRequests.length === 0) {
    return (
      <PageShell title="RMA Management" breadcrumbs={[{ label: 'RMA' }]}>
        <LoadingSpinner message="Loading RMA requests..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="RMA Management"
      breadcrumbs={[{ label: 'RMA' }]}
      actions={
        <Button onClick={() => navigate('/rma/new')}>
          <Plus className="h-4 w-4 mr-2" />
          Create RMA Request
        </Button>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      {/* Filters */}
      <div className="mb-2 flex items-center gap-2">
        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-muted-foreground" />
          <Select
            label="Status"
            value={filters.status || ''}
            onChange={(e) => setFilters({ ...filters, status: e.target.value || null })}
            options={[
              { value: '', label: 'All Statuses' },
              { value: 'Pending', label: 'Pending' },
              { value: 'Approved', label: 'Approved' },
              { value: 'Rejected', label: 'Rejected' },
              { value: 'Completed', label: 'Completed' }
            ]}
            className="w-48"
          />
        </div>
      </div>

      {/* Error Banner */}
      {error && (
        <div className="mb-2 rounded border border-red-200 bg-red-50 p-2 text-xs text-red-800" role="alert">
          {error}
        </div>
      )}

      {/* Content */}
      <Card>
        {rmaRequests.length > 0 ? (
          <div className="flex flex-col gap-2">
            {rmaRequests.map(rma => (
              <Card
                key={rma.id}
                className="p-4 cursor-pointer hover:shadow-md transition-shadow"
                onClick={() => navigate(`/rma/${rma.id}`)}
              >
                <div className="flex justify-between items-start mb-3">
                  <div>
                    <h3 className="text-lg font-semibold mb-1">RMA #{rma.rmaNumber || rma.id}</h3>
                    <p className="text-sm text-muted-foreground">Partner: {rma.partnerName || 'N/A'}</p>
                  </div>
                  <StatusBadge
                    status={rma.status || 'Pending'}
                    variant={getStatusVariant(rma.status)}
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <div className="flex gap-4 text-sm">
                    <span className="text-muted-foreground font-medium min-w-[80px]">Created:</span>
                    <span>{rma.createdAt ? new Date(rma.createdAt).toLocaleDateString() : 'N/A'}</span>
                  </div>
                  {rma.reason && (
                    <div className="flex gap-4 text-sm">
                      <span className="text-muted-foreground font-medium min-w-[80px]">Reason:</span>
                      <span>{rma.reason}</span>
                    </div>
                  )}
                  {rma.quantity && (
                    <div className="flex gap-4 text-sm">
                      <span className="text-muted-foreground font-medium min-w-[80px]">Quantity:</span>
                      <span>{rma.quantity}</span>
                    </div>
                  )}
                </div>
              </Card>
            ))}
          </div>
        ) : (
          <EmptyState
            title="No RMA requests found"
            description="RMA requests will appear here once created."
          />
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default RMAListPage;

