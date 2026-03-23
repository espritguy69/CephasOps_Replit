import apiClient from './client';

/**
 * Order Status Checklist API
 * Handles checklist items and answers for order statuses
 */

export interface OrderStatusChecklistItem {
  id: string;
  companyId?: string;
  statusCode: string;
  parentChecklistItemId?: string;
  name: string;
  description?: string;
  orderIndex: number;
  isRequired: boolean;
  isActive: boolean;
  createdByUserId?: string;
  updatedByUserId?: string;
  createdAt: string;
  updatedAt: string;
  subSteps?: OrderStatusChecklistItem[];
}

export interface OrderStatusChecklistAnswer {
  id: string;
  orderId: string;
  checklistItemId: string;
  answer: boolean;
  answeredAt: string;
  answeredByUserId: string;
  remarks?: string;
  createdAt: string;
  updatedAt: string;
  checklistItem?: OrderStatusChecklistItem;
}

export interface OrderStatusChecklistWithAnswers extends OrderStatusChecklistItem {
  answer?: OrderStatusChecklistAnswer;
  subSteps?: OrderStatusChecklistWithAnswers[];
}

export interface CreateOrderStatusChecklistItemDto {
  statusCode: string;
  parentChecklistItemId?: string;
  name: string;
  description?: string;
  orderIndex: number;
  isRequired: boolean;
  isActive?: boolean;
}

export interface UpdateOrderStatusChecklistItemDto {
  name?: string;
  description?: string;
  orderIndex?: number;
  isRequired?: boolean;
  isActive?: boolean;
}

export interface SubmitOrderStatusChecklistAnswerDto {
  checklistItemId: string;
  answer: boolean;
  remarks?: string;
}

export interface SubmitOrderStatusChecklistAnswersDto {
  answers: SubmitOrderStatusChecklistAnswerDto[];
}

export interface ChecklistValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Get all checklist items for a status
 */
export const getChecklistItemsByStatus = async (
  statusCode: string
): Promise<OrderStatusChecklistItem[]> => {
  const response = await apiClient.get<OrderStatusChecklistItem[]>(
    `/order-statuses/${statusCode}/checklist/items`
  );
  return response;
};

/**
 * Create a new checklist item
 */
export const createChecklistItem = async (
  statusCode: string,
  data: CreateOrderStatusChecklistItemDto
): Promise<OrderStatusChecklistItem> => {
  const response = await apiClient.post<OrderStatusChecklistItem>(
    `/order-statuses/${statusCode}/checklist/items`,
    data
  );
  return response;
};

/**
 * Update a checklist item
 */
export const updateChecklistItem = async (
  statusCode: string,
  itemId: string,
  data: UpdateOrderStatusChecklistItemDto
): Promise<OrderStatusChecklistItem> => {
  const response = await apiClient.put<OrderStatusChecklistItem>(
    `/order-statuses/${statusCode}/checklist/items/${itemId}`,
    data
  );
  return response;
};

/**
 * Delete a checklist item
 */
export const deleteChecklistItem = async (
  statusCode: string,
  itemId: string
): Promise<void> => {
  await apiClient.delete(`/order-statuses/${statusCode}/checklist/items/${itemId}`);
};

/**
 * Get checklist items with answers for an order
 */
export const getChecklistWithAnswers = async (
  orderId: string,
  statusCode: string
): Promise<OrderStatusChecklistWithAnswers[]> => {
  const response = await apiClient.get<OrderStatusChecklistWithAnswers[]>(
    `/orders/${orderId}/checklist/${statusCode}`
  );
  return response;
};

/**
 * Submit checklist answers
 */
export const submitChecklistAnswers = async (
  orderId: string,
  data: SubmitOrderStatusChecklistAnswersDto
): Promise<void> => {
  await apiClient.post(`/orders/${orderId}/checklist/answers`, data);
};

/**
 * Validate checklist completion for a status
 */
export const validateChecklist = async (
  orderId: string,
  statusCode: string
): Promise<ChecklistValidationResult> => {
  const response = await apiClient.get<ChecklistValidationResult>(
    `/orders/${orderId}/checklist/validate/${statusCode}`
  );
  return response;
};

/**
 * Reorder checklist items
 */
export const reorderChecklistItems = async (
  statusCode: string,
  itemOrderMap: Record<string, number>
): Promise<void> => {
  await apiClient.post(`/order-statuses/${statusCode}/checklist/items/reorder`, itemOrderMap);
};

/**
 * Bulk update checklist items
 */
export const bulkUpdateChecklistItems = async (
  statusCode: string,
  itemIds: string[],
  updateDto: UpdateOrderStatusChecklistItemDto
): Promise<void> => {
  await apiClient.post(`/order-statuses/${statusCode}/checklist/items/bulk-update`, {
    itemIds,
    updateDto,
  });
};

/**
 * Copy checklist from another status
 */
export const copyChecklistFromStatus = async (
  statusCode: string,
  sourceStatusCode: string
): Promise<void> => {
  await apiClient.post(`/order-statuses/${statusCode}/checklist/copy-from/${sourceStatusCode}`);
};

