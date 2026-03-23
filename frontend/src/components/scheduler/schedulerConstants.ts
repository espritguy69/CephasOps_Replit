/**
 * Scheduler UI constants – status-based card colors and labels
 * Aligned with CephasOps operations (activation, assurance, blocker, reschedule, at-risk)
 */

export const SCHEDULER_CARD_STATUS = {
  /** Activation / installation */
  activation: 'bg-blue-50 border-blue-200 text-blue-900 dark:bg-blue-950/50 dark:border-blue-800 dark:text-blue-100',
  /** Assurance / service */
  assurance: 'bg-emerald-50 border-emerald-200 text-emerald-900 dark:bg-emerald-950/50 dark:border-emerald-800 dark:text-emerald-100',
  /** Reschedule pending */
  reschedule: 'bg-amber-50 border-amber-200 text-amber-900 dark:bg-amber-950/50 dark:border-amber-800 dark:text-amber-100',
  /** Blocker */
  blocker: 'bg-red-50 border-red-200 text-red-900 dark:bg-red-950/50 dark:border-red-800 dark:text-red-100',
  /** Completed */
  completed: 'bg-slate-50 border-slate-200 text-slate-700 dark:bg-slate-900/50 dark:border-slate-700 dark:text-slate-200',
  /** At-risk / nearing SLA */
  atRisk: 'bg-orange-50 border-orange-200 text-orange-900 dark:bg-orange-950/50 dark:border-orange-800 dark:text-orange-100',
  /** Draft slot */
  draft: 'bg-gray-50 border-gray-200 text-gray-800 dark:bg-gray-900/50 dark:border-gray-700 dark:text-gray-200',
  /** Default */
  default: 'bg-gray-50 border-gray-200 text-gray-800 dark:bg-gray-900/50 dark:border-gray-700 dark:text-gray-200',
} as const;

/** Map order/slot status to scheduler card status key */
export function getSchedulerCardStatusClass(
  orderStatus?: string | null,
  slotStatus?: string | null
): string {
  const status = (orderStatus || slotStatus || '').toLowerCase();
  if (status.includes('blocker') || status.includes('blocked')) return SCHEDULER_CARD_STATUS.blocker;
  if (status.includes('reschedule')) return SCHEDULER_CARD_STATUS.reschedule;
  if (status.includes('completed') || status.includes('invoiced') || status === 'completed') return SCHEDULER_CARD_STATUS.completed;
  if (status.includes('assurance') || status.includes('service')) return SCHEDULER_CARD_STATUS.assurance;
  if (status.includes('activation') || status.includes('installation') || status.includes('assigned')) return SCHEDULER_CARD_STATUS.activation;
  if (slotStatus === 'Draft') return SCHEDULER_CARD_STATUS.draft;
  return SCHEDULER_CARD_STATUS.default;
}

/** Default work day range (hours) */
export const SCHEDULER_START_HOUR = 8;
export const SCHEDULER_END_HOUR = 18;
/** Slot height in pixels (1 hour) */
export const SCHEDULER_HOUR_HEIGHT = 64;
/** Minutes per slot for time axis labels */
export const SCHEDULER_SLOT_MINUTES = 60;
