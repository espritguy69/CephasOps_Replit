import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Filter, Building2 } from 'lucide-react';
import { 
  getSkills,
  getSkillsByCategory,
  createSkill,
  updateSkill,
  deleteSkill,
  type CreateSkillRequest,
  type UpdateSkillRequest
} from '../../api/skills';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, StandardListTable, Select } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import { useDepartment } from '../../contexts/DepartmentContext';
import { getDepartments } from '../../api/departments';
import { SKILL_CATEGORIES, type Skill, type SkillCategory } from '../../types/serviceInstallers';
import type { Department } from '../../types/departments';

interface SkillFormData {
  name: string;
  code: string;
  category: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
  departmentId: string;
}

const SkillsManagementPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { user } = useAuth();
  const { activeDepartment } = useDepartment();
  const [skills, setSkills] = useState<Skill[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingSkill, setEditingSkill] = useState<Skill | null>(null);
  const [selectedRows, setSelectedRows] = useState<string[]>([]);
  const [categoryFilter, setCategoryFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [formData, setFormData] = useState<SkillFormData>({
    name: '',
    code: '',
    category: SKILL_CATEGORIES[0] || 'FiberSkills',
    description: '',
    displayOrder: 0,
    isActive: true,
    departmentId: ''
  });
  const [formErrors, setFormErrors] = useState<Partial<Record<keyof SkillFormData, string>>>({});

  const canManage = user?.roles?.some(r => r === 'SuperAdmin' || r === 'Director' || r === 'HeadOfDepartment' || r === 'Supervisor') ?? false;

  useEffect(() => {
    loadDepartments();
    loadSkills();
  }, [activeDepartment?.id]);

  const loadDepartments = async (): Promise<void> => {
    try {
      const depts = await getDepartments({ isActive: true });
      setDepartments(depts);
    } catch (err: any) {
      console.error('Error loading departments:', err);
    }
  };

  const loadSkills = async (): Promise<void> => {
    try {
      setLoading(true);
      const departmentId = activeDepartment?.id;
      const data = await getSkills(departmentId, undefined, undefined);
      setSkills(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading skills:', err);
      showError(err.message || 'Failed to load skills');
      setSkills([]);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: '',
      code: '',
      category: SKILL_CATEGORIES[0] || 'FiberSkills',
      description: '',
      displayOrder: 0,
      isActive: true,
      departmentId: activeDepartment?.id || ''
    });
    setFormErrors({});
  };

  const validateForm = (): boolean => {
    const errors: Partial<Record<keyof SkillFormData, string>> = {};

    if (!formData.name.trim()) {
      errors.name = 'Skill name is required';
    }

    if (!formData.code.trim()) {
      errors.code = 'Skill code is required';
    } else if (!/^[A-Z0-9_]+$/.test(formData.code)) {
      errors.code = 'Code must contain only uppercase letters, numbers, and underscores';
    }

    if (!formData.category) {
      errors.category = 'Category is required';
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleCreate = async (): Promise<void> => {
    if (!validateForm()) {
      return;
    }

    try {
      // Check for duplicate code
      const codeTrimmed = formData.code.trim().toUpperCase();
      const exists = skills.some(
        s => s.code.toUpperCase() === codeTrimmed
      );

      if (exists) {
        showError(`A skill with code "${codeTrimmed}" already exists.`);
        return;
      }

      const createData: CreateSkillRequest = {
        name: formData.name.trim(),
        code: codeTrimmed,
        category: formData.category,
        description: formData.description.trim() || undefined,
        displayOrder: formData.displayOrder,
        isActive: formData.isActive,
        departmentId: formData.departmentId || undefined
      };

      await createSkill(createData);

      showSuccess('Skill created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadSkills();
    } catch (err: any) {
      console.error('Error creating skill:', err);
      showError(err.message || 'Failed to create skill');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingSkill || !validateForm()) {
      return;
    }

    try {
      // Check for duplicate code (exclude current record)
      const codeTrimmed = formData.code.trim().toUpperCase();
      const exists = skills.some(
        s => s.id !== editingSkill.id && s.code.toUpperCase() === codeTrimmed
      );

      if (exists) {
        showError(`A skill with code "${codeTrimmed}" already exists.`);
        return;
      }

      const updateData: UpdateSkillRequest = {
        name: formData.name.trim(),
        code: codeTrimmed,
        category: formData.category,
        description: formData.description.trim() || undefined,
        displayOrder: formData.displayOrder,
        isActive: formData.isActive,
        departmentId: formData.departmentId || undefined
      };

      await updateSkill(editingSkill.id, updateData);

      showSuccess('Skill updated successfully!');
      setEditingSkill(null);
      resetForm();
      await loadSkills();
    } catch (err: any) {
      console.error('Error updating skill:', err);
      showError(err.message || 'Failed to update skill');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    const skill = skills.find(s => s.id === id);
    if (!skill) return;

    if (!window.confirm(`Are you sure you want to delete skill "${skill.name}"? This action cannot be undone and may affect service installers who have this skill assigned.`)) {
      return;
    }

    try {
      await deleteSkill(id);
      showSuccess('Skill deleted successfully!');
      await loadSkills();
    } catch (err: any) {
      console.error('Error deleting skill:', err);
      showError(err.message || 'Failed to delete skill');
    }
  };

  const handleToggleStatus = async (skill: Skill): Promise<void> => {
    try {
      await updateSkill(skill.id, {
        isActive: !skill.isActive
      });
      showSuccess(`Skill ${!skill.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadSkills();
    } catch (err: any) {
      console.error('Error toggling skill status:', err);
      showError(err.message || 'Failed to update skill status');
    }
  };

  const openEditModal = async (skill: Skill): Promise<void> => {
    setEditingSkill(skill);
    setFormData({
      name: skill.name || '',
      code: skill.code || '',
      category: skill.category || SKILL_CATEGORIES[0] || 'FiberSkills',
      description: skill.description || '',
      displayOrder: skill.displayOrder || 0,
      isActive: skill.isActive ?? true,
      departmentId: skill.departmentId || activeDepartment?.id || ''
    });
  };

  // Filter skills
  const filteredSkills = skills.filter(skill => {
    if (categoryFilter !== 'all' && skill.category !== categoryFilter) {
      return false;
    }
    if (statusFilter !== 'all') {
      const isActive = statusFilter === 'active';
      if (skill.isActive !== isActive) {
        return false;
      }
    }
    return true;
  });

  // Sort by category, then display order, then name
  const sortedSkills = [...filteredSkills].sort((a, b) => {
    if (a.category !== b.category) {
      return a.category.localeCompare(b.category);
    }
    if (a.displayOrder !== b.displayOrder) {
      return a.displayOrder - b.displayOrder;
    }
    return a.name.localeCompare(b.name);
  });

  if (loading) {
    return (
      <PageShell title="Skills" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Skills' }]}>
        <LoadingSpinner message="Loading skills..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Skills Management"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Skills' }]}
      actions={
        canManage ? (
          <Button
            onClick={() => {
              resetForm();
              setShowCreateModal(true);
            }}
            className="gap-2"
          >
            <Plus className="h-4 w-4" />
            Add Skill
          </Button>
        ) : undefined
      }
    >
      <div className="space-y-4">
      <Card>
        <div className="p-4 border-b">
          <div>
            <div className="flex items-center gap-2 mb-1">
              {activeDepartment && (
                <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded border border-blue-200">
                  <Building2 className="h-3 w-3" />
                  {activeDepartment.name}
                </span>
              )}
            </div>
            <p className="text-sm text-muted-foreground mt-1">
              Manage skills that can be assigned to service installers (e.g., Fiber Splicing, ONT Installation, Safety Compliance)
              {activeDepartment && (
                <span className="ml-1 text-blue-600">• Department: {activeDepartment.name}</span>
              )}
            </p>
          </div>
        </div>

        {/* Filters */}
        <div className="p-4 border-b bg-gray-50 flex gap-4 items-center flex-wrap">
          <div className="flex items-center gap-2">
            <Filter className="h-4 w-4 text-gray-500" />
            <span className="text-sm font-medium">Filters:</span>
          </div>
          
          <div className="flex items-center gap-2">
            <label className="text-sm text-muted-foreground">Category:</label>
            <select
              value={categoryFilter}
              onChange={(e) => setCategoryFilter(e.target.value)}
              className="px-3 py-1.5 text-sm rounded border border-gray-300 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="all">All Categories</option>
              {SKILL_CATEGORIES.map(cat => (
                <option key={cat} value={cat}>{cat}</option>
              ))}
            </select>
          </div>

          <div className="flex items-center gap-2">
            <label className="text-sm text-muted-foreground">Status:</label>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="px-3 py-1.5 text-sm rounded border border-gray-300 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="all">All</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>

          {(categoryFilter !== 'all' || statusFilter !== 'all') && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setCategoryFilter('all');
                setStatusFilter('all');
              }}
            >
              Clear Filters
            </Button>
          )}
        </div>

        {/* Skills Table */}
        <StandardListTable
          data={sortedSkills}
          selectedRows={selectedRows}
          onSelectionChange={setSelectedRows}
          onRowClick={(row: Skill) => canManage && openEditModal(row)}
          columns={[
            { key: 'category', label: 'Category' },
            { key: 'name', label: 'Name' },
            { key: 'code', label: 'Code' },
            { 
              key: 'departmentName', 
              label: 'Department',
              render: (value: unknown) => value ? (
                <span className="text-sm text-gray-700">{value as string}</span>
              ) : (
                <span className="text-xs text-gray-400">All Departments</span>
              )
            },
            { key: 'description', label: 'Description' },
            { 
              key: 'displayOrder', 
              label: 'Order',
              render: (value: unknown) => value ?? 0
            },
            { 
              key: 'isActive', 
              label: 'Status',
              render: (value: unknown) => (
                <span className={`px-2 py-0.5 rounded text-xs ${
                  value ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                }`}>
                  {value ? 'Active' : 'Inactive'}
                </span>
              )
            }
          ]}
          actions={{
            onEdit: canManage ? openEditModal : undefined,
            ...(canManage && {
              onDeactivate: handleToggleStatus,
              onDelete: (row: Skill) => handleDelete(row.id)
            })
          }}
          pageSize={50}
          loading={loading}
          emptyMessage={
            categoryFilter !== 'all' || statusFilter !== 'all'
              ? "No skills match your filter criteria."
              : "No skills found. Add your first skill to get started."
          }
        />
      </Card>

      {/* Create/Edit Skill Modal */}
      <Modal
        isOpen={showCreateModal || editingSkill !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingSkill(null);
          resetForm();
        }}
        title={editingSkill ? 'Edit Skill' : 'Create Skill'}
        size="md"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Skill Name *"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
              placeholder="e.g., Fiber Splicing (Fusion)"
              error={formErrors.name}
            />

            <TextInput
              label="Skill Code *"
              value={formData.code}
              onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
              required
              placeholder="e.g., FIBER_SPLICE_FUSION"
              error={formErrors.code}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium">Category *</label>
              <select
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
                required
              >
                {SKILL_CATEGORIES.map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
              </select>
              {formErrors.category && (
                <p className="text-sm text-red-600">{formErrors.category}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-sm font-medium">Department</label>
              <select
                value={formData.departmentId}
                onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
                className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
              >
                <option value="">All Departments</option>
                {departments.map(dept => (
                  <option key={dept.id} value={dept.id}>{dept.name}</option>
                ))}
              </select>
            </div>
          </div>

          <TextInput
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            placeholder="Optional detailed description of the skill"
            multiline
            rows={3}
          />

          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Display Order"
              type="number"
              value={formData.displayOrder.toString()}
              onChange={(e) => setFormData({ ...formData, displayOrder: parseInt(e.target.value) || 0 })}
              placeholder="0"
            />

            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="h-4 w-4 rounded border-gray-300"
                />
                <label htmlFor="isActive" className="text-sm text-muted-foreground">
                  Active
                </label>
              </div>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button
              variant="outline"
              onClick={() => {
                setShowCreateModal(false);
                setEditingSkill(null);
                resetForm();
              }}
            >
              <X className="h-4 w-4 mr-2" />
              Cancel
            </Button>
            <Button
              onClick={editingSkill ? handleUpdate : handleCreate}
              className="gap-2"
            >
              <Save className="h-4 w-4" />
              {editingSkill ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default SkillsManagementPage;

