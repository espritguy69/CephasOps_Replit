import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Package, Power } from 'lucide-react';
import {
  getMaterialTemplates,
  createMaterialTemplate,
  updateMaterialTemplate,
  deleteMaterialTemplate,
} from '../../api/settings';
import { getBuildingTypes } from '../../api/buildingTypes';
import { getPartners } from '../../api/partners';
import { getMaterials } from '../../api/inventory';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, Select } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type {
  MaterialTemplate,
  CreateMaterialTemplateRequest,
  UpdateMaterialTemplateRequest,
  MaterialTemplateFilters,
} from '../../types/settings';
import type { ReferenceDataItem } from '../../types/referenceData';
import type { Partner } from '../../types/partners';
import type { Material } from '../../types/inventory';

interface MaterialTemplateFormData {
  name: string;
  buildingTypeId: string;
  partnerId: string;
  isActive: boolean;
  materials: Array<{
    materialId: string;
    quantity: number | string;
  }>;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const MaterialTemplatesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [templates, setTemplates] = useState<MaterialTemplate[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingTemplate, setEditingTemplate] = useState<MaterialTemplate | null>(null);
  const [formData, setFormData] = useState<MaterialTemplateFormData>({
    name: '',
    buildingTypeId: '',
    partnerId: '',
    isActive: true,
    materials: [],
  });
  const [buildingTypes, setBuildingTypes] = useState<ReferenceDataItem[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [availableMaterials, setAvailableMaterials] = useState<Material[]>([]);
  const [filters, setFilters] = useState<MaterialTemplateFilters>({});

  useEffect(() => {
    loadTemplates();
    loadReferenceData();
  }, [filters]);

  const loadReferenceData = async (): Promise<void> => {
    try {
      const [buildingTypesData, partnersData, materialsData] = await Promise.all([
        getBuildingTypes({ isActive: true }),
        getPartners({ isActive: true }),
        getMaterials({ isActive: true }),
      ]);
      setBuildingTypes(Array.isArray(buildingTypesData) ? buildingTypesData : []);
      setPartners(Array.isArray(partnersData) ? partnersData : []);
      setAvailableMaterials(Array.isArray(materialsData) ? materialsData : []);
    } catch (err: any) {
      console.error('Error loading reference data:', err);
    }
  };

  const loadTemplates = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getMaterialTemplates(filters);
      setTemplates(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load material templates');
      console.error('Error loading templates:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      if (!formData.name.trim()) {
        showError('Template name is required');
        return;
      }

      if (formData.materials.length === 0) {
        showError('At least one material is required');
        return;
      }

      const templateData: CreateMaterialTemplateRequest = {
        name: formData.name.trim(),
        buildingTypeId: formData.buildingTypeId || undefined,
        partnerId: formData.partnerId || undefined,
        materials: formData.materials.map((m) => ({
          materialId: m.materialId,
          quantity: typeof m.quantity === 'string' ? parseFloat(m.quantity) || 0 : m.quantity,
        })),
        isActive: formData.isActive,
      };

      await createMaterialTemplate(templateData);
      showSuccess('Material template created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to create material template');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingTemplate) return;
    try {
      if (!formData.name.trim()) {
        showError('Template name is required');
        return;
      }

      if (formData.materials.length === 0) {
        showError('At least one material is required');
        return;
      }

      const templateData: UpdateMaterialTemplateRequest = {
        name: formData.name.trim(),
        buildingTypeId: formData.buildingTypeId || undefined,
        partnerId: formData.partnerId || undefined,
        materials: formData.materials.map((m) => ({
          materialId: m.materialId,
          quantity: typeof m.quantity === 'string' ? parseFloat(m.quantity) || 0 : m.quantity,
        })),
        isActive: formData.isActive,
      };

      await updateMaterialTemplate(editingTemplate.id, templateData);
      showSuccess('Material template updated successfully!');
      setEditingTemplate(null);
      resetForm();
      await loadTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to update material template');
    }
  };

  const handleDelete = async (templateId: string): Promise<void> => {
    if (!confirm('Are you sure you want to delete this material template?')) {
      return;
    }

    try {
      await deleteMaterialTemplate(templateId);
      showSuccess('Material template deleted successfully!');
      await loadTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to delete material template');
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: '',
      buildingTypeId: '',
      partnerId: '',
      isActive: true,
      materials: [],
    });
  };

  const openEditModal = (template: MaterialTemplate): void => {
    setEditingTemplate(template);
    setFormData({
      name: template.name,
      buildingTypeId: template.buildingTypeId || '',
      partnerId: template.partnerId || '',
      isActive: template.isActive,
      materials: template.materials.map((m) => ({
        materialId: m.materialId,
        quantity: m.quantity,
      })),
    });
  };

  const addMaterial = (): void => {
    setFormData({
      ...formData,
      materials: [...formData.materials, { materialId: '', quantity: 1 }],
    });
  };

  const removeMaterial = (index: number): void => {
    setFormData({
      ...formData,
      materials: formData.materials.filter((_, i) => i !== index),
    });
  };

  const updateMaterial = (index: number, field: 'materialId' | 'quantity', value: string | number): void => {
    const updatedMaterials = [...formData.materials];
    updatedMaterials[index] = {
      ...updatedMaterials[index],
      [field]: value,
    };
    setFormData({
      ...formData,
      materials: updatedMaterials,
    });
  };

  const columns: TableColumn<MaterialTemplate>[] = [
    {
      key: 'name',
      label: 'Name',
      render: (value, row) => (
        <div className="flex items-center gap-2">
          <span className="font-medium">{value as string}</span>
          {!row.isActive && (
            <span className="text-xs px-2 py-0.5 rounded-full bg-gray-100 text-gray-600">Inactive</span>
          )}
        </div>
      ),
    },
    {
      key: 'buildingTypeName',
      label: 'Building Type',
      render: (value) => <span className="text-muted-foreground">{value || 'Any'}</span>,
    },
    {
      key: 'partnerName',
      label: 'Partner',
      render: (value) => <span className="text-muted-foreground">{value || 'Any'}</span>,
    },
    {
      key: 'materials',
      label: 'Materials',
      render: (value) => {
        const materials = value as MaterialTemplate['materials'];
        return (
          <div className="flex flex-wrap gap-1">
            {materials.length > 0 ? (
              materials.slice(0, 3).map((m, idx) => (
                <span key={idx} className="text-xs px-2 py-0.5 rounded bg-primary/10 text-primary">
                  {m.materialName || m.materialId} ({m.quantity})
                </span>
              ))
            ) : (
              <span className="text-xs text-muted-foreground">No materials</span>
            )}
            {materials.length > 3 && (
              <span className="text-xs text-muted-foreground">+{materials.length - 3} more</span>
            )}
          </div>
        );
      },
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_, row) => (
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => openEditModal(row)}
            className="h-8"
          >
            <Edit className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => handleDelete(row.id)}
            className="h-8 text-destructive hover:text-destructive"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ];

  if (loading) {
    return (
      <PageShell title="Material Templates" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Material Templates' }]}>
        <LoadingSpinner message="Loading material templates..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Material Templates"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Material Templates' }]}
      actions={
        <Button size="sm" onClick={() => setShowCreateModal(true)} className="gap-1">
          <Plus className="h-4 w-4" />
          Create Template
        </Button>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      <p className="text-muted-foreground text-sm">
        Configure default materials for orders based on building type and partner
      </p>
        {/* Filters */}
        <Card className="p-4 mb-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="text-sm font-medium mb-2 block">Building Type</label>
              <Select
                value={filters.buildingType || ''}
                onChange={(e) => setFilters({ ...filters, buildingType: e.target.value || undefined })}
              >
                <option value="">All Building Types</option>
                {buildingTypes.map((bt) => (
                  <option key={bt.id} value={bt.id}>
                    {bt.name}
                  </option>
                ))}
              </Select>
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Partner</label>
              <Select
                value={filters.partnerId || ''}
                onChange={(e) => setFilters({ ...filters, partnerId: e.target.value || undefined })}
              >
                <option value="">All Partners</option>
                {partners.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </Select>
            </div>
            <div className="flex items-end">
              <Button
                variant="outline"
                onClick={() => setFilters({})}
                className="w-full"
              >
                Clear Filters
              </Button>
            </div>
          </div>
        </Card>
      </div>

      {templates.length === 0 ? (
        <EmptyState
          title="No material templates found"
          description="Create your first material template to get started"
          action={
            <Button onClick={() => setShowCreateModal(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Create Template
            </Button>
          }
        />
      ) : (
        <Card>
          <DataTable columns={columns} data={templates} />
        </Card>
      )}

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingTemplate !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingTemplate(null);
          resetForm();
        }}
        title={editingTemplate ? 'Edit Material Template' : 'Create Material Template'}
        size="lg"
      >
        <div className="space-y-4">
          <div>
            <label className="text-sm font-medium mb-2 block">Template Name *</label>
            <TextInput
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="e.g., Standard Residential GPON"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-sm font-medium mb-2 block">Building Type (Optional)</label>
              <Select
                value={formData.buildingTypeId}
                onChange={(e) => setFormData({ ...formData, buildingTypeId: e.target.value })}
              >
                <option value="">Any Building Type</option>
                {buildingTypes.map((bt) => (
                  <option key={bt.id} value={bt.id}>
                    {bt.name}
                  </option>
                ))}
              </Select>
            </div>

            <div>
              <label className="text-sm font-medium mb-2 block">Partner (Optional)</label>
              <Select
                value={formData.partnerId}
                onChange={(e) => setFormData({ ...formData, partnerId: e.target.value })}
              >
                <option value="">Any Partner</option>
                {partners.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </Select>
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium">Materials *</label>
              <Button variant="outline" size="sm" onClick={addMaterial}>
                <Plus className="h-4 w-4 mr-1" />
                Add Material
              </Button>
            </div>
            <div className="space-y-2 max-h-64 overflow-y-auto">
              {formData.materials.length === 0 ? (
                <p className="text-sm text-muted-foreground text-center py-4">
                  No materials added. Click "Add Material" to get started.
                </p>
              ) : (
                formData.materials.map((material, index) => (
                  <div key={index} className="flex items-center gap-2 p-2 bg-muted rounded border">
                    <div className="flex-1">
                      <Select
                        value={material.materialId}
                        onChange={(e) => updateMaterial(index, 'materialId', e.target.value)}
                      >
                        <option value="">Select Material</option>
                        {availableMaterials.map((m) => (
                          <option key={m.id} value={m.id}>
                            {m.name || m.code} {m.code ? `(${m.code})` : ''}
                          </option>
                        ))}
                      </Select>
                    </div>
                    <div className="w-24">
                      <TextInput
                        type="number"
                        min="0"
                        step="0.01"
                        value={material.quantity}
                        onChange={(e) => updateMaterial(index, 'quantity', e.target.value)}
                        placeholder="Qty"
                      />
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => removeMaterial(index)}
                      className="text-destructive hover:text-destructive"
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isActive"
              checked={formData.isActive}
              onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
              className="rounded"
            />
            <label htmlFor="isActive" className="text-sm font-medium cursor-pointer">
              Active
            </label>
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button
              variant="outline"
              onClick={() => {
                setShowCreateModal(false);
                setEditingTemplate(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={editingTemplate ? handleUpdate : handleCreate}
              className="flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {editingTemplate ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
    </PageShell>
  );
};

export default MaterialTemplatesPage;

