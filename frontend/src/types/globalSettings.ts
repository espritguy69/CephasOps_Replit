/**
 * Global Settings Types - Shared type definitions for Global Settings module
 */

export interface GlobalSetting {
  key: string;
  value: any;
  module?: string;
  category?: string;
  description?: string;
  dataType?: string;
  isEncrypted?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateGlobalSettingRequest {
  key: string;
  value: any;
  module?: string;
  category?: string;
  description?: string;
  dataType?: string;
  isEncrypted?: boolean;
}

export interface UpdateGlobalSettingRequest {
  value?: any;
  module?: string;
  category?: string;
  description?: string;
  dataType?: string;
  isEncrypted?: boolean;
}

export interface GlobalSettingFilters {
  module?: string;
}

