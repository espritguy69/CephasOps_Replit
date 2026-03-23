import React, { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Edit, Trash2, Save, X, Power, Lightbulb, ChevronDown, ChevronUp, Search, Download, Merge } from 'lucide-react';
import { getBuildings, createBuilding, updateBuilding, deleteBuilding } from '../../api/buildings';
import { getDepartments } from '../../api/departments';
import { getInstallationMethods } from '../../api/installationMethods';
import { getBuildingTypes } from '../../api/buildingTypes';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, Select, StatusBadge } from '../../components/ui';
import type { InstallationMethod } from '../../types/installationMethods';
import type { ReferenceDataItem } from '../../types/referenceData';
import { sortData, useSortState } from '../../components/ui/SortableHeader';
import { exportBuildingsToExcel } from '../../utils/excelExport';
import { getBuildingTypeBadgeColor } from '../../utils/statusColors';
import { PageShell } from '../../components/layout';
import type { Building, CreateBuildingRequest, UpdateBuildingRequest } from '../../types/buildings';
import type { Department } from '../../types/departments';

interface BuildingFormData {
  departmentId: string;
  name: string;
  code: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  postcode: string;
  latitude: string;
  longitude: string;
  buildingType: string;
  buildingTypeId: string;
  installationMethodId: string;
  isActive: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const BuildingsPage: React.FC = () => {
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const [buildings, setBuildings] = useState<Building[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [buildingTypes, setBuildingTypes] = useState<ReferenceDataItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingBuilding, setEditingBuilding] = useState<Building | null>(null);
  const [showGuide, setShowGuide] = useState<boolean>(false);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [sortConfig, handleSort] = useSortState({ key: 'name', direction: 'asc' });
  const [formData, setFormData] = useState<BuildingFormData>({
    departmentId: '',
    name: '',
    code: '',
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    postcode: '',
    latitude: '',
    longitude: '',
      buildingType: '',
      buildingTypeId: '',
    installationMethodId: '',
    isActive: true
  });

  useEffect(() => {
    loadBuildings();
  }, []);

  useEffect(() => {
    loadDepartments();
    loadInstallationMethods();
    loadBuildingTypes();
  }, []);

  const loadInstallationMethods = async (): Promise<void> => {
    try {
      const data = await getInstallationMethods({ isActive: true });
      setInstallationMethods(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading installation methods:', err);
      setInstallationMethods([]);
    }
  };

  const loadBuildingTypes = async (): Promise<void> => {
    try {
      const data = await getBuildingTypes({ isActive: true });
      setBuildingTypes(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading building types:', err);
      setBuildingTypes([]);
    }
  };

  // Filtered and sorted buildings
  const filteredBuildings = useMemo(() => {
    let result = buildings;

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(b =>
        b.name?.toLowerCase().includes(query) ||
        b.code?.toLowerCase().includes(query) ||
        b.city?.toLowerCase().includes(query) ||
        b.state?.toLowerCase().includes(query) ||
        b.postcode?.toLowerCase().includes(query) ||
        b.addressLine1?.toLowerCase().includes(query)
      );
    }

    // Apply status filter
    if (statusFilter !== 'all') {
      result = result.filter(b => 
        statusFilter === 'active' ? b.isActive : !b.isActive
      );
    }

    // Apply type filter (building classification from API or legacy propertyType)
    if (typeFilter !== 'all') {
      result = result.filter(b =>
        b.buildingTypeName === typeFilter || (b as any).propertyType === typeFilter
      );
    }

    // Apply sorting
    return sortData(result, sortConfig);
  }, [buildings, searchQuery, statusFilter, typeFilter, sortConfig]);

  const loadBuildings = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getBuildings();
      setBuildings(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load buildings');
      console.error('Error loading buildings:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadDepartments = async (): Promise<void> => {
    try {
      const data = await getDepartments();
      setDepartments(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading departments:', err);
      setDepartments([]);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      const buildingData: CreateBuildingRequest = {
        name: formData.name,
        code: formData.code || undefined,
        propertyType: 'Other' as any, // Deprecated - kept for backward compatibility
        addressLine1: formData.addressLine1,
        addressLine2: formData.addressLine2 || undefined,
        city: formData.city,
        state: formData.state,
        postcode: formData.postcode,
        departmentId: formData.departmentId || undefined,
        buildingTypeId: formData.buildingTypeId || undefined,
        installationMethodId: formData.installationMethodId || undefined,
        isActive: formData.isActive
      };
      await createBuilding(buildingData);
      showSuccess('Building created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadBuildings();
    } catch (err: any) {
      showError(err.message || 'Failed to create building');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingBuilding) return;
    try {
      const buildingData: UpdateBuildingRequest = {
        name: formData.name,
        code: formData.code || undefined,
        // propertyType is deprecated - not updating it
        addressLine1: formData.addressLine1,
        addressLine2: formData.addressLine2 || undefined,
        city: formData.city,
        state: formData.state,
        postcode: formData.postcode,
        departmentId: formData.departmentId || undefined,
        buildingTypeId: formData.buildingTypeId || undefined,
        installationMethodId: formData.installationMethodId || undefined,
        isActive: formData.isActive
      };
      await updateBuilding(editingBuilding.id, buildingData);
      showSuccess('Building updated successfully!');
      setShowCreateModal(false);
      setEditingBuilding(null);
      resetForm();
      await loadBuildings();
    } catch (err: any) {
      showError(err.message || 'Failed to update building');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this building?')) return;
    
    try {
      await deleteBuilding(id);
      showSuccess('Building deleted successfully!');
      loadBuildings();
    } catch (err: any) {
      showError(err.message || 'Failed to delete building');
    }
  };

  const handleToggleStatus = async (building: Building): Promise<void> => {
    try {
      const buildingData: UpdateBuildingRequest = {
        isActive: !building.isActive
      };
      await updateBuilding(building.id, buildingData);
      showSuccess(`Building ${!building.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadBuildings();
    } catch (err: any) {
      showError(err.message || 'Failed to update building status');
    }
  };

  const handleExport = (): void => {
    exportBuildingsToExcel(filteredBuildings);
    showSuccess('Buildings exported successfully!');
  };

  const resetForm = (): void => {
    setFormData({
      departmentId: '',
      name: '',
      code: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      postcode: '',
      latitude: '',
      longitude: '',
      buildingType: '',
      buildingTypeId: '',
      installationMethodId: '',
      isActive: true
    });
  };

  const openEditModal = async (building: Building): Promise<void> => {
    setEditingBuilding(building);
    setFormData({
      departmentId: building.departmentId || '',
      name: building.name,
      code: building.code || '',
      addressLine1: building.addressLine1,
      addressLine2: building.addressLine2 || '',
      city: building.city,
      state: building.state,
      postcode: building.postcode,
      latitude: (building as any).latitude?.toString() || '',
      longitude: (building as any).longitude?.toString() || '',
      buildingType: '', // Deprecated - use buildingTypeId instead
      buildingTypeId: building.buildingTypeId || '',
      installationMethodId: building.installationMethodId || '',
      isActive: building.isActive
    });
    await loadDepartments();
    setShowCreateModal(true);
  };

  // Get building type for display - prioritize BuildingTypeName over PropertyType
  const getBuildingType = (building: Building): string => {
    // First try buildingTypeName (from BuildingType entity)
    if (building.buildingTypeName) {
      return building.buildingTypeName;
    }
    // Fall back to propertyType (deprecated, kept for backward compatibility)
    if (building.propertyType) {
      return building.propertyType;
    }
    return '-';
  };

  const columns: TableColumn<Building>[] = [
    { key: 'name', label: 'Name' },
    { key: 'code', label: 'Code' },
    { 
      key: 'buildingType', 
      label: 'Type', 
      render: (_value: unknown, row: Building) => {
        const type = getBuildingType(row);
        return (
          <StatusBadge size="sm" className={getBuildingTypeBadgeColor(type)}>
            {type}
          </StatusBadge>
        );
      }
    },
    { key: 'city', label: 'City' },
    { key: 'state', label: 'State' },
    { 
      key: 'isActive', 
      label: 'Status', 
      render: (value: unknown) => (
        <StatusBadge variant={(value as boolean) ? 'success' : 'secondary'} size="sm">
          {(value as boolean) ? 'Active' : 'Inactive'}
        </StatusBadge>
      )
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_value: unknown, row: Building) => (
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
    return (
      <PageShell title="Buildings">
        <LoadingSpinner message="Loading buildings..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Buildings"
      actions={
        <>
          <span className="px-2 py-1 bg-muted rounded text-xs text-muted-foreground">
            {filteredBuildings.length} of {buildings.length}
          </span>
          <Button variant="outline" onClick={() => navigate('/settings/buildings-merge')} className="flex items-center gap-2">
            <Merge className="h-4 w-4" />
            Merge duplicates
          </Button>
          <Button variant="outline" onClick={handleExport} className="flex items-center gap-2">
            <Download className="h-4 w-4" />
            Export
          </Button>
          <Button onClick={() => setShowCreateModal(true)} className="flex items-center gap-2">
            <Plus className="h-4 w-4" />
            Add Building
          </Button>
        </>
      }
    >
      {/* How-To Guide */}
      <Card className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-4 py-3"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-white text-sm">How Buildings Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-4 pb-4">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-blue-500 rounded-full flex items-center justify-center text-xs">1</span>
                  Building Types
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• <strong>Prelaid:</strong> Pre-wired FTTH</li>
                  <li>• <strong>Non-Prelaid:</strong> New cabling</li>
                  <li>• <strong>SDU:</strong> Single dwelling</li>
                  <li>• <strong>RDF Pole:</strong> Pole deploy</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-green-500 rounded-full flex items-center justify-center text-xs">2</span>
                  Usage
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• Order assignment</li>
                  <li>• Splitter location</li>
                  <li>• Billing rate lookup</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-purple-500 rounded-full flex items-center justify-center text-xs">3</span>
                  Splitters
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• Each building has splitters</li>
                  <li>• Ports track customer usage</li>
                  <li>• Port 32 = standby (1:32)</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-orange-500 rounded-full flex items-center justify-center text-xs">4</span>
                  Billing Impact
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• Type affects rate</li>
                  <li>• Prelaid = standard rate</li>
                  <li>• Non-Prelaid = higher rate</li>
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
                placeholder="Search by name, code, city, state..."
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

          {/* Building Type Filter (classification from API) */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Building type:</span>
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="flex h-8 min-w-[140px] rounded border border-input bg-background px-2 py-1 text-sm"
            >
              <option value="all">All</option>
              {buildingTypes.map((type) => (
                <option key={type.id} value={type.name}>{type.name}</option>
              ))}
            </select>
          </div>
        </div>
      </Card>

      {/* Data Table */}
      <Card>
        {filteredBuildings.length > 0 ? (
          <DataTable
            data={filteredBuildings}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No buildings found"
            message={searchQuery || statusFilter !== 'all' || typeFilter !== 'all'
              ? "No buildings match your search criteria."
              : "Add your first building to get started."}
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingBuilding !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingBuilding(null);
          resetForm();
        }}
        title={editingBuilding ? 'Edit Building' : 'Create Building'}
        size="lg"
      >
        <div className="space-y-4">
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
          
          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Building Name *"
              name="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
            />
            <TextInput
              label="Building Code"
              name="code"
              value={formData.code}
              onChange={(e) => setFormData({ ...formData, code: e.target.value })}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Building Type (Optional)</label>
              <p className="text-xs text-muted-foreground mb-1">
                Select the building classification (e.g., Condominium, Office Tower, Terrace House)
              </p>
              <select
                name="buildingTypeId"
                value={formData.buildingTypeId}
                onChange={(e) => setFormData({ ...formData, buildingTypeId: e.target.value })}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value="">Select Building Type</option>
                {buildingTypes.map(type => (
                  <option key={type.id} value={type.id}>{type.name}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">Installation Method (Optional)</label>
            <select
              name="installationMethodId"
              value={formData.installationMethodId}
              onChange={(e) => setFormData({ ...formData, installationMethodId: e.target.value })}
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            >
              <option value="">Select Installation Method</option>
              {installationMethods.map(method => (
                <option key={method.id} value={method.id}>{method.name}</option>
              ))}
            </select>
          </div>

          <TextInput
            label="Address Line 1 *"
            name="addressLine1"
            value={formData.addressLine1}
            onChange={(e) => setFormData({ ...formData, addressLine1: e.target.value })}
            required
          />

          <TextInput
            label="Address Line 2"
            name="addressLine2"
            value={formData.addressLine2}
            onChange={(e) => setFormData({ ...formData, addressLine2: e.target.value })}
          />

          <div className="grid grid-cols-3 gap-4">
            <TextInput
              label="City *"
              name="city"
              value={formData.city}
              onChange={(e) => setFormData({ ...formData, city: e.target.value })}
              required
            />
            <TextInput
              label="State *"
              name="state"
              value={formData.state}
              onChange={(e) => setFormData({ ...formData, state: e.target.value })}
              required
            />
            <TextInput
              label="Postcode *"
              name="postcode"
              value={formData.postcode}
              onChange={(e) => setFormData({ ...formData, postcode: e.target.value })}
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Latitude"
              name="latitude"
              type="number"
              step="any"
              value={formData.latitude}
              onChange={(e) => setFormData({ ...formData, latitude: e.target.value })}
            />
            <TextInput
              label="Longitude"
              name="longitude"
              type="number"
              step="any"
              value={formData.longitude}
              onChange={(e) => setFormData({ ...formData, longitude: e.target.value })}
            />
          </div>

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
                setEditingBuilding(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={editingBuilding ? handleUpdate : handleCreate}
              className="flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {editingBuilding ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
    </PageShell>
  );
};

export default BuildingsPage;
