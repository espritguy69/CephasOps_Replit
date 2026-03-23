import React, { useState, useEffect } from 'react';
import { Plus, X, Trash2, Edit, Check, AlertCircle, ArrowRight } from 'lucide-react';
import { getWorkflowDefinitions, getWorkflowDefinition, createWorkflowDefinition, updateWorkflowDefinition, deleteWorkflowDefinition, getTransitions, addTransition, updateTransition, deleteTransition, getEffectiveWorkflowDefinition } from '../../api/workflowDefinitions';
import { getDepartments } from '../../api/departments';
import { getOrderTypeParents } from '../../api/orderTypes';
import { getPartners } from '../../api/partners';
import { getGuardConditionDefinitions, type GuardConditionDefinition } from '../../api/guardConditionDefinitions';
import { getSideEffectDefinitions, type SideEffectDefinition } from '../../api/sideEffectDefinitions';
import { LoadingSpinner, EmptyState, useToast, Button, Card, Modal, Select, TextInput, StatusBadge } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { WorkflowDefinition, WorkflowTransition } from '../../types/workflowDefinitions';
import type { Department } from '../../types/departments';

interface CollapsibleGuideProps {
  title: string;
  description: string;
  guides: Array<{
    number: number;
    title: string;
    content: string;
  }>;
}

const CollapsibleGuide: React.FC<CollapsibleGuideProps> = ({ title, description, guides }) => {
  const [isOpen, setIsOpen] = useState<boolean>(false);
  
  return (
    <div className="mb-4 border border-slate-200 rounded-lg overflow-hidden bg-slate-50">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="w-full flex items-center justify-between px-3 py-2 text-left hover:bg-slate-100 transition-colors"
      >
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-slate-700">{title}</span>
          <span className="text-xs text-slate-500">— {description}</span>
        </div>
        <span className="text-xs text-slate-400">{isOpen ? '▲ Hide' : '▼ Show Guide'}</span>
      </button>
      
      {isOpen && (
        <div className="px-3 py-2 border-t border-slate-200 bg-white">
          <div className="grid grid-cols-4 gap-2">
            {guides.map((guide, idx) => (
              <div key={idx} className="flex items-start gap-2">
                <div className="flex-shrink-0 w-4 h-4 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-bold">
                  {guide.number}
                </div>
                <div>
                  <h4 className="text-xs font-semibold text-slate-800">{guide.title}</h4>
                  <p className="text-xs text-slate-600 leading-tight">{guide.content}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

interface ExtendedWorkflowDefinition extends WorkflowDefinition {
  // All properties inherited from WorkflowDefinition
  // name, description, departmentId, departmentName, transitions already defined in base
}

interface ExtendedWorkflowTransition extends WorkflowTransition {
  // All properties inherited from WorkflowTransition
  // allowedRoles, guardConditions, sideEffectsConfig, displayOrder, isActive already defined in base
}

interface WorkflowFilters {
  entityType: string;
  isActive: boolean | undefined;
  departmentId: string;
  orderTypeCode: string;
}

interface NewDefinition {
  name: string;
  entityType: string;
  description: string;
  isActive: boolean;
  partnerId: string | null;
  departmentId: string;
  orderTypeCode: string;
}

interface NewTransition {
  fromStatus: string;
  toStatus: string;
  allowedRoles: string[];
  guardConditions: Record<string, unknown> | null;
  sideEffectsConfig: Record<string, unknown> | null;
  displayOrder: number;
  isActive: boolean;
}

/** Scope label for display: Partner > Department > Order Type > General */
function getScopeLabel(d: WorkflowDefinition): string {
  if (d.partnerId) return 'Partner scoped';
  if (d.departmentId) return 'Department scoped';
  if (d.orderTypeCode?.trim()) return 'Order Type scoped';
  return 'General';
}

const WorkflowDefinitionsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [definitions, setDefinitions] = useState<ExtendedWorkflowDefinition[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedDefinition, setSelectedDefinition] = useState<ExtendedWorkflowDefinition | null>(null);
  const [transitions, setTransitions] = useState<ExtendedWorkflowTransition[]>([]);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [showTransitionModal, setShowTransitionModal] = useState<boolean>(false);
  const [selectedTransition, setSelectedTransition] = useState<ExtendedWorkflowTransition | null>(null);
  const [saving, setSaving] = useState<boolean>(false);
  const [filters, setFilters] = useState<WorkflowFilters>({
    entityType: '',
    isActive: undefined,
    departmentId: '',
    orderTypeCode: ''
  });
  const [newDefinition, setNewDefinition] = useState<NewDefinition>({
    name: '',
    entityType: 'Order',
    description: '',
    isActive: true,
    partnerId: null,
    departmentId: '',
    orderTypeCode: ''
  });
  const [departments, setDepartments] = useState<Department[]>([]);
  const [partners, setPartners] = useState<Array<{ id: string; name: string }>>([]);
  const [parentOrderTypes, setParentOrderTypes] = useState<Array<{ id: string; name: string; code: string }>>([]);
  const [showEditModal, setShowEditModal] = useState<boolean>(false);
  const [editForm, setEditForm] = useState<{ name: string; description: string; isActive: boolean; partnerId: string; departmentId: string; orderTypeCode: string }>({ name: '', description: '', isActive: true, partnerId: '', departmentId: '', orderTypeCode: '' });
  const [effectivePreview, setEffectivePreview] = useState<WorkflowDefinition | null | 'loading' | undefined>(undefined);
  const [effectivePreviewParams, setEffectivePreviewParams] = useState<{ entityType: string; partnerId: string; departmentId: string; orderTypeCode: string }>({ entityType: 'Order', partnerId: '', departmentId: '', orderTypeCode: '' });
  const [guardConditionDefinitions, setGuardConditionDefinitions] = useState<GuardConditionDefinition[]>([]);
  const [sideEffectDefinitions, setSideEffectDefinitions] = useState<SideEffectDefinition[]>([]);
  const [selectedGuardConditions, setSelectedGuardConditions] = useState<string[]>([]);
  const [selectedSideEffects, setSelectedSideEffects] = useState<string[]>([]);
  const [newTransition, setNewTransition] = useState<NewTransition>({
    fromStatus: '',
    toStatus: '',
    allowedRoles: [],
    guardConditions: null,
    sideEffectsConfig: null,
    displayOrder: 0,
    isActive: true
  });

  useEffect(() => {
    loadDefinitions();
    loadDepartments();
  }, [filters]);

  useEffect(() => {
    getOrderTypeParents({ isActive: true })
      .then((data) => setParentOrderTypes(Array.isArray(data) ? data.map((p: { id: string; name: string; code: string }) => ({ id: p.id, name: p.name, code: p.code })) : []))
      .catch(() => setParentOrderTypes([]));
    getPartners({ isActive: true })
      .then((data) => setPartners(Array.isArray(data) ? data.map((p: { id: string; name: string }) => ({ id: p.id, name: p.name })) : []))
      .catch(() => setPartners([]));
  }, []);

  useEffect(() => {
    if (selectedDefinition) {
      loadGuardConditions();
      loadSideEffects();
    }
  }, [selectedDefinition]);

  const loadDepartments = async (): Promise<void> => {
    try {
      const data = await getDepartments();
      setDepartments(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Error loading departments:', err);
    }
  };

  const loadGuardConditions = async (): Promise<void> => {
    if (!selectedDefinition) return;
    try {
      const data = await getGuardConditionDefinitions({ 
        entityType: selectedDefinition.entityType,
        isActive: true 
      });
      setGuardConditionDefinitions(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Error loading guard conditions:', err);
      setGuardConditionDefinitions([]);
    }
  };

  const loadSideEffects = async (): Promise<void> => {
    if (!selectedDefinition) return;
    try {
      const data = await getSideEffectDefinitions({ 
        entityType: selectedDefinition.entityType,
        isActive: true 
      });
      setSideEffectDefinitions(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Error loading side effects:', err);
      setSideEffectDefinitions([]);
    }
  };

  useEffect(() => {
    if (selectedDefinition) {
      loadTransitions(selectedDefinition.id);
    }
  }, [selectedDefinition]);

  const loadDefinitions = async (): Promise<void> => {
    try {
      setLoading(true);
      setError(null);
      const cleanFilters: Record<string, any> = {};
      if (filters.entityType && filters.entityType.trim() !== '') {
        cleanFilters.entityType = filters.entityType;
      }
      if (filters.isActive !== undefined && filters.isActive !== null) {
        cleanFilters.isActive = filters.isActive;
      }
      const data = await getWorkflowDefinitions(cleanFilters);
      let list = Array.isArray(data) ? data : [];
      if (filters.departmentId?.trim()) {
        list = list.filter((d: WorkflowDefinition) => d.departmentId === filters.departmentId);
      }
      if (filters.orderTypeCode?.trim()) {
        list = list.filter((d: WorkflowDefinition) => (d.orderTypeCode ?? '') === filters.orderTypeCode);
      }
      setDefinitions(list);
    } catch (err) {
      const errorMessage = (err as Error).message || 'Failed to load workflow definitions';
      setError(errorMessage);
      console.error('Error loading definitions:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadTransitions = async (definitionId: string): Promise<void> => {
    try {
      const data = await getTransitions(definitionId);
      setTransitions(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Error loading transitions:', err);
      setTransitions([]);
    }
  };

  const handleCreate = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    if (!newDefinition.name?.trim() || !newDefinition.entityType) {
      setError('Name and Entity Type are required');
      return;
    }
    try {
      setSaving(true);
      setError(null);
      const payload: Record<string, unknown> = {
        name: newDefinition.name.trim(),
        entityType: newDefinition.entityType,
        description: newDefinition.description?.trim() || undefined,
        isActive: newDefinition.isActive
      };
      const pId = newDefinition.partnerId?.trim();
      const dId = newDefinition.departmentId?.trim();
      const otCode = newDefinition.orderTypeCode?.trim();
      if (pId) payload.partnerId = pId;
      if (dId) payload.departmentId = dId;
      if (otCode) payload.orderTypeCode = otCode;
      const created = await createWorkflowDefinition(payload as Parameters<typeof createWorkflowDefinition>[0]);
      setShowCreateModal(false);
      setNewDefinition({ name: '', entityType: 'Order', description: '', isActive: true, partnerId: null, departmentId: '', orderTypeCode: '' });
      showSuccess('Workflow definition created successfully');
      await loadDefinitions();
      if (created && created.id) {
        setSelectedDefinition(created as ExtendedWorkflowDefinition);
      }
    } catch (err) {
      setError((err as Error).message || 'Failed to create workflow definition');
      console.error('Error creating definition:', err);
      setSaving(false);
    }
  };

  const openEditModal = (): void => {
    if (!selectedDefinition) return;
    setEditForm({
      name: selectedDefinition.name ?? '',
      description: selectedDefinition.description ?? '',
      isActive: selectedDefinition.isActive ?? true,
      partnerId: selectedDefinition.partnerId ?? '',
      departmentId: selectedDefinition.departmentId ?? '',
      orderTypeCode: selectedDefinition.orderTypeCode ?? ''
    });
    setShowEditModal(true);
  };

  const handleUpdateDefinition = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    if (!selectedDefinition?.id) return;
    if (!editForm.name?.trim()) {
      setError('Name is required');
      return;
    }
    try {
      setSaving(true);
      setError(null);
      const payload: Record<string, unknown> = {
        name: editForm.name.trim(),
        description: editForm.description?.trim() || undefined,
        isActive: editForm.isActive
      };
      const pId = editForm.partnerId?.trim();
      const dId = editForm.departmentId?.trim();
      const otCode = editForm.orderTypeCode?.trim();
      payload.partnerId = pId || undefined;
      payload.departmentId = dId || undefined;
      payload.orderTypeCode = otCode || undefined;
      const updated = await updateWorkflowDefinition(selectedDefinition.id, payload as Parameters<typeof updateWorkflowDefinition>[1]);
      setShowEditModal(false);
      setSelectedDefinition(updated as ExtendedWorkflowDefinition);
      showSuccess('Workflow definition updated successfully');
      await loadDefinitions();
    } catch (err) {
      setError((err as Error).message || 'Failed to update workflow definition');
      console.error('Error updating definition:', err);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (definitionId: string): Promise<void> => {
    if (!confirm('Are you sure you want to delete this workflow definition? This will also delete all its transitions.')) return;
    try {
      setError(null);
      await deleteWorkflowDefinition(definitionId);
      if (selectedDefinition?.id === definitionId) {
        setSelectedDefinition(null);
        setTransitions([]);
      }
      showSuccess('Workflow definition deleted successfully');
      await loadDefinitions();
    } catch (err) {
      setError((err as Error).message || 'Failed to delete workflow definition');
      console.error('Error deleting definition:', err);
    }
  };

  const handleCreateTransition = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    if (!newTransition.toStatus || !selectedDefinition) {
      setError('Target status is required');
      return;
    }
    try {
      setSaving(true);
      setError(null);
      
      // Build guard conditions object from selected keys
      const guardConditions: Record<string, boolean> = {};
      selectedGuardConditions.forEach(key => {
        guardConditions[key] = true;
      });
      
      // Build side effects config object from selected keys
      const sideEffectsConfig: Record<string, boolean> = {};
      selectedSideEffects.forEach(key => {
        sideEffectsConfig[key] = true;
      });
      
      const transitionData = {
        ...newTransition,
        guardConditions: Object.keys(guardConditions).length > 0 ? guardConditions : null,
        sideEffectsConfig: Object.keys(sideEffectsConfig).length > 0 ? sideEffectsConfig : null
      };
      
      if (selectedTransition) {
        await updateTransition(selectedTransition.id, transitionData as any);
        showSuccess('Transition updated successfully');
      } else {
        await addTransition(selectedDefinition.id, transitionData as any);
        showSuccess('Transition added successfully');
      }
      setShowTransitionModal(false);
      setSelectedTransition(null);
      setSelectedGuardConditions([]);
      setSelectedSideEffects([]);
      setNewTransition({ fromStatus: '', toStatus: '', allowedRoles: [], guardConditions: null, sideEffectsConfig: null, displayOrder: transitions.length, isActive: true });
      await loadTransitions(selectedDefinition.id);
    } catch (err) {
      setError((err as Error).message || 'Failed to save transition');
      console.error('Error saving transition:', err);
      setSaving(false);
    }
  };

  const handleDeleteTransition = async (transitionId: string): Promise<void> => {
    if (!confirm('Are you sure you want to delete this transition?') || !selectedDefinition) return;
    try {
      setError(null);
      await deleteTransition(transitionId);
      showSuccess('Transition deleted successfully');
      await loadTransitions(selectedDefinition.id);
    } catch (err) {
      setError((err as Error).message || 'Failed to delete transition');
      console.error('Error deleting transition:', err);
    }
  };

  if (loading) {
    return (
      <PageShell title="Workflow Definitions" breadcrumbs={[{ label: 'Workflow' }]}>
        <LoadingSpinner message="Loading workflow definitions..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Workflow Definitions"
      breadcrumbs={[{ label: 'Workflow' }]}
      actions={
        <Button size="sm" onClick={() => setShowCreateModal(true)} className="gap-1">
          <Plus className="h-4 w-4" />
          Create Workflow
        </Button>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      {/* How-to Guide */}
      <CollapsibleGuide
        title="How to Configure Workflow Definitions"
        description="Define allowed status transitions, roles, and guard conditions for entity lifecycles."
        guides={[
          {
            number: 1,
            title: "Create Workflow",
            content: "Resolution: Partner → Department → Order Type → General. One active workflow per scope (entity type + optional partner, department, order type code)."
          },
          {
            number: 2,
            title: "Add Transitions",
            content: "Define which status can transition to which. E.g., Assigned → OnTheWay (SI only), OrderCompleted → DocketsReceived (Admin)."
          },
          {
            number: 3,
            title: "Guard Conditions",
            content: "Set requirements: photosRequired, docketUploaded, splitterRecorded. Transition blocked until conditions met."
          },
          {
            number: 4,
            title: "Roles & Permissions",
            content: "Specify allowed roles for each transition. SI can mark OnTheWay, only Admin can approve reschedules."
          }
        ]}
      />

      {/* Page Header */}
      <div className="mb-2 flex items-center justify-between">
        <h1 className="text-sm font-bold">Workflow Definitions</h1>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Create Workflow
        </Button>
      </div>

      {/* Error Banner */}
      {error && (
        <div className="mb-6 rounded-lg border border-red-200 bg-red-50 p-4 text-red-800 flex items-center gap-2" role="alert">
          <AlertCircle className="h-5 w-5" />
          {error}
          <button className="ml-auto hover:opacity-70" onClick={() => setError(null)} aria-label="Close">
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      {/* Filters */}
      <Card className="p-4 mb-6">
        <div className="flex flex-wrap items-end gap-4">
          <Select
            label="Entity Type"
            value={filters.entityType}
            onChange={(e) => setFilters({ ...filters, entityType: e.target.value })}
            options={[
              { value: '', label: 'All Entity Types' },
              { value: 'Order', label: 'Order' },
              { value: 'Invoice', label: 'Invoice' },
              { value: 'RmaRequest', label: 'RMA Request' }
            ]}
            className="w-48"
          />
          <Select
            label="Department"
            value={filters.departmentId}
            onChange={(e) => setFilters({ ...filters, departmentId: e.target.value })}
            options={[
              { value: '', label: 'All Departments' },
              ...departments.map(dept => ({ value: dept.id, label: dept.name }))
            ]}
            className="w-48"
          />
          <Select
            label="Order Type Code"
            value={filters.orderTypeCode}
            onChange={(e) => setFilters({ ...filters, orderTypeCode: e.target.value })}
            options={[
              { value: '', label: 'All' },
              ...parentOrderTypes.map(p => ({ value: p.code, label: `${p.name} (${p.code})` }))
            ]}
            className="w-52"
          />
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={filters.isActive === true}
              onChange={(e) => setFilters({ ...filters, isActive: e.target.checked ? true : undefined })}
              className="h-4 w-4 rounded border-gray-300"
            />
            <span className="text-sm">Active Only</span>
          </label>
        </div>
      </Card>

      {/* Preview effective workflow */}
      <Card className="p-4 mb-6">
        <h3 className="text-sm font-semibold mb-3">Preview effective workflow</h3>
        <p className="text-xs text-muted-foreground mb-3">Resolution order: Partner → Department → Order Type → General.</p>
        <div className="flex flex-wrap items-end gap-3">
          <Select
            label="Entity Type"
            value={effectivePreviewParams.entityType}
            onChange={(e) => setEffectivePreviewParams({ ...effectivePreviewParams, entityType: e.target.value })}
            options={[
              { value: 'Order', label: 'Order' },
              { value: 'Invoice', label: 'Invoice' },
              { value: 'RmaRequest', label: 'RMA Request' }
            ]}
            className="w-40"
          />
          <TextInput
            label="Partner ID (optional)"
            value={effectivePreviewParams.partnerId}
            onChange={(e) => setEffectivePreviewParams({ ...effectivePreviewParams, partnerId: e.target.value })}
            placeholder="GUID"
            className="w-64"
          />
          <Select
            label="Department (optional)"
            value={effectivePreviewParams.departmentId}
            onChange={(e) => setEffectivePreviewParams({ ...effectivePreviewParams, departmentId: e.target.value })}
            options={[
              { value: '', label: 'None' },
              ...departments.map(dept => ({ value: dept.id, label: dept.name }))
            ]}
            className="w-40"
          />
          <Select
            label="Order Type Code (optional)"
            value={effectivePreviewParams.orderTypeCode}
            onChange={(e) => setEffectivePreviewParams({ ...effectivePreviewParams, orderTypeCode: e.target.value })}
            options={[
              { value: '', label: 'None' },
              ...parentOrderTypes.map(p => ({ value: p.code, label: p.code }))
            ]}
            className="w-40"
          />
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={!effectivePreviewParams.entityType || effectivePreview === 'loading'}
            onClick={async () => {
              setEffectivePreview('loading');
              try {
                const entityType = effectivePreviewParams.entityType?.trim();
                if (!entityType) {
                  setEffectivePreview(undefined);
                  return;
                }
                const params: Record<string, string> = { entityType };
                if (effectivePreviewParams.partnerId?.trim()) params.partnerId = effectivePreviewParams.partnerId.trim();
                if (effectivePreviewParams.departmentId?.trim()) params.departmentId = effectivePreviewParams.departmentId.trim();
                if (effectivePreviewParams.orderTypeCode?.trim()) params.orderTypeCode = effectivePreviewParams.orderTypeCode.trim();
                const w = await getEffectiveWorkflowDefinition(params);
                setEffectivePreview(w);
              } catch {
                setEffectivePreview(null);
              }
            }}
          >
            {effectivePreview === 'loading' ? 'Loading...' : 'Preview'}
          </Button>
        </div>
        {effectivePreview !== null && effectivePreview !== undefined && effectivePreview !== 'loading' && (
          <p className="mt-3 text-sm">
            <strong>Effective workflow:</strong> {effectivePreview.name} {!effectivePreview.isActive && '(inactive)'}
          </p>
        )}
        {effectivePreview === null && (
          <p className="mt-3 text-sm text-muted-foreground">No active workflow found for this scope.</p>
        )}
      </Card>

      {/* Content */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Definitions List */}
        <div className="lg:col-span-1 space-y-4">
          <h2 className="text-xl font-semibold mb-4">Workflow Definitions</h2>
          {definitions.length === 0 ? (
            <EmptyState
              title="No workflow definitions found"
              description="Create your first workflow definition to get started."
            />
          ) : (
            definitions.map(definition => (
              <Card
                key={definition.id}
                className={`p-4 cursor-pointer transition-all ${
                  selectedDefinition?.id === definition.id
                    ? 'ring-2 ring-primary shadow-lg'
                    : 'hover:shadow-md'
                }`}
                onClick={() => setSelectedDefinition(definition)}
              >
                <div className="flex justify-between items-start mb-3">
                  <h3 className="font-semibold text-lg">{definition.name || definition.id}</h3>
                  <StatusBadge
                    status={definition.isActive ? 'Active' : 'Inactive'}
                    variant={definition.isActive ? 'success' : 'secondary'}
                  />
                </div>
                <div className="space-y-2 text-sm mb-4">
                  <p><strong>Entity Type:</strong> {definition.entityType}</p>
                  <p className="text-xs text-muted-foreground"><strong>Scope:</strong> {getScopeLabel(definition)}</p>
                  {definition.departmentName && <p><strong>Department:</strong> {definition.departmentName}</p>}
                  {definition.orderTypeCode && <p><strong>Order Type Code:</strong> {definition.orderTypeCode}</p>}
                  {definition.description && <p><strong>Description:</strong> {definition.description}</p>}
                  <p><strong>Transitions:</strong> {definition.transitions?.length || transitions.length || 0}</p>
                </div>
                <div className="flex gap-2" onClick={(e) => e.stopPropagation()}>
                  <Button size="sm" variant="ghost" onClick={() => setSelectedDefinition(definition)}>
                    <Edit className="h-4 w-4 mr-2" />
                    View
                  </Button>
                  <Button size="sm" variant="ghost" onClick={() => handleDelete(definition.id)}>
                    <Trash2 className="h-4 w-4 mr-2 text-destructive" />
                    Delete
                  </Button>
                </div>
              </Card>
            ))
          )}
        </div>

        {/* Definition Details */}
        {selectedDefinition && (
          <Card className="lg:col-span-2 p-6 sticky top-6 max-h-[calc(100vh-3rem)] overflow-y-auto">
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-bold">{selectedDefinition.name || selectedDefinition.id}</h2>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" onClick={openEditModal}>
                  <Edit className="h-4 w-4 mr-2" />
                  Edit definition
                </Button>
                <Button variant="outline" size="sm" onClick={() => setSelectedDefinition(null)}>
                  <X className="h-4 w-4 mr-2" />
                  Close
                </Button>
              </div>
            </div>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="font-medium text-muted-foreground">Entity Type:</span>
                  <p className="mt-1">{selectedDefinition.entityType}</p>
                </div>
                <div>
                  <span className="font-medium text-muted-foreground">Scope:</span>
                  <p className="mt-1">{getScopeLabel(selectedDefinition)}</p>
                </div>
                {selectedDefinition.departmentName && (
                  <div>
                    <span className="font-medium text-muted-foreground">Department:</span>
                    <p className="mt-1">{selectedDefinition.departmentName}</p>
                  </div>
                )}
                {selectedDefinition.orderTypeCode && (
                  <div>
                    <span className="font-medium text-muted-foreground">Order Type Code:</span>
                    <p className="mt-1">{selectedDefinition.orderTypeCode}</p>
                  </div>
                )}
              </div>
              {selectedDefinition.description && (
                <div className="text-sm">
                  <span className="font-medium text-muted-foreground">Description:</span>
                  <p className="mt-1">{selectedDefinition.description}</p>
                </div>
              )}
              
              {/* Transitions Section */}
              <div className="pt-4 border-t">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-lg font-semibold">Transitions ({transitions.length})</h3>
                  <Button
                    size="sm"
                    onClick={() => {
                      setShowTransitionModal(true);
                      setSelectedTransition(null);
                      setSelectedGuardConditions([]);
                      setSelectedSideEffects([]);
                      setNewTransition({ fromStatus: '', toStatus: '', allowedRoles: [], guardConditions: null, sideEffectsConfig: null, displayOrder: transitions.length, isActive: true });
                    }}
                  >
                    <Plus className="h-4 w-4 mr-2" />
                    Add Transition
                  </Button>
                </div>
                {transitions.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-4">No transitions defined</p>
                ) : (
                  <div className="space-y-3">
                    {transitions.map(transition => (
                      <div key={transition.id} className="border rounded-lg p-4 bg-slate-50/50">
                        <div className="flex items-center justify-between mb-2">
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-sm flex items-center gap-2">
                              <span className="px-2 py-1 bg-slate-200 rounded">{transition.fromStatus || 'Initial'}</span>
                              <ArrowRight className="h-4 w-4 text-muted-foreground" />
                              <span className="px-2 py-1 bg-blue-100 text-blue-700 rounded">{transition.toStatus}</span>
                            </span>
                            {transition.isActive === false && (
                              <StatusBadge status="Inactive" variant="secondary" />
                            )}
                          </div>
                          <div className="flex gap-2">
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => {
                                setSelectedTransition(transition);
                                setNewTransition({
                                  fromStatus: transition.fromStatus || '',
                                  toStatus: transition.toStatus,
                                  allowedRoles: transition.allowedRoles || [],
                                  guardConditions: transition.guardConditions || null,
                                  sideEffectsConfig: transition.sideEffectsConfig || null,
                                  displayOrder: transition.displayOrder || 0,
                                  isActive: transition.isActive !== false
                                });
                                // Load selected guard conditions and side effects from transition
                                setSelectedGuardConditions(transition.guardConditions ? Object.keys(transition.guardConditions) : []);
                                setSelectedSideEffects(transition.sideEffectsConfig ? Object.keys(transition.sideEffectsConfig) : []);
                                setShowTransitionModal(true);
                              }}
                            >
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => handleDeleteTransition(transition.id)}
                            >
                              <Trash2 className="h-4 w-4 text-destructive" />
                            </Button>
                          </div>
                        </div>
                        <div className="text-xs text-muted-foreground space-y-1">
                          <p><strong>Allowed Roles:</strong> {transition.allowedRoles?.join(', ') || 'Any'}</p>
                          {transition.guardConditions && (
                            <p><strong>Guard Conditions:</strong> {Object.keys(transition.guardConditions).join(', ')}</p>
                          )}
                          {transition.sideEffectsConfig && (
                            <p><strong>Side Effects:</strong> {Object.keys(transition.sideEffectsConfig).join(', ')}</p>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </Card>
        )}
      </div>

      {/* Create Workflow Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => !saving && setShowCreateModal(false)}
        title="Create Workflow Definition"
        size="medium"
      >
        <form onSubmit={handleCreate} className="space-y-4">
          <TextInput
            label="Workflow Name *"
            value={newDefinition.name}
            onChange={(e) => setNewDefinition({ ...newDefinition, name: e.target.value })}
            required
            placeholder="e.g., ISP Order Workflow"
            disabled={saving}
          />

          <Select
            label="Partner (optional – leave empty for general)"
            value={newDefinition.partnerId ?? ''}
            onChange={(e) => setNewDefinition({ ...newDefinition, partnerId: e.target.value || null })}
            options={[
              { value: '', label: 'None (general)' },
              ...partners.map(p => ({ value: p.id, label: p.name }))
            ]}
            disabled={saving}
          />
          <Select
            label="Department (optional – leave empty for general)"
            value={newDefinition.departmentId}
            onChange={(e) => setNewDefinition({ ...newDefinition, departmentId: e.target.value })}
            options={[
              { value: '', label: 'None (general)' },
              ...departments.map(dept => ({ value: dept.id, label: dept.name }))
            ]}
            disabled={saving}
          />
          {newDefinition.entityType === 'Order' && (
            <Select
              label="Order Type Code (optional – parent code e.g. MODIFICATION, ACTIVATION)"
              value={newDefinition.orderTypeCode}
              onChange={(e) => setNewDefinition({ ...newDefinition, orderTypeCode: e.target.value })}
              options={[
                { value: '', label: 'None (general)' },
                ...parentOrderTypes.map(p => ({ value: p.code, label: `${p.name} (${p.code})` }))
              ]}
              disabled={saving}
            />
          )}

          <Select
            label="Entity Type *"
            value={newDefinition.entityType}
            onChange={(e) => setNewDefinition({ ...newDefinition, entityType: e.target.value })}
            options={[
              { value: 'Order', label: 'Order' },
              { value: 'Invoice', label: 'Invoice' },
              { value: 'RmaRequest', label: 'RMA Request' }
            ]}
            required
            disabled={saving}
          />

          <div className="space-y-1">
            <label className="text-sm font-medium">Description</label>
            <textarea
              value={newDefinition.description || ''}
              onChange={(e) => setNewDefinition({ ...newDefinition, description: e.target.value })}
              placeholder="Optional description"
              disabled={saving}
              rows={3}
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            />
          </div>

          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isActive"
              checked={newDefinition.isActive}
              onChange={(e) => setNewDefinition({ ...newDefinition, isActive: e.target.checked })}
              disabled={saving}
              className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="isActive" className="text-sm font-medium">Active</label>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => !saving && setShowCreateModal(false)}
              disabled={saving}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving ? 'Creating...' : 'Create'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Edit Workflow Definition Modal */}
      <Modal
        isOpen={showEditModal}
        onClose={() => !saving && setShowEditModal(false)}
        title="Edit Workflow Definition"
        size="medium"
      >
        <form onSubmit={handleUpdateDefinition} className="space-y-4">
          <TextInput
            label="Workflow Name *"
            value={editForm.name}
            onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
            required
            placeholder="e.g., ISP Order Workflow"
            disabled={saving}
          />
          <Select
            label="Partner (optional)"
            value={editForm.partnerId ?? ''}
            onChange={(e) => setEditForm({ ...editForm, partnerId: e.target.value })}
            options={[
              { value: '', label: 'None (general)' },
              ...partners.map(p => ({ value: p.id, label: p.name }))
            ]}
            disabled={saving}
          />
          <Select
            label="Department (optional)"
            value={editForm.departmentId ?? ''}
            onChange={(e) => setEditForm({ ...editForm, departmentId: e.target.value })}
            options={[
              { value: '', label: 'None (general)' },
              ...departments.map(dept => ({ value: dept.id, label: dept.name }))
            ]}
            disabled={saving}
          />
          {selectedDefinition?.entityType === 'Order' && (
            <Select
              label="Order Type Code (optional)"
              value={editForm.orderTypeCode ?? ''}
              onChange={(e) => setEditForm({ ...editForm, orderTypeCode: e.target.value })}
              options={[
                { value: '', label: 'None (general)' },
                ...parentOrderTypes.map(p => ({ value: p.code, label: `${p.name} (${p.code})` }))
              ]}
              disabled={saving}
            />
          )}
          <div className="space-y-1">
            <label className="text-sm font-medium">Description</label>
            <textarea
              value={editForm.description ?? ''}
              onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
              placeholder="Optional description"
              disabled={saving}
              rows={3}
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="editIsActive"
              checked={editForm.isActive}
              onChange={(e) => setEditForm({ ...editForm, isActive: e.target.checked })}
              disabled={saving}
              className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="editIsActive" className="text-sm font-medium">Active</label>
          </div>
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => !saving && setShowEditModal(false)}
              disabled={saving}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving ? 'Saving...' : 'Save'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Transition Modal */}
      <Modal
        isOpen={showTransitionModal && selectedDefinition !== null}
        onClose={() => !saving && setShowTransitionModal(false)}
        title={`${selectedTransition ? 'Edit' : 'Add'} Transition`}
        size="large"
      >
        <form onSubmit={handleCreateTransition} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <TextInput
                label="From Status"
                value={newTransition.fromStatus}
                onChange={(e) => setNewTransition({ ...newTransition, fromStatus: e.target.value })}
                placeholder="Leave empty for initial state"
                disabled={saving}
              />
              <p className="text-xs text-muted-foreground mt-1">The starting status for this transition</p>
            </div>

            <div>
              <TextInput
                label="To Status *"
                value={newTransition.toStatus}
                onChange={(e) => setNewTransition({ ...newTransition, toStatus: e.target.value })}
                required
                placeholder="e.g., Assigned, OrderCompleted"
                disabled={saving}
              />
              <p className="text-xs text-muted-foreground mt-1">The target status after transition</p>
            </div>
          </div>

          <div>
            <TextInput
              label="Allowed Roles"
              value={newTransition.allowedRoles?.join(', ') || ''}
              onChange={(e) => setNewTransition({
                ...newTransition,
                allowedRoles: e.target.value.split(',').map(r => r.trim()).filter(Boolean)
              })}
              placeholder="Comma-separated: Admin, SI, Scheduler"
              disabled={saving}
            />
            <p className="text-xs text-muted-foreground mt-1">Leave empty to allow all roles</p>
          </div>

          <TextInput
            label="Display Order"
            type="number"
            value={newTransition.displayOrder}
            onChange={(e) => setNewTransition({ ...newTransition, displayOrder: parseInt(e.target.value) || 0 })}
            disabled={saving}
          />

          {/* Guard Conditions Selection */}
          <div>
            <label className="text-sm font-medium mb-2 block">Guard Conditions</label>
            <p className="text-xs text-muted-foreground mb-2">Select validation rules that must be met before this transition</p>
            <div className="border rounded-lg p-3 max-h-48 overflow-y-auto space-y-2">
              {guardConditionDefinitions.length === 0 ? (
                <p className="text-xs text-muted-foreground">No guard conditions available for {selectedDefinition?.entityType}. Create them in Settings → Guard Condition Definitions.</p>
              ) : (
                guardConditionDefinitions.map(def => (
                  <label key={def.id} className="flex items-center gap-2 cursor-pointer hover:bg-slate-50 p-2 rounded">
                    <input
                      type="checkbox"
                      checked={selectedGuardConditions.includes(def.key)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedGuardConditions([...selectedGuardConditions, def.key]);
                        } else {
                          setSelectedGuardConditions(selectedGuardConditions.filter(k => k !== def.key));
                        }
                      }}
                      disabled={saving}
                      className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                    />
                    <div className="flex-1">
                      <span className="text-sm font-medium">{def.name}</span>
                      {def.description && (
                        <p className="text-xs text-muted-foreground">{def.description}</p>
                      )}
                    </div>
                    <span className="text-xs text-muted-foreground font-mono">{def.key}</span>
                  </label>
                ))
              )}
            </div>
          </div>

          {/* Side Effects Selection */}
          <div>
            <label className="text-sm font-medium mb-2 block">Side Effects</label>
            <p className="text-xs text-muted-foreground mb-2">Select automatic actions to execute during this transition</p>
            <div className="border rounded-lg p-3 max-h-48 overflow-y-auto space-y-2">
              {sideEffectDefinitions.length === 0 ? (
                <p className="text-xs text-muted-foreground">No side effects available for {selectedDefinition?.entityType}. Create them in Settings → Side Effect Definitions.</p>
              ) : (
                sideEffectDefinitions.map(def => (
                  <label key={def.id} className="flex items-center gap-2 cursor-pointer hover:bg-slate-50 p-2 rounded">
                    <input
                      type="checkbox"
                      checked={selectedSideEffects.includes(def.key)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedSideEffects([...selectedSideEffects, def.key]);
                        } else {
                          setSelectedSideEffects(selectedSideEffects.filter(k => k !== def.key));
                        }
                      }}
                      disabled={saving}
                      className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                    />
                    <div className="flex-1">
                      <span className="text-sm font-medium">{def.name}</span>
                      {def.description && (
                        <p className="text-xs text-muted-foreground">{def.description}</p>
                      )}
                    </div>
                    <span className="text-xs text-muted-foreground font-mono">{def.key}</span>
                  </label>
                ))
              )}
            </div>
          </div>

          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="transitionIsActive"
              checked={newTransition.isActive}
              onChange={(e) => setNewTransition({ ...newTransition, isActive: e.target.checked })}
              disabled={saving}
              className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="transitionIsActive" className="text-sm font-medium">Active</label>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => !saving && setShowTransitionModal(false)}
              disabled={saving}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving ? 'Saving...' : (selectedTransition ? 'Update' : 'Add')} Transition
            </Button>
          </div>
        </form>
      </Modal>
      </div>
    </PageShell>
  );
};

export default WorkflowDefinitionsPage;

