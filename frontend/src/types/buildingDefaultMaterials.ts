/**
 * Building Default Materials Types - Shared type definitions for Building Default Materials module
 */

export interface BuildingDefaultMaterial {
  id: string;
  buildingId: string;
  buildingName?: string;
  orderTypeId: string;
  orderTypeName?: string;
  orderTypeCode?: string;
  materialId: string;
  materialCode?: string;
  materialDescription?: string;
  materialUnitOfMeasure?: string;
  defaultQuantity: number;
  unit?: string;
  notes?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateBuildingDefaultMaterialRequest {
  orderTypeId: string;
  materialId: string;
  defaultQuantity: number;
  unit?: string;
  notes?: string;
  isActive?: boolean;
}

export interface UpdateBuildingDefaultMaterialRequest {
  orderTypeId?: string;
  materialId?: string;
  defaultQuantity?: number;
  unit?: string;
  notes?: string;
  isActive?: boolean;
}

export interface BuildingDefaultMaterialFilters {
  orderTypeId?: string;
  isActive?: boolean;
}

export interface DefaultMaterialsSummary {
  totalBuildings: number;
  totalMaterials: number;
  byOrderType: Record<string, number>;
  byBuilding: Record<string, number>;
}

