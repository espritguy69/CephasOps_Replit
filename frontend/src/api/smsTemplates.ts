import apiClient from './client';

/**
 * SMS Templates API
 * Handles SMS template management
 */

export interface SmsTemplate {
  id: string;
  companyId: string;
  code: string;
  name: string;
  description?: string;
  category: string;
  messageText: string;
  charCount: number;
  isActive: boolean;
  notes?: string;
  createdByUserId?: string;
  updatedByUserId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSmsTemplateDto {
  code: string;
  name: string;
  description?: string;
  category: string;
  messageText: string;
  isActive?: boolean;
  notes?: string;
}

export interface UpdateSmsTemplateDto {
  name?: string;
  description?: string;
  category?: string;
  messageText?: string;
  isActive?: boolean;
  notes?: string;
}

export interface SmsTemplateFilters {
  companyId: string;
  category?: string;
  isActive?: boolean;
}

/**
 * Get all SMS templates
 */
export const getSmsTemplates = async (filters: SmsTemplateFilters): Promise<SmsTemplate[]> => {
  const response = await apiClient.get('/sms-templates', { params: filters });
  return Array.isArray(response) ? response : (response?.data || []);
};

/**
 * Get SMS template by ID
 */
export const getSmsTemplate = async (id: string): Promise<SmsTemplate> => {
  const response = await apiClient.get(`/sms-templates/${id}`);
  return response as SmsTemplate;
};

/**
 * Get SMS template by code
 */
export const getSmsTemplateByCode = async (companyId: string, code: string): Promise<SmsTemplate> => {
  const response = await apiClient.get(`/sms-templates/by-code/${code}`, { 
    params: { companyId } 
  });
  return response as SmsTemplate;
};

/**
 * Create new SMS template
 */
export const createSmsTemplate = async (
  companyId: string, 
  data: CreateSmsTemplateDto
): Promise<SmsTemplate> => {
  const response = await apiClient.post('/sms-templates', data, { 
    params: { companyId } 
  });
  return response as SmsTemplate;
};

/**
 * Update SMS template
 */
export const updateSmsTemplate = async (
  id: string, 
  data: UpdateSmsTemplateDto
): Promise<SmsTemplate> => {
  const response = await apiClient.put(`/sms-templates/${id}`, data);
  return response as SmsTemplate;
};

/**
 * Delete SMS template
 */
export const deleteSmsTemplate = async (id: string): Promise<void> => {
  await apiClient.delete(`/sms-templates/${id}`);
};

/**
 * Render SMS message with placeholders
 */
export const renderSmsMessage = async (
  id: string, 
  placeholders: Record<string, string>
): Promise<{ message: string; charCount: number }> => {
  const response = await apiClient.post(`/sms-templates/${id}/render`, placeholders);
  return response as { message: string; charCount: number };
};

