import apiClient from './client';

export interface CreateRmaRequestItem {
  serialisedItemId: string;
  originalOrderId?: string;
  notes?: string;
}

export interface CreateRmaRequest {
  partnerId: string;
  orderId?: string;
  items: CreateRmaRequestItem[];
  reason: string;
}

export interface RmaRequest {
  id: string;
  partnerId: string;
  partnerName?: string;
  orderId?: string;
  status: string;
  serialNumbers: string[];
  reason: string;
  notes?: string;
  createdAt: string;
}

/**
 * Create RMA request
 */
export const createRmaRequest = async (
  data: CreateRmaRequest
): Promise<RmaRequest> => {
  const response = await apiClient.post<RmaRequest | { data: RmaRequest }>(
    `/rma/requests`,
    data
  );
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: RmaRequest }).data;
  }
  return response as RmaRequest;
};

/**
 * Create RMA request from order (convenience method)
 */
export const createRmaFromOrder = async (
  orderId: string,
  data: CreateRmaRequest
): Promise<RmaRequest> => {
  return createRmaRequest({
    ...data,
    orderId: orderId
  });
};

/**
 * Get RMA requests for an order
 */
export const getRmaRequestsByOrder = async (orderId: string): Promise<RmaRequest[]> => {
  const response = await apiClient.get<RmaRequest[] | { data: RmaRequest[] }>(
    `/rma/orders/${orderId}`
  );
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: RmaRequest[] }).data;
  }
  return [];
};

