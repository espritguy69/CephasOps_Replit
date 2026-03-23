/**
 * Ledger-based inventory API (Phase 1 Chunk 2)
 * Uses existing api client; departmentId is passed as query param (GET via params, POST via URL).
 */
import apiClient from './client';
import type {
  StockSummaryResultDto,
  LedgerListResultDto,
  LedgerFilterParams,
  LedgerReceiveRequest,
  LedgerTransferRequest,
  LedgerAllocateRequest,
  LedgerIssueRequest,
  LedgerReturnRequest,
  LedgerWriteResultDto
} from '../types/ledger';

function buildQueryString(params: Record<string, string | number | boolean | undefined | null>): string {
  const cleaned: Record<string, string> = {};
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') cleaned[k] = String(v);
  });
  const qs = new URLSearchParams(cleaned).toString();
  return qs ? `?${qs}` : '';
}

/**
 * GET stock summary (by location + serialised status). Uses department from context via params.
 */
export async function getStockSummary(params: {
  departmentId?: string | null;
  locationId?: string | null;
  materialId?: string | null;
} = {}): Promise<StockSummaryResultDto> {
  const response = await apiClient.get<StockSummaryResultDto>('/inventory/stock-summary', { params });
  return response ?? { byLocation: [], serialisedItems: [] };
}

/**
 * GET ledger (filterable, paged). Uses department from context via params.
 */
export async function getLedger(params: LedgerFilterParams = {}): Promise<LedgerListResultDto> {
  const response = await apiClient.get<LedgerListResultDto>('/inventory/ledger', {
    params: {
      departmentId: params.departmentId ?? undefined,
      materialId: params.materialId ?? undefined,
      locationId: params.locationId ?? undefined,
      orderId: params.orderId ?? undefined,
      entryType: params.entryType ?? undefined,
      fromDate: params.fromDate ?? undefined,
      toDate: params.toDate ?? undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 50
    }
  });
  return response ?? { items: [], totalCount: 0, page: 1, pageSize: 50 };
}

/**
 * POST receive. Pass departmentId so backend can enforce access (query param).
 */
export async function receiveStock(
  body: LedgerReceiveRequest,
  departmentId?: string | null
): Promise<LedgerWriteResultDto> {
  const url = '/inventory/receive' + buildQueryString({ departmentId: departmentId ?? undefined });
  return apiClient.post<LedgerWriteResultDto>(url, body);
}

/**
 * POST transfer.
 */
export async function transferStock(
  body: LedgerTransferRequest,
  departmentId?: string | null
): Promise<LedgerWriteResultDto> {
  const url = '/inventory/transfer' + buildQueryString({ departmentId: departmentId ?? undefined });
  return apiClient.post<LedgerWriteResultDto>(url, body);
}

/**
 * POST allocate.
 */
export async function allocateStock(
  body: LedgerAllocateRequest,
  departmentId?: string | null
): Promise<LedgerWriteResultDto> {
  const url = '/inventory/allocate' + buildQueryString({ departmentId: departmentId ?? undefined });
  return apiClient.post<LedgerWriteResultDto>(url, body);
}

/**
 * POST issue.
 */
export async function issueStock(
  body: LedgerIssueRequest,
  departmentId?: string | null
): Promise<LedgerWriteResultDto> {
  const url = '/inventory/issue' + buildQueryString({ departmentId: departmentId ?? undefined });
  return apiClient.post<LedgerWriteResultDto>(url, body);
}

/**
 * POST return.
 */
export async function returnStock(
  body: LedgerReturnRequest,
  departmentId?: string | null
): Promise<LedgerWriteResultDto> {
  const url = '/inventory/return' + buildQueryString({ departmentId: departmentId ?? undefined });
  return apiClient.post<LedgerWriteResultDto>(url, body);
}
