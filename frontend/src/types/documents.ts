/**
 * Documents Types - Shared type definitions for Documents module
 */

export interface GeneratedDocument {
  id: string;
  documentType: string;
  referenceEntity: string;
  referenceId: string;
  templateId?: string;
  templateName?: string;
  fileName: string;
  fileUrl?: string;
  generatedAt: string;
  generatedBy?: string;
}

export interface GenerateDocumentRequest {
  templateId?: string;
}

export interface DocumentFilters {
  referenceEntity?: string;
  referenceId?: string;
  documentType?: string;
}

