import React, { useState, useEffect, useRef } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Network, Building2, Lightbulb, ChevronDown, ChevronUp, Upload, AlertTriangle } from 'lucide-react';
import { getSplitters, getSplitter, createSplitter, updateSplitter, deleteSplitter, updateSplitterPort } from '../../api/splitters';
import { getBuildings } from '../../api/buildings';
import { getDepartments } from '../../api/departments';
import { uploadFile } from '../../api/files';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, Tabs, TabPanel, Select, StatusBadge, Label, Textarea } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Building } from '../../types/buildings';
import type { Department } from '../../types/departments';
import type { Splitter, SplitterPort } from '../../types/splitters';

interface SplitterWithPorts extends Splitter {
  ports?: SplitterPort[];
  totalPorts?: number;
  usedPorts?: number;
  buildingName?: string;
  splitterType?: string;
}

interface SplitterFormData {
  departmentId: string;
  buildingId: string;
  name: string;
  code: string;
  splitterType: string;
  location: string;
  block: string;
  floor: string;
  isActive: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const SplittersPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [splitters, setSplitters] = useState<SplitterWithPorts[]>([]);
  const [buildings, setBuildings] = useState<Building[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingSplitter, setEditingSplitter] = useState<SplitterWithPorts | null>(null);
  const [selectedBuilding, setSelectedBuilding] = useState<string>('');
  const [selectedSplitter, setSelectedSplitter] = useState<SplitterWithPorts | null>(null);
  const [showGuide, setShowGuide] = useState<boolean>(true);
  const [formData, setFormData] = useState<SplitterFormData>({
    departmentId: '',
    buildingId: '',
    name: '',
    code: '',
    splitterType: '1:8',
    location: '',
    block: '',
    floor: '',
    isActive: true
  });
  
  // Standby port approval state
  const [showStandbyApprovalModal, setShowStandbyApprovalModal] = useState<boolean>(false);
  const [standbyPortToApprove, setStandbyPortToApprove] = useState<SplitterPort | null>(null);
  const [standbyApprovalFile, setStandbyApprovalFile] = useState<File | null>(null);
  const [standbyApprovalNotes, setStandbyApprovalNotes] = useState<string>('');
  const [uploadingApproval, setUploadingApproval] = useState<boolean>(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    loadDepartments();
    loadAllData();
  }, [selectedBuilding]);

  const loadAllData = async (): Promise<void> => {
    try {
      setLoading(true);
      
      const [splittersResponse, buildingsResponse] = await Promise.all([
        getSplitters({ buildingId: selectedBuilding || undefined, isActive: true }).catch(() => []),
        getBuildings({ isActive: true }).catch(() => [])
      ]);

      setSplitters(Array.isArray(splittersResponse) ? splittersResponse : []);
      setBuildings(Array.isArray(buildingsResponse) ? buildingsResponse : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load data');
      console.error('Error loading data:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadDepartments = async (): Promise<void> => {
    try {
      const data = await getDepartments({});
      setDepartments(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading departments:', err);
      setDepartments([]);
    }
  };

  const loadSplitterDetails = async (splitterId: string): Promise<void> => {
    try {
      const splitter = await getSplitter(splitterId);
      if (splitter) {
        setSelectedSplitter(splitter as SplitterWithPorts);
      }
    } catch (err: any) {
      showError(err.message || 'Failed to load splitter details');
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      const splitterData: any = {
        name: formData.name,
        code: formData.code || undefined,
        buildingId: formData.buildingId,
        departmentId: formData.departmentId || undefined,
        splitterType: formData.splitterType,
        location: formData.location || undefined,
        block: formData.block || undefined,
        floor: formData.floor || undefined,
        isActive: formData.isActive
      };
      await createSplitter(splitterData);
      showSuccess('Splitter created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to create splitter');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingSplitter) return;
    try {
      const splitterData: any = {
        name: formData.name,
        code: formData.code || undefined,
        buildingId: formData.buildingId,
        departmentId: formData.departmentId || undefined,
        splitterType: formData.splitterType,
        location: formData.location || undefined,
        block: formData.block || undefined,
        floor: formData.floor || undefined,
        isActive: formData.isActive
      };
      await updateSplitter(editingSplitter.id, splitterData);
      showSuccess('Splitter updated successfully!');
      setShowCreateModal(false);
      setEditingSplitter(null);
      resetForm();
      await loadAllData();
      if (selectedSplitter?.id === editingSplitter.id) {
        setSelectedSplitter(null);
      }
    } catch (err: any) {
      showError(err.message || 'Failed to update splitter');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this splitter? All ports will be deleted as well.')) return;
    
    try {
      await deleteSplitter(id);
      showSuccess('Splitter deleted successfully!');
      loadAllData();
      if (selectedSplitter?.id === id) {
        setSelectedSplitter(null);
      }
    } catch (err: any) {
      showError(err.message || 'Failed to delete splitter');
    }
  };

  const handleToggleStatus = async (splitter: SplitterWithPorts): Promise<void> => {
    try {
      const updatedStatus = !splitter.isActive;
      await updateSplitter(splitter.id, { isActive: updatedStatus });
      showSuccess(`Splitter ${updatedStatus ? 'activated' : 'deactivated'} successfully!`);
      loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to toggle splitter status');
    }
  };

  const handlePortStatusChange = async (port: SplitterPort, newStatus: string, orderId: string | null = null): Promise<void> => {
    // Check if this is a standby port being used
    if (port.isStandby && newStatus === 'Used') {
      // Open standby approval modal
      setStandbyPortToApprove(port);
      setShowStandbyApprovalModal(true);
      return;
    }
    
    try {
      await updateSplitterPort(port.splitterId || '', port.id, {
        status: newStatus as any,
        orderId: orderId || undefined
      });
      showSuccess('Port status updated successfully!');
      if (port.splitterId) {
        await loadSplitterDetails(port.splitterId);
      }
      loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to update port status');
    }
  };

  const handleStandbyApproval = async (): Promise<void> => {
    if (!standbyPortToApprove || !standbyApprovalFile) {
      showError('Please upload an approval document');
      return;
    }

    try {
      setUploadingApproval(true);
      
      // Upload the approval file
      const uploadResult = await uploadFile(standbyApprovalFile, { 
        module: 'standby-approval',
        entityType: 'SplitterPort',
        entityId: standbyPortToApprove.id
      });
      
      if (!uploadResult?.id) {
        throw new Error('Failed to upload approval document');
      }

      // Update the port with approval
      await updateSplitterPort(standbyPortToApprove.splitterId || '', standbyPortToApprove.id, {
        status: 'Used',
        standbyOverrideApproved: true,
        approvalAttachmentId: uploadResult.id,
        notes: standbyApprovalNotes || undefined
      });

      showSuccess('Standby port approved and activated successfully!');
      
      // Reset state
      setShowStandbyApprovalModal(false);
      setStandbyPortToApprove(null);
      setStandbyApprovalFile(null);
      setStandbyApprovalNotes('');
      
      // Reload data
      if (standbyPortToApprove.splitterId) {
        await loadSplitterDetails(standbyPortToApprove.splitterId);
      }
      loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to approve standby port');
    } finally {
      setUploadingApproval(false);
    }
  };

  const handleApprovalFileChange = (e: React.ChangeEvent<HTMLInputElement>): void => {
    const file = e.target.files?.[0];
    if (file) {
      // Validate file type and size
      const allowedTypes = ['application/pdf', 'image/jpeg', 'image/png', 'image/gif'];
      const maxSize = 10 * 1024 * 1024; // 10MB
      
      if (!allowedTypes.includes(file.type)) {
        showError('Please upload a PDF or image file (JPEG, PNG, GIF)');
        return;
      }
      
      if (file.size > maxSize) {
        showError('File size must be less than 10MB');
        return;
      }
      
      setStandbyApprovalFile(file);
    }
  };

  const resetForm = (): void => {
    setFormData({
      departmentId: '',
      buildingId: '',
      name: '',
      code: '',
      splitterType: '1:8',
      location: '',
      block: '',
      floor: '',
      isActive: true
    });
  };

  const openEditModal = async (splitter: SplitterWithPorts): Promise<void> => {
    setEditingSplitter(splitter);
    setFormData({
      departmentId: splitter.departmentId || '',
      buildingId: splitter.buildingId || '',
      name: splitter.name,
      code: splitter.code || '',
      splitterType: splitter.splitterType || '1:8',
      location: splitter.location || '',
      block: (splitter as any).block || '',
      floor: (splitter as any).floor || '',
      isActive: splitter.isActive ?? true
    });
    await loadDepartments();
    setShowCreateModal(true);
  };

  const columns: TableColumn<SplitterWithPorts>[] = [
    { key: 'name', label: 'Name' },
    { key: 'code', label: 'Code' },
    { key: 'buildingName', label: 'Building' },
    { key: 'splitterType', label: 'Type' },
    { key: 'totalPorts', label: 'Total Ports' },
    { 
      key: 'usedPorts', 
      label: 'Used Ports',
      render: (value: unknown, row: SplitterWithPorts) => `${value || 0}/${row.totalPorts || 0}`
    },
    { 
      key: 'isActive', 
      label: 'Status', 
      render: (value: unknown) => (
        <StatusBadge variant={value ? 'success' : 'default'}>
          {value ? 'Active' : 'Inactive'}
        </StatusBadge>
      )
    }
  ];

  if (loading) {
    return (
      <PageShell title="Splitters" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Splitters' }]}>
        <LoadingSpinner message="Loading splitters..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Splitters"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Splitters' }]}
      actions={
        <Button size="sm" onClick={() => setShowCreateModal(true)} className="gap-1">
          <Plus className="h-4 w-4" />
          Add Splitter
        </Button>
      }
    >
      <div className="max-w-7xl mx-auto h-full flex flex-col space-y-2">
      {/* How-To Guide */}
      <Card className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30 flex-shrink-0">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-3 py-2"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-white text-sm">How Splitters Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[10px]">1</span>
                  Splitter Types
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <strong>1:8</strong> - 8 ports</li>
                  <li>• <strong>1:12</strong> - 12 ports</li>
                  <li>• <strong>1:32</strong> - 32 ports</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Port Rules
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• 1 port = 1 customer</li>
                  <li>• Used port locked forever</li>
                  <li>• Port 32 = standby (1:32)</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Job Completion
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• SI records splitter + port</li>
                  <li>• Must be validated</li>
                  <li>• No port = job blocked</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Standby Port
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Reserved for emergencies</li>
                  <li>• Needs approval to use</li>
                  <li>• Usually last port (32)</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      {/* Filters */}
      <Card className="flex-shrink-0">
        <div className="grid grid-cols-1 gap-2">
          <div>
            <label className="text-xs font-medium mb-0.5 block">Building Filter</label>
            <Select
              value={selectedBuilding}
              onChange={(e) => setSelectedBuilding(e.target.value)}
              options={[
                { value: '', label: 'All Buildings' },
                ...buildings.map(b => ({ value: b.id, label: b.name }))
              ]}
            />
          </div>
        </div>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-2 flex-1 min-h-0">
        {/* Splitters List */}
        <Card className="flex flex-col min-h-0">
          <h2 className="text-xs font-semibold mb-2">Splitters List</h2>
          <div className="flex-1 overflow-y-auto">
            {splitters.length > 0 ? (
              <DataTable
                data={splitters}
                columns={columns}
                actions={(row: SplitterWithPorts) => (
                  <div className="flex items-center gap-2">
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        handleToggleStatus(row);
                      }}
                      title={row.isActive ? 'Deactivate' : 'Activate'}
                      className={`${row.isActive ? 'text-yellow-600' : 'text-green-600'} hover:opacity-75 cursor-pointer transition-colors`}
                    >
                      <Power className="h-3 w-3" />
                    </button>
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        openEditModal(row);
                        loadSplitterDetails(row.id);
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
                )}
              />
            ) : (
              <EmptyState
                title="No splitters found"
                message="Add your first splitter to get started."
              />
            )}
          </div>
        </Card>

        {/* Port Management */}
        {selectedSplitter && (
          <Card className="flex flex-col min-h-0 lg:col-span-2">
            <div className="flex items-center justify-between mb-2">
              <div>
                <h2 className="text-xs font-semibold">{selectedSplitter.name}</h2>
                <p className="text-xs text-muted-foreground">
                  {selectedSplitter.splitterType} - {selectedSplitter.buildingName}
                </p>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSelectedSplitter(null)}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <div className="flex-1 overflow-y-auto">
              <div className="grid grid-cols-8 gap-2">
                {selectedSplitter.ports && selectedSplitter.ports.length > 0 ? (
                  selectedSplitter.ports.map((port) => (
                    <div
                      key={port.id}
                      className={`p-3 rounded-lg border-2 text-center ${
                        port.status === 'Occupied' || port.status === 'Used'
                          ? 'border-red-500 bg-red-50 dark:bg-red-900/20'
                          : port.status === 'Reserved'
                          ? 'border-yellow-500 bg-yellow-50 dark:bg-yellow-900/20'
                          : port.status === 'Maintenance'
                          ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
                          : port.isStandby || port.status === 'Standby'
                          ? 'border-orange-500 bg-orange-50 dark:bg-orange-900/20'
                          : 'border-green-500 bg-green-50 dark:bg-green-900/20'
                      }`}
                    >
                      <div className="font-semibold text-sm flex items-center justify-center gap-1">
                        Port {port.portNumber}
                        {port.isStandby && (
                          <AlertTriangle className="h-3 w-3 text-orange-500" title="Standby Port" />
                        )}
                      </div>
                      <div className="text-xs mt-1">
                        {port.isStandby ? 'Standby' : port.status}
                        {port.standbyOverrideApproved && (
                          <span className="ml-1 text-green-600">(Approved)</span>
                        )}
                      </div>
                      <div className="mt-2 flex gap-1 justify-center">
                        {(port.status === 'Available' || (port.isStandby && port.status === 'Standby')) && (
                          <Button
                            variant="outline"
                            size="sm"
                            className={`text-xs px-2 py-1 ${port.isStandby ? 'border-orange-500 text-orange-600 hover:bg-orange-50' : ''}`}
                            onClick={() => handlePortStatusChange(port, port.isStandby ? 'Used' : 'Occupied')}
                          >
                            {port.isStandby ? 'Request Use' : 'Use'}
                          </Button>
                        )}
                        {(port.status === 'Occupied' || port.status === 'Used') && !port.isStandby && (
                          <Button
                            variant="outline"
                            size="sm"
                            className="text-xs px-2 py-1"
                            onClick={() => handlePortStatusChange(port, 'Available', null)}
                          >
                            Free
                          </Button>
                        )}
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="col-span-8">
                    <EmptyState
                      title="No ports found"
                      message="Ports should be automatically created when splitter is created."
                    />
                  </div>
                )}
              </div>
            </div>
          </Card>
        )}
      </div>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingSplitter !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingSplitter(null);
          resetForm();
        }}
        title={editingSplitter ? 'Edit Splitter' : 'Create Splitter'}
        size="md"
      >
        <div className="space-y-2">
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
            label="Building *"
            value={formData.buildingId}
            onChange={(e) => setFormData({ ...formData, buildingId: e.target.value })}
            options={[
              { value: '', label: 'Select Building' },
              ...buildings.map(b => ({ value: b.id, label: b.name }))
            ]}
            required
          />

          <div className="grid grid-cols-2 gap-2">
            <TextInput
              label="Name *"
              name="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
            />
            <TextInput
              label="Code"
              name="code"
              value={formData.code}
              onChange={(e) => setFormData({ ...formData, code: e.target.value })}
            />
          </div>

          <Select
            label="Splitter Type *"
            value={formData.splitterType}
            onChange={(e) => setFormData({ ...formData, splitterType: e.target.value })}
            options={[
              { value: '1:8', label: '1:8 (8 ports)' },
              { value: '1:12', label: '1:12 (12 ports)' },
              { value: '1:32', label: '1:32 (32 ports)' }
            ]}
            required
          />

          <TextInput
            label="Location"
            name="location"
            value={formData.location}
            onChange={(e) => setFormData({ ...formData, location: e.target.value })}
            placeholder="e.g., MDF, Riser A"
          />
          
          <div className="grid grid-cols-2 gap-2">
            <TextInput
              label="Block"
              name="block"
              value={formData.block}
              onChange={(e) => setFormData({ ...formData, block: e.target.value })}
              placeholder="e.g., Block A, Block B"
            />
            <TextInput
              label="Floor"
              name="floor"
              value={formData.floor}
              onChange={(e) => setFormData({ ...formData, floor: e.target.value })}
              placeholder="e.g., Floor 1, Ground Floor"
            />
          </div>

          <div className="flex items-center gap-3 pt-2">
            <input
              type="checkbox"
              id="isActive"
              checked={formData.isActive}
              onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
              className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="isActive" className="text-xs font-medium cursor-pointer">
              Active Status
            </label>
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t">
            <Button
              variant="outline"
              onClick={() => {
                setShowCreateModal(false);
                setEditingSplitter(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={editingSplitter ? handleUpdate : handleCreate}
              className="flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {editingSplitter ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Standby Port Approval Modal */}
      <Modal
        isOpen={showStandbyApprovalModal}
        onClose={() => {
          setShowStandbyApprovalModal(false);
          setStandbyPortToApprove(null);
          setStandbyApprovalFile(null);
          setStandbyApprovalNotes('');
        }}
        title="Standby Port Approval Required"
        size="md"
      >
        <div className="space-y-4">
          <div className="p-3 bg-orange-50 border border-orange-200 rounded-md dark:bg-orange-900/20 dark:border-orange-700">
            <div className="flex items-start gap-2">
              <AlertTriangle className="h-5 w-5 text-orange-500 flex-shrink-0 mt-0.5" />
              <div>
                <h4 className="text-sm font-medium text-orange-800 dark:text-orange-200">
                  Standby Port Usage Requires Approval
                </h4>
                <p className="text-xs text-orange-700 dark:text-orange-300 mt-1">
                  Port {standbyPortToApprove?.portNumber} is a standby port reserved for emergencies. 
                  Using this port requires documented approval from management.
                </p>
              </div>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="approval-file">Approval Document *</Label>
            <p className="text-xs text-muted-foreground">
              Upload approval evidence (email screenshot, signed form, or management approval)
            </p>
            <div className="flex items-center gap-2">
              <input
                ref={fileInputRef}
                type="file"
                id="approval-file"
                accept=".pdf,.jpg,.jpeg,.png,.gif"
                onChange={handleApprovalFileChange}
                className="hidden"
              />
              <Button
                variant="outline"
                onClick={() => fileInputRef.current?.click()}
                className="flex items-center gap-2"
              >
                <Upload className="h-4 w-4" />
                {standbyApprovalFile ? 'Change File' : 'Upload File'}
              </Button>
              {standbyApprovalFile && (
                <span className="text-sm text-muted-foreground truncate max-w-[200px]">
                  {standbyApprovalFile.name}
                </span>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="approval-notes">Notes (Optional)</Label>
            <Textarea
              id="approval-notes"
              value={standbyApprovalNotes}
              onChange={(e) => setStandbyApprovalNotes(e.target.value)}
              placeholder="Add any relevant notes about this standby port usage..."
              rows={3}
            />
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t">
            <Button
              variant="outline"
              onClick={() => {
                setShowStandbyApprovalModal(false);
                setStandbyPortToApprove(null);
                setStandbyApprovalFile(null);
                setStandbyApprovalNotes('');
              }}
              disabled={uploadingApproval}
            >
              Cancel
            </Button>
            <Button
              onClick={handleStandbyApproval}
              disabled={!standbyApprovalFile || uploadingApproval}
              className="flex items-center gap-2"
            >
              {uploadingApproval ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                  Uploading...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4" />
                  Approve & Activate Port
                </>
              )}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default SplittersPage;

