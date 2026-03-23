import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Star } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { getKpiProfiles, createKpiProfile, updateKpiProfile, deleteKpiProfile, setKpiProfileAsDefault } from '../../api/kpiProfiles';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, Select } from '../../components/ui';
import type { KpiProfile, CreateKpiProfileRequest, UpdateKpiProfileRequest } from '../../types/kpiProfiles';

interface KpiProfileFormData {
  name: string;
  orderType: string;
  partnerId: string;
  buildingTypeId: string;
  maxJobDurationMinutes: number | string;
  docketKpiMinutes: number | string;
  maxReschedulesAllowed: string;
  isDefault: boolean;
  effectiveFrom: string;
  effectiveTo: string;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const KpiProfilesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [profiles, setProfiles] = useState<KpiProfile[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingProfile, setEditingProfile] = useState<KpiProfile | null>(null);
  const [formData, setFormData] = useState<KpiProfileFormData>({
    name: '',
    orderType: '',
    partnerId: '',
    buildingTypeId: '',
    maxJobDurationMinutes: 120,
    docketKpiMinutes: 60,
    maxReschedulesAllowed: '',
    isDefault: false,
    effectiveFrom: new Date().toISOString().split('T')[0],
    effectiveTo: ''
  });

  useEffect(() => {
    loadProfiles();
  }, []);

  const loadProfiles = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getKpiProfiles();
      setProfiles(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load KPI profiles');
      console.error('Error loading profiles:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      if (!formData.name.trim() || !formData.orderType) {
        showError('Please fill in all required fields');
        return;
      }
      
      const profileData: CreateKpiProfileRequest = {
        name: formData.name.trim(),
        orderTypeId: formData.orderType || undefined,
        partnerId: formData.partnerId || undefined,
        buildingTypeId: formData.buildingTypeId || undefined,
        isDefault: formData.isDefault,
        kpis: []
      };
      
      await createKpiProfile(profileData);
      showSuccess('KPI Profile created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadProfiles();
    } catch (err: any) {
      showError(err.message || 'Failed to create KPI profile');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingProfile) return;
    try {
      if (!formData.name.trim() || !formData.orderType) {
        showError('Please fill in all required fields');
        return;
      }
      
      const profileData: UpdateKpiProfileRequest = {
        name: formData.name.trim(),
        orderTypeId: formData.orderType || undefined,
        partnerId: formData.partnerId || undefined,
        buildingTypeId: formData.buildingTypeId || undefined,
        isDefault: formData.isDefault
      };
      
      await updateKpiProfile(editingProfile.id, profileData);
      showSuccess('KPI Profile updated successfully!');
      setShowCreateModal(false);
      setEditingProfile(null);
      resetForm();
      await loadProfiles();
    } catch (err: any) {
      showError(err.message || 'Failed to update KPI profile');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this KPI profile?')) return;
    
    try {
      await deleteKpiProfile(id);
      showSuccess('KPI Profile deleted successfully!');
      await loadProfiles();
    } catch (err: any) {
      showError(err.message || 'Failed to delete KPI profile');
    }
  };

  const handleSetDefault = async (profileId: string): Promise<void> => {
    try {
      await setKpiProfileAsDefault(profileId);
      showSuccess('Profile set as default successfully!');
      await loadProfiles();
    } catch (err: any) {
      showError(err.message || 'Failed to set profile as default');
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: '',
      orderType: '',
      partnerId: '',
      buildingTypeId: '',
      maxJobDurationMinutes: 120,
      docketKpiMinutes: 60,
      maxReschedulesAllowed: '',
      isDefault: false,
      effectiveFrom: new Date().toISOString().split('T')[0],
      effectiveTo: ''
    });
  };

  const openEditModal = (profile: KpiProfile): void => {
    setEditingProfile(profile);
    setFormData({
      name: profile.name || '',
      orderType: profile.orderTypeId || '',
      partnerId: profile.partnerId || '',
      buildingTypeId: profile.buildingTypeId || '',
      maxJobDurationMinutes: 120,
      docketKpiMinutes: 60,
      maxReschedulesAllowed: '',
      isDefault: profile.isDefault || false,
      effectiveFrom: '',
      effectiveTo: ''
    });
    setShowCreateModal(true);
  };

  const columns: TableColumn<KpiProfile>[] = [
    { 
      key: 'name', 
      label: 'Name',
      render: (value: unknown, row: KpiProfile) => (
        <div className="flex items-center gap-2">
          <span>{value as string}</span>
          {row.isDefault && (
            <Star className="h-3 w-3 text-yellow-500 fill-yellow-500" />
          )}
        </div>
      )
    },
    { key: 'orderTypeName', label: 'Order Type' },
    { 
      key: 'isDefault', 
      label: 'Status',
      render: (value: unknown) => (
        <span className={`px-2 py-1 rounded-full text-xs font-medium ${
          value 
            ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' 
            : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
        }`}>
          {value ? 'Default' : 'Active'}
        </span>
      )
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (value: unknown, row: KpiProfile) => (
        <div className="flex items-center gap-2">
          {!row.isDefault && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleSetDefault(row.id);
              }}
              title="Set as Default"
              className="text-yellow-600 hover:opacity-75 cursor-pointer transition-colors"
            >
              <Star className="h-3 w-3" />
            </button>
          )}
          <button
            onClick={(e) => {
              e.stopPropagation();
              openEditModal(row);
            }}
            title="Edit"
            className="text-blue-600 hover:opacity-75 cursor-pointer transition-colors"
          >
            <Edit className="h-3 w-3" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleDelete(row.id);
            }}
            title="Delete"
            className="text-red-600 hover:opacity-75 cursor-pointer transition-colors"
          >
            <Trash2 className="h-3 w-3" />
          </button>
        </div>
      )
    }
  ];

  if (loading) {
    return (
      <PageShell title="KPI Profiles" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'KPI Profiles' }]}>
        <LoadingSpinner message="Loading KPI profiles..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="KPI Profiles"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'KPI Profiles' }]}
      actions={
        <Button onClick={() => setShowCreateModal(true)} className="flex items-center gap-2">
          <Plus className="h-4 w-4" />
          Add KPI Profile
        </Button>
      }
    >
      <Card>
        {profiles.length > 0 ? (
          <DataTable
            data={profiles}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No KPI profiles found"
            message="Add your first KPI profile to get started."
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingProfile !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingProfile(null);
          resetForm();
        }}
        title={editingProfile ? 'Edit KPI Profile' : 'Create KPI Profile'}
        size="md"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-2">
            <TextInput
              label="Profile Name *"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="e.g., TIME Prelaid KPI"
              required
            />
            <Select
              label="Order Type *"
              value={formData.orderType}
              onChange={(e) => setFormData({ ...formData, orderType: e.target.value })}
              options={[
                { value: '', label: 'Select Order Type' },
                { value: 'Activation', label: 'Activation' },
                { value: 'Assurance', label: 'Assurance' },
                { value: 'FTTR', label: 'FTTR' },
                { value: 'FTTC', label: 'FTTC' },
                { value: 'SDU', label: 'SDU' },
                { value: 'RDFPole', label: 'RDF Pole' }
              ]}
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-2">
            <TextInput
              label="Max Job Duration (minutes) *"
              type="number"
              min="1"
              value={formData.maxJobDurationMinutes}
              onChange={(e) => setFormData({ ...formData, maxJobDurationMinutes: e.target.value })}
              required
            />
            <TextInput
              label="Docket KPI (minutes) *"
              type="number"
              min="1"
              value={formData.docketKpiMinutes}
              onChange={(e) => setFormData({ ...formData, docketKpiMinutes: e.target.value })}
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-2">
            <TextInput
              label="Max Reschedules Allowed"
              type="number"
              min="0"
              value={formData.maxReschedulesAllowed}
              onChange={(e) => setFormData({ ...formData, maxReschedulesAllowed: e.target.value })}
              placeholder="Leave empty for unlimited"
            />
            <TextInput
              label="Effective From *"
              type="date"
              value={formData.effectiveFrom}
              onChange={(e) => setFormData({ ...formData, effectiveFrom: e.target.value })}
              required
            />
          </div>

          <TextInput
            label="Effective To (Optional)"
            type="date"
            value={formData.effectiveTo}
            onChange={(e) => setFormData({ ...formData, effectiveTo: e.target.value })}
          />

          <div className="flex items-center gap-3 pt-2">
            <input
              type="checkbox"
              id="isDefault"
              checked={formData.isDefault}
              onChange={(e) => setFormData({ ...formData, isDefault: e.target.checked })}
              className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="isDefault" className="text-xs font-medium cursor-pointer">
              Set as Default Profile for this Order Type
            </label>
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t">
            <Button
              variant="outline"
              onClick={() => {
                setShowCreateModal(false);
                setEditingProfile(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={editingProfile ? handleUpdate : handleCreate}
              className="flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {editingProfile ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
    </PageShell>
  );
};

export default KpiProfilesPage;

