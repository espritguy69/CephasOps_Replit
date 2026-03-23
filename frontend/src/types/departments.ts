/**
 * Department Types - Shared type definitions for Departments module
 */

export interface Department {
  id: string;
  companyId?: string;
  name: string;
  code?: string;
  description?: string;
  costCentreId?: string;
  costCentreName?: string;
  isActive: boolean;
  materialAllocations?: MaterialAllocation[];
  createdAt?: string;
  updatedAt?: string;
}

export interface MaterialAllocation {
  id: string;
  departmentId: string;
  materialId: string;
  materialName?: string;
  quantity?: number;
  unit?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateDepartmentRequest {
  name: string;
  code?: string;
  description?: string;
  costCentreId?: string;
  isActive?: boolean;
}

export interface UpdateDepartmentRequest {
  name?: string;
  code?: string;
  description?: string;
  costCentreId?: string;
  costCentreName?: string;
  isActive?: boolean;
}

export interface CreateMaterialAllocationRequest {
  materialId: string;
  quantity?: number;
  unit?: string;
}

export interface DepartmentFilters {
  isActive?: boolean;
}

