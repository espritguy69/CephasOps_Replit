/**
 * Assets Types - Shared type definitions for Assets module
 */

export enum AssetStatus {
  Active = 'Active',
  UnderMaintenance = 'UnderMaintenance',
  Reserved = 'Reserved',
  OutOfService = 'OutOfService',
  Disposed = 'Disposed',
  WrittenOff = 'WrittenOff',
  PendingDisposal = 'PendingDisposal'
}

export enum MaintenanceType {
  Preventive = 'Preventive',
  Corrective = 'Corrective',
  Inspection = 'Inspection',
  Overhaul = 'Overhaul',
  Emergency = 'Emergency',
  Calibration = 'Calibration',
  SoftwareUpdate = 'SoftwareUpdate'
}

export enum DisposalMethod {
  Sale = 'Sale',
  Scrap = 'Scrap',
  Donation = 'Donation',
  TradeIn = 'TradeIn',
  Transfer = 'Transfer',
  WriteOff = 'WriteOff',
  Return = 'Return'
}

export interface Asset {
  id: string;
  assetTypeId: string;
  assetTypeName?: string;
  assetNumber: string;
  name: string;
  description?: string;
  status: AssetStatus;
  purchaseDate?: string;
  purchasePrice?: number;
  currentValue?: number;
  location?: string;
  departmentId?: string;
  departmentName?: string;
  serialNumber?: string;
  manufacturer?: string;
  model?: string;
  warrantyExpiry?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface AssetSummary {
  totalAssets: number;
  totalValue: number;
  byStatus: Record<AssetStatus, number>;
  byType: Record<string, number>;
  upcomingMaintenance: number;
}

export interface CreateAssetRequest {
  assetTypeId: string;
  assetNumber: string;
  name: string;
  description?: string;
  status?: AssetStatus;
  purchaseDate?: string;
  purchasePrice?: number;
  location?: string;
  departmentId?: string;
  serialNumber?: string;
  manufacturer?: string;
  model?: string;
  warrantyExpiry?: string;
}

export interface UpdateAssetRequest {
  assetTypeId?: string;
  assetNumber?: string;
  name?: string;
  description?: string;
  status?: AssetStatus;
  purchaseDate?: string;
  purchasePrice?: number;
  currentValue?: number;
  location?: string;
  departmentId?: string;
  serialNumber?: string;
  manufacturer?: string;
  model?: string;
  warrantyExpiry?: string;
}

export interface MaintenanceRecord {
  id: string;
  assetId: string;
  assetName?: string;
  maintenanceType: MaintenanceType;
  scheduledDate: string;
  completedDate?: string;
  cost?: number;
  description?: string;
  performedBy?: string;
  notes?: string;
  status: 'Scheduled' | 'Completed' | 'Cancelled';
}

export interface CreateMaintenanceRecordRequest {
  assetId: string;
  maintenanceType: MaintenanceType;
  scheduledDate: string;
  cost?: number;
  description?: string;
  performedBy?: string;
  notes?: string;
}

export interface UpdateMaintenanceRecordRequest {
  maintenanceType?: MaintenanceType;
  scheduledDate?: string;
  completedDate?: string;
  cost?: number;
  description?: string;
  performedBy?: string;
  notes?: string;
  status?: 'Scheduled' | 'Completed' | 'Cancelled';
}

export interface DepreciationEntry {
  id: string;
  assetId: string;
  assetName?: string;
  period: string;
  amount: number;
  accumulatedDepreciation: number;
  bookValue: number;
  isPosted: boolean;
}

export interface DepreciationSchedule {
  assetId: string;
  assetName?: string;
  entries: DepreciationEntry[];
}

export interface RunDepreciationRequest {
  period: string;
  assetIds?: string[];
}

export interface Disposal {
  id: string;
  assetId: string;
  assetName?: string;
  disposalDate: string;
  disposalMethod: DisposalMethod;
  disposalAmount?: number;
  reason?: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  approvedBy?: string;
  approvedAt?: string;
}

export interface CreateDisposalRequest {
  assetId: string;
  disposalDate: string;
  disposalMethod: DisposalMethod;
  disposalAmount?: number;
  reason?: string;
}

export interface ApproveDisposalRequest {
  approved: boolean;
  notes?: string;
}

export interface AssetFilters {
  assetTypeId?: string;
  status?: AssetStatus;
  departmentId?: string;
  location?: string;
}

export interface MaintenanceFilters {
  assetId?: string;
  maintenanceType?: MaintenanceType;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

export interface DepreciationFilters {
  assetId?: string;
  period?: string;
  isPosted?: boolean;
}

export interface DisposalFilters {
  assetId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

export const AssetStatusLabels: Record<AssetStatus, string> = {
  [AssetStatus.Active]: 'Active',
  [AssetStatus.UnderMaintenance]: 'Under Maintenance',
  [AssetStatus.Reserved]: 'Reserved',
  [AssetStatus.OutOfService]: 'Out of Service',
  [AssetStatus.Disposed]: 'Disposed',
  [AssetStatus.WrittenOff]: 'Written Off',
  [AssetStatus.PendingDisposal]: 'Pending Disposal'
};

export const MaintenanceTypeLabels: Record<MaintenanceType, string> = {
  [MaintenanceType.Preventive]: 'Preventive',
  [MaintenanceType.Corrective]: 'Corrective',
  [MaintenanceType.Inspection]: 'Inspection',
  [MaintenanceType.Overhaul]: 'Overhaul',
  [MaintenanceType.Emergency]: 'Emergency',
  [MaintenanceType.Calibration]: 'Calibration',
  [MaintenanceType.SoftwareUpdate]: 'Software Update'
};

export const DisposalMethodLabels: Record<DisposalMethod, string> = {
  [DisposalMethod.Sale]: 'Sale',
  [DisposalMethod.Scrap]: 'Scrap/Recycle',
  [DisposalMethod.Donation]: 'Donation',
  [DisposalMethod.TradeIn]: 'Trade In',
  [DisposalMethod.Transfer]: 'Transfer',
  [DisposalMethod.WriteOff]: 'Write Off',
  [DisposalMethod.Return]: 'Return to Vendor'
};

