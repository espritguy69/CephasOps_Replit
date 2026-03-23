/**
 * Document Templates Types - Shared type definitions for Document Templates module
 */

/**
 * Document engine type - determines how templates are rendered
 * - Handlebars: HTML template with Handlebars syntax, rendered via QuestPDF
 * - CarboneHtml: HTML template rendered via Carbone engine
 * - CarboneDocx: DOCX/ODT template file rendered via Carbone engine
 */
export type DocumentEngineType = 'Handlebars' | 'CarboneHtml' | 'CarboneDocx';

export interface DocumentTemplate {
  id: string;
  name: string;
  documentType: string;
  partnerId?: string;
  partnerName?: string;
  engine: DocumentEngineType;
  htmlBody: string;
  templateFileId?: string;
  templateFileName?: string;
  jsonSchema?: string;
  description?: string;
  tags?: string[];
  version?: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  // Legacy field mapping
  content?: string;
  variables?: string[];
}

export interface PlaceholderDefinition {
  id: string;
  key: string;
  description: string;
  exampleValue?: string;
  isRequired?: boolean;
  // Legacy field mapping
  name?: string;
  example?: string;
  required?: boolean;
}

export interface CreateDocumentTemplateRequest {
  name: string;
  documentType: string;
  partnerId?: string;
  engine: DocumentEngineType;
  htmlBody: string;
  templateFileId?: string;
  jsonSchema?: string;
  description?: string;
  tags?: string[];
  // Legacy field mapping
  content?: string;
  variables?: string[];
  isActive?: boolean;
}

export interface UpdateDocumentTemplateRequest {
  name?: string;
  documentType?: string;
  partnerId?: string;
  engine?: DocumentEngineType;
  htmlBody?: string;
  templateFileId?: string;
  jsonSchema?: string;
  isActive?: boolean;
  description?: string;
  tags?: string[];
  // Legacy field mapping
  content?: string;
  variables?: string[];
}

export interface DocumentTemplateFilters {
  documentType?: string;
  partnerId?: string;
  isActive?: boolean;
}

export interface CarboneStatus {
  enabled: boolean;
  configured: boolean;
}

