import type {
  ServiceInstaller,
  CalendarSlot,
  JobContext,
  AutoAssignment,
  AutoAssignResult,
} from '../types';
import { computeWorkloads, rankInstallers } from './scoringService';
import { findBestWindow } from './timeSlotService';

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
