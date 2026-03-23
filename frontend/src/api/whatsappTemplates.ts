import apiClient from './client';

/**
 * WhatsApp Templates API
 * Handles WhatsApp template management
 */

export interface WhatsAppTemplate {
  id: string;
  companyId: string;
  code: string;
  name: string;
  description?: string;
  category: string;
  templateId?: string;
  approvalStatus: 'Approved' | 'Pending' | 'Rejected';
  messageBody?: string;
  language?: string;
  isActive: boolean;
  notes?: string;
  submittedAt?: string;
  approvedAt?: string;
  createdByUserId?: string;
  updatedByUserId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWhatsAppTemplateDto {
  code: string;
  name: string;
  description?: string;
  category: string;
  templateId?: string;
  approvalStatus?: 'Approved' | 'Pending' | 'Rejected';
  messageBody?: string;
  language?: string;
  isActive?: boolean;
  notes?: string;
}

export interface UpdateWhatsAppTemplateDto {
  name?: string;
  description?: string;
  category?: string;
  templateId?: string;
  approvalStatus?: 'Approved' | 'Pending' | 'Rejected';
  messageBody?: string;
  language?: string;
  isActive?: boolean;
  notes?: string;
}

export interface WhatsAppTemplateFilters {
  companyId: string;
  category?: string;
  approvalStatus?: string;
  isActive?: boolean;
}

/**
 * Get all WhatsApp templates
 */
export const getWhatsAppTemplates = async (filters: WhatsAppTemplateFilters): Promise<WhatsAppTemplate[]> => {
  const response = await apiClient.get('/whatsapp-templates', { params: filters });
  return Array.isArray(response) ? response : (response?.data || []);
};

/**
 * Get WhatsApp template by ID
 */
export const getWhatsAppTemplate = async (id: string): Promise<WhatsAppTemplate> => {
  const response = await apiClient.get(`/whatsapp-templates/${id}`);
  return response as WhatsAppTemplate;
};

/**
 * Get WhatsApp template by code
 */
export const getWhatsAppTemplateByCode = async (companyId: string, code: string): Promise<WhatsAppTemplate> => {
  const response = await apiClient.get(`/whatsapp-templates/by-code/${code}`, { 
    params: { companyId } 
  });
  return response as WhatsAppTemplate;
};

/**
 * Create new WhatsApp template
 */
export const createWhatsAppTemplate = async (
  companyId: string, 
  data: CreateWhatsAppTemplateDto
): Promise<WhatsAppTemplate> => {
  const response = await apiClient.post('/whatsapp-templates', data, { 
    params: { companyId } 
  });
  return response as WhatsAppTemplate;
};

/**
 * Update WhatsApp template
 */
export const updateWhatsAppTemplate = async (
  id: string, 
  data: UpdateWhatsAppTemplateDto
): Promise<WhatsAppTemplate> => {
  const response = await apiClient.put(`/whatsapp-templates/${id}`, data);
  return response as WhatsAppTemplate;
};

/**
 * Update WhatsApp template approval status
 */
export const updateWhatsAppTemplateApprovalStatus = async (
  id: string, 
  approvalStatus: string
): Promise<WhatsAppTemplate> => {
  const response = await apiClient.patch(`/whatsapp-templates/${id}/approval-status`, { 
    approvalStatus 
  });
  return response as WhatsAppTemplate;
};

/**
 * Delete WhatsApp template
 */
export const deleteWhatsAppTemplate = async (id: string): Promise<void> => {
  await apiClient.delete(`/whatsapp-templates/${id}`);
};

