import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Download, Scan, LayoutGrid, List, Search, Package } from 'lucide-react';
import { getMaterials, createMaterial, updateMaterial, deleteMaterial, getMaterialCategories, getMaterialVerticals, getMaterialTags, exportMaterials, importMaterials, downloadMaterialsTemplate, getMaterialByBarcode } from '../../api/inventory';
import { getDepartments } from '../../api/departments';
import { getPartners } from '../../api/partners';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, StandardListTable, BulkActionsBar, ImportExportButtons, StatusBadge, Tabs, TabPanel } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { BarcodeScanner } from '../../components/scanner/BarcodeScanner';
import { GroupedMaterialTable } from '../../components/materials/GroupedMaterialTable';
import { useAuth } from '../../contexts/AuthContext';
import { canViewInventoryCost, canEditInventoryCost } from '../../utils/fieldPermissions';
import { cn } from '@/lib/utils';
import type { Material, CreateMaterialRequest, UpdateMaterialRequest, MaterialFilters, MaterialVertical, MaterialTag, MaterialAttribute } from '../../types/inventory';
import type { Department } from '../../types/departments';
import type { Partner } from '../../types/partners';

interface MaterialCategory {
  id: string;
  name: string;
  code?: string;
  description?: string;
  isActive: boolean;
}

interface MaterialFormData {
  itemCode: string;
  description: string;
  category: string; // Legacy category field
  materialCategoryId: string; // New FK-based category
  unitOfMeasure: string;
  isSerialised: boolean;
  defaultCost: string;
  departmentId: string;
  partnerIds: string[]; // Changed from partnerId to partnerIds (array)
  materialVerticalIds: string[]; // Material Verticals (many-to-many)
  materialTagIds: string[]; // Material Tags (many-to-many)
  materialAttributes: MaterialAttribute[]; // Material Attributes (key-value pairs)
  barcode: string;
  isActive: boolean;
}

interface MaterialFiltersState {
  isActive?: boolean;
}

const MaterialsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { user } = useAuth();
  const showCostColumn = canViewInventoryCost(user);
  const canEditCost = canEditInventoryCost(user);
  const [materials, setMaterials] = useState<Material[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [categories, setCategories] = useState<MaterialCategory[]>([]);
  const [materialVerticals, setMaterialVerticals] = useState<MaterialVertical[]>([]);
  const [materialTags, setMaterialTags] = useState<MaterialTag[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [timeInternetPartnerId, setTimeInternetPartnerId] = useState<string | null>(null);
  const [gponDepartmentId, setGponDepartmentId] = useState<string | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingMaterial, setEditingMaterial] = useState<Material | null>(null);
  const [selectedRows, setSelectedRows] = useState<string[]>([]);
  const [filters, setFilters] = useState<MaterialFiltersState>({
    isActive: undefined // Show all materials by default
  });
  
  // View mode: 'flat' or 'grouped'
  const [viewMode, setViewMode] = useState<'flat' | 'grouped'>(() => {
    const saved = localStorage.getItem('materialsViewMode');
    return (saved === 'flat' || saved === 'grouped') ? saved : 'grouped';
  });
  
  // Search query
  const [searchQuery, setSearchQuery] = useState('');
  
  // Category filter
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [formData, setFormData] = useState<MaterialFormData>({
    itemCode: '',
    description: '',
    category: '', // Legacy category field
    materialCategoryId: '', // New FK-based category
    unitOfMeasure: 'Unit',
    isSerialised: false,
    defaultCost: '',
    departmentId: '',
    partnerIds: [], // Changed from partnerId to partnerIds (array)
    materialVerticalIds: [], // Material Verticals
    materialTagIds: [], // Material Tags
    materialAttributes: [], // Material Attributes
    barcode: '',
    isActive: true
  });
  const [showBarcodeScanner, setShowBarcodeScanner] = useState(false);

  useEffect(() => {
    loadDepartments();
    loadCategories();
    loadMaterialVerticals();
    loadMaterialTags();
    loadPartners();
    loadMaterials();
  }, [filters]);

  // Save view mode to localStorage
  useEffect(() => {
    localStorage.setItem('materialsViewMode', viewMode);
  }, [viewMode]);

  const loadDepartments = async (): Promise<void> => {
    try {
      const data = await getDepartments({ isActive: true });
      setDepartments(Array.isArray(data) ? data : []);
      
      // Find GPON department
      const gpon = data.find((d: Department) => d.name?.toUpperCase() === 'GPON');
      if (gpon) {
        setGponDepartmentId(gpon.id);
      }
    } catch (err: any) {
      console.error('Error loading departments:', err);
      setDepartments([]);
    }
  };

  const loadPartners = async (): Promise<void> => {
    try {
      const data = await getPartners({ isActive: true });
      setPartners(Array.isArray(data) ? data : []);
      
      // Find TIME INTERNET partner
      const timeInternet = data.find((p: Partner) => {
        const nameUpper = p.name?.toUpperCase() || '';
        return (nameUpper.includes('TIME') && nameUpper.includes('INTERNET')) || 
               nameUpper === 'TIME INTERNET';
      });
      if (timeInternet) {
        setTimeInternetPartnerId(timeInternet.id);
      }
    } catch (err: any) {
      console.error('Error loading partners:', err);
      setPartners([]);
    }
  };

  const loadCategories = async (): Promise<void> => {
    try {
      const data = await getMaterialCategories({ isActive: true });
      setCategories(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading categories:', err);
      setCategories([]);
    }
  };

  const loadMaterialVerticals = async (): Promise<void> => {
    try {
      const data = await getMaterialVerticals({ isActive: true });
      setMaterialVerticals(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading material verticals:', err);
      setMaterialVerticals([]);
    }
  };

  const loadMaterialTags = async (): Promise<void> => {
    try {
      const data = await getMaterialTags({ isActive: true });
      setMaterialTags(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading material tags:', err);
      setMaterialTags([]);
    }
  };

  // Get partners filtered by selected department
  const getFilteredPartners = (): Partner[] => {
    if (!formData.departmentId) {
      return []; // Don't show any until department is selected
    }
    return partners.filter(p => p.departmentId === formData.departmentId);
  };

  // Auto-set TIME INTERNET when GPON department is selected
  useEffect(() => {
    if (formData.departmentId === gponDepartmentId && timeInternetPartnerId) {
      // Verify TIME INTERNET belongs to GPON
      const timeInternet = partners.find(
        p => p.id === timeInternetPartnerId && 
        p.departmentId === gponDepartmentId
      );
      if (timeInternet && !formData.partnerIds.includes(timeInternetPartnerId)) {
        setFormData(prev => ({ ...prev, partnerIds: [...prev.partnerIds, timeInternetPartnerId!] }));
      }
    } else if (formData.departmentId && formData.departmentId !== gponDepartmentId) {
      // Clear TIME INTERNET partner if switching away from GPON
      if (formData.partnerIds.includes(timeInternetPartnerId || '')) {
        setFormData(prev => ({ ...prev, partnerIds: prev.partnerIds.filter(id => id !== timeInternetPartnerId) }));
      }
    }
  }, [formData.departmentId, gponDepartmentId, timeInternetPartnerId, partners]);

  const loadMaterials = async (): Promise<void> => {
    try {
      setLoading(true);
      const params: MaterialFilters = {};
      if (filters.isActive !== undefined) params.isActive = filters.isActive;
      const data = await getMaterials(params);
      setMaterials(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load materials');
      console.error('Error loading materials:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      // Check for duplicate item code
      const itemCodeTrimmed = formData.itemCode.trim();
      const duplicate = materials.find(
        m => m.code?.toLowerCase() === itemCodeTrimmed.toLowerCase()
      );
      if (duplicate) {
        showError(`A material with item code "${itemCodeTrimmed}" already exists.`);
        return;
      }

      if (!formData.partnerIds || formData.partnerIds.length === 0) {
        showError('At least one partner is required.');
        return;
      }

      // Find category name from category ID
      const selectedCategory = categories.find(c => c.id === formData.category);
      const categoryName = selectedCategory?.name || formData.category || undefined;

      const materialData: CreateMaterialRequest = {
        itemCode: itemCodeTrimmed, // Use itemCode to match backend
        description: formData.description.trim(),
        // Material Category (new FK-based system)
        materialCategoryId: formData.materialCategoryId || undefined,
        // Legacy category field (kept for backward compatibility)
        category: categoryName, // Send category name (string), not ID
        // Material Verticals
        materialVerticalIds: formData.materialVerticalIds.length > 0 ? formData.materialVerticalIds : undefined,
        // Material Tags
        materialTagIds: formData.materialTagIds.length > 0 ? formData.materialTagIds : undefined,
        // Material Attributes
        materialAttributes: formData.materialAttributes.length > 0 ? formData.materialAttributes : undefined,
        unitOfMeasure: formData.unitOfMeasure.trim(),
        defaultCost: formData.defaultCost ? parseFloat(formData.defaultCost) : undefined,
        isSerialised: formData.isSerialised,
        isActive: formData.isActive ?? true,
        partnerIds: formData.partnerIds, // Send partnerIds array
        departmentId: formData.departmentId || undefined,
        barcode: formData.barcode.trim() || undefined
      };
      
      await createMaterial(materialData);
      showSuccess('Material created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadMaterials();
    } catch (err: any) {
      showError(err.message || 'Failed to create material');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingMaterial) return;
    try {
      // Check for duplicate item code (exclude current record)
      const itemCodeTrimmed = formData.itemCode.trim();
      const duplicate = materials.find(
        m => m.id !== editingMaterial.id && m.code?.toLowerCase() === itemCodeTrimmed.toLowerCase()
      );
      if (duplicate) {
        showError(`A material with item code "${itemCodeTrimmed}" already exists.`);
        return;
      }

      if (!formData.partnerIds || formData.partnerIds.length === 0) {
        showError('At least one partner is required.');
        return;
      }

      // Find category name from category ID
      const selectedCategory = categories.find(c => c.id === formData.category);
      const categoryName = selectedCategory?.name || formData.category || undefined;

      const materialData: UpdateMaterialRequest = {
        itemCode: itemCodeTrimmed, // Use itemCode to match backend
        description: formData.description.trim(),
        // Material Category (new FK-based system)
        materialCategoryId: formData.materialCategoryId || undefined,
        // Legacy category field (kept for backward compatibility)
        category: categoryName, // Send category name (string), not ID
        // Material Verticals
        materialVerticalIds: formData.materialVerticalIds.length > 0 ? formData.materialVerticalIds : [],
        // Material Tags
        materialTagIds: formData.materialTagIds.length > 0 ? formData.materialTagIds : [],
        // Material Attributes
        materialAttributes: formData.materialAttributes.length > 0 ? formData.materialAttributes : [],
        unitOfMeasure: formData.unitOfMeasure.trim(),
        defaultCost: formData.defaultCost ? parseFloat(formData.defaultCost) : undefined,
        isSerialised: formData.isSerialised,
        isActive: formData.isActive ?? true,
        partnerIds: formData.partnerIds, // Send partnerIds array
        departmentId: formData.departmentId || undefined,
        barcode: formData.barcode.trim() || undefined
      };
      
      await updateMaterial(editingMaterial.id, materialData);
      showSuccess('Material updated successfully!');
      setShowCreateModal(false);
      setEditingMaterial(null);
      resetForm();
      await loadMaterials();
    } catch (err: any) {
      showError(err.message || 'Failed to update material');
    }
  };

  const handleToggleStatus = async (material: Material): Promise<void> => {
    try {
      const materialData: UpdateMaterialRequest = {
        isActive: !material.isActive
      };
      
      await updateMaterial(material.id, materialData);
      showSuccess(`Material ${!material.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadMaterials();
    } catch (err: any) {
      showError(err.message || 'Failed to update material status');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this material?')) return;
    
    try {
      await deleteMaterial(id);
      showSuccess('Material deleted successfully!');
      await loadMaterials();
    } catch (err: any) {
      showError(err.message || 'Failed to delete material');
    }
  };

  const resetForm = (): void => {
    setFormData({
      itemCode: '',
      description: '',
      category: '', // Legacy category field
      materialCategoryId: '', // New FK-based category
      unitOfMeasure: 'Unit',
      isSerialised: false,
      defaultCost: '',
      departmentId: '',
      partnerIds: [], // Reset to empty array
      materialVerticalIds: [],
      materialTagIds: [],
      materialAttributes: [],
      barcode: '',
      isActive: true
    });
    setShowBarcodeScanner(false);
  };

  const handleBarcodeScan = async (barcode: string) => {
    try {
      // First, set the barcode in the form
      setFormData(prev => ({ ...prev, barcode }));

      // Try to lookup existing material by barcode
      const existingMaterial = await getMaterialByBarcode(barcode);
      
      if (existingMaterial) {
        // Material exists - populate form with existing data
        showSuccess(`Found existing material: ${existingMaterial.description || existingMaterial.name}`);
        
        const partnerIds = existingMaterial.partnerIds && existingMaterial.partnerIds.length > 0 
          ? existingMaterial.partnerIds 
          : (existingMaterial.partnerId ? [existingMaterial.partnerId] : []);
        
        const categoryName = existingMaterial.category || existingMaterial.categoryName || '';
        const categoryId = categoryName 
          ? categories.find(c => c.name === categoryName)?.id || ''
          : existingMaterial.categoryId || '';

        setFormData(prev => ({
          ...prev,
          itemCode: existingMaterial.code || existingMaterial.itemCode || prev.itemCode,
          description: existingMaterial.description || existingMaterial.name || prev.description,
          category: categoryId || prev.category,
          unitOfMeasure: existingMaterial.unitOfMeasure || existingMaterial.unit || prev.unitOfMeasure,
          isSerialised: existingMaterial.isSerialised ?? prev.isSerialised,
          defaultCost: (existingMaterial.defaultCost ?? existingMaterial.unitPrice)?.toString() || prev.defaultCost,
          departmentId: existingMaterial.departmentId || prev.departmentId,
          partnerIds: partnerIds.length > 0 ? partnerIds : prev.partnerIds
        }));
      } else {
        // New material - barcode is set, user can fill in other details
        showSuccess('Barcode scanned. Please fill in material details.');
      }
      
      setShowBarcodeScanner(false);
    } catch (error: any) {
      console.error('Error looking up barcode:', error);
      // Still set the barcode even if lookup fails
      setFormData(prev => ({ ...prev, barcode }));
      setShowBarcodeScanner(false);
    }
  };

  const openEditModal = async (material: Material): Promise<void> => {
    setEditingMaterial(material);
    await loadDepartments();
    await loadCategories();
    await loadPartners();
    // Use partnerIds if available, otherwise fallback to partnerId for backward compatibility
    const partnerIds = material.partnerIds && material.partnerIds.length > 0 
      ? material.partnerIds 
      : (material.partnerId ? [material.partnerId] : []);
    
    // Find category ID from category name (backend returns category as string name)
    const categoryName = material.category || material.categoryName || '';
    const categoryId = categoryName 
      ? categories.find(c => c.name === categoryName)?.id || ''
      : material.categoryId || '';

    setFormData({
      itemCode: material.code || material.itemCode || '',
      description: material.description || material.name || '',
      category: categoryId, // Use category ID for the form dropdown (legacy)
      materialCategoryId: material.materialCategoryId || '', // New FK-based category
      unitOfMeasure: material.unitOfMeasure || material.unit || 'Unit',
      isSerialised: material.isSerialised || false,
      defaultCost: (material.defaultCost ?? material.unitPrice)?.toString() || '',
      departmentId: '',
      partnerIds: partnerIds, // Populate with partnerIds array
      materialVerticalIds: material.materialVerticalIds || [],
      materialTagIds: material.materialTagIds || [],
      materialAttributes: material.materialAttributes || [],
      barcode: material.barcode || '',
      isActive: material.isActive ?? true
    });
  };

  // Check if user has elevated permissions
  const canManage = user?.roles?.some(r => r === 'SuperAdmin' || r === 'Director' || r === 'HeadOfDepartment' || r === 'Supervisor') ?? false;

  const handleDeleteMaterials = async (): Promise<void> => {
    if (!window.confirm(`Are you sure you want to delete ${selectedRows.length} material(s)?`)) return;
    try {
      const deletePromises = selectedRows.map((row: any) => deleteMaterial(row.id));
      await Promise.all(deletePromises);
      showSuccess(`Successfully deleted ${selectedRows.length} material(s)`);
      setSelectedRows([]);
      await loadMaterials();
    } catch (err: any) {
      showError(err.message || 'Failed to delete materials');
    }
  };

  const handleExportMaterials = async (): Promise<void> => {
    try {
      await exportMaterials(filters);
      showSuccess('Materials exported successfully');
    } catch (err: any) {
      showError(err.message || 'Failed to export materials');
    }
  };

  // Filter and search materials
  const filteredMaterials = useMemo(() => {
    let filtered = [...materials];

    // Apply category filter
    if (selectedCategory !== 'all') {
      filtered = filtered.filter(m => {
        const categoryName = m.category || m.categoryName || m.materialCategoryName || 'Uncategorized';
        return categoryName === selectedCategory;
      });
    }

    // Apply search query
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(m => 
        (m.description || m.name || '').toLowerCase().includes(query) ||
        (m.code || m.itemCode || '').toLowerCase().includes(query) ||
        (m.category || m.categoryName || m.materialCategoryName || '').toLowerCase().includes(query)
      );
    }

    return filtered;
  }, [materials, selectedCategory, searchQuery]);

  // Get unique categories for filter tabs
  const uniqueCategories = useMemo(() => {
    const categorySet = new Set<string>();
    materials.forEach(m => {
      const categoryName = m.category || m.categoryName || m.materialCategoryName || 'Uncategorized';
      categorySet.add(categoryName);
    });
    return Array.from(categorySet).sort();
  }, [materials]);

  const getStockLevelColor = (current?: number, min?: number): string => {
    if (!current || !min) return 'text-gray-500';
    const ratio = current / min;
    if (ratio < 1) return 'text-red-600 font-semibold';
    if (ratio >= 1 && ratio <= 1.5) return 'text-amber-600';
    return 'text-green-600';
  };

  const getStockLevelText = (material: Material): string => {
    const current = material.minStockLevel; // Using minStockLevel as current for now
    const min = material.reorderPoint || material.minStockLevel;
    if (!current && !min) return '—';
    return `${current || 0}/${min || 0}`;
  };

  const getSupplierText = (material: Material): string => {
    const partnerNames = (material.partnerNames && material.partnerNames.length > 0)
      ? material.partnerNames
      : (material.partnerName ? [material.partnerName] : []);
    
    if (partnerNames.length === 0) return '—';
    if (partnerNames.length === 1) return partnerNames[0];
    return `${partnerNames[0]} +${partnerNames.length - 1} more`;
  };

  if (loading) {
    return <LoadingSpinner message="Loading materials..." fullPage />;
  }

  return (
    <PageShell title="Materials" breadcrumbs={[{ label: 'Settings' }, { label: 'Materials' }]}>
    <div className="flex-1 p-4 md:p-6 max-w-7xl mx-auto">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-lg font-bold text-foreground">Materials</h1>
        <div className="flex items-center gap-2">
          <ImportExportButtons
            entityName="Materials"
            onExport={handleExportMaterials}
            onImport={async (file: File) => {
              const result = await importMaterials(file);
              await loadMaterials();
              return result;
            }}
            onDownloadTemplate={downloadMaterialsTemplate}
          />
          <Button onClick={() => setShowCreateModal(true)} className="flex items-center gap-2">
            <Plus className="h-4 w-4" />
            Add Material
          </Button>
        </div>
      </div>

      {/* Search and View Controls */}
      <Card className="mb-3">
        <div className="p-3 space-y-3">
          {/* Search Bar */}
          <div className="flex items-center gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-2 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search materials by name, code, or category..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-8 pr-3 py-1.5 text-sm border border-input rounded-md bg-background focus:outline-none focus:ring-1 focus:ring-ring"
              />
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  className="absolute right-2 top-1/2 transform -translate-y-1/2 text-muted-foreground hover:text-foreground"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>
            
            {/* View Toggle */}
            <div className="flex items-center gap-2">
              <span className="text-xs text-muted-foreground">View:</span>
              <div className="flex border rounded-md overflow-hidden">
                <button
                  onClick={() => setViewMode('flat')}
                  className={cn(
                    "px-3 py-1.5 text-xs font-medium transition-colors flex items-center gap-1",
                    viewMode === 'flat' 
                      ? "bg-primary text-primary-foreground" 
                      : "bg-background text-muted-foreground hover:bg-muted"
                  )}
                >
                  <List className="h-3 w-3" />
                  List
                </button>
                <button
                  onClick={() => setViewMode('grouped')}
                  className={cn(
                    "px-3 py-1.5 text-xs font-medium transition-colors flex items-center gap-1",
                    viewMode === 'grouped' 
                      ? "bg-primary text-primary-foreground" 
                      : "bg-background text-muted-foreground hover:bg-muted"
                  )}
                >
                  <LayoutGrid className="h-3 w-3" />
                  Grouped
                </button>
              </div>
            </div>
          </div>

          {/* Category Filter Tabs */}
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-xs font-medium text-muted-foreground">Category:</span>
            <div className="flex gap-1 flex-wrap">
              <button
                onClick={() => setSelectedCategory('all')}
                className={cn(
                  "px-2 py-1 text-xs font-medium rounded transition-colors",
                  selectedCategory === 'all'
                    ? "bg-primary text-primary-foreground"
                    : "bg-muted text-muted-foreground hover:bg-muted/80"
                )}
              >
                All
              </button>
              {uniqueCategories.map((category) => (
                <button
                  key={category}
                  onClick={() => setSelectedCategory(category)}
                  className={cn(
                    "px-2 py-1 text-xs font-medium rounded transition-colors flex items-center gap-1",
                    selectedCategory === category
                      ? "bg-primary text-primary-foreground"
                      : "bg-muted text-muted-foreground hover:bg-muted/80"
                  )}
                >
                  <Package className="h-3 w-3" />
                  {category}
                </button>
              ))}
            </div>
          </div>

          {/* Active Filters */}
          <div className="flex items-center gap-2">
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={filters.isActive === true}
                onChange={(e) => setFilters({ ...filters, isActive: e.target.checked ? true : undefined })}
                className="h-3 w-3 rounded border-gray-300"
              />
              <span className="text-xs">Active Only</span>
            </label>
            {(searchQuery || selectedCategory !== 'all') && (
              <span className="text-xs text-muted-foreground">
                Showing {filteredMaterials.length} of {materials.length} materials
              </span>
            )}
          </div>
        </div>
      </Card>

      {/* Materials Table */}
      <Card>
        {/* Bulk Actions Bar */}
        {selectedRows.length > 0 && (
          <BulkActionsBar
            selectedCount={selectedRows.length}
            onClearSelection={() => setSelectedRows([])}
            actions={[
              {
                label: 'Export',
                icon: Download,
                onClick: handleExportMaterials
              },
              ...(canManage ? [{
                label: 'Delete',
                icon: Trash2,
                variant: 'destructive' as const,
                onClick: handleDeleteMaterials
              }] : [])
            ]}
          />
        )}

        {/* Materials View */}
        {viewMode === 'grouped' ? (
          <div className="p-3">
            <GroupedMaterialTable
              materials={filteredMaterials}
              onEdit={openEditModal}
              onDelete={(material) => handleDelete(material.id)}
              searchQuery={searchQuery}
              emptyMessage="No materials found. Try adjusting your filters or add a new material."
              canManage={canManage}
              showCostColumn={showCostColumn}
              selectedRows={selectedRows}
              onSelectionChange={setSelectedRows}
            />
          </div>
        ) : (
          <StandardListTable
            data={filteredMaterials}
            selectedRows={selectedRows}
            onSelectionChange={setSelectedRows}
            onRowClick={(row: Material) => openEditModal(row)}
            columns={[
              {
                key: 'description',
                label: 'Material Name',
                render: (value: unknown, row: Material) => {
                  const name = row.description || row.name || '—';
                  const code = row.code || row.itemCode || '';
                  return (
                    <div className="space-y-0.5">
                      <div className="text-sm font-medium">{name}</div>
                      <div className="text-xs text-muted-foreground truncate max-w-md">{code}</div>
                    </div>
                  );
                }
              },
              {
                key: 'category',
                label: 'Category',
                render: (value: unknown, row: Material) => (
                  <span className="text-xs">
                    {row.category || row.categoryName || row.materialCategoryName || 'Uncategorized'}
                  </span>
                )
              },
              {
                key: 'unit',
                label: 'Unit',
                render: (value: unknown, row: Material) => (
                  <span className="text-xs">{row.unit || row.unitOfMeasure || '—'}</span>
                )
              },
              ...(showCostColumn ? [{
                key: 'unitPrice',
                label: 'Unit Price',
                render: (value: unknown, row: Material) => (
                  <div className="text-right text-xs">
                    {row.unitPrice || row.defaultCost
                      ? `RM ${((row.unitPrice || row.defaultCost || 0) as number).toFixed(2)}`
                      : '—'}
                  </div>
                )
              }] : []),
              {
                key: 'stockLevel',
                label: 'Stock Level',
                render: (value: unknown, row: Material) => (
                  <span className={cn("text-xs", getStockLevelColor(row.minStockLevel, row.reorderPoint))}>
                    {getStockLevelText(row)}
                  </span>
                )
              },
              {
                key: 'supplier',
                label: 'Supplier',
                render: (value: unknown, row: Material) => (
                  <span className="text-xs text-muted-foreground truncate max-w-[120px]">
                    {getSupplierText(row)}
                  </span>
                )
              },
              {
                key: 'isActive',
                label: 'Status',
                render: (value: unknown, row: Material) => (
                  <div className="flex justify-center">
                    <StatusBadge
                      variant={row.isActive ? 'success' : 'secondary'}
                    >
                      {row.isActive ? 'Active' : 'Inactive'}
                    </StatusBadge>
                  </div>
                )
              }
            ]}
            actions={{
              onEdit: openEditModal,
              ...(canManage && {
                onDeactivate: handleToggleStatus,
                onDelete: (row: Material) => handleDelete(row.id)
              })
            }}
            pageSize={20}
            loading={loading}
            emptyMessage="Add your first material to get started."
          />
        )}
      </Card>

      {/* Create/Edit Material Modal */}
      <Modal
        isOpen={showCreateModal || editingMaterial !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingMaterial(null);
          resetForm();
        }}
        title={editingMaterial ? 'Edit Material' : 'Create Material'}
        size="lg"
      >
        <div className="space-y-2">
          {/* Barcode Scanner */}
          {showBarcodeScanner && (
            <div className="border border-border rounded-lg p-3 bg-muted/30">
              <BarcodeScanner
                isOpen={showBarcodeScanner}
                onScan={handleBarcodeScan}
                onClose={() => setShowBarcodeScanner(false)}
              />
            </div>
          )}

          {/* Barcode Field */}
          <div className="space-y-1">
            <div className="flex items-center justify-between">
              <label className="text-xs font-medium">Barcode (Optional)</label>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setShowBarcodeScanner(!showBarcodeScanner)}
                className="h-6 text-xs"
              >
                <Scan className="h-3 w-3 mr-1" />
                {showBarcodeScanner ? 'Hide Scanner' : 'Scan Barcode'}
              </Button>
            </div>
            <TextInput
              value={formData.barcode}
              onChange={(e) => setFormData({ ...formData, barcode: e.target.value })}
              placeholder="Enter or scan barcode..."
              onBlur={async () => {
                // Auto-lookup when barcode is entered manually
                if (formData.barcode.trim() && !editingMaterial) {
                  try {
                    const existingMaterial = await getMaterialByBarcode(formData.barcode.trim());
                    if (existingMaterial) {
                      showSuccess(`Found existing material: ${existingMaterial.description || existingMaterial.name}`);
                      // Optionally populate form with existing material data
                    }
                  } catch (error) {
                    // Material not found - that's okay, it's a new material
                  }
                }
              }}
            />
          </div>

          <div className="grid grid-cols-2 gap-2">
            <TextInput
              label="Item Code *"
              value={formData.itemCode}
              onChange={(e) => setFormData({ ...formData, itemCode: e.target.value })}
              required
              placeholder="e.g., CAE-000-0820"
            />
            <div className="space-y-0.5">
              <label className="text-xs font-medium">Unit of Measure *</label>
              <select
                value={formData.unitOfMeasure}
                onChange={(e) => setFormData({ ...formData, unitOfMeasure: e.target.value })}
                className="flex h-8 w-full rounded border border-input bg-background px-2 py-1 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              >
                <option value="Unit">Unit</option>
                <option value="Piece">Piece</option>
                <option value="Meter">Meter</option>
                <option value="pcs">pcs</option>
                <option value="m">m</option>
                <option value="kg">kg</option>
              </select>
            </div>
          </div>

          <TextInput
            label="Description *"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            required
            placeholder="e.g., Huawei HG8145X6 - Dual-band WiFi 6 ONT"
          />

          <div className="grid grid-cols-2 gap-2">
            <div className="space-y-0.5">
              <label className="text-xs font-medium">Category (New)</label>
              <select
                value={formData.materialCategoryId || ''}
                onChange={(e) => setFormData({ ...formData, materialCategoryId: e.target.value || '' })}
                className="flex h-8 w-full rounded border border-input bg-background px-2 py-1 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              >
                <option value="">Select Category</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-0.5">
              <label className="text-xs font-medium">Category (Legacy)</label>
              <select
                value={formData.category || ''}
                onChange={(e) => setFormData({ ...formData, category: e.target.value || '' })}
                className="flex h-8 w-full rounded border border-input bg-background px-2 py-1 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              >
                <option value="">Select Category (Legacy)</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-0.5">
              <label className="text-xs font-medium">Department *</label>
              <select
                value={formData.departmentId || ''}
                onChange={(e) => setFormData({ ...formData, departmentId: e.target.value || '' })}
                className="flex h-8 w-full rounded border border-input bg-background px-2 py-1 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                required
              >
                <option value="">Select Department</option>
                {departments.map((dept) => (
                  <option key={dept.id} value={dept.id}>
                    {dept.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="space-y-0.5">
            <label className="text-xs font-medium">Partners *</label>
            {!formData.departmentId ? (
              <div className="text-xs text-muted-foreground p-2 border border-dashed rounded bg-muted/30">
                Select Department First
              </div>
            ) : (
              <div className="space-y-2">
                <div className="space-y-1 max-h-[200px] overflow-y-auto border border-input rounded p-2 bg-background">
                  {getFilteredPartners().length === 0 ? (
                    <div className="text-xs text-muted-foreground p-2 text-center">
                      No partners available for this department
                    </div>
                  ) : (
                    getFilteredPartners().map((partner) => (
                      <label
                        key={partner.id}
                        className="flex items-center space-x-2 p-1.5 hover:bg-accent rounded cursor-pointer transition-colors"
                      >
                        <input
                          type="checkbox"
                          checked={formData.partnerIds.includes(partner.id)}
                          onChange={(e) => {
                            if (e.target.checked) {
                              setFormData({
                                ...formData,
                                partnerIds: [...formData.partnerIds, partner.id]
                              });
                            } else {
                              setFormData({
                                ...formData,
                                partnerIds: formData.partnerIds.filter(id => id !== partner.id)
                              });
                            }
                          }}
                          className="h-4 w-4 rounded border-input text-primary focus:ring-2 focus:ring-ring focus:ring-offset-1 cursor-pointer"
                        />
                        <span className="text-xs select-none">{partner.name}</span>
                      </label>
                    ))
                  )}
                </div>
                {formData.partnerIds.length > 0 && (
                  <div className="text-xs text-muted-foreground">
                    {formData.partnerIds.length} partner(s) selected
                  </div>
                )}
                {formData.partnerIds.length === 0 && formData.departmentId && (
                  <div className="text-xs text-destructive">
                    At least one partner is required.
                  </div>
                )}
              </div>
            )}
          </div>

          {canEditCost && (
            <div className="grid grid-cols-2 gap-2">
              <TextInput
                label="Default Cost"
                type="number"
                step="0.01"
                value={formData.defaultCost}
                onChange={(e) => setFormData({ ...formData, defaultCost: e.target.value })}
                placeholder="0.00"
              />
            </div>
          )}

          <div className="flex items-center gap-3 pt-2">
            <input
              type="checkbox"
              id="isSerialised"
              checked={formData.isSerialised}
              onChange={(e) => setFormData({ ...formData, isSerialised: e.target.checked })}
              className="h-3 w-3 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="isSerialised" className="text-xs font-medium cursor-pointer">
              Serialized Item
            </label>
          </div>

          {/* Material Verticals */}
          <div className="space-y-0.5">
            <label className="text-xs font-medium">Material Verticals (Optional)</label>
            <div className="space-y-1 max-h-[150px] overflow-y-auto border border-input rounded p-2 bg-background">
              {materialVerticals.length === 0 ? (
                <div className="text-xs text-muted-foreground p-2 text-center">
                  No verticals available
                </div>
              ) : (
                materialVerticals.map((vertical) => (
                  <label
                    key={vertical.id}
                    className="flex items-center space-x-2 p-1.5 hover:bg-accent rounded cursor-pointer transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={formData.materialVerticalIds.includes(vertical.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setFormData({
                            ...formData,
                            materialVerticalIds: [...formData.materialVerticalIds, vertical.id]
                          });
                        } else {
                          setFormData({
                            ...formData,
                            materialVerticalIds: formData.materialVerticalIds.filter(id => id !== vertical.id)
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-2 focus:ring-ring focus:ring-offset-1 cursor-pointer"
                    />
                    <span className="text-xs select-none">{vertical.name}</span>
                  </label>
                ))
              )}
            </div>
          </div>

          {/* Material Tags */}
          <div className="space-y-0.5">
            <label className="text-xs font-medium">Material Tags (Optional)</label>
            <div className="space-y-1 max-h-[150px] overflow-y-auto border border-input rounded p-2 bg-background">
              {materialTags.length === 0 ? (
                <div className="text-xs text-muted-foreground p-2 text-center">
                  No tags available
                </div>
              ) : (
                materialTags.map((tag) => (
                  <label
                    key={tag.id}
                    className="flex items-center space-x-2 p-1.5 hover:bg-accent rounded cursor-pointer transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={formData.materialTagIds.includes(tag.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setFormData({
                            ...formData,
                            materialTagIds: [...formData.materialTagIds, tag.id]
                          });
                        } else {
                          setFormData({
                            ...formData,
                            materialTagIds: formData.materialTagIds.filter(id => id !== tag.id)
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-2 focus:ring-ring focus:ring-offset-1 cursor-pointer"
                    />
                    <span
                      className="text-xs select-none px-2 py-0.5 rounded"
                      style={{
                        backgroundColor: tag.color ? `${tag.color}20` : undefined,
                        color: tag.color || undefined,
                        border: tag.color ? `1px solid ${tag.color}` : undefined
                      }}
                    >
                      {tag.name}
                    </span>
                  </label>
                ))
              )}
            </div>
          </div>

          {/* Material Attributes */}
          <div className="space-y-0.5">
            <label className="text-xs font-medium">Material Attributes (Optional)</label>
            <div className="space-y-2 border border-input rounded p-2 bg-background">
              {formData.materialAttributes.map((attr, index) => (
                <div key={index} className="flex gap-2 items-center">
                  <input
                    type="text"
                    placeholder="Key"
                    value={attr.key}
                    onChange={(e) => {
                      const newAttrs = [...formData.materialAttributes];
                      newAttrs[index] = { ...newAttrs[index], key: e.target.value };
                      setFormData({ ...formData, materialAttributes: newAttrs });
                    }}
                    className="flex-1 h-7 rounded border border-input bg-background px-2 py-1 text-xs"
                  />
                  <input
                    type="text"
                    placeholder="Value"
                    value={attr.value}
                    onChange={(e) => {
                      const newAttrs = [...formData.materialAttributes];
                      newAttrs[index] = { ...newAttrs[index], value: e.target.value };
                      setFormData({ ...formData, materialAttributes: newAttrs });
                    }}
                    className="flex-1 h-7 rounded border border-input bg-background px-2 py-1 text-xs"
                  />
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      const newAttrs = formData.materialAttributes.filter((_, i) => i !== index);
                      setFormData({ ...formData, materialAttributes: newAttrs });
                    }}
                    className="h-7 w-7 p-0"
                  >
                    <X className="h-3 w-3" />
                  </Button>
                </div>
              ))}
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => {
                  setFormData({
                    ...formData,
                    materialAttributes: [...formData.materialAttributes, { key: '', value: '', dataType: 'String' }]
                  });
                }}
                className="w-full text-xs"
              >
                <Plus className="h-3 w-3 mr-1" />
                Add Attribute
              </Button>
            </div>
          </div>

          <div className="flex items-center gap-3 pt-2">
            <input
              type="checkbox"
              id="isActive"
              checked={formData.isActive}
              onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
              className="h-3 w-3 rounded border-gray-300 text-primary focus:ring-primary"
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
                setEditingMaterial(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={editingMaterial ? handleUpdate : handleCreate}
              className="flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {editingMaterial ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default MaterialsPage;

