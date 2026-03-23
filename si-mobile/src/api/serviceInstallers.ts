/**
 * Service installers – resolve current SI profile from list.
 * Backend: GET /api/service-installers (filtered by current user).
 */
import { apiClient } from './client';
import type { ServiceInstaller } from '../types/api';

export async function getServiceInstallers(): Promise<ServiceInstaller[]> {
  const res = await apiClient.get<ServiceInstaller[] | { data: ServiceInstaller[] }>(
    '/service-installers'
  );
  if (Array.isArray(res)) return res;
  if (res && typeof res === 'object' && 'data' in res)
    return (res as { data: ServiceInstaller[] }).data;
  return [];
}
