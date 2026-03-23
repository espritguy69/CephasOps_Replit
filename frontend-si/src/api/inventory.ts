import apiClient from './client';

/**
 * Inventory API
 * Handles material and stock-related API calls
 */

export interface Material {
  id: string;
  name: string;
  code?: string;
  category?: string;
  categoryName?: string;
  unit?: string;
  description?: string;
  isActive?: boolean;
  barcode?: string;
  isSerialised?: boolean; // Whether this material requires serial number tracking
}

export interface StockLevel {
  materialId: string;
  materialName: string;
  warehouseId?: string;
  warehouseName?: string;
  quantity: number;
  reservedQuantity?: number;
  availableQuantity?: number;
  unit?: string;
}

export interface StockMovement {
  id: string;
  materialId: string;
  materialName?: string;
  quantity: number;
  movementType: string;
  fromLocation?: string;
  toLocation?: string;
  orderId?: string;
  serialNumber?: string;
  createdAt?: string;
}

export interface ScanRecord {
  id: string;
  serialNumber: string;
  deviceType?: string;
  materialId?: string;
  materialName?: string;
  orderId?: string;
  orderNumber?: string;
  scannedBy?: string;
  scannedAt?: string;
  location?: {
    latitude: number;
    longitude: number;
  };
}

export interface MaterialFilters {
  categoryId?: string;
  isActive?: boolean;
  search?: string;
}

export interface StockFilters {
  warehouseId?: string;
  materialId?: string;
  lowStockOnly?: boolean;
}

/**
 * Get all materials
 */
export const getAllMaterials = async (filters: MaterialFilters = {}): Promise<Material[]> => {
  const response = await apiClient.get<Material[] | { data: Material[] }>('/inventory/materials', {
    params: filters
  });
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: Material[] }).data;
  }
  return [];
};

/**
 * Get material by ID
 */
export const getMaterial = async (materialId: string): Promise<Material> => {
  const response = await apiClient.get<Material | { data: Material }>(`/inventory/materials/${materialId}`);
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: Material }).data;
  }
  return response as Material;
};

/**
 * Get material by barcode
 */
export const getMaterialByBarcode = async (barcode: string): Promise<Material | null> => {
  try {
    const response = await apiClient.get<Material | { data: Material }>(`/inventory/materials/by-barcode/${encodeURIComponent(barcode)}`);
    
    if (response && typeof response === 'object' && 'data' in response) {
      return (response as { data: Material }).data;
    }
    return response as Material;
  } catch (error: any) {
    if (error.response?.status === 404) {
      return null; // Material not found
    }
    throw error;
  }
};

/**
 * Get stock levels
 */
export const getStockLevels = async (filters: StockFilters = {}): Promise<StockLevel[]> => {
  const response = await apiClient.get<StockLevel[] | { data: StockLevel[] }>('/inventory/stock', {
    params: filters
  });
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: StockLevel[] }).data;
  }
  return [];
};

/**
 * Get stock movements
 */
export const getStockMovements = async (filters: {
  materialId?: string;
  orderId?: string;
  fromDate?: string;
  toDate?: string;
} = {}): Promise<StockMovement[]> => {
  const response = await apiClient.get<StockMovement[] | { data: StockMovement[] }>('/inventory/stock/movements', {
    params: filters
  });
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: StockMovement[] }).data;
  }
  return [];
};

/**
 * Get scan history
 */
export const getScanHistory = async (filters: {
  orderId?: string;
  materialId?: string;
  fromDate?: string;
  toDate?: string;
} = {}): Promise<ScanRecord[]> => {
  // This endpoint may need to be created on the backend
  // For now, we'll use a placeholder endpoint
  const response = await apiClient.get<ScanRecord[] | { data: ScanRecord[] }>('/inventory/scans', {
    params: filters
  });
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: ScanRecord[] }).data;
  }
  return [];
};

/**
 * Lookup material by serial number
 * Tries multiple endpoints to find material information
 */
export const lookupMaterialBySerial = async (serialNumber: string): Promise<Material | null> => {
  try {
    // Try endpoint: /inventory/serialised-items/{serialNumber}
    try {
      const response = await apiClient.get<any>(`/inventory/serialised-items/${encodeURIComponent(serialNumber)}`);
      
      if (response && typeof response === 'object') {
        // Handle response envelope
        const serialisedItem = (response as any).data || response;
        
        // If response has material info, return it
        if (serialisedItem.materialId || serialisedItem.material) {
          const materialId = serialisedItem.materialId || serialisedItem.material?.id || serialisedItem.material?.Id;
          if (materialId) {
            return getMaterial(materialId);
          }
          // If material info is already in response
          if (serialisedItem.material) {
            const mat = serialisedItem.material;
            return {
              id: mat.id || mat.Id,
              name: mat.name || mat.Name || mat.description || mat.Description,
              code: mat.code || mat.Code || mat.itemCode || mat.ItemCode,
              description: mat.description || mat.Description,
              category: mat.category || mat.Category,
              categoryName: mat.categoryName || mat.CategoryName,
              unit: mat.unit || mat.Unit || mat.unitOfMeasure || mat.UnitOfMeasure,
              isActive: mat.isActive ?? mat.IsActive ?? true,
              isSerialised: mat.isSerialised ?? mat.IsSerialised ?? true, // SerialisedItem implies serialised
            };
          }
        }
      }
    } catch (e) {
      // Endpoint doesn't exist or serial not found, try alternative
    }

    // Try endpoint: /inventory/serial/{serialNumber} (alternative endpoint)
    try {
      const response = await apiClient.get<any>(`/inventory/serial/${encodeURIComponent(serialNumber)}`);
      
      if (response && typeof response === 'object') {
        const data = (response as any).data || response;
        if (data.materialId || data.material) {
          const materialId = data.materialId || data.material?.id || data.material?.Id;
          if (materialId) {
            return getMaterial(materialId);
          }
        }
      }
    } catch (e) {
      // Endpoint doesn't exist
    }

    // Try searching materials by serial number in description or code
    try {
      const materials = await getAllMaterials({ search: serialNumber });
      // Find exact match in code or description
      const match = materials.find(m => 
        m.code?.toLowerCase() === serialNumber.toLowerCase() ||
        m.description?.toLowerCase().includes(serialNumber.toLowerCase())
      );
      if (match) {
        return match;
      }
    } catch (e) {
      // Search failed
    }

    return null;
  } catch (error) {
    // Serial number not found
    return null;
  }
};

/**
 * Record a material scan (standalone, not tied to an order)
 */
export const recordMaterialScan = async (scanData: {
  serialNumber: string;
  materialId?: string;
  deviceType?: string;
  location?: {
    latitude: number;
    longitude: number;
  };
}): Promise<ScanRecord> => {
  const response = await apiClient.post<ScanRecord | { data: ScanRecord }>('/inventory/scans', scanData);
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: ScanRecord }).data;
  }
  return response as ScanRecord;
};

