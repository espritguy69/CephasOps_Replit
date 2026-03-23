/**
 * Integration Settings API
 * Manages MyInvois, SMS, and WhatsApp integration settings
 */

import apiClient from './client';

export interface MyInvoisSettings {
  isEnabled: boolean;
  baseUrl: string;
  clientId: string;
  clientSecret: string;
  environment?: string;
}

export interface SmsSettings {
  isEnabled: boolean;
  provider: string;
  twilioAccountSid?: string;
  twilioAuthToken?: string;
  twilioFromNumber?: string;
  gatewayUrl?: string;
  gatewayApiKey?: string;
  gatewaySenderId?: string;
}

export interface WhatsAppSettings {
  isEnabled: boolean;
  provider: string;
  phoneNumberId?: string;
  accessToken?: string;
  businessAccountId?: string;
  apiVersion?: string;
  jobUpdateTemplate?: string;
  siOnTheWayTemplate?: string;
  ttktTemplate?: string;
}

export interface IntegrationSettings {
  myInvois: MyInvoisSettings;
  sms: SmsSettings;
  whatsApp: WhatsAppSettings;
}

/**
 * Get all integration settings
 */
export const getIntegrationSettings = async (): Promise<IntegrationSettings> => {
  const response = await apiClient.get<IntegrationSettings>('/integrations');
  return response.data?.data || response.data || response;
};

/**
 * Update MyInvois settings
 */
export const updateMyInvoisSettings = async (settings: MyInvoisSettings): Promise<void> => {
  await apiClient.put('/integrations/myinvois', settings);
};

/**
 * Update SMS settings
 */
export const updateSmsSettings = async (settings: SmsSettings): Promise<void> => {
  await apiClient.put('/integrations/sms', settings);
};

/**
 * Update WhatsApp settings
 */
export const updateWhatsAppSettings = async (settings: WhatsAppSettings): Promise<void> => {
  await apiClient.put('/integrations/whatsapp', settings);
};

/**
 * Test MyInvois connection
 */
export const testMyInvoisConnection = async (): Promise<{ connected: boolean }> => {
  const response = await apiClient.post<{ connected: boolean }>('/integrations/myinvois/test');
  return response.data?.data || response.data || response;
};

/**
 * Test SMS connection
 */
export const testSmsConnection = async (): Promise<{ connected: boolean }> => {
  const response = await apiClient.post<{ connected: boolean }>('/integrations/sms/test');
  return response.data?.data || response.data || response;
};

/**
 * Test WhatsApp connection
 */
export const testWhatsAppConnection = async (): Promise<{ connected: boolean }> => {
  const response = await apiClient.post<{ connected: boolean }>('/integrations/whatsapp/test');
  return response.data?.data || response.data || response;
};

