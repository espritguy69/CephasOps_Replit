/**
 * RBAC Types - Shared type definitions for RBAC module
 */

export interface Role {
  id: string;
  name: string;
  scope?: string;
  description?: string;
  permissions?: Permission[];
  createdAt?: string;
  updatedAt?: string;
}

export interface Permission {
  id: string;
  name: string;
  code: string;
  category?: string;
  description?: string;
}

export interface User {
  id: string;
  name: string;
  email: string;
  phone?: string;
  roles?: Role[];
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface UserAccess {
  userId: string;
  roles: Role[];
  permissions: Permission[];
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
}

export interface UpdateRoleRequest {
  name?: string;
  description?: string;
}

export interface AssignPermissionsRequest {
  permissionIds: string[];
}

export interface CreateUserRequest {
  name: string;
  email: string;
  phone?: string;
  password: string;
  isActive?: boolean;
}

export interface UpdateUserRequest {
  name?: string;
  email?: string;
  phone?: string;
  isActive?: boolean;
}

export interface AssignRoleRequest {
  roleId: string;
}

export interface UserFilters {
  roleId?: string;
  search?: string;
}

