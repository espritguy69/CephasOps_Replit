import apiClient from './client';
import type { Company, CreateCompanyRequest, UpdateCompanyRequest } from '../types/companies';

/**
 * Companies API client
 * Handles creation and retrieval of company records.
 */

/**
 * Get all companies
 * @returns Array of companies
 */
export const getCompanies = async (): Promise<Company[]> => {
  const response = await apiClient.get<Company[] | { data: Company[] }>('/companies');
  return Array.isArray(response) ? response : (response as { data: Company[] }).data || [];
};

/**
 * Get company by ID
 * @param companyId - Company ID
 * @returns Company details
 */
export const getCompany = async (companyId: string): Promise<Company> => {
  const response = await apiClient.get<Company>(`/companies/${companyId}`);
  return response;
};

/**
 * Create company
 * @param companyData - Company creation data
 * @returns Created company
 */
export const createCompany = async (companyData: CreateCompanyRequest): Promise<Company> => {
  const response = await apiClient.post<Company>('/companies', companyData);
  return response;
};

/**
 * Update company
 * @param companyId - Company ID
 * @param companyData - Company update data
 * @returns Updated company
 */
export const updateCompany = async (companyId: string, companyData: UpdateCompanyRequest): Promise<Company> => {
  const response = await apiClient.put<Company>(`/companies/${companyId}`, companyData);
  return response;
};

