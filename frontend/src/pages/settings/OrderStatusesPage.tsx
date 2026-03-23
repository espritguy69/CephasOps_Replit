import React, { useState, useEffect, useMemo } from 'react';
import { Plus, X, Trash2, Edit, ArrowRight, CheckCircle, Clock, AlertTriangle, FileText, Truck, Users, Wrench, Ban, Receipt, CreditCard, Eye, RefreshCw, Shield, ThumbsUp, Flag, Building, ListChecks } from 'lucide-react';
import { LoadingSpinner, EmptyState, useToast, Button, Card, Modal, TextInput, StatusBadge } from '../../components/ui';
import { PageShell } from '../../components/layout';
import OrderStatusChecklistManager from '../../components/checklist/OrderStatusChecklistManager';
import apiClient from '../../api/client';

const ICON_MAP: Record<string, React.ComponentType<{ className?: string }>> = {
  Clock, AlertTriangle, FileText, Truck, Users, Wrench, Ban, Receipt, CreditCard, CheckCircle, Eye, RefreshCw, Shield, ThumbsUp, Flag, Building, Trash2
};

const WORKFLOW_COLORS: Record<string, string> = {
  Order: '#3b82f6',
  RMA: '#f97316',
  KPI: '#22c55e'
};

interface Guide {
  number: number;
  title: string;
  content: string;
}

interface CollapsibleGuideProps {
  title: string;
  description: string;
  guides: Guide[];
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
          <div className="grid grid-cols-3 gap-2">
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

interface OrderStatus {
  code: string;
  name: string;
  description?: string;
  order: number;
  color?: string;
  icon?: string;
  triggeredBy?: string;
  workflowType: string;
  phase?: string;
  kpiCategory?: string;
}

interface WorkflowType {
  code: string;
  name: string;
  description?: string;
  color?: string;
  statusCount?: number;
}

interface StatusFormData {
  code: string;
  name: string;
  description: string;
  order: number;
  color: string;
  icon: string;
  triggeredBy: string;
  workflowType: string;
  phase: string;
  kpiCategory: string;
  insertPosition: string;
  insertRelativeTo: string;
}

const OrderStatusesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [statuses, setStatuses] = useState<OrderStatus[]>([]);
  const [workflowTypes, setWorkflowTypes] = useState<WorkflowType[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState<boolean>(false);
  const [editingStatus, setEditingStatus] = useState<OrderStatus | null>(null);
  const [saving, setSaving] = useState<boolean>(false);
  const [viewMode, setViewMode] = useState<'flow' | 'list'>('flow');
  const [selectedWorkflow, setSelectedWorkflow] = useState<string>('Order');
  const [checklistStatus, setChecklistStatus] = useState<{ code: string; name: string } | null>(null);
  
  const [formData, setFormData] = useState<StatusFormData>({
    code: '',
    name: '',
    description: '',
    order: 0,
    color: '#3b82f6',
    icon: 'Clock',
    triggeredBy: 'Admin',
    workflowType: 'Order',
    phase: '',
    kpiCategory: '',
    insertPosition: 'after',
    insertRelativeTo: ''
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const [statusesResponse, typesResponse] = await Promise.all([
        apiClient.get('/order-statuses'),
        apiClient.get('/order-statuses/workflow-types')
      ]);
      setStatuses(Array.isArray(statusesResponse) ? statusesResponse : []);
      setWorkflowTypes(Array.isArray(typesResponse) ? typesResponse : []);
    } catch {
      setError('Failed to load statuses');
    } finally {
      setLoading(false);
    }
  };

  const filteredStatuses = statuses.filter(s => s.workflowType === selectedWorkflow);

  const handleSave = async (e: React.FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    if (!formData.code || !formData.name) {
      setError('Status code and name are required');
      return;
    }
    
    try {
      setSaving(true);
      setError(null);
      
      const payload = {
        ...formData,
        workflowType: selectedWorkflow
      };
      
      if (editingStatus) {
        await apiClient.put(`/order-statuses/${editingStatus.code}`, payload);
        showSuccess('Status updated successfully');
      } else {
        await apiClient.post('/order-statuses', payload);
        showSuccess('Status created successfully');
      }
      
      setShowModal(false);
      setEditingStatus(null);
      resetForm();
      await loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to save status');
    } finally {
      setSaving(false);
    }
  };

  const resetForm = (): void => {
    setFormData({
      code: '',
      name: '',
      description: '',
      order: filteredStatuses.length + 1,
      color: WORKFLOW_COLORS[selectedWorkflow] || '#3b82f6',
      icon: 'Clock',
      triggeredBy: 'Admin',
      workflowType: selectedWorkflow,
      phase: '',
      kpiCategory: selectedWorkflow === 'KPI' ? 'SI' : '',
      insertPosition: 'after',
      insertRelativeTo: ''
    });
  };

  const handleEdit = (status: OrderStatus): void => {
    setEditingStatus(status);
    setFormData({
      code: status.code,
      name: status.name,
      description: status.description || '',
      order: status.order,
      color: status.color || '#3b82f6',
      icon: status.icon || 'Clock',
      triggeredBy: status.triggeredBy || 'Admin',
      workflowType: status.workflowType || 'Order',
      phase: status.phase || '',
      kpiCategory: status.kpiCategory || '',
      insertPosition: 'after',
      insertRelativeTo: ''
    });
    setShowModal(true);
  };

  const handleAddNew = (): void => {
    setEditingStatus(null);
    resetForm();
    setShowModal(true);
  };

  const getStatusIcon = (iconName: string): React.ComponentType<{ className?: string }> => {
    return ICON_MAP[iconName] || Clock;
  };

  // Group statuses by phase - memoized to update when filteredStatuses changes
  const phases = useMemo(() => {
    const phasesMap: Record<string, OrderStatus[]> = {};
    filteredStatuses.forEach(status => {
      const phase = status.phase || 'Other';
      if (!phasesMap[phase]) {
        phasesMap[phase] = [];
      }
      phasesMap[phase].push(status);
    });
    // Sort each phase by order
    Object.keys(phasesMap).forEach(phase => {
      phasesMap[phase].sort((a, b) => a.order - b.order);
    });
    return phasesMap;
  }, [filteredStatuses]);

  if (loading) {
    return (
      <PageShell title="Workflow Statuses" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Workflow Statuses' }]}>
        <LoadingSpinner message="Loading workflow statuses..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title={`${selectedWorkflow} Workflow Statuses`}
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Workflow Statuses' }]}
      actions={
        <div className="flex items-center gap-2">
          <div className="flex border border-slate-200 rounded-md overflow-hidden">
            <button
              onClick={() => setViewMode('flow')}
              className={`px-3 py-1.5 text-xs font-medium transition-colors ${viewMode === 'flow' ? 'bg-blue-600 text-white' : 'bg-white text-slate-600 hover:bg-slate-50'}`}
            >
              Flow View
            </button>
            <button
              onClick={() => setViewMode('list')}
              className={`px-3 py-1.5 text-xs font-medium transition-colors ${viewMode === 'list' ? 'bg-blue-600 text-white' : 'bg-white text-slate-600 hover:bg-slate-50'}`}
            >
              List View
            </button>
          </div>
          <Button size="sm" onClick={handleAddNew} className="gap-1">
            <Plus className="h-4 w-4" />
            Add Status
          </Button>
        </div>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      {/* How-to Guide */}
      <CollapsibleGuide
        title="How to Manage Workflow Statuses"
        description="Define lifecycle stages for Orders, RMA, and KPI tracking."
        guides={[
          {
            number: 1,
            title: "Order Workflow",
            content: "15 steps: Pending → Assigned → OnTheWay → MetCustomer → OrderCompleted → Dockets → Invoice → Completed. Includes DocketsVerified QA step."
          },
          {
            number: 2,
            title: "RMA Workflow",
            content: "Material Return Authorization: RMARequested → PendingReview → MRA Received → Approved → InTransit → Repaired/Replaced/Credited → Closed."
          },
          {
            number: 3,
            title: "KPI Workflow",
            content: "Performance tracking: SI KPIs (OnTime/Late/ExceededSla), Admin KPIs (Docket/Invoice timing), Employer Reviews."
          }
        ]}
      />

      {/* Workflow Type Tabs */}
      <div className="mb-4 flex items-center gap-2">
        {workflowTypes.map((wf) => (
          <button
            key={wf.code}
            onClick={() => setSelectedWorkflow(wf.code)}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-all flex items-center gap-2 ${
              selectedWorkflow === wf.code 
                ? 'text-white shadow-md' 
                : 'bg-white text-slate-600 border border-slate-200 hover:bg-slate-50'
            }`}
            style={selectedWorkflow === wf.code ? { backgroundColor: wf.color } : {}}
          >
            <span className="w-2 h-2 rounded-full" style={{ backgroundColor: wf.color }}></span>
            {wf.name}
            <span className="text-xs opacity-75">({wf.statusCount || 0})</span>
          </button>
        ))}
      </div>

      <p className="text-xs text-muted-foreground mb-2">
        {workflowTypes.find(w => w.code === selectedWorkflow)?.description} ({filteredStatuses.length} statuses)
      </p>

      {/* Error Banner */}
      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-red-800 flex items-center gap-2 text-sm" role="alert">
          <AlertTriangle className="h-4 w-4" />
          {error}
          <button className="ml-auto hover:opacity-70" onClick={() => setError(null)} aria-label="Close">
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      {/* Flow View */}
      {viewMode === 'flow' && (
        <div className="space-y-4">
          {Object.entries(phases).map(([phaseName, phaseStatuses], phaseIdx) => (
            <Card key={phaseName} className="p-4">
              <h3 className="text-sm font-semibold text-slate-700 mb-3 flex items-center gap-2">
                <span 
                  className="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold text-white"
                  style={{ backgroundColor: WORKFLOW_COLORS[selectedWorkflow] }}
                >
                  {phaseIdx + 1}
                </span>
                {phaseName}
                <span className="text-xs text-slate-400 font-normal">({phaseStatuses.length} statuses)</span>
              </h3>
              <div className="flex flex-wrap items-center gap-2">
                {phaseStatuses.map((status, idx) => {
                  const IconComponent = getStatusIcon(status.icon || 'Clock');
                  return (
                    <React.Fragment key={status.code}>
                      <div className="flex items-center gap-2">
                        <div
                          onClick={() => handleEdit(status)}
                          className="flex items-center gap-2 px-3 py-2 rounded-lg border-2 cursor-pointer hover:shadow-md transition-all"
                          style={{ borderColor: status.color, backgroundColor: `${status.color}15` }}
                        >
                          <div className="w-8 h-8 rounded-full flex items-center justify-center" style={{ backgroundColor: status.color }}>
                            <IconComponent className="h-4 w-4 text-white" />
                          </div>
                          <div>
                            <p className="text-sm font-semibold" style={{ color: status.color }}>{status.name}</p>
                            <p className="text-xs text-slate-500">
                              #{status.order} • {status.triggeredBy}
                              {status.kpiCategory && <span className="ml-1 text-slate-400">• {status.kpiCategory}</span>}
                            </p>
                          </div>
                        </div>
                        <button
                          onClick={() => setChecklistStatus({ code: status.code, name: status.name })}
                          className="p-2 text-green-600 hover:text-green-800 hover:bg-green-50 rounded transition-colors"
                          title="Manage Process Checklist"
                        >
                          <ListChecks className="h-4 w-4" />
                        </button>
                      </div>
                      {idx < phaseStatuses.length - 1 && (
                        <ArrowRight className="h-4 w-4 text-slate-300" />
                      )}
                    </React.Fragment>
                  );
                })}
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* List View */}
      {viewMode === 'list' && (
        <Card className="overflow-hidden">
          <table className="w-full">
            <thead className="bg-slate-50">
              <tr>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-600">#</th>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-600">Code</th>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-600">Name</th>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-600">Phase</th>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-600">Description</th>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-600">Triggered By</th>
                {selectedWorkflow === 'KPI' && (
                  <th className="px-4 py-2 text-left text-xs font-semibold text-slate-600">Category</th>
                )}
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-600">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filteredStatuses.sort((a, b) => a.order - b.order).map((status, index) => {
                const IconComponent = getStatusIcon(status.icon || 'Clock');
                return (
                  <tr key={status.code} className="hover:bg-slate-50">
                    <td className="px-4 py-3">
                      <span className="inline-flex w-6 h-6 rounded-full items-center justify-center text-xs font-bold text-white flex-shrink-0" style={{ backgroundColor: status.color }}>
                        {index + 1}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <code className="text-xs bg-slate-100 px-2 py-1 rounded">{status.code}</code>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <IconComponent className="h-4 w-4" style={{ color: status.color }} />
                        <span className="text-sm font-medium">{status.name}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span className="text-xs px-2 py-1 rounded-full bg-slate-100 text-slate-600">{status.phase}</span>
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-600 max-w-xs truncate">{status.description}</td>
                    <td className="px-4 py-3">
                      <span className="text-xs px-2 py-1 rounded-full bg-slate-100 text-slate-700">{status.triggeredBy}</span>
                    </td>
                    {selectedWorkflow === 'KPI' && (
                      <td className="px-4 py-3">
                        <span className="text-xs px-2 py-1 rounded-full bg-green-100 text-green-700">{status.kpiCategory}</span>
                      </td>
                    )}
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => setChecklistStatus({ code: status.code, name: status.name })}
                          className="text-green-600 hover:text-green-800 text-xs flex items-center gap-1"
                          title="Manage Process Checklist"
                        >
                          <ListChecks className="h-3 w-3" />
                          Process
                        </button>
                        <button
                          onClick={() => handleEdit(status)}
                          className="text-blue-600 hover:text-blue-800 text-xs flex items-center gap-1"
                        >
                          <Edit className="h-3 w-3" />
                          Edit
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </Card>
      )}

      {/* Visual Flow Diagram */}
      <Card className="mt-6 p-4">
        <h3 className="text-sm font-semibold text-slate-700 mb-4">Complete {selectedWorkflow} Flow Diagram</h3>
        <div className="bg-slate-50 rounded-lg p-4 overflow-x-auto">
          <div className="flex items-center gap-1 text-xs min-w-max">
            {filteredStatuses
              .sort((a, b) => a.order - b.order)
              .map((status, idx, arr) => (
              <React.Fragment key={status.code}>
                <div
                  className="px-2 py-1 rounded text-white font-medium whitespace-nowrap flex-shrink-0"
                  style={{ backgroundColor: status.color }}
                  title={status.description}
                >
                  {status.name}
                </div>
                {idx < arr.length - 1 && <ArrowRight className="h-3 w-3 text-slate-400 flex-shrink-0" />}
              </React.Fragment>
            ))}
          </div>
        </div>
      </Card>

      {/* Checklist Manager Modal */}
      {checklistStatus && (
        <OrderStatusChecklistManager
          statusCode={checklistStatus.code}
          statusName={checklistStatus.name}
          isOpen={!!checklistStatus}
          onClose={() => setChecklistStatus(null)}
        />
      )}

      {/* Edit/Create Modal */}
      {showModal && (
        <Modal
          isOpen={showModal}
          onClose={() => { setShowModal(false); setEditingStatus(null); }}
          title={editingStatus ? `Edit ${selectedWorkflow} Status` : `Add ${selectedWorkflow} Status`}
          size="lg"
        >
          <form onSubmit={handleSave} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Code *</label>
                <input
                  type="text"
                  value={formData.code}
                  onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-md text-sm"
                  placeholder="e.g., OrderCompleted"
                  disabled={!!editingStatus}
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Name *</label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-md text-sm"
                  placeholder="e.g., Order Completed"
                  required
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-md text-sm"
                rows={2}
                placeholder="Describe when this status is used"
              />
            </div>

            {/* Position in Flow - only show when creating new status */}
            {!editingStatus && (
              <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg">
                <label className="block text-sm font-medium text-blue-800 mb-2">Position in Flow *</label>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <select
                      value={formData.insertPosition || 'after'}
                      onChange={(e) => {
                        const pos = e.target.value;
                        let newOrder = filteredStatuses.length + 1;
                        if (pos === 'at_start') newOrder = 1;
                        if (pos === 'at_end') newOrder = filteredStatuses.length + 1;
                        setFormData({ ...formData, insertPosition: pos, order: newOrder });
                      }}
                      className="w-full px-3 py-2 border border-blue-300 rounded-md text-sm bg-white"
                    >
                      <option value="after">Insert After</option>
                      <option value="before">Insert Before</option>
                      <option value="at_end">Add at End</option>
                      <option value="at_start">Add at Start</option>
                    </select>
                  </div>
                  {(formData.insertPosition === 'after' || formData.insertPosition === 'before' || !formData.insertPosition) && (
                    <div>
                      <select
                        value={formData.insertRelativeTo || ''}
                        onChange={(e) => {
                          const selectedStatus = filteredStatuses.find(s => s.code === e.target.value);
                          if (selectedStatus) {
                            const newOrder = formData.insertPosition === 'before' 
                              ? selectedStatus.order 
                              : selectedStatus.order + 1;
                            setFormData({ ...formData, insertRelativeTo: e.target.value, order: newOrder });
                          }
                        }}
                        className="w-full px-3 py-2 border border-blue-300 rounded-md text-sm bg-white"
                      >
                        <option value="">Select status...</option>
                        {filteredStatuses.sort((a, b) => a.order - b.order).map((s) => (
                          <option key={s.code} value={s.code}>
                            #{s.order} - {s.name}
                          </option>
                        ))}
                      </select>
                    </div>
                  )}
                </div>
                {formData.insertRelativeTo && (
                  <p className="mt-2 text-xs text-blue-600">
                    New status will be inserted at position #{formData.order} 
                    {formData.insertPosition === 'after' ? ' (after ' : ' (before '}
                    {filteredStatuses.find(s => s.code === formData.insertRelativeTo)?.name})
                  </p>
                )}
                {formData.insertPosition === 'at_end' && (
                  <p className="mt-2 text-xs text-blue-600">
                    New status will be added at position #{filteredStatuses.length + 1} (end of flow)
                  </p>
                )}
                {formData.insertPosition === 'at_start' && (
                  <p className="mt-2 text-xs text-blue-600">
                    New status will be added at position #1 (start of flow)
                  </p>
                )}
              </div>
            )}

            <div className="grid grid-cols-3 gap-4">
              {editingStatus && (
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Order</label>
                  <input
                    type="number"
                    value={formData.order}
                    onChange={(e) => setFormData({ ...formData, order: parseInt(e.target.value) || 0 })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-md text-sm"
                    min={1}
                  />
                </div>
              )}
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Phase</label>
                <input
                  type="text"
                  value={formData.phase}
                  onChange={(e) => setFormData({ ...formData, phase: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-md text-sm"
                  placeholder="e.g., FieldWork, Billing"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Color</label>
                <input
                  type="color"
                  value={formData.color}
                  onChange={(e) => setFormData({ ...formData, color: e.target.value })}
                  className="w-full h-10 border border-slate-300 rounded-md cursor-pointer"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Triggered By</label>
                <select
                  value={formData.triggeredBy}
                  onChange={(e) => setFormData({ ...formData, triggeredBy: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-md text-sm"
                >
                  <option value="Admin">Admin</option>
                  <option value="SI">SI</option>
                  <option value="System">System</option>
                  <option value="SI/Admin">SI/Admin</option>
                  <option value="Admin/System">Admin/System</option>
                  <option value="Warehouse">Warehouse</option>
                  <option value="Partner">Partner</option>
                  <option value="Employer">Employer</option>
                </select>
              </div>
            </div>

            {/* KPI Category - only for KPI workflow */}
            {selectedWorkflow === 'KPI' && (
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">KPI Category</label>
                <select
                  value={formData.kpiCategory}
                  onChange={(e) => setFormData({ ...formData, kpiCategory: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-md text-sm"
                >
                  <option value="SI">SI (Service Installer)</option>
                  <option value="Admin">Admin</option>
                  <option value="Employer">Employer</option>
                </select>
              </div>
            )}

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Icon</label>
              <div className="flex flex-wrap gap-2">
                {Object.keys(ICON_MAP).map((iconName) => {
                  const IconComp = ICON_MAP[iconName];
                  return (
                    <button
                      type="button"
                      key={iconName}
                      onClick={() => setFormData({ ...formData, icon: iconName })}
                      className={`p-2 rounded border ${formData.icon === iconName ? 'border-blue-500 bg-blue-50' : 'border-slate-200 hover:border-slate-300'}`}
                      title={iconName}
                    >
                      <IconComp className="h-4 w-4" />
                    </button>
                  );
                })}
              </div>
            </div>
            <div className="flex justify-end gap-2 pt-4 border-t">
              <Button type="button" variant="outline" onClick={() => { setShowModal(false); setEditingStatus(null); }}>
                Cancel
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? 'Saving...' : (editingStatus ? 'Update' : 'Create')}
              </Button>
            </div>
          </form>
        </Modal>
      )}
      </div>
    </PageShell>
  );
};

export default OrderStatusesPage;

