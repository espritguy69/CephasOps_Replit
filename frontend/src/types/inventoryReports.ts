/**
 * Types for Phase 2.2.1 ledger-based report APIs (contract-aligned).
 * Backend may not implement yet; UI handles empty/error.
 */

/** Usage summary: totals in date range, optional group by material/location/department */
export interface UsageSummaryTotalsDto {
  received: number;
  transferred: number;
  issued: number;
  returned: number;
  adjusted?: number;
  scrapped?: number;
}

export interface UsageSummaryRowDto {
  keyId: string;
  keyName?: string;
  received: number;
  transferred: number;
  issued: number;
  returned: number;
  adjusted?: number;
  scrapped?: number;
}

export interface UsageSummaryReportResultDto {
  fromDate: string;
  toDate: string;
  groupBy?: string | null;
  totals?: UsageSummaryTotalsDto | null;
  items?: UsageSummaryRowDto[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
}

/** Stock-by-location over time (daily/weekly/monthly) */
export interface StockByLocationHistoryRowDto {
  periodStart: string;
  periodEnd: string;
  materialId: string;
  materialCode?: string;
  locationId: string;
  locationName?: string;
  quantityOnHand: number;
  quantityReserved?: number;
}

export interface StockByLocationHistoryResultDto {
  fromDate: string;
  toDate: string;
  snapshotType: string;
  items: StockByLocationHistoryRowDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Serial lifecycle: events per serial */
export interface SerialLifecycleEventDto {
  ledgerEntryId: string;
  entryType: string;
  quantity: number;
  locationId?: string;
  locationName?: string;
  fromLocationId?: string;
  toLocationId?: string;
  orderId?: string;
  orderReference?: string;
  createdAt: string;
  remarks?: string;
  referenceType?: string;
}

export interface SerialLifecycleDto {
  serialNumber: string;
  materialId: string;
  materialCode?: string;
  serialisedItemId?: string;
  events: SerialLifecycleEventDto[];
}

export interface SerialLifecycleReportResultDto {
  serialsQueried: string[];
  serialLifecycles: SerialLifecycleDto[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
}
