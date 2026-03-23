/**
 * Splitters Types - Shared type definitions for Splitters module
 */

export interface SplitterPort {
  id: string;
  splitterId?: string;
  portNumber: number;
  status: 'Available' | 'Occupied' | 'Reserved' | 'Maintenance' | 'Standby';
  orderId?: string;
  orderNumber?: string;
  notes?: string;
  isStandby?: boolean;
  standbyOverrideApproved?: boolean;
  approvalAttachmentId?: string;
}

export interface Splitter {
  id: string;
  name: string;
  code?: string;
  buildingId?: string;
  buildingName?: string;
  splitterTypeId?: string;
  splitterTypeName?: string;
  location?: string;
  portCount: number;
  status: 'Active' | 'Inactive' | 'Maintenance';
  ports?: SplitterPort[];
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateSplitterRequest {
  name: string;
  code?: string;
  buildingId?: string;
  splitterTypeId?: string;
  location?: string;
  portCount: number;
  status?: 'Active' | 'Inactive' | 'Maintenance';
  notes?: string;
}

export interface UpdateSplitterRequest {
  name?: string;
  code?: string;
  buildingId?: string;
  splitterTypeId?: string;
  location?: string;
  portCount?: number;
  status?: 'Active' | 'Inactive' | 'Maintenance';
  notes?: string;
}

export interface UpdateSplitterPortRequest {
  status?: 'Available' | 'Occupied' | 'Reserved' | 'Maintenance' | 'Standby' | 'Used';
  orderId?: string;
  notes?: string;
  standbyOverrideApproved?: boolean;
  approvalAttachmentId?: string;
}

export interface SplitterFilters {
  buildingId?: string;
  isActive?: boolean;
}

