export type {
  JobContext,
  InstallerWorkload,
  ScoringResult,
  WindowScore,
  WindowStatus,
  BestWindowResult,
  NoSlotResult,
  AutoAssignment,
  AutoAssignResult,
  ConflictCheckResult,
  OverrideRecord,
} from './types';

export type {
  SchedulerConfig,
  InstallerScoringWeights,
  WindowScoringWeights,
  WorkloadThresholds,
  ScoringThresholds,
} from './config/schedulerConfig';

export {
  getSchedulerConfig,
  getSystemDefaultConfig,
  setTenantConfigOverrides,
  clearTenantConfigOverrides,
  clearAllTenantConfigs,
  setActiveTenant,
  getActiveTenant,
} from './config/schedulerConfig';

export {
  checkConflict,
  hasTimeOverlap,
  isWithinWorkingHours,
  hasExceededDailyLimit,
} from './services/conflictService';

export {
  getInstallerScore,
  rankInstallers,
  computeWorkloads,
} from './services/scoringService';

export {
  findBestWindow,
  getWindowScore,
  checkAnyWindowAvailable,
} from './services/timeSlotService';

export {
  autoAssignJobs,
  smartDistribute,
} from './services/assignmentService';

export {
  logOverride,
  getOverrideLog,
  clearOverrideLog,
  clearAllOverrideLogs,
  canOverride,
  validateOverride,
} from './policies/overridePolicy';

export {
  parseTimeToMinutes,
  minutesToTime,
  getSlotDuration,
} from './services/timeUtils';
