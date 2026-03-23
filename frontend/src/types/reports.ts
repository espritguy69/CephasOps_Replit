/**
 * Types for Reports Hub (api/reports/definitions, api/reports/{reportKey}/run).
 */

export interface ReportParameterSchemaDto {
  name: string;
  type: string;
  required: boolean;
  label?: string;
}

export interface ReportDefinitionHubDto {
  reportKey: string;
  name: string;
  description?: string;
  tags: string[];
  category?: string;
  parameterSchema: ReportParameterSchemaDto[];
  supportsExport: boolean;
}

export interface RunReportRequestDto {
  departmentId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  assignedSiId?: string;
  keyword?: string;
  search?: string;
  category?: string;
  isSerialised?: boolean;
  isActive?: boolean;
  locationId?: string;
  materialId?: string;
  orderId?: string;
  entryType?: string;
  page?: number;
  pageSize?: number;
  siId?: string;
}

export interface RunReportResultDto {
  items: Record<string, unknown>[];
  totalCount: number;
  page?: number;
  pageSize?: number;
}
