/**
 * Rate group and order type/subtype mapping types (GPON Phase 1).
 * Does not affect payout resolution.
 */

export interface RateGroupDto {
  id: string;
  companyId?: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateRateGroupRequest {
  name: string;
  code: string;
  description?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface UpdateRateGroupRequest {
  name?: string;
  code?: string;
  description?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface OrderTypeSubtypeRateGroupMappingDto {
  id: string;
  orderTypeId: string;
  orderTypeName?: string;
  orderTypeCode?: string;
  orderSubtypeId?: string;
  orderSubtypeName?: string;
  orderSubtypeCode?: string;
  rateGroupId: string;
  rateGroupName?: string;
  rateGroupCode?: string;
  companyId?: string;
}

export interface AssignRateGroupToOrderTypeSubtypeRequest {
  orderTypeId: string;
  orderSubtypeId?: string;
  rateGroupId: string;
}

/** Base Work Rate (GPON Phase 2). Does not affect payout resolution. */
export interface BaseWorkRateDto {
  id: string;
  companyId?: string;
  rateGroupId: string;
  rateGroupName?: string;
  rateGroupCode?: string;
  orderCategoryId?: string;
  orderCategoryName?: string;
  orderCategoryCode?: string;
  serviceProfileId?: string;
  serviceProfileName?: string;
  serviceProfileCode?: string;
  installationMethodId?: string;
  installationMethodName?: string;
  installationMethodCode?: string;
  orderSubtypeId?: string;
  orderSubtypeName?: string;
  orderSubtypeCode?: string;
  amount: number;
  currency: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  priority: number;
  isActive: boolean;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateBaseWorkRateRequest {
  rateGroupId: string;
  orderCategoryId?: string;
  serviceProfileId?: string;
  installationMethodId?: string;
  orderSubtypeId?: string;
  amount: number;
  currency?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  priority?: number;
  isActive?: boolean;
  notes?: string;
}

export interface UpdateBaseWorkRateRequest {
  rateGroupId?: string;
  orderCategoryId?: string;
  serviceProfileId?: string;
  installationMethodId?: string;
  orderSubtypeId?: string;
  clearOrderCategoryId?: boolean;
  clearServiceProfileId?: boolean;
  clearInstallationMethodId?: boolean;
  clearOrderSubtypeId?: boolean;
  amount?: number;
  currency?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  priority?: number;
  isActive?: boolean;
  notes?: string;
}

export interface BaseWorkRateListFilter {
  rateGroupId?: string;
  orderCategoryId?: string;
  serviceProfileId?: string;
  installationMethodId?: string;
  orderSubtypeId?: string;
  isActive?: boolean;
}
