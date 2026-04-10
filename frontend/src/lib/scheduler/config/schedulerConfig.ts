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
  enableSkillBlockOnMismatch: false,
  overrideRulesEnabled: true,
};

let _tenantOverrides: Partial<SchedulerConfig> | null = null;

export function setTenantConfigOverrides(overrides: Partial<SchedulerConfig>): void {
  _tenantOverrides = overrides;
}

export function clearTenantConfigOverrides(): void {
  _tenantOverrides = null;
}

export function getSchedulerConfig(): SchedulerConfig {
  if (!_tenantOverrides) return SYSTEM_DEFAULT_CONFIG;

  return {
    ...SYSTEM_DEFAULT_CONFIG,
    ..._tenantOverrides,
    workingHours: {
      ...SYSTEM_DEFAULT_CONFIG.workingHours,
      ...(_tenantOverrides.workingHours || {}),
    },
    installerScoringWeights: {
      ...SYSTEM_DEFAULT_CONFIG.installerScoringWeights,
      ...(_tenantOverrides.installerScoringWeights || {}),
    },
    windowScoringWeights: {
      ...SYSTEM_DEFAULT_CONFIG.windowScoringWeights,
      ...(_tenantOverrides.windowScoringWeights || {}),
    },
    workloadThresholds: {
      ...SYSTEM_DEFAULT_CONFIG.workloadThresholds,
      ...(_tenantOverrides.workloadThresholds || {}),
    },
  };
}

export function getSystemDefaultConfig(): SchedulerConfig {
  return { ...SYSTEM_DEFAULT_CONFIG };
}
