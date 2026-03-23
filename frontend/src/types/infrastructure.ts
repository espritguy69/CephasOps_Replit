/**
 * Infrastructure Types - Shared type definitions for Building Infrastructure module
 */

export enum SplitterStatus {
  Active = 'Active',
  Full = 'Full',
  Faulty = 'Faulty',
  MaintenanceRequired = 'MaintenanceRequired',
  Decommissioned = 'Decommissioned'
}

export enum HubBoxStatus {
  Active = 'Active',
  Full = 'Full',
  Faulty = 'Faulty',
  MaintenanceRequired = 'MaintenanceRequired',
  Decommissioned = 'Decommissioned'
}

export enum PoleStatus {
  Good = 'Good',
  NeedsInspection = 'NeedsInspection',
  Damaged = 'Damaged',
  Unusable = 'Unusable',
  PendingApproval = 'PendingApproval'
}

export type PoleType = 'TNB' | 'Telekom' | 'Private' | 'JKR' | 'Other';

export interface BuildingInfrastructure {
  buildingId: string;
  blocks: BuildingBlock[];
  splitters: BuildingSplitter[];
  streets: Street[];
  hubBoxes: HubBox[];
  poles: Pole[];
}

export interface BuildingBlock {
  id: string;
  buildingId: string;
  name: string;
  code?: string;
  description?: string;
  floor?: number;
  notes?: string;
}

export interface BuildingSplitter {
  id: string;
  buildingId: string;
  name: string;
  code?: string;
  splitterTypeId?: string;
  splitterTypeName?: string;
  location?: string;
  portCount: number;
  portsUsed: number;
  status: SplitterStatus;
  notes?: string;
}

export interface Street {
  id: string;
  buildingId: string;
  name: string;
  code?: string;
  description?: string;
  notes?: string;
}

export interface HubBox {
  id: string;
  buildingId: string;
  name: string;
  code?: string;
  location?: string;
  portCount?: number;
  portsUsed?: number;
  status: HubBoxStatus;
  notes?: string;
}

export interface Pole {
  id: string;
  buildingId: string;
  poleNumber?: string;
  poleType: PoleType;
  location?: string;
  status: PoleStatus;
  height?: number;
  notes?: string;
}

export interface CreateBuildingBlockRequest {
  name: string;
  code?: string;
  description?: string;
  floor?: number;
  notes?: string;
}

export interface UpdateBuildingBlockRequest {
  name?: string;
  code?: string;
  description?: string;
  floor?: number;
  notes?: string;
}

export interface CreateBuildingSplitterRequest {
  name: string;
  code?: string;
  splitterTypeId?: string;
  location?: string;
  portCount: number;
  status?: SplitterStatus;
  notes?: string;
}

export interface UpdateBuildingSplitterRequest {
  name?: string;
  code?: string;
  splitterTypeId?: string;
  location?: string;
  portCount?: number;
  status?: SplitterStatus;
  notes?: string;
}

export interface CreateStreetRequest {
  name: string;
  code?: string;
  description?: string;
  notes?: string;
}

export interface UpdateStreetRequest {
  name?: string;
  code?: string;
  description?: string;
  notes?: string;
}

export interface CreateHubBoxRequest {
  name: string;
  code?: string;
  location?: string;
  portCount?: number;
  status?: HubBoxStatus;
  notes?: string;
}

export interface UpdateHubBoxRequest {
  name?: string;
  code?: string;
  location?: string;
  portCount?: number;
  status?: HubBoxStatus;
  notes?: string;
}

export interface CreatePoleRequest {
  poleNumber?: string;
  poleType: PoleType;
  location?: string;
  status?: PoleStatus;
  height?: number;
  notes?: string;
}

export interface UpdatePoleRequest {
  poleNumber?: string;
  poleType?: PoleType;
  location?: string;
  status?: PoleStatus;
  height?: number;
  notes?: string;
}

export interface SplitterFilters {
  status?: SplitterStatus;
}

export const SplitterStatusLabels: Record<SplitterStatus, string> = {
  [SplitterStatus.Active]: 'Active',
  [SplitterStatus.Full]: 'Full',
  [SplitterStatus.Faulty]: 'Faulty',
  [SplitterStatus.MaintenanceRequired]: 'Maintenance Required',
  [SplitterStatus.Decommissioned]: 'Decommissioned'
};

export const PoleStatusLabels: Record<PoleStatus, string> = {
  [PoleStatus.Good]: 'Good',
  [PoleStatus.NeedsInspection]: 'Needs Inspection',
  [PoleStatus.Damaged]: 'Damaged',
  [PoleStatus.Unusable]: 'Unusable',
  [PoleStatus.PendingApproval]: 'Pending Approval'
};

export const PoleTypes: PoleType[] = ['TNB', 'Telekom', 'Private', 'JKR', 'Other'];

