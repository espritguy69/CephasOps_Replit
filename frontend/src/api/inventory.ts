import apiClient from './client';
import { getApiBaseUrl } from './config';
import type {
  Material,
  MaterialCategory,
  StockLocation,
  StockBalance,
  StockMovement,
  SerialisedItem,
  RmaRequest,
  CreateMaterialRequest,
  UpdateMaterialRequest,
  CreateMaterialCategoryRequest,
  UpdateMaterialCategoryRequest,
  RecordStockMovementRequest,
  RegisterSerialisedItemRequest,
  CreateRmaRequestRequest,
  UpdateRmaRequestRequest,
  MaterialFilters,
  StockBalanceFilters,
  StockMovementFilters,
  SerialisedItemFilters,
  RmaRequestFilters,
  ImportResult
} from '../types/inventory';

/**
 * Inventory & RMA API
 * Handles materials, stock locations, stock movements, serialised items, and RMA tickets
 */

/**
 * Get authentication token from storage
 * @returns Auth token or empty string
 */
const getAuthToken = (): string => {
  return localStorage.getItem('authToken') || '';
};

/**
 * Map backend MaterialDto to frontend Material type
 */
const mapMaterialDtoToMaterial = (dto: any): Material => {
  return {
    id: dto.id || dto.Id || '',
    name: dto.name || dto.Description || '',
    code: dto.code || dto.itemCode || dto.ItemCode || '',
    itemCode: dto.itemCode || dto.ItemCode || dto.code || '',
    description: dto.description || dto.Description || '',
    // Material Category (new FK-based system)
    materialCategoryId: dto.materialCategoryId || dto.MaterialCategoryId || undefined,
    materialCategoryName: dto.materialCategoryName || dto.MaterialCategoryName || undefined,
    // Legacy category fields (kept for backward compatibility)
    categoryId: dto.categoryId || dto.category || dto.Category || '',
    category: dto.category || dto.Category || dto.categoryId || '',
    categoryName: dto.categoryName || dto.CategoryName || '',
    // Material Verticals
    materialVerticalIds: dto.materialVerticalIds || dto.MaterialVerticalIds || [],
    materialVerticalNames: dto.materialVerticalNames || dto.MaterialVerticalNames || [],
    // Material Tags
    materialTagIds: dto.materialTagIds || dto.MaterialTagIds || [],
    materialTagNames: dto.materialTagNames || dto.MaterialTagNames || [],
    materialTagColors: dto.materialTagColors || dto.MaterialTagColors || [],
    // Material Attributes
    materialAttributes: dto.materialAttributes || dto.MaterialAttributes || [],
    unit: dto.unit || dto.unitOfMeasure || dto.UnitOfMeasure || '',
    unitOfMeasure: dto.unitOfMeasure || dto.UnitOfMeasure || dto.unit || '',
    unitPrice: dto.unitPrice ?? dto.defaultCost ?? dto.DefaultCost ?? undefined,
    defaultCost: dto.defaultCost ?? dto.DefaultCost ?? dto.unitPrice ?? undefined,
    isSerialised: dto.isSerialised ?? dto.IsSerialised ?? false,
    isActive: dto.isActive ?? dto.IsActive ?? true,
    partnerId: dto.partnerId || dto.PartnerId || undefined,
    partnerName: dto.partnerName || dto.PartnerName || undefined,
    partnerIds: dto.partnerIds || dto.PartnerIds || [],
    partnerNames: dto.partnerNames || dto.PartnerNames || [],
    barcode: dto.barcode || dto.Barcode || undefined,
    createdAt: dto.createdAt || dto.CreatedAt || undefined,
    updatedAt: dto.updatedAt || dto.UpdatedAt || undefined,
  };
};

/**
 * Get materials list
 * @param filters - Optional filters (category, search, isActive)
 * @returns Array of materials
 */
export const getMaterials = async (filters: MaterialFilters = {}): Promise<Material[]> => {
  const response = await apiClient.get<any[]>('/inventory/materials', { params: filters });
  // Map backend DTOs to frontend Material type
  return Array.isArray(response) ? response.map(mapMaterialDtoToMaterial) : [];
};

/**
 * Get material by ID
 * @param materialId - Material ID
 * @returns Material details
 */
export const getMaterial = async (materialId: string): Promise<Material> => {
  const response = await apiClient.get<any>(`/inventory/materials/${materialId}`);
  return mapMaterialDtoToMaterial(response);
};

/**
 * Get material by barcode
 * @param barcode - Barcode to search for
 * @returns Material details if found, null otherwise
 */
export const getMaterialByBarcode = async (barcode: string): Promise<Material | null> => {
  try {
    const response = await apiClient.get<any>(`/inventory/materials/by-barcode/${encodeURIComponent(barcode)}`);
    return mapMaterialDtoToMaterial(response);
  } catch (error: any) {
    if (error.status === 404) {
      return null;
    }
    throw error;
  }
};

/**
 * Map frontend CreateMaterialRequest to backend CreateMaterialDto
 */
const mapCreateRequestToDto = (request: CreateMaterialRequest): any => {
  return {
    ItemCode: request.itemCode || request.code || '',
    Description: request.description || request.name || '',
    // Material Category (new FK-based system)
    MaterialCategoryId: request.materialCategoryId || undefined,
    // Legacy category field (kept for backward compatibility)
    Category: request.category || request.categoryId || null,
    // Material Verticals
    MaterialVerticalIds: request.materialVerticalIds || [],
    // Material Tags
    MaterialTagIds: request.materialTagIds || [],
    // Material Attributes
    MaterialAttributes: request.materialAttributes?.map(attr => ({
      Key: attr.key,
      Value: attr.value,
      DataType: attr.dataType || 'String',
      DisplayOrder: attr.displayOrder || 0
    })) || [],
    IsSerialised: request.isSerialised,
    UnitOfMeasure: request.unitOfMeasure || request.unit || '',
    DefaultCost: request.defaultCost ?? request.unitPrice ?? null,
    PartnerIds: request.partnerIds?.map(id => typeof id === 'string' ? id : id) || [],
    PartnerId: request.partnerId || undefined,
    DepartmentId: request.departmentId || undefined,
    Barcode: request.barcode || undefined,
  };
};

/**
 * Create material
 * @param materialData - Material creation data
 * @returns Created material
 */
export const createMaterial = async (materialData: CreateMaterialRequest): Promise<Material> => {
  const dto = mapCreateRequestToDto(materialData);
  const response = await apiClient.post<any>(`/inventory/materials`, dto);
  return mapMaterialDtoToMaterial(response);
};

/**
 * Map frontend UpdateMaterialRequest to backend UpdateMaterialDto
 */
const mapUpdateRequestToDto = (request: UpdateMaterialRequest): any => {
  const dto: any = {};
  if (request.itemCode !== undefined || request.code !== undefined) {
    dto.ItemCode = request.itemCode || request.code || null;
  }
  if (request.description !== undefined || request.name !== undefined) {
    dto.Description = request.description || request.name || null;
  }
  // Material Category (new FK-based system)
  if (request.materialCategoryId !== undefined) {
    dto.MaterialCategoryId = request.materialCategoryId || null;
  }
  // Legacy category field (kept for backward compatibility)
  if (request.category !== undefined || request.categoryId !== undefined) {
    dto.Category = request.category || request.categoryId || null;
  }
  // Material Verticals
  if (request.materialVerticalIds !== undefined) {
    dto.MaterialVerticalIds = request.materialVerticalIds;
  }
  // Material Tags
  if (request.materialTagIds !== undefined) {
    dto.MaterialTagIds = request.materialTagIds;
  }
  // Material Attributes
  if (request.materialAttributes !== undefined) {
    dto.MaterialAttributes = request.materialAttributes?.map(attr => ({
      Key: attr.key,
      Value: attr.value,
      DataType: attr.dataType || 'String',
      DisplayOrder: attr.displayOrder || 0
    })) || [];
  }
  if (request.unitOfMeasure !== undefined || request.unit !== undefined) {
    dto.UnitOfMeasure = request.unitOfMeasure || request.unit || null;
  }
  if (request.defaultCost !== undefined || request.unitPrice !== undefined) {
    dto.DefaultCost = request.defaultCost ?? request.unitPrice ?? null;
  }
  if (request.isSerialised !== undefined) {
    dto.IsSerialised = request.isSerialised;
  }
  if (request.isActive !== undefined) {
    dto.IsActive = request.isActive;
  }
  if (request.partnerIds !== undefined) {
    dto.PartnerIds = request.partnerIds;
  }
  if (request.partnerId !== undefined) {
    dto.PartnerId = request.partnerId || null;
  }
  if (request.departmentId !== undefined) {
    dto.DepartmentId = request.departmentId || null;
  }
  if (request.barcode !== undefined) {
    dto.Barcode = request.barcode || null;
  }
  return dto;
};

/**
 * Update material
 * @param materialId - Material ID
 * @param materialData - Material update data
 * @returns Updated material
 */
export const updateMaterial = async (materialId: string, materialData: UpdateMaterialRequest): Promise<Material> => {
  const dto = mapUpdateRequestToDto(materialData);
  const response = await apiClient.put<any>(`/inventory/materials/${materialId}`, dto);
  return mapMaterialDtoToMaterial(response);
};

/**
 * Delete material
 * @param materialId - Material ID
 * @returns Promise that resolves when material is deleted
 */
export const deleteMaterial = async (materialId: string): Promise<void> => {
  await apiClient.delete(`/inventory/materials/${materialId}`);
};

/**
 * Material Categories API
 */

/**
 * Get material categories list
 * @param filters - Optional filters (isActive)
 * @returns Array of material categories
 */
export const getMaterialCategories = async (filters: { isActive?: boolean } = {}): Promise<MaterialCategory[]> => {
  try {
    // Use settings endpoint as material categories are reference data
    const response = await apiClient.get<any>('/settings/material-categories', { params: filters });
    // Handle response envelope: response.data.data or response.data or direct array
    // API client returns response.json(), so if backend uses envelope, it's response.data
    const data = (response && typeof response === 'object' && 'data' in response) 
      ? (Array.isArray(response.data) ? response.data : (response.data?.data || []))
      : (Array.isArray(response) ? response : []);
    return Array.isArray(data) ? data : [];
  } catch (error) {
    console.error('Error fetching material categories:', error);
    return []; // Return empty array on error to prevent page crash
  }
};

/**
 * Material Verticals API
 */

/**
 * Get material verticals list
 * @param filters - Optional filters (isActive)
 * @returns Array of material verticals
 */
export const getMaterialVerticals = async (filters: { isActive?: boolean } = {}): Promise<import('../types/inventory').MaterialVertical[]> => {
  try {
    const response = await apiClient.get<any>('/inventory/material-verticals', { params: filters });
    const data = (response && typeof response === 'object' && 'data' in response) 
      ? (Array.isArray(response.data) ? response.data : (response.data?.data || []))
      : (Array.isArray(response) ? response : []);
    return Array.isArray(data) ? data : [];
  } catch (error) {
    console.error('Error fetching material verticals:', error);
    return [];
  }
};

/**
 * Material Tags API
 */

/**
 * Get material tags list
 * @param filters - Optional filters (isActive)
 * @returns Array of material tags
 */
export const getMaterialTags = async (filters: { isActive?: boolean } = {}): Promise<import('../types/inventory').MaterialTag[]> => {
  try {
    const response = await apiClient.get<any>('/inventory/material-tags', { params: filters });
    const data = (response && typeof response === 'object' && 'data' in response) 
      ? (Array.isArray(response.data) ? response.data : (response.data?.data || []))
      : (Array.isArray(response) ? response : []);
    return Array.isArray(data) ? data : [];
  } catch (error) {
    console.error('Error fetching material tags:', error);
    return [];
  }
};

/**
 * Get material tag by ID
 * @param tagId - Tag ID
 * @returns Tag details
 */
export const getMaterialTag = async (tagId: string): Promise<import('../types/inventory').MaterialTag> => {
  const response = await apiClient.get<any>(`/inventory/material-tags/${tagId}`);
  return response?.data?.data || response?.data || response;
};

/**
 * Create material tag
 * @param tagData - Tag creation data
 * @returns Created tag
 */
export const createMaterialTag = async (tagData: { name: string; description?: string; color?: string; displayOrder?: number; isActive?: boolean }): Promise<import('../types/inventory').MaterialTag> => {
  const response = await apiClient.post<any>('/inventory/material-tags', tagData);
  return response?.data?.data || response?.data || response;
};

/**
 * Update material tag
 * @param tagId - Tag ID
 * @param tagData - Tag update data
 * @returns Updated tag
 */
export const updateMaterialTag = async (tagId: string, tagData: { name?: string; description?: string; color?: string; displayOrder?: number; isActive?: boolean }): Promise<import('../types/inventory').MaterialTag> => {
  const response = await apiClient.put<any>(`/inventory/material-tags/${tagId}`, tagData);
  return response?.data?.data || response?.data || response;
};

/**
 * Delete material tag
 * @param tagId - Tag ID
 * @returns Promise that resolves when tag is deleted
 */
export const deleteMaterialTag = async (tagId: string): Promise<void> => {
  await apiClient.delete(`/inventory/material-tags/${tagId}`);
};

/**
 * Get material vertical by ID
 * @param verticalId - Vertical ID
 * @returns Vertical details
 */
export const getMaterialVertical = async (verticalId: string): Promise<import('../types/inventory').MaterialVertical> => {
  const response = await apiClient.get<any>(`/inventory/material-verticals/${verticalId}`);
  return response?.data?.data || response?.data || response;
};

/**
 * Create material vertical
 * @param verticalData - Vertical creation data
 * @returns Created vertical
 */
export const createMaterialVertical = async (verticalData: { code: string; name: string; description?: string; displayOrder?: number; isActive?: boolean }): Promise<import('../types/inventory').MaterialVertical> => {
  const response = await apiClient.post<any>('/inventory/material-verticals', verticalData);
  return response?.data?.data || response?.data || response;
};

/**
 * Update material vertical
 * @param verticalId - Vertical ID
 * @param verticalData - Vertical update data
 * @returns Updated vertical
 */
export const updateMaterialVertical = async (verticalId: string, verticalData: { code?: string; name?: string; description?: string; displayOrder?: number; isActive?: boolean }): Promise<import('../types/inventory').MaterialVertical> => {
  const response = await apiClient.put<any>(`/inventory/material-verticals/${verticalId}`, verticalData);
  return response?.data?.data || response?.data || response;
};

/**
 * Delete material vertical
 * @param verticalId - Vertical ID
 * @returns Promise that resolves when vertical is deleted
 */
export const deleteMaterialVertical = async (verticalId: string): Promise<void> => {
  await apiClient.delete(`/inventory/material-verticals/${verticalId}`);
};

/**
 * Get material category by ID
 * @param categoryId - Category ID
 * @returns Category details
 */
export const getMaterialCategory = async (categoryId: string): Promise<MaterialCategory> => {
  // Use settings endpoint as material categories are reference data
  const response = await apiClient.get<any>(`/settings/material-categories/${categoryId}`);
  // Handle response envelope: response.data.data or direct response
  return response?.data?.data || response?.data || response;
};

/**
 * Create material category
 * @param categoryData - Category creation data
 * @returns Created category
 */
export const createMaterialCategory = async (categoryData: CreateMaterialCategoryRequest): Promise<MaterialCategory> => {
  // Use settings endpoint as material categories are reference data
  const response = await apiClient.post<any>('/settings/material-categories', categoryData);
  // Handle response envelope: response.data.data or direct response
  return response?.data?.data || response?.data || response;
};

/**
 * Update material category
 * @param categoryId - Category ID
 * @param categoryData - Category update data
 * @returns Updated category
 */
export const updateMaterialCategory = async (
  categoryId: string,
  categoryData: UpdateMaterialCategoryRequest
): Promise<MaterialCategory> => {
  // Use settings endpoint as material categories are reference data
  const response = await apiClient.put<any>(`/settings/material-categories/${categoryId}`, categoryData);
  // Handle response envelope: response.data.data or direct response
  return response?.data?.data || response?.data || response;
};

/**
 * Delete material category
 * @param categoryId - Category ID
 * @returns Promise that resolves when category is deleted
 */
export const deleteMaterialCategory = async (categoryId: string): Promise<void> => {
  // Use settings endpoint as material categories are reference data
  await apiClient.delete(`/settings/material-categories/${categoryId}`);
};

/**
 * Stock Locations API
 */

/**
 * Get stock locations
 * @returns Array of stock locations
 */
export const getStockLocations = async (): Promise<StockLocation[]> => {
  const response = await apiClient.get<StockLocation[]>(`/inventory/stock/locations`);
  return response;
};

/**
 * Stock Balances API
 */

/**
 * Get stock balances
 * @param filters - Optional filters (locationId, materialId)
 * @returns Array of stock balances
 */
export const getStockByLocation = async (filters: StockBalanceFilters = {}): Promise<StockBalance[]> => {
  const response = await apiClient.get<StockBalance[]>('/inventory/stock', { params: filters });
  return response;
};

/**
 * Stock Movements API
 */

/**
 * Get stock movements
 * @param filters - Optional filters (materialId, locationId, movementType, dateRange)
 * @returns Array of stock movements
 */
export const getStockMovements = async (filters: StockMovementFilters = {}): Promise<StockMovement[]> => {
  const response = await apiClient.get<StockMovement[]>(`/inventory/stock/movements`, { params: filters });
  return response;
};

/**
 * Record stock movement
 * @param movementData - Stock movement data
 * @returns Stock movement record
 */
export const recordStockMovement = async (movementData: RecordStockMovementRequest): Promise<StockMovement> => {
  const response = await apiClient.post<StockMovement>(`/inventory/stock/movements`, movementData);
  return response;
};

/**
 * Serialised Items API
 */

/**
 * Get serialised items
 * @param filters - Optional filters (materialId, serialNumber, status)
 * @returns Array of serialised items
 */
export const getSerialisedItems = async (filters: SerialisedItemFilters = {}): Promise<SerialisedItem[]> => {
  const response = await apiClient.get<SerialisedItem[]>(`/inventory/serialised-items`, { params: filters });
  return response;
};

/**
 * Get serialised item by serial number
 * @param serialNumber - Serial number
 * @returns Serialised item details
 */
export const getSerialisedItem = async (serialNumber: string): Promise<SerialisedItem> => {
  const response = await apiClient.get<SerialisedItem>(`/inventory/serialised-items/${serialNumber}`);
  return response;
};

/**
 * Register serialised item
 * @param itemData - Serialised item data
 * @returns Registered serialised item
 */
export const registerSerialisedItem = async (itemData: RegisterSerialisedItemRequest): Promise<SerialisedItem> => {
  const response = await apiClient.post<SerialisedItem>(`/inventory/serialised-items`, itemData);
  return response;
};

/**
 * RMA Requests API
 */

/**
 * Get RMA requests
 * @param filters - Optional filters (partnerId, status, fromDate, toDate)
 * @returns Array of RMA requests
 */
export const getRmaRequests = async (filters: RmaRequestFilters = {}): Promise<RmaRequest[]> => {
  const response = await apiClient.get<RmaRequest[]>('/rma/requests', { params: filters });
  return response;
};

/**
 * Get RMA request by ID
 * @param rmaId - RMA request ID
 * @returns RMA request details
 */
export const getRmaRequest = async (rmaId: string): Promise<RmaRequest> => {
  const response = await apiClient.get<RmaRequest>(`/rma/requests/${rmaId}`);
  return response;
};

/**
 * Create RMA request
 * @param rmaData - RMA request data
 * @returns Created RMA request
 */
export const createRmaRequest = async (rmaData: CreateRmaRequestRequest): Promise<RmaRequest> => {
  const response = await apiClient.post<RmaRequest>('/rma/requests', rmaData);
  return response;
};

/**
 * Update RMA request
 * @param rmaId - RMA request ID
 * @param rmaData - RMA request update data
 * @returns Updated RMA request
 */
export const updateRmaRequest = async (rmaId: string, rmaData: UpdateRmaRequestRequest): Promise<RmaRequest> => {
  const response = await apiClient.put<RmaRequest>(`/rma/requests/${rmaId}`, rmaData);
  return response;
};

/**
 * Delete RMA request
 * @param rmaId - RMA request ID
 * @returns Promise that resolves when RMA request is deleted
 */
export const deleteRmaRequest = async (rmaId: string): Promise<void> => {
  await apiClient.delete(`/rma/requests/${rmaId}`);
};

/**
 * Materials - Import/Export
 */

/**
 * Export materials to CSV file
 * @param filters - Optional filters
 */
export const exportMaterials = async (filters: MaterialFilters = {}): Promise<void> => {
  const token = getAuthToken();
  const params = new URLSearchParams(filters as any).toString();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/inventory/materials/export${params ? '?' + params : ''}`;
  
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Export failed');
  }
  
  const blob = await response.blob();
  const filename = response.headers.get('Content-Disposition')?.match(/filename="(.+)"/)?.[1] || 'materials.csv';
  
  // Trigger download
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(downloadUrl);
  a.remove();
};

/**
 * Download materials CSV template
 */
export const downloadMaterialsTemplate = async (): Promise<void> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/inventory/materials/template`;
  
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Template download failed');
  }
  
  const blob = await response.blob();
  
  // Trigger download
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = 'materials-template.csv';
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(downloadUrl);
  a.remove();
};

/**
 * Import materials from CSV file
 * @param file - CSV file to import
 * @returns Import result
 */
export const importMaterials = async (file: File): Promise<ImportResult> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/inventory/materials/import`;
  
  const formData = new FormData();
  formData.append('file', file);
  
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    },
    body: formData
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Import failed');
  }
  
  return response.json();
};

