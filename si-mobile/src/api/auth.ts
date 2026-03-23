/**
 * Auth API – login, me.
 * Backend: POST /api/auth/login, GET /api/auth/me.
 * Request uses PascalCase (Email, Password) for backend compatibility.
 */
import { apiClient } from './client';
import type { User } from '../types/api';

export interface LoginRequest {
  Email: string;
  Password: string;
}

export interface LoginResponse {
  accessToken?: string;
  AccessToken?: string;
  refreshToken?: string;
  RefreshToken?: string;
  user?: User;
  User?: User;
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  const raw = await apiClient.post<LoginResponse>(
    '/auth/login',
    { Email: email, Password: password },
    { skipAuth: true }
  );
  return raw;
}

export async function getCurrentUser(): Promise<User> {
  const raw = await apiClient.get<unknown>('/auth/me');
  const r = raw as { User?: User; user?: User } & User;
  const user = r.User ?? r.user ?? r;
  return {
    id: String((user as User).id ?? (user as { Id?: string }).Id ?? ''),
    name: (user as User).name ?? (user as { Name?: string }).Name ?? '',
    email: (user as User).email ?? (user as { Email?: string }).Email ?? '',
    phone: (user as User).phone ?? (user as { Phone?: string }).Phone,
    roles: (user as User).roles ?? (user as { Roles?: string[] }).Roles ?? [],
  };
}
