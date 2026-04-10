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

export interface AutoAssignResult {
  assignments: Array<{
    orderId: string;
    installerId: string;
    installerName: string;
    score: number;
    reasons: string[];
  }>;
  unassignable: Array<{
    orderId: string;
    reason: string;
  }>;
}

const WEIGHTS = {
  availability: 40,
  workload: 20,
  distance: 15,
  skillMatch: 15,
  jobHistory: 10,
} as const;

const MAX_WORKING_MINUTES = 480;

function parseTimeToMinutes(timeStr: string): number {
  const parts = timeStr.split(':').map(Number);
  return (parts[0] ?? 0) * 60 + (parts[1] ?? 0);
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
  const windowFrom = job.windowFrom || job.appointmentTime || '09:00:00';
  const windowTo = job.windowTo || '11:00:00';

  const dateSlotsRaw = installerSlots.filter((s) => s.date === date);
  const conflictSlot = hasTimeOverlap(dateSlotsRaw, windowFrom, windowTo);

  if (conflictSlot) {
    return {
      score: -100,
      blocked: true,
      blockReason: `Time conflict with ${conflictSlot.customerName || conflictSlot.serviceId || 'existing job'} (${conflictSlot.windowFrom?.slice(0, 5)}–${conflictSlot.windowTo?.slice(0, 5)})`,
      conflictSlot,
    };
  }

  return { score: WEIGHTS.availability, blocked: false };
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

export function autoAssignJobs(
  jobs: JobContext[],
  installers: ServiceInstaller[],
  allSlots: CalendarSlot[],
  date: string
): AutoAssignResult {
  const assignments: AutoAssignResult['assignments'] = [];
  const unassignable: AutoAssignResult['unassignable'] = [];

  let dynamicSlots = [...allSlots];

  const sortedJobs = [...jobs].sort((a, b) => {
    const timeA = a.appointmentTime || a.windowFrom || '23:59';
    const timeB = b.appointmentTime || b.windowFrom || '23:59';
    return timeA.localeCompare(timeB);
  });

  for (const job of sortedJobs) {
    const workloads = computeWorkloads(installers, dynamicSlots);
    const ranked = rankInstallers(installers, job, dynamicSlots, workloads, date);
    const best = ranked.find((r) => !r.blocked && r.score > 0);

    if (!best) {
      unassignable.push({
        orderId: job.orderId,
        reason: ranked[0]?.blockReason || 'No suitable installer found',
      });
      continue;
    }

    const installer = installers.find((i) => i.id === best.installerId)!;
    assignments.push({
      orderId: job.orderId,
      installerId: best.installerId,
      installerName: installer.name,
      score: best.score,
      reasons: best.reasons,
    });

    const syntheticSlot: CalendarSlot = {
      id: `auto-${job.orderId}`,
      orderId: job.orderId,
      serviceInstallerId: best.installerId,
      date,
      windowFrom: job.windowFrom || '09:00:00',
      windowTo: job.windowTo || '11:00:00',
      sequenceIndex: 0,
      status: 'Draft',
      createdByUserId: '',
      createdAt: new Date().toISOString(),
      serviceType: job.serviceType || job.orderType,
      customerName: job.customerName,
      buildingName: job.buildingName,
    };
    dynamicSlots.push(syntheticSlot);
  }

  return { assignments, unassignable };
}

export function smartDistribute(
  jobs: JobContext[],
  installers: ServiceInstaller[],
  allSlots: CalendarSlot[],
  date: string
): AutoAssignResult {
  return autoAssignJobs(jobs, installers, allSlots, date);
}
