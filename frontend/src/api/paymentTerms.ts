import apiClient from './client';

export interface PaymentTerm {
  id: string;
  companyId: string;
  code: string;
  name: string;
  description?: string;
  dueDays: number;
  discountPercent: number;
  discountDays: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreatePaymentTermDto {
  code: string;
  name: string;
  description?: string;
  dueDays?: number;
  discountPercent?: number;
  discountDays?: number;
  isActive?: boolean;
}

export interface UpdatePaymentTermDto {
  name?: string;
  description?: string;
  dueDays?: number;
  discountPercent?: number;
  discountDays?: number;
  isActive?: boolean;
}

export const getPaymentTerms = async (companyId: string, isActive?: boolean): Promise<PaymentTerm[]> => {
  const response = await apiClient.get('/payment-terms', { params: { companyId, isActive } });
  return Array.isArray(response) ? response : (response?.data || []);
};

export const getPaymentTerm = async (id: string): Promise<PaymentTerm> => {
  const response = await apiClient.get(`/payment-terms/${id}`);
  return response as PaymentTerm;
};

export const createPaymentTerm = async (companyId: string, data: CreatePaymentTermDto): Promise<PaymentTerm> => {
  const response = await apiClient.post('/payment-terms', data, { params: { companyId } });
  return response as PaymentTerm;
};

export const updatePaymentTerm = async (id: string, data: UpdatePaymentTermDto): Promise<PaymentTerm> => {
  const response = await apiClient.put(`/payment-terms/${id}`, data);
  return response as PaymentTerm;
};

export const deletePaymentTerm = async (id: string): Promise<void> => {
  await apiClient.delete(`/payment-terms/${id}`);
};
