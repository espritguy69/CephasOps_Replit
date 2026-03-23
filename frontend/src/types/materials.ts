/**
 * Material Types
 */

export interface Material {
  id: string;
  code: string;
  description: string;
  categoryId?: string;
  categoryName?: string;
  unit: string;
  unitPrice: number;
  isSerialised: boolean;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface MaterialCategory {
  id: string;
  code: string;
  name: string;
  description?: string;
  parentCategoryId?: string;
  parentCategoryName?: string;
  materialCount: number;
  isSerialised: boolean;
  isActive: boolean;
}

