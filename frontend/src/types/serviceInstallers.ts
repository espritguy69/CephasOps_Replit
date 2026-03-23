/**
 * Service Installers Types - Shared type definitions for Service Installers module
 * Matches backend DTOs in CephasOps.Application.ServiceInstallers.DTOs
 */

export interface ServiceInstaller {
  id: string;
  companyId?: string;
  departmentId?: string;
  departmentName?: string;
  name: string;
  employeeId?: string; // Employee ID (replaces deprecated 'code' field)
  phone?: string;
  email?: string;
  siLevel: 'Junior' | 'Senior'; // Only Junior or Senior (Subcon is a Type, not Level)
  installerType: 'InHouse' | 'Subcontractor'; // Installer type
  isSubcontractor: boolean; // Deprecated - use installerType instead
  isActive: boolean;
  userId?: string; // Link to User if SI has login access
  availabilityStatus?: string; // Available, Busy, OnLeave, etc.
  
  // Conditional Fields - In-House Only
  hireDate?: string;
  employmentStatus?: string; // Permanent, Probation, etc.
  
  // Conditional Fields - Subcontractor Only
  contractorId?: string;
  contractorCompany?: string;
  contractStartDate?: string;
  contractEndDate?: string;
  
  icNumber?: string; // Malaysian IC/Identity Card Number
  bankName?: string;
  bankAccountNumber?: string;
  address?: string;
  emergencyContact?: string; // Emergency contact name/phone
  createdAt?: string;
  updatedAt?: string;
  contacts?: ServiceInstallerContact[];
  skills?: ServiceInstallerSkill[]; // Skills assigned to this installer
}

export interface ServiceInstallerContact {
  id: string;
  serviceInstallerId: string; // Matches backend field name
  siId?: string; // Alias for serviceInstallerId (for backward compatibility)
  name: string;
  phone?: string;
  email?: string;
  contactType: string; // Backup, Emergency, etc.
  role?: string; // Deprecated - use contactType instead
  isPrimary: boolean;
}

export interface CreateServiceInstallerRequest {
  departmentId?: string;
  name: string;
  employeeId?: string; // Employee ID (replaces deprecated 'code' field)
  phone?: string;
  email?: string;
  siLevel?: 'Junior' | 'Senior'; // Default: "Junior"
  installerType?: 'InHouse' | 'Subcontractor'; // Default: "InHouse"
  isSubcontractor?: boolean; // Deprecated - use installerType instead
  isActive?: boolean; // Default: true
  userId?: string;
  availabilityStatus?: string;
  
  // Conditional Fields - In-House Only
  hireDate?: string;
  employmentStatus?: string;
  
  // Conditional Fields - Subcontractor Only
  contractorId?: string;
  contractorCompany?: string;
  contractStartDate?: string;
  contractEndDate?: string;
  
  icNumber?: string;
  bankName?: string;
  bankAccountNumber?: string;
  address?: string;
  emergencyContact?: string;
  
  // Skills (array of skill IDs for creation)
  skillIds?: string[];
}

export interface UpdateServiceInstallerRequest {
  departmentId?: string;
  name?: string;
  employeeId?: string;
  phone?: string;
  email?: string;
  siLevel?: 'Junior' | 'Senior';
  installerType?: 'InHouse' | 'Subcontractor';
  isSubcontractor?: boolean; // Deprecated - use installerType instead
  isActive?: boolean;
  userId?: string;
  availabilityStatus?: string;
  
  // Conditional Fields - In-House Only
  hireDate?: string;
  employmentStatus?: string;
  
  // Conditional Fields - Subcontractor Only
  contractorId?: string;
  contractorCompany?: string;
  contractStartDate?: string;
  contractEndDate?: string;
  
  icNumber?: string;
  bankName?: string;
  bankAccountNumber?: string;
  address?: string;
  emergencyContact?: string;
  
  // Skills (array of skill IDs for update)
  skillIds?: string[];
}

export interface CreateServiceInstallerContactRequest {
  name: string;
  phone?: string;
  email?: string;
  contactType?: string; // Default: "Backup"
  role?: string; // Deprecated - use contactType instead
  isPrimary?: boolean; // Default: false
}

export interface UpdateServiceInstallerContactRequest {
  name?: string;
  phone?: string;
  email?: string;
  contactType?: string;
  role?: string; // Deprecated - use contactType instead
  isPrimary?: boolean;
}

export interface ServiceInstallerFilters {
  departmentId?: string;
  isActive?: boolean;
  installerType?: 'InHouse' | 'Subcontractor';
  siLevel?: 'Junior' | 'Senior';
  skillIds?: string[]; // Array of skill IDs - installers must have ALL specified skills
}

export interface ImportResult {
  success: boolean;
  imported: number;
  failed: number;
  errors?: string[];
}

// SI Level constants (Subcon is a Type, not a Level)
export const SI_LEVELS = ['Junior', 'Senior'] as const;
export type SiLevel = typeof SI_LEVELS[number];

// Contact Type constants
export const CONTACT_TYPES = ['Backup', 'Emergency', 'Other'] as const;
export type ContactType = typeof CONTACT_TYPES[number];

// Skill types
export interface Skill {
  id: string;
  name: string;
  code: string;
  category: string;
  description?: string;
  isActive: boolean;
  displayOrder: number;
  departmentId?: string;
  departmentName?: string;
}

export interface ServiceInstallerSkill {
  id: string;
  serviceInstallerId: string;
  skillId: string;
  skill?: Skill; // Populated skill details
  acquiredAt?: string;
  verifiedAt?: string;
  verifiedByUserId?: string;
  notes?: string;
  isActive: boolean;
}

// Skill categories
export const SKILL_CATEGORIES = [
  'FiberSkills',
  'NetworkEquipment',
  'InstallationMethods',
  'SafetyCompliance',
  'CustomerService'
] as const;
export type SkillCategory = typeof SKILL_CATEGORIES[number];

