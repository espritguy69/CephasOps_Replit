import apiClient from './client';
import type {
  GlobalSetting,
  ParserTemplate,
  MaterialTemplate,
  DocumentTemplate,
  Splitter,
  KpiProfile,
  UpdateGlobalSettingRequest,
  CreateMaterialTemplateRequest,
  UpdateMaterialTemplateRequest,
  CreateDocumentTemplateRequest,
  UpdateDocumentTemplateRequest,
  SplitterFilters,
  MaterialTemplateFilters,
  DocumentTemplateFilters
} from '../types/settings';

/**
 * Settings API
 * Handles global settings, material templates, document templates, splitters, and KPI profiles
 */

/**
 * Get global settings
 * @param filters - Optional filters (key, category)
 * @returns Global settings object
 */
export const getGlobalSettings = async (filters: { key?: string; category?: string } = {}): Promise<GlobalSetting[]> => {
  const response = await apiClient.get<GlobalSetting[]>(`/settings/global`, { params: filters });
  return response;
};

/**
 * Update global setting
 * @param key - Setting key
 * @param value - Setting value
 * @returns Updated setting
 */
export const updateGlobalSetting = async (key: string, value: any): Promise<GlobalSetting> => {
  const request: UpdateGlobalSettingRequest = { value };
  const response = await apiClient.put<GlobalSetting>(`/settings/global/${key}`, request);
  return response;
};

/**
 * Get parser template for partner
 * @param partnerId - Partner ID
 * @returns Parser template
 */
export const getParserTemplate = async (partnerId: string): Promise<ParserTemplate> => {
  const response = await apiClient.get<ParserTemplate>(`/settings/parser-template/${partnerId}`);
  return response;
};

/**
 * Get material templates list
 * @param filters - Optional filters (buildingType, partnerId)
 * @returns Array of material templates
 */
export const getMaterialTemplates = async (filters: MaterialTemplateFilters = {}): Promise<MaterialTemplate[]> => {
  const response = await apiClient.get<MaterialTemplate[]>(`/settings/material-templates`, { params: filters });
  return response;
};

/**
 * Get material template by ID
 * @param templateId - Material template ID
 * @returns Material template details
 */
export const getMaterialTemplate = async (templateId: string): Promise<MaterialTemplate> => {
  const response = await apiClient.get<MaterialTemplate>(`/settings/material-templates/${templateId}`);
  return response;
};

/**
 * Create material template
 * @param templateData - Material template creation data
 * @returns Created material template
 */
export const createMaterialTemplate = async (templateData: CreateMaterialTemplateRequest): Promise<MaterialTemplate> => {
  const response = await apiClient.post<MaterialTemplate>(`/settings/material-templates`, templateData);
  return response;
};

/**
 * Update material template
 * @param templateId - Material template ID
 * @param templateData - Material template update data
 * @returns Updated material template
 */
export const updateMaterialTemplate = async (
  templateId: string,
  templateData: UpdateMaterialTemplateRequest
): Promise<MaterialTemplate> => {
  const response = await apiClient.put<MaterialTemplate>(`/settings/material-templates/${templateId}`, templateData);
  return response;
};

/**
 * Delete material template
 * @param templateId - Material template ID
 */
export const deleteMaterialTemplate = async (templateId: string): Promise<void> => {
  await apiClient.delete(`/settings/material-templates/${templateId}`);
};

/**
 * Get document templates list
 * @param filters - Optional filters (type, category)
 * @returns Array of document templates
 */
export const getDocumentTemplates = async (filters: DocumentTemplateFilters = {}): Promise<DocumentTemplate[]> => {
  const response = await apiClient.get<DocumentTemplate[]>(`/settings/document-templates`, { params: filters });
  return response;
};

/**
 * Get document template by ID
 * @param templateId - Document template ID
 * @returns Document template details
 */
export const getDocumentTemplate = async (templateId: string): Promise<DocumentTemplate> => {
  const response = await apiClient.get<DocumentTemplate>(`/settings/document-templates/${templateId}`);
  return response;
};

/**
 * Create document template
 * @param templateData - Document template creation data
 * @returns Created document template
 */
export const createDocumentTemplate = async (templateData: CreateDocumentTemplateRequest): Promise<DocumentTemplate> => {
  const response = await apiClient.post<DocumentTemplate>(`/settings/document-templates`, templateData);
  return response;
};

/**
 * Get splitters list
 * @param filters - Optional filters (location, status)
 * @returns Array of splitters
 */
export const getSplitters = async (filters: SplitterFilters = {}): Promise<Splitter[]> => {
  const response = await apiClient.get<Splitter[]>(`/settings/splitters`, { params: filters });
  return response;
};

/**
 * Get splitter by ID
 * @param splitterId - Splitter ID
 * @returns Splitter details
 */
export const getSplitter = async (splitterId: string): Promise<Splitter> => {
  const response = await apiClient.get<Splitter>(`/settings/splitters/${splitterId}`);
  return response;
};

/**
 * Get KPI profiles list
 * @returns Array of KPI profiles
 */
export const getKpiProfiles = async (): Promise<KpiProfile[]> => {
  const response = await apiClient.get<KpiProfile[]>(`/settings/kpi-profiles`);
  return response;
};

/**
 * Get KPI profile by ID
 * @param profileId - KPI profile ID
 * @returns KPI profile details
 */
export const getKpiProfile = async (profileId: string): Promise<KpiProfile> => {
  const response = await apiClient.get<KpiProfile>(`/settings/kpi-profiles/${profileId}`);
  return response;
};

