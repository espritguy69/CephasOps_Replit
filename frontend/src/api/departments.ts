import apiClient from './client';
import type {
  Department,
  MaterialAllocation,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
  CreateMaterialAllocationRequest,
  DepartmentFilters
} from '../types/departments';

/**
 * Departments API
 * Handles department management and material allocations
 */

/**
 * Get departments list
 * @param filters - Optional filters (isActive)
 * @returns Array of departments
 */
export const getDepartments = async (filters: DepartmentFilters = {}): Promise<Department[]> => {
  const response = await apiClient.get<Department[]>('/departments', { params: filters });
  return response;
};

/**
 * Get department by ID
 * @param departmentId - Department ID
 * @returns Department details
 */
export const getDepartment = async (departmentId: string): Promise<Department> => {
  const response = await apiClient.get<Department>(`/departments/${departmentId}`);
  return response;
};

/**
 * Create department
 * @param departmentData - Department creation data
 * @returns Created department
 */
export const createDepartment = async (departmentData: CreateDepartmentRequest): Promise<Department> => {
  const response = await apiClient.post<Department>('/departments', departmentData);
  return response;
};

/**
 * Update department
 * @param departmentId - Department ID
 * @param departmentData - Department update data
 * @returns Updated department
 */
export const updateDepartment = async (
  departmentId: string,
  departmentData: UpdateDepartmentRequest
): Promise<Department> => {
  const response = await apiClient.put<Department>(`/departments/${departmentId}`, departmentData);
  return response;
};

/**
 * Delete department
 * @param departmentId - Department ID
 * @returns Promise that resolves when department is deleted
 */
export const deleteDepartment = async (departmentId: string): Promise<void> => {
  await apiClient.delete(`/departments/${departmentId}`);
};

/**
 * Get department material allocations
 * @param departmentId - Department ID
 * @returns Array of material allocations
 */
export const getDepartmentMaterialAllocations = async (departmentId: string): Promise<MaterialAllocation[]> => {
  const response = await apiClient.get<MaterialAllocation[]>(`/departments/${departmentId}/material-allocations`);
  return response;
};

/**
 * Create department material allocation
 * @param departmentId - Department ID
 * @param allocationData - Material allocation data
 * @returns Created allocation
 */
export const createDepartmentMaterialAllocation = async (
  departmentId: string,
  allocationData: CreateMaterialAllocationRequest
): Promise<MaterialAllocation> => {
  const response = await apiClient.post<MaterialAllocation>(
    `/departments/${departmentId}/material-allocations`,
    allocationData
  );
  return response;
};

/**
 * Delete department material allocation
 * @param allocationId - Allocation ID
 * @returns Promise that resolves when allocation is deleted
 */
export const deleteDepartmentMaterialAllocation = async (allocationId: string): Promise<void> => {
  await apiClient.delete(`/departments/material-allocations/${allocationId}`);
};

