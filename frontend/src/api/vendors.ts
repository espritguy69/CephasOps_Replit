import apiClient from './client';

export interface Vendor {
  id: string;
  companyId: string;
  code: string;
  name: string;
  description?: string;
  contactPerson?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: string;
  city?: string;
  state?: string;
  postCode?: string;
  country?: string;
  paymentTerms?: string;
  paymentDueDays?: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateVendorDto {
  code: string;
  name: string;
  description?: string;
  contactPerson?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: string;
  city?: string;
  state?: string;
  postCode?: string;
  country?: string;
  paymentTerms?: string;
  paymentDueDays?: number;
  isActive?: boolean;
}

export interface UpdateVendorDto {
  name?: string;
  description?: string;
  contactPerson?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: string;
  city?: string;
  state?: string;
  postCode?: string;
  country?: string;
  paymentTerms?: string;
  paymentDueDays?: number;
  isActive?: boolean;
}

export const getVendors = async (companyId: string, isActive?: boolean): Promise<Vendor[]> => {
  const response = await apiClient.get('/vendors', { params: { companyId, isActive } });
  return Array.isArray(response) ? response : (response?.data || []);
};

export const getVendor = async (id: string): Promise<Vendor> => {
  const response = await apiClient.get(`/vendors/${id}`);
  return response as Vendor;
};

export const createVendor = async (companyId: string, data: CreateVendorDto): Promise<Vendor> => {
  const response = await apiClient.post('/vendors', data, { params: { companyId } });
  return response as Vendor;
};

export const updateVendor = async (id: string, data: UpdateVendorDto): Promise<Vendor> => {
  const response = await apiClient.put(`/vendors/${id}`, data);
  return response as Vendor;
};

export const deleteVendor = async (id: string): Promise<void> => {
  await apiClient.delete(`/vendors/${id}`);
};
