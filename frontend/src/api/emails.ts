import apiClient from './client';
import { getApiBaseUrl } from './config';

export interface EmailMessage {
  id: string;
  emailAccountId: string;
  messageId: string;
  fromAddress: string;
  toAddresses: string;
  ccAddresses?: string;
  subject: string;
  bodyPreview?: string;
  bodyText?: string;
  bodyHtml?: string;
  receivedAt: string;
  sentAt?: string;
  direction: string;
  hasAttachments: boolean;
  parserStatus: string;
  parserError?: string;
  isVip: boolean;
  departmentId?: string;
  companyId?: string;
  createdAt: string;
  updatedAt: string;
  expiresAt: string;
  isExpired: boolean;
  attachments?: EmailAttachment[];
}

export interface EmailAttachment {
  id: string;
  emailMessageId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  isInline: boolean;
  contentId?: string;
  expiresAt: string;
  isExpired: boolean;
}

/**
 * Get email messages (list view - metadata only)
 */
export const getEmails = async (params?: {
  direction?: 'Inbound' | 'Outbound';
  status?: string;
  emailAccountId?: string;
  limit?: number;
}): Promise<EmailMessage[]> => {
  const response = await apiClient.get('/emails', { params });
  return Array.isArray(response) ? response : (response?.data || []);
};

/**
 * Get email message by ID (detail view - full body + attachments)
 */
export const getEmail = async (id: string): Promise<EmailMessage> => {
  const response = await apiClient.get(`/emails/${id}`);
  return response as EmailMessage;
};

/**
 * Download email attachment
 */
export const downloadAttachment = async (
  emailId: string,
  attachmentId: string,
  fileName: string
): Promise<void> => {
  const token = localStorage.getItem('authToken');
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/emails/${emailId}/attachments/${attachmentId}/download`;

  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  if (!response.ok) {
    if (response.status === 410) {
      throw new Error('Attachment has expired and is no longer available');
    }
    if (response.status === 404) {
      throw new Error('Attachment not found');
    }
    throw new Error(`Download failed: ${response.statusText}`);
  }

  const blob = await response.blob();
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(downloadUrl);
  a.remove();
};

/**
 * Format file size for display
 */
export const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
};

