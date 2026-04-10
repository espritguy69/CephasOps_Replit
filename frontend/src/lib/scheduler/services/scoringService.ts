import type {
  ServiceInstaller,
  CalendarSlot,
  JobContext,
  InstallerWorkload,
  ScoringResult,
} from '../types';
import { getSchedulerConfig } from '../config/schedulerConfig';
import { parseTimeToMinutes, getSlotDuration, getInstallerAllSlots } from './timeUtils';
import { hasTimeOverlap } from './conflictService';
import { checkAnyWindowAvailable } from './timeSlotService';

function scoreAvailability(
  installerSlots: CalendarSlot[],
  job: JobContext,
  date: string
): { score: number; blocked: boolean; blockReason?: string } {
  const cfg = getSchedulerConfig();
  const weights = cfg.installerScoringWeights;
  const th = cfg.scoringThresholds;
  const dateSlotsRaw = installerSlots.filter((s) => s.date === date);

  if (dateSlotsRaw.length >= cfg.maxJobsPerInstallerPerDay) {
    return {
      score: th.blockedScore,
      blocked: true,
      blockReason: `Max ${cfg.maxJobsPerInstallerPerDay} jobs per day reached`,
    };
  }

  if (job.windowFrom && job.windowTo) {
    const conflictSlot = hasTimeOverlap(dateSlotsRaw, job.windowFrom, job.windowTo);
    if (conflictSlot) {
      const hasAnyFreeSlot = checkAnyWindowAvailable(dateSlotsRaw);
      if (!hasAnyFreeSlot) {
        return {
          score: th.blockedScore,
          blocked: true,
          blockReason: 'No available time slots today',
        };
      }
      return {
        score: weights.availability * th.noDataFactor,
        blocked: false,
      };
    }
    return { score: weights.availability, blocked: false };
  }

  const hasAnyFreeSlot = checkAnyWindowAvailable(dateSlotsRaw);
  if (!hasAnyFreeSlot) {
    return {
      score: th.blockedScore,
      blocked: true,
      blockReason: 'No available time slots today',
    };
  }

  return { score: weights.availability, blocked: false };
}

function scoreWorkload(
  workload: InstallerWorkload
): { score: number; reason: string } {
  const cfg = getSchedulerConfig();
  const weight = cfg.installerScoringWeights.workload;
  const th = cfg.scoringThresholds;
  const utilizationFactor = 1 - (workload.utilizationPct / 100);
  const score = weight * Math.max(utilizationFactor, 0);

  if (workload.jobCount === 0) return { score, reason: 'No jobs today' };
  if (workload.level === 'free') return { score: score * th.workloadFreeFactor, reason: 'Low workload' };
  if (workload.level === 'medium') return { score: score * th.workloadMediumFactor, reason: 'Medium workload' };
  return { score: score * th.workloadOverloadedFactor, reason: 'Overloaded' };
}

function scoreDistance(
  installer: ServiceInstaller,
  job: JobContext
): { score: number; reason: string } {
  const cfg = getSchedulerConfig();
  const weight = cfg.installerScoringWeights.distance;
  const th = cfg.scoringThresholds;

  if (!installer.address || !job.address) {
    return { score: weight * th.noDataFactor, reason: 'Distance unknown' };
  }

  const installerAddr = (installer.address || '').toLowerCase();
  const jobAddr = (job.address || '').toLowerCase();
  const buildingName = (job.buildingName || '').toLowerCase();

  const installerWords = new Set(installerAddr.split(/[\s,]+/).filter(w => w.length > th.addressWordMinLength));
  const jobWords = new Set([...jobAddr.split(/[\s,]+/).filter(w => w.length > th.addressWordMinLength), ...buildingName.split(/[\s,]+/).filter(w => w.length > th.addressWordMinLength)]);

  let matchCount = 0;
  for (const w of installerWords) {
    if (jobWords.has(w)) matchCount++;
  }

  const similarity = jobWords.size > 0 ? matchCount / jobWords.size : 0;

  if (similarity >= th.distanceSameAreaMinSimilarity) return { score: weight, reason: 'Same area' };
  if (similarity >= th.distanceNearbyMinSimilarity) return { score: weight * 0.7, reason: 'Nearby area' };
  return { score: weight * 0.3, reason: 'Different area' };
}

function scoreSkillMatch(
  installer: ServiceInstaller,
  job: JobContext
): { score: number; reason: string; blocked: boolean } {
  const cfg = getSchedulerConfig();
  const weight = cfg.installerScoringWeights.skillMatch;
  const th = cfg.scoringThresholds;

  const installerSkills = (installer.skills || [])
    .filter((s) => s.isActive)
    .map((s) => ({
      name: (s.skill?.name || '').toLowerCase(),
      code: (s.skill?.code || '').toLowerCase(),
      category: (s.skill?.category || '').toLowerCase(),
    }));

  if (installerSkills.length === 0) {
    return { score: weight * th.noDataFactor, reason: 'Skills not specified', blocked: false };
  }

  const jobType = (job.orderType || job.serviceType || '').toLowerCase();

  if (!jobType) {
    return { score: weight * 0.7, reason: 'Job type unspecified', blocked: false };
  }

  const hasMatch = installerSkills.some(
    (s) =>
      s.name.includes(jobType) ||
      s.code.includes(jobType) ||
      jobType.includes(s.name) ||
      jobType.includes(s.code)
  );

  if (hasMatch) return { score: weight, reason: 'Skill match', blocked: false };

  const categoryMatch = installerSkills.some((s) => {
    if (jobType.includes('fiber') || jobType.includes('ftth') || jobType.includes('ftto'))
      return s.category === 'fiberskills';
    if (jobType.includes('network') || jobType.includes('router'))
      return s.category === 'networkequipment';
    return false;
  });

  if (categoryMatch) return { score: weight * th.partialMatchFactor, reason: 'Partial skill match', blocked: false };

  return { score: th.skillMismatchPenalty, reason: 'Skill mismatch', blocked: cfg.enableSkillBlockOnMismatch };
}

function scoreJobHistory(
  installer: ServiceInstaller,
  job: JobContext,
  allSlots: CalendarSlot[]
): { score: number; reason: string } {
  const cfg = getSchedulerConfig();
  const weight = cfg.installerScoringWeights.jobHistory;
  const th = cfg.scoringThresholds;
  const installerSlots = getInstallerAllSlots(allSlots, installer.id);

  const jobType = (job.orderType || job.serviceType || '').toLowerCase();
  if (!jobType || installerSlots.length === 0) {
    return { score: weight * th.noDataFactor, reason: 'No history data' };
  }

  const similarJobs = installerSlots.filter((s) => {
    const slotType = (s.serviceType || '').toLowerCase();
    return slotType && (slotType.includes(jobType) || jobType.includes(slotType));
  });

  if (similarJobs.length > 0) {
    return { score: weight, reason: `${similarJobs.length} similar jobs done` };
  }

  const sameBuildingJobs = installerSlots.filter(
    (s) =>
      job.buildingName &&
      s.buildingName &&
      s.buildingName.toLowerCase() === job.buildingName.toLowerCase()
  );

  if (sameBuildingJobs.length > 0) {
    return { score: weight * 0.8, reason: 'Familiar with building' };
  }

  return { score: weight * 0.3, reason: 'No similar jobs' };
}

export function getInstallerScore(
  installer: ServiceInstaller,
  job: JobContext,
  allSlots: CalendarSlot[],
  workload: InstallerWorkload,
  date: string
): ScoringResult {
  const cfg = getSchedulerConfig();
  const weights = cfg.installerScoringWeights;
  const th = cfg.scoringThresholds;
  const installerSlots = getInstallerAllSlots(allSlots, installer.id);

  const avail = scoreAvailability(installerSlots, job, date);
  if (avail.blocked) {
    return {
      installerId: installer.id,
      score: th.blockedScore,
      reasons: [avail.blockReason || 'Time conflict'],
      breakdown: { availability: th.blockedScore, workload: 0, distance: 0, skillMatch: 0, jobHistory: 0 },
      blocked: true,
      blockReason: avail.blockReason,
    };
  }

  const wl = scoreWorkload(workload);
  const dist = scoreDistance(installer, job);
  const skill = scoreSkillMatch(installer, job);
  const history = scoreJobHistory(installer, job, allSlots);

  const totalScore = avail.score + wl.score + dist.score + skill.score + history.score;

  const reasons: string[] = [];
  if (avail.score === weights.availability) reasons.push('Available');
  if (wl.score >= weights.workload * th.partialMatchFactor) reasons.push(wl.reason);
  if (dist.score >= weights.distance * 0.7) reasons.push(dist.reason);
  if (skill.score >= weights.skillMatch * th.partialMatchFactor) reasons.push(skill.reason);
  if (history.score >= weights.jobHistory * th.noDataFactor) reasons.push(history.reason);

  return {
    installerId: installer.id,
    score: Math.round(totalScore * 10) / 10,
    reasons,
    breakdown: {
      availability: avail.score,
      workload: wl.score,
      distance: dist.score,
      skillMatch: skill.score,
      jobHistory: history.score,
    },
    blocked: skill.blocked,
    blockReason: skill.blocked ? skill.reason : undefined,
  };
}

export function rankInstallers(
  installers: ServiceInstaller[],
  job: JobContext,
  allSlots: CalendarSlot[],
  workloads: InstallerWorkload[],
  date: string
): ScoringResult[] {
  const workloadMap = new Map(workloads.map((w) => [w.installerId, w]));
  const defaultWorkload: InstallerWorkload = {
    installerId: '',
    jobCount: 0,
    totalMinutes: 0,
    utilizationPct: 0,
    level: 'free',
  };

  return installers
    .map((inst) => {
      const wl = workloadMap.get(inst.id) || { ...defaultWorkload, installerId: inst.id };
      return getInstallerScore(inst, job, allSlots, wl, date);
    })
    .sort((a, b) => {
      if (a.blocked && !b.blocked) return 1;
      if (!a.blocked && b.blocked) return -1;
      return b.score - a.score;
    });
}

export function computeWorkloads(
  installers: ServiceInstaller[],
  slots: CalendarSlot[]
): InstallerWorkload[] {
  const cfg = getSchedulerConfig();
  const maxMinutes = cfg.maxWorkingMinutesPerDay;
  const thresholds = cfg.workloadThresholds;

  return installers.map((inst) => {
    const installerSlots = slots.filter(
      (s) => (s.serviceInstallerId || s.siId) === inst.id
    );
    const jobCount = installerSlots.length;
    const totalMinutes = installerSlots.reduce((sum, s) => sum + getSlotDuration(s), 0);
    const utilizationPct = Math.min(Math.round((totalMinutes / maxMinutes) * 100), 100);
    let level: 'free' | 'medium' | 'overloaded' = 'free';
    if (utilizationPct >= thresholds.mediumMaxPct || jobCount >= thresholds.mediumMaxJobs) level = 'overloaded';
    else if (utilizationPct >= thresholds.freeMaxPct || jobCount >= thresholds.freeMaxJobs) level = 'medium';
    return { installerId: inst.id, jobCount, totalMinutes, utilizationPct, level };
  });
}
