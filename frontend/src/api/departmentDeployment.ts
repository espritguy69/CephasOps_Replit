/**
 * Department Deployment API Client
 * Handles Excel import/export for department deployment (GPON, CWO, NWO, etc.)
 */

import apiClient from './client';

export interface DepartmentDeploymentConfig {
  departmentCode: string;
  departmentName: string;
  requiredDataTypes: string[];
  optionalDataTypes: string[];
  dataTypeDescriptions: Record<string, string>;
}

export interface DepartmentDeploymentValidationResult {
  isValid: boolean;
  departmentCode: string;
  totalRecords: number;
  recordCounts: Record<string, number>;
  errors: DepartmentDeploymentError[];
  warnings: DepartmentDeploymentWarning[];
  missingDependencies: Record<string, string[]>;
}

export interface DepartmentDeploymentError {
  dataType: string;
  rowNumber: number;
  field: string;
  message: string;
  rawValue?: string;
  sheetName?: string;
}

export interface DepartmentDeploymentWarning {
  dataType: string;
  rowNumber: number;
  message: string;
  sheetName?: string;
}

export interface DepartmentDeploymentImportResult {
  success: boolean;
  departmentCode: string;
  departmentId?: string;
  totalRecords: number;
  successCount: number;
  errorCount: number;
  warningCount: number;
  summaries: Record<string, DepartmentImportSummary>;
  errors: DepartmentDeploymentError[];
  warnings: DepartmentDeploymentWarning[];
}

export interface DepartmentImportSummary {
  dataType: string;
  total: number;
  success: number;
  errors: number;
  warnings: number;
}

export interface DepartmentDeploymentImportOptions {
  departmentCode: string;
  departmentName?: string;
  duplicateHandling: 'Skip' | 'Update' | 'CreateNew';
  createMissingDependencies?: boolean;
  createDepartmentIfNotExists?: boolean;
  dataTypesToImport?: string[];
}

/**
 * Get available department deployment configurations
 */
export async function getDeploymentConfigurations(): Promise<DepartmentDeploymentConfig[]> {
  const response = await apiClient.get<DepartmentDeploymentConfig[]>('/departments/deployment/configurations');
  return response;
}

/**
 * Get deployment configuration for a specific department
 */
export async function getDeploymentConfiguration(
  departmentCode: string
): Promise<DepartmentDeploymentConfig> {
  const response = await apiClient.get<DepartmentDeploymentConfig>(
    `/departments/deployment/configurations/${departmentCode}`
  );
  return response;
}

/**
 * Download deployment template
 */
export async function downloadDeploymentTemplate(departmentCode: string): Promise<void> {
  const response = await apiClient.get(`/departments/deployment/template?departmentCode=${departmentCode}`, {
    responseType: 'blob',
  });

  const blob = new Blob([response.data], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });

  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `${departmentCode.toLowerCase()}-deployment-template.xlsx`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
}

/**
 * Validate deployment files (dry-run)
 */
export async function validateDeployment(
  files: File[],
  departmentCode: string
): Promise<DepartmentDeploymentValidationResult> {
  const formData = new FormData();
  files.forEach((file) => {
    formData.append('files', file);
  });

  const response = await apiClient.post<DepartmentDeploymentValidationResult>(
    `/departments/deployment/validate?departmentCode=${departmentCode}`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );

  return response;
}

/**
 * Import department deployment
 */
export async function importDeployment(
  files: File[],
  options: DepartmentDeploymentImportOptions
): Promise<DepartmentDeploymentImportResult> {
  const formData = new FormData();
  files.forEach((file) => {
    formData.append('files', file);
  });

  // Add options as query params
  const params = new URLSearchParams();
  params.append('departmentCode', options.departmentCode);
  if (options.departmentName) {
    params.append('departmentName', options.departmentName);
  }
  params.append('duplicateHandling', options.duplicateHandling);
  if (options.createMissingDependencies !== undefined) {
    params.append('createMissingDependencies', String(options.createMissingDependencies));
  }
  if (options.createDepartmentIfNotExists !== undefined) {
    params.append('createDepartmentIfNotExists', String(options.createDepartmentIfNotExists));
  }
  if (options.dataTypesToImport && options.dataTypesToImport.length > 0) {
    options.dataTypesToImport.forEach((type) => {
      params.append('dataTypesToImport', type);
    });
  }

  const response = await apiClient.post<DepartmentDeploymentImportResult>(
    `/departments/deployment/import?${params.toString()}`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );

  return response;
}

/**
 * Export existing department data
 */
export async function exportDepartmentData(
  departmentCode: string,
  departmentId?: string
): Promise<void> {
  const params = new URLSearchParams();
  params.append('departmentCode', departmentCode);
  if (departmentId) {
    params.append('departmentId', departmentId);
  }

  const response = await apiClient.get(`/departments/deployment/export?${params.toString()}`, {
    responseType: 'blob',
  });

  const blob = new Blob([response.data], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });

  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `${departmentCode.toLowerCase()}-export-${new Date().toISOString().split('T')[0]}.xlsx`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
}

