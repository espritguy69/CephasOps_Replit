/**
 * Installation Methods Types - Shared type definitions for Installation Methods module
 */

export enum InstallationCategory {
  FTTH = 'FTTH',
  FTTO = 'FTTO',
  FTTR = 'FTTR',
  FTTC = 'FTTC',
  FTTB = 'FTTB',
  FTTP = 'FTTP'
}

export interface InstallationMethod {
  id: string;
  name: string;
  code?: string;
  description?: string;
  category: InstallationCategory;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateInstallationMethodRequest {
  name: string;
  code?: string;
  description?: string;
  category: InstallationCategory;
  isActive?: boolean;
}

export interface UpdateInstallationMethodRequest {
  name?: string;
  code?: string;
  description?: string;
  category?: InstallationCategory;
  isActive?: boolean;
}

export interface InstallationMethodFilters {
  category?: InstallationCategory;
  isActive?: boolean;
}

export const InstallationCategoryLabels: Record<InstallationCategory, string> = {
  [InstallationCategory.FTTH]: 'Fibre To The Home',
  [InstallationCategory.FTTO]: 'Fibre To The Office',
  [InstallationCategory.FTTR]: 'Fibre To The Room',
  [InstallationCategory.FTTC]: 'Fibre To The Curb',
  [InstallationCategory.FTTB]: 'Fibre To The Building',
  [InstallationCategory.FTTP]: 'Fibre To The Premises'
};

