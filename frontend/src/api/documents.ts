import apiClient from './client';
import type { GeneratedDocument, GenerateDocumentRequest, DocumentFilters } from '../types/documents';

/**
 * Documents API
 * Handles document generation and retrieval
 */

/**
 * Get generated documents
 * @param filters - Optional filters (referenceEntity, referenceId, documentType)
 * @returns Array of generated documents
 */
export const getGeneratedDocuments = async (filters: DocumentFilters = {}): Promise<GeneratedDocument[]> => {
  const response = await apiClient.get<GeneratedDocument[]>('/documents', { params: filters });
  return response;
};

/**
 * Get generated document by ID
 * @param documentId - Document ID
 * @returns Document details
 */
export const getGeneratedDocument = async (documentId: string): Promise<GeneratedDocument> => {
  const response = await apiClient.get<GeneratedDocument>(`/documents/${documentId}`);
  return response;
};

/**
 * Generate invoice document
 * @param invoiceId - Invoice ID
 * @param templateId - Optional template ID
 * @returns Generated document
 */
export const generateInvoiceDocument = async (
  invoiceId: string,
  templateId: string | null = null
): Promise<GeneratedDocument> => {
  const params: GenerateDocumentRequest = templateId ? { templateId } : {};
  const response = await apiClient.post<GeneratedDocument>(`/documents/invoices/${invoiceId}`, null, { params });
  return response;
};

/**
 * Generate job docket
 * @param orderId - Order ID
 * @param templateId - Optional template ID
 * @returns Generated document
 */
export const generateJobDocket = async (orderId: string, templateId: string | null = null): Promise<GeneratedDocument> => {
  const params: GenerateDocumentRequest = templateId ? { templateId } : {};
  const response = await apiClient.post<GeneratedDocument>(`/documents/orders/${orderId}/docket`, null, { params });
  return response;
};

/**
 * Generate RMA form
 * @param rmaRequestId - RMA Request ID
 * @param templateId - Optional template ID
 * @returns Generated document
 */
export const generateRmaForm = async (
  rmaRequestId: string,
  templateId: string | null = null
): Promise<GeneratedDocument> => {
  const params: GenerateDocumentRequest = templateId ? { templateId } : {};
  const response = await apiClient.post<GeneratedDocument>(`/documents/rma/${rmaRequestId}`, null, { params });
  return response;
};

