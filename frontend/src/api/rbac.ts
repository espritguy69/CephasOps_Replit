import apiClient from './client';
import type {
  Role,
  Permission,
  User,
  UserAccess,
  CreateRoleRequest,
  UpdateRoleRequest,
  AssignPermissionsRequest,
  CreateUserRequest,
  UpdateUserRequest,
  AssignRoleRequest,
  UserFilters
} from '../types/rbac';

/**
 * RBAC API
 * Handles roles, permissions, and user access management
 */

/**
 * Get roles list
 * @returns Array of roles
 */
export const getRoles = async (): Promise<Role[]> => {
  const response = await apiClient.get<Role[]>(`/admin/roles`);
  return response;
};

/**
 * Get role by ID (from list; backend has no single-role endpoint)
 */
export const getRole = async (roleId: string): Promise<Role> => {
  const roles = await getRoles();
  const role = roles.find((r) => r.id === roleId);
  if (!role) throw new Error('Role not found');
  return role;
};

/**
 * Get permission names assigned to a role
 */
export const getRolePermissions = async (roleId: string): Promise<string[]> => {
  const response = await apiClient.get<string[]>(`/admin/roles/${roleId}/permissions`);
  return Array.isArray(response) ? response : [];
};

/**
 * Set permissions for a role (replaces existing). Uses permission names.
 */
export const setRolePermissions = async (roleId: string, permissionNames: string[]): Promise<void> => {
  await apiClient.put(`/admin/roles/${roleId}/permissions`, { permissionNames });
};

/**
 * Create role
 * @param roleData - Role creation data
 * @returns Created role
 */
export const createRole = async (roleData: CreateRoleRequest): Promise<Role> => {
  const response = await apiClient.post<Role>(`/admin/roles`, roleData);
  return response;
};

/**
 * Update role
 * @param roleId - Role ID
 * @param roleData - Role update data
 * @returns Updated role
 */
export const updateRole = async (roleId: string, roleData: UpdateRoleRequest): Promise<Role> => {
  const response = await apiClient.put<Role>(`/admin/roles/${roleId}`, roleData);
  return response;
};

/**
 * Delete role
 * @param roleId - Role ID
 * @returns Promise that resolves when role is deleted
 */
export const deleteRole = async (roleId: string): Promise<void> => {
  await apiClient.delete(`/admin/roles/${roleId}`);
};

/**
 * Get permissions list
 * @returns Array of permissions
 */
export const getPermissions = async (): Promise<Permission[]> => {
  const response = await apiClient.get<Permission[]>(`/admin/permissions`);
  return response;
};

/**
 * Assign permissions to role (by permission names). Replaces existing permissions.
 */
export const assignPermissionsToRole = async (roleId: string, permissionNames: string[]): Promise<void> => {
  await setRolePermissions(roleId, permissionNames);
};

/**
 * Get users list
 * @param filters - Optional filters (roleId, search)
 * @returns Array of users
 */
export const getUsers = async (filters: UserFilters = {}): Promise<User[]> => {
  const response = await apiClient.get<User[]>(`/admin/users`, { params: filters });
  return response;
};

/**
 * Get user by ID
 * @param userId - User ID
 * @returns User details with roles
 */
export const getUser = async (userId: string): Promise<User> => {
  const response = await apiClient.get<User>(`/admin/users/${userId}`);
  return response;
};

/**
 * Create user
 * @param userData - User creation data
 * @returns Created user
 */
export const createUser = async (userData: CreateUserRequest): Promise<User> => {
  const response = await apiClient.post<User>(`/admin/users`, userData);
  return response;
};

/**
 * Update user
 * @param userId - User ID
 * @param userData - User update data
 * @returns Updated user
 */
export const updateUser = async (userId: string, userData: UpdateUserRequest): Promise<User> => {
  const response = await apiClient.put<User>(`/admin/users/${userId}`, userData);
  return response;
};

/**
 * Assign role to user
 * @param userId - User ID
 * @param roleId - Role ID
 * @returns User role assignment
 */
export const assignRoleToUser = async (userId: string, roleId: string): Promise<User> => {
  const request: AssignRoleRequest = { roleId };
  const response = await apiClient.post<User>(`/admin/users/${userId}/roles`, request);
  return response;
};

/**
 * Remove role from user
 * @param userId - User ID
 * @param roleId - Role ID
 * @returns Promise that resolves when role is removed
 */
export const removeRoleFromUser = async (userId: string, roleId: string): Promise<void> => {
  await apiClient.delete(`/admin/users/${userId}/roles/${roleId}`);
};

/**
 * Get user access information
 * @param userId - User ID
 * @returns User access details (roles, permissions)
 */
export const getUserAccess = async (userId: string): Promise<UserAccess> => {
  const response = await apiClient.get<UserAccess>(`/admin/users/${userId}/access`);
  return response;
};

