/**
 * Ledger-based inventory API types (Phase 1 Chunk 2 backend)
 */

export interface LedgerReceiveRequest {
  materialId: string;
  locationId: string;
  quantity: number;
  referenceType?: string;
  referenceId?: string;
  remarks?: string;
  serialNumber?: string;
}

export interface LedgerTransferRequest {
  materialId: string;
  fromLocationId: string;
  toLocationId: string;
  quantity: number;
  referenceType?: string;
  referenceId?: string;
  remarks?: string;
}

export interface LedgerAllocateRequest {
  orderId: string;
  materialId: string;
  locationId: string;
  quantity: number;
  remarks?: string;
  serialNumber?: string;
}

export interface LedgerIssueRequest {
  orderId: string;
  materialId: string;
  locationId: string;
  quantity: number;
  remarks?: string;
  serialNumber?: string;
  allocationId?: string;
}

export interface LedgerReturnRequest {
  orderId: string;
  materialId: string;
  locationId: string;
  quantity: number;
  remarks?: string;
  serialNumber?: string;
  allocationId?: string;
}

export interface LedgerEntryDto {
  id: string;
  entryType: string;
  materialId: string;
  materialCode?: string;
  locationId: string;
  locationName?: string;
  quantity: number;
  fromLocationId?: string;
  fromLocationName?: string;
  toLocationId?: string;
  toLocationName?: string;
  orderId?: string;
  serialisedItemId?: string;
  serialNumber?: string;
  allocationId?: string;
  referenceType?: string;
  referenceId?: string;
  remarks?: string;
  createdAt: string;
  createdByUserId: string;
}

export interface LedgerFilterParams {
  departmentId?: string | null;
  materialId?: string | null;
  locationId?: string | null;
  orderId?: string | null;
  entryType?: string | null;
  fromDate?: string | null;
  toDate?: string | null;
  page?: number;
  pageSize?: number;
}

export interface LedgerListResultDto {
  items: LedgerEntryDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface StockByLocationDto {
  materialId: string;
  materialCode?: string;
  materialDescription?: string;
  locationId: string;
  locationName?: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityAvailable: number;
  isSerialised: boolean;
}

export interface SerialisedStatusDto {
  serialisedItemId: string;
  materialId: string;
  materialCode?: string;
  serialNumber: string;
  currentLocationId?: string;
  currentLocationName?: string;
  status: string;
  lastOrderId?: string;
}

export interface StockSummaryResultDto {
  byLocation: StockByLocationDto[];
  serialisedItems: SerialisedStatusDto[];
}

export interface LedgerWriteResultDto {
  ledgerEntryId?: string;
  allocationId?: string;
  entryType: string;
  message: string;
}
