/**
 * Partner Groups Types - Shared type definitions for Partner Groups module
 */

export interface PartnerGroup {
  id: string;
  companyId?: string | null;
  name: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePartnerGroupRequest {
  companyId?: string | null;
  name: string;
}

export interface UpdatePartnerGroupRequest {
  name?: string;
}

