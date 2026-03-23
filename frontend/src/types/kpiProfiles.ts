/**
 * KPI Profiles Types - Shared type definitions for KPI Profiles module
 */

export interface KpiProfile {
  id: string;
  name: string;
  description?: string;
  orderType?: string; // Backend uses OrderType (string)
  orderTypeId?: string; // Optional - for frontend use
  orderTypeName?: string; // Optional - for frontend use
  partnerId?: string;
  partnerName?: string;
  buildingTypeId?: string;
  buildingTypeName?: string;
  maxJobDurationMinutes?: number; // Backend field
  docketKpiMinutes?: number; // Backend field
  maxReschedulesAllowed?: number; // Backend field
  kpis?: KpiProfileItem[]; // Optional - may not be in DTO
  isActive?: boolean; // Computed from EffectiveFrom/EffectiveTo
  isDefault: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface KpiProfileItem {
  kpiId: string;
  kpiName?: string;
  weight?: number;
  target?: number;
}

export interface CreateKpiProfileRequest {
  name: string;
  description?: string;
  orderType?: string; // Backend uses OrderType (string)
  orderTypeId?: string; // Optional - for frontend use
  partnerId?: string;
  buildingTypeId?: string;
  maxJobDurationMinutes?: number; // Backend field
  docketKpiMinutes?: number; // Backend field
  maxReschedulesAllowed?: number; // Backend field
  kpis?: Omit<KpiProfileItem, 'kpiName'>[]; // Optional - may not be in DTO
  isActive?: boolean; // Computed from EffectiveFrom/EffectiveTo
  isDefault?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export interface UpdateKpiProfileRequest {
  name?: string;
  description?: string;
  orderType?: string; // Backend uses OrderType (string)
  orderTypeId?: string; // Optional - for frontend use
  partnerId?: string;
  buildingTypeId?: string;
  maxJobDurationMinutes?: number; // Backend field
  docketKpiMinutes?: number; // Backend field
  maxReschedulesAllowed?: number; // Backend field
  kpis?: Omit<KpiProfileItem, 'kpiName'>[]; // Optional - may not be in DTO
  isActive?: boolean; // Computed from EffectiveFrom/EffectiveTo
  isDefault?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export interface KpiProfileFilters {
  orderType?: string;
  partnerId?: string;
  buildingTypeId?: string;
  isActive?: boolean;
}

export interface EffectiveKpiProfileParams {
  orderType: string;
  partnerId?: string;
  buildingTypeId?: string;
  jobDate?: string;
}

export interface KpiEvaluationResult {
  orderId: string;
  kpiProfileId: string;
  kpiProfileName?: string;
  scores: Record<string, number>;
  totalScore: number;
  passed: boolean;
  evaluatedAt: string;
}

