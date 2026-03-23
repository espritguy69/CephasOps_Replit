/**
 * Company Deployment API Client
 * Handles Excel import/export for company deployment
 */

import apiClient from './client';

export interface DeploymentValidationResult {
  isValid: boolean;
  totalRecords: number;
  recordCounts: Record<string, number>;
  errors: DeploymentError[];
  warnings: DeploymentWarning[];
  missingDependencies: Record<string, string[]>;
  format: string;
}

export interface DeploymentError {
  dataType: string;
  rowNumber: number;
  field: string;
  message: string;
  rawValue?: string;
  sheetName?: string;
}

export interface DeploymentWarning {
  dataType: string;
  rowNumber: number;
  message: string;
  sheetName?: string;
}

export interface DeploymentImportResult {
  success: boolean;
  totalRecords: number;
  successCount: number;
  errorCount: number;
  warningCount: number;
  summaries: Record<string, ImportSummary>;
  errors: DeploymentError[];
  warnings: DeploymentWarning[];
  companyId?: string;
  companyName?: string;
}

export interface ImportSummary {
  dataType: string;
  total: number;
  success: number;
  errors: number;
  warnings: number;
}

export interface DeploymentImportOptions {
  duplicateHandling: 'Skip' | 'Update' | 'CreateNew';
  skipSplittersIfNotGpon?: boolean;
  createMissingDependencies?: boolean;
  dataTypesToImport?: string[];
}

/**
 * Download deployment template
 */
export async function downloadDeploymentTemplate(format: 'single' | 'separate' = 'single'): Promise<void> {
  const response = await apiClient.get(`/companies/deployment/template?format=${format}`, {
    responseType: 'blob',
  });

  const blob = new Blob([response.data], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });

  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = format === 'single' 
    ? 'company-deployment-template.xlsx' 
    : 'company-deployment-instructions.xlsx';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
}

/**
 * Validate deployment files (dry-run)
 */
export async function validateDeployment(files: File[]): Promise<DeploymentValidationResult> {
  const formData = new FormData();
  files.forEach((file) => {
    formData.append('files', file);
  });

  const response = await apiClient.post<DeploymentValidationResult>(
    '/companies/deployment/validate',
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );

  return response.data;
}

/**
 * Import company deployment
 */
export async function importDeployment(
  files: File[],
  options: DeploymentImportOptions
): Promise<DeploymentImportResult> {
  const formData = new FormData();
  files.forEach((file) => {
    formData.append('files', file);
  });

  // Add options as query params
  const params = new URLSearchParams();
  params.append('duplicateHandling', options.duplicateHandling);
  if (options.skipSplittersIfNotGpon !== undefined) {
    params.append('skipSplittersIfNotGpon', String(options.skipSplittersIfNotGpon));
  }
  if (options.createMissingDependencies !== undefined) {
    params.append('createMissingDependencies', String(options.createMissingDependencies));
  }
  if (options.dataTypesToImport && options.dataTypesToImport.length > 0) {
    options.dataTypesToImport.forEach((type) => {
      params.append('dataTypesToImport', type);
    });
  }

  const response = await apiClient.post<DeploymentImportResult>(
    `/companies/deployment/import?${params.toString()}`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );

  return response.data;
}

/**
 * Export existing company data
 */
export async function exportCompany(
  companyId?: string,
  format: 'single' | 'separate' = 'single'
): Promise<void> {
  const params = new URLSearchParams();
  if (companyId) {
    params.append('companyId', companyId);
  }
  params.append('format', format);

  const response = await apiClient.get(`/companies/deployment/export?${params.toString()}`, {
    responseType: 'blob',
  });

  const blob = new Blob([response.data], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });

  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `company-export-${new Date().toISOString().split('T')[0]}.xlsx`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
}

