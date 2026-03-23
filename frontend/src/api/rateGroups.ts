import apiClient from './client';
import type {
  RateGroupDto,
  CreateRateGroupRequest,
  UpdateRateGroupRequest,
  OrderTypeSubtypeRateGroupMappingDto,
  AssignRateGroupToOrderTypeSubtypeRequest,
  BaseWorkRateDto,
  CreateBaseWorkRateRequest,
  UpdateBaseWorkRateRequest,
  BaseWorkRateListFilter,
} from '../types/rateGroups';

const BASE = '/settings/gpon/rate-groups';
const MAPPINGS_BASE = '/settings/gpon/rate-group-mappings';
const BASE_WORK_RATES_BASE = '/settings/gpon/base-work-rates';

export const getRateGroups = async (params?: { isActive?: boolean }): Promise<RateGroupDto[]> => {
  const response = await apiClient.get<RateGroupDto[]>(BASE, { params });
  return response;
};

export const getRateGroupById = async (id: string): Promise<RateGroupDto> => {
  const response = await apiClient.get<RateGroupDto>(`${BASE}/${id}`);
  return response;
};

export const createRateGroup = async (data: CreateRateGroupRequest): Promise<RateGroupDto> => {
  const response = await apiClient.post<RateGroupDto>(BASE, data);
  return response;
};

export const updateRateGroup = async (id: string, data: UpdateRateGroupRequest): Promise<RateGroupDto> => {
  const response = await apiClient.put<RateGroupDto>(`${BASE}/${id}`, data);
  return response;
};

export const deleteRateGroup = async (id: string): Promise<void> => {
  await apiClient.delete(`${BASE}/${id}`);
};

export const getRateGroupMappings = async (params?: {
  rateGroupId?: string;
  orderTypeId?: string;
}): Promise<OrderTypeSubtypeRateGroupMappingDto[]> => {
  const response = await apiClient.get<OrderTypeSubtypeRateGroupMappingDto[]>(MAPPINGS_BASE, { params });
  return response;
};

export const assignRateGroupToOrderTypeSubtype = async (
  data: AssignRateGroupToOrderTypeSubtypeRequest
): Promise<OrderTypeSubtypeRateGroupMappingDto> => {
  const response = await apiClient.post<OrderTypeSubtypeRateGroupMappingDto>(MAPPINGS_BASE, data);
  return response;
};

export const unassignRateGroupMapping = async (mappingId: string): Promise<void> => {
  await apiClient.delete(`${MAPPINGS_BASE}/${mappingId}`);
};

// Base Work Rates (Phase 2)
export const getBaseWorkRates = async (params?: BaseWorkRateListFilter): Promise<BaseWorkRateDto[]> => {
  const response = await apiClient.get<BaseWorkRateDto[]>(BASE_WORK_RATES_BASE, { params });
  return response;
};

export const getBaseWorkRateById = async (id: string): Promise<BaseWorkRateDto> => {
  const response = await apiClient.get<BaseWorkRateDto>(`${BASE_WORK_RATES_BASE}/${id}`);
  return response;
};

export const createBaseWorkRate = async (data: CreateBaseWorkRateRequest): Promise<BaseWorkRateDto> => {
  const response = await apiClient.post<BaseWorkRateDto>(BASE_WORK_RATES_BASE, data);
  return response;
};

export const updateBaseWorkRate = async (id: string, data: UpdateBaseWorkRateRequest): Promise<BaseWorkRateDto> => {
  const response = await apiClient.put<BaseWorkRateDto>(`${BASE_WORK_RATES_BASE}/${id}`, data);
  return response;
};

export const deleteBaseWorkRate = async (id: string): Promise<void> => {
  await apiClient.delete(`${BASE_WORK_RATES_BASE}/${id}`);
};
