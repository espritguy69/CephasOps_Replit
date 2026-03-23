import apiClient from './client';

/**
 * Email Templates API
 * Handles email template management for automated email sending
 */

export interface EmailTemplate {
  id: string;
  companyId?: string;
  name: string;
  code: string;
  emailAccountId?: string;
  emailAccountName?: string;
  subjectTemplate: string;
  bodyTemplate: string;
  departmentId?: string;
  departmentName?: string;
  relatedEntityType?: string;
  priority: number;
  isActive: boolean;
  autoProcessReplies: boolean;
  replyPattern?: string;
  description?: string;
  direction: string;
  createdByUserId: string;
  updatedByUserId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEmailTemplateDto {
  name: string;
  code: string;
  emailAccountId?: string;
  subjectTemplate: string;
  bodyTemplate: string;
  departmentId?: string;
  relatedEntityType?: string;
  priority?: number;
  isActive?: boolean;
  autoProcessReplies?: boolean;
  replyPattern?: string;
  description?: string;
  direction?: string;
}

export interface UpdateEmailTemplateDto {
  name?: string;
  subjectTemplate?: string;
  bodyTemplate?: string;
  emailAccountId?: string;
  departmentId?: string;
  relatedEntityType?: string;
  priority?: number;
  isActive?: boolean;
  autoProcessReplies?: boolean;
  replyPattern?: string;
  description?: string;
  direction?: string;
}

/**
 * Get all email templates, optionally filtered by direction (Incoming/Outgoing)
 */
export const getEmailTemplates = async (direction?: string): Promise<EmailTemplate[]> => {
  const params = direction ? { direction } : {};
  const response = await apiClient.get('/email-templates', { params });
  return Array.isArray(response) ? response : (response?.data || []);
};

/**
 * Get email template by ID
 */
export const getEmailTemplate = async (id: string): Promise<EmailTemplate> => {
  const response = await apiClient.get(`/email-templates/${id}`);
  return response as EmailTemplate;
};

/**
 * Get email template by code
 */
export const getEmailTemplateByCode = async (code: string): Promise<EmailTemplate> => {
  const response = await apiClient.get(`/email-templates/by-code/${code}`);
  return response as EmailTemplate;
};

/**
 * Get active templates for a specific entity type
 */
export const getActiveEmailTemplatesByEntityType = async (entityType: string): Promise<EmailTemplate[]> => {
  const response = await apiClient.get(`/email-templates/active/${entityType}`);
  return Array.isArray(response) ? response : (response?.data || []);
};

/**
 * Create new email template
 */
export const createEmailTemplate = async (data: CreateEmailTemplateDto): Promise<EmailTemplate> => {
  const response = await apiClient.post('/email-templates', data);
  return response as EmailTemplate;
};

/**
 * Update email template
 */
export const updateEmailTemplate = async (
  id: string, 
  data: UpdateEmailTemplateDto
): Promise<EmailTemplate> => {
  const response = await apiClient.put(`/email-templates/${id}`, data);
  return response as EmailTemplate;
};

/**
 * Delete email template
 */
export const deleteEmailTemplate = async (id: string): Promise<void> => {
  await apiClient.delete(`/email-templates/${id}`);
};

/**
 * Render email template with placeholders
 */
export const renderEmailTemplate = async (
  id: string, 
  placeholders: Record<string, string>
): Promise<{ subject: string; body: string }> => {
  const response = await apiClient.post(`/email-templates/${id}/render`, placeholders);
  return response as { subject: string; body: string };
};

