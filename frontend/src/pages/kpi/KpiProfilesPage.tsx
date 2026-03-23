import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Edit, Trash2, RefreshCw, Target, CheckCircle2, XCircle } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { 
  LoadingSpinner, Card, Button, useToast, DataTable, Modal, 
  TextInput, Select, ConfirmDialog, EmptyState, Textarea 
} from '../../components/ui';
import { 
  getKpiProfiles, createKpiProfile, updateKpiProfile, deleteKpiProfile, setKpiProfileAsDefault 
} from '../../api/kpiProfiles';
import { getOrderTypes } from '../../api/orderTypes';
import { getPartners } from '../../api/partners';
import { getBuildingTypes } from '../../api/buildingTypes';
import { useDepartment } from '../../contexts/DepartmentContext';
import type { KpiProfile, CreateKpiProfileRequest, UpdateKpiProfileRequest } from '../../types/kpiProfiles';

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const KpiProfilesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { activeDepartment } = useDepartment();
  const queryClient = useQueryClient();
  const [showModal, setShowModal] = useState(false);
  const [editingProfile, setEditingProfile] = useState<KpiProfile | null>(null);
  const [deletingProfile, setDeletingProfile] = useState<KpiProfile | null>(null);
  const [formData, setFormData] = useState<Partial<CreateKpiProfileRequest>>({
    name: '',
    orderType: '',
    maxJobDurationMinutes: 0,
    docketKpiMinutes: 0,
    maxReschedulesAllowed: undefined,
    isDefault: false,
  });

  const companyId = activeDepartment?.companyId || '';

  // Fetch KPI profiles
  const { data: kpiProfiles = [], isLoading, refetch } = useQuery({
    queryKey: ['kpiProfiles', companyId],
    queryFn: async () => {
      const profiles = await getKpiProfiles({ isActive: true } as any);
      return profiles;
    },
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });

  // Fetch reference data
  const { data: orderTypes = [] } = useQuery({
    queryKey: ['orderTypes'],
    queryFn: async () => {
      const types = await getOrderTypes();
      return types;
    },
  });

  const { data: partners = [] } = useQuery({
    queryKey: ['partners'],
    queryFn: async () => {
      const partnersList = await getPartners();
      return partnersList;
    },
  });

  const { data: buildingTypes = [] } = useQuery({
    queryKey: ['buildingTypes'],
    queryFn: async () => {
      const types = await getBuildingTypes();
      return types;
    },
  });

  // Create mutation
  const createMutation = useMutation({
    mutationFn: (data: CreateKpiProfileRequest) => createKpiProfile(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kpiProfiles'] });
      showSuccess('KPI profile created successfully');
      setShowModal(false);
      resetForm();
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create KPI profile');
    },
  });

  // Update mutation
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateKpiProfileRequest }) =>
      updateKpiProfile(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kpiProfiles'] });
      showSuccess('KPI profile updated successfully');
      setShowModal(false);
      resetForm();
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update KPI profile');
    },
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteKpiProfile(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kpiProfiles'] });
      showSuccess('KPI profile deleted successfully');
      setDeletingProfile(null);
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete KPI profile');
    },
  });

  // Set default mutation
  const setDefaultMutation = useMutation({
    mutationFn: (id: string) => setKpiProfileAsDefault(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kpiProfiles'] });
      showSuccess('KPI profile set as default');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to set default KPI profile');
    },
  });

  const resetForm = () => {
    setFormData({
      name: '',
      orderType: '',
      maxJobDurationMinutes: 0,
      docketKpiMinutes: 0,
      maxReschedulesAllowed: undefined,
      isDefault: false,
    });
    setEditingProfile(null);
  };

  const handleCreate = () => {
    resetForm();
    setShowModal(true);
  };

  const handleEdit = (profile: KpiProfile) => {
    setEditingProfile(profile);
    setFormData({
      name: profile.name,
      description: profile.description,
      orderType: profile.orderType || '',
      partnerId: profile.partnerId,
      buildingTypeId: profile.buildingTypeId,
      maxJobDurationMinutes: (profile as any).maxJobDurationMinutes || 0,
      docketKpiMinutes: (profile as any).docketKpiMinutes || 0,
      maxReschedulesAllowed: (profile as any).maxReschedulesAllowed,
      isDefault: profile.isDefault,
    });
    setShowModal(true);
  };

  const handleDelete = (profile: KpiProfile) => {
    setDeletingProfile(profile);
  };

  const handleSubmit = () => {
    if (!formData.name || !formData.orderType) {
      showError('Name and Order Type are required');
      return;
    }

    // Prepare data matching backend DTO structure
    const submitData: any = {
      name: formData.name,
      orderType: formData.orderType,
      maxJobDurationMinutes: formData.maxJobDurationMinutes || 0,
      docketKpiMinutes: formData.docketKpiMinutes || 0,
      maxReschedulesAllowed: formData.maxReschedulesAllowed,
      isDefault: formData.isDefault || false,
    };

    if (formData.description) {
      submitData.description = formData.description;
    }
    if (formData.partnerId) {
      submitData.partnerId = formData.partnerId;
    }
    if (formData.buildingTypeId) {
      submitData.buildingTypeId = formData.buildingTypeId;
    }

    if (editingProfile) {
      updateMutation.mutate({
        id: editingProfile.id,
        data: submitData as UpdateKpiProfileRequest,
      });
    } else {
      createMutation.mutate(submitData as CreateKpiProfileRequest);
    }
  };

  const columns: TableColumn<KpiProfile>[] = [
    {
      key: 'name',
      label: 'Name',
      render: (value, row) => (
        <div>
          <div className="font-medium text-slate-900">{value as string}</div>
          {row.description && (
            <div className="text-xs text-slate-500 mt-0.5">{row.description}</div>
          )}
        </div>
      ),
    },
    {
      key: 'orderType',
      label: 'Order Type',
      render: (value) => <span className="text-slate-700">{value as string || 'All'}</span>,
    },
    {
      key: 'maxJobDurationMinutes',
      label: 'Max Duration',
      render: (value, row) => (
        <span className="text-slate-700">
          {(row as any).maxJobDurationMinutes ? `${(row as any).maxJobDurationMinutes} min` : 'N/A'}
        </span>
      ),
    },
    {
      key: 'maxReschedulesAllowed',
      label: 'Max Reschedules',
      render: (value, row) => (
        <span className="text-slate-700">
          {(row as any).maxReschedulesAllowed ?? 'Unlimited'}
        </span>
      ),
    },
    {
      key: 'isDefault',
      label: 'Default',
      render: (value) => (
        <span className={`px-2 py-1 rounded text-xs font-medium ${
          value ? 'bg-blue-100 text-blue-700' : 'bg-gray-100 text-gray-600'
        }`}>
          {value ? 'Yes' : 'No'}
        </span>
      ),
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_, row) => (
        <div className="flex gap-2">
          <Button
            size="sm"
            variant="ghost"
            onClick={() => handleEdit(row)}
          >
            <Edit className="h-4 w-4" />
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => handleDelete(row)}
          >
            <Trash2 className="h-4 w-4 text-red-600" />
          </Button>
          {!row.isDefault && (
            <Button
              size="sm"
              variant="ghost"
              onClick={() => setDefaultMutation.mutate(row.id)}
              title="Set as default"
            >
              <CheckCircle2 className="h-4 w-4 text-blue-600" />
            </Button>
          )}
        </div>
      ),
    },
  ];

  if (isLoading) {
    return <LoadingSpinner message="Loading KPI profiles..." fullPage />;
  }

  return (
    <PageShell
      title="KPI Profiles - Manage KPI profiles and performance targets"
      actions={
        <div className="flex gap-2">
          <Button size="sm" variant="outline" className="gap-2" onClick={() => refetch()}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          <Button size="sm" className="gap-2" onClick={handleCreate}>
            <Plus className="h-4 w-4" />
            Add Profile
          </Button>
        </div>
      }
    >
      {kpiProfiles.length === 0 ? (
          <EmptyState
            title="No KPI Profiles"
            description="Create your first KPI profile to start tracking performance metrics"
            icon={<Target className="h-12 w-12" />}
            action={
            <Button onClick={handleCreate}>
              <Plus className="h-4 w-4 mr-2" />
              Create KPI Profile
            </Button>
          }
        />
      ) : (
        <Card>
          <DataTable
            data={kpiProfiles}
            columns={columns}
          />
        </Card>
      )}

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showModal}
        onClose={() => {
          setShowModal(false);
          resetForm();
        }}
        title={editingProfile ? 'Edit KPI Profile' : 'Create KPI Profile'}
        size="large"
      >
        <div className="space-y-4">
          <TextInput
            label="Name"
            value={formData.name || ''}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            required
          />
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Description
            </label>
            <Textarea
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              rows={2}
              className="w-full"
            />
          </div>
          <Select
            label="Order Type"
            value={formData.orderType || ''}
            onChange={(e) => setFormData({ ...formData, orderType: e.target.value })}
            options={[
              { value: '', label: 'All Order Types' },
              ...orderTypes.map(ot => ({ value: ot.code, label: ot.name })),
            ]}
            required
          />
          <Select
            label="Partner (Optional)"
            value={formData.partnerId || ''}
            onChange={(e) => setFormData({ ...formData, partnerId: e.target.value || undefined })}
            options={[
              { value: '', label: 'All Partners' },
              ...partners.map(p => ({ value: p.id, label: p.name })),
            ]}
          />
          <Select
            label="Building Type (Optional)"
            value={formData.buildingTypeId || ''}
            onChange={(e) => setFormData({ ...formData, buildingTypeId: e.target.value || undefined })}
            options={[
              { value: '', label: 'All Building Types' },
              ...buildingTypes.map(bt => ({ value: bt.id, label: bt.name })),
            ]}
          />
          <TextInput
            label="Max Job Duration (minutes)"
            type="number"
            value={formData.maxJobDurationMinutes?.toString() || '0'}
            onChange={(e) => setFormData({ 
              ...formData, 
              maxJobDurationMinutes: parseInt(e.target.value) || 0 
            })}
            required
          />
          <TextInput
            label="Docket KPI Minutes"
            type="number"
            value={formData.docketKpiMinutes?.toString() || '0'}
            onChange={(e) => setFormData({ 
              ...formData, 
              docketKpiMinutes: parseInt(e.target.value) || 0 
            })}
            required
          />
          <TextInput
            label="Max Reschedules Allowed (leave empty for unlimited)"
            type="number"
            value={formData.maxReschedulesAllowed?.toString() || ''}
            onChange={(e) => setFormData({ 
              ...formData, 
              maxReschedulesAllowed: e.target.value ? parseInt(e.target.value) : undefined 
            })}
          />
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isDefault"
              checked={formData.isDefault || false}
              onChange={(e) => setFormData({ ...formData, isDefault: e.target.checked })}
              className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
            />
            <label htmlFor="isDefault" className="text-sm font-medium text-slate-700">
              Set as default profile
            </label>
          </div>
          <div className="flex justify-end gap-2 pt-4">
            <Button
              variant="outline"
              onClick={() => {
                setShowModal(false);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleSubmit}
              disabled={createMutation.isPending || updateMutation.isPending}
            >
              {editingProfile ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={!!deletingProfile}
        onClose={() => setDeletingProfile(null)}
        onConfirm={() => {
          if (deletingProfile) {
            deleteMutation.mutate(deletingProfile.id);
          }
        }}
        title="Delete KPI Profile"
        message={`Are you sure you want to delete "${deletingProfile?.name}"? This action cannot be undone.`}
        confirmText="Delete"
        variant="danger"
      />
    </PageShell>
  );
};

export default KpiProfilesPage;

