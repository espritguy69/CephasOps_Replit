import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X } from 'lucide-react';
import { getOverheads, createOverhead, deleteOverhead } from '../../api/pnl';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Overhead, CreateOverheadRequest } from '../../types/pnl';

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
  sortable?: boolean;
}

interface OverheadFormData {
  costCentreId: string;
  period: string;
  amount: string;
  description: string;
}

const PnlOverheadsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [overheads, setOverheads] = useState<Overhead[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [formData, setFormData] = useState<OverheadFormData>({
    costCentreId: '',
    period: '',
    amount: '',
    description: ''
  });

  useEffect(() => {
    loadOverheads();
  }, []);

  const loadOverheads = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getOverheads();
      setOverheads(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load overheads');
      console.error('Error loading overheads:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      if (!formData.amount || parseFloat(formData.amount) <= 0) {
        showError('Amount is required and must be greater than 0');
        return;
      }
      const overheadData: CreateOverheadRequest = {
        costCentreId: formData.costCentreId,
        period: formData.period,
        amount: parseFloat(formData.amount),
        description: formData.description || undefined
      };
      await createOverhead(overheadData);
      showSuccess('Overhead created successfully!');
      setShowCreateModal(false);
      resetForm();
      loadOverheads();
    } catch (err: any) {
      showError(err.message || 'Failed to create overhead');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this overhead?')) return;
    
    try {
      await deleteOverhead(id);
      showSuccess('Overhead deleted successfully!');
      loadOverheads();
    } catch (err: any) {
      showError(err.message || 'Failed to delete overhead');
    }
  };

  const resetForm = (): void => {
    setFormData({
      costCentreId: '',
      period: '',
      amount: '',
      description: ''
    });
  };

  const columns: TableColumn<Overhead>[] = [
    { key: 'costCentreName', label: 'Cost Centre' },
    { key: 'period', label: 'Period' },
    { key: 'amount', label: 'Amount', render: (value: unknown) => value ? `RM ${parseFloat(value as string).toFixed(2)}` : '-' },
    { key: 'description', label: 'Description' }
  ];

  if (loading) {
    return <LoadingSpinner message="Loading overheads..." fullPage />;
  }

  return (
    <PageShell title="Overheads" breadcrumbs={[{ label: 'P&L' }, { label: 'Overheads' }]}>
    <div className="flex-1 p-3 max-w-7xl mx-auto">
      <div className="mb-2 flex items-center justify-between">
        <h1 className="text-sm font-bold text-foreground">Overheads</h1>
        <Button onClick={() => setShowCreateModal(true)} className="flex items-center gap-2">
          <Plus className="h-4 w-4" />
          Add Overhead
        </Button>
      </div>

      <Card>
        {overheads.length > 0 ? (
          <DataTable
            data={overheads}
            columns={columns}
            actions={(row: Overhead) => (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleDelete(row.id)}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
          />
        ) : (
          <EmptyState
            title="No overheads found"
            message="Add overhead entries to track costs."
          />
        )}
      </Card>

      {/* Create Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => {
          setShowCreateModal(false);
          resetForm();
        }}
        title="Add Overhead"
        size="md"
      >
        <div className="space-y-4">
          <TextInput
            label="Cost Centre ID"
            name="costCentreId"
            value={formData.costCentreId}
            onChange={(e) => setFormData({ ...formData, costCentreId: e.target.value })}
          />

          <TextInput
            label="Period"
            name="period"
            value={formData.period}
            onChange={(e) => setFormData({ ...formData, period: e.target.value })}
          />

          <TextInput
            label="Amount *"
            name="amount"
            type="number"
            step="0.01"
            value={formData.amount}
            onChange={(e) => setFormData({ ...formData, amount: e.target.value })}
            required
          />

          <TextInput
            label="Description"
            name="description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            multiline
            rows={3}
          />

          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button
              variant="outline"
              onClick={() => {
                setShowCreateModal(false);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreate}
              className="flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              Create
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default PnlOverheadsPage;

