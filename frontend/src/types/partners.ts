/**
 * Partners Types - Shared type definitions for Partners module
 */

export interface Partner {
  id: string;
  companyId?: string | null;
  name: string;
  /** Short code for derived labels (e.g. TIME, CELCOM). Optional. */
  code?: string | null;
  partnerType: string; // Telco, Customer, Vendor, Landlord
  groupId?: string | null;
  departmentId?: string | null;
  billingAddress?: string;
  contactName?: string;
  contactEmail?: string;
  contactPhone?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreatePartnerRequest {
  companyId?: string | null;
  name: string;
  code?: string | null;
  partnerType: string;
  groupId?: string | null;
  departmentId?: string | null;
  billingAddress?: string;
  contactName?: string;
  contactEmail?: string;
  contactPhone?: string;
  isActive?: boolean;
}

export interface UpdatePartnerRequest {
  name?: string;
  code?: string | null;
  partnerType?: string;
  groupId?: string | null;
  departmentId?: string | null;
  billingAddress?: string;
  contactName?: string;
  contactEmail?: string;
  contactPhone?: string;
  isActive?: boolean;
}

export interface PartnerFilters {
  isActive?: boolean;
}

