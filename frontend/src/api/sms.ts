/**
 * SMS Messaging API
 */

import apiClient from './client';

export interface SmsResult {
  success: boolean;
  messageId?: string;
  status: string;
  errorMessage?: string;
  errorCode?: string;
  sentAt?: string;
}

export interface SendSmsRequest {
  to: string;
  message: string;
}

export interface SendTemplateSmsRequest {
  to: string;
  templateCode: string;
  placeholders?: Record<string, string>;
}

/**
 * Send SMS message
 */
export const sendSms = async (request: SendSmsRequest): Promise<SmsResult> => {
  const response = await apiClient.post<SmsResult>('/sms/send', request);
  return response.data?.data || response.data || response;
};

/**
 * Send SMS using template
 */
export const sendTemplateSms = async (request: SendTemplateSmsRequest): Promise<SmsResult> => {
  const response = await apiClient.post<SmsResult>('/sms/send-template', request);
  return response.data?.data || response.data || response;
};

/**
 * Get SMS message status
 */
export const getSmsStatus = async (messageId: string): Promise<SmsResult> => {
  const response = await apiClient.get<SmsResult>(`/sms/status/${messageId}`);
  return response.data?.data || response.data || response;
};

