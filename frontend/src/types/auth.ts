/**
 * Auth Types - Shared type definitions for Authentication module
 */

export interface User {
  id: string;
  name: string;
  email: string;
  phone?: string;
  roles: string[];
  /** Permission names (module.action) from RBAC v2. Used for sidebar and guards when present. */
  permissions?: string[];
  departmentId?: string;
  departmentName?: string;
  /** When true, user must change password before using the app (e.g. after admin reset). */
  mustChangePassword?: boolean;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  refreshToken?: string;
  expiresAt?: string;
  user: User;
  /** Set when login is blocked because user must change password (403 response). */
  requiresPasswordChange?: boolean;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

