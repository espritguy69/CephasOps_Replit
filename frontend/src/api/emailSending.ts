import apiClient from './client';

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
  createdAt: string;
  updatedAt: string;
}

export interface SendEmailDto {
  emailAccountId: string;
  to: string;
  subject: string;
  body: string;
  cc?: string;
  bcc?: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
}

export interface SendEmailWithTemplateDto {
  templateId: string;
  to: string;
  cc?: string;
  bcc?: string;
  placeholders?: Record<string, string>;
  relatedEntityId?: string;
  relatedEntityType?: string;
  emailAccountId?: string;
}

export interface SendRescheduleRequestDto {
  orderId: string;
  newDate: string;
  newWindowFrom: string;
  newWindowTo: string;
  reason: string;
  rescheduleType?: string;
  emailAccountId?: string;
  emailTemplateId?: string;
}

export interface EmailSendingResult {
  success: boolean;
  errorMessage?: string;
  emailAccountId: string;
  emailMessageId?: string;
  messageId?: string;
}

/**
 * Get all email templates
 */
export const getEmailTemplates = async (): Promise<EmailTemplate[]> => {
  const response = await apiClient.get('/email-templates');
  return Array.isArray(response) ? (response as EmailTemplate[]) : [];
};

/**
 * Get email template by ID
 */
export const getEmailTemplate = async (templateId: string): Promise<EmailTemplate> => {
  const response = await apiClient.get(`/email-templates/${templateId}`);
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
 * Get active templates for entity type
 */
export const getEmailTemplatesByEntityType = async (entityType: string): Promise<EmailTemplate[]> => {
  const response = await apiClient.get(`/email-templates/by-entity-type/${entityType}`);
  return Array.isArray(response) ? (response as EmailTemplate[]) : [];
};

/**
 * Create email template
 */
export const createEmailTemplate = async (templateData: Partial<EmailTemplate>): Promise<EmailTemplate> => {
  const response = await apiClient.post('/email-templates', templateData);
  return response as EmailTemplate;
};

/**
 * Update email template
 */
export const updateEmailTemplate = async (templateId: string, templateData: Partial<EmailTemplate>): Promise<EmailTemplate> => {
  const response = await apiClient.put(`/email-templates/${templateId}`, templateData);
  return response as EmailTemplate;
};

/**
 * Delete email template
 */
export const deleteEmailTemplate = async (templateId: string): Promise<void> => {
  await apiClient.delete(`/email-templates/${templateId}`);
};

/**
 * Send email directly
 */
export const sendEmail = async (dto: SendEmailDto, files?: File[]): Promise<EmailSendingResult> => {
  const formData = new FormData();
  formData.append('emailAccountId', dto.emailAccountId);
  formData.append('to', dto.to);
  formData.append('subject', dto.subject);
  formData.append('body', dto.body);
  if (dto.cc) formData.append('cc', dto.cc);
  if (dto.bcc) formData.append('bcc', dto.bcc);
  if (dto.relatedEntityId) formData.append('relatedEntityId', dto.relatedEntityId);
  if (dto.relatedEntityType) formData.append('relatedEntityType', dto.relatedEntityType);
  
  if (files) {
    files.forEach(file => formData.append('files', file));
  }

  const response = await apiClient.post('/email-sending/send', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });

  return response as EmailSendingResult;
};

/**
 * Send email using template
 */
export const sendEmailWithTemplate = async (dto: SendEmailWithTemplateDto, files?: File[]): Promise<EmailSendingResult> => {
  const formData = new FormData();
  formData.append('templateId', dto.templateId);
  formData.append('to', dto.to);
  if (dto.cc) formData.append('cc', dto.cc);
  if (dto.bcc) formData.append('bcc', dto.bcc);
  if (dto.placeholders) {
    formData.append('placeholdersJson', JSON.stringify(dto.placeholders));
  }
  if (dto.relatedEntityId) formData.append('relatedEntityId', dto.relatedEntityId);
  if (dto.relatedEntityType) formData.append('relatedEntityType', dto.relatedEntityType);
  if (dto.emailAccountId) formData.append('emailAccountId', dto.emailAccountId);
  
  if (files) {
    files.forEach(file => formData.append('files', file));
  }

  const response = await apiClient.post('/email-sending/send-with-template', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });

  return response as EmailSendingResult;
};

/**
 * Send reschedule request
 */
export const sendRescheduleRequest = async (dto: SendRescheduleRequestDto): Promise<EmailSendingResult> => {
  const response = await apiClient.post('/email-sending/reschedule-request', dto);
  return response as EmailSendingResult;
};

