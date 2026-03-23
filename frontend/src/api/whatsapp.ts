/**
 * WhatsApp Messaging API
 */

import apiClient from './client';

export interface WhatsAppResult {
  success: boolean;
  messageId?: string;
  status: string;
  errorMessage?: string;
  errorCode?: string;
  sentAt?: string;
}

export interface SendWhatsAppRequest {
  to: string;
  templateName: string;
  parameters?: Record<string, string>;
  languageCode?: string;
}

export interface SendJobUpdateRequest {
  customerPhone: string;
  orderNumber: string;
  status: string;
  appointmentDate?: string;
  installerName?: string;
}

export interface SendSiAlertRequest {
  customerPhone: string;
  orderNumber: string;
  installerName: string;
  estimatedArrival?: string;
}

/**
 * Send WhatsApp template message
 */
export const sendWhatsApp = async (request: SendWhatsAppRequest): Promise<WhatsAppResult> => {
  const response = await apiClient.post<WhatsAppResult>('/whatsapp/send', request);
  return response.data?.data || response.data || response;
};

/**
 * Send job update notification via WhatsApp
 */
export const sendJobUpdate = async (request: SendJobUpdateRequest): Promise<WhatsAppResult> => {
  const response = await apiClient.post<WhatsAppResult>('/whatsapp/send-job-update', request);
  return response.data?.data || response.data || response;
};

/**
 * Send SI on-the-way alert via WhatsApp
 */
export const sendSiAlert = async (request: SendSiAlertRequest): Promise<WhatsAppResult> => {
  const response = await apiClient.post<WhatsAppResult>('/whatsapp/send-si-alert', request);
  return response.data?.data || response.data || response;
};

