import apiClient from './client';
import type {
  PnlType,
  CreatePnlTypeRequest,
  UpdatePnlTypeRequest,
  PnlTypeFilters,
  PnlTypeCategory
} from '../types/pnlTypes';

/**
 * P&L Types API
 */

// Export enum (re-export from types)
export { PnlTypeCategory } from '../types/pnlTypes';

/**
 * Get all P&L types (flat list)
 * @param params - Optional filters
 * @returns Array of P&L types
 */
export const getPnlTypes = async (params: PnlTypeFilters = {}): Promise<PnlType[]> => {
  const response = await apiClient.get<PnlType[]>('/pnl-types', { params });
  return response;
};

/**
 * Get P&L types as hierarchical tree
 * @param params - Optional filters
 * @returns Array of P&L types in tree structure
 */
export const getPnlTypeTree = async (params: PnlTypeFilters = {}): Promise<PnlType[]> => {
  const response = await apiClient.get<PnlType[]>('/pnl-types/tree', { params });
  return response;
};

/**
 * Get transactional P&L types (for dropdowns)
 * @param category - Optional category filter
 * @returns Array of transactional P&L types
 */
export const getTransactionalPnlTypes = async (category: PnlTypeCategory | null = null): Promise<PnlType[]> => {
  const params = category ? { category } : {};
  const response = await apiClient.get<PnlType[]>('/pnl-types/transactional', { params });
  return response;
};

/**
 * Get single P&L type by ID
 * @param id - P&L type ID
 * @returns P&L type details
 */
export const getPnlType = async (id: string): Promise<PnlType> => {
  const response = await apiClient.get<PnlType>(`/pnl-types/${id}`);
  return response;
};

/**
 * Create new P&L type
 * @param data - P&L type creation data
 * @returns Created P&L type
 */
export const createPnlType = async (data: CreatePnlTypeRequest): Promise<PnlType> => {
  const response = await apiClient.post<PnlType>('/pnl-types', data);
  return response;
};

/**
 * Update P&L type
 * @param id - P&L type ID
 * @param data - P&L type update data
 * @returns Updated P&L type
 */
export const updatePnlType = async (id: string, data: UpdatePnlTypeRequest): Promise<PnlType> => {
  const response = await apiClient.put<PnlType>(`/pnl-types/${id}`, data);
  return response;
};

/**
 * Delete P&L type
 * @param id - P&L type ID
 * @returns Promise that resolves when P&L type is deleted
 */
export const deletePnlType = async (id: string): Promise<void> => {
  await apiClient.delete(`/pnl-types/${id}`);
};

