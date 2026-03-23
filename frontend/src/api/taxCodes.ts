import apiClient from './client';

export interface TaxCode {
  id: string;
  companyId: string;
  code: string;
  name: string;
  description?: string;
  taxRate: number;
  isDefault: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateTaxCodeDto {
  code: string;
  name: string;
  description?: string;
  taxRate: number;
  isDefault?: boolean;
  isActive?: boolean;
}

export interface UpdateTaxCodeDto {
  name?: string;
  description?: string;
  taxRate?: number;
  isDefault?: boolean;
  isActive?: boolean;
}

export const getTaxCodes = async (companyId: string, isActive?: boolean): Promise<TaxCode[]> => {
  const response = await apiClient.get('/tax-codes', { params: { companyId, isActive } });
  return Array.isArray(response) ? response : (response?.data || []);
};

export const getTaxCode = async (id: string): Promise<TaxCode> => {
  const response = await apiClient.get(`/tax-codes/${id}`);
  return response as TaxCode;
};

export const createTaxCode = async (companyId: string, data: CreateTaxCodeDto): Promise<TaxCode> => {
  const response = await apiClient.post('/tax-codes', data, { params: { companyId } });
  return response as TaxCode;
};

export const updateTaxCode = async (id: string, data: UpdateTaxCodeDto): Promise<TaxCode> => {
  const response = await apiClient.put(`/tax-codes/${id}`, data);
  return response as TaxCode;
};

export const deleteTaxCode = async (id: string): Promise<void> => {
  await apiClient.delete(`/tax-codes/${id}`);
};
