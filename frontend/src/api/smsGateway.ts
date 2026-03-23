import apiClient from './client';

/**
 * SMS Gateway API
 * Handles SMS Gateway registration and management
 */

export interface SmsGateway {
  id: string;
  deviceName: string;
  baseUrl: string;
  apiKey: string;
  lastSeenAtUtc: string;
  isActive: boolean;
  additionalInfo?: string;
  createdAt: string;
  updatedAt: string;
}

export interface RegisterSmsGatewayRequest {
  deviceName: string;
  baseUrl: string;
  apiKey: string;
  additionalInfo?: string;
}

/**
 * Register or update an SMS Gateway
 */
export const registerSmsGateway = async (data: RegisterSmsGatewayRequest): Promise<string> => {
  const response = await apiClient.post<{ data: string }>('/sms-gateway/register', data);
  return typeof response === 'string' ? response : response.data || response;
};

/**
 * Get the currently active SMS Gateway
 */
export const getActiveSmsGateway = async (): Promise<SmsGateway | null> => {
  try {
    const response = await apiClient.get<SmsGateway>('/sms-gateway/active');
    return response;
  } catch (error: any) {
    if (error?.response?.status === 404) {
      return null;
    }
    throw error;
  }
};

/**
 * Get all SMS Gateways
 */
export const getAllSmsGateways = async (): Promise<SmsGateway[]> => {
  const response = await apiClient.get<SmsGateway[]>('/sms-gateway');
  return response;
};

/**
 * Deactivate an SMS Gateway
 */
export const deactivateSmsGateway = async (id: string): Promise<void> => {
  await apiClient.delete(`/sms-gateway/${id}`);
};

