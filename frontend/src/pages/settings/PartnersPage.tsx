import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Lightbulb, ChevronDown, ChevronUp, Users, Search, Download } from 'lucide-react';
import { getPartners, createPartner, updatePartner, deletePartner } from '../../api/partners';
import { getDepartments } from '../../api/departments';
import { usePartnerGroups } from '../../hooks/usePartnerGroups';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, Select } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { StatusBadge } from '../../components/ui/StatusDropdown';
import { SortableTableHeader, sortData, useSortState, type SortConfig, type ColumnDef } from '../../components/ui/SortableHeader';
import { exportPartnersToExcel } from '../../utils/excelExport';
import { getBooleanStatusColor } from '../../utils/statusColors';
import type { Partner, CreatePartnerRequest, UpdatePartnerRequest } from '../../types/partners';
import type { Department } from '../../types/departments';

interface PartnerFormData {
  departmentId: string;
  groupId: string;
  name: string;
  partnerType: string;
  billingAddress: string;
  contactName: string;
  contactEmail: string;
  contactPhone: string;
  isActive: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const PartnersPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [partners, setPartners] = useState<Partner[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const { data: partnerGroups = [] } = usePartnerGroups();
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingPartner, setEditingPartner] = useState<Partner | null>(null);
  const [showGuide, setShowGuide] = useState<boolean>(false);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [sortConfig, handleSort] = useSortState({ key: 'name', direction: 'asc' });
  const [formData, setFormData] = useState<PartnerFormData>({
    departmentId: '',
    groupId: '',
    name: '',
    partnerType: 'Telco',
    billingAddress: '',
    contactName: '',
    contactEmail: '',
    contactPhone: '',
    isActive: true
  });

  useEffect(() => {
    loadPartners();
  }, []);

  useEffect(() => {
    loadDepartments();
  }, []);

  // Filtered and sorted partners
  const filteredPartners = useMemo(() => {
    let result = partners;

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(p =>
        p.name?.toLowerCase().includes(query) ||
        p.contactName?.toLowerCase().includes(query) ||
        p.contactEmail?.toLowerCase().includes(query) ||
        p.contactPhone?.toLowerCase().includes(query) ||
        p.partnerType?.toLowerCase().includes(query)
      );
    }

    // Apply status filter
    if (statusFilter !== 'all') {
      result = result.filter(p => 
        statusFilter === 'active' ? p.isActive : !p.isActive
      );
    }

    // Apply sorting
    return sortData(result, sortConfig);
  }, [partners, searchQuery, statusFilter, sortConfig]);

  const loadPartners = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getPartners();
      setPartners(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load partners');
      console.error('Error loading partners:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadDepartments = async (): Promise<void> => {
    try {
      const data = await getDepartments();
      setDepartments(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Error loading departments:', err);
      setDepartments([]);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      const partnerData: CreatePartnerRequest = {
        name: formData.name.trim(),
        partnerType: formData.partnerType as any,
        billingAddress: formData.billingAddress?.trim() || undefined,
        contactName: formData.contactName?.trim() || undefined,
        contactEmail: formData.contactEmail?.trim() || undefined,
        contactPhone: formData.contactPhone?.trim() || undefined,
        isActive: formData.isActive
      };
      
      if (formData.departmentId) {
        (partnerData as any).departmentId = formData.departmentId;
      }
      
      if (formData.groupId) {
        (partnerData as any).groupId = formData.groupId;
      }
      
      await createPartner(partnerData);
      showSuccess('Partner created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadPartners();
    } catch (err) {
      showError((err as Error).message || 'Failed to create partner');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingPartner) return;
    
    try {
      const partnerData: UpdatePartnerRequest = {
        name: formData.name.trim(),
        partnerType: formData.partnerType as any,
        billingAddress: formData.billingAddress?.trim() || undefined,
        contactName: formData.contactName?.trim() || undefined,
        contactEmail: formData.contactEmail?.trim() || undefined,
        contactPhone: formData.contactPhone?.trim() || undefined,
        isActive: formData.isActive
      };
      
      if (formData.departmentId) {
        (partnerData as any).departmentId = formData.departmentId;
      }
      
      if (formData.groupId) {
        (partnerData as any).groupId = formData.groupId;
      }
      
      await updatePartner(editingPartner.id, partnerData);
      showSuccess('Partner updated successfully!');
      setShowCreateModal(false);
      setEditingPartner(null);
      resetForm();
      await loadPartners();
    } catch (err) {
      showError((err as Error).message || 'Failed to update partner');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this partner?')) return;
    
    try {
      await deletePartner(id);
      showSuccess('Partner deleted successfully!');
      await loadPartners();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete partner');
    }
  };

  const handleToggleStatus = async (partner: Partner): Promise<void> => {
    try {
      const partnerData: UpdatePartnerRequest = {
        name: partner.name,
        partnerType: partner.partnerType as any,
        billingAddress: partner.billingAddress || undefined,
        contactName: partner.contactName || undefined,
        contactEmail: partner.contactEmail || undefined,
        contactPhone: partner.contactPhone || undefined,
        isActive: !partner.isActive
      };
      
      await updatePartner(partner.id, partnerData);
      showSuccess(`Partner ${!partner.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadPartners();
    } catch (err) {
      showError((err as Error).message || 'Failed to update partner status');
    }
  };

  const handleExport = (): void => {
    exportPartnersToExcel(filteredPartners);
    showSuccess('Partners exported successfully!');
  };

  const resetForm = (): void => {
    setFormData({
      departmentId: '',
      groupId: '',
      name: '',
      partnerType: 'Telco',
      billingAddress: '',
      contactName: '',
      contactEmail: '',
      contactPhone: '',
      isActive: true
    });
  };

  const openEditModal = async (partner: Partner): Promise<void> => {
    setEditingPartner(partner);
    setFormData({
      departmentId: partner.departmentId || '',
      groupId: partner.groupId || '',
      name: partner.name,
      partnerType: partner.partnerType || 'Telco',
      billingAddress: partner.billingAddress || '',
      contactName: partner.contactName || '',
      contactEmail: partner.contactEmail || '',
      contactPhone: partner.contactPhone || '',
      isActive: partner.isActive
    });
    await loadDepartments();
    setShowCreateModal(true);
  };

  const columns: TableColumn<Partner>[] = [
    { key: 'name', label: 'Name' },
    { 
      key: 'partnerType', 
      label: 'Type',
      render: (value) => (
        <span className="px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-800 border border-blue-300">
          {value as string || '-'}
        </span>
      )
    },
    { 
      key: 'groupId', 
      label: 'Group',
      render: (value) => {
        if (!value) return <span className="text-gray-400 text-xs">—</span>;
        const group = partnerGroups.find(g => g.id === value);
        return group ? (
          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs bg-purple-100 text-purple-800 border border-purple-300">
            <Users className="h-3 w-3" />
            {group.name}
          </span>
        ) : <span className="text-gray-400 text-xs">—</span>;
      }
    },
    { key: 'contactName', label: 'Contact' },
    { key: 'contactPhone', label: 'Phone' },
    { 
      key: 'isActive', 
      label: 'Status', 
      render: (value) => (
        <span className={`px-2 py-1 rounded text-xs font-medium border ${getBooleanStatusColor(value as boolean)}`}>
          {value ? 'Active' : 'Inactive'}
        </span>
      )
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_value, row) => (
        <div className="flex items-center gap-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleToggleStatus(row);
            }}
            title={row.isActive ? 'Deactivate' : 'Activate'}
            className={`p-1 rounded hover:bg-muted transition-colors ${row.isActive ? 'text-yellow-600 hover:text-yellow-700' : 'text-green-600 hover:text-green-700'}`}
          >
            <Power className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              openEditModal(row);
            }}
            title="Edit"
            className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
          >
            <Edit className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleDelete(row.id);
            }}
            title="Delete"
            className="p-1 rounded text-red-600 hover:text-red-700 hover:bg-muted transition-colors"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      )
    }
  ];

  if (loading) {
    return <LoadingSpinner message="Loading partners..." fullPage />;
  }

  return (
    <PageShell
      title="Partners"
      breadcrumbs={[{ label: 'Settings' }, { label: 'Partners' }]}
      actions={
        <Button size="sm" onClick={() => setShowCreateModal(true)} className="gap-1">
          <Plus className="h-4 w-4" />
          Add Partner
        </Button>
      }
    >
    <div className="flex-1 p-4 max-w-7xl mx-auto space-y-4">
      {/* How-To Guide */}
      <Card className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-4 py-3"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-white text-sm">How Partners Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-4 pb-4">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-blue-500 rounded-full flex items-center justify-center text-xs">1</span>
                  Purpose
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• Define who pays us</li>
                  <li>• Billing relationships</li>
                  <li>• Order sources</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-green-500 rounded-full flex items-center justify-center text-xs">2</span>
                  Partner Types
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• <strong>Telco</strong> - TM, Maxis</li>
                  <li>• <strong>Customer</strong> - Direct</li>
                  <li>• <strong>Vendor</strong> - Suppliers</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-purple-500 rounded-full flex items-center justify-center text-xs">3</span>
                  Rate Cards
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• Link to Partner Rates</li>
                  <li>• PU pricing</li>
                  <li>• Invoice generation</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-orange-500 rounded-full flex items-center justify-center text-xs">4</span>
                  Contact Info
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• Billing address</li>
                  <li>• Contact person</li>
                  <li>• Communication</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      {/* Filters */}
      <Card className="p-4">
        <div className="flex flex-wrap items-center gap-4">
          {/* Search */}
          <div className="flex-1 min-w-[200px]">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search partners..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          {/* Status Filter */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Status:</span>
            <div className="flex gap-1">
              <button
                onClick={() => setStatusFilter('all')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'all'
                    ? 'bg-gray-600 text-white border-gray-700'
                    : 'bg-gray-100 text-gray-700 border-gray-300 hover:bg-gray-200'
                }`}
              >
                All
              </button>
              <button
                onClick={() => setStatusFilter('active')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'active'
                    ? 'bg-green-600 text-white border-green-700'
                    : 'bg-green-100 text-green-700 border-green-300 hover:bg-green-200'
                }`}
              >
                Active
              </button>
              <button
                onClick={() => setStatusFilter('inactive')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'inactive'
                    ? 'bg-gray-600 text-white border-gray-700'
                    : 'bg-gray-100 text-gray-600 border-gray-300 hover:bg-gray-200'
                }`}
              >
                Inactive
              </button>
            </div>
          </div>
        </div>
      </Card>

      {/* Data Table */}
      <Card>
        {filteredPartners.length > 0 ? (
          <DataTable
            data={filteredPartners}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No partners found"
            message={searchQuery || statusFilter !== 'all' 
              ? "No partners match your search criteria."
              : "Add your first partner to get started."}
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingPartner !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingPartner(null);
          resetForm();
        }}
      >
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full">
          <div className="flex items-center justify-between p-4 border-b">
            <h2 className="text-lg font-bold">
              {editingPartner ? 'Edit Partner' : 'Create Partner'}
            </h2>
            <button
              onClick={() => {
                setShowCreateModal(false);
                setEditingPartner(null);
                resetForm();
              }}
              className="text-gray-400 hover:text-gray-600"
            >
              <X className="h-6 w-6" />
            </button>
          </div>

          <div className="p-4 space-y-4">
            <Select
              label="Department (Optional)"
              name="departmentId"
              value={formData.departmentId}
              onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
              options={[
                { value: '', label: 'No Department' },
                ...departments.map(d => ({ 
                  value: d.id, 
                  label: d.name 
                }))
              ]}
            />

            <Select
              label="Partner Group (Optional)"
              name="groupId"
              value={formData.groupId}
              onChange={(e) => setFormData({ ...formData, groupId: e.target.value })}
              options={[
                { value: '', label: 'No Group' },
                ...partnerGroups.map(g => ({ 
                  value: g.id, 
                  label: g.name 
                }))
              ]}
            />
            
            <TextInput
              label="Partner Name *"
              name="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
            />

            <div className="space-y-2">
              <label className="text-sm font-medium">Partner Type *</label>
              <select
                name="partnerType"
                value={formData.partnerType}
                onChange={(e) => setFormData({ ...formData, partnerType: e.target.value })}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value="Telco">Telco</option>
                <option value="Customer">Customer</option>
                <option value="Vendor">Vendor</option>
                <option value="Landlord">Landlord</option>
              </select>
            </div>

            <TextInput
              label="Billing Address"
              name="billingAddress"
              value={formData.billingAddress}
              onChange={(e) => setFormData({ ...formData, billingAddress: e.target.value })}
            />

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Contact Name"
                name="contactName"
                value={formData.contactName}
                onChange={(e) => setFormData({ ...formData, contactName: e.target.value })}
              />
              <TextInput
                label="Contact Phone"
                name="contactPhone"
                value={formData.contactPhone}
                onChange={(e) => setFormData({ ...formData, contactPhone: e.target.value })}
              />
            </div>

            <TextInput
              label="Contact Email"
              name="contactEmail"
              type="email"
              value={formData.contactEmail}
              onChange={(e) => setFormData({ ...formData, contactEmail: e.target.value })}
            />

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="isActive"
                checked={formData.isActive}
                onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                className="h-4 w-4"
              />
              <label htmlFor="isActive" className="text-sm font-medium">
                Active
              </label>
            </div>

            <div className="flex justify-end gap-3 pt-4 border-t">
              <Button
                variant="outline"
                onClick={() => {
                  setShowCreateModal(false);
                  setEditingPartner(null);
                  resetForm();
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={editingPartner ? handleUpdate : handleCreate}
                className="flex items-center gap-2"
              >
                <Save className="h-4 w-4" />
                {editingPartner ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default PartnersPage;
