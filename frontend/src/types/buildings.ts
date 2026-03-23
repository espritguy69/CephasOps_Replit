/**
 * Buildings Types - Shared type definitions for Buildings module
 *
 * Building classification (what the building is) comes from the API: use buildingTypeId / buildingTypeName
 * from the BuildingTypes reference data (e.g. Condominium, Office Tower, Terrace House).
 * The PropertyType enum is deprecated for new code; kept for backward compatibility and legacy display.
 */

export enum PropertyType {
  MDU = 'MDU',
  SDU = 'SDU',
  Shoplot = 'Shoplot',
  Factory = 'Factory',
  Office = 'Office',
  Other = 'Other'
}

export type ContactRole = 
  | 'Building Manager'
  | 'Maintenance'
  | 'Security'
  | 'Reception'
  | 'Management Office'
  | 'Other';

export interface Building {
  id: string;
  name: string;
  code?: string;
  /** @deprecated Use buildingTypeId / buildingTypeName (from API) for building classification. */
  propertyType?: PropertyType;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  postcode: string;
  installationMethodId?: string;
  installationMethodName?: string;
  buildingTypeId?: string;
  buildingTypeName?: string;
  isActive: boolean;
  notes?: string;
  contacts?: BuildingContact[];
  rules?: BuildingRules;
  createdAt?: string;
  updatedAt?: string;
}

export interface BuildingContact {
  id: string;
  buildingId: string;
  name: string;
  role: ContactRole | string; // Allow string for flexibility
  phone?: string;
  email?: string;
  remarks?: string; // Backend uses 'remarks', frontend may use 'notes'
  notes?: string; // Keep for backward compatibility
  isPrimary?: boolean;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface BuildingRules {
  id: string;
  buildingId: string;
  accessRules?: string;
  installationRules?: string;
  otherNotes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface BuildingsSummary {
  totalBuildings: number;
  byPropertyType: Record<PropertyType, number>;
  byState: Record<string, number>;
  activeBuildings: number;
  inactiveBuildings: number;
}

export interface CreateBuildingRequest {
  name: string;
  code?: string;
  /** @deprecated Prefer buildingTypeId (from BuildingTypes API) for building classification. */
  propertyType?: PropertyType;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  postcode: string;
  installationMethodId?: string;
  buildingTypeId?: string;
  isActive?: boolean;
  notes?: string;
}

export interface UpdateBuildingRequest {
  name?: string;
  code?: string;
  propertyType?: PropertyType;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  postcode?: string;
  installationMethodId?: string;
  buildingTypeId?: string;
  isActive?: boolean;
  notes?: string;
}

export interface CreateBuildingContactRequest {
  name: string;
  role: ContactRole | string;
  phone?: string;
  email?: string;
  remarks?: string;
  notes?: string; // Keep for backward compatibility
  isPrimary?: boolean;
  isActive?: boolean;
}

export interface UpdateBuildingContactRequest {
  name?: string;
  role?: ContactRole | string;
  phone?: string;
  email?: string;
  remarks?: string;
  notes?: string; // Keep for backward compatibility
  isPrimary?: boolean;
  isActive?: boolean;
}

export interface SaveBuildingRulesRequest {
  accessRules?: string;
  installationRules?: string;
  otherNotes?: string;
}

export interface BuildingFilters {
  propertyType?: PropertyType;
  installationMethodId?: string;
  state?: string;
  city?: string;
  isActive?: boolean;
}

export interface ImportResult {
  success: boolean;
  imported: number;
  failed: number;
  errors?: string[];
}

/** Building list item (e.g. for merge candidates). */
export interface BuildingListItem {
  id: string;
  name: string;
  code?: string;
  city: string;
  state: string;
  ordersCount: number;
}

/** Preview of merging source building into target. */
export interface BuildingMergePreview {
  sourceBuildingId: string;
  targetBuildingId: string;
  sourceBuildingName: string;
  targetBuildingName: string;
  ordersToReassignCount: number;
  parsedDraftsToReassignCount: number;
  orderIdsToReassign: string[];
}

/** Request to merge source into target. */
export interface MergeBuildingsRequest {
  sourceBuildingId: string;
  targetBuildingId: string;
}

/** Result of building merge. */
export interface BuildingMergeResult {
  ordersMovedCount: number;
  parsedDraftsReassignedCount: number;
  sourceSoftDeleted: boolean;
  message: string;
}

export const PropertyTypeLabels: Record<PropertyType, string> = {
  [PropertyType.MDU]: 'Condo/Apartment (MDU)',
  [PropertyType.SDU]: 'Landed/SDU',
  [PropertyType.Shoplot]: 'Shoplot',
  [PropertyType.Factory]: 'Factory',
  [PropertyType.Office]: 'Office',
  [PropertyType.Other]: 'Other'
};

export const ContactRoles: ContactRole[] = [
  'Building Manager',
  'Maintenance',
  'Security',
  'Reception',
  'Management Office',
  'Other'
];

