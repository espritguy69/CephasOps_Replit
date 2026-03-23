/**
 * Background jobs API - health, summary, and job run observability
 */
import apiClient from './client';

export interface BackgroundJobHealthDto {
  status: string;
  timestamp: string;
  application: { isRunning: boolean; uptime: string };
  backgroundWorker: { isRunning: boolean; recentJobsCount: number; recentJobs: RecentJobDto[] };
  emailPolling?: {
    activeAccountsCount: number;
    accounts: Array<{
      id: string;
      name: string;
      email: string;
      lastPolledAt: string | null;
      minutesSinceLastPoll: number | null;
      status: string;
    }>;
  };
}

export interface RecentJobDto {
  id: string;
  jobType: string;
  state: number;
  startedAt: string | null;
  completedAt: string | null;
  updatedAt: string;
  lastError: string | null;
}

export interface BackgroundJobsSummaryDto {
  totalJobs: number;
  runningJobs: number;
  queuedJobs: number;
  failedJobs: number;
  summary: Array<{ state: string; count: number }>;
}

/** Job run observability record */
export interface JobRunDto {
  id: string;
  companyId: string | null;
  jobName: string;
  jobType: string;
  triggerSource: string;
  correlationId: string | null;
  queueOrChannel: string | null;
  payloadSummary: string | null;
  status: string;
  startedAtUtc: string;
  completedAtUtc: string | null;
  durationMs: number | null;
  retryCount: number;
  workerNode: string | null;
  errorCode: string | null;
  errorMessage: string | null;
  errorDetails: string | null;
  initiatedByUserId: string | null;
  parentJobRunId: string | null;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  backgroundJobId: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  canRetry: boolean;
  effectiveStuckThresholdSeconds?: number | null;
}

export interface TopFailingCompanyDto {
  companyId: string;
  companyName?: string | null;
  failedCount: number;
}

export interface TopFailingJobTypeDto {
  jobType: string;
  failedCount: number;
}

export interface JobRunDashboardDto {
  totalRunsLast24h: number;
  succeededLast24h: number;
  failedLast24h: number;
  successRateLast24h: number;
  runningNow: number;
  stuckCount: number;
  queuedNow: number;
  p95DurationMsLast24h?: number | null;
  jobsPerHourLast24h?: number;
  retryRateLast24h?: number;
  byJobType: Array<{
    jobType: string;
    total: number;
    succeeded: number;
    failed: number;
    avgDurationMs: number | null;
  }>;
  topFailingCompanies?: TopFailingCompanyDto[];
  topFailingJobTypes?: TopFailingJobTypeDto[];
  recentFailures: JobRunDto[];
}

export interface ListJobRunsParams {
  fromUtc?: string;
  toUtc?: string;
  companyId?: string;
  jobType?: string;
  status?: string;
  triggerSource?: string;
  correlationId?: string;
  page?: number;
  pageSize?: number;
}

export async function getBackgroundJobsHealth(): Promise<BackgroundJobHealthDto> {
  const response = await apiClient.get<BackgroundJobHealthDto>('/background-jobs/health');
  return response as BackgroundJobHealthDto;
}

export async function getBackgroundJobsSummary(): Promise<BackgroundJobsSummaryDto> {
  const response = await apiClient.get<BackgroundJobsSummaryDto>('/background-jobs/summary');
  return response as BackgroundJobsSummaryDto;
}

export async function getJobRunsDashboard(): Promise<JobRunDashboardDto> {
  const response = await apiClient.get<JobRunDashboardDto>('/background-jobs/job-runs/dashboard');
  return response as JobRunDashboardDto;
}

export async function listJobRuns(params: ListJobRunsParams = {}): Promise<{ items: JobRunDto[]; total: number; page: number; pageSize: number }> {
  const response = await apiClient.get<{ items: JobRunDto[]; total: number; page: number; pageSize: number }>(
    '/background-jobs/job-runs',
    { params: params as Record<string, unknown> }
  );
  return response as { items: JobRunDto[]; total: number; page: number; pageSize: number };
}

export async function listFailedJobRuns(limit = 100): Promise<{ items: JobRunDto[] }> {
  const response = await apiClient.get<{ items: JobRunDto[] }>('/background-jobs/job-runs/failed', {
    params: { limit }
  });
  return response as { items: JobRunDto[] };
}

export async function listRunningJobRuns(): Promise<{ items: JobRunDto[] }> {
  const response = await apiClient.get<{ items: JobRunDto[] }>('/background-jobs/job-runs/running');
  return response as { items: JobRunDto[] };
}

export async function getJobRun(id: string): Promise<JobRunDto> {
  const response = await apiClient.get<JobRunDto>(`/background-jobs/job-runs/${id}`);
  return response as JobRunDto;
}

export async function listStuckJobRuns(olderThanHours = 2): Promise<{ items: JobRunDto[] }> {
  const response = await apiClient.get<{ items: JobRunDto[] }>('/background-jobs/job-runs/stuck', {
    params: { olderThanHours }
  });
  return response as { items: JobRunDto[] };
}

export async function retryJobRun(id: string): Promise<{ message: string; backgroundJobId: string }> {
  const response = await apiClient.post<{ message: string; backgroundJobId: string }>(
    `/background-jobs/job-runs/${id}/retry`
  );
  return response as { message: string; backgroundJobId: string };
}
