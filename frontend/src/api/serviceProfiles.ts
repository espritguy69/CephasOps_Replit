import apiClient from './client';

export interface ServiceProfileDto {
  id: string;
  companyId?: string;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateServiceProfileRequest {
  code: string;
  name: string;
  description?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface UpdateServiceProfileRequest {
  code?: string;
  name?: string;
  description?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface OrderCategoryServiceProfileDto {
  id: string;
  companyId?: string;
  orderCategoryId: string;
  orderCategoryName?: string;
  orderCategoryCode?: string;
  serviceProfileId: string;
  serviceProfileName?: string;
  serviceProfileCode?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateOrderCategoryServiceProfileRequest {
  orderCategoryId: string;
  serviceProfileId: string;
}

const PROFILES_BASE = '/settings/gpon/service-profiles';
const MAPPINGS_BASE = '/settings/gpon/service-profile-mappings';

export const getServiceProfiles = async (params?: {
  isActive?: boolean;
  search?: string;
}): Promise<ServiceProfileDto[]> => {
  return apiClient.get<ServiceProfileDto[]>(PROFILES_BASE, { params });
};

export const getServiceProfileById = async (id: string): Promise<ServiceProfileDto> => {
  return apiClient.get<ServiceProfileDto>(`${PROFILES_BASE}/${id}`);
};

export const createServiceProfile = async (
  data: CreateServiceProfileRequest
): Promise<ServiceProfileDto> => {
  return apiClient.post<ServiceProfileDto>(PROFILES_BASE, data);
};

export const updateServiceProfile = async (
  id: string,
  data: UpdateServiceProfileRequest
): Promise<ServiceProfileDto> => {
  return apiClient.put<ServiceProfileDto>(`${PROFILES_BASE}/${id}`, data);
};

export const deleteServiceProfile = async (id: string): Promise<void> => {
  await apiClient.delete(`${PROFILES_BASE}/${id}`);
};

export const getServiceProfileMappings = async (params?: {
  serviceProfileId?: string;
  orderCategoryId?: string;
}): Promise<OrderCategoryServiceProfileDto[]> => {
  return apiClient.get<OrderCategoryServiceProfileDto[]>(MAPPINGS_BASE, { params });
};

export const getServiceProfileMappingById = async (
  id: string
): Promise<OrderCategoryServiceProfileDto> => {
  return apiClient.get<OrderCategoryServiceProfileDto>(`${MAPPINGS_BASE}/${id}`);
};

export const createServiceProfileMapping = async (
  data: CreateOrderCategoryServiceProfileRequest
): Promise<OrderCategoryServiceProfileDto> => {
  return apiClient.post<OrderCategoryServiceProfileDto>(MAPPINGS_BASE, data);
};

export const deleteServiceProfileMapping = async (id: string): Promise<void> => {
  await apiClient.delete(`${MAPPINGS_BASE}/${id}`);
};
