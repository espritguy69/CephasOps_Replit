import apiClient from './client';

/**
 * Messaging API
 * Handles SMS and WhatsApp notifications via unified messaging service
 */

export interface MessagingResult {
  smsSent: boolean;
  smsResult?: {
    success: boolean;
    messageId?: string;
    errorMessage?: string;
  };
  whatsAppSent: boolean;
  whatsAppResult?: {
    success: boolean;
    messageId?: string;
    errorMessage?: string;
  };
  success: boolean;
  errorMessage?: string;
}

export interface JobUpdateRequest {
  customerPhone: string;
  orderNumber: string;
  status: string;
  appointmentDate?: string;
  installerName?: string;
  isUrgent?: boolean;
}

export interface SiOnTheWayRequest {
  customerPhone: string;
  orderNumber: string;
  installerName: string;
  estimatedArrival?: string;
  isUrgent?: boolean;
}

export interface TtktRequest {
  customerPhone: string;
  ticketNumber: string;
  issueDescription: string;
  resolution?: string;
  isUrgent?: boolean;
}

/**
 * Send job update notification
 * @param request - Job update request data
 * @returns Messaging result
 */
export const sendJobUpdate = async (request: JobUpdateRequest): Promise<MessagingResult> => {
  const response = await apiClient.post('/messaging/job-update', request);
  return response.data?.data || response.data || response;
};

/**
 * Send SI on-the-way alert
 * @param request - SI on-the-way request data
 * @returns Messaging result
 */
export const sendSiOnTheWay = async (request: SiOnTheWayRequest): Promise<MessagingResult> => {
  const response = await apiClient.post('/messaging/si-on-the-way', request);
  return response.data?.data || response.data || response;
};

/**
 * Send TTKT (ticket) notification
 * @param request - TTKT request data
 * @returns Messaging result
 */
export const sendTtkt = async (request: TtktRequest): Promise<MessagingResult> => {
  const response = await apiClient.post('/messaging/ttkt', request);
  return response.data?.data || response.data || response;
};

