import React, { useState, useEffect, useCallback } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Link2 } from 'lucide-react';
import {
  getRateGroups,
  createRateGroup,
  updateRateGroup,
  deleteRateGroup,
  getRateGroupMappings,
  assignRateGroupToOrderTypeSubtype,
  unassignRateGroupMapping,
  getBaseWorkRates,
  createBaseWorkRate,
  updateBaseWorkRate,
  deleteBaseWorkRate,
} from '../../api/rateGroups';
import { getOrderTypeParents, getOrderTypeSubtypes, getOrderTypes } from '../../api/orderTypes';
import { getOrderCategories } from '../../api/orderCategories';
import { getServiceProfiles } from '../../api/serviceProfiles';
import { getInstallationMethods } from '../../api/installationMethods';
import type { RateGroupDto, OrderTypeSubtypeRateGroupMappingDto, BaseWorkRateDto, CreateBaseWorkRateRequest, UpdateBaseWorkRateRequest, BaseWorkRateListFilter } from '../../types/rateGroups';
import type { OrderTypeDto } from '../../types/orderTypes';
import type { ReferenceDataItem } from '../../types/referenceData';
import type { InstallationMethod } from '../../types/installationMethods';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, Select, ConfirmDialog } from '../../components/ui';
import { PageShell } from '../../components/layout';

interface RateGroupFormData {
  name: string;
  code: string;
  description: string;
  displayOrder: number | string;
  isActive: boolean;
}

interface MappingFormData {
  orderTypeId: string;
  orderSubtypeId: string;
  rateGroupId: string;
}

const RateGroupsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [rateGroups, setRateGroups] = useState<RateGroupDto[]>([]);
  const [mappings, setMappings] = useState<OrderTypeSubtypeRateGroupMappingDto[]>([]);
  const [parents, setParents] = useState<OrderTypeDto[]>([]);
  const [subtypes, setSubtypes] = useState<OrderTypeDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMappings, setLoadingMappings] = useState(false);
  const [showRateGroupModal, setShowRateGroupModal] = useState(false);
  const [editingRateGroup, setEditingRateGroup] = useState<RateGroupDto | null>(null);
  const [showMappingModal, setShowMappingModal] = useState(false);
  const [deletingRateGroup, setDeletingRateGroup] = useState<RateGroupDto | null>(null);
  const [deletingMapping, setDeletingMapping] = useState<OrderTypeSubtypeRateGroupMappingDto | null>(null);
  const [rateGroupForm, setRateGroupForm] = useState<RateGroupFormData>({
    name: '',
    code: '',
    description: '',
    displayOrder: 0,
    isActive: true,
  });
  const [mappingForm, setMappingForm] = useState<MappingFormData>({
    orderTypeId: '',
    orderSubtypeId: '',
    rateGroupId: '',
  });

  // Base Work Rates (Phase 2)
  const [baseWorkRates, setBaseWorkRates] = useState<BaseWorkRateDto[]>([]);
  const [loadingBwr, setLoadingBwr] = useState(false);
  const [bwrFilter, setBwrFilter] = useState<BaseWorkRateListFilter>({});
  const [showBwrModal, setShowBwrModal] = useState(false);
  const [editingBwr, setEditingBwr] = useState<BaseWorkRateDto | null>(null);
  const [deletingBwr, setDeletingBwr] = useState<BaseWorkRateDto | null>(null);
  const [orderCategories, setOrderCategories] = useState<ReferenceDataItem[]>([]);
  const [serviceProfiles, setServiceProfiles] = useState<{ id: string; name: string; code: string; isActive: boolean }[]>([]);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [allSubtypes, setAllSubtypes] = useState<OrderTypeDto[]>([]);
  const [bwrForm, setBwrForm] = useState<CreateBaseWorkRateRequest & { clearOrderCategoryId?: boolean; clearServiceProfileId?: boolean; clearInstallationMethodId?: boolean; clearOrderSubtypeId?: boolean }>({
    rateGroupId: '',
    orderCategoryId: undefined,
    serviceProfileId: undefined,
    installationMethodId: undefined,
    orderSubtypeId: undefined,
    amount: 0,
    currency: 'MYR',
    effectiveFrom: undefined,
    effectiveTo: undefined,
    priority: 0,
    isActive: true,
    notes: undefined,
  });

  const loadRateGroups = useCallback(async () => {
    try {
      const data = await getRateGroups();
      setRateGroups(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load rate groups');
      setRateGroups([]);
    } finally {
      setLoading(false);
    }
  }, [showError]);

  const loadMappings = useCallback(async () => {
    try {
      setLoadingMappings(true);
      const data = await getRateGroupMappings();
      setMappings(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load mappings');
      setMappings([]);
    } finally {
      setLoadingMappings(false);
    }
  }, [showError]);

  const loadParents = useCallback(async () => {
    try {
      const data = await getOrderTypeParents({ isActive: undefined });
      setParents(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error(err);
      setParents([]);
    }
  }, []);

  const loadSubtypes = useCallback(async (parentId: string) => {
    if (!parentId) {
      setSubtypes([]);
      return;
    }
    try {
      const data = await getOrderTypeSubtypes(parentId);
      setSubtypes(Array.isArray(data) ? data : []);
    } catch (err) {
      setSubtypes([]);
    }
  }, []);

  useEffect(() => {
    loadRateGroups();
    loadParents();
  }, [loadRateGroups, loadParents]);

  useEffect(() => {
    loadMappings();
  }, [loadMappings]);

  useEffect(() => {
    if (mappingForm.orderTypeId) loadSubtypes(mappingForm.orderTypeId);
    else setSubtypes([]);
  }, [mappingForm.orderTypeId, loadSubtypes]);

  const loadBaseWorkRates = useCallback(async () => {
    try {
      setLoadingBwr(true);
      const data = await getBaseWorkRates(bwrFilter);
      setBaseWorkRates(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load base work rates');
      setBaseWorkRates([]);
    } finally {
      setLoadingBwr(false);
    }
  }, [bwrFilter, showError]);

  const loadOrderCategories = useCallback(async () => {
    try {
      const data = await getOrderCategories({});
      setOrderCategories(Array.isArray(data) ? data : []);
    } catch {
      setOrderCategories([]);
    }
  }, []);

  const loadInstallationMethods = useCallback(async () => {
    try {
      const data = await getInstallationMethods({});
      setInstallationMethods(Array.isArray(data) ? data : []);
    } catch {
      setInstallationMethods([]);
    }
  }, []);

  const loadAllSubtypes = useCallback(async () => {
    try {
      const all = await getOrderTypes({});
      setAllSubtypes(Array.isArray(all) ? all.filter((t) => t.parentOrderTypeId) : []);
    } catch {
      setAllSubtypes([]);
    }
  }, []);

  const loadServiceProfiles = useCallback(async () => {
    try {
      const data = await getServiceProfiles({ isActive: undefined });
      setServiceProfiles(Array.isArray(data) ? data.map((p) => ({ id: p.id, name: p.name, code: p.code, isActive: p.isActive })) : []);
    } catch {
      setServiceProfiles([]);
    }
  }, []);

  useEffect(() => {
    loadBaseWorkRates();
  }, [loadBaseWorkRates]);

  useEffect(() => {
    loadOrderCategories();
    loadServiceProfiles();
    loadInstallationMethods();
    loadAllSubtypes();
  }, [loadOrderCategories, loadServiceProfiles, loadInstallationMethods, loadAllSubtypes]);

  const handleCreateRateGroup = async () => {
    try {
      await createRateGroup({
        name: rateGroupForm.name.trim(),
        code: rateGroupForm.code.trim(),
        description: rateGroupForm.description?.trim() || undefined,
        isActive: rateGroupForm.isActive,
        displayOrder: typeof rateGroupForm.displayOrder === 'number' ? rateGroupForm.displayOrder : parseInt(String(rateGroupForm.displayOrder), 10) || 0,
      });
      showSuccess('Rate group created.');
      setShowRateGroupModal(false);
      setRateGroupForm({ name: '', code: '', description: '', displayOrder: 0, isActive: true });
      await loadRateGroups();
    } catch (err) {
      showError((err as Error).message || 'Failed to create rate group');
    }
  };

  const handleUpdateRateGroup = async () => {
    if (!editingRateGroup) return;
    try {
      await updateRateGroup(editingRateGroup.id, {
        name: rateGroupForm.name.trim(),
        code: rateGroupForm.code.trim(),
        description: rateGroupForm.description?.trim() || undefined,
        isActive: rateGroupForm.isActive,
        displayOrder: typeof rateGroupForm.displayOrder === 'number' ? rateGroupForm.displayOrder : parseInt(String(rateGroupForm.displayOrder), 10) || 0,
      });
      showSuccess('Rate group updated.');
      setShowRateGroupModal(false);
      setEditingRateGroup(null);
      setRateGroupForm({ name: '', code: '', description: '', displayOrder: 0, isActive: true });
      await loadRateGroups();
    } catch (err) {
      showError((err as Error).message || 'Failed to update rate group');
    }
  };

  const handleDeleteRateGroup = async () => {
    if (!deletingRateGroup) return;
    try {
      await deleteRateGroup(deletingRateGroup.id);
      showSuccess('Rate group deleted.');
      setDeletingRateGroup(null);
      await loadRateGroups();
      await loadMappings();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete rate group');
      setDeletingRateGroup(null);
    }
  };

  const openEditRateGroup = (row: RateGroupDto) => {
    setEditingRateGroup(row);
    setRateGroupForm({
      name: row.name,
      code: row.code,
      description: row.description ?? '',
      displayOrder: row.displayOrder,
      isActive: row.isActive,
    });
    setShowRateGroupModal(true);
  };

  const handleSaveMapping = async () => {
    if (!mappingForm.orderTypeId || !mappingForm.rateGroupId) {
      showError('Select order type and rate group.');
      return;
    }
    try {
      await assignRateGroupToOrderTypeSubtype({
        orderTypeId: mappingForm.orderTypeId,
        orderSubtypeId: mappingForm.orderSubtypeId || undefined,
        rateGroupId: mappingForm.rateGroupId,
      });
      showSuccess('Mapping saved.');
      setShowMappingModal(false);
      setMappingForm({ orderTypeId: '', orderSubtypeId: '', rateGroupId: '' });
      await loadMappings();
    } catch (err) {
      showError((err as Error).message || 'Failed to save mapping');
    }
  };

  const handleUnassignMapping = async () => {
    if (!deletingMapping) return;
    try {
      await unassignRateGroupMapping(deletingMapping.id);
      showSuccess('Mapping removed.');
      setDeletingMapping(null);
      await loadMappings();
    } catch (err) {
      showError((err as Error).message || 'Failed to remove mapping');
      setDeletingMapping(null);
    }
  };

  const handleCreateBwr = async () => {
    if (!bwrForm.rateGroupId) {
      showError('Select a rate group.');
      return;
    }
    if (bwrForm.orderCategoryId && bwrForm.serviceProfileId) {
      showError('Use either Order Category (exact pricing) or Service Profile (shared pricing), not both.');
      return;
    }
    try {
      await createBaseWorkRate({
        rateGroupId: bwrForm.rateGroupId,
        orderCategoryId: bwrForm.orderCategoryId || undefined,
        serviceProfileId: bwrForm.serviceProfileId || undefined,
        installationMethodId: bwrForm.installationMethodId || undefined,
        orderSubtypeId: bwrForm.orderSubtypeId || undefined,
        amount: Number(bwrForm.amount) || 0,
        currency: bwrForm.currency || 'MYR',
        effectiveFrom: bwrForm.effectiveFrom || undefined,
        effectiveTo: bwrForm.effectiveTo || undefined,
        priority: Number(bwrForm.priority) || 0,
        isActive: bwrForm.isActive ?? true,
        notes: bwrForm.notes || undefined,
      });
      showSuccess('Base work rate created.');
      setShowBwrModal(false);
      resetBwrForm();
      await loadBaseWorkRates();
    } catch (err) {
      showError((err as Error).message || 'Failed to create base work rate');
    }
  };

  const handleUpdateBwr = async () => {
    if (!editingBwr) return;
    if (bwrForm.orderCategoryId && bwrForm.serviceProfileId) {
      showError('Use either Order Category (exact pricing) or Service Profile (shared pricing), not both.');
      return;
    }
    try {
      await updateBaseWorkRate(editingBwr.id, {
        rateGroupId: bwrForm.rateGroupId || undefined,
        orderCategoryId: bwrForm.orderCategoryId || undefined,
        serviceProfileId: bwrForm.serviceProfileId || undefined,
        installationMethodId: bwrForm.installationMethodId || undefined,
        orderSubtypeId: bwrForm.orderSubtypeId || undefined,
        clearOrderCategoryId: !bwrForm.orderCategoryId && !!editingBwr.orderCategoryId,
        clearServiceProfileId: !bwrForm.serviceProfileId && !!editingBwr.serviceProfileId,
        clearInstallationMethodId: !bwrForm.installationMethodId && !!editingBwr.installationMethodId,
        clearOrderSubtypeId: !bwrForm.orderSubtypeId && !!editingBwr.orderSubtypeId,
        amount: Number(bwrForm.amount),
        currency: bwrForm.currency || 'MYR',
        effectiveFrom: bwrForm.effectiveFrom || undefined,
        effectiveTo: bwrForm.effectiveTo || undefined,
        priority: Number(bwrForm.priority) ?? 0,
        isActive: bwrForm.isActive ?? true,
        notes: bwrForm.notes || undefined,
      });
      showSuccess('Base work rate updated.');
      setShowBwrModal(false);
      setEditingBwr(null);
      resetBwrForm();
      await loadBaseWorkRates();
    } catch (err) {
      showError((err as Error).message || 'Failed to update base work rate');
    }
  };

  const handleDeleteBwr = async () => {
    if (!deletingBwr) return;
    try {
      await deleteBaseWorkRate(deletingBwr.id);
      showSuccess('Base work rate deleted.');
      setDeletingBwr(null);
      await loadBaseWorkRates();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete base work rate');
      setDeletingBwr(null);
    }
  };

  const resetBwrForm = () => {
    setBwrForm({
      rateGroupId: '',
      orderCategoryId: undefined,
      serviceProfileId: undefined,
      installationMethodId: undefined,
      orderSubtypeId: undefined,
      amount: 0,
      currency: 'MYR',
      effectiveFrom: undefined,
      effectiveTo: undefined,
      priority: 0,
      isActive: true,
      notes: undefined,
    });
  };

  const openEditBwr = (row: BaseWorkRateDto) => {
    setEditingBwr(row);
    setBwrForm({
      rateGroupId: row.rateGroupId,
      orderCategoryId: row.orderCategoryId || undefined,
      serviceProfileId: row.serviceProfileId || undefined,
      installationMethodId: row.installationMethodId || undefined,
      orderSubtypeId: row.orderSubtypeId || undefined,
      amount: row.amount,
      currency: row.currency || 'MYR',
      effectiveFrom: row.effectiveFrom || undefined,
      effectiveTo: row.effectiveTo || undefined,
      priority: row.priority ?? 0,
      isActive: row.isActive ?? true,
      notes: row.notes || undefined,
    });
    setShowBwrModal(true);
  };

  const rateGroupColumns = [
    { key: 'displayOrder', label: 'Order' },
    { key: 'name', label: 'Name' },
    { key: 'code', label: 'Code' },
    {
      key: 'isActive',
      label: 'Status',
      render: (value: unknown) => (
        <span className={`px-2 py-1 rounded-full text-xs font-medium ${value ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'}`}>
          {value ? 'Active' : 'Inactive'}
        </span>
      ),
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_: unknown, row: RateGroupDto) => (
        <div className="flex items-center gap-2">
          <button onClick={() => openEditRateGroup(row)} title="Edit" className="text-blue-600 hover:opacity-75">
            <Edit className="h-3 w-3" />
          </button>
          <button onClick={() => setDeletingRateGroup(row)} title="Delete" className="text-red-600 hover:opacity-75">
            <Trash2 className="h-3 w-3" />
          </button>
        </div>
      ),
    },
  ];

  const mappingColumns = [
    {
      key: 'mapping',
      label: 'Mapping',
      render: (_: unknown, row: OrderTypeSubtypeRateGroupMappingDto) => {
        const left = row.orderSubtypeName ? `${row.orderTypeName} / ${row.orderSubtypeName}` : row.orderTypeName;
        const right = row.rateGroupCode ?? row.rateGroupName ?? '';
        return <span>{left} → {right}</span>;
      },
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_: unknown, row: OrderTypeSubtypeRateGroupMappingDto) => (
        <button onClick={() => setDeletingMapping(row)} title="Remove" className="text-red-600 hover:opacity-75">
          <Trash2 className="h-3 w-3" />
        </button>
      ),
    },
  ];

  const bwrColumns = [
    { key: 'rateGroupCode', label: 'Rate Group', render: (v: unknown, row: BaseWorkRateDto) => row.rateGroupCode ?? row.rateGroupName ?? '—' },
    {
      key: 'appliesTo',
      label: 'Applies To',
      render: (_: unknown, row: BaseWorkRateDto) => {
        if (row.orderCategoryId) return `Order Category: ${row.orderCategoryCode ?? row.orderCategoryName ?? '—'} (exact)`;
        if (row.serviceProfileId) return `Service Profile: ${row.serviceProfileCode ?? row.serviceProfileName ?? '—'} (shared)`;
        return '— (broad)';
      },
    },
    { key: 'installationMethodCode', label: 'Installation Method', render: (v: unknown, row: BaseWorkRateDto) => row.installationMethodCode ?? row.installationMethodName ?? '—' },
    { key: 'orderSubtypeCode', label: 'Subtype Override', render: (v: unknown, row: BaseWorkRateDto) => row.orderSubtypeCode ?? row.orderSubtypeName ?? '—' },
    { key: 'amount', label: 'Amount', render: (v: unknown, row: BaseWorkRateDto) => `${row.currency} ${Number(row.amount).toFixed(2)}` },
    { key: 'priority', label: 'Priority' },
    {
      key: 'isActive',
      label: 'Active',
      render: (value: unknown) => (
        <span className={`px-2 py-1 rounded-full text-xs font-medium ${value ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'}`}>
          {value ? 'Yes' : 'No'}
        </span>
      ),
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_: unknown, row: BaseWorkRateDto) => (
        <div className="flex items-center gap-2">
          <button onClick={() => openEditBwr(row)} title="Edit" className="text-blue-600 hover:opacity-75">
            <Edit className="h-3 w-3" />
          </button>
          <button onClick={() => setDeletingBwr(row)} title="Delete" className="text-red-600 hover:opacity-75">
            <Trash2 className="h-3 w-3" />
          </button>
        </div>
      ),
    },
  ];

  if (loading) {
    return <LoadingSpinner message="Loading rate groups..." fullPage />;
  }

  return (
    <PageShell
      title="Rate Groups"
      breadcrumbs={[{ label: 'Settings' }, { label: 'GPON' }, { label: 'Rate Groups' }]}
    >
      <div className="flex-1 p-2 max-w-7xl mx-auto space-y-6">
        <Card>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold">Rate Groups</h2>
            <Button
              onClick={() => {
                setEditingRateGroup(null);
                setRateGroupForm({ name: '', code: '', description: '', displayOrder: 0, isActive: true });
                setShowRateGroupModal(true);
              }}
              className="flex items-center gap-2"
            >
              <Plus className="h-4 w-4" />
              Add rate group
            </Button>
          </div>
          {rateGroups.length > 0 ? (
            <DataTable data={rateGroups} columns={rateGroupColumns} />
          ) : (
            <EmptyState title="No rate groups" message="Add a rate group to use for order type mappings (e.g. INSTALL, SERVICE)." />
          )}
        </Card>

        <Card>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold">Order Type → Rate Group Mappings</h2>
            <Button
              onClick={() => {
                setMappingForm({ orderTypeId: '', orderSubtypeId: '', rateGroupId: '' });
                setShowMappingModal(true);
              }}
              className="flex items-center gap-2"
            >
              <Link2 className="h-4 w-4" />
              Add mapping
            </Button>
          </div>
          <p className="text-sm text-slate-500 dark:text-slate-400 mb-3">
            Map order types (and optional subtypes) to a rate group. Activation can map with no subtype; subtypes override parent when both exist.
          </p>
          {loadingMappings ? (
            <LoadingSpinner message="Loading mappings..." />
          ) : mappings.length > 0 ? (
            <DataTable data={mappings} columns={mappingColumns} />
          ) : (
            <EmptyState title="No mappings" message="Add a mapping to assign an order type (or subtype) to a rate group." />
          )}
        </Card>

        <Card>
          <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
            <h2 className="text-lg font-semibold">Base Work Rates</h2>
            <Button
              onClick={() => {
                setEditingBwr(null);
                resetBwrForm();
                setShowBwrModal(true);
              }}
              className="flex items-center gap-2"
            >
              <Plus className="h-4 w-4" />
              Add base work rate
            </Button>
          </div>
          <p className="text-sm text-slate-500 dark:text-slate-400 mb-3">
            Define base work rates by Rate Group and optional dimensions. <strong>Parent-only mapping</strong>: leave Applies To, Installation Method and Order Subtype blank for a broad fallback. <strong>Subtype override</strong>: set Order Subtype for an exact subtype rate (overrides parent-only when both exist). Not used for payout until a later phase.
          </p>
          <div className="flex flex-wrap gap-2 mb-3 items-end">
            <Select
              label="Rate group"
              value={bwrFilter.rateGroupId ?? ''}
              onChange={(v) => setBwrFilter((f) => ({ ...f, rateGroupId: v || undefined }))}
              options={[{ value: '', label: 'All rate groups' }, ...rateGroups.map((r) => ({ value: r.id, label: r.code }))]}
            />
            <Select
              label="Status"
              value={bwrFilter.isActive === undefined ? '' : bwrFilter.isActive ? 'true' : 'false'}
              onChange={(v) => setBwrFilter((f) => ({ ...f, isActive: v === '' ? undefined : v === 'true' }))}
              options={[{ value: '', label: 'All' }, { value: 'true', label: 'Active' }, { value: 'false', label: 'Inactive' }]}
            />
          </div>
          {loadingBwr ? (
            <LoadingSpinner message="Loading base work rates..." />
          ) : baseWorkRates.length > 0 ? (
            <DataTable data={baseWorkRates} columns={bwrColumns} />
          ) : (
            <EmptyState title="No base work rates" message="Add a base work rate to define amounts by rate group and optional order category, installation method, or subtype." />
          )}
        </Card>
      </div>

      {/* Rate Group create/edit modal */}
      <Modal
        isOpen={showRateGroupModal}
        onClose={() => { setShowRateGroupModal(false); setEditingRateGroup(null); }}
        title={editingRateGroup ? 'Edit rate group' : 'Create rate group'}
      >
        <div className="space-y-3">
          <TextInput
            label="Name"
            value={rateGroupForm.name}
            onChange={(v) => setRateGroupForm((f) => ({ ...f, name: v }))}
            placeholder="e.g. Installation"
          />
          <TextInput
            label="Code"
            value={rateGroupForm.code}
            onChange={(v) => setRateGroupForm((f) => ({ ...f, code: v }))}
            placeholder="e.g. INSTALL"
          />
          <TextInput
            label="Description"
            value={rateGroupForm.description}
            onChange={(v) => setRateGroupForm((f) => ({ ...f, description: v }))}
            placeholder="Optional"
          />
          <TextInput
            label="Display order"
            type="number"
            value={String(rateGroupForm.displayOrder)}
            onChange={(v) => setRateGroupForm((f) => ({ ...f, displayOrder: v }))}
          />
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={rateGroupForm.isActive}
              onChange={(e) => setRateGroupForm((f) => ({ ...f, isActive: e.target.checked }))}
            />
            <span>Active</span>
          </label>
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <Button variant="outline" onClick={() => { setShowRateGroupModal(false); setEditingRateGroup(null); }}>Cancel</Button>
          <Button onClick={editingRateGroup ? handleUpdateRateGroup : handleCreateRateGroup} className="flex items-center gap-2">
            <Save className="h-4 w-4" />
            {editingRateGroup ? 'Update' : 'Create'}
          </Button>
        </div>
      </Modal>

      {/* Mapping modal */}
      <Modal
        isOpen={showMappingModal}
        onClose={() => setShowMappingModal(false)}
        title="Map order type to rate group"
      >
        <div className="space-y-3">
          <Select
            label="Order type (parent)"
            value={mappingForm.orderTypeId}
            onChange={(v) => setMappingForm((f) => ({ ...f, orderTypeId: v, orderSubtypeId: '' }))}
            options={[{ value: '', label: '— Select —' }, ...parents.map((p) => ({ value: p.id, label: `${p.name} (${p.code})` }))]}
          />
          <Select
            label="Subtype (optional; leave blank for whole type e.g. Activation)"
            value={mappingForm.orderSubtypeId}
            onChange={(v) => setMappingForm((f) => ({ ...f, orderSubtypeId: v }))}
            options={[{ value: '', label: '— None (whole type) —' }, ...subtypes.map((s) => ({ value: s.id, label: `${s.name} (${s.code})` }))]}
          />
          <Select
            label="Rate group"
            value={mappingForm.rateGroupId}
            onChange={(v) => setMappingForm((f) => ({ ...f, rateGroupId: v }))}
            options={[{ value: '', label: '— Select —' }, ...rateGroups.map((r) => ({ value: r.id, label: `${r.name} (${r.code})` }))]}
          />
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <Button variant="outline" onClick={() => setShowMappingModal(false)}>Cancel</Button>
          <Button onClick={handleSaveMapping} className="flex items-center gap-2">
            <Save className="h-4 w-4" />
            Save mapping
          </Button>
        </div>
      </Modal>

      <ConfirmDialog
        isOpen={!!deletingRateGroup}
        onClose={() => setDeletingRateGroup(null)}
        onConfirm={handleDeleteRateGroup}
        title="Delete rate group"
        message={deletingRateGroup ? `Delete "${deletingRateGroup.name}"? This will fail if any mappings use it.` : ''}
        confirmLabel="Delete"
        variant="danger"
      />
      <ConfirmDialog
        isOpen={!!deletingMapping}
        onClose={() => setDeletingMapping(null)}
        onConfirm={handleUnassignMapping}
        title="Remove mapping"
        message="Remove this order type/subtype → rate group mapping?"
        confirmLabel="Remove"
        variant="danger"
      />

      {/* Base Work Rate create/edit modal */}
      <Modal
        isOpen={showBwrModal}
        onClose={() => { setShowBwrModal(false); setEditingBwr(null); resetBwrForm(); }}
        title={editingBwr ? 'Edit base work rate' : 'Create base work rate'}
      >
        <div className="space-y-3">
          <p className="text-sm text-slate-500 dark:text-slate-400">
            Use <strong>Order Category</strong> for exact pricing for one category, or <strong>Service Profile</strong> for shared pricing across categories. Use only one (or leave both blank for broad fallback). Parent-only: leave Applies To, Installation Method and Subtype blank. Subtype override: set Subtype for an exact rate.
          </p>
          <Select
            label="Rate Group"
            value={bwrForm.rateGroupId}
            onChange={(v) => setBwrForm((f) => ({ ...f, rateGroupId: v }))}
            options={[{ value: '', label: '— Select —' }, ...rateGroups.filter((r) => r.isActive).map((r) => ({ value: r.id, label: `${r.name} (${r.code})` }))]}
          />
          <Select
            label="Order Category (exact pricing) — optional"
            value={bwrForm.orderCategoryId ?? ''}
            onChange={(v) => setBwrForm((f) => ({ ...f, orderCategoryId: v || undefined, serviceProfileId: undefined, clearOrderCategoryId: false, clearServiceProfileId: !!f.serviceProfileId }))}
            options={[{ value: '', label: '— None —' }, ...orderCategories.filter((c) => c.isActive).map((c) => ({ value: c.id, label: `${c.name} (${c.code ?? ''})` }))]}
          />
          <Select
            label="Service Profile (shared pricing) — optional"
            value={bwrForm.serviceProfileId ?? ''}
            onChange={(v) => setBwrForm((f) => ({ ...f, serviceProfileId: v || undefined, orderCategoryId: undefined, clearServiceProfileId: false, clearOrderCategoryId: !!f.orderCategoryId }))}
            options={[{ value: '', label: '— None —' }, ...serviceProfiles.filter((p) => p.isActive).map((p) => ({ value: p.id, label: `${p.name} (${p.code})` }))]}
          />
          <Select
            label="Installation Method — optional"
            value={bwrForm.installationMethodId ?? ''}
            onChange={(v) => setBwrForm((f) => ({ ...f, installationMethodId: v || undefined, clearInstallationMethodId: false }))}
            options={[{ value: '', label: '— Any (broad fallback) —' }, ...installationMethods.filter((m) => m.isActive).map((m) => ({ value: m.id, label: `${m.name} (${m.code ?? ''})` }))]}
          />
          <Select
            label="Order Subtype Override — optional"
            value={bwrForm.orderSubtypeId ?? ''}
            onChange={(v) => setBwrForm((f) => ({ ...f, orderSubtypeId: v || undefined, clearOrderSubtypeId: false }))}
            options={[{ value: '', label: '— None (parent-only) —' }, ...allSubtypes.filter((s) => s.isActive).map((s) => ({ value: s.id, label: `${s.name} (${s.code})` }))]}
          />
          <TextInput
            label="Amount"
            type="number"
            value={String(bwrForm.amount)}
            onChange={(v) => setBwrForm((f) => ({ ...f, amount: parseFloat(v) || 0 }))}
          />
          <TextInput
            label="Currency"
            value={bwrForm.currency ?? 'MYR'}
            onChange={(v) => setBwrForm((f) => ({ ...f, currency: v || 'MYR' }))}
            placeholder="MYR"
          />
          <TextInput
            label="Effective From (optional)"
            type="date"
            value={bwrForm.effectiveFrom ? bwrForm.effectiveFrom.slice(0, 10) : ''}
            onChange={(v) => setBwrForm((f) => ({ ...f, effectiveFrom: v ? `${v}T00:00:00Z` : undefined }))}
          />
          <TextInput
            label="Effective To (optional)"
            type="date"
            value={bwrForm.effectiveTo ? bwrForm.effectiveTo.slice(0, 10) : ''}
            onChange={(v) => setBwrForm((f) => ({ ...f, effectiveTo: v ? `${v}T23:59:59Z` : undefined }))}
          />
          <TextInput
            label="Priority (higher wins)"
            type="number"
            value={String(bwrForm.priority ?? 0)}
            onChange={(v) => setBwrForm((f) => ({ ...f, priority: parseInt(v, 10) || 0 }))}
          />
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={bwrForm.isActive ?? true}
              onChange={(e) => setBwrForm((f) => ({ ...f, isActive: e.target.checked }))}
            />
            <span>Active</span>
          </label>
          <TextInput
            label="Notes"
            value={bwrForm.notes ?? ''}
            onChange={(v) => setBwrForm((f) => ({ ...f, notes: v || undefined }))}
            placeholder="Optional"
          />
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <Button variant="outline" onClick={() => { setShowBwrModal(false); setEditingBwr(null); resetBwrForm(); }}>Cancel</Button>
          <Button onClick={editingBwr ? handleUpdateBwr : handleCreateBwr} className="flex items-center gap-2">
            <Save className="h-4 w-4" />
            {editingBwr ? 'Update' : 'Create'}
          </Button>
        </div>
      </Modal>

      <ConfirmDialog
        isOpen={!!deletingBwr}
        onClose={() => setDeletingBwr(null)}
        onConfirm={handleDeleteBwr}
        title="Delete base work rate"
        message={deletingBwr ? `Delete this base work rate (${deletingBwr.rateGroupCode ?? ''} – ${deletingBwr.amount})?` : ''}
        confirmLabel="Delete"
        variant="danger"
      />
    </PageShell>
  );
};

export default RateGroupsPage;
