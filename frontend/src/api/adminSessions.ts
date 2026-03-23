/**
 * Admin session management API
 * GET/POST /api/admin/security/sessions
 */
import apiClient from './client';

export interface UserSession {
  sessionId: string;
  userId: string;
  userEmail?: string | null;
  createdAtUtc: string;
  expiresAtUtc: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  isRevoked: boolean;
}

export async function getSessions(params: {
  userId?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  activeOnly?: boolean;
}): Promise<UserSession[]> {
  const q: Record<string, string | boolean> = {};
  if (params.userId != null && params.userId !== '') q.userId = params.userId;
  if (params.dateFrom != null && params.dateFrom !== '') q.dateFrom = params.dateFrom;
  if (params.dateTo != null && params.dateTo !== '') q.dateTo = params.dateTo;
  if (params.activeOnly !== undefined) q.activeOnly = params.activeOnly;
  const response = await apiClient.get<{ data?: UserSession[] } | UserSession[]>('/admin/security/sessions', { params: q });
  const data = Array.isArray(response) ? response : (response as { data?: UserSession[] }).data;
  return Array.isArray(data) ? data : [];
}

export async function getSessionsForUser(userId: string): Promise<UserSession[]> {
  const response = await apiClient.get<{ data?: UserSession[] } | UserSession[]>(`/admin/security/sessions/user/${userId}`);
  const data = Array.isArray(response) ? response : (response as { data?: UserSession[] }).data;
  return Array.isArray(data) ? data : [];
}

export async function revokeSession(sessionId: string): Promise<void> {
  await apiClient.post(`/admin/security/sessions/${sessionId}/revoke`);
}

export async function revokeAllSessionsForUser(userId: string): Promise<{ revokedCount: number }> {
  const response = await apiClient.post<{ data?: { revokedCount: number } }>(`/admin/security/sessions/revoke-all/${userId}`);
  const data = response?.data ?? response;
  return { revokedCount: (data as { revokedCount?: number })?.revokedCount ?? 0 };
}
