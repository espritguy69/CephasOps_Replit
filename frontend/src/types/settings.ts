/**
 * Settings Types - Shared type definitions for Settings module
 */

export interface GlobalSetting {
  key: string;
  value: any;
  category?: string;
  description?: string;
}

export interface ParserTemplate {
  id: string;
  partnerId: string;
  partnerName?: string;
  template: any;
  isActive: boolean;
}

export interface MaterialTemplate {
  id: string;
  name: string;
  buildingTypeId?: string;
  buildingTypeName?: string;
  partnerId?: string;
  partnerName?: string;
  materials: MaterialTemplateItem[];
  isActive: boolean;
}

export interface MaterialTemplateItem {
  materialId: string;
  materialName?: string;
  quantity: number;
  unit?: string;
}

export interface DocumentTemplate {
  id: string;
  name: string;
  type: string;
  category?: string;
  content: string;
  variables?: string[];
  isActive: boolean;
}

export interface Splitter {
  id: string;
  name: string;
  code?: string;
  location?: string;
  splitterTypeId?: string;
  splitterTypeName?: string;
  status: 'Active' | 'Inactive' | 'Maintenance';
  portCount?: number;
  notes?: string;
}

export interface KpiProfile {
  id: string;
  name: string;
  description?: string;
  kpis: KpiProfileItem[];
  isActive: boolean;
}

export interface KpiProfileItem {
  kpiId: string;
  kpiName?: string;
  weight?: number;
  target?: number;
}

export interface UpdateGlobalSettingRequest {
  value: any;
}

export interface CreateMaterialTemplateRequest {
  name: string;
  buildingTypeId?: string;
  partnerId?: string;
  materials: Omit<MaterialTemplateItem, 'materialName'>[];
  isActive?: boolean;
}

export interface UpdateMaterialTemplateRequest {
  name?: string;
  buildingTypeId?: string;
  partnerId?: string;
  materials?: Omit<MaterialTemplateItem, 'materialName'>[];
  isActive?: boolean;
}

export interface CreateDocumentTemplateRequest {
  name: string;
  type: string;
  category?: string;
  content: string;
  variables?: string[];
  isActive?: boolean;
}

export interface UpdateDocumentTemplateRequest {
  name?: string;
  type?: string;
  category?: string;
  content?: string;
  variables?: string[];
  isActive?: boolean;
}

export interface SplitterFilters {
  location?: string;
  status?: string;
}

export interface MaterialTemplateFilters {
  buildingType?: string;
  partnerId?: string;
}

export interface DocumentTemplateFilters {
  type?: string;
  category?: string;
}

