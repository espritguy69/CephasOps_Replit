/**
 * Rate Engine Types - Shared type definitions for Universal Rate Engine
 * Per RATE_ENGINE.md and GPON_RATECARDS.md specifications
 */

// Rate Context enum
export type RateContext = 
  | 'GponJob'
  | 'NwoScope'
  | 'CwoScope'
  | 'BarberService'
  | 'TravelPackage'
  | 'SpaService';

// Rate Kind enum
export type RateKind = 
  | 'Revenue'
  | 'Payout'
  | 'Bonus'
  | 'Penalty'
  | 'Commission';

// Unit of Measure enum
export type UnitOfMeasure = 
  | 'Job'
  | 'Meter'
  | 'Unit'
  | 'Hour'
  | 'Service'
  | 'Session'
  | 'Pax'
  | 'Device';

// Payout Type enum
export type PayoutType = 'FixedAmount' | 'Percentage';

/**
 * RateCard entity - header for rate card lines
 */
export interface RateCard {
  id: string;
  companyId: string;
  verticalId?: string;
  verticalName?: string;
  departmentId?: string;
  departmentName?: string;
  rateContext: RateContext;
  rateKind: RateKind;
  name: string;
  description?: string;
  validFrom?: string;
  validTo?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  lines?: RateCardLine[];
}

/**
 * RateCardLine entity - individual rate entries
 */
export interface RateCardLine {
  id: string;
  companyId: string;
  rateCardId: string;
  dimension1?: string;
  dimension2?: string;
  dimension3?: string;
  dimension4?: string;
  partnerGroupId?: string;
  partnerGroupName?: string;
  partnerId?: string;
  partnerName?: string;
  rateAmount: number;
  unitOfMeasure: UnitOfMeasure;
  currency: string;
  payoutType: PayoutType;
  extraJson?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

/**
 * GPON Partner Job Rate - revenue rates from partners
 */
export interface GponPartnerJobRate {
  id: string;
  companyId: string;
  partnerGroupId: string;
  partnerGroupName?: string;
  partnerId?: string;
  partnerName?: string;
  orderTypeId: string;
  orderTypeName?: string;
  orderCategoryId: string;
  orderCategoryName?: string;
  installationMethodId?: string;
  installationMethodName?: string;
  revenueAmount: number;
  currency: string;
  validFrom?: string;
  validTo?: string;
  isActive: boolean;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

/**
 * GPON SI Job Rate - payout rates to SIs by level
 */
export interface GponSiJobRate {
  id: string;
  companyId: string;
  orderTypeId: string;
  orderTypeName?: string;
  orderCategoryId: string;
  orderCategoryName?: string;
  installationMethodId?: string;
  installationMethodName?: string;
  siLevel: string;
  partnerGroupId?: string;
  partnerGroupName?: string;
  payoutAmount: number;
  currency: string;
  validFrom?: string;
  validTo?: string;
  isActive: boolean;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

/**
 * GPON SI Custom Rate - per-SI custom rate overrides
 */
export interface GponSiCustomRate {
  id: string;
  companyId: string;
  serviceInstallerId: string;
  serviceInstallerName?: string;
  orderTypeId: string;
  orderTypeName?: string;
  orderCategoryId: string;
  orderCategoryName?: string;
  installationMethodId?: string;
  installationMethodName?: string;
  customPayoutAmount: number;
  currency: string;
  reason?: string;
  approvedById?: string;
  approvedByName?: string;
  validFrom?: string;
  validTo?: string;
  isActive: boolean;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

// Request/Response types
export interface CreateRateCardRequest {
  verticalId?: string;
  departmentId?: string;
  rateContext: RateContext;
  rateKind: RateKind;
  name: string;
  description?: string;
  validFrom?: string;
  validTo?: string;
  isActive?: boolean;
}

export interface UpdateRateCardRequest {
  verticalId?: string;
  departmentId?: string;
  rateContext?: RateContext;
  rateKind?: RateKind;
  name?: string;
  description?: string;
  validFrom?: string;
  validTo?: string;
  isActive?: boolean;
}

export interface CreateRateCardLineRequest {
  rateCardId: string;
  dimension1?: string;
  dimension2?: string;
  dimension3?: string;
  dimension4?: string;
  partnerGroupId?: string;
  partnerId?: string;
  rateAmount: number;
  unitOfMeasure?: UnitOfMeasure;
  currency?: string;
  payoutType?: PayoutType;
  extraJson?: string;
  isActive?: boolean;
}

export interface CreateGponPartnerJobRateRequest {
  partnerGroupId: string;
  partnerId?: string;
  orderTypeId: string;
  orderCategoryId: string;
  installationMethodId?: string;
  revenueAmount: number;
  currency?: string;
  validFrom?: string;
  validTo?: string;
  isActive?: boolean;
  notes?: string;
}

export interface CreateGponSiJobRateRequest {
  orderTypeId: string;
  orderCategoryId: string;
  installationMethodId?: string;
  siLevel: string;
  partnerGroupId?: string;
  payoutAmount: number;
  currency?: string;
  validFrom?: string;
  validTo?: string;
  isActive?: boolean;
  notes?: string;
}

export interface CreateGponSiCustomRateRequest {
  serviceInstallerId: string;
  orderTypeId: string;
  orderCategoryId: string;
  installationMethodId?: string;
  customPayoutAmount: number;
  currency?: string;
  reason?: string;
  validFrom?: string;
  validTo?: string;
  isActive?: boolean;
  notes?: string;
}

// Filter types
export interface RateCardFilters {
  verticalId?: string;
  departmentId?: string;
  rateContext?: RateContext;
  rateKind?: RateKind;
  isActive?: boolean;
}

export interface GponPartnerJobRateFilters {
  partnerGroupId?: string;
  partnerId?: string;
  orderTypeId?: string;
  orderCategoryId?: string;
  isActive?: boolean;
}

export interface GponSiJobRateFilters {
  orderTypeId?: string;
  orderCategoryId?: string;
  siLevel?: string;
  isActive?: boolean;
}

export interface GponSiCustomRateFilters {
  serviceInstallerId?: string;
  orderTypeId?: string;
  isActive?: boolean;
}

// Rate resolution types (legacy / simple)
export interface RateResolutionRequest {
  partnerId?: string;
  partnerGroupId?: string;
  orderTypeId: string;
  orderCategoryId: string;
  installationMethodId?: string;
  siLevel?: string;
  serviceInstallerId?: string;
}

export interface RateResolutionResult {
  revenueAmount: number;
  payoutAmount: number;
  revenueRateSource?: string;
  revenueRateId?: string;
  payoutRateSource?: string;
  payoutRateId?: string;
  margin: number;
  marginPercentage: number;
}

// GPON rate resolution (full result with trace)
export interface GponRateResolutionRequest {
  orderTypeId: string;
  orderCategoryId: string;
  installationMethodId?: string;
  partnerGroupId?: string;
  partnerId?: string;
  serviceInstallerId?: string;
  siLevel?: string;
  referenceDate?: string;
  companyId?: string;
}

/** Context used for resolution (request echo). */
export interface ResolutionContextDto {
  companyId?: string;
  effectiveDateUsed?: string;
  orderTypeId?: string;
  orderSubtypeId?: string;
  orderCategoryId?: string;
  installationMethodId?: string;
  siTier?: string;
  partnerGroupId?: string;
}

/** IDs of matched rate(s) for debugging. */
export interface MatchedRuleDetailsDto {
  rateGroupId?: string;
  baseWorkRateId?: string;
  legacyRateId?: string;
  customRateId?: string;
  serviceProfileId?: string;
}

/** One modifier application for trace. */
export interface ModifierTraceItemDto {
  modifierType: string;
  operation: string;
  value: number;
  amountBefore: number;
  amountAfter: number;
}

export interface GponRateResolutionResult {
  success: boolean;
  errorMessage?: string;
  revenueAmount?: number;
  revenueSource?: string;
  revenueRateId?: string;
  payoutAmount?: number;
  payoutSource?: string;
  payoutRateId?: string;
  grossMargin?: number;
  marginPercentage?: number;
  currency: string;
  resolvedAt: string;
  resolutionSteps: string[];
  payoutPath?: string;
  baseAmountBeforeModifiers?: number;
  warnings?: string[];
  /** Match level: Custom | ExactCategory | ServiceProfile | BroadRateGroup | Legacy */
  resolutionMatchLevel?: string;
  resolutionContext?: ResolutionContextDto | null;
  matchedRuleDetails?: MatchedRuleDetailsDto | null;
  modifierTrace?: ModifierTraceItemDto[];
}

