export interface InstallerScoringWeights {
  availability: number;
  workload: number;
  distance: number;
  skillMatch: number;
  jobHistory: number;
}

export interface WindowScoringWeights {
  gapFit: number;
  travelClustering: number;
  timePreference: number;
  utilizationBalance: number;
  idleTimeReduction: number;
}

export interface WorkloadThresholds {
  freeMaxPct: number;
  freeMaxJobs: number;
  mediumMaxPct: number;
  mediumMaxJobs: number;
}

export interface ScoringThresholds {
  blockedScore: number;
  skillMismatchPenalty: number;
  noDataFactor: number;
  partialMatchFactor: number;
  workloadFreeFactor: number;
  workloadMediumFactor: number;
  workloadOverloadedFactor: number;
  distanceSameAreaMinSimilarity: number;
  distanceNearbyMinSimilarity: number;
  addressWordMinLength: number;
  travelClusteringMinSimilarity: number;
  timePreferenceCloseMinutes: number;
  timePreferenceNearMinutes: number;
  timePreferenceFarMinutes: number;
  utilizationBalancedMax: number;
  utilizationModerateMax: number;
  utilizationHeavyMax: number;
  gapFitCleanMinutes: number;
  gapFitNearMinutes: number;
  gapFitSmallMinutes: number;
  idleTimeLowMinutes: number;
  minSlotDurationMinutes: number;
  fallbackStartTime: string;
  fallbackEndTime: string;
  defaultSortTime: string;
  syntheticSlotStatus: string;
}

export interface SchedulerConfig {
  workingHours: {
    start: string;
    end: string;
  };
  slotIntervalMinutes: number;
  defaultJobDurationMinutes: number;
  bufferTimeMinutes: number;
  maxJobsPerInstallerPerDay: number;
  maxWorkingMinutesPerDay: number;
  installerScoringWeights: InstallerScoringWeights;
  windowScoringWeights: WindowScoringWeights;
  workloadThresholds: WorkloadThresholds;
  scoringThresholds: ScoringThresholds;
  enableSkillBlockOnMismatch: boolean;
  overrideRulesEnabled: boolean;
}

const SYSTEM_DEFAULT_CONFIG: SchedulerConfig = {
  workingHours: {
    start: '08:00:00',
    end: '18:00:00',
  },
  slotIntervalMinutes: 30,
  defaultJobDurationMinutes: 120,
  bufferTimeMinutes: 0,
  maxJobsPerInstallerPerDay: 5,
  maxWorkingMinutesPerDay: 480,
  installerScoringWeights: {
    availability: 40,
    workload: 20,
    distance: 15,
    skillMatch: 15,
    jobHistory: 10,
  },
  windowScoringWeights: {
    gapFit: 30,
    travelClustering: 20,
    timePreference: 20,
    utilizationBalance: 15,
    idleTimeReduction: 15,
  },
  workloadThresholds: {
    freeMaxPct: 50,
    freeMaxJobs: 3,
    mediumMaxPct: 85,
    mediumMaxJobs: 6,
  },
  scoringThresholds: {
    blockedScore: -100,
    skillMismatchPenalty: -50,
    noDataFactor: 0.5,
    partialMatchFactor: 0.6,
    workloadFreeFactor: 1.0,
    workloadMediumFactor: 0.6,
    workloadOverloadedFactor: 0.2,
    distanceSameAreaMinSimilarity: 0.5,
    distanceNearbyMinSimilarity: 0.2,
    addressWordMinLength: 3,
    travelClusteringMinSimilarity: 0.4,
    timePreferenceCloseMinutes: 30,
    timePreferenceNearMinutes: 60,
    timePreferenceFarMinutes: 120,
    utilizationBalancedMax: 0.6,
    utilizationModerateMax: 0.8,
    utilizationHeavyMax: 0.95,
    gapFitCleanMinutes: 0,
    gapFitNearMinutes: 30,
    gapFitSmallMinutes: 60,
    idleTimeLowMinutes: 60,
    minSlotDurationMinutes: 30,
    fallbackStartTime: '09:00',
    fallbackEndTime: '11:00',
    defaultSortTime: '23:59',
    syntheticSlotStatus: 'Draft',
  },
  enableSkillBlockOnMismatch: false,
  overrideRulesEnabled: true,
};

const _tenantConfigStore = new Map<string, Partial<SchedulerConfig>>();

let _activeTenantId: string | null = null;

export function setActiveTenant(tenantId: string): void {
  _activeTenantId = tenantId;
}

export function getActiveTenant(): string | null {
  return _activeTenantId;
}

export function setTenantConfigOverrides(
  tenantId: string,
  overrides: Partial<SchedulerConfig>
): void {
  _tenantConfigStore.set(tenantId, overrides);
}

export function clearTenantConfigOverrides(tenantId: string): void {
  _tenantConfigStore.delete(tenantId);
}

export function clearAllTenantConfigs(): void {
  _tenantConfigStore.clear();
  _activeTenantId = null;
}

function mergeConfig(overrides: Partial<SchedulerConfig>): SchedulerConfig {
  return {
    ...SYSTEM_DEFAULT_CONFIG,
    ...overrides,
    workingHours: {
      ...SYSTEM_DEFAULT_CONFIG.workingHours,
      ...(overrides.workingHours || {}),
    },
    installerScoringWeights: {
      ...SYSTEM_DEFAULT_CONFIG.installerScoringWeights,
      ...(overrides.installerScoringWeights || {}),
    },
    windowScoringWeights: {
      ...SYSTEM_DEFAULT_CONFIG.windowScoringWeights,
      ...(overrides.windowScoringWeights || {}),
    },
    workloadThresholds: {
      ...SYSTEM_DEFAULT_CONFIG.workloadThresholds,
      ...(overrides.workloadThresholds || {}),
    },
    scoringThresholds: {
      ...SYSTEM_DEFAULT_CONFIG.scoringThresholds,
      ...(overrides.scoringThresholds || {}),
    },
  };
}

export function getSchedulerConfig(tenantId?: string): SchedulerConfig {
  const resolvedTenant = tenantId || _activeTenantId;
  if (!resolvedTenant) return { ...SYSTEM_DEFAULT_CONFIG };

  const overrides = _tenantConfigStore.get(resolvedTenant);
  if (!overrides) return { ...SYSTEM_DEFAULT_CONFIG };

  return mergeConfig(overrides);
}

export function getSystemDefaultConfig(): SchedulerConfig {
  return { ...SYSTEM_DEFAULT_CONFIG };
}
