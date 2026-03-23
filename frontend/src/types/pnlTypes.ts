/**
 * P&L Types Types - Shared type definitions for P&L Types module
 */

export enum PnlTypeCategory {
  Income = 'Income',
  Expense = 'Expense'
}

export interface PnlType {
  id: string;
  name: string;
  code?: string;
  description?: string;
  category: PnlTypeCategory;
  parentId?: string;
  parentName?: string;
  level: number;
  path?: string;
  isTransactional: boolean;
  isActive: boolean;
  children?: PnlType[];
  createdAt?: string;
  updatedAt?: string;
}

export interface CreatePnlTypeRequest {
  name: string;
  code?: string;
  description?: string;
  category: PnlTypeCategory;
  parentId?: string;
  isTransactional?: boolean;
  isActive?: boolean;
}

export interface UpdatePnlTypeRequest {
  name?: string;
  code?: string;
  description?: string;
  category?: PnlTypeCategory;
  parentId?: string;
  isTransactional?: boolean;
  isActive?: boolean;
}

export interface PnlTypeFilters {
  category?: PnlTypeCategory;
  isTransactional?: boolean;
  isActive?: boolean;
}

