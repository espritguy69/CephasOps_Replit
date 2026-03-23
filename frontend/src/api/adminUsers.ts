/**
 * Admin User Management API
 * Requires SuperAdmin or Admin role. Base path: /api/admin/users
 */
import apiClient from './client';

export interface AdminUserListItem {
  id: string;
  name: string;
  email: string;
  phone?: string | null;
  isActive: boolean;
  createdAt: string;
  lastLoginAtUtc?: string | null;
  mustChangePassword?: boolean;
  roles: string[];
  departments: AdminUserDepartment[];
}

export interface AdminUserDepartment {
  departmentId: string;
  departmentName: string;
  role?: string | null;
}

export interface AdminUserListResult {
  items: AdminUserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminUserDetail extends AdminUserListItem {}

export interface CreateAdminUserRequest {
  name: string;
  email: string;
  phone?: string | null;
  password: string;
  roleNames: string[];
  departmentMemberships?: AdminUserDepartmentMembership[] | null;
}

export interface UpdateAdminUserRequest {
  name: string;
  email: string;
  phone?: string | null;
  roleNames: string[];
  departmentMemberships?: AdminUserDepartmentMembership[] | null;
}

export interface AdminUserDepartmentMembership {
  departmentId: string;
  role: string;
  isDefault: boolean;
}

export interface CreateAdminUserResult {
  id: string;
}

const BASE = '/admin/users';

export async function getAdminUserList(params: {
  page?: number;
  pageSize?: number;
  search?: string | null;
  role?: string | null;
  isActive?: boolean | null;
}): Promise<AdminUserListResult> {
  const q: Record<string, string | number | boolean> = {};
  if (params.page != null) q.page = params.page;
  if (params.pageSize != null) q.pageSize = params.pageSize;
  if (params.search != null && params.search.trim() !== '') q.search = params.search.trim();
  if (params.role != null && params.role.trim() !== '') q.role = params.role.trim();
  if (params.isActive != null) q.isActive = params.isActive;
  const data = await apiClient.get<AdminUserListResult>(BASE, { params: q });
  return data;
}

export async function getAdminUserRoles(): Promise<string[]> {
  const data = await apiClient.get<string[]>(`${BASE}/roles`);
  return Array.isArray(data) ? data : [];
}

export async function getAdminUserById(id: string): Promise<AdminUserDetail | null> {
  const data = await apiClient.get<AdminUserDetail>(`${BASE}/${id}`);
  return data ?? null;
}

export async function createAdminUser(request: CreateAdminUserRequest): Promise<CreateAdminUserResult> {
  const data = await apiClient.post<CreateAdminUserResult>(BASE, request);
  return data as CreateAdminUserResult;
}

export async function updateAdminUser(id: string, request: UpdateAdminUserRequest): Promise<void> {
  await apiClient.put(`${BASE}/${id}`, request);
}

export async function setAdminUserActive(id: string, isActive: boolean): Promise<void> {
  await apiClient.patch(`${BASE}/${id}/active`, { isActive });
}

export async function setAdminUserRoles(id: string, roleNames: string[]): Promise<void> {
  await apiClient.put(`${BASE}/${id}/roles`, { roleNames });
}

export async function resetAdminUserPassword(
  id: string,
  newPassword: string,
  forceMustChangePassword: boolean = true
): Promise<void> {
  await apiClient.post(`${BASE}/${id}/reset-password`, {
    newPassword,
    forceMustChangePassword
  });
}
