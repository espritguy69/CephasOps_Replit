import type { ServiceInstaller } from '../../types/serviceInstallers';
import type { CalendarSlot } from '../../types/scheduler';

export interface ScoringResult {
  installerId: string;
  score: number;
  reasons: string[];
  breakdown: {
    availability: number;
    workload: number;
    distance: number;
    skillMatch: number;
    jobHistory: number;
  };
  blocked: boolean;
  blockReason?: string;
}

export interface InstallerWorkload {
  installerId: string;
  jobCount: number;
  totalMinutes: number;
  utilizationPct: number;
  level: 'free' | 'medium' | 'overloaded';
}

export interface JobContext {
  orderId: string;
  orderType?: string;
  serviceType?: string;
  buildingName?: string;
  address?: string;
  appointmentDate?: string;
  appointmentTime?: string;
  windowFrom?: string;
  windowTo?: string;
  customerName?: string;
  requiredSkills?: string[];
}


const WEIGHTS = {
  availability: 40,
  workload: 20,
  distance: 15,
  skillMatch: 15,
  jobHistory: 10,
} as const;

const MAX_WORKING_MINUTES = 480;
const WORKING_HOURS = { start: '08:00:00', end: '18:00:00' };
const DEFAULT_JOB_DURATION_MINUTES = 120;
const MAX_JOBS_PER_INSTALLER_PER_DAY = 5;

function parseTimeToMinutes(timeStr: string): number {
  const parts = timeStr.split(':').map(Number);
  return (parts[0] ?? 0) * 60 + (parts[1] ?? 0);
}

function minutesToTime(minutes: number): string {
  const h = Math.floor(minutes / 60).toString().padStart(2, '0');
  const m = (minutes % 60).toString().padStart(2, '0');
  return `${h}:${m}:00`;
}

function getSlotDuration(slot: CalendarSlot): number {
  const from = parseTimeToMinutes(slot.windowFrom || slot.startTime || '09:00');
  const to = parseTimeToMinutes(slot.windowTo || slot.endTime || '11:00');
  return Math.max(to - from, 0);
}

function hasTimeOverlap(
  existingSlots: CalendarSlot[],
  windowFrom: string,
  windowTo: string
): CalendarSlot | null {
  const newFrom = parseTimeToMinutes(windowFrom);
  const newTo = parseTimeToMinutes(windowTo);

  for (const slot of existingSlots) {
    const slotFrom = parseTimeToMinutes(slot.windowFrom || slot.startTime || '00:00');
    const slotTo = parseTimeToMinutes(slot.windowTo || slot.endTime || '00:00');
    if (newFrom < slotTo && newTo > slotFrom) {
      return slot;
    }
  }
  return null;
}

function scoreAvailability(
  installerSlots: CalendarSlot[],
  job: JobContext,
  date: string
): { score: number; blocked: boolean; blockReason?: string; conflictSlot?: CalendarSlot } {
  const dateSlotsRaw = installerSlots.filter((s) => s.date === date);

  if (dateSlotsRaw.length >= MAX_JOBS_PER_INSTALLER_PER_DAY) {
    return {
      score: -100,
      blocked: true,
      blockReason: `Max ${MAX_JOBS_PER_INSTALLER_PER_DAY} jobs per day reached`,
    };
  }

  if (job.windowFrom && job.windowTo) {
    const conflictSlot = hasTimeOverlap(dateSlotsRaw, job.windowFrom, job.windowTo);
    if (conflictSlot) {
      const hasAnyFreeSlot = checkAnyWindowAvailable(dateSlotsRaw);
      if (!hasAnyFreeSlot) {
        return {
          score: -100,
          blocked: true,
          blockReason: `No available time slots today`,
        };
      }
      return {
        score: WEIGHTS.availability * 0.5,
        blocked: false,
        blockReason: undefined,
      };
    }
    return { score: WEIGHTS.availability, blocked: false };
  }

  const hasAnyFreeSlot = checkAnyWindowAvailable(dateSlotsRaw);
  if (!hasAnyFreeSlot) {
    return {
      score: -100,
      blocked: true,
      blockReason: 'No available time slots today',
    };
  }

  return { score: WEIGHTS.availability, blocked: false };
}

function checkAnyWindowAvailable(dateSlotsRaw: CalendarSlot[]): boolean {
  const startMin = parseTimeToMinutes(WORKING_HOURS.start);
  const endMin = parseTimeToMinutes(WORKING_HOURS.end);
  for (let t = startMin; t + 60 <= endMin; t += 30) {
    const from = minutesToTime(t);
    const to = minutesToTime(t + DEFAULT_JOB_DURATION_MINUTES);
    if (parseTimeToMinutes(to) > endMin) continue;
    if (!hasTimeOverlap(dateSlotsRaw, from, to)) return true;
  }
  return false;
}

function scoreWorkload(
  workload: InstallerWorkload
): { score: number; reason: string } {
  const utilizationFactor = 1 - (workload.utilizationPct / 100);
  const score = WEIGHTS.workload * Math.max(utilizationFactor, 0);

  if (workload.jobCount === 0) return { score, reason: 'No jobs today' };
  if (workload.level === 'free') return { score, reason: 'Low workload' };
  if (workload.level === 'medium') return { score: score * 0.6, reason: 'Medium workload' };
  return { score: score * 0.2, reason: 'Overloaded' };
}

function scoreDistance(
  installer: ServiceInstaller,
  job: JobContext
): { score: number; reason: string } {
  if (!installer.address || !job.address) {
    return { score: WEIGHTS.distance * 0.5, reason: 'Distance unknown' };
  }

  const installerAddr = (installer.address || '').toLowerCase();
  const jobAddr = (job.address || '').toLowerCase();
  const buildingName = (job.buildingName || '').toLowerCase();

  const installerWords = new Set(installerAddr.split(/[\s,]+/).filter(w => w.length > 2));
  const jobWords = new Set([...jobAddr.split(/[\s,]+/).filter(w => w.length > 2), ...buildingName.split(/[\s,]+/).filter(w => w.length > 2)]);

  let matchCount = 0;
  for (const w of installerWords) {
    if (jobWords.has(w)) matchCount++;
  }

  const similarity = jobWords.size > 0 ? matchCount / jobWords.size : 0;

  if (similarity >= 0.5) return { score: WEIGHTS.distance, reason: 'Same area' };
  if (similarity >= 0.2) return { score: WEIGHTS.distance * 0.7, reason: 'Nearby area' };
  return { score: WEIGHTS.distance * 0.3, reason: 'Different area' };
}

function scoreSkillMatch(
  installer: ServiceInstaller,
  job: JobContext
): { score: number; reason: string; blocked: boolean } {
  const installerSkills = (installer.skills || [])
    .filter((s) => s.isActive)
    .map((s) => ({
      name: (s.skill?.name || '').toLowerCase(),
      code: (s.skill?.code || '').toLowerCase(),
      category: (s.skill?.category || '').toLowerCase(),
    }));

  if (installerSkills.length === 0) {
    return { score: WEIGHTS.skillMatch * 0.5, reason: 'Skills not specified', blocked: false };
  }

  const jobType = (job.orderType || job.serviceType || '').toLowerCase();

  if (!jobType) {
    return { score: WEIGHTS.skillMatch * 0.7, reason: 'Job type unspecified', blocked: false };
  }

  const hasMatch = installerSkills.some(
    (s) =>
      s.name.includes(jobType) ||
      s.code.includes(jobType) ||
      jobType.includes(s.name) ||
      jobType.includes(s.code)
  );

  if (hasMatch) return { score: WEIGHTS.skillMatch, reason: 'Skill match', blocked: false };

  const categoryMatch = installerSkills.some((s) => {
    if (jobType.includes('fiber') || jobType.includes('ftth') || jobType.includes('ftto'))
      return s.category === 'fiberskills';
    if (jobType.includes('network') || jobType.includes('router'))
      return s.category === 'networkequipment';
    return false;
  });

  if (categoryMatch) return { score: WEIGHTS.skillMatch * 0.6, reason: 'Partial skill match', blocked: false };

  return { score: -50, reason: 'Skill mismatch', blocked: false };
}

function scoreJobHistory(
  installer: ServiceInstaller,
  job: JobContext,
  allSlots: CalendarSlot[]
): { score: number; reason: string } {
  const installerSlots = allSlots.filter(
    (s) => (s.serviceInstallerId || s.siId) === installer.id
  );

  const jobType = (job.orderType || job.serviceType || '').toLowerCase();
  if (!jobType || installerSlots.length === 0) {
    return { score: WEIGHTS.jobHistory * 0.5, reason: 'No history data' };
  }

  const similarJobs = installerSlots.filter((s) => {
    const slotType = (s.serviceType || '').toLowerCase();
    return slotType && (slotType.includes(jobType) || jobType.includes(slotType));
  });

  if (similarJobs.length > 0) {
    return { score: WEIGHTS.jobHistory, reason: `${similarJobs.length} similar jobs done` };
  }

  const sameBuildingJobs = installerSlots.filter(
    (s) =>
      job.buildingName &&
      s.buildingName &&
      s.buildingName.toLowerCase() === job.buildingName.toLowerCase()
  );

  if (sameBuildingJobs.length > 0) {
    return { score: WEIGHTS.jobHistory * 0.8, reason: 'Familiar with building' };
  }

  return { score: WEIGHTS.jobHistory * 0.3, reason: 'No similar jobs' };
}

export function computeWorkloads(
  installers: ServiceInstaller[],
  slots: CalendarSlot[],
  maxMinutes: number = MAX_WORKING_MINUTES
): InstallerWorkload[] {
  return installers.map((inst) => {
    const installerSlots = slots.filter(
      (s) => (s.serviceInstallerId || s.siId) === inst.id
    );
    const jobCount = installerSlots.length;
    const totalMinutes = installerSlots.reduce((sum, s) => sum + getSlotDuration(s), 0);
    const utilizationPct = Math.min(Math.round((totalMinutes / maxMinutes) * 100), 100);
    let level: 'free' | 'medium' | 'overloaded' = 'free';
    if (utilizationPct >= 85 || jobCount >= 6) level = 'overloaded';
    else if (utilizationPct >= 50 || jobCount >= 3) level = 'medium';
    return { installerId: inst.id, jobCount, totalMinutes, utilizationPct, level };
  });
}

export function getInstallerScore(
  installer: ServiceInstaller,
  job: JobContext,
  allSlots: CalendarSlot[],
  workload: InstallerWorkload,
  date: string
): ScoringResult {
  const installerSlots = allSlots.filter(
    (s) => (s.serviceInstallerId || s.siId) === installer.id
  );

  const avail = scoreAvailability(installerSlots, job, date);
  if (avail.blocked) {
    return {
      installerId: installer.id,
      score: -100,
      reasons: [avail.blockReason || 'Time conflict'],
      breakdown: { availability: -100, workload: 0, distance: 0, skillMatch: 0, jobHistory: 0 },
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
  if (avail.score === WEIGHTS.availability) reasons.push('Available');
  if (wl.score >= WEIGHTS.workload * 0.6) reasons.push(wl.reason);
  if (dist.score >= WEIGHTS.distance * 0.7) reasons.push(dist.reason);
  if (skill.score >= WEIGHTS.skillMatch * 0.6) reasons.push(skill.reason);
  if (history.score >= WEIGHTS.jobHistory * 0.5) reasons.push(history.reason);

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

export function checkConflict(
  installerId: string,
  date: string,
  windowFrom: string,
  windowTo: string,
  allSlots: CalendarSlot[]
): { hasConflict: boolean; conflictSlot?: CalendarSlot; message?: string } {
  const installerDaySlots = allSlots.filter(
    (s) => (s.serviceInstallerId || s.siId) === installerId && s.date === date
  );

  const conflict = hasTimeOverlap(installerDaySlots, windowFrom, windowTo);
  if (conflict) {
    return {
      hasConflict: true,
      conflictSlot: conflict,
      message: `Time conflict: ${conflict.customerName || conflict.serviceId || 'existing job'} (${conflict.windowFrom?.slice(0, 5)}–${conflict.windowTo?.slice(0, 5)})`,
    };
  }

  return { hasConflict: false };
}

export interface AutoAssignment {
  orderId: string;
  installerId: string;
  installerName: string;
  score: number;
  reasons: string[];
  date: string;
  windowFrom: string;
  windowTo: string;
  windowReason: string;
}

export interface AutoAssignResult {
  assignments: AutoAssignment[];
  unassignable: Array<{
    orderId: string;
    reason: string;
  }>;
}

export interface WindowScore {
  windowFrom: string;
  windowTo: string;
  score: number;
  reasons: string[];
  isPreferred: boolean;
}

export type WindowStatus = 'PREFERRED' | 'BEST_FIT' | 'NO_SLOT';

export interface BestWindowResult {
  status: WindowStatus;
  windowFrom: string;
  windowTo: string;
  score: number;
  reason: string;
}

export interface NoSlotResult {
  status: 'NO_SLOT';
  reason: string;
}

const WINDOW_WEIGHTS = {
  gapFit: 30,
  travelClustering: 20,
  timePreference: 20,
  utilizationBalance: 15,
  idleTimeReduction: 15,
} as const;

function generateCandidateWindows(
  preferredFrom?: string,
  preferredTo?: string,
  durationMinutes: number = DEFAULT_JOB_DURATION_MINUTES
): Array<{ from: string; to: string; isPreferred: boolean }> {
  const candidates: Array<{ from: string; to: string; isPreferred: boolean }> = [];
  const seen = new Set<string>();

  if (preferredFrom && preferredTo) {
    const key = `${preferredFrom}-${preferredTo}`;
    candidates.push({ from: preferredFrom, to: preferredTo, isPreferred: true });
    seen.add(key);
  }

  const startMin = parseTimeToMinutes(WORKING_HOURS.start);
  const endMin = parseTimeToMinutes(WORKING_HOURS.end);

  for (let t = startMin; t + durationMinutes <= endMin; t += 30) {
    const from = minutesToTime(t);
    const to = minutesToTime(t + durationMinutes);
    const key = `${from}-${to}`;
    if (!seen.has(key)) {
      candidates.push({ from, to, isPreferred: false });
      seen.add(key);
    }
  }

  return candidates;
}

function scoreGapFit(
  windowFrom: string,
  windowTo: string,
  existingSlots: CalendarSlot[]
): { score: number; reason: string } {
  if (existingSlots.length === 0) {
    return { score: WINDOW_WEIGHTS.gapFit * 0.8, reason: 'First job of the day' };
  }

  const wFrom = parseTimeToMinutes(windowFrom);
  const wTo = parseTimeToMinutes(windowTo);

  const sorted = existingSlots
    .map((s) => ({
      from: parseTimeToMinutes(s.windowFrom || s.startTime || '00:00'),
      to: parseTimeToMinutes(s.windowTo || s.endTime || '00:00'),
    }))
    .sort((a, b) => a.from - b.from);

  let bestGapBefore = Infinity;
  let bestGapAfter = Infinity;
  let fitsCleanly = false;

  for (const slot of sorted) {
    if (slot.to <= wFrom) {
      bestGapBefore = Math.min(bestGapBefore, wFrom - slot.to);
    }
    if (slot.from >= wTo) {
      bestGapAfter = Math.min(bestGapAfter, slot.from - wTo);
    }
  }

  if (bestGapBefore === 0 || bestGapAfter === 0) {
    fitsCleanly = true;
  }

  if (fitsCleanly) {
    return { score: WINDOW_WEIGHTS.gapFit, reason: 'Fits cleanly between jobs' };
  }

  const smallestGap = Math.min(
    bestGapBefore === Infinity ? 999 : bestGapBefore,
    bestGapAfter === Infinity ? 999 : bestGapAfter
  );

  if (smallestGap <= 30) {
    return { score: WINDOW_WEIGHTS.gapFit * 0.85, reason: 'Near adjacent job' };
  }
  if (smallestGap <= 60) {
    return { score: WINDOW_WEIGHTS.gapFit * 0.6, reason: 'Small gap to next job' };
  }
  return { score: WINDOW_WEIGHTS.gapFit * 0.3, reason: 'Creates idle gap' };
}

function scoreTravelClustering(
  windowFrom: string,
  existingSlots: CalendarSlot[],
  job: JobContext
): { score: number; reason: string } {
  if (existingSlots.length === 0 || !job.address) {
    return { score: WINDOW_WEIGHTS.travelClustering * 0.5, reason: 'No clustering data' };
  }

  const wFrom = parseTimeToMinutes(windowFrom);
  const jobAddr = (job.address || '').toLowerCase();
  const jobBuilding = (job.buildingName || '').toLowerCase();

  let nearestSlot: CalendarSlot | null = null;
  let nearestDist = Infinity;

  for (const slot of existingSlots) {
    const slotEnd = parseTimeToMinutes(slot.windowTo || slot.endTime || '00:00');
    const slotStart = parseTimeToMinutes(slot.windowFrom || slot.startTime || '00:00');
    const dist = Math.min(Math.abs(wFrom - slotEnd), Math.abs(wFrom - slotStart));
    if (dist < nearestDist) {
      nearestDist = dist;
      nearestSlot = slot;
    }
  }

  if (!nearestSlot) {
    return { score: WINDOW_WEIGHTS.travelClustering * 0.5, reason: 'No adjacent jobs' };
  }

  const slotAddr = (nearestSlot.buildingName || '').toLowerCase();
  const addrWords = new Set(jobAddr.split(/[\s,]+/).filter((w) => w.length > 2));
  const buildWords = new Set(jobBuilding.split(/[\s,]+/).filter((w) => w.length > 2));
  const slotWords = new Set(slotAddr.split(/[\s,]+/).filter((w) => w.length > 2));
  const allJobWords = new Set([...addrWords, ...buildWords]);

  let matchCount = 0;
  for (const w of allJobWords) {
    if (slotWords.has(w)) matchCount++;
  }

  if (allJobWords.size > 0 && matchCount / allJobWords.size >= 0.4) {
    return { score: WINDOW_WEIGHTS.travelClustering, reason: 'Near previous job location' };
  }
  if (matchCount > 0) {
    return { score: WINDOW_WEIGHTS.travelClustering * 0.6, reason: 'Partial area match' };
  }
  return { score: WINDOW_WEIGHTS.travelClustering * 0.3, reason: 'Different area' };
}

function scoreTimePreference(
  windowFrom: string,
  windowTo: string,
  preferredFrom?: string,
  preferredTo?: string
): { score: number; reason: string } {
  if (!preferredFrom || !preferredTo) {
    return { score: WINDOW_WEIGHTS.timePreference * 0.7, reason: 'No preference specified' };
  }

  if (windowFrom === preferredFrom && windowTo === preferredTo) {
    return { score: WINDOW_WEIGHTS.timePreference, reason: 'Scheduled as requested' };
  }

  const prefStart = parseTimeToMinutes(preferredFrom);
  const actStart = parseTimeToMinutes(windowFrom);
  const diff = Math.abs(prefStart - actStart);

  if (diff <= 30) return { score: WINDOW_WEIGHTS.timePreference * 0.9, reason: 'Close to requested time' };
  if (diff <= 60) return { score: WINDOW_WEIGHTS.timePreference * 0.7, reason: 'Within 1 hour of preference' };
  if (diff <= 120) return { score: WINDOW_WEIGHTS.timePreference * 0.4, reason: '1-2 hours from preference' };
  return { score: WINDOW_WEIGHTS.timePreference * 0.1, reason: 'Far from preferred time' };
}

function scoreUtilizationBalance(
  windowFrom: string,
  windowTo: string,
  existingSlots: CalendarSlot[]
): { score: number; reason: string } {
  const totalExisting = existingSlots.reduce((sum, s) => sum + getSlotDuration(s), 0);
  const newDuration = parseTimeToMinutes(windowTo) - parseTimeToMinutes(windowFrom);
  const totalAfter = totalExisting + newDuration;
  const utilizationAfter = totalAfter / MAX_WORKING_MINUTES;

  if (utilizationAfter <= 0.6) {
    return { score: WINDOW_WEIGHTS.utilizationBalance, reason: 'Balanced workload' };
  }
  if (utilizationAfter <= 0.8) {
    return { score: WINDOW_WEIGHTS.utilizationBalance * 0.7, reason: 'Moderate workload' };
  }
  if (utilizationAfter <= 0.95) {
    return { score: WINDOW_WEIGHTS.utilizationBalance * 0.3, reason: 'Heavy workload' };
  }
  return { score: 0, reason: 'Would exceed capacity' };
}

function scoreIdleTimeReduction(
  windowFrom: string,
  windowTo: string,
  existingSlots: CalendarSlot[]
): { score: number; reason: string } {
  if (existingSlots.length === 0) {
    return { score: WINDOW_WEIGHTS.idleTimeReduction * 0.5, reason: 'First assignment' };
  }

  const allTimes = existingSlots
    .map((s) => ({
      from: parseTimeToMinutes(s.windowFrom || s.startTime || '00:00'),
      to: parseTimeToMinutes(s.windowTo || s.endTime || '00:00'),
    }))
    .concat([{ from: parseTimeToMinutes(windowFrom), to: parseTimeToMinutes(windowTo) }])
    .sort((a, b) => a.from - b.from);

  let totalIdle = 0;
  for (let i = 1; i < allTimes.length; i++) {
    const gap = allTimes[i].from - allTimes[i - 1].to;
    if (gap > 0) totalIdle += gap;
  }

  const allTimesWithout = existingSlots
    .map((s) => ({
      from: parseTimeToMinutes(s.windowFrom || s.startTime || '00:00'),
      to: parseTimeToMinutes(s.windowTo || s.endTime || '00:00'),
    }))
    .sort((a, b) => a.from - b.from);

  let idleBefore = 0;
  for (let i = 1; i < allTimesWithout.length; i++) {
    const gap = allTimesWithout[i].from - allTimesWithout[i - 1].to;
    if (gap > 0) idleBefore += gap;
  }

  const reduction = idleBefore - totalIdle;
  if (reduction > 0) {
    return { score: WINDOW_WEIGHTS.idleTimeReduction, reason: `Reduces idle time by ${reduction}min` };
  }
  if (totalIdle <= 60) {
    return { score: WINDOW_WEIGHTS.idleTimeReduction * 0.7, reason: 'Minimal idle time' };
  }
  return { score: WINDOW_WEIGHTS.idleTimeReduction * 0.3, reason: 'Creates idle gaps' };
}

export function getWindowScore(
  windowFrom: string,
  windowTo: string,
  installerSlots: CalendarSlot[],
  job: JobContext,
  preferredFrom?: string,
  preferredTo?: string,
  isPreferred: boolean = false
): WindowScore {
  const gapFit = scoreGapFit(windowFrom, windowTo, installerSlots);
  const travel = scoreTravelClustering(windowFrom, installerSlots, job);
  const timePref = scoreTimePreference(windowFrom, windowTo, preferredFrom, preferredTo);
  const utilBal = scoreUtilizationBalance(windowFrom, windowTo, installerSlots);
  const idleRed = scoreIdleTimeReduction(windowFrom, windowTo, installerSlots);

  const total = gapFit.score + travel.score + timePref.score + utilBal.score + idleRed.score;
  const reasons: string[] = [];

  if (gapFit.score >= WINDOW_WEIGHTS.gapFit * 0.7) reasons.push(gapFit.reason);
  if (travel.score >= WINDOW_WEIGHTS.travelClustering * 0.6) reasons.push(travel.reason);
  if (timePref.score >= WINDOW_WEIGHTS.timePreference * 0.8) reasons.push(timePref.reason);
  if (utilBal.score >= WINDOW_WEIGHTS.utilizationBalance * 0.6) reasons.push(utilBal.reason);
  if (idleRed.score >= WINDOW_WEIGHTS.idleTimeReduction * 0.6) reasons.push(idleRed.reason);

  return {
    windowFrom,
    windowTo,
    score: Math.round(total * 10) / 10,
    reasons,
    isPreferred,
  };
}

export function findBestWindow(
  installerId: string,
  date: string,
  dynamicSlots: CalendarSlot[],
  job: JobContext,
  preferredFrom?: string,
  preferredTo?: string
): BestWindowResult | NoSlotResult {
  const installerDaySlots = dynamicSlots.filter(
    (s) => (s.serviceInstallerId || s.siId) === installerId && s.date === date
  );

  if (installerDaySlots.length >= MAX_JOBS_PER_INSTALLER_PER_DAY) {
    return { status: 'NO_SLOT', reason: `Max ${MAX_JOBS_PER_INSTALLER_PER_DAY} jobs per day reached` };
  }

  const duration = preferredFrom && preferredTo
    ? parseTimeToMinutes(preferredTo) - parseTimeToMinutes(preferredFrom)
    : DEFAULT_JOB_DURATION_MINUTES;

  const candidates = generateCandidateWindows(preferredFrom, preferredTo, Math.max(duration, 30));

  const validWindows: WindowScore[] = [];

  for (const cand of candidates) {
    const candFrom = parseTimeToMinutes(cand.from);
    const candTo = parseTimeToMinutes(cand.to);
    const workStart = parseTimeToMinutes(WORKING_HOURS.start);
    const workEnd = parseTimeToMinutes(WORKING_HOURS.end);
    if (candFrom < workStart || candTo > workEnd) continue;

    const conflict = hasTimeOverlap(installerDaySlots, cand.from, cand.to);
    if (conflict) continue;

    const scored = getWindowScore(
      cand.from,
      cand.to,
      installerDaySlots,
      job,
      preferredFrom,
      preferredTo,
      cand.isPreferred
    );
    validWindows.push(scored);
  }

  if (validWindows.length === 0) {
    return { status: 'NO_SLOT', reason: 'No available time slots today' };
  }

  validWindows.sort((a, b) => b.score - a.score);
  const best = validWindows[0];

  return {
    status: best.isPreferred ? 'PREFERRED' : 'BEST_FIT',
    windowFrom: best.windowFrom,
    windowTo: best.windowTo,
    score: best.score,
    reason: best.isPreferred
      ? 'Scheduled as requested'
      : `Suggested: ${best.windowFrom.slice(0, 5)}–${best.windowTo.slice(0, 5)} — ${best.reasons[0] || 'Best available fit'}`,
  };
}

export function autoAssignJobs(
  jobs: JobContext[],
  installers: ServiceInstaller[],
  allSlots: CalendarSlot[],
  fallbackDate: string
): AutoAssignResult {
  const assignments: AutoAssignment[] = [];
  const unassignable: AutoAssignResult['unassignable'] = [];

  let dynamicSlots = [...allSlots];

  const sortedJobs = [...jobs].sort((a, b) => {
    const timeA = a.appointmentTime || a.windowFrom || '23:59';
    const timeB = b.appointmentTime || b.windowFrom || '23:59';
    return timeA.localeCompare(timeB);
  });

  for (const job of sortedJobs) {
    const jobDate = job.appointmentDate || fallbackDate;
    const jobWithWindow = {
      ...job,
      windowFrom: job.windowFrom || undefined,
      windowTo: job.windowTo || undefined,
    };

    const workloads = computeWorkloads(installers, dynamicSlots);
    const ranked = rankInstallers(installers, jobWithWindow, dynamicSlots, workloads, jobDate);

    let assigned = false;

    for (const candidate of ranked) {
      if (candidate.blocked || candidate.score <= 0) continue;

      const windowResult = findBestWindow(
        candidate.installerId,
        jobDate,
        dynamicSlots,
        job,
        job.windowFrom,
        job.windowTo
      );

      if (windowResult.status === 'NO_SLOT') continue;

      const combinedScore = candidate.score + windowResult.score;
      const installer = installers.find((i) => i.id === candidate.installerId)!;
      assignments.push({
        orderId: job.orderId,
        installerId: candidate.installerId,
        installerName: installer.name,
        score: Math.round(combinedScore * 10) / 10,
        reasons: [...candidate.reasons, windowResult.reason],
        date: jobDate,
        windowFrom: windowResult.windowFrom,
        windowTo: windowResult.windowTo,
        windowReason: windowResult.reason,
      });

      const syntheticSlot: CalendarSlot = {
        id: `auto-${job.orderId}`,
        orderId: job.orderId,
        serviceInstallerId: candidate.installerId,
        date: jobDate,
        windowFrom: windowResult.windowFrom,
        windowTo: windowResult.windowTo,
        sequenceIndex: 0,
        status: 'Draft',
        createdByUserId: '',
        createdAt: new Date().toISOString(),
        serviceType: job.serviceType || job.orderType,
        customerName: job.customerName,
        buildingName: job.buildingName,
      };
      dynamicSlots.push(syntheticSlot);
      assigned = true;
      break;
    }

    if (!assigned) {
      unassignable.push({
        orderId: job.orderId,
        reason: ranked[0]?.blockReason || 'No available time slot for any installer',
      });
    }
  }

  return { assignments, unassignable };
}

export function smartDistribute(
  jobs: JobContext[],
  installers: ServiceInstaller[],
  allSlots: CalendarSlot[],
  fallbackDate: string
): AutoAssignResult {
  return autoAssignJobs(jobs, installers, allSlots, fallbackDate);
}
