import apiClient from './client';
import type {
  DocumentTemplate,
  PlaceholderDefinition,
  CreateDocumentTemplateRequest,
  UpdateDocumentTemplateRequest,
  DocumentTemplateFilters,
  CarboneStatus
} from '../types/documentTemplates';
import type { GeneratedDocument, GenerateDocumentRequest, DocumentFilters } from '../types/documents';

/**
 * Document Templates API
 * Handles document templates and generation
 */

/**
 * Get document templates
 * @param filters - Optional filters (documentType, partnerId, isActive)
 * @returns Array of document templates
 */
export const getDocumentTemplates = async (filters: DocumentTemplateFilters = {}): Promise<DocumentTemplate[]> => {
  const response = await apiClient.get<DocumentTemplate[]>('/document-templates', { params: filters });
  return response;
};

/**
 * Get document template by ID
 * @param templateId - Template ID
 * @returns Document template details
 */
export const getDocumentTemplate = async (templateId: string): Promise<DocumentTemplate> => {
  const response = await apiClient.get<DocumentTemplate>(`/document-templates/${templateId}`);
  return response;
};

/**
 * Get placeholder definitions for document type
 * @param documentType - Document type
 * @returns Array of placeholder definitions
 */
export const getPlaceholderDefinitions = async (documentType: string): Promise<PlaceholderDefinition[]> => {
  const response = await apiClient.get<PlaceholderDefinition[]>(`/document-templates/placeholders/${documentType}`);
  return response;
};

/**
 * Create document template
 * @param templateData - Template creation data
 * @returns Created template
 */
export const createDocumentTemplate = async (templateData: CreateDocumentTemplateRequest): Promise<DocumentTemplate> => {
  const response = await apiClient.post<DocumentTemplate>('/document-templates', templateData);
  return response;
};

/**
 * Update document template
 * @param templateId - Template ID
 * @param templateData - Template update data
 * @returns Updated template
 */
export const updateDocumentTemplate = async (
  templateId: string,
  templateData: UpdateDocumentTemplateRequest
): Promise<DocumentTemplate> => {
  const response = await apiClient.put<DocumentTemplate>(`/document-templates/${templateId}`, templateData);
  return response;
};

/**
 * Delete document template
 * @param templateId - Template ID
 * @returns Promise that resolves when template is deleted
 */
export const deleteDocumentTemplate = async (templateId: string): Promise<void> => {
  await apiClient.delete(`/document-templates/${templateId}`);
};

/**
 * Activate document template
 * @param templateId - Template ID
 * @returns Activated template
 */
export const activateDocumentTemplate = async (templateId: string): Promise<DocumentTemplate> => {
  const response = await apiClient.post<DocumentTemplate>(`/document-templates/${templateId}/activate`);
  return response;
};

/**
 * Duplicate document template
 * @param templateId - Template ID
 * @returns Duplicated template
 */
export const duplicateDocumentTemplate = async (templateId: string): Promise<DocumentTemplate> => {
  const response = await apiClient.post<DocumentTemplate>(`/document-templates/${templateId}/duplicate`);
  return response;
};

/**
 * Get Carbone engine status
 * @returns Carbone status (enabled, configured)
 */
export const getCarboneStatus = async (): Promise<CarboneStatus> => {
  const response = await apiClient.get<CarboneStatus>('/document-templates/carbone-status');
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
 * @param rmaRequestId - RMA request ID
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
 * @returns Generated document details
 */
export const getGeneratedDocument = async (documentId: string): Promise<GeneratedDocument> => {
  const response = await apiClient.get<GeneratedDocument>(`/documents/${documentId}`);
  return response;
};

