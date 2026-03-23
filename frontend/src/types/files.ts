/**
 * Files Types - Shared type definitions for Files module
 */

export interface FileMetadata {
  id: string;
  fileName: string;
  originalFileName: string;
  fileSize: number;
  contentType: string;
  module?: string;
  entityId?: string;
  entityType?: string;
  uploadedBy?: string;
  uploadedAt?: string;
  url?: string;
}

export interface UploadFileMetadata {
  module?: string;
  entityId?: string;
  entityType?: string;
}

export interface FileFilters {
  module?: string;
  entityId?: string;
  entityType?: string;
}

