import apiClient from './client';
import type { DocumentTemplate } from '../types/documentTemplates';

export interface DocTemplatePayload {
  name: string;
  documentType: string;
  engine: 'Handlebars';
  htmlBody: string;
  jsonSchema?: string;
  isActive?: boolean;
  description?: string;
  tags?: string[];
}

export interface TestRenderRequest {
  templateContent: string;
  outputType: string;
  dataJson: Record<string, unknown>;
}

export interface TestRenderResponse {
  renderedHtml: string;
  warnings: string[];
}

export const getTemplate = async (id: string): Promise<DocumentTemplate> => {
  return apiClient.get<DocumentTemplate>(`/document-templates/${id}`);
};

export const createTemplate = async (payload: DocTemplatePayload): Promise<DocumentTemplate> => {
  return apiClient.post<DocumentTemplate>('/document-templates', payload);
};

export const updateTemplate = async (id: string, payload: DocTemplatePayload): Promise<DocumentTemplate> => {
  return apiClient.put<DocumentTemplate>(`/document-templates/${id}`, payload);
};

export const publishTemplate = async (id: string): Promise<DocumentTemplate> => {
  return apiClient.post<DocumentTemplate>(`/document-templates/${id}/publish`);
};

export const duplicateTemplate = async (id: string): Promise<DocumentTemplate> => {
  return apiClient.post<DocumentTemplate>(`/document-templates/${id}/duplicate`);
};

export const testRenderTemplate = async (payload: TestRenderRequest): Promise<TestRenderResponse> => {
  return apiClient.post<TestRenderResponse>('/document-templates/test-render', payload);
};

export const getTemplateVariables = async (): Promise<string[]> => {
  return apiClient.get<string[]>('/document-templates/variables');
};

export const getTemplateCategories = async (): Promise<string[]> => {
  return apiClient.get<string[]>('/document-templates/categories');
};
