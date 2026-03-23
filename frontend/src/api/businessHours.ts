import apiClient from './client';

export interface BusinessHours {
  id: string;
  companyId?: string;
  name: string;
  description?: string;
  departmentId?: string;
  timezone: string;
  mondayStart?: string;
  mondayEnd?: string;
  tuesdayStart?: string;
  tuesdayEnd?: string;
  wednesdayStart?: string;
  wednesdayEnd?: string;
  thursdayStart?: string;
  thursdayEnd?: string;
  fridayStart?: string;
  fridayEnd?: string;
  saturdayStart?: string;
  saturdayEnd?: string;
  sundayStart?: string;
  sundayEnd?: string;
  isDefault: boolean;
  isActive: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  createdAt: string;
  updatedAt: string;
}

export interface PublicHoliday {
  id: string;
  companyId?: string;
  name: string;
  holidayDate: string;
  holidayType: string; // National, State, Regional, Custom
  state?: string;
  isRecurring: boolean;
  isActive: boolean;
  description?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateBusinessHoursDto {
  name: string;
  description?: string;
  departmentId?: string;
  timezone: string;
  mondayStart?: string;
  mondayEnd?: string;
  tuesdayStart?: string;
  tuesdayEnd?: string;
  wednesdayStart?: string;
  wednesdayEnd?: string;
  thursdayStart?: string;
  thursdayEnd?: string;
  fridayStart?: string;
  fridayEnd?: string;
  saturdayStart?: string;
  saturdayEnd?: string;
  sundayStart?: string;
  sundayEnd?: string;
  isDefault: boolean;
  isActive: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export interface UpdateBusinessHoursDto {
  name?: string;
  description?: string;
  timezone?: string;
  mondayStart?: string;
  mondayEnd?: string;
  tuesdayStart?: string;
  tuesdayEnd?: string;
  wednesdayStart?: string;
  wednesdayEnd?: string;
  thursdayStart?: string;
  thursdayEnd?: string;
  fridayStart?: string;
  fridayEnd?: string;
  saturdayStart?: string;
  saturdayEnd?: string;
  sundayStart?: string;
  sundayEnd?: string;
  isDefault?: boolean;
  isActive?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export interface CreatePublicHolidayDto {
  name: string;
  holidayDate: string;
  holidayType: string;
  state?: string;
  isRecurring: boolean;
  isActive: boolean;
  description?: string;
}

export interface UpdatePublicHolidayDto {
  name?: string;
  holidayDate?: string;
  holidayType?: string;
  state?: string;
  isRecurring?: boolean;
  isActive?: boolean;
  description?: string;
}

export async function getBusinessHours(params?: {
  departmentId?: string;
  isActive?: boolean;
}): Promise<BusinessHours[]> {
  const response = await apiClient.get<BusinessHours[]>('/business-hours', { params });
  return response.data;
}

export async function getBusinessHoursById(id: string): Promise<BusinessHours> {
  const response = await apiClient.get<BusinessHours>(`/business-hours/${id}`);
  return response.data;
}

export async function isBusinessHours(dateTime: string, departmentId?: string): Promise<boolean> {
  const response = await apiClient.get<boolean>('/business-hours/check', {
    params: { dateTime, departmentId }
  });
  return response.data;
}

export async function createBusinessHours(data: CreateBusinessHoursDto): Promise<BusinessHours> {
  const response = await apiClient.post<BusinessHours>('/business-hours', data);
  return response.data;
}

export async function updateBusinessHours(id: string, data: UpdateBusinessHoursDto): Promise<BusinessHours> {
  const response = await apiClient.put<BusinessHours>(`/business-hours/${id}`, data);
  return response.data;
}

export async function deleteBusinessHours(id: string): Promise<void> {
  await apiClient.delete(`/business-hours/${id}`);
}

export async function getPublicHolidays(params?: {
  year?: number;
  isActive?: boolean;
}): Promise<PublicHoliday[]> {
  const response = await apiClient.get<PublicHoliday[]>('/business-hours/holidays', { params });
  return response.data;
}

export async function isPublicHoliday(date: string): Promise<boolean> {
  const response = await apiClient.get<boolean>('/business-hours/holidays/check', {
    params: { date }
  });
  return response.data;
}

export async function createPublicHoliday(data: CreatePublicHolidayDto): Promise<PublicHoliday> {
  const response = await apiClient.post<PublicHoliday>('/business-hours/holidays', data);
  return response.data;
}

export async function updatePublicHoliday(id: string, data: UpdatePublicHolidayDto): Promise<PublicHoliday> {
  const response = await apiClient.put<PublicHoliday>(`/business-hours/holidays/${id}`, data);
  return response.data;
}

export async function deletePublicHoliday(id: string): Promise<void> {
  await apiClient.delete(`/business-hours/holidays/${id}`);
}

export async function createTemplateBusinessHours(name?: string, departmentId?: string): Promise<BusinessHours> {
  const params = new URLSearchParams();
  if (name) params.append('name', name);
  if (departmentId) params.append('departmentId', departmentId);
  
  const response = await apiClient.post<BusinessHours>(`/business-hours/template?${params.toString()}`);
  return response.data;
}

