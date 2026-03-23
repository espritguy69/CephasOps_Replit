/**
 * Asset Types Types - Shared type definitions for Asset Types module
 */

export enum DepreciationMethod {
  StraightLine = 'StraightLine',
  DecliningBalance = 'DecliningBalance',
  DoubleDecliningBalance = 'DoubleDecliningBalance',
  SumOfYearsDigits = 'SumOfYearsDigits',
  UnitsOfProduction = 'UnitsOfProduction',
  None = 'None'
}

export interface AssetType {
  id: string;
  name: string;
  code?: string;
  description?: string;
  category?: string;
  depreciationMethod: DepreciationMethod;
  usefulLifeYears?: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateAssetTypeRequest {
  name: string;
  code?: string;
  description?: string;
  category?: string;
  depreciationMethod: DepreciationMethod;
  usefulLifeYears?: number;
  isActive?: boolean;
}

export interface UpdateAssetTypeRequest {
  name?: string;
  code?: string;
  description?: string;
  category?: string;
  depreciationMethod?: DepreciationMethod;
  usefulLifeYears?: number;
  isActive?: boolean;
}

export interface AssetTypeFilters {
  category?: string;
  isActive?: boolean;
}

export const DepreciationMethodLabels: Record<DepreciationMethod, string> = {
  [DepreciationMethod.StraightLine]: 'Straight Line',
  [DepreciationMethod.DecliningBalance]: 'Declining Balance',
  [DepreciationMethod.DoubleDecliningBalance]: 'Double Declining Balance',
  [DepreciationMethod.SumOfYearsDigits]: 'Sum of Years Digits',
  [DepreciationMethod.UnitsOfProduction]: 'Units of Production',
  [DepreciationMethod.None]: 'No Depreciation'
};

