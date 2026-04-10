import type {
  CalendarSlot,
  JobContext,
  WindowScore,
  BestWindowResult,
  NoSlotResult,
} from '../types';
import { getSchedulerConfig } from '../config/schedulerConfig';
import { parseTimeToMinutes, minutesToTime, getSlotDuration, getInstallerDaySlots } from './timeUtils';
import { hasTimeOverlap } from './conflictService';

export function checkAnyWindowAvailable(dateSlotsRaw: CalendarSlot[]): boolean {
  const cfg = getSchedulerConfig();
  const startMin = parseTimeToMinutes(cfg.workingHours.start);
  const endMin = parseTimeToMinutes(cfg.workingHours.end);
  for (let t = startMin; t + 60 <= endMin; t += cfg.slotIntervalMinutes) {
    const from = minutesToTime(t);
    const to = minutesToTime(t + cfg.defaultJobDurationMinutes);
    if (parseTimeToMinutes(to) > endMin) continue;
    if (!hasTimeOverlap(dateSlotsRaw, from, to)) return true;
  }
  return false;
}

function generateCandidateWindows(
  preferredFrom?: string,
  preferredTo?: string,
  durationMinutes?: number
): Array<{ from: string; to: string; isPreferred: boolean }> {
  const cfg = getSchedulerConfig();
  const dur = durationMinutes ?? cfg.defaultJobDurationMinutes;
  const candidates: Array<{ from: string; to: string; isPreferred: boolean }> = [];
  const seen = new Set<string>();

  if (preferredFrom && preferredTo) {
    const key = `${preferredFrom}-${preferredTo}`;
    candidates.push({ from: preferredFrom, to: preferredTo, isPreferred: true });
    seen.add(key);
  }

  const startMin = parseTimeToMinutes(cfg.workingHours.start);
  const endMin = parseTimeToMinutes(cfg.workingHours.end);

  for (let t = startMin; t + dur <= endMin; t += cfg.slotIntervalMinutes) {
    const from = minutesToTime(t);
    const to = minutesToTime(t + dur);
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
  const weight = getSchedulerConfig().windowScoringWeights.gapFit;

  if (existingSlots.length === 0) {
    return { score: weight * 0.8, reason: 'First job of the day' };
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

  for (const slot of sorted) {
    if (slot.to <= wFrom) bestGapBefore = Math.min(bestGapBefore, wFrom - slot.to);
    if (slot.from >= wTo) bestGapAfter = Math.min(bestGapAfter, slot.from - wTo);
  }

  const fitsCleanly = bestGapBefore === 0 || bestGapAfter === 0;

  if (fitsCleanly) return { score: weight, reason: 'Fits cleanly between jobs' };

  const smallestGap = Math.min(
    bestGapBefore === Infinity ? 999 : bestGapBefore,
    bestGapAfter === Infinity ? 999 : bestGapAfter
  );

  if (smallestGap <= 30) return { score: weight * 0.85, reason: 'Near adjacent job' };
  if (smallestGap <= 60) return { score: weight * 0.6, reason: 'Small gap to next job' };
  return { score: weight * 0.3, reason: 'Creates idle gap' };
}

function scoreTravelClustering(
  windowFrom: string,
  existingSlots: CalendarSlot[],
  job: JobContext
): { score: number; reason: string } {
  const weight = getSchedulerConfig().windowScoringWeights.travelClustering;

  if (existingSlots.length === 0 || !job.address) {
    return { score: weight * 0.5, reason: 'No clustering data' };
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

  if (!nearestSlot) return { score: weight * 0.5, reason: 'No adjacent jobs' };

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
    return { score: weight, reason: 'Near previous job location' };
  }
  if (matchCount > 0) return { score: weight * 0.6, reason: 'Partial area match' };
  return { score: weight * 0.3, reason: 'Different area' };
}

function scoreTimePreference(
  windowFrom: string,
  windowTo: string,
  preferredFrom?: string,
  preferredTo?: string
): { score: number; reason: string } {
  const weight = getSchedulerConfig().windowScoringWeights.timePreference;

  if (!preferredFrom || !preferredTo) {
    return { score: weight * 0.7, reason: 'No preference specified' };
  }

  if (windowFrom === preferredFrom && windowTo === preferredTo) {
    return { score: weight, reason: 'Scheduled as requested' };
  }

  const prefStart = parseTimeToMinutes(preferredFrom);
  const actStart = parseTimeToMinutes(windowFrom);
  const diff = Math.abs(prefStart - actStart);

  if (diff <= 30) return { score: weight * 0.9, reason: 'Close to requested time' };
  if (diff <= 60) return { score: weight * 0.7, reason: 'Within 1 hour of preference' };
  if (diff <= 120) return { score: weight * 0.4, reason: '1-2 hours from preference' };
  return { score: weight * 0.1, reason: 'Far from preferred time' };
}

function scoreUtilizationBalance(
  windowFrom: string,
  windowTo: string,
  existingSlots: CalendarSlot[]
): { score: number; reason: string } {
  const cfg = getSchedulerConfig();
  const weight = cfg.windowScoringWeights.utilizationBalance;
  const totalExisting = existingSlots.reduce((sum, s) => sum + getSlotDuration(s), 0);
  const newDuration = parseTimeToMinutes(windowTo) - parseTimeToMinutes(windowFrom);
  const totalAfter = totalExisting + newDuration;
  const utilizationAfter = totalAfter / cfg.maxWorkingMinutesPerDay;

  if (utilizationAfter <= 0.6) return { score: weight, reason: 'Balanced workload' };
  if (utilizationAfter <= 0.8) return { score: weight * 0.7, reason: 'Moderate workload' };
  if (utilizationAfter <= 0.95) return { score: weight * 0.3, reason: 'Heavy workload' };
  return { score: 0, reason: 'Would exceed capacity' };
}

function scoreIdleTimeReduction(
  windowFrom: string,
  windowTo: string,
  existingSlots: CalendarSlot[]
): { score: number; reason: string } {
  const weight = getSchedulerConfig().windowScoringWeights.idleTimeReduction;

  if (existingSlots.length === 0) {
    return { score: weight * 0.5, reason: 'First assignment' };
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
  if (reduction > 0) return { score: weight, reason: `Reduces idle time by ${reduction}min` };
  if (totalIdle <= 60) return { score: weight * 0.7, reason: 'Minimal idle time' };
  return { score: weight * 0.3, reason: 'Creates idle gaps' };
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
  const weights = getSchedulerConfig().windowScoringWeights;

  const gapFit = scoreGapFit(windowFrom, windowTo, installerSlots);
  const travel = scoreTravelClustering(windowFrom, installerSlots, job);
  const timePref = scoreTimePreference(windowFrom, windowTo, preferredFrom, preferredTo);
  const utilBal = scoreUtilizationBalance(windowFrom, windowTo, installerSlots);
  const idleRed = scoreIdleTimeReduction(windowFrom, windowTo, installerSlots);

  const total = gapFit.score + travel.score + timePref.score + utilBal.score + idleRed.score;
  const reasons: string[] = [];

  if (gapFit.score >= weights.gapFit * 0.7) reasons.push(gapFit.reason);
  if (travel.score >= weights.travelClustering * 0.6) reasons.push(travel.reason);
  if (timePref.score >= weights.timePreference * 0.8) reasons.push(timePref.reason);
  if (utilBal.score >= weights.utilizationBalance * 0.6) reasons.push(utilBal.reason);
  if (idleRed.score >= weights.idleTimeReduction * 0.6) reasons.push(idleRed.reason);

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
  const cfg = getSchedulerConfig();
  const installerDaySlots = getInstallerDaySlots(dynamicSlots, installerId, date);

  if (installerDaySlots.length >= cfg.maxJobsPerInstallerPerDay) {
    return { status: 'NO_SLOT', reason: `Max ${cfg.maxJobsPerInstallerPerDay} jobs per day reached` };
  }

  const duration = preferredFrom && preferredTo
    ? parseTimeToMinutes(preferredTo) - parseTimeToMinutes(preferredFrom)
    : cfg.defaultJobDurationMinutes;

  const candidates = generateCandidateWindows(preferredFrom, preferredTo, Math.max(duration, 30));

  const validWindows: WindowScore[] = [];
  const workStart = parseTimeToMinutes(cfg.workingHours.start);
  const workEnd = parseTimeToMinutes(cfg.workingHours.end);

  for (const cand of candidates) {
    const candFrom = parseTimeToMinutes(cand.from);
    const candTo = parseTimeToMinutes(cand.to);
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
