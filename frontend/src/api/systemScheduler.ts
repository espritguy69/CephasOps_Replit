/**
 * System scheduler API - job polling coordinator diagnostics
 */
import apiClient from './client';

export interface ClaimAttemptDto {
  jobId: string;
  success: boolean;
}

export interface SchedulerDiagnosticsDto {
  pollIntervalSeconds: number;
  maxJobsPerPoll: number;
  workerId: string | null;
  lastPollUtc: string | null;
  totalDiscovered: number;
  totalClaimAttempts: number;
  totalClaimSuccess: number;
  totalClaimFailure: number;
  recentDiscovered: string[];
  recentClaimAttempts: ClaimAttemptDto[];
}

export async function getSchedulerDiagnostics(): Promise<SchedulerDiagnosticsDto> {
  const res = await apiClient.get<SchedulerDiagnosticsDto | { data: SchedulerDiagnosticsDto }>('/system/scheduler');
  if (res && typeof res === 'object' && 'pollIntervalSeconds' in res) return res as SchedulerDiagnosticsDto;
  return (res as { data?: SchedulerDiagnosticsDto })?.data ?? (res as unknown as SchedulerDiagnosticsDto);
}
