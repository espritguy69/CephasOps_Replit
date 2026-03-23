/**
 * Inventory Types - Shared type definitions for Inventory & RMA module
 */

export interface Material {
  id: string;
  name?: string; // Legacy field - use description instead
  code?: string; // Maps to ItemCode from backend
  itemCode?: string; // Backend field name
  description?: string;
  // Material Category (new FK-based system)
  materialCategoryId?: string; // FK to MaterialCategory entity
  materialCategoryName?: string; // Category name from MaterialCategory
  // Legacy category field (string value, kept for backward compatibility)
  categoryId?: string; // Maps to Category from backend (string value, not GUID)
  category?: string; // Backend field name (category name/value as string)
  categoryName?: string;
  // Material Verticals (many-to-many)
  materialVerticalIds?: string[];
  materialVerticalNames?: string[];
  // Material Tags (many-to-many)
  materialTagIds?: string[];
  materialTagNames?: string[];
  materialTagColors?: string[];
  // Material Attributes (key-value pairs)
  materialAttributes?: MaterialAttribute[];
  unit?: string; // Maps to UnitOfMeasure from backend
  unitOfMeasure?: string; // Backend field name
  unitPrice?: number; // Maps to DefaultCost from backend
  defaultCost?: number; // Backend field name
  isSerialised: boolean;
  isActive: boolean;
  minStockLevel?: number;
  maxStockLevel?: number;
  reorderPoint?: number;
  // Partner fields - legacy single partner (for backward compatibility)
  partnerId?: string;
  partnerName?: string;
  // Partner fields - multiple partners (source of truth)
  partnerIds?: string[];
  partnerNames?: string[];
  barcode?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface MaterialAttribute {
  id?: string;
  key: string;
  value: string;
  dataType?: string; // String, Number, Boolean, Date, etc.
  displayOrder?: number;
}

export interface MaterialVertical {
  id: string;
  code: string;
  name: string;
  description?: string;
  displayOrder?: number;
  isActive: boolean;
}

export interface MaterialTag {
  id: string;
  name: string;
  description?: string;
  color?: string; // Hex color code for UI display
  displayOrder?: number;
  isActive: boolean;
}

export interface MaterialCategory {
  id: string;
  name: string;
  code?: string;
  description?: string;
  parentCategoryId?: string;
  isActive: boolean;
}

export interface StockLocation {
  id: string;
  name: string;
  code?: string;
  address?: string;
  isActive: boolean;
}

export interface StockBalance {
  id: string;
  materialId: string;
  materialName?: string;
  locationId: string;
  locationName?: string;
  quantity: number;
  reservedQuantity?: number;
  availableQuantity: number;
  unit: string;
  lastUpdated?: string;
}

export interface StockMovement {
  id: string;
  materialId: string;
  materialName?: string;
  locationId: string;
  locationName?: string;
  movementType: 'In' | 'Out' | 'Transfer' | 'Adjustment' | 'Return';
  quantity: number;
  unit: string;
  reference?: string;
  referenceId?: string;
  notes?: string;
  createdBy?: string;
  createdAt: string;
}

export interface SerialisedItem {
  id: string;
  materialId: string;
  materialName?: string;
  serialNumber: string;
  status: 'Available' | 'Allocated' | 'Installed' | 'Returned' | 'Damaged' | 'Lost';
  locationId?: string;
  locationName?: string;
  orderId?: string;
  installedAt?: string;
  notes?: string;
  createdAt?: string;
}

export interface RmaRequest {
  id: string;
  rmaNumber: string;
  partnerId: string;
  partnerName?: string;
  orderId?: string;
  materialId?: string;
  materialName?: string;
  serialNumber?: string;
  quantity: number;
  reason: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Processing' | 'Completed' | 'Cancelled';
  requestedDate: string;
  approvedDate?: string;
  completedDate?: string;
  notes?: string;
  createdAt?: string;
}

export interface CreateMaterialRequest {
  name?: string; // Legacy - maps to Description
  code?: string; // Maps to ItemCode in backend
  itemCode?: string; // Backend field name (preferred)
  description?: string;
  // Material Category (new FK-based system)
  materialCategoryId?: string; // FK to MaterialCategory entity
  // Legacy category field (string value, kept for backward compatibility)
  categoryId?: string; // Maps to Category in backend (string value, not GUID)
  category?: string; // Backend field name (category name/value as string)
  // Material Verticals (many-to-many)
  materialVerticalIds?: string[];
  // Material Tags (many-to-many)
  materialTagIds?: string[];
  // Material Attributes (key-value pairs)
  materialAttributes?: MaterialAttribute[];
  unit?: string; // Maps to UnitOfMeasure in backend
  unitOfMeasure?: string; // Backend field name (preferred)
  unitPrice?: number; // Maps to DefaultCost in backend
  defaultCost?: number; // Backend field name (preferred)
  isSerialised: boolean;
  isActive?: boolean;
  minStockLevel?: number;
  maxStockLevel?: number;
  reorderPoint?: number;
  // Partner fields - legacy single partner (for backward compatibility)
  partnerId?: string;
  // Partner fields - multiple partners (source of truth, at least one required)
  partnerIds?: string[];
  departmentId?: string;
  barcode?: string;
}

export interface UpdateMaterialRequest {
  name?: string; // Legacy - maps to Description
  code?: string; // Maps to ItemCode in backend
  itemCode?: string; // Backend field name (preferred)
  description?: string;
  // Material Category (new FK-based system)
  materialCategoryId?: string; // FK to MaterialCategory entity
  // Legacy category field (string value, kept for backward compatibility)
  categoryId?: string; // Maps to Category in backend (string value, not GUID)
  category?: string; // Backend field name (category name/value as string)
  // Material Verticals (many-to-many) - null = no change, empty array = remove all
  materialVerticalIds?: string[] | null;
  // Material Tags (many-to-many) - null = no change, empty array = remove all
  materialTagIds?: string[] | null;
  // Material Attributes (key-value pairs) - null = no change, empty array = remove all
  materialAttributes?: MaterialAttribute[] | null;
  unit?: string; // Maps to UnitOfMeasure in backend
  unitOfMeasure?: string; // Backend field name (preferred)
  unitPrice?: number; // Maps to DefaultCost in backend
  defaultCost?: number; // Backend field name (preferred)
  isSerialised?: boolean;
  isActive?: boolean;
  minStockLevel?: number;
  maxStockLevel?: number;
  reorderPoint?: number;
  // Partner fields - legacy single partner (for backward compatibility)
  partnerId?: string;
  // Partner fields - multiple partners (source of truth, at least one required)
  partnerIds?: string[];
  departmentId?: string;
  barcode?: string;
}

export interface CreateMaterialCategoryRequest {
  name: string;
  code?: string;
  description?: string;
  parentCategoryId?: string;
  isActive?: boolean;
}

export interface UpdateMaterialCategoryRequest {
  name?: string;
  code?: string;
  description?: string;
  parentCategoryId?: string;
  isActive?: boolean;
}

export interface RecordStockMovementRequest {
  materialId: string;
  locationId: string;
  movementType: 'In' | 'Out' | 'Transfer' | 'Adjustment' | 'Return';
  quantity: number;
  reference?: string;
  referenceId?: string;
  notes?: string;
}

export interface RegisterSerialisedItemRequest {
  materialId: string;
  serialNumber: string;
  locationId?: string;
  notes?: string;
}

export interface CreateRmaRequestRequest {
  partnerId: string;
  orderId?: string;
  materialId?: string;
  serialNumber?: string;
  quantity: number;
  reason: string;
  notes?: string;
}

export interface UpdateRmaRequestRequest {
  status?: string;
  notes?: string;
}

export interface MaterialFilters {
  category?: string;
  search?: string;
  isActive?: boolean;
}

export interface StockBalanceFilters {
  locationId?: string;
  materialId?: string;
}

export interface StockMovementFilters {
  materialId?: string;
  locationId?: string;
  movementType?: string;
  dateRange?: string;
}

export interface SerialisedItemFilters {
  materialId?: string;
  serialNumber?: string;
  status?: string;
}

export interface RmaRequestFilters {
  partnerId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

export interface ImportResult {
  success: boolean;
  imported: number;
  failed: number;
  errors?: string[];
}

