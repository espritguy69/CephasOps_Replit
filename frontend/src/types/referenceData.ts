/**
 * Reference Data Types - Shared type definitions for reference data entities
 */

export interface ReferenceDataItem {
  id: string;
  name: string;
  code?: string;
  description?: string;
  isActive: boolean;
  departmentId?: string;
  departmentName?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateReferenceDataRequest {
  name: string;
  code?: string;
  description?: string;
  isActive?: boolean;
  departmentId?: string;
}

export interface UpdateReferenceDataRequest {
  name?: string;
  code?: string;
  description?: string;
  isActive?: boolean;
  departmentId?: string;
}

export interface ReferenceDataFilters {
  departmentId?: string;
  isActive?: boolean;
}

