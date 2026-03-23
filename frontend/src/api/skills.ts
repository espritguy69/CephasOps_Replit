import apiClient from './client';
import type { Skill, ServiceInstallerSkill } from '../types/serviceInstallers';

/**
 * Skills API
 * Handles skill management and queries
 */

/**
 * Get all skills, optionally filtered by category and department
 * @param departmentId - Optional department ID filter
 * @param category - Optional category filter
 * @param isActive - Optional active status filter
 * @returns Array of skills
 */
export const getSkills = async (departmentId?: string, category?: string, isActive?: boolean): Promise<Skill[]> => {
  const params: Record<string, string> = {};
  if (departmentId) params.departmentId = departmentId;
  if (category) params.category = category;
  if (isActive !== undefined) params.isActive = isActive.toString();
  
  const response = await apiClient.get<Skill[] | { data: Skill[] }>('/skills', { params });
  return Array.isArray(response) ? response : (response as { data: Skill[] }).data || [];
};

/**
 * Get skills grouped by category
 * @param departmentId - Optional department ID filter
 * @param isActive - Optional active status filter
 * @returns Dictionary of skills grouped by category
 */
export const getSkillsByCategory = async (departmentId?: string, isActive?: boolean): Promise<Record<string, Skill[]>> => {
  const params: Record<string, string> = {};
  if (departmentId) params.departmentId = departmentId;
  if (isActive !== undefined) params.isActive = isActive.toString();
  
  const response = await apiClient.get<Record<string, Skill[]>>('/skills/by-category', { params });
  return response;
};

/**
 * Get all skill categories
 * @returns Array of category names
 */
export const getSkillCategories = async (): Promise<string[]> => {
  const response = await apiClient.get<string[] | { data: string[] }>('/skills/categories');
  return Array.isArray(response) ? response : (response as { data: string[] }).data || [];
};

/**
 * Get skill by ID
 * @param skillId - Skill ID
 * @returns Skill details
 */
export const getSkill = async (skillId: string): Promise<Skill> => {
  const response = await apiClient.get<Skill>(`/skills/${skillId}`);
  return response;
};

/**
 * Get skills for a service installer
 * @param serviceInstallerId - Service Installer ID
 * @returns Array of service installer skills
 */
export const getInstallerSkills = async (serviceInstallerId: string): Promise<ServiceInstallerSkill[]> => {
  const response = await apiClient.get<ServiceInstallerSkill[] | { data: ServiceInstallerSkill[] }>(
    `/service-installers/${serviceInstallerId}/skills`
  );
  return Array.isArray(response) ? response : (response as { data: ServiceInstallerSkill[] }).data || [];
};

/**
 * Assign skills to a service installer
 * @param serviceInstallerId - Service Installer ID
 * @param skillIds - Array of skill IDs to assign
 * @returns Array of assigned skills
 */
export const assignSkills = async (
  serviceInstallerId: string,
  skillIds: string[]
): Promise<ServiceInstallerSkill[]> => {
  const response = await apiClient.post<ServiceInstallerSkill[]>(
    `/service-installers/${serviceInstallerId}/skills`,
    skillIds
  );
  return response;
};

/**
 * Remove a skill from a service installer
 * @param serviceInstallerId - Service Installer ID
 * @param skillId - Skill ID to remove
 */
export const removeSkill = async (serviceInstallerId: string, skillId: string): Promise<void> => {
  await apiClient.delete(`/service-installers/${serviceInstallerId}/skills/${skillId}`);
};

/**
 * Create a new skill
 * @param data - Skill data
 * @returns Created skill
 */
export const createSkill = async (data: CreateSkillRequest): Promise<Skill> => {
  const response = await apiClient.post<Skill>('/skills', data);
  return response;
};

/**
 * Update an existing skill
 * @param skillId - Skill ID
 * @param data - Updated skill data
 * @returns Updated skill
 */
export const updateSkill = async (skillId: string, data: UpdateSkillRequest): Promise<Skill> => {
  const response = await apiClient.put<Skill>(`/skills/${skillId}`, data);
  return response;
};

/**
 * Delete a skill
 * @param skillId - Skill ID
 */
export const deleteSkill = async (skillId: string): Promise<void> => {
  await apiClient.delete(`/skills/${skillId}`);
};

export interface CreateSkillRequest {
  name: string;
  code: string;
  category: string;
  description?: string;
  isActive?: boolean;
  displayOrder?: number;
  departmentId?: string;
}

export interface UpdateSkillRequest {
  name?: string;
  code?: string;
  category?: string;
  description?: string;
  isActive?: boolean;
  displayOrder?: number;
  departmentId?: string;
}

